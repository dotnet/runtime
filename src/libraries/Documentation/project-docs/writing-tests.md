This document provides an overview of paradigm we are using in our tests.

# RemoteExecutor
We usually run tests fixtures in parallel, unless we add an exception to the root of the project. That mean that test cases in different test fixtures execute at the same time in random order. Therefore we need to be careful to avoid possible side-effects when manipulating static members (e.g. properties). Examples of problematic values: CurrentCulture, ServicePointManager.DefaultConnectionLimit, SetErrorMode (Windows).
We use `RemoteInvoke` which is defined in `RemoteExecutorTestBase.cs` to run test cases which need to be isolated. We mostly do this when we need to modify static members.
RemoteExecutor is a simple console application which accepts arguments that point to an existing method in the test assembly and additional arguments you might need in your test. For additional information see https://github.com/dotnet/corefx/blob/master/src/Common/tests/System/Diagnostics/RemoteExecutorTestBase.cs and https://xunit.github.io/docs/running-tests-in-parallel.html

Example (skipping additional usings):
```cs
using System.Diagnostics;

public class HttpWebRequestTest : RemoteExecutorTestBase
{
    [Fact]
    public void DefaultMaximumResponseHeadersLength_SetAndGetLength_ValuesMatch()
    {
        RemoteInvoke(() =>
        {
            const int NewDefaultMaximumResponseHeadersLength = 255;
            HttpWebRequest.DefaultMaximumResponseHeadersLength = NewDefaultMaximumResponseHeadersLength;
            Assert.Equal(NewDefaultMaximumResponseHeadersLength, HttpWebRequest.DefaultMaximumResponseHeadersLength);

            return SuccessExitCode;
        }).Dispose();
    }
}
```

 # LoopbackServer
When writing network related tests we try to avoid running tests against a remote endpoint if possible. We provide simple APIs to create a LoopbackServer and send responses. A high number of scenarios can be tested with it. For additional information see https://github.com/dotnet/corefx/blob/master/src/Common/tests/System/Net/Http/LoopbackServer.cs

Example (skipping additional usings and class scoping):
```cs
using System.Net.Test.Common;

[Fact]
public async Task Headers_SetAfterRequestSubmitted_ThrowsInvalidOperationException()
{
    await LoopbackServer.CreateServerAsync(async (server, uri) =>
    {
        HttpWebRequest request = WebRequest.CreateHttp(uri);
        Task<WebResponse> getResponse = request.GetResponseAsync();
        await LoopbackServer.ReadRequestAndSendResponseAsync(server);
        using (WebResponse response = await getResponse)
        {
            Assert.Throws<InvalidOperationException>(() => request.AutomaticDecompression = DecompressionMethods.Deflate);
        }
    });
}
```

# Outerloop
This one is fairly simple but often used incorrectly. When running tests which depend on outside influences like e.g. Hardware (Internet, SerialPort, ...) and you can't mitigate these dependencies, you might consider using the `[Outerloop]` attribute for your test. 
With this attribute, tests are executed in a dedicated CI loop and won't break the default CI loops which get created when you submit a PR.
To run Outerloop tests locally you need to set the msbuild property "Outerloop" to true: `/p:Outerloop=true`.
To run Outerloop tests in CI you need to mention dotnet-bot and tell him which tests you want to run. See `@dotnet-bot help` for the exact loop names.

This doesn't mean that you should mark every test which executes against a remote endpoint as Outerloop. See below.

# Relay Server
For network related tests which needs to contact a remote endpoint instead of a LoopbackServer, you can use our Relay Servers. We invest in Infrastructure to provide these "safe" remote endpoints.
For more information see https://github.com/dotnet/corefx/blob/master/src/Common/tests/System/Net/Configuration.Http.cs

Example:
```cs
public static readonly object[][] EchoServers = System.Net.Test.Common.Configuration.Http.EchoServers;

[Theory, MemberData(nameof(EchoServers))]
public async Task ContentLength_Get_ExpectSameAsGetResponseStream(Uri remoteServer)
{
    HttpWebRequest request = WebRequest.CreateHttp(remoteServer);
    ...
}
```

# Temp Directory
To support our tests running on as many target frameworks as possible, we need to be cautious when it comes to system resource access. The best example is trying to access a file outside of an AppContainer (UWP). We should depend on APIs which are specifically designed for these scenarios to work, e.g. TempDirectory. If a test case needs to store data on the FileSystem, consider using TempDirectory and TempFile APIs.

Example (skipping additional usings and class scoping):
```cs
using System.IO;

[Fact]
public void FileSystemWatcher_File_Changed_LastWrite()
{
    using (var testDirectory = new TempDirectory())
    using (var file = new TempFile(Path.Combine(testDirectory.Path, "file")))
    {
        Directory.SetLastWriteTime(file.Path, DateTime.Now + TimeSpan.FromSeconds(10));
        ...
    }
}
```
