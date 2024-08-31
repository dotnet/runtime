// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    [ConditionalClass(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
    public class PageResultResponseControlTests
    {
        private const string ControlOid = "1.2.840.113556.1.4.319";

        private static MethodInfo s_transformControlsMethod = typeof(DirectoryControl)
            .GetMethod("TransformControls", BindingFlags.NonPublic | BindingFlags.Static);

        public static IEnumerable<object[]> ConformantControlValues()
        {
            // {iO}, single-byte length
            // Varying combinations of a zero & non-zero value for the first INTEGER, and a populated & zero-length OCTET STRING
            yield return new object[] { new byte[] { 0x30, 0x0A,
                0x02, 0x01, 0x00,
                0x04, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4
            }, 0x00, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };
            yield return new object[] { new byte[] { 0x30, 0x0A,
                0x02, 0x01, 0x40,
                0x04, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4
            }, 0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };
            yield return new object[] { new byte[] { 0x30, 0x05,
                0x02, 0x01, 0x00,
                0x04, 0x00
            }, 0x00, Array.Empty<byte>() };
            yield return new object[] { new byte[] { 0x30, 0x05,
                0x02, 0x01, 0x40,
                0x04, 0x00
            }, 0x40, Array.Empty<byte>() };

            // {iO}, four-byte length
            // Varying combinations of a zero & non-zero value for the first INTEGER, and a populated & zero-length OCTET STRING
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x0E,
                0x02, 0x01, 0x00,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4
            }, 0x00, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x0E,
                0x02, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4
            }, 0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x09,
                0x02, 0x01, 0x00,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x00
            }, 0x00, Array.Empty<byte>() };
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x09,
                0x02, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x00
            }, 0x40, Array.Empty<byte>() };
        }

        public static IEnumerable<object[]> NonconformantControlValues()
        {
            // {eO}, single-byte length. ASN.1 type of ENUMERATED rather than INTEGER
            yield return new object[] { new byte[] { 0x30, 0x0A,
                0x0A, 0x01, 0x40,
                0x04, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4
            }, 0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };

            // {eO}, four-byte length. ASN.1 type of ENUMERATED rather than INTEGER
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x0E,
                0x0A, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4
            }, 0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };

            // {iO}, single-byte length. Trailing data after the end of the sequence
            yield return new object[] { new byte[] { 0x30, 0x0A,
                0x02, 0x01, 0x40,
                0x04, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4,
                0x80, 0x80, 0x80, 0x80
            }, 0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };

            // {iO}, four-byte length. Trailing data after the end of the sequence
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x0E,
                0x02, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4,
                0x80, 0x80, 0x80, 0x80
            }, 0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };

            // {iO}, single-byte length. Trailing data within the sequence (after the octet string)
            yield return new object[] { new byte[] { 0x30, 0x0E,
                0x02, 0x01, 0x40,
                0x04, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4,
                0x80, 0x80, 0x80, 0x80
            }, 0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };

            // {iO}, four-byte length. Trailing data within the sequence (after the octet string)
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x12,
                0x0A, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4,
                0x80, 0x80, 0x80, 0x80
            }, 0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };

            // Windows will treat these values as invalid. OpenLDAP has slightly looser parsing rules around octet string lengths.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // {iO}, single-byte length. Octet string length extending beyond the end of the sequence (but within the buffer)
                yield return new object[] { new byte[] { 0x30, 0x0A,
                    0x02, 0x01, 0x40,
                    0x04, 0x06, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0x80,
                    0x80, 0x80, 0x80
                }, 0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0x80 } };

                // {iO}, four-byte length. Octet string length extending beyond the end of the sequence (but within the buffer)
                yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x0E,
                    0x02, 0x01, 0x40,
                    0x04, 0x84, 0x00, 0x00, 0x00, 0x06, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0x80,
                    0x80, 0x80, 0x80
                }, 0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0x80 } };
            }
        }

        public static IEnumerable<object[]> InvalidControlValues()
        {
            // i, not wrapped in an ASN.1 SEQUENCE
            yield return new object[] { new byte[] { 0x02, 0x01, 0x40 } };

            // iO, single-byte length, not wrapped in an ASN.1 SEQUENCE
            yield return new object[] { new byte[] { 0x02, 0x01, 0x40,
                0x04, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4} };

            // iO, four-byte length, not wrapped in an ASN.1 SEQUENCE
            yield return new object[] { new byte[] { 0x02, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4} };

            // {iO}, single-byte length, sequence length extending beyond the end of the buffer
            yield return new object[] { new byte[] { 0x30, 0x06,
                0x02, 0x01, 0x40,
                0x04, 0x00} };

            // {iO}, four-byte length, sequence length extending beyond the end of the buffer
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x0A,
                0x02, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x00} };

            // Only Windows treats these values as invalid. These values are present in NonconformantControlValues to prove
            // the OpenLDAP behavior.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // {iO}, single-byte length. Octet string length extending beyond the end of the sequence (but within the buffer)
                yield return new object[] { new byte[] { 0x30, 0x0A,
                    0x02, 0x01, 0x40,
                    0x04, 0x06, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0x80,
                    0x80, 0x80, 0x80 } };

                // {iO}, four-byte length. Octet string length extending beyond the end of the sequence (but within the buffer)
                yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x0E,
                    0x02, 0x01, 0x40,
                    0x04, 0x84, 0x00, 0x00, 0x00, 0x06, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0x80,
                    0x80, 0x80, 0x80 } };
            }

            // {iO}, single-byte length. Octet string length extending beyond the end of the buffer
            yield return new object[] { new byte[] { 0x30, 0x0A,
                0x02, 0x01, 0x40,
                0x04, 0x06, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };

            // {iO}, four-byte length. Octet string length extending beyond the end of the buffer
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x0E,
                0x02, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x06, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };
        }

        [Theory]
        [MemberData(nameof(ConformantControlValues))]
        public void ConformantResponseControlParsedSuccessfully(byte[] value, int totalCount, byte[] cookie)
            => VerifyResponseControl(value, totalCount, cookie);

        [Theory]
        [MemberData(nameof(NonconformantControlValues))]
        public void NonconformantResponseControlParsedSuccessfully(byte[] value, int totalCount, byte[] cookie)
            => VerifyResponseControl(value, totalCount, cookie);

        [Theory]
        [MemberData(nameof(InvalidControlValues))]
        public void InvalidResponseControlThrowsException(byte[] value)
        {
            DirectoryControl control = new(ControlOid, value, true, true);

            Assert.Throws<BerConversionException>(() => TransformResponseControl(control, false));
        }

        private static void VerifyResponseControl(byte[] value, int totalCount, byte[] cookie)
        {
            DirectoryControl control = new(ControlOid, value, true, true);
            PageResultResponseControl castControl = TransformResponseControl(control, true) as PageResultResponseControl;

            Assert.Equal(totalCount, castControl.TotalCount);
            Assert.Equal(cookie, castControl.Cookie);
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

                return Assert.IsType<PageResultResponseControl>(resultantControl);
            }
            else
            {
                return resultantControl;
            }
        }
    }
}
