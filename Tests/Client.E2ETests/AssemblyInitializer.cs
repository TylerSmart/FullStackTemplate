namespace App.Client.E2ETests;

[TestClass]
public static class AssemblyInitializer
{
    private static PlaywrightServerFixture? _server;

    public static string ServerUrl =>
        _server?.ServerUrl
        ?? throw new InvalidOperationException("E2E Test server is not initialized.");

    [AssemblyInitialize]
    public static async Task AssemblyInit(TestContext context)
    {
        _server = await PlaywrightServerFixture.StartAsync();
    }

    [AssemblyCleanup]
    public static async Task AssemblyCleanup()
    {
        if (_server is not null)
        {
            await _server.DisposeAsync();
            _server = null;
        }
    }
}
