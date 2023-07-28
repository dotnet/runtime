// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections;
using System.IO.IsolatedStorage;
using System.Security.Policy;

public class Helper
{

    #region methods for InnerSequential struct

    // Return new InnerSequential instance
    public static InnerSequential NewInnerSequential(int f1, float f2, string f3)
    {
        InnerSequential inner_seq = new InnerSequential();
        inner_seq.f1 = f1;
        inner_seq.f2 = f2;
        inner_seq.f3 = f3;
        return inner_seq;
    }

    //	Prints InnerSequential
    public static void PrintInnerSequential(InnerSequential inner_seq, string name)
    {
        Console.WriteLine("\t{0}.f1 = {1}", name, inner_seq.f1);
        Console.WriteLine("\t{0}.f2 = {1}", name, inner_seq.f2);
        Console.WriteLine("\t{0}.f3 = {1}", name, inner_seq.f3);
    }

    public static bool ValidateInnerSequential(InnerSequential s1, InnerSequential s2, string methodName)
    {
        if (s1.f1 != s2.f1 || s1.f2 != s2.f2 || s1.f3 != s2.f3)
        {
            Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
            Console.WriteLine("\tThe Actual is...");
            PrintInnerSequential(s1, s1.ToString());
            Console.WriteLine("\tThe Expected is...");
            PrintInnerSequential(s2, s2.ToString());
            return false;
        }
        else
        {
            Console.WriteLine("\tPASSED!");
            return true;
        }
    }

    #endregion

    #region methods for INNER2 struct

    // Return new INNER2 instance
    public static INNER2 NewINNER2(int f1, float f2, string f3)
    {
        INNER2 inner = new INNER2();
        inner.f1 = f1;
        inner.f2 = f2;
        inner.f3 = f3;
        return inner;
    }

    //	Prints INNER2
    public static void PrintINNER2(INNER2 inner, string name)
    {
        Console.WriteLine("\t{0}.f1 = {1}", name, inner.f1);
        Console.WriteLine("\t{0}.f2 = {1}", name, inner.f2);
        Console.WriteLine("\t{0}.f3 = {1}", name, inner.f3);
    }

    public static bool ValidateINNER2(INNER2 inner1, INNER2 inner2, string methodName)
    {
        if (inner1.f1 != inner2.f1 || inner1.f2 != inner2.f2 || inner1.f3 != inner2.f3)
        {
            Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
            Console.WriteLine("\tThe Actual is...");
            PrintINNER2(inner1, inner1.ToString());
            Console.WriteLine("\tThe Expected is...");
            PrintINNER2(inner2, inner2.ToString());
            return false;
        }
        else
        {
            Console.WriteLine("\tPASSED!");
            return true;
        }
    }

    #endregion

    #region methods for InnerExplicit struct

    // Return new InnerExplicit instance
    public static InnerExplicit NewInnerExplicit(int f1, float f2, string f3)
    {
        InnerExplicit inner = new InnerExplicit();
        inner.f1 = f1;
        inner.f2 = f2;
        inner.f3 = f3;
        return inner;
    }

    //	Prints InnerExplicit
    public static void PrintInnerExplicit(InnerExplicit inner, string name)
    {
        Console.WriteLine("\t{0}.f1 = {1}", name, inner.f1);
        Console.WriteLine("\t{0}.f2 = {1}", name, inner.f2);
        Console.WriteLine("\t{0}.f3 = {1}", name, inner.f3);
    }

    public static bool ValidateInnerExplicit(InnerExplicit inner1, InnerExplicit inner2, string methodName)
    {
        if (inner1.f1 != inner2.f1 || inner1.f2 != inner2.f2 || inner1.f3 != inner2.f3)
        {
            Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
            Console.WriteLine("\tThe Actual is...");
            PrintInnerExplicit(inner1, inner1.ToString());
            Console.WriteLine("\tThe Expected is...");
            PrintInnerExplicit(inner2, inner2.ToString());
            return false;
        }
        else
        {
            Console.WriteLine("\tPASSED!");
            return true;
        }
    }

    #endregion

    #region methods for InnerArraySequential struct

    //	Returns new OUTER instance; the params are the fields of INNER;
    //	all the INNER elements have the same field values
    public static InnerArraySequential NewInnerArraySequential(int f1, float f2, string f3)
    {
        InnerArraySequential outer = new InnerArraySequential();
        outer.arr = new InnerSequential[Common.NumArrElements];
        for (int i = 0; i < Common.NumArrElements; i++)
        {
            outer.arr[i].f1 = f1;
            outer.arr[i].f2 = f2;
            outer.arr[i].f3 = f3;
        }
        return outer;
    }

    //	Prints InnerArraySequential
    public static void PrintInnerArraySequential(InnerArraySequential outer, string name)
    {
        for (int i = 0; i < Common.NumArrElements; i++)
        {
            Console.WriteLine("\t{0}.arr[{1}].f1 = {2}", name, i, outer.arr[i].f1);
            Console.WriteLine("\t{0}.arr[{1}].f2 = {2}", name, i, outer.arr[i].f2);
            Console.WriteLine("\t{0}.arr[{1}].f3 = {2}", name, i, outer.arr[i].f3);
        }
    }

