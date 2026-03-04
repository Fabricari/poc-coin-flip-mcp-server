using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json;

public static class Program
{
	private const string ServerName = "CoinFlip";
	private const string ServerVersion = "1.0.0";
	private const string CoinFlipToolName = "coin_flip";

	/*
	 * Entry point for the MCP server process.
	 *
	 * In order, we:
	 * 1) Build the tool metadata shown in tools/list.
	 * 2) Build server options (identity + request handlers).
	 * 3) Start stdio transport so MCP messages use stdin/stdout.
	 * 4) Run until the host process stops.
	 */
	public static async Task Main(string[] args)
	{
		Tool coinFlipTool = BuildCoinFlipTool();
		McpServerOptions serverOptions = BuildServerOptions(coinFlipTool);

		await using McpServer server = McpServer.Create(new StdioServerTransport(ServerName), serverOptions);
		await server.RunAsync();
	}

	/*
	 * Describes the coin_flip tool for discovery.
	 *
	 * - Name is the tool ID clients call.
	 * - Description is shown in client UIs.
	 * - InputSchema is empty because this tool takes no arguments.
	 */
	private static Tool BuildCoinFlipTool()
	{
		return new Tool
		{
			Name = CoinFlipToolName,
			Description = "Flips a fair coin and returns heads or tails.",
			InputSchema = JsonSerializer.Deserialize<JsonElement>("""
				{
				  "type": "object",
				  "properties": {}
				}
				""")
		};
	}

	/*
	 * Builds server options in one place so Main stays easy to scan.
	 */
	private static McpServerOptions BuildServerOptions(Tool coinFlipTool)
	{
		return new McpServerOptions
		{
			ServerInfo = new Implementation
			{
				Name = ServerName,
				Version = ServerVersion
			},
			Handlers = BuildHandlers(coinFlipTool)
		};
	}

	/*
	 * Configures the two handler paths this demo supports.
	 *
	 * - tools/list returns the one tool we expose.
	 * - tools/call runs the coin flip and returns text.
	 */
	private static McpServerHandlers BuildHandlers(Tool coinFlipTool)
	{
		return new McpServerHandlers
		{
			ListToolsHandler = (_, _) => ValueTask.FromResult(new ListToolsResult
			{
				Tools = [coinFlipTool]
			}),
			CallToolHandler = (_, _) => ValueTask.FromResult(new CallToolResult
			{
				Content = [new TextContentBlock { Text = FlipCoin() }]
			})
		};
	}

	/*
	 * Core logic for the demo tool.
	 *
	 * Random.Shared.Next(2) returns either 0 or 1,
	 * giving a simple 50/50 heads-or-tails result.
	 */
	private static string FlipCoin() => Random.Shared.Next(2) == 0 ? "heads" : "tails";
}
