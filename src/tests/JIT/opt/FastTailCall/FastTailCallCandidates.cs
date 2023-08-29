// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices; 
using System.Runtime.InteropServices;
using Xunit;

////////////////////////////////////////////////////////////////////////////////
// Types
////////////////////////////////////////////////////////////////////////////////

public class FastTailCallCandidates
{
    ////////////////////////////////////////////////////////////////////////////
    // Globals
    ////////////////////////////////////////////////////////////////////////////

    public static int s_ret_value = 100;

    ////////////////////////////////////////////////////////////////////////////
    // Helpers
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Check the return value of the test and set s_ret_value if incorrect
    /// </summary>
    internal static void CheckOutput(int code)
    {
        // If there has been a previous failure then do not reset the first
        // failure this will be the return value.
        if (s_ret_value != 100)
        {
            return;
        }

        if (code != 100)
        {
            s_ret_value = code;
        }
    }

    /// <summary>
    /// Run each individual test case
    /// </summary>
    ///
    /// <remarks>
    /// If you add any new test case scenarios please use reuse code and follow
    /// the pattern below. Please increment the return value so it
    /// is easy to determine in the future which scenario is failing.
    /// </remarks>
    public static int Tester(int a)
    {
        CheckOutput(SimpleTestCase());
        CheckOutput(IntegerArgs(10, 11, 12, 13, 14, 15));
        CheckOutput(FloatArgs(10.0f, 11.0f, 12.0f, 13.0f, 14.0f, 15.0f));
        CheckOutput(IntAndFloatArgs(10, 11, 12, 13, 14, 15, 10.0f, 11.0f, 12.0f, 13.0f, 14.0f, 15.0f));
        CheckOutput(CallerGithubIssue12468(1, 2, 3, 4, 5, 6, 7, 8, new StructSizeSixteenNotExplicit(1, 2)));
        CheckOutput(DoNotFastTailCallSimple(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14));
        CheckOutput(StackBasedCaller(16, new StructSizeTwentyFour(1, 2, 3)));
        CheckOutput(CallerSimpleHFACase(new HFASize32(1.0, 2.0, 3.0, 4.0), 1.0, 2.0, 3.0, 4.0));
        CheckOutput(CallerHFACaseWithStack(1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, new HFASize32(1.0, 2.0, 3.0, 4.0)));
        CheckOutput(CallerHFACaseCalleeOnly(1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0));
        CheckOutput(CallerHFaCaseCalleeStackArgs(1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0));
        CheckOutput(DoubleCountRetBuffCaller(1));
        CheckOutput(Struct32CallerWrapper());
        CheckOutput(Struct32CallerWrapperCalleeHasStack(2));
        CheckOutput(CallerEnregisterableAmd64WindowsStructs8Bytes(1, 2));
        CheckOutput(CallerAmd64WindowsStructs7Bytes(1, 2));
        CheckOutput(CallerAmd64WindowsStructs6Bytes(1, 2));
        CheckOutput(CallerAmd64WindowsStructs5Bytes(1, 2));
        CheckOutput(CallerAmd64WindowsStructs4Bytes(1, 2));
        CheckOutput(CallerAmd64WindowsStructs3Bytes(1, 2));

        return s_ret_value;

    }

