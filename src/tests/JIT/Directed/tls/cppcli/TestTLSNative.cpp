// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

__declspec(thread) volatile int s_tlsFieldData = 51966;

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
