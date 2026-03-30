// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Sdk;

namespace System.Formats.Asn1.Tests.Reader
{
    internal delegate void AsnReaderWrapperCallback(ref AsnReaderWrapper reader);

    internal static class AssertHelpers
    {
        extension(Assert)
        {
            internal static E Throws<E>(ref AsnReaderWrapper reader, AsnReaderWrapperCallback action) where E : Exception
            {
                Exception exception;

                try
                {
                    action(ref reader);
                    exception = null;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                switch(exception)
                {
                    case null:
                        throw ThrowsException.ForNoException(typeof(E));
                    case E ex when (ex.GetType() == typeof(E)):
                        return ex;
                    default:
                        throw ThrowsException.ForIncorrectExceptionType(typeof(E), exception);
                }
            }

            internal static E Throws<E>(ref AsnReaderWrapper reader, string expectedParamName, AsnReaderWrapperCallback action) where E : ArgumentException
            {
                Exception exception;

                try
                {
                    action(ref reader);
                    exception = null;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                switch(exception)
                {
                    case null:
                        throw ThrowsException.ForNoException(typeof(E));
                    case E ex when (ex.GetType() == typeof(E)):
                        Assert.Equal(expectedParamName, ex.ParamName);
                        return ex;
                    default:
                        throw ThrowsException.ForIncorrectExceptionType(typeof(E), exception);
                }
            }
        }
    }
}
