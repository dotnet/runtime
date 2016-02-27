// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

namespace Test
{
    public static class IntValues
    {
        public static readonly int ForArg0 = (11100 + 0);
    }


    public static class LongValues
    {
        public static readonly long ForArg1 = (3333333300L + 1L);
    }


    public static class FloatValues
    {
        public static readonly float ForArg2 = (4400.0f + 2.0f);
    }


    public static class DoubleValues
    {
        public static readonly double ForArg3 = (88888800.0d + 3.0d);
    }


    public static class StructValues
    {
        public static readonly BasicStruct ForArg4 = new BasicStruct(
            /* Field1 = */ (1000 + 4),
            /* Field2 = */ (2000 + 4),
            /* Field3 = */ (3000 + 4),
            /* Field4 = */ (4000 + 4)
        );
    }


    public static class StringValues
    {
        public static readonly string ForArg5 = "StringValue5";
    }


    public static class CalleeSide
    {
        public static void Pass6Args_Maxstack_2(
            int arg0,
            long arg1,
            float arg2,
            double arg3,
            BasicStruct arg4,
            string arg5
            )
        {
            Console.WriteLine("        Executing C# target for: \"Pass6Args_Maxstack_2\"");
            Support.VerifyInt(arg0, IntValues.ForArg0);
            Support.VerifyLong(arg1, LongValues.ForArg1);
            Support.VerifyFloat(arg2, FloatValues.ForArg2);
            Support.VerifyDouble(arg3, DoubleValues.ForArg3);
            Support.VerifyStruct(arg4, StructValues.ForArg4);
            Support.VerifyString(arg5, StringValues.ForArg5);
            CallerSide.RecordExecutedCaller("Pass6Args_Maxstack_2");
        }
        public static void Pass6Args_Maxstack_3(
            int arg0,
            long arg1,
            float arg2,
            double arg3,
            BasicStruct arg4,
            string arg5
            )
        {
            Console.WriteLine("        Executing C# target for: \"Pass6Args_Maxstack_3\"");
            Support.VerifyInt(arg0, IntValues.ForArg0);
            Support.VerifyLong(arg1, LongValues.ForArg1);
            Support.VerifyFloat(arg2, FloatValues.ForArg2);
            Support.VerifyDouble(arg3, DoubleValues.ForArg3);
            Support.VerifyStruct(arg4, StructValues.ForArg4);
            Support.VerifyString(arg5, StringValues.ForArg5);
            CallerSide.RecordExecutedCaller("Pass6Args_Maxstack_3");
        }
        public static void Pass6Args_Maxstack_4(
            int arg0,
            long arg1,
            float arg2,
            double arg3,
            BasicStruct arg4,
            string arg5
            )
        {
            Console.WriteLine("        Executing C# target for: \"Pass6Args_Maxstack_4\"");
            Support.VerifyInt(arg0, IntValues.ForArg0);
            Support.VerifyLong(arg1, LongValues.ForArg1);
            Support.VerifyFloat(arg2, FloatValues.ForArg2);
            Support.VerifyDouble(arg3, DoubleValues.ForArg3);
            Support.VerifyStruct(arg4, StructValues.ForArg4);
            Support.VerifyString(arg5, StringValues.ForArg5);
            CallerSide.RecordExecutedCaller("Pass6Args_Maxstack_4");
        }
        public static void Pass6Args_Maxstack_5(
            int arg0,
            long arg1,
            float arg2,
            double arg3,
            BasicStruct arg4,
            string arg5
            )
        {
            Console.WriteLine("        Executing C# target for: \"Pass6Args_Maxstack_5\"");
            Support.VerifyInt(arg0, IntValues.ForArg0);
            Support.VerifyLong(arg1, LongValues.ForArg1);
            Support.VerifyFloat(arg2, FloatValues.ForArg2);
            Support.VerifyDouble(arg3, DoubleValues.ForArg3);
            Support.VerifyStruct(arg4, StructValues.ForArg4);
            Support.VerifyString(arg5, StringValues.ForArg5);
            CallerSide.RecordExecutedCaller("Pass6Args_Maxstack_5");
        }
        public static void Pass6Args_Maxstack_6(
            int arg0,
            long arg1,
            float arg2,
            double arg3,
            BasicStruct arg4,
            string arg5
            )
        {
            Console.WriteLine("        Executing C# target for: \"Pass6Args_Maxstack_6\"");
            Support.VerifyInt(arg0, IntValues.ForArg0);
            Support.VerifyLong(arg1, LongValues.ForArg1);
            Support.VerifyFloat(arg2, FloatValues.ForArg2);
            Support.VerifyDouble(arg3, DoubleValues.ForArg3);
            Support.VerifyStruct(arg4, StructValues.ForArg4);
            Support.VerifyString(arg5, StringValues.ForArg5);
            CallerSide.RecordExecutedCaller("Pass6Args_Maxstack_6");
        }
        public static void Pass6Args_Maxstack_7(
            int arg0,
            long arg1,
            float arg2,
            double arg3,
            BasicStruct arg4,
            string arg5
            )
        {
            Console.WriteLine("        Executing C# target for: \"Pass6Args_Maxstack_7\"");
            Support.VerifyInt(arg0, IntValues.ForArg0);
            Support.VerifyLong(arg1, LongValues.ForArg1);
            Support.VerifyFloat(arg2, FloatValues.ForArg2);
            Support.VerifyDouble(arg3, DoubleValues.ForArg3);
            Support.VerifyStruct(arg4, StructValues.ForArg4);
            Support.VerifyString(arg5, StringValues.ForArg5);
            CallerSide.RecordExecutedCaller("Pass6Args_Maxstack_7");
        }
        public static void Pass6Args_Maxstack_8(
            int arg0,
            long arg1,
            float arg2,
            double arg3,
            BasicStruct arg4,
            string arg5
            )
        {
            Console.WriteLine("        Executing C# target for: \"Pass6Args_Maxstack_8\"");
            Support.VerifyInt(arg0, IntValues.ForArg0);
            Support.VerifyLong(arg1, LongValues.ForArg1);
            Support.VerifyFloat(arg2, FloatValues.ForArg2);
            Support.VerifyDouble(arg3, DoubleValues.ForArg3);
            Support.VerifyStruct(arg4, StructValues.ForArg4);
            Support.VerifyString(arg5, StringValues.ForArg5);
            CallerSide.RecordExecutedCaller("Pass6Args_Maxstack_8");
        }
        public static void Pass6Args_Maxstack_9(
            int arg0,
            long arg1,
            float arg2,
            double arg3,
            BasicStruct arg4,
            string arg5
            )
        {
            Console.WriteLine("        Executing C# target for: \"Pass6Args_Maxstack_9\"");
            Support.VerifyInt(arg0, IntValues.ForArg0);
            Support.VerifyLong(arg1, LongValues.ForArg1);
            Support.VerifyFloat(arg2, FloatValues.ForArg2);
            Support.VerifyDouble(arg3, DoubleValues.ForArg3);
            Support.VerifyStruct(arg4, StructValues.ForArg4);
            Support.VerifyString(arg5, StringValues.ForArg5);
            CallerSide.RecordExecutedCaller("Pass6Args_Maxstack_9");
        }
        public static void Pass6Args_Maxstack_10(
            int arg0,
            long arg1,
            float arg2,
            double arg3,
            BasicStruct arg4,
            string arg5
            )
        {
            Console.WriteLine("        Executing C# target for: \"Pass6Args_Maxstack_10\"");
            Support.VerifyInt(arg0, IntValues.ForArg0);
            Support.VerifyLong(arg1, LongValues.ForArg1);
            Support.VerifyFloat(arg2, FloatValues.ForArg2);
            Support.VerifyDouble(arg3, DoubleValues.ForArg3);
            Support.VerifyStruct(arg4, StructValues.ForArg4);
            Support.VerifyString(arg5, StringValues.ForArg5);
            CallerSide.RecordExecutedCaller("Pass6Args_Maxstack_10");
        }
    }
    public static partial class CallerSide
    {
        public static void MakeWrapperCallFor_Pass6Args_Maxstack_2()
        {
            int arg0;
            long arg1;
            float arg2;
            double arg3;
            BasicStruct arg4;
            string arg5;
            arg0       = IntValues.ForArg0;
            arg1       = LongValues.ForArg1;
            arg2       = FloatValues.ForArg2;
            arg3       = DoubleValues.ForArg3;
            arg4       = StructValues.ForArg4;
            arg5       = StringValues.ForArg5;
            CallerSide.PrepareForWrapperCall();


            ILJmpWrappers.Pass6Args_Maxstack_2(
                arg0,
                arg1,
                arg2,
                arg3,
                arg4,
                arg5
            );
            CallerSide.VerifyExecutedCaller("Pass6Args_Maxstack_2");
        }
        public static void MakeWrapperCallFor_Pass6Args_Maxstack_3()
        {
            int arg0;
            long arg1;
            float arg2;
            double arg3;
            BasicStruct arg4;
            string arg5;
            arg0       = IntValues.ForArg0;
            arg1       = LongValues.ForArg1;
            arg2       = FloatValues.ForArg2;
            arg3       = DoubleValues.ForArg3;
            arg4       = StructValues.ForArg4;
            arg5       = StringValues.ForArg5;
            CallerSide.PrepareForWrapperCall();


            ILJmpWrappers.Pass6Args_Maxstack_3(
                arg0,
                arg1,
                arg2,
                arg3,
                arg4,
                arg5
            );
            CallerSide.VerifyExecutedCaller("Pass6Args_Maxstack_3");
        }
        public static void MakeWrapperCallFor_Pass6Args_Maxstack_4()
        {
            int arg0;
            long arg1;
            float arg2;
            double arg3;
            BasicStruct arg4;
            string arg5;
            arg0       = IntValues.ForArg0;
            arg1       = LongValues.ForArg1;
            arg2       = FloatValues.ForArg2;
            arg3       = DoubleValues.ForArg3;
            arg4       = StructValues.ForArg4;
            arg5       = StringValues.ForArg5;
            CallerSide.PrepareForWrapperCall();


            ILJmpWrappers.Pass6Args_Maxstack_4(
                arg0,
                arg1,
                arg2,
                arg3,
                arg4,
                arg5
            );
            CallerSide.VerifyExecutedCaller("Pass6Args_Maxstack_4");
        }
        public static void MakeWrapperCallFor_Pass6Args_Maxstack_5()
        {
            int arg0;
            long arg1;
            float arg2;
            double arg3;
            BasicStruct arg4;
            string arg5;
            arg0       = IntValues.ForArg0;
            arg1       = LongValues.ForArg1;
            arg2       = FloatValues.ForArg2;
            arg3       = DoubleValues.ForArg3;
            arg4       = StructValues.ForArg4;
            arg5       = StringValues.ForArg5;
            CallerSide.PrepareForWrapperCall();


            ILJmpWrappers.Pass6Args_Maxstack_5(
                arg0,
                arg1,
                arg2,
                arg3,
                arg4,
                arg5
            );
            CallerSide.VerifyExecutedCaller("Pass6Args_Maxstack_5");
        }
        public static void MakeWrapperCallFor_Pass6Args_Maxstack_6()
        {
            int arg0;
            long arg1;
            float arg2;
            double arg3;
            BasicStruct arg4;
            string arg5;
            arg0       = IntValues.ForArg0;
            arg1       = LongValues.ForArg1;
            arg2       = FloatValues.ForArg2;
            arg3       = DoubleValues.ForArg3;
            arg4       = StructValues.ForArg4;
            arg5       = StringValues.ForArg5;
            CallerSide.PrepareForWrapperCall();


            ILJmpWrappers.Pass6Args_Maxstack_6(
                arg0,
                arg1,
                arg2,
                arg3,
                arg4,
                arg5
            );
            CallerSide.VerifyExecutedCaller("Pass6Args_Maxstack_6");
        }
        public static void MakeWrapperCallFor_Pass6Args_Maxstack_7()
        {
            int arg0;
            long arg1;
            float arg2;
            double arg3;
            BasicStruct arg4;
            string arg5;
            arg0       = IntValues.ForArg0;
            arg1       = LongValues.ForArg1;
            arg2       = FloatValues.ForArg2;
            arg3       = DoubleValues.ForArg3;
            arg4       = StructValues.ForArg4;
            arg5       = StringValues.ForArg5;
            CallerSide.PrepareForWrapperCall();


            ILJmpWrappers.Pass6Args_Maxstack_7(
                arg0,
                arg1,
                arg2,
                arg3,
                arg4,
                arg5
            );
            CallerSide.VerifyExecutedCaller("Pass6Args_Maxstack_7");
        }
        public static void MakeWrapperCallFor_Pass6Args_Maxstack_8()
        {
            int arg0;
            long arg1;
            float arg2;
            double arg3;
            BasicStruct arg4;
            string arg5;
            arg0       = IntValues.ForArg0;
            arg1       = LongValues.ForArg1;
            arg2       = FloatValues.ForArg2;
            arg3       = DoubleValues.ForArg3;
            arg4       = StructValues.ForArg4;
            arg5       = StringValues.ForArg5;
            CallerSide.PrepareForWrapperCall();


            ILJmpWrappers.Pass6Args_Maxstack_8(
                arg0,
                arg1,
                arg2,
                arg3,
                arg4,
                arg5
            );
            CallerSide.VerifyExecutedCaller("Pass6Args_Maxstack_8");
        }
        public static void MakeWrapperCallFor_Pass6Args_Maxstack_9()
        {
            int arg0;
            long arg1;
            float arg2;
            double arg3;
            BasicStruct arg4;
            string arg5;
            arg0       = IntValues.ForArg0;
            arg1       = LongValues.ForArg1;
            arg2       = FloatValues.ForArg2;
            arg3       = DoubleValues.ForArg3;
            arg4       = StructValues.ForArg4;
            arg5       = StringValues.ForArg5;
            CallerSide.PrepareForWrapperCall();


            ILJmpWrappers.Pass6Args_Maxstack_9(
                arg0,
                arg1,
                arg2,
                arg3,
                arg4,
                arg5
            );
            CallerSide.VerifyExecutedCaller("Pass6Args_Maxstack_9");
        }
        public static void MakeWrapperCallFor_Pass6Args_Maxstack_10()
        {
            int arg0;
            long arg1;
            float arg2;
            double arg3;
            BasicStruct arg4;
            string arg5;
            arg0       = IntValues.ForArg0;
            arg1       = LongValues.ForArg1;
            arg2       = FloatValues.ForArg2;
            arg3       = DoubleValues.ForArg3;
            arg4       = StructValues.ForArg4;
            arg5       = StringValues.ForArg5;
            CallerSide.PrepareForWrapperCall();


            ILJmpWrappers.Pass6Args_Maxstack_10(
                arg0,
                arg1,
                arg2,
                arg3,
                arg4,
                arg5
            );
            CallerSide.VerifyExecutedCaller("Pass6Args_Maxstack_10");
        }
        public static bool MakeAllWrapperCalls()
        {
            bool bret = true;
            bret &= CallerSide.MakeWrapperCall("Pass6Args_Maxstack_2", CallerSide.MakeWrapperCallFor_Pass6Args_Maxstack_2);
            bret &= CallerSide.MakeWrapperCall("Pass6Args_Maxstack_3", CallerSide.MakeWrapperCallFor_Pass6Args_Maxstack_3);
            bret &= CallerSide.MakeWrapperCall("Pass6Args_Maxstack_4", CallerSide.MakeWrapperCallFor_Pass6Args_Maxstack_4);
            bret &= CallerSide.MakeWrapperCall("Pass6Args_Maxstack_5", CallerSide.MakeWrapperCallFor_Pass6Args_Maxstack_5);
            bret &= CallerSide.MakeWrapperCall("Pass6Args_Maxstack_6", CallerSide.MakeWrapperCallFor_Pass6Args_Maxstack_6);
            bret &= CallerSide.MakeWrapperCall("Pass6Args_Maxstack_7", CallerSide.MakeWrapperCallFor_Pass6Args_Maxstack_7);
            bret &= CallerSide.MakeWrapperCall("Pass6Args_Maxstack_8", CallerSide.MakeWrapperCallFor_Pass6Args_Maxstack_8);
            bret &= CallerSide.MakeWrapperCall("Pass6Args_Maxstack_9", CallerSide.MakeWrapperCallFor_Pass6Args_Maxstack_9);
            bret &= CallerSide.MakeWrapperCall("Pass6Args_Maxstack_10", CallerSide.MakeWrapperCallFor_Pass6Args_Maxstack_10);
            return bret;
        }
    }


}
