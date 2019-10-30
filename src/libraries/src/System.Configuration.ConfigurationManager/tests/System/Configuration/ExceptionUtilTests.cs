// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;
using System.Xml;
using Xunit;

namespace System.ConfigurationTests
{
    public class ExceptionUtilTests
    {
        [Fact]
        public void WrapAsConfigException_ConfigurationErrorsException()
        {
            ConfigurationErrorsException e = new ConfigurationErrorsException();
            Assert.Same(e, ExceptionUtil.WrapAsConfigException(null, e, null));
        }

        [Fact]
        public void WrapAsConfigException_ConfigurationException()
        {
            Exception inner = new Exception();
#pragma warning disable CS0618
            ConfigurationException obsolete = new ConfigurationException("mymessage", inner, "myfilename", 42);
#pragma warning restore CS0618
            var result = ExceptionUtil.WrapAsConfigException(null, obsolete, null);
            Assert.NotNull(result);
            Assert.Same(inner, result.InnerException);
            Assert.Equal("mymessage", result.BareMessage);
            Assert.Equal("myfilename", result.Filename);
            Assert.Equal(42, result.Line);
        }

        [Fact]
        public void WrapAsConfigException_XmlExceptionWithLine()
        {
            XmlException xml = new XmlException("xmlmessage", null, 42, 0);
            var result = ExceptionUtil.WrapAsConfigException(null, xml, null, 99);
            Assert.NotNull(result);
            Assert.Same(xml, result.InnerException);
            Assert.Equal(xml.Message, result.BareMessage);
            Assert.Equal(xml.LineNumber, result.Line);
            Assert.Null(result.Filename);
        }

        [Fact]
        public void WrapAsConfigException_XmlExceptionWithNoLine()
        {
            XmlException xml = new XmlException("xmlmessage", null, 0, 0);
            var result = ExceptionUtil.WrapAsConfigException(null, xml, "myfilename", 99);
            Assert.NotNull(result);
            Assert.Same(xml, result.InnerException);
            Assert.Equal(xml.Message, result.BareMessage);
            Assert.Equal(99, result.Line);
            Assert.Equal("myfilename", result.Filename);
        }

        [Fact]
        public void WrapAsConfigException_OtherException()
        {
            Exception e = new Exception();
            var result = ExceptionUtil.WrapAsConfigException("mymessage", e, "myfilename", 55);
            Assert.NotNull(result);
            Assert.Same(e, result.InnerException);

            // The "bare" message is whatever the base class returns
            Assert.StartsWith("mymessage:", result.BareMessage);
            Assert.Equal("myfilename", result.Filename);
            Assert.Equal(55, result.Line);
        }

        [Fact]
        public void WrapAsConfigException_Null()
        {
            var result = ExceptionUtil.WrapAsConfigException("mymessage", null, "myfilename", 55);
            Assert.NotNull(result);
            Assert.Null(result.InnerException);

            // The "bare" message is whatever the base class returns
            Assert.StartsWith("mymessage:", result.BareMessage);
            Assert.Equal("myfilename", result.Filename);
            Assert.Equal(55, result.Line);
        }

        [Fact]
        public void ParameterInvalid()
        {
            ArgumentException e = ExceptionUtil.ParameterInvalid("foo");
            Assert.NotNull(e);
            Assert.Equal("foo", e.ParamName);
            Assert.StartsWith(string.Format(SR.GetResourceString("Parameter_Invalid", null), "foo"), e.Message);
        }

        [Fact]
        public void ParameterNullOrEmpty()
        {
            ArgumentException e = ExceptionUtil.ParameterNullOrEmpty("foo");
            Assert.NotNull(e);
            Assert.Equal("foo", e.ParamName);
            Assert.StartsWith(string.Format(SR.GetResourceString("Parameter_NullOrEmpty", null), "foo"), e.Message);
        }

        [Fact]
        public void PropertyInvalid()
        {
            ArgumentException e = ExceptionUtil.PropertyInvalid("foo");
            Assert.NotNull(e);
            Assert.Equal("foo", e.ParamName);
            Assert.StartsWith(string.Format(SR.GetResourceString("Property_Invalid", null), "foo"), e.Message);
        }

        [Fact]
        public void PropertyNullOrEmpty()
        {
            ArgumentException e = ExceptionUtil.PropertyNullOrEmpty("foo");
            Assert.NotNull(e);
            Assert.Equal("foo", e.ParamName);
            Assert.StartsWith(string.Format(SR.GetResourceString("Property_NullOrEmpty", null), "foo"), e.Message);
        }

        [Fact]
        public void UnexpectedError()
        {
            InvalidOperationException e = ExceptionUtil.UnexpectedError("foo");
            Assert.NotNull(e);
            Assert.StartsWith(string.Format(SR.GetResourceString("Unexpected_Error", null), "foo"), e.Message);
        }
    }
}
