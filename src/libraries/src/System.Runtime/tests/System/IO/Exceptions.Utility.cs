// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.IO.Tests
{
    public static class ExceptionUtility
    {
        public static void ValidateExceptionProperties(Exception e,
            int dataCount = 0,
            string helpLink = null,
            int hResult = HResults.COR_E_EXCEPTION,
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
