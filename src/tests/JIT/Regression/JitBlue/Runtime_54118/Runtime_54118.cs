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

        Test(nameof(TestConstantByref), TestConstantByref);
        Test(nameof(TestConstantArr), TestConstantArr);
        Test(nameof(TestConstantClsVar), TestConstantClsVar);
        Test(nameof(TestParamByref), () => TestParamByref(1));
        Test(nameof(TestParamArr), () => TestParamArr(1));
        Test(nameof(TestParamClsVar), () => TestParamClsVar(1));
        Test(nameof(TestPhiByref), TestPhiByref);
        Test(nameof(TestPhiArr), TestPhiArr);
        Test(nameof(TestPhiClsVar), TestPhiClsVar);
        Test(nameof(TestCastByref), TestCastByref);
        Test(nameof(TestCastArr), TestCastArr);
        Test(nameof(TestCastClsVar), TestCastClsVar);

        return 100 + result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestConstantByref()
    {
        int[] arr = { -1 };
        ref int r = ref arr[0];
        int val = -1;
        for (int i = 0; i < 2; i++)
        {
            r = 1;
            val = r;
        }

        return val == 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestConstantArr()
    {
        int[] arr = { -1 };
        int val = -1;
        for (int i = 0; i < 2; i++)
        {
            arr[0] = 1;
            val = arr[0];
        }

        return val == 1;
    }

    static int _clsVar;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestConstantClsVar()
    {
        _clsVar = -1;
        int val = -1;
        for (int i = 0; i < 2; i++)
        {
            _clsVar = 1;
            val = _clsVar;
        }

        return val == 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestParamByref(int one)
    {
        int[] arr = { -1 };
        ref int r = ref arr[0];
        int val = -1;
        for (int i = 0; i < 2; i++)
        {
            r = one;
            val = r;
        }

        return val == 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestParamArr(int one)
    {
        int[] arr = { -1 };
        int val = -1;
        for (int i = 0; i < 2; i++)
        {
            arr[0] = one;
            val = arr[0];
        }

        return val == 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestParamClsVar(int one)
    {
        _clsVar = -1;
        int val = -1;
        for (int i = 0; i < 2; i++)
        {
            _clsVar = one;
            val = _clsVar;
        }

        return val == 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestPhiByref()
    {
        int[] arr = { -1 };
        ref int r = ref arr[0];
        int val = -1;
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                r = i;
                val = r;
            }
        }

        return val == 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestPhiArr()
    {
        int[] arr = { -1 };
        int val = -1;
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                arr[0] = i;
                val = arr[0];
            }
        }

        return val == 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestPhiClsVar()
    {
        _clsVar = -1;
        int val = -1;
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                _clsVar = i;
                val = _clsVar;
            }
        }

        return val == 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestCastByref()
    {
        int[] arr = { -1 };
        ref int r = ref arr[0];
        int val = -1;
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                r = i;
                val = (byte)r;
            }
        }

        return val == 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestCastArr()
    {
        int[] arr = { -1 };
        int val = -1;
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                arr[0] = i;
                val = (byte)arr[0];
            }
        }

        return val == 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TestCastClsVar()
    {
        _clsVar = -1;
        int val = -1;
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                _clsVar = i;
                val = (byte)_clsVar;
            }
        }

        return val == 1;
    }
}
