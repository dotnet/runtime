// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    [ConditionalClass(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
    public class AsqResponseControlTests
    {
        private const string ControlOid = "1.2.840.113556.1.4.1504";

        private static MethodInfo s_transformControlsMethod = typeof(DirectoryControl)
            .GetMethod("TransformControls", BindingFlags.NonPublic | BindingFlags.Static);

        public static IEnumerable<object[]> ConformantControlValues()
        {
            // {e}, single-byte length. ENUMERATED varies between zero & non-zero
            yield return new object[] { new byte[] { 0x30, 0x03,
                0x0A, 0x01, 0x00
            }, (ResultCode)0x00 };
            yield return new object[] { new byte[] { 0x30, 0x03,
                0x0A, 0x01, 0x7F
            }, (ResultCode)0x7F };

            // {e}, four-byte length. ENUMERATED varies between zero & non-zero
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x03,
                0x0A, 0x01, 0x00
            }, (ResultCode)0x00 };
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x03,
                0x0A, 0x01, 0x7F
            }, (ResultCode)0x7F };
        }

        public static IEnumerable<object[]> NonconformantControlValues()
        {
            // {i}, single-byte length. ASN.1 type of INTEGER rather than ENUMERATED
            yield return new object[] { new byte[] { 0x30, 0x03,
                0x02, 0x01, 0x00
            }, (ResultCode)0x00 };
            yield return new object[] { new byte[] { 0x30, 0x03,
                0x02, 0x01, 0x7F
            }, (ResultCode)0x7F };

            // {i}, four-byte length. ASN.1 type of INTEGER rather than ENUMERATED
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x03,
                0x02, 0x01, 0x00
            }, (ResultCode)0x00 };
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x03,
                0x02, 0x01, 0x7F
            }, (ResultCode)0x7F };

            // {e}, single-byte length. Trailing data after the end of the sequence
            yield return new object[] { new byte[] { 0x30, 0x03,
                0x0A, 0x01, 0x00,
                0x80, 0x80, 0x80, 0x80
            }, (ResultCode)0x00 };
            yield return new object[] { new byte[] { 0x30, 0x03,
                0x0A, 0x01, 0x7F,
                0x80, 0x80, 0x80, 0x80
            }, (ResultCode)0x7F };

            // {e}, four-byte length. Trailing data after the end of the sequence
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x03,
                0x0A, 0x01, 0x00,
                0x80, 0x80, 0x80, 0x80
            }, (ResultCode)0x00 };
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x03,
                0x0A, 0x01, 0x7F,
                0x80, 0x80, 0x80, 0x80
            }, (ResultCode)0x7F };

            // {e}, single-byte length. Trailing data within the sequence
            yield return new object[] { new byte[] { 0x30, 0x07,
                0x0A, 0x01, 0x00,
                0x80, 0x80, 0x80, 0x80
            }, (ResultCode)0x00 };
            yield return new object[] { new byte[] { 0x30, 0x07,
                0x0A, 0x01, 0x7F,
                0x80, 0x80, 0x80, 0x80
            }, (ResultCode)0x7F };

            // {e}, four-byte length. Trailing data within the sequence
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x07,
                0x0A, 0x01, 0x00,
                0x80, 0x80, 0x80, 0x80
            }, (ResultCode)0x00 };
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x07,
                0x0A, 0x01, 0x7F,
                0x80, 0x80, 0x80, 0x80
            }, (ResultCode)0x7F };
        }

        public static IEnumerable<object[]> InvalidControlValues()
        {
            // e, not wrapped in an ASN.1 SEQUENCE
            yield return new object[] { new byte[] { 0x02, 0x01, 0x00 } };

            // {e}, single-byte length, sequence length extending beyond the end of the buffer
            yield return new object[] { new byte[] { 0x30, 0x04,
                0x0A, 0x01, 0x00 } };

            // {e}, four-byte length, sequence length extending beyond the end of the buffer
            yield return new object[] { new byte[] { 0x30, 0x84, 0x00, 0x00, 0x00, 0x04,
                0x0A, 0x01, 0x00 } };
        }

        [Theory]
        [MemberData(nameof(ConformantControlValues))]
        public void ConformantResponseControlParsedSuccessfully(byte[] value, ResultCode expectedResultCode)
            => VerifyResponseControl(value, expectedResultCode);

        [Theory]
        [MemberData(nameof(NonconformantControlValues))]
        public void NonconformantResponseControlParsedSuccessfully(byte[] value, ResultCode expectedResultCode)
            => VerifyResponseControl(value, expectedResultCode);

        [Theory]
        [MemberData(nameof(InvalidControlValues))]
        public void InvalidResponseControlThrowsException(byte[] value)
        {
            DirectoryControl control = new(ControlOid, value, true, true);

            Assert.Throws<BerConversionException>(() => TransformResponseControl(control, false));
        }

        private static void VerifyResponseControl(byte[] value, ResultCode expectedResultCode)
        {
            DirectoryControl control = new(ControlOid, value, true, true);
            AsqResponseControl castControl = TransformResponseControl(control, true) as AsqResponseControl;

            Assert.Equal(expectedResultCode, castControl.Result);
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

                return Assert.IsType<AsqResponseControl>(resultantControl);
            }
            else
            {
                return resultantControl;
            }
        }
    }
}
