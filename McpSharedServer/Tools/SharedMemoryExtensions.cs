using System.IO.MemoryMappedFiles;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

/// <summary>
/// Extension methods and utilities for typed shared memory operations
/// </summary>
public static class SharedMemoryExtensions
{
    private static readonly JsonSerializerOptions _defaultOptions = new JsonSerializerOptions
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Simplified shared memory manager for extensions
    /// </summary>
    internal class SharedMemoryManager : IDisposable
    {
        private const string MemoryName = "Global\\MCP_SharedMemory";
        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _accessor;
        private bool _isReadOnly;
        private bool _disposed;

        public SharedMemoryManager(bool readOnly = true)
        {
            _isReadOnly = readOnly;
            InitializeMemoryMappedFile();
        }

        private void InitializeMemoryMappedFile()
        {
            try
            {
                if (_isReadOnly)
                {
                    _mmf = MemoryMappedFile.OpenExisting(MemoryName);
                }
                else
                {
                    _mmf = MemoryMappedFile.CreateOrOpen(MemoryName, 65536);
                }
                _accessor = _mmf.CreateViewAccessor();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize shared memory: {ex.Message}", ex);
            }
        }

        public bool WriteData<T>(T data, out string error)
        {
            error = null;
            if (_isReadOnly)
            {
                error = "Memory manager is in read-only mode";
                return false;
            }

            try
            {
                string json = JsonSerializer.Serialize(data);
                byte[] buffer = Encoding.UTF8.GetBytes(json);

                if (buffer.Length > 1024 * 1024 - sizeof(int)) // 1MB limit
                {
                    error = $"Data size ({buffer.Length}) exceeds maximum allowed size";
                    return false;
                }

                _accessor.Write(0, buffer.Length);
                _accessor.WriteArray(sizeof(int), buffer, 0, buffer.Length);

                return true;
            }
            catch (Exception ex)
            {
                error = $"Failed to write data: {ex.Message}";
                return false;
            }
        }

        public bool ReadData(out JsonElement result, out string error)
        {
            result = default;
            error = null;

            try
            {
                int size = _accessor.ReadInt32(0);
                if (size <= 0 || size > 1024 * 1024)
                {
                    error = $"Invalid data size: {size}";
                    return false;
                }

                byte[] buffer = new byte[size];
                _accessor.ReadArray(sizeof(int), buffer, 0, size);

                string json = Encoding.UTF8.GetString(buffer);
                result = JsonDocument.Parse(json).RootElement.Clone();

                return true;
            }
            catch (Exception ex)
            {
                error = $"Failed to read data: {ex.Message}";
                return false;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _accessor?.Dispose();
                _mmf?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Generic data container for shared memory
    /// </summary>
    public class SharedMemoryData<T>
    {
        public string Type { get; set; }
        public DateTime Timestamp { get; set; }
        public T Payload { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }

        public SharedMemoryData(T payload, Dictionary<string, object>? metadata = null)
        {
            Type = typeof(T).Name;
            Timestamp = DateTime.UtcNow;
            Payload = payload;
            Metadata = metadata;
        }
    }

    /// <summary>
    /// Result wrapper for shared memory operations
    /// </summary>
    public class SharedMemoryResult<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Error { get; set; }
        public DateTime Timestamp { get; set; }

        public SharedMemoryResult(bool success, T? data = default, string? error = null)
        {
            Success = success;
            Data = data;
            Error = error;
            Timestamp = DateTime.UtcNow;
        }

        public static SharedMemoryResult<T> Ok(T data) => new SharedMemoryResult<T>(true, data);
        public static SharedMemoryResult<T> Fail(string error) => new SharedMemoryResult<T>(false, default, error);
    }

    /// <summary>
    /// Configuration for shared memory operations
    /// </summary>
    public class SharedMemoryConfig
    {
        public int MaxRetries { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);
        public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public bool EnableCompression { get; set; } = false;
        public JsonSerializerOptions SerializerOptions { get; set; } = _defaultOptions;
    }

    /// <summary>
    /// Typed wrapper for shared memory operations with retry logic
    /// </summary>
    public static class SharedMemoryTyped<T> where T : class
    {
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private static SharedMemoryConfig _config = new SharedMemoryConfig();

        /// <summary>
        /// Configure the typed operations
        /// </summary>
        public static void Configure(SharedMemoryConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Write typed data with retry logic
        /// </summary>
        public static async Task<SharedMemoryResult<bool>> WriteAsync(T data, Dictionary<string, object>? metadata = null)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                using var manager = new SharedMemoryManager(readOnly: false);

                var wrappedData = new SharedMemoryData<T>(data, metadata);
                string json = JsonSerializer.Serialize(wrappedData, _config.SerializerOptions);

                // Check size limits
                int size = Encoding.UTF8.GetByteCount(json);
                if (size > 1024 * 1024 - sizeof(int)) // 1MB limit
                {
                    return SharedMemoryResult<bool>.Fail($"Data size ({size} bytes) exceeds limit");
                }

                if (manager.WriteData(wrappedData, out string error))
                {
                    return SharedMemoryResult<bool>.Ok(true);
                }
                else
                {
                    return SharedMemoryResult<bool>.Fail(error);
                }
            });
        }

