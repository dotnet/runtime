using System;
using System.Threading.Tasks;
using Xunit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DebuggerTests
{
    public class MonoJsTests : DebuggerTestBase
    {
        [Fact]
        public async Task InvalidScopeId()
        {
            var insp = new Inspector();
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);

                var bp1_res = await SetBreakpointInMethod("debugger-test.dll", "Math", "IntAdd", 3);
                await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_static_method('[debugger-test] Math:IntAdd', 1, 2); })",
                    null, -1, -1, "IntAdd");

                var var_ids = new[]
                {
                    new { index = 0, name = "one" },
                };

                var scope_id = "-12";
                var expression = $"MONO.mono_wasm_get_variables({scope_id}, {JsonConvert.SerializeObject(var_ids)})";
                var res = await ctx.cli.SendCommand($"Runtime.evaluate", JObject.FromObject(new { expression, returnByValue = true }), ctx.token);
                Assert.False(res.IsOk);

                Console.WriteLine (res);

                scope_id = "30000";
                expression = $"MONO.mono_wasm_get_variables({scope_id}, {JsonConvert.SerializeObject(var_ids)})";
                res = await ctx.cli.SendCommand($"Runtime.evaluate", JObject.FromObject(new { expression, returnByValue = true }), ctx.token);
                Console.WriteLine (res);
                Assert.False(res.IsOk);

            });
        }
    }
}
