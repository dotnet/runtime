// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Xunit;

namespace System.Tests
{
    public static partial class LazyTests
    {
        [Fact]
        public static void Ctor()
        {
            var lazyString = new Lazy<string>();
            VerifyLazy(lazyString, "", hasValue: false, isValueCreated: false);

            var lazyObject = new Lazy<int>();
            VerifyLazy(lazyObject, 0, hasValue: true, isValueCreated: false);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void Ctor_Bool(bool isThreadSafe)
        {
            var lazyString = new Lazy<string>(isThreadSafe);
            VerifyLazy(lazyString, "", hasValue: false, isValueCreated: false);
        }

        [Fact]
        public static void Ctor_ValueFactory()
        {
            var lazyString = new Lazy<string>(() => "foo");
            VerifyLazy(lazyString, "foo", hasValue: true, isValueCreated: false);

            var lazyInt = new Lazy<int>(() => 1);
            VerifyLazy(lazyInt, 1, hasValue: true, isValueCreated: false);
        }

        [Fact]
        public static void Ctor_ValueFactory_NullValueFactory_ThrowsArguentNullException()
        {
            Assert.Throws<ArgumentNullException>("valueFactory", () => new Lazy<object>(null)); // Value factory is null
        }

        [Fact]
        public static void Ctor_LazyThreadSafetyMode()
        {
            var lazyString = new Lazy<string>(LazyThreadSafetyMode.PublicationOnly);
            VerifyLazy(lazyString, "", hasValue: false, isValueCreated: false);
        }

        [Fact]
        public static void Ctor_LazyThreadSafetyMode_InvalidMode_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>("mode", () => new Lazy<string>(LazyThreadSafetyMode.None - 1)); // Invalid thread saftety mode
            Assert.Throws<ArgumentOutOfRangeException>("mode", () => new Lazy<string>(LazyThreadSafetyMode.ExecutionAndPublication + 1)); // Invalid thread saftety mode
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void Ctor_ValueFactor_Bool(bool isThreadSafe)
        {
            var lazyString = new Lazy<string>(() => "foo", isThreadSafe);
            VerifyLazy(lazyString, "foo", hasValue: true, isValueCreated: false);
        }

        [Fact]
        public static void Ctor_ValueFactory_Bool_NullValueFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("valueFactory", () => new Lazy<object>(null, false)); // Value factory is null
        }

        [Fact]
        public static void Ctor_ValueFactor_LazyThreadSafetyMode()
        {
            var lazyString = new Lazy<string>(() => "foo", LazyThreadSafetyMode.PublicationOnly);
            VerifyLazy(lazyString, "foo", hasValue: true, isValueCreated: false);

            var lazyInt = new Lazy<int>(() => 1, LazyThreadSafetyMode.PublicationOnly);
            VerifyLazy(lazyInt, 1, hasValue: true, isValueCreated: false);
        }

        [Fact]
        public static void Ctor_ValueFactor_LazyThreadSafetyMode_Invalid()
        {
            Assert.Throws<ArgumentNullException>("valueFactory", () => new Lazy<object>(null, LazyThreadSafetyMode.PublicationOnly)); // Value factory is null

            Assert.Throws<ArgumentOutOfRangeException>("mode", () => new Lazy<string>(() => "foo", LazyThreadSafetyMode.None - 1)); // Invalid thread saftety mode
            Assert.Throws<ArgumentOutOfRangeException>("mode", () => new Lazy<string>(() => "foof", LazyThreadSafetyMode.ExecutionAndPublication + 1)); // Invalid thread saftety mode
        }

        [Fact]
        public static void ToString_DoesntForceAllocation()
        {
            var lazy = new Lazy<object>(() => 1);
            Assert.NotEqual("1", lazy.ToString());
            Assert.False(lazy.IsValueCreated);

            object tmp = lazy.Value;
            Assert.Equal("1", lazy.ToString());
        }

