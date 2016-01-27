// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal class CTest
{
    private static volatile int s_sx = 0;

    private static unsafe void wstrcpy(char* dmem, char* smem, int charCount)
    {
        if (charCount > 0)
        {
            if ((((int)dmem ^ (int)smem) & 3) == 0)
            {
                while (((int)dmem & 3) != 0 && charCount > 0)
                {
                    dmem[0] = smem[0];
                    dmem += 1;
                    smem += 1;
                    charCount -= 1;
                }
                if (charCount >= 8)
                {
                    charCount -= 8;
                    do
                    {
                        ((uint*)dmem)[0] = ((uint*)smem)[0];
                        ((uint*)dmem)[1] = ((uint*)smem)[1];
                        ((uint*)dmem)[2] = ((uint*)smem)[2];
                        ((uint*)dmem)[3] = ((uint*)smem)[3];
                        dmem += 8;
                        smem += 8;
                        charCount -= 8;
                    } while (charCount >= 0);
                }
                if ((charCount & 4) != 0)
                {
                    ((uint*)dmem)[0] = ((uint*)smem)[0];
                    ((uint*)dmem)[1] = ((uint*)smem)[1];
                    dmem += 4;
                    smem += 4;
                }
                if ((charCount & 2) != 0)
                {
                    ((uint*)dmem)[0] = ((uint*)smem)[0];
                    dmem += 2;
                    smem += 2;
                }
            }
            else
            {
                if (charCount >= 8)
                {
                    charCount -= 8;
                    do
                    {
                        dmem[0] = smem[0];
                        dmem[1] = smem[1];
                        dmem[2] = smem[2];
                        dmem[3] = smem[3];
                        dmem[4] = smem[4];
                        dmem[5] = smem[5];
                        dmem[6] = smem[6];
                        dmem[7] = smem[7];
                        dmem += 8;
                        smem += 8;
                        charCount -= 8;
                    }
                    while (charCount >= 0);
                }
                if ((charCount & 4) != 0)
                {
                    dmem[0] = smem[0];
                    dmem[1] = smem[1];
                    dmem[2] = smem[2];
                    dmem[3] = smem[3];
                    dmem += 4;
                    smem += 4;
                }
                if ((charCount & 2) != 0)
                {
                    dmem[0] = smem[0];
                    dmem[1] = smem[1];
                    dmem += 2;
                    smem += 2;
                }
            }

            if ((charCount & 1) != 0)
            {
                dmem[0] = smem[0];
            }
        }
    }

    public static unsafe String CtorCharPtr(IntPtr p)
    {
        char* ptr = (char*)p;
        if (ptr >= (char*)64000)
        {
            try
            {
                char* end = ptr;


                while (((uint)end & 3) != 0 && *end != 0)
                    end++;
                if (*end != 0)
                {
                    while ((end[0] & end[1]) != 0 || (end[0] != 0 && end[1] != 0))
                    {
                        end += 2;
                    }
                }
                for (; *end != 0; end++)
                    ;

                int count = (int)(end - ptr);
                String result = "abcdef";
                fixed (char* dest = result)
                    wstrcpy(dest, ptr, count);

                for (int j = 0; j < 2000; j++)
                {
                    if ((j & 5) != 0)
                        s_sx++;
                    else if ((j & 10) == 8)
                        s_sx--;

                    s_sx = (s_sx + j) / 541;
                }

                return result;
            }
            catch (NullReferenceException)
            {
                throw new Exception();
            }
        }
        else if (ptr == null)
            return "";
        else
            throw new Exception();
    }

    private static unsafe String Test()
    {
        String s = "Hello!";
        fixed (char* p = s)
        {
            return CtorCharPtr((IntPtr)p);
        }
    }

    public static int Main()
    {
        System.Console.WriteLine(Test());
        return 100;
    }
}
