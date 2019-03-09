// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration.Internal;
using System.Xml;

namespace System.Configuration
{
    internal static class ExceptionUtil
    {
        internal static string NoExceptionInformation => SR.No_exception_information_available;

        internal static ArgumentException ParameterInvalid(string parameter)
        {
            return new ArgumentException(SR.Format(SR.Parameter_Invalid, parameter), parameter);
        }

        internal static ArgumentException ParameterNullOrEmpty(string parameter)
        {
            return new ArgumentException(SR.Format(SR.Parameter_NullOrEmpty, parameter), parameter);
        }

        internal static ArgumentException PropertyInvalid(string property)
        {
            return new ArgumentException(SR.Format(SR.Property_Invalid, property), property);
        }

        internal static ArgumentException PropertyNullOrEmpty(string property)
        {
            return new ArgumentException(SR.Format(SR.Property_NullOrEmpty, property), property);
        }

        internal static InvalidOperationException UnexpectedError(string methodName)
        {
            return new InvalidOperationException(SR.Format(SR.Unexpected_Error, methodName));
        }

        internal static ConfigurationErrorsException WrapAsConfigException(string outerMessage, Exception e,
            IConfigErrorInfo errorInfo)
        {
            return errorInfo != null
                ? WrapAsConfigException(outerMessage, e, errorInfo.Filename, errorInfo.LineNumber)
                : WrapAsConfigException(outerMessage, e, null, 0);
        }

        internal static ConfigurationErrorsException WrapAsConfigException(string outerMessage, Exception e,
            string filename, int line)
        {
            // Preserve ConfigurationErrorsException
            ConfigurationErrorsException ce = e as ConfigurationErrorsException;
            if (ce != null) return ce;

            // Promote deprecated ConfigurationException to ConfigurationErrorsException
            ConfigurationException deprecatedException = e as ConfigurationException;
            if (deprecatedException != null)
                return new ConfigurationErrorsException(
                    deprecatedException.BareMessage,
                    deprecatedException.InnerException,
                    deprecatedException.Filename,
                    deprecatedException.Line);

            // For XML exceptions, preserve the text of the exception in the outer message.
            XmlException xe = e as XmlException;
            if (xe != null)
            {
                if (xe.LineNumber != 0) line = xe.LineNumber;

                return new ConfigurationErrorsException(xe.Message, xe, filename, line);
            }

            // Wrap other exceptions in an inner exception, and give as much info as possible
            if (e != null)
            {
                return new ConfigurationErrorsException(
                    SR.Format(SR.Wrapped_exception_message, outerMessage, e.Message),
                    e,
                    filename,
                    line);
            }

            // If there is no exception, create a new exception with no further information.
            return new ConfigurationErrorsException(
                SR.Format(SR.Wrapped_exception_message, outerMessage, NoExceptionInformation),
                filename,
                line);
        }
    }
}
