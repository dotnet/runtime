// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

public class Utility
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
