using System;
using System.Runtime.InteropServices;
using System.Security;

public class Managed
{
    static int failures = 0;
    private static string strOne;
    private static string strTwo;

    enum StructID
    {
        InnerSequentialId,
        InnerArraySequentialId,
        CharSetAnsiSequentialId,
        CharSetUnicodeSequentialId,
        NumberSequentialId,
        S3Id,
        S5Id,
        StringStructSequentialAnsiId,
        StringStructSequentialUnicodeId,
        S8Id,
        S9Id,
        IncludeOuterIntergerStructSequentialId,
        S11Id
    }

    private static void InitialArray(int[] iarr, int[] icarr)
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

    [SecuritySafeCritical]
    private static void testMethod(S9 s9)
    {
        Console.WriteLine("\tThe first field of s9 is:", s9.i32);
    }

    [SecuritySafeCritical]
    public static int Main()
    {
        RunMarshalSeqStructAsParamByVal();
        RunMarshalSeqStructAsParamByRef();
        RunMarshalSeqStructAsParamByValIn();
        RunMarshalSeqStructAsParamByRefIn();
        RunMarshalSeqStructAsParamByValOut();
        RunMarshalSeqStructAsParamByRefOut();
        RunMarshalSeqStructAsParamByValInOut();
        RunMarshalSeqStructAsParamByRefInOut();
        
        if (failures > 0)
        {
            Console.WriteLine("\nTEST FAILED!");
            return 101;
        }
        else
        {
            Console.WriteLine("\nTEST PASSED!");
            return 100;
        }
    }

