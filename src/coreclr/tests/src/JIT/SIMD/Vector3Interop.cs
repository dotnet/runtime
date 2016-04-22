// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;


public struct DT 
{
    public Vector3 a;
    public Vector3 b;
};

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct ComplexDT 
{
    public int iv;
    public DT vecs;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=256)] public string str;
    public Vector3 v3;
};

class PInvokeTest 
{
    [DllImport(@"Vector3TestNative", CallingConvention = CallingConvention.StdCall)]
    public static extern int nativeCall_PInvoke_CheckVector3Size();

    [DllImport(@"Vector3TestNative", CallingConvention = CallingConvention.StdCall)]
    public static extern float nativeCall_PInvoke_Vector3Arg(
        int i, 
        Vector3 v1, 
        [MarshalAs(UnmanagedType.LPStr)] string s, 
        Vector3 v2);

    [DllImport(@"Vector3TestNative", CallingConvention = CallingConvention.StdCall)]
    public static extern Vector3 nativeCall_PInvoke_Vector3Ret();

    [DllImport(@"Vector3TestNative", CallingConvention = CallingConvention.StdCall)]
    public static extern float nativeCall_PInvoke_Vector3Array(Vector3[] v_array);

    [DllImport(@"Vector3TestNative", CallingConvention = CallingConvention.StdCall)]
    public static extern DT nativeCall_PInvoke_Vector3InStruct(DT d);

    [DllImport(@"Vector3TestNative", CallingConvention = CallingConvention.StdCall)]
    public static extern void nativeCall_PInvoke_Vector3InComplexStruct(ref ComplexDT cdt);
    
    public static bool test() 
    {

        // Expected return value is 12 bytes.
        if (nativeCall_PInvoke_CheckVector3Size() != 12) 
        {
            Console.WriteLine("The size of native Vector3 type is not 12 bytes");
            return false;            
        }
    
        {
            int iv = 123;
            Vector3 v1 = new Vector3(1,2,3);
            string str = "abcdefg";
            Vector3 v2 = new Vector3(10,11,12);
            // Expected return value = 1 + 2 + 3 + 10 + 11 + 12 = 39
            if (nativeCall_PInvoke_Vector3Arg(iv, v1, str, v2) != 39) 
            {
                Console.Write("PInvoke Vector3Arg test failed\n");
                return false;            
            }
        }
        
        // JIT crashes with this testcase.
        // Disabled temporarily.
        // {
        //     // Expected return value = 1 + 2 + 3 = 6
        //     Vector3 ret = nativeCall_PInvoke_Vector3Ret();
        //     float sum = ret.X + ret.Y + ret.Z;
        //     if (sum != 6) {
        //         Console.WriteLine("PInvoke Vector3Ret test failed");
        //         return false;            
        //     }
        // }
        

        {
            Vector3[] v3_array = new Vector3[2];
            v3_array[0].X = 1; v3_array[0].Y = 2; v3_array[0].Z = 3;
            v3_array[1].X = 5; v3_array[1].Y = 6; v3_array[1].Z = 7;
            // Expected resutn value = 1 + 2 + 3 + 5 + 6 + 7 = 24
            if (nativeCall_PInvoke_Vector3Array(v3_array) != 24) 
            {
                Console.WriteLine("PInvoke Vector3Array test failed");
                return false;            
            }
        }
        
        {
            DT data = new DT();
            data.a = new Vector3(1,2,3);
            data.b = new Vector3(5,6,7);
            DT ret = nativeCall_PInvoke_Vector3InStruct(data);
            // Expected return value = 2 + 3 + 4 + 6 + 7 + 8 = 30
            float sum = ret.a.X + ret.a.Y + ret.a.Z + ret.b.X + ret.b.Y + ret.b.Z;
            if (sum != 30) 
            {
                Console.WriteLine("PInvoke Vector3InStruct test failed");
                return false;            
            }
        }
        
        {
            ComplexDT cdt = new ComplexDT();
            cdt.iv = 99;
            cdt.str = "arg_string";
            cdt.vecs.a = new Vector3(1,2,3);
            cdt.vecs.b = new Vector3(5,6,7);
            cdt.v3 = new Vector3(10, 20, 30);

            nativeCall_PInvoke_Vector3InComplexStruct(ref cdt);
            
            Console.WriteLine("    Managed ival: {0}", cdt.iv);
            Console.WriteLine("    Managed Vector3 v1: ({0} {1} {2})", cdt.vecs.a.X, cdt.vecs.a.Y, cdt.vecs.a.Z);
            Console.WriteLine("    Managed Vector3 v2: ({0} {1} {2})", cdt.vecs.b.X, cdt.vecs.b.Y, cdt.vecs.b.Z);
            Console.WriteLine("    Managed Vector3 v3: ({0} {1} {2})", cdt.v3.X, cdt.v3.Y, cdt.v3.Z);
            Console.WriteLine("    Managed string arg: {0}", cdt.str); 
        
            // Expected return value = 2 + 3 + 4 + 6 + 7 + 8 + 11 + 12 + 13 = 93
            float sum = cdt.vecs.a.X + cdt.vecs.a.Y + cdt.vecs.a.Z 
                + cdt.vecs.b.X + cdt.vecs.b.Y + cdt.vecs.b.Z 
                + cdt.v3.X + cdt.v3.Y + cdt.v3.Z;
            if ((sum != 93) || (cdt.iv != 100) || (cdt.str.ToString() != "ret_string") )
            {
                Console.WriteLine("PInvoke Vector3InStruct test failed");
                return false;            
            }
        }        

        Console.WriteLine("All PInvoke testcases passed");
        return true;
    }    
}

