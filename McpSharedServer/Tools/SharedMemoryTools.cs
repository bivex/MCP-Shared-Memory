using ModelContextProtocol.Server;
using System.ComponentModel;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Text.Json;
using System.Threading;

[McpServerToolType]
public static class SharedMemoryTools
{
    private const string MemoryName = "Global\\MCP_SharedMemory";
    private const int DefaultMemorySize = 65536;
    internal const int MaxMemorySize = 1024 * 1024; // 1MB limit for safety
    private static readonly object _syncLock = new object();

    /// <summary>
    /// Enhanced shared memory manager with better resource management
    /// </summary>
    internal class SharedMemoryManager : IDisposable
    {
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
                    _mmf = MemoryMappedFile.CreateOrOpen(MemoryName, DefaultMemorySize);
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

                if (buffer.Length > MaxMemorySize - sizeof(int))
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
                if (size <= 0 || size > MaxMemorySize)
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

        public bool ClearMemory(out string error)
        {
            error = null;
            if (_isReadOnly)
            {
                error = "Memory manager is in read-only mode";
                return false;
            }

            try
            {
                // Write zero size to indicate empty memory
                _accessor.Write(0, 0);
                return true;
            }
            catch (Exception ex)
            {
                error = $"Failed to clear memory: {ex.Message}";
                return false;
            }
        }

