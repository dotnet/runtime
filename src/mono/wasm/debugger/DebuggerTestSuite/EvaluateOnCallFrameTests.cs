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
    public class EvaluateOnCallFrameTests : DebuggerTests
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

        public static IEnumerable<object[]> EvaluateStaticClassFromStaticMethodTestData(string type_name)
        {
            yield return new object[] { type_name, "EvaluateAsyncMethods", "EvaluateAsyncMethods", true };
            yield return new object[] { type_name, "EvaluateMethods", "EvaluateMethods", false };
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
               Console.WriteLine ($"------- test running the bits..");
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

               Console.WriteLine ($"------- test done!");
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

        [ConditionalFact(nameof(RunningOnChrome))]
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
            "DebuggerTests.GetPropertiesTests.DerivedClass", "InstanceMethod", 1, "InstanceMethod",
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
            "DebuggerTests.EvaluateTestsClass.TestEvaluate", "run", 9, "run",
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

        [ConditionalTheory(nameof(RunningOnChrome))]
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
            "DebuggerTests.EvaluateTestsClass.TestEvaluate", "run", 9, "run",
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

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task NegativeTestsInStaticMethod() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateTestsClass", "EvaluateLocals", 9, "EvaluateLocals",
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
            "DebuggerTests.EvaluateTestsClassWithProperties", "InstanceMethod", /*line_offset*/1, "InstanceMethod",
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
            "DebuggerTests.EvaluateMethodTestsClass.TestEvaluate", "run", 9, "run",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateMethodTestsClass:EvaluateMethods'); })",
            wait_for_event_fn: async (pause_location) =>
           {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

                var (_, res) = await EvaluateOnCallFrame(id, "this.objToTest.MyMethodWrong()", expect_ok: false );
                Assert.Contains($"Method 'MyMethodWrong' not found", res.Error["message"]?.Value<string>());

                (_, res) = await EvaluateOnCallFrame(id, "this.objToTest.MyMethod(1)", expect_ok: false);
                Assert.Contains("Cannot invoke method 'this.objToTest.MyMethod(1)' - too many arguments passed", res.Error["message"]?.Value<string>());

                (_, res) = await EvaluateOnCallFrame(id, "this.CallMethodWithParm(\"1\")", expect_ok: false );
                Assert.Contains("Unable to evaluate method 'this.CallMethodWithParm(\"1\")'", res.Error["message"]?.Value<string>());

                (_, res) = await EvaluateOnCallFrame(id, "this.ParmToTestObjNull.MyMethod()", expect_ok: false );
                Assert.Contains("Expression 'this.ParmToTestObjNull.MyMethod' evaluated to null", res.Error["message"]?.Value<string>());

                (_, res) = await EvaluateOnCallFrame(id, "this.ParmToTestObjException.MyMethod()", expect_ok: false );
                Assert.Contains("Cannot invoke method 'MyMethod'", res.Error["message"]?.Value<string>());
           });

        [Fact]
        public async Task EvaluateSimpleMethodCallsWithoutParms() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateMethodTestsClass.TestEvaluate", "run", 9, "run",
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
            "DebuggerTests.EvaluateMethodTestsClass.TestEvaluate", "run", 9, "run",
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
            "DebuggerTests.EvaluateMethodTestsClass.TestEvaluate", "run", 9, "run",
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

        [Fact]
        public async Task EvaluateExpressionsWithElementAccessByConstant() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateLocalsWithElementAccessTests", "EvaluateLocals", 5, "EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithElementAccessTests:EvaluateLocals'); })",
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
        public async Task EvaluateExpressionsWithElementAccessByLocalVariable() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateLocalsWithElementAccessTests", "EvaluateLocals", 5, "EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithElementAccessTests:EvaluateLocals'); })",
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
        public async Task EvaluateExpressionsWithElementAccessByMemberVariables() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateLocalsWithElementAccessTests", "EvaluateLocals", 5, "EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithElementAccessTests:EvaluateLocals'); })",
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
        public async Task EvaluateExpressionsWithElementAccessNested() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateLocalsWithElementAccessTests", "EvaluateLocals", 5, "EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithElementAccessTests:EvaluateLocals'); })",
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
        public async Task EvaluateExpressionsWithElementAccessMultidimentional() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateLocalsWithElementAccessTests", "EvaluateLocals", 5, "EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithElementAccessTests:EvaluateLocals'); })",
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
            "DebuggerTests.EvaluateMethodTestsClass.TestEvaluate", "run", 9, "run",
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

        [Fact]
        public async Task EvaluateStaticClass() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateMethodTestsClass.TestEvaluate", "run", 9, "run",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateMethodTestsClass:EvaluateMethods'); })",
            wait_for_event_fn: async (pause_location) =>
           {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

                var frame = pause_location["callFrames"][0];

                await EvaluateOnCallFrameAndCheck(id,
                    ("DebuggerTests.EvaluateStaticClass.StaticField1", TNumber(10)));
                await EvaluateOnCallFrameAndCheck(id,
                    ("DebuggerTests.EvaluateStaticClass.StaticProperty1", TString("StaticProperty1")));
                await EvaluateOnCallFrameAndCheck(id,
                    ("DebuggerTests.EvaluateStaticClass.StaticPropertyWithError", TString("System.Exception: not implemented")));
           });

        [Theory]
        [MemberData(nameof(EvaluateStaticClassFromStaticMethodTestData), parameters: "DebuggerTests.EvaluateMethodTestsClass")]
        // [MemberData(nameof(EvaluateStaticClassFromStaticMethodTestData), parameters: "EvaluateMethodTestsClass")]
        public async Task EvaluateStaticClassFromStaticMethod(string type, string method, string bp_function_name, bool is_async)
        => await CheckInspectLocalsAtBreakpointSite(
            type, method, 1, bp_function_name,
            $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] {type}:{method}'); }})",
            wait_for_event_fn: async (pause_location) =>
           {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

                var frame = pause_location["callFrames"][0];

                await EvaluateOnCallFrameAndCheck(id,
                    ("EvaluateStaticClass.StaticField1", TNumber(10)),
                    ("EvaluateStaticClass.StaticProperty1", TString("StaticProperty1")),
                    ("EvaluateStaticClass.StaticPropertyWithError", TString("System.Exception: not implemented")),
                    ("DebuggerTests.EvaluateStaticClass.StaticField1", TNumber(10)),
                    ("DebuggerTests.EvaluateStaticClass.StaticProperty1", TString("StaticProperty1")),
                    ("DebuggerTests.EvaluateStaticClass.StaticPropertyWithError", TString("System.Exception: not implemented")));
           });

        [Fact]
        public async Task EvaluateNonStaticClassWithStaticFields() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateMethodTestsClass", "EvaluateAsyncMethods", 3, "EvaluateAsyncMethods",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateMethodTestsClass:EvaluateAsyncMethods'); })",
            wait_for_event_fn: async (pause_location) =>
           {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

                var frame = pause_location["callFrames"][0];

                await EvaluateOnCallFrameAndCheck(id,
                    ("DebuggerTests.EvaluateNonStaticClassWithStaticFields.StaticField1", TNumber(10)),
                    ("DebuggerTests.EvaluateNonStaticClassWithStaticFields.StaticProperty1", TString("StaticProperty1")),
                    ("DebuggerTests.EvaluateNonStaticClassWithStaticFields.StaticPropertyWithError", TString("System.Exception: not implemented")),
                    ("EvaluateNonStaticClassWithStaticFields.StaticField1", TNumber(10)),
                    ("EvaluateNonStaticClassWithStaticFields.StaticProperty1", TString("StaticProperty1")),
                    ("EvaluateNonStaticClassWithStaticFields.StaticPropertyWithError", TString("System.Exception: not implemented")));
           });

        [Fact]
        public async Task EvaluateStaticClassesNested() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateMethodTestsClass", "EvaluateMethods", 3, "EvaluateMethods",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateMethodTestsClass:EvaluateMethods'); })",
            wait_for_event_fn: async (pause_location) =>
           {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

                var frame = pause_location["callFrames"][0];

                await EvaluateOnCallFrameAndCheck(id,
                    ("DebuggerTests.EvaluateStaticClass.NestedClass1.NestedClass2.NestedClass3.StaticField1", TNumber(3)),
                    ("DebuggerTests.EvaluateStaticClass.NestedClass1.NestedClass2.NestedClass3.StaticProperty1", TString("StaticProperty3")),
                    ("DebuggerTests.EvaluateStaticClass.NestedClass1.NestedClass2.NestedClass3.StaticPropertyWithError", TString("System.Exception: not implemented 3")),
                    ("EvaluateStaticClass.NestedClass1.NestedClass2.NestedClass3.StaticField1", TNumber(3)),
                    ("EvaluateStaticClass.NestedClass1.NestedClass2.NestedClass3.StaticProperty1", TString("StaticProperty3")),
                    ("EvaluateStaticClass.NestedClass1.NestedClass2.NestedClass3.StaticPropertyWithError", TString("System.Exception: not implemented 3")));
           });

        [Fact]
        public async Task EvaluateStaticClassesNestedWithNoNamespace() => await CheckInspectLocalsAtBreakpointSite(
            "NoNamespaceClass", "EvaluateMethods", 1, "EvaluateMethods",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] NoNamespaceClass:EvaluateMethods'); })",
            wait_for_event_fn: async (pause_location) =>
           {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

                var frame = pause_location["callFrames"][0];

                await EvaluateOnCallFrameAndCheck(id,
                    ("NoNamespaceClass.NestedClass1.NestedClass2.NestedClass3.StaticField1", TNumber(30)),
                    ("NoNamespaceClass.NestedClass1.NestedClass2.NestedClass3.StaticProperty1", TString("StaticProperty30")),
                    ("NoNamespaceClass.NestedClass1.NestedClass2.NestedClass3.StaticPropertyWithError", TString("System.Exception: not implemented 30")));
           });

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task EvaluateStaticClassesFromDifferentNamespaceInDifferentFrames() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTestsV2.EvaluateStaticClass", "Run", 1, "Run",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateMethodTestsClass:EvaluateMethods'); })",
            wait_for_event_fn: async (pause_location) =>
            {
                var id_top = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                var frame = pause_location["callFrames"][0];

                await EvaluateOnCallFrameAndCheck(id_top,
                    ("EvaluateStaticClass.StaticField1", TNumber(20)),
                    ("EvaluateStaticClass.StaticProperty1", TString("StaticProperty2")),
                    ("EvaluateStaticClass.StaticPropertyWithError", TString("System.Exception: not implemented")));

                var id_second = pause_location["callFrames"][1]["callFrameId"].Value<string>();

                await EvaluateOnCallFrameAndCheck(id_second,
                    ("EvaluateStaticClass.StaticField1", TNumber(10)),
                    ("EvaluateStaticClass.StaticProperty1", TString("StaticProperty1")),
                    ("EvaluateStaticClass.StaticPropertyWithError", TString("System.Exception: not implemented")));
           });

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task EvaluateStaticClassInvalidField() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateMethodTestsClass.TestEvaluate", "run", 9, "run",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateMethodTestsClass:EvaluateMethods'); })",
            wait_for_event_fn: async (pause_location) =>
           {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

                var frame = pause_location["callFrames"][0];

                var (_, res) = await EvaluateOnCallFrame(id, "DebuggerTests.EvaluateStaticClass.StaticProperty2", expect_ok: false);
                AssertEqual("Failed to resolve member access for DebuggerTests.EvaluateStaticClass.StaticProperty2", res.Error["result"]?["description"]?.Value<string>(), "wrong error message");

                (_, res) = await EvaluateOnCallFrame(id, "DebuggerTests.InvalidEvaluateStaticClass.StaticProperty2", expect_ok: false);
                AssertEqual("Failed to resolve member access for DebuggerTests.InvalidEvaluateStaticClass.StaticProperty2", res.Error["result"]?["description"]?.Value<string>(), "wrong error message");
           });

         [ConditionalFact(nameof(RunningOnChrome))]
         public async Task AsyncLocalsInContinueWithBlock() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.AsyncTests.ContinueWithTests", "ContinueWithStaticAsync", 4, "<ContinueWithStaticAsync>b__3_0",
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
            "DebuggerTests.EvaluateTestsClass", "EvaluateLocals", 9, "EvaluateLocals",
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
        [InlineData("EvaluateBrowsableProperties", "TestEvaluateFieldsNone", "testFieldsNone", 10)]
        [InlineData("EvaluateBrowsableProperties", "TestEvaluatePropertiesNone", "testPropertiesNone", 10)]
        [InlineData("EvaluateBrowsableCustomProperties", "TestEvaluatePropertiesNone", "testPropertiesNone", 5, true)]
        public async Task EvaluateBrowsableNone(string outerClassName, string className, string localVarName, int breakLine, bool isCustomGetter = false) => await CheckInspectLocalsAtBreakpointSite(
            $"DebuggerTests.{outerClassName}", "Evaluate", breakLine, "Evaluate",
            $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] DebuggerTests.{outerClassName}:Evaluate'); 1 }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

                var (testNone, _) = await EvaluateOnCallFrame(id, localVarName);
                await CheckValue(testNone, TObject($"DebuggerTests.{outerClassName}.{className}"), nameof(testNone));
                var testNoneProps = await GetProperties(testNone["objectId"]?.Value<string>());

                if (isCustomGetter)
                    await CheckProps(testNoneProps, new
                    {
                        list = TGetter("list", TObject("System.Collections.Generic.List<int>", description: "Count = 2")),
                        array = TGetter("array", TObject("int[]", description: "int[2]")),
                        text = TGetter("text", TString("text"))
                    }, "testNoneProps#1");
                else
                    await CheckProps(testNoneProps, new
                    {
                        list = TObject("System.Collections.Generic.List<int>", description: "Count = 2"),
                        array = TObject("int[]", description: "int[2]"),
                        text = TString("text")
                    }, "testNoneProps#1");
           });

        [Theory]
        [InlineData("EvaluateBrowsableProperties", "TestEvaluateFieldsNever", "testFieldsNever", 10)]
        [InlineData("EvaluateBrowsableProperties", "TestEvaluatePropertiesNever", "testPropertiesNever", 10)]
        [InlineData("EvaluateBrowsableStaticProperties", "TestEvaluateFieldsNever", "testFieldsNever", 10)]
        [InlineData("EvaluateBrowsableStaticProperties", "TestEvaluatePropertiesNever", "testPropertiesNever", 10)]
        [InlineData("EvaluateBrowsableCustomProperties", "TestEvaluatePropertiesNever", "testPropertiesNever", 5)]
        public async Task EvaluateBrowsableNever(string outerClassName, string className, string localVarName, int breakLine) => await CheckInspectLocalsAtBreakpointSite(
            $"DebuggerTests.{outerClassName}", "Evaluate", breakLine, "Evaluate",
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
        [InlineData("EvaluateBrowsableProperties", "TestEvaluateFieldsCollapsed", "testFieldsCollapsed", 10)]
        [InlineData("EvaluateBrowsableProperties", "TestEvaluatePropertiesCollapsed", "testPropertiesCollapsed", 10)]
        [InlineData("EvaluateBrowsableStaticProperties", "TestEvaluateFieldsCollapsed", "testFieldsCollapsed", 10)]
        [InlineData("EvaluateBrowsableStaticProperties", "TestEvaluatePropertiesCollapsed", "testPropertiesCollapsed", 10)]
        [InlineData("EvaluateBrowsableCustomProperties", "TestEvaluatePropertiesCollapsed", "testPropertiesCollapsed", 5, true)]
        public async Task EvaluateBrowsableCollapsed(string outerClassName, string className, string localVarName, int breakLine, bool isCustomGetter = false) => await CheckInspectLocalsAtBreakpointSite(
            $"DebuggerTests.{outerClassName}", "Evaluate", breakLine, "Evaluate",
            $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] DebuggerTests.{outerClassName}:Evaluate'); 1 }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

                var (testCollapsed, _) = await EvaluateOnCallFrame(id, localVarName);
                await CheckValue(testCollapsed, TObject($"DebuggerTests.{outerClassName}.{className}"), nameof(testCollapsed));
                var testCollapsedProps = await GetProperties(testCollapsed["objectId"]?.Value<string>());
                if (isCustomGetter)
                    await CheckProps(testCollapsedProps, new
                    {
                        listCollapsed = TGetter("listCollapsed", TObject("System.Collections.Generic.List<int>", description: "Count = 2")),
                        arrayCollapsed = TGetter("arrayCollapsed", TObject("int[]", description: "int[2]")),
                        textCollapsed = TGetter("textCollapsed", TString("textCollapsed"))
                    }, "testCollapsedProps#1");
                else
                    await CheckProps(testCollapsedProps, new
                    {
                        listCollapsed = TObject("System.Collections.Generic.List<int>", description: "Count = 2"),
                        arrayCollapsed = TObject("int[]", description: "int[2]"),
                        textCollapsed = TString("textCollapsed")
                    }, "testCollapsedProps#1");
           });

        [Theory]
        [InlineData("EvaluateBrowsableProperties", "TestEvaluateFieldsRootHidden", "testFieldsRootHidden", 10)]
        [InlineData("EvaluateBrowsableProperties", "TestEvaluatePropertiesRootHidden", "testPropertiesRootHidden", 10)]
        [InlineData("EvaluateBrowsableStaticProperties", "TestEvaluateFieldsRootHidden", "testFieldsRootHidden", 10)]
        [InlineData("EvaluateBrowsableStaticProperties", "TestEvaluatePropertiesRootHidden", "testPropertiesRootHidden", 10)]
        [InlineData("EvaluateBrowsableCustomProperties", "TestEvaluatePropertiesRootHidden", "testPropertiesRootHidden", 5, true)]
        public async Task EvaluateBrowsableRootHidden(string outerClassName, string className, string localVarName, int breakLine, bool isCustomGetter = false) => await CheckInspectLocalsAtBreakpointSite(
            $"DebuggerTests.{outerClassName}", "Evaluate", breakLine, "Evaluate",
            $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] DebuggerTests.{outerClassName}:Evaluate'); 1 }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

                var (testRootHidden, _) = await EvaluateOnCallFrame(id, localVarName);
                await CheckValue(testRootHidden, TObject($"DebuggerTests.{outerClassName}.{className}"), nameof(testRootHidden));
                var testRootHiddenProps = await GetProperties(testRootHidden["objectId"]?.Value<string>());
                var (refList, _) = await EvaluateOnCallFrame(id, "testPropertiesNone.list");
                var refListProp = await GetProperties(refList["objectId"]?.Value<string>());
                var refListElementsProp = await GetProperties(refListProp[0]["value"]["objectId"]?.Value<string>());
                var (refArray, _) = await EvaluateOnCallFrame(id, "testPropertiesNone.array");
                var refArrayProp = await GetProperties(refArray["objectId"]?.Value<string>());

                //in Console App names are in []
                //adding variable name to make elements unique
                foreach (var item in refArrayProp)
                {
                    item["name"] = string.Concat("arrayRootHidden[", item["name"], "]");
                }
                foreach (var item in refListElementsProp)
                {
                    item["name"] = string.Concat("listRootHidden[", item["name"], "]");
                }
                var mergedRefItems = new JArray(refListElementsProp.Union(refArrayProp));
                Assert.Equal(mergedRefItems, testRootHiddenProps);
           });

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task EvaluateStaticAttributeInAssemblyNotRelatedButLoaded() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateTestsClass", "EvaluateLocals", 9, "EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateTestsClass:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               await RuntimeEvaluateAndCheck(
                   ("ClassToBreak.valueToCheck", TNumber(10)));
           });

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task EvaluateLocalObjectFromAssemblyNotRelatedButLoaded()
         => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateTestsClass", "EvaluateLocalsFromAnotherAssembly", 5, "EvaluateLocalsFromAnotherAssembly",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateTestsClass:EvaluateLocalsFromAnotherAssembly'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               await RuntimeEvaluateAndCheck(
                   ("a.valueToCheck", TNumber(20)));
           });

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task EvaluateProtectionLevels() =>  await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.GetPropertiesTests.DerivedClass", "InstanceMethod", 1, "InstanceMethod",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.GetPropertiesTests.DerivedClass:run'); })",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                var (obj, _) = await EvaluateOnCallFrame(id, "this");
                var (pub, internalAndProtected, priv) = await GetPropertiesSortedByProtectionLevels(obj["objectId"]?.Value<string>());

                await CheckProps(pub, new
                {
                    a = TNumber(4),
                    Base_AutoStringPropertyForOverrideWithField = TString("DerivedClass#Base_AutoStringPropertyForOverrideWithField"),
                    Base_GetterForOverrideWithField = TString("DerivedClass#Base_GetterForOverrideWithField"),
                    BaseBase_MemberForOverride = TString("DerivedClass#BaseBase_MemberForOverride"),
                    DateTime = TGetter("DateTime", TDateTime(new DateTime(2200, 5, 6, 7, 18, 9))),
                    _DTProp = TGetter("_DTProp", TDateTime(new DateTime(2200, 5, 6, 7, 8, 9))),
                    FirstName = TGetter("FirstName", TString("DerivedClass#FirstName")),
                    _base_dateTime = TGetter("_base_dateTime", TDateTime(new DateTime(2134, 5, 7, 1, 9, 2))),
                    LastName = TGetter("LastName", TString("BaseClass#LastName"))
                }, "public");

                await CheckProps(internalAndProtected, new
                {
                    base_num = TNumber(5)
                }, "internalAndProtected");

                await CheckProps(priv, new
                {
                    _stringField = TString("DerivedClass#_stringField"),
                    _dateTime = TDateTime(new DateTime(2020, 7, 6, 5, 4, 3)),
                    AutoStringProperty = TString("DerivedClass#AutoStringProperty"),
                    StringPropertyForOverrideWithAutoProperty = TString("DerivedClass#StringPropertyForOverrideWithAutoProperty"),
                    _base_name = TString("private_name"),
                    Base_AutoStringProperty = TString("base#Base_AutoStringProperty"),
                    DateTimeForOverride = TGetter("DateTimeForOverride", TDateTime(new DateTime(2190, 9, 7, 5, 3, 2)))
                }, "private");
           });

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task StructureGetters() =>  await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.StructureGetters", "Evaluate", 2, "Evaluate",
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
            $"DebuggerTests.DefaultParamMethods", "Evaluate", 2, "Evaluate",
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
                   ("test.GetString(null)", TObject("string", is_null: true)),
                   ("test.GetStringNullable()", TString("1.23")),

                   ("test.GetSingle()", JObject.FromObject( new { type = "number", value = 1.23, description = "1.23" })),
                   ("test.GetDouble()", JObject.FromObject( new { type = "number", value = 1.23, description = "1.23" })),
                   ("test.GetSingleNullable()", JObject.FromObject( new { type = "number", value = 1.23, description = "1.23" })),
                   ("test.GetDoubleNullable()", JObject.FromObject( new { type = "number", value = 1.23, description = "1.23" })),

                   ("test.GetBool()", JObject.FromObject( new { type = "object", value = true, description = "True", className = "System.Boolean" })),
                   ("test.GetBoolNullable()", JObject.FromObject( new { type = "object", value = true, description = "True", className = "System.Boolean" })),
                   ("test.GetNull()", JObject.FromObject( new { type = "object", value = true, description = "True", className = "System.Boolean" })),

                   ("test.GetDefaultAndRequiredParam(2)", TNumber(5)),
                   ("test.GetDefaultAndRequiredParam(3, 2)", TNumber(5)),
                   ("test.GetDefaultAndRequiredParamMixedTypes(\"a\")", TString("a; -1; False")),
                   ("test.GetDefaultAndRequiredParamMixedTypes(\"a\", 23)", TString("a; 23; False")),
                   ("test.GetDefaultAndRequiredParamMixedTypes(\"a\", 23, true)", TString("a; 23; True"))
                   );

                var (_, res) = await EvaluateOnCallFrame(id, "test.GetDefaultAndRequiredParamMixedTypes(\"a\", 23, true, 1.23f)", expect_ok: false);
                Assert.Contains("method 'test.GetDefaultAndRequiredParamMixedTypes(\"a\", 23, true, 1.23f)' - too many arguments passed", res.Error["message"]?.Value<string>());
           });

        [Fact]
        public async Task EvaluateMethodWithLinq() => await CheckInspectLocalsAtBreakpointSite(
            $"DebuggerTests.DefaultParamMethods", "Evaluate", 2, "Evaluate",
            $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] DebuggerTests.DefaultParamMethods:Evaluate'); 1 }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                await EvaluateOnCallFrameAndCheck(id,
                   ("test.listToLinq.ToList()", TObject("System.Collections.Generic.List<int>", description: "Count = 11"))
                   );
           });
    }

}