    //	Returns true if the two params have the same fields
    public static bool ValidateInnerArraySequential(InnerArraySequential outer1, InnerArraySequential outer2, string methodName)
    {
        for (int i = 0; i < Common.NumArrElements; i++)
        {
            if (outer1.arr[i].f1 != outer2.arr[i].f1 ||
                outer1.arr[i].f2 != outer2.arr[i].f2 ||
                outer1.arr[i].f3 != outer2.arr[i].f3)
            {
                Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
                Console.WriteLine("\tThe Actual is...");
                Console.WriteLine("\t{0}.arr[{1}].f1 = {2}", outer1.ToString(), i, outer1.arr[i].f1);
                Console.WriteLine("\t{0}.arr[{1}].f2 = {2}", outer1.ToString(), i, outer1.arr[i].f2);
                Console.WriteLine("\t{0}.arr[{1}].f3 = {2}", outer1.ToString(), i, outer1.arr[i].f3);
                Console.WriteLine("\tThe Expected is...");
                Console.WriteLine("\t{0}.arr[{1}].f1 = {2}", outer2.ToString(), i, outer2.arr[i].f1);
                Console.WriteLine("\t{0}.arr[{1}].f2 = {2}", outer2.ToString(), i, outer2.arr[i].f2);
                Console.WriteLine("\t{0}.arr[{1}].f3 = {2}", outer2.ToString(), i, outer2.arr[i].f3);
                return false;
            }
        }
        Console.WriteLine("\tPASSED!");
        return true;
    }

    #endregion

    #region methods for InnerArrayExplicit struct

    //	Returns new InnerArrayExplicit instance; the params are the fields of INNER;
    //	all the INNER elements have the same field values
    public static InnerArrayExplicit NewInnerArrayExplicit(int f1, float f2, string f3, string f4)
    {
        InnerArrayExplicit outer = new InnerArrayExplicit();
        outer.arr = new InnerSequential[Common.NumArrElements];
        for (int i = 0; i < Common.NumArrElements; i++)
        {
            outer.arr[i].f1 = f1;
            outer.arr[i].f2 = f2;
            outer.arr[i].f3 = f3;
        }
        outer.f4 = f4;
        return outer;
    }

    //	Prints InnerArrayExplicit
    public static void PrintInnerArrayExplicit(InnerArrayExplicit outer, string name)
    {
        for (int i = 0; i < Common.NumArrElements; i++)
        {
            Console.WriteLine("\t{0}.arr[{1}].f1 = {2}", name, i, outer.arr[i].f1);
            Console.WriteLine("\t{0}.arr[{1}].f2 = {2}", name, i, outer.arr[i].f2);
            Console.WriteLine("\t{0}.arr[{1}].f3 = {2}", name, i, outer.arr[i].f3);
        }
        Console.WriteLine("\t{0}.f4 = {1}", name, outer.f4);
    }

    //	Returns true if the two params have the same fields
    public static bool ValidateInnerArrayExplicit(InnerArrayExplicit outer1, InnerArrayExplicit InnerArrayExplicit, string methodName)
    {
        for (int i = 0; i < Common.NumArrElements; i++)
        {
            if (outer1.arr[i].f1 != InnerArrayExplicit.arr[i].f1)
            {
                Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
                Console.WriteLine("\tThe Actual f1 field is...");
                Console.WriteLine("\t{0}.arr[{1}].f1 = {2}", outer1.ToString(), i, outer1.arr[i].f1);
                Console.WriteLine("\tThe Expected f1 field is...");
                Console.WriteLine("\t{0}.arr[{1}].f1 = {2}", InnerArrayExplicit.ToString(), i, InnerArrayExplicit.arr[i].f1);
                return false;
            }
        }
        if (outer1.f4 != InnerArrayExplicit.f4)
        {
            Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
            Console.WriteLine("\tThe Actual f4 field is...");
            Console.WriteLine("\t{0}.f4 = {1}", outer1.ToString(), outer1.f4);
            Console.WriteLine("\tThe Expected f4 field is...");
            Console.WriteLine("\t{0}.f4 = {1}", InnerArrayExplicit.ToString(), InnerArrayExplicit.f4);
            return false;
        }
        Console.WriteLine("\tPASSED!");
        return true;
    }

    #endregion

    #region methods for OUTER3 struct

    //	Returns new OUTER3 instance; the params are the fields of INNER;
    //	all the INNER elements have the same field values
    public static OUTER3 NewOUTER3(int f1, float f2, string f3, string f4)
    {
        OUTER3 outer = new OUTER3();
        outer.arr = new InnerSequential[Common.NumArrElements];
        for (int i = 0; i < Common.NumArrElements; i++)
        {
            outer.arr[i].f1 = f1;
            outer.arr[i].f2 = f2;
            outer.arr[i].f3 = f3;
        }
        outer.f4 = f4;
        return outer;
    }

    //	Prints OUTER3
    public static void PrintOUTER3(OUTER3 outer, string name)
    {
        for (int i = 0; i < Common.NumArrElements; i++)
        {
            Console.WriteLine("\t{0}.arr[{1}].f1 = {2}", name, i, outer.arr[i].f1);
            Console.WriteLine("\t{0}.arr[{1}].f2 = {2}", name, i, outer.arr[i].f2);
            Console.WriteLine("\t{0}.arr[{1}].f3 = {2}", name, i, outer.arr[i].f3);
        }
        Console.WriteLine("\t{0}.f4 = {1}", name, outer.f4);
    }

