// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Net.Security.Tests
{
    public class KerberosPacLogonInfoTest
    {
        // An "urn:mspac:logon-info" buffer for CONTOSO\testuser (RID 1104) in the domain
        // S-1-5-21-2127521184-1604012920-1887927527, member of the domain groups 513, 512 and
        // 1105 and of S-1-5-21-111-222-333-1201 from another domain.
        private const string LogonInfoHex =
            "01100800CCCCCCCCE00100000000000000000200FFFFFFFFFFFFFF7FFFFFFFFFFFFFFF7FFFFFFFFFFFFFFF7FFFFFFFFFFFFF" +
            "FF7FFFFFFFFFFFFFFF7FFFFFFFFFFFFFFF7F10001000040002001200120008000200000000000C0002000000000010000200" +
            "00000000140002000000000018000200000000005004000001020000030000001C0002000000000000000000000000000000" +
            "00000000000008000800200002000E000E00240002002800020000000000000000000000000000000000FFFFFFFFFFFFFF7F" +
            "FFFFFFFFFFFFFF7F0000000000000000010000002C0002000000000000000000000000000800000000000000080000007400" +
            "6500730074007500730065007200090000000000000009000000540065007300740020005500730065007200000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000030000000102" +
            "0000040000000002000004000000510400000400000004000000000000000400000044004300300031000700000000000000" +
            "0700000043004F004E0054004F0053004F00000004000000010400000000000515000000A065CF7E784B9B5FE77C87700100" +
            "00003000020004000000050000000105000000000005150000006F000000DE0000004D010000B104000000000000";

        private const string DomainSid = "S-1-5-21-2127521184-1604012920-1887927527";

        [Fact]
        public void Decode_ValidLogonInfo_ReturnsExpectedIdentity()
        {
            KerberosPacLogonInfo? logonInfo = KerberosPacLogonInfo.Decode(Convert.FromHexString(LogonInfoHex));

            Assert.NotNull(logonInfo);
            Assert.Equal("testuser", logonInfo.EffectiveName);
            Assert.Equal("CONTOSO", logonInfo.LogonDomainName);
            Assert.Equal($"{DomainSid}-1104", logonInfo.UserSid);
            Assert.Equal($"{DomainSid}-513", logonInfo.PrimaryGroupSid);
            Assert.Equal(
                new[] { $"{DomainSid}-513", $"{DomainSid}-512", $"{DomainSid}-1105", "S-1-5-21-111-222-333-1201" },
                logonInfo.GroupSids);
        }

        [Fact]
        public void Decode_TruncatedLogonInfo_DoesNotThrow()
        {
            byte[] logonInfo = Convert.FromHexString(LogonInfoHex);

            for (int length = 0; length < logonInfo.Length; length++)
            {
                // A buffer missing any of the fields the decoder reads must be rejected, while
                // one missing only trailing padding may still decode. Neither may throw.
                KerberosPacLogonInfo.Decode(logonInfo.AsSpan(0, length));
            }

            Assert.Null(KerberosPacLogonInfo.Decode(logonInfo.AsSpan(0, logonInfo.Length / 2)));
        }

        [Fact]
        public void Decode_CorruptedLogonInfo_DoesNotThrow()
        {
            byte[] logonInfo = Convert.FromHexString(LogonInfoHex);
            var random = new Random(42);

            for (int iteration = 0; iteration < 10_000; iteration++)
            {
                byte[] corrupted = (byte[])logonInfo.Clone();
                int mutations = 1 + random.Next(6);
                for (int mutation = 0; mutation < mutations; mutation++)
                {
                    corrupted[random.Next(corrupted.Length)] = (byte)random.Next(256);
                }

                // Any outcome other than an exception is acceptable. A PAC comes from the network
                // and a malformed one must degrade the identity, not fail the authentication.
                KerberosPacLogonInfo.Decode(corrupted);
            }
        }
    }
}
