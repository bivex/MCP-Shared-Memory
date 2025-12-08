using System.Text.Json;

/// <summary>
/// Example demonstrating usage patterns for the MCP Shared Memory Server.
/// This shows how MCP clients would interact with the server.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("🔗 MCP Shared Memory Client Usage Patterns");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        // Demonstrate the conceptual usage patterns
        await DemonstrateUsagePatterns();

        Console.WriteLine("🏁 Usage pattern demonstration completed!");
        Console.WriteLine();
        Console.WriteLine("📖 For actual MCP client implementation using the official SDK:");
        Console.WriteLine("   https://modelcontextprotocol.io/docs/tools/overview");
        Console.WriteLine("   Install: dotnet add package ModelContextProtocol --prerelease");
    }

    private static async Task DemonstrateUsagePatterns()
    {
        Console.WriteLine("📝 MCP Protocol Usage Patterns");
        Console.WriteLine("-----------------------------");
        Console.WriteLine();

        // Show how MCP clients would interact with our server via JSON-RPC
        Console.WriteLine("1. 🔍 List Available Tools");
        Console.WriteLine("   JSON-RPC Request:");
        var listToolsRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/list",
            @params = new { }
        };
        Console.WriteLine(JsonSerializer.Serialize(listToolsRequest, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine();

        Console.WriteLine("2. 💾 Write Shared Memory Data");
        Console.WriteLine("   JSON-RPC Request:");
        var testData = new
        {
            operation = "demo",
            timestamp = DateTime.UtcNow,
            client_version = "1.0.0",
            metadata = new { source = "MCP_Client_Example" }
        };
        var writeRequest = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/call",
            @params = new
            {
                name = "write_shared_memory",
                arguments = new
                {
                    json_data = JsonSerializer.Serialize(testData)
                }
            }
        };
        Console.WriteLine(JsonSerializer.Serialize(writeRequest, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine();

        Console.WriteLine("3. 📖 Read Shared Memory Data");
        Console.WriteLine("   JSON-RPC Request:");
        var readRequest = new
        {
            jsonrpc = "2.0",
            id = 3,
            method = "tools/call",
            @params = new
            {
                name = "get_shared_memory",
                arguments = new { }
            }
        };
        Console.WriteLine(JsonSerializer.Serialize(readRequest, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine();

        Console.WriteLine("4. 🏷️  Create Typed System Status");
        Console.WriteLine("   JSON-RPC Request:");
        var statusRequest = new
        {
            jsonrpc = "2.0",
            id = 4,
            method = "tools/call",
            @params = new
            {
                name = "create_system_status",
                arguments = new
                {
                    status = "healthy",
                    cpu_usage = 42,
                    memory_usage = 536870912L // 512MB in bytes
                }
            }
        };
        Console.WriteLine(JsonSerializer.Serialize(statusRequest, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine();

        Console.WriteLine("5. 💬 Send Message");
        Console.WriteLine("   JSON-RPC Request:");
        var messageRequest = new
        {
            jsonrpc = "2.0",
            id = 5,
            method = "tools/call",
            @params = new
            {
                name = "create_message",
                arguments = new
                {
                    content = "Hello from MCP client!",
                    sender = "SampleClient"
                }
            }
        };
        Console.WriteLine(JsonSerializer.Serialize(messageRequest, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine();

        Console.WriteLine("6. 📊 Record Performance Metrics");
        Console.WriteLine("   JSON-RPC Request:");
        var metricsRequest = new
        {
            jsonrpc = "2.0",
            id = 6,
            method = "tools/call",
            @params = new
            {
                name = "create_metrics",
                arguments = new
                {
                    name = "response_time",
                    value = 87.3,
                    unit = "milliseconds"
                }
            }
        };
        Console.WriteLine(JsonSerializer.Serialize(metricsRequest, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine();
    }

}
