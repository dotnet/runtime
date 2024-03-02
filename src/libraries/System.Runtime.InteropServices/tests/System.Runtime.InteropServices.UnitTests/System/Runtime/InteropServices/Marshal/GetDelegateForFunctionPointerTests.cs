// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/39187", TestPlatforms.Browser)]
    public class GetDelegateForFunctionPointerTests
    {
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoAOT))]
        [InlineData(typeof(NonGenericDelegate))]
        [InlineData(typeof(MulticastDelegate))]
        [InlineData(typeof(OtherNonGenericDelegate))]
        public void GetDelegateForFunctionPointer_NonGeneric_ReturnsExpected(Type t)
        {
            MethodInfo targetMethod = typeof(GetDelegateForFunctionPointerTests).GetMethod(nameof(Method), BindingFlags.NonPublic | BindingFlags.Static);
            Delegate d = targetMethod.CreateDelegate(typeof(NonGenericDelegate));
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(d);

            Delegate functionDelegate = Marshal.GetDelegateForFunctionPointer(ptr, t);
            GC.KeepAlive(d);
            VerifyDelegate(functionDelegate, targetMethod);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void GetDelegateForFunctionPointer_CollectibleType_ReturnsExpected()
        {
            MethodInfo targetMethod = typeof(GetDelegateForFunctionPointerTests).GetMethod(nameof(Method), BindingFlags.NonPublic | BindingFlags.Static);
            Delegate d = targetMethod.CreateDelegate(typeof(NonGenericDelegate));
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(d);

            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type", TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass, typeof(MulticastDelegate));
            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(object), typeof(IntPtr) });
            constructorBuilder.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            MethodBuilder methodBuilder = typeBuilder.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, targetMethod.ReturnType, targetMethod.GetParameters().Select(p => p.ParameterType).ToArray());
            methodBuilder.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            Type type = typeBuilder.CreateType();

            Delegate functionDelegate = Marshal.GetDelegateForFunctionPointer(ptr, type);
            GC.KeepAlive(d);
            VerifyDelegate(functionDelegate, targetMethod);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoAOT))]
        public void GetDelegateForFunctionPointer_Generic_ReturnsExpected()
        {
            MethodInfo targetMethod = typeof(GetDelegateForFunctionPointerTests).GetMethod(nameof(Method), BindingFlags.NonPublic | BindingFlags.Static);
            Delegate d = targetMethod.CreateDelegate(typeof(NonGenericDelegate));
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(d);

            Delegate functionDelegate = Marshal.GetDelegateForFunctionPointer<NonGenericDelegate>(ptr);
            GC.KeepAlive(d);
            VerifyDelegate(functionDelegate, targetMethod);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoAOT))]
        public void GetDelegateForFunctionPointer_GenericInvalidType_ReturnsExpected()
        {
            MethodInfo targetMethod = typeof(GetDelegateForFunctionPointerTests).GetMethod(nameof(Method), BindingFlags.NonPublic | BindingFlags.Static);
            Delegate d = targetMethod.CreateDelegate(typeof(NonGenericDelegate));
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(d);

            Delegate functionDelegate = Marshal.GetDelegateForFunctionPointer<MulticastDelegate>(ptr);
            GC.KeepAlive(d);
            VerifyDelegate(functionDelegate, targetMethod);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/48379", TestRuntimes.Mono)]
        public void GetDelegateForFunctionPointer_MulticastDelegate_ThrowsMustBeDelegate()
        {
            IntPtr ptr = Marshal.AllocHGlobal(16);
            AssertExtensions.Throws<ArgumentException>("t", () => Marshal.GetDelegateForFunctionPointer(ptr, typeof(MulticastDelegate)));
            AssertExtensions.Throws<ArgumentException>("t", () => Marshal.GetDelegateForFunctionPointer<MulticastDelegate>(ptr));
            Marshal.FreeHGlobal(ptr);
        }

        private static void VerifyDelegate(Delegate d, MethodInfo expectedMethod)
        {
            Assert.IsType<NonGenericDelegate>(d);
            Assert.Equal(expectedMethod, d.Method);
            Assert.Null(d.Target);
        }

        [Fact]
        public void GetDelegateForFunctionPointer_ZeroPointer_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("ptr", () => Marshal.GetDelegateForFunctionPointer(IntPtr.Zero, typeof(NonGenericDelegate)));
            AssertExtensions.Throws<ArgumentNullException>("ptr", () => Marshal.GetDelegateForFunctionPointer<string>(IntPtr.Zero));
        }

        [Fact]
        public void GetDelegateForFunctionPointer_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("t", () => Marshal.GetDelegateForFunctionPointer((IntPtr)1, null));
        }

        public static IEnumerable<object[]> GetDelegateForFunctionPointer_InvalidType_TestData()
        {
            yield return new object[] { typeof(int).MakeByRefType() };
            yield return new object[] { typeof(int).MakePointerType() };
            yield return new object[] { typeof(string) };

            yield return new object[] { typeof(NonGenericClass) };
            yield return new object[] { typeof(GenericClass<>) };
            yield return new object[] { typeof(GenericClass<string>) };

            yield return new object[] { typeof(GenericStruct<>) };
            yield return new object[] { typeof(GenericStruct<string>) };
            yield return new object[] { typeof(IGenericInterface<>) };
            yield return new object[] { typeof(IGenericInterface<string>) };

            yield return new object[] { typeof(GenericClass<>).GetTypeInfo().GenericTypeParameters[0] };

            if (PlatformDetection.IsReflectionEmitSupported)
            {
                AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.Run);
                ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
                TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
                yield return new object[] { typeBuilder };
            }

            yield return new object[] { typeof(Delegate) };
            yield return new object[] { typeof(GenericDelegate<>) };
            yield return new object[] { typeof(GenericDelegate<string>) };
        }

        [Theory]
        [MemberData(nameof(GetDelegateForFunctionPointer_InvalidType_TestData))]
        public void GetDelegateForFunctionPointer_InvalidType_ThrowsArgumentException(Type t)
        {
            AssertExtensions.Throws<ArgumentException>("t", () => Marshal.GetDelegateForFunctionPointer((IntPtr)1, t));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoAOT))]
        public void GetDelegateForFunctionPointer_CantCast_ThrowsInvalidCastException()
        {
            MethodInfo targetMethod = typeof(GetDelegateForFunctionPointerTests).GetMethod(nameof(Method), BindingFlags.NonPublic | BindingFlags.Static);
            Delegate d = targetMethod.CreateDelegate(typeof(NonGenericDelegate));
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(d);

            Assert.Throws<InvalidCastException>(() => Marshal.GetDelegateForFunctionPointer<OtherNonGenericDelegate>(ptr));
            GC.KeepAlive(d);
        }

        [Fact]
        public void GetDelegateForFunctionPointer_Resurrection()
        {
            GCHandle handle = Alloc();

            if (PlatformDetection.IsPreciseGcSupported)
            {
                while (handle.Target != null)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }

            handle.Free();

            [MethodImpl(MethodImplOptions.NoInlining)]
            static GCHandle Alloc()
            {
                GCHandle gcHandle = default;
                gcHandle = GCHandle.Alloc(new FreachableObject(), GCHandleType.WeakTrackResurrection);
                return gcHandle;
            }
        }

        private class FreachableObject
        {
            private readonly Action _del;
            private readonly IntPtr _fnptr;
            private int _count;

            internal FreachableObject()
            {
                _del = new Action(() => { });
                _fnptr = Marshal.GetFunctionPointerForDelegate(_del);
            }

            ~FreachableObject()
            {
                Assert.Same(Marshal.GetDelegateForFunctionPointer<Action>(_fnptr), _del);

                if (_count++ < 4)
                {
                    GC.ReRegisterForFinalize(this);
                }
            }
        }

        public delegate void GenericDelegate<T>(T t);
        public delegate void NonGenericDelegate(string t);
        public delegate void OtherNonGenericDelegate(string t);

        private static void Method(string s) { }
    }
}
