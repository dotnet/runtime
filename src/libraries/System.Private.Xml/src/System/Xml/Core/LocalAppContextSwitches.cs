// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System
{
    internal static partial class LocalAppContextSwitches
    {
        private static int s_dontThrowOnInvalidSurrogatePairs;
        public static bool DontThrowOnInvalidSurrogatePairs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return GetCachedSwitchValue("Switch.System.Xml.DontThrowOnInvalidSurrogatePairs", ref s_dontThrowOnInvalidSurrogatePairs);
            }
        }

        private static int s_ignoreEmptyKeySequences;
        public static bool IgnoreEmptyKeySequences
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return GetCachedSwitchValue("Switch.System.Xml.IgnoreEmptyKeySequencess", ref s_ignoreEmptyKeySequences);
            }
        }

        private static int s_ignoreKindInUtcTimeSerialization;
        public static bool IgnoreKindInUtcTimeSerialization
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return GetCachedSwitchValue("Switch.System.Xml.IgnoreKindInUtcTimeSerialization", ref s_ignoreKindInUtcTimeSerialization);
            }
        }

        private static int s_limitXPathComplexity;
        public static bool LimitXPathComplexity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return GetCachedSwitchValue("Switch.System.Xml.LimitXPathComplexity", ref s_limitXPathComplexity);
            }
        }

        private static int s_allowDefaultResolver;
        public static bool AllowDefaultResolver
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return GetCachedSwitchValue("Switch.System.Xml.AllowDefaultResolver", ref s_allowDefaultResolver);
            }
        }
    }
}
