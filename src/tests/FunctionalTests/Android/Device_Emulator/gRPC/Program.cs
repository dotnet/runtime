using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// The code of the tests is cloned from https://github.com/grpc/grpc-dotnet
using Grpc.Shared.TestAssets;

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
        ServerHost = "localhost",
        ServerPort = 50052,
        UseTls = true,
    },
};

int failedTests = 0;

foreach (var options in configurations)
{
    Console.WriteLine($"""
        gRPC client options:
        --------------------
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
        ---
        """);

    foreach (var testName in InteropClient.TestNames)
    {
        if (skippedTests.Contains(testName))
        {
            Log(testName, "SKIPPED");
            continue;
        }

        options.TestCase = testName;
        var client = new InteropClientWrapper(options);

        try
        {
            Log(testName, "STARTED");
            await client.Run();
            Log(testName, "PASSED");
        } catch (Exception e) {
            Log(testName, "FAILED");
            Console.Error.WriteLine(e);
            failedTests++;
        }
    }
}

return 42 + failedTests;

void Log(string testName, string status)
    => Console.WriteLine($"TestCase: {testName} ... {status}");

sealed class InteropClientWrapper
{
    private readonly InteropClient interopClient;

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Grpc.Testing.TestService.TestServiceClient", "Android.Device_Emulator.gRPC.Test")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Grpc.Testing.UnimplementedService.UnimplementedServiceClient", "Android.Device_Emulator.gRPC.Test")]
    public InteropClientWrapper(ClientOptions options)
    {
        interopClient = new InteropClient(
            options,
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance
        );
    }

    public Task Run()
        => interopClient.Run();
}