        [Fact]
        public static void Value_Invalid()
        {
            string lazilyAllocatedValue = "abc";

            int x = 0;
            Lazy<string> lazy = null;
            lazy = new Lazy<string>(() =>  x++ < 5 ? lazy.Value : "Test", true);

            Assert.Throws<InvalidOperationException>(() => lazilyAllocatedValue = lazy.Value);
            Assert.Equal("abc", lazilyAllocatedValue);
        }

        [Theory]
        [InlineData(LazyThreadSafetyMode.ExecutionAndPublication)]
        [InlineData(LazyThreadSafetyMode.None)]
        public static void Value_ThrownException_DoesntCreateValue(LazyThreadSafetyMode mode)
        {
            var lazy = new Lazy<string>(() => { throw new DivideByZeroException(); }, mode);

            Exception exception1 = Assert.Throws<DivideByZeroException>(() => lazy.Value);
            Exception exception2 = Assert.Throws<DivideByZeroException>(() => lazy.Value);
            Assert.Same(exception1, exception2);

            Assert.False(lazy.IsValueCreated);
        }

        [Fact]
        public static void Value_ThrownException_DoesntCreateValue_PublicationOnly()
        {
            var lazy = new Lazy<string>(() => { throw new DivideByZeroException(); }, LazyThreadSafetyMode.PublicationOnly);

            Exception exception1 = Assert.Throws<DivideByZeroException>(() => lazy.Value);
            Exception exception2 = Assert.Throws<DivideByZeroException>(() => lazy.Value);
            Assert.NotSame(exception1, exception2);

            Assert.False(lazy.IsValueCreated);
        }

        [Fact]
        public static void EnsureInitalized_SimpleRefTypes()
        {
            var hdcTemplate = new HasDefaultCtor();
            string strTemplate = "foo";

            // Activator.CreateInstance (uninitialized).
            HasDefaultCtor a = null;
            Assert.NotNull(LazyInitializer.EnsureInitialized(ref a));
            Assert.Same(a, LazyInitializer.EnsureInitialized(ref a));
            Assert.NotNull(a);

            // Activator.CreateInstance (already initialized).
            HasDefaultCtor b = hdcTemplate;
            Assert.Equal(hdcTemplate, LazyInitializer.EnsureInitialized(ref b));
            Assert.Same(b, LazyInitializer.EnsureInitialized(ref b));
            Assert.Equal(hdcTemplate, b);

            // Func based initialization (uninitialized).
            string c = null;
            Assert.Equal(strTemplate, LazyInitializer.EnsureInitialized(ref c, () => strTemplate));
            Assert.Same(c, LazyInitializer.EnsureInitialized(ref c));
            Assert.Equal(strTemplate, c);

            // Func based initialization (already initialized).
            string d = strTemplate;
            Assert.Equal(strTemplate, LazyInitializer.EnsureInitialized(ref d, () => strTemplate + "bar"));
            Assert.Same(d, LazyInitializer.EnsureInitialized(ref d));
            Assert.Equal(strTemplate, d);
        }

        [Fact]
        public static void EnsureInitalized_SimpleRefTypes_Invalid()
        {
            // Func based initialization (nulls not permitted).
            string e = null;
            Assert.Throws<InvalidOperationException>(() => LazyInitializer.EnsureInitialized(ref e, () => null));

            // Activator.CreateInstance (for a type without a default ctor).
            NoDefaultCtor ndc = null;
            Assert.Throws<MissingMemberException>(() => LazyInitializer.EnsureInitialized(ref ndc));
        }

