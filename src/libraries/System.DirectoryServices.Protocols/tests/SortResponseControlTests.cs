// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    [ConditionalClass(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
    public class SortResponseControlTests
    {
        private const string ControlOid = "1.2.840.113556.1.4.474";

        private static MethodInfo s_transformControlsMethod = typeof(DirectoryControl)
            .GetMethod("TransformControls", BindingFlags.NonPublic | BindingFlags.Static);

        public static IEnumerable<object[]> ConformantControlValues()
        {
            // {e}, single-byte length
            yield return new object[] { new byte[] { 0x30, 0x03,
                0x0A, 0x01, 0x40
            }, (ResultCode)0x40, null };
            yield return new object[] { new byte[] { 0x30, 0x03,
                0x0A, 0x01, 0x7F
            }, (ResultCode)0x7F, null };

            // {e}, four-byte length
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x03,
                0x0A, 0x01, 0x40
            }, (ResultCode)0x40, null };
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x03,
                0x0A, 0x01, 0x7F
            }, (ResultCode)0x7F, null };

            // {ea}, single-byte length
            yield return new object[] { new byte[] { 0x30, 0x0A,
                0x0A, 0x01, 0x40,
                0x80, 0x05, 0x6E, 0x61, 0x6D, 0x65, 0x31
            }, (ResultCode)0x40, "name1" };
            yield return new object[] { new byte[] { 0x30, 0x0A,
                0x0A, 0x01, 0x7F,
                0x80, 0x05, 0x6E, 0x61, 0x6D, 0x65, 0x31
            }, (ResultCode)0x7F, "name1" };
            yield return new object[] { new byte[] { 0x30, 0x47,
                0x0A, 0x01, 0x40,
                0x80, 0x42, 0x6E, 0x61, 0x6D, 0x65, 0x31, 0x5F, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39
            }, (ResultCode)0x40, "name1_012345678901234567890123456789012345678901234567890123456789" };
            yield return new object[] { new byte[] { 0x30, 0x47,
                0x0A, 0x01, 0x7F,
                0x80, 0x42, 0x6E, 0x61, 0x6D, 0x65, 0x31, 0x5F, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39
            }, (ResultCode)0x7F, "name1_012345678901234567890123456789012345678901234567890123456789" };
            yield return new object[] { new byte[] { 0x30, 0x05,
                0x0A, 0x01, 0x40,
                0x80, 0x00
            }, (ResultCode)0x40, string.Empty };
            yield return new object[] { new byte[] { 0x30, 0x05,
                0x0A, 0x01, 0x7F,
                0x80, 0x00
            }, (ResultCode)0x7F, string.Empty };

            // {ea}, four-byte length
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x0E,
                0x0A, 0x01, 0x40,
                0x80, 0x84, 0x00, 0x00, 0x00, 0x05, 0x6E, 0x61, 0x6D, 0x65, 0x31
            }, (ResultCode)0x40, "name1" };
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x0E,
                0x0A, 0x01, 0x7F,
                0x80, 0x84, 0x00, 0x00, 0x00, 0x05, 0x6E, 0x61, 0x6D, 0x65, 0x31
            }, (ResultCode)0x7F, "name1" };
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x4B,
                0x0A, 0x01, 0x40,
                0x80, 0x84, 0x00, 0x00, 0x00, 0x42, 0x6E, 0x61, 0x6D, 0x65, 0x31, 0x5F, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39
            }, (ResultCode)0x40, "name1_012345678901234567890123456789012345678901234567890123456789" };
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x4B,
                0x0A, 0x01, 0x7F,
                0x80, 0x84, 0x00, 0x00, 0x00, 0x42, 0x6E, 0x61, 0x6D, 0x65, 0x31, 0x5F, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39
            }, (ResultCode)0x7F, "name1_012345678901234567890123456789012345678901234567890123456789" };
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x09,
                0x0A, 0x01, 0x40,
                0x80, 0x84, 0x00, 0x00, 0x00, 0x00
            }, (ResultCode)0x40, string.Empty };
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x09,
                0x0A, 0x01, 0x7F,
                0x80, 0x84, 0x00, 0x00, 0x00, 0x00
            }, (ResultCode)0x7F, string.Empty };
        }

        public static IEnumerable<object[]> NonconformantControlValues()
        {
#if NETFRAMEWORK
            // {i}, single-byte length. ASN.1 type of INTEGER rather than ENUMERATED
            yield return new object[] { new byte[] { 0x30, 0x03,
                0x02, 0x01, 0x40
            }, (ResultCode)0x40, null };

            // {i}, four-byte length. ASN.1 type of INTEGER rather than ENUMERATED
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x03,
                0x02, 0x01, 0x40
            }, (ResultCode)0x40, null };

            // {e}, single-byte length. Trailing data after the end of the sequence
            yield return new object[] { new byte[] { 0x30, 0x03,
                0x0A, 0x01, 0x40,
                0x80, 0x80, 0x80, 0x80
            }, (ResultCode)0x40, null };

            // {e}, four-byte length. Trailing data after the end of the sequence
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x03,
                0x0A, 0x01, 0x40,
                0x80, 0x80, 0x80, 0x80
            }, (ResultCode)0x40, null };

            // {e}, single-byte length. Trailing data within the sequence is interpreted as an empty string by Windows
            yield return new object[] { new byte[] { 0x30, 0x07,
                0x0A, 0x01, 0x40,
                0x80, 0x80, 0x80, 0x80
            }, (ResultCode)0x40, string.Empty };

            // {e}, four-byte length. Trailing data within the sequence is interpreted as an empty string by Windows
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x07,
                0x0A, 0x01, 0x40,
                0x80, 0x80, 0x80, 0x80
            }, (ResultCode)0x40, string.Empty };

            // {ea}, single-byte length. Octet string length extending beyond the end of the sequence (but within the buffer.)
            // The result of the "a" format specifier is null on Windows
            yield return new object[] { new byte[] { 0x30, 0x0A,
                0x0A, 0x01, 0x40,
                0x04, 0x06, 0x6E, 0x61, 0x6D, 0x65, 0x31, 0x31,
                0x80, 0x80, 0x80
            }, (ResultCode)0x40, null };

            // {ea}, four-byte length. Octet string length extending beyond the end of the sequence (but within the buffer.)
            // The result of the "a" format specifier is null on Windows
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x0A,
                0x0A, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x06, 0x6E, 0x61, 0x6D, 0x65, 0x31, 0x31,
                0x80, 0x80, 0x80
            }, (ResultCode)0x40, null };

            // {ea}, single-byte length. Octet string length extending beyond the end of the buffer. Result of the "a" format specifier is null
            yield return new object[] { new byte[] { 0x30, 0x0A,
                0x0A, 0x01, 0x40,
                0x04, 0x06, 0x6E, 0x61, 0x6D, 0x65, 0x31
            }, (ResultCode)0x40, null };

            // {ea}, four-byte length. Octet string length extending beyond the end of the buffer. Result of the "a" format specifier is null
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x0A,
                0x0A, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x06, 0x6E, 0x61, 0x6D, 0x65, 0x31
            }, (ResultCode)0x40, null };

            // {ea}, single-byte length. Trailing data within the sequence (after the octet string)
            yield return new object[] { new byte[] { 0x30, 0x0E,
                0x0A, 0x01, 0x40,
                0x04, 0x05, 0x6E, 0x61, 0x6D, 0x65, 0x31,
                0x80, 0x80, 0x80, 0x80
            }, (ResultCode)0x40, "name1" };

            // {ea}, four-byte length. Trailing data within the sequence (after the octet string)
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x12,
                0x0A, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x05, 0x6E, 0x61, 0x6D, 0x65, 0x31,
                0x80, 0x80, 0x80, 0x80
            }, (ResultCode)0x40, "name1" };
