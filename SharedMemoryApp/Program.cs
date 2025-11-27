using System;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Text.Json;

public class Program
{
    private const string MemoryName = "Global\\MCP_SharedMemory";
    private const int MemorySize = 65536;

    public static void Main()
    {
        var payload = new
        {
            time = DateTime.Now,
            status = "ok",
            value = 42,
            message = "data from app"
        };

        string json = JsonSerializer.Serialize(payload);
        byte[] buf = Encoding.UTF8.GetBytes(json);

        // Ensure we handle the case where the file might not exist or needs to be created fresh
        // For simplicity in this example, we'll try to create it, or open if it exists.
        // Note: In Windows, "Global\" prefix requires running as Admin or specific privileges usually,
        // but for local testing "Local\" or just name might be safer if Admin is not guaranteed.
        // However, user asked for "Global\\MCP_SharedMemory", so we stick to it.
        
        MemoryMappedFile mmf;
        try 
        {
            mmf = MemoryMappedFile.CreateOrOpen(MemoryName, MemorySize);
        }
        catch (UnauthorizedAccessException)
        {
             Console.WriteLine("Error: Access denied. Try running as Administrator or check permissions for Global\\ shared memory.");
             return;
        }

        using (mmf)
        {
            using var accessor = mmf.CreateViewAccessor();

            accessor.Write(0, buf.Length);
            accessor.WriteArray(sizeof(int), buf, 0, buf.Length);

            Console.WriteLine("App → wrote JSON to shared memory:");
            Console.WriteLine(json);
            
            Console.WriteLine("Press any key to exit...");
            Console.Read();
        }
    }
}
