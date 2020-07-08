// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using Xunit;

using static System.Tests.Utf8TestUtilities;

namespace System.Tests
{
    [SkipOnMono("The features from System.Utf8String.Experimental namespace are experimental.")]
    public partial class ReflectionTests
    {
        [Fact]
        public static void ActivatorCreateInstance_CanCallParameterfulCtor()
        {
            Utf8String theString = (Utf8String)Activator.CreateInstance(typeof(Utf8String), "Hello");
            Assert.Equal(u8("Hello"), theString);
        }

        [Fact]
        public static void ActivatorCreateInstance_CannotCallParameterlessCtor()
        {
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(Utf8String)));
        }
    }
}
