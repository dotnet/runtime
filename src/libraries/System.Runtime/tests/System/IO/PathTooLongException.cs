// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using Xunit;

public partial class PathTooLongException_40100_Tests
{
    [Fact]
    public static void PathTooLongException_ctor()
    {
        PathTooLongException plte = new PathTooLongException();
        Utility.ValidateExceptionProperties(plte, hResult: HResults.COR_E_PATHTOOLONG, validateMessage: false);
    }

    [Fact]
    public static void PathTooLongException_ctor_string()
    {
        string message = "This path is too long to hike in a single day.";
        PathTooLongException plte = new PathTooLongException(message);
        Utility.ValidateExceptionProperties(plte, hResult: HResults.COR_E_PATHTOOLONG, message: message);
    }

    [Fact]
    public static void PathTooLongException_ctor_string_exception()
    {
        string message = "This path is too long to hike in a single day.";
        Exception innerException = new Exception("Inner exception");
        PathTooLongException plte = new PathTooLongException(message, innerException);
        Utility.ValidateExceptionProperties(plte, hResult: HResults.COR_E_PATHTOOLONG, innerException: innerException, message: message);
    }

    [Fact]
    public static void PathTooLongException_From_Path()
    {
        // This test case ensures that the PathTooLongException defined in System.IO.Primitives is the same that
        // is thrown by Path.  The S.IO.FS.P implementation forwards to the core assembly to ensure this is true.

        // Build up a path until GetFullPath throws, and verify that the right exception type
        // emerges from it and related APIs.
        var sb = new StringBuilder("directoryNameHere" + Path.DirectorySeparatorChar);
        string path = null;
        Assert.Throws<PathTooLongException>(new Action(() =>
        {
            while (true)
            {
                path = sb.ToString();
                Path.GetPathRoot(path); // will eventually throw when path is too long
                sb.Append(path); // double the number of directories for the next time
            }
        }));
        Assert.Throws<PathTooLongException>(() => Path.GetFullPath(path));
        Assert.Throws<PathTooLongException>(() => Path.GetDirectoryName(path));
    }
}
