// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace System.Tests
{
    public static class ExceptionHelpers
    {
        public static void ValidateExceptionProperties(Exception e,
            int hResult,
            int dataCount = 0,
            string helpLink = null,
            Exception innerException = null,
            string message = null,
            string source = null,
            string stackTrace = null,
            bool validateMessage = true)
        {
            Assert.Equal(dataCount, e.Data.Count);
            Assert.Equal(helpLink, e.HelpLink);
            Assert.Equal(hResult, e.HResult);
            Assert.Equal(innerException, e.InnerException);
            if (validateMessage)
            {
                Assert.Equal(message, e.Message);
            }
            else
            {
                Assert.NotNull(e.Message);
            }
            Assert.Equal(source, e.Source);
            Assert.Equal(stackTrace, e.StackTrace);
        }
    }
}