    //	Returns true if the two params have the same fields
    public static bool ValidateOUTER3(OUTER3 outer1, OUTER3 InnerArrayExplicit, string methodName)
    {
        for (int i = 0; i < Common.NumArrElements; i++)
        {
            if (outer1.arr[i].f1 != InnerArrayExplicit.arr[i].f1 ||
                outer1.arr[i].f2 != InnerArrayExplicit.arr[i].f2 ||
                outer1.arr[i].f3 != InnerArrayExplicit.arr[i].f3)
            {
                Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
                Console.WriteLine("\tThe Actual is...");
                Console.WriteLine("\t{0}.arr[{1}].f1 = {2}", outer1.ToString(), i, outer1.arr[i].f1);
                Console.WriteLine("\t{0}.arr[{1}].f2 = {2}", outer1.ToString(), i, outer1.arr[i].f2);
                Console.WriteLine("\t{0}.arr[{1}].f3 = {2}", outer1.ToString(), i, outer1.arr[i].f3);
                Console.WriteLine("\tThe Expected is...");
                Console.WriteLine("\t{0}.arr[{1}].f1 = {2}", InnerArrayExplicit.ToString(), i, InnerArrayExplicit.arr[i].f1);
                Console.WriteLine("\t{0}.arr[{1}].f2 = {2}", InnerArrayExplicit.ToString(), i, InnerArrayExplicit.arr[i].f2);
                Console.WriteLine("\t{0}.arr[{1}].f3 = {2}", InnerArrayExplicit.ToString(), i, InnerArrayExplicit.arr[i].f3);
                return false;
            }
        }
        if (outer1.f4 != InnerArrayExplicit.f4)
        {
            Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
            Console.WriteLine("\tThe Actual f4 field is...");
            Console.WriteLine("\t{0}.f4 = {1}", outer1.ToString(), outer1.f4);
            Console.WriteLine("\tThe Expected f4 field is...");
            Console.WriteLine("\t{0}.f4 = {1}", InnerArrayExplicit.ToString(), InnerArrayExplicit.f4);
            return false;
        }
        Console.WriteLine("\tPASSED!");
        return true;
    }

    #endregion

    #region methods for CharSetAnsiSequential struct

    //return CharSetAnsiSequential struct instance
    public static CharSetAnsiSequential NewCharSetAnsiSequential(string f1, char f2)
    {
        CharSetAnsiSequential str1 = new CharSetAnsiSequential();
        str1.f1 = f1;
        str1.f2 = f2;
        return str1;
    }

    //print the struct CharSetAnsiSequential element
    public static void PrintCharSetAnsiSequential(CharSetAnsiSequential str1, string name)
    {
        Console.WriteLine("\t{0}.f1 = {1}", name, str1.f1);
        Console.WriteLine("\t{0}.f2 = {1}", name, str1.f2);
    }

    //	Returns true if the two params have the same fields
    public static bool ValidateCharSetAnsiSequential(CharSetAnsiSequential str1, CharSetAnsiSequential str2, string methodName)
    {
        if (str1.f1 != str2.f1 || str1.f2 != str2.f2)
        {
            Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
            Console.WriteLine("\tThe Actual is...");
            PrintCharSetAnsiSequential(str1, str1.ToString());
            Console.WriteLine("\tThe Expected is...");
            PrintCharSetAnsiSequential(str2, str2.ToString());
            return false;
        }
        else
        {
            Console.WriteLine("\tPASSED!");
            return true;
        }
    }

    #endregion

    #region methods for CharSetUnicodeSequential struct

    //return the struct CharSetUnicodeSequential instance
    public static CharSetUnicodeSequential NewCharSetUnicodeSequential(string f1, char f2)
    {
        CharSetUnicodeSequential str1 = new CharSetUnicodeSequential();
        str1.f1 = f1;
        str1.f2 = f2;
        return str1;
    }

    //print the struct CharSetUnicodeSequential element
    public static void PrintCharSetUnicodeSequential(CharSetUnicodeSequential str1, string name)
    {
        Console.WriteLine("\t{0}.f1 = {1}", name, str1.f1);
        Console.WriteLine("\t{0}.f2 = {1}", name, str1.f2);
    }

    //	Returns true if the two params have the same fields
    public static bool ValidateCharSetUnicodeSequential(CharSetUnicodeSequential str1, CharSetUnicodeSequential str2, string methodName)
    {
        if (str1.f1 != str2.f1 || str1.f2 != str2.f2)
        {
            Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
            Console.WriteLine("\tThe Actual is...");
            PrintCharSetUnicodeSequential(str1, str1.ToString());
            Console.WriteLine("\tThe Expected is...");
            PrintCharSetUnicodeSequential(str2, str2.ToString());
            return false;
        }
        else
        {
            Console.WriteLine("\tPASSED!");
            return true;
        }
    }

    #endregion

    #region methods for NumberSequential struct

    public static NumberSequential NewNumberSequential(int i32, uint ui32, short s1, ushort us1, Byte b, SByte sb,
        Int16 i16, UInt16 ui16, Int64 i64, UInt64 ui64, Single sgl, Double d)
    {
        NumberSequential str1 = new NumberSequential();
        str1.i32 = i32;
        str1.ui32 = ui32;
        str1.s1 = s1;
        str1.us1 = us1;
        str1.b = b;
        str1.sb = sb;
        str1.i16 = i16;
        str1.ui16 = ui16;
        str1.i64 = i64;
        str1.ui64 = ui64;
        str1.sgl = sgl;
        str1.d = d;
        return str1;
    }

