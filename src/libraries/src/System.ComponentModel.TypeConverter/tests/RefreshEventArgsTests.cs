// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class RefreshEventArgsTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData(typeof(int))]
        public void Ctor_Type(Type typeChanged)
        {
            var args = new RefreshEventArgs(typeChanged);
            Assert.Null(args.ComponentChanged);
            Assert.Same(typeChanged, args.TypeChanged);
        }

        public static IEnumerable<object[]> Ctor_Object_TestData()
        {
            if (!PlatformDetection.IsFullFramework)
            {
                yield return new object[] { null, null };
            }

                yield return new object[] { "componentChanged", typeof(string) };
        }

        [Theory]
        [MemberData(nameof(Ctor_Object_TestData))]
        public void Ctor_Object(object componentChanged, Type expectedTypeChanged)
        {
            var args = new RefreshEventArgs(componentChanged);
            Assert.Same(componentChanged, args.ComponentChanged);
            Assert.Equal(expectedTypeChanged, args.TypeChanged);
        }
    }
}
