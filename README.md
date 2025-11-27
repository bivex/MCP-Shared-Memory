# MCP Shared Memory Solution

This solution demonstrates a **Model Context Protocol (MCP)** server that reads data from **Shared Memory** (Memory Mapped File) which is populated by a separate .NET application.

## Projects

### 1. SharedMemoryApp
A console application that writes a JSON payload to a shared memory segment named `Global\MCP_SharedMemory`.

### 2. McpSharedServer
An MCP Server implementation that exposes a tool `get_shared_memory`. This tool reads the data from the shared memory and returns it to the MCP client.

## Prerequisites
- .NET 8.0 SDK

## How to Run

### Step 1: Start the Writer App
This application must be running (or have run) to populate the shared memory.

```powershell
dotnet run --project SharedMemoryApp/SharedMemoryApp.csproj
```

### Step 2: Start the MCP Server
In a separate terminal, start the MCP server.

```powershell
dotnet run --project McpSharedServer/McpSharedServer.csproj
```

### Step 3: Connect an MCP Client
Configure your MCP client (e.g., Claude Desktop, VS Code MCP Extension) to use the server.

**Command:** `dotnet`
**Args:** `run --project /absolute/path/to/McpSharedServer/McpSharedServer.csproj`

(Or build the binary and point to the `.exe`)

## Available Tools

- **`get_shared_memory`**: Reads the current JSON payload from the shared memory.
