// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace CoreFx.Private.TestUtilities.Tests
{
    public class AssertExtensionTests
    {
        public class OneException : Exception { }

        public class TwoException : Exception { }

        public class ThreeException : Exception { }

        public class NotMyException : Exception { }

        [Fact]
        public void ThrowsAny_Two_ThrowsExpected()
        {
            Action throwExpected = () => { throw new OneException(); };
            try
            {
                AssertExtensions.ThrowsAny<OneException, TwoException>(throwExpected);
            }
            catch (Exception e)
            {
                Assert.False(true, $"Should not have thrown for first, threw {e}");
            }

            throwExpected = () => { throw new TwoException(); };
            try
            {
                AssertExtensions.ThrowsAny<OneException, TwoException>(throwExpected);
            }
            catch (Exception e)
            {
                Assert.False(true, $"Should not have thrown for second, threw {e}");
            }
        }

        [Fact]
        public void ThrowsAny_Two_ThrowsUnexpected()
        {
            Action throwUnexpected = () => { throw new NotMyException(); };
            try
            {
                AssertExtensions.ThrowsAny<OneException, TwoException>(throwUnexpected);
            }
            catch (XunitException e)
            {
                Assert.Equal(
                    "Expected one of: (CoreFx.Private.TestUtilities.Tests.AssertExtensionTests+OneException, CoreFx.Private.TestUtilities.Tests.AssertExtensionTests+TwoException) -> Actual: (CoreFx.Private.TestUtilities.Tests.AssertExtensionTests+NotMyException)",
                    e.Message);
                return;
            }

            Assert.False(true, "Should have thrown");
        }

        [Fact]
        public void ThrowsAny_Two_DoesNotThrow()
        {
            Action doNothing = () => { };
            try
            {
                AssertExtensions.ThrowsAny<OneException, TwoException>(doNothing);
            }
            catch (XunitException e)
            {
                Assert.Equal(
                    "Expected one of: (CoreFx.Private.TestUtilities.Tests.AssertExtensionTests+OneException, CoreFx.Private.TestUtilities.Tests.AssertExtensionTests+TwoException) -> Actual: No exception thrown",
                    e.Message);
                return;
            }

            Assert.False(true, "Should have thrown");
        }


        [Fact]
        public void ThrowsAny_Three_ThrowsExpected()
        {
            Action throwExpected = () => { throw new ThreeException(); };
            try
            {
                AssertExtensions.ThrowsAny<OneException, TwoException, ThreeException>(throwExpected);
            }
            catch (Exception e)
            {
                Assert.False(true, $"Should not have thrown for first, threw {e}");
            }
        }

        [Fact]
        public void ThrowsAny_Three_ThrowsUnexpected()
        {
            Action throwUnexpected = () => { throw new NotMyException(); };
            try
            {
                AssertExtensions.ThrowsAny<OneException, TwoException, ThreeException>(throwUnexpected);
            }
            catch (XunitException)
            {
                // We're ok
                return;
            }

            Assert.False(true, "Should have thrown");
        }

        [Fact]
        public void ThrowsAny_Three_DoesNotThrow()
        {
            Action doNothing = () => { };
            try
            {
                AssertExtensions.ThrowsAny<OneException, TwoException, ThreeException>(doNothing);
            }
            catch (XunitException)
            {
                // We're ok
                return;
            }

            Assert.False(true, "Should have thrown");
        }
    }
}