    public static void PrintNumberSequential(NumberSequential str1, string name)
    {
        Console.WriteLine("\t{0}.i32 = {1}", name, str1.i32);
        Console.WriteLine("\t{0}.ui32 = {1}", name, str1.ui32);
        Console.WriteLine("\t{0}.s1 = {1}", name, str1.s1);
        Console.WriteLine("\t{0}.us1 = {1}", name, str1.us1);
        Console.WriteLine("\t{0}.b = {1}", name, str1.b);
        Console.WriteLine("\t{0}.sb = {1}", name, str1.sb);
        Console.WriteLine("\t{0}.i16 = {1}", name, str1.i16);
        Console.WriteLine("\t{0}.ui16 = {1}", name, str1.ui16);
        Console.WriteLine("\t{0}.i64 = {1}", name, str1.i64);
        Console.WriteLine("\t{0}.ui64 = {1}", name, str1.ui64);
        Console.WriteLine("\t{0}.sgl = {1}", name, str1.sgl);
        Console.WriteLine("\t{0}.d = {1}", name, str1.d);
    }

    public static bool ValidateNumberSequential(NumberSequential str1, NumberSequential str2, string methodName)
    {
        if (str1.i32 != str2.i32 || str1.ui32 != str2.ui32 || str1.s1 != str2.s1 ||
            str1.us1 != str2.us1 || str1.b != str2.b || str1.sb != str2.sb || str1.i16 != str2.i16 ||
            str1.ui16 != str2.ui16 || str1.i64 != str2.i64 || str1.ui64 != str2.ui64 ||
            str1.sgl != str2.sgl || str1.d != str2.d)
        {
            Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
            Console.WriteLine("\tThe Actual is...");
            PrintNumberSequential(str1, str1.ToString());
            Console.WriteLine("\tThe Expected is...");
            PrintNumberSequential(str2, str2.ToString());
            return false;
        }
        else
        {
            Console.WriteLine("\tPASSED!");
            return true;
        }
    }

    #endregion

    #region methods for S3 struct

    public static void InitialArray(int[] iarr, int[] icarr)
    {
        for (int i = 0; i < iarr.Length; i++)
        {
            iarr[i] = i;
        }

        for (int i = 1; i < icarr.Length + 1; i++)
        {
            icarr[i - 1] = i;
        }
    }

    public static S3 NewS3(bool flag, string str, int[] vals)
    {
        S3 str1 = new S3();
        str1.flag = flag;
        str1.str = str;
        str1.vals = vals;
        return str1;
    }

    public static void PrintS3(S3 str1, string name)
    {
        Console.WriteLine("\t{0}.flag = {1}", name, str1.flag);
        Console.WriteLine("\t{0}.flag = {1}", name, str1.str);
        for (int i = 0; i < str1.vals.Length; i++)
        {
            Console.WriteLine("\t{0}.vals[{1}] = {2}", name, i, str1.vals[i]);
        }
    }

    public static bool ValidateS3(S3 str1, S3 str2, string methodName)
    {
        int iflag = 0;
        if (str1.flag != str2.flag || str1.str != str2.str)
        {
            Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
            Console.WriteLine("\tThe Actual flag field is...");
            Console.WriteLine("\t{0}.flag = {1}", str1.ToString(), str1.flag);
            Console.WriteLine("\t{0}.str = {1}", str1.ToString(), str1.str);
            Console.WriteLine("\tThe Expected is...");
            Console.WriteLine("\t{0}.flag = {1}", str2.ToString(), str2.flag);
            Console.WriteLine("\t{0}.str = {1}", str2.ToString(), str2.str);
            return false;
        }
        for (int i = 0; i < 256; i++)
        {
            if (str1.vals[i] != str2.vals[i])
            {
                Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
                Console.WriteLine("\tThe Actual vals field is...");
                Console.WriteLine("\t{0}.vals[{1}] = {2}", str1.ToString(), i, str1.vals[i]);
                Console.WriteLine("\tThe Expected vals field is...");
                Console.WriteLine("\t{0}.vals[{1}] = {2}", str2.ToString(), i, str2.vals[i]);
                iflag++;
            }
        }
        if (iflag != 0)
        {
            return false;
        }
        Console.WriteLine("\tPASSED!");
        return true;
    }

    #endregion

    #region methods for S5 struct

    public static S5 NewS5(int age, string name, Enum1 ef)
    {
        S4 s4 = new S4();
        s4.age = age;
        s4.name = name;

        S5 s5 = new S5();
        s5.s4 = s4;
        s5.ef = ef;

        return s5;
    }

    public static void PrintS5(S5 str1, string name)
    {
        Console.WriteLine("\t{0}.s4.age = {1}", str1.s4.age);
        Console.WriteLine("\t{0}.s4.name = {1}", str1.s4.name);
        Console.WriteLine("\t{0}.ef = {1}", str1.ef.ToString());
    }

    public static bool ValidateS5(S5 str1, S5 str2, string methodName)
    {
        if (str1.s4.age != str2.s4.age || str1.s4.name != str2.s4.name)
        {
            Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
            Console.WriteLine("\tThe Actual s4 field is...");
            Console.WriteLine("\t{0}.s4.age = {1}", str1.ToString(), str1.s4.age);
            Console.WriteLine("\t{0}.s4.name = {1}", str1.ToString(), str1.s4.name);
            Console.WriteLine("\tThe Expected s4 field is...");
            Console.WriteLine("\t{0}.s4.age = {1}", str2.ToString(), str2.s4.age);
            Console.WriteLine("\t{0}.s4.name = {1}", str2.ToString(), str2.s4.name);
            return false;
        }
        if (str1.ef != str2.ef)
        {
            Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
            Console.WriteLine("\tThe Actual ef field is...");
            Console.WriteLine("\t{0}.ef = {1}", str1.ToString(), str1.ef);
            Console.WriteLine("\tThe Expected s4 field is...");
            Console.WriteLine("\t{0}.ef = {1}", str2.ToString(), str2.ef);
            return false;
        }
        Console.WriteLine("\tPASSED!");
        return true;
    }

