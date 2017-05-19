// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    using System.Runtime.CompilerServices;

    internal static class LocalAppContextSwitches
    {
        private static int s_dontSupportPngFramesInIcons;
        private static int s_optimizePrintPreview;

        public static bool DontSupportPngFramesInIcons
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return LocalAppContext.GetCachedSwitchValue(@"Switch.System.Drawing.DontSupportPngFramesInIcons", ref LocalAppContextSwitches.s_dontSupportPngFramesInIcons);
            }
        }

        public static bool OptimizePrintPreview
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return LocalAppContext.GetCachedSwitchValue(@"Switch.System.Drawing.Printing.OptimizePrintPreview", ref LocalAppContextSwitches.s_optimizePrintPreview);
            }
        }
    }
}
