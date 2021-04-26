// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;

using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public static class SocketAddressTest
    {
        [Fact]
        public static void Ctor_AddressFamily_Success()
        {
            SocketAddress sa = new SocketAddress(AddressFamily.InterNetwork);
            Assert.Equal(AddressFamily.InterNetwork, sa.Family);
            Assert.Equal(32, sa.Size);
        }

        [Fact]
        public static void Ctor_AddressFamilySize_Success()
        {
            SocketAddress sa = new SocketAddress(AddressFamily.InterNetwork, 64);
            Assert.Equal(AddressFamily.InterNetwork, sa.Family);
            Assert.Equal(64, sa.Size);
        }

        [Fact]
        public static void Ctor_AddressFamilySize_Invalid()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SocketAddress(AddressFamily.InterNetwork, 1)); //Size < MinSize (32)
        }

        [Fact]
        public static void Equals_Compare_Success()
        {
            SocketAddress sa1 = new SocketAddress(AddressFamily.InterNetwork, 64);
            SocketAddress sa2 = new SocketAddress(AddressFamily.InterNetwork, 64);
            SocketAddress sa3 = new SocketAddress(AddressFamily.InterNetworkV6, 64);
            SocketAddress sa4 = new SocketAddress(AddressFamily.InterNetwork, 60000);

            Assert.False(sa1.Equals(null));
            Assert.False(sa1.Equals(""));

            Assert.Equal(sa1, sa2);
            Assert.Equal(sa2, sa1);
            Assert.Equal(sa1.GetHashCode(), sa2.GetHashCode());

            Assert.NotEqual(sa1, sa3);
            Assert.NotEqual(sa1.GetHashCode(), sa3.GetHashCode());

            Assert.NotEqual(sa1, sa4);
        }

        [Theory]
        [InlineData(AddressFamily.InterNetwork, 64, false, "InterNetwork:64:{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}")]
        [InlineData(AddressFamily.InterNetwork, 48, false, "InterNetwork:48:{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}")]
        [InlineData(AddressFamily.InterNetworkV6, 48, false, "InterNetworkV6:48:{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0}")]
        [InlineData(AddressFamily.InterNetworkV6, 48, true, "InterNetworkV6:48:{2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47}")]
        [InlineData(AddressFamily.Unix, 255, true, "Unix:255:{2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,96,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,112,113,114,115,116,117,118,119,120,121,122,123,124,125,126,127,128,129,130,131,132,133,134,135,136,137,138,139,140,141,142,143,144,145,146,147,148,149,150,151,152,153,154,155,156,157,158,159,160,161,162,163,164,165,166,167,168,169,170,171,172,173,174,175,176,177,178,179,180,181,182,183,184,185,186,187,188,189,190,191,192,193,194,195,196,197,198,199,200,201,202,203,204,205,206,207,208,209,210,211,212,213,214,215,216,217,218,219,220,221,222,223,224,225,226,227,228,229,230,231,232,233,234,235,236,237,238,239,240,241,242,243,244,245,246,247,248,249,250,251,252,253,254}")]
        [InlineData(AddressFamily.Unix, 256, true, "Unix:256:{2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,96,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,112,113,114,115,116,117,118,119,120,121,122,123,124,125,126,127,128,129,130,131,132,133,134,135,136,137,138,139,140,141,142,143,144,145,146,147,148,149,150,151,152,153,154,155,156,157,158,159,160,161,162,163,164,165,166,167,168,169,170,171,172,173,174,175,176,177,178,179,180,181,182,183,184,185,186,187,188,189,190,191,192,193,194,195,196,197,198,199,200,201,202,203,204,205,206,207,208,209,210,211,212,213,214,215,216,217,218,219,220,221,222,223,224,225,226,227,228,229,230,231,232,233,234,235,236,237,238,239,240,241,242,243,244,245,246,247,248,249,250,251,252,253,254,255}")]
        [InlineData(AddressFamily.Unix, 257, true, "Unix:257:{2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,96,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,112,113,114,115,116,117,118,119,120,121,122,123,124,125,126,127,128,129,130,131,132,133,134,135,136,137,138,139,140,141,142,143,144,145,146,147,148,149,150,151,152,153,154,155,156,157,158,159,160,161,162,163,164,165,166,167,168,169,170,171,172,173,174,175,176,177,178,179,180,181,182,183,184,185,186,187,188,189,190,191,192,193,194,195,196,197,198,199,200,201,202,203,204,205,206,207,208,209,210,211,212,213,214,215,216,217,218,219,220,221,222,223,224,225,226,227,228,229,230,231,232,233,234,235,236,237,238,239,240,241,242,243,244,245,246,247,248,249,250,251,252,253,254,255,0}")]
        [InlineData(AddressFamily.Unix, 258, true, "Unix:258:{2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,96,97,98,99,100,101,102,103,104,105,106,107,108,109,110,111,112,113,114,115,116,117,118,119,120,121,122,123,124,125,126,127,128,129,130,131,132,133,134,135,136,137,138,139,140,141,142,143,144,145,146,147,148,149,150,151,152,153,154,155,156,157,158,159,160,161,162,163,164,165,166,167,168,169,170,171,172,173,174,175,176,177,178,179,180,181,182,183,184,185,186,187,188,189,190,191,192,193,194,195,196,197,198,199,200,201,202,203,204,205,206,207,208,209,210,211,212,213,214,215,216,217,218,219,220,221,222,223,224,225,226,227,228,229,230,231,232,233,234,235,236,237,238,239,240,241,242,243,244,245,246,247,248,249,250,251,252,253,254,255,0,1}")]
        public static void ToString_Expected_Success(AddressFamily family, int size, bool fillBytes, string expected)
        {
            var sa = size >= 0 ? new SocketAddress(family, size) : new SocketAddress(family);
            if (fillBytes)
            {
                for (int i = 2; i < sa.Size; i++)
                {
                    sa[i] = (byte)i;
                }
            }
            Assert.Equal(expected, sa.ToString());
        }

        [Theory]
        [InlineData((AddressFamily)65539, -1)]
        [InlineData((AddressFamily)65539, 16)]
        [InlineData((AddressFamily)1_000_000, -1)]
        public static void ToString_UnknownFamily_Throws(AddressFamily family, int size)
        {
            // Values above last known value should throw.
            Assert.Throws<PlatformNotSupportedException>(() => size >= 0 ? new SocketAddress(family, size) : new SocketAddress(family));
        }

        [Theory]
        [InlineData((AddressFamily)125)]
        [InlineData((AddressFamily)65535)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void ToString_LegacyUnknownFamily_Success(AddressFamily family)
        {
            // For legacy reasons, unknown values in ushort range don't throw on Windows.
            var sa = new SocketAddress(family);
            Assert.NotNull(sa.ToString());
        }

        [Theory]
        [InlineData(AddressFamily.Packet)]
        [InlineData(AddressFamily.ControllerAreaNetwork)]
        [SkipOnPlatform(TestPlatforms.Android | TestPlatforms.Linux | TestPlatforms.Browser, "Expected behavior is different on Android, Linux, or Browser")]
        public static void ToString_UnsupportedFamily_Throws(AddressFamily family)
        {
            Assert.Throws<PlatformNotSupportedException>(() => new SocketAddress(family));
        }

        [Theory]
        [InlineData(AddressFamily.Packet)]
        [InlineData(AddressFamily.ControllerAreaNetwork)]
        [PlatformSpecific(TestPlatforms.Linux)]
        public static void ToString_LinuxSpecificFamily_Success(AddressFamily family)
        {
            SocketAddress sa = new SocketAddress(family);
            Assert.Equal(family, sa.Family);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void ToString_AllFamilies_Success()
        {
            foreach (AddressFamily family in Enum.GetValues(typeof(AddressFamily)))
            {
                if (family == AddressFamily.Packet || family == AddressFamily.ControllerAreaNetwork)
                {
                    // Skip Linux specific protocols.
                    continue;
                }

                var sa = new SocketAddress(family);
                Assert.NotNull(sa.ToString());
            }
        }
    }
}
