using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json;

public static class Program
{
	private const string ServerName = "CoinFlip";
	private const string ServerVersion = "1.0.0";
	private const string CoinFlipToolName = "coin_flip";

	/*
	 * Main is the complete startup path for this MCP server process.
	 *
	 * Flow:
	 * 1) Build a tool definition that clients can discover via tools/list.
	 * 2) Create McpServerOptions with server identity and request handlers.
	 * 3) Start the server using stdio transport so MCP messages flow over stdin/stdout.
	 * 4) Run the server loop until the host/client stops the process.
	 */
	public static async Task Main(string[] args)
	{
		Tool coinFlipTool = CreateCoinFlipToolDefinition();

		McpServerOptions options = new()
		{
			ServerInfo = new Implementation { Name = ServerName, Version = ServerVersion },
			Handlers = new McpServerHandlers
			{
				ListToolsHandler = (request, cancellationToken) => ValueTask.FromResult(new ListToolsResult
				{
					Tools = [coinFlipTool]
				}),
				CallToolHandler = HandleCallTool
			}
		};

		await using McpServer server = McpServer.Create(new StdioServerTransport(ServerName), options);
		await server.RunAsync();
	}

	/*
	 * Defines how the coin_flip tool appears to MCP clients during discovery.
	 *
	 * - Name is the stable protocol contract the client calls.
	 * - Description is surfaced in tool metadata for humans/agents.
	 * - InputSchema is an empty object, which means this tool accepts no arguments.
	 */
	private static Tool CreateCoinFlipToolDefinition()
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
	 * Handles MCP tools/call requests for this server.
	 *
	 * For this PoC we support one tool only: coin_flip.
	 * If the requested tool name matches, we execute the local coin-flip logic
	 * and return a protocol-compliant text content block.
	 *
	 * If an unknown tool name is requested, we return an MCP protocol error.
	 */
	private static ValueTask<CallToolResult> HandleCallTool(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken)
	{
		if (request.Params?.Name == CoinFlipToolName)
		{
			return ValueTask.FromResult(new CallToolResult
			{
				Content = [new TextContentBlock { Text = FlipCoin() }]
			});
		}

		throw new McpProtocolException($"Unknown tool: '{request.Params?.Name}'", McpErrorCode.InvalidRequest);
	}

	/*
	 * Core business logic for the demo tool.
	 *
	 * Random.Shared.Next(2) returns 0 or 1 with equal probability,
	 * so this produces a simple 50/50 heads-or-tails result.
	 */
	private static string FlipCoin() => Random.Shared.Next(2) == 0 ? "heads" : "tails";
}
