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
        [Fact]
        async Task InspectObjectAssignmentsDuringSteppingIn()
        {
            await InspectAssignmentDuringSteppingIn("MONO_TYPE_OBJECT",
                (locals) => CheckObject(locals, "r", "object", is_null: true),
                (locals) => CheckObject(locals, "r", "object")
                );
        }

        [Fact]
        async Task InspectClassAssignmentsDuringSteppingIn()
        {
            await InspectAssignmentDuringSteppingIn("MONO_TYPE_CLASS",
                (locals) => CheckObject(locals, "r", "DebuggerTests.MONO_TYPE_CLASS", is_null: true),
                (locals) => CheckObject(locals, "r", "DebuggerTests.MONO_TYPE_CLASS")
                );
        }

        [Fact]
        async Task InspectBoolAssignmentsDuringSteppingIn()
        {
            await InspectAssignmentDuringSteppingIn("MONO_TYPE_BOOLEAN",
                (locals) => CheckBool(locals, "r", default),
                (locals) => CheckBool(locals, "r", true)
                );
        }

        [Fact]
        async Task InspectCharAssignmentsDuringSteppingIn()
        {
            await InspectAssignmentDuringSteppingIn("MONO_TYPE_CHAR",
                (locals) => CheckSymbol(locals, "r", "0 '\u0000'"),
                (locals) => CheckSymbol(locals, "r", "97 'a'")
                );
        }

        [Fact]
        async Task InspectSByteAssignmentsDuringSteppingIn()
        {
            await InspectAssignmentDuringSteppingIn("MONO_TYPE_I1",
                (locals) => CheckNumber<sbyte>(locals, "r", default),
                (locals) => CheckNumber<sbyte>(locals, "r", -1)
                );
        }

        [Fact]
        async Task InspectShortAssignmentsDuringSteppingIn()
        {
            await InspectAssignmentDuringSteppingIn("MONO_TYPE_I2",
                (locals) => CheckNumber<short>(locals, "r", default),
                (locals) => CheckNumber<short>(locals, "r", -1)
                );
        }

        [Fact]
        async Task InspectIntAssignmentsDuringSteppingIn()
        {
            await InspectAssignmentDuringSteppingIn("MONO_TYPE_I4",
                (locals) => CheckNumber<int>(locals, "r", default),
                (locals) => CheckNumber<int>(locals, "r", -1)
                );
        }

        [Fact]
        async Task InspectLongAssignmentsDuringSteppingIn()
        {
            await InspectAssignmentDuringSteppingIn("MONO_TYPE_I8",
                (locals) => CheckNumber<long>(locals, "r", default),
                (locals) => CheckNumber<long>(locals, "r", -1)
                );
        }

        [Fact]
        async Task InspectByteAssignmentsDuringSteppingIn()
        {
            await InspectAssignmentDuringSteppingIn("MONO_TYPE_U1",
                (locals) => CheckNumber<byte>(locals, "r", default),
                (locals) => CheckNumber<byte>(locals, "r", 1)
                );
        }

        [Fact]
        async Task InspectUShortAssignmentsDuringSteppingIn()
        {
            await InspectAssignmentDuringSteppingIn("MONO_TYPE_U2",
                (locals) => CheckNumber<ushort>(locals, "r", default),
                (locals) => CheckNumber<ushort>(locals, "r", 1)
                );
        }

        [Fact]
        async Task InspectUIntAssignmentsDuringSteppingIn()
        {
            await InspectAssignmentDuringSteppingIn("MONO_TYPE_U4",
                (locals) => CheckNumber<uint>(locals, "r", default),
                (locals) => CheckNumber<uint>(locals, "r", 1)
                );
        }

        [Fact]
        async Task InspectULongAssignmentsDuringSteppingIn()
        {
            await InspectAssignmentDuringSteppingIn("MONO_TYPE_U8",
                (locals) => CheckNumber<ulong>(locals, "r", default),
                (locals) => CheckNumber<ulong>(locals, "r", 1)
                );
        }

        [Fact]
        async Task InspectFloatAssignmentsDuringSteppingIn()
        {
            await InspectAssignmentDuringSteppingIn("MONO_TYPE_R4",
                (locals) => CheckNumber<float>(locals, "r", default),
                (locals) => CheckNumber<float>(locals, "r", 3.1415F)
                );
        }

        [Fact]
        async Task InspectDoubleAssignmentsDuringSteppingIn()
        {
            await InspectAssignmentDuringSteppingIn("MONO_TYPE_R8",
                (locals) => CheckNumber<double>(locals, "r", default),
                (locals) => CheckNumber<double>(locals, "r", 3.1415D)
                );
        }

        [Fact]
        async Task InspectStringAssignmentsDuringSteppingIn()
        {
            await InspectAssignmentDuringSteppingIn("MONO_TYPE_STRING",
                (locals) => CheckString(locals, "r", default),
                (locals) => CheckString(locals, "r", "hello")
                );
        }

        [Fact]
        async Task InspectEnumAssignmentsDuringSteppingIn()
        {
            await InspectAssignmentDuringSteppingIn("MONO_TYPE_ENUM",
                (locals) => CheckEnum(locals, "r", "DebuggerTests.RGB", "Red"),
                (locals) => CheckEnum(locals, "r", "DebuggerTests.RGB", "Blue")
                );
        }

        [Fact]
        async Task InspectArrayAssignmentsDuringSteppingIn()
        {
            await InspectAssignmentDuringSteppingIn("MONO_TYPE_ARRAY",
                (locals) => CheckObject(locals, "r", "byte[]", is_null: true),
                (locals) => CheckArray(locals, "r", "byte[]", 2)
                );
        }

        [Fact]
        async Task InspectStructAssignmentsDuringSteppingIn()
        {
            await InspectAssignmentDuringSteppingIn("MONO_TYPE_VALUETYPE",
                (locals) => CheckValueType(locals, "r", "DebuggerTests.Point"),
                (locals) => CheckValueType(locals, "r", "DebuggerTests.Point")
                );
        }

        [Fact]
        async Task InspectDecimalAssignmentsDuringSteppingIn()
        {
            await InspectAssignmentDuringSteppingIn("MONO_TYPE_VALUETYPE2",
                (locals) => CheckValueType(locals, "r", "System.Decimal", description: "0"),
                (locals) => CheckValueType(locals, "r", "System.Decimal", description: "1.1")
                );
        }

        [Fact]
        async Task InspectFunctionAssignmentsDuringSteppingIn()
        {
            await InspectAssignmentDuringSteppingIn("MONO_TYPE_GENERICINST",
                (locals) => CheckObject(locals, "r", "System.Func<int>", is_null: true),
                (locals) => CheckObject(locals, "r", "System.Func<int>", description: "int Prepare ()")
                );
        }

        [Fact]
        async Task InspectFunctionPtrAssignmentsDuringSteppingIn()
        {
            await InspectAssignmentDuringSteppingIn("MONO_TYPE_FNPTR",
                (locals) => CheckSymbol(locals, "r", "(*()) 0"),
                (locals) => { } // CheckObject(locals, "r", "(*()) 0x22dd230") // FixMe: Could we do better ?
                );
        }

        [Fact]
        async Task InspectIntPtrAssignmentsDuringSteppingIn()
        {
            await InspectAssignmentDuringSteppingIn("MONO_TYPE_PTR",
                (locals) => CheckSymbol(locals, "r", "(int*) 0"),
                (locals) => { } //CheckObject(locals, "r", "(int*) 0x211c058") // FixMe: Could we do better ?
                );
        }

        private async Task InspectAssignmentDuringSteppingIn(string clazz, Action<JToken> checkDefault, Action<JToken> checkValue)
        {
            await SetBreakpointInMethod("debugger-test", "DebuggerTests." + clazz, "Prepare", 2);
            await EvaluateAndCheck("window.setTimeout(function() { invoke_static_method('[debugger-test] DebuggerTests." + clazz + ":Prepare'); })", null, -1, -1, "Prepare");

            // 1) check un-assigned variables
            await StepAndCheck(StepKind.Into, "dotnet://debugger-test.dll/debugger-assignment-test.cs", -1, -1, "TestedMethod",
                locals_fn: (locals) =>
                {
                    Console.WriteLine(locals);
                    Assert.Equal(2, locals.Count());
                    checkDefault(locals);
                }
            );

            // 2) check assigned variables
            await StepAndCheck(StepKind.Over, "dotnet://debugger-test.dll/debugger-assignment-test.cs", -1, -1, "TestedMethod", times: 3,
                locals_fn: (locals) =>
                {
                    Console.WriteLine(locals);
                    Assert.Equal(2, locals.Count());
                    checkValue(locals);
                }
            );
        }
    }
}
