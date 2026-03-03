# CoinFlip

This repository is a learning-focused proof of concept for building a minimal Model Context Protocol (MCP) server in C#. The goal is to understand MCP plumbing end to end: how a local process is launched, how tools are advertised, and how tool calls flow over stdio transport.

The server intentionally stays small and predictable. It exposes one tool, `coin_flip`, that returns `heads` or `tails`.

## Prerequisites

Before you start, install:

- .NET SDK 10 (`dotnet --version` should print `10.x`)
- VS Code
- GitHub Copilot extension for VS Code with Agent mode enabled

## Repository Structure

```text
PoC Coin Flip MCP/
├── CoinFlip.sln                 # Solution file for build/test commands
├── README.md                    # Project overview and usage notes
├── Docs/                        # Planning notes and project guidance
├── .vscode/                     # Local MCP host/editor configuration
├── CoinFlip.McpServer/          # C# console MCP server implementation
└── CoinFlip.McpServer.Tests/    # Integration tests invoking MCP transport
```

## Quick Start (After Cloning)

From the repository root:

```bash
dotnet restore
dotnet build CoinFlip.sln
dotnet test
```

If tests pass, your local setup is good.

## How To Use This Server In VS Code

1. Build the server so the DLL exists:

```bash
dotnet build CoinFlip.sln
```

2. Confirm [.vscode/mcp.json](.vscode/mcp.json) includes this server entry:

```jsonc
{
	"servers": {
		"coin-flip": {
			"type": "stdio",
			"command": "dotnet",
			"args": [
				"${workspaceFolder}/CoinFlip.McpServer/bin/Debug/net10.0/CoinFlip.McpServer.dll"
			]
		}
	}
}
```

3. Open Copilot Chat and switch to **Agent** mode.
4. Check available tools and verify `coin_flip` appears.
5. Test it with a prompt like: `Call coin_flip once and return only the result.`

### About The DLL Path

`${workspaceFolder}` is resolved automatically by VS Code to your current repo root. You usually should not change it.

You only need to update the rest of the path if you move project folders or change target framework output paths.

In other words, VS Code only needs a command + executable target it can launch; it does not require the DLL to stay in the project output folder.

## What The Tests Are Checking (And Why)

The tests in [CoinFlip.McpServer.Tests/CoinFlipInvocationTests.cs](CoinFlip.McpServer.Tests/CoinFlipInvocationTests.cs) are integration tests intended to verify MCP behavior at the boundary where real usage happens.

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

## Troubleshooting

- `coin_flip` missing in tools: run `dotnet build CoinFlip.sln`, then reload VS Code window.
- DLL not found: verify path in [.vscode/mcp.json](.vscode/mcp.json) matches `CoinFlip.McpServer/bin/Debug/net10.0/`.
- Tool approval prompts every time: in VS Code MCP tool permissions, set `coin_flip` to always allow for this workspace/session.

