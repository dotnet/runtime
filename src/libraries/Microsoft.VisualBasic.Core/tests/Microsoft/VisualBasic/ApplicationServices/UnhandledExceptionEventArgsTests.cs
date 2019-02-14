// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.VisualBasic.ApplicationServices.Tests
{
    public class UnhandledExceptionEventArgsTests
    {
        public static IEnumerable<object[]> Ctor_Bool_Exception_TestData()
        {
            yield return new object[] { true, null };
            yield return new object[] { false, new Exception() };
        }

        [Theory]
        [MemberData(nameof(Ctor_Bool_Exception_TestData))]
        public void Ctor_Bool_Exception(bool exitApplication, Exception exception)
        {
            var args = new UnhandledExceptionEventArgs(exitApplication, exception);
            Assert.Same(exception, args.Exception);
            Assert.Equal(exitApplication, args.ExitApplication);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExitApplication_Set_GetReturnsExpected(bool value)
        {
            var args = new UnhandledExceptionEventArgs(true, null);
            args.ExitApplication = value;
            Assert.Equal(value, args.ExitApplication);
        }
    }
}
