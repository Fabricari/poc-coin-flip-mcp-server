# CoinFlip

This repository is a learning-focused proof of concept for building a minimal Model Context Protocol (MCP) server in C#. The goal is to understand MCP plumbing end to end: how a local process is launched, how tools are advertised, and how tool calls flow over stdio transport.

The server intentionally stays small and predictable. It exposes one tool, `coin_flip`, that returns `heads` or `tails`.

## Repository Structure

- [Docs](Docs) — planning notes and project guidance.
- [McpServer](McpServer) — the C# console MCP server implementation.
- [Tests](Tests) — integration tests that invoke the server through MCP transport.
- [McpClient](McpClient) — reserved for future experiments (not required for the current flow).
- [CoinFlip.sln](CoinFlip.sln) — solution file for build/test commands.

## How To Use This Server In VS Code

1. Build the server so the DLL exists:

```bash
dotnet build CoinFlip.sln
```

2. Configure local MCP host wiring in [.vscode/mcp.json](.vscode/mcp.json).

3. Start the MCP server from VS Code/Copilot Agent mode and verify `coin_flip` appears.

Example configuration shape:

```json
{
	"servers": {
		"coin-flip": {
			"type": "stdio",
			"command": "dotnet",
			"args": ["/absolute/path/to/CoinFlip.McpServer.dll"]
		}
	}
}
```

### About The DLL Path

The server DLL does not need to live under this repository. You can point `args` to any valid DLL location on your machine (absolute path), or use a repository-relative value if that is more convenient for local development.

In other words, VS Code only needs a command + executable target it can launch; it does not require the DLL to stay in the project output folder.

## What The Tests Are Checking (And Why)

The tests in [Tests/CoinFlip.McpServer.Tests/CoinFlipInvocationTests.cs](Tests/CoinFlip.McpServer.Tests/CoinFlipInvocationTests.cs) are integration tests intended to verify MCP behavior at the boundary where real usage happens.

They check:

1. The server process target can be located.
2. MCP tool discovery includes `coin_flip`.
3. MCP tool invocation returns a valid result (`heads` or `tails`).
4. A small sanity loop observes both outcomes over repeated calls.

Why this matters: these tests confirm the server can be launched and used through MCP transport (the same style used by hosts like VS Code), not just that an internal method returns a value.

## Run Tests

```bash
dotnet test
```

