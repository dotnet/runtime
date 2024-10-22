// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    [ConditionalClass(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
    public class VlvResponseControlTests
    {
        private const string ControlOid = "2.16.840.1.113730.3.4.10";

        private static MethodInfo s_transformControlsMethod = typeof(DirectoryControl)
            .GetMethod("TransformControls", BindingFlags.NonPublic | BindingFlags.Static);

        public static IEnumerable<object[]> ConformantControlValues()
        {
            // {iie}, single-byte length
            yield return new object[] { new byte[] { 0x30, 0x09,
                0x02, 0x01, 0x00,
                0x02, 0x01, 0x20,
                0x0A, 0x01, 0x40
            }, 0x00, 0x20, (ResultCode)0x40, Array.Empty<byte>() };

            // {iie}, four-byte length
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x09,
                0x02, 0x01, 0x00,
                0x02, 0x01, 0x20,
                0x0A, 0x01, 0x40
            }, 0x00, 0x20, (ResultCode)0x40, Array.Empty<byte>() };

            // {iieO}, single-byte length. Varying combinations of a populated & zero-length OCTET STRING
            yield return new object[] { new byte[] { 0x30, 0x10,
                0x02, 0x01, 0x00,
                0x02, 0x01, 0x20,
                0x0A, 0x01, 0x40,
                0x04, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4
            }, 0x00, 0x20, (ResultCode)0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };
            yield return new object[] { new byte[] { 0x30, 0x0B,
                0x02, 0x01, 0x00,
                0x02, 0x01, 0x20,
                0x0A, 0x01, 0x40,
                0x04, 0x00
            }, 0x00, 0x20, (ResultCode)0x40, Array.Empty<byte>() };

            // {iieO}, four-byte length. Varying combinations of a populated & zero-length OCTET STRING
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x14,
                0x02, 0x01, 0x00,
                0x02, 0x01, 0x20,
                0x0A, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4
            }, 0x00, 0x20, (ResultCode)0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x0F,
                0x02, 0x01, 0x00,
                0x02, 0x01, 0x20,
                0x0A, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x00
            }, 0x00, 0x20, (ResultCode)0x40, Array.Empty<byte>() };
        }

        public static IEnumerable<object[]> NonconformantControlValues()
        {
            // {eei}, single-byte length. ASN.1 types of ENUMERATED rather than INTEGER, and INTEGER rather than ENUMERATED
            yield return new object[] { new byte[] { 0x30, 0x09,
                0x0A, 0x01, 0x00,
                0x0A, 0x01, 0x20,
                0x02, 0x01, 0x40
            }, 0x00, 0x20, (ResultCode)0x40, Array.Empty<byte>() };

            // {eei}, four-byte length. ASN.1 types of ENUMERATED rather than INTEGER, and INTEGER rather than ENUMERATED
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x09,
                0x0A, 0x01, 0x00,
                0x0A, 0x01, 0x20,
                0x02, 0x01, 0x40
            }, 0x00, 0x20, (ResultCode)0x40, Array.Empty<byte>() };

            // {eeiO}, single-byte length. ASN.1 types of ENUMERATED rather than INTEGER, and INTEGER rather than ENUMERATED
            yield return new object[] { new byte[] { 0x30, 0x10,
                0x0A, 0x01, 0x00,
                0x0A, 0x01, 0x20,
                0x02, 0x01, 0x40,
                0x04, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4
            }, 0x00, 0x20, (ResultCode)0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };

            // {eeiO}, four-byte length. ASN.1 types of ENUMERATED rather than INTEGER, and INTEGER rather than ENUMERATED
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x14,
                0x0A, 0x01, 0x00,
                0x0A, 0x01, 0x20,
                0x02, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4
            }, 0x00, 0x20, (ResultCode)0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };

            // {iieO}, single-byte length. Trailing data after the end of the sequence
            yield return new object[] { new byte[] { 0x30, 0x10,
                0x02, 0x01, 0x00,
                0x02, 0x01, 0x20,
                0x0A, 0x01, 0x40,
                0x04, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4,
                0x80, 0x80, 0x80, 0x80
            }, 0x00, 0x20, (ResultCode)0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };

            // {iieO}, four-byte length. Trailing data after the end of the sequence
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x14,
                0x02, 0x01, 0x00,
                0x02, 0x01, 0x20,
                0x0A, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4,
                0x80, 0x80, 0x80, 0x80
            }, 0x00, 0x20, (ResultCode)0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };

            // {iieO}, single-byte length. Trailing data within the sequence (after the end of the OCTET STRING)
            yield return new object[] { new byte[] { 0x30, 0x14,
                0x02, 0x01, 0x00,
                0x02, 0x01, 0x20,
                0x0A, 0x01, 0x40,
                0x04, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4,
                0x80, 0x80, 0x80, 0x80
            }, 0x00, 0x20, (ResultCode)0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };

            // {iieO}, four-byte length. Trailing data within the sequence (after the end of the OCTET STRING)
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x18,
                0x02, 0x01, 0x00,
                0x02, 0x01, 0x20,
                0x0A, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4,
                0x80, 0x80, 0x80, 0x80
            }, 0x00, 0x20, (ResultCode)0x40, new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };

            // These cases would normally fail, but TransformControls will fall back to ignore the octet string.
            // This behavior is inconsistent with other tests which cover the parsing of octet strings (which would
            // throw an exception rather than return an empty array.) It is also inconsistent between Windows and
            // non-Windows platforms.
            // {iieO}, single-byte length. Octet string length extending beyond the end of the sequence (but within the buffer)
            yield return new object[] { new byte[] { 0x30, 0x10,
                0x02, 0x01, 0x00,
                0x02, 0x01, 0x20,
                0x0A, 0x01, 0x40,
                0x04, 0x06, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0x80,
                0x80, 0x80, 0x80
            }, 0x00, 0x20, (ResultCode)0x40, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Array.Empty<byte>() : new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0x80 } };

            // {iieO}, four-byte length. Octet string length extending beyond the end of the sequence (but within the buffer)
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x14,
                0x02, 0x01, 0x00,
                0x02, 0x01, 0x20,
                0x0A, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x06, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0x80,
                0x80, 0x80, 0x80
            }, 0x00, 0x20, (ResultCode)0x40, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Array.Empty<byte>() : new byte[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0x80 } };

            // {iieO}, single-byte length. Octet string length extending beyond the end of the buffer
            yield return new object[] { new byte[] { 0x30, 0x10,
                0x02, 0x01, 0x00,
                0x02, 0x01, 0x20,
                0x0A, 0x01, 0x40,
                0x04, 0x06, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4
            }, 0x00, 0x20, (ResultCode)0x40, Array.Empty<byte>() };

            // {iieO}, four-byte length. Octet string length extending beyond the end of the buffer
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x14,
                0x02, 0x01, 0x00,
                0x02, 0x01, 0x20,
                0x0A, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x06, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4
            }, 0x00, 0x20, (ResultCode)0x40, Array.Empty<byte>() };
        }

        public static IEnumerable<object[]> InvalidControlValues()
        {
            // i, not wrapped in an ASN.1 SEQUENCE
            yield return new object[] { new byte[] { 0x02, 0x01, 0x00 } };

            // ii, not wrapped in an ASN.1 SEQUENCE
            yield return new object[] { new byte[] { 0x02, 0x01, 0x00,
                0x02, 0x01, 0x20 } };

            // iie, not wrapped in an ASN.1 SEQUENCE
            yield return new object[] { new byte[] { 0x02, 0x01, 0x00,
                0x02, 0x01, 0x20,
                0x0A, 0x01, 0x40 } };

            // iieO, single-byte length, not wrapped in an ASN.1 SEQUENCE
            yield return new object[] { new byte[] { 0x02, 0x01, 0x00,
                0x02, 0x01, 0x20,
                0x0A, 0x01, 0x40,
                0x04, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };

            // iieO, four-byte length, not wrapped in an ASN.1 SEQUENCE
            yield return new object[] { new byte[] { 0x02, 0x01, 0x00,
                0x02, 0x01, 0x20,
                0x0A, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x05, 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 } };

            // {iieO}, single-byte length, sequence length extending beyond the end of the buffer
            yield return new object[] { new byte[] { 0x30, 0x0C,
                0x02, 0x01, 0x00,
                0x02, 0x01, 0x20,
                0x0A, 0x01, 0x40,
                0x04, 0x00 } };

            // {iieO}, four-byte length, sequence length extending beyond the end of the buffer
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x10,
                0x02, 0x01, 0x00,
                0x02, 0x01, 0x20,
                0x0A, 0x01, 0x40,
                0x04, 0x84, 0x00, 0x00, 0x00, 0x00 } };
        }

        [Theory]
        [MemberData(nameof(ConformantControlValues))]
        public void ConformantResponseControlParsedSuccessfully(byte[] value, int targetPosition, int contentCount, ResultCode result, byte[] contextId)
            => VerifyResponseControl(value, targetPosition, contentCount, result, contextId);

        [Theory]
        [MemberData(nameof(NonconformantControlValues))]
        public void NonconformantResponseControlParsedSuccessfully(byte[] value, int targetPosition, int contentCount, ResultCode result, byte[] contextId)
            => VerifyResponseControl(value, targetPosition, contentCount, result, contextId);

        [Theory]
        [MemberData(nameof(InvalidControlValues))]
        public void InvalidResponseControlThrowsException(byte[] value)
        {
            DirectoryControl control = new(ControlOid, value, true, true);

            Assert.Throws<BerConversionException>(() => TransformResponseControl(control, false));
        }

        private static void VerifyResponseControl(byte[] value, int targetPosition, int contentCount, ResultCode result, byte[] contextId)
        {
            DirectoryControl control = new(ControlOid, value, true, true);
            VlvResponseControl castControl = TransformResponseControl(control, true) as VlvResponseControl;

            Assert.Equal(targetPosition, castControl.TargetPosition);
            Assert.Equal(contentCount, castControl.ContentCount);
            Assert.Equal(result, castControl.Result);
            Assert.Equal(contextId, castControl.ContextId);
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

                return Assert.IsType<VlvResponseControl>(resultantControl);
            }
            else
            {
                return resultantControl;
            }
        }
    }
}
