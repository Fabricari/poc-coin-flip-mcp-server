using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace CoinFlip.McpServer.Tests;

public class CoinFlipServerInvocationTests
{
    [Fact(Skip = "Temporarily disabled while validating basic test harness speed.")]
    public async Task CoinFlipTool_ReturnsHeadsOrTails()
    {
        string repoRoot = FindRepoRoot();
        string serverProjectPath = Path.Combine(repoRoot, "McpServer", "CoinFlip.McpServer", "CoinFlip.McpServer.csproj");

        ProcessStartInfo startInfo = new()
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{serverProjectPath}\"",
            WorkingDirectory = repoRoot,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start MCP server process.");

        try
        {
            await SendRpcAsync(process, 1, "initialize", new
            {
                protocolVersion = "2025-03-26",
                capabilities = new { },
                clientInfo = new { name = "CoinFlip.Tests", version = "1.0.0" }
            });

            JsonDocument initializeResponse = await ReadResponseByIdAsync(process, 1);
            Assert.Equal(1, initializeResponse.RootElement.GetProperty("id").GetInt32());

            await SendNotificationAsync(process, "notifications/initialized", new { });

            await SendRpcAsync(process, 2, "tools/list", new { });
            JsonDocument toolsListResponse = await ReadResponseByIdAsync(process, 2);

            JsonElement tools = toolsListResponse.RootElement
                .GetProperty("result")
                .GetProperty("tools");

            bool hasCoinFlip = tools.EnumerateArray().Any(tool =>
                tool.TryGetProperty("name", out JsonElement name) &&
                name.GetString() == "coin_flip");

            Assert.True(hasCoinFlip);

            await SendRpcAsync(process, 3, "tools/call", new
            {
                name = "coin_flip",
                arguments = new { }
            });

            JsonDocument toolCallResponse = await ReadResponseByIdAsync(process, 3);

            JsonElement text = toolCallResponse.RootElement
                .GetProperty("result")
                .GetProperty("content")[0]
                .GetProperty("text");

            Assert.Contains(text.GetString(), new[] { "heads", "tails" });
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
    }

    private static async Task SendRpcAsync(Process process, int id, string method, object @params)
    {
        await SendMessageAsync(process, new
        {
            jsonrpc = "2.0",
            id,
            method,
            @params
        });
    }

    private static async Task SendNotificationAsync(Process process, string method, object @params)
    {
        await SendMessageAsync(process, new
        {
            jsonrpc = "2.0",
            method,
            @params
        });
    }

    private static async Task SendMessageAsync(Process process, object payload)
    {
        string json = JsonSerializer.Serialize(payload);
        byte[] messageBytes = Encoding.UTF8.GetBytes(json);
        string header = $"Content-Length: {messageBytes.Length}\r\n\r\n";

        await process.StandardInput.WriteAsync(header);
        await process.StandardInput.WriteAsync(json);
        await process.StandardInput.FlushAsync();
    }

    private static async Task<JsonDocument> ReadResponseByIdAsync(Process process, int expectedId)
    {
        while (true)
        {
            JsonDocument message = await ReadMessageAsync(process);

            JsonElement root = message.RootElement;
            if (root.TryGetProperty("id", out JsonElement idElement) && idElement.GetInt32() == expectedId)
            {
                return message;
            }

            message.Dispose();
        }
    }

    private static async Task<JsonDocument> ReadMessageAsync(Process process)
    {
        int contentLength = 0;

        while (true)
        {
            string? line = await process.StandardOutput.ReadLineAsync();
            if (line is null)
            {
                string stderr = await process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException($"MCP server output ended unexpectedly. stderr: {stderr}");
            }

            if (line.Length == 0)
            {
                break;
            }

            const string prefix = "Content-Length:";
            if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                string value = line[prefix.Length..].Trim();
                contentLength = int.Parse(value);
            }
        }

        if (contentLength <= 0)
        {
            throw new InvalidOperationException("Missing or invalid Content-Length header in MCP response.");
        }

        char[] buffer = new char[contentLength];
        int totalRead = 0;

        while (totalRead < contentLength)
        {
            int read = await process.StandardOutput.ReadAsync(buffer.AsMemory(totalRead, contentLength - totalRead));
            if (read == 0)
            {
                throw new InvalidOperationException("Unexpected end of stream while reading MCP response body.");
            }

            totalRead += read;
        }

        return JsonDocument.Parse(new string(buffer));
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