        /// <summary>
        /// Read typed data with retry logic
        /// </summary>
        public static async Task<SharedMemoryResult<SharedMemoryData<T>>> ReadAsync()
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                using var manager = new SharedMemoryManager(readOnly: true);

                if (manager.ReadData(out JsonElement rawData, out string error))
                {
                    try
                    {
                        var data = JsonSerializer.Deserialize<SharedMemoryData<T>>(rawData.GetRawText(), _config.SerializerOptions);
                        if (data != null)
                        {
                            return SharedMemoryResult<SharedMemoryData<T>>.Ok(data);
                        }
                        else
                        {
                            return SharedMemoryResult<SharedMemoryData<T>>.Fail("Failed to deserialize data");
                        }
                    }
                    catch (Exception ex)
                    {
                        return SharedMemoryResult<SharedMemoryData<T>>.Fail($"Deserialization error: {ex.Message}");
                    }
                }
                else
                {
                    return SharedMemoryResult<SharedMemoryData<T>>.Fail(error);
                }
            });
        }

        /// <summary>
        /// Update typed data atomically
        /// </summary>
        public static async Task<SharedMemoryResult<bool>> UpdateAsync(Func<T, T> updateFunc)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                // Read current data
                var readResult = await ReadAsync();
                if (!readResult.Success)
                {
                    return SharedMemoryResult<bool>.Fail(readResult.Error ?? "Failed to read current data");
                }

                // Apply update
                var updatedData = updateFunc(readResult.Data!.Payload);

                // Write back
                var writeResult = await WriteAsync(updatedData, readResult.Data.Metadata);
                if (writeResult.Success)
                {
                    return SharedMemoryResult<bool>.Ok(true);
                }
                else
                {
                    return SharedMemoryResult<bool>.Fail(writeResult.Error ?? "Failed to write updated data");
                }
            });
        }

        /// <summary>
        /// Execute operation with retry logic and semaphore for thread safety
        /// </summary>
        private static async Task<TResult> ExecuteWithRetryAsync<TResult>(Func<Task<TResult>> operation)
        {
            await _semaphore.WaitAsync(_config.OperationTimeout);

            try
            {
                for (int attempt = 1; attempt <= _config.MaxRetries; attempt++)
                {
                    try
                    {
                        var result = await operation();
                        return result;
                    }
                    catch (Exception ex) when (attempt < _config.MaxRetries)
                    {
                        Console.Error.WriteLine($"Attempt {attempt} failed: {ex.Message}. Retrying...");
                        await Task.Delay(_config.RetryDelay);
                    }
                }

                // All retries failed - create a failure result
                // This assumes TResult has a constructor like SharedMemoryResult<T>.Fail(string)
                throw new InvalidOperationException("Operation failed after all retries");
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    /// <summary>
    /// Common data types for shared memory operations
    /// </summary>
    public static class SharedMemoryDataTypes
    {
        public record SystemStatus(string Status, int CpuUsage, long MemoryUsage, DateTime LastUpdate);

        public record Message(string Id, string Content, string Sender, DateTime SentAt, bool IsRead = false);

        public record Configuration(string Key, object Value, string Description, DateTime ModifiedAt);

        public record Metrics(string Name, double Value, string Unit, DateTime MeasuredAt);

        public record Command(string Action, Dictionary<string, object> Parameters, string Initiator);
    }

    /// <summary>
    /// Factory methods for common shared memory data types
    /// </summary>
    public static class SharedMemoryFactory
    {
        public static SharedMemoryData<SharedMemoryDataTypes.SystemStatus> CreateSystemStatus(
            string status, int cpuUsage, long memoryUsage)
        {
            return new SharedMemoryData<SharedMemoryDataTypes.SystemStatus>(
                new SharedMemoryDataTypes.SystemStatus(status, cpuUsage, memoryUsage, DateTime.UtcNow)
            );
        }

        public static SharedMemoryData<SharedMemoryDataTypes.Message> CreateMessage(
            string content, string sender)
        {
            return new SharedMemoryData<SharedMemoryDataTypes.Message>(
                new SharedMemoryDataTypes.Message(
                    Guid.NewGuid().ToString(),
                    content,
                    sender,
                    DateTime.UtcNow
                )
            );
        }

        public static SharedMemoryData<SharedMemoryDataTypes.Configuration> CreateConfiguration(
            string key, object value, string description)
        {
            return new SharedMemoryData<SharedMemoryDataTypes.Configuration>(
                new SharedMemoryDataTypes.Configuration(key, value, description, DateTime.UtcNow)
            );
        }

        public static SharedMemoryData<SharedMemoryDataTypes.Metrics> CreateMetrics(
            string name, double value, string unit)
        {
            return new SharedMemoryData<SharedMemoryDataTypes.Metrics>(
                new SharedMemoryDataTypes.Metrics(name, value, unit, DateTime.UtcNow)
            );
        }
    }
}
