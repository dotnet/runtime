// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System
{
    internal static partial class LocalAppContextSwitches
    {
        private static int s_enforceJapaneseEraYearRanges;
        public static bool EnforceJapaneseEraYearRanges
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return GetCachedSwitchValue("Switch.System.Globalization.EnforceJapaneseEraYearRanges", ref s_enforceJapaneseEraYearRanges);
            }
        }

        private static int s_formatJapaneseFirstYearAsANumber;
        public static bool FormatJapaneseFirstYearAsANumber
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return GetCachedSwitchValue("Switch.System.Globalization.FormatJapaneseFirstYearAsANumber", ref s_formatJapaneseFirstYearAsANumber);
            }
        }
        private static int s_enforceLegacyJapaneseDateParsing;
        public static bool EnforceLegacyJapaneseDateParsing
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return GetCachedSwitchValue("Switch.System.Globalization.EnforceLegacyJapaneseDateParsing", ref s_enforceLegacyJapaneseDateParsing);
            }
        }

        private static int s_preserveEventListnerObjectIdentity;
        public static bool PreserveEventListnerObjectIdentity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return GetCachedSwitchValue("Switch.System.Diagnostics.EventSource.PreserveEventListnerObjectIdentity", ref s_preserveEventListnerObjectIdentity);
            }
        }

        private static int s_serializationGuard;
        public static bool SerializationGuard
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return GetCachedSwitchValue("Switch.System.Runtime.Serialization.SerializationGuard", ref s_serializationGuard);
            }
        }
    }
}
