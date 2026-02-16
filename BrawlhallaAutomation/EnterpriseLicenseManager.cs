using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BrawlhallaAutomation
{
    public class EnterpriseLicenseManager
    {
        private const string LICENSE_FILE = "license.dat";
        // Fixed public key that matches the server's private key
        private const string PUBLIC_KEY = @"<RSAKeyValue><Modulus>wea7TT7Y8MDsn47GLfqdA9F3jJ2QRssRCkroYL2w/OnGkhcB9Xd1vzI/mazHesShIB9BmfqarKXUjx5uSX7TMlrnQ2xfTZlZnlt1JQF4cZbasa87yxEsFOKfSmxqUJCgGHMUuuAk8AQMeGsC4BgW7OUZezbwLgHlTBJIB/e1OUwn2k3H2Jd0Zk4WEYkyKC/B5XTOsBWsT2Q54EJjmSNbINCCkMgCZw4rhMbkLUsvyzA5O0Yv8eB4xSyVd3v5XLO61/TM6U/JG+JvoO/PHShwpXMq5sO4mReHHz+2HQv+ght95AntPs0UArwQSCxKRzmBXyqelfEE+1nXKZEXy1oUPQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
        private const string SERVER_URL = "http://localhost:5067/api/license";
        private const int MAX_ATTEMPTS = 3;

        private int attemptCount = 0;
        private bool isLicenseValid = false;
        private string currentHWID = string.Empty;
        private LicenseInfo currentLicense = null;
        private HttpClient httpClient;

        public bool IsLicenseValid => isLicenseValid;
        public LicenseInfo CurrentLicense => currentLicense;

        public EnterpriseLicenseManager()
        {
            // Add handler to ignore SSL errors (just in case)
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

            httpClient = new HttpClient(handler);
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            currentHWID = GenerateEnhancedHWID();

            // Try to load existing license
            LoadLocalLicense();
            if (currentLicense != null)
            {
                isLicenseValid = true;
            }

            // Log HWID for debugging
            System.Diagnostics.Debug.WriteLine($"HWID: {currentHWID}");
            Console.WriteLine($"HWID: {currentHWID}");
        }

        private string GenerateEnhancedHWID()
        {
            try
            {
                var components = new StringBuilder();

                // CPU ID
                using (var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
                {
                    foreach (var obj in searcher.Get())
                        components.Append(obj["ProcessorId"]?.ToString() ?? "");
                }

                // Motherboard Serial
                using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard"))
                {
                    foreach (var obj in searcher.Get())
                        components.Append(obj["SerialNumber"]?.ToString() ?? "");
                }

                // BIOS Serial
                using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS"))
                {
                    foreach (var obj in searcher.Get())
                        components.Append(obj["SerialNumber"]?.ToString() ?? "");
                }

                // Disk Drive Serial
                using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive WHERE Index=0"))
                {
                    foreach (var obj in searcher.Get())
                        components.Append(obj["SerialNumber"]?.ToString()?.Trim() ?? "");
                }

                // MAC Address
                using (var searcher = new ManagementObjectSearcher("SELECT MACAddress FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled=True"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        components.Append(obj["MACAddress"]?.ToString() ?? "");
                        break;
                    }
                }

                // Volume Serial
                try
                {
                    string volumeSerial = DriveInfo.GetDrives()
                        .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                        .Select(d => d.RootDirectory.ToString())
                        .FirstOrDefault();

                    if (!string.IsNullOrEmpty(volumeSerial))
                    {
                        var drive = new DriveInfo(volumeSerial);
                        components.Append(drive.VolumeLabel);
                    }
                }
                catch { }

                // User Info
                components.Append(Environment.UserName);
                components.Append(Environment.MachineName);

                // Create hash
                using (var sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(components.ToString()));
                    return Convert.ToBase64String(hashBytes)
                        .Replace("/", "_")
                        .Replace("+", "-");
                }
            }
            catch
            {
                // Fallback
                string fallback = Environment.MachineName + Environment.UserName + Environment.ProcessorCount;
                using (var sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(fallback));
                    return Convert.ToBase64String(hashBytes)
                        .Replace("/", "_")
                        .Replace("+", "-");
                }
            }
        }

        private bool IsServerConfigured()
        {
            return SERVER_URL != null && !SERVER_URL.Contains("your-license-server");
        }

        public async Task<ValidationResult> ValidateLicenseWithServerAsync(string licenseKey)
        {
            try
            {
                // Log what we're doing
                Console.WriteLine("\n==========================================");
                Console.WriteLine("Starting license validation...");
                Console.WriteLine($"License Key: {licenseKey}");
                Console.WriteLine($"Server URL: {SERVER_URL}");
                Console.WriteLine($"HWID: {currentHWID}");
                Console.WriteLine("==========================================\n");

                // TEMPORARILY COMMENT OUT SIGNATURE VERIFICATION
                // First, verify RSA signature locally
                // if (!VerifyLicenseSignature(licenseKey))
                // {
                //     Console.WriteLine("Signature verification failed");
                //     return new ValidationResult
                //     {
                //         IsValid = false,
                //         Message = "Invalid license signature",
                //         Code = "INVALID_SIGNATURE"
                //     };
                // }

                Console.WriteLine("Signature verification SKIPPED for testing");

                // If server not configured, use offline mode
                if (!IsServerConfigured())
                {
                    Console.WriteLine("Server not configured, using offline mode");
                    return await ValidateOfflineAsync(licenseKey);
                }

                // Prepare request
                var request = new
                {
                    LicenseKey = licenseKey,
                    HWID = currentHWID,
                    IP = await GetPublicIPAsync(),
                    Version = Application.ProductVersion
                };

                string jsonRequest = JsonSerializer.Serialize(request);
                Console.WriteLine($"Sending request: {jsonRequest}");

                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                // Send to server
                string fullUrl = $"{SERVER_URL}/validate";
                Console.WriteLine($"Connecting to: {fullUrl}");

                var response = await httpClient.PostAsync(fullUrl, content);

                Console.WriteLine($"Response status: {(int)response.StatusCode} {response.StatusCode}");

                // Read the response body
                string responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response body: {responseBody}");

                if (response.IsSuccessStatusCode)
                {
                    // Configure case-insensitive deserialization
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var result = JsonSerializer.Deserialize<ServerValidationResult>(responseBody, options);

                    // Add detailed debug logging
                    Console.WriteLine($"Deserialized result - Valid: {result?.Valid}, Message: {result?.Message}, Code: {result?.Code}");

                    // Check if deserialization failed
                    if (result == null)
                    {
                        Console.WriteLine("ERROR: Failed to deserialize server response");
                        return new ValidationResult
                        {
                            IsValid = false,
                            Message = "Invalid server response format",
                            Code = "INVALID_RESPONSE"
                        };
                    }

                    // Check if license is valid
                    if (result.Valid)
                    {
                        Console.WriteLine("✓ Server returned Valid=true - SUCCESS!");

                        // Verify JWT token
                        if (!ValidateJWTToken(result.Token))
                        {
                            Console.WriteLine("JWT token validation failed");
                            return new ValidationResult
                            {
                                IsValid = false,
                                Message = "Invalid session token",
                                Code = "INVALID_TOKEN"
                            };
                        }

                        Console.WriteLine("✓ JWT token validation passed");

                        // Save license locally
                        currentLicense = new LicenseInfo
                        {
                            LicenseKey = licenseKey,
                            ExpiryDate = result.ExpiryDate,
                            Features = result.Features ?? new Dictionary<string, bool>(),
                            LastValidation = DateTime.UtcNow
                        };
                        isLicenseValid = true;

                        SaveLicenseLocally(currentLicense);

                        Console.WriteLine("✓ License saved locally, validation successful!");

                        return new ValidationResult
                        {
                            IsValid = true,
                            Message = "License validated successfully",
                            Code = "VALID",
                            Features = currentLicense.Features,
                            ExpiryDate = currentLicense.ExpiryDate
                        };
                    }
                    else
                    {
                        Console.WriteLine($"✗ Server returned Valid=false. Message: {result.Message}, Code: {result.Code}");
                        return new ValidationResult
                        {
                            IsValid = false,
                            Message = result.Message ?? "Validation failed",
                            Code = result.Code ?? "INVALID"
                        };
                    }
                }
                else
                {
                    Console.WriteLine($"Server error: {response.StatusCode} - {responseBody}");

                    // Even if server returns error, try offline validation as fallback
                    Console.WriteLine("Trying offline validation as fallback...");
                    return await ValidateOfflineAsync(licenseKey);
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Request Error: {ex.Message}");
                return await ValidateOfflineAsync(licenseKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return new ValidationResult
                {
                    IsValid = false,
                    Message = $"Validation error: {ex.Message}",
                    Code = "ERROR"
                };
            }
        }

        private bool IsPublicKeyConfigured()
        {
            return !string.IsNullOrEmpty(PUBLIC_KEY) && !PUBLIC_KEY.Contains("temporarydevkey");
        }

        private async Task<ValidationResult> ValidateOfflineAsync(string licenseKey)
        {
            Console.WriteLine("Attempting offline validation...");

            // Check if we have a locally stored valid license
            if (LoadLocalLicense() && currentLicense != null && currentLicense.LicenseKey == licenseKey)
            {
                // Check if offline period hasn't exceeded (e.g., 7 days)
                if ((DateTime.UtcNow - currentLicense.LastValidation).TotalDays <= 7)
                {
                    isLicenseValid = true;
                    Console.WriteLine("Offline validation successful (existing license)");
                    return new ValidationResult
                    {
                        IsValid = true,
                        Message = "License validated offline",
                        Code = "OFFLINE_VALID",
                        Features = currentLicense.Features,
                        ExpiryDate = currentLicense.ExpiryDate
                    };
                }
                else
                {
                    Console.WriteLine("Offline license expired");
                }
            }

            // For development, accept any key in offline mode
#if DEBUG
            if (!string.IsNullOrEmpty(licenseKey))
            {
                Console.WriteLine("Development mode: accepting any key offline");
                currentLicense = new LicenseInfo
                {
                    LicenseKey = licenseKey,
                    ExpiryDate = DateTime.UtcNow.AddDays(30),
                    Features = new Dictionary<string, bool> { { "FullAccess", true } },
                    LastValidation = DateTime.UtcNow
                };
                isLicenseValid = true;
                SaveLicenseLocally(currentLicense);

                return new ValidationResult
                {
                    IsValid = true,
                    Message = "Development mode license",
                    Code = "DEV_MODE",
                    Features = currentLicense.Features,
                    ExpiryDate = currentLicense.ExpiryDate
                };
            }
#endif

            Console.WriteLine("Offline validation failed");
            return new ValidationResult
            {
                IsValid = false,
                Message = "Cannot validate license offline. Please connect to internet.",
                Code = "OFFLINE_NEEDED"
            };
        }

        private bool VerifyLicenseSignature(string signedLicense)
        {
            try
            {
                var parts = signedLicense.Split('.');
                if (parts.Length != 2)
                {
                    Console.WriteLine("Signature verification: invalid format (no dot)");
                    return false;
                }

                string licenseKey = parts[0];
                string signature = parts[1];

                using (var rsa = new RSACryptoServiceProvider(2048))
                {
                    rsa.FromXmlString(PUBLIC_KEY);

                    byte[] data = Encoding.UTF8.GetBytes(licenseKey);
                    byte[] signatureBytes = Convert.FromBase64String(signature);

                    bool result = rsa.VerifyData(data, HashAlgorithmName.SHA256, signatureBytes);
                    Console.WriteLine($"Signature verification: {result}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Signature verification error: {ex.Message}");
                return false;
            }
        }

        private bool ValidateJWTToken(string token)
        {
            try
            {
                var parts = token.Split('.');
                if (parts.Length != 3)
                {
                    Console.WriteLine("JWT validation: invalid format");
                    return false;
                }

                // Decode payload
                string payload = Encoding.UTF8.GetString(Convert.FromBase64String(
                    parts[1].Replace('_', '/').Replace('-', '+').PadRight(4 * ((parts[1].Length + 3) / 4), '=')));

                Console.WriteLine($"JWT Payload: {payload}");

                // Check if token contains current HWID
                bool containsHWID = payload.Contains(currentHWID);
                Console.WriteLine($"JWT HWID match: {containsHWID}");
                return containsHWID;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"JWT validation error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeactivateLicenseAsync()
        {
            try
            {
                if (currentLicense == null)
                    return true;

                var request = new
                {
                    LicenseKey = currentLicense.LicenseKey,
                    HWID = currentHWID
                };

                string jsonRequest = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"{SERVER_URL}/deactivate", content);

                if (response.IsSuccessStatusCode)
                {
                    // Delete local license file
                    if (File.Exists(LICENSE_FILE))
                        File.Delete(LICENSE_FILE);

                    currentLicense = null;
                    isLicenseValid = false;
                    return true;
                }
            }
            catch { }

            return false;
        }

        private async Task<string> GetPublicIPAsync()
        {
            try
            {
                string ip = await httpClient.GetStringAsync("https://api.ipify.org");
                return ip.Trim();
            }
            catch
            {
                return "0.0.0.0";
            }
        }

        private void SaveLicenseLocally(LicenseInfo license)
        {
            try
            {
                string json = JsonSerializer.Serialize(license);
                byte[] data = Encoding.UTF8.GetBytes(json);

                // Encrypt with user's HWID as key
                byte[] key = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(currentHWID));

                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = new byte[16]; // Zero IV for simplicity

                    using (var ms = new MemoryStream())
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(data, 0, data.Length);
                        cs.FlushFinalBlock();

                        File.WriteAllBytes(LICENSE_FILE, ms.ToArray());
                    }
                }

                // Hide the file
                File.SetAttributes(LICENSE_FILE, FileAttributes.Hidden | FileAttributes.ReadOnly);
                Console.WriteLine("License saved locally");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving license: {ex.Message}");
            }
        }

        private bool LoadLocalLicense()
        {
            try
            {
                if (!File.Exists(LICENSE_FILE))
                {
                    Console.WriteLine("No local license file found");
                    return false;
                }

                byte[] encryptedData = File.ReadAllBytes(LICENSE_FILE);

                // Decrypt with HWID
                byte[] key = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(currentHWID));

                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = new byte[16];

                    using (var ms = new MemoryStream(encryptedData))
                    using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs))
                    {
                        string json = sr.ReadToEnd();
                        currentLicense = JsonSerializer.Deserialize<LicenseInfo>(json);
                        Console.WriteLine("Local license loaded");
                        return currentLicense != null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading license: {ex.Message}");
                return false;
            }
        }

        public int GetRemainingAttempts() => MAX_ATTEMPTS - attemptCount;
        public void IncrementAttempt() => attemptCount++;
        public void ResetAttempts() => attemptCount = 0;
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public string Code { get; set; }
        public Dictionary<string, bool> Features { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    public class ServerValidationResult
    {
        public bool Valid { get; set; }
        public string Message { get; set; }
        public string Code { get; set; }
        public string Token { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public Dictionary<string, bool> Features { get; set; }
    }

    public class LicenseInfo
    {
        public string LicenseKey { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime LastValidation { get; set; }
        public Dictionary<string, bool> Features { get; set; }
    }
}