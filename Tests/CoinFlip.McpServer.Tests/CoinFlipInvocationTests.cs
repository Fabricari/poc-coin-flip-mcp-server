using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace CoinFlip.McpServer.Tests;

public class CoinFlipInvocationTests
{
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(15);

    [Fact]
    public async Task CoinFlipTool_ReturnsHeadsOrTails()
    {
        string serverDllPath = GetServerDllPath();
        Assert.True(File.Exists(serverDllPath), $"Server DLL not found at expected path: {serverDllPath}");

        StdioClientTransport transport = new(new StdioClientTransportOptions
        {
            Name = "CoinFlip.Tests",
            Command = "dotnet",
            Arguments = [$"{serverDllPath}"]
        });

        await using McpClient client = await McpClient.CreateAsync(transport).WaitAsync(OperationTimeout);

        IList<McpClientTool> tools = await client.ListToolsAsync().AsTask().WaitAsync(OperationTimeout);
        Assert.Contains(tools, tool => tool.Name == "coin_flip");

        CallToolResult result = await client.CallToolAsync(
            "coin_flip",
            new Dictionary<string, object?>(),
            cancellationToken: CancellationToken.None).AsTask().WaitAsync(OperationTimeout);

        string? text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text;
        Assert.Contains(text, new[] { "heads", "tails" });
    }

    private static string GetServerDllPath()
    {
        string repoRoot = FindRepoRoot();
        return Path.Combine(repoRoot, "McpServer", "CoinFlip.McpServer", "bin", "Debug", "net10.0", "CoinFlip.McpServer.dll");
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            string solutionPath = Path.Combine(current.FullName, "CoinFlip.sln");
            if (File.Exists(solutionPath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing CoinFlip.sln.");
    }
}
