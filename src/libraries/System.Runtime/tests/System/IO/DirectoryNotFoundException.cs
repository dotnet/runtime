// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

public partial class DirectoryNotFoundException_40100_Tests
{
    [Fact]
    public static void DirectoryNotFoundException_ctor()
    {
        DirectoryNotFoundException dnfe = new DirectoryNotFoundException();
        Utility.ValidateExceptionProperties(dnfe, hResult: HResults.COR_E_DIRECTORYNOTFOUND, validateMessage: false);
    }

    [Fact]
    public static void DirectoryNotFoundException_ctor_string()
    {
        string message = "That page was missing from the directory.";
        DirectoryNotFoundException dnfe = new DirectoryNotFoundException(message);
        Utility.ValidateExceptionProperties(dnfe, hResult: HResults.COR_E_DIRECTORYNOTFOUND, message: message);
    }

    [Fact]
    public static void DirectoryNotFoundException_ctor_string_exception()
    {
        string message = "That page was missing from the directory.";
        Exception innerException = new Exception("Inner exception");
        DirectoryNotFoundException dnfe = new DirectoryNotFoundException(message, innerException);
        Utility.ValidateExceptionProperties(dnfe, hResult: HResults.COR_E_DIRECTORYNOTFOUND, innerException: innerException, message: message);
    }
}
