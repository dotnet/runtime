// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma unmanaged
__declspec(thread) int s_tlsFieldData = 51966;
#pragma managed

public ref class TlsTest
{
public:
    static int Test()
    {
        int value = s_tlsFieldData;
        s_tlsFieldData = value;
        s_tlsFieldData = 100;
        return s_tlsFieldData;
    }
};
