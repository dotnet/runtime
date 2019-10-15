// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public class ErrorEventArgsTests
    {
        [Fact]
        public void ErrorEventArgs_ctor()
        {
            Exception exception = new Exception();

            ErrorEventArgs args = new ErrorEventArgs(exception);

            Assert.Equal(exception, args.GetException());

            // Make sure method is consistent.
            Assert.Equal(exception, args.GetException());
        }

        [Fact]
        public void ErrorEventArgs_ctor_Null()
        {
            ErrorEventArgs args = new ErrorEventArgs(null);

            Assert.Null(args.GetException());

            // Make sure method is consistent.
            Assert.Null(args.GetException());
        }
    }
}