    ////////////////////////////////////////////////////////////////////////////
    // Simple fast tail call case
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Simple fast tail call case.
    /// </summary>
    ///
    /// <remarks>
    /// This is mostly supposed to be a smoke test. It can also be seen as a
    /// constant
    ///
    /// Return 100 is a pass.
    ///
    /// </remarks>
    public static int SimpleTestCase(int retValue = 10)
    {
        retValue += 1;

        if (retValue == 100)
        {
            return retValue;
        }
        else
        {
            return SimpleTestCase(retValue);
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    // Integer args
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Simple fast tail call case that includes integer args
    /// </summary>
    /// <remarks>
    ///
    /// Return 100 is a pass.
    /// Return 101 is a failure.
    ///
    /// </remarks>
    public static int IntegerArgs(int arg1,
                                  int arg2,
                                  int arg3,
                                  int arg4,
                                  int arg5,
                                  int arg6,
                                  int retValue = 10)
    {
        retValue += 1;

        if (retValue == 100)
        {
            if (arg1 != 10 ||
                arg2 != 11 ||
                arg3 != 12 ||
                arg4 != 13 ||
                arg5 != 14 ||
                arg6 != 15)
            {
                return 101;
            }

            return retValue;
        }
        else
        {
            return IntegerArgs(arg1,
                               arg2,
                               arg3,
                               arg4,
                               arg5,
                               arg6, 
                               retValue);
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    // Float args
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Simple fast tail call case that includes floating point args
    /// </summary>
    /// <remarks>
    ///
    /// Return 100 is a pass.
    /// Return 102 is a failure.
    ///
    /// </remarks>
    public static int FloatArgs(float arg1,
                                float arg2,
                                float arg3,
                                float arg4,
                                float arg5,
                                float arg6,
                                int retValue = 10)
    {
        retValue += 1;

        if (retValue == 100)
        {
            if (arg1 != 10.0f ||
                arg2 != 11.0f ||
                arg3 != 12.0f ||
                arg4 != 13.0f ||
                arg5 != 14.0f ||
                arg6 != 15.0f)
            {
                return 102;
            }

            return retValue;
        }
        else
        {
            return FloatArgs(arg1,
                             arg2,
                             arg3,
                             arg4,
                             arg5,
                             arg6, 
                             retValue);
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    // Integer and Float args
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Simple fast tail call case that includes integer and floating point args
    /// </summary>
    /// <remarks>
    ///
    /// Return 100 is a pass.
    /// Return 103 is a failure.
    ///
    /// </remarks>
    public static int IntAndFloatArgs(int argi1,
                                      int argi2,
                                      int argi3,
                                      int argi4,
                                      int argi5,
                                      int argi6,
                                      float argf1,
                                      float argf2,
                                      float argf3,
                                      float argf4,
                                      float argf5,
                                      float argf6,
                                      int retValue = 10)
    {
        retValue += 1;

        if (retValue == 100)
        {
            if (argi1 != 10 ||
                argi2 != 11 ||
                argi3 != 12 ||
                argi4 != 13 ||
                argi5 != 14 ||
                argi6 != 15 ||
                argf1 != 10.0f ||
                argf2 != 11.0f ||
                argf3 != 12.0f ||
                argf4 != 13.0f ||
                argf5 != 14.0f ||
                argf6 != 15.0f)
            {
                return 103;
            }

            return retValue;
        }
        else
        {
            return IntAndFloatArgs(argi1,
                                   argi2,
                                   argi3,
                                   argi4,
                                   argi5,
                                   argi6,
                                   argf1,
                                   argf2,
                                   argf3,
                                   argf4,
                                   argf5,
                                   argf6, 
                                   retValue);
        }
    }

    /// <summary>
    /// Decision not to tail call. See DoNotFastTailCallSimple for more info
    /// </summary>
    public static int DoNotFastTailCallHelper(int one,
                                              int two,
                                              int three,
                                              int four,
                                              int five,
                                              int six,
                                              int seven,
                                              int eight,
                                              int nine,
                                              int ten,
                                              int eleven,
                                              int twelve,
                                              int thirteen,
                                              int fourteen)
    {
        if (one == 1)
        {
            two = one + two;
        }

        if  (two == 3)
        {
            three = two + three;
        }

        if (three == 6)
        {
            four = four + three;
        }

        if (four != 10)
        {
            return 104;
        }

        if (five != 5)
        {
            return 104;
        }

        if (six != 6)
        {
            return 104;
        }
        
        if (seven != 7)
        {
            return 104;
        }

        if (eight != 8)
        {
            return 104;
        }

        if (nine != 9)
        {
            return 104;
        }

        if (ten != 10)
        {
            return 104;
        }

        if (eleven != 11)
        {
            return 104;
        }

        if (twelve != 12)
        {
            return 104;
        }

        if (thirteen != 13)
        {
            return 104;
        }

        if (fourteen != 14)
        {
            return 104;
        }

        return 100;
    }

    /// <summary>
    /// Decision not to tail call.
    /// </summary>
    /// <remarks>
    ///
    /// The callee has 6 int register arguments on x64 linux. 
    /// With 8 * 8 (64) bytes stack size
    ///
    /// Return 100 is a pass.
    /// Return 104 is a failure.
    ///
    /// </remarks>
    public static int DoNotFastTailCallSimple(float one,
                                              float two,
                                              float three,
                                              float four,
                                              float five,
                                              float six,
                                              float seven,
                                              float eight,
                                              int first,
                                              int second,
                                              int third,
                                              int fourth,
                                              int fifth,
                                              int sixth)
    {
        if (one % 2 == 0)
        {
            return DoNotFastTailCallHelper((int) two,
                                           (int) one,
                                           (int) three,
                                           (int) four,
                                           (int) five,
                                           (int) six,
                                           (int) seven,
                                           (int) eight,
                                           first,
                                           second,
                                           third,
                                           fourth,
                                           fifth,
                                           sixth); // Cannot fast tail call
        }
        else
        {
            return DoNotFastTailCallHelper((int) one,
                                           (int) two,
                                           (int) three,
                                           (int) four,
                                           (int) five,
                                           (int) six,
                                           (int) seven,
                                           (int) eight,
                                           first,
                                           second,
                                           third,
                                           fourth,
                                           fifth,
                                           sixth); // Cannot fast tail call
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    // HFAs
    ////////////////////////////////////////////////////////////////////////////

    public struct HFASize16
    {
        public double a;
        public double b;

        public HFASize16(double a, double b)
        {
            this.a = a;
            this.b = b;
        }
    }

    public struct HFASize24
    {
        public double a;
        public double b;
        public double c;

        public HFASize24(double a, double b, double c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }
    }

    public struct HFASize32
    {
        public double a;
        public double b;
        public double c;
        public double d;

        public HFASize32(double a, double b, double c, double d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }
    }

    /// <summary>
    /// Possible to fast tail call only on arm64. See CallerSimpleHFACase for
    /// more information.
    /// </summary>
    public static int CalleeSimpleHFACase(double one,
                                          double two,
                                          double three,
                                          double four,
                                          double five,
                                          double six,
                                          double seven,
                                          double eight)
    {
        int count = 0;
        for (double i = 0; i < one; ++i)
        {
            if (i % 2 == 0)
            {
                ++count;
            }
        }

        if (count == 1)
        {
            return 100;
        }

        else
        {
            return 107;
        }
    }

    /// <summary>
    /// Possible to fast tail call only on arm64
    /// </summary>
    /// <remarks>
    ///
    /// This test case is really only interesting on arm64
    ///
    /// Arm64:
    /// caller has 8 register arguments and no stack space
    /// callee has 8 register arguments and no stack space
    ///
    /// x64 Linux:
    /// caller has 4 register arguments and 64 bytes of stack space
    /// callee has 8 register arguments
    ///
    /// Arm64 and linux x64 can both fast tail call
    ///
    /// Return 100 is a pass.
    /// Return 107 is a failure.
    ///
    /// </remarks>
    public static int CallerSimpleHFACase(HFASize32 s1,
                                          double one,
                                          double two,
                                          double three,
                                          double four)
    {
        if (one % 2 == 0)
        {
            double a = one * 100;
            double b = one + 1100;
            return CalleeSimpleHFACase(one, 
                                       two,
                                       three,
                                       four,
                                       a,
                                       b,
                                       one,
                                       two);
        }
        else
        {
            double b = one + 1599;
            double a = one + 16;
            return CalleeSimpleHFACase(two, 
                                       one,
                                       three,
                                       four,
                                       a,
                                       b,
                                       two,
                                       one);
        }
    }

    /// <summary>
    /// Possible to fast tail call only on arm64. See CallerHFACaseWithStack
    /// for more information.
    /// </summary>
    public static int CalleeHFAStackSpace(double one,
                                          double two,
                                          double three,
                                          double four,
                                          double five,
                                          double six,
                                          double seven,
                                          double eight,
                                          double nine,
                                          double ten)
    {
        int count = 0;
        for (double i = 0; i < one; ++i)
        {
            if (i % 2 == 0)
            {
                ++count;
            }
        }

        if (count == 1)
        {
            return 100;
        }

        else
        {
            return 108;
        }
    }

    /// <summary>
    /// Possible to fast tail call only on arm64
    /// </summary>
    /// <remarks>
    ///
    /// This test case is really only interesting on arm64
    ///
    /// Arm64:
    /// caller has 8 register arguments and 32 bytes of stack space
    /// callee has 8 register arguments and 16 bytes of stack space
    ///
    /// x64 linix:
    /// caller has 8 register arguments and 32 bytes of stack space
    /// callee has 8 register arguments and 16 bytes of stack space
    ///
    /// Arm64 can fast tail call while x64 linux will not. 
    /// Note that this is due to a bug in LowerFastTailCall that assumes 
    /// nCallerArgs <= nCalleeArgs
    ///
    /// Return 100 is a pass.
    /// Return 108 is a failure.
    ///
    /// </remarks>
    public static int CallerHFACaseWithStack(double one,
                                             double two,
                                             double three,
                                             double four,
                                             double five,
                                             double six,
                                             double seven,
                                             double eight,
                                             HFASize32 s1)
    {
        if (one % 2 == 0)
        {
            double a = one * 100;
            double b = one + 1100;
            return CalleeHFAStackSpace(one, 
                                       two,
                                       three,
                                       four,
                                       a,
                                       b,
                                       five,
                                       six,
                                       seven,
                                       eight);
        }
        else
        {
            double b = one + 1599;
            double a = one + 16;
            return CalleeHFAStackSpace(one, 
                                       two,
                                       three,
                                       four,
                                       a,
                                       b,
                                       six,
                                       five,
                                       seven,
                                       eight);
        }
    }

    /// <summary>
    /// Possible to fast tail call only on arm64. See CallerHFACaseCalleeOnly
    /// for more information.
    /// </summary>
    public static int CalleeWithHFA(double one,
                                    double two,
                                    double three,
                                    double four,
                                    HFASize32 s1)
    {
        int count = 0;
        for (double i = 0; i < one; ++i)
        {
            if (i % 2 == 0)
            {
                ++count;
            }
        }

        if (count == 1)
        {
            return 100;
        }

        else
        {
            return 109;
        }
    }

    /// <summary>
    /// Possible to fast tail call only on arm64
    /// </summary>
    /// <remarks>
    ///
    /// This test case is really only interesting on arm64
    ///
    /// Arm64:
    /// caller has 8 register arguments
    /// callee has 8 register arguments
    ///
    /// x64 Linux:
    /// caller has 8 register arguments
    /// callee has 4 register arguments and 32 bytes of stack space
    ///
    /// Arm64 can fast tail call while x64 linux cannot
    ///
    /// Return 100 is a pass.
    /// Return 109 is a failure.
    ///
    /// </remarks>
    public static int CallerHFACaseCalleeOnly(double one,
                                              double two,
                                              double three,
                                              double four,
                                              double five,
                                              double six,
                                              double seven,
                                              double eight)
    {
        if (one % 2 == 0)
        {
            double a = one * 100;
            double b = one + 1100;
            return CalleeWithHFA(one, 
                                 a,
                                 b,
                                 four,
                                 new HFASize32(a, b, five, six));
        }
        else
        {
            double b = one + 1599;
            double a = one + 16;
            return CalleeWithHFA(one, 
                                 b,
                                 a,
                                 four,
                                 new HFASize32(a, b, five, six));
        }
    }

    /// <summary>
    /// Possible to fast tail call on all targets. See 
    /// CallerHFaCaseCalleeStackArgs for info.
    /// </summary>
    /// <remarks>
    public static int CalleeWithStackHFA(double one,
                                         double two,
                                         double three,
                                         double four,
                                         double five,
                                         double six,
                                         double seven,
                                         double eight,
                                         HFASize16 s1)
    {
        int count = 0;
        for (double i = 0; i < one; ++i)
        {
            if (i % 2 == 0)
            {
                ++count;
            }
        }

        if (count == 1)
        {
            return 100;
        }

        else
        {
            return 110;
        }
    }

    /// <summary>
    /// Possible to fast tail call on all targets
    /// </summary>
    /// <remarks>
    ///
    /// This test case is really only interesting on arm64 and Linux x64
    /// because the decision to fast tail call will be reported as false.
    ///
    /// On arm64 this is because callee has stack args and has an hfa arg.
    /// While on x64 Linux this is because the callee has stack args and has
    /// a special 16 byte struct.
    ///
    /// Arm64:
    /// caller has 8 register arguments and 16 bytes of stack space
    /// callee has 8 register arguments and 16 bytes of stack space
    ///
    /// x64 Linux:
    /// caller has 8 register arguments and 16 bytes of stack space
    /// callee has 8 register arguments and 16 bytes of stack space
    ///
    /// Arm64 can fast tail call while x64 linux cannot. Note that this is
    /// due to an implementation limitation. fgCanFastTail call relies on
    /// fgMorphArgs, but fgMorphArgs relies on fgCanfast tail call. Therefore,
    /// fgCanFastTailCall will not fast tail call if there is a 16 byte
    /// struct and stack usage.
    ///
    /// Return 100 is a pass.
    /// Return 110 is a failure.
    ///
    /// </remarks>
    public static int CallerHFaCaseCalleeStackArgs(double one,
                                                   double two,
                                                   double three,
                                                   double four,
                                                   double five,
                                                   double six,
                                                   double seven,
                                                   double eight,
                                                   double nine,
                                                   double ten)
    {
        if (one % 2 == 0)
        {
            double a = one * 100;
            double b = one + 1100;
            return CalleeWithStackHFA(one, 
                                      a,
                                      b,
                                      four,
                                      five,
                                      six,
                                      seven,
                                      eight,
                                      new HFASize16(a, b));
        }
        else
        {
            double b = one + 1599;
            double a = one + 16;
            return CalleeWithStackHFA(one, 
                                      a,
                                      b,
                                      four,
                                      five,
                                      six,
                                      seven,
                                      eight,
                                      new HFASize16(a, b));
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    // Stack Based args.
    ////////////////////////////////////////////////////////////////////////////

    public struct StructSizeOneNotExplicit
    {
        public byte a;

        public StructSizeOneNotExplicit(byte a)
        {
            this.a = a;
        }
    }

    public struct StructSizeTwoNotExplicit
    {
        public byte a;
        public byte b;

        public StructSizeTwoNotExplicit(byte a, byte b)
        {
            this.a = a;
            this.b = b;
        }
    }

    public struct StructSizeThreeNotExplicit
    {
        public byte a;
        public byte b;
        public byte c;

        public StructSizeThreeNotExplicit(byte a, byte b, byte c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }
    }

    public struct StructSizeFourNotExplicit
    {
        public int a;

        public StructSizeFourNotExplicit(int a)
        {
            this.a = a;
        }
    }

    public struct StructSizeFiveNotExplicit
    {
        public int a;
        public byte b;

        public StructSizeFiveNotExplicit(int a, byte b)
        {
            this.a = a;
            this.b = b;
        }
    }

    public struct StructSizeSixNotExplicit
    {
        public int a;
        public byte b;
        public byte c;

        public StructSizeSixNotExplicit(int a, byte b, byte c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }
    }

    public struct StructSizeSevenNotExplicit
    {
        public int a;
        public byte b;
        public byte c;
        public byte d;

        public StructSizeSevenNotExplicit(int a, byte b, byte c, byte d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }
    }

    public struct StructSizeEightNotExplicit
    {
        public long a;

        public StructSizeEightNotExplicit(long a)
        {
            this.a = a;
        }
    }

    public struct StructSizeEightIntNotExplicit
    {
        public int a;
        public int b;

        public StructSizeEightIntNotExplicit(int a, int b)
        {
            this.a = a;
            this.b = b;
        }
    }

    public struct StructSizeSixteenNotExplicit
    {
        public long a;
        public long b;

        public StructSizeSixteenNotExplicit(long a, long b)
        {
            this.a = a;
            this.b = b;
        }
    }

    public struct StructSize24NotExplicit
    {
        public long a;
        public long b;
        public long c;

        public StructSize24NotExplicit(long a, long b, long c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }
    }

    public struct StructSize48Nested
    {
        public StructSize24NotExplicit a;
        public StructSize24NotExplicit b;

        public StructSize48Nested(long a, long b, long c, long d, long e, long f)
        {
            this.a = new StructSize24NotExplicit(a, b, c);
            this.b = new StructSize24NotExplicit(d, e, f);
        }
    }

    /// <summary>
    /// Possible to fast tail call. See CallerGithubIssue12468 for more info.
    /// </summary>
    public static int CalleeGithubIssue12468(int one,
                                             int two,
                                             int three,
                                             int four,
                                             int five,
                                             int six,
                                             int seven,
                                             int eight,
                                             StructSizeEightNotExplicit s1,
                                             StructSizeEightNotExplicit s2)
    {
        int count = 0;
        for (int i = 0; i < s1.a; ++i)
        {
            if (i % 10 == 0)
            {
                ++count;
            }
        }

        if (count == 160)
        {
            return 100;
        }

        else
        {
            return 106;
        }
    }

    /// <summary>
    /// Possible to fast tail call
    /// </summary>
    /// <remarks>
    ///
    /// Caller has 6 register arguments and 1 stack argument (size 16)
    /// Callee has 6 register arguments and 2 stack arguments (size 16)
    ///
    /// It is possible to fast tail call but will not due to a bug in
    /// LowerFastTailCall which assumes nCallerArgs <= nCalleeArgs
    ///
    ///
    /// Return 100 is a pass.
    /// Return 106 is a failure.
    ///
    /// </remarks>
    public static int CallerGithubIssue12468(int one,
                                             int two,
                                             int three,
                                             int four,
                                             int five,
                                             int six,
                                             int seven,
                                             int eight,
                                             StructSizeSixteenNotExplicit s1)
    {
        if (one % 2 == 0)
        {
            long a = one * 100;
            long b = one + 1100;
            return CalleeGithubIssue12468(two, 
                                          one,
                                          three,
                                          four,
                                          five,
                                          six,
                                          seven,
                                          eight,
                                          new StructSizeEightNotExplicit(a), 
                                          new StructSizeEightNotExplicit(b));
        }
        else
        {
            long b = one + 1599;
            long a = one + 16;
            return CalleeGithubIssue12468(one, 
                                          two,
                                          three,
                                          four,
                                          five,
                                          six,
                                          seven,
                                          eight,
                                          new StructSizeEightNotExplicit(b), 
                                          new StructSizeEightNotExplicit(a));
        }
    }
    
    [StructLayout(LayoutKind.Explicit, Size=8, CharSet=CharSet.Ansi)]
    public struct StructSizeThirtyTwo
    {
        [FieldOffset(0)]  public long a;
        [FieldOffset(8)]  public long b;
        [FieldOffset(16)] public long c;
        [FieldOffset(24)] public long d;

        public StructSizeThirtyTwo(long a, long b, long c, long d)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }
    };

    [StructLayout(LayoutKind.Explicit, Size=8, CharSet=CharSet.Ansi)]
    public struct StructSizeTwentyFour
    {
        [FieldOffset(0)] public long a;
        [FieldOffset(8)] public long b;
        [FieldOffset(16)] public long c;

        public StructSizeTwentyFour(int a, int b, int c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }
    }

    /// <summary>
    /// Decision to fast tail call. See StackBasedCaller for more
    /// information.
    /// </summary>
    public static int StackBasedCallee(int a, int b, StructSizeThirtyTwo sstt)
    {
        int count = 0;
        for (int i = 0; i < sstt.a; ++i)
        {
            if (i % 10 == 0)
            {
                ++count;
            }
        }

        if (count == 160)
        {
            return 100;
        }

        else
        {
            return 105;
        }
    }

    /// <summary>
    /// Decision to fast tail call
    /// </summary>
    /// <remarks>
    ///
    /// On x64 linux this will not fast tail call.
    ///
    /// The caller has one stack argument of size 24
    /// The callee has one stack argument of size 32
    ///
    /// On Arm64 this will fast tail call
    ///
    /// Both caller and callee have two register args.
    ///
    /// Return 100 is a pass.
    /// Return 105 is a failure.
    ///
    /// </remarks>
    public static int StackBasedCaller(int i, StructSizeTwentyFour sstf)
    {
        if (i % 2 == 0)
        {
            int a = i * 100;
            int b = i + 1100;
            return StackBasedCallee(a, b, new StructSizeThirtyTwo(a, b, b, a));
        }
        else
        {
            int b = i + 829;
            int a = i + 16;
            return StackBasedCallee(b, a, new StructSizeThirtyTwo(b, a, a, b));
        }
    }

    /// <summary>
    /// Decision to fast tail call. See DoubleCountRetBuffCaller for more
    /// information.
    /// </summary>
    public static StructSizeThirtyTwo DoubleCountRetBuffCallee(StructSizeEightIntNotExplicit sstf,
                                                               StructSizeEightIntNotExplicit sstf2,
                                                               StructSizeEightIntNotExplicit sstf3,
                                                               StructSizeEightIntNotExplicit sstf4,
                                                               StructSizeEightIntNotExplicit sstf5)
    {
        int a = sstf.a;
        int b = sstf.b;

        StructSizeThirtyTwo retVal = new StructSizeThirtyTwo(b, a, a, b);

        int count = 0;
        for (int i = 0; i < b; ++i)
        {
            if (i % 2 == 0)
            {
                ++count;
            }
        }

        return retVal;
    }

    /// <summary>
    /// Decision to fast tail call. See DoubleCountRetBuffCaller for more
    /// information.
    /// </summary>
    public static StructSizeThirtyTwo DoubleCountRetBuffCallerWrapper(int a, int b)
    {
        if (a % 2 == 0)
        {
            a = 1;
            return DoubleCountRetBuffCallee(new StructSizeEightIntNotExplicit(a, a), 
                                            new StructSizeEightIntNotExplicit(a, a), 
                                            new StructSizeEightIntNotExplicit(a, a), 
                                            new StructSizeEightIntNotExplicit(a, a),
                                            new StructSizeEightIntNotExplicit(a, a));
        }
        else
        {
            b = b + 1;
            return DoubleCountRetBuffCallee(new StructSizeEightIntNotExplicit(b, b), 
                                            new StructSizeEightIntNotExplicit(b, b), 
                                            new StructSizeEightIntNotExplicit(b, b), 
                                            new StructSizeEightIntNotExplicit(b, b),
                                            new StructSizeEightIntNotExplicit(b, b));
        }
    }

    /// <summary>
    /// Decision to fast tail call
    /// </summary>
    /// <remarks>
    ///
    /// On x64 linux this will fast tail call.
    ///
    /// The caller uses 3 integer registers (2 args, one ret buf)
    /// The callee uses 6 integer registers (5 args, one ret buf)
    ///
    /// Return 100 is a pass.
    /// Return 112 is a failure.
    ///
    /// </remarks>
    public static int DoubleCountRetBuffCaller(int i)
    {
        if (i % 2 == 0)
        {
            StructSizeThirtyTwo retVal = DoubleCountRetBuffCallerWrapper(4, 2);
            
            if (retVal.b == 6.0)
            {
                return 100;
            }
            else
            {
                return 112;
            }
        }
        else
        {
            StructSizeThirtyTwo retVal = DoubleCountRetBuffCallerWrapper(3, 1);
            
            if (retVal.b == 2.0)
            {
                return 100;
            }
            else
            {
                return 112;
            }
        }
    }

    /// <summary>
    /// Decision to fast tail call
    /// </summary>
    ///
    /// On x64 linux this will fast tail call.
    ///
    /// The caller uses 2 integer registers 32 bytes of stack (3 args)
    /// The callee uses 3 integer registers, 0 bytes of stack (3 args)
    ///
    /// Return 100 is a pass.
    /// Return 113 is a failure.
    ///
    /// </remarks>
    public static int Struct32Caller(StructSizeThirtyTwo one, long two, long three)
    {
        if (two % 2 == 1)
        {
            return Struct32Callee(two, two, three);
        }
        else
        {
            return Struct32Callee(three, two, three);
        }
    }

    /// <summary>
    /// Decision to fast tail call
    /// </summary>
    ///
    /// On x64 linux this will fast tail call.
    ///
    /// The caller uses 2 integer registers 32 bytes of stack (3 args)
    /// The callee uses 3 integer registers, 0 bytes of stack (3 args)
    ///
    /// Return 100 is a pass.
    /// Return 113 is a failure.
    ///
    /// </remarks>
    public static int Struct32Callee(long one, long two, long three)
    {
        int count = 0;
        for (int i = 0; i < three * 100; ++i)
        {
            if (i % 10 == 0)
            {
                ++count;
            }
        }

        if (count == 30)
        {
            return 84;
        }
        else
        {
            return 85;
        }
    }

    /// <summary>
    /// Decision to fast tail call
    /// </summary>
    ///
    /// On x64 linux this will fast tail call.
    ///
    /// The caller uses 2 integer registers 32 bytes of stack (3 args)
    /// The callee uses 3 integer registers, 0 bytes of stack (3 args)
    ///
    /// Return 100 is a pass.
    /// Return 113 is a failure.
    ///
    /// </remarks>
    public static int Struct32CallerWrapper()
    {
        int ret = Struct32Caller(new StructSizeThirtyTwo(1, 2, 3, 4), 2, 3);

        if (ret != 84)
        {
            return 113;
        }

        return 100;
    }

    /// <summary>
    /// Decision to not fast tail call
    /// </summary>
    ///
    /// On x64 linux this will not fast tail call.
    ///
    /// The caller uses 6 integer registers 56 bytes of stack (10 args)
    /// The callee uses 6 integer registers, 32 bytes of stack (10 args)
    ///
    /// Return 100 is a pass.
    /// Return 114 is a failure.
    ///
    /// </remarks>
    public static int Struct32CallerCalleeHasStackSpace(StructSizeThirtyTwo one,    // stack slot 1, 2, 3, 4
                                                        long two, 
                                                        long three,
                                                        long four,
                                                        long five,
                                                        long six,
                                                        long seven,
                                                        long eight,                 // stack slot 6
                                                        long nine,                  // stack slot 7
                                                        long ten)                   // stack slot 8
    {
        int count = 0;
        for (int i = 0; i < two * 100; ++i)
        {
            if (i % 10 == 0)
            {
                ++count;
            }
        }

        if (count == 20)
        {
            return Struct32CalleeWithStack(one.a, 
                                           one.b, 
                                           one.c,
                                           one.d,
                                           two,
                                           three,
                                           four,        // stack slot 1
                                           five,        // stack slot 2
                                           six,         // stack slot 3
                                           seven);      // stack slot 4
        }
        else
        {
            return Struct32CalleeWithStack(one.a, 
                                           one.b, 
                                           one.c,
                                           one.d,
                                           two,
                                           three,
                                           four,        // stack slot 1
                                           five,        // stack slot 2
                                           six,         // stack slot 3
                                           seven);      // stack slot 4
        }
    }

    /// <summary>
    /// Decision to not fast tail call
    /// </summary>
    ///
    /// On x64 linux this will not fast tail call.
    ///
    /// The caller uses 6 integer registers 56 bytes of stack (3 args)
    /// The callee uses 6 integer registers, 32 bytes of stack (3 args)
    ///
    /// Return 100 is a pass.
    /// Return 113 is a failure.
    ///
    /// </remarks>
    public static int Struct32CalleeWithStack(long one, 
                                              long two, 
                                              long three,
                                              long four,
                                              long five,
                                              long six,
                                              long seven,
                                              long eight,
                                              long nine,
                                              long ten)
    {
        int count = 0;
        for (int i = 0; i < one * 100; ++i)
        {
            if (i % 10 == 0)
            {
                ++count;
            }
        }

        if (count == 10)
        {
            return 84;
        }
        else
        {
            return 85;
        }
    }

    /// <summary>
    /// Decision to not fast tail call
    /// </summary>
    ///
    /// On x64 linux this will not fast tail call.
    ///
    /// The caller uses 6 integer registers 56 bytes of stack (3 args)
    /// The callee uses 6 integer registers, 32 bytes of stack (3 args)
    ///
    /// Return 100 is a pass.
    /// Return 114 is a failure.
    ///
    /// </remarks>
    public static int Struct32CallerWrapperCalleeHasStack(int two)
    {
        int ret = Struct32CallerCalleeHasStackSpace(new StructSizeThirtyTwo(1, 2, 3, 4),
                                                    5, 
                                                    6,
                                                    7,
                                                    8,
                                                    9,
                                                    10,
                                                    11,
                                                    12,
                                                    13);

        if (ret != 84)
        {
            return 114;
        }

        return 100;
    }

    /// <summary>
    /// Decision to fast tail call. See CallerEnregisterableAmd64WindowsStructs8Bytes for more
    /// information.
    /// </summary>
    public static int CalleeEnregisterableAmd64WindowsStructs8Bytes(StructSizeEightNotExplicit eightByteStruct)
    {
        long a = eightByteStruct.a;

        // Force this to not be inlined
        int count = 0;
        for (int i = 0; i < a; ++i)
        {
            if (i % 2 == 0)
            {
                ++count;
            }
        }

        if (count == 1000000)
        {
            a = count;
        }

        if (count == 1)
        {
            a = 100;
        }
        else
        {
            a = 115;
        }

        return (int)a;
    }

    /// <summary>
    /// Windows x64 tail call tests
    /// </summary>
    /// <remarks>
    ///
    /// All targets will fast tail call
    ///
    /// The caller uses 2 integer registers (2 args)
    /// The callee uses 1 integer registers (1 args)
    ///
    /// Return 100 is a pass.
    /// Return 115 is a failure.
    ///
    /// </remarks>
    public static int CallerEnregisterableAmd64WindowsStructs8Bytes(int a, int b)
    {
        if (a % 2 == 0)
        {
            return CalleeEnregisterableAmd64WindowsStructs8Bytes(new StructSizeEightNotExplicit(a));
        }
        else
        {
            return CalleeEnregisterableAmd64WindowsStructs8Bytes(new StructSizeEightNotExplicit(b));
        }
    }

    /// <summary>
    /// Decision to fast tail call. See CallerAmd64WindowsStructs7Bytes for more
    /// information.
    /// </summary>
    public static int CalleeAmd64WindowsStructs7Bytes(StructSizeSevenNotExplicit sevenByteStruct)
    {
        int a = sevenByteStruct.a;

        // Force this to not be inlined
        int count = 0;
        for (int i = 0; i < a; ++i)
        {
            if (i % 2 == 0)
            {
                ++count;
            }
        }

        if (count == 1000000)
        {
            a = count;
        }

        if (count == 1)
        {
            a = 100;
        }
        else
        {
            a = 116;
        }

        return (int)a;
    }

    /// <summary>
    /// Windows x64 tail call tests
    /// </summary>
    /// <remarks>
    ///
    /// All targets will fast tail call
    ///
    /// The caller uses 2 integer registers (2 args)
    /// The callee uses 1 integer registers (1 args)
    ///
    /// Return 100 is a pass.
    /// Return 116 is a failure.
    ///
    /// </remarks>
    public static int CallerAmd64WindowsStructs7Bytes(int a, int b)
    {
        if (a % 2 == 0)
        {
            return CalleeAmd64WindowsStructs7Bytes(new StructSizeSevenNotExplicit(a, 1, 2, 3));
        }
        else
        {
            return CalleeAmd64WindowsStructs7Bytes(new StructSizeSevenNotExplicit(b, 1, 2, 3));
        }
    }

    /// <summary>
    /// Decision to fast tail call. See CallerAmd64WindowsStructs6Bytes for more
    /// information.
    /// </summary>
    public static int CalleeAmd64WindowsStructs6Bytes(StructSizeSixNotExplicit sixByteStruct)
    {
        int a = sixByteStruct.a;

        // Force this to not be inlined
        int count = 0;
        for (int i = 0; i < a; ++i)
        {
            if (i % 2 == 0)
            {
                ++count;
            }
        }

        if (count == 1000000)
        {
            a = count;
        }

        if (count == 1)
        {
            a = 100;
        }
        else
        {
            a = 117;
        }

        return (int)a;
    }

    /// <summary>
    /// Windows x64 tail call tests
    /// </summary>
    /// <remarks>
    ///
    /// All targets will fast tail call
    ///
    /// The caller uses 2 integer registers (2 args)
    /// The callee uses 1 integer registers (1 args)
    ///
    /// Return 100 is a pass.
    /// Return 117 is a failure.
    ///
    /// </remarks>
    public static int CallerAmd64WindowsStructs6Bytes(int a, int b)
    {
        if (a % 2 == 0)
        {
            return CalleeAmd64WindowsStructs6Bytes(new StructSizeSixNotExplicit(a, 1, 2));
        }
        else
        {
            return CalleeAmd64WindowsStructs6Bytes(new StructSizeSixNotExplicit(b, 1, 2));
        }
    }

    /// <summary>
    /// Decision to fast tail call. See CallerAmd64WindowsStructs5Bytes for more
    /// information.
    /// </summary>
    public static int CalleeAmd64WindowsStructs5Bytes(StructSizeFiveNotExplicit fiveByteStruct)
    {
        int a = fiveByteStruct.a;

        // Force this to not be inlined
        int count = 0;
        for (int i = 0; i < a; ++i)
        {
            if (i % 2 == 0)
            {
                ++count;
            }
        }

        if (count == 1000000)
        {
            a = count;
        }

        if (count == 1)
        {
            a = 100;
        }
        else
        {
            a = 118;
        }

        return (int)a;
    }

    /// <summary>
    /// Windows x64 tail call tests
    /// </summary>
    /// <remarks>
    ///
    /// All targets will fast tail call
    ///
    /// The caller uses 2 integer registers (2 args)
    /// The callee uses 1 integer registers (1 args)
    ///
    /// Return 100 is a pass.
    /// Return 118 is a failure.
    ///
    /// </remarks>
    public static int CallerAmd64WindowsStructs5Bytes(int a, int b)
    {
        if (a % 2 == 0)
        {
            return CalleeAmd64WindowsStructs5Bytes(new StructSizeFiveNotExplicit(a, 1));
        }
        else
        {
            return CalleeAmd64WindowsStructs5Bytes(new StructSizeFiveNotExplicit(b, 1));
        }
    }

    /// <summary>
    /// Decision to fast tail call. See CallerAmd64WindowsStructs4Bytes for more
    /// information.
    /// </summary>
    public static int CalleeAmd64WindowsStructs4Bytes(StructSizeFourNotExplicit fourByteStruct)
    {
        int a = fourByteStruct.a;

        // Force this to not be inlined
        int count = 0;
        for (int i = 0; i < a; ++i)
        {
            if (i % 2 == 0)
            {
                ++count;
            }
        }

        if (count == 1000000)
        {
            a = count;
        }

        if (count == 1)
        {
            a = 100;
        }
        else
        {
            a = 119;
        }

        return (int)a;
    }

    /// <summary>
    /// Windows x64 tail call tests
    /// </summary>
    /// <remarks>
    ///
    /// All targets will fast tail call
    ///
    /// The caller uses 2 integer registers (2 args)
    /// The callee uses 1 integer registers (1 args)
    ///
    /// Return 100 is a pass.
    /// Return 119 is a failure.
    ///
    /// </remarks>
    public static int CallerAmd64WindowsStructs4Bytes(int a, int b)
    {
        if (a % 2 == 0)
        {
            return CalleeAmd64WindowsStructs4Bytes(new StructSizeFourNotExplicit(a));
        }
        else
        {
            return CalleeAmd64WindowsStructs4Bytes(new StructSizeFourNotExplicit(a));
        }
    }

    /// <summary>
    /// Decision to fast tail call. See CallerAmd64WindowsStructs3Bytes for more
    /// information.
    /// </summary>
    public static int CalleeAmd64WindowsStructs3Bytes(StructSizeThreeNotExplicit threeByteStruct)
    {
        int a = threeByteStruct.a;

        // Force this to not be inlined
        int count = 0;
        for (int i = 0; i < a; ++i)
        {
            if (i % 2 == 0)
            {
                ++count;
            }
        }

        if (count == 1000000)
        {
            a = count;
        }

        if (count == 1)
        {
            a = 100;
        }
        else
        {
            a = 120;
        }

        return (int)a;
    }

    /// <summary>
    /// Windows x64 tail call tests
    /// </summary>
    /// <remarks>
    ///
    /// x64 windows will not fast tail call because the struct is passed
    /// byref.
    ///
    /// The caller uses 2 integer registers (2 args)
    /// The callee uses 1 integer registers (1 args)
    ///
    /// Return 100 is a pass.
    /// Return 120 is a failure.
    ///
    /// </remarks>
    public static int CallerAmd64WindowsStructs3Bytes(byte a, byte b)
    {
        if (a % 2 == 0)
        {
            return CalleeAmd64WindowsStructs3Bytes(new StructSizeThreeNotExplicit(a, a, a));
        }
        else
        {
            return CalleeAmd64WindowsStructs3Bytes(new StructSizeThreeNotExplicit(b, b, b));
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    // Main
    ////////////////////////////////////////////////////////////////////////////

    [Fact]
    public static int TestEntryPoint()
    {
        return Tester(1);
    }
}
