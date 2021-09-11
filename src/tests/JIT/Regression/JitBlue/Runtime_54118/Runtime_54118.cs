// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

// Various tests for memory-dependent loop hoisting

class Runtime_54118
{
    public static int Main()
    {
        _clsVar = -1;
        int result = 0;
        int index = 0;
        void Test(string name, Func<bool> act)
        {
            Console.Write("{0}: ", name);
            if (act())
            {
                Console.WriteLine("PASS");
            }
            else
            {
                Console.WriteLine("FAIL");
                result |= 1 << index;
            }

            index++;
        }

        Test(nameof(TestConstantByref), () => TestConstantByref(2));
        Test(nameof(TestConstantArr), () => TestConstantArr(2));
        Test(nameof(TestConstantClsVar), () => TestConstantClsVar(2));
        Test(nameof(TestParamByref), () => TestParamByref(1, 2));
        Test(nameof(TestParamArr), () => TestParamArr(1, 2));
        Test(nameof(TestParamClsVar), () => TestParamClsVar(1, 2));
        Test(nameof(TestPhiByref), () => TestPhiByref(2));
        Test(nameof(TestPhiArr), () => TestPhiArr(2));
        Test(nameof(TestPhiClsVar), () => TestPhiClsVar(2));
        Test(nameof(TestCastByref), () => TestCastByref(2));
        Test(nameof(TestCastArr), () => TestCastArr(2));
        Test(nameof(TestCastClsVar), () => TestCastClsVar(2));

        return 100 + result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestConstantByref(int k)
    {
        int[] arr = { -1 };
        ref int r = ref arr[0];
        int val = -1;
        for (int i = 0; i < k; i++)
        {
            r = 1;
            val = r;
        }

        return val == 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestConstantArr(int k)
    {
        int[] arr = { -1 };
        int val = -1;
        for (int i = 0; i < k; i++)
        {
            arr[0] = 1;
            val = arr[0];
        }

        return val == 1;
    }

    static int _clsVar;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestConstantClsVar(int k)
    {
        _clsVar = -1;
        int val = -1;
        for (int i = 0; i < k; i++)
        {
            _clsVar = 1;
            val = _clsVar;
        }

        return val == 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestParamByref(int one, int k)
    {
        int[] arr = { -1 };
        ref int r = ref arr[0];
        int val = -1;
        for (int i = 0; i < k; i++)
        {
            r = one;
            val = r;
        }

        return val == 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestParamArr(int one, int k)
    {
        int[] arr = { -1 };
        int val = -1;
        for (int i = 0; i < k; i++)
        {
            arr[0] = one;
            val = arr[0];
        }

        return val == 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestParamClsVar(int one, int k)
    {
        _clsVar = -1;
        int val = -1;
        for (int i = 0; i < k; i++)
        {
            _clsVar = one;
            val = _clsVar;
        }

        return val == 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestPhiByref(int k)
    {
        int[] arr = { -1 };
        ref int r = ref arr[0];
        int val = -1;
        for (int i = 0; i < k; i++)
        {
            for (int j = 0; j < k; j++)
            {
                r = i;
                val = r;
            }
        }

        return val == 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestPhiArr(int k)
    {
        int[] arr = { -1 };
        int val = -1;
        for (int i = 0; i < k; i++)
        {
            for (int j = 0; j < k; j++)
            {
                arr[0] = i;
                val = arr[0];
            }
        }

        return val == 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestPhiClsVar(int k)
    {
        _clsVar = -1;
        int val = -1;
        for (int i = 0; i < k; i++)
        {
            for (int j = 0; j < k; j++)
            {
                _clsVar = i;
                val = _clsVar;
            }
        }

        return val == 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestCastByref(int k)
    {
        int[] arr = { -1 };
        ref int r = ref arr[0];
        int val = -1;
        for (int i = 0; i < k; i++)
        {
            for (int j = 0; j < k; j++)
            {
                r = i;
                val = (byte)r;
            }
        }

        return val == 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestCastArr(int k)
    {
        int[] arr = { -1 };
        int val = -1;
        for (int i = 0; i < k; i++)
        {
            for (int j = 0; j < k; j++)
            {
                arr[0] = i;
                val = (byte)arr[0];
            }
        }

        return val == 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestCastClsVar(int k)
    {
        _clsVar = -1;
        int val = -1;
        for (int i = 0; i < k; i++)
        {
            for (int j = 0; j < k; j++)
            {
                _clsVar = i;
                val = (byte)_clsVar;
            }
        }

        return val == 1;
    }
}
