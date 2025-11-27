using ModelContextProtocol.Server;
using System.ComponentModel;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Text.Json;

[McpServerToolType]
public static class SharedMemoryTools
{
    private const string MemoryName = "Global\\MCP_SharedMemory";
    // private const int MemorySize = 65536; // Not strictly needed for OpenExisting, but good to keep in sync mentally

    [McpServerTool(Name = "get_shared_memory")]
    [Description("Reads JSON data from shared memory and returns it to the MCP client.")]
    public static JsonElement ReadSharedMemory()
    {
        try 
        {
            using var mmf = MemoryMappedFile.OpenExisting(MemoryName);
            using var accessor = mmf.CreateViewAccessor();

            int size = accessor.ReadInt32(0);
            if (size <= 0 || size > 65536) 
            {
                 return JsonDocument.Parse($"{{\"error\": \"Invalid size read from memory: {size}\"}}").RootElement.Clone();
            }

            byte[] buf = new byte[size];
            accessor.ReadArray(sizeof(int), buf, 0, size);

            string json = Encoding.UTF8.GetString(buf);

            // Console.WriteLine is tricky in Stdio transport as it might corrupt stdout JSON-RPC. 
            // Better to use ILogger if possible, or write to stderr.
            // For this example, we'll comment out Console.WriteLine or write to Error.
            Console.Error.WriteLine("MCP Server → read JSON: " + json);

            return JsonDocument.Parse(json).RootElement.Clone();
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
