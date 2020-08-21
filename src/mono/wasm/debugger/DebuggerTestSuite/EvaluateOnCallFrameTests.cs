// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WebAssembly.Diagnostics;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DebuggerTests
{
    // TODO: static async, static method args
    public class EvaluateOnCallFrameTests : DebuggerTestBase
    {
        public static IEnumerable<object[]> InstanceMethodsTestData(string type_name)
        {
            yield return new object[] { type_name, "InstanceMethod", "InstanceMethod", false };
            yield return new object[] { type_name, "GenericInstanceMethod", "GenericInstanceMethod<int>", false };
            yield return new object[] { type_name, "InstanceMethodAsync", "MoveNext", true };
            yield return new object[] { type_name, "GenericInstanceMethodAsync", "MoveNext", true };

            // TODO: { "DebuggerTests.EvaluateTestsGeneric`1", "Instance", 9, "EvaluateTestsGenericStructInstanceMethod", prefix }
        }

        public static IEnumerable<object[]> InstanceMethodForTypeMembersTestData(string type_name)
        {
            foreach (var data in InstanceMethodsTestData(type_name))
            {
                yield return new object[] { "", 0 }.Concat(data).ToArray();
                yield return new object[] { "this.", 0 }.Concat(data).ToArray();
                yield return new object[] { "NewInstance.", 3 }.Concat(data).ToArray();
                yield return new object[] { "this.NewInstance.", 3 }.Concat(data).ToArray();
            }
        }

        [Theory]
        [MemberData(nameof(InstanceMethodForTypeMembersTestData), parameters: "DebuggerTests.EvaluateTestsStructWithProperties")]
        [MemberData(nameof(InstanceMethodForTypeMembersTestData), parameters: "DebuggerTests.EvaluateTestsClassWithProperties")]
        public async Task EvaluateTypeInstanceMembers(string prefix, int bias, string type, string method, string bp_function_name, bool is_async)
        => await CheckInspectLocalsAtBreakpointSite(
            type, method, /*line_offset*/1, bp_function_name,
            $"window.setTimeout(function() {{ invoke_static_method_async('[debugger-test] {type}:run');}}, 1);",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
               var dateTime = new DateTime(2010, 9, 8, 7, 6, 5 + bias);
               var DTProp = dateTime.AddMinutes(10);

                foreach (var pad in new[] { String.Empty, "  " })
                {
                    var padded_prefix = pad + prefix;
                    await EvaluateOnCallFrameAndCheck(id,
                        ($"{padded_prefix}a", TNumber(4)),

                        // fields
                        ($"{padded_prefix}dateTime.TimeOfDay", TValueType("System.TimeSpan", dateTime.TimeOfDay.ToString())),
                        ($"{padded_prefix}dateTime", TDateTime(dateTime)),
                        ($"{padded_prefix}dateTime.TimeOfDay.Minutes", TNumber(dateTime.TimeOfDay.Minutes)),

                        // properties
                        ($"{padded_prefix}DTProp.TimeOfDay.Minutes", TNumber(DTProp.TimeOfDay.Minutes)),
                        ($"{padded_prefix}DTProp", TDateTime(DTProp)),
                        ($"{padded_prefix}DTProp.TimeOfDay", TValueType("System.TimeSpan", DTProp.TimeOfDay.ToString())),

                        ($"{padded_prefix}IntProp", TNumber(9)),
                        ($"{padded_prefix}NullIfAIsNotZero", TObject("DebuggerTests.EvaluateTestsClassWithProperties", is_null: true))
                    );
                }
           });

        [Theory]
        [MemberData(nameof(InstanceMethodsTestData), parameters: "DebuggerTests.EvaluateTestsStructWithProperties")]
        [MemberData(nameof(InstanceMethodsTestData), parameters: "DebuggerTests.EvaluateTestsClassWithProperties")]
        public async Task EvaluateInstanceMethodArguments(string type, string method, string bp_function_name, bool is_async)
        => await CheckInspectLocalsAtBreakpointSite(
            type, method, /*line_offset*/1, bp_function_name,
            $"window.setTimeout(function() {{ invoke_static_method_async('[debugger-test] {type}:run');}}, 1);",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
               var DTProp = new DateTime(2010, 9, 8, 7, 6, 5).AddMinutes(10);
               await EvaluateOnCallFrameAndCheck(id,
                   ("g", TNumber(400)),
                   ("h", TNumber(123)),
                   ("valString", TString("just a test")),
                   ("me", TObject(type)),

                   // property on method arg
                   ("me.DTProp", TDateTime(DTProp)),
                   ("me.DTProp.TimeOfDay.Minutes", TNumber(DTProp.TimeOfDay.Minutes)),
                   ("me.DTProp.Second + (me.IntProp - 5)", TNumber(DTProp.Second + 4)));
           });

        [Theory]
        [MemberData(nameof(InstanceMethodsTestData), parameters: "DebuggerTests.EvaluateTestsStructWithProperties")]
        [MemberData(nameof(InstanceMethodsTestData), parameters: "DebuggerTests.EvaluateTestsClassWithProperties")]
        public async Task EvaluateMethodLocals(string type, string method, string bp_function_name, bool is_async)
        => await CheckInspectLocalsAtBreakpointSite(
            type, method, /*line_offset*/5, bp_function_name,
            $"window.setTimeout(function() {{ invoke_static_method_async('[debugger-test] {type}:run');}}, 1);",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

               var dt = new DateTime(2025, 3, 5, 7, 9, 11);
               await EvaluateOnCallFrameAndCheck(id,
                   ("d", TNumber(401)),
                   (" d", TNumber(401)),
                   ("e", TNumber(402)),
                   ("f", TNumber(403)),

                   // property on a local
                   ("local_dt", TDateTime(dt)),
                   ("  local_dt", TDateTime(dt)),
                   ("local_dt.Date", TDateTime(dt.Date)),
                   ("  local_dt.Date", TDateTime(dt.Date)));
           });

        [Fact]
        public async Task EvaluateStaticLocalsWithDeepMemberAccess() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateTestsClass", "EvaluateLocals", 9, "EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateTestsClass:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

               var dt = new DateTime(2020, 1, 2, 3, 4, 5);
               await EvaluateOnCallFrameAndCheck(id,
                   ("f_s.c", TNumber(4)),
                   ("f_s", TValueType("DebuggerTests.EvaluateTestsStructWithProperties")),

                   ("f_s.dateTime", TDateTime(dt)),
                   ("f_s.dateTime.Date", TDateTime(dt.Date)));
           });

        [Fact]
        public async Task EvaluateLocalsAsync() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.Point", "AsyncInstanceMethod", 1, "MoveNext",
            "window.setTimeout(function() { invoke_static_method_async ('[debugger-test] DebuggerTests.ArrayTestsClass:EntryPointForStructMethod', true); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

                // sc_arg
                {
                   var (sc_arg, _) = await EvaluateOnCallFrame(id, "sc_arg");
                   await CheckValue(sc_arg, TObject("DebuggerTests.SimpleClass"), nameof(sc_arg));

                    // Check that we did get the correct object
                    var sc_arg_props = await GetProperties(sc_arg["objectId"]?.Value<string>());
                   await CheckProps(sc_arg_props, new
                   {
                       X = TNumber(10),
                       Y = TNumber(45),
                       Id = TString("sc#Id"),
                       Color = TEnum("DebuggerTests.RGB", "Blue"),
                       PointWithCustomGetter = TGetter("PointWithCustomGetter")
                   }, "sc_arg_props#1");

                   await EvaluateOnCallFrameAndCheck(id,
                       ("(sc_arg.PointWithCustomGetter.X)", TNumber(100)),
                       ("sc_arg.Id + \"_foo\"", TString($"sc#Id_foo")),
                       ("sc_arg.Id + (sc_arg.X==10 ? \"_is_ten\" : \"_not_ten\")", TString($"sc#Id_is_ten")));
               }

                // local_gs
                {
                   var (local_gs, _) = await EvaluateOnCallFrame(id, "local_gs");
                   await CheckValue(local_gs, TValueType("DebuggerTests.SimpleGenericStruct<int>"), nameof(local_gs));

                   var local_gs_props = await GetProperties(local_gs["objectId"]?.Value<string>());
                   await CheckProps(local_gs_props, new
                   {
                       Id = TObject("string", is_null: true),
                       Color = TEnum("DebuggerTests.RGB", "Red"),
                       Value = TNumber(0)
                   }, "local_gs_props#1");
                   await EvaluateOnCallFrameAndCheck(id, ("(local_gs.Id)", TString(null)));
               }
           });

        [Theory]
        [MemberData(nameof(InstanceMethodForTypeMembersTestData), parameters: "DebuggerTests.EvaluateTestsStructWithProperties")]
        [MemberData(nameof(InstanceMethodForTypeMembersTestData), parameters: "DebuggerTests.EvaluateTestsClassWithProperties")]
        public async Task EvaluateExpressionsWithDeepMemberAccesses(string prefix, int bias, string type, string method, string bp_function_name, bool _)
        => await CheckInspectLocalsAtBreakpointSite(
            type, method, /*line_offset*/4, bp_function_name,
            $"window.setTimeout(function() {{ invoke_static_method_async('[debugger-test] {type}:run');}}, 1);",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
               var dateTime = new DateTime(2010, 9, 8, 7, 6, 5 + bias);
               var DTProp = dateTime.AddMinutes(10);

               await EvaluateOnCallFrameAndCheck(id,
                   ($"{prefix}a + 5", TNumber(9)),
                   ($"10 + {prefix}IntProp", TNumber(19)),
                   ($" {prefix}IntProp  +  {prefix}DTProp.Second", TNumber(9 + DTProp.Second)),
                   ($" {prefix}IntProp + ({prefix}DTProp.Second+{prefix}dateTime.Year)", TNumber(9 + DTProp.Second + dateTime.Year)),
                   ($" {prefix}DTProp.Second > 0 ? \"_foo_\": \"_zero_\"", TString("_foo_")),

                   // local_dt is not live yet
                   ($"local_dt.Date.Year * 10", TNumber(10)));
           });

        [Fact]
        public async Task EvaluateSimpleExpressions() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateTestsClass/TestEvaluate", "run", 9, "run",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateTestsClass:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

               await EvaluateOnCallFrameAndCheck(id,
                   // "((this))", TObject("foo")); //FIXME:
                   // "((dt))", TObject("foo")); //FIXME:

                   ("this", TObject("DebuggerTests.EvaluateTestsClass.TestEvaluate")),
                   ("  this", TObject("DebuggerTests.EvaluateTestsClass.TestEvaluate")),

                   ("5", TNumber(5)),
                   ("  5", TNumber(5)),
                   ("d + e", TNumber(203)),
                   ("e + 10", TNumber(112)),

                   // repeated expressions
                   ("this.a + this.a", TNumber(2)),
                   ("a + \"_\" + a", TString("9000_9000")),
                   ("a+(a  )", TString("90009000")),

                   // possible duplicate arg name
                   ("this.a + this_a", TNumber(46)),

                   ("this.a + this.b", TNumber(3)),
                   ("\"test\" + \"test\"", TString("testtest")),
                   ("5 + 5", TNumber(10)));
           });

        public static TheoryData<string, string, string> ShadowMethodArgsTestData => new TheoryData<string, string, string>
        {
            { "DebuggerTests.EvaluateTestsClassWithProperties", "EvaluateShadow", "EvaluateShadow" },
            { "DebuggerTests.EvaluateTestsClassWithProperties", "EvaluateShadowAsync", "MoveNext" },
            { "DebuggerTests.EvaluateTestsStructWithProperties", "EvaluateShadow", "EvaluateShadow" },
            { "DebuggerTests.EvaluateTestsStructWithProperties", "EvaluateShadowAsync", "MoveNext" },
        };

        [Theory]
        [MemberData(nameof(ShadowMethodArgsTestData))]
        public async Task LocalsAndArgsShadowingThisMembers(string type_name, string method, string bp_function_name) => await CheckInspectLocalsAtBreakpointSite(
            type_name, method, 2, bp_function_name,
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] " + type_name + ":run'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

               await EvaluateOnCallFrameAndCheck(id,
                   ("a", TString("hello")),
                   ("this.a", TNumber(4)));

               await CheckExpressions("this.", new DateTime(2010, 9, 8, 7, 6, 5 + 0));
               await CheckExpressions(String.Empty, new DateTime(2020, 3, 4, 5, 6, 7));

               async Task CheckExpressions(string prefix, DateTime dateTime)
               {
                   await EvaluateOnCallFrameAndCheck(id,
                       (prefix + "dateTime", TDateTime(dateTime)),
                       (prefix + "dateTime.TimeOfDay.Minutes", TNumber(dateTime.TimeOfDay.Minutes)),
                       (prefix + "dateTime.TimeOfDay", TValueType("System.TimeSpan", dateTime.TimeOfDay.ToString())));
               }
           });

        [Theory]
        [InlineData("DebuggerTests.EvaluateTestsStructWithProperties", true)]
        [InlineData("DebuggerTests.EvaluateTestsClassWithProperties", false)]
        public async Task EvaluateOnPreviousFrames(string type_name, bool is_valuetype) => await CheckInspectLocalsAtBreakpointSite(
            type_name, "EvaluateShadow", 1, "EvaluateShadow",
            $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] {type_name}:run'); }})",
            wait_for_event_fn: async (pause_location) =>
           {
               var dt_local = new DateTime(2020, 3, 4, 5, 6, 7);
               var dt_this = new DateTime(2010, 9, 8, 7, 6, 5);

                // At EvaluateShadow
                {
                   var id0 = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                   await EvaluateOnCallFrameAndCheck(id0,
                       ("dateTime", TDateTime(dt_local)),
                       ("this.dateTime", TDateTime(dt_this))
                   );

                   await EvaluateOnCallFrameFail(id0, ("obj.IntProp", "ReferenceError"));
               }

               {
                   var id1 = pause_location["callFrames"][1]["callFrameId"].Value<string>();
                   await EvaluateOnCallFrameFail(id1,
                       ("dateTime", "ReferenceError"),
                       ("this.dateTime", "ReferenceError"));

                    // obj available only on the -1 frame
                    await EvaluateOnCallFrameAndCheck(id1, ("obj.IntProp", TNumber(7)));
               }

               await SetBreakpointInMethod("debugger-test.dll", type_name, "SomeMethod", 1);
               pause_location = await SendCommandAndCheck(null, "Debugger.resume", null, 0, 0, "SomeMethod");

                // At SomeMethod

                // TODO: change types also.. so, that `this` is different!

                // Check frame0
                {
                   var id0 = pause_location["callFrames"][0]["callFrameId"].Value<string>();

                    // 'me' and 'dateTime' are reversed in this method
                    await EvaluateOnCallFrameAndCheck(id0,
                       ("dateTime", is_valuetype ? TValueType(type_name) : TObject(type_name)),
                       ("this.dateTime", TDateTime(dt_this)),
                       ("me", TDateTime(dt_local)),

                       // local variable shadows field, but isn't "live" yet
                       ("DTProp", TString(null)),

                       // access field via `this.`
                       ("this.DTProp", TDateTime(dt_this.AddMinutes(10))));

                   await EvaluateOnCallFrameFail(id0, ("obj", "ReferenceError"));
               }

                // check frame1
                {
                   var id1 = pause_location["callFrames"][1]["callFrameId"].Value<string>();

                   await EvaluateOnCallFrameAndCheck(id1,
                       // 'me' and 'dateTime' are reversed in this method
                       ("dateTime", TDateTime(dt_local)),
                       ("this.dateTime", TDateTime(dt_this)),
                       ("me", is_valuetype ? TValueType(type_name) : TObject(type_name)),

                       // not shadowed here
                       ("DTProp", TDateTime(dt_this.AddMinutes(10))),

                       // access field via `this.`
                       ("this.DTProp", TDateTime(dt_this.AddMinutes(10))));

                   await EvaluateOnCallFrameFail(id1, ("obj", "ReferenceError"));
               }

                // check frame2
                {
                   var id2 = pause_location["callFrames"][2]["callFrameId"].Value<string>();

                    // Only obj should be available
                    await EvaluateOnCallFrameFail(id2,
                       ("dateTime", "ReferenceError"),
                       ("this.dateTime", "ReferenceError"),
                       ("me", "ReferenceError"));

                   await EvaluateOnCallFrameAndCheck(id2, ("obj", is_valuetype ? TValueType(type_name) : TObject(type_name)));
               }
           });

        [Fact]
        public async Task JSEvaluate()
        {
            var insp = new Inspector();
            //Collect events
            var scripts = SubscribeToScripts(insp);

            var bp_loc = "/other.js";
            var line = 76;
            var col = 1;

            await Ready();
            await insp.Ready(async (cli, token) =>
            {
                ctx = new DebugTestContext(cli, insp, token, scripts);
                await SetBreakpoint(bp_loc, line, col);

                var eval_expr = "window.setTimeout(function() { eval_call_on_frame_test (); }, 1)";
                var result = await ctx.cli.SendCommand("Runtime.evaluate", JObject.FromObject(new { expression = eval_expr }), ctx.token);
                var pause_location = await ctx.insp.WaitFor(Inspector.PAUSE);

                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

                await EvaluateOnCallFrameFail(id,
                    ("me.foo", null),
                    ("obj.foo.bar", null));

                await EvaluateOnCallFrame(id, "obj.foo", expect_ok: true);
            });
        }

        [Fact]
        public async Task NegativeTestsInInstanceMethod() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateTestsClass/TestEvaluate", "run", 9, "run",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateTestsClass:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

                // Use '.' on a primitive member
                await EvaluateOnCallFrameFail(id,
                   //BUG: TODO:
                   //("a)", "CompilationError"),

                   ("this.a.", "ReferenceError"),
                   ("a.", "ReferenceError"),

                   ("this..a", "CompilationError"),
                   (".a.", "ReferenceError"),

                   ("me.foo", "ReferenceError"),

                   ("this.a + non_existant", "ReferenceError"),

                   ("this.NullIfAIsNotZero.foo", "ReferenceError"),
                   ("NullIfAIsNotZero.foo", "ReferenceError"));
           });

        [Fact]
        public async Task NegativeTestsInStaticMethod() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateTestsClass", "EvaluateLocals", 9, "EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateTestsClass:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

               await EvaluateOnCallFrameFail(id,
                   ("me.foo", "ReferenceError"),
                   ("this", "ReferenceError"),
                   ("this.NullIfAIsNotZero.foo", "ReferenceError"));
           });

        async Task EvaluateOnCallFrameAndCheck(string call_frame_id, params (string expression, JObject expected)[] args)
        {
            foreach (var arg in args)
            {
                var (eval_val, _) = await EvaluateOnCallFrame(call_frame_id, arg.expression);
                try
                {
                    await CheckValue(eval_val, arg.expected, arg.expression);
                }
                catch
                {
                    Console.WriteLine($"CheckValue failed for {arg.expression}. Expected: {arg.expected}, vs {eval_val}");
                    throw;
                }
            }
        }

        async Task EvaluateOnCallFrameFail(string call_frame_id, params (string expression, string class_name)[] args)
        {
            foreach (var arg in args)
            {
                var (_, res) = await EvaluateOnCallFrame(call_frame_id, arg.expression, expect_ok: false);
                if (arg.class_name != null)
                    AssertEqual(arg.class_name, res.Error["result"]?["className"]?.Value<string>(), $"Error className did not match for expression '{arg.expression}'");
            }
        }
    }

}
