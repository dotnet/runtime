// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    [ConditionalClass(typeof(DirectoryServicesTestHelpers), nameof(DirectoryServicesTestHelpers.IsWindowsOrLibLdapIsInstalled))]
    public class BerConverterTests
    {
        public static IEnumerable<object[]> Encode_TestData()
        {
            yield return new object[] { "", null, new byte[0] };
            yield return new object[] { "", new object[10], new byte[0] };
            yield return new object[] { "b", new object[] { true, false, true, false }, new byte[] { 1, 1, 255 } };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                yield return new object[] { "{", new object[] { "a" }, new byte[] { 48, 0, 0, 0, 0, 0 } }; // This format is not supported by Linux OpenLDAP
            }
            yield return new object[] { "{}", new object[] { "a" }, (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 48, 132, 0, 0, 0, 0 } : new byte[] { 48, 0 } };
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                yield return new object[] { "[", new object[] { "a" }, new byte[] { 49, 0, 0, 0, 0, 0 } }; // This format is not supported by Linux OpenLDAP
            }
            yield return new object[] { "[]", new object[] { "a" }, (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 49, 132, 0, 0, 0, 0 } : new byte[] { 49, 0 } };
            yield return new object[] { "n", new object[] { "a" }, new byte[] { 5, 0 } };

            yield return new object[] { "e", new object[] { 128 }, new byte[] { 10, 2, 0, 128 } };
            yield return new object[] { "te", new object[] { 128, 0 }, new byte[] { 128, 1, 0 } };
            yield return new object[] { "tet", new object[] { 128, 0, 133 }, new byte[] { 128, 1, 0 } };

            yield return new object[] { "tetie", new object[] { 128, 0, 133, 2, 3 }, new byte[] { 128, 1, 0, 133, 1, 2, 10, 1, 3 } };
            yield return new object[] { "{tetie}", new object[] { 128, 0, 133, 2, 3 }, (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 48, 132, 0, 0, 0, 9, 128, 1, 0, 133, 1, 2, 10, 1, 3 } : new byte[] { 48, 9, 128, 1, 0, 133, 1, 2, 10, 1, 3 } };

            yield return new object[] { "bb", new object[] { true, false }, new byte[] { 1, 1, 255, 1, 1, 0 } };
            yield return new object[] { "{bb}", new object[] { true, false }, (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 48, 132, 0, 0, 0, 6, 1, 1, 255, 1, 1, 0 } : new byte[] { 48, 6, 1, 1, 255, 1, 1, 0 } };

            yield return new object[] { "ssss", new object[] { null, "", "abc", "\0" }, new byte[] { 4, 0, 4, 0, 4, 3, 97, 98, 99, 4, 1, 0 } };

            yield return new object[] { "o", new object[] { null },  new byte[] { 4, 0} };
            yield return new object[] { "X", new object[] { new byte[] { 0, 1, 2, 255 } }, (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 3, 4, 0, 1, 2, 255 } : new byte[] { 3, 2, 4, 0 } };
            yield return new object[] { "oXo", new object[] { null, new byte[] { 0, 1, 2, 255 }, new byte[0] }, (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 4, 0, 3, 4, 0, 1, 2, 255, 4, 0 } : new byte[] { 4, 0, 3, 2, 4, 0, 4, 0 } };
            yield return new object[] { "vv", new object[] { null, new string[] { "abc", "", null } }, new byte[] { 4, 3, 97, 98, 99, 4, 0, 4, 0 } };
            yield return new object[] { "{vv}", new object[] { null, new string[] { "abc", "", null } }, (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? new byte[] { 48, 132, 0, 0, 0, 9, 4, 3, 97, 98, 99, 4, 0, 4, 0 } : new byte[] { 48, 9, 4, 3, 97, 98, 99, 4, 0, 4, 0 } };
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                yield return new object[] { "VVVV", new object[] { null, new byte[][] { new byte[] { 0, 1, 2, 3 }, null }, new byte[][] { new byte[0] }, new byte[0][] }, new byte[] { 4, 4, 0, 1, 2, 3, 4, 0, 4, 0 } };
            }
        }

        [Theory]
        [MemberData(nameof(Encode_TestData))]
        public void Encode_Objects_ReturnsExpected(string format, object[] values, byte[] expected)
        {
            AssertExtensions.Equal(expected, BerConverter.Encode(format, values));
        }

        [Fact]
        public void Encode_NullFormat_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("format", () => BerConverter.Encode(null, new object[0]));
        }

        public static IEnumerable<object[]> Encode_Invalid_TestData()
        {
            yield return new object[] { "t", new object[0] };
            yield return new object[] { "t", new object[] { "string" } };
            yield return new object[] { "t", new object[] { null } };

            yield return new object[] { "i", new object[0] };
            yield return new object[] { "i", new object[] { "string" } };
            yield return new object[] { "i", new object[] { null } };

            yield return new object[] { "e", new object[0] };
            yield return new object[] { "e", new object[] { "string" } };
            yield return new object[] { "e", new object[] { null } };

            yield return new object[] { "b", new object[0] };
            yield return new object[] { "b", new object[0] };
            yield return new object[] { "b", new object[] { "string" } };
            yield return new object[] { "b", new object[] { null } };

            yield return new object[] { "s", new object[0] };
            yield return new object[] { "s", new object[] { 123 } };

            yield return new object[] { "o", new object[0] };
            yield return new object[] { "o", new object[] { "string" } };
            yield return new object[] { "o", new object[] { 123 } };

            yield return new object[] { "X", new object[0] };
            yield return new object[] { "X", new object[] { "string" } };
            yield return new object[] { "X", new object[] { 123 } };

            yield return new object[] { "v", new object[0] };
            yield return new object[] { "v", new object[] { "string" } };
            yield return new object[] { "v", new object[] { 123 } };

            yield return new object[] { "V", new object[0] };
            yield return new object[] { "V", new object[] { "string" } };
            yield return new object[] { "V", new object[] { new byte[0] } };

            yield return new object[] { "a", new object[0] };
        }

        [Theory]
        [MemberData(nameof(Encode_Invalid_TestData))]
        public void Encode_Invalid_ThrowsArgumentException(string format, object[] values)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => BerConverter.Encode(format, values));
        }

        [Theory]
        [InlineData("]")]
        [InlineData("}")]
        [InlineData("{{}}}")]
        public void Encode_InvalidFormat_ThrowsBerConversionException(string format)
        {
            Assert.Throws<BerConversionException>(() => BerConverter.Encode(format, new object[0]));
        }

        public static IEnumerable<object[]> Decode_TestData()
        {
            yield return new object[] { "{}", new byte[] { 48, 0, 0, 0, 0, 0 }, new object[0] };
            yield return new object[] { "{a}", new byte[] { 48, 132, 0, 0, 0, 5, 4, 3, 97, 98, 99 }, new object[] { "abc" } };
            yield return new object[] { "{i}", new byte[] { 48, 132, 0, 0, 0, 3, 2, 1, 10 }, new object[] { 10 } };
            yield return new object[] { "{ie}", new byte[] { 48, 132, 0, 0, 0, 6, 1, 1, 255, 1, 1, 0 }, new object[] { -1, 0 } };
            yield return new object[] { "{bb}", new byte[] { 48, 132, 0, 0, 0, 6, 1, 1, 255, 1, 1, 0 }, new object[] { true, false } };
            yield return new object[] { "{OO}", new byte[] { 48, 132, 0, 0, 0, 6, 1, 1, 255, 1, 1, 0 }, new object[] { new byte[] { 255 }, new byte[] { 0 } } };
            yield return new object[] { "{BB}", new byte[] { 48, 132, 0, 0, 0, 6, 1, 1, 255, 1, 1, 0 }, new object[] { new byte[] { 255 }, new byte[] { 0 } } };
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) // vv and VV formats are not supported yet in Linux
            {
                yield return new object[] { "{vv}", new byte[] { 48, 132, 0, 0, 0, 9, 4, 3, 97, 98, 99, 4, 0, 4, 0 }, new object[] { null, null } };
                yield return new object[] { "{vv}", new byte[] { 48, 132, 0, 0, 0, 6, 1, 1, 255, 1, 1, 0 }, new object[] { new string[] { "\x01" }, null } };
                yield return new object[] { "{VV}", new byte[] { 48, 132, 0, 0, 0, 9, 4, 3, 97, 98, 99, 4, 0, 4, 0 }, new object[] { null, null } };
                yield return new object[] { "{VV}", new byte[] { 48, 132, 0, 0, 0, 6, 1, 1, 255, 1, 1, 0 }, new object[] { new byte[][] { new byte[] { 1 } }, null } };
            }
        }

        [Theory]
        [MemberData(nameof(Decode_TestData))]
        public void Decode_Bytes_ReturnsExpected(string format, byte[] values, object[] expected)
        {
            object value = BerConverter.Decode(format, values);
            Assert.Equal(expected, value);
        }

        [Fact]
        public void Decode_NullFormat_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("format", () => BerConverter.Decode(null, new byte[0]));
        }

        [Theory]
        [InlineData("p", new byte[] { 48, 132, 0, 0, 0, 6, 1, 1, 255, 1, 1, 0 })]
        public void UnknownFormat_ThrowsArgumentException(string format, byte[] values)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => BerConverter.Decode(format, values));
        }

        public static IEnumerable<object[]> Decode_Invalid_ThrowsBerConversionException_Data()
        {
            yield return new object[] { "n", null };
            yield return new object[] { "n", new byte[0] };
            yield return new object[] { "{", new byte[] { 1 } };
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                yield return new object[] { "}", new byte[] { 1 } }; // This is considered a valid case in Linux
            }
            yield return new object[] { "{}{}{}{}{}{}{}", new byte[] { 48, 132, 0, 0, 0, 6, 1, 1, 255, 1, 1, 0 } };
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                yield return new object[] { "aaa", new byte[] { 48, 132, 0, 0, 0, 6, 1, 1, 255, 1, 1, 0 } };
            } 
            yield return new object[] { "iii", new byte[] { 48, 132, 0, 0, 0, 6, 1, 1, 255, 1, 1, 0 } };
            yield return new object[] { "eee", new byte[] { 48, 132, 0, 0, 0, 6, 1, 1, 255, 1, 1, 0 } };
            yield return new object[] { "bbb", new byte[] { 48, 132, 0, 0, 0, 6, 1, 1, 255, 1, 1, 0 } };
            yield return new object[] { "OOO", new byte[] { 48, 132, 0, 0, 0, 6, 1, 1, 255, 1, 1, 0 } };
            yield return new object[] { "BBB", new byte[] { 48, 132, 0, 0, 0, 6, 1, 1, 255, 1, 1, 0 } };
        }

        [Theory]
        [MemberData(nameof(Decode_Invalid_ThrowsBerConversionException_Data))]
        public void Decode_Invalid_ThrowsBerConversionException(string format, byte[] values)
        {
            Assert.Throws<BerConversionException>(() => BerConverter.Decode(format, values));
        }
    }
}