#else
            yield break;
#endif
        }

        public static IEnumerable<object[]> InvalidControlValues()
        {
            // e, not wrapped in an ASN.1 SEQUENCE
            yield return new object[] { new byte[] { 0x02, 0x01, 0x40 } };

            // {e}, single-byte length, sequence length extending beyond the end of the buffer
            yield return new object[] { new byte[] { 0x30, 0x04,
                0x0A, 0x01, 0x40 } };

            // {e}, four-byte length, sequence length extending beyond the end of the buffer
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x04,
                0x0A, 0x01, 0x40 } };

#if NET
            // {i}, single-byte length. ASN.1 type of INTEGER rather than ENUMERATED
            yield return new object[] { new byte[] { 0x30, 0x03,
                0x02, 0x01, 0x40 } };

            // {i}, four-byte length. ASN.1 type of INTEGER rather than ENUMERATED
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x03,
                0x02, 0x01, 0x40 } };

            // {e}, single-byte length. Trailing data after the end of the sequence
            yield return new object[] { new byte[] { 0x30, 0x03,
                0x0A, 0x01, 0x40,
                0x80, 0x80, 0x80, 0x80 } };

            // {e}, four-byte length. Trailing data after the end of the sequence
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x03,
                0x0A, 0x01, 0x40,
                0x80, 0x80, 0x80, 0x80 } };

            // {e}, single-byte length. Trailing data within the sequence
            yield return new object[] { new byte[] { 0x30, 0x07,
                0x0A, 0x01, 0x40,
                0x80, 0x80, 0x80, 0x80 } };

            // {e}, four-byte length. Trailing data within the sequence
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x07,
                0x0A, 0x01, 0x40,
                0x80, 0x80, 0x80, 0x80 } };

            // {ea}, single-byte length. Octet string length extending beyond the end of the sequence (but within the buffer)
            yield return new object[] { new byte[] { 0x30, 0x0A,
                0x0A, 0x01, 0x40,
                0x80, 0x06, 0x6E, 0x61, 0x6D, 0x65, 0x31, 0x31,
                0x80, 0x80, 0x80 } };

            // {ea}, four-byte length. Octet string length extending beyond the end of the sequence (but within the buffer)
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x0A,
                0x0A, 0x01, 0x40,
                0x80, 0x84, 0x00, 0x00, 0x00, 0x06, 0x6E, 0x61, 0x6D, 0x65, 0x31, 0x31,
                0x80, 0x80, 0x80 } };

            // {ea}, single-byte length. Octet string length extending beyond the end of the buffer
            yield return new object[] { new byte[] { 0x30, 0x0A,
                0x0A, 0x01, 0x40,
                0x80, 0x06, 0x6E, 0x61, 0x6D, 0x65, 0x31 } };

            // {ea}, four-byte length. Octet string length extending beyond the end of the buffer
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x0A,
                0x0A, 0x01, 0x40,
                0x80, 0x84, 0x00, 0x00, 0x00, 0x06, 0x6E, 0x61, 0x6D, 0x65, 0x31 } };

            // {ea}, single-byte length. Trailing data within the sequence (after the octet string)
            yield return new object[] { new byte[] { 0x30, 0x0E,
                0x0A, 0x01, 0x40,
                0x80, 0x05, 0x6E, 0x61, 0x6D, 0x65, 0x31,
                0x80, 0x80, 0x80, 0x80 } };

            // {ea}, four-byte length. Trailing data within the sequence (after the octet string)
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x12,
                0x0A, 0x01, 0x40,
                0x80, 0x84, 0x00, 0x00, 0x00, 0x05, 0x6E, 0x61, 0x6D, 0x65, 0x31,
                0x80, 0x80, 0x80, 0x80 } };
