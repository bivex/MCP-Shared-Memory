# MCP Shared Memory Server User Guide

## Front Matter

**Document Title:** MCP Shared Memory Server User Guide  
**Document Version:** 1.0.0  
**Document Date:** December 2025  
**Document Status:** Released  
**Product Name:** MCP Shared Memory Server  
**Product Version:** 1.0.0  
**Target Audience:** Software developers, system administrators, MCP client users  
**Purpose:** This guide provides instructions for installing, configuring, and using the MCP Shared Memory Server for inter-process communication via shared memory on Windows systems.

**Copyright Notice:**  
© 2025 MCP Shared Memory Server Project. All rights reserved.  
This document is provided "as is" without warranty of any kind.

**Document Conventions:**
- **Bold text** indicates user interface elements or emphasis.
- `Monospace text` indicates code, commands, or file names.
- ⚠️ **Warning** indicates potential data loss or security risks.
- ℹ️ **Note** provides additional information.
- 📋 **Procedure** indicates step-by-step instructions.

---

## Table of Contents

### Front Matter
- Document Title, Version, and Status ................... 1
- Copyright Notice ...................................... 1
- Document Conventions .................................. 1

### 1. Introduction ....................................... 2
- 1.1 Purpose and Scope ................................. 2
- 1.2 Target Audience ................................... 2
- 1.3 Product Overview .................................. 3
- 1.4 Related Documents ................................. 3

### 2. Concept of Operations ............................... 4
- 2.1 System Architecture ............................... 4
- 2.2 Operational Modes ................................. 7
- 2.3 Data Flow ......................................... 7

### 3. Procedures ......................................... 8
- 3.1 System Requirements ............................... 8
- 3.2 Installation Procedure ............................ 8
- 3.3 Configuration Procedure ........................... 10
- 3.4 Startup Procedure ................................. 10
- 3.5 Normal Operation Procedures ....................... 11
  - 3.5.1 Basic Memory Operations ....................... 11
  - 3.5.2 Typed Data Operations ......................... 12
- 3.6 Shutdown Procedure ................................ 13

### 4. Information for Uninstallation ...................... 14
- 4.1 Uninstallation Procedure .......................... 14
- 4.2 Post-Uninstallation Considerations ................ 14

### 5. Troubleshooting .................................... 15
- 5.1 Common Issues and Solutions ....................... 15
- 5.2 Error Messages and Meanings ....................... 16
- 5.3 Diagnostic Procedures ............................. 16

### 6. Glossary ............................................ 17

### 7. Index ............................................... 19

### Appendices ............................................. 23
- Appendix A: MCP Tools Reference ........................ 23
- Appendix B: Sample Applications ........................ 24

---

## 1. Introduction

### 1.1 Purpose and Scope

This user guide describes the MCP Shared Memory Server, a software component that implements the Model Context Protocol (MCP) for managing shared memory operations on Windows systems. The guide covers installation, configuration, operation, and troubleshooting of the server.

The scope includes:
- System requirements and prerequisites
- Installation and setup procedures
- Basic and advanced usage scenarios
- Maintenance and troubleshooting
- Uninstallation procedures

### 1.2 Target Audience

This guide is intended for:
- **Software Developers** who need to integrate shared memory capabilities into their applications
- **System Administrators** responsible for deploying and maintaining MCP servers
- **End Users** of MCP-compatible client applications (e.g., Claude Desktop, VS Code)

### 1.3 Product Overview

The MCP Shared Memory Server is an advanced implementation of a Model Context Protocol (MCP) server that provides comprehensive shared memory management capabilities for Windows applications. Built using the official MCP C# SDK, this server enables secure and efficient inter-process communication through memory-mapped files.

Key features:
- Thread-safe shared memory operations
- Typed data handling (SystemStatus, Messages, Metrics)
- MCP protocol compliance
- JSON-RPC 2.0 communication
- Comprehensive error handling and logging

### 1.4 Related Documents