        public long GetMemorySize()
        {
            try
            {
                return _accessor.Capacity;
            }
            catch
            {
                return 0;
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

    [McpServerTool(Name = "get_shared_memory")]
    [Description("Reads JSON data from shared memory and returns it to the MCP client.")]
    public static JsonElement ReadSharedMemory()
    {
        lock (_syncLock)
        {
            try
            {
                using var manager = new SharedMemoryManager(readOnly: true);

                if (manager.ReadData(out JsonElement result, out string error))
                {
                    Console.Error.WriteLine("MCP Server → successfully read data from shared memory");
                    return result;
                }
                else
                {
                    return JsonDocument.Parse($"{{\"error\": \"{error}\"}}").RootElement.Clone();
                }
            }
            catch (FileNotFoundException)
            {
                return JsonDocument.Parse("{\"error\": \"Shared memory not found. Run SharedMemoryApp first.\"}").RootElement.Clone();
            }
            catch (Exception ex)
            {
                return JsonDocument.Parse($"{{\"error\": \"{ex.Message}\"}}").RootElement.Clone();
            }
        }
    }

    [McpServerTool(Name = "write_shared_memory")]
    [Description("Writes JSON data to shared memory. Parameters: json_data (string) - JSON string to write")]
    public static JsonElement WriteSharedMemory(string json_data)
    {
        lock (_syncLock)
        {
            try
            {
                using var manager = new SharedMemoryManager(readOnly: false);

                // Try to parse the JSON first to validate it
                JsonElement data = JsonDocument.Parse(json_data).RootElement;

                if (manager.WriteData(data, out string error))
                {
                    Console.Error.WriteLine("MCP Server → successfully wrote data to shared memory");
                    return JsonDocument.Parse("{\"success\": true, \"message\": \"Data written to shared memory\"}").RootElement.Clone();
                }
                else
                {
                    return JsonDocument.Parse($"{{\"success\": false, \"error\": \"{error}\"}}").RootElement.Clone();
                }
            }
            catch (Exception ex)
            {
                return JsonDocument.Parse($"{{\"success\": false, \"error\": \"{ex.Message}\"}}").RootElement.Clone();
            }
        }
    }

    [McpServerTool(Name = "update_shared_memory")]
    [Description("Updates existing data in shared memory with new JSON data. Parameters: json_data (string) - JSON string to update")]
    public static JsonElement UpdateSharedMemory(string json_data)
    {
        // For update, we can reuse the write logic since we're replacing the entire content
        return WriteSharedMemory(json_data);
    }

    [McpServerTool(Name = "clear_shared_memory")]
    [Description("Clears all data from shared memory.")]
    public static JsonElement ClearSharedMemory()
    {
        lock (_syncLock)
        {
            try
            {
                using var manager = new SharedMemoryManager(readOnly: false);

                if (manager.ClearMemory(out string error))
                {
                    Console.Error.WriteLine("MCP Server → successfully cleared shared memory");
                    return JsonDocument.Parse("{\"success\": true, \"message\": \"Shared memory cleared\"}").RootElement.Clone();
                }
                else
                {
                    return JsonDocument.Parse($"{{\"success\": false, \"error\": \"{error}\"}}").RootElement.Clone();
                }
            }
            catch (Exception ex)
            {
                return JsonDocument.Parse($"{{\"success\": false, \"error\": \"{ex.Message}\"}}").RootElement.Clone();
            }
        }
    }

    [McpServerTool(Name = "get_shared_memory_info")]
    [Description("Returns information about the shared memory state.")]
    public static JsonElement GetSharedMemoryInfo()
    {
        lock (_syncLock)
        {
            try
            {
                using var manager = new SharedMemoryManager(readOnly: true);

                var info = new
                {
                    memory_name = MemoryName,
                    max_size = MaxMemorySize,
                    current_capacity = manager.GetMemorySize(),
                    has_data = manager.ReadData(out _, out _) // Quick check if data exists
                };

                return JsonDocument.Parse(JsonSerializer.Serialize(info)).RootElement.Clone();
            }
            catch (Exception ex)
            {
                return JsonDocument.Parse($"{{\"error\": \"{ex.Message}\"}}").RootElement.Clone();
            }
        }
    }

    [McpServerTool(Name = "write_typed_data")]
    [Description("Writes typed data to shared memory. Parameters: data_type (string), json_payload (string)")]
    public static async Task<JsonElement> WriteTypedDataAsync(string data_type, string json_payload)
    {
        try
        {
            var result = data_type.ToLower() switch
            {
                "systemstatus" => await SharedMemoryExtensions.SharedMemoryTyped<SharedMemoryExtensions.SharedMemoryDataTypes.SystemStatus>
                    .WriteAsync(System.Text.Json.JsonSerializer.Deserialize<SharedMemoryExtensions.SharedMemoryDataTypes.SystemStatus>(json_payload)),

                "message" => await SharedMemoryExtensions.SharedMemoryTyped<SharedMemoryExtensions.SharedMemoryDataTypes.Message>
                    .WriteAsync(System.Text.Json.JsonSerializer.Deserialize<SharedMemoryExtensions.SharedMemoryDataTypes.Message>(json_payload)),

                "configuration" => await SharedMemoryExtensions.SharedMemoryTyped<SharedMemoryExtensions.SharedMemoryDataTypes.Configuration>
                    .WriteAsync(System.Text.Json.JsonSerializer.Deserialize<SharedMemoryExtensions.SharedMemoryDataTypes.Configuration>(json_payload)),

                "metrics" => await SharedMemoryExtensions.SharedMemoryTyped<SharedMemoryExtensions.SharedMemoryDataTypes.Metrics>
                    .WriteAsync(System.Text.Json.JsonSerializer.Deserialize<SharedMemoryExtensions.SharedMemoryDataTypes.Metrics>(json_payload)),

                _ => SharedMemoryExtensions.SharedMemoryResult<bool>.Fail($"Unknown data type: {data_type}")
            };

            return JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(result)).RootElement.Clone();
        }
        catch (Exception ex)
        {
            return JsonDocument.Parse($"{{\"success\": false, \"error\": \"{ex.Message}\"}}").RootElement.Clone();
        }
    }

    [McpServerTool(Name = "read_typed_data")]
    [Description("Reads typed data from shared memory. Parameters: data_type (string)")]
    public static async Task<JsonElement> ReadTypedDataAsync(string data_type)
    {
        try
        {
            object result = data_type.ToLower() switch
            {
                "systemstatus" => await SharedMemoryExtensions.SharedMemoryTyped<SharedMemoryExtensions.SharedMemoryDataTypes.SystemStatus>.ReadAsync(),
                "message" => await SharedMemoryExtensions.SharedMemoryTyped<SharedMemoryExtensions.SharedMemoryDataTypes.Message>.ReadAsync(),
                "configuration" => await SharedMemoryExtensions.SharedMemoryTyped<SharedMemoryExtensions.SharedMemoryDataTypes.Configuration>.ReadAsync(),
                "metrics" => await SharedMemoryExtensions.SharedMemoryTyped<SharedMemoryExtensions.SharedMemoryDataTypes.Metrics>.ReadAsync(),
                _ => SharedMemoryExtensions.SharedMemoryResult<SharedMemoryExtensions.SharedMemoryData<object>>.Fail($"Unknown data type: {data_type}")
            };

            return JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(result)).RootElement.Clone();
        }
        catch (Exception ex)
        {
            return JsonDocument.Parse($"{{\"success\": false, \"error\": \"{ex.Message}\"}}").RootElement.Clone();
        }
    }

