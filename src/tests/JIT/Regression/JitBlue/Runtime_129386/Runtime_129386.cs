// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Under JitOptRepeat (STRESS_OPT_REPEAT), a second value-numbering + CSE round
// could observe a divide whose DivideByZeroException had been suppressed by the
// flow-sensitive GTF_DIV_MOD_NO_BY_ZERO flag (set by assertion propagation under
// a dominating zero-check). CSE/loop-hoisting then moved the division into the
// loop preheader, above the dominating check, where it executed unconditionally
// and divided by zero. The fix marks the divide with an ordering side effect
// (GTF_ORDER_SIDEEFF) when assertion propagation proves it non-throwing, so it
// stays pinned below the dominating check and cannot be reordered/hoisted above
// its guard even though it now looks non-throwing.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_129386
{
    private static volatile uint s_inputP0 = 0u;
    private static volatile uint s_inputP1 = 0xFFFFFFFFu;
    private static int[] s_trace = new int[64];
    private static int s_traceLen = 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static uint Fn30000002142(uint p0, uint p1)
    {
        unchecked
        {
            uint v1 = 0, v3 = 0, v4 = 0, v5 = 0, v7 = 0, v11 = 0, v13 = 0, v14 = 0, v19 = 0,
                 v22 = 0, v23 = 0, v24 = 0, v25 = 0, v33 = 0, v35 = 0, v39 = 0, v42 = 0,
                 v50 = 0, v51 = 0, v58 = 0, v61 = 0, v63 = 0, v66 = 0, v67 = 0, v68 = 0,
                 v69 = 0, v71 = 0, v72 = 0, v73 = 0, v74 = 0, v75 = 0, v78 = 0, v80 = 0,
                 v82 = 0, v83 = 0, v84 = 0, v85 = 0, v86 = 0, v87 = 0, v88 = 0, v93 = 0,
                 v119 = 0, v120 = 0, v121 = 0, v123 = 0, v124 = 0;
            int v6 = 0, v10 = 0, v17 = 0, v21 = 0, v26 = 0, v70 = 0, v76 = 0, v77 = 0,
                v79 = 0, v81 = 0, v118 = 0, v122 = 0;

        b0:
            v6 = (0u < p0) ? 1 : 0;
            if (v6 != 0) goto b1;
            goto b3;
        b1:
            v10 = BitOperations.IsPow2(v7) ? 1 : 0;
            v11 = (uint)v10;
            v14 = v13 - 0x000D3403u;
            v17 = (v14 > v4) ? 1 : 0;
            if (v17 != 0) goto b2;
            goto b10;
        b2:
            s_trace[s_traceLen++] = 2;
            v19 = v5 % p0;
            v21 = (p0 <= v11) ? 1 : 0;
            if (v21 != 0) goto b10;
            goto b11;
        b3:
            v22 = v14 - 0x000CBA13u;
            v23 = v11 | 0xFFFC3D1Au;
            v24 = v4 & v22;
            v25 = 0u & v1;
            v26 = (v24 > p0) ? 1 : 0;
            if (v26 != 0) goto b10;
            goto b12;
        b10:
            s_trace[s_traceLen++] = 10;
            v66 = v51 >>> (int)v61;
            v67 = v58 - 0x0004A8B9u;
            v68 = Math.Max(0xFFFA3E09u, 0xFFFFFFFDu);
            v69 = v33 - v50;
            v70 = BitOperations.PopCount(0xFFFBC8AAu);
            v71 = (uint)v70;
            v72 = v4 | 0x00004D11u;
            goto b19;
        b11:
            s_trace[s_traceLen++] = 11;
            v73 = v51 + 0x000003E8u;
            v74 = BitOperations.RotateRight(v39, (int)v42);
            v75 = 0xFFFAF3CAu + v58;
            v76 = (v68 == v19) ? 1 : 0;
            if (v76 != 0) goto b13;
            goto b0;
        b12:
            s_trace[s_traceLen++] = 12;
            v77 = (v4 < v23) ? 1 : 0;
            v78 = (uint)v77;
            v79 = (v35 <= 0x000B9492u) ? 1 : 0;
            v80 = (uint)v79;
            v81 = BitOperations.TrailingZeroCount(0xFFFFFFFFu);
            v82 = (uint)v81;
            return v82;
        b13:
            s_trace[s_traceLen++] = 13;
            v83 = ~v23;
            v84 = v25 + v1;
            v85 = Math.Max(0x80000000u, p1);
            v86 = 0xFFFF5264u % v84;
            v87 = p0 & 0xFFFFFFF9u;
            v88 = ~p0;
            goto b2;
        b19:
            s_trace[s_traceLen++] = 19;
            v118 = (v83 <= 0x000BF458u) ? 1 : 0;
            v119 = (uint)v118;
            v120 = 0x0000001Fu * v93;
            v121 = ~v75;
            v122 = BitOperations.Log2(v24);
            v123 = (uint)v122;
            v124 = v63 + v3;
            return v124;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        // With p0 == 0 the source-correct path is b0 -> b3 -> b12 -> return 0;
        // block b2 (which evaluates 'v5 % p0', a divide-by-zero) is unreachable.
        // The miscompile produced a DivideByZeroException instead.
        uint result = Fn30000002142(s_inputP0, s_inputP1);
        return result == 0 ? 100 : 101;
    }
}
