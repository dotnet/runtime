// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// SmtpExceptionTest.cs - Unit Test Cases for System.Net.Mail.SmtpException
//
// Authors:
//  Gert Driesen (drieseng@users.sourceforge.net)
//
// (C) 2008 Gert Driesen
//

using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Text.RegularExpressions;
using Xunit;

namespace System.Net.Mail.Tests
{
    public class SmtpExceptionTest
    {
        [Fact]
        public void TestDefaultConstructor()
        {
            SmtpException se = new SmtpException();
            Assert.NotNull(se.Data);
            Assert.Equal(0, se.Data.Keys.Count);
            Assert.Null(se.InnerException);
            Assert.NotNull(se.Message);
            Assert.Equal(-1, se.Message.IndexOf(typeof(SmtpException).FullName));
            Assert.Equal(SmtpStatusCode.GeneralFailure, se.StatusCode);
        }

        [Fact]
        public void TestConstructorWithStatusCodeArgument()
        {
            SmtpException se;

            se = new SmtpException(SmtpStatusCode.HelpMessage);
            Assert.NotNull(se.Data);
            Assert.Equal(0, se.Data.Keys.Count);
            Assert.Null(se.InnerException);
            Assert.NotNull(se.Message);
            Assert.Equal(-1, se.Message.IndexOf(typeof(SmtpException).FullName));
            Assert.Equal(SmtpStatusCode.HelpMessage, se.StatusCode);

            se = new SmtpException((SmtpStatusCode)666);
            Assert.NotNull(se.Data);
            Assert.Equal(0, se.Data.Keys.Count);
            Assert.Null(se.InnerException);
            Assert.NotNull(se.Message);
            Assert.Equal(-1, se.Message.IndexOf(typeof(SmtpException).FullName));
            Assert.Equal((SmtpStatusCode)666, se.StatusCode);
        }

        [Fact]
        public void TestConstructorWithStringArgument()
        {
            string msg;
            SmtpException se;

            msg = "MESSAGE";
            se = new SmtpException(msg);
            Assert.NotNull(se.Data);
            Assert.Equal(0, se.Data.Keys.Count);
            Assert.Null(se.InnerException);
            Assert.Same(msg, se.Message);
            Assert.Equal(SmtpStatusCode.GeneralFailure, se.StatusCode);

            msg = string.Empty;
            se = new SmtpException(msg);
            Assert.NotNull(se.Data);
            Assert.Equal(0, se.Data.Keys.Count);
            Assert.Null(se.InnerException);
            Assert.Same(msg, se.Message);
            Assert.Equal(SmtpStatusCode.GeneralFailure, se.StatusCode);

            msg = null;
            se = new SmtpException(msg);
            Assert.NotNull(se.Data);
            Assert.Equal(0, se.Data.Keys.Count);
            Assert.Null(se.InnerException);
            Assert.NotNull(se.Message);

            // \p{Pi} any kind of opening quote https://www.compart.com/en/unicode/category/Pi
            // \p{Pf} any kind of closing quote https://www.compart.com/en/unicode/category/Pf
            // \p{Po} any kind of punctuation character that is not a dash, bracket, quote or connector https://www.compart.com/en/unicode/category/Po
            Assert.Matches(@"[\p{Pi}\p{Po}]" + Regex.Escape(typeof(SmtpException).FullName) + @"[\p{Pf}\p{Po}]", se.Message);

            Assert.Equal(SmtpStatusCode.GeneralFailure, se.StatusCode);
        }

        [Fact]
        public void TestConstructorWithStatusCodeAndStringArgument()
        {
            string msg;
            SmtpException se;

            msg = "MESSAGE";
            se = new SmtpException(SmtpStatusCode.HelpMessage, msg);
            Assert.NotNull(se.Data);
            Assert.Equal(0, se.Data.Keys.Count);
            Assert.Null(se.InnerException);
            Assert.Same(msg, se.Message);
            Assert.Equal(SmtpStatusCode.HelpMessage, se.StatusCode);

            msg = string.Empty;
            se = new SmtpException(SmtpStatusCode.ServiceReady, msg);
            Assert.NotNull(se.Data);
            Assert.Equal(0, se.Data.Keys.Count);
            Assert.Null(se.InnerException);
            Assert.Same(msg, se.Message);
            Assert.Equal(SmtpStatusCode.ServiceReady, se.StatusCode);

            msg = null;
            se = new SmtpException((SmtpStatusCode)666, msg);
            Assert.NotNull(se.Data);
            Assert.Equal(0, se.Data.Keys.Count);
            Assert.Null(se.InnerException);
            Assert.NotNull(se.Message);

            // \p{Pi} any kind of opening quote https://www.compart.com/en/unicode/category/Pi
            // \p{Pf} any kind of closing quote https://www.compart.com/en/unicode/category/Pf
            // \p{Po} any kind of punctuation character that is not a dash, bracket, quote or connector https://www.compart.com/en/unicode/category/Po
            Assert.Matches(@"[\p{Pi}\p{Po}]" + Regex.Escape(typeof(SmtpException).FullName) + @"[\p{Pf}\p{Po}]", se.Message);

            Assert.Equal((SmtpStatusCode)666, se.StatusCode);
        }

        [Fact]
        public void TestConstructorWithStringAndExceptionArgument()
        {
            string msg = "MESSAGE";
            Exception inner = new Exception();
            SmtpException se;

            se = new SmtpException(msg, inner);
            Assert.NotNull(se.Data);
            Assert.Equal(0, se.Data.Keys.Count);
            Assert.Same(inner, se.InnerException);
            Assert.Same(msg, se.Message);
            Assert.Equal(SmtpStatusCode.GeneralFailure, se.StatusCode);

            se = new SmtpException(msg, null);
            Assert.NotNull(se.Data);
            Assert.Equal(0, se.Data.Keys.Count);
            Assert.Null(se.InnerException);
            Assert.Same(msg, se.Message);
            Assert.Equal(SmtpStatusCode.GeneralFailure, se.StatusCode);

            se = new SmtpException((string)null, inner);
            Assert.NotNull(se.Data);
            Assert.Equal(0, se.Data.Keys.Count);
            Assert.Same(inner, se.InnerException);
            Assert.NotNull(se.Message);
            Assert.Equal(new SmtpException((string)null).Message, se.Message);
            Assert.Equal(SmtpStatusCode.GeneralFailure, se.StatusCode);

            se = new SmtpException((string)null, (Exception)null);
            Assert.NotNull(se.Data);
            Assert.Equal(0, se.Data.Keys.Count);
            Assert.Null(se.InnerException);
            Assert.NotNull(se.Message);
            Assert.Equal(new SmtpException((string)null).Message, se.Message);
            Assert.Equal(SmtpStatusCode.GeneralFailure, se.StatusCode);
        }
    }
}
