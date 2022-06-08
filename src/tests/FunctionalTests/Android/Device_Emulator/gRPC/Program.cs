using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// The code of the tests is cloned from https://github.com/grpc/grpc-dotnet
using Grpc.Shared.TestAssets;

int failedTests = 0;
var skippedTests = new[]
{
    "compute_engine_creds",
    "jwt_token_creds",
    "oauth2_auth_token",
    "per_rpc_creds",
    "client_compressed_streaming", // flaky test
};

var configurations = new[]
{
    new ClientOptions
    {
        ServerHost = "grpc.rozsival.com", // TODO
        ServerPort = 443,
        UseTls = true,
    },
};

// var services = new ServiceCollection();
// services.AddLogging(configure =>
// {
//     configure.SetMinimumLevel(LogLevel.Trace);
//     configure.AddConsole(loggerOptions => loggerOptions.IncludeScopes = true);
// });

// using var serviceProvider = services.BuildServiceProvider();
// var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
var loggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;

foreach (var options in configurations) {
    Console.WriteLine($$"""
        gRPC client options
        -------------------
        ClientType: {options.ClientType}
        ServerHost: {options.ServerHost}
        ServerHostOverride: {options.ServerHostOverride}
        ServerPort: {options.ServerPort}
        UseTls: {options.UseTls}
        UseTestCa: {options.UseTestCa}
        DefaultServiceAccount: {options.DefaultServiceAccount}
        OAuthScope: {options.OAuthScope}
        ServiceAccountKeyFile: {options.ServiceAccountKeyFile}
        GrpcWebMode: {options.GrpcWebMode}
        UseWinHttp: {options.UseWinHttp}
        UseHttp3: {options.UseHttp3}
        """);

    foreach (var testName in InteropClient.TestNames) {
        if (skippedTests.Contains(testName)) {
            Console.WriteLine($"TestCase: {testName} ... FAILED");
            continue;
        }

        options.TestCase = testName;
        var client = new InteropClient(options, loggerFactory);

        try {
            Console.WriteLine($"TestCase: {testName} ... STARTED");
            await client.Run();
            Console.WriteLine($"TestCase: {testName} ... PASSED");
        } catch (Exception e) {
            Console.WriteLine($"TestCase: {testName} ... FAILED");
            Console.Error.WriteLine(e);
            failedTests++;
        }
    }
}

return 42 + failedTests;
