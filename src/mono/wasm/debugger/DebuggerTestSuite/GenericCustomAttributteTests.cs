using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace DebuggerTests;

public class GenericCustomAttributeTests : DebuggerTests
{
    public GenericCustomAttributeTests(ITestOutputHelper testOutput) : base(testOutput)
    {}

    [ConditionalFact(nameof(RunningOnChrome))]
    public async Task StopInCustomAttributeDecoratedClass()
    {
        int line = 18;
        int column = 8;
        string srcName = "debugger-generic-custom-attribute.cs";

        var bp1_res = await SetBreakpoint($"dotnet://debugger-test.dll/{srcName}", line, column);

        Assert.EndsWith(srcName, bp1_res.Value["breakpointId"].ToString());
        Assert.Equal(1, bp1_res.Value["locations"]?.Value<JArray>()?.Count);

        var loc = bp1_res.Value["locations"]?.Value<JArray>()[0];

        Assert.NotNull(loc["scriptId"]);
        Assert.Equal($"dotnet://debugger-test.dll/{srcName}", scripts[loc["scriptId"]?.Value<string>()]);
        Assert.Equal(line, (int)loc["lineNumber"]);
        Assert.Equal(column, (int)loc["columnNumber"]);
    }
}
