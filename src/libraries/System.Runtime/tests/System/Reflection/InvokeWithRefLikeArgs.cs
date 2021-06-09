// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;
using Xunit;
using Xunit.Sdk;

namespace System.Reflection.Tests
{
    public class InvokeWithRefLikeArgs
    {
        [Fact]
        public static void MethodReturnsRefToRefStruct_ThrowsNSE()
        {
            MethodInfo mi = GetMethod(nameof(TestClass.ReturnsRefToRefStruct));
            Assert.Throws<NotSupportedException>(() => mi.Invoke(null, null));
        }

        [Fact]
        public static void MethodTakesRefStructAsArg_DoesNotCopyValueBack()
        {
            MethodInfo mi = GetMethod(nameof(TestClass.TakesRefStructAsArg));

            object[] args = new object[] { null };
            mi.Invoke(null, args);

            Assert.Null(args[0]); // no value should have been copied back
        }

        [Fact]
        public static void MethodTakesRefStructAsArgWithDefaultValue_DoesNotCopyValueBack()
        {
            MethodInfo mi = GetMethod(nameof(TestClass.TakesRefStructAsArgWithDefaultValue));

            object[] args = new object[] { Type.Missing };
            mi.Invoke(null, args);

            Assert.Null(args[0]); // no value should have been copied back
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))] // Moq uses Reflection.Emit
        [SkipOnMono("https://github.com/dotnet/runtime/issues/40738")]
        public static void MethodTakesRefToRefStructAsArg_ThrowsNSE()
        {
            // Use a Binder to trick the reflection stack into treating the returned null
            // as meaning "use the default value of the ref struct".

            Mock<Binder> mockBinder = new Mock<Binder>(MockBehavior.Strict);
            Type myRefStructType = typeof(MyRefStruct);
            mockBinder.Setup(o => o.ChangeType("hello", myRefStructType.MakeByRefType(), null)).Returns((object)null);

            MethodInfo mi = GetMethod(nameof(TestClass.TakesRefToRefStructAsArg));
            Assert.Throws<NotSupportedException>(() => mi.Invoke(null, BindingFlags.InvokeMethod, mockBinder.Object, new object[] { "hello" }, null));
        }

        [Fact]
        [SkipOnMono("https://github.com/dotnet/runtime/issues/40738")]
        public static void MethodTakesOutToRefStructAsArg_ThrowsNSE()
        {
            MethodInfo mi = GetMethod(nameof(TestClass.TakesOutToRefStructAsArg));
            Assert.Throws<NotSupportedException>(() => mi.Invoke(null, new object[] { null }));
        }

        [Fact]
        public static void PropertyTypedAsRefToRefStruct_AsMethodInfo_ThrowsNSE()
        {
            MethodInfo mi = GetMethod("get_" + nameof(TestClass.PropertyTypedAsRefToRefStruct));
            Assert.Throws<NotSupportedException>(() => mi.Invoke(null, null));
        }

        [Fact]
        public static void PropertyTypedAsRefToRefStruct_AsPropInfo_ThrowsNSE()
        {
            PropertyInfo pi = typeof(TestClass).GetProperty(nameof(TestClass.PropertyTypedAsRefToRefStruct));
            Assert.NotNull(pi);
            Assert.Throws<NotSupportedException>(() => pi.GetValue(null));
        }

        [Fact]
        public static void PropertyIndexerWithRefStructArg_DoesNotCopyValueBack()
        {
            PropertyInfo pi = typeof(TestClassWithIndexerWithRefStructArg).GetProperty("Item");
            Assert.NotNull(pi);

            object obj = new TestClassWithIndexerWithRefStructArg();
            object[] args = new object[] { null };

            object retVal = pi.GetValue(obj, args);
            Assert.Equal(42, retVal);
            Assert.Null(args[0]); // no value should have been copied back

            pi.SetValue(obj, 42, args);
            Assert.Null(args[0]); // no value should have been copied back
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
