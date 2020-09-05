using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DebuggerTests
{
    public class MonoJsTests : DebuggerTestBase
    {
        [Fact]
        public async Task FixupNameValueObjectsWithMissingParts()
        {
            var insp = new Inspector();
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);

                var bp1_res = await SetBreakpointInMethod("debugger-test.dll", "Math", "IntAdd", 3);

                var names = new JObject[]
                {
                    JObject.FromObject(new { name = "Abc" }),
                    JObject.FromObject(new { name = "Def" }),
                    JObject.FromObject(new { name = "Xyz" })
                };

                var values = new JObject[]
                {
                    JObject.FromObject(new { value = TObject("testclass") }),
                    JObject.FromObject(new { value = TString("test string") }),
                };

                var getters = new JObject[]
                {
                    GetterRes("xyz"),
                    GetterRes("unattached")
                };

                var list = new[] { names[0], names[1], values[0], names[2], getters[0], getters[1] };
                var res = await ctx.cli.SendCommand($"Runtime.evaluate", JObject.FromObject(new { expression = $"MONO._fixup_name_value_objects({JsonConvert.SerializeObject(list)})", returnByValue = true }), ctx.token);
                Assert.True(res.IsOk);

                await CheckProps(res.Value["result"]["value"], new
                {
                    Abc = TSymbol("<unreadable value>"),
                    Def = TObject("testclass"),
                    Xyz = TGetter("xyz")
                }, "#1", num_fields: 4);

                JObject.DeepEquals(getters[1], res.Value["result"]["value"].Values<JObject>().ToArray()[3]);

                JObject GetterRes(string name) => JObject.FromObject(new
                {
                    get = new
                    {
                        className = "Function",
                        description = $"get {name} () {{}}",
                        type = "function"
                    }
                });
            });
        }

        [Fact]
        public async Task GetParamsAndLocalsWithInvalidIndices()
        {
            var insp = new Inspector();
            var scripts = SubscribeToScripts(insp);

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);

                var bp1_res = await SetBreakpointInMethod("debugger-test.dll", "Math", "IntAdd", 3);
                var pause_location = await EvaluateAndCheck(
                    "window.setTimeout(function() { invoke_static_method('[debugger-test] Math:IntAdd', 1, 2); })",
                    null, -1, -1, "IntAdd");

                var scope_id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                var scope = int.Parse(scope_id.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[2]);

                var var_ids = new[]
                {
                    new { index = 0, name = "one" },
                    new { index = -12, name = "bad0" },
                    new { index = 1231, name = "bad1" }
                };

                var expression = $"MONO.mono_wasm_get_variables({scope}, {JsonConvert.SerializeObject(var_ids)})";

                var res = await ctx.cli.SendCommand($"Runtime.evaluate", JObject.FromObject(new { expression, returnByValue = true }), ctx.token);
                Assert.True(res.IsOk);

                await CheckProps(res.Value["result"]?["value"], new
                {
                    one = TNumber(3),
                    bad0 = TSymbol("<unreadable value>"),
                    bad1 = TSymbol("<unreadable value>")
                }, "results");
            });
        }

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

                scope_id = "30000";
                expression = $"MONO.mono_wasm_get_variables({scope_id}, {JsonConvert.SerializeObject(var_ids)})";
                res = await ctx.cli.SendCommand($"Runtime.evaluate", JObject.FromObject(new { expression, returnByValue = true }), ctx.token);
                Assert.False(res.IsOk);
            });
        }
    }
}
