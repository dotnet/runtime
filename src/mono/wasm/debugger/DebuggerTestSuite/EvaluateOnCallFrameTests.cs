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
using Xunit.Abstractions;

namespace DebuggerTests
{
    // TODO: static async, static method args
    public class EvaluateOnCallFrameTests : DebuggerTests
    {
        public EvaluateOnCallFrameTests(ITestOutputHelper testOutput) : base(testOutput)
        {}

        public static IEnumerable<object[]> InstanceMethodsTestData(string type_name)
        {
            yield return new object[] { type_name, "InstanceMethod", $"{type_name}.InstanceMethod", false };
            yield return new object[] { type_name, "GenericInstanceMethod", $"{type_name}.GenericInstanceMethod<int>", false };
            yield return new object[] { type_name, "InstanceMethodAsync", $"{type_name}.InstanceMethodAsync", true };
            yield return new object[] { type_name, "GenericInstanceMethodAsync", $"{type_name}.GenericInstanceMethodAsync<int>", true };

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

        [ConditionalTheory(nameof(RunningOnChrome))]
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
               _testOutput.WriteLine ($"------- test running the bits..");
               await EvaluateOnCallFrameAndCheck(id,
                   ("g", TNumber(400)),
                   ("h", TNumber(123)),
                   ("valString", TString("just a test")),
                   ("me", TObject(type)),

                   // property on method arg
                   ("me.DTProp", TDateTime(DTProp)),
                   ("me.DTProp.TimeOfDay.Minutes", TNumber(DTProp.TimeOfDay.Minutes)),
                   ("me.DTProp.Second + (me.IntProp - 5)", TNumber(DTProp.Second + 4)))
                    .ConfigureAwait(false);

               _testOutput.WriteLine ($"------- test done!");
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
                   ("  d ", TNumber(401)),
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

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task EvaluateStaticLocalsWithDeepMemberAccess() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateTestsClass", "EvaluateLocals", 9, "DebuggerTests.EvaluateTestsClass.EvaluateLocals",
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

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task EvaluateLocalsAsync() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.Point", "AsyncInstanceMethod", 1, "DebuggerTests.Point.AsyncInstanceMethod",
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

                   (local_gs, _) = await EvaluateOnCallFrame(id, "  local_gs");
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

        [ConditionalTheory(nameof(RunningOnChrome))]
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

        [Theory]
        [InlineData("")]
        [InlineData("this.")]
        public async Task InheritedAndPrivateMembersInAClass(string prefix)
        => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.GetPropertiesTests.DerivedClass", "InstanceMethod", 1, "DebuggerTests.GetPropertiesTests.DerivedClass.InstanceMethod",
            $"window.setTimeout(function() {{ invoke_static_method_async('[debugger-test] DebuggerTests.GetPropertiesTests.DerivedClass:run');}}, 1);",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

               foreach (var pad in new[] { String.Empty, "  " })
               {
                   var padded_prefix = pad + prefix;
                   await EvaluateOnCallFrameAndCheck(id,
                       // overridden
                       ($"{padded_prefix}FirstName + \"_foo\"", TString("DerivedClass#FirstName_foo")),
                       ($"{padded_prefix}DateTimeForOverride.Date.Year", TNumber(2190)),
                       ($"{padded_prefix}DateTimeForOverride.Date.Year - 10", TNumber(2180)),
                       ($"\"foo_\" + {padded_prefix}StringPropertyForOverrideWithAutoProperty", TString("foo_DerivedClass#StringPropertyForOverrideWithAutoProperty")),

                       // private
                       ($"{padded_prefix}_stringField + \"_foo\"", TString("DerivedClass#_stringField_foo")),
                       ($"{padded_prefix}_stringField", TString("DerivedClass#_stringField")),
                       ($"{padded_prefix}_dateTime.Second + 4", TNumber(7)),
                       ($"{padded_prefix}_DTProp.Second + 4", TNumber(13)),

                       // inherited public
                       ($"\"foo_\" + {padded_prefix}Base_AutoStringProperty", TString("foo_base#Base_AutoStringProperty")),
                       // inherited private
                       ($"{padded_prefix}_base_dateTime.Date.Year - 10", TNumber(2124))
                   );
               }
           });