    [McpServerTool(Name = "create_system_status")]
    [Description("Creates and writes system status data. Parameters: status (string), cpu_usage (int), memory_usage (long)")]
    public static async Task<JsonElement> CreateSystemStatusAsync(string status, int cpu_usage, long memory_usage)
    {
        try
        {
            var data = SharedMemoryExtensions.SharedMemoryFactory.CreateSystemStatus(status, cpu_usage, memory_usage);
            var result = await SharedMemoryExtensions.SharedMemoryTyped<SharedMemoryExtensions.SharedMemoryDataTypes.SystemStatus>.WriteAsync(data.Payload, data.Metadata);

            return JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(result)).RootElement.Clone();
        }
        catch (Exception ex)
        {
            return JsonDocument.Parse($"{{\"success\": false, \"error\": \"{ex.Message}\"}}").RootElement.Clone();
        }
    }

    [McpServerTool(Name = "create_message")]
    [Description("Creates and writes a message. Parameters: content (string), sender (string)")]
    public static async Task<JsonElement> CreateMessageAsync(string content, string sender)
    {
        try
        {
            var data = SharedMemoryExtensions.SharedMemoryFactory.CreateMessage(content, sender);
            var result = await SharedMemoryExtensions.SharedMemoryTyped<SharedMemoryExtensions.SharedMemoryDataTypes.Message>.WriteAsync(data.Payload, data.Metadata);

            return JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(result)).RootElement.Clone();
        }
        catch (Exception ex)
        {
            return JsonDocument.Parse($"{{\"success\": false, \"error\": \"{ex.Message}\"}}").RootElement.Clone();
        }
    }

    [McpServerTool(Name = "create_metrics")]
    [Description("Creates and writes metrics data. Parameters: name (string), value (double), unit (string)")]
    public static async Task<JsonElement> CreateMetricsAsync(string name, double value, string unit)
    {
        try
        {
            var data = SharedMemoryExtensions.SharedMemoryFactory.CreateMetrics(name, value, unit);
            var result = await SharedMemoryExtensions.SharedMemoryTyped<SharedMemoryExtensions.SharedMemoryDataTypes.Metrics>.WriteAsync(data.Payload, data.Metadata);

            return JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(result)).RootElement.Clone();
        }
        catch (Exception ex)
        {
            return JsonDocument.Parse($"{{\"success\": false, \"error\": \"{ex.Message}\"}}").RootElement.Clone();
        }
    }

    [McpServerTool(Name = "list_supported_types")]
    [Description("Returns list of supported data types for typed operations.")]
    public static JsonElement ListSupportedTypes()
    {
        var types = new[]
        {
            new { name = "SystemStatus", description = "System status information (status, CPU, memory)" },
            new { name = "Message", description = "Messages with sender and content" },
            new { name = "Configuration", description = "Configuration key-value pairs" },
            new { name = "Metrics", description = "Performance metrics with units" }
        };

        return JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(new { supported_types = types })).RootElement.Clone();
    }
}
