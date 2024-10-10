// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    [ConditionalClass(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
    public class DirSyncResponseControlTests
    {
        private const string ControlOid = "1.2.840.113556.1.4.841";

        private static MethodInfo s_transformControlsMethod = typeof(DirectoryControl)
            .GetMethod("TransformControls", BindingFlags.NonPublic | BindingFlags.Static);

        public static IEnumerable<object[]> ConformantControlValues()
        {
            // {iiO}, single-byte length
            // Varying combinations of a zero & non-zero value for the first INTEGER, and a populated & zero-length OCTET STRING
            yield return new object[] { new byte[] { 0x30, 0x0D,
                0x02, 0x01, 0x00,
                0x02, 0x01, 0x40,
                0x04, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4
            }, false, 0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };
            yield return new object[] { new byte[] { 0x30, 0x0D,
                0x02, 0x01, 0xFF,
                0x02, 0x01, 0x40,
                0x04, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4
            }, true, 0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };
            yield return new object[] { new byte[] { 0x30, 0x08,
                0x02, 0x01, 0x00,
                0x02, 0x01, 0x40,
                0x04, 0x00
            }, false, 0x40, Array.Empty<byte>() };
            yield return new object[] { new byte[] { 0x30, 0x08,
                0x02, 0x01, 0xFF,
                0x02, 0x01, 0x40,
                0x04, 0x00
            }, true, 0x40, Array.Empty<byte>() };

            // {iiO}, four-byte length
            // Varying combinations of a zero & non-zero value for the first INTEGER, and a populated & zero-length OCTET STRING
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x11,
                0x02, 0x01, 0x00,
                0x02, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4
            }, false, 0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x11,
                0x02, 0x01, 0xFF,
                0x02, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4
            }, true, 0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x0C,
                0x02, 0x01, 0x00,
                0x02, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x00
            }, false, 0x40, Array.Empty<byte>() };
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x0C,
                0x02, 0x01, 0xFF,
                0x02, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x00
            }, true, 0x40, Array.Empty<byte>() };
        }

        public static IEnumerable<object[]> NonconformantControlValues()
        {
            // {eeO}, single-byte length. ASN.1 type of ENUMERATED rather than INTEGER
            yield return new object[] { new byte[] { 0x30, 0x0D,
                0x0A, 0x01, 0xFF,
                0x0A, 0x01, 0x40,
                0x04, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4
            }, true, 0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };

            // {eeO}, four-byte length. ASN.1 type of ENUMERATED rather than INTEGER
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x11,
                0x0A, 0x01, 0xFF,
                0x0A, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4
            }, true, 0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };

            // {iiO}, single-byte length. Trailing data after the end of the sequence
            yield return new object[] { new byte[] { 0x30, 0x0D,
                0x02, 0x01, 0xFF,
                0x02, 0x01, 0x40,
                0x04, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4,
                0x80, 0x80, 0x80, 0x80
            }, true, 0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };

            // {iiO}, four-byte length. Trailing data after the end of the sequence
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x11,
                0x02, 0x01, 0xFF,
                0x02, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4,
                0x80, 0x80, 0x80, 0x80
            }, true, 0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };

            // {iiO}, single-byte length. Trailing data within the sequence (after the octet string)
            yield return new object[] { new byte[] { 0x30, 0x11,
                0x02, 0x01, 0xFF,
                0x02, 0x01, 0x40,
                0x04, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4,
                0x80, 0x80, 0x80, 0x80
            }, true, 0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };

            // {iiO}, four-byte length. Trailing data within the sequence (after the octet string)
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x15,
                0x02, 0x01, 0xFF,
                0x02, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4,
                0x80, 0x80, 0x80, 0x80
            }, true, 0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };

            // Windows will treat these values as invalid. OpenLDAP has slightly looser parsing rules around octet string lengths.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // {iiO}, single-byte length. Octet string length extending beyond the end of the sequence (but within the buffer)
                yield return new object[] { new byte[] { 0x30, 0x0D,
                    0x02, 0x01, 0x00,
                    0x02, 0x01, 0x40,
                    0x04, 0x06, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0x80,
                    0x80, 0x80, 0x80
                }, false, 0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0x80 } };

                // {iiO}, four-byte length. Octet string length extending beyond the end of the sequence (but within the buffer)
                yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x11,
                    0x02, 0x01, 0x00,
                    0x02, 0x01, 0x40,
                    0x04, 0x84, 0x00, 0x00, 0x00, 0x06, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0x80,
                    0x80, 0x80, 0x80
                }, false, 0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0x80 } };
            }
        }

        public static IEnumerable<object[]> InvalidControlValues()
        {
            // i, not wrapped in an ASN.1 SEQUENCE
            yield return new object[] { new byte[] { 0x02, 0x01, 0x00 } };

            // ii, not wrapped in an ASN.1 SEQUENCE
            yield return new object[] { new byte[] { 0x02, 0x01, 0x00,
                0x02, 0x01, 0x40 } };

            // iiO, single-byte length, not wrapped in an ASN.1 SEQUENCE
            yield return new object[] { new byte[] { 0x02, 0x01, 0x00,
                0x02, 0x01, 0x40,
                0x04, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4} };

            // iiO, four-byte length, not wrapped in an ASN.1 SEQUENCE
            yield return new object[] { new byte[] { 0x02, 0x01, 0x00,
                0x02, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4} };

            // {iiO}, single-byte length, sequence length extending beyond the end of the buffer
            yield return new object[] { new byte[] { 0x30, 0x09,
                0x02, 0x01, 0x00,
                0x02, 0x01, 0x40,
                0x04, 0x00} };

            // {iiO}, four-byte length, sequence length extending beyond the end of the buffer
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x0D,
                0x02, 0x01, 0x00,
                0x02, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x00} };

            // Only Windows treats these values as invalid. These values are present in NonconformantControlValues to prove
            // the OpenLDAP behavior.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // {iiO}, single-byte length. Octet string length extending beyond the end of the sequence (but within the buffer)
                yield return new object[] { new byte[] { 0x30, 0x0D,
                    0x02, 0x01, 0x00,
                    0x02, 0x01, 0x40,
                    0x04, 0x06, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0x80,
                    0x80, 0x80, 0x80 } };

                // {iiO}, four-byte length. Octet string length extending beyond the end of the sequence (but within the buffer)
                yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x11,
                    0x02, 0x01, 0x00,
                    0x02, 0x01, 0x40,
                    0x04, 0x84, 0x00, 0x00, 0x00, 0x06, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0x80,
                    0x80, 0x80, 0x80 } };
            }

            // {iiO}, single-byte length. Octet string length extending beyond the end of the buffer
            yield return new object[] { new byte[] { 0x30, 0x0D,
                0x02, 0x01, 0x00,
                0x02, 0x01, 0x40,
                0x04, 0x06, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };

            // {iiO}, four-byte length. Octet string length extending beyond the end of the buffer
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x11,
                0x02, 0x01, 0x00,
                0x02, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x06, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };
        }

        [Theory]
        [MemberData(nameof(ConformantControlValues))]
        public void ConformantResponseControlParsedSuccessfully(byte[] value, bool moreData, int resultSize, byte[] cookie)
            => VerifyResponseControl(value, moreData, resultSize, cookie);

        [Theory]
        [MemberData(nameof(NonconformantControlValues))]
        public void NonconformantResponseControlParsedSuccessfully(byte[] value, bool moreData, int resultSize, byte[] cookie)
            => VerifyResponseControl(value, moreData, resultSize, cookie);

        [Theory]
        [MemberData(nameof(InvalidControlValues))]
        public void InvalidResponseControlThrowsException(byte[] value)
        {
            DirectoryControl control = new(ControlOid, value, true, true);

            Assert.Throws<BerConversionException>(() => TransformResponseControl(control, false));
        }

        private static void VerifyResponseControl(byte[] value, bool moreData, int resultSize, byte[] cookie)
        {
            DirectoryControl control = new(ControlOid, value, true, true);
            DirSyncResponseControl castControl = TransformResponseControl(control, true) as DirSyncResponseControl;

            Assert.Equal(moreData, castControl.MoreData);
            Assert.Equal(resultSize, castControl.ResultSize);
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

                return Assert.IsType<DirSyncResponseControl>(resultantControl);
            }
            else
            {
                return resultantControl;
            }
        }
    }
}
