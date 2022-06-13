// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.CSharp.RuntimeBinder;
using Xunit;

namespace System.Dynamic.Tests
{
    public class InvokeMemberBindingTests
    {
        private class MinimumOverrideInvokeMemberBinding : InvokeMemberBinder
        {
            public MinimumOverrideInvokeMemberBinding(string name, bool ignoreCase, CallInfo callInfo)
                : base(name, ignoreCase, callInfo)
            {
            }

            public override DynamicMetaObject FallbackInvokeMember(
                DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
            {
                throw new NotSupportedException();
            }

            public override DynamicMetaObject FallbackInvoke(
                DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
            {
                throw new NotSupportedException();
            }
        }

        private class TestBaseClass
        {
            public object Method(int x, int y) => new TestBaseClass();

            public string TellType<T>(T item) => typeof(T).Name;

            public bool TryParseInt(string value, out int result) => int.TryParse(value, out result);
        }

        private class TestDerivedClass : TestBaseClass
        {
            public new string Method(int x, int y) => "Hiding";
        }

        public static IEnumerable<object[]> ObjectArguments()
        {
            yield return new object[] {0};
            yield return new object[] {""};
            yield return new object[] {new Uri("http://example.net/")};
            yield return new[] {new object()};
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/55070", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        [Theory, MemberData(nameof(ObjectArguments))]
        public void InvokeVirtualMethod(object value)
        {
            dynamic d = value;
            Assert.Equal(value.ToString(), d.ToString());
        }

        [Theory, MemberData(nameof(ObjectArguments))]
        public void InvokeNonVirtualMethod(object value)
        {
            dynamic d = value;
            Assert.Equal(value.GetType(), d.GetType());
        }

        [Theory, MemberData(nameof(ObjectArguments))]
        public void InvokeCaseInsensitiveFails(dynamic value)
        {
            Assert.Throws<RuntimeBinderException>(() => value.tostring());
        }

        // TODO: Use a case-insensitive binder so that the above actually works.
        // https://github.com/dotnet/runtime/issues/19482

        [Fact]
        public void MethodHiding()
        {
            dynamic d = new TestDerivedClass();
            Assert.Equal("Hiding", d.Method(1, 2));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/69944", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public void GenericMethod()
        {
            dynamic d = new TestDerivedClass();
            Assert.Equal(nameof(Int32), d.TellType(0));
            Assert.Equal(nameof(TestDerivedClass), d.TellType(d));
            // Explicit type selection.
            Assert.Equal(nameof(TestBaseClass), d.TellType<TestBaseClass>(d));
        }

        [Fact]
        public void ByRef()
        {
            dynamic d = new TestDerivedClass();
            int x;
            dynamic s = "21";
            Assert.True(d.TryParseInt(s, out x));
            Assert.Equal(21, x);
        }

        [Fact]
        public void GenericType()
        {
            dynamic d = new List<int>();
            dynamic x = 32;
            d.Add(x);
            d.Add(x);
            int tally = 0;
            foreach (int item in d)
            {
                tally += item;
            }

            Assert.Equal(64, tally);
        }

        [Fact]
        public void NoSuchMethod()
        {
            dynamic d = new object();
            Assert.Throws<RuntimeBinderException>(() => d.MagicallyFixAllTheBugs());
        }

        [Fact]
        public void NoArgumentMatch()
        {
            dynamic d = new TestDerivedClass();
            Assert.Throws<RuntimeBinderException>(() => d.Method());
        }

        [Fact]
        public void NotAMethod()
        {
            dynamic d = "A string";
            Assert.Throws<RuntimeBinderException>(() => d.Length());
        }

        [Fact]
        public void NullName()
        {
            CallInfo info = new CallInfo(0);
            AssertExtensions.Throws<ArgumentNullException>(
                "name", () => new MinimumOverrideInvokeMemberBinding(null, false, info));
        }

        [Fact]
        public void NullCallInfo()
        {
            AssertExtensions.Throws<ArgumentNullException>(
                "callInfo", () => new MinimumOverrideInvokeMemberBinding("Name", false, null));
        }

        [Fact]
        public void NameStored()
        {
            var binding = new MinimumOverrideInvokeMemberBinding("My test name", false, new CallInfo(0));
            Assert.Equal("My test name", binding.Name);
        }

        [Fact]
        public void TypeIsObject()
        {
            Assert.Equal(
                typeof(object), new MinimumOverrideInvokeMemberBinding("name", true, new CallInfo(0)).ReturnType);
        }

        [Fact]
        public void IgnoreCaseStored()
        {
            CallInfo info = new CallInfo(0);
            Assert.False(new MinimumOverrideInvokeMemberBinding("name", false, info).IgnoreCase);
            Assert.True(new MinimumOverrideInvokeMemberBinding("name", true, info).IgnoreCase);
        }

        [Fact]
        public void CallInfoStored()
        {
            CallInfo info = new CallInfo(0);
            Assert.Same(info, new MinimumOverrideInvokeMemberBinding("name", false, info).CallInfo);
        }

        private static dynamic GetObjectWithNonIndexerParameterProperty(bool hasGetter, bool hasSetter)
        {
            TypeBuilder typeBuild = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("TestAssembly"), AssemblyBuilderAccess.RunAndCollect)
                .DefineDynamicModule("TestModule")
                .DefineType("TestType", TypeAttributes.Public);
            FieldBuilder field = typeBuild.DefineField("_value", typeof(int), FieldAttributes.Private);

            PropertyBuilder property = typeBuild.DefineProperty(
                "ItemProp", PropertyAttributes.None, typeof(int), new[] { typeof(int) });

            if (hasGetter)
            {
                MethodBuilder getter = typeBuild.DefineMethod(
                    "get_ItemProp",
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig
                    | MethodAttributes.PrivateScope, typeof(int), new[] { typeof(int) });

                ILGenerator ilGen = getter.GetILGenerator();
                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldfld, field);
                ilGen.Emit(OpCodes.Ret);

                property.SetGetMethod(getter);
            }

            if (hasSetter)
            {

                MethodBuilder setter = typeBuild.DefineMethod(
                    "set_ItemProp",
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig
                    | MethodAttributes.PrivateScope, typeof(void), new[] { typeof(int), typeof(int) });

                ILGenerator ilGen = setter.GetILGenerator();
                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ldarg_2);
                ilGen.Emit(OpCodes.Stfld, field);
                ilGen.Emit(OpCodes.Ret);

                property.SetSetMethod(setter);
            }

            return Activator.CreateInstance(typeBuild.CreateType());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void NonIndexerParameterizedDirectAccess()
        {
            // If a parameterized property isn't the type's indexer, we should be allowed to use the
            // getter or setter directly.
            dynamic d = GetObjectWithNonIndexerParameterProperty(true, true);
            d.set_ItemProp(2, 19);
            int value = d.get_ItemProp(2);
            Assert.Equal(19, value);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void NonIndexerParameterizedGetterAndSetterIndexAccess()
        {
            dynamic d = GetObjectWithNonIndexerParameterProperty(true, true);
            RuntimeBinderException ex = Assert.Throws<RuntimeBinderException>(() => d.ItemProp[2] = 3);
            // Similar message to CS1545 advises about getter and setter methods.
            Assert.Contains("get_ItemProp", ex.Message);
            Assert.Contains("set_ItemProp", ex.Message);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void NonIndexerParameterizedGetterOnlyIndexAccess()
        {
            dynamic d = GetObjectWithNonIndexerParameterProperty(true, false);
            int dump;
            RuntimeBinderException ex = Assert.Throws<RuntimeBinderException>(() => dump = d.ItemProp[2]);
            // Similar message to CS1546 advises about getter method.
            Assert.Contains("get_ItemProp", ex.Message);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void NonIndexerParameterizedSetterOnlyIndexAccess()
        {
            dynamic d = GetObjectWithNonIndexerParameterProperty(false, true);
            RuntimeBinderException ex = Assert.Throws<RuntimeBinderException>(() => d.ItemProp[2] = 9);
            // Similar message to CS1546 advises about setter method.
            Assert.Contains("set_ItemProp", ex.Message);
        }

        private class ManyOverloads
        {
            public int GetValue() => 0;

            public int GetValue(int arg0) => 1;

            public int GetValue(int arg0, int arg1) => 2;

            public int GetValue(int arg0, int arg1, int arg2) => 3;

            public int GetValue(int arg0, int arg1, int arg2, int arg3) => 4;

            public int GetValue(int arg0, int arg1, int arg2, int arg3, int arg4) => 5;

            public int GetValue(int arg0, int arg1, int arg2, int arg3, int arg4, int arg5) => 6;

            public int GetValue(int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6) => 7;

            public int GetValue(int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, int arg7) => 8;

            public int GetValue(
                int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, int arg7, int arg8) => 9;

            public int GetValue(
                int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, int arg7, int arg8, int arg9) =>
                10;

            public int GetValue(
                int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, int arg7, int arg8, int arg9,
                int arg10) => 11;
            public int GetValue2() => 0;

            public int GetValue2(int arg0) => 1;

            public int GetValue2(int arg0, int arg1) => 2;

            public int GetValue2(int arg0, int arg1, int arg2) => 3;

            public int GetValue2(int arg0, int arg1, int arg2, int arg3) => 4;

            public int GetValue2(int arg0, int arg1, int arg2, int arg3, int arg4) => 5;

            public int GetValue2(int arg0, int arg1, int arg2, int arg3, int arg4, int arg5) => 6;

            public int GetValue2(int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6) => 7;

            public int GetValue2(int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, int arg7) => 8;

            public int GetValue2(
                int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, int arg7, int arg8) => 9;

            public int GetValue2(
                int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, int arg7, int arg8, int arg9) =>
                10;

            public int GetValue2(
                int arg0, int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, int arg7, int arg8, int arg9,
                int arg10) => 11;
        }

        [Fact]
        public void ManyArities()
        {
            dynamic d = new ManyOverloads();
            Assert.Equal(0, d.GetValue());
            Assert.Equal(0, d.GetValue2());
            Assert.Equal(1, d.GetValue(0));
            Assert.Equal(1, d.GetValue2(0));
            Assert.Equal(2, d.GetValue(0, 1));
            Assert.Equal(2, d.GetValue2(0, 1));
            Assert.Equal(3, d.GetValue(0, 1, 2));
            Assert.Equal(3, d.GetValue2(0, 1, 2));
            Assert.Equal(4, d.GetValue(0, 1, 2, 3));
            Assert.Equal(4, d.GetValue2(0, 1, 2, 3));
            Assert.Equal(5, d.GetValue(0, 1, 2, 3, 4));
            Assert.Equal(5, d.GetValue2(0, 1, 2, 3, 4));
            Assert.Equal(6, d.GetValue(0, 1, 2, 3, 4, 5));
            Assert.Equal(6, d.GetValue2(0, 1, 2, 3, 4, 5));
            Assert.Equal(7, d.GetValue(0, 1, 2, 3, 4, 5, 6));
            Assert.Equal(7, d.GetValue2(0, 1, 2, 3, 4, 5, 6));
            Assert.Equal(8, d.GetValue(0, 1, 2, 3, 4, 5, 6, 7));
            Assert.Equal(8, d.GetValue2(0, 1, 2, 3, 4, 5, 6, 7));
            Assert.Equal(9, d.GetValue(0, 1, 2, 3, 4, 5, 6, 7, 8));
            Assert.Equal(9, d.GetValue2(0, 1, 2, 3, 4, 5, 6, 7, 8));
            Assert.Equal(10, d.GetValue(0, 1, 2, 3, 4, 5, 6, 7, 8, 9));
            Assert.Equal(10, d.GetValue2(0, 1, 2, 3, 4, 5, 6, 7, 8, 9));
            Assert.Equal(11, d.GetValue(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10));
            Assert.Equal(11, d.GetValue2(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10));
        }

        public static IEnumerable<object[]> SameNameObjectPairs()
        {
            object[] testObjects = Enumerable.Range(0, 4)
                .Select(
                    _ => Activator.CreateInstance(
                        AssemblyBuilder
                            .DefineDynamicAssembly(new AssemblyName("TestAssembly"), AssemblyBuilderAccess.RunAndCollect)
                            .DefineDynamicModule("TestModule")
                            .DefineType("TestType", TypeAttributes.Public)
                            .CreateType()))
                .ToArray();
            return testObjects.SelectMany(i => testObjects.Select(j => new[] { i, j }));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        [MemberData(nameof(SameNameObjectPairs))]
        public void OperationOnTwoObjectsDifferentTypesOfSameName(object x, object y)
        {
            dynamic dX = x;
            dynamic dY = y;
            bool equal = dX.Equals(dY);
            Assert.Equal(x == y, equal);
        }

        public class FuncWrapper<TResult>
        {
            public delegate void OutAction(out TResult arg);
            private Func<TResult> _delegate;

            public Func<TResult> Delegate
            {
                get => _delegate;
                set
                {
                    _delegate = value;
                    OutDelegate = value == null ? default(OutAction) : (out TResult arg) =>
                    {
                        arg = value();
                    };
                }
            }

            public OutAction OutDelegate;
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/55071", typeof(PlatformDetection), nameof(PlatformDetection.IsLinqExpressionsBuiltWithIsInterpretingOnly))]
        [Fact]
        public void InvokeFuncMember()
        {
            dynamic d = new FuncWrapper<int>
            {
                Delegate = () => 2
            };
            int result = d.Delegate();
            Assert.Equal(2, result);
            result = 0;
            d.OutDelegate(out result);
            Assert.Equal(2, result);
        }
    }
}
