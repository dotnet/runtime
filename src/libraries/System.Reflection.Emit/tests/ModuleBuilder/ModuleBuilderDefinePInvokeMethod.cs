// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ModuleBuilderDefinePInvokeMethodTests
    {
        public static IEnumerable<object[]> TheoryData1 => TypeBuilderDefinePInvokeMethodTests.TestData.Where(dpm => dpm.NoCMods).Select(dpm => new object[] { dpm });
        [Theory]
        [MemberData(nameof(TheoryData1))]
        public static void TestDefinePInvokeMethod1(DpmParams p)
        {
            ModuleBuilder modb = Helpers.DynamicModule();
            MethodBuilder mb = modb.DefinePInvokeMethod(
                p.MethodName,
                p.LibName,
                p.EntrypointName,
                p.Attributes,
                p.ManagedCallConv,
                p.ReturnType,
                p.ParameterTypes,
                p.NativeCallConv,
                p.Charset);
            mb.SetImplementationFlags(mb.GetMethodImplementationFlags() | MethodImplAttributes.PreserveSig);

            modb.CreateGlobalFunctions();
            MethodInfo m = modb.GetMethod(p.MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, CallingConventions.Any, p.ParameterTypes, null);
            Assert.NotNull(m);
            TypeBuilderDefinePInvokeMethodTests.VerifyPInvokeMethod(m.DeclaringType, m, p);
        }

        public static IEnumerable<object[]> TheoryData2 => TypeBuilderDefinePInvokeMethodTests.TestData.Where(dpm => dpm.NoCMods && dpm.EntrypointName == dpm.MethodName).Select(dpm => new object[] { dpm });
        [Theory]
        [MemberData(nameof(TheoryData2))]
        public static void TestDefinePInvokeMethod2(DpmParams p)
        {
            ModuleBuilder modb = Helpers.DynamicModule();
            MethodBuilder mb = modb.DefinePInvokeMethod(
                p.MethodName,
                p.LibName,
                p.Attributes,
                p.ManagedCallConv,
                p.ReturnType,
                p.ParameterTypes,
                p.NativeCallConv,
                p.Charset);
            mb.SetImplementationFlags(mb.GetMethodImplementationFlags() | MethodImplAttributes.PreserveSig);

            modb.CreateGlobalFunctions();
            MethodInfo m = modb.GetMethod(p.MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, CallingConventions.Any, p.ParameterTypes, null);
            Assert.NotNull(m);
            TypeBuilderDefinePInvokeMethodTests.VerifyPInvokeMethod(m.DeclaringType, m, p);
        }
    }
}