        [Fact]
        public async Task EvaluateSimpleExpressions() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateTestsClass.TestEvaluate", "run", 9, "DebuggerTests.EvaluateTestsClass.TestEvaluate.run",
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
            { "DebuggerTests.EvaluateTestsClassWithProperties", "EvaluateShadow", "DebuggerTests.EvaluateTestsClassWithProperties.EvaluateShadow" },
            { "DebuggerTests.EvaluateTestsClassWithProperties", "EvaluateShadowAsync", "DebuggerTests.EvaluateTestsClassWithProperties.EvaluateShadowAsync" },
            { "DebuggerTests.EvaluateTestsStructWithProperties", "EvaluateShadow", "DebuggerTests.EvaluateTestsStructWithProperties.EvaluateShadow" },
            { "DebuggerTests.EvaluateTestsStructWithProperties", "EvaluateShadowAsync", "DebuggerTests.EvaluateTestsStructWithProperties.EvaluateShadowAsync" },
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

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData("DebuggerTests.EvaluateTestsStructWithProperties", true)]
        [InlineData("DebuggerTests.EvaluateTestsClassWithProperties", false)]
        public async Task EvaluateOnPreviousFrames(string type_name, bool is_valuetype) => await CheckInspectLocalsAtBreakpointSite(
            type_name, "EvaluateShadow", 1, $"{type_name}.EvaluateShadow",
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
               pause_location = await SendCommandAndCheck(null, "Debugger.resume", null, 0, 0,  $"{type_name}.SomeMethod");

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

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task JSEvaluate()
        {
            var bp_loc = "/other.js";
            var line = 78;
            var col = 1;

            await SetBreakpoint(bp_loc, line, col);

            var eval_expr = "window.setTimeout(function() { eval_call_on_frame_test (); }, 1)";
            var result = await cli.SendCommand("Runtime.evaluate", JObject.FromObject(new { expression = eval_expr }), token);
            var pause_location = await insp.WaitFor(Inspector.PAUSE);

            var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

            await EvaluateOnCallFrameFail(id,
                ("me.foo", null),
                ("obj.foo.bar", null));

            await EvaluateOnCallFrame(id, "obj.foo", expect_ok: true);
        }

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task NegativeTestsInInstanceMethod() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateTestsClass.TestEvaluate", "run", 9, "DebuggerTests.EvaluateTestsClass.TestEvaluate.run",
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

                  ("this.a + non_existent", "ReferenceError"),

                  ("this.NullIfAIsNotZero.foo", "ReferenceError"),
                  ("NullIfAIsNotZero.foo", "ReferenceError"));
           });

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task NegativeTestsInStaticMethod() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateTestsClass", "EvaluateLocals", 9, "DebuggerTests.EvaluateTestsClass.EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateTestsClass:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

               await EvaluateOnCallFrameFail(id,
                   ("me.foo", "ReferenceError"),
                   ("this", "CompilationError"),
                   ("this.NullIfAIsNotZero.foo", "ReferenceError"));
           });

        [Fact]
        public async Task EvaluatePropertyThatThrows()
        => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateTestsClassWithProperties", "InstanceMethod", /*line_offset*/1, "DebuggerTests.EvaluateTestsClassWithProperties.InstanceMethod",
            $"window.setTimeout(function() {{ invoke_static_method_async('[debugger-test] DebuggerTests.EvaluateTestsClassWithProperties:run');}}, 1);",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                await EvaluateOnCallFrameAndCheck(id, ("this.PropertyThrowException", TString("System.Exception: error")));
            });

        async Task EvaluateOnCallFrameFail(string call_frame_id, params (string expression, string class_name)[] args)
        {
            foreach (var arg in args)
            {
                var (_, res) = await EvaluateOnCallFrame(call_frame_id, arg.expression, expect_ok: false);
                if (arg.class_name != null)
                    AssertEqual(arg.class_name, res.Error["result"]?["className"]?.Value<string>(), $"Error className did not match for expression '{arg.expression}'");
            }
        }


        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task EvaluateSimpleMethodCallsError() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateMethodTestsClass.TestEvaluate", "run", 9, "DebuggerTests.EvaluateMethodTestsClass.TestEvaluate.run",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateMethodTestsClass:EvaluateMethods'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

               var (_, res) = await EvaluateOnCallFrame(id, "this.objToTest.MyMethodWrong()", expect_ok: false );
               Assert.Equal(
                    $"Method 'MyMethodWrong' not found in type 'DebuggerTests.EvaluateMethodTestsClass.ParmToTest'",
                    res.Error["result"]?["description"]?.Value<string>());

               (_, res) = await EvaluateOnCallFrame(id, "this.objToTest.MyMethod(1)", expect_ok: false);
               Assert.Equal(
                   "Unable to evaluate method 'MyMethod'. Too many arguments passed.",
                   res.Error["result"]?["description"]?.Value<string>());

               (_, res) = await EvaluateOnCallFrame(id, "this.CallMethodWithParm(\"1\")", expect_ok: false );
               Assert.Contains("No implementation of method 'CallMethodWithParm' matching 'this.CallMethodWithParm(\"1\")' found in type DebuggerTests.EvaluateMethodTestsClass.TestEvaluate.", res.Error["result"]?["description"]?.Value<string>());

               (_, res) = await EvaluateOnCallFrame(id, "this.ParmToTestObjNull.MyMethod()", expect_ok: false );
               Assert.Equal("Expression 'this.ParmToTestObjNull.MyMethod' evaluated to null", res.Error["message"]?.Value<string>());

               (_, res) = await EvaluateOnCallFrame(id, "this.ParmToTestObjException.MyMethod()", expect_ok: false );
               Assert.Equal("Method 'MyMethod' not found in type 'string'", res.Error["result"]?["description"]?.Value<string>());
           });

        [Fact]
        public async Task EvaluateSimpleMethodCallsWithoutParms() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateMethodTestsClass.TestEvaluate", "run", 9, "DebuggerTests.EvaluateMethodTestsClass.TestEvaluate.run",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateMethodTestsClass:EvaluateMethods'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

               await EvaluateOnCallFrameAndCheck(id,
                    ("this.CallMethod()", TNumber(1)),
                    ("this.CallMethod()", TNumber(1)),
                    ("this.CallMethodReturningChar()", TChar('A')),
                    ("this.ParmToTestObj.MyMethod()", TString("methodOK")),
                    ("this.ParmToTestObj.ToString()", TString("DebuggerTests.EvaluateMethodTestsClass+ParmToTest")),
                    ("this.objToTest.MyMethod()", TString("methodOK")));
           });


        [Fact]
        public async Task EvaluateSimpleMethodCallsWithConstParms() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateMethodTestsClass.TestEvaluate", "run", 9, "DebuggerTests.EvaluateMethodTestsClass.TestEvaluate.run",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateMethodTestsClass:EvaluateMethods'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

               await EvaluateOnCallFrameAndCheck(id,
                    ("this.CallMethodWithParm(10)", TNumber(11)),
                    ("this.CallMethodWithMultipleParms(10, 10)", TNumber(21)),
                    ("this.CallMethodWithParmBool(true)", TString("TRUE")),
                    ("this.CallMethodWithParmBool(false)", TString("FALSE")),
                    ("this.CallMethodWithParmString(\"concat\")", TString("str_const_concat")),
                    ("this.CallMethodWithParmString(\"\\\"\\\"\")", TString("str_const_\"\"")),
                    ("this.CallMethodWithParmString(\"🛶\")", TString("str_const_🛶")),
                    ("this.CallMethodWithParmString(\"\\uD83D\\uDEF6\")", TString("str_const_🛶")),
                    ("this.CallMethodWithParmString(\"🚀\")", TString("str_const_🚀")),
                    ("this.CallMethodWithParmString_λ(\"🚀\")", TString("λ_🚀")),
                    ("this.CallMethodWithParm(10) + this.a", TNumber(12)),
                    ("this.CallMethodWithObj(null)", TNumber(-1)),
                    ("this.CallMethodWithChar('a')", TString("str_const_a")));
           });

        [Fact]
        public async Task EvaluateSimpleMethodCallsWithVariableParms() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateMethodTestsClass.TestEvaluate", "run", 9, "DebuggerTests.EvaluateMethodTestsClass.TestEvaluate.run",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateMethodTestsClass:EvaluateMethods'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

               await EvaluateOnCallFrameAndCheck(id,
                    ("this.CallMethodWithParm(this.a)", TNumber(2)),
                    ("this.CallMethodWithMultipleParms(this.a, 10)", TNumber(12)),
                    ("this.CallMethodWithParmString(this.str)", TString("str_const_str_const_")),
                    ("this.CallMethodWithParmBool(this.t)", TString("TRUE")),
                    ("this.CallMethodWithParmBool(this.f)", TString("FALSE")),
                    ("this.CallMethodWithParm(this.a) + this.a", TNumber(3)),
                    ("this.CallMethodWithObj(this.objToTest)", TNumber(10)));
           });

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task EvaluateIndexingNegative() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateLocalsWithIndexingTests", "EvaluateLocals", 5, "DebuggerTests.EvaluateLocalsWithIndexingTests.EvaluateLocals",
            $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithIndexingTests:EvaluateLocals'); 1 }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                var (_, res) = await EvaluateOnCallFrame(id, "f.idx0[2]", expect_ok: false );
                Assert.Equal("Unable to evaluate element access 'f.idx0[2]': Cannot apply indexing with [] to a primitive object of type 'number'", res.Error["message"]?.Value<string>());
                (_, res) = await EvaluateOnCallFrame(id, "f[1]", expect_ok: false );
                Assert.Equal( "Unable to evaluate element access 'f[1]': Type 'DebuggerTests.EvaluateLocalsWithIndexingTests.TestEvaluate' cannot be indexed.", res.Error["message"]?.Value<string>());
           });

        [Fact]
        public async Task EvaluateIndexingsByConstant() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateLocalsWithIndexingTests", "EvaluateLocals", 5, "DebuggerTests.EvaluateLocalsWithIndexingTests.EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithIndexingTests:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

               await EvaluateOnCallFrameAndCheck(id,
                   ("f.numList[0]", TNumber(1)),
                   ("f.textList[1]", TString("2")),
                   ("f.numArray[1]", TNumber(2)),
                   ("f.textArray[0]", TString("1")));
           });

        [Fact]
        public async Task EvaluateIndexingByLocalVariable() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateLocalsWithIndexingTests", "EvaluateLocals", 5, "DebuggerTests.EvaluateLocalsWithIndexingTests.EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithIndexingTests:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

               await EvaluateOnCallFrameAndCheck(id,
                   ("f.numList[i]", TNumber(1)),
                   ("f.textList[j]", TString("2")),
                   ("f.numArray[j]", TNumber(2)),
                   ("f.textArray[i]", TString("1")));

           });

        [Fact]
        public async Task EvaluateIndexingByMemberVariables() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateLocalsWithIndexingTests", "EvaluateLocals", 5, "DebuggerTests.EvaluateLocalsWithIndexingTests.EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithIndexingTests:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

               await EvaluateOnCallFrameAndCheck(id,
                   ("f.idx0", TNumber(0)),
                   ("f.idx1", TNumber(1)),
                   ("f.numList[f.idx0]", TNumber(1)),
                   ("f.textList[f.idx1]", TString("2")),
                   ("f.numArray[f.idx1]", TNumber(2)),
                   ("f.textArray[f.idx0]", TString("1")));

           });

        [Fact]
        public async Task EvaluateIndexingNested() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateLocalsWithIndexingTests", "EvaluateLocals", 5, "DebuggerTests.EvaluateLocalsWithIndexingTests.EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithIndexingTests:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

               await EvaluateOnCallFrameAndCheck(id,
                   ("f.idx0", TNumber(0)),
                   ("f.numList[f.numList[f.idx0]]", TNumber(2)),
                   ("f.textList[f.numList[f.idx0]]", TString("2")),
                   ("f.numArray[f.numArray[f.idx0]]", TNumber(2)),
                   ("f.textArray[f.numArray[f.idx0]]", TString("2")));

           });

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task EvaluateIndexingMultidimensional() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateLocalsWithMultidimensionalIndexingTests", "EvaluateLocals", 5, "DebuggerTests.EvaluateLocalsWithMultidimensionalIndexingTests.EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithMultidimensionalIndexingTests:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

               await EvaluateOnCallFrameAndCheck(id,
                   ("j", TNumber(1)),
                   ("f.idx1", TNumber(1)),
                   ("f.numArray2D[0, 0]", TNumber(1)),
                   ("f.numArray2D[0, 1]", TNumber(2)),
                   ("f.numArray2D[1,  0]", TNumber(3)),
                   ("f.numArray2D[1  ,1]", TNumber(4)),
                   ("f.numArray3D[0, 0, 0]", TNumber(1)),
                   ("f.numArray3D[0 ,0  ,1]", TNumber(2)),
                   ("f.numArray3D[0  ,0,    2]", TNumber(3)),
                   ("f.numArray3D[1,  1,        0]", TNumber(10)),
                   ("f.numArray3D[1, 1,  1]", TNumber(11)),
                   ("f.numArray3D[1 , 1, 2]", TNumber(12)),
                   ("f.numArray2D[0,0]", TNumber(1)),
                   ("f.numArray2D[0,1]", TNumber(2)),
                   ("f.numArray2D[1,0]", TNumber(3)),
                   ("f.numArray2D[1,1]", TNumber(4)),
                   ("f.numArray3D[0,0,0]", TNumber(1)),
                   ("f.numArray3D[0,0,1]", TNumber(2)),
                   ("f.numArray3D[0,0,2]", TNumber(3)),
                   ("f.numArray3D[1,1,0]", TNumber(10)),
                   ("f.numArray3D[1,1,1]", TNumber(11)),
                   ("f.numArray3D[1,1,2]", TNumber(12)),
                   ("f.textArray2D[0,0]", TString("one")),
                   ("f.textArray2D[0,1]", TString("two")),
                   ("f.textArray2D[1,0]", TString("three")),
                   ("f.textArray2D[1,1]", TString("four")),
                   ("f.numArray2D[i,i]", TNumber(1)),
                   ("f.numArray2D[i,j]", TNumber(2)),
                   ("f.numArray2D[j,i]", TNumber(3)),
                   ("f.numArray2D[j,j]", TNumber(4)),
                   ("f.numArray3D[i,i,i]", TNumber(1)),
                   ("f.numArray3D[i,i,j]", TNumber(2)),
                   ("f.numArray3D[i,i,2]", TNumber(3)),
                   ("f.numArray3D[j,j,i]", TNumber(10)),
                   ("f.numArray3D[j,j,1]", TNumber(11)),
                   ("f.numArray3D[j,j,2]", TNumber(12)),
                   ("f.textArray2D[i,i]", TString("one")),
                   ("f.textArray2D[i,j]", TString("two")),
                   ("f.textArray2D[j,i]", TString("three")),
                   ("f.textArray2D[j,j]", TString("four")),
                   ("f.numArray2D[f.idx0,f.idx0]", TNumber(1)),
                   ("f.numArray2D[f.idx0,f.idx1]", TNumber(2)),
                   ("f.numArray2D[f.idx1,f.idx0]", TNumber(3)),
                   ("f.numArray2D[f.idx1,f.idx1]", TNumber(4)),
                   ("f.numArray3D[f.idx0,f.idx0,f.idx0]", TNumber(1)),
                   ("f.numArray3D[f.idx0,f.idx0,f.idx1]", TNumber(2)),
                   ("f.numArray3D[f.idx0,f.idx0,2]", TNumber(3)),
                   ("f.numArray3D[f.idx1,f.idx1,f.idx0]", TNumber(10)),
                   ("f.numArray3D[f.idx1,f.idx1,f.idx1]", TNumber(11)),
                   ("f.numArray3D[f.idx1,f.idx1,2]", TNumber(12)),
                   ("f.textArray2D[f.idx0,f.idx0]", TString("one")),
                   ("f.textArray2D[f.idx0,f.idx1]", TString("two")),
                   ("f.textArray2D[f.idx1,f.idx0]", TString("three")),
                   ("f.textArray2D[f.idx1,f.idx1]", TString("four")));
           });

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task EvaluateIndexingJagged() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateLocalsWithIndexingTests", "EvaluateLocals", 5, "DebuggerTests.EvaluateLocalsWithIndexingTests.EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithIndexingTests:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

               await EvaluateOnCallFrameAndCheck(id,
                   ("j", TNumber(1)),
                   ("f.idx1", TNumber(1)),
                   ("f.numArrayOfArrays[1][1]", TNumber(2)),
                   ("f.numArrayOfArrays[j][j]", TNumber(2)),
                   ("f.numArrayOfArrays[f.idx1][f.idx1]", TNumber(2)),
                   ("f.numListOfLists[1][1]", TNumber(2)),
                   ("f.numListOfLists[j][j]", TNumber(2)),
                   ("f.numListOfLists[f.idx1][f.idx1]", TNumber(2)),
                   ("f.textArrayOfArrays[1][1]", TString("2")),
                   ("f.textArrayOfArrays[j][j]", TString("2")),
                   ("f.textArrayOfArrays[f.idx1][f.idx1]", TString("2")),
                   ("f.textListOfLists[1][1]", TString("2")),
                   ("f.textListOfLists[j][j]", TString("2")),
                   ("f.textListOfLists[f.idx1][f.idx1]", TString("2")));

           });

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task EvaluateSimpleMethodCallsCheckChangedValue() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateMethodTestsClass.TestEvaluate", "run", 9, "DebuggerTests.EvaluateMethodTestsClass.TestEvaluate.run",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateMethodTestsClass:EvaluateMethods'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

               var frame = pause_location["callFrames"][0];
               var props = await GetObjectOnFrame(frame, "this");
               CheckNumber(props, "a", 1);

               await EvaluateOnCallFrameAndCheck(id,
                   ("this.CallMethodChangeValue()", TObject("object", is_null : true)));

               frame = pause_location["callFrames"][0];
               props = await GetObjectOnFrame(frame, "this");
               CheckNumber(props, "a", 11);
           });

        [Theory]
        [InlineData("DebuggerTestsV2.EvaluateStaticFieldsInStaticClass", "Run", "DebuggerTestsV2", "EvaluateStaticFieldsInStaticClass", "EvaluateMethods", 1, 2)]
        [InlineData("DebuggerTests.EvaluateStaticFieldsInStaticClass", "Run", "DebuggerTests", "EvaluateStaticFieldsInStaticClass", "EvaluateMethods", 1, 1)]
        [InlineData("DebuggerTests.EvaluateStaticFieldsInStaticClass", "RunAsync", "DebuggerTests", "EvaluateStaticFieldsInStaticClass", "EvaluateMethodsAsync", 1, 1)]
        [InlineData("DebuggerTests.EvaluateStaticFieldsInInstanceClass", "RunStatic", "DebuggerTests", "EvaluateStaticFieldsInInstanceClass", "EvaluateMethods", 1, 7)]
        [InlineData("DebuggerTests.EvaluateStaticFieldsInInstanceClass", "RunStaticAsync", "DebuggerTests", "EvaluateStaticFieldsInInstanceClass", "EvaluateMethodsAsync", 1, 7)]
        [InlineData("DebuggerTests.EvaluateStaticFieldsInInstanceClass", "Run", "DebuggerTests", "EvaluateStaticFieldsInInstanceClass", "EvaluateMethods", 1, 7)]
        [InlineData("DebuggerTests.EvaluateStaticFieldsInInstanceClass", "RunAsync", "DebuggerTests", "EvaluateStaticFieldsInInstanceClass", "EvaluateMethodsAsync", 1, 7)]
        public async Task EvaluateStaticFields(
            string bpLocation, string bpMethod, string namespaceName, string className, string triggeringMethod, int bpLine, int expectedInt) =>
            await CheckInspectLocalsAtBreakpointSite(
                bpLocation, bpMethod, bpLine, $"{bpLocation}.{bpMethod}",
                $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] DebuggerTests.EvaluateMethodTestsClass:{triggeringMethod}'); }})",
                wait_for_event_fn: async (pause_location) =>
                {
                    var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                    foreach (var pad in new[] { String.Empty, "  " })
                    {
                        await EvaluateOnCallFrameAndCheck(id,
                            ($"{pad}{namespaceName}.{className}.StaticField", TNumber(expectedInt * 10)),
                            ($"{pad}{namespaceName}{pad}.{className}.{pad}StaticProperty", TString($"StaticProperty{expectedInt}")),
                            ($"{namespaceName}.{pad}{className}.StaticPropertyWithError", TString($"System.Exception: not implemented {expectedInt}")),
                            ($"{pad}{className}.{pad}StaticField", TNumber(expectedInt * 10)),
                            ($"{pad}{pad}{className}.StaticProperty", TString($"StaticProperty{expectedInt}")),
                            ($"{pad}{className}.StaticPropertyWithError", TString($"System.Exception: not implemented {expectedInt}")),
                            ($"{pad}StaticField{pad}", TNumber(expectedInt * 10)),
                            ($"{pad}StaticProperty", TString($"StaticProperty{expectedInt}")),
                            ($"{pad}StaticPropertyWithError", TString($"System.Exception: not implemented {expectedInt}"))
                        );
                    }
                });

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task EvaluateStaticClassesNested() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateMethodTestsClass", "EvaluateMethods", 3, "DebuggerTests.EvaluateMethodTestsClass.EvaluateMethods",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateMethodTestsClass:EvaluateMethods'); })",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                foreach (var pad in new[] { String.Empty, "  " })
                {
                    await EvaluateOnCallFrameAndCheck(id,
                        ($"{pad}DebuggerTests{pad}.EvaluateStaticFieldsInStaticClass.NestedClass1.{pad}NestedClass2.NestedClass3.{pad}StaticField", TNumber(3)),
                        ($"{pad}DebuggerTests.EvaluateStaticFieldsInStaticClass.NestedClass1.NestedClass2.NestedClass3.StaticProperty", TString("StaticProperty3")),
                        ($"{pad}{pad}DebuggerTests.{pad}EvaluateStaticFieldsInStaticClass.NestedClass1.NestedClass2.NestedClass3.{pad}StaticPropertyWithError", TString("System.Exception: not implemented 3")),
                        ($"EvaluateStaticFieldsInStaticClass.{pad}NestedClass1.{pad}NestedClass2.NestedClass3.StaticField", TNumber(3)),
                        ($"EvaluateStaticFieldsInStaticClass.NestedClass1.{pad}{pad}NestedClass2.NestedClass3.{pad}StaticProperty", TString("StaticProperty3")),
                        ($"{pad}EvaluateStaticFieldsInStaticClass.NestedClass1.{pad}NestedClass2.{pad}NestedClass3.StaticPropertyWithError", TString("System.Exception: not implemented 3")));
                }
            });

        [Fact]
        public async Task EvaluateStaticClassesNestedWithNoNamespace() => await CheckInspectLocalsAtBreakpointSite(
            "NoNamespaceClass", "EvaluateMethods", 1, "NoNamespaceClass.EvaluateMethods",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] NoNamespaceClass:EvaluateMethods'); })",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                foreach (var pad in new[] { String.Empty, "  " })
                {
                    await EvaluateOnCallFrameAndCheck(id,
                        ($"{pad}NoNamespaceClass.NestedClass1.NestedClass2.{pad}NestedClass3.StaticField", TNumber(30)),
                        ($"NoNamespaceClass.NestedClass1.{pad}NestedClass2.NestedClass3.StaticProperty", TString("StaticProperty30")),
                        ($"NoNamespaceClass.{pad}NestedClass1.NestedClass2.NestedClass3.{pad}StaticPropertyWithError", TString("System.Exception: not implemented 30")));
                }
            });

        [Fact]
        public async Task EvaluateStaticClassesNestedWithSameNames() => await CheckInspectLocalsAtBreakpointSite(
            "NestedWithSameNames.B.NestedWithSameNames.B", "Run", 1, "NestedWithSameNames.B.NestedWithSameNames.B.Run",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] NestedWithSameNames:Evaluate'); })",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                foreach (var pad in new[] { String.Empty, "  " })
                {
                    await EvaluateOnCallFrameAndCheck(id,
                        ($"{pad}NestedWithSameNames", TNumber(90)),
                        ($"B.{pad}NestedWithSameNames", TNumber(90)),
                        ($"{pad}B.{pad}StaticField", TNumber(40)),
                        ($"{pad}{pad}B.StaticProperty", TString("StaticProperty4")),
                        ($"B.{pad}StaticPropertyWithError{pad}", TString("System.Exception: not implemented V4"))
                    );
                    await CheckEvaluateFail(id,
                        ($"{pad}NestedWithSameNames.B.{pad}StaticField", GetPrimitiveHasNoMembersMessage("B")),
                        ($"NestedWithSameNames.{pad}B.StaticProperty", GetPrimitiveHasNoMembersMessage("B")),
                        ($"{pad}NestedWithSameNames{pad}.{pad}B.StaticPropertyWithError", GetPrimitiveHasNoMembersMessage("B")),
                        ($"{pad}NestedWithSameNames.B.{pad}NestedWithSameNames", GetPrimitiveHasNoMembersMessage("B")),
                        ($"B.NestedWithSameNames.{pad}B{pad}.StaticField", GetPrimitiveHasNoMembersMessage("B")),
                        ($"{pad}B.NestedWithSameNames.{pad}B.StaticProperty", GetPrimitiveHasNoMembersMessage("B")),
                        ($"B.NestedWithSameNames{pad}.B.{pad}StaticPropertyWithError", GetPrimitiveHasNoMembersMessage("B")),
                        ($"{pad}NestedWithSameNames.B{pad}.NestedWithSameNames.B{pad}.NestedWithSameNames{pad}", GetPrimitiveHasNoMembersMessage("B")),
                        ($"NestedWithSameNames.B{pad}.{pad}{pad}NestedWithDifferentName.B.{pad}StaticField", GetPrimitiveHasNoMembersMessage("B")),
                        ($"{pad}NestedWithSameNames.B.NestedWithDifferentName.B.StaticProperty", GetPrimitiveHasNoMembersMessage("B")),
                        ($"NestedWithSameNames.{pad}B.{pad}NestedWithDifferentName.B.{pad}StaticPropertyWithError", GetPrimitiveHasNoMembersMessage("B"))
                    );
                }
                string GetPrimitiveHasNoMembersMessage(string name) => $"Cannot find member '{name}' on a primitive type";
            });

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData("DebuggerTests", "EvaluateStaticFieldsInInstanceClass", 7, true)]
        [InlineData("DebuggerTestsV2", "EvaluateStaticFieldsInStaticClass", 2, false)]
        public async Task EvaluateStaticFieldsFromDifferentNamespaceInDifferentFrames(
            string namespaceName, string className, int expectedInt, bool isFromDifferentNamespace) =>
            await CheckInspectLocalsAtBreakpointSite(
                "DebuggerTestsV2.EvaluateStaticFieldsInStaticClass", "Run", 1, "DebuggerTestsV2.EvaluateStaticFieldsInStaticClass.Run",
                "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateMethodTestsClass:EvaluateMethods'); })",
                wait_for_event_fn: async (pause_location) =>
                {
                    var id_top = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                    var id_second = pause_location["callFrames"][1]["callFrameId"].Value<string>();
                    int expectedIntInPrevFrame = isFromDifferentNamespace ? 7 : 1;

                    foreach (var pad in new[] { String.Empty, "  " })
                    {
                        await EvaluateOnCallFrameAndCheck(id_top,
                            ($"{pad}StaticField", TNumber(20)),
                            ($"{pad}{namespaceName}.{pad}{className}.StaticField{pad}", TNumber(expectedInt * 10)),
                            ($"{pad}StaticProperty", TString($"StaticProperty2")),
                            ($"{pad}{namespaceName}.{pad}{className}.StaticProperty", TString($"StaticProperty{expectedInt}")),
                            ($"{pad}StaticPropertyWithError", TString($"System.Exception: not implemented 2")),
                            ($"{pad}{namespaceName}{pad}.{pad}{className}.StaticPropertyWithError", TString($"System.Exception: not implemented {expectedInt}"))
                        );

                        if (!isFromDifferentNamespace)
                        {
                            await EvaluateOnCallFrameAndCheck(id_top,
                                ($"{pad}{className}.StaticField", TNumber(expectedInt * 10)),
                                ($"{className}{pad}.StaticProperty{pad}", TString($"StaticProperty{expectedInt}")),
                                ($"{className}{pad}.{pad}StaticPropertyWithError", TString($"System.Exception: not implemented {expectedInt}"))
                            );
                        }

                        await EvaluateOnCallFrameAndCheck(id_second,
                            ($"{pad}{namespaceName}.{pad}{className}.{pad}StaticField", TNumber(expectedInt * 10)),
                            ($"{pad}{className}.StaticField", TNumber(expectedIntInPrevFrame * 10)),
                            ($"{namespaceName}{pad}.{pad}{className}.StaticProperty", TString($"StaticProperty{expectedInt}")),
                            ($"{pad}{className}.StaticProperty", TString($"StaticProperty{expectedIntInPrevFrame}")),
                            ($"{pad}{namespaceName}.{className}.StaticPropertyWithError", TString($"System.Exception: not implemented {expectedInt}")),
                            ($"{className}{pad}.StaticPropertyWithError{pad}", TString($"System.Exception: not implemented {expectedIntInPrevFrame}"))
                        );

                        await CheckEvaluateFail(id_second,
                            ($"{pad}StaticField", GetNonExistingVarMessage("StaticField")),
                            ($"{pad}{pad}StaticProperty", GetNonExistingVarMessage("StaticProperty")),
                            ($"{pad}StaticPropertyWithError{pad}", GetNonExistingVarMessage("StaticPropertyWithError"))
                        );
                    }
                    string GetNonExistingVarMessage(string name) => $"The name {name} does not exist in the current context";
                });

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task EvaluateStaticClassInvalidField() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateMethodTestsClass.TestEvaluate", "run", 9, "DebuggerTests.EvaluateMethodTestsClass.TestEvaluate.run",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateMethodTestsClass:EvaluateMethods'); })",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

                var (_, res) = await EvaluateOnCallFrame(id, "DebuggerTests.EvaluateStaticFieldsInStaticClass.StaticProperty2", expect_ok: false);
                AssertEqual("Failed to resolve member access for DebuggerTests.EvaluateStaticFieldsInStaticClass.StaticProperty2", res.Error["result"]?["description"]?.Value<string>(), "wrong error message");

                (_, res) = await EvaluateOnCallFrame(id, "DebuggerTests.InvalidEvaluateStaticClass.StaticProperty2", expect_ok: false);
                AssertEqual("Failed to resolve member access for DebuggerTests.InvalidEvaluateStaticClass.StaticProperty2", res.Error["result"]?["description"]?.Value<string>(), "wrong error message");
            });

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task AsyncLocalsInContinueWithBlock() => await CheckInspectLocalsAtBreakpointSite(
           "DebuggerTests.AsyncTests.ContinueWithTests", "ContinueWithStaticAsync", 4, "DebuggerTests.AsyncTests.ContinueWithTests.ContinueWithStaticAsync.AnonymousMethod__3_0",
           "window.setTimeout(function() { invoke_static_method('[debugger-test] DebuggerTests.AsyncTests.ContinueWithTests:RunAsync'); })",
           wait_for_event_fn: async (pause_location) =>
           {
               var frame_locals = await GetProperties(pause_location["callFrames"][0]["callFrameId"].Value<string>());
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

               await EvaluateOnCallFrameAndCheck(id,
                   ($"t.Status", TEnum("System.Threading.Tasks.TaskStatus", "RanToCompletion")),
                   ($"  t.Status", TEnum("System.Threading.Tasks.TaskStatus", "RanToCompletion"))
               );

               await EvaluateOnCallFrameFail(id,
                   ("str", "ReferenceError"),
                   ("  str", "ReferenceError")
               );
           });

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task EvaluateConstantValueUsingRuntimeEvaluate() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateTestsClass", "EvaluateLocals", 9, "DebuggerTests.EvaluateTestsClass.EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateTestsClass:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var dt = new DateTime(2020, 1, 2, 3, 4, 5);
               await RuntimeEvaluateAndCheck(
                   ("15\n//comment as vs does\n", TNumber(15)),
                   ("15", TNumber(15)),
                   ("\"15\"\n//comment as vs does\n", TString("15")),
                   ("\"15\"", TString("15")));
           });

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData("EvaluateBrowsableClass", "TestEvaluateFieldsNone", "testFieldsNone", 10)]
        [InlineData("EvaluateBrowsableClass", "TestEvaluatePropertiesNone", "testPropertiesNone", 10)]
        [InlineData("EvaluateBrowsableStruct", "TestEvaluateFieldsNone", "testFieldsNone", 10)]
        [InlineData("EvaluateBrowsableStruct", "TestEvaluatePropertiesNone", "testPropertiesNone", 10)]
        [InlineData("EvaluateBrowsableClassStatic", "TestEvaluateFieldsNone", "testFieldsNone", 10)]
        [InlineData("EvaluateBrowsableClassStatic", "TestEvaluatePropertiesNone", "testPropertiesNone", 10)]
        [InlineData("EvaluateBrowsableStructStatic", "TestEvaluateFieldsNone", "testFieldsNone", 10)]
        [InlineData("EvaluateBrowsableStructStatic", "TestEvaluatePropertiesNone", "testPropertiesNone", 10)]
        [InlineData("EvaluateBrowsableNonAutoPropertiesClass", "TestEvaluatePropertiesNone", "testPropertiesNone", 5, true)]
        [InlineData("EvaluateBrowsableNonAutoPropertiesStruct", "TestEvaluatePropertiesNone", "testPropertiesNone", 5, true)]
        [InlineData("EvaluateBrowsableNonAutoPropertiesClassStatic", "TestEvaluatePropertiesNone", "testPropertiesNone", 5, true)]
        [InlineData("EvaluateBrowsableNonAutoPropertiesStructStatic", "TestEvaluatePropertiesNone", "testPropertiesNone", 5, true)]
        public async Task EvaluateBrowsableNone(
            string outerClassName, string className, string localVarName, int breakLine, bool allMembersAreProperties = false) => await CheckInspectLocalsAtBreakpointSite(
            $"DebuggerTests.{outerClassName}", "Evaluate", breakLine, $"DebuggerTests.{outerClassName}.Evaluate",
            $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] DebuggerTests.{outerClassName}:Evaluate'); 1 }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

                var (testNone, _) = await EvaluateOnCallFrame(id, localVarName);
                await CheckValue(testNone, TObject($"DebuggerTests.{outerClassName}.{className}"), nameof(testNone));
                var testNoneProps = await GetProperties(testNone["objectId"]?.Value<string>());

                if (allMembersAreProperties)
                    await CheckProps(testNoneProps, new
                    {
                        list = TGetter("list", TObject("System.Collections.Generic.List<int>", description: "Count = 2")),
                        array = TGetter("array", TObject("int[]", description: "int[2]")),
                        text = TGetter("text", TString("text")),
                        nullNone = TGetter("nullNone", TObject("bool[]", is_null: true)),
                        valueTypeEnum = TGetter("valueTypeEnum", TEnum("DebuggerTests.SampleEnum", "yes")),
                        sampleStruct = TGetter("sampleStruct", TObject("DebuggerTests.SampleStructure", description: "DebuggerTests.SampleStructure")),
                        sampleClass = TGetter("sampleClass", TObject("DebuggerTests.SampleClass", description: "DebuggerTests.SampleClass"))
                    }, "testNoneProps#1");
                else
                    await CheckProps(testNoneProps, new
                    {
                        list = TObject("System.Collections.Generic.List<int>", description: "Count = 2"),
                        array = TObject("int[]", description: "int[2]"),
                        text = TString("text"),
                        nullNone = TObject("bool[]", is_null: true),
                        valueTypeEnum = TEnum("DebuggerTests.SampleEnum", "yes"),
                        sampleStruct = TObject("DebuggerTests.SampleStructure", description: "DebuggerTests.SampleStructure"),
                        sampleClass = TObject("DebuggerTests.SampleClass", description: "DebuggerTests.SampleClass")
                    }, "testNoneProps#1");
            });

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData("EvaluateBrowsableClass", "TestEvaluateFieldsNever", "testFieldsNever", 10)]
        [InlineData("EvaluateBrowsableClass", "TestEvaluatePropertiesNever", "testPropertiesNever", 10)]
        [InlineData("EvaluateBrowsableStruct", "TestEvaluateFieldsNever", "testFieldsNever", 10)]
        [InlineData("EvaluateBrowsableStruct", "TestEvaluatePropertiesNever", "testPropertiesNever", 10)]
        [InlineData("EvaluateBrowsableClassStatic", "TestEvaluateFieldsNever", "testFieldsNever", 10)]
        [InlineData("EvaluateBrowsableClassStatic", "TestEvaluatePropertiesNever", "testPropertiesNever", 10)]
        [InlineData("EvaluateBrowsableStructStatic", "TestEvaluateFieldsNever", "testFieldsNever", 10)]
        [InlineData("EvaluateBrowsableStructStatic", "TestEvaluatePropertiesNever", "testPropertiesNever", 10)]
        [InlineData("EvaluateBrowsableNonAutoPropertiesClass", "TestEvaluatePropertiesNever", "testPropertiesNever", 5)]
        [InlineData("EvaluateBrowsableNonAutoPropertiesStruct", "TestEvaluatePropertiesNever", "testPropertiesNever", 5)]
        [InlineData("EvaluateBrowsableNonAutoPropertiesClassStatic", "TestEvaluatePropertiesNever", "testPropertiesNever", 5)]
        [InlineData("EvaluateBrowsableNonAutoPropertiesStructStatic", "TestEvaluatePropertiesNever", "testPropertiesNever", 5)]
        public async Task EvaluateBrowsableNever(string outerClassName, string className, string localVarName, int breakLine) => await CheckInspectLocalsAtBreakpointSite(
            $"DebuggerTests.{outerClassName}", "Evaluate", breakLine, $"DebuggerTests.{outerClassName}.Evaluate",
            $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] DebuggerTests.{outerClassName}:Evaluate'); 1 }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

                var (testNever, _) = await EvaluateOnCallFrame(id, localVarName);
                await CheckValue(testNever, TObject($"DebuggerTests.{outerClassName}.{className}"), nameof(testNever));
                var testNeverProps = await GetProperties(testNever["objectId"]?.Value<string>());
                await CheckProps(testNeverProps, new
                {
                }, "testNeverProps#1");
            });

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData("EvaluateBrowsableClass", "TestEvaluateFieldsCollapsed", "testFieldsCollapsed", 10)]
        [InlineData("EvaluateBrowsableClass", "TestEvaluatePropertiesCollapsed", "testPropertiesCollapsed", 10)]
        [InlineData("EvaluateBrowsableStruct", "TestEvaluateFieldsCollapsed", "testFieldsCollapsed", 10)]
        [InlineData("EvaluateBrowsableStruct", "TestEvaluatePropertiesCollapsed", "testPropertiesCollapsed", 10)]
        [InlineData("EvaluateBrowsableClassStatic", "TestEvaluateFieldsCollapsed", "testFieldsCollapsed", 10)]
        [InlineData("EvaluateBrowsableClassStatic", "TestEvaluatePropertiesCollapsed", "testPropertiesCollapsed", 10)]
        [InlineData("EvaluateBrowsableStructStatic", "TestEvaluateFieldsCollapsed", "testFieldsCollapsed", 10)]
        [InlineData("EvaluateBrowsableStructStatic", "TestEvaluatePropertiesCollapsed", "testPropertiesCollapsed", 10)]
        [InlineData("EvaluateBrowsableNonAutoPropertiesClass", "TestEvaluatePropertiesCollapsed", "testPropertiesCollapsed", 5, true)]
        [InlineData("EvaluateBrowsableNonAutoPropertiesStruct", "TestEvaluatePropertiesCollapsed", "testPropertiesCollapsed", 5, true)]
        [InlineData("EvaluateBrowsableNonAutoPropertiesClassStatic", "TestEvaluatePropertiesCollapsed", "testPropertiesCollapsed", 5, true)]
        [InlineData("EvaluateBrowsableNonAutoPropertiesStructStatic", "TestEvaluatePropertiesCollapsed", "testPropertiesCollapsed", 5, true)]
        public async Task EvaluateBrowsableCollapsed(
            string outerClassName, string className, string localVarName, int breakLine, bool allMembersAreProperties = false) => await CheckInspectLocalsAtBreakpointSite(
            $"DebuggerTests.{outerClassName}", "Evaluate", breakLine, $"DebuggerTests.{outerClassName}.Evaluate",
            $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] DebuggerTests.{outerClassName}:Evaluate'); 1 }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

                var (testCollapsed, _) = await EvaluateOnCallFrame(id, localVarName);
                await CheckValue(testCollapsed, TObject($"DebuggerTests.{outerClassName}.{className}"), nameof(testCollapsed));
                var testCollapsedProps = await GetProperties(testCollapsed["objectId"]?.Value<string>());
                if (allMembersAreProperties)
                    await CheckProps(testCollapsedProps, new
                    {
                        listCollapsed = TGetter("listCollapsed", TObject("System.Collections.Generic.List<int>", description: "Count = 2")),
                        arrayCollapsed = TGetter("arrayCollapsed", TObject("int[]", description: "int[2]")),
                        textCollapsed = TGetter("textCollapsed", TString("textCollapsed")),
                        nullCollapsed = TGetter("nullCollapsed", TObject("bool[]", is_null: true)),
                        valueTypeEnumCollapsed = TGetter("valueTypeEnumCollapsed", TEnum("DebuggerTests.SampleEnum", "yes")),
                        sampleStructCollapsed = TGetter("sampleStructCollapsed", TObject("DebuggerTests.SampleStructure", description: "DebuggerTests.SampleStructure")),
                        sampleClassCollapsed = TGetter("sampleClassCollapsed", TObject("DebuggerTests.SampleClass", description: "DebuggerTests.SampleClass"))
                    }, "testCollapsedProps#1");
                else
                    await CheckProps(testCollapsedProps, new
                    {
                        listCollapsed = TObject("System.Collections.Generic.List<int>", description: "Count = 2"),
                        arrayCollapsed = TObject("int[]", description: "int[2]"),
                        textCollapsed = TString("textCollapsed"),
                        nullCollapsed = TObject("bool[]", is_null: true),
                        valueTypeEnumCollapsed = TEnum("DebuggerTests.SampleEnum", "yes"),
                        sampleStructCollapsed = TObject("DebuggerTests.SampleStructure", description: "DebuggerTests.SampleStructure"),
                        sampleClassCollapsed = TObject("DebuggerTests.SampleClass", description: "DebuggerTests.SampleClass")
                    }, "testCollapsedProps#1");
            });

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData("EvaluateBrowsableClass", "TestEvaluateFieldsRootHidden", "testFieldsRootHidden", 10)]
        [InlineData("EvaluateBrowsableClass", "TestEvaluatePropertiesRootHidden", "testPropertiesRootHidden", 10)]
        [InlineData("EvaluateBrowsableStruct", "TestEvaluateFieldsRootHidden", "testFieldsRootHidden", 10)]
        [InlineData("EvaluateBrowsableStruct", "TestEvaluatePropertiesRootHidden", "testPropertiesRootHidden", 10)]
        [InlineData("EvaluateBrowsableClassStatic", "TestEvaluateFieldsRootHidden", "testFieldsRootHidden", 10)]
        [InlineData("EvaluateBrowsableClassStatic", "TestEvaluatePropertiesRootHidden", "testPropertiesRootHidden", 10)]
        [InlineData("EvaluateBrowsableStructStatic", "TestEvaluateFieldsRootHidden", "testFieldsRootHidden", 10)]
        [InlineData("EvaluateBrowsableStructStatic", "TestEvaluatePropertiesRootHidden", "testPropertiesRootHidden", 10)]
        [InlineData("EvaluateBrowsableNonAutoPropertiesClass", "TestEvaluatePropertiesRootHidden", "testPropertiesRootHidden", 5)]
        [InlineData("EvaluateBrowsableNonAutoPropertiesStruct", "TestEvaluatePropertiesRootHidden", "testPropertiesRootHidden", 5)]
        [InlineData("EvaluateBrowsableNonAutoPropertiesClassStatic", "TestEvaluatePropertiesRootHidden", "testPropertiesRootHidden", 5)]
        [InlineData("EvaluateBrowsableNonAutoPropertiesStructStatic", "TestEvaluatePropertiesRootHidden", "testPropertiesRootHidden", 5)]
        public async Task EvaluateBrowsableRootHidden(
            string outerClassName, string className, string localVarName, int breakLine) => await CheckInspectLocalsAtBreakpointSite(
            $"DebuggerTests.{outerClassName}", "Evaluate", breakLine, $"DebuggerTests.{outerClassName}.Evaluate",
            $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] DebuggerTests.{outerClassName}:Evaluate'); 1 }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

                var (testRootHidden, _) = await EvaluateOnCallFrame(id, localVarName);
                await CheckValue(testRootHidden, TObject($"DebuggerTests.{outerClassName}.{className}"), nameof(testRootHidden));
                var testRootHiddenProps = await GetProperties(testRootHidden["objectId"]?.Value<string>());

                var (refList, _) = await EvaluateOnCallFrame(id, "testPropertiesNone.list");
                var refListProp = await GetProperties(refList["objectId"]?.Value<string>());
                var list = refListProp
                    .Where(v => v["name"]?.Value<string>() == "Items" || v["name"]?.Value<string>() == "_items")
                    .FirstOrDefault();
                var refListElementsProp = await GetProperties(list["value"]["objectId"]?.Value<string>());

                var (refArray, _) = await EvaluateOnCallFrame(id, "testPropertiesNone.array");
                var refArrayProp = await GetProperties(refArray["objectId"]?.Value<string>());

                var (refStruct, _) = await EvaluateOnCallFrame(id, "testPropertiesNone.sampleStruct");
                var refStructProp = await GetProperties(refStruct["objectId"]?.Value<string>());

                var (refClass, _) = await EvaluateOnCallFrame(id, "testPropertiesNone.sampleClass");
                var refClassProp = await GetProperties(refClass["objectId"]?.Value<string>());

                int refItemsCnt = refListElementsProp.Count() + refArrayProp.Count() + refStructProp.Count() + refClassProp.Count();
                Assert.Equal(refItemsCnt, testRootHiddenProps.Count());

                //in Console App names are in []
                //adding variable name to make elements unique
                foreach (var item in refListElementsProp)
                {
                    item["name"] = string.Concat("listRootHidden[", item["name"], "]");
                    CheckContainsJObject(testRootHiddenProps, item, item["name"].Value<string>());
                }
                foreach (var item in refArrayProp)
                {
                    item["name"] = string.Concat("arrayRootHidden[", item["name"], "]");
                    CheckContainsJObject(testRootHiddenProps, item, item["name"].Value<string>());
                }

                // valuetype/class members unique names are created by concatenation with a dot
                foreach (var item in refStructProp)
                {
                    item["name"] = string.Concat("sampleStructRootHidden.", item["name"]);
                    CheckContainsJObject(testRootHiddenProps, item, item["name"].Value<string>());
                }
                foreach (var item in refClassProp)
                {
                    item["name"] = string.Concat("sampleClassRootHidden.", item["name"]);
                    CheckContainsJObject(testRootHiddenProps, item, item["name"].Value<string>());
                }
            });

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task EvaluateStaticAttributeInAssemblyNotRelatedButLoaded() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateTestsClass", "EvaluateLocals", 9, "DebuggerTests.EvaluateTestsClass.EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateTestsClass:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               await RuntimeEvaluateAndCheck(
                   ("ClassToBreak.valueToCheck", TNumber(10)));
           });

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task EvaluateLocalObjectFromAssemblyNotRelatedButLoaded()
         => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateTestsClass", "EvaluateLocalsFromAnotherAssembly", 5, "DebuggerTests.EvaluateTestsClass.EvaluateLocalsFromAnotherAssembly",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateTestsClass:EvaluateLocalsFromAnotherAssembly'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               await RuntimeEvaluateAndCheck(
                   ("a.valueToCheck", TNumber(20)));
           });

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task StructureGetters() =>  await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.StructureGetters", "Evaluate", 2, $"DebuggerTests.StructureGetters.Evaluate",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.StructureGetters:Evaluate'); })",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                var (obj, _) = await EvaluateOnCallFrame(id, "s");
                var props = await GetProperties(obj["objectId"]?.Value<string>());
                await CheckProps(props, new
                {
                    Id = TGetter("Id", TNumber(123))
                }, "s#1");
            });

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task EvaluateMethodWithDefaultParam() => await CheckInspectLocalsAtBreakpointSite(
            $"DebuggerTests.DefaultParamMethods", "Evaluate", 2, "DebuggerTests.DefaultParamMethods.Evaluate",
            $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] DebuggerTests.DefaultParamMethods:Evaluate'); 1 }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                await EvaluateOnCallFrameAndCheck(id,
                   ("test.GetByte()", TNumber(1)),
                   ("test.GetSByte()", TNumber(1)),
                   ("test.GetByteNullable()", TNumber(1)),
                   ("test.GetSByteNullable()", TNumber(1)),

                   ("test.GetInt16()", TNumber(1)),
                   ("test.GetUInt16()", TNumber(1)),
                   ("test.GetInt16Nullable()", TNumber(1)),
                   ("test.GetUInt16Nullable()", TNumber(1)),

                   ("test.GetInt32()", TNumber(1)),
                   ("test.GetUInt32()", TNumber(1)),
                   ("test.GetInt32Nullable()", TNumber(1)),
                   ("test.GetUInt32Nullable()", TNumber(1)),

                   ("test.GetInt64()", TNumber(1)),
                   ("test.GetUInt64()", TNumber(1)),
                   ("test.GetInt64Nullable()", TNumber(1)),
                   ("test.GetUInt64Nullable()", TNumber(1)),

                   ("test.GetChar()", TChar('T')),
                   ("test.GetCharNullable()", TChar('T')),
                   ("test.GetUnicodeChar()", TChar('ą')),

                   ("test.GetString()", TString("1.23")),
                   ("test.GetUnicodeString()", TString("żółć")),
                   ("test.GetString(null)", TString(null)),
                   ("test.GetStringNullable()", TString("1.23")),

                   ("test.GetSingle()", TNumber("1.23", isDecimal: true)),
                   ("test.GetDouble()",  TNumber("1.23", isDecimal: true)),
                   ("test.GetSingleNullable()",  TNumber("1.23", isDecimal: true)),
                   ("test.GetDoubleNullable()",  TNumber("1.23", isDecimal: true)),

                   ("test.GetBool()", TBool(true)),
                   ("test.GetBoolNullable()", TBool(true)),
                   ("test.GetNull()", TBool(true)),

                   ("test.GetDefaultAndRequiredParam(2)", TNumber(5)),
                   ("test.GetDefaultAndRequiredParam(3, 2)", TNumber(5)),
                   ("test.GetDefaultAndRequiredParamMixedTypes(\"a\")", TString("a; -1; False")),
                   ("test.GetDefaultAndRequiredParamMixedTypes(\"a\", 23)", TString("a; 23; False")),
                   ("test.GetDefaultAndRequiredParamMixedTypes(\"a\", 23, true)", TString("a; 23; True"))
                   );

                var (_, res) = await EvaluateOnCallFrame(id, "test.GetDefaultAndRequiredParamMixedTypes(\"a\", 23, true, 1.23f)", expect_ok: false);
                AssertEqual("Unable to evaluate method 'GetDefaultAndRequiredParamMixedTypes'. Too many arguments passed.",
                    res.Error["result"]["description"]?.Value<string>(), "wrong error message");
            });

        [Fact]
        public async Task EvaluateMethodWithLinq() => await CheckInspectLocalsAtBreakpointSite(
            $"DebuggerTests.DefaultParamMethods", "Evaluate", 2, "DebuggerTests.DefaultParamMethods.Evaluate",
            $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] DebuggerTests.DefaultParamMethods:Evaluate'); 1 }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                await EvaluateOnCallFrameAndCheck(id,
                   ("test.listToLinq.ToList()", TObject("System.Collections.Generic.List<int>", description: "Count = 11"))
                   );
            });

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task EvaluateNullObjectPropertiesPositive() => await CheckInspectLocalsAtBreakpointSite(
            $"DebuggerTests.EvaluateNullableProperties", "Evaluate", 11, "DebuggerTests.EvaluateNullableProperties.Evaluate",
            $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] DebuggerTests.EvaluateNullableProperties:Evaluate'); 1 }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

                // we have no way of returning int? for null values,
                // so we return the last non-null class name
                await EvaluateOnCallFrameAndCheck(id,
                   ("list.Count", TNumber(1)),
                   ("list!.Count", TNumber(1)),
                   ("list?.Count", TNumber(1)),
                   ("listNull", TObject("System.Collections.Generic.List<int>", is_null: true)),
                   ("listNull?.Count", TObject("System.Collections.Generic.List<int>", is_null: true)),
                   ("tc?.MemberList?.Count", TNumber(2)),
                   ("tc!.MemberList?.Count", TNumber(2)),
                   ("tc!.MemberList!.Count", TNumber(2)),
                   ("tc?.MemberListNull?.Count", TObject("System.Collections.Generic.List<int>", is_null: true)),
                   ("tc.MemberListNull?.Count", TObject("System.Collections.Generic.List<int>", is_null: true)),
                   ("tcNull?.MemberListNull?.Count", TObject("DebuggerTests.EvaluateNullableProperties.TestClass", is_null: true)),
                   ("str!.Length", TNumber(9)),
                   ("str?.Length", TNumber(9)),
                   ("str_null?.Length", TObject("string", is_null: true))
                );
            });

        [Fact]
        public async Task EvaluateNullObjectPropertiesNegative() => await CheckInspectLocalsAtBreakpointSite(
            $"DebuggerTests.EvaluateNullableProperties", "Evaluate", 6, "DebuggerTests.EvaluateNullableProperties.Evaluate",
            $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] DebuggerTests.EvaluateNullableProperties:Evaluate'); 1 }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                await CheckEvaluateFail(id,
                    ("list.Count.x", "Cannot find member 'x' on a primitive type"),
                    ("listNull.Count", GetNullReferenceErrorOn("\"Count\"")),
                    ("listNull!.Count", GetNullReferenceErrorOn("\"Count\"")),
                    ("tcNull.MemberListNull.Count", GetNullReferenceErrorOn("\"MemberListNull\"")),
                    ("tc.MemberListNull.Count", GetNullReferenceErrorOn("\"Count\"")),
                    ("tcNull?.MemberListNull.Count", GetNullReferenceErrorOn("\"Count\"")),
                    ("listNull?.Count.NonExistingProperty", GetNullReferenceErrorOn("\"NonExistingProperty\"")),
                    ("tc?.MemberListNull! .Count", GetNullReferenceErrorOn("\"Count\"")),
                    ("tc?. MemberListNull!.Count", GetNullReferenceErrorOn("\"Count\"")),
                    ("tc?.MemberListNull.Count", GetNullReferenceErrorOn("\"Count\"")),
                    ("tc! .MemberListNull!.Count", GetNullReferenceErrorOn("\"Count\"")),
                    ("tc!.MemberListNull. Count", GetNullReferenceErrorOn("\"Count\"")),
                    ("tcNull?.Sibling.MemberListNull?.Count", GetNullReferenceErrorOn("\"MemberListNull?\"")),
                    ("listNull?", "Expected expression."),
                    ("listNull!.Count", GetNullReferenceErrorOn("\"Count\"")),
                    ("x?.p", "Operation '?' not allowed on primitive type - 'x?'"),
                    ("str_null.Length", GetNullReferenceErrorOn("\"Length\"")),
                    ("str_null!.Length", GetNullReferenceErrorOn("\"Length\""))
                );

                string GetNullReferenceErrorOn(string name) => $"Expression threw NullReferenceException trying to access {name} on a null-valued object.";
            });

        [Fact]
        public async Task EvaluateMethodsOnPrimitiveTypesReturningPrimitives() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.PrimitiveTypeMethods", "Evaluate", 11, "DebuggerTests.PrimitiveTypeMethods.Evaluate",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.PrimitiveTypeMethods:Evaluate'); })",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                await EvaluateOnCallFrameAndCheck(id,
                    ("test.propInt.ToString()", TString("12")),
                    ("test.propUint.ToString()", TString("12")),
                    ("test.propLong.ToString()", TString("12")),
                    ("test.propUlong.ToString()", TString("12")),
                    ("test.propBool.ToString()", TString("True")),
                    ("test.propChar.ToString()", TString("X")),
                    ("test.propString.ToString()", TString("s_t_r")),
                    ("test.propString.EndsWith('r')", TBool(true)),
                    ("test.propString.StartsWith('S')", TBool(false)),
                    ("localInt.ToString()", TString("2")),
                    ("localUint.ToString()", TString("2")),
                    ("localLong.ToString()", TString("2")),
                    ("localUlong.ToString()", TString("2")),
                    ("localBool.ToString()", TString("False")),
                    ("localBool.GetHashCode()", TNumber(0)),
                    ("localBool.GetTypeCode()", TObject("System.TypeCode", "Boolean")),
                    ("localChar.ToString()", TString("Y")),
                    ("localString.ToString()", TString("S*T*R")),
                    ("localString.EndsWith('r')", TBool(false)),
                    ("localString.StartsWith('S')", TBool(true))
                );
             });

        [Fact]
        public async Task EvaluateMethodsOnPrimitiveTypesReturningPrimitivesCultureDependant() =>
            await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.PrimitiveTypeMethods", "Evaluate", 11, "DebuggerTests.PrimitiveTypeMethods.Evaluate",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.PrimitiveTypeMethods:Evaluate'); })",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                var (floatMemberVal, _) = await EvaluateOnCallFrame(id, "test.propFloat");
                var (doubleMemberVal, _) = await EvaluateOnCallFrame(id, "test.propDouble");
                var (floatLocalVal, _) = await EvaluateOnCallFrame(id, "localFloat");
                var (doubleLocalVal, _) = await EvaluateOnCallFrame(id, "localDouble");

                // expected value depends on the debugger's user culture and is equal to
                // description of the number that also respects user's culture settings
                await EvaluateOnCallFrameAndCheck(id,
                    ("test.propFloat.ToString()", TString(floatMemberVal["description"]?.Value<string>())),
                    ("test.propDouble.ToString()", TString(doubleMemberVal["description"]?.Value<string>())),

                    ("localFloat.ToString()", TString(floatLocalVal["description"]?.Value<string>())),
                    ("localDouble.ToString()", TString(doubleLocalVal["description"]?.Value<string>())));
             });

        [Fact]
        public async Task EvaluateMethodsOnPrimitiveTypesReturningObjects() =>  await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.PrimitiveTypeMethods", "Evaluate", 11, "DebuggerTests.PrimitiveTypeMethods.Evaluate",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.PrimitiveTypeMethods:Evaluate'); })",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

                var (res, _) = await EvaluateOnCallFrame(id, "test.propString.Split('_', 3, System.StringSplitOptions.TrimEntries)");
                var props = await GetProperties(res["objectId"]?.Value<string>());
                var expected_props = new[] { TString("s"), TString("t"), TString("r") };
                await CheckProps(props, expected_props, "props#1");

                (res, _) = await EvaluateOnCallFrame(id, "localString.Split('*', 3, System.StringSplitOptions.RemoveEmptyEntries)");
                props = await GetProperties(res["objectId"]?.Value<string>());
                expected_props = new[] { TString("S"), TString("T"), TString("R") };
                await CheckProps(props, expected_props, "props#2");
            });

        [Theory]
        [InlineData("DefaultMethod", "IDefaultInterface", "Evaluate")]
        [InlineData("DefaultMethod2", "IExtendIDefaultInterface", "Evaluate")]
        [InlineData("DefaultMethodAsync", "IDefaultInterface", "EvaluateAsync", true)]
        public async Task EvaluateLocalsInDefaultInterfaceMethod(string pauseMethod, string methodInterface, string entryMethod, bool isAsync = false) =>
            await CheckInspectLocalsAtBreakpointSite(
            methodInterface, pauseMethod, 2, methodInterface + "." + pauseMethod,
            $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] DefaultInterfaceMethod:{entryMethod}'); }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                await EvaluateOnCallFrameAndCheck(id,
                    ("localString", TString($"{pauseMethod}()")),
                    ("this", TObject("DIMClass")),
                    ("this.dimClassMember", TNumber(123)));
            });

        [Theory]
        [InlineData("DefaultMethodStatic", "IDefaultInterface", "EvaluateStatic")]
        [InlineData("DefaultMethodAsyncStatic", "IDefaultInterface", "EvaluateAsyncStatic", true)]
        public async Task EvaluateLocalsInDefaultInterfaceMethodStatic(string pauseMethod, string methodInterface, string entryMethod, bool isAsync = false) =>
            await CheckInspectLocalsAtBreakpointSite(
            methodInterface, pauseMethod, 2, methodInterface + "." + pauseMethod,
            $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] DefaultInterfaceMethod:{entryMethod}'); }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                await EvaluateOnCallFrameAndCheck(id,
                    ("localString", TString($"{pauseMethod}()")),
                    ("IDefaultInterface.defaultInterfaceMember", TString("defaultInterfaceMember")),
                    ("defaultInterfaceMember", TString("defaultInterfaceMember"))
                );
            });

        [Fact]
        public async Task EvaluateStringProperties() => await CheckInspectLocalsAtBreakpointSite(
             $"DebuggerTests.TypeProperties", "Run", 3, "DebuggerTests.TypeProperties.Run",
            $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] DebuggerTests.TypeProperties:Run'); 1 }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                await EvaluateOnCallFrameAndCheck(id,
                   ("localString.Length", TNumber(5)),
                   ("localString[1]", TChar('B')),
                   ("instance.str.Length", TNumber(5)),
                   ("instance.str[3]", TChar('c'))
                );
           });
    }
}
