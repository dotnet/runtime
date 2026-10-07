// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters.Tests;
using System.Threading.Tasks;
using Xunit;

namespace System.Tests
{
    public static class TestExtensionMethod
    {
        public static DelegateTests.TestStruct TestFunc(this DelegateTests.TestClass testparam)
        {
            return testparam.structField;
        }

        public static void IncrementX(this DelegateTests.TestSerializableClass t)
        {
            t.x++;
        }
    }

    public static unsafe class DelegateTests
    {
        [Fact]
        public static void GetDelegateType_InvalidArguments()
        {
            AssertExtensions.Throws<ArgumentNullException>("typeArgs", () => RuntimeHelpers.GetDelegateType(null));
            AssertExtensions.Throws<ArgumentException>("typeArgs", () => RuntimeHelpers.GetDelegateType());
            AssertExtensions.Throws<ArgumentNullException>("typeArgs[1]", () => RuntimeHelpers.GetDelegateType(typeof(int), null));
            Assert.Throws<ArgumentException>(() => RuntimeHelpers.GetDelegateType(typeof(void), typeof(int)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(16)]
        public static void GetDelegateType_PredefinedTypes(int parameterCount)
        {
            Type[] signature = Enumerable.Repeat(typeof(int), parameterCount).Append(typeof(void)).ToArray();
            Assert.Same(Expression.GetDelegateType(signature), RuntimeHelpers.GetDelegateType(signature));
            signature[parameterCount] = typeof(string);
            Assert.Same(Expression.GetDelegateType(signature), RuntimeHelpers.GetDelegateType(signature));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotReflectionEmitSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public static void GetDelegateType_CustomGenerationNotSupported(bool highArity)
        {
            Type[] signature = highArity
                ? Enumerable.Repeat(typeof(int), 18).Append(typeof(void)).ToArray()
                : new[] { typeof(int).MakeByRefType(), typeof(void) };
            Assert.Throws<PlatformNotSupportedException>(() => RuntimeHelpers.GetDelegateType(signature));
            Assert.Throws<PlatformNotSupportedException>(() => Expression.GetDelegateType(signature));
        }

        public static IEnumerable<object[]> CustomDelegateSignatures()
        {
            yield return new object[] { new[] { typeof(int).MakeByRefType(), typeof(int) } };
            yield return new object[] { new[] { typeof(int).MakeByRefType() } };
            yield return new object[] { new[] { typeof(int*), typeof(void*) } };
            yield return new object[] { new[] { typeof(string).MakePointerType(), typeof(void) } };
            yield return new object[] { new[] { typeof(Span<int>), typeof(ReadOnlySpan<int>) } };
            yield return new object[] { new[] { typeof(List<int[]>).MakeByRefType(), typeof(Dictionary<string, List<int[,]>>) } };
            yield return new object[] { new[] { typeof(int[,]), typeof(int).MakeArrayType(1), typeof(int).MakeByRefType() } };
            yield return new object[] { new[] { typeof(delegate*<int, int>), typeof(void) } };
            yield return new object[] { new[] { typeof(delegate* unmanaged[Cdecl]<int, int>), typeof(void) } };
            yield return new object[] { new[] { typeof(delegate*<int, int>) } };
            yield return new object[] { new[] { typeof(delegate* unmanaged[Cdecl]<int, int>) } };
            yield return new object[] { Enumerable.Repeat(typeof(int), 18).ToArray() };
            yield return new object[] { Enumerable.Repeat(typeof(List<int>), 18).ToArray() };
            yield return new object[] { Enumerable.Repeat(typeof(int).MakeByRefType(), 130).Append(typeof(void)).ToArray() };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR))]
        [MemberData(nameof(CustomDelegateSignatures))]
        public static void GetDelegateType_CustomSignatures(Type[] signature)
        {
            Type delegateType = RuntimeHelpers.GetDelegateType(signature);
            Assert.Equal(typeof(MulticastDelegate), delegateType.BaseType);
            Assert.True(delegateType.IsSealed);
            MethodInfo invoke = delegateType.GetMethod("Invoke");
            Assert.Equal(signature[^1], invoke.ReturnType);
            Assert.Equal(signature.Take(signature.Length - 1), invoke.GetParameters().Select(p => p.ParameterType));
            Assert.Equal(MethodImplAttributes.Runtime, invoke.GetMethodImplementationFlags() & MethodImplAttributes.CodeTypeMask);
            Assert.NotNull(delegateType.GetConstructor(new[] { typeof(object), typeof(IntPtr) }));
            Assert.Same(delegateType, RuntimeHelpers.GetDelegateType((Type[])signature.Clone()));

            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName(nameof(GetDelegateType_CustomSignatures)), AssemblyBuilderAccess.RunAndCollect);
            TypeBuilder wrapper = assembly.DefineDynamicModule("Wrapper").DefineType("Wrapper", TypeAttributes.Public);
            // Ordinary Emit cannot encode function-pointer types; native ints still exercise importing Invoke.
            Type[] wrapperSignature = signature.Select(type => type.IsFunctionPointer ? typeof(IntPtr) : type).ToArray();
            MethodBuilder method = wrapper.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Static,
                wrapperSignature[^1], new[] { delegateType }.Concat(wrapperSignature.Take(signature.Length - 1)).ToArray());
            ILGenerator il = method.GetILGenerator();
            for (short i = 0; i < signature.Length; i++)
            {
                il.Emit(OpCodes.Ldarg, i);
            }
            il.Emit(OpCodes.Callvirt, invoke);
            il.Emit(OpCodes.Ret);
            MethodInfo wrapperMethod = wrapper.CreateType().GetMethod("Invoke");
            Assert.Equal(wrapperSignature[^1], wrapperMethod.ReturnType);
            RuntimeHelpers.PrepareMethod(wrapperMethod.MethodHandle);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public static void GetDelegateType_InvokeAndCombine()
        {
            Type delegateType = RuntimeHelpers.GetDelegateType(typeof(int).MakeByRefType(), typeof(int));
            MethodInfo target = typeof(DelegateTests).GetMethod(nameof(IncrementAndReturn), BindingFlags.NonPublic | BindingFlags.Static);
            Delegate first = target.CreateDelegate(delegateType);
            Delegate combined = Delegate.Combine(first, target.CreateDelegate(delegateType));
            object[] arguments = { 40 };
            Assert.Equal(42, combined.DynamicInvoke(arguments));
            Assert.Equal(42, arguments[0]);
            Assert.Equal(2, combined.GetInvocationList().Length);

            Delegate compiled = CompileIncrementExpression(delegateType, target);
            Assert.Equal(43, compiled.DynamicInvoke(arguments));

            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName(nameof(GetDelegateType_InvokeAndCombine)), AssemblyBuilderAccess.RunAndCollect);
            TypeBuilder wrapper = assembly.DefineDynamicModule("Wrapper").DefineType("Wrapper", TypeAttributes.Public);
            MethodBuilder method = wrapper.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Static,
                typeof(int), new[] { delegateType, typeof(int).MakeByRefType() });
            ILGenerator il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Callvirt, delegateType.GetMethod("Invoke"));
            il.Emit(OpCodes.Ret);
            object[] wrapperArguments = { first, 43 };
            Assert.Equal(44, wrapper.CreateType().GetMethod("Invoke").Invoke(null, wrapperArguments));
            Assert.Equal(44, wrapperArguments[1]);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR))]
        public static void GetDelegateType_CompiledExpressionWritesBackByRefArguments()
        {
            Type delegateType = RuntimeHelpers.GetDelegateType(typeof(int).MakeByRefType(), typeof(int));
            MethodInfo target = typeof(DelegateTests).GetMethod(nameof(IncrementAndReturn), BindingFlags.NonPublic | BindingFlags.Static);
            Delegate compiled = CompileIncrementExpression(delegateType, target);
            object[] arguments = { 42 };
            Assert.Equal(43, compiled.DynamicInvoke(arguments));
            Assert.Equal(43, arguments[0]);
        }

        private static Delegate CompileIncrementExpression(Type delegateType, MethodInfo target)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(int).MakeByRefType());
            return Expression.Lambda(delegateType, Expression.Call(target, parameter), parameter).Compile();
        }

        private static int IncrementAndReturn(ref int value) => ++value;

        public static IEnumerable<object[]> ReconstructedDelegateSignatures()
        {
            yield return new object[] { new[] { typeof(List<int>).MakeByRefType(), typeof(Dictionary<string, List<int[,]>>) } };
            yield return new object[] { new[] { typeof(int[,]).MakeByRefType(), typeof(int).MakeArrayType(1), typeof(int[,,]) } };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR))]
        [MemberData(nameof(ReconstructedDelegateSignatures))]
        public static void GetDelegateType_ReconstructedSignatures(Type[] signature)
        {
            Type delegateType = RuntimeHelpers.GetDelegateType(signature);
            Assert.False(delegateType.IsGenericType);
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName(nameof(GetDelegateType_ReconstructedSignatures)), AssemblyBuilderAccess.RunAndCollect);
            TypeBuilder wrapper = assembly.DefineDynamicModule("Wrapper").DefineType("Wrapper", TypeAttributes.Public);
            MethodBuilder method = wrapper.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Static,
                signature[^1], new[] { delegateType }.Concat(signature.Take(signature.Length - 1)).ToArray());
            ILGenerator il = method.GetILGenerator();
            for (short i = 0; i < signature.Length; i++)
            {
                il.Emit(OpCodes.Ldarg, i);
            }
            il.Emit(OpCodes.Callvirt, new ForwardingMethodInfo(delegateType.GetMethod("Invoke")));
            il.Emit(OpCodes.Ret);
            RuntimeHelpers.PrepareMethod(wrapper.CreateType().GetMethod("Invoke").MethodHandle);
        }

        private sealed class ForwardingMethodInfo(MethodInfo method) : MethodInfo
        {
            public override MethodAttributes Attributes => method.Attributes;
            public override CallingConventions CallingConvention => method.CallingConvention;
            public override Type DeclaringType => method.DeclaringType;
            public override RuntimeMethodHandle MethodHandle => method.MethodHandle;
            public override Module Module => method.Module;
            public override string Name => method.Name;
            public override Type ReflectedType => method.ReflectedType;
            public override Type ReturnType => method.ReturnType;
            public override ParameterInfo ReturnParameter => method.ReturnParameter;
            public override ICustomAttributeProvider ReturnTypeCustomAttributes => method.ReturnTypeCustomAttributes;
            public override MethodInfo GetBaseDefinition() => method.GetBaseDefinition();
            public override object[] GetCustomAttributes(bool inherit) => method.GetCustomAttributes(inherit);
            public override object[] GetCustomAttributes(Type attributeType, bool inherit) => method.GetCustomAttributes(attributeType, inherit);
            public override MethodImplAttributes GetMethodImplementationFlags() => method.GetMethodImplementationFlags();
            public override ParameterInfo[] GetParameters() => method.GetParameters();
            public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, Globalization.CultureInfo culture) =>
                method.Invoke(obj, invokeAttr, binder, parameters, culture);
            public override bool IsDefined(Type attributeType, bool inherit) => method.IsDefined(attributeType, inherit);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public static void GetDelegateType_SharedAssemblyAndConcurrentCache()
        {
            Type[] signature = { typeof(long).MakeByRefType(), typeof(long) };
            Type[] results = new Type[32];
            Parallel.For(0, results.Length, i => results[i] = RuntimeHelpers.GetDelegateType(signature));
            Assert.All(results, result => Assert.Same(results[0], result));
            Type other = RuntimeHelpers.GetDelegateType(typeof(byte).MakeByRefType(), typeof(byte));
            Assert.Same(results[0].Assembly, other.Assembly);
            Assert.False(other.IsCollectible);
            signature[0] = typeof(short).MakeByRefType();
            Assert.NotSame(results[0], RuntimeHelpers.GetDelegateType(signature));
            Assert.Same(results[0], RuntimeHelpers.GetDelegateType(typeof(long).MakeByRefType(), typeof(long)));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR))]
        [InlineData(false)]
        [InlineData(true)]
        public static void GetDelegateType_Collectible(bool useLoadContext)
        {
            WeakReference[] references = CreateCollectibleDelegateTypes(useLoadContext);
            for (int i = 0; i < 100 && references.Any(reference => reference.IsAlive); i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Assert.All(references, reference => Assert.False(reference.IsAlive));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference[] CreateCollectibleDelegateTypes(bool useLoadContext)
        {
            AssemblyLoadContext context = null;
            Type parameterType;
            if (useLoadContext)
            {
                context = new AssemblyLoadContext(nameof(GetDelegateType_Collectible), isCollectible: true);
                using FileStream stream = File.OpenRead(typeof(DelegateTests).Assembly.Location);
                Assembly assembly = context.LoadFromStream(stream);
                parameterType = assembly.GetType(typeof(TestClass).FullName, throwOnError: true);
            }
            else
            {
                AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                    new AssemblyName("DelegateFactoryInput"), AssemblyBuilderAccess.RunAndCollect);
                parameterType = assembly.DefineDynamicModule("Input").DefineType("Parameter", TypeAttributes.Public).CreateType();
            }

            Type first = RuntimeHelpers.GetDelegateType(parameterType.MakeByRefType(), typeof(void));
            Type second = RuntimeHelpers.GetDelegateType(typeof(List<>).MakeGenericType(parameterType).MakeByRefType(), typeof(void));
            Assert.True(first.IsCollectible);
            Assert.Same(first.Assembly, second.Assembly);
            Assert.Same(first, RuntimeHelpers.GetDelegateType(parameterType.MakeByRefType(), typeof(void)));
            Assert.Same(first, Expression.GetDelegateType(parameterType.MakeByRefType(), typeof(void)));
            Assert.Equal(parameterType.MakeByRefType(), first.GetMethod("Invoke").GetParameters()[0].ParameterType);

            MethodInfo target = typeof(DelegateTests).GetMethod(nameof(CollectibleTarget), BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(parameterType);
            Delegate instance = target.CreateDelegate(first);
            instance.DynamicInvoke(new object[] { null });
            context?.Unload();
            instance.DynamicInvoke(new object[] { null });

            return new[] { new WeakReference(parameterType), new WeakReference(first), new WeakReference(first.Assembly), new WeakReference(instance) };
        }

        private static void CollectibleTarget<T>(ref T value) { }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR))]
        [InlineData(false)]
        [InlineData(true)]
        public static void GetDelegateType_DistinctCollectibleTypeIdentities(bool verifyRetainedLifetime)
        {
            WeakReference[] references = CreateAndReleaseMixedCollectibleDelegateTypes(verifyRetainedLifetime);
            for (int i = 0; i < 100 && references.Any(reference => reference.IsAlive); i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Assert.All(references, reference => Assert.False(reference.IsAlive));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference[] CreateAndReleaseMixedCollectibleDelegateTypes(bool verifyRetainedLifetime)
        {
            (Type delegateType, WeakReference[] references) = CreateMixedCollectibleDelegateTypes();
            if (verifyRetainedLifetime)
            {
                for (int i = 0; i < 3; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                Assert.All(references, reference => Assert.True(reference.IsAlive));
            }
            GC.KeepAlive(delegateType);
            return references;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (Type, WeakReference[]) CreateMixedCollectibleDelegateTypes()
        {
            Type CreateInput()
            {
                AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                    new AssemblyName("SameIdentity"), AssemblyBuilderAccess.RunAndCollect);
                return assembly.DefineDynamicModule("Input").DefineType("Parameter", TypeAttributes.Public).CreateType();
            }

            Type first = CreateInput();
            Type second = CreateInput();
            Type mixed = RuntimeHelpers.GetDelegateType(first.MakeByRefType(), second);
            Assert.Equal(first.MakeByRefType(), mixed.GetMethod("Invoke").GetParameters()[0].ParameterType);
            Assert.Equal(second, mixed.GetMethod("Invoke").ReturnType);
            Assert.Same(mixed.Assembly, RuntimeHelpers.GetDelegateType(second.MakeByRefType(), typeof(void)).Assembly);
            Assert.NotSame(mixed.Assembly, RuntimeHelpers.GetDelegateType(first.MakeByRefType(), typeof(void)).Assembly);
            Assert.Same(mixed, RuntimeHelpers.GetDelegateType(first.MakeByRefType(), second));
            return (mixed, new[] { new WeakReference(first), new WeakReference(second), new WeakReference(mixed), new WeakReference(mixed.Assembly) });
        }

        public struct TestStruct
        {
            public object o1;
            public object o2;
        }

        public class TestClass
        {
            public TestStruct structField;
        }

        [Serializable]
        public class TestSerializableClass
        {
            public int x = 1;
        }

        private static void EmptyFunc() { }

        public delegate TestStruct StructReturningDelegate();

        [Fact]
        public static void ClosedStaticDelegate()
        {
            TestClass foo = new TestClass();
            foo.structField.o1 = new object();
            foo.structField.o2 = new object();
            StructReturningDelegate testDelegate = foo.TestFunc;
            TestStruct returnedStruct = testDelegate();
            Assert.Same(foo.structField.o1, returnedStruct.o1);
            Assert.Same(foo.structField.o2, returnedStruct.o2);
            Assert.Same(foo, testDelegate.Target);
            Assert.Equal(nameof(TestExtensionMethod.TestFunc), testDelegate.Method.Name);

            StructReturningDelegate equivalentDelegate = foo.TestFunc;
            Assert.Equal(testDelegate, equivalentDelegate);

            TestClass other = new TestClass();
            Assert.NotEqual(testDelegate, other.TestFunc);
        }

        public class A { }
        public class B : A { }
        public delegate A DynamicInvokeDelegate(A nonRefParam1, B nonRefParam2, ref A refParam, out B outParam);

        public static A DynamicInvokeTestFunction(A nonRefParam1, B nonRefParam2, ref A refParam, out B outParam)
        {
            outParam = (B)refParam;
            refParam = nonRefParam2;
            return nonRefParam1;
        }

        [Fact]
        public static void DynamicInvoke()
        {
            A a1 = new A();
            A a2 = new A();
            B b1 = new B();
            B b2 = new B();

            DynamicInvokeDelegate testDelegate = DynamicInvokeTestFunction;

            // Check that the delegate behaves as expected
            A refParam = b2;
            B outParam = null;
            A returnValue = testDelegate(a1, b1, ref refParam, out outParam);
            Assert.Same(returnValue, a1);
            Assert.Same(refParam, b1);
            Assert.Same(outParam, b2);

            // Check dynamic invoke behavior
            object[] parameters = new object[] { a1, b1, b2, null };

            object retVal = testDelegate.DynamicInvoke(parameters);
            Assert.Same(retVal, a1);
            Assert.Same(parameters[2], b1);
            Assert.Same(parameters[3], b2);

            // Check invoke on a delegate that takes no parameters.
            Action emptyDelegate = EmptyFunc;
            emptyDelegate.DynamicInvoke(new object[] { });
            emptyDelegate.DynamicInvoke(null);
        }

        [Fact]
        public static void DynamicInvoke_MissingTypeForCustomConstantAttribute_Succeeds()
        {
            Assert.Equal("SomeValue", (string)(new ObjectDelegateWithStringCustomConstantAttribute(ObjectMethod).DynamicInvoke(Type.Missing)));
        }

        [Fact]
        public static void DynamicInvoke_MissingTypeForCustomConstantAttributeWithDefault_Succeeds()
        {
            Assert.Equal("DefaultValue", new StringDelegateWithStringCustomConstantAttributeWithDefault(StringMethod).DynamicInvoke(Type.Missing));
        }

        [Fact]
        public static void DynamicInvoke_MissingTypeForTwoCustomConstantAttributes_Succeeds()
        {
            Assert.Equal("SomeValue", (string)(new ObjectDelegateWithTwoCustomConstantAttributes(ObjectMethod).DynamicInvoke(Type.Missing)));
        }

        [Fact]
        public static void DynamicInvoke_MissingTypeForDefaultParameter_Succeeds()
        {
            // Passing Type.Missing with default.
            Delegate d = new IntIntDelegateWithDefault(IntIntMethod);
            d.DynamicInvoke(7, Type.Missing);
        }

        [Fact]
        public static void DynamicInvoke_MissingTypeForNonDefaultParameter_ThrowsArgumentException()
        {
            Delegate d = new IntIntDelegate(IntIntMethod);
            AssertExtensions.Throws<ArgumentException>("parameters", () => d.DynamicInvoke(7, Type.Missing));
        }

        [Theory]
        [InlineData(new object[] { 7 }, new object[] { 8 })]
        [InlineData(new object[] { null }, new object[] { 1 })]
        public static void DynamicInvoke_RefValueTypeParameter(object[] args, object[] expected)
        {
            Delegate d = new RefIntDelegate(RefIntMethod);
            d.DynamicInvoke(args);
            Assert.Equal(expected, args);
        }

        [Fact]
        public static void DynamicInvoke_NullRefValueTypeParameter_ReturnsValueTypeDefault()
        {
            Delegate d = new RefValueTypeDelegate(RefValueTypeMethod);
            object[] args = new object[] { null };
            d.DynamicInvoke(args);
            MyStruct s = (MyStruct)(args[0]);
            Assert.Equal(7, s.X);
            Assert.Equal(8, s.Y);
        }

        [Fact]
        public static void DynamicInvoke_TypeDoesntExactlyMatchRefValueType_ThrowsArgumentException()
        {
            Delegate d = new RefIntDelegate(RefIntMethod);
            AssertExtensions.Throws<ArgumentException>(null, () => d.DynamicInvoke((uint)7));
            AssertExtensions.Throws<ArgumentException>(null, () => d.DynamicInvoke(IntEnum.One));
        }

        [Theory]
        [InlineData(7, (short)7)] // short -> int
        [InlineData(7, IntEnum.Seven)] // Enum (int) -> int
        [InlineData(7, ShortEnum.Seven)] // Enum (short) -> int
        public static void DynamicInvoke_ValuePreservingPrimitiveWidening_Succeeds(object o1, object o2)
        {
            Delegate d = new IntIntDelegate(IntIntMethod);
            d.DynamicInvoke(o1, o2);
        }

        [Theory]
        [InlineData(IntEnum.Seven, 7)]
        [InlineData(IntEnum.Seven, (short)7)]
        public static void DynamicInvoke_ValuePreservingWideningToEnum_Succeeds(object o1, object o2)
        {
            Delegate d = new EnumEnumDelegate(EnumEnumMethod);
            d.DynamicInvoke(o1, o2);
        }

        [Fact]
        public static void DynamicInvoke_SizePreservingNonVauePreservingConversion_ThrowsArgumentException()
        {
            Delegate d = new IntIntDelegate(IntIntMethod);
            AssertExtensions.Throws<ArgumentException>(null, () => d.DynamicInvoke(7, (uint)7));
            AssertExtensions.Throws<ArgumentException>(null, () => d.DynamicInvoke(7, U4.Seven));
        }

        [Fact]
        public static void DynamicInvoke_NullValueType_Succeeds()
        {
            Delegate d = new ValueTypeDelegate(ValueTypeMethod);
            d.DynamicInvoke(new object[] { null });
        }

        [Fact]
        public static void DynamicInvoke_ConvertMatchingTToNullable_Succeeds()
        {
            Delegate d = new NullableDelegate(NullableMethod);
            d.DynamicInvoke(7);
        }

        [Fact]
        public static void DynamicInvoke_ConvertNonMatchingTToNullable_ThrowsArgumentException()
        {
            Delegate d = new NullableDelegate(NullableMethod);
            AssertExtensions.Throws<ArgumentException>(null, () => d.DynamicInvoke((short)7));
            AssertExtensions.Throws<ArgumentException>(null, () => d.DynamicInvoke(IntEnum.Seven));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_AllPrimitiveParametersWithMissingValues()
        {
            object[] parameters = new object[13];
            for (int i = 0; i < parameters.Length; i++) { parameters[i] = Type.Missing; }

            Assert.Equal(
                "True, test, c, 2, -1, -3, 4, -5, 6, -7, 8, 9.1, 11.12",
                (string)(new AllPrimitivesWithDefaultValues(AllPrimitivesMethod)).DynamicInvoke(parameters));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_AllPrimitiveParametersWithAllExplicitValues()
        {
            Assert.Equal(
                "False, value, d, 102, -101, -103, 104, -105, 106, -107, 108, 109.1, 111.12",
                (string)(new AllPrimitivesWithDefaultValues(AllPrimitivesMethod)).DynamicInvoke(
                    new object[13]
                    {
                        false,
                        "value",
                        'd',
                        (byte)102,
                        (sbyte)-101,
                        (short)-103,
                        (ushort)104,
                        (int)-105,
                        (uint)106,
                        (long)-107,
                        (ulong)108,
                        (float)109.1,
                        (double)111.12
                    }
                    ));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_AllPrimitiveParametersWithSomeExplicitValues()
        {
            Assert.Equal(
                "False, test, d, 2, -101, -3, 104, -5, 106, -7, 108, 9.1, 111.12",
                (string)(new AllPrimitivesWithDefaultValues(AllPrimitivesMethod)).DynamicInvoke(
                    new object[13]
                    {
                        false,
                        Type.Missing,
                        'd',
                        Type.Missing,
                        (sbyte)-101,
                        Type.Missing,
                        (ushort)104,
                        Type.Missing,
                        (uint)106,
                        Type.Missing,
                        (ulong)108,
                        Type.Missing,
                        (double)111.12
                    }
                    ));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_StringParameterWithMissingValue()
        {
            Assert.Equal
                ("test",
                (string)(new StringWithDefaultValue(StringMethod)).DynamicInvoke(new object[] { Type.Missing }));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_StringParameterWithExplicitValue()
        {
            Assert.Equal(
                "value",
                (string)(new StringWithDefaultValue(StringMethod)).DynamicInvoke(new object[] { "value" }));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_ReferenceTypeParameterWithMissingValue()
        {
            Assert.Null((new ReferenceWithDefaultValue(ReferenceMethod)).DynamicInvoke(new object[] { Type.Missing }));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_ReferenceTypeParameterWithExplicitValue()
        {
            CustomReferenceType referenceInstance = new CustomReferenceType();
            Assert.Same(
                referenceInstance,
                (new ReferenceWithDefaultValue(ReferenceMethod)).DynamicInvoke(new object[] { referenceInstance }));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_ValueTypeParameterWithMissingValue()
        {
            Assert.Equal(
                0,
                ((CustomValueType)(new ValueTypeWithDefaultValue(ValueTypeMethod)).DynamicInvoke(new object[] { Type.Missing })).Id);
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_ValueTypeParameterWithExplicitValue()
        {
            Assert.Equal(
                1,
                ((CustomValueType)(new ValueTypeWithDefaultValue(ValueTypeMethod)).DynamicInvoke(new object[] { new CustomValueType { Id = 1 } })).Id);
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_DateTimeParameterWithMissingValue()
        {
            Assert.Equal(
                new DateTime(42),
                (DateTime)(new DateTimeWithDefaultValueAttribute(DateTimeMethod)).DynamicInvoke(new object[] { Type.Missing }));
        }

        [Fact]
        public static void DynamicInvoke_DateTimeAndCustomConstantAttribute_DateTimeParameterWithMissingValue()
        {
            Assert.Equal(
                new DateTime(42),
                (DateTime)(new DateTimeDelegateWithDateTimeAndCustomConstantAttribute(DateTimeMethod)).DynamicInvoke(new object[] { Type.Missing }));
        }

        [Fact]
        public static void DynamicInvoke_CustomConstantAndDateTimeAttribute_DateTimeParameterWithMissingValue()
        {
            Assert.Equal(
                new DateTime(43),
                (DateTime)(new DateTimeDelegateWithCustomConstantAndDateTimeAttribute(DateTimeMethod)).DynamicInvoke(new object[] { Type.Missing }));
        }

        [Fact]
        public static void DynamicInvoke_CustomConstantAttribute_DateTimeParameterWithMissingValue()
        {
            Assert.Equal(
                new DateTime(43),
                (DateTime)(new DateTimeDelegateWithCustomConstantAttribute(DateTimeMethod)).DynamicInvoke(new object[] { Type.Missing }));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_DateTimeParameterWithExplicitValue()
        {
            Assert.Equal(
                new DateTime(43),
                (DateTime)(new DateTimeWithDefaultValueAttribute(DateTimeMethod)).DynamicInvoke(new object[] { new DateTime(43) }));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_DecimalParameterWithAttributeAndMissingValue()
        {
            Assert.Equal(
                new decimal(4, 3, 2, true, 1),
                (decimal)(new DecimalWithDefaultValueAttribute(DecimalMethod)).DynamicInvoke(new object[] { Type.Missing }));
        }

        [Fact]
        public static void DynamicInvoke_DecimalAndCustomConstantAttribute_DecimalParameterWithAttributeAndMissingValue()
        {
            Assert.Equal(
                new decimal(12, 13, 14, true, 1),
                (decimal)(new DecimalDelegateWithDecimalAndCustomConstantAttribute(DecimalMethod)).DynamicInvoke(new object[] { Type.Missing }));
        }

        [Fact]
        public static void DynamicInvoke_CustomConstantAndDecimalAttribute_DecimalParameterWithAttributeAndMissingValue()
        {
            Assert.Equal(
                new decimal(12, 13, 14, true, 1),
                (decimal)(new DecimalDelegateWithCustomConstantAndDecimalAttribute(DecimalMethod)).DynamicInvoke(new object[] { Type.Missing }));
        }

        [Fact]
        public static void DynamicInvoke_CustomConstantAttribute_DecimalParameterWithAttributeAndMissingValue()
        {
            Assert.Equal(
                new decimal(12, 13, 14, true, 1),
                (decimal)(new DecimalDelegateWithCustomConstantAttribute(DecimalMethod)).DynamicInvoke(new object[] { Type.Missing }));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_DecimalParameterWithAttributeAndExplicitValue()
        {
            Assert.Equal(
                new decimal(12, 13, 14, true, 1),
                (decimal)(new DecimalWithDefaultValueAttribute(DecimalMethod)).DynamicInvoke(new object[] { new decimal(12, 13, 14, true, 1) }));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_DecimalParameterWithMissingValue()
        {
            Assert.Equal(
                3.14m,
                (decimal)(new DecimalWithDefaultValue(DecimalMethod)).DynamicInvoke(new object[] { Type.Missing }));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_DecimalParameterWithExplicitValue()
        {
            Assert.Equal(
                103.14m,
                (decimal)(new DecimalWithDefaultValue(DecimalMethod)).DynamicInvoke(new object[] { 103.14m }));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_NullableIntWithMissingValue()
        {
            Assert.Null((int?)(new NullableIntWithDefaultValue(NullableIntMethod)).DynamicInvoke(new object[] { Type.Missing }));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_NullableIntWithExplicitValue()
        {
            Assert.Equal(
                (int?)42,
                (int?)(new NullableIntWithDefaultValue(NullableIntMethod)).DynamicInvoke(new object[] { (int?)42 }));
        }

        [Fact]
        public static void DynamicInvoke_DefaultParameter_EnumParameterWithMissingValue()
        {
            Assert.Equal(
                IntEnum.Seven,
                (IntEnum)(new EnumWithDefaultValue(EnumMethod)).DynamicInvoke(new object[] { Type.Missing }));
        }

        [Fact]
        public static void DynamicInvoke_OptionalParameter_WithExplicitValue()
        {
            Assert.Equal(
                "value",
                (new OptionalObjectParameter(ObjectMethod)).DynamicInvoke(new object[] { "value" }));
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/15148", TestRuntimes.Mono)]
        public static void DynamicInvoke_OptionalParameter_WithMissingValue()
        {
            Assert.Equal(
                Type.Missing,
                (new OptionalObjectParameter(ObjectMethod)).DynamicInvoke(new object[] { Type.Missing }));
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/15148", TestRuntimes.Mono)]
        public static void DynamicInvoke_OptionalParameterUnassingableFromMissing_WithMissingValue()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => (new OptionalStringParameter(StringMethod)).DynamicInvoke(new object[] { Type.Missing }));
        }

        [Fact]
        public static void DynamicInvoke_ParameterSpecification_ArrayOfStrings()
        {
            Assert.Equal(
                "value",
               (new StringParameter(StringMethod)).DynamicInvoke(new string[] { "value" }));
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/15148", TestRuntimes.Mono)]
        public static void DynamicInvoke_ParameterSpecification_ArrayOfMissing()
        {
            Assert.Same(
                Missing.Value,
                (new OptionalObjectParameter(ObjectMethod)).DynamicInvoke(new Missing[] { Missing.Value }));
        }

        private static void IntIntMethod(int expected, int actual)
        {
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void DifferentMethodForDerivedBase()
        {
            var d1 = (Action<Derived>)Delegate.CreateDelegate(typeof(Action<Derived>), typeof(Base).GetMethod("M")!);
            var d2 = (Action<Derived>)Delegate.CreateDelegate(typeof(Action<Derived>), typeof(Derived).GetMethod("M")!);
            Assert.False(d1.Equals(d2));
            Assert.False(d1.Method.Equals(d2.Method));
        }

        [Fact]
        public static void SameMethodObtainedViaDelegateAndReflectionAreSameForClass()
        {
            var m1 = ((MethodCallExpression)((Expression<Action>)(() => new Class().M())).Body).Method;
            var m2 = new Action(new Class().M).Method;
            Assert.True(m1.Equals(m2));
        }

        [Fact]
        public static void SameMethodObtainedViaDelegateAndReflectionAreSameForStruct()
        {
            var m1 = ((MethodCallExpression)((Expression<Action>)(() => new Struct().M())).Body).Method;
            var m2 = new Action(new Struct().M).Method;
            Assert.True(m1.Equals(m2));
        }

        [Fact]
        public static void SameGenericMethodObtainedViaDelegateAndReflectionAreSameForClass()
        {
            var m1 = ((MethodCallExpression)((Expression<Action>)(() => new ClassG().M<string, object>())).Body).Method;
            var m2 = new Action(new ClassG().M<string, object>).Method;
            Assert.True(m1.Equals(m2));
            Assert.Equal(m1.GetHashCode(), m2.GetHashCode());
            Assert.Equal(m1.MethodHandle.Value, m2.MethodHandle.Value);
        }

        [Fact]
        public static void SameGenericMethodObtainedViaDelegateAndReflectionAreSameForStruct()
        {
            var m1 = ((MethodCallExpression)((Expression<Action>)(() => new StructG().M<string, object>())).Body).Method;
            var m2 = new Action(new StructG().M<string, object>).Method;
            Assert.True(m1.Equals(m2));
            Assert.Equal(m1.GetHashCode(), m2.GetHashCode());
            Assert.Equal(m1.MethodHandle.Value, m2.MethodHandle.Value);
        }

        [Fact]
        public static void DifferentOpenVirtualDelegates()
        {
            MethodInfo m1 = typeof(OpenVirtualClass).GetMethod(nameof(OpenVirtualClass.M1), BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo m2 = typeof(OpenVirtualClass).GetMethod(nameof(OpenVirtualClass.M2), BindingFlags.Instance | BindingFlags.NonPublic);

            Delegate a = m1.CreateDelegate<Action<OpenVirtualClass>>();
            Delegate b = m2.CreateDelegate<Action<OpenVirtualClass>>();

            Assert.False(a.Equals(b));

            Assert.Equal(m1, a.Method);
            Assert.Equal(m2, b.Method);
        }

        [Fact]
        public static void OpenVirtualDelegates_InvokeResolvesOverride()
        {
            Func<object, string> toString = typeof(object).GetMethod(nameof(object.ToString)).CreateDelegate<Func<object, string>>();
            Assert.Equal(nameof(OpenVirtualDerived), toString(new OpenVirtualDerived()));
            Assert.Equal(typeof(Struct).ToString(), toString(new Struct()));
            Assert.Equal(nameof(DayOfWeek.Monday), toString(DayOfWeek.Monday));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/134707", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        public static void OpenVirtualDelegates_InterfaceMethod_InvokeResolvesImplementation()
        {
            Func<IOpenVirtual, int> interfaceMethod = typeof(IOpenVirtual).GetMethod(nameof(IOpenVirtual.M)).CreateDelegate<Func<IOpenVirtual, int>>();
            Assert.Equal(1, interfaceMethod(new OpenVirtualDerived()));
            Assert.Equal(2, interfaceMethod(new OpenVirtualStruct()));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsTypeEquivalenceSupported))]
        public static void TypeEquivalentDelegatesPointingToSameMethod_AreEqualAndHaveSameHashCode()
        {
            // Get the type-equivalent delegate from System.TestEquivalentTypes.dll.
            // Both types are compiled from TestEquivalentTypes/System.TestEquivalentTypes.cs.
            Type otherDelegateType = Type.GetType($"{typeof(EquivalentDelegate).FullName}, System.TestEquivalentTypes", throwOnError: true)!;
            Assert.False(typeof(EquivalentDelegate).Equals(otherDelegateType));
            Assert.True(typeof(EquivalentDelegate).IsEquivalentTo(otherDelegateType));

            MethodInfo methodInfo = typeof(DelegateTests).GetMethod(nameof(DelegateEquivalentTypeTargetMethod), BindingFlags.Static | BindingFlags.NonPublic);
            Delegate a = Delegate.CreateDelegate(typeof(EquivalentDelegate), methodInfo);
            Delegate b = Delegate.CreateDelegate(otherDelegateType, methodInfo);

            // Delegates of type-equivalent types pointing to the same method should be equal
            // and must return the same hash code.
            Assert.True(a.Equals(b));
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        private static void DelegateEquivalentTypeTargetMethod() { }

        class Class { internal void M() { } }

        struct Struct { internal void M() { } }

        class ClassG { internal void M<Key, Value>() { } }

        struct StructG { internal void M<Key, Value>() { } }

        class OpenVirtualClass
        {
            internal virtual void M1() { }
            internal virtual void M2() { }
        }

        interface IOpenVirtual { int M(); }
        class OpenVirtualDerived : IOpenVirtual
        {
            public int M() => 1;
            public override string ToString() => nameof(OpenVirtualDerived);
        }
        struct OpenVirtualStruct : IOpenVirtual { public int M() => 2; }

        class Base { public virtual void M() { } }
        class Derived : Base { public override void M() { } }

        private delegate void IntIntDelegate(int expected, int actual);
        private delegate void IntIntDelegateWithDefault(int expected, int actual = 7);

        private static void RefIntMethod(ref int i) => i++;

        private delegate void RefIntDelegate(ref int i);

        private struct MyStruct
        {
            public int X;
            public int Y;
        }

        private static void RefValueTypeMethod(ref MyStruct s)
        {
            s.X += 7;
            s.Y += 8;
        }

        private delegate void RefValueTypeDelegate(ref MyStruct s);

        private static void EnumEnumMethod(IntEnum expected, IntEnum actual)
        {
            Assert.Equal(expected, actual);
        }

        private delegate void EnumEnumDelegate(IntEnum expected, IntEnum actual);

        private static void ValueTypeMethod(MyStruct s)
        {
            Assert.Equal(0, s.X);
            Assert.Equal(0, s.Y);
        }

        private delegate void ValueTypeDelegate(MyStruct s);

        private static void NullableMethod(int? n)
        {
            Assert.True(n.HasValue);
            Assert.Equal(7, n.Value);
        }

        private delegate void NullableDelegate(int? s);

        public enum ShortEnum : short
        {
            One = 1,
            Seven = 7,
        }

        public enum IntEnum : int
        {
            One = 1,
            Seven = 7,
        }

        public enum U4 : uint
        {
            One = 1,
            Seven = 7,
        }

        private delegate string AllPrimitivesWithDefaultValues(
            bool boolean = true,
            string str = "test",
            char character = 'c',
            byte unsignedbyte = 2,
            sbyte signedbyte = -1,
            short int16 = -3,
            ushort uint16 = 4,
            int int32 = -5,
            uint uint32 = 6,
            long int64 = -7,
            ulong uint64 = 8,
            float single = (float)9.1,
            double dbl = 11.12);

        private static string AllPrimitivesMethod(
            bool boolean,
            string str,
            char character,
            byte unsignedbyte,
            sbyte signedbyte,
            short int16,
            ushort uint16,
            int int32,
            uint uint32,
            long int64,
            ulong uint64,
            float single,
            double dbl)
        {
            return FormattableString.Invariant($"{boolean}, {str}, {character}, {unsignedbyte}, {signedbyte}, {int16}, {uint16}, {int32}, {uint32}, {int64}, {uint64}, {single}, {dbl}");
        }

        private delegate string StringParameter(string parameter);
        private delegate string StringWithDefaultValue(string parameter = "test");
        private static string StringMethod(string parameter)
        {
            return parameter;
        }

        private class CustomReferenceType { };

        private delegate CustomReferenceType ReferenceWithDefaultValue(CustomReferenceType parameter = null);
        private static CustomReferenceType ReferenceMethod(CustomReferenceType parameter)
        {
            return parameter;
        }

        private struct CustomValueType { public int Id; };

        private delegate CustomValueType ValueTypeWithDefaultValue(CustomValueType parameter = default(CustomValueType));
        private static CustomValueType ValueTypeMethod(CustomValueType parameter)
        {
            return parameter;
        }

        private delegate DateTime DateTimeWithDefaultValueAttribute([DateTimeConstant(42)] DateTime parameter);
        private static DateTime DateTimeMethod(DateTime parameter)
        {
            return parameter;
        }

        private delegate decimal DecimalWithDefaultValueAttribute([DecimalConstant(1, 1, 2, 3, 4)] decimal parameter);
        private delegate decimal DecimalWithDefaultValue(decimal parameter = 3.14m);
        private static decimal DecimalMethod(decimal parameter)
        {
            return parameter;
        }

        private delegate int? NullableIntWithDefaultValue(int? parameter = null);
        private static int? NullableIntMethod(int? parameter)
        {
            return parameter;
        }

        private delegate IntEnum EnumWithDefaultValue(IntEnum parameter = IntEnum.Seven);
        private static IntEnum EnumMethod(IntEnum parameter = IntEnum.Seven)
        {
            return parameter;
        }

        private delegate object OptionalObjectParameter([Optional] object parameter);
        private static object ObjectMethod(object parameter)
        {
            return parameter;
        }

        private delegate string OptionalStringParameter([Optional] string parameter);

        private class StringCustomConstantAttribute : CustomConstantAttribute
        {
            public override object Value => "SomeValue";
        }

        private class AnotherStringCustomConstantAttribute : CustomConstantAttribute
        {
            public override object Value => "SomeOtherValue";
        }

        private class AnotherDateTimeCustomConstantAttribute : CustomConstantAttribute
        {
            public override object Value => new DateTime(43);
        }

        private class AnotherDecimalCustomConstantAttribute : CustomConstantAttribute
        {
            public override object Value => new decimal(12, 13, 14, true, 1);
        }

        private delegate object ObjectDelegateWithStringCustomConstantAttribute([StringCustomConstant] object o);
        private delegate object ObjectDelegateWithTwoCustomConstantAttributes([StringCustomConstant][AnotherStringCustomConstant] object o);
        private delegate DateTime DateTimeDelegateWithDateTimeAndCustomConstantAttribute([DateTimeConstant(42)][AnotherDateTimeCustomConstant] DateTime parameter);
        private delegate DateTime DateTimeDelegateWithCustomConstantAndDateTimeAttribute([AnotherDateTimeCustomConstant][DateTimeConstant(42)] DateTime parameter);
        private delegate DateTime DateTimeDelegateWithCustomConstantAttribute([AnotherDateTimeCustomConstant] DateTime parameter);
        private delegate decimal DecimalDelegateWithDecimalAndCustomConstantAttribute([DecimalConstant(1, 1, 2, 3, 4)][AnotherDecimalCustomConstant] decimal parameter);
        private delegate decimal DecimalDelegateWithCustomConstantAndDecimalAttribute([AnotherDecimalCustomConstant][DecimalConstant(1, 1, 2, 3, 4)] decimal parameter);
        private delegate decimal DecimalDelegateWithCustomConstantAttribute([AnotherDecimalCustomConstant] decimal parameter);
        private delegate string StringDelegateWithStringCustomConstantAttributeWithDefault([StringCustomConstant] string o = "DefaultValue");
    }

    public static class CreateDelegateTests
    {
        #region Tests
        [Fact]
        public static void CreateDelegate1_Method_Static()
        {
            C c = new C();
            MethodInfo mi = typeof(C).GetMethod("S");
            Delegate dg = Delegate.CreateDelegate(typeof(D), mi);
            Assert.Equal(mi, dg.Method);
            Assert.Null(dg.Target);
            D d = (D)dg;
            d(c);
        }

        [Fact]
        public static void CreateDelegate1_Method_Null()
        {
            ArgumentNullException ex = AssertExtensions.Throws<ArgumentNullException>("method", () => Delegate.CreateDelegate(typeof(D), (MethodInfo)null));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate1_Type_Null()
        {
            MethodInfo mi = typeof(C).GetMethod("S");
            ArgumentNullException ex = AssertExtensions.Throws<ArgumentNullException>("type", () => Delegate.CreateDelegate((Type)null, mi));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate2()
        {
            E e;

            e = (E)Delegate.CreateDelegate(typeof(E), new B(), "Execute");
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            e = (E)Delegate.CreateDelegate(typeof(E), new C(), "Execute");
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            e = (E)Delegate.CreateDelegate(typeof(E), new C(), "DoExecute");
            Assert.NotNull(e);
            Assert.Equal(102, e(new C()));

            e = (E)Delegate.CreateDelegate(typeof(E), new B() { field = 42 }, "GetField");
            Assert.NotNull(e);
            Assert.Equal(42, e(new C()));
        }

        [Fact]
        public static void CreateDelegate2_Method_ArgumentsMismatch()
        {
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(null, () => Delegate.CreateDelegate(typeof(E), new B(), "StartExecute"));
            // Error binding to target method
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate2_Method_CaseMismatch()
        {
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(null, () => Delegate.CreateDelegate(typeof(E), new B(), "ExecutE"));
            // Error binding to target method
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate2_Method_DoesNotExist()
        {
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(null, () => Delegate.CreateDelegate(typeof(E), new B(), "DoesNotExist"));
            // Error binding to target method
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate2_Method_Null()
        {
            C c = new C();
            ArgumentNullException ex = AssertExtensions.Throws<ArgumentNullException>("method", () => Delegate.CreateDelegate(typeof(D), c, (string)null));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate2_Method_ReturnTypeMismatch()
        {
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(null, () => Delegate.CreateDelegate(typeof(E), new B(), "DoExecute"));
            // Error binding to target method
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate2_Method_Static()
        {
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(null, () => Delegate.CreateDelegate(typeof(E), new B(), "Run"));
            // Error binding to target method
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate2_Target_Null()
        {
            ArgumentNullException ex = AssertExtensions.Throws<ArgumentNullException>("target", () => Delegate.CreateDelegate(typeof(D), null, "N"));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate2_Target_GenericTypeParameter()
        {

            Type theT = typeof(DummyGenericClassForDelegateTests<>).GetTypeInfo().GenericTypeParameters[0];
            Type delegateType = typeof(Func<object, object, bool>);
            AssertExtensions.Throws<ArgumentException>("target", () => Delegate.CreateDelegate(delegateType, theT, "ReferenceEquals"));
        }

        [Fact]
        public static void CreateDelegate2_Type_Null()
        {
            C c = new C();
            ArgumentNullException ex = AssertExtensions.Throws<ArgumentNullException>("type", () => Delegate.CreateDelegate((Type)null, c, "N"));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate3()
        {
            E e;

            // matching static method
            e = (E)Delegate.CreateDelegate(typeof(E), typeof(B), "Run");
            Assert.NotNull(e);
            Assert.Equal(5, e(new C()));

            // matching static method
            e = (E)Delegate.CreateDelegate(typeof(E), typeof(C), "Run");
            Assert.NotNull(e);
            Assert.Equal(5, e(new C()));

            // matching static method
            e = (E)Delegate.CreateDelegate(typeof(E), typeof(C), "DoRun");
            Assert.NotNull(e);
            Assert.Equal(107, e(new C()));
        }

        [Fact]
        public static void CreateDelegate3_Method_ArgumentsMismatch()
        {
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(null, () => Delegate.CreateDelegate(typeof(E), typeof(B), "StartRun"));
            // Error binding to target method
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate3_Method_CaseMismatch()
        {
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(null, () => Delegate.CreateDelegate(typeof(E), typeof(B), "RuN"));
            // Error binding to target method
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate3_Method_DoesNotExist()
        {
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(null, () => Delegate.CreateDelegate(typeof(E), typeof(B), "DoesNotExist"));
            // Error binding to target method
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate3_Method_Instance()
        {
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(null, () => Delegate.CreateDelegate(typeof(E), typeof(B), "Execute"));
            // Error binding to target method
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate3_Method_Null()
        {
            ArgumentNullException ex = AssertExtensions.Throws<ArgumentNullException>("method", () => Delegate.CreateDelegate(typeof(D), typeof(C), (string)null));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate3_Method_ReturnTypeMismatch()
        {
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(null, () => Delegate.CreateDelegate(typeof(E), typeof(B), "DoRun"));
            // Error binding to target method
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate3_Target_Null()
        {
            ArgumentNullException ex = AssertExtensions.Throws<ArgumentNullException>("target", () => Delegate.CreateDelegate(typeof(D), (Type)null, "S"));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate3_Type_Null()
        {
            ArgumentNullException ex = AssertExtensions.Throws<ArgumentNullException>("type", () => Delegate.CreateDelegate((Type)null, typeof(C), "S"));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate4()
        {
            E e;

            B b = new B();

            // instance method, exact case, ignore case
            e = (E)Delegate.CreateDelegate(typeof(E), b, "Execute", true);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // instance method, exact case, do not ignore case
            e = (E)Delegate.CreateDelegate(typeof(E), b, "Execute", false);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // instance method, case mismatch, ignore case
            e = (E)Delegate.CreateDelegate(typeof(E), b, "ExecutE", true);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            C c = new C();

            // instance method, exact case, ignore case
            e = (E)Delegate.CreateDelegate(typeof(E), c, "Execute", true);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // instance method, exact case, ignore case
            e = (E)Delegate.CreateDelegate(typeof(E), c, "DoExecute", true);
            Assert.NotNull(e);
            Assert.Equal(102, e(new C()));

            // instance method, exact case, do not ignore case
            e = (E)Delegate.CreateDelegate(typeof(E), c, "Execute", false);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // instance method, case mismatch, ignore case
            e = (E)Delegate.CreateDelegate(typeof(E), c, "ExecutE", true);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));
        }

        [Fact]
        public static void CreateDelegate4_Method_ArgumentsMismatch()
        {
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(null, () => Delegate.CreateDelegate(typeof(E), new B(), "StartExecute", false));
            // Error binding to target method
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate4_Method_CaseMismatch()
        {
            // instance method, case mismatch, do not ignore case
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(null, () => Delegate.CreateDelegate(typeof(E), new B(), "ExecutE", false));
            // Error binding to target method
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate4_Method_DoesNotExist()
        {
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(null, () => Delegate.CreateDelegate(typeof(E), new B(), "DoesNotExist", false));
            // Error binding to target method
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate4_Method_Null()
        {
            ArgumentNullException ex = AssertExtensions.Throws<ArgumentNullException>("method", () => Delegate.CreateDelegate(typeof(D), new C(), (string)null, true));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate4_Method_ReturnTypeMismatch()
        {
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(null, () => Delegate.CreateDelegate(typeof(E), new B(), "DoExecute", false));
            // Error binding to target method
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate4_Method_Static()
        {
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(null, () => Delegate.CreateDelegate(typeof(E), new B(), "Run", true));
            // Error binding to target method
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate4_Target_Null()
        {
            ArgumentNullException ex = AssertExtensions.Throws<ArgumentNullException>("target", () => Delegate.CreateDelegate(typeof(D), null, "N", true));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate4_Type_Null()
        {
            C c = new C();
            ArgumentNullException ex = AssertExtensions.Throws<ArgumentNullException>("type", () => Delegate.CreateDelegate((Type)null, c, "N", true));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate9()
        {
            E e;

            // do not ignore case, do not throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new B(),
                "Execute", false, false);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // do not ignore case, throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new B(),
                "Execute", false, true);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // ignore case, do not throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new B(),
                "Execute", true, false);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // ignore case, throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new B(),
                "Execute", true, true);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // do not ignore case, do not throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new C(),
                "Execute", false, false);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // do not ignore case, throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new C(),
                "Execute", false, true);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // ignore case, do not throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new C(),
                "Execute", true, false);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // ignore case, throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new C(),
                "Execute", true, true);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // do not ignore case, do not throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new C(),
                "DoExecute", false, false);
            Assert.NotNull(e);
            Assert.Equal(102, e(new C()));

            // do not ignore case, throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new C(),
                "DoExecute", false, true);
            Assert.NotNull(e);
            Assert.Equal(102, e(new C()));

            // ignore case, do not throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new C(),
                "DoExecute", true, false);
            Assert.NotNull(e);
            Assert.Equal(102, e(new C()));

            // ignore case, throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new C(),
                "DoExecute", true, true);
            Assert.NotNull(e);
            Assert.Equal(102, e(new C()));
        }

        [Fact]
        public static void CreateDelegate9_Method_ArgumentsMismatch()
        {
            // throw bind failure
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(null, () => Delegate.CreateDelegate(typeof(E), new B(), "StartExecute", false, true));
            // Error binding to target method
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);

            // do not throw on bind failure
            E e = (E)Delegate.CreateDelegate(typeof(E), new B(),
                "StartExecute", false, false);
            Assert.Null(e);
        }

        [Fact]
        public static void CreateDelegate9_Method_CaseMismatch()
        {
            E e;

            // do not ignore case, throw bind failure
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(null, () => Delegate.CreateDelegate(typeof(E), new B(), "ExecutE", false, true));
            // Error binding to target method
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);

            // do not ignore case, do not throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new B(),
                "ExecutE", false, false);
            Assert.Null(e);

            // ignore case, throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new B(),
                "ExecutE", true, true);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));

            // ignore case, do not throw bind failure
            e = (E)Delegate.CreateDelegate(typeof(E), new B(),
                "ExecutE", true, false);
            Assert.NotNull(e);
            Assert.Equal(4, e(new C()));
        }

        [Fact]
        public static void CreateDelegate9_Method_DoesNotExist()
        {
            // throw bind failure
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(null, () => Delegate.CreateDelegate(typeof(E), new B(), "DoesNotExist", false, true));
            // Error binding to target method
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);

            // do not throw on bind failure
            E e = (E)Delegate.CreateDelegate(typeof(E), new B(),
                "DoesNotExist", false, false);
            Assert.Null(e);
        }

        [Fact]
        public static void CreateDelegate9_Method_Null()
        {
            ArgumentNullException ex = AssertExtensions.Throws<ArgumentNullException>("method", () => Delegate.CreateDelegate(typeof(E), new B(), (string)null, false, false));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate9_Method_ReturnTypeMismatch()
        {
            // throw bind failure
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(null, () => Delegate.CreateDelegate(typeof(E), new B(), "DoExecute", false, true));
            // Error binding to target method
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);

            // do not throw on bind failure
            E e = (E)Delegate.CreateDelegate(typeof(E), new B(),
                "DoExecute", false, false);
            Assert.Null(e);
        }

        [Fact]
        public static void CreateDelegate9_Method_Static()
        {
            // throw bind failure
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>(null, () => Delegate.CreateDelegate(typeof(E), new B(), "Run", true, true));
            // Error binding to target method
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);

            // do not throw on bind failure
            E e = (E)Delegate.CreateDelegate(typeof(E), new B(),
                "Run", true, false);
            Assert.Null(e);
        }

        [Fact]
        public static void CreateDelegate9_Target_Null()
        {
            ArgumentNullException ex = AssertExtensions.Throws<ArgumentNullException>("target", () => Delegate.CreateDelegate(typeof(E), (object)null, "Execute", true, false));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate9_Type_Null()
        {
            ArgumentNullException ex = AssertExtensions.Throws<ArgumentNullException>("type", () => Delegate.CreateDelegate((Type)null, new B(), "Execute", true, false));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
        }

        [Fact]
        public static void CreateDelegate10_Nullable_Method()
        {
            int? num = 123;
            MethodInfo mi = typeof(int?).GetMethod("ToString");
            NullableIntToString toString = (NullableIntToString)Delegate.CreateDelegate(
                typeof(NullableIntToString), mi);
            string s = toString(ref num);
            Assert.Equal(num.ToString(), s);
        }

        [Fact]
        public static void CreateDelegate10_Nullable_ClosedDelegate()
        {
            int? num = 123;
            MethodInfo mi = typeof(int?).GetMethod("ToString");
            AssertExtensions.Throws<ArgumentException>(
                () => Delegate.CreateDelegate(typeof(NullableIntToString), num, mi));
        }

        #endregion Tests

        #region Test Setup

        public class B
        {
            public int field;

            public virtual string retarg3(string s)
            {
                return s;
            }

            static int Run(C x)
            {
                return 5;
            }

            public static void DoRun(C x)
            {
            }

            public static int StartRun(C x, B b)
            {
                return 6;
            }

            int Execute(C c)
            {
                return 4;
            }

            public static void DoExecute(C c)
            {
            }

            public int StartExecute(C c, B b)
            {
                return 3;
            }

            public int GetField(C c)
            {
                return field;
            }
        }

        public class C : B, Iface
        {
            public string retarg(string s)
            {
                return s;
            }

            public string retarg2(Iface iface, string s)
            {
                return s + "2";
            }

            public override string retarg3(string s)
            {
                return s + "2";
            }

            static void Run(C x)
            {
            }

            public static new int DoRun(C x)
            {
                return 107;
            }

            void Execute(C c)
            {
            }

            public new int DoExecute(C c)
            {
                return 102;
            }

            public static void M()
            {
            }

            public static void N(C c)
            {
            }

            public static void S(C c)
            {
            }

            private void PrivateInstance()
            {
            }
        }

        public interface Iface
        {
            string retarg(string s);
        }

        public delegate void D(C c);
        public delegate int E(C c);

        delegate string NullableIntToString(ref int? obj);

        #endregion Test Setup
    }
}

internal class DummyGenericClassForDelegateTests<T> { }
