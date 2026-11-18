// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test relies on the CMakeLists.txt forcing /Od for this project so
// the redundant compare/store below is emitted as real ldsfld/stsfld ops
// instead of being optimized away, matching the access pattern of the
// original IL test this was converted from.
__declspec(thread) int s_tlsFieldData = 51966;

public ref class TlsTest
{
public:
    static int Test()
    {
        if (s_tlsFieldData != 51966)
        {
            return 1;
        }

        s_tlsFieldData = 51967;
        return s_tlsFieldData == 51967 ? 100 : 1;
    }
};
