// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test has two effectively identical initializations of an
// array of byte vs. an array of structs containing a single byte field.
// They should generate the same code.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class GitHub_2003
{
    static byte[] byteArray;
    struct MyByte
    {
        private readonly byte _byte;
        public MyByte(byte b)
        {
            _byte = b;
        }

        public byte Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _byte; }
        }
    }
    static MyByte[] myByteArray;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void initByteArray()
    {
        for (int j = 0; j < byteArray.Length; j++)
        {
            byteArray[j] = 123;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void initMyByteArray()
    {
        for (int j = 0; j < myByteArray.Length; j++)
        {
            myByteArray[j] = new MyByte(123);
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        byteArray = new byte[100];
        myByteArray = new MyByte[100];
        initByteArray();
        initMyByteArray();
        int returnVal = 100;
        for (int j = 0; j < 100; j++)
        {
            if (byteArray[j] != myByteArray[j].Value)
            {
                returnVal = -1;
            }
        }
        return returnVal;
    }
}
