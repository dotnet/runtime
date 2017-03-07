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
        public static void Ctor_Value_ReferenceType()
        {
            var lazyString = new Lazy<string>("abc");
            VerifyLazy(lazyString, "abc", hasValue: true, isValueCreated: true);
        }

        [Fact]
        public static void Ctor_Value_ValueType()
        {
            var lazyObject = new Lazy<int>(123);
            VerifyLazy(lazyObject, 123, hasValue: true, isValueCreated: true);
        }

        [Fact]
        public static void EnsureInitialized_FuncInitializationWithoutTrackingBool_Uninitialized()
        {
            string template = "foo";
            string target = null;
            object syncLock = null;
            Assert.Equal(template, LazyInitializer.EnsureInitialized(ref target, ref syncLock, () => template));
            Assert.Equal(template, target);
            Assert.NotNull(syncLock);
        }

        [Fact]
        public static void EnsureInitialized_FuncInitializationWithoutTrackingBool_Initialized()
        {
            string template = "foo";
            string target = template;
            object syncLock = null;
            Assert.Equal(template, LazyInitializer.EnsureInitialized(ref target, ref syncLock, () => template + "bar"));
            Assert.Equal(template, target);
            Assert.Null(syncLock);
        }

        [Fact]
        public static void EnsureInitializer_FuncInitializationWithoutTrackingBool_Null()
        {
            string target = null;
            object syncLock = null;
            Assert.Throws<InvalidOperationException>(() => LazyInitializer.EnsureInitialized(ref target, ref syncLock, () => null));
        }
    }
}