    #region Struct with Layout Sequential scenario1
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByVal(InnerSequential str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRef(ref InnerSequential str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal")]
    static extern bool MarshalStructAsParam_AsSeqByValIn([In] InnerSequential str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefIn([In] ref InnerSequential str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByValOut([Out] InnerSequential str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefOut(out InnerSequential str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal")]
    static extern bool MarshalStructAsParam_AsSeqByValInOut([In, Out] InnerSequential str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByRef")]
    static extern bool MarshalStructAsParam_AsSeqByRefInOut([In, Out] ref InnerSequential str1);
    #endregion
    #region Struct with Layout Sequential scenario2
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByVal2(InnerArraySequential str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRef2(ref InnerArraySequential str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal2")]
    static extern bool MarshalStructAsParam_AsSeqByValIn2([In] InnerArraySequential str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefIn2([In] ref InnerArraySequential str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByValOut2([Out] InnerArraySequential str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefOut2(out InnerArraySequential str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal2")]
    static extern bool MarshalStructAsParam_AsSeqByValInOut2([In, Out] InnerArraySequential str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByRef2")]
    static extern bool MarshalStructAsParam_AsSeqByRefInOut2([In, Out] ref InnerArraySequential str1);
    #endregion
    #region Struct with Layout Sequential scenario3
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByVal3(CharSetAnsiSequential str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRef3(ref CharSetAnsiSequential str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal3")]
    static extern bool MarshalStructAsParam_AsSeqByValIn3([In] CharSetAnsiSequential str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefIn3([In] ref CharSetAnsiSequential str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByValOut3([Out] CharSetAnsiSequential str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefOut3(out CharSetAnsiSequential str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal3")]
    static extern bool MarshalStructAsParam_AsSeqByValInOut3([In, Out] CharSetAnsiSequential str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByRef3")]
    static extern bool MarshalStructAsParam_AsSeqByRefInOut3([In, Out] ref CharSetAnsiSequential str1);
    #endregion
    #region Struct with Layout Sequential scenario4
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByVal4(CharSetUnicodeSequential str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRef4(ref CharSetUnicodeSequential str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal4")]
    static extern bool MarshalStructAsParam_AsSeqByValIn4([In] CharSetUnicodeSequential str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefIn4([In] ref CharSetUnicodeSequential str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByValOut4([Out] CharSetUnicodeSequential str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefOut4(out CharSetUnicodeSequential str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal4")]
    static extern bool MarshalStructAsParam_AsSeqByValInOut4([In, Out] CharSetUnicodeSequential str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByRef4")]
    static extern bool MarshalStructAsParam_AsSeqByRefInOut4([In, Out] ref CharSetUnicodeSequential str1);
    #endregion
    #region Struct with Layout Sequential scenario5
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByVal6(NumberSequential str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRef6(ref NumberSequential str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal6")]
    static extern bool MarshalStructAsParam_AsSeqByValIn6([In] NumberSequential str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefIn6([In] ref NumberSequential str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByValOut6([Out] NumberSequential str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefOut6(out NumberSequential str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal6")]
    static extern bool MarshalStructAsParam_AsSeqByValInOut6([In, Out] NumberSequential str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByRef6")]
    static extern bool MarshalStructAsParam_AsSeqByRefInOut6([In, Out] ref NumberSequential str1);
    #endregion
    #region Struct with Layout Sequential scenario6
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByVal7(S3 str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRef7(ref S3 str1);
    [DllImport("MarshalStructAsParam",EntryPoint = "MarshalStructAsParam_AsSeqByVal7")]
    static extern bool MarshalStructAsParam_AsSeqByValIn7([In] S3 str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefIn7([In] ref S3 str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefOut7(out S3 str1);
    [DllImport("MarshalStructAsParam",EntryPoint = "MarshalStructAsParam_AsSeqByRef7")]
    static extern bool MarshalStructAsParam_AsSeqByRefInOut7([In,Out] ref S3 str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByValOut7([Out] S3 str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal7")]
    static extern bool MarshalStructAsParam_AsSeqByValInOut7([In, Out] S3 str1);
    #endregion
    #region Struct with Layout Sequential scenario7
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByVal8(S5 str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRef8(ref S5 str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal8")]
    static extern bool MarshalStructAsParam_AsSeqByValIn8([In] S5 str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefIn8([In] ref S5 str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal8")]
    static extern bool MarshalStructAsParam_AsSeqByValOut8([Out] S5 str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefOut8(out S5 str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal8")]
    static extern bool MarshalStructAsParam_AsSeqByValInOut8([In, Out] S5 str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByRef8")]
    static extern bool MarshalStructAsParam_AsSeqByRefInOut8([In, Out] ref S5 str1);
    #endregion
    #region Struct with Layout Sequential scenario8
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByVal9(StringStructSequentialAnsi str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRef9(ref StringStructSequentialAnsi str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal9")]
    static extern bool MarshalStructAsParam_AsSeqByValIn9([In] StringStructSequentialAnsi str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefIn9([In] ref StringStructSequentialAnsi str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByValOut9([Out] StringStructSequentialAnsi str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefOut9(out StringStructSequentialAnsi str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal9")]
    static extern bool MarshalStructAsParam_AsSeqByValInOut9([In, Out] StringStructSequentialAnsi str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByRef9")]
    static extern bool MarshalStructAsParam_AsSeqByRefInOut9([In, Out] ref StringStructSequentialAnsi str1);
    #endregion
    #region Struct with Layout Sequential scenario9
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByVal10(StringStructSequentialUnicode str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRef10(ref StringStructSequentialUnicode str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal10")]
    static extern bool MarshalStructAsParam_AsSeqByValIn10([In] StringStructSequentialUnicode str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefIn10([In] ref StringStructSequentialUnicode str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByValOut10([Out] StringStructSequentialUnicode str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefOut10(out StringStructSequentialUnicode str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal10")]
    static extern bool MarshalStructAsParam_AsSeqByValInOut10([In, Out] StringStructSequentialUnicode str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByRef10")]
    static extern bool MarshalStructAsParam_AsSeqByRefInOut10([In, Out] ref StringStructSequentialUnicode str1);
    #endregion
    #region Struct with Layout Sequential scenario10
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByVal11(S8 str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRef11(ref S8 str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal11")]
    static extern bool MarshalStructAsParam_AsSeqByValIn11([In] S8 str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefIn11([In] ref S8 str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByValOut11([Out] S8 str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefOut11(out S8 str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal11")]
    static extern bool MarshalStructAsParam_AsSeqByValInOut11([In, Out] S8 str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByRef11")]
    static extern bool MarshalStructAsParam_AsSeqByRefInOut11([In, Out] ref S8 str1);
    #endregion
    #region Struct with Layout Sequential scenario11
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByVal12(S9 str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRef12(ref S9 str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal12")]
    static extern bool MarshalStructAsParam_AsSeqByValIn12(S9 str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByRef12")]
    static extern bool MarshalStructAsParam_AsSeqByRefIn12([In] ref S9 str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByValOut12([Out] S9 str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefOut12(out S9 str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal12")]
    static extern bool MarshalStructAsParam_AsSeqByValInOut12([In, Out] S9 str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByRef12")]
    static extern bool MarshalStructAsParam_AsSeqByRefInOut12([In, Out] ref S9 str1);
    #endregion
    #region Struct with Layout Sequential scenario12
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByVal13(IncludeOuterIntergerStructSequential str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRef13(ref IncludeOuterIntergerStructSequential str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal13")]
    static extern bool MarshalStructAsParam_AsSeqByValIn13([In] IncludeOuterIntergerStructSequential str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefIn13([In] ref IncludeOuterIntergerStructSequential str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByValOut13([Out] IncludeOuterIntergerStructSequential str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefOut13(out IncludeOuterIntergerStructSequential str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal13")]
    static extern bool MarshalStructAsParam_AsSeqByValInOut13([In, Out] IncludeOuterIntergerStructSequential str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByRef13")]
    static extern bool MarshalStructAsParam_AsSeqByRefInOut13([In, Out] ref IncludeOuterIntergerStructSequential str1);
    #endregion
    #region Struct with Layout Sequential scenario13
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByVal14(S11 str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRef14(ref S11 str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal14")]
    static extern bool MarshalStructAsParam_AsSeqByValIn14([In] S11 str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefIn14([In] ref S11 str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByValOut14")]
    static extern bool MarshalStructAsParam_AsSeqByValOut14([Out] S11 str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsSeqByRefOut14(out S11 str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByVal14")]
    static extern bool MarshalStructAsParam_AsSeqByValInOut14([In, Out] S11 str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsSeqByRef14")]
    static extern bool MarshalStructAsParam_AsSeqByRefInOut14([In, Out] ref S11 str1);
    #endregion

    #region Marshal struct method in PInvoke
    [SecuritySafeCritical]
    unsafe private static void MarshalStructAsParam_AsSeqByVal(StructID id)
    {
        try
        {
            switch (id)
            {
                case StructID.InnerSequentialId:
                    InnerSequential source_is = Helper.NewInnerSequential(1, 1.0F, "some string");
                    InnerSequential clone_is = Helper.NewInnerSequential(1, 1.0F, "some string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByVal...");
                    if (!MarshalStructAsParam_AsSeqByVal(source_is))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByVal.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerSequential(source_is, clone_is, "MarshalStructAsParam_AsSeqByVal"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.InnerArraySequentialId:
                    InnerArraySequential source_ias = Helper.NewInnerArraySequential(1, 1.0F, "some string");
                    InnerArraySequential clone_ias = Helper.NewInnerArraySequential(1, 1.0F, "some string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByVal2...");
                    if (!MarshalStructAsParam_AsSeqByVal2(source_ias))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByVal2.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerArraySequential(source_ias, clone_ias, "MarshalStructAsParam_AsSeqByVal2"))
                    {
                        failures++;
                    }
                    break; 
                case StructID.CharSetAnsiSequentialId:
                    CharSetAnsiSequential source_csas = Helper.NewCharSetAnsiSequential("some string", 'c');
                    CharSetAnsiSequential clone_csas = Helper.NewCharSetAnsiSequential("some string", 'c');

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByVal3...");
                    if (!MarshalStructAsParam_AsSeqByVal3(source_csas))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByVal3.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateCharSetAnsiSequential(source_csas, clone_csas, "MarshalStructAsParam_AsSeqByVal3"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.CharSetUnicodeSequentialId:
                    CharSetUnicodeSequential source_csus = Helper.NewCharSetUnicodeSequential("some string", 'c');
                    CharSetUnicodeSequential clone_csus = Helper.NewCharSetUnicodeSequential("some string", 'c');

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByVal4...");
                    if (!MarshalStructAsParam_AsSeqByVal4(source_csus))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByVal4.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateCharSetUnicodeSequential(source_csus, clone_csus, "MarshalStructAsParam_AsSeqByVal4"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.NumberSequentialId:
                    NumberSequential source_ns = Helper.NewNumberSequential(Int32.MinValue, UInt32.MaxValue, short.MinValue, ushort.MaxValue, byte.MinValue, sbyte.MaxValue, Int16.MinValue, UInt16.MaxValue, -1234567890, 1234567890, 32.0F, 3.2);
                    NumberSequential clone_ns = Helper.NewNumberSequential(Int32.MinValue, UInt32.MaxValue, short.MinValue, ushort.MaxValue, byte.MinValue, sbyte.MaxValue, Int16.MinValue, UInt16.MaxValue, -1234567890, 1234567890, 32.0F, 3.2);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByVal6...");
                    if (!MarshalStructAsParam_AsSeqByVal6(source_ns))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByVal6.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateNumberSequential(source_ns, clone_ns, "MarshalStructAsParam_AsSeqByVal6"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S3Id:
                    int[] iarr = new int[256];
                    int[] icarr = new int[256];
                    InitialArray(iarr, icarr);

                    S3 sourceS3 = Helper.NewS3(true, "some string", iarr);
                    S3 cloneS3 = Helper.NewS3(true, "some string", iarr);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByVal7...");
                    if (!MarshalStructAsParam_AsSeqByVal7(sourceS3))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByVal7.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS3(sourceS3, cloneS3, "MarshalStructAsParam_AsSeqByVal7"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S5Id:
                    Enum1 enums = Enum1.e1;
                    Enum1 enumcl = Enum1.e1;

                    S5 sourceS5 = Helper.NewS5(32, "some string", enums);
                    S5 cloneS5 = Helper.NewS5(32, "some string", enumcl);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByVal8...");
                    if (!MarshalStructAsParam_AsSeqByVal8(sourceS5))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByVal8.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS5(sourceS5, cloneS5, "MarshalStructAsParam_AsSeqByVal8"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.StringStructSequentialAnsiId:
                    strOne = new String('a', 512);
                    strTwo = new String('b', 512);
                    StringStructSequentialAnsi source_sssa = Helper.NewStringStructSequentialAnsi(strOne, strTwo);
                    StringStructSequentialAnsi clone_sssa = Helper.NewStringStructSequentialAnsi(strOne, strTwo);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByVal9...");
                    if (!MarshalStructAsParam_AsSeqByVal9(source_sssa))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByVal9.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateStringStructSequentialAnsi(source_sssa, clone_sssa, "MarshalStructAsParam_AsSeqByVal9"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.StringStructSequentialUnicodeId:
                    strOne = new String('a', 256);
                    strTwo = new String('b', 256);
                    StringStructSequentialUnicode source_sssu = Helper.NewStringStructSequentialUnicode(strOne, strTwo);
                    StringStructSequentialUnicode clone_sssu = Helper.NewStringStructSequentialUnicode(strOne, strTwo);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByVal10...");
                    if (!MarshalStructAsParam_AsSeqByVal10(source_sssu))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByVal10.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateStringStructSequentialUnicode(source_sssu, clone_sssu, "MarshalStructAsParam_AsSeqByVal10"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S8Id:
                    S8 sourceS8 = Helper.NewS8("hello", true, 10, 128, 128, 32);
                    S8 cloneS8 = Helper.NewS8("hello", true, 10, 128, 128, 32);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByVal11...");
                    if (!MarshalStructAsParam_AsSeqByVal11(sourceS8))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByVal11.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS8(sourceS8, cloneS8, "MarshalStructAsParam_AsSeqByVal11"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S9Id:
                    S9 sourceS9 = Helper.NewS9(128, new TestDelegate1(testMethod));
                    S9 cloneS9 = Helper.NewS9(128, new TestDelegate1(testMethod));

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByVal12...");
                    if (!MarshalStructAsParam_AsSeqByVal12(sourceS9))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByVal12.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS9(sourceS9, cloneS9, "MarshalStructAsParam_AsSeqByVal12"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.IncludeOuterIntergerStructSequentialId:
                    IncludeOuterIntergerStructSequential sourceIncludeOuterIntergerStructSequential = Helper.NewIncludeOuterIntergerStructSequential(32, 32);
                    IncludeOuterIntergerStructSequential cloneIncludeOuterIntergerStructSequential = Helper.NewIncludeOuterIntergerStructSequential(32, 32);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByVal13...");
                    if (!MarshalStructAsParam_AsSeqByVal13(sourceIncludeOuterIntergerStructSequential))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByVal13.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateIncludeOuterIntergerStructSequential(sourceIncludeOuterIntergerStructSequential, cloneIncludeOuterIntergerStructSequential, "MarshalStructAsParam_AsSeqByVal13"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S11Id:
                    S11 sourceS11 = Helper.NewS11((int*)new Int32(), 32);
                    S11 cloneS11 = Helper.NewS11((int*)new Int64(), 32);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByVal14...");
                    if (!MarshalStructAsParam_AsSeqByVal14(sourceS11))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByVal14.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS11(sourceS11, cloneS11, "MarshalStructAsParam_AsSeqByVal14"))
                    {
                        failures++;
                    }
                    break;

                default:
                    Console.WriteLine("\tThere is not the struct id");
                    failures++;
                    break;               
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected Exception:" + e.ToString());
            failures++;
        }

    }

    [SecuritySafeCritical]
    unsafe private static void MarshalStructAsParam_AsSeqByRef(StructID id)
    {
        try
        {
            switch (id)
            {
                case StructID.InnerSequentialId:
                    InnerSequential source_is = Helper.NewInnerSequential(1, 1.0F, "some string");
                    InnerSequential change_is = Helper.NewInnerSequential(77, 77.0F, "changed string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRef...");
                    if (!MarshalStructAsParam_AsSeqByRef(ref source_is))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRef.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerSequential(source_is, change_is, "MarshalStructAsParam_AsSeqByRef"))
                    {
                        failures++;
                    }
                    break; 
                case StructID.InnerArraySequentialId:
                    InnerArraySequential source_ias = Helper.NewInnerArraySequential(1, 1.0F, "some string");
                    InnerArraySequential change_ias = Helper.NewInnerArraySequential(77, 77.0F, "changed string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRef2...");
                    if (!MarshalStructAsParam_AsSeqByRef2(ref source_ias))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRef2.Expected:True;Actual:False");
                    }
                    if (!Helper.ValidateInnerArraySequential(source_ias, change_ias, "MarshalStructAsParam_AsSeqByRef2"))
                    {
                        failures++;
                    }
                    break;
                case StructID.CharSetAnsiSequentialId:
                    CharSetAnsiSequential source_csas = Helper.NewCharSetAnsiSequential("some string", 'c');
                    CharSetAnsiSequential changeStr1 = Helper.NewCharSetAnsiSequential("change string", 'n');

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRef3...");
                    if (!MarshalStructAsParam_AsSeqByRef3(ref source_csas))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRef3.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateCharSetAnsiSequential(source_csas, changeStr1, "MarshalStructAsParam_AsSeqByRef3"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.CharSetUnicodeSequentialId:
                    CharSetUnicodeSequential source_csus = Helper.NewCharSetUnicodeSequential("some string", 'c');
                    CharSetUnicodeSequential change_csus = Helper.NewCharSetUnicodeSequential("change string", 'n');

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRef4...");
                    if (!MarshalStructAsParam_AsSeqByRef4(ref source_csus))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRef4.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateCharSetUnicodeSequential(source_csus, change_csus, "MarshalStructAsParam_AsSeqByRef4"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.NumberSequentialId:
                    NumberSequential source_ns = Helper.NewNumberSequential(Int32.MinValue, UInt32.MaxValue, short.MinValue, ushort.MaxValue, byte.MinValue, sbyte.MaxValue, Int16.MinValue, UInt16.MaxValue, -1234567890, 1234567890, 32.0F, 3.2);
                    NumberSequential change_ns = Helper.NewNumberSequential(0, 32, 0, 16, 0, 8, 0, 16, 0, 64, 64.0F, 6.4);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRef6...");
                    if (!MarshalStructAsParam_AsSeqByRef6(ref source_ns))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRef6.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateNumberSequential(source_ns, change_ns, "MarshalStructAsParam_AsSeqByRef6"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S3Id:
                    int[] iarr = new int[256];
                    int[] icarr = new int[256];
                    InitialArray(iarr, icarr);

                    S3 sourceS3 = Helper.NewS3(true, "some string", iarr);
                    S3 changeS3 = Helper.NewS3(false, "change string", icarr);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRef7...");
                    if (!MarshalStructAsParam_AsSeqByRef7(ref sourceS3))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRef7.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS3(sourceS3, changeS3, "MarshalStructAsParam_AsSeqByRef7"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S5Id:
                    Enum1 enums = Enum1.e1;
                    Enum1 enumch = Enum1.e2;
                    S5 sourceS5 = Helper.NewS5(32, "some string", enums);
                    S5 changeS5 = Helper.NewS5(64, "change string", enumch);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRef8...");
                    if (!MarshalStructAsParam_AsSeqByRef8(ref sourceS5))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRef8.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS5(sourceS5, changeS5, "MarshalStructAsParam_AsSeqByRef8"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.StringStructSequentialAnsiId:
                    strOne = new String('a', 512);
                    strTwo = new String('b', 512);
                    StringStructSequentialAnsi source_sssa = Helper.NewStringStructSequentialAnsi(strOne, strTwo);
                    StringStructSequentialAnsi change_sssa = Helper.NewStringStructSequentialAnsi(strTwo, strOne);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRef9...");
                    if (!MarshalStructAsParam_AsSeqByRef9(ref source_sssa))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRef9.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateStringStructSequentialAnsi(source_sssa, change_sssa, "MarshalStructAsParam_AsSeqByRef9"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.StringStructSequentialUnicodeId:
                    strOne = new String('a', 256);
                    strTwo = new String('b', 256);
                    StringStructSequentialUnicode source_sssu = Helper.NewStringStructSequentialUnicode(strOne, strTwo);
                    StringStructSequentialUnicode change_sssu = Helper.NewStringStructSequentialUnicode(strTwo, strOne);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRef10...");
                    if (!MarshalStructAsParam_AsSeqByRef10(ref source_sssu))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRef10.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateStringStructSequentialUnicode(source_sssu, change_sssu, "MarshalStructAsParam_AsSeqByRef10"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S8Id:
                    S8 sourceS8 = Helper.NewS8("hello", true, 10, 128, 128, 32);
                    S8 changeS8 = Helper.NewS8("world", false, 1, 256, 256, 64);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRef11...");
                    if (!MarshalStructAsParam_AsSeqByRef11(ref sourceS8))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRef11.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS8(sourceS8, changeS8, "MarshalStructAsParam_AsSeqByRef11"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S9Id:
                    S9 sourceS9 = Helper.NewS9(128, new TestDelegate1(testMethod));
                    S9 changeS9 = Helper.NewS9(256, null);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRef12...");
                    if (!MarshalStructAsParam_AsSeqByRef12(ref sourceS9))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRef12.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS9(sourceS9, changeS9, "MarshalStructAsParam_AsSeqByRef12"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.IncludeOuterIntergerStructSequentialId:
                    IncludeOuterIntergerStructSequential sourceIncludeOuterIntergerStructSequential = Helper.NewIncludeOuterIntergerStructSequential(32, 32);
                    IncludeOuterIntergerStructSequential changeIncludeOuterIntergerStructSequential = Helper.NewIncludeOuterIntergerStructSequential(64, 64);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRef13...");
                    if (!MarshalStructAsParam_AsSeqByRef13(ref sourceIncludeOuterIntergerStructSequential))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRef13.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateIncludeOuterIntergerStructSequential(sourceIncludeOuterIntergerStructSequential, changeIncludeOuterIntergerStructSequential, "MarshalStructAsParam_AsSeqByRef13"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S11Id:
                    S11 sourceS11 = Helper.NewS11((int*)new Int32(), 32);
                    S11 changeS11 = Helper.NewS11((int*)(32), 64);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRef14...");
                    if (!MarshalStructAsParam_AsSeqByRef14(ref sourceS11))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRef14.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS11(sourceS11, changeS11, "MarshalStructAsParam_AsSeqByRef14"))
                    {
                        failures++;
                    }
                    break;    
                default:
                    Console.WriteLine("\tThere is not the struct id");
                    failures++;
                    break;    
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected Exception:" + e.ToString());
            failures++;
        }
    }

    [SecuritySafeCritical]
    unsafe private static void MarshalStructAsParam_AsSeqByValIn(StructID id)
    {
        try
        {
            switch (id)
            {
                case StructID.InnerSequentialId:
                    InnerSequential source_is = Helper.NewInnerSequential(1, 1.0F, "some string");
                    InnerSequential clone_is = Helper.NewInnerSequential(1, 1.0F, "some string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValIn...");
                    if (!MarshalStructAsParam_AsSeqByValIn(source_is))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValIn.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerSequential(source_is, clone_is, "MarshalStructAsParam_AsSeqByValIn"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.InnerArraySequentialId:
                    InnerArraySequential source_ias = Helper.NewInnerArraySequential(1, 1.0F, "some string");
                    InnerArraySequential clone_ias = Helper.NewInnerArraySequential(1, 1.0F, "some string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValIn2...");
                    if (!MarshalStructAsParam_AsSeqByValIn2(source_ias))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValIn2.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerArraySequential(source_ias, clone_ias, "MarshalStructAsParam_AsSeqByValIn2"))
                    {
                        failures++;
                    }
                    break; 
                case StructID.CharSetAnsiSequentialId:
                    CharSetAnsiSequential source_csas = Helper.NewCharSetAnsiSequential("some string", 'c');
                    CharSetAnsiSequential clone_csas = Helper.NewCharSetAnsiSequential("some string", 'c');

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValIn3...");
                    if (!MarshalStructAsParam_AsSeqByValIn3(source_csas))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValIn3.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateCharSetAnsiSequential(source_csas, clone_csas, "MarshalStructAsParam_AsSeqByValIn3"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.CharSetUnicodeSequentialId:
                    CharSetUnicodeSequential source_csus = Helper.NewCharSetUnicodeSequential("some string", 'c');
                    CharSetUnicodeSequential clone_csus = Helper.NewCharSetUnicodeSequential("some string", 'c');

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValIn4...");
                    if (!MarshalStructAsParam_AsSeqByValIn4(source_csus))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValIn4.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateCharSetUnicodeSequential(source_csus, clone_csus, "MarshalStructAsParam_AsSeqByValIn4"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.NumberSequentialId:
                    NumberSequential source_ns = Helper.NewNumberSequential(Int32.MinValue, UInt32.MaxValue, short.MinValue, ushort.MaxValue, byte.MinValue, sbyte.MaxValue, Int16.MinValue, UInt16.MaxValue, -1234567890, 1234567890, 32.0F, 3.2);
                    NumberSequential clone_ns = Helper.NewNumberSequential(Int32.MinValue, UInt32.MaxValue, short.MinValue, ushort.MaxValue, byte.MinValue, sbyte.MaxValue, Int16.MinValue, UInt16.MaxValue, -1234567890, 1234567890, 32.0F, 3.2);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValIn6...");
                    if (!MarshalStructAsParam_AsSeqByValIn6(source_ns))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValIn6.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateNumberSequential(source_ns, clone_ns, "MarshalStructAsParam_AsSeqByValIn6"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S3Id:
                    int[] iarr = new int[256];
                    int[] icarr = new int[256];
                    InitialArray(iarr, icarr);

                    S3 sourceS3 = Helper.NewS3(true, "some string", iarr);
                    S3 cloneS3 = Helper.NewS3(true, "some string", iarr);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValIn7...");
                    if (!MarshalStructAsParam_AsSeqByValIn7(sourceS3))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValIn7.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS3(sourceS3, cloneS3, "MarshalStructAsParam_AsSeqByValIn7"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S5Id:
                    Enum1 enums = Enum1.e1;
                    Enum1 enumcl = Enum1.e1;
                    S5 sourceS5 = Helper.NewS5(32, "some string", enums);
                    S5 cloneS5 = Helper.NewS5(32, "some string", enumcl);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValIn8...");
                    if (!MarshalStructAsParam_AsSeqByValIn8(sourceS5))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValIn8.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS5(sourceS5, cloneS5, "MarshalStructAsParam_AsSeqByValIn8"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.StringStructSequentialAnsiId:
                    strOne = new String('a', 512);
                    strTwo = new String('b', 512);
                    StringStructSequentialAnsi source_sssa = Helper.NewStringStructSequentialAnsi(strOne, strTwo);
                    StringStructSequentialAnsi clone_sssa = Helper.NewStringStructSequentialAnsi(strOne, strTwo);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValIn9...");
                    if (!MarshalStructAsParam_AsSeqByValIn9(source_sssa))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValIn9.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateStringStructSequentialAnsi(source_sssa, clone_sssa, "MarshalStructAsParam_AsSeqByValIn9"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.StringStructSequentialUnicodeId:
                    strOne = new String('a', 256);
                    strTwo = new String('b', 256);
                    StringStructSequentialUnicode source_sssu = Helper.NewStringStructSequentialUnicode(strOne, strTwo);
                    StringStructSequentialUnicode clone_sssu = Helper.NewStringStructSequentialUnicode(strOne, strTwo);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValIn10...");
                    if (!MarshalStructAsParam_AsSeqByValIn10(source_sssu))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValIn10.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateStringStructSequentialUnicode(source_sssu, clone_sssu, "MarshalStructAsParam_AsSeqByValIn10"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S8Id:
                    S8 sourceS8 = Helper.NewS8("hello", true, 10, 128, 128, 32);
                    S8 cloneS8 = Helper.NewS8("hello", true, 10, 128, 128, 32);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValIn11...");
                    if (!MarshalStructAsParam_AsSeqByValIn11(sourceS8))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValIn11.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS8(sourceS8, cloneS8, "MarshalStructAsParam_AsSeqByValIn11"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S9Id:
                    S9 sourceS9 = Helper.NewS9(128, new TestDelegate1(testMethod));
                    S9 cloneS9 = Helper.NewS9(128, new TestDelegate1(testMethod));

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValIn12...");
                    if (!MarshalStructAsParam_AsSeqByValIn12(sourceS9))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValIn12.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS9(sourceS9, cloneS9, "MarshalStructAsParam_AsSeqByValIn12"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.IncludeOuterIntergerStructSequentialId:
                    IncludeOuterIntergerStructSequential sourceIncludeOuterIntergerStructSequential = Helper.NewIncludeOuterIntergerStructSequential(32, 32);
                    IncludeOuterIntergerStructSequential cloneIncludeOuterIntergerStructSequential = Helper.NewIncludeOuterIntergerStructSequential(32, 32);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValIn13...");
                    if (!MarshalStructAsParam_AsSeqByValIn13(sourceIncludeOuterIntergerStructSequential))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValIn13.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateIncludeOuterIntergerStructSequential(sourceIncludeOuterIntergerStructSequential, cloneIncludeOuterIntergerStructSequential, "MarshalStructAsParam_AsSeqByValIn13"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S11Id:
                    S11 sourceS11 = Helper.NewS11((int*)new Int32(), 32);
                    S11 cloneS11 = Helper.NewS11((int*)new Int64(), 32);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValIn14...");
                    if (!MarshalStructAsParam_AsSeqByValIn14(sourceS11))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValIn14.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS11(sourceS11, cloneS11, "MarshalStructAsParam_AsSeqByValIn14"))
                    {
                        failures++;
                    }
                    break;

                default:
                    Console.WriteLine("\tThere is not the struct id");
                    failures++;
                    break;    
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected Exception:" + e.ToString());
            failures++;
        }
    }

    [SecuritySafeCritical]
    unsafe private static void MarshalStructAsParam_AsSeqByRefIn(StructID id)
    {
        try
        {
            switch (id)
            {
                case StructID.InnerSequentialId:
                    InnerSequential source_is = Helper.NewInnerSequential(1, 1.0F, "some string");
                    InnerSequential clone_is = Helper.NewInnerSequential(1, 1.0F, "some string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefIn...");
                    if (!MarshalStructAsParam_AsSeqByRefIn(ref source_is))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefIn.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerSequential(source_is, clone_is, "MarshalStructAsParam_AsSeqByRefIn"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.InnerArraySequentialId:
                    InnerArraySequential source_ias = Helper.NewInnerArraySequential(1, 1.0F, "some string");
                    InnerArraySequential clone_ias = Helper.NewInnerArraySequential(1, 1.0F, "some string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefIn2...");
                    if (!MarshalStructAsParam_AsSeqByRefIn2(ref source_ias))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefIn2.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerArraySequential(source_ias, clone_ias, "MarshalStructAsParam_AsSeqByRefIn2"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.CharSetAnsiSequentialId:
                    CharSetAnsiSequential source_csas = Helper.NewCharSetAnsiSequential("some string", 'c');
                    CharSetAnsiSequential clone_csas = Helper.NewCharSetAnsiSequential("some string", 'c');

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefIn3...");
                    if (!MarshalStructAsParam_AsSeqByRefIn3(ref source_csas))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefIn3.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateCharSetAnsiSequential(source_csas, clone_csas, "MarshalStructAsParam_AsSeqByRefIn3"))
                    {
                        failures++;
                    }
                    break; 
                case StructID.CharSetUnicodeSequentialId:
                    CharSetUnicodeSequential source_csus = Helper.NewCharSetUnicodeSequential("some string", 'c');
                    CharSetUnicodeSequential clone_csus = Helper.NewCharSetUnicodeSequential("some string", 'c');

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefIn4...");
                    if (!MarshalStructAsParam_AsSeqByRefIn4(ref source_csus))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefIn4.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateCharSetUnicodeSequential(source_csus, clone_csus, "MarshalStructAsParam_AsSeqByRefIn4"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.NumberSequentialId:
                    NumberSequential source_ns = Helper.NewNumberSequential(Int32.MinValue, UInt32.MaxValue, short.MinValue, ushort.MaxValue, byte.MinValue, sbyte.MaxValue, Int16.MinValue, UInt16.MaxValue, -1234567890, 1234567890, 32.0F, 3.2);
                    NumberSequential change_ns = Helper.NewNumberSequential(0, 32, 0, 16, 0, 8, 0, 16, 0, 64, 64.0F, 6.4);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefIn6...");
                    if (!MarshalStructAsParam_AsSeqByRefIn6(ref source_ns))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefIn6.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateNumberSequential(source_ns, change_ns, "MarshalStructAsParam_AsSeqByRefIn6"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S3Id:
                    int[] iarr = new int[256];
                    int[] icarr = new int[256];
                    InitialArray(iarr, icarr);

                    S3 sourceS3 = Helper.NewS3(true, "some string", iarr);
                    S3 cloneS3 = Helper.NewS3(true, "some string", iarr);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefIn7...");
                    if (!MarshalStructAsParam_AsSeqByRefIn7(ref sourceS3))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefIn7.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS3(sourceS3, cloneS3, "MarshalStructAsParam_AsSeqByRefIn7"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S5Id:
                    Enum1 enums = Enum1.e1;
                    Enum1 enumcl = Enum1.e1;
                    S5 sourceS5 = Helper.NewS5(32, "some string", enums);
                    S5 cloneS5 = Helper.NewS5(32, "some string", enumcl);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefIn8...");
                    if (!MarshalStructAsParam_AsSeqByRefIn8(ref sourceS5))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefIn8.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS5(sourceS5, cloneS5, "MarshalStructAsParam_AsSeqByRefIn8"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.StringStructSequentialAnsiId:
                    strOne = new String('a', 512);
                    strTwo = new String('b', 512);
                    StringStructSequentialAnsi source_sssa = Helper.NewStringStructSequentialAnsi(strOne, strTwo);
                    StringStructSequentialAnsi clone_sssa = Helper.NewStringStructSequentialAnsi(strOne, strTwo);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefIn9...");
                    if (!MarshalStructAsParam_AsSeqByRefIn9(ref source_sssa))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefIn9.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateStringStructSequentialAnsi(source_sssa, clone_sssa, "MarshalStructAsParam_AsSeqByRefIn9"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.StringStructSequentialUnicodeId:
                    strOne = new String('a', 256);
                    strTwo = new String('b', 256);
                    StringStructSequentialUnicode source_sssu = Helper.NewStringStructSequentialUnicode(strOne, strTwo);
                    StringStructSequentialUnicode clone_sssu = Helper.NewStringStructSequentialUnicode(strOne, strTwo);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefIn10...");
                    if (!MarshalStructAsParam_AsSeqByRefIn10(ref source_sssu))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefIn10.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateStringStructSequentialUnicode(source_sssu, clone_sssu, "MarshalStructAsParam_AsSeqByRefIn10"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S8Id:
                    S8 sourceS8 = Helper.NewS8("hello", true, 10, 128, 128, 32);
                    S8 cloneS8 = Helper.NewS8("hello", true, 10, 128, 128, 32);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefIn11...");
                    if (!MarshalStructAsParam_AsSeqByRefIn11(ref sourceS8))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefIn11.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS8(sourceS8, cloneS8, "MarshalStructAsParam_AsSeqByRefIn11"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S9Id:
                    S9 sourceS9 = Helper.NewS9(128, new TestDelegate1(testMethod));
                    S9 cloneS9 = Helper.NewS9(128, new TestDelegate1(testMethod));

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefIn12...");
                    if (!MarshalStructAsParam_AsSeqByRefIn12(ref sourceS9))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefIn12.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS9(sourceS9, cloneS9, "MarshalStructAsParam_AsSeqByRefIn12"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.IncludeOuterIntergerStructSequentialId:
                    IncludeOuterIntergerStructSequential sourceIncludeOuterIntergerStructSequential = Helper.NewIncludeOuterIntergerStructSequential(32, 32);
                    IncludeOuterIntergerStructSequential changeIncludeOuterIntergerStructSequential = Helper.NewIncludeOuterIntergerStructSequential(64, 64);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefIn13...");
                    if (!MarshalStructAsParam_AsSeqByRefIn13(ref sourceIncludeOuterIntergerStructSequential))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefIn13.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateIncludeOuterIntergerStructSequential(sourceIncludeOuterIntergerStructSequential, changeIncludeOuterIntergerStructSequential, "MarshalStructAsParam_AsSeqByRefIn13"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S11Id:
                    S11 sourceS11 = Helper.NewS11((int*)new Int32(), 32);
                    S11 changeS11 = Helper.NewS11((int*)(32), 64);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefIn14...");
                    if (!MarshalStructAsParam_AsSeqByRefIn14(ref sourceS11))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefIn14.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS11(sourceS11, changeS11, "MarshalStructAsParam_AsSeqByRefIn14"))
                    {
                        failures++;
                    }
                    break;    
                default:
                    Console.WriteLine("\tThere is not the struct id");
                    failures++;
                    break;    
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected Exception:" + e.ToString());
            failures++;
        }
    }

    [SecuritySafeCritical]
    unsafe private static void MarshalStructAsParam_AsSeqByValOut(StructID id)
    {
        try
        {
            switch (id)
            {
                case StructID.InnerSequentialId:
                    InnerSequential source_is = Helper.NewInnerSequential(1, 1.0F, "some string");
                    InnerSequential clone_is = Helper.NewInnerSequential(1, 1.0F, "some string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValOut...");
                    if (!MarshalStructAsParam_AsSeqByValOut(source_is))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValOut.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerSequential(source_is, clone_is, "MarshalStructAsParam_AsSeqByValOut"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.InnerArraySequentialId:
                    InnerArraySequential source_ias = Helper.NewInnerArraySequential(1, 1.0F, "some string");
                    InnerArraySequential clone_ias = Helper.NewInnerArraySequential(1, 1.0F, "some string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValOut2...");
                    if (!MarshalStructAsParam_AsSeqByValOut2(source_ias))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValOut2.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerArraySequential(source_ias, clone_ias, "MarshalStructAsParam_AsSeqByValOut2"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.CharSetAnsiSequentialId:
                    CharSetAnsiSequential source_csas = Helper.NewCharSetAnsiSequential("some string", 'c');
                    CharSetAnsiSequential clone_csas = Helper.NewCharSetAnsiSequential("some string", 'c');

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValOut3...");
                    if (!MarshalStructAsParam_AsSeqByValOut3(source_csas))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValOut3.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateCharSetAnsiSequential(source_csas, clone_csas, "MarshalStructAsParam_AsSeqByValOut3"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.CharSetUnicodeSequentialId:
                    CharSetUnicodeSequential source_csus = Helper.NewCharSetUnicodeSequential("some string", 'c');
                    CharSetUnicodeSequential clone_csus = Helper.NewCharSetUnicodeSequential("some string", 'c');

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValOut4...");
                    if (!MarshalStructAsParam_AsSeqByValOut4(source_csus))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValOut4.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateCharSetUnicodeSequential(source_csus, clone_csus, "MarshalStructAsParam_AsSeqByValOut4"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.NumberSequentialId:
                    NumberSequential source_ns = Helper.NewNumberSequential(Int32.MinValue, UInt32.MaxValue, short.MinValue, ushort.MaxValue, byte.MinValue, sbyte.MaxValue, Int16.MinValue, UInt16.MaxValue, -1234567890, 1234567890, 32.0F, 3.2);
                    NumberSequential clone_ns = Helper.NewNumberSequential(Int32.MinValue, UInt32.MaxValue, short.MinValue, ushort.MaxValue, byte.MinValue, sbyte.MaxValue, Int16.MinValue, UInt16.MaxValue, -1234567890, 1234567890, 32.0F, 3.2);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValOut6...");
                    if (!MarshalStructAsParam_AsSeqByValOut6(source_ns))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValOut6.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateNumberSequential(source_ns, clone_ns, "MarshalStructAsParam_AsSeqByValOut6"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S3Id:
                    int[] iarr = new int[256];
                    int[] icarr = new int[256];
                    InitialArray(iarr, icarr);

                    S3 sourceS3 = Helper.NewS3(true, "some string", iarr);
                    S3 cloneS3 = Helper.NewS3(true, "some string", iarr);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValOut7...");
                    if (!MarshalStructAsParam_AsSeqByValOut7(sourceS3))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValOut7.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS3(sourceS3, cloneS3, "MarshalStructAsParam_AsSeqByValOut7"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S5Id:
                    Enum1 enums = Enum1.e1;
                    Enum1 enumcl = Enum1.e1;
                    S5 sourceS5 = Helper.NewS5(32, "some string", enums);
                    S5 cloneS5 = Helper.NewS5(32, "some string", enumcl);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValOut8...");
                    if (!MarshalStructAsParam_AsSeqByValOut8(sourceS5))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValOut8.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS5(sourceS5, cloneS5, "MarshalStructAsParam_AsSeqByValOut8"))
                    {
                        failures++;
                    }
                    break;        
                case StructID.StringStructSequentialAnsiId:
                    strOne = new String('a', 512);
                    strTwo = new String('b', 512);
                    StringStructSequentialAnsi source_sssa = Helper.NewStringStructSequentialAnsi(strOne, strTwo);
                    StringStructSequentialAnsi clone_sssa = Helper.NewStringStructSequentialAnsi(strOne, strTwo);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValOut9...");
                    if (!MarshalStructAsParam_AsSeqByValOut9(source_sssa))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValOut9.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateStringStructSequentialAnsi(source_sssa, clone_sssa, "MarshalStructAsParam_AsSeqByValOut9"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.StringStructSequentialUnicodeId:
                    strOne = new String('a', 256);
                    strTwo = new String('b', 256);
                    StringStructSequentialUnicode source_sssu = Helper.NewStringStructSequentialUnicode(strOne, strTwo);
                    StringStructSequentialUnicode clone_sssu = Helper.NewStringStructSequentialUnicode(strOne, strTwo);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValOut10...");
                    if (!MarshalStructAsParam_AsSeqByValOut10(source_sssu))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValOut10.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateStringStructSequentialUnicode(source_sssu, clone_sssu, "MarshalStructAsParam_AsSeqByValOut10"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S8Id:
                    S8 sourceS8 = Helper.NewS8("hello", true, 10, 128, 128, 32);
                    S8 cloneS8 = Helper.NewS8("hello", true, 10, 128, 128, 32);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValOut11...");
                    if (!MarshalStructAsParam_AsSeqByValOut11(sourceS8))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValOut11.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS8(sourceS8, cloneS8, "MarshalStructAsParam_AsSeqByValOut11"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S9Id:
                    S9 sourceS9 = Helper.NewS9(128, new TestDelegate1(testMethod));
                    S9 cloneS9 = Helper.NewS9(128, new TestDelegate1(testMethod));

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValOut12...");
                    if (!MarshalStructAsParam_AsSeqByValOut12(sourceS9))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValOut12.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS9(sourceS9, cloneS9, "MarshalStructAsParam_AsSeqByValOut12"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.IncludeOuterIntergerStructSequentialId:
                    IncludeOuterIntergerStructSequential sourceIncludeOuterIntergerStructSequential = Helper.NewIncludeOuterIntergerStructSequential(32, 32);
                    IncludeOuterIntergerStructSequential cloneIncludeOuterIntergerStructSequential = Helper.NewIncludeOuterIntergerStructSequential(32, 32);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValOut13...");
                    if (!MarshalStructAsParam_AsSeqByValOut13(sourceIncludeOuterIntergerStructSequential))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValOut13.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateIncludeOuterIntergerStructSequential(sourceIncludeOuterIntergerStructSequential, cloneIncludeOuterIntergerStructSequential, "MarshalStructAsParam_AsSeqByValOut13"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S11Id:
                    S11 sourceS11 = Helper.NewS11((int*)32, 32);
                    S11 cloneS11 = Helper.NewS11((int*)32, 32);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValOut14...");
                    if (!MarshalStructAsParam_AsSeqByValOut14(sourceS11))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValOut14.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS11(sourceS11, cloneS11, "MarshalStructAsParam_AsSeqByValOut14"))
                    {
                        failures++;
                    }
                    break;   
                default:
                    Console.WriteLine("\tThere is not the struct id");
                    failures++;
                    break;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected Exception:" + e.ToString());
            failures++;
        }

    }

    [SecuritySafeCritical]
    unsafe private static void MarshalStructAsParam_AsSeqByRefOut(StructID id)
    {
        try
        {
            switch (id)
            {
                case StructID.InnerSequentialId:
                    InnerSequential source_is = Helper.NewInnerSequential(1, 1.0F, "some string");
                    InnerSequential change_is = Helper.NewInnerSequential(77, 77.0F, "changed string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefOut...");
                    if (!MarshalStructAsParam_AsSeqByRefOut(out source_is))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefOut.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerSequential(source_is, change_is, "MarshalStructAsParam_AsSeqByRefOut"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.InnerArraySequentialId:
                    InnerArraySequential source_ias = Helper.NewInnerArraySequential(1, 1.0F, "some string");
                    InnerArraySequential change_ias = Helper.NewInnerArraySequential(77, 77.0F, "changed string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefOut2...");
                    if (!MarshalStructAsParam_AsSeqByRefOut2(out source_ias))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefOut2.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerArraySequential(source_ias, change_ias, "MarshalStructAsParam_AsSeqByRefOut2"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.CharSetAnsiSequentialId:
                    CharSetAnsiSequential source_csas = Helper.NewCharSetAnsiSequential("some string", 'c');
                    CharSetAnsiSequential changeStr1 = Helper.NewCharSetAnsiSequential("change string", 'n');

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefOut3...");
                    if (!MarshalStructAsParam_AsSeqByRefOut3(out source_csas))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefOut3.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateCharSetAnsiSequential(source_csas, changeStr1, "MarshalStructAsParam_AsSeqByRefOut3"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.CharSetUnicodeSequentialId:
                    CharSetUnicodeSequential source_csus = Helper.NewCharSetUnicodeSequential("some string", 'c');
                    CharSetUnicodeSequential change_csus = Helper.NewCharSetUnicodeSequential("change string", 'n');

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefOut4...");
                    if (!MarshalStructAsParam_AsSeqByRefOut4(out source_csus))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefOut4.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateCharSetUnicodeSequential(source_csus, change_csus, "MarshalStructAsParam_AsSeqByRefOut4"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.NumberSequentialId:
                    NumberSequential source_ns = Helper.NewNumberSequential(Int32.MinValue, UInt32.MaxValue, short.MinValue, ushort.MaxValue, byte.MinValue, sbyte.MaxValue, Int16.MinValue, UInt16.MaxValue, -1234567890, 1234567890, 32.0F, 3.2);
                    NumberSequential change_ns = Helper.NewNumberSequential(0, 32, 0, 16, 0, 8, 0, 16, 0, 64, 64.0F, 6.4);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefOut6...");
                    if (!MarshalStructAsParam_AsSeqByRefOut6(out source_ns))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefOut6.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateNumberSequential(source_ns, change_ns, "MarshalStructAsParam_AsSeqByRefOut6"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S3Id:
                    int[] iarr = new int[256];
                    int[] icarr = new int[256];
                    InitialArray(iarr, icarr);

                    S3 sourceS3 = Helper.NewS3(true, "some string", iarr);
                    S3 changeS3 = Helper.NewS3(false, "change string", icarr);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefOut7...");
                    if (!MarshalStructAsParam_AsSeqByRefOut7(out sourceS3))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefOut7.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS3(sourceS3, changeS3, "MarshalStructAsParam_AsSeqByRefOut7"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S5Id:
                    Enum1 enums = Enum1.e1;
                    Enum1 enumch = Enum1.e2;
                    S5 sourceS5 = Helper.NewS5(32, "some string", enums);
                    S5 changeS5 = Helper.NewS5(64, "change string", enumch);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefOut8...");
                    if (!MarshalStructAsParam_AsSeqByRefOut8(out sourceS5))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefOut8.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS5(sourceS5, changeS5, "MarshalStructAsParam_AsSeqByRefOut8"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.StringStructSequentialAnsiId:
                    strOne = new String('a', 512);
                    strTwo = new String('b', 512);
                    StringStructSequentialAnsi source_sssa = Helper.NewStringStructSequentialAnsi(strOne, strTwo);
                    StringStructSequentialAnsi change_sssa = Helper.NewStringStructSequentialAnsi(strTwo, strOne);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefOut9...");
                    if (!MarshalStructAsParam_AsSeqByRefOut9(out source_sssa))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefOut9.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateStringStructSequentialAnsi(source_sssa, change_sssa, "MarshalStructAsParam_AsSeqByRefOut9"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.StringStructSequentialUnicodeId:
                    strOne = new String('a', 256);
                    strTwo = new String('b', 256);
                    StringStructSequentialUnicode source_sssu = Helper.NewStringStructSequentialUnicode(strOne, strTwo);
                    StringStructSequentialUnicode change_sssu = Helper.NewStringStructSequentialUnicode(strTwo, strOne);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefOut10...");
                    if (!MarshalStructAsParam_AsSeqByRefOut10(out source_sssu))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefOut10.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateStringStructSequentialUnicode(source_sssu, change_sssu, "MarshalStructAsParam_AsSeqByRefOut10"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S8Id:
                    S8 sourceS8 = Helper.NewS8("hello", true, 10, 128, 128, 32);
                    S8 changeS8 = Helper.NewS8("world", false, 1, 256, 256, 64);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefOut11...");
                    if (!MarshalStructAsParam_AsSeqByRefOut11(out sourceS8))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefOut11.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS8(sourceS8, changeS8, "MarshalStructAsParam_AsSeqByRefOut11"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S9Id:
                    S9 sourceS9 = Helper.NewS9(128, new TestDelegate1(testMethod));

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefOut12...");
                    if (!MarshalStructAsParam_AsSeqByRefOut12(out sourceS9))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefOut12.Expected:True;Actual:False");
                        failures++;
                    }
                    else if (sourceS9.i32 != 256 || sourceS9.myDelegate1 == null)
                    {
                        Console.WriteLine("\tFAILED! Native to Managed failed in MarshalStructAsParam_AsSeqByRefOut12.");
                        failures++;
                    }
                    else
                    {
                        Console.WriteLine("\tPASSED!");
                    }
                    break;    
                case StructID.IncludeOuterIntergerStructSequentialId:
                    IncludeOuterIntergerStructSequential sourceIncludeOuterIntergerStructSequential = Helper.NewIncludeOuterIntergerStructSequential(32, 32);
                    IncludeOuterIntergerStructSequential changeIncludeOuterIntergerStructSequential = Helper.NewIncludeOuterIntergerStructSequential(64, 64);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefOut13...");
                    if (!MarshalStructAsParam_AsSeqByRefOut13(out sourceIncludeOuterIntergerStructSequential))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefOut13.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateIncludeOuterIntergerStructSequential(sourceIncludeOuterIntergerStructSequential, changeIncludeOuterIntergerStructSequential, "MarshalStructAsParam_AsSeqByRefOut13"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S11Id:
                    S11 sourceS11 = Helper.NewS11((int*)new Int32(), 32);
                    S11 changeS11 = Helper.NewS11((int*)(32), 64);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefOut14...");
                    if (!MarshalStructAsParam_AsSeqByRefOut14(out sourceS11))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefOut14.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS11(sourceS11, changeS11, "MarshalStructAsParam_AsSeqByRefOut14"))
                    {
                        failures++;
                    }
                    break;  
                default:
                    Console.WriteLine("\tThere is not the struct id");
                    failures++;
                    break;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected Exception:" + e.ToString());
            failures++;
        }
    }

    [SecuritySafeCritical]
    unsafe private static void MarshalStructAsParam_AsSeqByValInOut(StructID id)
    {
        try
        {
            switch (id)
            {
                case StructID.InnerSequentialId:
                    InnerSequential source_is = Helper.NewInnerSequential(1, 1.0F, "some string");
                    InnerSequential clone_is = Helper.NewInnerSequential(1, 1.0F, "some string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValInOut...");
                    if (!MarshalStructAsParam_AsSeqByValInOut(source_is))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValInOut.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerSequential(source_is, clone_is, "MarshalStructAsParam_AsSeqByValInOut"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.InnerArraySequentialId:
                    InnerArraySequential source_ias = Helper.NewInnerArraySequential(1, 1.0F, "some string");
                    InnerArraySequential clone_ias = Helper.NewInnerArraySequential(1, 1.0F, "some string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValInOut2...");
                    if (!MarshalStructAsParam_AsSeqByValInOut2(source_ias))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValInOut2.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerArraySequential(source_ias, clone_ias, "MarshalStructAsParam_AsSeqByValInOut2"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.CharSetAnsiSequentialId:
                    CharSetAnsiSequential source_csas = Helper.NewCharSetAnsiSequential("some string", 'c');
                    CharSetAnsiSequential clone_csas = Helper.NewCharSetAnsiSequential("some string", 'c');

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValInOut3...");
                    if (!MarshalStructAsParam_AsSeqByValInOut3(source_csas))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValInOut3.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateCharSetAnsiSequential(source_csas, clone_csas, "MarshalStructAsParam_AsSeqByValInOut3"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.CharSetUnicodeSequentialId:
                    CharSetUnicodeSequential source_csus = Helper.NewCharSetUnicodeSequential("some string", 'c');
                    CharSetUnicodeSequential clone_csus = Helper.NewCharSetUnicodeSequential("some string", 'c');

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValInOut4...");
                    if (!MarshalStructAsParam_AsSeqByValInOut4(source_csus))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValInOut4.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateCharSetUnicodeSequential(source_csus, clone_csus, "MarshalStructAsParam_AsSeqByValInOut4"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.NumberSequentialId:
                    NumberSequential source_ns = Helper.NewNumberSequential(Int32.MinValue, UInt32.MaxValue, short.MinValue, ushort.MaxValue, byte.MinValue, sbyte.MaxValue, Int16.MinValue, UInt16.MaxValue, -1234567890, 1234567890, 32.0F, 3.2);
                    NumberSequential clone_ns = Helper.NewNumberSequential(Int32.MinValue, UInt32.MaxValue, short.MinValue, ushort.MaxValue, byte.MinValue, sbyte.MaxValue, Int16.MinValue, UInt16.MaxValue, -1234567890, 1234567890, 32.0F, 3.2);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValInOut6...");
                    if (!MarshalStructAsParam_AsSeqByValInOut6(source_ns))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValInOut6.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateNumberSequential(source_ns, clone_ns, "MarshalStructAsParam_AsSeqByValInOut6"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S3Id:
                    int[] iarr = new int[256];
                    int[] icarr = new int[256];
                    InitialArray(iarr, icarr);

                    S3 sourceS3 = Helper.NewS3(true, "some string", iarr);
                    S3 cloneS3 = Helper.NewS3(true, "some string", iarr);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValInOut7...");
                    if (!MarshalStructAsParam_AsSeqByValInOut7(sourceS3))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValInOut7.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS3(sourceS3, cloneS3, "MarshalStructAsParam_AsSeqByValInOut7"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S5Id:
                    Enum1 enums = Enum1.e1;
                    Enum1 enumcl = Enum1.e1;
                    S5 sourceS5 = Helper.NewS5(32, "some string", enums);
                    S5 cloneS5 = Helper.NewS5(32, "some string", enumcl);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValInOut8...");
                    if (!MarshalStructAsParam_AsSeqByValInOut8(sourceS5))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValInOut8.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS5(sourceS5, cloneS5, "MarshalStructAsParam_AsSeqByValInOut8"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.StringStructSequentialAnsiId:
                    strOne = new String('a', 512);
                    strTwo = new String('b', 512);
                    StringStructSequentialAnsi source_sssa = Helper.NewStringStructSequentialAnsi(strOne, strTwo);
                    StringStructSequentialAnsi clone_sssa = Helper.NewStringStructSequentialAnsi(strOne, strTwo);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValInOut9...");
                    if (!MarshalStructAsParam_AsSeqByValInOut9(source_sssa))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValInOut9.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateStringStructSequentialAnsi(source_sssa, clone_sssa, "MarshalStructAsParam_AsSeqByValInOut9"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.StringStructSequentialUnicodeId:
                    strOne = new String('a', 256);
                    strTwo = new String('b', 256);
                    StringStructSequentialUnicode source_sssu = Helper.NewStringStructSequentialUnicode(strOne, strTwo);
                    StringStructSequentialUnicode clone_sssu = Helper.NewStringStructSequentialUnicode(strOne, strTwo);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValInOut10...");
                    if (!MarshalStructAsParam_AsSeqByValInOut10(source_sssu))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValInOut10.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateStringStructSequentialUnicode(source_sssu, clone_sssu, "MarshalStructAsParam_AsSeqByValInOut10"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S8Id:
                    S8 sourceS8 = Helper.NewS8("hello", true, 10, 128, 128, 32);
                    S8 cloneS8 = Helper.NewS8("hello", true, 10, 128, 128, 32);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValInOut11...");
                    if (!MarshalStructAsParam_AsSeqByValInOut11(sourceS8))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValInOut11.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS8(sourceS8, cloneS8, "MarshalStructAsParam_AsSeqByValInOut11"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S9Id:
                    S9 sourceS9 = Helper.NewS9(128, new TestDelegate1(testMethod));
                    S9 cloneS9 = Helper.NewS9(128, new TestDelegate1(testMethod));

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValInOut12...");
                    if (!MarshalStructAsParam_AsSeqByValInOut12(sourceS9))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValInOut12.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS9(sourceS9, cloneS9, "MarshalStructAsParam_AsSeqByValInOut12"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.IncludeOuterIntergerStructSequentialId:
                    IncludeOuterIntergerStructSequential sourceIncludeOuterIntergerStructSequential = Helper.NewIncludeOuterIntergerStructSequential(32, 32);
                    IncludeOuterIntergerStructSequential cloneIncludeOuterIntergerStructSequential = Helper.NewIncludeOuterIntergerStructSequential(32, 32);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValInOut13...");
                    if (!MarshalStructAsParam_AsSeqByValInOut13(sourceIncludeOuterIntergerStructSequential))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValInOut13.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateIncludeOuterIntergerStructSequential(sourceIncludeOuterIntergerStructSequential, cloneIncludeOuterIntergerStructSequential, "MarshalStructAsParam_AsSeqByValInOut13"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S11Id:
                    S11 sourceS11 = Helper.NewS11((int*)new Int32(), 32);
                    S11 cloneS11 = Helper.NewS11((int*)new Int64(), 32);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByValInOut14...");
                    if (!MarshalStructAsParam_AsSeqByValInOut14(sourceS11))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByValInOut14.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS11(sourceS11, cloneS11, "MarshalStructAsParam_AsSeqByValInOut14"))
                    {
                        failures++;
                    }
                    break;    
                default:
                    Console.WriteLine("\tThere is not the struct id");
                    failures++;
                    break;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected Exception:" + e.ToString());
            failures++;
        }

    }

    [SecuritySafeCritical]
    unsafe private static void MarshalStructAsParam_AsSeqByRefInOut(StructID id)
    {
        try
        {
            switch (id)
            {
                case StructID.InnerSequentialId:
                    InnerSequential source_is = Helper.NewInnerSequential(1, 1.0F, "some string");
                    InnerSequential change_is = Helper.NewInnerSequential(77, 77.0F, "changed string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefInOut...");
                    if (!MarshalStructAsParam_AsSeqByRefInOut(ref source_is))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefInOut.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerSequential(source_is, change_is, "MarshalStructAsParam_AsSeqByRefInOut"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.InnerArraySequentialId:
                    InnerArraySequential source_ias = Helper.NewInnerArraySequential(1, 1.0F, "some string");
                    InnerArraySequential change_ias = Helper.NewInnerArraySequential(77, 77.0F, "changed string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefInOut2...");
                    if (!MarshalStructAsParam_AsSeqByRefInOut2(ref source_ias))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefInOut2.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerArraySequential(source_ias, change_ias, "MarshalStructAsParam_AsSeqByRefInOut2"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.CharSetAnsiSequentialId:
                    CharSetAnsiSequential source_csas = Helper.NewCharSetAnsiSequential("some string", 'c');
                    CharSetAnsiSequential changeStr1 = Helper.NewCharSetAnsiSequential("change string", 'n');

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefInOut3...");
                    if (!MarshalStructAsParam_AsSeqByRefInOut3(ref source_csas))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefInOut3.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateCharSetAnsiSequential(source_csas, changeStr1, "MarshalStructAsParam_AsSeqByRefInOut3"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.CharSetUnicodeSequentialId:
                    CharSetUnicodeSequential source_csus = Helper.NewCharSetUnicodeSequential("some string", 'c');
                    CharSetUnicodeSequential change_csus = Helper.NewCharSetUnicodeSequential("change string", 'n');

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefInOut4...");
                    if (!MarshalStructAsParam_AsSeqByRefInOut4(ref source_csus))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefInOut4.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateCharSetUnicodeSequential(source_csus, change_csus, "MarshalStructAsParam_AsSeqByRefInOut4"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.NumberSequentialId:
                    NumberSequential source_ns = Helper.NewNumberSequential(Int32.MinValue, UInt32.MaxValue, short.MinValue, ushort.MaxValue, byte.MinValue, sbyte.MaxValue, Int16.MinValue, UInt16.MaxValue, -1234567890, 1234567890, 32.0F, 3.2);
                    NumberSequential change_ns = Helper.NewNumberSequential(0, 32, 0, 16, 0, 8, 0, 16, 0, 64, 64.0F, 6.4);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefInOut6...");
                    if (!MarshalStructAsParam_AsSeqByRefInOut6(ref source_ns))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefInOut6.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateNumberSequential(source_ns, change_ns, "MarshalStructAsParam_AsSeqByRefInOut6"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S3Id:
                    int[] iarr = new int[256];
                    int[] icarr = new int[256];
                    InitialArray(iarr, icarr);

                    S3 sourceS3 = Helper.NewS3(true, "some string", iarr);
                    S3 changeS3 = Helper.NewS3(false, "change string", icarr);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefInOut7...");
                    if (!MarshalStructAsParam_AsSeqByRefInOut7(ref sourceS3))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefInOut7.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS3(sourceS3, changeS3, "MarshalStructAsParam_AsSeqByRefInOut7"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S5Id:
                    Enum1 enums = Enum1.e1;
                    Enum1 enumch = Enum1.e2;
                    S5 sourceS5 = Helper.NewS5(32, "some string", enums);
                    S5 changeS5 = Helper.NewS5(64, "change string", enumch);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefInOut8...");
                    if (!MarshalStructAsParam_AsSeqByRefInOut8(ref sourceS5))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefInOut8.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS5(sourceS5, changeS5, "MarshalStructAsParam_AsSeqByRefInOut8"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.StringStructSequentialAnsiId:
                    strOne = new String('a', 512);
                    strTwo = new String('b', 512);
                    StringStructSequentialAnsi source_sssa = Helper.NewStringStructSequentialAnsi(strOne, strTwo);
                    StringStructSequentialAnsi change_sssa = Helper.NewStringStructSequentialAnsi(strTwo, strOne);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefInOut9...");
                    if (!MarshalStructAsParam_AsSeqByRefInOut9(ref source_sssa))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefInOut9.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateStringStructSequentialAnsi(source_sssa, change_sssa, "MarshalStructAsParam_AsSeqByRefInOut9"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.StringStructSequentialUnicodeId:
                    strOne = new String('a', 256);
                    strTwo = new String('b', 256);
                    StringStructSequentialUnicode source_sssu = Helper.NewStringStructSequentialUnicode(strOne, strTwo);
                    StringStructSequentialUnicode change_sssu = Helper.NewStringStructSequentialUnicode(strTwo, strOne);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefInOut10...");
                    if (!MarshalStructAsParam_AsSeqByRefInOut10(ref source_sssu))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefInOut10.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateStringStructSequentialUnicode(source_sssu, change_sssu, "MarshalStructAsParam_AsSeqByRefInOut10"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S8Id:
                    S8 sourceS8 = Helper.NewS8("hello", true, 10, 128, 128, 32);
                    S8 changeS8 = Helper.NewS8("world", false, 1, 256, 256, 64);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefInOut11...");
                    if (!MarshalStructAsParam_AsSeqByRefInOut11(ref sourceS8))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefInOut11.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS8(sourceS8, changeS8, "MarshalStructAsParam_AsSeqByRefInOut11"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S9Id:
                    S9 sourceS9 = Helper.NewS9(128, new TestDelegate1(testMethod));
                    S9 changeS9 = Helper.NewS9(256, null);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefInOut12...");
                    if (!MarshalStructAsParam_AsSeqByRefInOut12(ref sourceS9))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefInOut12.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS9(sourceS9, changeS9, "MarshalStructAsParam_AsSeqByRefInOut12"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.IncludeOuterIntergerStructSequentialId:
                    IncludeOuterIntergerStructSequential sourceIncludeOuterIntergerStructSequential = Helper.NewIncludeOuterIntergerStructSequential(32, 32);
                    IncludeOuterIntergerStructSequential changeIncludeOuterIntergerStructSequential = Helper.NewIncludeOuterIntergerStructSequential(64, 64);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefInOut13...");
                    if (!MarshalStructAsParam_AsSeqByRefInOut13(ref sourceIncludeOuterIntergerStructSequential))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefInOut13.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateIncludeOuterIntergerStructSequential(sourceIncludeOuterIntergerStructSequential, changeIncludeOuterIntergerStructSequential, "MarshalStructAsParam_AsSeqByRefInOut13"))
                    {
                        failures++;
                    }
                    break;    
                case StructID.S11Id:
                    S11 sourceS11 = Helper.NewS11((int*)new Int32(), 32);
                    S11 changeS11 = Helper.NewS11((int*)(32), 64);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsSeqByRefInOut14...");
                    if (!MarshalStructAsParam_AsSeqByRefInOut14(ref sourceS11))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsSeqByRefInOut14.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateS11(sourceS11, changeS11, "MarshalStructAsParam_AsSeqByRefInOut14"))
                    {
                        failures++;
                    }
                    break;    
                default:
                    Console.WriteLine("\tThere is not the struct id");
                    failures++;
                    break;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected Exception:" + e.ToString());
            failures++;
        }
    }
    #endregion

    [SecuritySafeCritical]
    private static void RunMarshalSeqStructAsParamByVal()
    {
        Console.WriteLine("\nVerify marshal sequential layout struct as param as ByVal");
        MarshalStructAsParam_AsSeqByVal(StructID.InnerSequentialId);
        MarshalStructAsParam_AsSeqByVal(StructID.InnerArraySequentialId);
        MarshalStructAsParam_AsSeqByVal(StructID.CharSetAnsiSequentialId);
        MarshalStructAsParam_AsSeqByVal(StructID.CharSetUnicodeSequentialId);
        MarshalStructAsParam_AsSeqByVal(StructID.NumberSequentialId);
        MarshalStructAsParam_AsSeqByVal(StructID.S3Id);
        MarshalStructAsParam_AsSeqByVal(StructID.S5Id);
        MarshalStructAsParam_AsSeqByVal(StructID.StringStructSequentialAnsiId);
        MarshalStructAsParam_AsSeqByVal(StructID.StringStructSequentialUnicodeId);
        MarshalStructAsParam_AsSeqByVal(StructID.S8Id);
        MarshalStructAsParam_AsSeqByVal(StructID.S9Id);
        MarshalStructAsParam_AsSeqByVal(StructID.IncludeOuterIntergerStructSequentialId);
        MarshalStructAsParam_AsSeqByVal(StructID.S11Id);
    }

    [SecuritySafeCritical]
    private static void RunMarshalSeqStructAsParamByRef()
    {
        Console.WriteLine("\nVerify marshal sequential layout struct as param as ByRef");
        MarshalStructAsParam_AsSeqByRef(StructID.InnerSequentialId);
        MarshalStructAsParam_AsSeqByRef(StructID.InnerArraySequentialId);
        MarshalStructAsParam_AsSeqByRef(StructID.CharSetAnsiSequentialId);
        MarshalStructAsParam_AsSeqByRef(StructID.CharSetUnicodeSequentialId);
        MarshalStructAsParam_AsSeqByRef(StructID.NumberSequentialId);
        MarshalStructAsParam_AsSeqByRef(StructID.S3Id);
        MarshalStructAsParam_AsSeqByRef(StructID.S5Id);
        MarshalStructAsParam_AsSeqByRef(StructID.StringStructSequentialAnsiId);
        MarshalStructAsParam_AsSeqByRef(StructID.StringStructSequentialUnicodeId);
        MarshalStructAsParam_AsSeqByRef(StructID.S8Id);
        MarshalStructAsParam_AsSeqByRef(StructID.S9Id);
        MarshalStructAsParam_AsSeqByRef(StructID.IncludeOuterIntergerStructSequentialId);
        MarshalStructAsParam_AsSeqByRef(StructID.S11Id);
    }

    [SecuritySafeCritical]
    private static void RunMarshalSeqStructAsParamByValIn()
    {
        Console.WriteLine("\nVerify marshal sequential layout struct as param as ByValIn");
        MarshalStructAsParam_AsSeqByValIn(StructID.InnerSequentialId);
        MarshalStructAsParam_AsSeqByValIn(StructID.InnerArraySequentialId);
        MarshalStructAsParam_AsSeqByValIn(StructID.CharSetAnsiSequentialId);
        MarshalStructAsParam_AsSeqByValIn(StructID.CharSetUnicodeSequentialId);
        MarshalStructAsParam_AsSeqByValIn(StructID.NumberSequentialId);
        MarshalStructAsParam_AsSeqByValIn(StructID.S3Id);
        MarshalStructAsParam_AsSeqByValIn(StructID.S5Id);
        MarshalStructAsParam_AsSeqByValIn(StructID.StringStructSequentialAnsiId);
        MarshalStructAsParam_AsSeqByValIn(StructID.StringStructSequentialUnicodeId);
        MarshalStructAsParam_AsSeqByValIn(StructID.S8Id);
        MarshalStructAsParam_AsSeqByValIn(StructID.S9Id);
        MarshalStructAsParam_AsSeqByValIn(StructID.IncludeOuterIntergerStructSequentialId);
        MarshalStructAsParam_AsSeqByValIn(StructID.S11Id);
    }

    [SecuritySafeCritical]
    private static void RunMarshalSeqStructAsParamByRefIn()
    {
        Console.WriteLine("\nVerify marshal sequential layout struct as param as ByRefIn");
        MarshalStructAsParam_AsSeqByRefIn(StructID.InnerSequentialId);
        MarshalStructAsParam_AsSeqByRefIn(StructID.InnerArraySequentialId);
        MarshalStructAsParam_AsSeqByRefIn(StructID.CharSetAnsiSequentialId);
        MarshalStructAsParam_AsSeqByRefIn(StructID.CharSetUnicodeSequentialId);
        MarshalStructAsParam_AsSeqByRefIn(StructID.NumberSequentialId);
        MarshalStructAsParam_AsSeqByRefIn(StructID.S3Id);
        MarshalStructAsParam_AsSeqByRefIn(StructID.S5Id);
        MarshalStructAsParam_AsSeqByRefIn(StructID.StringStructSequentialAnsiId);
        MarshalStructAsParam_AsSeqByRefIn(StructID.StringStructSequentialUnicodeId);
        MarshalStructAsParam_AsSeqByRefIn(StructID.S8Id);
        MarshalStructAsParam_AsSeqByRefIn(StructID.S9Id);
        MarshalStructAsParam_AsSeqByRefIn(StructID.IncludeOuterIntergerStructSequentialId);
        MarshalStructAsParam_AsSeqByRefIn(StructID.S11Id);
    }

    [SecuritySafeCritical]
    private static void RunMarshalSeqStructAsParamByValOut()
    {
        Console.WriteLine("\nVerify marshal sequential layout struct as param as ByValOut");
        MarshalStructAsParam_AsSeqByValOut(StructID.InnerSequentialId);
        MarshalStructAsParam_AsSeqByValOut(StructID.InnerArraySequentialId);
        MarshalStructAsParam_AsSeqByValOut(StructID.CharSetAnsiSequentialId);
        MarshalStructAsParam_AsSeqByValOut(StructID.CharSetUnicodeSequentialId);
        MarshalStructAsParam_AsSeqByValOut(StructID.NumberSequentialId);
        MarshalStructAsParam_AsSeqByValOut(StructID.S3Id);
        MarshalStructAsParam_AsSeqByValOut(StructID.S5Id);
        MarshalStructAsParam_AsSeqByValOut(StructID.StringStructSequentialAnsiId);
        MarshalStructAsParam_AsSeqByValOut(StructID.StringStructSequentialUnicodeId);
        MarshalStructAsParam_AsSeqByValOut(StructID.S8Id);
        MarshalStructAsParam_AsSeqByValOut(StructID.S9Id);
        MarshalStructAsParam_AsSeqByValOut(StructID.IncludeOuterIntergerStructSequentialId);
        MarshalStructAsParam_AsSeqByValOut(StructID.S11Id);
    }

    [SecuritySafeCritical]
    private static void RunMarshalSeqStructAsParamByRefOut()
    {
        Console.WriteLine("\nVerify marshal sequential layout struct as param as ByRefOut");
        MarshalStructAsParam_AsSeqByRefOut(StructID.InnerSequentialId);
        MarshalStructAsParam_AsSeqByRefOut(StructID.InnerArraySequentialId);
        MarshalStructAsParam_AsSeqByRefOut(StructID.CharSetAnsiSequentialId);
        MarshalStructAsParam_AsSeqByRefOut(StructID.CharSetUnicodeSequentialId);
        MarshalStructAsParam_AsSeqByRefOut(StructID.NumberSequentialId);
        MarshalStructAsParam_AsSeqByRefOut(StructID.S3Id);
        MarshalStructAsParam_AsSeqByRefOut(StructID.S5Id);
        MarshalStructAsParam_AsSeqByRefOut(StructID.StringStructSequentialAnsiId);
        MarshalStructAsParam_AsSeqByRefOut(StructID.StringStructSequentialUnicodeId);
        MarshalStructAsParam_AsSeqByRefOut(StructID.S8Id);
        MarshalStructAsParam_AsSeqByRefOut(StructID.S9Id);
        MarshalStructAsParam_AsSeqByRefOut(StructID.IncludeOuterIntergerStructSequentialId);
        MarshalStructAsParam_AsSeqByRefOut(StructID.S11Id);
    }

    [SecuritySafeCritical]
    private static void RunMarshalSeqStructAsParamByValInOut()
    {
        Console.WriteLine("\nVerify marshal sequential layout struct as param as ByValInOut");
        MarshalStructAsParam_AsSeqByValInOut(StructID.InnerSequentialId);
        MarshalStructAsParam_AsSeqByValInOut(StructID.InnerArraySequentialId);
        MarshalStructAsParam_AsSeqByValInOut(StructID.CharSetAnsiSequentialId);
        MarshalStructAsParam_AsSeqByValInOut(StructID.CharSetUnicodeSequentialId);
        MarshalStructAsParam_AsSeqByValInOut(StructID.NumberSequentialId);
        MarshalStructAsParam_AsSeqByValInOut(StructID.S3Id);
        MarshalStructAsParam_AsSeqByValInOut(StructID.S5Id);
        MarshalStructAsParam_AsSeqByValInOut(StructID.StringStructSequentialAnsiId);
        MarshalStructAsParam_AsSeqByValInOut(StructID.StringStructSequentialUnicodeId);
        MarshalStructAsParam_AsSeqByValInOut(StructID.S8Id);
        MarshalStructAsParam_AsSeqByValInOut(StructID.S9Id);
        MarshalStructAsParam_AsSeqByValInOut(StructID.IncludeOuterIntergerStructSequentialId);
        MarshalStructAsParam_AsSeqByValInOut(StructID.S11Id);
    }

    [SecuritySafeCritical]
    private static void RunMarshalSeqStructAsParamByRefInOut()
    {
        Console.WriteLine("\nVerify marshal sequential layout struct as param as ByRefInOut");
        MarshalStructAsParam_AsSeqByRefInOut(StructID.InnerSequentialId);
        MarshalStructAsParam_AsSeqByRefInOut(StructID.InnerArraySequentialId);
        MarshalStructAsParam_AsSeqByRefInOut(StructID.CharSetAnsiSequentialId);
        MarshalStructAsParam_AsSeqByRefInOut(StructID.CharSetUnicodeSequentialId);
        MarshalStructAsParam_AsSeqByRefInOut(StructID.NumberSequentialId);
        MarshalStructAsParam_AsSeqByRefInOut(StructID.S3Id);
        MarshalStructAsParam_AsSeqByRefInOut(StructID.S5Id);
        MarshalStructAsParam_AsSeqByRefInOut(StructID.StringStructSequentialAnsiId);
        MarshalStructAsParam_AsSeqByRefInOut(StructID.StringStructSequentialUnicodeId);
        MarshalStructAsParam_AsSeqByRefInOut(StructID.S8Id);
        MarshalStructAsParam_AsSeqByRefInOut(StructID.S9Id);
        MarshalStructAsParam_AsSeqByRefInOut(StructID.IncludeOuterIntergerStructSequentialId);
        MarshalStructAsParam_AsSeqByRefInOut(StructID.S11Id);
    }
}


   