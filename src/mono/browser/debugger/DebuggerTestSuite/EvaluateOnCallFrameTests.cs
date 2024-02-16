// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
                Assert.Equal("Expression 'this.ParmToTestObjNull.MyMethod' evaluated to null", res.Error["result"]?["description"]?.Value<string>());
                var exceptionDetailsStack = res.Error["exceptionDetails"]?["stackTrace"]?["callFrames"]?[0];
                Assert.Equal("DebuggerTests.EvaluateMethodTestsClass.TestEvaluate.run", exceptionDetailsStack?["functionName"]?.Value<string>());
                Assert.Equal(358, exceptionDetailsStack?["lineNumber"]?.Value<int>());
                Assert.Equal(16, exceptionDetailsStack?["columnNumber"]?.Value<int>());;

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
                    ("this.CallMethodWithParmString(\"\uD83D\uDEF6\")", TString("str_const_\uD83D\uDEF6")),
                    ("this.CallMethodWithParmString(\"\\uD83D\\uDEF6\")", TString("str_const_\uD83D\uDEF6")),
                    ("this.CallMethodWithParmString(\"\uD83D\uDE80\")", TString("str_const_\uD83D\uDE80")),
                    ("this.CallMethodWithParmString_\u03BB(\"\uD83D\uDE80\")", TString("\u03BB_\uD83D\uDE80")),
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
            "DebuggerTests.EvaluateLocalsWithIndexingTests", "EvaluateLocals", 6, "DebuggerTests.EvaluateLocalsWithIndexingTests.EvaluateLocals",
            $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithIndexingTests:EvaluateLocals'); 1 }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                var (_, res) = await EvaluateOnCallFrame(id, "cc.idx0[2]", expect_ok: false );
                Assert.Equal("Unable to evaluate element access 'cc.idx0[2]': Cannot apply indexing with [] to a primitive object of type 'number'", res.Error["result"]?["description"]?.Value<string>());
                var exceptionDetailsStack = res.Error["exceptionDetails"]?["stackTrace"]?["callFrames"]?[0];
                Assert.Equal("DebuggerTests.EvaluateLocalsWithIndexingTests.EvaluateLocals", exceptionDetailsStack?["functionName"]?.Value<string>());
                Assert.Equal(576, exceptionDetailsStack?["lineNumber"]?.Value<int>());
                Assert.Equal(12, exceptionDetailsStack?["columnNumber"]?.Value<int>());
                (_, res) = await EvaluateOnCallFrame(id, "c[1]", expect_ok: false );
                Assert.Equal( "Unable to evaluate element access 'c[1]': Cannot apply indexing with [] to an object of type 'DebuggerTests.EvaluateLocalsWithIndexingTests.ClassWithIndexers'", res.Error["result"]?["description"]?.Value<string>());
           });

        [Fact]
        public async Task EvaluateIndexingsByConstant() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateLocalsWithIndexingTests", "EvaluateLocals", 6, "DebuggerTests.EvaluateLocalsWithIndexingTests.EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithIndexingTests:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

               await EvaluateOnCallFrameAndCheck(id,
                   ("cc.numList[0]", TNumber(1)),
                   ("cc.textList[1]", TString("2")),
                   ("cc.numArray[1]", TNumber(2)),
                   ("cc.textArray[0]", TString("1")));
           });

        [Fact]
        public async Task EvaluateIndexingByLocalVariable() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateLocalsWithIndexingTests", "EvaluateLocals", 6, "DebuggerTests.EvaluateLocalsWithIndexingTests.EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithIndexingTests:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

               await EvaluateOnCallFrameAndCheck(id,
                   ("cc.numList[i]", TNumber(1)),
                   ("cc.textList[j]", TString("2")),
                   ("cc.numArray[j]", TNumber(2)),
                   ("cc.textArray[i]", TString("1")));
           });

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task EvaluateObjectIndexingByNonIntConst() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateLocalsWithIndexingTests", "EvaluateLocals", 6, "DebuggerTests.EvaluateLocalsWithIndexingTests.EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithIndexingTests:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                await EvaluateOnCallFrameAndCheck(id,
                    ("c[\"longstring\"]", TBool(true)), // class
                    ("c[\"-\"]", TBool(false)),
                    ("c[\'-\']", TString("res_-")),
                    ("c[true]", TString("True")),
                    ("c[1.23]", TNumber(1)),
                    ("s[\"longstring\"]", TBool(true)), // struct
                    ("s[\"-\"]", TBool(false)),
                    ("s[\'-\']", TString("res_-")),
                    ("s[true]", TString("True")),
                    ("s[1.23]", TNumber(1)),
                    ("cc.indexedByStr[\"1\"]", TBool(true)),
                    ("cc.indexedByStr[\"111\"]", TBool(false)),
                    ("cc.indexedByStr[\"true\"]", TBool(true)),
                    ("cc.indexedByChar[\'i\']", TString("I")),
                    ("cc.indexedByChar[\'5\']", TString("5")),
                    ("cc.indexedByBool[true]", TString("TRUE")),
                    ("cc.indexedByBool[false]", TString("FALSE"))
                );
                var (_, res) = await EvaluateOnCallFrame(id,"cc.indexedByStr[\"invalid\"]", expect_ok: false);
                Assert.True(res.Error["result"]?["description"]?.Value<string>().StartsWith("Cannot evaluate '(cc.indexedByStr[\"invalid\"]", StringComparison.Ordinal)); 
                (_, res) = await EvaluateOnCallFrame(id,"cc.indexedByStr[null]", expect_ok: false);
                Assert.True(res.Error["result"]?["description"]?.Value<string>().StartsWith("Cannot evaluate '(cc.indexedByStr[null]", StringComparison.Ordinal)); 
            });

        [Fact]
        public async Task EvaluateObjectByNonIntLocals() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateLocalsWithIndexingTests", "EvaluateLocals", 15, "DebuggerTests.EvaluateLocalsWithIndexingTests.EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithIndexingTests:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                await EvaluateOnCallFrameAndCheck(id,
                    ("c[longString]", TBool(true)),
                    ("c[aBool]", TString("True")),
                    ("c[aChar]", TString("res_9")),
                    ("c[shortString]", TBool(false)),
                    ("c[aFloat]", TNumber(1)),
                    ("c[aDouble]", TNumber(2)),
                    ("c[aDecimal]", TNumber(3)),
                    ("c[arr]", TChar('t')),
                    ("c[objIdx]", TNumber(123)),
                    ("s[longString]", TBool(true)),
                    ("s[aBool]", TString("True")),
                    ("s[aChar]", TString("res_9")),
                    ("s[shortString]", TBool(false)),
                    ("s[aFloat]", TNumber(1)),
                    ("s[aDouble]", TNumber(2)),
                    ("s[aDecimal]", TNumber(3)),
                    ("s[arr]", TChar('t')),
                    ("s[objIdx]", TNumber(123))
                );
            });

        [Fact]
        public async Task EvaluateNestedObjectIndexingByNonIntLocals() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateLocalsWithIndexingTests", "EvaluateLocals", 12, "DebuggerTests.EvaluateLocalsWithIndexingTests.EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithIndexingTests:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                await EvaluateOnCallFrameAndCheck(id,
                    ("c[cc.textArray[0]]", TBool(false)), // c["1"]
                    ("c[cc.textArray[j]]", TBool(false)), // c["2"]
                    ("s[cc.textArray[0]]", TBool(false)), // s["1"]
                    ("s[cc.textArray[j]]", TBool(false)) // s["2"]
                );
            });

        // ToDo: https://github.com/dotnet/runtime/issues/76015
        [Fact]
        public async Task EvaluateIndexingByExpression() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateLocalsWithIndexingTests", "EvaluateLocals", 6, "DebuggerTests.EvaluateLocalsWithIndexingTests.EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithIndexingTests:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                await EvaluateOnCallFrameAndCheck(id,
                    ("cc.numList[i + 1]", TNumber(2)),
                    ("cc.textList[(2 * j) - 1]", TString("2")),
                    ("cc.textList[j - 1]", TString("1")),
                    ("cc.numArray[cc.numList[j - 1]]", TNumber(2))
                );
            });

        [Fact]
        public async Task EvaluateIndexingByExpressionMultidimensional() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateLocalsWithMultidimensionalIndexingTests", "EvaluateLocals", 5, "DebuggerTests.EvaluateLocalsWithMultidimensionalIndexingTests.EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithMultidimensionalIndexingTests:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                await EvaluateOnCallFrameAndCheck(id,
                    ("f.numArray2D[0, j - 1]", TNumber(1)), // 0, 0
                    ("f.numArray2D[f.idx1, i + j]", TNumber(4)), // 1, 1
                    ("f.numArray2D[(f.idx1 - j) * 5, i + j]", TNumber(2)), // 0, 1
                    ("f.numArray2D[i + j, f.idx1 - 1]", TNumber(3)) // 1, 0
                );
            });

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task EvaluateIndexingByExpressionNegative() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateLocalsWithIndexingTests", "EvaluateLocals", 6, "DebuggerTests.EvaluateLocalsWithIndexingTests.EvaluateLocals",
            $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithIndexingTests:EvaluateLocals'); 1 }})",
            wait_for_event_fn: async (pause_location) =>
            {
                // indexing with expression of a wrong type
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                var (_, res) = await EvaluateOnCallFrame(id, "cc.numList[\"a\" + 1]", expect_ok: false );
                Assert.Equal("Unable to evaluate element access 'cc.numList[\"a\" + 1]': Cannot index with an object of type 'string'", res.Error["result"]?["description"]?.Value<string>());
                var exceptionDetailsStack = res.Error["exceptionDetails"]?["stackTrace"]?["callFrames"]?[0];
                Assert.Equal("DebuggerTests.EvaluateLocalsWithIndexingTests.EvaluateLocals", exceptionDetailsStack?["functionName"]?.Value<string>());
                Assert.Equal(576, exceptionDetailsStack?["lineNumber"]?.Value<int>());
                Assert.Equal(12, exceptionDetailsStack?["columnNumber"]?.Value<int>());
            });

        [ConditionalFact(nameof(RunningOnChrome))]
        public async Task EvaluateIndexingByExpressionContainingUnknownIdentifier() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateLocalsWithIndexingTests", "EvaluateLocals", 6, "DebuggerTests.EvaluateLocalsWithIndexingTests.EvaluateLocals",
            $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithIndexingTests:EvaluateLocals'); 1 }})",
            wait_for_event_fn: async (pause_location) =>
            {
                // indexing with expression of a wrong type
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                var (_, res) = await EvaluateOnCallFrame(id, "cc.numList[\"a\" + x]", expect_ok: false);
                Assert.Equal("The name x does not exist in the current context", res.Error["result"]?["description"]?.Value<string>());
            });

        [Fact]
        public async Task EvaluateIndexingByMemberVariables() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateLocalsWithIndexingTests", "EvaluateLocals", 6, "DebuggerTests.EvaluateLocalsWithIndexingTests.EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithIndexingTests:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

               await EvaluateOnCallFrameAndCheck(id,
                   ("cc.idx0", TNumber(0)),
                   ("cc.idx1", TNumber(1)),
                   ("cc.numList[cc.idx0]", TNumber(1)),
                   ("cc.textList[cc.idx1]", TString("2")),
                   ("cc.numArray[cc.idx1]", TNumber(2)),
                   ("cc.textArray[cc.idx0]", TString("1")));
           });

        [Fact]
        public async Task EvaluateIndexingNested() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateLocalsWithIndexingTests", "EvaluateLocals", 6, "DebuggerTests.EvaluateLocalsWithIndexingTests.EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithIndexingTests:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

               await EvaluateOnCallFrameAndCheck(id,
                   ("cc.idx0", TNumber(0)),
                   ("cc.numList[cc.numList[cc.idx0]]", TNumber(2)),
                   ("cc.textList[cc.numList[cc.idx0]]", TString("2")),
                   ("cc.numArray[cc.numArray[cc.idx0]]", TNumber(2)),
                   ("cc.textArray[cc.numArray[cc.idx0]]", TString("2")));

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
            "DebuggerTests.EvaluateLocalsWithIndexingTests", "EvaluateLocals", 6, "DebuggerTests.EvaluateLocalsWithIndexingTests.EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithIndexingTests:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();

               await EvaluateOnCallFrameAndCheck(id,
                   ("j", TNumber(1)),
                   ("cc.idx1", TNumber(1)),
                   ("cc.numArrayOfArrays[1][1]", TNumber(2)),
                   ("cc.numArrayOfArrays[j][j]", TNumber(2)),
                   ("cc.numArrayOfArrays[cc.idx1][cc.idx1]", TNumber(2)),
                   ("cc.numListOfLists[1][1]", TNumber(2)),
                   ("cc.numListOfLists[j][j]", TNumber(2)),
                   ("cc.numListOfLists[cc.idx1][cc.idx1]", TNumber(2)),
                   ("cc.textArrayOfArrays[1][1]", TString("2")),
                   ("cc.textArrayOfArrays[j][j]", TString("2")),
                   ("cc.textArrayOfArrays[cc.idx1][cc.idx1]", TString("2")),
                   ("cc.textListOfLists[1][1]", TString("2")),
                   ("cc.textListOfLists[j][j]", TString("2")),
                   ("cc.textListOfLists[cc.idx1][cc.idx1]", TString("2")),
                   ("cc.numArrayOfArrays[cc.numArray[cc.numList[1]]][cc.numList[0]]", TNumber(2))
                   );

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

        [ConditionalTheory(nameof(RunningOnChrome))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EvaluateMethodWithBPWhilePausedInADifferentMethodAndNotHit(bool setBreakpointBeforePause)
        {
            await cli.SendCommand("DotnetDebugger.setEvaluationOptions", JObject.FromObject(new { options = new { noFuncEval = false } }), token);
            var waitForScript = WaitForConsoleMessage("console.warning: MONO_WASM: Adding an id (0) that already exists in commands_received");
            if (setBreakpointBeforePause)
                await SetBreakpointInMethod("debugger-test.dll", "TestEvaluateDontPauseOnBreakpoint", "MyMethod2", 1);
            await CheckInspectLocalsAtBreakpointSite(
            "TestEvaluateDontPauseOnBreakpoint", "run", 3, "TestEvaluateDontPauseOnBreakpoint.run",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] TestEvaluateDontPauseOnBreakpoint:run'); })",
            wait_for_event_fn: async (pause_location) =>
           {
                if (!setBreakpointBeforePause)
                    await SetBreakpointInMethod("debugger-test.dll", "TestEvaluateDontPauseOnBreakpoint", "MyMethod2", 1);
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                await EvaluateOnCallFrameAndCheck(id,
                    ("myVar.MyMethod2()", TString("Object 11")),
                    ("myVar.MyMethod3()", TString("Object 11")),
                    ("myVar.MyCount", TString("Object 11")),
                    ("myVar.MyMethod()", TString("Object 10")),
                    ("myVar", TObject("TestEvaluateDontPauseOnBreakpoint", description: "Object 11")));
                var props = await GetObjectOnFrame(pause_location["callFrames"][0], "myVar");
                await CheckString(props, "MyCount", "Object 11");
           });
           await SendCommandAndCheck(null, "Debugger.resume", null, 0, 0,  "TestEvaluateDontPauseOnBreakpoint.MyMethod2");
           await SendCommandAndCheck(null, "Debugger.resume", null, 0, 0,  "TestEvaluateDontPauseOnBreakpoint.MyMethod");
           Assert.False(waitForScript.IsCompleted);
        }
    }
}