    #endregion

    #region methods for StringStructSequentialAnsi struct

    public static StringStructSequentialAnsi NewStringStructSequentialAnsi(string first, string last)
    {
        StringStructSequentialAnsi s6 = new StringStructSequentialAnsi();
        s6.first = first;
        s6.last = last;

        return s6;
    }

    public static void PrintStringStructSequentialAnsi(StringStructSequentialAnsi str1, string name)
    {
        Console.WriteLine("\t{0}.first = {1}", name, str1.first);
        Console.WriteLine("\t{0}.last = {1}", name, str1.last);
    }

    public static bool ValidateStringStructSequentialAnsi(StringStructSequentialAnsi str1, StringStructSequentialAnsi str2, string methodName)
    {
        if (str1.first != str2.first || str1.last != str2.last)
        {
            Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
            Console.WriteLine("\tThe Actual is...");
            PrintStringStructSequentialAnsi(str1, str1.ToString());
            Console.WriteLine("\tThe Expected is...");
            PrintStringStructSequentialAnsi(str2, str2.ToString());
            return false;
        }
        else
        {
            Console.WriteLine("\tPASSED!");
            return true;
        }
    }

    #endregion

    #region methods for StringStructSequentialUnicode struct

    public static StringStructSequentialUnicode NewStringStructSequentialUnicode(string first, string last)
    {
        StringStructSequentialUnicode s7 = new StringStructSequentialUnicode();
        s7.first = first;
        s7.last = last;

        return s7;
    }

    public static void PrintStringStructSequentialUnicode(StringStructSequentialUnicode str1, string name)
    {
        Console.WriteLine("\t{0}.first = {1}", name, str1.first);
        Console.WriteLine("\t{0}.last = {1}", name, str1.last);
    }

    public static bool ValidateStringStructSequentialUnicode(StringStructSequentialUnicode str1, StringStructSequentialUnicode str2, string methodName)
    {
        if (str1.first != str2.first || str1.last != str2.last)
        {
            Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
            Console.WriteLine("\tThe Actual is...");
            PrintStringStructSequentialUnicode(str1, str1.ToString());
            Console.WriteLine("\tThe Expected is...");
            PrintStringStructSequentialUnicode(str2, str2.ToString());
            return false;
        }
        else
        {
            Console.WriteLine("\tPASSED!");
            return true;
        }
    }

    #endregion

    #region methods for S8 struct

    public static S8 NewS8(string name, bool gender, UInt16 jobNum, int i32, uint ui32, sbyte mySByte)
    {
        S8 s8 = new S8();
        s8.name = name;
        s8.gender = gender;
        s8.i32 = i32;
        s8.ui32 = ui32;
        s8.jobNum = jobNum;
        s8.mySByte = mySByte;
        return s8;
    }

    public static void PrintS8(S8 str1, string name)
    {
        Console.WriteLine("\t{0}.name = {1}", name, str1.name);
        Console.WriteLine("\t{0}.gender = {1}", name, str1.gender);
        Console.WriteLine("\t{0}.jobNum = {1}", name, str1.jobNum);
        Console.WriteLine("\t{0}.i32 = {1}", name, str1.i32);
        Console.WriteLine("\t{0}.ui32 = {1}", name, str1.ui32);
        Console.WriteLine("\t{0}.mySByte = {1}", name, str1.mySByte);
    }

    public static bool ValidateS8(S8 str1, S8 str2, string methodName)
    {
        if (str1.name != str2.name || str1.gender != str2.gender ||
            str1.jobNum != str2.jobNum ||
            str1.i32 != str2.i32 || str1.ui32 != str2.ui32 || str1.mySByte != str2.mySByte)
        {
            Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
            Console.WriteLine("\tThe Actual is...");
            PrintS8(str1, str1.ToString());
            Console.WriteLine("\tThe Expected is...");
            PrintS8(str2, str2.ToString());
            return false;
        }
        Console.WriteLine("\tPASSED!");
        return true;

    }

    #endregion

    #region methods for S9 struct

    public static S9 NewS9(int i32, TestDelegate1 testDel1)
    {
        S9 s9 = new S9();
        s9.i32 = i32;
        s9.myDelegate1 = testDel1;
        return s9;
    }

    public static bool ValidateS9(S9 str1, S9 str2, string methodName)
    {
        if (str1.i32 != str2.i32 || str1.myDelegate1 != str2.myDelegate1)
        {
            Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
            Console.WriteLine("\tThe Actual is...");
            Console.WriteLine("\t{0}.i32 = {1}", str1.ToString(), str1.i32);
            Console.WriteLine("\t{0}.myDelegate1 = {1}", str1.ToString(), str1.myDelegate1);
            Console.WriteLine("\tThe Expected is...");
            Console.WriteLine("\t{0}.i32 = {1}", str2.ToString(), str2.i32);
            Console.WriteLine("\t{0}.myDelegate1 = {1}", str2.ToString(), str2.myDelegate1);
            return false;
        }
        Console.WriteLine("\tPASSED!");
        return true;
    }

    #endregion

    #region methods for IncludeOuterIntegerStructSequential struct

    public static IncludeOuterIntegerStructSequential NewIncludeOuterIntegerStructSequential(int i321, int i322)
    {
        IncludeOuterIntegerStructSequential s10 = new IncludeOuterIntegerStructSequential();
        s10.s.s_int.i = i321;
        s10.s.i = i322;
        return s10;
    }

