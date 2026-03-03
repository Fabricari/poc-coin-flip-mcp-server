using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace CoinFlip.McpServer.Tests;

public class CoinFlipInvocationTests
{
    // Update this parameter if your solution/project layout changes.
    // It should be the server DLL path relative to the repository root (where CoinFlip.sln is located).
    private const string ServerDllRelativePath = "McpServer/CoinFlip.McpServer/bin/Debug/net10.0/CoinFlip.McpServer.dll";

    [Fact]
    public void ServerDllPath_ResolvesToExistingFile()
    {
        string serverDllPath = ResolveServerDllPath();

        Assert.True(
            File.Exists(serverDllPath),
            $"Server DLL not found at resolved path: {serverDllPath}. Update {nameof(ServerDllRelativePath)} to the server DLL path relative to the repository root (where CoinFlip.sln is located).");
    }

    [Fact]
    public async Task CoinFlipTool_IsAdvertisedInToolsList()
    {
        await using McpClient client = await CreateClientAsync();

        IList<McpClientTool> tools = await client.ListToolsAsync();
        Assert.Contains(tools, tool => tool.Name == "coin_flip");
    }

    [Fact]
    public async Task CoinFlipTool_ReturnsHeadsOrTails()
    {
        await using McpClient client = await CreateClientAsync();

        CallToolResult result = await client.CallToolAsync(
            "coin_flip",
            new Dictionary<string, object?>(),
            cancellationToken: CancellationToken.None);

        string? text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text;
        Assert.Contains(text, new[] { "heads", "tails" });
    }

    private static Task<McpClient> CreateClientAsync()
    {
        string serverDllPath = ResolveServerDllPath();

        StdioClientTransport transport = new(new StdioClientTransportOptions
        {
            Name = "CoinFlip.Tests",
            Command = "dotnet",
            Arguments = [$"{serverDllPath}"]
        });

        return McpClient.CreateAsync(transport);
    }

    private static string ResolveServerDllPath()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            string solutionPath = Path.Combine(current.FullName, "CoinFlip.sln");
            if (File.Exists(solutionPath))
            {
                return Path.Combine(current.FullName, ServerDllRelativePath);
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing CoinFlip.sln.");
    }
}
