# Kerberos Testing Environment

## Introduction

In a typical enterprise setting, there is a domain controller that handles authentication requests and clients connecting to it. The clients here are referring both to the clients in a traditional sense (eg. `HttpClient`) and the services that are running on the server-side (eg. `HttpListener`).

Replicating that environment for unit testing is non-trivial since it usually requires multiple machines and adjusting system configuration. Alternatively, it could be set up using containers running on a single machine with a preprepared configuration. Unfortunately, using containers restricts the possibility of testing various operating systems, or at least makes it non-trivial.

To make the setup more approachable, this directory contains an implementation of `KerberosExecutor` class that sets up a virtual environment with Kerberos Domain Controller (KDC) running inside the unit test. The system configuration is then locally redirected for the test itself through environment variables to connect to this KDC instead of a system one. All the tests are run within a `RemoteExecutor` environment. The KDC is powered by the [Kerberos.NET](https://github.com/dotnet/Kerberos.NET) library.

## Usage

Since the environment currently works only on Linux and macOS platforms the test class or test method needs to be decorated to only run when `KerberosExecutor` is supported:

```csharp
[ConditionalClass(typeof(KerberosExecutor), nameof(KerberosExecutor.IsSupported))]
public class MyKerberosTest
```

The xUnit logging through `ITestOutputHelper` must be set up for the test class:

```csharp
private readonly ITestOutputHelper _testOutputHelper;

public MyKerberosTest(ITestOutputHelper testOutputHelper)
{
    _testOutputHelper = testOutputHelper;
}
```

Each test then uses the `KerberosExecutor` class to set up the virtual environment and the test code to run inside the virtual environment:

```csharp
[Fact]
public async Task Loopback_Success()
{
    using var kerberosExecutor = new KerberosExecutor(_testOutputHelper, "LINUX.CONTOSO.COM");

    kerberosExecutor.AddService("HTTP/linux.contoso.com");
    kerberosExecutor.AddUser("user");

    await kerberosExecutor.Invoke(() =>
    {
        // Test code
    }
}
```

The test itself can add its own users and services. Each service must be specified using full service principal name (SPN). In the example above the default password is used for the user, so the test would use `new NetworkCredential("user", KerberosExecutor.DefaultUserPassword, "LINUX.CONTOSO.COM")` to construct credentials for use in `HttpClient` or similar scenario.

## Logging

For failed unit test the verbose output of the KDC server is logged into the test output. If the information is insufficient it is possible to get a trace from the native libraries by setting the `KRB5_TRACE="/dev/stdout"` environment variable.