// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

public partial class FileLoadException_40100_Tests
{
    [Fact]
    public static void FileLoadException_ctor()
    {
        FileLoadException fle = new FileLoadException();
        Utility.ValidateExceptionProperties(fle, hResult: HResults.COR_E_FILELOAD, validateMessage: false);
        Assert.Equal(null, fle.FileName);
    }

    [Fact]
    public static void FileLoadException_ctor_string()
    {
        string message = "this is not the file you're looking for";
        FileLoadException fle = new FileLoadException(message);
        Utility.ValidateExceptionProperties(fle, hResult: HResults.COR_E_FILELOAD, message: message);
        Assert.Equal(null, fle.FileName);
    }

    [Fact]
    public static void FileLoadException_ctor_string_exception()
    {
        string message = "this is not the file you're looking for";
        Exception innerException = new Exception("Inner exception");
        FileLoadException fle = new FileLoadException(message, innerException);
        Utility.ValidateExceptionProperties(fle, hResult: HResults.COR_E_FILELOAD, innerException: innerException, message: message);
        Assert.Equal(null, fle.FileName);
    }

    [Fact]
    public static void FileLoadException_ctor_string_string()
    {
        string message = "this is not the file you're looking for";
        string fileName = "file.txt";
        FileLoadException fle = new FileLoadException(message, fileName);
        Utility.ValidateExceptionProperties(fle, hResult: HResults.COR_E_FILELOAD, message: message);
        Assert.Equal(fileName, fle.FileName);
    }

    [Fact]
    public static void FileLoadException_ctor_string_string_exception()
    {
        string message = "this is not the file you're looking for";
        string fileName = "file.txt";
        Exception innerException = new Exception("Inner exception");
        FileLoadException fle = new FileLoadException(message, fileName, innerException);
        Utility.ValidateExceptionProperties(fle, hResult: HResults.COR_E_FILELOAD, innerException: innerException, message: message);
        Assert.Equal(fileName, fle.FileName);
    }

    [Fact]
    public static void FileLoadException_ToString()
    {
        string message = "this is not the file you're looking for";
        string fileName = "file.txt";
        Exception innerException = new Exception("Inner exception");
        FileLoadException fle = new FileLoadException(message, fileName, innerException);
        var toString = fle.ToString();
        AssertContains(": " + message, toString);
        AssertContains(": '" + fileName + "'", toString);
        AssertContains("---> " + innerException.ToString(), toString);

        // set the stack trace
        try { throw fle; }
        catch
        {
            Assert.False(String.IsNullOrEmpty(fle.StackTrace));
            AssertContains(fle.StackTrace, fle.ToString());
        }
    }

    private static void AssertContains(string expected, string actual)
    {
        Assert.True(actual.Contains(expected), String.Format("\"{0}\" contains \"{1}\"", actual, expected));
    }
}