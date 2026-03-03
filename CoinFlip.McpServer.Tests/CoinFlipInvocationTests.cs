using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace CoinFlip.McpServer.Tests;

// Integration tests that validate MCP client/server behavior over stdio transport.
public class CoinFlipInvocationTests
{
    // Update this parameter if your solution/project layout changes.
    // It should be the server DLL path relative to the repository root (where CoinFlip.sln is located).
    private const string ServerDllRelativePath = "CoinFlip.McpServer/bin/Debug/net10.0/CoinFlip.McpServer.dll";
    private const int RandomnessSampleSize = 40;

    [Fact]
    // Verifies the test can locate the built server process before attempting MCP calls.
    public void ServerDllPath_ResolvesToExistingFile()
    {
        string serverDllPath = ResolveServerDllPath();

        Assert.True(
            File.Exists(serverDllPath),
            $"Server DLL not found at resolved path: {serverDllPath}. Update {nameof(ServerDllRelativePath)} to the server DLL path relative to the repository root (where CoinFlip.sln is located).");
    }

    [Fact]
    // Confirms the server advertises coin_flip in MCP tool discovery metadata.
    public async Task CoinFlipTool_IsAdvertisedInToolsList()
    {
        // Resolve the built server DLL so the test can launch the MCP server process.
        string serverDllPath = ResolveServerDllPath();

        // Configure stdio transport: the client will start `dotnet <server-dll>`
        // and then speak MCP over the child process stdin/stdout streams.
        StdioClientTransport transport = new(new StdioClientTransportOptions
        {
            Name = "CoinFlip.Tests",
            Command = "dotnet",
            Arguments = [$"{serverDllPath}"]
        });

        // Create an MCP client connected to that transport.
        // `await using` ensures the underlying process/streams are cleaned up.
        await using McpClient client = await McpClient.CreateAsync(transport);

        // Ask the server for its advertised tools and verify `coin_flip` is present.
        IList<McpClientTool> tools = await client.ListToolsAsync();
        Assert.Contains(tools, tool => tool.Name == "coin_flip");
    }

    [Fact]
    // Executes coin_flip through MCP and validates the response contract.
    public async Task CoinFlipTool_ReturnsHeadsOrTails()
    {
        // Resolve the built server DLL so the test can launch the MCP server process.
        string serverDllPath = ResolveServerDllPath();

        // Configure stdio transport: the client will start `dotnet <server-dll>`
        // and then speak MCP over the child process stdin/stdout streams.
        StdioClientTransport transport = new(new StdioClientTransportOptions
        {
            Name = "CoinFlip.Tests",
            Command = "dotnet",
            Arguments = [$"{serverDllPath}"]
        });

        // Create an MCP client connected to that transport.
        // `await using` ensures the underlying process/streams are cleaned up.
        await using McpClient client = await McpClient.CreateAsync(transport);

        // Invoke the MCP tool by name with an empty argument payload.
        CallToolResult result = await client.CallToolAsync(
            "coin_flip",
            new Dictionary<string, object?>(),
            cancellationToken: CancellationToken.None);

        // The tool result can contain multiple content blocks; grab the first text block.
        string? text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text;

        // Contract check: this demo tool should only return one of two values.
        Assert.Contains(text, new[] { "heads", "tails" });
    }

    [Fact]
    // Sanity-checks randomness by verifying both outcomes appear over multiple invocations.
    public async Task CoinFlipTool_ProducesBothOutcomes_OverMultipleInvocations()
    {
        // Resolve the built server DLL so the test can launch the MCP server process.
        string serverDllPath = ResolveServerDllPath();

        // Configure stdio transport: the client will start `dotnet <server-dll>`
        // and then speak MCP over the child process stdin/stdout streams.
        StdioClientTransport transport = new(new StdioClientTransportOptions
        {
            Name = "CoinFlip.Tests",
            Command = "dotnet",
            Arguments = [$"{serverDllPath}"]
        });

        // Create an MCP client connected to that transport.
        // `await using` ensures the underlying process/streams are cleaned up.
        await using McpClient client = await McpClient.CreateAsync(transport);

        // Track outcomes across repeated tool invocations.
        HashSet<string> observedOutcomes = [];

        for (int attempt = 0; attempt < RandomnessSampleSize; attempt++)
        {
            // Invoke the MCP tool by name with an empty argument payload.
            CallToolResult result = await client.CallToolAsync(
                "coin_flip",
                new Dictionary<string, object?>(),
                cancellationToken: CancellationToken.None);

            // The tool result can contain multiple content blocks; grab the first text block.
            string? text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text;

            // Contract check for each invocation.
            Assert.Contains(text, new[] { "heads", "tails" });

            // Record the outcome so we can assert both values eventually appear.
            observedOutcomes.Add(text!);
        }

        // Over enough samples, we expect to observe both possible outcomes.
        Assert.Contains("heads", observedOutcomes);
        Assert.Contains("tails", observedOutcomes);
    }

    // Walks upward from test output paths until the repository root (CoinFlip.sln) is found.
    private static string ResolveServerDllPath()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            string solutionPath = Path.Combine(current.FullName, "CoinFlip.sln");
            if (File.Exists(solutionPath))
            {
                // Builds an absolute path to the server DLL from the discovered repository root.
                return Path.Combine(current.FullName, ServerDllRelativePath);
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing CoinFlip.sln.");
    }
}
