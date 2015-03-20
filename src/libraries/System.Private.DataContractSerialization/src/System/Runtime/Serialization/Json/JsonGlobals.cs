// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//----------------------------------------------------------------
//----------------------------------------------------------------

using System.Runtime.Serialization;
using System.Xml;
using System.Security;
using System.Reflection;
using System.Text;


namespace System.Runtime.Serialization.Json
{
    internal static class JsonGlobals
    {
        public const char QuoteChar = '"';
        public const string KeyString = "Key";
        public const string ValueString = "Value";
        public const string ServerTypeString = "__type";
        public static readonly int DataContractXsdBaseNamespaceLength = Globals.DataContractXsdBaseNamespace.Length;
        public static readonly long unixEpochTicks = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
        public static readonly SecurityException SecurityException = new SecurityException();
        public static readonly UnicodeEncoding ValidatingBEUTF16 = new UnicodeEncoding(true, false, true);
        public static readonly UnicodeEncoding ValidatingUTF16 = new UnicodeEncoding(false, false, true);
        public static readonly UTF8Encoding ValidatingUTF8 = new UTF8Encoding(false, true);
        public const string PositiveInf = "INF";
        public const string NegativeInf = "-INF";
        public static readonly char[] FloatingPointCharacters = new char[] { '.', 'e', 'E' };

        public const string SimpleSMWInternalsVisiblePattern = @"^[\s]*System\.ServiceModel\.Web[\s]*$";
        public const string FullSMWInternalsVisiblePattern = @"^[\s]*System\.ServiceModel\.Web[\s]*,[\s]*PublicKey[\s]*=[\s]*(?i:00240000048000009400000006020000002400005253413100040000010001008d56c76f9e8649383049f383c44be0ec204181822a6c31cf5eb7ef486944d032188ea1d3920763712ccb12d75fb77e9811149e6148e5d32fbaab37611c1878ddc19e20ef135d0cb2cff2bfec3d115810c3d9069638fe4be215dbf795861920e5ab6f7db2e2ceef136ac23d5dd2bf031700aec232f6c6b1c785b4305c123b37ab)[\s]*$";

        [SecurityCritical]
        private static string[] s_jsonSerializationPatterns;
        internal static string[] JsonSerializationPatterns
        {
            [SecuritySafeCritical]
            get
            {
                if (s_jsonSerializationPatterns == null)
                {
                    s_jsonSerializationPatterns = new string[] { Globals.SimpleSRSInternalsVisiblePattern, Globals.FullSRSInternalsVisiblePattern,
                                                               SimpleSMWInternalsVisiblePattern, FullSMWInternalsVisiblePattern };
                }
                return s_jsonSerializationPatterns;
            }
        }
    }
}
