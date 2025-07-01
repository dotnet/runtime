// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json
{
    internal static class AppContextSwitchHelper
    {
        public static bool IsSourceGenReflectionFallbackEnabled { get; } =
            AppContext.TryGetSwitch(
                switchName: "System.Text.Json.Serialization.EnableSourceGenReflectionFallback",
                isEnabled: out bool value)
            ? value : false;

        public static bool RespectNullableAnnotationsDefault { get; } =
            AppContext.TryGetSwitch(
                switchName: "System.Text.Json.Serialization.RespectNullableAnnotationsDefault",
                isEnabled: out bool value)
            ? value : false;

        public static bool RespectRequiredConstructorParametersDefault { get; } =
            AppContext.TryGetSwitch(
                switchName: "System.Text.Json.Serialization.RespectRequiredConstructorParametersDefault",
                isEnabled: out bool value)
            ? value : false;
    }
}
