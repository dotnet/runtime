// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;
using Xunit;
using Xunit.Sdk;

namespace System.Reflection.Tests
{
    public class InvokeWithRefLikeArgs_Emit : InvokeWithRefLikeArgs
    {
        public InvokeWithRefLikeArgs_Emit() : base(useEmit: true) { }
    }

    public class InvokeWithRefLikeArgs_Interpreted : InvokeWithRefLikeArgs
    {
        public InvokeWithRefLikeArgs_Interpreted() : base(useEmit: false) { }
    }

    public abstract class InvokeWithRefLikeArgs : InvokeStrategy
    {
        public InvokeWithRefLikeArgs(bool useEmit) : base(useEmit) { }

        [Fact]
        public void MethodReturnsRefToRefStruct_ThrowsNSE()
        {
            MethodInfo mi = GetMethod(nameof(TestClass.ReturnsRefToRefStruct));
            Assert.Throws<NotSupportedException>(() => Invoke(mi, null, null));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtimelab/issues/155", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public void MethodTakesRefStructAsArg_ThrowsNSE()
        {
            MethodInfo mi = GetMethod(nameof(TestClass.TakesRefStructAsArg));

            object[] args = new object[] { null };
            Assert.Throws<NotSupportedException>(() => Invoke(mi, null, args));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtimelab/issues/155", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public void MethodTakesRefStructAsArgWithDefaultValue_ThrowsNSE()
        {
            MethodInfo mi = GetMethod(nameof(TestClass.TakesRefStructAsArgWithDefaultValue));

            object[] args = new object[] { Type.Missing };
            Assert.Throws<NotSupportedException>(() => Invoke(mi, null, args));
        }

        // Moq heavily utilizes RefEmit, which does not work on most aot workloads
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        [SkipOnMono("https://github.com/dotnet/runtime/issues/40738")]
        public void MethodTakesRefToRefStructAsArg_ThrowsNSE()
        {
            // Use a Binder to trick the reflection stack into treating the returned null
            // as meaning "use the default value of the ref struct".

            Mock<Binder> mockBinder = new Mock<Binder>(MockBehavior.Strict);
            Type myRefStructType = typeof(MyRefStruct);
            mockBinder.Setup(o => o.ChangeType("hello", myRefStructType.MakeByRefType(), null)).Returns((object)null);

            MethodInfo mi = GetMethod(nameof(TestClass.TakesRefToRefStructAsArg));
            Assert.Throws<NotSupportedException>(() => Invoke(mi, null, BindingFlags.InvokeMethod, mockBinder.Object, new object[] { "hello" }, null));
        }

        [Fact]
        [SkipOnMono("https://github.com/dotnet/runtime/issues/40738")]
        public void MethodTakesOutToRefStructAsArg_ThrowsNSE()
        {
            MethodInfo mi = GetMethod(nameof(TestClass.TakesOutToRefStructAsArg));
            Assert.Throws<NotSupportedException>(() => Invoke(mi, null, new object[] { null }));
        }

        [Fact]
        public void PropertyTypedAsRefToRefStruct_AsMethodInfo_ThrowsNSE()
        {
            MethodInfo mi = GetMethod("get_" + nameof(TestClass.PropertyTypedAsRefToRefStruct));
            Assert.Throws<NotSupportedException>(() => Invoke(mi, null, null));
        }

        [Fact]
        public void PropertyTypedAsRefToRefStruct_AsPropInfo_ThrowsNSE()
        {
            PropertyInfo pi = typeof(TestClass).GetProperty(nameof(TestClass.PropertyTypedAsRefToRefStruct));
            Assert.NotNull(pi);
            Assert.Throws<NotSupportedException>(() => GetValue(pi, null));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtimelab/issues/155", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public void PropertyIndexerWithRefStructArg_ThrowsNSE()
        {
            PropertyInfo pi = typeof(TestClassWithIndexerWithRefStructArg).GetProperty("Item");
            Assert.NotNull(pi);

            object obj = new TestClassWithIndexerWithRefStructArg();
            object[] args = new object[] { null };

            Assert.Throws<NotSupportedException>(() => GetValue(pi, obj, args));
            Assert.Throws<NotSupportedException>(() => SetValue(pi, obj, 42, args));
        }

        private sealed class TestClass
        {
            private static int _backingField = 42;

            public unsafe static ref MyRefStruct ReturnsRefToRefStruct()
            {
                fixed (int* pInt = &_backingField)
                {
                    return ref *(MyRefStruct*)pInt; // will return a valid ref
                }
            }

            public static void TakesRefStructAsArg(MyRefStruct o)
            {
                Assert.Equal(0, o.MyInt); // should be default(T)
            }

            public static void TakesRefStructAsArgWithDefaultValue(MyRefStruct o = default)
            {
                Assert.Equal(0, o.MyInt); // should be default(T)
            }

            public static void TakesRefToRefStructAsArg(ref MyRefStruct o)
            {
                throw new XunitException("Should never be called.");
            }

            public static void TakesOutToRefStructAsArg(out MyRefStruct o)
            {
                throw new XunitException("Should never be called.");
            }

            public static ref MyRefStruct PropertyTypedAsRefToRefStruct
            {
                get { return ref ReturnsRefToRefStruct(); }
            }
        }

        private static MethodInfo GetMethod(string name)
        {
            MethodInfo mi = typeof(TestClass).GetMethod(name, BindingFlags.Static | BindingFlags.Public);
            Assert.NotNull(mi);
            return mi;
        }

        private sealed class TestClassWithIndexerWithRefStructArg
        {
            public int this[MyRefStruct o]
            {
                get
                {
                    Assert.Equal(0, o.MyInt); // should be default(T)
                    return 42;
                }
                set
                {
                    Assert.Equal(0, o.MyInt); // should be default(T)
                }
            }
        }

        private ref struct MyRefStruct
        {
            public int MyInt;
        }
    }
}