    public static void PrintIncludeOuterIntegerStructSequential(IncludeOuterIntegerStructSequential str1, string name)
    {
        Console.WriteLine("\t{0}.s.s_int.i = {1}", name, str1.s.s_int.i);
        Console.WriteLine("\t{0}.s.i = {1}", name, str1.s.i);
    }

    public static bool ValidateIncludeOuterIntegerStructSequential(IncludeOuterIntegerStructSequential str1, IncludeOuterIntegerStructSequential str2, string methodName)
    {
        if (str1.s.s_int.i != str2.s.s_int.i || str1.s.i != str2.s.i)
        {
            Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
            Console.WriteLine("\tThe Actual is...");
            PrintIncludeOuterIntegerStructSequential(str1, str1.ToString());
            Console.WriteLine("\tThe Expected is...");
            PrintIncludeOuterIntegerStructSequential(str2, str2.ToString());
            return false;
        }
        else
        {
            Console.WriteLine("\tPASSED!");
            return true;
        }
    }

    #endregion

    #region methods for S11 struct

    unsafe public static void PrintS11(S11 str1, string name)
    {
        Console.WriteLine("\t{0}.i32 = {1}", name, (int)(str1.i32));
        Console.WriteLine("\t{0}.i = {1}", name, str1.i);
    }

    unsafe public static S11 NewS11(int* i32, int i)
    {
        S11 s11 = new S11();
        s11.i32 = i32;
        s11.i = i;
        return s11;
    }

    unsafe public static bool ValidateS11(S11 str1, S11 str2, string methodName)
    {
        if (str1.i32 != str2.i32 || str1.i != str2.i)
        {
            Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
            Console.WriteLine("\tThe Actual is...");
            PrintS11(str1, str1.ToString());
            Console.WriteLine("\tThe Expected is...");
            PrintS11(str2, str2.ToString());
            return false;
        }
        Console.WriteLine("\tPASSED!");
        return true;
    }

    #endregion

    #region methods for U struct

    public static U NewU(int i32, uint ui32, IntPtr iPtr, UIntPtr uiPtr, short s, ushort us, byte b, sbyte sb, long l, ulong ul, float f, double d)
    {
        U u = new U();
        u.i32 = i32;
        u.ui32 = ui32;
        u.iPtr = iPtr;
        u.uiPtr = uiPtr;
        u.s = s;
        u.us = us;
        u.b = b;
        u.sb = sb;
        u.l = l;
        u.ul = ul;
        u.f = f;
        u.d = d;

        return u;
    }

    public static void PrintU(U str1, string name)
    {
        Console.WriteLine("\t{0}.i32 = {1}", name, str1.i32);
        Console.WriteLine("\t{0}.ui32 = {1}", name, str1.ui32);
        Console.WriteLine("\t{0}.iPtr = {1}", name, str1.iPtr);
        Console.WriteLine("\t{0}.uiPtr = {1}", name, str1.uiPtr);
        Console.WriteLine("\t{0}.s = {1}", name, str1.s);
        Console.WriteLine("\t{0}.us = {1}", name, str1.us);
        Console.WriteLine("\t{0}.b = {1}", name, str1.b);
        Console.WriteLine("\t{0}.sb = {1}", name, str1.sb);
        Console.WriteLine("\t{0}.l = {1}", name, str1.l);
        Console.WriteLine("\t{0}.ul = {1}", name, str1.ul);
        Console.WriteLine("\t{0}.f = {1}", name, str1.f);
        Console.WriteLine("\t{0}.d = {1}", name, str1.d);
    }

    public static bool ValidateU(U str1, U str2, string methodName)
    {
        if (str1.i32 != str2.i32 || str1.ui32 != str2.ui32 || str1.iPtr != str2.iPtr ||
            str1.uiPtr != str2.uiPtr || str1.s != str2.s || str1.us != str2.us ||
            str1.b != str2.b || str1.sb != str2.sb || str1.l != str2.l || str1.ul != str2.ul ||
            str1.f != str2.f || str1.d != str2.d)
        {
            Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
            Console.WriteLine("\tThe Actual is...");
            PrintU(str1, str1.ToString());
            Console.WriteLine("\tThe Expected is...");
            PrintU(str2, str2.ToString());
            return false;
        }
        Console.WriteLine("\tPASSED!");
        return true;
    }

    #endregion

    #region methods for ByteStructPack2Explicit struct

    public static ByteStructPack2Explicit NewByteStructPack2Explicit(byte b1, byte b2)
    {
        ByteStructPack2Explicit u1 = new ByteStructPack2Explicit();
        u1.b1 = b1;
        u1.b2 = b2;

        return u1;
    }

    public static void PrintByteStructPack2Explicit(ByteStructPack2Explicit str1, string name)
    {
        Console.WriteLine("\t{0}.b1 = {1}", name, str1.b1);
        Console.WriteLine("\t{0}.b2 = {1}", name, str1.b2);
    }

    public static bool ValidateByteStructPack2Explicit(ByteStructPack2Explicit str1, ByteStructPack2Explicit str2, string methodName)
    {
        if (str1.b1 != str2.b1 || str1.b2 != str2.b2)
        {
            Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
            Console.WriteLine("\tThe Actual is...");
            PrintByteStructPack2Explicit(str1, str1.ToString());
            Console.WriteLine("\tThe Expected is...");
            PrintByteStructPack2Explicit(str2, str2.ToString());
            return false;
        }
        else
        {
            Console.WriteLine("\tPASSED!");
            return true;
        }
    }

