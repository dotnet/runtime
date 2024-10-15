// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using System.Tests;

namespace System.IO.Tests
{
    public static class DirectoryNotFoundExceptionTests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new DirectoryNotFoundException();
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: HResults.COR_E_DIRECTORYNOTFOUND, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "That page was missing from the directory.";
            var exception = new DirectoryNotFoundException(message);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: HResults.COR_E_DIRECTORYNOTFOUND, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "That page was missing from the directory.";
            var innerException = new Exception("Inner exception");
            var exception = new DirectoryNotFoundException(message, innerException);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: HResults.COR_E_DIRECTORYNOTFOUND, innerException: innerException, message: message);
        }
    }
}
