// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using Xunit;

namespace System.Tests
{
    public partial class ReflectionTests
    {
        [Fact]
        public static void FormatterServices_GetUninitializedObject_Throws()
        {
            // Like String, shouldn't be able to create an uninitialized Utf8String.

            Assert.Throws<ArgumentException>(() => FormatterServices.GetSafeUninitializedObject(typeof(Utf8String)));
            Assert.Throws<ArgumentException>(() => FormatterServices.GetUninitializedObject(typeof(Utf8String)));
        }
    }
}
