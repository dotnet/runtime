// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public sealed class DpmParams
    {
        public string MethodName;
        public MethodAttributes Attributes = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.PinvokeImpl;
        public string LibName;
        public string EntrypointName;
        public CallingConventions ManagedCallConv = CallingConventions.Standard;
        public CallingConvention NativeCallConv = CallingConvention.StdCall;
        public CharSet Charset = CharSet.Unicode;
        public Type ReturnType;
        public Type[] ParameterTypes;
        public Type[] ReturnTypeReqMods;
        public Type[] ReturnTypeOptMods;
        public Type[][] ParameterTypeReqMods;
        public Type[][] ParameterTypeOptMods;

        public bool NoCMods => ReturnTypeReqMods == null && ReturnTypeOptMods == null && ParameterTypeReqMods == null && ParameterTypeOptMods == null;

        public sealed override string ToString() => MethodName;
    }

    public class TypeBuilderDefinePInvokeMethodTests
    {
        public static IEnumerable<DpmParams> TestData
        {
            get
            {
                // The Dll/Entrypoint names can be arbitrary as these tests only generate the P/Invoke metadata and do not attempt to invoke them.
                // Keep the "MethodNames" unique so that if a test fails, the theory member that failed can be identified easily from the log output.

                yield return new DpmParams() { MethodName = "A1", LibName = "Foo1.dll", EntrypointName = "Wha1", ReturnType = typeof(int), ParameterTypes = new Type[] { typeof(string) } };
                yield return new DpmParams() { MethodName = "A2", LibName = "Foo2.dll", EntrypointName = "Wha2", ReturnType = typeof(int), ParameterTypes = new Type[] { typeof(int) },
                    NativeCallConv = CallingConvention.Cdecl};
                yield return new DpmParams() { MethodName = "A3", LibName = "Foo3.dll", EntrypointName = "Wha3", ReturnType = typeof(double), ParameterTypes = new Type[] { typeof(string) },
                    Charset = CharSet.Ansi};
                yield return new DpmParams() { MethodName = "A4", LibName = "Foo4.dll", EntrypointName = "Wha4", ReturnType = typeof(IntPtr), ParameterTypes = new Type[] { typeof(string) },
                    Charset = CharSet.Unicode};
                yield return new DpmParams() { MethodName = "A5", LibName = "Foo5.dll", EntrypointName = "Wha5", ReturnType = typeof(int), ParameterTypes = new Type[] { typeof(object) },
                    Charset = CharSet.Auto};
                yield return new DpmParams() { MethodName = "A6", LibName = "Foo6.dll", EntrypointName = "Wha6", ReturnType = typeof(char), ParameterTypes = new Type[] { typeof(string) },
                    Charset = CharSet.None};
                yield return new DpmParams() { MethodName = "B1", LibName = "Foo7.dll", EntrypointName = "B1", ReturnType = typeof(void), ParameterTypes = new Type[] { typeof(string) } };
                yield return new DpmParams() { MethodName = "C1", LibName = "Foo8.dll", EntrypointName = "Wha7", ReturnType = typeof(int), ParameterTypes = new Type[] { typeof(string) },
                    ReturnTypeReqMods = new Type[] { typeof(int) },
                    ReturnTypeOptMods = new Type[] { typeof(short) },
                    ParameterTypeOptMods = new Type[][] { new Type[] { typeof(double) } },
                    ParameterTypeReqMods = new Type[][] { new Type[] { typeof(float) } },
                };
            }
        }

        public static IEnumerable<object[]> TheoryData1 => TestData.Where(dpm => dpm.NoCMods).Select(dpm => new object[] { dpm });
        [Theory]
        [MemberData(nameof(TheoryData1))]
        public static void TestDefinePInvokeMethod1(DpmParams p)
        {
            TypeBuilder tb = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder mb = tb.DefinePInvokeMethod(
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

            Type t = tb.CreateType();
            MethodInfo m = t.GetMethod(p.MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(m);
            VerifyPInvokeMethod(t, m, p);
        }

        public static IEnumerable<object[]> TheoryData2 => TestData.Where(dpm => dpm.NoCMods && dpm.EntrypointName == dpm.MethodName).Select(dpm => new object[] { dpm });
        [Theory]
        [MemberData(nameof(TheoryData2))]
        public static void TestDefinePInvokeMethod2(DpmParams p)
        {
            TypeBuilder tb = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder mb = tb.DefinePInvokeMethod(
                p.MethodName,
                p.LibName,
                p.Attributes,
                p.ManagedCallConv,
                p.ReturnType,
                p.ParameterTypes,
                p.NativeCallConv,
                p.Charset);
            mb.SetImplementationFlags(mb.GetMethodImplementationFlags() | MethodImplAttributes.PreserveSig);

            Type t = tb.CreateType();
            MethodInfo m = t.GetMethod(p.MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(m);
            VerifyPInvokeMethod(t, m, p);
        }

        public static IEnumerable<object[]> TheoryData3 => TestData.Select(dpm => new object[] { dpm });
        [Theory]
        [MemberData(nameof(TheoryData3))]
        public static void TestDefinePInvokeMethod3(DpmParams p)
        {
            TypeBuilder tb = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder mb = tb.DefinePInvokeMethod(
                p.MethodName,
                p.LibName,
                p.EntrypointName,
                p.Attributes,
                p.ManagedCallConv,
                p.ReturnType,
                p.ReturnTypeReqMods,
                p.ReturnTypeOptMods,
                p.ParameterTypes,
                p.ParameterTypeReqMods,
                p.ParameterTypeOptMods,
                p.NativeCallConv,
                p.Charset);
            mb.SetImplementationFlags(mb.GetMethodImplementationFlags() | MethodImplAttributes.PreserveSig);

            Type t = tb.CreateType();
            MethodInfo m = t.GetMethod(p.MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(m);
            VerifyPInvokeMethod(t, m, p);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void TestDefinePInvokeMethodExecution_Windows()
        {
            const string EnvironmentVariable = "COMPUTERNAME";

            TypeBuilder tb = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder mb = tb.DefinePInvokeMethod(
                "GetEnvironmentVariableW",
                "kernel32.dll",
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.PinvokeImpl,
                CallingConventions.Standard,
                typeof(int),
                new Type[] { typeof(string), typeof(StringBuilder), typeof(int) },
                CallingConvention.StdCall,
                CharSet.Unicode);
            mb.SetImplementationFlags(mb.GetMethodImplementationFlags() | MethodImplAttributes.PreserveSig);

            Type t = tb.CreateType();
            MethodInfo m = t.GetMethod("GetEnvironmentVariableW", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(m);

            string expected = Environment.GetEnvironmentVariable(EnvironmentVariable);

            int numCharsRequired = (int)m.Invoke(null, new object[] { EnvironmentVariable, null, 0 });
            if (numCharsRequired == 0)
            {
                // Environment variable is not defined. Make sure we got that result using both techniques.
                Assert.Null(expected);
            }
            else
            {
                StringBuilder sb = new StringBuilder(numCharsRequired);
                int numCharsWritten = (int)m.Invoke(null, new object[] { EnvironmentVariable, sb, numCharsRequired });
                Assert.NotEqual(0, numCharsWritten);
                string actual = sb.ToString();
                Assert.Equal(expected, actual);
            }
        }

        public static void VerifyPInvokeMethod(Type type, MethodInfo method, DpmParams p)
        {
            Assert.Equal(type.AsType(), method.DeclaringType);
            Assert.Equal(p.MethodName, method.Name);
            Assert.Equal(p.Attributes, method.Attributes);
            Assert.Equal(p.ManagedCallConv, method.CallingConvention);
            Assert.Equal(p.ReturnType, method.ReturnType);

            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal<Type>(p.ParameterTypes, parameters.Select(pi => pi.ParameterType));

            DllImportAttribute dia = method.GetCustomAttribute<DllImportAttribute>();
            Assert.NotNull(dia);
            Assert.Equal(p.LibName, dia.Value);
            Assert.Equal(p.EntrypointName, dia.EntryPoint);
            Assert.Equal(p.Charset, dia.CharSet);
            Assert.Equal(p.NativeCallConv, dia.CallingConvention);
            Assert.Equal(false, dia.BestFitMapping);
            Assert.Equal(false, dia.ExactSpelling);
            Assert.Equal(true, dia.PreserveSig);  
            Assert.Equal(false, dia.SetLastError);

            IList<Type> returnTypeOptMods = method.ReturnParameter.GetOptionalCustomModifiers();
            if (p.ReturnTypeOptMods == null)
            {
                Assert.Equal(0, returnTypeOptMods.Count);
            }
            else
            {
                Assert.Equal<Type>(p.ReturnTypeOptMods, returnTypeOptMods);
            }

            IList<Type> returnTypeReqMods = method.ReturnParameter.GetRequiredCustomModifiers();
            if (p.ReturnTypeReqMods == null)
            {
                Assert.Equal(0, returnTypeReqMods.Count);
            }
            else
            {
                Assert.Equal<Type>(p.ReturnTypeReqMods, returnTypeReqMods);
            }

            if (p.ParameterTypeOptMods == null)
            {
                foreach (ParameterInfo pi in method.GetParameters())
                {
                    Assert.Equal(0, pi.GetOptionalCustomModifiers().Length);
                }
            }
            else
            {
                Assert.Equal(parameters.Length, p.ParameterTypeOptMods.Length);
                for (int i = 0; i < p.ParameterTypeOptMods.Length; i++)
                {
                    Type[] mods = parameters[i].GetOptionalCustomModifiers();
                    Assert.Equal<Type>(p.ParameterTypeOptMods[i], mods);
                }
            }

            if (p.ParameterTypeReqMods == null)
            {
                foreach (ParameterInfo pi in method.GetParameters())
                {
                    Assert.Equal(0, pi.GetRequiredCustomModifiers().Length);
                }
            }
            else
            {
                Assert.Equal(parameters.Length, p.ParameterTypeReqMods.Length);
                for (int i = 0; i < p.ParameterTypeReqMods.Length; i++)
                {
                    Type[] mods = parameters[i].GetRequiredCustomModifiers();
                    Assert.Equal<Type>(p.ParameterTypeReqMods[i], mods);
                }
            }
        }
    }
}
