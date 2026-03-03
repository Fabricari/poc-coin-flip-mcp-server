# CoinFlip

MCP coin-flip demo workspace.

## Structure

- Docs/
- McpServer/
- McpClient/
- CoinFlip.sln

## What The Tests Validate

The integration test file at `Tests/CoinFlip.McpServer.Tests/CoinFlipInvocationTests.cs` is intentionally focused on MCP plumbing, not just coin-flip business logic.

It verifies three things:

1. The built server DLL path resolves correctly.
2. The MCP server advertises the `coin_flip` tool in tool discovery.
3. Invoking `coin_flip` returns a valid contract value (`heads` or `tails`).

## Why This Matters

These tests confirm the server can be launched and used the same way an MCP host would use it in practice: as a process connected over stdio transport.

That means we are validating:

- process startup,
- MCP handshake/tool discovery,
- tool invocation through the MCP protocol.

So this is an integration-style validation of the MCP service boundary, not a unit test of internal methods.

## How This Mirrors VS Code MCP Invocation

In the test, `StdioClientTransport` launches the server DLL with `dotnet <path-to-dll>`.

In VS Code, the built-in MCP host does the equivalent process launch from `.vscode/mcp.json` and communicates over stdio.

The test's DLL path resolution acts like local injection of the executable target, which is conceptually the same role that VS Code configuration plays when wiring a local MCP server process.
