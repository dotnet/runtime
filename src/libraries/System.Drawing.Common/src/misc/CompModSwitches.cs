// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.ComponentModel.CompModSwitches.get_DGEditColumnEditing():System.Diagnostics.TraceSwitch")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.ComponentModel.CompModSwitches.get_LayoutPerformance():System.Diagnostics.TraceSwitch")]

namespace System.ComponentModel
{
    internal static class CompModSwitches
    {
        private static TraceSwitch s_handleLeak;

        public static TraceSwitch HandleLeak
        {
            get
            {
                if (s_handleLeak == null)
                {
                    s_handleLeak = new TraceSwitch("HANDLELEAK", "HandleCollector: Track Win32 Handle Leaks");
                }

                return s_handleLeak;
            }
        }

        private static BooleanSwitch s_traceCollect;

        public static BooleanSwitch TraceCollect
        {
            get
            {
                if (s_traceCollect == null)
                {
                    s_traceCollect = new BooleanSwitch("TRACECOLLECT", "HandleCollector: Trace HandleCollector operations");
                }

                return s_traceCollect;
            }
        }
    }
}
