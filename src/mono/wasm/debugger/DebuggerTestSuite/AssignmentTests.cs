// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DebuggerTests
{
    public class AssignmentTests : DebuggerTestBase
    {
        public static TheoryData<string, JObject, JObject> GetTestData => new TheoryData<string, JObject, JObject>
        {
            { "MONO_TYPE_OBJECT",      TObject("object", is_null: true),                        TObject("object") },
            { "MONO_TYPE_CLASS",       TObject("DebuggerTests.MONO_TYPE_CLASS", is_null: true), TObject("DebuggerTests.MONO_TYPE_CLASS") },
            { "MONO_TYPE_BOOLEAN",     TBool(default),                                          TBool(true) },
            { "MONO_TYPE_CHAR",        TSymbol("0 '\u0000'"),                                   TSymbol("97 'a'") },
            { "MONO_TYPE_STRING",      TString(default),                                        TString("hello") },
            { "MONO_TYPE_ENUM",        TEnum("DebuggerTests.RGB", "Red"),                       TEnum("DebuggerTests.RGB", "Blue") },
            { "MONO_TYPE_ARRAY",       TObject("byte[]", is_null: true),                        TArray("byte[]", 2) },
            { "MONO_TYPE_VALUETYPE",   TValueType("DebuggerTests.Point"),                       TValueType("DebuggerTests.Point") },
            { "MONO_TYPE_VALUETYPE2",  TValueType("System.Decimal","0"),                        TValueType("System.Decimal", "1.1") },
            { "MONO_TYPE_GENERICINST", TObject("System.Func<int>", is_null: true),              TDelegate("System.Func<int>", "int Prepare ()") },
            { "MONO_TYPE_FNPTR",       TPointer("*()",  is_null: true),                         TPointer("*()") },
            { "MONO_TYPE_PTR",         TPointer("int*", is_null: true),                         TPointer("int*") },
            { "MONO_TYPE_I1",          TNumber(0),                                              TNumber(-1) },
            { "MONO_TYPE_I2",          TNumber(0),                                              TNumber(-1) },
            { "MONO_TYPE_I4",          TNumber(0),                                              TNumber(-1) },
            { "MONO_TYPE_I8",          TNumber(0),                                              TNumber(-1) },
            { "MONO_TYPE_U1",          TNumber(0),                                              TNumber(1) },
            { "MONO_TYPE_U2",          TNumber(0),                                              TNumber(1) },
            { "MONO_TYPE_U4",          TNumber(0),                                              TNumber(1) },
            { "MONO_TYPE_U8",          TNumber(0),                                              TNumber(1) },
            { "MONO_TYPE_R4",          TNumber(0),                                              TNumber("3.1414999961853027") },
            { "MONO_TYPE_R8",          TNumber(0),                                              TNumber("3.1415") },
        };

        [Theory]
        [MemberData("GetTestData")]
        async Task InspectVariableBeforeAndAfterAssignment(string clazz, JObject checkDefault, JObject checkValue)
        {
            await SetBreakpointInMethod("debugger-test", "DebuggerTests." + clazz, "Prepare", 2);
            await EvaluateAndCheck("window.setTimeout(function() { invoke_static_method('[debugger-test] DebuggerTests." + clazz + ":Prepare'); })", null, -1, -1, "Prepare");

            // 1) check un-assigned variables
            await StepAndCheck(StepKind.Into, "dotnet://debugger-test.dll/debugger-assignment-test.cs", -1, -1, "TestedMethod",
                locals_fn: (locals) =>
                {
                    Assert.Equal(2, locals.Count());
                    Check(locals, "r", checkDefault);
                }
            );

            // 2) check assigned variables
            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-assignment-test.cs", -1, -1, "TestedMethod", times: 3,
                locals_fn: (locals) =>
                {
                    Assert.Equal(2, locals.Count());
                    Check(locals, "r", checkValue);
                }
            );
        }
    }
}
