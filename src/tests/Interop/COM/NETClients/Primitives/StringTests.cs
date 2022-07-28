// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace NetClient
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Runtime.InteropServices;
    using Xunit;

    class StringTests
    {
        private readonly Server.Contract.Servers.StringTesting server;

        private readonly IEnumerable<Tuple<string, string>> addPairs = new Tuple<string, string>[]
        {
            Tuple.Create("", ""),
            Tuple.Create("", "def"),
            Tuple.Create("abc", ""),
            Tuple.Create("abc", "def"),
            Tuple.Create("", "结合"),
            Tuple.Create("结合", ""),
            Tuple.Create("a", "结合"),
            Tuple.Create("结合", "a"),
            Tuple.Create("结合", "结合"),

            // String marshalling is optimized where strings shorter than MAX_PATH are
            // allocated on the stack. Longer strings have memory allocated for them.
            Tuple.Create("123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901", "123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901")
        };

        private readonly IEnumerable<string> reversibleStrings = new string[]
        {
            "",
            "a",
            "abc",
            "reversible string",
            "Unicode 相反 Unicode",

            // Long string optimization validation
            "123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901"
        };

        public StringTests()
        {
            this.server = (Server.Contract.Servers.StringTesting)new Server.Contract.Servers.StringTestingClass();
        }

        public void Run()
        {
            this.Marshal_LPString();
            this.Marshal_LPWString();
            this.Marshal_BStrString();
            this.Marshal_LCID();
        }

        static private string Reverse(string s)
        {
            var chars = s.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }

        static private bool AllAscii(string s)
        {
            const int MaxAscii = 0x7f;
            return s.ToCharArray().All(c => c <= MaxAscii);
        }

        private void Marshal_LPString()
        {
            Console.WriteLine($"Marshal strings as { UnmanagedType.LPStr }");
            foreach (var p in addPairs)
            {
                if (!AllAscii(p.Item1) || !AllAscii(p.Item2))
                {
                    // LPStr doesn't support non-ascii characters
                    continue;
                }

                string expected = p.Item1 + p.Item2;
                string actual = this.server.Add_LPStr(p.Item1, p.Item2);
                Assert.Equal(expected, actual);
            }

            foreach (var s in reversibleStrings)
            {
                if (!AllAscii(s))
                {
                    // LPStr doesn't support non-ascii characters
                    continue;
                }

                string local = s;
                string expected = Reverse(local);

                string actual = this.server.Reverse_LPStr(local);
                Assert.Equal(expected, actual);

                actual = this.server.Reverse_LPStr_Ref(ref local);
                Assert.Equal(expected, actual);
                Assert.Equal(expected, local);

                local = s;
                actual = this.server.Reverse_LPStr_InRef(ref local);
                Assert.Equal(expected, actual);
                Assert.Equal(s, local);

                this.server.Reverse_LPStr_Out(local, out actual);
                Assert.Equal(expected, actual);

                actual = local;
                this.server.Reverse_LPStr_OutAttr(local, actual); // No-op for strings
                Assert.Equal(local, actual);
            }

            foreach (var s in reversibleStrings)
            {
                if (!AllAscii(s))
                {
                    // LPStr doesn't support non-ascii characters
                    continue;
                }

                var local = new StringBuilder(s);
                string expected = Reverse(local.ToString());

                StringBuilder actual = this.server.Reverse_SB_LPStr(local);
                Assert.Equal(expected, actual.ToString());
                Assert.Equal(expected, local.ToString());

                local = new StringBuilder(s);
                actual = this.server.Reverse_SB_LPStr_Ref(ref local);
                Assert.Equal(expected, actual.ToString());
                Assert.Equal(expected, local.ToString());

                local = new StringBuilder(s);
                actual = this.server.Reverse_SB_LPStr_InRef(ref local);
                Assert.Equal(expected, actual.ToString());

                // Palindromes are _always_ equal
                if (!string.Equals(s, expected))
                {
                    Assert.NotEqual(expected, local.ToString());
                }

                local = new StringBuilder(s);
                actual = new StringBuilder();
                this.server.Reverse_SB_LPStr_Out(local, out actual);
                Assert.Equal(expected, actual.ToString());
                Assert.Equal(expected, local.ToString());

                local = new StringBuilder(s);
                actual = new StringBuilder(s.Length);
                this.server.Reverse_SB_LPStr_OutAttr(local, actual);
                Assert.Equal(expected, actual.ToString());
                Assert.Equal(expected, local.ToString());
            }
        }

        private void Marshal_LPWString()
        {
            Console.WriteLine($"Marshal strings as { UnmanagedType.LPWStr }");
            foreach (var p in addPairs)
            {
                string expected = p.Item1 + p.Item2;
                string actual = this.server.Add_LPWStr(p.Item1, p.Item2);
                Assert.Equal(expected, actual);
            }

            foreach (var s in reversibleStrings)
            {
                string local = s;
                string expected = Reverse(local);

                string actual = this.server.Reverse_LPWStr(local);
                Assert.Equal(expected, actual);

                actual = this.server.Reverse_LPWStr_Ref(ref local);
                Assert.Equal(expected, actual);
                Assert.Equal(expected, local);

                local = s;
                actual = this.server.Reverse_LPWStr_InRef(ref local);
                Assert.Equal(expected, actual);
                Assert.Equal(s, local);

                this.server.Reverse_LPWStr_Out(local, out actual);
                Assert.Equal(expected, actual);

                actual = local;
                Assert.Throws<MarshalDirectiveException>( () => this.server.Reverse_LPWStr_OutAttr(local, actual));
            }

            foreach (var s in reversibleStrings)
            {
                var local = new StringBuilder(s);
                string expected = Reverse(local.ToString());

                StringBuilder actual = this.server.Reverse_SB_LPWStr(local);
                Assert.Equal(expected, actual.ToString());
                Assert.Equal(expected, local.ToString());

                local = new StringBuilder(s);
                actual = this.server.Reverse_SB_LPWStr_Ref(ref local);
                Assert.Equal(expected, actual.ToString());
                Assert.Equal(expected, local.ToString());

                local = new StringBuilder(s);
                actual = this.server.Reverse_SB_LPWStr_InRef(ref local);
                Assert.Equal(expected, actual.ToString());

                // Palindromes are _always_ equal
                if (!string.Equals(s, expected))
                {
                    Assert.NotEqual(expected, local.ToString());
                }

                local = new StringBuilder(s);
                actual = new StringBuilder();
                this.server.Reverse_SB_LPWStr_Out(local, out actual);
                Assert.Equal(expected, actual.ToString());
                Assert.Equal(expected, local.ToString());

                local = new StringBuilder(s);
                actual = new StringBuilder(s.Length);
                this.server.Reverse_SB_LPWStr_OutAttr(local, actual);
                Assert.Equal(expected, actual.ToString());
                Assert.Equal(expected, local.ToString());
            }
        }

        private void Marshal_BStrString()
        {
            Console.WriteLine($"Marshal strings as { UnmanagedType.BStr }");
            foreach (var p in addPairs)
            {
                string expected = p.Item1 + p.Item2;
                string actual = this.server.Add_BStr(p.Item1, p.Item2);
                Assert.Equal(expected, actual);
            }

            foreach (var s in reversibleStrings)
            {
                string local = s;
                string expected = Reverse(local);

                string actual = this.server.Reverse_BStr(local);
                Assert.Equal(expected, actual);

                actual = this.server.Reverse_BStr_Ref(ref local);
                Assert.Equal(expected, actual);
                Assert.Equal(expected, local);

                local = s;
                actual = this.server.Reverse_BStr_InRef(ref local);
                Assert.Equal(expected, actual);
                Assert.Equal(s, local);

                this.server.Reverse_BStr_Out(local, out actual);
                Assert.Equal(expected, actual);

                actual = local;
                this.server.Reverse_BStr_OutAttr(local, actual); // No-op for strings
                Assert.Equal(local, actual);
            }
        }

        private void Marshal_LCID()
        {
            Console.WriteLine("Marshal LCID");
            foreach (var s in reversibleStrings)
            {
                string local = s;
                string expected = Reverse(local);

                string actual = this.server.Reverse_LPWStr_With_LCID(local);
                Assert.Equal(expected, actual);
            }

            CultureInfo culture = new CultureInfo("es-ES", false);
            CultureInfo englishCulture = new CultureInfo("en-US", false);
            CultureInfo oldCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = culture;
                this.server.Pass_Through_LCID(out int lcid);
                Assert.Equal(englishCulture.LCID, lcid); // CLR->COM LCID marshalling is explicitly hardcoded to en-US as requested by VSTO instead of passing the current culture.
            }
            finally
            {
                CultureInfo.CurrentCulture = oldCulture;
            }
        }
    }
}
