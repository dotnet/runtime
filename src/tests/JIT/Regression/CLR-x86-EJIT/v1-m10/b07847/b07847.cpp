// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
