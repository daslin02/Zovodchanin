using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Zovodchanin
{
    /// <summary>
    /// Класс для хранения кэшированных данных пользователя
    /// </summary>
    public class CashFile
    {
        public string ID { get; set; } = "";
        public string Password { get; set; } = "";
        public bool DarkMode { get; set; } = false;

        public CashFile()
        {
            ID = "";
            Password = "";
            DarkMode = false;
        }
    }

    /// <summary>
    /// Менеджер для работы с файлом кэша
    /// </summary>
    internal class FileManager
    {
        private readonly string _cacheFilePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public FileManager()
        {

            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Zovodchanin"
            );
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            _cacheFilePath = Path.Combine(appDataPath, "cache.json");

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
        }

        /// <summary>
        /// Updates the data in the cache file
        /// </summary>
        /// <param name="ID">ID User</param>
        /// <param name="Password">Password</param>
        /// <param name="DarkMode">Theme Mode</param>
        public void UpdateData(string ID, string Password, bool DarkMode)
        {
            try
            {
                var cashData = new CashFile
                {
                    ID = ID,
                    Password = Password,
                    DarkMode = DarkMode
                };

                string json = JsonSerializer.Serialize(cashData, _jsonOptions);
                File.WriteAllText(_cacheFilePath, json);

                Console.WriteLine($"[FileManager] Data updated for user: {ID}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileManager] Error updating data: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads data from a cache file
        /// </summary>
        /// <returns>A CashFile object with data, or an empty object if the file is not found</returns>
        public CashFile LoadData()
        {
            try
            {
                if (!File.Exists(_cacheFilePath))
                {
                    Console.WriteLine("[FileManager] Cache file not found, returning empty data");
                    return new CashFile();
                }

                string json = File.ReadAllText(_cacheFilePath);
                var cashData = JsonSerializer.Deserialize<CashFile>(json);

                if (cashData != null)
                {
                    Console.WriteLine($"[FileManager] Data loaded for user: {cashData.ID}");
                    return cashData;
                }

                return new CashFile();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileManager] Error loading data: {ex.Message}");
                return new CashFile();
            }
        }

        /// <summary>
        /// Checks if the cache file exists
        /// </summary>
        public bool CacheExists()
        {
            return File.Exists(_cacheFilePath);
        }

        /// <summary>
        /// Deletes the cache file (log out)
        /// </summary>
        public void ClearCache()
        {
            try
            {
                if (File.Exists(_cacheFilePath))
                {
                    File.Delete(_cacheFilePath);
                    Console.WriteLine("[FileManager] Cache file deleted");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileManager] Error clearing cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the path to the cache file
        /// </summary>
        public string GetCachePath()
        {
            return _cacheFilePath;
        }
    }
}