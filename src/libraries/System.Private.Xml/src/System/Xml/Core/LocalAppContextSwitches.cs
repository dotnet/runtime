// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml
{
    internal static class LocalAppContextSwitches
    {
        public static bool DontThrowOnInvalidSurrogatePairs { get; } =
            AppContext.TryGetSwitch(
                switchName: "Switch.System.Xml.DontThrowOnInvalidSurrogatePairs",
                isEnabled: out bool value)
            ? value : false;

        public static bool IgnoreEmptyKeySequences { get; } =
            AppContext.TryGetSwitch(
                switchName: "Switch.System.Xml.IgnoreEmptyKeySequences",
                isEnabled: out bool value)
            ? value : false;

        public static bool IgnoreKindInUtcTimeSerialization { get; } =
            AppContext.TryGetSwitch(
                switchName: "Switch.System.Xml.IgnoreKindInUtcTimeSerialization",
                isEnabled: out bool value)
            ? value : false;

        public static bool LimitXPathComplexity { get; } =
            AppContext.TryGetSwitch(
                switchName: "Switch.System.Xml.LimitXPathComplexity",
                isEnabled: out bool value)
            ? value : false;

        public static bool AllowDefaultResolver { get; } =
            AppContext.TryGetSwitch(
                switchName: "Switch.System.Xml.AllowDefaultResolver",
                isEnabled: out bool value)
            ? value : false;


        public static bool IsNetworkingEnabledByDefault { get; } =
            AppContext.TryGetSwitch(
                switchName: "System.Xml.XmlResolver.IsNetworkingEnabledByDefault",
                isEnabled: out bool value)
            ? value : true;
    }
}