    #endregion

    #region methods for ShortStructPack4Explicit struct

    public static ShortStructPack4Explicit NewShortStructPack4Explicit(short s1, short s2)
    {
        ShortStructPack4Explicit u2 = new ShortStructPack4Explicit();
        u2.s1 = s1;
        u2.s2 = s2;

        return u2;
    }

    public static void PrintShortStructPack4Explicit(ShortStructPack4Explicit str1, string name)
    {
        Console.WriteLine("\t{0}.s1 = {1}", name, str1.s1);
        Console.WriteLine("\t{0}.s2 = {1}", name, str1.s2);
    }

    public static bool ValidateShortStructPack4Explicit(ShortStructPack4Explicit str1, ShortStructPack4Explicit str2, string methodName)
    {
        if (str1.s1 != str2.s1 || str1.s2 != str2.s2)
        {
            Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
            Console.WriteLine("\tThe Actual is...");
            PrintShortStructPack4Explicit(str1, str1.ToString());
            Console.WriteLine("\tThe Expected is...");
            PrintShortStructPack4Explicit(str2, str2.ToString());
            return false;
        }
        else
        {
            Console.WriteLine("\tPASSED!");
            return true;
        }
    }

    #endregion

    #region methods for IntStructPack8Explicit struct

    public static IntStructPack8Explicit NewIntStructPack8Explicit(int i1, int i2)
    {
        IntStructPack8Explicit u3 = new IntStructPack8Explicit();
        u3.i1 = i1;
        u3.i2 = i2;

        return u3;
    }

    public static void PrintIntStructPack8Explicit(IntStructPack8Explicit str1, string name)
    {
        Console.WriteLine("\t{0}.i1 = {1}", name, str1.i1);
        Console.WriteLine("\t{0}.i2 = {1}", name, str1.i2);
    }

    public static bool ValidateIntStructPack8Explicit(IntStructPack8Explicit str1, IntStructPack8Explicit str2, string methodName)
    {
        if (str1.i1 != str2.i1 || str1.i2 != str2.i2)
        {
            Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
            Console.WriteLine("\tThe Actual is...");
            PrintIntStructPack8Explicit(str1, str1.ToString());
            Console.WriteLine("\tThe Expected is...");
            PrintIntStructPack8Explicit(str2, str2.ToString());
            return false;
        }
        else
        {
            Console.WriteLine("\tPASSED!");
            return true;
        }
    }

    #endregion

    #region methods for LongStructPack16Explicit struct

    public static LongStructPack16Explicit NewLongStructPack16Explicit(long l1, long l2)
    {
        LongStructPack16Explicit u4 = new LongStructPack16Explicit();
        u4.l1 = l1;
        u4.l2 = l2;

        return u4;
    }

    public static void PrintLongStructPack16Explicit(LongStructPack16Explicit str1, string name)
    {
        Console.WriteLine("\t{0}.l1 = {1}", name, str1.l1);
        Console.WriteLine("\t{0}.l2 = {1}", name, str1.l2);
    }

    public static bool ValidateLongStructPack16Explicit(LongStructPack16Explicit str1, LongStructPack16Explicit str2, string methodName)
    {
        if (str1.l1 != str2.l1 || str1.l2 != str2.l2)
        {
            Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
            Console.WriteLine("\tThe Actual is...");
            PrintLongStructPack16Explicit(str1, str1.ToString());
            Console.WriteLine("\tThe Expected is...");
            PrintLongStructPack16Explicit(str2, str2.ToString());
            return false;
        }
        else
        {
            Console.WriteLine("\tPASSED!");
            return true;
        }
    }

    #endregion

    #region methods for ByteStruct3Byte struct
    public static ByteStruct3Byte NewByteStruct3Byte(byte b1, byte b2, byte b3)
    {
        ByteStruct3Byte u1 = new ByteStruct3Byte();
        u1.b1 = b1;
        u1.b2 = b2;
        u1.b3 = b3;

        return u1;
    }
    public static void PrintByteStruct3Byte(ByteStruct3Byte str1, string name)
    {
        Console.WriteLine("\t{0}.b1 = {1}", name, str1.b1);
        Console.WriteLine("\t{0}.b2 = {1}", name, str1.b2);
        Console.WriteLine("\t{0}.b3 = {1}", name, str1.b3);
    }
    public static bool ValidateByteStruct3Byte(ByteStruct3Byte str1, ByteStruct3Byte str2, string methodName)
    {
        if (str1.b1 != str2.b1 || str1.b2 != str2.b2)
        {
            Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
            Console.WriteLine("\tThe Actual is...");
            PrintByteStruct3Byte(str1, str1.ToString());
            Console.WriteLine("\tThe Expected is...");
            PrintByteStruct3Byte(str2, str2.ToString());
            return false;
        }
        else
        {
            Console.WriteLine("\tPASSED!");
            return true;
        }
    }
    #endregion

    #region methods for IntegerStructSequential struct
    public static IntegerStructSequential NewIntegerStructSequential(int i1)
    {
        IntegerStructSequential u1 = new IntegerStructSequential();
        u1.i = i1;

        return u1;
    }
    public static void PrintIntegerStructSequential(IntegerStructSequential str1, string name)
    {
        Console.WriteLine("\t{0}.i = {1}", name, str1.i);
    }
    public static bool ValidateIntegerStructSequential(IntegerStructSequential str1, IntegerStructSequential str2, string methodName)
    {
        if (str1.i != str2.i)
        {
            Console.WriteLine("\tFAILED! " + methodName + "did not receive result as expected.");
            Console.WriteLine("\tThe Actual is...");
            PrintIntegerStructSequential(str1, str1.ToString());
            Console.WriteLine("\tThe Expected is...");
            PrintIntegerStructSequential(str2, str2.ToString());
            return false;
        }
        else
        {
            Console.WriteLine("\tPASSED!");
            return true;
        }
    }
    #endregion

}

