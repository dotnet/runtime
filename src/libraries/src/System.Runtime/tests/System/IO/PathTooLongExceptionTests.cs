// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Xunit;

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

        [Fact]
        public static void OverlyLongPath_ThrowsPathTooLongException()
        {
            // This test case ensures that the PathTooLongException defined in System.IO.Primitives is the same that
            // is thrown by Path.  The S.IO.FS.P implementation forwards to the core assembly to ensure this is true.

            // Build up a path until GetFullPath throws, and verify that the right exception type
            // emerges from it and related APIs.
            var builder = new StringBuilder("directoryNameHere" + Path.DirectorySeparatorChar);
            string path = null;
            Assert.Throws<PathTooLongException>(new Action(() =>
            {
                while (true)
                {
                    path = builder.ToString();
                    Path.GetFullPath(path); // will eventually throw when path is too long
                builder.Append(path); // double the number of directories for the next time
            }
            }));
        }
    }
}
