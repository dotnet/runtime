// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureNotFoundExceptionTests
    {
        [Fact]
        public void TestMessage()
        {
            CultureNotFoundException cnf = new CultureNotFoundException("this is a test string");
            Assert.Equal("this is a test string", cnf.Message);
        }

        [Fact]
        public void TestInnerExceptionMessage()
        {
            CultureNotFoundException cnf = new CultureNotFoundException("this is a test string", new Exception("inner exception string"));

            Assert.Equal("this is a test string", cnf.Message);
            Assert.Equal("inner exception string", cnf.InnerException.Message);
        }

        [Fact]
        public void TestParamName()
        {
            CultureNotFoundException cnf = new CultureNotFoundException("aNameOfAParam", "this is a test string");
            Assert.Equal("this is a test string" + System.Environment.NewLine +"Parameter name: aNameOfAParam", cnf.Message);
        }

        [Fact]
        public void TestInvalidCultureName1()
        {
            CultureNotFoundException cnf = new CultureNotFoundException("this is a test string", "abcd", new Exception("inner exception string"));
            Assert.Equal("this is a test string" + System.Environment.NewLine + "abcd is an invalid culture identifier.", cnf.Message);
            Assert.Equal("inner exception string", cnf.InnerException.Message);
            Assert.Equal("abcd", cnf.InvalidCultureName);
        }

        [Fact]
        public void TestInvalidCultureName2()
        {
            CultureNotFoundException cnf = new CultureNotFoundException("aNameOfAParam", "abcd", "this is a test string");
            Assert.Equal("this is a test string" + System.Environment.NewLine + "Parameter name: aNameOfAParam" + System.Environment.NewLine + "abcd is an invalid culture identifier.", cnf.Message);
            Assert.Equal("abcd", cnf.InvalidCultureName);
        }
    }
}