class RPInvokeTest 
{
    public delegate void CallBackDelegate_RPInvoke_Vector3Arg(
        int i, 
        Vector3 v1, 
        [MarshalAs(UnmanagedType.LPStr)] string s, 
        Vector3 v2);
        
    public delegate Vector3 CallBackDelegate_RPInvoke_Vector3Ret();
    
    public delegate void CallBackDelegate_RPInvoke_Vector3Array(
        [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)] Vector3[] v, 
        int size); 
        
    public delegate void CallBackDelegate_RPInvoke_Vector3InStruct(
        DT v);        
        
    public delegate void CallBackDelegate_RPInvoke_Vector3InComplexStruct(
        ref ComplexDT v);    

    [DllImport(@"Vector3TestNative", CallingConvention = CallingConvention.StdCall)]
    public static extern void nativeCall_RPInvoke_Vector3Arg(
        CallBackDelegate_RPInvoke_Vector3Arg callBack);
        
    [DllImport(@"Vector3TestNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool nativeCall_RPInvoke_Vector3Ret(
        CallBackDelegate_RPInvoke_Vector3Ret callBack);        
        
    [DllImport(@"Vector3TestNative", CallingConvention = CallingConvention.StdCall)]
    public static extern void nativeCall_RPInvoke_Vector3Array(
        CallBackDelegate_RPInvoke_Vector3Array callBack, 
        int v);
           
    [DllImport(@"Vector3TestNative", CallingConvention = CallingConvention.StdCall)]
    public static extern void nativeCall_RPInvoke_Vector3InStruct(
        CallBackDelegate_RPInvoke_Vector3InStruct callBack, 
        int v);
        
    [DllImport(@"Vector3TestNative", CallingConvention = CallingConvention.StdCall)]        
    public static extern bool nativeCall_RPInvoke_Vector3InComplexStruct(
        CallBackDelegate_RPInvoke_Vector3InComplexStruct callBack);
    
    static bool result = false;
   
    static void callBack_RPInvoke_Vector3Arg(
        int i, 
        Vector3 v1, 
        [MarshalAs(UnmanagedType.LPStr)] string s, 
        Vector3 v2)
    {
        Vector3 tmp = new Vector3(2, 2, 2);
        
        // sum = (1, 2, 3) dot (2, 2, 2) = 12
        float sum0 = Vector3.Dot(v1, tmp);   
        // sum = (10, 20, 30) dot (2, 2, 2) = 20 + 40 + 60 = 120
        float sum1 = Vector3.Dot(v2, tmp);        
        
        Console.WriteLine("callBack_RPInvoke_Vector3Arg:");
        Console.WriteLine("    iVal {0}", i);
        Console.WriteLine("    SumOfEles(v1) = {0} SumOfEles(v2) = {1}", sum0, sum1);
        Console.WriteLine("    str {0}", s);
        
        result = (sum0 == 12) && (sum1 == 120) && (s == "abcdefg") && (i == 123);
    }
    
    static Vector3 callBack_RPInvoke_Vector3Ret()
    {
        Vector3 tmp = new Vector3(1, 2, 3);
        return tmp;
    }        
    
    static void callBack_RPInvoke_Vector3Array(
        [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)] Vector3[] v, 
        int size)
    {   
        Vector3 tmp = new Vector3(2, 2, 2);
        
        // sum0 = (2,3,4) dot (2,2,2) = 4 + 6 + 8 = 18
        float sum0 = Vector3.Dot(v[0], tmp);
        // sum0 = (11,21,31) dot (2,2,2) = 22 + 42 + 62 = 126
        float sum1 = Vector3.Dot(v[1], tmp);
        
        Console.WriteLine("callBack_RPInvoke_Vector3Array: Sum0 = {0} Sum1 = {1}", sum0, sum1);
        
        result = (sum0 == 18) && (sum1 == 126);
    }
        
    
    static void callBack_RPInvoke_Vector3InStruct(DT v)
    {   
        Vector3 tmp = new Vector3(2, 2, 2);
        
        // sum0 = (2,3,4) dot (2,2,2) = 4 + 6 + 8 = 18
        float sum0 = Vector3.Dot(v.a, tmp);
        // sum1 = (11,21,31) dot (2,2,2) = 22 + 42 + 62 = 126
        float sum1 = Vector3.Dot(v.b, tmp);
        
        Console.WriteLine("callBack_RPInvoke_Vector3InStruct: Sum0 = {0} Sum1 = {1}", sum0, sum1);
        
        result = (sum0 == 18) && (sum1 == 126);
    }
    
    static void callBack_RPInvoke_Vector3InComplexStruct(ref ComplexDT arg)
    {   
        ComplexDT ret;
        Console.WriteLine("callBack_RPInvoke_Vector3InComplexStruct");
        Console.WriteLine("    Arg ival: {0}", arg.iv);
        Console.WriteLine("    Arg Vector3 v1: ({0} {1} {2})", arg.vecs.a.X, arg.vecs.a.Y, arg.vecs.a.Z);
        Console.WriteLine("    Arg Vector3 v2: ({0} {1} {2})", arg.vecs.b.X, arg.vecs.b.Y, arg.vecs.b.Z);
        Console.WriteLine("    Arg Vector3 v3: ({0} {1} {2})", arg.v3.X, arg.v3.Y, arg.v3.Z);
        Console.WriteLine("    Arg string arg: {0}", arg.str);        
       
        arg.vecs.a.X = arg.vecs.a.X + 1;
        arg.vecs.a.Y = arg.vecs.a.Y + 1;
        arg.vecs.a.Z = arg.vecs.a.Z + 1;
        arg.vecs.b.X = arg.vecs.b.X + 1;
        arg.vecs.b.Y = arg.vecs.b.Y + 1;
        arg.vecs.b.Z = arg.vecs.b.Z + 1;
        arg.v3.X = arg.v3.X + 1;
        arg.v3.Y = arg.v3.Y + 1;
        arg.v3.Z = arg.v3.Z + 1;    
        arg.iv = arg.iv + 1;
        arg.str = "ret_string";
        
        Console.WriteLine("    Return ival: {0}", arg.iv);
        Console.WriteLine("    Return Vector3 v1: ({0} {1} {2})", arg.vecs.a.X, arg.vecs.a.Y, arg.vecs.a.Z);
        Console.WriteLine("    Return Vector3 v2: ({0} {1} {2})", arg.vecs.b.X, arg.vecs.b.Y, arg.vecs.b.Z);
        Console.WriteLine("    Return Vector3 v3: ({0} {1} {2})", arg.v3.X, arg.v3.Y, arg.v3.Z);
        Console.WriteLine("    Return string arg: {0}", arg.str);        
        float sum = arg.vecs.a.X + arg.vecs.a.Y + arg.vecs.a.Z
            + arg.vecs.b.X + arg.vecs.b.Y + arg.vecs.b.Z
            + arg.v3.X + arg.v3.Y + arg.v3.Z;
        Console.WriteLine("    Sum of all return float scalar values = {0}", sum);            
    }    
    
    public static bool test() {
        int x = 1;
        
        nativeCall_RPInvoke_Vector3Arg(callBack_RPInvoke_Vector3Arg);
        if (!result) 
        {
            Console.WriteLine("RPInvoke Vector3Arg test failed");
            return false;            
        }
        
        result = nativeCall_RPInvoke_Vector3Ret(callBack_RPInvoke_Vector3Ret);
        if (!result) 
        {
            Console.WriteLine("RPInvoke Vector3Ret test failed");
            return false;            
        }        
        
        nativeCall_RPInvoke_Vector3Array(callBack_RPInvoke_Vector3Array, x);
        if (!result) 
        {
            Console.WriteLine("RPInvoke Vector3Array test failed");
            return false;            
        }        
        
        nativeCall_RPInvoke_Vector3InStruct(callBack_RPInvoke_Vector3InStruct, x);
        if (!result) 
        {
            Console.WriteLine("RPInvoke Vector3InStruct test failed");
            return false;            
        }
        
        result = nativeCall_RPInvoke_Vector3InComplexStruct(callBack_RPInvoke_Vector3InComplexStruct);
        if (!result) 
        {
            Console.WriteLine("RPInvoke Vector3InComplexStruct test failed");
            return false;            
        }        
        
        Console.WriteLine("All RPInvoke testcases passed");
        return true;
    }     
}

class Test 
{  
    public static int Main() 
    {

        if (!PInvokeTest.test()) 
        {
            return 101;
        }
        
        if (!RPInvokeTest.test()) 
        {
            return 101;
        }
        return 100;
    } 
}