- [Model Context Protocol Specification](https://modelcontextprotocol.io/specification/)
- [MCP C# SDK Documentation](https://modelcontextprotocol.github.io/csharp-sdk/api/ModelContextProtocol.html)
- [.NET 8.0 Documentation](https://learn.microsoft.com/en-us/dotnet/)

---

## 2. Concept of Operations

### 2.1 System Architecture

The MCP Shared Memory Server operates within a client-server architecture using the Model Context Protocol. The system consists of three main components:

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│ SharedMemoryApp │───▶│  Global Shared   │◀───│ McpSharedServer │
│   (Writer)      │    │    Memory        │    │   (MCP Server)  │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                              │
                              ▼
                       ┌──────────────┐
                       │ MCP Clients  │
                       │ (Claude, VS  │
                       │  Code, etc.) │
                       └──────────────┘
```

**SharedMemoryApp:** A .NET console application that writes data to shared memory.  
**McpSharedServer:** The MCP server that exposes shared memory operations via MCP tools.  
**Global Shared Memory:** Windows memory-mapped file for inter-process communication.  
**MCP Clients:** Compatible applications that can invoke server tools.

### 2.2 Operational Modes

The server supports two primary operational modes:

1. **Standalone Mode:** Server runs independently, managing shared memory operations.
2. **Integrated Mode:** Server integrates with MCP-compatible client applications for enhanced functionality.

### 2.3 Data Flow

1. SharedMemoryApp writes data to Windows shared memory.
2. McpSharedServer monitors and manages the shared memory.
3. MCP clients connect to the server and invoke tools to read/write data.
4. All operations are thread-safe with proper synchronization.

---

## 3. Procedures

### 3.1 System Requirements

**Hardware Requirements:**
- Windows operating system (Windows 10/11 recommended)
- Minimum 4 GB RAM
- 100 MB available disk space

**Software Requirements:**
- .NET 8.0 SDK or later
- Administrator privileges (for global shared memory access)
- MCP-compatible client application

### 3.2 Installation Procedure

📋 **Procedure: Installing the MCP Shared Memory Server**

1. **Download the source code:**
   ```bash
   git clone <repository-url>
   cd MCP-Shared-Memory
   ```

2. **Install required packages:**
   ```bash
   dotnet add package ModelContextProtocol --prerelease
   dotnet add package Microsoft.Extensions.Hosting
   ```

3. **Build the solution:**
   ```bash
   dotnet build
   ```

4. **Verify installation:**
   ```bash
   dotnet run --project SharedMemoryApp/SharedMemoryApp.csproj
   ```
   Expected result: Application starts and provides an interactive menu.

### 3.3 Configuration Procedure

The server uses default configuration. For advanced scenarios:

1. Review `SharedMemoryTools.cs` for memory size limits.
2. Modify `MaxMemorySize` constant if needed (default: 1MB).
3. Ensure global shared memory name matches: `Global\MCP_SharedMemory`.

### 3.4 Startup Procedure

📋 **Procedure: Starting the MCP Shared Memory Server**

1. **Start the shared memory writer (optional for testing):**
   ```bash
dotnet run --project SharedMemoryApp/SharedMemoryApp.csproj
```

2. **Start the MCP server:**
   ```bash
   dotnet run --project McpSharedServer/McpSharedServer.csproj
   ```

3. **Verify server is running:**
   - Server should display startup messages
   - No error messages should appear

### 3.5 Normal Operation Procedures

#### 3.5.1 Basic Memory Operations

**Reading data:**
```csharp
var result = await client.CallToolAsync("get_shared_memory");
```

**Writing data:**
```csharp
await client.CallToolAsync("write_shared_memory", new()
{
    ["json_data"] = "{\"status\": \"active\"}"
});
```

#### 3.5.2 Typed Data Operations

**Creating system status:**
```csharp
await client.CallToolAsync("create_system_status", new()
{
    ["status"] = "healthy",
    ["cpu_usage"] = 45,
    ["memory_usage"] = 2048576000L
});
```

**Sending messages:**
```csharp
await client.CallToolAsync("create_message", new()
{
    ["content"] = "Hello from MCP client!",
    ["sender"] = "MCP_Client_1"
});
```

### 3.6 Shutdown Procedure

📋 **Procedure: Shutting Down the Server**

1. **Stop MCP clients** connected to the server.
2. **Terminate the MCP server process:**
   - Press `Ctrl+C` in the terminal running the server
   - Or close the terminal window

3. **Stop the shared memory application** (if running):
   - Press `Ctrl+C` or close the application

---

## 4. Information for Uninstallation

### 4.1 Uninstallation Procedure

📋 **Procedure: Uninstalling the MCP Shared Memory Server**

1. **Stop all running processes:**
   - Terminate any running instances of `McpSharedServer`
   - Terminate any running instances of `SharedMemoryApp`

2. **Remove shared memory resources:**
   - Windows automatically cleans up memory-mapped files when processes exit
   - No manual cleanup required

3. **Remove project files:**
   ```bash
   # Navigate to project directory
   cd MCP-Shared-Memory

   # Remove build artifacts
   dotnet clean

   # Remove entire directory (optional)
   cd ..
   rm -rf MCP-Shared-Memory
   ```

4. **Remove NuGet packages** (if installed globally):
   ```bash
   dotnet nuget locals all --clear
   ```

### 4.2 Post-Uninstallation Considerations

- Shared memory resources are automatically released by Windows
- No system registry entries are modified
- No Windows services are installed
- Project can be safely deleted without residual files

---

## 5. Troubleshooting

### 5.1 Common Issues and Solutions

**Issue: "Access denied" when starting applications**  
**Solution:** Run applications as Administrator. Global shared memory requires elevated privileges.

**Issue: "Shared memory not found" error**  
**Solution:** Ensure SharedMemoryApp has been run at least once to create the shared memory segment.

**Issue: Server fails to start**  
**Solution:** Verify .NET 8.0 SDK is installed and all dependencies are available.

**Issue: Memory size limit exceeded**  
**Solution:** Reduce data size or modify `MaxMemorySize` constant in the source code.

### 5.2 Error Messages and Meanings

- `"Invalid data size: X"` - Data being read exceeds expected limits
- `"Memory manager is in read-only mode"` - Attempting to write when manager is read-only
- `"Operation failed after all retries"` - Network or system errors after retry attempts

### 5.3 Diagnostic Procedures

📋 **Procedure: Collecting Diagnostic Information**

1. **Check server logs:**
   - Review console output for error messages
   - Look for startup confirmation messages

2. **Verify shared memory state:**
   ```csharp
   var info = await client.CallToolAsync("get_shared_memory_info");
   ```

3. **Test basic connectivity:**
   ```csharp
   var tools = await client.ListToolsAsync();
   ```

---

## 6. Glossary

**MCP (Model Context Protocol):** An open protocol that standardizes how applications provide context to Large Language Models.

**Shared Memory:** A memory segment that can be accessed by multiple processes, enabling inter-process communication.

**Memory-Mapped File:** A mechanism that maps a file or memory region into the address space of a process.

**JSON-RPC 2.0:** A stateless, light-weight remote procedure call protocol encoded in JSON.

**Global Shared Memory:** A shared memory segment accessible system-wide on Windows (requires Administrator privileges).

**Thread Safety:** Property of code that allows it to run correctly in multi-threaded environments.

---

## 7. Index

### A
- Access Control: Section 3.5, Appendix A
- Administrator Privileges: Section 3.1, Section 5.1
- Appendices: Section 8
- Architecture: Section 2.1
- Audience, Target: Section 1.2

### B
- Basic Memory Operations: Section 3.5.1, Appendix A

### C
- Client Integration: Section 3.5
- Common Issues: Section 5.1
- Concept of Operations: Section 2
- Configuration: Section 3.3
- Conventions, Document: Front Matter
- Copyright: Front Matter
- CPU Usage: Section 3.5.2

### D
- Data Flow: Section 2.3
- Data Types, Typed: Section 3.5.2, Appendix A
- Diagnostic Procedures: Section 5.3
- Document Conventions: Front Matter

### E
- Error Messages: Section 5.2
- Error Handling: Section 5

### F
- Features, Key: Section 1.3
- Front Matter: Front Matter

### G
- Glossary: Section 6
- Global Shared Memory: Section 2.1, Section 6

### H
- Hardware Requirements: Section 3.1

### I
- Index: Section 7
- Installation: Section 3.2
- Integrated Mode: Section 2.2
- Introduction: Section 1

### J
- JSON-RPC 2.0: Section 2.3, Section 6

### L
- License: Appendix B (implied)

### M
- MCP Clients: Section 2.1
- MCP Protocol: Section 1.3, Section 6
- MCP Tools: Section 3.5, Appendix A
- Memory Configuration: Section 3.3
- Memory Size Limits: Section 3.5, Appendix A
- Memory-Mapped File: Section 6
- Metrics, Performance: Section 3.5.2, Appendix A

### N
- Normal Operation: Section 3.5

### O
- Operational Modes: Section 2.2
- Overview, Product: Section 1.3

### P
- Performance Metrics: Section 3.5.2
- Prerequisites: Section 3.1
- Procedures: Section 3
- Product Overview: Section 1.3
- Purpose and Scope: Section 1.1

### R
- Related Documents: Section 1.4
- Requirements, System: Section 3.1

### S
- Sample Applications: Appendix B
- Scope: Section 1.1
- Shared Memory: Section 2.3, Section 6
- Shutdown Procedure: Section 3.6
- Software Requirements: Section 3.1
- Standalone Mode: Section 2.2
- Startup Procedure: Section 3.4
- Status, System: Section 3.5.2, Appendix A
- System Administrators: Section 1.2
- System Architecture: Section 2.1
- System Requirements: Section 3.1

### T
- Table of Contents: Table of Contents
- Target Audience: Section 1.2
- Thread Safety: Section 1.3, Section 6
- Tools, MCP: Section 3.5, Appendix A
- Troubleshooting: Section 5
- Typed Data Operations: Section 3.5.2, Appendix A

### U
- Uninstallation: Section 4
- Usage Scenarios: Section 1.1

### V
- Version, Document: Front Matter
- Version, Product: Front Matter

---

## Appendices

### Appendix A: MCP Tools Reference

**Basic Memory Operations:**
- `get_shared_memory` - Reads JSON data from shared memory
- `write_shared_memory` - Writes JSON data to shared memory
- `clear_shared_memory` - Clears all data from shared memory
- `get_shared_memory_info` - Returns memory state information

**Typed Data Operations:**
- `create_system_status` - Creates system status data
- `create_message` - Creates message data
- `create_metrics` - Creates performance metrics data
- `write_typed_data` / `read_typed_data` - Typed data operations

### Appendix B: Sample Applications

The `samples/McpSharedMemoryClient/` directory contains a demonstration application showing JSON-RPC protocol patterns.

**Run the sample:**
```bash
dotnet run --project samples/McpSharedMemoryClient/McpSharedMemoryClient.csproj
```

---

*End of Document*
