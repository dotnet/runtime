// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

////////////////////////////////////////////////////////////////////////////////
// Tests
////////////////////////////////////////////////////////////////////////////////

namespace NativeVarargTest
{
    public class ManagedNativeVarargTests
    {
        ////////////////////////////////////////////////////////////////////////
        // Test passing fixed args to functions marked varargs
        //
        // Does not use ArgIterator, only tests fixed args not varargs.
        //
        // Note that all methods will take an empty arglist, which will mark
        // the method as vararg.
        ////////////////////////////////////////////////////////////////////////
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int TestPassingIntsNoVarargs(int one, 
                                                   int two, 
                                                   int three, 
                                                   int four,
                                                   int five, 
                                                   int six, 
                                                   int seven, 
                                                   int eight,
                                                   int nine,
                                                   __arglist)
        {
            return one + two + three + four + five + six + seven + eight + nine;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static long TestPassingLongsNoVarargs(long one, 
                                                     long two, 
                                                     long three, 
                                                     long four,
                                                     long five, 
                                                     long six, 
                                                     long seven, 
                                                     long eight,
                                                     long nine,
                                                     __arglist)
        {
            return one + two + three + four + five + six + seven + eight + nine;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float TestPassingFloatsNoVarargs(float one, 
                                                     float two, 
                                                     float three, 
                                                     float four,
                                                     float five, 
                                                     float six, 
                                                     float seven, 
                                                     float eight,
                                                     float nine,
                                                     __arglist)
        {
            return one + two + three + four + five + six + seven + eight + nine;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static double TestPassingDoublesNoVarargs(double one, 
                                                         double two, 
                                                         double three, 
                                                         double four,
                                                         double five, 
                                                         double six, 
                                                         double seven, 
                                                         double eight,
                                                         double nine,
                                                         __arglist)
        {
            return one + two + three + four + five + six + seven + eight + nine;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float TestPassingIntAndFloatsNoVarargs(int one, 
                                                             int two, 
                                                             int three, 
                                                             int four,
                                                             int five, 
                                                             int six, 
                                                             int seven, 
                                                             int eight,
                                                             int nine,
                                                             float ten, 
                                                             float eleven, 
                                                             float twelve, 
                                                             float thirteen,
                                                             float fourteen, 
                                                             float fifteen, 
                                                             float sixteen, 
                                                             float seventeen,
                                                             float eighteen,
                                                            __arglist)
        {
            float sum = one + two + three + four + five + six + seven + eight + nine;
            sum += ten + eleven + twelve + thirteen + fourteen + fifteen + sixteen + seventeen + eighteen;

            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float TestPassingFloatsAndIntNoVarargs(float one, 
                                                             float two, 
                                                             float three, 
                                                             float four,
                                                             float five, 
                                                             float six, 
                                                             float seven, 
                                                             float eight,
                                                             float nine,
                                                             int ten, 
                                                             int eleven, 
                                                             int twelve, 
                                                             int thirteen,
                                                             int fourteen, 
                                                             int fifteen, 
                                                             int sixteen, 
                                                             int seventeen,
                                                             int eighteen,
                                                            __arglist)
        {
            float sum = one + two + three + four + five + six + seven + eight + nine;
            sum += ten + eleven + twelve + thirteen + fourteen + fifteen + sixteen + seventeen + eighteen;

            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static double TestPassingIntAndDoublesNoVarargs(int one, 
                                                               int two, 
                                                               int three, 
                                                               int four,
                                                               int five, 
                                                               int six, 
                                                               int seven, 
                                                               int eight,
                                                               int nine,
                                                               double ten, 
                                                               double eleven, 
                                                               double twelve, 
                                                               double thirteen,
                                                               double fourteen, 
                                                               double fifteen, 
                                                               double sixteen, 
                                                               double seventeen,
                                                               double eighteen,
                                                              __arglist)
        {
            double sum = one + two + three + four + five + six + seven + eight + nine;
            sum += ten + eleven + twelve + thirteen + fourteen + fifteen + sixteen + seventeen + eighteen;

            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static double TestPassingDoublesAndIntNoVarargs(double one, 
                                                               double two, 
                                                               double three, 
                                                               double four,
                                                               double five, 
                                                               double six, 
                                                               double seven, 
                                                               double eight,
                                                               double nine,
                                                               int ten, 
                                                               int eleven, 
                                                               int twelve, 
                                                               int thirteen,
                                                               int fourteen, 
                                                               int fifteen, 
                                                               int sixteen, 
                                                               int seventeen,
                                                               int eighteen,
                                                              __arglist)
        {
            double sum = one + two + three + four + five + six + seven + eight + nine;
            sum += ten + eleven + twelve + thirteen + fourteen + fifteen + sixteen + seventeen + eighteen;

            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float TestPassingLongAndFloatsNoVarargs(long one, 
                                                              long two, 
                                                              long three, 
                                                              long four,
                                                              long five, 
                                                              long six, 
                                                              long seven, 
                                                              long eight,
                                                              long nine,
                                                              float ten, 
                                                              float eleven, 
                                                              float twelve, 
                                                              float thirteen,
                                                              float fourteen, 
                                                              float fifteen, 
                                                              float sixteen, 
                                                              float seventeen,
                                                              float eighteen,
                                                             __arglist)
        {
            float sum = one + two + three + four + five + six + seven + eight + nine;
            sum += ten + eleven + twelve + thirteen + fourteen + fifteen + sixteen + seventeen + eighteen;

            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float TestPassingFloatsAndlongNoVarargs(float one, 
                                                              float two, 
                                                              float three, 
                                                              float four,
                                                              float five, 
                                                              float six, 
                                                              float seven, 
                                                              float eight,
                                                              float nine,
                                                              long ten, 
                                                              long eleven, 
                                                              long twelve, 
                                                              long thirteen,
                                                              long fourteen, 
                                                              long fifteen, 
                                                              long sixteen, 
                                                              long seventeen,
                                                              long eighteen,
                                                             __arglist)
        {
            float sum = one + two + three + four + five + six + seven + eight + nine;
            sum += ten + eleven + twelve + thirteen + fourteen + fifteen + sixteen + seventeen + eighteen;

            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static double TestPassinglongAndDoublesNoVarargs(long one, 
                                                                long two, 
                                                                long three, 
                                                                long four,
                                                                long five, 
                                                                long six, 
                                                                long seven, 
                                                                long eight,
                                                                long nine,
                                                                double ten, 
                                                                double eleven, 
                                                                double twelve, 
                                                                double thirteen,
                                                                double fourteen, 
                                                                double fifteen, 
                                                                double sixteen, 
                                                                double seventeen,
                                                                double eighteen,
                                                               __arglist)
        {
            double sum = one + two + three + four + five + six + seven + eight + nine;
            sum += ten + eleven + twelve + thirteen + fourteen + fifteen + sixteen + seventeen + eighteen;

            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static double TestPassingDoublesAndlongNoVarargs(double one, 
                                                                double two, 
                                                                double three, 
                                                                double four,
                                                                double five, 
                                                                double six, 
                                                                double seven, 
                                                                double eight,
                                                                double nine,
                                                                long ten, 
                                                                long eleven, 
                                                                long twelve, 
                                                                long thirteen,
                                                                long fourteen, 
                                                                long fifteen, 
                                                                long sixteen, 
                                                                long seventeen,
                                                                long eighteen,
                                                               __arglist)
        {
            double sum = one + two + three + four + five + six + seven + eight + nine;
            sum += ten + eleven + twelve + thirteen + fourteen + fifteen + sixteen + seventeen + eighteen;

            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int TestPassingTwoIntStructsNoVarargs(TwoIntStruct one, 
                                                            TwoIntStruct two, 
                                                            TwoIntStruct three, 
                                                            TwoIntStruct four,
                                                            TwoIntStruct five, 
                                                            TwoIntStruct six, 
                                                            TwoIntStruct seven, 
                                                            TwoIntStruct eight,
                                                            TwoIntStruct nine,
                                                            TwoIntStruct ten, 
                                                            __arglist)
        {
            int sum = one.a + one.b;
            sum += two.a + two.b;
            sum += three.a + three.b;
            sum += four.a + four.b;
            sum += five.a + five.b;
            sum += six.a + six.b;
            sum += seven.a + seven.b;
            sum += eight.a + eight.b;
            sum += nine.a + nine.b;
            sum += ten.a + ten.b;

            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int TestPassingFourIntStructsNoVarargs(FourIntStruct one, 
                                                             FourIntStruct two, 
                                                             FourIntStruct three, 
                                                             FourIntStruct four,
                                                             FourIntStruct five, 
                                                             FourIntStruct six, 
                                                             FourIntStruct seven, 
                                                             FourIntStruct eight,
                                                             FourIntStruct nine,
                                                             FourIntStruct ten, 
                                                             __arglist)
        {
            int sum = one.a + one.b + one.c + one.d;
            sum += two.a + two.b + two.c + two.d;
            sum += three.a + three.b + three.c + three.d;
            sum += four.a + four.b + four.c + four.d;
            sum += five.a + five.b + five.c + five.d;
            sum += six.a + six.b + six.c + six.d;
            sum += seven.a + seven.b + seven.c + seven.d;
            sum += eight.a + eight.b + eight.c + eight.d;
            sum += nine.a + nine.b + nine.c + nine.d;
            sum += ten.a + ten.b + ten.c + ten.d;

            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool TestPassingTwoLongStructsNoVarargs(int count,
                                                              long expected,
                                                              TwoLongStruct one, 
                                                              TwoLongStruct two, 
                                                              TwoLongStruct three, 
                                                              TwoLongStruct four,
                                                              TwoLongStruct five, 
                                                              TwoLongStruct six, 
                                                              TwoLongStruct seven, 
                                                              TwoLongStruct eight,
                                                              TwoLongStruct nine,
                                                              TwoLongStruct ten, 
                                                              __arglist)
        {
            long sum = one.a + one.b;
            sum += two.a + two.b;
            sum += three.a + three.b;
            sum += four.a + four.b;
            sum += five.a + five.b;
            sum += six.a + six.b;
            sum += seven.a + seven.b;
            sum += eight.a + eight.b;
            sum += nine.a + nine.b;
            sum += ten.a + ten.b;

            return sum == expected;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static long TestPassingTwoLongStructsNoVarargs(TwoLongStruct one, 
                                                              TwoLongStruct two, 
                                                              TwoLongStruct three, 
                                                              TwoLongStruct four,
                                                              TwoLongStruct five, 
                                                              TwoLongStruct six, 
                                                              TwoLongStruct seven, 
                                                              TwoLongStruct eight,
                                                              TwoLongStruct nine,
                                                              TwoLongStruct ten, 
                                                              __arglist)
        {
            long sum = one.a + one.b;
            sum += two.a + two.b;
            sum += three.a + three.b;
            sum += four.a + four.b;
            sum += five.a + five.b;
            sum += six.a + six.b;
            sum += seven.a + seven.b;
            sum += eight.a + eight.b;
            sum += nine.a + nine.b;
            sum += ten.a + ten.b;

            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static long TestPassingTwoLongStructsAndIntNoVarargs(int a,
                                                           TwoLongStruct one, 
                                                           TwoLongStruct two, 
                                                           TwoLongStruct three, 
                                                           TwoLongStruct four,
                                                           TwoLongStruct five, 
                                                           TwoLongStruct six, 
                                                           TwoLongStruct seven, 
                                                           TwoLongStruct eight,
                                                           TwoLongStruct nine,
                                                           TwoLongStruct ten, 
                                                           __arglist)
        {
            long sum = one.a + one.b;
            sum += two.a + two.b;
            sum += three.a + three.b;
            sum += four.a + four.b;
            sum += five.a + five.b;
            sum += six.a + six.b;
            sum += seven.a + seven.b;
            sum += eight.a + eight.b;
            sum += nine.a + nine.b;
            sum += ten.a + ten.b;

            sum += a;

            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static long TestPassingFourLongStructsNoVarargs(FourLongStruct one, 
                                                               FourLongStruct two, 
                                                               FourLongStruct three, 
                                                               FourLongStruct four,
                                                               FourLongStruct five, 
                                                               FourLongStruct six, 
                                                               FourLongStruct seven, 
                                                               FourLongStruct eight,
                                                               FourLongStruct nine,
                                                               FourLongStruct ten, 
                                                               __arglist)
        {
            long sum = one.a + one.b + one.c + one.d;
            sum += two.a + two.b + two.c + two.d;
            sum += three.a + three.b + three.c + three.d;
            sum += four.a + four.b + four.c + four.d;
            sum += five.a + five.b + five.c + five.d;
            sum += six.a + six.b + six.c + six.d;
            sum += seven.a + seven.b + seven.c + seven.d;
            sum += eight.a + eight.b + eight.c + eight.d;
            sum += nine.a + nine.b + nine.c + nine.d;
            sum += ten.a + ten.b + ten.c + ten.d;

            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float TestPassingTwoFloatStructsNoVarargs(TwoFloatStruct one, 
                                                     TwoFloatStruct two, 
                                                     TwoFloatStruct three, 
                                                     TwoFloatStruct four,
                                                     TwoFloatStruct five, 
                                                     TwoFloatStruct six, 
                                                     TwoFloatStruct seven, 
                                                     TwoFloatStruct eight,
                                                     TwoFloatStruct nine,
                                                     TwoFloatStruct ten, 
                                                     __arglist)
        {
            float sum = one.a + one.b;
            sum += two.a + two.b;
            sum += three.a + three.b;
            sum += four.a + four.b;
            sum += five.a + five.b;
            sum += six.a + six.b;
            sum += seven.a + seven.b;
            sum += eight.a + eight.b;
            sum += nine.a + nine.b;
            sum += ten.a + ten.b;

            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float TestPassingFourFloatStructsNoVarargs(FourFloatStruct one, 
                                                        FourFloatStruct two, 
                                                        FourFloatStruct three, 
                                                        FourFloatStruct four,
                                                        FourFloatStruct five, 
                                                        FourFloatStruct six, 
                                                        FourFloatStruct seven, 
                                                        FourFloatStruct eight,
                                                        FourFloatStruct nine,
                                                        FourFloatStruct ten, 
                                                        __arglist)
        {
            float sum = one.a + one.b + one.c + one.d;
            sum += two.a + two.b + two.c + two.d;
            sum += three.a + three.b + three.c + three.d;
            sum += four.a + four.b + four.c + four.d;
            sum += five.a + five.b + five.c + five.d;
            sum += six.a + six.b + six.c + six.d;
            sum += seven.a + seven.b + seven.c + seven.d;
            sum += eight.a + eight.b + eight.c + eight.d;
            sum += nine.a + nine.b + nine.c + nine.d;
            sum += ten.a + ten.b + ten.c + ten.d;

            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static double TestPassingTwoDoubleStructsNoVarargs(TwoDoubleStruct one, 
                                                         TwoDoubleStruct two, 
                                                         TwoDoubleStruct three, 
                                                         TwoDoubleStruct four,
                                                         TwoDoubleStruct five, 
                                                         TwoDoubleStruct six, 
                                                         TwoDoubleStruct seven, 
                                                         TwoDoubleStruct eight,
                                                         TwoDoubleStruct nine,
                                                         TwoDoubleStruct ten, 
                                                         __arglist)
        {
            double sum = one.a + one.b;
            sum += two.a + two.b;
            sum += three.a + three.b;
            sum += four.a + four.b;
            sum += five.a + five.b;
            sum += six.a + six.b;
            sum += seven.a + seven.b;
            sum += eight.a + eight.b;
            sum += nine.a + nine.b;
            sum += ten.a + ten.b;

            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static double TestPassingTwoDoubleStructsAndFloatNoVarargs(float a,
                                                                    TwoDoubleStruct one, 
                                                                    TwoDoubleStruct two, 
                                                                    TwoDoubleStruct three, 
                                                                    TwoDoubleStruct four,
                                                                    TwoDoubleStruct five, 
                                                                    TwoDoubleStruct six, 
                                                                    TwoDoubleStruct seven, 
                                                                    TwoDoubleStruct eight,
                                                                    TwoDoubleStruct nine,
                                                                    TwoDoubleStruct ten, 
                                                             __arglist)
        {
            double sum = one.a + one.b;
            sum += two.a + two.b;
            sum += three.a + three.b;
            sum += four.a + four.b;
            sum += five.a + five.b;
            sum += six.a + six.b;
            sum += seven.a + seven.b;
            sum += eight.a + eight.b;
            sum += nine.a + nine.b;
            sum += ten.a + ten.b;

            sum += a;

            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static double TestPassingFourDoubleStructsNoVarargs(FourDoubleStruct one, 
                                                                   FourDoubleStruct two, 
                                                                   FourDoubleStruct three, 
                                                                   FourDoubleStruct four,
                                                                   FourDoubleStruct five, 
                                                                   FourDoubleStruct six, 
                                                                   FourDoubleStruct seven, 
                                                                   FourDoubleStruct eight,
                                                                   FourDoubleStruct nine,
                                                                   FourDoubleStruct ten, 
                                                                   __arglist)
        {
            double sum = one.a + one.b + one.c + one.d;
            sum += two.a + two.b + two.c + two.d;
            sum += three.a + three.b + three.c + three.d;
            sum += four.a + four.b + four.c + four.d;
            sum += five.a + five.b + five.c + five.d;
            sum += six.a + six.b + six.c + six.d;
            sum += seven.a + seven.b + seven.c + seven.d;
            sum += eight.a + eight.b + eight.c + eight.d;
            sum += nine.a + nine.b + nine.c + nine.d;
            sum += ten.a + ten.b + ten.c + ten.d;

            return sum;
        }

        ////////////////////////////////////////////////////////////////////////
        // Test returns
        ////////////////////////////////////////////////////////////////////////

        public static byte TestEchoByteManagedNoVararg(byte arg, __arglist)
        {
            return arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static char TestEchoCharManagedNoVararg(char arg, __arglist)
        {
            return arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static short TestEchoShortManagedNoVararg(short arg, __arglist)
        {
            return arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int TestEchoIntManagedNoVararg(int arg, __arglist)
        {
            return arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static long TestEchoLongManagedNoVararg(long arg, __arglist)
        {
            return arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float TestEchoFloatManagedNoVararg(float arg, __arglist)
        {
            return arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static double TestEchoDoubleManagedNoVararg(double arg, __arglist)
        {
            return arg;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static OneIntStruct TestEchoOneIntStructManagedNoVararg(OneIntStruct arg, __arglist)
        {
            return arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TwoIntStruct TestEchoTwoIntStructManagedNoVararg(TwoIntStruct arg, __arglist)
        {
            return arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static OneLongStruct TestEchoOneLongStructManagedNoVararg(OneLongStruct arg, __arglist)
        {
            return arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TwoLongStruct TestEchoTwoLongStructManagedNoVararg(TwoLongStruct arg, __arglist)
        {
            return arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static EightByteStruct TestEchoEightByteStructStructManagedNoVararg(EightByteStruct arg, __arglist)
        {
            return arg;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FourIntStruct TestEchoFourIntStructManagedNoVararg(FourIntStruct arg, __arglist)
        {
            return arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static SixteenByteStruct TestEchoSixteenByteStructManagedNoVararg(SixteenByteStruct arg, __arglist)
        {
            return arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FourLongStruct TestEchoFourLongStructManagedNoVararg(FourLongStruct arg, __arglist)
        {
            return arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static OneFloatStruct TestEchoOneFloatStructManagedNoVararg(OneFloatStruct arg, __arglist) 
        {
            return arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TwoFloatStruct TestEchoTwoFloatStructManagedNoVararg(TwoFloatStruct arg, __arglist)
        {
            return arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static OneDoubleStruct TestEchoOneDoubleStructManagedNoVararg(OneDoubleStruct arg, __arglist)
        {
            return arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TwoDoubleStruct TestEchoTwoDoubleStructManagedNoVararg(TwoDoubleStruct arg, __arglist)
        {
            return arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ThreeDoubleStruct TestEchoThreeDoubleStructManagedNoVararg(ThreeDoubleStruct arg, __arglist)
        {
            return arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FourFloatStruct TestEchoFourFloatStructManagedNoVararg(FourFloatStruct arg, __arglist)
        {
            return arg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FourDoubleStruct TestEchoFourDoubleStructManagedNoVararg(FourDoubleStruct arg, __arglist)
        {
            return arg;
        }

        ////////////////////////////////////////////////////////////////////////
        // Test passing variable amount of args
        //
        // Uses ArgIterator
        ////////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int TestPassingInts(int count, __arglist)
        {
            int calculatedCount = 0;
            ArgIterator it = new ArgIterator(__arglist);

            int sum = 0;
            while (it.GetRemainingCount() != 0)
            {
                int arg = __refvalue(it.GetNextArg(), int);

                sum += arg;

                ++calculatedCount;
            }

            if (calculatedCount != count) return -1;
            
            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static long TestPassingLongs(int count, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            int calculatedCount = 0;

            long sum = 0;
            while (it.GetRemainingCount() != 0)
            {
                long arg = __refvalue(it.GetNextArg(), long);

                sum += arg;
                ++calculatedCount;
            }

            if (calculatedCount != count) return -1;
            
            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float TestPassingFloats(int count, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            int calculatedCount = 0;

            float sum = 0;
            while (it.GetRemainingCount() != 0)
            {
                float arg = __refvalue(it.GetNextArg(), float);

                sum += arg;
                ++calculatedCount;
            }

            if (calculatedCount != count) return -1;
            
            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static double TestPassingDoubles(int count, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            int calculatedCount = 0;

            double sum = 0;
            while (it.GetRemainingCount() != 0)
            {
                double arg = __refvalue(it.GetNextArg(), double);

                sum += arg;
                ++calculatedCount;
            }

            if (calculatedCount != count) return -1;
            
            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static long TestPassingIntsAndLongs(int int_count, int long_count, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);

            int count = int_count + long_count;
            long sum = 0;

            for (int index = 0; index < int_count; ++index)
            {
                sum += __refvalue(it.GetNextArg(), int);
            }

            for (int index = 0; index < long_count; ++index)
            {
                sum += __refvalue(it.GetNextArg(), long);
            }

            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static double TestPassingFloatsAndDoubles(int float_count, int double_count, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);

            int count = float_count + double_count;
            double sum = 0;

            for (int index = 0; index < float_count; ++index)
            {
                sum += __refvalue(it.GetNextArg(), float);
            }

            for (int index = 0; index < double_count; ++index)
            {
                sum += __refvalue(it.GetNextArg(), double);
            }

            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float TestPassingIntsAndFloats(float expected_value, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            float sum = 0;

            for (int index = 0; index < 6; ++index)
            {
                if (index % 2 == 0) {
                    sum += __refvalue(it.GetNextArg(), int);
                }
                else
                {
                    sum += __refvalue(it.GetNextArg(), float);
                }
            }

            if (expected_value != 66.0f) return -1;

            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static double TestPassingLongsAndDoubles(double expected_value, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            double sum = 0;

            for (int index = 0; index < 6; ++index)
            {
                if (index % 2 == 0) {
                    sum += __refvalue(it.GetNextArg(), long);
                }
                else
                {
                    sum += __refvalue(it.GetNextArg(), double);
                }
            }

            if (expected_value != 66.0) return -1;

            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int CheckPassingStruct(int count, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
    
            int passed = 0;

            bool is_b = __refvalue(it.GetNextArg(), int) == 1;
            bool is_floating = __refvalue(it.GetNextArg(), int) == 1;
            bool is_mixed = __refvalue(it.GetNextArg(), int) == 1;
            int byte_count = __refvalue(it.GetNextArg(), int);
            int struct_count = __refvalue(it.GetNextArg(), int);

            if (!is_floating)
            {
                if (byte_count == 8)
                {
                    // Eight byte structs.
                    if (is_b)
                    {
                        OneLongStruct s = new OneLongStruct();

                        long sum = 0;
                        long expected_value = __refvalue(it.GetNextArg(), long);

                        while (struct_count-- != 0) {
                            s = __refvalue(it.GetNextArg(), OneLongStruct);
                            sum += s.a;
                        }

                        if (sum != expected_value) passed = 1;
                    }
                    else
                    {
                        TwoIntStruct s = new TwoIntStruct();

                        int sum = 0;
                        int expected_value =  __refvalue(it.GetNextArg(), int);

                        while (struct_count-- != 0) {
                            s = __refvalue(it.GetNextArg(), TwoIntStruct);
                            sum += s.a + s.b;
                        }

                        if (sum != expected_value) passed = 1;
                    }
                }
                else if (byte_count == 16)
                {
                    // 16 byte structs.
                    if (is_b)
                    {
                        FourIntStruct s = new FourIntStruct();

                        int sum = 0;
                        int expected_value = __refvalue(it.GetNextArg(), int);

                        while (struct_count-- != 0) {
                            s = __refvalue(it.GetNextArg(), FourIntStruct);
                            sum += s.a + s.b + s.c + s.d;
                        }

                        if (sum != expected_value) passed = 1;
                    }
                    else
                    {
                        TwoLongStruct s = new TwoLongStruct();

                        long sum = 0;
                        long expected_value = __refvalue(it.GetNextArg(), long);
                        sum = 0;

                        while (struct_count-- != 0) {
                            s = __refvalue(it.GetNextArg(), TwoLongStruct);
                            sum += s.a + s.b;
                        }

                        if (sum != expected_value) passed = 1;
                    }
                }

                else if (byte_count == 32)
                {
                    FourLongStruct s = new FourLongStruct();
                    
                    long sum = 0;
                    long expected_value = __refvalue(it.GetNextArg(), long);

                    while (struct_count-- != 0) {
                        s = __refvalue(it.GetNextArg(), FourLongStruct);
                        sum += s.a + s.b + s.c + s.d;
                    }

                    if (sum != expected_value) passed = 1;
                }
            }
            else
            {
                if (byte_count == 8)
                {
                    // Eight byte structs.
                    if (is_b)
                    {
                        OneDoubleStruct s = new OneDoubleStruct();

                        double sum = 0;
                        double expected_value = __refvalue(it.GetNextArg(), double);

                        while (struct_count-- != 0) {
                            s = __refvalue(it.GetNextArg(), OneDoubleStruct);
                            sum += s.a;
                        }

                        if (sum != expected_value) passed = 1;
                    }
                    else
                    {
                        TwoFloatStruct s = new TwoFloatStruct();

                        float sum = 0f;
                        float expected_value = __refvalue(it.GetNextArg(), float);

                        while (struct_count-- != 0) {
                            s = __refvalue(it.GetNextArg(), TwoFloatStruct);
                            sum += s.a + s.b;
                        }

                        if (sum != expected_value) passed = 1;
                    }
                }
                else if (byte_count == 16)
                {
                    // 16 byte structs.
                    if (is_b)
                    {
                        FourFloatStruct s = new FourFloatStruct();
                        
                        float sum = 0;
                        float expected_value = __refvalue(it.GetNextArg(), float);

                        while (struct_count-- != 0) {
                            s = __refvalue(it.GetNextArg(), FourFloatStruct);
                            sum += s.a + s.b + s.c + s.d;
                        }

                        if (sum != expected_value) passed = 1;
                    }
                    else
                    {
                        TwoDoubleStruct s = new TwoDoubleStruct();
                        
                        double sum = 0;
                        double expected_value = __refvalue(it.GetNextArg(), double);

                        while (struct_count-- != 0) {
                            s = __refvalue(it.GetNextArg(), TwoDoubleStruct);
                            sum += s.a + s.b;
                        }

                        if (sum != expected_value) passed = 1;
                    }
                }

                else if (byte_count == 32)
                {
                    FourDoubleStruct s = new FourDoubleStruct();
                    
                    double sum = 0;
                    double expected_value = __refvalue(it.GetNextArg(), double);

                    while (struct_count-- != 0) {
                        s = __refvalue(it.GetNextArg(), FourDoubleStruct);
                        sum += s.a + s.b + s.c + s.d;
                    }

                    if (sum != expected_value) passed = 1;
                }
            }

            return passed;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int CheckPassingFourSixteenByteStructs(int count, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);

            int passed = 0;
            long calculated_value = 0;

            long expected_value = __refvalue(it.GetNextArg(), long);

            for (int index = 0; index < 4; ++index) {
                TwoLongStruct s = __refvalue(it.GetNextArg(), TwoLongStruct);

                calculated_value += s.a + s.b;
            }

            passed = expected_value == calculated_value ? 0 : 1;
            return passed;
        }

        ////////////////////////////////////////////////////////////////////////
        // Test returns, using passed vararg
        ////////////////////////////////////////////////////////////////////////

        public static byte TestEchoByteManaged(byte arg, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            Debug.Assert(it.GetRemainingCount() > 0);

            var varArg = __refvalue(it.GetNextArg(), byte);

            return varArg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static char TestEchoCharManaged(char arg, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            var varArg = __refvalue(it.GetNextArg(), char);

            return varArg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static short TestEchoShortManaged(short arg, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            var varArg = __refvalue(it.GetNextArg(), short);

            return varArg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int TestEchoIntManaged(int arg, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            var varArg = __refvalue(it.GetNextArg(), int);

            return varArg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static long TestEchoLongManaged(long arg, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            var varArg = __refvalue(it.GetNextArg(), long);

            return varArg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float TestEchoFloatManaged(float arg, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            var varArg = __refvalue(it.GetNextArg(), float);

            return varArg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static double TestEchoDoubleManaged(double arg, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            var varArg = __refvalue(it.GetNextArg(), double);

            return varArg;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static OneIntStruct TestEchoOneIntStructManaged(OneIntStruct arg, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            var varArg = __refvalue(it.GetNextArg(), OneIntStruct);

            return varArg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TwoIntStruct TestEchoTwoIntStructManaged(TwoIntStruct arg, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            var varArg = __refvalue(it.GetNextArg(), TwoIntStruct);

            return varArg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static OneLongStruct TestEchoOneLongStructManaged(OneLongStruct arg, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            var varArg = __refvalue(it.GetNextArg(), OneLongStruct);

            return varArg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TwoLongStruct TestEchoTwoLongStructManaged(TwoLongStruct arg, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            var varArg = __refvalue(it.GetNextArg(), TwoLongStruct);

            return varArg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static EightByteStruct TestEchoEightByteStructStructManaged(EightByteStruct arg, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            var varArg = __refvalue(it.GetNextArg(), EightByteStruct);

            return varArg;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FourIntStruct TestEchoFourIntStructManaged(FourIntStruct arg, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            var varArg = __refvalue(it.GetNextArg(), FourIntStruct);

            return varArg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static SixteenByteStruct TestEchoSixteenByteStructManaged(SixteenByteStruct arg, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            var varArg = __refvalue(it.GetNextArg(), SixteenByteStruct);

            return varArg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FourLongStruct TestEchoFourLongStructManaged(FourLongStruct arg, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            var varArg = __refvalue(it.GetNextArg(), FourLongStruct);

            return varArg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static OneFloatStruct TestEchoOneFloatStructManaged(OneFloatStruct arg, __arglist) 
        {
            ArgIterator it = new ArgIterator(__arglist);
            var varArg = __refvalue(it.GetNextArg(), OneFloatStruct);

            return varArg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TwoFloatStruct TestEchoTwoFloatStructManaged(TwoFloatStruct arg, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            var varArg = __refvalue(it.GetNextArg(), TwoFloatStruct);

            return varArg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static OneDoubleStruct TestEchoOneDoubleStructManaged(OneDoubleStruct arg, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            var varArg = __refvalue(it.GetNextArg(), OneDoubleStruct);

            return varArg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TwoDoubleStruct TestEchoTwoDoubleStructManaged(TwoDoubleStruct arg, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            var varArg = __refvalue(it.GetNextArg(), TwoDoubleStruct);

            return varArg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ThreeDoubleStruct TestEchoThreeDoubleStructManaged(ThreeDoubleStruct arg, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            var varArg = __refvalue(it.GetNextArg(), ThreeDoubleStruct);

            return varArg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FourFloatStruct TestEchoFourFloatStructManaged(FourFloatStruct arg, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            var varArg = __refvalue(it.GetNextArg(), FourFloatStruct);

            return varArg;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FourDoubleStruct TestEchoFourDoubleStructManaged(FourDoubleStruct arg, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            var varArg = __refvalue(it.GetNextArg(), FourDoubleStruct);

            return varArg;
        }

        // Tests that take the address of a parameter of a vararg method

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FourDoubleStruct TestEchoFourDoubleStructManagedViaAddress(FourDoubleStruct arg, __arglist)
        {
            ArgIterator it = new ArgIterator(__arglist);
            var varArg = __refvalue(it.GetNextArg(), FourDoubleStruct);

            return NewFourDoubleStructViaAddress(ref arg.a, ref arg.b, ref varArg.c, ref varArg.d);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static FourDoubleStruct NewFourDoubleStructViaAddress(ref double a, ref double b, ref double c, ref double d)
        {
            return new FourDoubleStruct { a = a, b = b, c = c, d = d };
        }
    }
}