        [Fact]
        public static void EnsureInitialized_ComplexRefTypes()
        {
            string strTemplate = "foo";
            var hdcTemplate = new HasDefaultCtor();

            // Activator.CreateInstance (uninitialized).
            HasDefaultCtor a = null;
            bool aInit = false;
            object aLock = null;
            Assert.NotNull(LazyInitializer.EnsureInitialized(ref a, ref aInit, ref aLock));
            Assert.NotNull(a);

            // Activator.CreateInstance (already initialized).
            HasDefaultCtor b = hdcTemplate;
            bool bInit = true;
            object bLock = null;
            Assert.Equal(hdcTemplate, LazyInitializer.EnsureInitialized(ref b, ref bInit, ref bLock));
            Assert.Equal(hdcTemplate, b);

            // Func based initialization (uninitialized).
            string c = null;
            bool cInit = false;
            object cLock = null;
            Assert.Equal(strTemplate, LazyInitializer.EnsureInitialized(ref c, ref cInit, ref cLock, () => strTemplate));
            Assert.Equal(strTemplate, c);

            // Func based initialization (already initialized).
            string d = strTemplate;
            bool dInit = true;
            object dLock = null;
            Assert.Equal(strTemplate, LazyInitializer.EnsureInitialized(ref d, ref dInit, ref dLock, () => strTemplate + "bar"));
            Assert.Equal(strTemplate, d);

            // Func based initialization (nulls *ARE* permitted).
            string e = null;
            bool einit = false;
            object elock = null;
            int initCount = 0;

            Assert.Null(LazyInitializer.EnsureInitialized(ref e, ref einit, ref elock, () => { initCount++; return null; }));
            Assert.Null(e);
            Assert.Equal(1, initCount);
            Assert.Null(LazyInitializer.EnsureInitialized(ref e, ref einit, ref elock, () => { initCount++; return null; }));
        }

        [Fact]
        public static void EnsureInitalized_ComplexRefTypes_Invalid()
        {
            // Activator.CreateInstance (for a type without a default ctor).
            NoDefaultCtor ndc = null;
            bool ndcInit = false;
            object ndcLock = null;
            Assert.Throws<MissingMemberException>(() => LazyInitializer.EnsureInitialized(ref ndc, ref ndcInit, ref ndcLock));
        }

        [Fact]
        public static void LazyInitializerComplexValueTypes()
        {
            var empty = new LIX();
            var template = new LIX(33);

            // Activator.CreateInstance (uninitialized).
            LIX a = default(LIX);
            bool aInit = false;
            object aLock = null;
            LIX ensuredValA = LazyInitializer.EnsureInitialized(ref a, ref aInit, ref aLock);
            Assert.Equal(empty, ensuredValA);
            Assert.Equal(empty, a);

            // Activator.CreateInstance (already initialized).
            LIX b = template;
            bool bInit = true;
            object bLock = null;
            LIX ensuredValB = LazyInitializer.EnsureInitialized(ref b, ref bInit, ref bLock);
            Assert.Equal(template, ensuredValB);
            Assert.Equal(template, b);

            // Func based initialization (uninitialized).
            LIX c = default(LIX);
            bool cInit = false;
            object cLock = null;
            LIX ensuredValC = LazyInitializer.EnsureInitialized(ref c, ref cInit, ref cLock, () => template);
            Assert.Equal(template, c);
            Assert.Equal(template, ensuredValC);

            // Func based initialization (already initialized).
            LIX d = template;
            bool dInit = true;
            object dLock = null;
            LIX template2 = new LIX(template.f * 2);
            LIX ensuredValD = LazyInitializer.EnsureInitialized(ref d, ref dInit, ref dLock, () => template2);
            Assert.Equal(template, ensuredValD);
            Assert.Equal(template, d);
        }

        private static void VerifyLazy<T>(Lazy<T> lazy, T expectedValue, bool hasValue, bool isValueCreated)
        {
            Assert.Equal(isValueCreated, lazy.IsValueCreated);
            if (hasValue)
            {
                Assert.Equal(expectedValue, lazy.Value);
                Assert.True(lazy.IsValueCreated);
            }
            else
            {
                Assert.Throws<MissingMemberException>(() => lazy.Value); // Value could not be created
                Assert.False(lazy.IsValueCreated);
            }
        }

        private class HasDefaultCtor { }

        private class NoDefaultCtor
        {
            public NoDefaultCtor(int x) { }
        }

        private struct LIX
        {
            public int f;
            public LIX(int f) { this.f = f; }

            public override bool Equals(object other) => other is LIX && ((LIX)other).f == f;
            public override int GetHashCode() => f.GetHashCode();
            public override string ToString() => "LIX<" + f + ">";
        }
    }
}
