// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    using System.Diagnostics;

    internal static class CoreSwitches
    {
        private static BooleanSwitch s_perfTrack;

        public static BooleanSwitch PerfTrack
        {
            get
            {
                if (s_perfTrack == null)
                {
                    s_perfTrack = new BooleanSwitch("PERFTRACK", "Debug performance critical sections.");
                }
                return s_perfTrack;
            }
        }
    }
}

