// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace System.Tests
{
    public static class ExceptionTests
    {
        [Fact]
        public static void Exception_GetType()
        {
            Assert.Equal(typeof(Exception), (new Exception()).GetType());
            Assert.Equal(typeof(NullReferenceException), (new NullReferenceException()).GetType());
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Exception.TargetSite always returns null on UapAot.")]
        public static void Exception_TargetSite_Jit()
        {
            bool caught = false;

            try
            {
                throw new Exception();
            }
            catch (Exception ex)
            {
                caught = true;

                Assert.Equal(MethodInfo.GetCurrentMethod(), ex.TargetSite);
            }

            Assert.True(caught);
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.UapAot, "Exception.TargetSite always returns null on UapAot.")]
        public static void Exception_TargetSite_Aot()
        {
            bool caught = false;

            try
            {
                throw new Exception();
            }
            catch (Exception ex)
            {
                caught = true;

                Assert.Null(ex.TargetSite);
            }

            Assert.True(caught);
        }
    }

    public class ExceptionDerivedTests: Exception
    {
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void Exception_SerializeObjectState()
        {
            var excp = new ExceptionDerivedTests();
            Assert.Throws<PlatformNotSupportedException>( () => excp.SerializeObjectState += (exception, eventArgs) => eventArgs.AddSerializedState(null));
            Assert.Throws<PlatformNotSupportedException>( () => excp.SerializeObjectState -= (exception, eventArgs) => eventArgs.AddSerializedState(null));
        }
    }
}
