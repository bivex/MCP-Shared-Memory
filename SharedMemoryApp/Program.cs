using System;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Text.Json;
using System.Threading;

public class Program
{
    private const string MemoryName = "Global\\MCP_SharedMemory";
    private const int DefaultMemorySize = 65536;
    private const int MaxMemorySize = 1024 * 1024; // 1MB limit for safety
    private static readonly object _syncLock = new object();

    /// <summary>
    /// Enhanced shared memory manager for the client application
    /// </summary>
    private class SharedMemoryManager : IDisposable
    {
        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _accessor;
        private bool _disposed;

        public SharedMemoryManager()
        {
            InitializeMemoryMappedFile();
        }

        private void InitializeMemoryMappedFile()
        {
            try
            {
                _mmf = MemoryMappedFile.CreateOrOpen(MemoryName, DefaultMemorySize);
                _accessor = _mmf.CreateViewAccessor();
            }
            catch (UnauthorizedAccessException)
            {
                throw new InvalidOperationException("Access denied. Try running as Administrator for Global\\ shared memory access.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize shared memory: {ex.Message}", ex);
            }
        }

        public bool WriteData<T>(T data, out string error)
        {
            error = null;
            try
            {
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                byte[] buffer = Encoding.UTF8.GetBytes(json);

                if (buffer.Length > MaxMemorySize - sizeof(int))
                {
                    error = $"Data size ({buffer.Length} bytes) exceeds maximum allowed size ({MaxMemorySize - sizeof(int)} bytes)";
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
            try
            {
                _accessor.Write(0, 0);
                return true;
            }
            catch (Exception ex)
            {
                error = $"Failed to clear memory: {ex.Message}";
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

    public static void Main()
    {
        Console.WriteLine("=== MCP Shared Memory Client Application ===");
        Console.WriteLine($"Memory Name: {MemoryName}");
        Console.WriteLine($"Max Memory Size: {MaxMemorySize} bytes");
        Console.WriteLine();

        while (true)
        {
            Console.WriteLine("Choose operation:");
            Console.WriteLine("1. Write sample data");
            Console.WriteLine("2. Write custom JSON");
            Console.WriteLine("3. Read current data");
            Console.WriteLine("4. Clear memory");
            Console.WriteLine("5. Show memory info");
            Console.WriteLine("6. Continuous write mode (demo)");
            Console.WriteLine("0. Exit");
            Console.Write("Your choice: ");

            string choice = Console.ReadLine()?.Trim();

            switch (choice)
            {
                case "1":
                    WriteSampleData();
                    break;
                case "2":
                    WriteCustomJson();
                    break;
                case "3":
                    ReadCurrentData();
                    break;
                case "4":
                    ClearMemory();
                    break;
                case "5":
                    ShowMemoryInfo();
                    break;
                case "6":
                    ContinuousWriteDemo();
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
            Console.Clear();
        }
    }

    private static void WriteSampleData()
    {
        lock (_syncLock)
        {
            try
            {
                using var manager = new SharedMemoryManager();

                var payload = new
                {
                    timestamp = DateTime.Now,
                    status = "active",
                    value = new Random().Next(1, 1000),
                    message = $"Sample data from client app - {DateTime.Now:HH:mm:ss}",
                    metadata = new
                    {
                        version = "2.0",
                        client = "SharedMemoryApp"
                    }
                };

                if (manager.WriteData(payload, out string error))
                {
                    Console.WriteLine("✅ Successfully wrote sample data to shared memory:");
                    Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    Console.WriteLine($"❌ Failed to write data: {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }
    }

    private static void WriteCustomJson()
    {
        Console.Write("Enter JSON data: ");
        string jsonInput = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(jsonInput))
        {
            Console.WriteLine("❌ Empty input. Operation cancelled.");
            return;
        }

        lock (_syncLock)
        {
            try
            {
                using var manager = new SharedMemoryManager();

                // Validate JSON
                JsonDocument.Parse(jsonInput);

                if (manager.WriteData(JsonDocument.Parse(jsonInput).RootElement, out string error))
                {
                    Console.WriteLine("✅ Successfully wrote custom JSON to shared memory");
                }
                else
                {
                    Console.WriteLine($"❌ Failed to write data: {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Invalid JSON or error: {ex.Message}");
            }
        }
    }

    private static void ReadCurrentData()
    {
        lock (_syncLock)
        {
            try
            {
                using var manager = new SharedMemoryManager();

                if (manager.ReadData(out JsonElement result, out string error))
                {
                    Console.WriteLine("✅ Current data in shared memory:");
                    Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    Console.WriteLine($"❌ Failed to read data: {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }
    }

    private static void ClearMemory()
    {
        lock (_syncLock)
        {
            try
            {
                using var manager = new SharedMemoryManager();

                if (manager.ClearMemory(out string error))
                {
                    Console.WriteLine("✅ Successfully cleared shared memory");
                }
                else
                {
                    Console.WriteLine($"❌ Failed to clear memory: {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }
    }

    private static void ShowMemoryInfo()
    {
        Console.WriteLine("📊 Shared Memory Information:");
        Console.WriteLine($"- Memory Name: {MemoryName}");
        Console.WriteLine($"- Default Size: {DefaultMemorySize} bytes");
        Console.WriteLine($"- Maximum Size: {MaxMemorySize} bytes");
        Console.WriteLine("- Features: Thread-safe operations, automatic resource management");
        Console.WriteLine("- Access: Global (requires Administrator privileges on Windows)");
    }

    private static void ContinuousWriteDemo()
    {
        Console.WriteLine("🔄 Starting continuous write demo (press Ctrl+C to stop)...");
        Console.WriteLine("This will write timestamped data every 2 seconds.");
        Console.WriteLine();

        var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            Console.WriteLine("\n🛑 Stopping continuous write demo...");
        };

        int counter = 0;
        while (!cts.IsCancellationRequested)
        {
            try
            {
                lock (_syncLock)
                {
                    using var manager = new SharedMemoryManager();

                    var payload = new
                    {
                        counter = ++counter,
                        timestamp = DateTime.Now,
                        demo_mode = true,
                        uptime_seconds = counter * 2,
                        random_value = new Random().Next(100, 999)
                    };

                    if (manager.WriteData(payload, out string error))
                    {
                        Console.WriteLine($"✅ [{DateTime.Now:HH:mm:ss}] Wrote data #{counter}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ [{DateTime.Now:HH:mm:ss}] Failed to write data: {error}");
                    }
                }

                Thread.Sleep(2000); // Wait 2 seconds
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in continuous mode: {ex.Message}");
                break;
            }
        }

        Console.WriteLine("🏁 Continuous write demo finished.");
    }
}
