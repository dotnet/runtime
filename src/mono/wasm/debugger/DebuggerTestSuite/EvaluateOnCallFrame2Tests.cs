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
    public class EvaluateOnCallFrame2Tests : DebuggerTests
    {
        public EvaluateOnCallFrame2Tests(ITestOutputHelper testOutput) : base(testOutput)
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
                var id_prev = pause_location["callFrames"][1]["callFrameId"].Value<string>();

                var (_, res) = await EvaluateOnCallFrame(id, "DebuggerTests.EvaluateStaticFieldsInStaticClass.StaticProperty2", expect_ok: false);
                AssertEqual("Failed to resolve member access for DebuggerTests.EvaluateStaticFieldsInStaticClass.StaticProperty2", res.Error["result"]?["description"]?.Value<string>(), "wrong error message");
                var exceptionDetailsStack = res.Error["exceptionDetails"]?["stackTrace"]?["callFrames"]?[0];
                Assert.Equal("DebuggerTests.EvaluateMethodTestsClass.TestEvaluate.run", exceptionDetailsStack?["functionName"]?.Value<string>());
                Assert.Equal(358, exceptionDetailsStack?["lineNumber"]?.Value<int>());
                Assert.Equal(16, exceptionDetailsStack?["columnNumber"]?.Value<int>());
                (_, res) = await EvaluateOnCallFrame(id_prev, "DebuggerTests.EvaluateStaticFieldsInStaticClass.StaticProperty2", expect_ok: false);
                exceptionDetailsStack = res.Error["exceptionDetails"]?["stackTrace"]?["callFrames"]?[0];
                AssertEqual("Failed to resolve member access for DebuggerTests.EvaluateStaticFieldsInStaticClass.StaticProperty2", res.Error["result"]?["description"]?.Value<string>(), "wrong error message");
                Assert.Equal("DebuggerTests.EvaluateMethodTestsClass.EvaluateMethods", exceptionDetailsStack?["functionName"]?.Value<string>());
                Assert.Equal(422, exceptionDetailsStack?["lineNumber"]?.Value<int>());
                Assert.Equal(12, exceptionDetailsStack?["columnNumber"]?.Value<int>());
                (_, res) = await EvaluateOnCallFrame(id, "DebuggerTests.InvalidEvaluateStaticClass.StaticProperty2", expect_ok: false);
                AssertEqual("Failed to resolve member access for DebuggerTests.InvalidEvaluateStaticClass.StaticProperty2", res.Error["result"]?["description"]?.Value<string>(), "wrong error message");
            });

        [ConditionalFact(nameof(WasmSingleThreaded), nameof(RunningOnChrome))]
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

        // [ConditionalTheory(nameof(RunningOnChrome))]
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

                JObject[] expectedTestRootHiddenProps = new[]
                {
                    JObject.FromObject(new { value = TNumber(1), name = "listRootHidden[0]"}),
                    JObject.FromObject(new { value = TNumber(2), name = "listRootHidden[1]"}),
                    JObject.FromObject(new { value = TNumber(11), name = "arrayRootHidden[0]"}),
                    JObject.FromObject(new { value = TNumber(22), name = "arrayRootHidden[1]"}),
                    JObject.FromObject(new { value = TNumber(100), name = "sampleStructRootHidden.Id"}),
                    JObject.FromObject(new { value = TBool(true), name = "sampleStructRootHidden.IsStruct"}),
                    JObject.FromObject(new { value = TNumber(200), name = "sampleClassRootHidden.ClassId"}),
                    JObject.FromObject(new { value = TObject("System.Collections.Generic.List<string>", description: "Count = 1"), name = "sampleClassRootHidden.Items"})
                };
                await CheckProps(testRootHiddenProps, expectedTestRootHiddenProps, "listRootHidden");
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
                   ("test.GetUnicodeChar()", TChar('\u0105')),

                   ("test.GetString()", TString("1.23")),
                   ("test.GetUnicodeString()", TString("\u017C\u00F3\u0142\u0107")),
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
        
        [Fact]
        public async Task EvaluateStaticGetterInValueType() => await CheckInspectLocalsAtBreakpointSite(
             $"DebuggerTests.TypeProperties", "Run", 3, "DebuggerTests.TypeProperties.Run",
            $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] DebuggerTests.TypeProperties:Run'); 1 }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                await EvaluateOnCallFrameAndCheck(id,
                   ("EvaluateStaticGetterInValueType.A", TNumber(5))
                );
           });

        [Fact]
        public async Task EvaluateSumBetweenObjectAndString() => await CheckInspectLocalsAtBreakpointSite(
             $"DebuggerTests.SumObjectAndString", "run", 7, "DebuggerTests.SumObjectAndString.run",
            $"window.setTimeout(function() {{ invoke_static_method ('[debugger-test] DebuggerTests.SumObjectAndString:run'); 1 }})",
            wait_for_event_fn: async (pause_location) =>
            {
                var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
                await EvaluateOnCallFrameAndCheck(id,
                   ("myList+\"asd\"", TString("System.Collections.Generic.List`1[System.Int32]asd")),
                   ("dt+\"asd\"", TString("1/1/0001 12:00:00 AMasd")),
                   ("myClass+\"asd\"", TString("OverridenToStringasd")),
                   ("listNull+\"asd\"", TString("asd"))
                );
                await CheckEvaluateFail(id,
                    ("myClass+dt", "Cannot evaluate '(myClass+dt\n)': (3,9): error CS0019: Operator '+' cannot be applied to operands of type 'object' and 'object'"),
                    ("myClass+1", "Cannot evaluate '(myClass+1\n)': (2,9): error CS0019: Operator '+' cannot be applied to operands of type 'object' and 'int'"),
                    ("dt+1", "Cannot evaluate '(dt+1\n)': (2,9): error CS0019: Operator '+' cannot be applied to operands of type 'object' and 'int'")
                );
           });

        [Fact]
        public async Task EvaluateObjectIndexingMultidimensional() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateLocalsWithIndexingTests", "EvaluateLocals", 12, "DebuggerTests.EvaluateLocalsWithIndexingTests.EvaluateLocals",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateLocalsWithIndexingTests:EvaluateLocals'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               var id = pause_location["callFrames"][0]["callFrameId"].Value<string>();
               await EvaluateOnCallFrameAndCheck(id,
                   ("f[j, aDouble]", TNumber("3.34")), //only IdentifierNameSyntaxes
                   ("f[1, aDouble]", TNumber("3.34")), //IdentifierNameSyntax with LiteralExpressionSyntax
                   ("f[aChar, \"&\", longString]", TString("9-&-longString")),
                   ("f[f.numArray[j], aDouble]", TNumber("4.34")), //ElementAccessExpressionSyntax
                   ("f[f.numArray[j], f.numArray[0]]", TNumber("3")), //multiple ElementAccessExpressionSyntaxes
                   ("f[f.numArray[f.numList[0]], f.numArray[i]]", TNumber("3")),
                   ("f[f.numArray[f.numList[0]], f.numArray[f.numArray[i]]]", TNumber("4"))
                ); 
           });

        [Fact]
        public async Task EvaluateValueTypeWithFixedArrayAndMoreFields() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateValueTypeWithFixedArray", "run", 3, "DebuggerTests.EvaluateValueTypeWithFixedArray.run",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateValueTypeWithFixedArray:run'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               await RuntimeEvaluateAndCheck(
                   ("myVar.MyMethod()", TNumber(13)),
                   ("myVar.myIntArray[0]", TNumber(1)),
                   ("myVar.myIntArray[1]", TNumber(2)),
                   ("myVar.myCharArray[2]", TChar('a')));
           });

        [Fact]
        public async Task EvaluateValueTypeWithObjectValueType() => await CheckInspectLocalsAtBreakpointSite(
            "DebuggerTests.EvaluateValueTypeWithObjectValueType", "run", 3, "DebuggerTests.EvaluateValueTypeWithObjectValueType.run",
            "window.setTimeout(function() { invoke_static_method ('[debugger-test] DebuggerTests.EvaluateValueTypeWithObjectValueType:run'); })",
            wait_for_event_fn: async (pause_location) =>
           {
               await RuntimeEvaluateAndCheck(
                   ("myVar.MyMethod()", TNumber(10)));
           });
    }
}
