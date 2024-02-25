// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System
{
    internal static partial class LocalAppContextSwitches
    {
        private static int s_enableUnsafeUTF7Encoding;
        public static bool EnableUnsafeUTF7Encoding
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("System.Text.Encoding.EnableUnsafeUTF7Encoding", ref s_enableUnsafeUTF7Encoding);
        }

        private static int s_enforceJapaneseEraYearRanges;
        public static bool EnforceJapaneseEraYearRanges
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("Switch.System.Globalization.EnforceJapaneseEraYearRanges", ref s_enforceJapaneseEraYearRanges);
        }

        private static int s_formatJapaneseFirstYearAsANumber;
        public static bool FormatJapaneseFirstYearAsANumber
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("Switch.System.Globalization.FormatJapaneseFirstYearAsANumber", ref s_formatJapaneseFirstYearAsANumber);
        }
        private static int s_enforceLegacyJapaneseDateParsing;
        public static bool EnforceLegacyJapaneseDateParsing
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("Switch.System.Globalization.EnforceLegacyJapaneseDateParsing", ref s_enforceLegacyJapaneseDateParsing);
        }

        private static int s_preserveEventListenerObjectIdentity;
        public static bool PreserveEventListenerObjectIdentity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("Switch.System.Diagnostics.EventSource.PreserveEventListenerObjectIdentity", ref s_preserveEventListenerObjectIdentity);
        }

        private static int s_forceEmitInvoke;
        public static bool ForceEmitInvoke
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("Switch.System.Reflection.ForceEmitInvoke", ref s_forceEmitInvoke);
        }

        private static int s_forceInterpretedInvoke;
        public static bool ForceInterpretedInvoke
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("Switch.System.Reflection.ForceInterpretedInvoke", ref s_forceInterpretedInvoke);
        }

        private static int s_serializationGuard;
        public static bool SerializationGuard
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("Switch.System.Runtime.Serialization.SerializationGuard", ref s_serializationGuard);
        }

        private static int s_showILOffset;
        private static bool GetDefaultShowILOffsetSetting()
        {
            if (s_showILOffset < 0) return false;
            if (s_showILOffset > 0) return true;

            // Disabled by default.
            bool isSwitchEnabled = AppContextConfigHelper.GetBooleanConfig("Switch.System.Diagnostics.StackTrace.ShowILOffsets", false);
            s_showILOffset = isSwitchEnabled ? 1 : -1;

            return isSwitchEnabled;
        }

        public static bool ShowILOffsets
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetDefaultShowILOffsetSetting();
        }

        private static int s_showGenericInstantiations;
        private static bool GetDefaultShowGenericInstantiationsSetting()
        {
            if (s_showGenericInstantiations < 0) return false;
            if (s_showGenericInstantiations > 0) return true;

            // Disabled by default.
            bool isSwitchEnabled = AppContextConfigHelper.GetBooleanConfig("Switch.System.Diagnostics.StackTrace.ShowGenericInstantiations", false);
            s_showGenericInstantiations = isSwitchEnabled ? 1 : -1;

            return isSwitchEnabled;
        }

        public static bool ShowGenericInstantiations
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetDefaultShowGenericInstantiationsSetting();
        }
    }
}
