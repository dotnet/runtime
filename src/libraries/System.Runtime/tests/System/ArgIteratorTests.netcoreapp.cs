// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;
using Xunit;

namespace System.Tests
{
    public static class ArgIteratorTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsArgIteratorNotSupported))]
        public unsafe static void ArgIterator_Throws_PlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new ArgIterator(new RuntimeArgumentHandle()));
            Assert.Throws<PlatformNotSupportedException>(() => {
                fixed (void* p = "test")
                {
                    new ArgIterator(new RuntimeArgumentHandle(), p);
                }
            });
            Assert.Throws<PlatformNotSupportedException>(() => new ArgIterator().End());
            Assert.Throws<PlatformNotSupportedException>(() => new ArgIterator().Equals(new object()));
            Assert.Throws<PlatformNotSupportedException>(() => new ArgIterator().GetHashCode());
            Assert.Throws<PlatformNotSupportedException>(() => new ArgIterator().GetNextArg());
            Assert.Throws<PlatformNotSupportedException>(() => new ArgIterator().GetNextArg(new RuntimeTypeHandle()));
            Assert.Throws<PlatformNotSupportedException>(() => new ArgIterator().GetNextArgType());
            Assert.Throws<PlatformNotSupportedException>(() => new ArgIterator().GetRemainingCount());
        }
    }
}
