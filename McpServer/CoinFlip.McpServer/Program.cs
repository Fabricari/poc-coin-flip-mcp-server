using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json;

public static class Program
{
	public static async Task Main(string[] args)
	{
		McpServerOptions options = new()
		{
			ServerInfo = new Implementation { Name = "CoinFlip", Version = "1.0.0" },
			Handlers = new McpServerHandlers
			{
				ListToolsHandler = (request, cancellationToken) =>
					ValueTask.FromResult(new ListToolsResult
					{
						Tools =
						[
							new Tool
							{
								Name = "coin_flip",
								Description = "Flips a fair coin and returns heads or tails.",
								InputSchema = JsonSerializer.Deserialize<JsonElement>("""
									{
									  "type": "object",
									  "properties": {}
									}
									""")
							}
						]
					}),

				CallToolHandler = (request, cancellationToken) =>
				{
					if (request.Params?.Name == "coin_flip")
					{
						return ValueTask.FromResult(new CallToolResult
						{
							Content = [new TextContentBlock { Text = FlipCoin() }]
						});
					}

					throw new McpProtocolException($"Unknown tool: '{request.Params?.Name}'", McpErrorCode.InvalidRequest);
				}
			}
		};

		await using McpServer server = McpServer.Create(new StdioServerTransport("CoinFlip"), options);
		await server.RunAsync();
	}

	private static string FlipCoin() => Random.Shared.Next(2) == 0 ? "heads" : "tails";
}
