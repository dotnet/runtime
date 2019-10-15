// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Xunit;

using static System.Tests.Utf8TestUtilities;

namespace System.Tests
{
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

        [Fact]
        public static void FormatterServices_GetUninitializedObject_Throws()
        {
            // Like String, shouldn't be able to create an uninitialized Utf8String.

            Assert.Throws<ArgumentException>(() => FormatterServices.GetSafeUninitializedObject(typeof(Utf8String)));
            Assert.Throws<ArgumentException>(() => FormatterServices.GetUninitializedObject(typeof(Utf8String)));
        }
    }
}
