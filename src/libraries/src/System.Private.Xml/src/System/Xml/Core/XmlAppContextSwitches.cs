// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System
{
    internal partial class XmlAppContextSwitches
    {
        private const string DontThrowOnInvalidSurrogatePairsName = "DontThrowOnInvalidSurrogatePairs";
        private static int _dontThrowOnInvalidSurrogatePairs;

        public static bool DontThrowOnInvalidSurrogatePairs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return AppContextSwitches.GetCachedSwitchValue(DontThrowOnInvalidSurrogatePairsName, ref _dontThrowOnInvalidSurrogatePairs);
            }
        }

        private const string IgnoreEmptyKeySequencesName = "IgnoreEmptyKeySequencess";
        private static int _ignoreEmptyKeySequences;

        public static bool IgnoreEmptyKeySequences
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return AppContextSwitches.GetCachedSwitchValue(IgnoreEmptyKeySequencesName, ref _ignoreEmptyKeySequences);
            }
        }

        private const string IgnoreKindInUtcTimeSerializationName = "IgnoreKindInUtcTimeSerialization";
        private static int _ignoreKindInUtcTimeSerialization;

        public static bool IgnoreKindInUtcTimeSerialization
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return AppContextSwitches.GetCachedSwitchValue(IgnoreKindInUtcTimeSerializationName, ref _ignoreKindInUtcTimeSerialization);
            }
        }

        private const string LimitXPathComplexityName = "LimitXPathComplexity";
        private static int _limitXPathComplexity;

        public static bool LimitXPathComplexity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return AppContextSwitches.GetCachedSwitchValue(LimitXPathComplexityName, ref _limitXPathComplexity);
            }
        }
    }
}