public static class TestFramework
{
    public static void LogInformation(string str)
    {
        Logging.WriteLine(str);
    }
    public static void LogError(string id, string msg)
    {
        Logging.WriteLine("ERROR!!!-" + id + ": " + msg);
    }
    public static void BeginScenario(string name)
    {
        Logging.WriteLine("Beginning scenario: " + name);
    }
}

public static class Logging
{
    static TextWriter stdout = Console.Out;

#if (!WIN_8_P)
    static TextWriter loggingFile = null;
#endif

    public static void SetConsole(string fileName)
    {
#if (!WIN_8_P)
        FileStream fs = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        loggingFile = new StreamWriter(fs, Encoding.Unicode);
        Console.SetOut(loggingFile);
#endif
    }

    public static void ResetConsole()
    {
#if (!WIN_8_P)
        loggingFile.Close();
#endif
        Console.SetOut(stdout);
    }

    public static void WriteLine()
    {
        Console.WriteLine();
        Console.Out.Flush();
    }

    public static void WriteLine(bool value)
    {
        Console.WriteLine(value);
        Console.Out.Flush();
    }

    public static void WriteLine(char value)
    {
        Console.WriteLine(value);
        Console.Out.Flush();
    }

    public static void WriteLine(char[] buffer)
    {
        Console.WriteLine(buffer);
        Console.Out.Flush();
    }

    public static void WriteLine(char[] buffer, int index, int count)
    {
        Console.WriteLine(new string(buffer, index, count));
        Console.Out.Flush();
    }

    public static void WriteLine(decimal value)
    {
        Console.WriteLine(value);
        Console.Out.Flush();
    }

    public static void WriteLine(double value)
    {
        Console.WriteLine(value);
        Console.Out.Flush();
    }

    public static void WriteLine(float value)
    {
        Console.WriteLine(value);
        Console.Out.Flush();
    }

    public static void WriteLine(int value)
    {
        Console.WriteLine(value);
        Console.Out.Flush();
    }

    public static void WriteLine(uint value)
    {
        Console.WriteLine(value);
        Console.Out.Flush();
    }

    public static void WriteLine(long value)
    {
        Console.WriteLine(value);
        Console.Out.Flush();
    }

    public static void WriteLine(ulong value)
    {
        Console.WriteLine(value);
        Console.Out.Flush();
    }

    public static void WriteLine(Object value)
    {
        Console.WriteLine(value);
        Console.Out.Flush();
    }

    public static void WriteLine(String value)
    {
        Console.WriteLine(value);
        Console.Out.Flush();
    }

    public static void WriteLine(String format, Object arg0)
    {
        Console.WriteLine(format, arg0);
        Console.Out.Flush();
    }

    public static void WriteLine(String format, Object arg0, Object arg1)
    {
        Console.WriteLine(format, arg0, arg1);
        Console.Out.Flush();
    }

    public static void WriteLine(String format, Object arg0, Object arg1, Object arg2)
    {
        Console.WriteLine(format, arg0, arg1, arg2);
        Console.Out.Flush();
    }

    public static void WriteLine(String format, params Object[] arg)
    {
        Console.WriteLine(format, arg);
        Console.Out.Flush();
    }

    public static void Write(String format, Object arg0)
    {
        Console.Write(format, arg0);
        Console.Out.Flush();
    }

    public static void Write(String format, Object arg0, Object arg1)
    {
        Console.Write(format, arg0, arg1);
        Console.Out.Flush();
    }

    public static void Write(String format, Object arg0, Object arg1, Object arg2)
    {
        Console.Write(format, arg0, arg1, arg2);
        Console.Out.Flush();
    }

    public static void Write(String format, params Object[] arg)
    {
        Console.Write(format, arg);
        Console.Out.Flush();
    }

    public static void Write(bool value)
    {
        Console.Write(value);
        Console.Out.Flush();
    }

    public static void Write(char value)
    {
        Console.Write(value);
        Console.Out.Flush();
    }

    public static void Write(char[] buffer)
    {
        Console.Write(buffer);
        Console.Out.Flush();
    }

#if (!WIN_8_P)
    public static void Write(char[] buffer, int index, int count)
    {
        Console.Write(buffer, index, count);
        Console.Out.Flush();
    }
#endif

    public static void Write(double value)
    {
        Console.Write(value);
        Console.Out.Flush();
    }

    public static void Write(decimal value)
    {
        Console.Write(value);
        Console.Out.Flush();
    }

    public static void Write(float value)
    {
        Console.Write(value);
        Console.Out.Flush();
    }

    public static void Write(int value)
    {
        Console.Write(value);
        Console.Out.Flush();
    }

    public static void Write(uint value)
    {
        Console.Write(value);
        Console.Out.Flush();
    }

    public static void Write(long value)
    {
        Console.Write(value);
        Console.Out.Flush();
    }

    public static void Write(ulong value)
    {
        Console.Write(value);
        Console.Out.Flush();
    }

    public static void Write(Object value)
    {
        Console.Write(value);
        Console.Out.Flush();
    }

    public static void Write(String value)
    {
        Console.Write(value);
        Console.Out.Flush();
    }

}
