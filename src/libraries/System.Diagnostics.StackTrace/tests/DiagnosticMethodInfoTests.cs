// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class DiagnosticMethodInfoTests
    {
        [Fact]
        public void Create_Null()
        {
            Assert.Throws<ArgumentNullException>(() => DiagnosticMethodInfo.Create((Delegate)null));
            Assert.Throws<ArgumentNullException>(() => DiagnosticMethodInfo.Create((StackFrame)null));
        }

        public static IEnumerable<object[]> Create_OpenDelegate_TestData()
        {
            // Tracked at https://github.com/dotnet/runtime/issues/100748
            bool hasGvmOpenDelegateBug = !PlatformDetection.IsMonoRuntime && !PlatformDetection.IsNativeAot;

            const string TestNamespace = nameof(System) + "." + nameof(System.Diagnostics) + "." + nameof(System.Diagnostics.Tests) + ".";

            yield return new object[] {
                typeof(IInterfaceForDiagnosticMethodInfoTests).GetMethod(nameof(IInterfaceForDiagnosticMethodInfoTests.NonGenericMethod)).CreateDelegate<Action<IInterfaceForDiagnosticMethodInfoTests>>(),
                nameof(IInterfaceForDiagnosticMethodInfoTests.NonGenericMethod),
                TestNamespace + nameof(IInterfaceForDiagnosticMethodInfoTests)
            };

            if (!hasGvmOpenDelegateBug)
            {
                yield return new object[] {
                typeof(IInterfaceForDiagnosticMethodInfoTests).GetMethod(nameof(IInterfaceForDiagnosticMethodInfoTests.GenericMethod)).MakeGenericMethod(typeof(object)).CreateDelegate<Action<IInterfaceForDiagnosticMethodInfoTests>>(),
                nameof(IInterfaceForDiagnosticMethodInfoTests.GenericMethod),
                TestNamespace + nameof(IInterfaceForDiagnosticMethodInfoTests)
                };
            }

            yield return new object[] {
                typeof(IGenericInterfaceForDiagnosticMethodInfoTests<object>).GetMethod(nameof(IGenericInterfaceForDiagnosticMethodInfoTests<object>.NonGenericMethod)).CreateDelegate<Action<IGenericInterfaceForDiagnosticMethodInfoTests<object>>>(),
                nameof(IGenericInterfaceForDiagnosticMethodInfoTests<object>.NonGenericMethod),
                TestNamespace + nameof(IGenericInterfaceForDiagnosticMethodInfoTests<object>) + "`1"
            };

            if (!hasGvmOpenDelegateBug)
            {
                yield return new object[] {
                typeof(IGenericInterfaceForDiagnosticMethodInfoTests<object>).GetMethod(nameof(IGenericInterfaceForDiagnosticMethodInfoTests<object>.GenericMethod)).MakeGenericMethod(typeof(object)).CreateDelegate<Action<IGenericInterfaceForDiagnosticMethodInfoTests<object>>>(),
                nameof(IGenericInterfaceForDiagnosticMethodInfoTests<object>.GenericMethod),
                TestNamespace + nameof(IGenericInterfaceForDiagnosticMethodInfoTests<object>) + "`1"
                };
            }

            yield return new object[] {
                typeof(StructForDiagnosticMethodInfoTests).GetMethod(nameof(StructForDiagnosticMethodInfoTests.NonGenericMethod)).CreateDelegate<RefAction<StructForDiagnosticMethodInfoTests>>(),
                nameof(StructForDiagnosticMethodInfoTests.NonGenericMethod),
                TestNamespace + nameof(StructForDiagnosticMethodInfoTests)
            };

            yield return new object[] {
                typeof(StructForDiagnosticMethodInfoTests).GetMethod(nameof(StructForDiagnosticMethodInfoTests.GenericMethod)).MakeGenericMethod(typeof(object)).CreateDelegate<RefAction<StructForDiagnosticMethodInfoTests>>(),
                nameof(StructForDiagnosticMethodInfoTests.GenericMethod),
                TestNamespace + nameof(StructForDiagnosticMethodInfoTests)
            };
        }

        [Theory]
        [MemberData(nameof(Create_OpenDelegate_TestData))]
        public void Create_OpenDelegate(Delegate del, string expectedName, string expectedTypeName)
        {
            DiagnosticMethodInfo dmi = DiagnosticMethodInfo.Create(del);

            Assert.Equal(expectedName, dmi.Name);
            Assert.Equal(expectedTypeName, dmi.DeclaringTypeName);
            AssertEqualAssemblyName(Assembly.GetExecutingAssembly().GetName(), dmi.DeclaringAssemblyName);
        }

        public static IEnumerable<object[]> Create_ClosedDelegate_TestData()
        {
            const string TestNamespace = nameof(System) + "." + nameof(System.Diagnostics) + "." + nameof(System.Diagnostics.Tests) + ".";

            IInterfaceForDiagnosticMethodInfoTests o = new ClassForDiagnosticMethodInfoTests();
            yield return new object[] {
                (Action)o.NonGenericDefaultMethod,
                nameof(IInterfaceForDiagnosticMethodInfoTests.NonGenericDefaultMethod),
                TestNamespace + nameof(IInterfaceForDiagnosticMethodInfoTests)
            };
            yield return new object[] {
                (Action)o.GenericDefaultMethod<object>,
                nameof(IInterfaceForDiagnosticMethodInfoTests.GenericDefaultMethod),
                TestNamespace + nameof(IInterfaceForDiagnosticMethodInfoTests)
            };
            yield return new object[] {
                (Action)o.NonGenericMethod,
                TestNamespace + nameof(IInterfaceForDiagnosticMethodInfoTests) + "." + nameof(IInterfaceForDiagnosticMethodInfoTests.NonGenericMethod),
                TestNamespace + nameof(ClassForDiagnosticMethodInfoTests)
            };
            yield return new object[] {
                (Action)o.GenericMethod<object>,
                TestNamespace + nameof(IInterfaceForDiagnosticMethodInfoTests) + "." + nameof(IInterfaceForDiagnosticMethodInfoTests.GenericMethod),
                TestNamespace + nameof(ClassForDiagnosticMethodInfoTests)
            };

            IGenericInterfaceForDiagnosticMethodInfoTests<object> og = new GenericClassForDiagnosticMethodInfoTests<object>();

            // Making this work with native AOT tracked in https://github.com/dotnet/runtime/issues/103219
            if (!PlatformDetection.IsNativeAot)
            {
                yield return new object[] {
                        (Action)og.NonGenericDefaultMethod,
                        nameof(IGenericInterfaceForDiagnosticMethodInfoTests<object>.NonGenericDefaultMethod) ,
                        TestNamespace + nameof(IGenericInterfaceForDiagnosticMethodInfoTests<object>) + "`1"
                    };
            }
            yield return new object[] {
                    (Action)og.GenericDefaultMethod<object>,
                    nameof(IGenericInterfaceForDiagnosticMethodInfoTests<object>.GenericDefaultMethod),
                    TestNamespace + nameof(IGenericInterfaceForDiagnosticMethodInfoTests<object>) + "`1"
                };
            yield return new object[] {
                (Action)og.NonGenericMethod,
                TestNamespace + nameof(IGenericInterfaceForDiagnosticMethodInfoTests<object>) + "<T>." + nameof(IGenericInterfaceForDiagnosticMethodInfoTests<object>.NonGenericMethod),
                TestNamespace + nameof(GenericClassForDiagnosticMethodInfoTests<object>) + "`1"
            };
            yield return new object[] {
                (Action)og.GenericMethod<object>,
                TestNamespace + nameof(IGenericInterfaceForDiagnosticMethodInfoTests<object>) + "<T>." + nameof(IGenericInterfaceForDiagnosticMethodInfoTests<object>.GenericMethod),
                TestNamespace + nameof(GenericClassForDiagnosticMethodInfoTests<object>) + "`1"
            };

            StructForDiagnosticMethodInfoTests s = default;
            yield return new object[] {
                (Action)s.NonGenericMethod,
                nameof(StructForDiagnosticMethodInfoTests.NonGenericMethod),
                TestNamespace + nameof(StructForDiagnosticMethodInfoTests)
            };
            yield return new object[] {
                (Action)s.GenericMethod<object>,
                nameof(StructForDiagnosticMethodInfoTests.GenericMethod),
                TestNamespace + nameof(StructForDiagnosticMethodInfoTests)
            };
        }

        [Theory]
        [MemberData(nameof(Create_ClosedDelegate_TestData))]
        public void Create_ClosedDelegate(Delegate del, string expectedName, string expectedTypeName)
        {
            DiagnosticMethodInfo dmi = DiagnosticMethodInfo.Create(del);

            Assert.Equal(expectedName, dmi.Name);
            Assert.Equal(expectedTypeName, dmi.DeclaringTypeName);
            AssertEqualAssemblyName(Assembly.GetExecutingAssembly().GetName(), dmi.DeclaringAssemblyName);
        }

        private static void AssertEqualAssemblyName(AssemblyName aname, string s)
        {
            var ani = AssemblyNameInfo.Parse(s);
            Assert.Equal(aname.Name, ani.Name);
            Assert.Equal(aname.Version, ani.Version);
        }

        [Fact]
        public void Create_MulticastDelegate()
        {
            var c = new ClassForDiagnosticMethodInfoTests();
            Action a1 = c.Method1;
            Action a2 = c.Method2;

            Action a = a1 + a2;

            DiagnosticMethodInfo dmi = DiagnosticMethodInfo.Create(a);
            Assert.Equal(nameof(ClassForDiagnosticMethodInfoTests.Method2), dmi.Name);
        }

        [Fact]
        [SkipOnMono("needs triage") /* Same as https://github.com/dotnet/runtime/blob/0686ce61ed1e1cb3cb420281a0154efa5d0d00d5/src/tests/Interop/MarshalAPI/FunctionPointer/FunctionPointer.cs#L9 */]
        public unsafe void Create_MarshalledPointer()
        {
            void* pMem = NativeMemory.Alloc(1);
            Action del = Marshal.GetDelegateForFunctionPointer<Action>((nint)pMem);
            NativeMemory.Free(pMem);

            DiagnosticMethodInfo dmi = DiagnosticMethodInfo.Create(del);
            Assert.Equal(nameof(Action.Invoke), dmi.Name);
            Assert.Equal(nameof(System) + "." + nameof(Action), dmi.DeclaringTypeName);
        }

        [Fact]
        public unsafe void Create_StackFrame()
        {
            StackTrace tr = NonGenericStackTraceClass.TestNonGeneric();

            Verify(tr.GetFrame(0), "Test", "System.Diagnostics.Tests.GenericStackTraceClass`1+Nested");
            Verify(tr.GetFrame(1), "TestGeneric", "System.Diagnostics.Tests.GenericStackTraceClass`1");
            Verify(tr.GetFrame(2), "TestNonGeneric", "System.Diagnostics.Tests.GenericStackTraceClass`1");
            Verify(tr.GetFrame(3), "TestGeneric", "System.Diagnostics.Tests.NonGenericStackTraceClass");
            Verify(tr.GetFrame(4), "TestNonGeneric", "System.Diagnostics.Tests.NonGenericStackTraceClass");

            static void Verify(StackFrame fr, string expectedName, string expectedDeclaringName)
            {
                DiagnosticMethodInfo dmi = DiagnosticMethodInfo.Create(fr);
                Assert.Equal(expectedName, dmi.Name);
                Assert.Equal(expectedDeclaringName, dmi.DeclaringTypeName);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Create_Delegate_StackTraceSupportDisabled()
        {
            var options = new RemoteInvokeOptions()
            {
                RuntimeConfigurationOptions =
                {
                    ["System.Diagnostics.StackTrace.IsSupported"] = false
                }
            };

            RemoteExecutor.Invoke(static () =>
            {
                var c = new ClassForDiagnosticMethodInfoTests();
                Action a = c.Method1;

                DiagnosticMethodInfo dmi = DiagnosticMethodInfo.Create(a);
                Assert.Null(dmi);
            }, options).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void Create_Frame_StackTraceSupportDisabled()
        {
            var options = new RemoteInvokeOptions()
            {
                RuntimeConfigurationOptions =
                {
                    ["System.Diagnostics.StackTrace.IsSupported"] = false
                }
            };

            RemoteExecutor.Invoke(static () =>
            {
                StackFrame f = new StackFrame();
                DiagnosticMethodInfo dmi = DiagnosticMethodInfo.Create(f);
                Assert.Null(dmi);
            }, options).Dispose();
        }
    }

    interface IInterfaceForDiagnosticMethodInfoTests
    {
        void NonGenericMethod();
        void GenericMethod<T>();

        void NonGenericDefaultMethod() { }
        void GenericDefaultMethod<T>() { }
    }

    class ClassForDiagnosticMethodInfoTests : IInterfaceForDiagnosticMethodInfoTests
    {
        void IInterfaceForDiagnosticMethodInfoTests.GenericMethod<T>() => throw new NotImplementedException();
        void IInterfaceForDiagnosticMethodInfoTests.NonGenericMethod() => throw new NotImplementedException();
        public void Method1() { }
        public void Method2() { }
    }

    interface IGenericInterfaceForDiagnosticMethodInfoTests<T>
    {
        void NonGenericMethod();
        void GenericMethod<U>();

        void NonGenericDefaultMethod() { }
        void GenericDefaultMethod<U>() { }
    }

    class GenericClassForDiagnosticMethodInfoTests<T> : IGenericInterfaceForDiagnosticMethodInfoTests<T>
    {
        void IGenericInterfaceForDiagnosticMethodInfoTests<T>.GenericMethod<U>() => throw new NotImplementedException();
        void IGenericInterfaceForDiagnosticMethodInfoTests<T>.NonGenericMethod() => throw new NotImplementedException();
        public void Method1() { }
        public void Method2() { }
    }

    struct StructForDiagnosticMethodInfoTests : IInterfaceForDiagnosticMethodInfoTests
    {
        public void NonGenericMethod() { }
        public void GenericMethod<T>() { }
    }

    delegate void RefAction<T>(ref T t);

    class NonGenericStackTraceClass
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static StackTrace TestNonGeneric() => TestGeneric<int>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static StackTrace TestGeneric<T>() => GenericStackTraceClass<object>.TestNonGeneric();
    }

    class GenericStackTraceClass<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static StackTrace TestNonGeneric() => TestGeneric<object>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static StackTrace TestGeneric<U>() => Nested.Test();

        public class Nested
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static StackTrace Test() => new StackTrace();
        }
    }
}
