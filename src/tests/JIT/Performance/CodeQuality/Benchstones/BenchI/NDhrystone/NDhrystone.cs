// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Adapted from
//
// Dhrystone: a synthetic systems programming benchmark
// Reinhold P. Weicker
// Communications of the ACM, Volume 27 Issue 10, Oct 1984, Pages 1013-1030

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.BenchI
{
public static class NDhrystone
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 7000000;
#endif

    static T[][] AllocArray<T>(int n1, int n2) {
        T[][] a = new T[n1][];
        for (int i = 0; i < n1; ++i) {
            a[i] = new T[n2];
        }
        return a;
    }

    enum Enumeration
    {
        Ident1 = 1, Ident2, Ident3, Ident4, Ident5
    }

    sealed class Record
    {
        public Record PtrComp;
        public Enumeration Discr;
        public Enumeration EnumComp;
        public int IntComp;
        public char[] StringComp;
    }

    static int s_intGlob;
    static bool s_boolGlob;
    static char s_char1Glob;
    static char s_char2Glob;
    static int[] m_array1Glob = new int[51];
    static int[][] m_array2Glob;
    static Record m_ptrGlb = new Record();
    static Record m_ptrGlbNext = new Record();
    static char[] m_string1Loc;
    static char[] m_string2Loc;

    static void Proc0() {
        int intLoc1;
        int intLoc2;
        int intLoc3 = 0;
        Enumeration enumLoc;

        int i;   /* modification */

        m_ptrGlb.PtrComp = m_ptrGlbNext;
        m_ptrGlb.Discr = Enumeration.Ident1;
        m_ptrGlb.EnumComp = Enumeration.Ident3;
        m_ptrGlb.IntComp = 40;
        m_ptrGlb.StringComp = "DHRYSTONE PROGRAM, SOME STRING".ToCharArray();
        m_string1Loc = "DHRYSTONE PROGRAM, 1'ST STRING".ToCharArray();
        m_array2Glob[8][7] = 10;  /* Was missing in published program */

        for (i = 0; i < Iterations; ++i) {
            Proc5();
            Proc4();
            intLoc1 = 2;
            intLoc2 = 3;
            m_string2Loc = "DHRYSTONE PROGRAM, 2'ND STRING".ToCharArray();
            enumLoc = Enumeration.Ident2;
            s_boolGlob = !Func2(m_string1Loc, m_string2Loc);
            while (intLoc1 < intLoc2) {
                intLoc3 = 5 * intLoc1 - intLoc2;
                Proc7(intLoc1, intLoc2, ref intLoc3);
                ++intLoc1;
            }
            Proc8(m_array1Glob, m_array2Glob, intLoc1, intLoc3);
            Proc1(ref m_ptrGlb);
            for (char charIndex = 'A'; charIndex <= s_char2Glob; ++charIndex) {
                if (enumLoc == Func1(charIndex, 'C')) {
                    Proc6(Enumeration.Ident1, ref enumLoc);
                }
            }
            intLoc3 = intLoc2 * intLoc1;
            intLoc2 = intLoc3 / intLoc1;
            intLoc2 = 7 * (intLoc3 - intLoc2) - intLoc1;
            Proc2(ref intLoc1);
        }
    }

    static void Proc1(ref Record ptrParIn) {
        ptrParIn.PtrComp = m_ptrGlb;
        ptrParIn.IntComp = 5;
        ptrParIn.PtrComp.IntComp = ptrParIn.IntComp;
        ptrParIn.PtrComp.PtrComp = ptrParIn.PtrComp;
        Proc3(ref ptrParIn.PtrComp.PtrComp);
        if (ptrParIn.PtrComp.Discr == Enumeration.Ident1) {
            ptrParIn.PtrComp.IntComp = 6;
            Proc6(ptrParIn.EnumComp, ref ptrParIn.PtrComp.EnumComp);
            ptrParIn.PtrComp.PtrComp = m_ptrGlb.PtrComp;
            Proc7(ptrParIn.PtrComp.IntComp, 10, ref ptrParIn.PtrComp.IntComp);
        }
        else {
            ptrParIn = ptrParIn.PtrComp;
        }
    }

    static void Proc2(ref int intParIO) {
        int intLoc;
        Enumeration enumLoc = Enumeration.Ident2;
        intLoc = intParIO + 10;

        for (;;) {
            if (s_char1Glob == 'A') {
                --intLoc;
                intParIO = intLoc - s_intGlob;
                enumLoc = Enumeration.Ident1;
            }
            if (enumLoc == Enumeration.Ident1) {
                break;
            }
        }
    }

    static void Proc3(ref Record ptrParOut) {
        if (m_ptrGlb != null) {
            ptrParOut = m_ptrGlb.PtrComp;
        }
        else {
            s_intGlob = 100;
        }

        Proc7(10, s_intGlob, ref m_ptrGlb.IntComp);
    }

    static void Proc4() {
        bool boolLoc;
        boolLoc = s_char1Glob == 'A';
        boolLoc |= s_boolGlob;
        s_char2Glob = 'B';
    }

    static void Proc5() {
        s_char1Glob = 'A';
        s_boolGlob = false;
    }

    static void Proc6(Enumeration enumParIn, ref Enumeration enumParOut) {
        enumParOut = enumParIn;
        if (!Func3(enumParIn)) {
            enumParOut = Enumeration.Ident4;
        }

        switch (enumParIn) {
            case Enumeration.Ident1:
                enumParOut = Enumeration.Ident1;
                break;
            case Enumeration.Ident2:
                if (s_intGlob > 100) {
                    enumParOut = Enumeration.Ident1;
                }
                else {
                    enumParOut = Enumeration.Ident4;
                }
                break;
            case Enumeration.Ident3:
                enumParOut = Enumeration.Ident2;
                break;
            case Enumeration.Ident4:
                break;
            case Enumeration.Ident5:
                enumParOut = Enumeration.Ident3;
                break;
        }
    }

    static void Proc7(int intParI1, int intParI2, ref int intParOut) {
        int intLoc;
        intLoc = intParI1 + 2;
        intParOut = intParI2 + intLoc;
    }

    static void Proc8(int[] array1Par, int[][] array2Par, int intParI1, int intParI2) {
        int intLoc;
        intLoc = intParI1 + 5;
        array1Par[intLoc] = intParI2;
        array1Par[intLoc + 1] = array1Par[intLoc];
        array1Par[intLoc + 30] = intLoc;
        for (int intIndex = intLoc; intIndex <= (intLoc + 1); ++intIndex) {
            array2Par[intLoc][intIndex] = intLoc;
        }
        ++array2Par[intLoc][intLoc - 1];
        array2Par[intLoc + 20][intLoc] = array1Par[intLoc];
        s_intGlob = 5;
    }

    static Enumeration Func1(char charPar1, char charPar2) {
        char charLoc1;
        char charLoc2;
        charLoc1 = charPar1;
        charLoc2 = charLoc1;
        if (charLoc2 != charPar2) {
            return (Enumeration.Ident1);
        }
        else {
            return (Enumeration.Ident2);
        }
    }

    static bool Func2(char[] strParI1, char[] strParI2) {
        int intLoc;
        char charLoc = '\0';
        intLoc = 1;
        while (intLoc <= 1) {
            if (Func1(strParI1[intLoc], strParI2[intLoc + 1]) == Enumeration.Ident1) {
                charLoc = 'A';
                ++intLoc;
            }
        }
        if (charLoc >= 'W' && charLoc <= 'Z') {
            intLoc = 7;
        }
        if (charLoc == 'X') {
            return true;
        }
        else {
            for (int i = 0; i < 30; i++) {
                if (strParI1[i] > strParI2[i]) {
                    intLoc += 7;
                    return true;
                }
            }

            return false;
        }
    }

    static bool Func3(Enumeration enumParIn) {
        Enumeration enumLoc;
        enumLoc = enumParIn;
        if (enumLoc == Enumeration.Ident3) {
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Bench() {
        m_array2Glob = AllocArray<int>(51, 51);
        Proc0();
        return true;
    }

    static bool TestBase() {
        bool result = Bench();
        return result;
    }

    [Fact]
    public static int TestEntryPoint() {
        bool result = TestBase();
        return (result ? 100 : -1);
    }
}
}
