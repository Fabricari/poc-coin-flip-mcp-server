using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json;

public static class Program
{
	/*
	 * Entry point for the MCP server process.
	 *
	 * In order, we:
	 * 1) Build the tool metadata shown in tools/list.
	 * 2) Build server options (identity + request handlers wired to that tool).
	 * 3) Start stdio transport so MCP messages use stdin/stdout.
	 * 4) Run until the host process stops.
	 */
	public static async Task Main(string[] args)
	{
		Tool coinFlipTool = BuildCoinFlipTool();
		McpServerOptions serverOptions = BuildServerOptions(coinFlipTool);

		await using McpServer server = McpServer.Create(new StdioServerTransport("CoinFlip"), serverOptions);
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
			Name = "coin_flip",
			Description = "Can't decide? Flip a coin and get heads or tails.",
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
	 *
	 * Includes:
	 * - server identity metadata
	 * - tools/list handler that returns our single tool
	 * - tools/call handler that executes the coin flip
	 */
	private static McpServerOptions BuildServerOptions(Tool coinFlipTool)
	{
		return new McpServerOptions
		{
			ServerInfo = new Implementation
			{
				Name = "CoinFlip",
				Version = "1.0.0"
			},
			Handlers = new McpServerHandlers
			{
				ListToolsHandler = (_, _) => ValueTask.FromResult(new ListToolsResult
				{
					Tools = [coinFlipTool]
				}),
				CallToolHandler = (_, _) => ValueTask.FromResult(new CallToolResult
				{
					Content = [new TextContentBlock { Text = FlipCoin() }]
				})
			}
		};
	}

	/*
	* Core logic for the demo tool.
	*
	* Random.Shared.Next(2) returns either 0 or 1,
	* giving a simple heads-or-tails result.
	*
	* NOTE: We log to stderr (Console.Error) so the message appears
	* in the VS Code output window without interfering with the MCP
	* protocol messages that use stdout.
	*/
	private static string FlipCoin()
	{
		string result = Random.Shared.Next(2) == 0 ? "heads" : "tails";

		Console.Error.WriteLine($"Did a coin flip! And we got... {result}");

		return result;
	}
}
