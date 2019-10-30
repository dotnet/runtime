// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Xunit;
using System.Tests;

namespace System.IO.Tests
{
    public static class PathTooLongExceptionTests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new PathTooLongException();
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: HResults.COR_E_PATHTOOLONG, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "This path is too long to hike in a single day.";
            var exception = new PathTooLongException(message);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: HResults.COR_E_PATHTOOLONG, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "This path is too long to hike in a single day.";
            var innerException = new Exception("Inner exception");
            var exception = new PathTooLongException(message, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: HResults.COR_E_PATHTOOLONG, innerException: innerException, message: message);
        }
    }
}
