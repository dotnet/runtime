// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// volatile forces every access to be emitted as a real ldsfld/stsfld,
// matching the access pattern of the original IL test this was
// converted from. #pragma optimize has no effect on /clr-compiled code,
// so it cannot be used to suppress this optimization instead.
__declspec(thread) volatile int s_tlsFieldData = 51966;

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
