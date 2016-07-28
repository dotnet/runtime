// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    internal static class LocalAppContextSwitches
    {
        public static bool DontThrowOnInvalidSurrogatePairs => false;
        public static bool IgnoreEmptyKeySequences => false;
        public static bool IgnoreKindInUtcTimeSerialization => false;

        //XmlConfiguration settings
        public static bool ProhibitDefaultUrlResolver => true;
        public static bool s_ProhibitDefaultUrlResolver => true;
        public static bool LimitXPathComplexity => false;
        public static bool EnableMemberAccessForXslCompiledTransform => true;
    }
}
