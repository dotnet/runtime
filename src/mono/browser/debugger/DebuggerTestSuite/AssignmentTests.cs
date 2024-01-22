// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace DebuggerTests
{
    public class AssignmentTests : DebuggerTests
    {
        public AssignmentTests(ITestOutputHelper testOutput)
                : base(testOutput)
        {}

        public static TheoryData<string, JObject, JObject, string> GetTestData => new()
        {
            { "MONO_TYPE_OBJECT",      TObject("object", is_null: true),                        TObject("object"),                                      "DebuggerTests.StepInTest<object>.TestedMethod"},
            { "MONO_TYPE_CLASS",       TObject("DebuggerTests.MONO_TYPE_CLASS", is_null: true), TObject("DebuggerTests.MONO_TYPE_CLASS"),               "DebuggerTests.StepInTest<DebuggerTests.MONO_TYPE_CLASS>.TestedMethod"},
            { "MONO_TYPE_BOOLEAN",     TBool(default),                                          TBool(true),                                            "DebuggerTests.StepInTest<bool>.TestedMethod"},
            { "MONO_TYPE_CHAR",        TChar('\u0000'),                                         TChar('a'),                                             "DebuggerTests.StepInTest<char>.TestedMethod"},
            { "MONO_TYPE_STRING",      TString(default),                                        TString("hello"),                                       "DebuggerTests.StepInTest<string>.TestedMethod"},

            // [ActiveIssue("https://github.com/dotnet/runtime/issues/64188")]
            // { "MONO_TYPE_ENUM",        TEnum("DebuggerTests.RGB", "Red"),                       TEnum("DebuggerTests.RGB", "Blue"),                     "DebuggerTests.StepInTest<DebuggerTests.RGB>.TestedMethod"},

            { "MONO_TYPE_ARRAY",       TObject("byte[]", is_null: true),                        TArray("byte[]", "byte[2]"),                            "DebuggerTests.StepInTest<byte[]>.TestedMethod"},
            { "MONO_TYPE_VALUETYPE",   TValueType("DebuggerTests.Point"),                       TValueType("DebuggerTests.Point"),                      "DebuggerTests.StepInTest<DebuggerTests.Point>.TestedMethod"},
            { "MONO_TYPE_VALUETYPE2",  TValueType("System.Decimal","0"),                        TValueType("System.Decimal", "1.1"),                    "DebuggerTests.StepInTest<System.Decimal>.TestedMethod"},
            { "MONO_TYPE_GENERICINST", TObject("System.Func<int>", is_null: true),              TDelegate("System.Func<int>", "int Prepare ()"),        "DebuggerTests.StepInTest<System.Func<int>>.TestedMethod"},

            // Disabled due to https://github.com/dotnet/runtime/issues/65881
            //{ "MONO_TYPE_FNPTR",       TPointer("*()",  is_null: true),                         TPointer("*()") },

            { "MONO_TYPE_PTR",         TPointer("int*", is_null: true),                         TPointer("int*"),                                       "DebuggerTests.MONO_TYPE_PTR.TestedMethod"},
            { "MONO_TYPE_I1",          TNumber(0),                                              TNumber(-1),                                            "DebuggerTests.StepInTest<sbyte>.TestedMethod"},
            { "MONO_TYPE_I2",          TNumber(0),                                              TNumber(-1),                                            "DebuggerTests.StepInTest<int16>.TestedMethod"},
            { "MONO_TYPE_I4",          TNumber(0),                                              TNumber(-1),                                            "DebuggerTests.StepInTest<int>.TestedMethod"},
            { "MONO_TYPE_I8",          TNumber(0),                                              TNumber(-1),                                            "DebuggerTests.StepInTest<long>.TestedMethod"},
            { "MONO_TYPE_U1",          TNumber(0),                                              TNumber(1),                                             "DebuggerTests.StepInTest<byte>.TestedMethod"},
            { "MONO_TYPE_U2",          TNumber(0),                                              TNumber(1),                                             "DebuggerTests.StepInTest<uint16>.TestedMethod"},
            { "MONO_TYPE_U4",          TNumber(0),                                              TNumber(1),                                             "DebuggerTests.StepInTest<uint>.TestedMethod"},
            { "MONO_TYPE_U8",          TNumber(0),                                              TNumber(1),                                             "DebuggerTests.StepInTest<ulong>.TestedMethod"},
            { "MONO_TYPE_R4",          TNumber(0),                                              TNumber("3.1415"),                                      "DebuggerTests.StepInTest<single>.TestedMethod"},
            { "MONO_TYPE_R8",          TNumber(0),                                              TNumber("3.1415"),                                      "DebuggerTests.StepInTest<double>.TestedMethod"}
        };

        [ConditionalTheory(nameof(RunningOnChrome))]
        [MemberData("GetTestData")]
        public async Task InspectVariableBeforeAndAfterAssignment(string clazz, JObject checkDefault, JObject checkValue, string methodName)
        {
            await SetBreakpointInMethod("debugger-test", "DebuggerTests." + clazz, "Prepare", 2);
            await EvaluateAndCheck("window.setTimeout(function() { invoke_static_method('[debugger-test] DebuggerTests." + clazz + ":Prepare'); })", null, -1, -1, $"DebuggerTests.{clazz}.Prepare");

            // 1) check un-assigned variables
            await StepAndCheck(StepKind.Into, "dotnet://debugger-test.dll/debugger-assignment-test.cs", -1, -1, methodName,
                locals_fn: async (locals) =>
                {
                    int numLocals = locals.Count();
                    if (numLocals != 2)
                        throw new XunitException($"Expected two locals but got {numLocals}. Locals: {locals}");
                    await Check(locals, "r", checkDefault);
                    await Task.CompletedTask;
                }
            );

            // 2) check assigned variables
            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-assignment-test.cs", -1, -1, methodName, times: 3,
                locals_fn: async (locals) =>
                {
                    int numLocals = locals.Count();
                    if (numLocals != 2)
                        throw new XunitException($"Expected two locals but got {numLocals}. Locals: {locals}");
                    await Check(locals, "r", checkValue);
                    await Task.CompletedTask;
                }
            );
        }
    }
}
