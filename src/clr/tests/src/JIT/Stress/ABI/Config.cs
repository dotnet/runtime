// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace ABIStress
{
    internal class Config
    {
        internal const string TailCallerPrefix = "ABIStress_TailCaller";
        internal const string TailCalleePrefix = "ABIStress_TailCallee";
        internal const string PInvokerPrefix = "ABIStress_PInvoker";
        internal const string PInvokeePrefix = "ABIStress_PInvokee";

        internal static StressModes StressModes { get; set; } = StressModes.None;
        // The base seed. This value combined with the index of the
        // caller/pinvoker/callee will uniquely determine how it is generated
        // and which callee is used.
        internal const int Seed = 0xeadbeef;
        internal const int MinParams = 1;
        internal static int MaxParams { get; set; } = 25;
        // The number of callees to use. When stressing tailcalls, this is the number of tailcallee parameter lists to pregenerate.
        // These parameter lists are pregenerated because we generate tailcallers
        // by first selecting a random parameter list. A callee is then
        // selected; to ensure we can actually do a fast tail call, we try to
        // select a callee which requires less incoming arg space.
        // For pinvokes this is the number of callees to use.
        internal const int NumCallees = 10000;
        internal static bool Verbose { get; set; }
    }

    [Flags]
    internal enum StressModes
    {
        None = 0,
        TailCalls = 0x1,
        PInvokes = 0x2,

        All = TailCalls | PInvokes,
    }
}
