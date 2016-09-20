// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public partial class AppContextTests
    {
        [Fact]
        public void UnhandledException_IsNull()
        {
            Assert.IsNull(AppContext.UnhandledException);
        }

        [Fact]
        public void UnhandledException_Add_Remove()
        {
            AppContext.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
            Assert.IsNotNull(AppContext.UnhandledException);
            AppContext.UnhandledException -= new UnhandledExceptionEventHandler(MyHandler);
            Assert.IsNull(AppContext.UnhandledException);
        }

        [Fact]
        public void UnhandledException_NotCalled_When_Handled()
        {
            AppContext.UnhandledException += new UnhandledExceptionEventHandler(NotExpectedToBeCalledHandler);
            try {
                throw new Exception();
            }
            catch(Exception e)
            {
            }
            AppContext.UnhandledException -= new UnhandledExceptionEventHandler(NotExpectedToBeCalledHandler);
        }

        static void NotExpectedToBeCalledHandler(object sender, UnhandledExceptionEventArgs args) 
        {
            Assert.Fail("UnhandledException handler not expected to be called");
        }

        static void MyHandler(object sender, UnhandledExceptionEventArgs args) 
        {
            Exception e = (Exception) args.ExceptionObject;
        }
    }
}