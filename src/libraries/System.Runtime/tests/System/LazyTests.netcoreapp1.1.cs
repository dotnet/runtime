// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    }
}
