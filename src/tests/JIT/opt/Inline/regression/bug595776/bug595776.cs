// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
internal class Buffer
{
    private byte[] _buffer;
    private int _offset;
    private int _end;

    public Buffer(String value)
    {
        int len = value.Length;
        _buffer = new byte[len];
        _offset = 0;
        _end = len - 1;
        for (int i = 0; i < len; i++)
        {
            _buffer[i] = (byte)value[i];
        }
    }

    public int Peek()
    {
        if (_offset < _end)
        {
            return _buffer[_offset];
        }
        else
        {
            return -1;
        }
    }

    public void AdvanceToEnd()
    {
        _offset = _end;
    }
}


public class Test_bug595776
{
    [Fact]
    public static int TestEntryPoint()
    {
        Buffer b1 = new Buffer("Abra-cadabra");
        int result = 0;

        if (b1.Peek() < 0)
        {
            result += 1;
        }

        b1.AdvanceToEnd();

        if (b1.Peek() < 0)
        {
            result += 2;
        }

        if (result == 2)
        {
            Console.WriteLine("====== PASSED ======");
            return 100;
        }
        else
        {
            Console.WriteLine("****** FAILED ******");
            return 101;
        }
    }
}


