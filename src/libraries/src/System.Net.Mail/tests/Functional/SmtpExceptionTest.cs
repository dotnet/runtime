// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// SmtpExceptionTest.cs - NUnit Test Cases for System.Net.Mail.SmtpException
//
// Authors:
//	Gert Driesen (drieseng@users.sourceforge.net)
//
// (C) 2008 Gert Driesen
//


using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
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
            Assert.NotEqual(-1, se.Message.IndexOf("'" + typeof(SmtpException).FullName + "'"));
            Assert.Equal(SmtpStatusCode.GeneralFailure, se.StatusCode);
        }

        [Fact]
        public void TestConstructorWithSerialization()
        {
            string msg = "MESSAGE";
            Exception inner = new ArgumentException("whatever");
            SerializationInfo si = new SerializationInfo(typeof(SmtpException), new FormatterConverter());

            new Exception(msg, inner).GetObjectData(si, new StreamingContext());
            si.AddValue("Status", (int)SmtpStatusCode.ServiceReady);

            SmtpException se = new MySmtpException(si, new StreamingContext());
            Assert.NotNull(se.Data);
            Assert.Equal(0, se.Data.Keys.Count);
            Assert.Same(inner, se.InnerException);
            Assert.Same(msg, se.Message);
            Assert.Equal(SmtpStatusCode.ServiceReady, se.StatusCode);
        }

        [Fact]
        public void TestConstructorThrowsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new MySmtpException((SerializationInfo)null, new StreamingContext()));
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
            Assert.NotEqual(-1, se.Message.IndexOf("'" + typeof(SmtpException).FullName + "'"));
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

    class MySmtpException : SmtpException
    {
        public MySmtpException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}

