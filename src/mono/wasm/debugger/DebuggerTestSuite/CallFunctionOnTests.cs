// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DebuggerTests
{

    public class CallFunctionOnTests : DebuggerTestBase
    {

        // This tests `callFunctionOn` with a function that the vscode-js-debug extension uses
        // Using this here as a non-trivial test case
        [Theory]
        [InlineData("big_array_js_test (10);", "/other.js", 10, 1, 10, false)]
        [InlineData("big_array_js_test (0);", "/other.js", 10, 1, 0, true)]
        [InlineData("invoke_static_method ('[debugger-test] DebuggerTests.CallFunctionOnTest:LocalsTest', 10);", "dotnet://debugger-test.dll/debugger-cfo-test.cs", 23, 12, 10, false)]
        [InlineData("invoke_static_method ('[debugger-test] DebuggerTests.CallFunctionOnTest:LocalsTest', 0);", "dotnet://debugger-test.dll/debugger-cfo-test.cs", 23, 12, 0, true)]
        public async Task CheckVSCodeTestFunction1(string eval_fn, string bp_loc, int line, int col, int len, bool roundtrip)
        {
            string vscode_fn0 = "function(){const e={__proto__:this.__proto__},t=Object.getOwnPropertyNames(this);for(let r=0;r<t.length;++r){const n=t[r],i=n>>>0;if(String(i>>>0)===n&&i>>>0!=4294967295)continue;const a=Object.getOwnPropertyDescriptor(this,n);a&&Object.defineProperty(e,n,a)}return e}";

            await RunCallFunctionOn(eval_fn, vscode_fn0, "big", bp_loc, line, col, res_array_len: len, roundtrip: roundtrip,
                test_fn: async (result) =>
               {

                   var is_js = bp_loc.EndsWith(".js", StringComparison.Ordinal);
                   var obj_accessors = await cli.SendCommand("Runtime.getProperties", JObject.FromObject(new
                   {
                       objectId = result.Value["result"]["objectId"].Value<string>(),
                       accessorPropertiesOnly = true,
                       ownProperties = false
                   }), token);
                   if (is_js)
                       await CheckProps(obj_accessors.Value["result"], new { __proto__ = TIgnore() }, "obj_accessors");
                   else
                       AssertEqual(0, obj_accessors.Value["result"]?.Count(), "obj_accessors-count");

                   // Check for a __proto__ object
                   // isOwn = true, accessorPropertiesOnly = false
                   var obj_own = await cli.SendCommand("Runtime.getProperties", JObject.FromObject(new
                   {
                       objectId = result.Value["result"]["objectId"].Value<string>(),
                       accessorPropertiesOnly = false,
                       ownProperties = true
                   }), token);

                   await CheckProps(obj_own.Value["result"], new
                   {
                       length = TNumber(len),
                       // __proto__ = TArray (type, 0) // Is this one really required?
                   }, $"obj_own");

               });
        }

        void CheckJFunction(JToken actual, string className, string label)
        {
            AssertEqual("function", actual["type"]?.Value<string>(), $"{label}-type");
            AssertEqual(className, actual["className"]?.Value<string>(), $"{label}-className");
        }

        // This tests `callFunctionOn` with a function that the vscode-js-debug extension uses
        // Using this here as a non-trivial test case
        [Theory]
        [InlineData("big_array_js_test (10);", "/other.js", 10, 1, 10)]
        [InlineData("big_array_js_test (0);", "/other.js", 10, 1, 0)]
        [InlineData("invoke_static_method ('[debugger-test] DebuggerTests.CallFunctionOnTest:LocalsTest', 10);", "dotnet://debugger-test.dll/debugger-cfo-test.cs", 23, 12, 10)]
        [InlineData("invoke_static_method ('[debugger-test] DebuggerTests.CallFunctionOnTest:LocalsTest', 0);", "dotnet://debugger-test.dll/debugger-cfo-test.cs", 23, 12, 0)]
        public async Task CheckVSCodeTestFunction2(string eval_fn, string bp_loc, int line, int col, int len)
        {
            var fetch_start_idx = 2;
            var num_elems_fetch = 3;
            string vscode_fn1 = "function(e,t){const r={},n=-1===e?0:e,i=-1===t?this.length:e+t;for(let e=n;e<i&&e<this.length;++e){const t=Object.getOwnPropertyDescriptor(this,e);t&&Object.defineProperty(r,e,t)}return r}";

            await RunCallFunctionOn(eval_fn, vscode_fn1, "big", bp_loc, line, col,
                fn_args: JArray.FromObject(new[]
                {
                    new { @value = fetch_start_idx },
                    new { @value = num_elems_fetch }
                }),
                test_fn: async (result) =>
                {

                    var is_js = bp_loc.EndsWith(".js", StringComparison.Ordinal);

                    // isOwn = false, accessorPropertiesOnly = true
                    var obj_accessors = await cli.SendCommand("Runtime.getProperties", JObject.FromObject(new
                    {
                        objectId = result.Value["result"]["objectId"].Value<string>(),
                        accessorPropertiesOnly = true,
                        ownProperties = false
                    }), token);

                    if (is_js)
                       await CheckProps(obj_accessors.Value["result"], new { __proto__ = TIgnore() }, "obj_accessors");
                    else
                        AssertEqual(0, obj_accessors.Value["result"]?.Count(), "obj_accessors-count");

                    // isOwn = true, accessorPropertiesOnly = false
                    var obj_own = await cli.SendCommand("Runtime.getProperties", JObject.FromObject(new
                    {
                        objectId = result.Value["result"]["objectId"].Value<string>(),
                        accessorPropertiesOnly = false,
                        ownProperties = true
                    }), token);

                    var obj_own_val = obj_own.Value["result"];
                    var num_elems_recd = len == 0 ? 0 : num_elems_fetch;
                    AssertEqual(num_elems_recd, obj_own_val.Count(), $"obj_own-count");

                    for (int i = fetch_start_idx; i < fetch_start_idx + num_elems_recd; i++)
                        CheckNumber(obj_own_val, i.ToString(), 1000 + i);
                });
        }

        [Theory]
        [InlineData("big_array_js_test (10);", "/other.js", 10, 1, false)]
        [InlineData("big_array_js_test (10);", "/other.js", 10, 1, true)]
        [InlineData("invoke_static_method ('[debugger-test] DebuggerTests.CallFunctionOnTest:LocalsTest', 10);", "dotnet://debugger-test.dll/debugger-cfo-test.cs", 23, 12, false)]
        [InlineData("invoke_static_method ('[debugger-test] DebuggerTests.CallFunctionOnTest:LocalsTest', 10);", "dotnet://debugger-test.dll/debugger-cfo-test.cs", 23, 12, true)]
        public async Task RunOnArrayReturnEmptyArray(string eval_fn, string bp_loc, int line, int col, bool roundtrip)
        {
            var ret_len = 0;

            await RunCallFunctionOn(eval_fn,
                "function () { return []; }",
                "big", bp_loc, line, col,
                res_array_len: ret_len,
                roundtrip: roundtrip,
                test_fn: async (result) =>
               {
                   var is_js = bp_loc.EndsWith(".js", StringComparison.Ordinal);

                   // getProperties (isOwn = false, accessorPropertiesOnly = true)
                   var obj_accessors = await cli.SendCommand("Runtime.getProperties", JObject.FromObject(new
                   {
                       objectId = result.Value["result"]["objectId"].Value<string>(),
                       accessorPropertiesOnly = true,
                       ownProperties = false
                   }), token);
                   if (is_js)
                       await CheckProps(obj_accessors.Value["result"], new { __proto__ = TIgnore() }, "obj_accessors");
                   else
                       AssertEqual(0, obj_accessors.Value["result"]?.Count(), "obj_accessors-count");

                   // getProperties (isOwn = true, accessorPropertiesOnly = false)
                   var obj_own = await cli.SendCommand("Runtime.getProperties", JObject.FromObject(new
                   {
                       objectId = result.Value["result"]["objectId"].Value<string>(),
                       accessorPropertiesOnly = false,
                       ownProperties = true
                   }), token);

                   await CheckProps(obj_own.Value["result"], new
                   {
                       length = TNumber(ret_len),
                   }, $"obj_own");
               });
        }

        [Theory]
        [InlineData("big_array_js_test (10);", "/other.js", 10, 1, false)]
        [InlineData("big_array_js_test (10);", "/other.js", 10, 1, true)]
        [InlineData("invoke_static_method ('[debugger-test] DebuggerTests.CallFunctionOnTest:LocalsTest', 10);", "dotnet://debugger-test.dll/debugger-cfo-test.cs", 23, 12, false)]
        [InlineData("invoke_static_method ('[debugger-test] DebuggerTests.CallFunctionOnTest:LocalsTest', 10);", "dotnet://debugger-test.dll/debugger-cfo-test.cs", 23, 12, true)]
        public async Task RunOnArrayReturnArray(string eval_fn, string bp_loc, int line, int col, bool roundtrip)
        {
            var ret_len = 5;
            await RunCallFunctionOn(eval_fn,
                "function (m) { return Object.values (this).filter ((k, i) => i%m == 0); }",
                "big", bp_loc, line, col,
                fn_args: JArray.FromObject(new[] { new { value = 2 } }),
                res_array_len: ret_len,
                roundtrip: roundtrip,
                test_fn: async (result) =>
               {
                   var is_js = bp_loc.EndsWith(".js");

                   // getProperties (own=false)
                   var obj_accessors = await cli.SendCommand("Runtime.getProperties", JObject.FromObject(new
                   {
                       objectId = result.Value["result"]["objectId"].Value<string>(),
                       accessorPropertiesOnly = true,
                       ownProperties = false
                   }), token);

                   if (is_js)
                       await CheckProps(obj_accessors.Value["result"], new { __proto__ = TIgnore() }, "obj_accessors");
                   else
                       AssertEqual(0, obj_accessors.Value["result"]?.Count(), "obj_accessors-count");

                   // getProperties (own=true)
                   // isOwn = true, accessorPropertiesOnly = false
                   var obj_own = await cli.SendCommand("Runtime.getProperties", JObject.FromObject(new
                   {
                       objectId = result.Value["result"]["objectId"].Value<string>(),
                       accessorPropertiesOnly = false,
                       ownProperties = true
                   }), token);

                   // AssertEqual (2, obj_own.Value ["result"].Count (), $"{label}-obj_own.count");

                   var obj_own_val = obj_own.Value["result"];
                   await CheckProps(obj_own_val, new
                   {
                       length = TNumber(ret_len),
                   }, $"obj_own", num_fields: (ret_len + 1));

                   for (int i = 0; i < ret_len; i++)
                       CheckNumber(obj_own_val, i.ToString(), i * 2 + 1000);
               });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RunOnVTArray(bool roundtrip) => await RunCallFunctionOn(
            "invoke_static_method ('[debugger-test] DebuggerTests.CallFunctionOnTest:LocalsTest', 10);",
            "function (m) { return Object.values (this).filter ((k, i) => i%m == 0); }",
            "ss_arr",
            "dotnet://debugger-test.dll/debugger-cfo-test.cs", 23, 12,
            fn_args: JArray.FromObject(new[] { new { value = 2 } }),
            res_array_len: 5,
            roundtrip: roundtrip,
            test_fn: async (result) =>
           {
               var ret_len = 5;

               // getProperties (own=false)
               var obj_accessors = await cli.SendCommand("Runtime.getProperties", JObject.FromObject(new
               {
                   objectId = result.Value["result"]["objectId"].Value<string>(),
                   accessorPropertiesOnly = true,
                   ownProperties = false
               }), token);

               AssertEqual(0, obj_accessors.Value["result"]?.Count(), "obj_accessors-count");

               // getProperties (own=true)
               // isOwn = true, accessorPropertiesOnly = false
               var obj_own = await cli.SendCommand("Runtime.getProperties", JObject.FromObject(new
               {
                   objectId = result.Value["result"]["objectId"].Value<string>(),
                   accessorPropertiesOnly = false,
                   ownProperties = true
               }), token);

               var obj_own_val = obj_own.Value["result"];
               await CheckProps(obj_own_val, new
               {
                   length = TNumber(ret_len),
                   // __proto__ returned by JS
               }, "obj_own", num_fields: ret_len + 1);

               for (int i = 0; i < ret_len; i++)
               {
                   var act_i = await CheckValueType(obj_own_val, i.ToString(), "Math.SimpleStruct");

                   // Valuetypes can get sent as part of the container's getProperties, so ensure that we can access it
                   var act_i_props = await GetProperties(act_i["value"]["objectId"]?.Value<string>());
                   await CheckProps(act_i_props, new
                   {
                       dt = TDateTime(new DateTime(2020 + (i * 2), 1, 2, 3, 4, 5)),
                       gs = TValueType("Math.GenericStruct<System.DateTime>")
                   }, "obj_own ss_arr[{i}]");

                   var gs_props = await GetObjectOnLocals(act_i_props, "gs");
                   await CheckProps(gs_props, new
                   {
                       List = TObject("System.Collections.Generic.List<System.DateTime>", is_null: true),
                       StringField = TString($"ss_arr # {i * 2} # gs # StringField")
                   }, "obj_own ss_arr[{i}].gs");

               }
           });

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RunOnCFOValueTypeResult(bool roundtrip) => await RunCallFunctionOn(
            eval_fn: "invoke_static_method ('[debugger-test] DebuggerTests.CallFunctionOnTest:LocalsTest', 10);",
            fn_decl: "function () { return this; }",
            local_name: "simple_struct",
            bp_loc: "dotnet://debugger-test.dll/debugger-cfo-test.cs", 23, 12,
            roundtrip: roundtrip,
            test_fn: async (result) =>
           {

               // getProperties (own=false)
               var obj_accessors = await cli.SendCommand("Runtime.getProperties", JObject.FromObject(new
               {
                   objectId = result.Value["result"]["objectId"].Value<string>(),
                   accessorPropertiesOnly = true,
                   ownProperties = false
               }), token);
               AssertEqual(0, obj_accessors.Value["result"].Count(), "obj_accessors-count");

               // getProperties (own=true)
               // isOwn = true, accessorPropertiesOnly = false
               var obj_own = await cli.SendCommand("Runtime.getProperties", JObject.FromObject(new
               {
                   objectId = result.Value["result"]["objectId"].Value<string>(),
                   accessorPropertiesOnly = false,
                   ownProperties = true
               }), token);

               var obj_own_val = obj_own.Value["result"];
               var dt = new DateTime(2020, 1, 2, 3, 4, 5);
               await CheckProps(obj_own_val, new
               {
                   dt = TDateTime(dt),
                   gs = TValueType("Math.GenericStruct<System.DateTime>")
               }, $"obj_own-props");

               var gs_props = await GetObjectOnLocals(obj_own_val, "gs");
               await CheckProps(gs_props, new
               {
                   List = TObject("System.Collections.Generic.List<System.DateTime>", is_null: true),
                   StringField = TString($"simple_struct # gs # StringField")
               }, "simple_struct.gs-props");
           });

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RunOnJSObject(bool roundtrip) => await RunCallFunctionOn(
            "object_js_test ();",
            "function () { return this; }",
            "obj", "/other.js", 19, 1,
            fn_args: JArray.FromObject(new[] { new { value = 2 } }),
            roundtrip: roundtrip,
            test_fn: async (result) =>
           {

               // getProperties (own=false)
               var obj_accessors = await cli.SendCommand("Runtime.getProperties", JObject.FromObject(new
               {
                   objectId = result.Value["result"]["objectId"].Value<string>(),
                   accessorPropertiesOnly = true,
                   ownProperties = false
               }), token);

               await CheckProps(obj_accessors.Value["result"], new { __proto__ = TIgnore() }, "obj_accessors");

               // getProperties (own=true)
               // isOwn = true, accessorPropertiesOnly = false
               var obj_own = await cli.SendCommand("Runtime.getProperties", JObject.FromObject(new
               {
                   objectId = result.Value["result"]["objectId"].Value<string>(),
                   accessorPropertiesOnly = false,
                   ownProperties = true
               }), token);

               var obj_own_val = obj_own.Value["result"];
               await CheckProps(obj_own_val, new
               {
                   a_obj = TObject("Object"),
                   b_arr = TArray("Array", "Array(2)")
               }, "obj_own");
           });

        [Theory]
        [InlineData("big_array_js_test (10);", "/other.js", 10, 1, false)]
        [InlineData("big_array_js_test (10);", "/other.js", 10, 1, true)]
        [InlineData("invoke_static_method ('[debugger-test] DebuggerTests.CallFunctionOnTest:LocalsTest', 10);", "dotnet://debugger-test.dll/debugger-cfo-test.cs", 23, 12, false)]
        [InlineData("invoke_static_method ('[debugger-test] DebuggerTests.CallFunctionOnTest:LocalsTest', 10);", "dotnet://debugger-test.dll/debugger-cfo-test.cs", 23, 12, true)]
        public async Task RunOnArrayReturnObjectArrayByValue(string eval_fn, string bp_loc, int line, int col, bool roundtrip)
        {
            var ret_len = 5;
            await RunCallFunctionOn(eval_fn,
                "function () { return Object.values (this).filter ((k, i) => i%2 == 0); }",
                "big", bp_loc, line, col, returnByValue: true, roundtrip: roundtrip,
                test_fn: async (result) =>
               {
                   // Check cfo result
                   AssertEqual(JTokenType.Object, result.Value["result"].Type, "cfo-result-jsontype");
                   AssertEqual("object", result.Value["result"]["type"]?.Value<string>(), "cfo-res-type");

                   AssertEqual(JTokenType.Array, result.Value["result"]["value"].Type, "cfo-res-value-jsontype");
                   var actual = result.Value["result"]?["value"].Values<JToken>().ToArray();
                   AssertEqual(ret_len, actual.Length, "cfo-res-value-length");

                   for (int i = 0; i < ret_len; i++)
                   {
                       var exp_num = i * 2 + 1000;
                       if (bp_loc.EndsWith(".js", StringComparison.Ordinal))
                           AssertEqual(exp_num, actual[i].Value<int>(), $"[{i}]");
                       else
                       {
                           AssertEqual("number", actual[i]?["type"]?.Value<string>(), $"[{i}]-type");
                           AssertEqual(exp_num.ToString(), actual[i]?["description"]?.Value<string>(), $"[{i}]-description");
                           AssertEqual(exp_num, actual[i]?["value"]?.Value<int>(), $"[{i}]-value");
                       }
                   }
                   await Task.CompletedTask;
               });
        }

        [Theory]
        [InlineData("big_array_js_test (10);", "/other.js", 10, 1, false)]
        [InlineData("big_array_js_test (10);", "/other.js", 10, 1, true)]
        [InlineData("invoke_static_method ('[debugger-test] DebuggerTests.CallFunctionOnTest:LocalsTest', 10);", "dotnet://debugger-test.dll/debugger-cfo-test.cs", 23, 12, false)]
        [InlineData("invoke_static_method ('[debugger-test] DebuggerTests.CallFunctionOnTest:LocalsTest', 10);", "dotnet://debugger-test.dll/debugger-cfo-test.cs", 23, 12, true)]
        public async Task RunOnArrayReturnArrayByValue(string eval_fn, string bp_loc, int line, int col, bool roundtrip) => await RunCallFunctionOn(eval_fn,
            "function () { return Object.getOwnPropertyNames (this); }",
            "big", bp_loc, line, col, returnByValue: true,
            roundtrip: roundtrip,
            test_fn: async (result) =>
           {
               // Check cfo result
               AssertEqual("object", result.Value["result"]["type"]?.Value<string>(), "cfo-res-type");

               var exp = new JArray();
               for (int i = 0; i < 10; i++)
                   exp.Add(i.ToString());
               exp.Add("length");

               var actual = result.Value["result"]?["value"];
               if (!JObject.DeepEquals(exp, actual))
               {
                   Assert.True(false, $"Results don't match.\nExpected: {exp}\nActual:  {actual}");
               }
               await Task.CompletedTask;
           });

        [Theory]
        [InlineData("big_array_js_test (10);", "/other.js", 10, 1, false)]
        [InlineData("big_array_js_test (10);", "/other.js", 10, 1, true)]
        [InlineData("invoke_static_method ('[debugger-test] DebuggerTests.CallFunctionOnTest:LocalsTest', 10);", "dotnet://debugger-test.dll/debugger-cfo-test.cs", 23, 12, false)]
        [InlineData("invoke_static_method ('[debugger-test] DebuggerTests.CallFunctionOnTest:LocalsTest', 10);", "dotnet://debugger-test.dll/debugger-cfo-test.cs", 23, 12, true)]
        public async Task RunOnArrayReturnPrimitive(string eval_fn, string bp_loc, int line, int col, bool return_by_val)
        {
            await SetBreakpoint(bp_loc, line, col);

            // callFunctionOn
            var eval_expr = $"window.setTimeout(function() {{ {eval_fn} }}, 1);";
            var result = await cli.SendCommand("Runtime.evaluate", JObject.FromObject(new { expression = eval_expr }), token);
            var pause_location = await insp.WaitFor(Inspector.PAUSE);

            // Um for js we get "scriptId": "6"
            // CheckLocation (bp_loc, line, col, scripts, pause_location ["callFrames"][0]["location"]);

            // Check the object at the bp
            var frame_locals = await GetProperties(pause_location["callFrames"][0]["scopeChain"][0]["object"]["objectId"].Value<string>());
            var obj = GetAndAssertObjectWithName(frame_locals, "big");
            var obj_id = obj["value"]["objectId"].Value<string>();

            var cfo_args = JObject.FromObject(new
            {
                functionDeclaration = "function () { return 5; }",
                objectId = obj_id
            });

            // value of @returnByValue doesn't matter, as the returned value
            // is a primitive
            if (return_by_val)
                cfo_args["returnByValue"] = return_by_val;

            // callFunctionOn
            result = await cli.SendCommand("Runtime.callFunctionOn", cfo_args, token);
            await CheckValue(result.Value["result"], TNumber(5), "cfo-res");

            cfo_args = JObject.FromObject(new
            {
                functionDeclaration = "function () { return 'test value'; }",
                objectId = obj_id
            });

            // value of @returnByValue doesn't matter, as the returned value
            // is a primitive
            if (return_by_val)
                cfo_args["returnByValue"] = return_by_val;

            // callFunctionOn
            result = await cli.SendCommand("Runtime.callFunctionOn", cfo_args, token);
            await CheckValue(result.Value["result"], JObject.FromObject(new { type = "string", value = "test value" }), "cfo-res");

            cfo_args = JObject.FromObject(new
            {
                functionDeclaration = "function () { return null; }",
                objectId = obj_id
            });

            // value of @returnByValue doesn't matter, as the returned value
            // is a primitive
            if (return_by_val)
                cfo_args["returnByValue"] = return_by_val;

            // callFunctionOn
            result = await cli.SendCommand("Runtime.callFunctionOn", cfo_args, token);
            await CheckValue(result.Value["result"], JObject.Parse("{ type: 'object', subtype: 'null', value: null }"), "cfo-res");
        }

        public static TheoryData<string, string, int, int, bool?> SilentErrorsTestData(bool? silent) => new TheoryData<string, string, int, int, bool?>
        { { "invoke_static_method ('[debugger-test] DebuggerTests.CallFunctionOnTest:LocalsTest', 10);", "dotnet://debugger-test.dll/debugger-cfo-test.cs", 23, 12, silent },
            { "big_array_js_test (10);", "/other.js", 10, 1, silent }
        };

        [Theory]
        [MemberData(nameof(SilentErrorsTestData), null)]
        [MemberData(nameof(SilentErrorsTestData), false)]
        [MemberData(nameof(SilentErrorsTestData), true)]
        public async Task CFOWithSilentReturnsErrors(string eval_fn, string bp_loc, int line, int col, bool? silent)
        {
            await SetBreakpoint(bp_loc, line, col);

            // callFunctionOn
            var eval_expr = "window.setTimeout(function() { " + eval_fn + " }, 1);";
            var result = await cli.SendCommand("Runtime.evaluate", JObject.FromObject(new { expression = eval_expr }), token);
            var pause_location = await insp.WaitFor(Inspector.PAUSE);

            var frame_locals = await GetProperties(pause_location["callFrames"][0]["scopeChain"][0]["object"]["objectId"].Value<string>());
            var obj = GetAndAssertObjectWithName(frame_locals, "big");
            var big_obj_id = obj["value"]["objectId"].Value<string>();
            var error_msg = "#This is an error message#";

            // Check the object at the bp
            var cfo_args = JObject.FromObject(new
            {
                functionDeclaration = $"function () {{ throw Error ('{error_msg}'); }}",
                objectId = big_obj_id
            });

            if (silent.HasValue)
                cfo_args["silent"] = silent;

            // callFunctionOn, Silent does not change the result, except that the error
            // doesn't get reported, and the execution is NOT paused even with setPauseOnException=true
            result = await cli.SendCommand("Runtime.callFunctionOn", cfo_args, token);
            Assert.False(result.IsOk, "result.IsOk");
            Assert.True(result.IsErr, "result.IsErr");

            var hasErrorMessage = result.Error["exceptionDetails"]?["exception"]?["description"]?.Value<string>()?.Contains(error_msg);
            Assert.True((hasErrorMessage ?? false), "Exception message not found");
        }

        public static TheoryData<string, string, int, int, string, Func<string[], object>, string, bool> GettersTestData(string local_name, bool use_cfo) => new TheoryData<string, string, int, int, string, Func<string[], object>, string, bool>
        {
            // Chrome sends this one
            {
                "invoke_static_method ('[debugger-test] DebuggerTests.CallFunctionOnTest:PropertyGettersTest');",
                "PropertyGettersTest",
                30,
                12,
                "function invokeGetter(arrayStr){ let result=this; const properties=JSON.parse(arrayStr); for(let i=0,n=properties.length;i<n;++i){ result=result[properties[i]]; } return result; }",
                (arg_strs) => JArray.FromObject(arg_strs).ToString(),
                local_name,
                use_cfo
            },
            {
                "invoke_static_method_async ('[debugger-test] DebuggerTests.CallFunctionOnTest:PropertyGettersTestAsync');",
                "MoveNext",
                38,
                12,
                "function invokeGetter(arrayStr){ let result=this; const properties=JSON.parse(arrayStr); for(let i=0,n=properties.length;i<n;++i){ result=result[properties[i]]; } return result; }",
                (arg_strs) => JArray.FromObject(arg_strs).ToString(),
                local_name,
                use_cfo
            },

            // VSCode sends this one
            {
                "invoke_static_method ('[debugger-test] DebuggerTests.CallFunctionOnTest:PropertyGettersTest');",
                "PropertyGettersTest",
                30,
                12,
                "function(e){return this[e]}",
                (args_str) => args_str?.Length > 0 ? args_str[0] : String.Empty,
                local_name,
                use_cfo
            },
            {
                "invoke_static_method_async ('[debugger-test] DebuggerTests.CallFunctionOnTest:PropertyGettersTestAsync');",
                "MoveNext",
                38,
                12,
                "function(e){return this[e]}",
                (args_str) => args_str?.Length > 0 ? args_str[0] : String.Empty,
                local_name,
                use_cfo
            }
        };

        [Theory]
        [MemberData(nameof(GettersTestData), "ptd", false)]
        [MemberData(nameof(GettersTestData), "ptd", true)]
        [MemberData(nameof(GettersTestData), "swp", false)]
        [MemberData(nameof(GettersTestData), "swp", true)]
        public async Task PropertyGettersTest(string eval_fn, string method_name, int line, int col, string cfo_fn, Func<string[], object> get_args_fn, string local_name, bool use_cfo) => await CheckInspectLocalsAtBreakpointSite(
            "dotnet://debugger-test.dll/debugger-cfo-test.cs", line, col,
            method_name,
            $"window.setTimeout(function() {{ {eval_fn} }}, 1);",
            use_cfo: use_cfo,
            wait_for_event_fn: async (pause_location) =>
           {
               var frame_locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());

               await CheckProps(frame_locals, new
               {
                   ptd = TObject("DebuggerTests.ClassWithProperties"),
                   swp = TObject("DebuggerTests.StructWithProperties")
               }, "locals#0");

               var obj = GetAndAssertObjectWithName(frame_locals, local_name);

               var dt = new DateTime(4, 5, 6, 7, 8, 9);
               var obj_props = await GetProperties(obj?["value"]?["objectId"]?.Value<string>());
               await CheckProps(obj_props, new
               {
                   V = TNumber(0xDEADBEEF),
                   Int = TGetter("Int"),
                   String = TGetter("String"),
                   DT = TGetter("DT"),
                   IntArray = TGetter("IntArray"),
                   DTArray = TGetter("DTArray"),
                   StringField = TString(null),

                   // Auto properties show w/o getters, because they have
                   // a backing field
                   DTAutoProperty = TDateTime(dt)
               }, local_name);

               // Invoke getters, and check values

               dt = new DateTime(3, 4, 5, 6, 7, 8);
               var res = await InvokeGetter(obj, get_args_fn(new[] { "Int" }), cfo_fn);
               await CheckValue(res.Value["result"], JObject.FromObject(new { type = "number", value = (0xDEADBEEF + (uint)dt.Month) }), $"{local_name}.Int");

               res = await InvokeGetter(obj, get_args_fn(new[] { "String" }), cfo_fn);
               await CheckValue(res.Value["result"], JObject.FromObject(new { type = "string", value = $"String property, V: 0xDEADBEEF" }), $"{local_name}.String");

               res = await InvokeGetter(obj, get_args_fn(new[] { "DT" }), cfo_fn);
               await CheckValue(res.Value["result"], TDateTime(dt), $"{local_name}.DT");

               // Check arrays through getters

               res = await InvokeGetter(obj, get_args_fn(new[] { "IntArray" }), cfo_fn);
               await CheckValue(res.Value["result"], TArray("int[]", "int[2]"), $"{local_name}.IntArray");
               {
                   var arr_elems = await GetProperties(res.Value["result"]?["objectId"]?.Value<string>());
                   var exp_elems = new[]
                   {
                        TNumber(10),
                        TNumber(20)
                   };

                   await CheckProps(arr_elems, exp_elems, $"{local_name}.IntArray");
               }

               res = await InvokeGetter(obj, get_args_fn(new[] { "DTArray" }), cfo_fn);
               await CheckValue(res.Value["result"], TArray("System.DateTime[]", "System.DateTime[2]"), $"{local_name}.DTArray");
               {
                   var dt0 = new DateTime(6, 7, 8, 9, 10, 11);
                   var dt1 = new DateTime(1, 2, 3, 4, 5, 6);

                   var arr_elems = await GetProperties(res.Value["result"]?["objectId"]?.Value<string>());
                   var exp_elems = new[]
                   {
                        TDateTime(dt0),
                        TDateTime(dt1)
                   };

                   await CheckProps(arr_elems, exp_elems, $"{local_name}.DTArray");

                   res = await InvokeGetter(arr_elems[0], "Date");
                   await CheckDateTimeValue(res.Value["result"], dt0.Date);
               }
           });

        [Fact]
        public async Task InvokeInheritedAndPrivateGetters() => await CheckInspectLocalsAtBreakpointSite(
            $"DebuggerTests.GetPropertiesTests.DerivedClass", "InstanceMethod", 1, "InstanceMethod",
            $"window.setTimeout(function() {{ invoke_static_method_async ('[debugger-test] DebuggerTests.GetPropertiesTests.DerivedClass:run'); }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var frame_id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                var frame_locals = await GetProperties(frame_id);
                var this_obj = GetAndAssertObjectWithName(frame_locals, "this");

                var args = new[]
                {
                    // private
                    ("_DTProp", TDateTime(new DateTime(2200, 5, 6, 7, 8, 9))),

                    // overridden
                    ("DateTimeForOverride", TDateTime(new DateTime(2190, 9, 7, 5, 3, 2))),
                    ("FirstName", TString("DerivedClass#FirstName")),
                    ("StringPropertyForOverrideWithAutoProperty", TString("DerivedClass#StringPropertyForOverrideWithAutoProperty")),

                    // inherited
                    ("_base_dateTime", TDateTime(new DateTime(2134, 5, 7, 1, 9, 2)))
                };

                foreach (var (name, expected) in args)
                {
                    var res = await InvokeGetter(this_obj, name);
                    await CheckValue(res.Value["result"], expected, name);
                }
            });


        [Theory]
        [InlineData("invoke_static_method_async ('[debugger-test] DebuggerTests.CallFunctionOnTest:PropertyGettersTestAsync');", "dotnet://debugger-test.dll/debugger-cfo-test.cs", 38, 12, true)]
        [InlineData("invoke_static_method_async ('[debugger-test] DebuggerTests.CallFunctionOnTest:PropertyGettersTestAsync');", "dotnet://debugger-test.dll/debugger-cfo-test.cs", 38, 12, false)]
        [InlineData("invoke_static_method ('[debugger-test] DebuggerTests.CallFunctionOnTest:PropertyGettersTest');", "dotnet://debugger-test.dll/debugger-cfo-test.cs", 30, 12, true)]
        [InlineData("invoke_static_method ('[debugger-test] DebuggerTests.CallFunctionOnTest:PropertyGettersTest');", "dotnet://debugger-test.dll/debugger-cfo-test.cs", 30, 12, false)]
        [InlineData("invoke_getters_js_test ();", "/other.js", 32, 1, false)]
        [InlineData("invoke_getters_js_test ();", "/other.js", 32, 1, true)]
        public async Task CheckAccessorsOnObjectsWithCFO(string eval_fn, string bp_loc, int line, int col, bool roundtrip)
        {
            await RunCallFunctionOn(
                eval_fn, "function() { return this; }", "ptd",
                bp_loc, line, col,
                roundtrip: roundtrip,
                test_fn: async (result) =>
               {

                   var is_js = bp_loc.EndsWith(".js");

                   // Check with `accessorPropertiesOnly=true`

                   var id = result.Value?["result"]?["objectId"]?.Value<string>();
                   var get_prop_req = JObject.FromObject(new
                   {
                       objectId = id,
                       accessorPropertiesOnly = true
                   });

                   var res = await GetPropertiesAndCheckAccessors(get_prop_req, 5);
                   Assert.False(res.Value["result"].Any(jt => jt["name"]?.Value<string>() == "StringField"), "StringField shouldn't be returned for `accessorPropertiesOnly`");

                   // Check with `accessorPropertiesOnly` unset, == false
                   get_prop_req = JObject.FromObject(new
                   {
                       objectId = id,
                   });

                   res = await GetPropertiesAndCheckAccessors(get_prop_req, 7);
                   Assert.True(res.Value["result"].Any(jt => jt["name"]?.Value<string>() == "StringField"), "StringField should be returned for `accessorPropertiesOnly=false`");
               });

            async Task<Result> GetPropertiesAndCheckAccessors(JObject get_prop_req, int num_fields)
            {
                var res = await cli.SendCommand("Runtime.getProperties", get_prop_req, token);
                if (!res.IsOk)
                    Assert.True(false, $"Runtime.getProperties failed for {get_prop_req.ToString()}, with Result: {res}");

                var accessors = new string[] { "Int", "String", "DT", "IntArray", "DTArray" };
                foreach (var name in accessors)
                {
                    var prop = GetAndAssertObjectWithName(res.Value["result"], name);
                    Assert.True(prop["value"] == null, $"{name} shouldn't have a `value`");

                    await CheckValue(prop, TGetter(name), $"{name}");
                }

                return res;
            }
        }

        public static TheoryData<string, string, int, int, bool> NegativeTestsData(bool use_cfo = false) => new TheoryData<string, string, int, int, bool>
        { { "invoke_static_method ('[debugger-test] DebuggerTests.CallFunctionOnTest:MethodForNegativeTests', null);", "dotnet://debugger-test.dll/debugger-cfo-test.cs", 45, 12, use_cfo },
            { "negative_cfo_test ();", "/other.js", 64, 1, use_cfo }
        };

        [Theory]
        [MemberData(nameof(NegativeTestsData), false)]
        public async Task RunOnInvalidCfoId(string eval_fn, string bp_loc, int line, int col, bool use_cfo) => await RunCallFunctionOn(
            eval_fn, "function() { return this; }", "ptd",
            bp_loc, line, col,
            test_fn: async (cfo_result) =>
           {
               var ptd_id = cfo_result.Value?["result"]?["objectId"]?.Value<string>();

               var cfo_args = JObject.FromObject(new
               {
                   functionDeclaration = "function () { return 0; }",
                   objectId = ptd_id + "_invalid"
               });

               var res = await cli.SendCommand("Runtime.callFunctionOn", cfo_args, token);
               Assert.True(res.IsErr);
           });

        [Theory]
        [MemberData(nameof(NegativeTestsData), false)]
        public async Task RunOnInvalidThirdSegmentOfObjectId(string eval_fn, string bp_loc, int line, int col, bool use_cfo)
        {
            UseCallFunctionOnBeforeGetProperties = use_cfo;
            await SetBreakpoint(bp_loc, line, col);

            // callFunctionOn
            var eval_expr = $"window.setTimeout(function() {{ {eval_fn} }}, 1);";
            var result = await cli.SendCommand("Runtime.evaluate", JObject.FromObject(new { expression = eval_expr }), token);
            var pause_location = await insp.WaitFor(Inspector.PAUSE);

            var frame_locals = await GetProperties(pause_location["callFrames"][0]["scopeChain"][0]["object"]["objectId"].Value<string>());
            var ptd = GetAndAssertObjectWithName(frame_locals, "ptd");
            var ptd_id = ptd["value"]["objectId"].Value<string>();

            var cfo_args = JObject.FromObject(new
            {
                functionDeclaration = "function () { return 0; }",
                objectId = ptd_id + "_invalid"
            });

            var res = await cli.SendCommand("Runtime.callFunctionOn", cfo_args, token);
            Assert.True(res.IsErr);
        }

        [Theory]
        [MemberData(nameof(NegativeTestsData), false)]
        [MemberData(nameof(NegativeTestsData), true)]
        public async Task InvalidPropertyGetters(string eval_fn, string bp_loc, int line, int col, bool use_cfo)
        {
            await SetBreakpoint(bp_loc, line, col);
            UseCallFunctionOnBeforeGetProperties = use_cfo;

            // callFunctionOn
            var eval_expr = $"window.setTimeout(function() {{ {eval_fn} }}, 1);";
            await SendCommand("Runtime.evaluate", JObject.FromObject(new { expression = eval_expr }));
            var pause_location = await insp.WaitFor(Inspector.PAUSE);

            var frame_locals = await GetProperties(pause_location["callFrames"][0]["scopeChain"][0]["object"]["objectId"].Value<string>());
            var ptd = GetAndAssertObjectWithName(frame_locals, "ptd");
            var ptd_id = ptd["value"]["objectId"].Value<string>();

            var invalid_args = new object[] { "NonExistant", String.Empty, null, 12310 };
            foreach (var invalid_arg in invalid_args)
            {
                var getter_res = await InvokeGetter(JObject.FromObject(new { value = new { objectId = ptd_id } }), invalid_arg);
                AssertEqual("undefined", getter_res.Value["result"]?["type"]?.ToString(), $"Expected to get undefined result for non-existant accessor - {invalid_arg}");
            }
        }

        [Theory]
        [MemberData(nameof(NegativeTestsData), false)]
        public async Task ReturnNullFromCFO(string eval_fn, string bp_loc, int line, int col, bool use_cfo) => await RunCallFunctionOn(
            eval_fn, "function() { return this; }", "ptd",
            bp_loc, line, col,
            test_fn: async (result) =>
           {
               var is_js = bp_loc.EndsWith(".js");
               var ptd = JObject.FromObject(new { value = new { objectId = result.Value?["result"]?["objectId"]?.Value<string>() } });

               var null_value_json = JObject.Parse("{ 'type': 'object', 'subtype': 'null', 'value': null }");
               foreach (var returnByValue in new bool?[] { null, false, true })
               {
                   var res = await InvokeGetter(ptd, "StringField", returnByValue: returnByValue);
                   if (is_js)
                   {
                       // In js case, it doesn't know the className, so the result looks slightly different
                       Assert.True(
                          JObject.DeepEquals(res.Value["result"], null_value_json),
                          $"[StringField#returnByValue = {returnByValue}] Json didn't match. Actual: {res.Value["result"]} vs {null_value_json}");
                   }
                   else
                   {
                       await CheckValue(res.Value["result"], TString(null), "StringField");
                   }
               }
           });

        /*
         * 1. runs `Runtime.callFunctionOn` on the objectId,
         * if @roundtrip == false, then
         *     -> calls @test_fn for that result (new objectId)
         * else
         *     -> runs it again on the *result's* objectId.
         *        -> calls @test_fn on the *new* result objectId
         *
         * Returns: result of `Runtime.callFunctionOn`
         */
        async Task RunCallFunctionOn(string eval_fn, string fn_decl, string local_name, string bp_loc, int line, int col, int res_array_len = -1,
            Func<Result, Task> test_fn = null, bool returnByValue = false, JArray fn_args = null, bool roundtrip = false)
        {
            await SetBreakpoint(bp_loc, line, col);

            // callFunctionOn
            var eval_expr = $"window.setTimeout(function() {{ {eval_fn} }}, 1);";
            var result = await cli.SendCommand("Runtime.evaluate", JObject.FromObject(new { expression = eval_expr }), token);
            var pause_location = await insp.WaitFor(Inspector.PAUSE);

            // Um for js we get "scriptId": "6"
            // CheckLocation (bp_loc, line, col, scripts, pause_location ["callFrames"][0]["location"]);

            // Check the object at the bp
            var frame_locals = await GetProperties(pause_location["callFrames"][0]["scopeChain"][0]["object"]["objectId"].Value<string>());
            var obj = GetAndAssertObjectWithName(frame_locals, local_name);
            var obj_id = obj["value"]["objectId"].Value<string>();

            var cfo_args = JObject.FromObject(new
            {
                functionDeclaration = fn_decl,
                objectId = obj_id
            });

            if (fn_args != null)
                cfo_args["arguments"] = fn_args;

            if (returnByValue)
                cfo_args["returnByValue"] = returnByValue;

            // callFunctionOn
            result = await cli.SendCommand("Runtime.callFunctionOn", cfo_args, token);
            await CheckCFOResult(result);

            // If it wasn't `returnByValue`, then try to run a new function
            // on that *returned* object
            // This second function, just returns the object as-is, so the same
            // test_fn is re-usable.
            if (!returnByValue && roundtrip)
            {
                cfo_args = JObject.FromObject(new
                {
                    functionDeclaration = "function () { return this; }",
                    objectId = result.Value["result"]["objectId"]?.Value<string>()
                });

                if (fn_args != null)
                    cfo_args["arguments"] = fn_args;

                result = await cli.SendCommand("Runtime.callFunctionOn", cfo_args, token);

                await CheckCFOResult(result);
            }

            if (test_fn != null)
                await test_fn(result);

            return;

            async Task CheckCFOResult(Result result)
            {
                if (returnByValue)
                    return;

                if (res_array_len < 0)
                    await CheckValue(result.Value["result"], TObject("Object"), $"cfo-res");
                else
                    await CheckValue(result.Value["result"], TArray("Array", $"Array({res_array_len})"), $"cfo-res");
            }
        }
    }

}