#endif
        }

        public static IEnumerable<object[]> InvalidUnicodeText()
        {
            // {ea}, single-byte length. Octet string contains a trailing 0x80 (which is invalid Unicode)
            yield return new object[] { new byte[] { 0x30, 0x0B,
                0x0A, 0x01, 0x40,
                0x80, 0x06, 0x6E, 0x61, 0x6D, 0x65, 0x31, 0x80
            } };

            // {ea}, four-byte length. Octet string contains a trailing 0x80 (which is invalid Unicode)
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x0F,
                0x0A, 0x01, 0x40,
                0x80, 0x84, 0x00, 0x00, 0x00, 0x06, 0x6E, 0x61, 0x6D, 0x65, 0x31, 0x80
            } };
        }

        [Theory]
        [MemberData(nameof(ConformantControlValues))]
        public void ConformantResponseControlParsedSuccessfully(byte[] value, ResultCode expectedResultCode, string expectedAttribute)
            => VerifyResponseControl(value, expectedResultCode, expectedAttribute);

        [Theory]
        [MemberData(nameof(NonconformantControlValues))]
        public void NonconformantResponseControlParsedSuccessfully(byte[] value, ResultCode expectedResultCode, string expectedAttribute)
            => VerifyResponseControl(value, expectedResultCode, expectedAttribute);

        [Theory]
        [MemberData(nameof(InvalidControlValues))]
        public void InvalidResponseControlThrowsException(byte[] value)
        {
            DirectoryControl control = new(ControlOid, value, true, true);

            Assert.Throws<BerConversionException>(() => TransformResponseControl(control, false));
        }

#if NETFRAMEWORK
        [Theory]
        [MemberData(nameof(InvalidUnicodeText))]
        public void InvalidUnicodeTextThrowsDecoderFallbackException(byte[] value)
        {
            DirectoryControl control = new(ControlOid, value, true, true);

            Assert.Throws<DecoderFallbackException>(() => TransformResponseControl(control, false));
        }
#else
        [Theory]
        [MemberData(nameof(InvalidUnicodeText))]
        public void InvalidUnicodeTextThrowsBerConversionException(byte[] value)
        {
            DirectoryControl control = new(ControlOid, value, true, true);

            BerConversionException conversionException = Assert.Throws<BerConversionException>(() => TransformResponseControl(control, false));

            Assert.IsType<DecoderFallbackException>(conversionException.InnerException);
        }
#endif

        private static void VerifyResponseControl(byte[] value, ResultCode expectedResultCode, string expectedAttribute)
        {
            DirectoryControl control = new(ControlOid, value, true, true);
            SortResponseControl castControl = TransformResponseControl(control, true) as SortResponseControl;

            Assert.Equal(expectedResultCode, castControl.Result);
            Assert.Equal(expectedAttribute, castControl.AttributeName);
        }

        private static DirectoryControl TransformResponseControl(DirectoryControl control, bool assertControlProperties)
        {
            DirectoryControl[] controls = [control];
            DirectoryControl resultantControl;

            try
            {
                s_transformControlsMethod.Invoke(null, new object[] { controls });
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }

            resultantControl = controls[0];

            if (assertControlProperties)
            {
                Assert.Equal(control.Type, resultantControl.Type);
                Assert.Equal(control.IsCritical, resultantControl.IsCritical);
                Assert.Equal(control.ServerSide, resultantControl.ServerSide);
                Assert.Equal(control.GetValue(), resultantControl.GetValue());

                return Assert.IsType<SortResponseControl>(resultantControl);
            }
            else
            {
                return resultantControl;
            }
        }
    }
}
