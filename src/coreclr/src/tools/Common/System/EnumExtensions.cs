// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ILCompiler.DependencyAnalysis.ReadyToRun;
using Internal.JitInterface;
using Internal.ReadyToRunConstants;

namespace System
{
    /// <summary>
    /// Enum.CompareTo takes an Object, so using that requires boxing the enum we are comparing to (which is slow
    /// and allocates unnecessarily).
    /// </summary>
    internal static class EnumExtensions
    {
        public static int CompareEnum(this ReadyToRunFixupKind first, ReadyToRunFixupKind second)
        {
            return (int)first - (int)second;
        }

        public static int CompareEnum(this ImportThunk.Kind first, ImportThunk.Kind second)
        {
            return (int)first - (int)second;
        }

        public static int CompareEnum(this mdToken first, mdToken second)
        {
            return (int)first - (int)second;
        }

        public static int CompareEnum(this CORINFO_RUNTIME_LOOKUP_KIND first, CORINFO_RUNTIME_LOOKUP_KIND second)
        {
            return (int)first - (int)second;
        }
    }
}
