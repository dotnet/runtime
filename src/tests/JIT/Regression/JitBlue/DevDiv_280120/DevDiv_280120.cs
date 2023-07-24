// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test was originally a repro for an assertion regarding incorrect lclVar ref counts due to a bug in the
// decomposition of a long-typed st.lclFld node. The repro requires that a dead store of this type survives until
// decomposition. We must therefore avoid running liveness before decomposition as part of this test, which requires
// skipping SSA (and dependent optimizations). This pass is disabled in the project file by setting JitDoSsa to 0
// before running the test.

using Xunit;

public struct S
{
    long m_fld;
    int m_a, m_b, m_c, m_d;

    [Fact]
    public static int TestEntryPoint()
    {
        S s;
        s.m_fld = 0;
        return 100;
    }
}
