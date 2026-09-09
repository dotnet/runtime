// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using namespace System;

#pragma unmanaged
__declspec(thread) int s_tlsFieldData = 32;
#pragma managed

static void Function1(int value, int remaining)
{
    array<Byte>^ memory = gcnew array<Byte>(value);
    memory[0] = 0;
    memory[value - 1] = static_cast<Byte>(value);

    if (remaining > 1)
    {
        Function1(value + 1, remaining - 1);
    }
}

public ref class Thread_EA
{
public:
    static void Run()
    {
        s_tlsFieldData = 1;
        for (int i = 1; i <= 70; i++)
        {
            s_tlsFieldData++;
            Function1(s_tlsFieldData, i);
        }

        Console::WriteLine("one thread finished");
    }
};
