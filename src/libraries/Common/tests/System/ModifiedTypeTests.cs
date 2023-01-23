// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Tests;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests.Types
{
    public partial class ModifiedTypeTests
    {
        private const BindingFlags Bindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71883", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public static unsafe void TestFields_Modified()
        {
            Type volatileInt = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._volatileInt), Bindings).GetModifiedFieldType();
            Verify(volatileInt);

            Type volatileIntArray = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._volatileIntArray), Bindings).GetModifiedFieldType();
            Verify(volatileIntArray);

            Type volatileIntPointer = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._volatileIntPointer), Bindings).GetModifiedFieldType();
            Verify(volatileIntPointer);
            Type volatileIntPointerElementType = volatileIntPointer.GetElementType();
            Assert.True(IsModifiedType(volatileIntPointerElementType));
            Assert.True(ReferenceEquals(volatileIntPointerElementType.UnderlyingSystemType, typeof(int).Project()));
            Assert.Equal(0, volatileIntPointerElementType.UnderlyingSystemType.GetRequiredCustomModifiers().Length);
            Assert.Equal(0, volatileIntPointerElementType.UnderlyingSystemType.GetOptionalCustomModifiers().Length);

            Type volatileFcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._volatileFcnPtr), Bindings).GetModifiedFieldType();
            Verify(volatileFcnPtr);
            Assert.True(IsModifiedType(volatileFcnPtr.GetFunctionPointerReturnType()));
            Assert.Equal(1, volatileFcnPtr.GetFunctionPointerParameterTypes().Length);
            Assert.True(IsModifiedType(volatileFcnPtr.GetFunctionPointerParameterTypes()[0]));

            void Verify(Type type)
            {
                Assert.True(IsModifiedType(type));

                Assert.Equal(1, type.GetRequiredCustomModifiers().Length);
                Assert.Equal(typeof(IsVolatile).Project(), type.GetRequiredCustomModifiers()[0]);

                Assert.Equal(0, type.GetOptionalCustomModifiers().Length);

                Assert.Equal(0, type.UnderlyingSystemType.GetRequiredCustomModifiers().Length);
                Assert.Equal(0, type.UnderlyingSystemType.GetOptionalCustomModifiers().Length);
            }
        }

        // NOTE: commented out due to compiler issue on NativeAOT:
        /*
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71883", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public static unsafe void TestFields_Generic_Unmodified()
        {
            Type arrayGenericFcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._arrayGenericFcnPtr), Bindings).FieldType;
            Assert.True(arrayGenericFcnPtr.IsGenericType);
            Assert.False(arrayGenericFcnPtr.IsGenericTypeDefinition);
            Assert.False(IsModifiedType(arrayGenericFcnPtr));

            Type genericParam = arrayGenericFcnPtr.GetGenericArguments()[0];
            Assert.False(IsModifiedType(genericParam));

            Type nestedFcnPtr = genericParam.GetElementType();
            Assert.True(nestedFcnPtr.IsFunctionPointer);
            Assert.False(IsModifiedType(nestedFcnPtr));

            Assert.Equal(1, nestedFcnPtr.GetFunctionPointerParameterTypes().Length);
            Type paramType = nestedFcnPtr.GetFunctionPointerParameterTypes()[0];
            Assert.False(IsModifiedType(paramType));
        }
        */

        // NOTE: commented out due to compiler issue on NativeAOT
        /*
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71883", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public static unsafe void TestFields_Generic_Modified()
        {
            Type arrayGenericFcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._arrayGenericFcnPtr), Bindings).GetModifiedFieldType();
            Assert.True(IsModifiedType(arrayGenericFcnPtr));
            Assert.True(arrayGenericFcnPtr.IsGenericType);
            Assert.False(arrayGenericFcnPtr.IsGenericTypeDefinition);

            Type genericParam = arrayGenericFcnPtr.GetGenericArguments()[0];
            Assert.True(IsModifiedType(genericParam));

            Type nestedFcnPtr = genericParam.GetElementType();
            Assert.True(nestedFcnPtr.IsFunctionPointer);
            Assert.True(IsModifiedType(nestedFcnPtr));

            Assert.Equal(1, nestedFcnPtr.GetFunctionPointerParameterTypes().Length);
            Type paramType = nestedFcnPtr.GetFunctionPointerParameterTypes()[0];
            Assert.True(IsModifiedType(paramType));

            Assert.Equal(typeof(OutAttribute).Project(), paramType.GetRequiredCustomModifiers()[0]);
        }
        */

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71883", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public static unsafe void TestMethods_OpenGeneric_Unmodified()
        {
            MethodInfo mi = typeof(ModifiedTypeHolder).Project().GetMethod(nameof(ModifiedTypeHolder.M_ArrayOpenGenericFcnPtr), Bindings);
            Assert.Equal(1, mi.GetGenericArguments().Length);
            Type p0 = mi.GetGenericArguments()[0];
            Assert.True(p0.IsGenericMethodParameter);
            Assert.False(IsModifiedType(p0));

            Type p1 = mi.GetParameters()[1].ParameterType.GetElementType();
            Assert.True(p1.IsFunctionPointer);
            Assert.False(p1.IsGenericTypeParameter);
            Assert.False(IsModifiedType(p1));

            Assert.Equal(1, p1.GetFunctionPointerParameterTypes().Length);
            Type paramType = p1.GetFunctionPointerParameterTypes()[0];
            Assert.False(IsModifiedType(paramType));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71883", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public static unsafe void TestMethods_OpenGeneric_Modified()
        {
            MethodInfo mi = typeof(ModifiedTypeHolder).Project().GetMethod(nameof(ModifiedTypeHolder.M_ArrayOpenGenericFcnPtr), Bindings);
            Assert.Equal(1, mi.GetGenericArguments().Length);
            Type p0 = mi.GetGenericArguments()[0];
            Assert.True(p0.IsGenericMethodParameter);
            Assert.False(IsModifiedType(p0));

            Type p1 = mi.GetParameters()[1].GetModifiedParameterType().GetElementType();
            Assert.True(p1.IsFunctionPointer);
            Assert.False(p1.IsGenericTypeParameter);
            Assert.True(IsModifiedType(p1));

            Assert.Equal(1, p1.GetFunctionPointerParameterTypes().Length);
            Type paramType = p1.GetFunctionPointerParameterTypes()[0];
            Assert.True(IsModifiedType(paramType));

            Assert.Equal(typeof(OutAttribute).Project(), paramType.GetRequiredCustomModifiers()[0]);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71883", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public static unsafe void TestFields_Unmodified()
        {
            Type volatileInt = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._volatileInt), Bindings).FieldType;
            Verify(volatileInt);

            Type volatileIntArray = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._volatileIntArray), Bindings).FieldType;
            Verify(volatileIntArray);

            Type volatileIntPointer = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._volatileIntPointer), Bindings).FieldType;
            Verify(volatileIntPointer);
            Type volatileIntPointerElementType = volatileIntPointer.GetElementType();
            Assert.False(IsModifiedType(volatileIntPointerElementType));
            Assert.True(ReferenceEquals(volatileIntPointerElementType, typeof(int).Project()));
            Assert.Equal(0, volatileIntPointerElementType.GetRequiredCustomModifiers().Length);
            Assert.Equal(0, volatileIntPointerElementType.GetOptionalCustomModifiers().Length);

            Type volatileFcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._volatileFcnPtr), Bindings).FieldType;
            Verify(volatileFcnPtr);
            Assert.False(IsModifiedType(volatileFcnPtr.GetFunctionPointerReturnType()));
            Assert.Equal(1, volatileFcnPtr.GetFunctionPointerParameterTypes().Length);
            Assert.False(IsModifiedType(volatileFcnPtr.GetFunctionPointerParameterTypes()[0]));

            void Verify(Type type)
            {
                Assert.False(IsModifiedType(type));
                Assert.Equal(0, type.GetRequiredCustomModifiers().Length);
                Assert.Equal(0, type.GetOptionalCustomModifiers().Length);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71883", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public static unsafe void TestFields_Nested_Basic()
        {
            Type ptr_ptr_int = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._ptr_ptr_int), Bindings).GetModifiedFieldType();
            Verify(ptr_ptr_int);
            Assert.True(ptr_ptr_int.UnderlyingSystemType.IsPointer);

            Type array_ptr_int = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._array_ptr_int), Bindings).GetModifiedFieldType();
            Verify(array_ptr_int);
            Assert.True(array_ptr_int.UnderlyingSystemType.IsArray);

            void Verify(Type type)
            {
                Assert.True(IsModifiedType(type));
                Assert.False(IsModifiedType(type.UnderlyingSystemType));
                Assert.True(IsModifiedType(type.GetElementType()));
                Assert.Equal(typeof(int).Project(), ptr_ptr_int.GetElementType().UnderlyingSystemType.GetElementType());
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71883", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public static unsafe void TestFields_Nested_FcnPtr()
        {
            Type ptr_fcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._ptr_fcnPtr), Bindings).GetModifiedFieldType();
            Assert.True(ptr_fcnPtr.IsPointer);
            Assert.True(IsModifiedType(ptr_fcnPtr));
            Verify(ptr_fcnPtr.GetElementType());

            Type array_ptr_fcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._array_ptr_fcnPtr), Bindings).GetModifiedFieldType();
            Assert.True(array_ptr_fcnPtr.IsArray);
            Assert.True(IsModifiedType(array_ptr_fcnPtr));
            Assert.True(array_ptr_fcnPtr.GetElementType().IsPointer);
            Assert.True(IsModifiedType(array_ptr_fcnPtr.GetElementType()));
            Verify(array_ptr_fcnPtr.GetElementType().GetElementType());

            Type fcnPtr_fcnPtrReturn = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._fcnPtr_fcnPtrReturn), Bindings).GetModifiedFieldType();
            Assert.True(fcnPtr_fcnPtrReturn.GetFunctionPointerReturnType().IsFunctionPointer);
            Assert.True(IsModifiedType(fcnPtr_fcnPtrReturn.GetFunctionPointerReturnType()));
            Verify(fcnPtr_fcnPtrReturn.GetFunctionPointerReturnType());

            void Verify(Type type)
            {
                Assert.True(IsModifiedType(type));
                Assert.True(type.IsFunctionPointer);
                Assert.Equal(0, type.GetFunctionPointerParameterTypes().Length);
                Assert.NotSame(typeof(void).Project(), type.GetFunctionPointerReturnType());
                Assert.Same(typeof(void).Project(), type.GetFunctionPointerReturnType().UnderlyingSystemType);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71883", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public static unsafe void TestFields_VerifyIdempotency()
        {
            // Call these again to ensure any backing caching strategy works.
            TestFields_Modified();
            TestFields_Unmodified();
            TestFields_Nested_Basic();
            TestFields_Nested_FcnPtr();
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71883", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public static unsafe void TestMethodParameters()
        {
            ParameterInfo[] parameters = typeof(ModifiedTypeHolder).Project().GetMethod(nameof(ModifiedTypeHolder.M_P0IntOut), Bindings).GetParameters();
            Assert.True(IsModifiedType(parameters[0].GetModifiedParameterType()));

            parameters = typeof(ModifiedTypeHolder).Project().GetMethod(nameof(ModifiedTypeHolder.M_P0FcnPtrOut), Bindings).GetParameters();
            Type[] fnParameters = parameters[0].GetModifiedParameterType().GetFunctionPointerParameterTypes();
            Assert.Equal(1, fnParameters.Length);
            Assert.Equal(typeof(OutAttribute).Project(), fnParameters[0].GetRequiredCustomModifiers()[0]);

            ParameterInfo returnParameter = typeof(ModifiedTypeHolder).Project().GetProperty(nameof(ModifiedTypeHolder.InitProperty_Int), Bindings).GetSetMethod().ReturnParameter;
            Assert.Equal(1, returnParameter.GetRequiredCustomModifiers().Length);
            Assert.Equal(1, returnParameter.GetModifiedParameterType().GetRequiredCustomModifiers().Length);
            Assert.Equal(typeof(IsExternalInit).Project(), returnParameter.GetModifiedParameterType().GetRequiredCustomModifiers()[0]);

            returnParameter = typeof(ModifiedTypeHolder).Project().GetProperty(nameof(ModifiedTypeHolder.Property_FcnPtr), Bindings).GetGetMethod().ReturnParameter;
            Assert.True(returnParameter.ParameterType.IsFunctionPointer);
            Assert.Equal(0, returnParameter.ParameterType.GetFunctionPointerParameterTypes()[0].GetRequiredCustomModifiers().Length);
            Assert.Equal(1, returnParameter.GetModifiedParameterType().GetFunctionPointerParameterTypes()[0].GetRequiredCustomModifiers().Length);
            Assert.Equal(typeof(OutAttribute).Project(), returnParameter.GetModifiedParameterType().GetFunctionPointerParameterTypes()[0].GetRequiredCustomModifiers()[0]);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71883", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public static unsafe void TestConstructorParameters()
        {
            ParameterInfo[] parameters = typeof(ModifiedTypeHolder).Project().GetConstructors()[0].GetParameters();

            Type param0 = parameters[0].ParameterType;
            Assert.True(param0.IsFunctionPointer);
            Assert.False(IsModifiedType(param0));
            Type[] mods = param0.GetFunctionPointerParameterTypes()[0].GetRequiredCustomModifiers();
            Assert.Equal(0, mods.Length);

            param0 = parameters[0].GetModifiedParameterType();
            Assert.True(param0.IsFunctionPointer);
            Assert.True(IsModifiedType(param0));
            mods = param0.GetFunctionPointerParameterTypes()[0].GetRequiredCustomModifiers();
            Assert.Equal(1, mods.Length);
            Assert.Equal(typeof(OutAttribute).Project(), mods[0]);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71883", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public static unsafe void TestFunctionPointerParameters_fcnPtrP0Out()
        {
            Type fcnPtr;

            fcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._fcnPtrP0Out), Bindings).GetModifiedFieldType();
            Verify(fcnPtr);
            Assert.True(IsModifiedType(fcnPtr));
            Assert.Equal(typeof(int).Project().MakeByRefType(), fcnPtr.GetFunctionPointerParameterTypes()[0].UnderlyingSystemType);

            fcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._fcnPtrP0Out), Bindings).FieldType;
            Verify(fcnPtr);
            Assert.False(IsModifiedType(fcnPtr));
            Assert.Equal(typeof(int).Project().MakeByRefType(), fcnPtr.GetFunctionPointerParameterTypes()[0]);

            void Verify(Type type)
            {
                Assert.True(type.IsFunctionPointer);
                Assert.Equal(1, type.GetFunctionPointerParameterTypes().Length);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71883", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public static unsafe void TestFunctionPointerParameters__fcnPtr_fcnPtrP0Out()
        {
            Type fcnPtr;
            Type param0;

            fcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._fcnPtr_fcnPtrP0Out), Bindings).GetModifiedFieldType();
            param0 = fcnPtr.GetFunctionPointerParameterTypes()[0];
            Verify(param0);
            Assert.True(IsModifiedType(param0));
            Assert.Equal(typeof(int).Project().MakeByRefType(), param0.GetFunctionPointerParameterTypes()[0].UnderlyingSystemType);
            Assert.Equal(1, param0.GetFunctionPointerParameterTypes()[0].GetRequiredCustomModifiers().Length);
            Assert.Equal(typeof(OutAttribute).Project(), param0.GetFunctionPointerParameterTypes()[0].GetRequiredCustomModifiers()[0]);

            fcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._fcnPtr_fcnPtrP0Out), Bindings).FieldType;
            param0 = fcnPtr.GetFunctionPointerParameterTypes()[0];
            Verify(param0);
            Assert.False(IsModifiedType(param0));
            Assert.Equal(typeof(int).Project().MakeByRefType(), param0.GetFunctionPointerParameterTypes()[0]);
            Assert.Equal(0, param0.GetFunctionPointerParameterTypes()[0].GetRequiredCustomModifiers().Length);

            void Verify(Type type)
            {
                Assert.True(type.IsFunctionPointer);
                Assert.Equal(1, type.GetFunctionPointerParameterTypes().Length);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71883", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public static unsafe void TestFunctionPointerParameters_fcnPtr_fcnPtrP0Ref()
        {
            Type fcnPtr;
            Type param0;

            fcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._fcnPtr_fcnPtrP0Ref), Bindings).GetModifiedFieldType();
            param0 = fcnPtr.GetFunctionPointerParameterTypes()[0];
            Verify(param0);
            Assert.True(IsModifiedType(param0));
            Assert.Equal(typeof(int).Project().MakeByRefType(), param0.GetFunctionPointerParameterTypes()[0].UnderlyingSystemType);

            fcnPtr = typeof(ModifiedTypeHolder).Project().GetField(nameof(ModifiedTypeHolder._fcnPtr_fcnPtrP0Ref), Bindings).FieldType;
            param0 = fcnPtr.GetFunctionPointerParameterTypes()[0];
            Verify(param0);
            Assert.False(IsModifiedType(param0));
            Assert.Equal(typeof(int).Project().MakeByRefType(), param0.GetFunctionPointerParameterTypes()[0]);

            void Verify(Type type)
            {
                Assert.True(type.IsFunctionPointer);
                Assert.Equal(1, type.GetFunctionPointerParameterTypes().Length);
            }
        }
        
        private static bool IsModifiedType(Type type)
        {
            return !ReferenceEquals(type, type.UnderlyingSystemType);
        }

        public unsafe class ModifiedTypeHolder
        {
            public ModifiedTypeHolder(delegate*<out int, void> d) { }

            public static volatile int _volatileInt;
            public static volatile int[] _volatileIntArray;
            public static volatile int* _volatileIntPointer;
            public static volatile delegate* unmanaged[Cdecl]<bool, void> _volatileFcnPtr;

            // Although function pointers can't be used in generics directly, they can be used indirectly
            // through an array or pointer.
            // NOTE: commented out due to compiler issue on NativeAOT:
            // public static volatile Tuple<delegate*<out bool, void>[]> _arrayGenericFcnPtr;

            public static int** _ptr_ptr_int;
            public static int*[] _array_ptr_int;
            public static delegate*<void>* _ptr_fcnPtr;
            public static delegate*<void>*[] _array_ptr_fcnPtr;
            public static delegate*<delegate*<void>> _fcnPtr_fcnPtrReturn;

            public static void M_P0IntOut(out int i) { i = 42; }
            public static void M_P0FcnPtrOut(delegate*<out int, void> fp) { }
            public static void M_ArrayOpenGenericFcnPtr<T>(T t, delegate*<out bool, void>[] fp) { }

            public int InitProperty_Int { get; init; }
            public static delegate*<out int, void> Property_FcnPtr { get; set; }

            public static delegate*<out int, void> FcnPtrP0Out { get; set; }
            public static delegate*<out int, void> _fcnPtrP0Out;
            public static delegate*<delegate*<out int, void>, void> _fcnPtr_fcnPtrP0Out;
            public static delegate*<delegate*<ref int, void>, void> _fcnPtr_fcnPtrP0Ref;
        }
    }
}
