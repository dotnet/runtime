// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The native type of Vector3 is struct {float x,y,z} whose size is 12 bytes. RyuJit uses 16-byte
// register or stack location to store a Vector3 variable with the assumptions below. New testcases
// are added to check whether:
// 
//  - RyuJit correctly generates code and memory layout that matches the native side.
//
//  - RyuJIt back-end assumptions about Vector3 types are satisfied.
//
//    - Assumption1: Vector3 type args passed in registers or on stack is rounded to POINTER_SIZE 
//      and hence on 64-bit targets it can be read/written as if it were TYP_SIMD16.
//
//    - Assumption2: Vector3 args passed in registers (e.g. unix) or on stack have their upper 
//      4-bytes being zero. Similarly Vector3 return type value returned from a method will have 
//      its upper 4-bytes zeroed out

using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;


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
    public static extern float nativeCall_PInvoke_Vector3Arg_Unix(
        Vector3 v3f32_xmm0, 
        float f32_xmm2, 
        float f32_xmm3,
        float f32_xmm4, 
        float f32_xmm5, 
        float f32_xmm6, 
        float f32_xmm7,
        float f32_mem0, 
        Vector3 v3f32_mem1, 
        float f32_mem2, 
        float f32_mem3);

    [DllImport(@"Vector3TestNative", CallingConvention = CallingConvention.StdCall)]
    public static extern float nativeCall_PInvoke_Vector3Arg_Unix2(
        Vector3 v3f32_xmm0, 
        float f32_xmm2, 
        float f32_xmm3,
        float f32_xmm4, 
        float f32_xmm5, 
        float f32_xmm6, 
        float f32_xmm7,
        float f32_mem0, 
        Vector3 v3f32_mem1, 
        float f32_mem2, 
        float f32_mem3,
        Vector3 v3f32_mem4,
        float f32_mem5);   
        
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

        // Argument passing test.
        // The native code accesses only 12 bytes for each Vector object.
        {
            int iv = 123;
            Vector3 v1 = new Vector3(1,2,3);
            string str = "abcdefg";
            Vector3 v2 = new Vector3(10,11,12);
            // Expected return value = 1 + 2 + 3 + 10 + 11 + 12 = 39
            if (nativeCall_PInvoke_Vector3Arg(iv, v1, str, v2) != 39) 
            {
                Console.WriteLine("PInvoke Vector3Arg test failed");
                return false;            
            }
        }
        
        // Argument passing test for Unix.
        // Few arguments are passed onto stack.
        {
            Vector3 v1 = new Vector3(1, 2, 3);
            Vector3 v2 = new Vector3(10, 20, 30);
            float f0 = 100, f1 = 101, f2 = 102, f3 = 103, f4 = 104, f5 = 105, f6 = 106, f7 = 107, f8 = 108;
            
            float sum = nativeCall_PInvoke_Vector3Arg_Unix(
                v1, // register
                f0, f1, f2, f3, f4, f5, // register
                f6, v2,  // stack
                f7, f8); // stack
            if (sum != 1002) {
                Console.WriteLine("PInvoke Vector3Arg_Unix test failed");
                return false;            
            }
        }
        
        // Argument passing test for Unix.
        // Few arguments are passed onto stack.
        {
            Vector3 v1 = new Vector3(1, 2, 3);
            Vector3 v2 = new Vector3(4, 5, 6);
            Vector3 v3 = new Vector3(7, 8, 9);
            float f0 = 100, f1 = 101, f2 = 102, f3 = 103, f4 = 104, f5 = 105, f6 = 106, f7 = 107, f8 = 108, f9 = 109;
            
            float sum = nativeCall_PInvoke_Vector3Arg_Unix2(
                v1, // register
                f0, f1, f2, f3, f4, f5, // register
                f6, v2,  // stack
                f7, f8,  // stack
                v3,      // stack
                f9);     // stack
            if (sum != 1090) {
                Console.WriteLine("PInvoke Vector3Arg_Unix2 test failed");
                return false;            
            }
        }        
        
        // Return test
        {
            Vector3 ret = nativeCall_PInvoke_Vector3Ret();
            // Expected return value = (1, 2, 3) dot (1, 2, 3) = 14
            float sum = Vector3.Dot(ret, ret);
            if (sum != 14) {
                Console.WriteLine("PInvoke Vector3Ret test failed");
                return false;            
            }
        }
        
        // Array argument test.
        // Both the managed and native code assumes 12 bytes for each element.
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
        
        // Structure pass and return test.
        // Both the managed and native side use 12 bytes for each Vector3 object.
        // Dot product makes sure that the backend assumption 1 and 2 are met.
        {
            DT data = new DT();
            data.a = new Vector3(1,2,3);
            data.b = new Vector3(5,6,7);
            DT ret = nativeCall_PInvoke_Vector3InStruct(data);
            // Expected return value = (2, 3, 4) dot (6, 7, 8) = 12 + 21 + 32 = 65
            float sum = Vector3.Dot(ret.a, ret.b);
            if (sum != 65) 
            {
                Console.WriteLine("PInvoke Vector3InStruct test failed");
                return false;            
            }
        }

        // Complex struct test
        // Dot product makes sure that the backend assumption 1 and 2 are met.
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
               
            // (2, 3, 4) dot (6, 7 , 8) = 12 + 21 + 32 = 65
            float t0 = Vector3.Dot(cdt.vecs.a, cdt.vecs.b);
            // (6, 7, 8) dot (11, 21, 31) = 66 + 147 + 248 = 461
            float t1 = Vector3.Dot(cdt.vecs.b, cdt.v3);
            // (11, 21, 31) dot (2, 3, 4) = 209
            float t2 = Vector3.Dot(cdt.v3, cdt.vecs.a);
            float sum = t0 + t1 + t2;
            
            Console.WriteLine("    Managed Sum = {0}", sum);
            if ((sum != 735) || (cdt.iv != 100) || (cdt.str.ToString() != "ret_string"))
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

    public delegate void CallBackDelegate_RPInvoke_Vector3Arg_Unix(
        Vector3 v3f32_xmm0, 
        float   f32_xmm2,
        float   f32_xmm3,
        float   f32_xmm4,
        float   f32_xmm5,
        float   f32_xmm6,
        float   f32_xmm7,
        float   f32_mem0,
        Vector3 v3f32_mem1,
        float   f32_mem2,
        float   f32_mem3);

    public delegate void CallBackDelegate_RPInvoke_Vector3Arg_Unix2(
        Vector3 v3f32_xmm0, 
        float   f32_xmm2,
        float   f32_xmm3,
        float   f32_xmm4,
        float   f32_xmm5,
        float   f32_xmm6,
        float   f32_xmm7,
        float   f32_mem0,
        Vector3 v3f32_mem1,
        float   f32_mem2,
        float   f32_mem3,
        Vector3 v3f32_mem4,
        float   f32_mem5);        
        
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
    public static extern void nativeCall_RPInvoke_Vector3Arg_Unix(
        CallBackDelegate_RPInvoke_Vector3Arg_Unix callBack);

    [DllImport(@"Vector3TestNative", CallingConvention = CallingConvention.StdCall)]
    public static extern void nativeCall_RPInvoke_Vector3Arg_Unix2(
        CallBackDelegate_RPInvoke_Vector3Arg_Unix2 callBack);        
        
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
    static float x,y,z;
   
    // Argument pass test
    // Test if the managed side correctly reads 12-byte Vector3 argument from the native side
    // and meet the backend assumption 1 and 2.
    static void callBack_RPInvoke_Vector3Arg(
        int i, 
        Vector3 v1, 
        [MarshalAs(UnmanagedType.LPStr)] string s, 
        Vector3 v2)
    {
        // sum = (1, 2, 3) dot (1, 2, 3) = 14
        float sum0 = Vector3.Dot(v1, v1);   
        // sum = (10, 20, 30) dot (10, 20, 30) = 1400
        float sum1 = Vector3.Dot(v2, v2);        
        // sum = (10, 20, 30) dot (1, 2, 3) = 140
        float sum2 = Vector3.Dot(v2, v1);        
        
        Console.WriteLine("callBack_RPInvoke_Vector3Arg:");
        Console.WriteLine("    iVal {0}", i);
        Console.WriteLine("    Sum0,1,2 = {0}, {1}, {2}", sum0, sum1, sum2);
        Console.WriteLine("    str {0}", s);
        
        result = (sum0 == 14) && (sum1 == 1400) && (sum2 == 140) && (s == "abcdefg") && (i == 123);
    }
    
    // Arugument test for Unix
    // Some arguments are mapped onto stack
    static void callBack_RPInvoke_Vector3Arg_Unix(
        Vector3 v3f32_xmm0, 
        float   f32_xmm2,
        float   f32_xmm3,
        float   f32_xmm4,
        float   f32_xmm5,
        float   f32_xmm6,
        float   f32_xmm7,
        float   f32_mem0,
        Vector3 v3f32_mem0,
        float   f32_mem1,
        float   f32_mem2)
    {
        // sum = (1, 2, 3) dot (1, 2, 3) = 14
        float sum0 = Vector3.Dot(v3f32_xmm0, v3f32_xmm0);   
        // sum = (10, 20, 30) dot (10, 20, 30) = 1400
        float sum1 = Vector3.Dot(v3f32_mem0, v3f32_mem0);       
        // sum = (1, 2, 3) dot (10, 20, 30) = 140
        float sum2 = Vector3.Dot(v3f32_xmm0, v3f32_mem0);      
        // sum = 100 + 101 + 102 + 103 + 104 + 105 + 106 + 107 + 108 = 936
        float sum3 = f32_xmm2 + f32_xmm3 + f32_xmm4 + f32_xmm5 + f32_xmm6 + f32_xmm7
            + f32_mem0 + f32_mem1 + f32_mem2;
        
        Console.WriteLine("callBack_RPInvoke_Vector3Arg_Unix:");
        Console.WriteLine("    {0}, {1}, {2}", v3f32_xmm0.X, v3f32_xmm0.Y, v3f32_xmm0.Z);
        Console.WriteLine("    {0}, {1}, {2}", v3f32_mem0.X, v3f32_mem0.Y, v3f32_mem0.Z);
        Console.WriteLine("    Sum0,1,2,3 = {0}, {1}, {2}, {3}", sum0, sum1, sum2, sum3);
        
        result = (sum0 == 14) && (sum1 == 1400) && (sum2 == 140) && (sum3==936);
    }    

    // Arugument test for Unix
    // Some arguments are mapped onto stack
    static void callBack_RPInvoke_Vector3Arg_Unix2(
        Vector3 v3f32_xmm0, 
        float   f32_xmm2,
        float   f32_xmm3,
        float   f32_xmm4,
        float   f32_xmm5,
        float   f32_xmm6,
        float   f32_xmm7,
        float   f32_mem0,
        Vector3 v3f32_mem0,
        float   f32_mem1,
        float   f32_mem2,
        Vector3 v3f32_mem3,
        float   f32_mem4)
    {
        // sum = (1, 2, 3) dot (1, 2, 3) = 14
        float sum0 = Vector3.Dot(v3f32_xmm0, v3f32_xmm0);   
        // sum = (4, 5, 6) dot (4, 5, 6) = 77
        float sum1 = Vector3.Dot(v3f32_mem0, v3f32_mem0);       
        // sum = (7, 8, 9) dot (7, 8, 9) = 194
        float sum2 = Vector3.Dot(v3f32_mem3, v3f32_mem3);       
        // sum = (1, 2, 3) dot (4, 5, 6) = 32
        float sum3 = Vector3.Dot(v3f32_xmm0, v3f32_mem0);      
        // sum = (4, 5, 6) dot (7, 8, 9) = 122
        float sum4 = Vector3.Dot(v3f32_mem0, v3f32_mem3);      
        
        // sum = 100 + 101 + 102 + 103 + 104 + 105 + 106 + 107 + 108 + 109 = 1045
        float sum5 = f32_xmm2 + f32_xmm3 + f32_xmm4 + f32_xmm5 + f32_xmm6 + f32_xmm7
            + f32_mem0 + f32_mem1 + f32_mem2 + f32_mem4;
        
        Console.WriteLine("callBack_RPInvoke_Vector3Arg_Unix2:");
        Console.WriteLine("    {0}, {1}, {2}", v3f32_xmm0.X, v3f32_xmm0.Y, v3f32_xmm0.Z);
        Console.WriteLine("    {0}, {1}, {2}", v3f32_mem0.X, v3f32_mem0.Y, v3f32_mem0.Z);
        Console.WriteLine("    {0}, {1}, {2}", v3f32_mem3.X, v3f32_mem3.Y, v3f32_mem3.Z);
        Console.WriteLine("    Sum0,1,2,3,4,5 = {0}, {1}, {2}, {3}, {4}, {5}", sum0, sum1, sum2, sum3, sum4, sum5);
        
        result = (sum0 == 14) && (sum1 == 77) && (sum2 == 194) && (sum3 == 32) && (sum4 == 122) && (sum5 == 1045);
    }        
  
    //  Return test.
    static Vector3 callBack_RPInvoke_Vector3Ret()
    {
        Vector3 tmp = new Vector3(1, 2, 3);
        return tmp;
    }        

    // Test if the managed side correctly reads an array of 12-byte Vector3 elements 
    // from the native side and meets the backend assumptions.
    static void callBack_RPInvoke_Vector3Array(
        [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)] Vector3[] v, 
        int size)
    {   
        // sum0 = (2,3,4) dot (2,3,4) = 4 + 9 + 16 = 29
        float sum0 = Vector3.Dot(v[0], v[0]);
        // sum0 = (11,21,31) dot (11,21,31) = 121 + 441 + 961 = 1523
        float sum1 = Vector3.Dot(v[1], v[1]);
        // sum0 = (11,21,31) dot (2,3,4) = 22 + 63 + 124 = 209
        float sum2 = Vector3.Dot(v[0], v[1]);
        
        Console.WriteLine("callBack_RPInvoke_Vector3Array:");
        Console.WriteLine("    Sum0 = {0} Sum1 = {1} Sum2 = {2}", sum0, sum1, sum2);
        
        result = (sum0 == 29) && (sum1 == 1523) && (sum2 == 209);
    }
        
    // Test if the managed side correctly reads 12-byte Vector objects in a struct and
    // meet the backend assumptions.
    static void callBack_RPInvoke_Vector3InStruct(DT v)
    {   
        // sum0 = (2,3,4) dot (2,3,4) = 29
        float sum0 = Vector3.Dot(v.a, v.a);
        // sum1 = (11,21,31) dot (11,21,31) = 22 + 42 + 62 = 1523
        float sum1 = Vector3.Dot(v.b, v.b);
        // sum2 = (2,3,4) dot (11,21,31) = 209
        float sum2 = Vector3.Dot(v.a, v.b);
        
        Console.WriteLine("callBack_RPInvoke_Vector3InStruct:");
        Console.WriteLine("    Sum0 = {0} Sum1 = {1} Sum2 = {2}", sum0, sum1, sum2);
        
        result = (sum0 == 29) && (sum1 == 1523) == (sum2 == 209);
    }
    
    // Complex struct type test
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
        
        nativeCall_RPInvoke_Vector3Arg_Unix(callBack_RPInvoke_Vector3Arg_Unix);
        if (!result) 
        {
            Console.WriteLine("RPInvoke Vector3Arg_Unix test failed");
            return false;            
        }

        nativeCall_RPInvoke_Vector3Arg_Unix2(callBack_RPInvoke_Vector3Arg_Unix2);
        if (!result) 
        {
            Console.WriteLine("RPInvoke Vector3Arg_Unix2 test failed");
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

public class Test_Vector3Interop 
{  
    [Fact]
    public static int TestEntryPoint() 
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
