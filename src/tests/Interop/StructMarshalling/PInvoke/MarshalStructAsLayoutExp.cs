using System;
using System.Runtime.InteropServices;
using System.Security;
using Xunit;

public class Managed
{
    static int failures = 0;
    enum StructID
    {
        INNER2Id,
        InnerExplicitId,
        InnerArrayExplicitId,
        OUTER3Id,
        UId,
        ByteStructPack2ExplicitId,
        ShortStructPack4ExplicitId,
        IntStructPack8ExplicitId,
        LongStructPack16ExplicitId,
        OverlappingLongFloatId,
        OverlappingMultipleEightbyteId,
        HFAId
    }

    [SecuritySafeCritical]
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static int TestEntryPoint()
    {
        RunMarshalStructAsParamAsExpByVal();
        RunMarshalStructAsParamAsExpByRef();
        RunMarshalStructAsParamAsExpByValIn();
        RunMarshalStructAsParamAsExpByRefIn();
        RunMarshalStructAsParamAsExpByValOut();
        RunMarshalStructAsParamAsExpByRefOut();
        RunMarshalStructAsParamAsExpByValInOut();
        RunMarshalStructAsParamAsExpByRefInOut();
        RunMarshalStructAsReturn();

        if (failures > 0)
        {
            Console.WriteLine("\nTEST FAILED!");
            return 100 + failures;
        }
        else
        {
            Console.WriteLine("\nTEST PASSED!");
            return 100;
        }
    }

    #region	Struct with Layout Explicit scenario1
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByValINNER2(INNER2 str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByRefINNER2(ref INNER2 str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValINNER2")]
    static extern bool MarshalStructAsParam_AsExpByValInINNER2([In] INNER2 str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByRefInINNER2([In] ref INNER2 str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValINNER2")]
    static extern bool MarshalStructAsParam_AsExpByValOutINNER2([Out] INNER2 str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByRefOutINNER2(out INNER2 str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValINNER2")]
    static extern bool MarshalStructAsParam_AsExpByValInOutINNER2([In, Out] INNER2 str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByRefINNER2")]
    static extern bool MarshalStructAsParam_AsExpByRefInOutINNER2([In, Out] ref INNER2 str1);
    #endregion
    #region Struct with Layout Explicit scenario2
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValInnerExplicit")]
    static extern bool MarshalStructAsParam_AsExpByValInnerExplicit(InnerExplicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByRefInnerExplicit")]
    static extern bool MarshalStructAsParam_AsExpByRefInnerExplicit(ref InnerExplicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValInnerExplicit")]
    static extern bool MarshalStructAsParam_AsExpByValInInnerExplicit([In] InnerExplicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByRefInInnerExplicit")]
    static extern bool MarshalStructAsParam_AsExpByRefInInnerExplicit([In] ref InnerExplicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValInnerExplicit")]
    static extern bool MarshalStructAsParam_AsExpByValOutInnerExplicit([Out] InnerExplicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByRefOutInnerExplicit")]
    static extern bool MarshalStructAsParam_AsExpByRefOutInnerExplicit(out InnerExplicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValInnerExplicit")]
    static extern bool MarshalStructAsParam_AsExpByValInOutInnerExplicit([In, Out] InnerExplicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByRefInnerExplicit")]
    static extern bool MarshalStructAsParam_AsExpByRefInOutInnerExplicit([In, Out] ref InnerExplicit str1);
    #endregion
    #region Struct with Layout Explicit scenario3
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByValInnerArrayExplicit(InnerArrayExplicit str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByRefInnerArrayExplicit(ref InnerArrayExplicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValInnerArrayExplicit")]
    static extern bool MarshalStructAsParam_AsExpByValInInnerArrayExplicit([In] InnerArrayExplicit str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByRefInInnerArrayExplicit([In] ref InnerArrayExplicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValInnerArrayExplicit")]
    static extern bool MarshalStructAsParam_AsExpByValOutInnerArrayExplicit([Out] InnerArrayExplicit str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByRefOutInnerArrayExplicit(out InnerArrayExplicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValInnerArrayExplicit")]
    static extern bool MarshalStructAsParam_AsExpByValInOutInnerArrayExplicit([In, Out] InnerArrayExplicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByRefInnerArrayExplicit")]
    static extern bool MarshalStructAsParam_AsExpByRefInOutInnerArrayExplicit([In, Out] ref InnerArrayExplicit str1);
    #endregion
    #region Struct with Layout Explicit scenario4
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByValOUTER3(OUTER3 str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByRefOUTER3(ref OUTER3 str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValOUTER3")]
    static extern bool MarshalStructAsParam_AsExpByValInOUTER3([In] OUTER3 str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByRefInOUTER3([In] ref OUTER3 str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValOUTER3")]
    static extern bool MarshalStructAsParam_AsExpByValOutOUTER3([Out] OUTER3 str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByRefOutOUTER3(out OUTER3 str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValOUTER3")]
    static extern bool MarshalStructAsParam_AsExpByValInOutOUTER3([In, Out] OUTER3 str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByRefOUTER3")]
    static extern bool MarshalStructAsParam_AsExpByRefInOutOUTER3([In, Out] ref OUTER3 str1);
    #endregion
    #region Struct(U) with Layout Explicit scenario5
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByValU(U str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByRefU(ref U str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValU")]
    static extern bool MarshalStructAsParam_AsExpByValInU([In] U str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByRefInU([In] ref U str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValU")]
    static extern bool MarshalStructAsParam_AsExpByValOutU([Out] U str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByRefOutU(out U str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValU")]
    static extern bool MarshalStructAsParam_AsExpByValInOutU([In, Out] U str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByRefU")]
    static extern bool MarshalStructAsParam_AsExpByRefInOutU([In, Out] ref U str1);
    #endregion

    #region Struct(ByteStructPack2Explicit) with Layout Explicit scenario6
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByValByteStructPack2Explicit(ByteStructPack2Explicit str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByRefByteStructPack2Explicit(ref ByteStructPack2Explicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValByteStructPack2Explicit")]
    static extern bool MarshalStructAsParam_AsExpByValInByteStructPack2Explicit([In] ByteStructPack2Explicit str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByRefInByteStructPack2Explicit([In] ref ByteStructPack2Explicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValByteStructPack2Explicit")]
    static extern bool MarshalStructAsParam_AsExpByValOutByteStructPack2Explicit([Out] ByteStructPack2Explicit str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByRefOutByteStructPack2Explicit(out ByteStructPack2Explicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValByteStructPack2Explicit")]
    static extern bool MarshalStructAsParam_AsExpByValInOutByteStructPack2Explicit([In, Out] ByteStructPack2Explicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByRefByteStructPack2Explicit")]
    static extern bool MarshalStructAsParam_AsExpByRefInOutByteStructPack2Explicit([In, Out] ref ByteStructPack2Explicit str1);
    #endregion
    #region Struct(ShortStructPack4Explicit) with Layout Explicit scenario7
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByValShortStructPack4Explicit(ShortStructPack4Explicit str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByRefShortStructPack4Explicit(ref ShortStructPack4Explicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValShortStructPack4Explicit")]
    static extern bool MarshalStructAsParam_AsExpByValInShortStructPack4Explicit([In] ShortStructPack4Explicit str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByRefInShortStructPack4Explicit([In] ref ShortStructPack4Explicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValShortStructPack4Explicit")]
    static extern bool MarshalStructAsParam_AsExpByValOutShortStructPack4Explicit([Out] ShortStructPack4Explicit str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByRefOutShortStructPack4Explicit(out ShortStructPack4Explicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValShortStructPack4Explicit")]
    static extern bool MarshalStructAsParam_AsExpByValInOutShortStructPack4Explicit([In, Out] ShortStructPack4Explicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByRefShortStructPack4Explicit")]
    static extern bool MarshalStructAsParam_AsExpByRefInOutShortStructPack4Explicit([In, Out] ref ShortStructPack4Explicit str1);
    #endregion
    #region Struct(IntStructPack8Explicit) with Layout Explicit scenario8
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByValIntStructPack8Explicit(IntStructPack8Explicit str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByRefIntStructPack8Explicit(ref IntStructPack8Explicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValIntStructPack8Explicit")]
    static extern bool MarshalStructAsParam_AsExpByValInIntStructPack8Explicit([In] IntStructPack8Explicit str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByRefInIntStructPack8Explicit([In] ref IntStructPack8Explicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValIntStructPack8Explicit")]
    static extern bool MarshalStructAsParam_AsExpByValOutIntStructPack8Explicit([Out] IntStructPack8Explicit str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByRefOutIntStructPack8Explicit(out IntStructPack8Explicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValIntStructPack8Explicit")]
    static extern bool MarshalStructAsParam_AsExpByValInOutIntStructPack8Explicit([In, Out] IntStructPack8Explicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByRefIntStructPack8Explicit")]
    static extern bool MarshalStructAsParam_AsExpByRefInOutIntStructPack8Explicit([In, Out] ref IntStructPack8Explicit str1);
    #endregion
    #region Struct(LongStructPack16Explicit) with Layout Explicit scenario9
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByValLongStructPack16Explicit(LongStructPack16Explicit str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByRefLongStructPack16Explicit(ref LongStructPack16Explicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValLongStructPack16Explicit")]
    static extern bool MarshalStructAsParam_AsExpByValInLongStructPack16Explicit([In] LongStructPack16Explicit str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByRefInLongStructPack16Explicit([In] ref LongStructPack16Explicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValLongStructPack16Explicit")]
    static extern bool MarshalStructAsParam_AsExpByValOutLongStructPack16Explicit([Out] LongStructPack16Explicit str1);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByRefOutLongStructPack16Explicit(out LongStructPack16Explicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByValLongStructPack16Explicit")]
    static extern bool MarshalStructAsParam_AsExpByValInOutLongStructPack16Explicit([In, Out] LongStructPack16Explicit str1);
    [DllImport("MarshalStructAsParam", EntryPoint = "MarshalStructAsParam_AsExpByRefLongStructPack16Explicit")]
    static extern bool MarshalStructAsParam_AsExpByRefInOutLongStructPack16Explicit([In, Out] ref LongStructPack16Explicit str1);
    #endregion
    [DllImport("MarshalStructAsParam")]
    static extern LongStructPack16Explicit GetLongStruct(long l1, long l2);
    [DllImport("MarshalStructAsParam")]
    static extern IntStructPack8Explicit GetIntStruct(int i, int j);

    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByValOverlappingLongFloat(OverlappingLongFloat str, long expected);
    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByValOverlappingLongFloat(OverlappingLongFloat2 str, long expected);

    [DllImport("MarshalStructAsParam")]
    static extern bool MarshalStructAsParam_AsExpByValOverlappingMultipleEightByte(OverlappingMultipleEightbyte str, float i1, float i2, float i3);

    [DllImport("MarshalStructAsParam")]
    static extern float ProductHFA(ExplicitHFA hfa);
    [DllImport("MarshalStructAsParam")]
    static extern float ProductHFA(ExplicitFixedHFA hfa);
    [DllImport("MarshalStructAsParam")]
    static extern float ProductHFA(OverlappingHFA hfa);

    #region Marshal Explicit struct method
    [SecuritySafeCritical]
    private static void MarshalStructAsParam_AsExpByVal(StructID id)
    {
        try
        {
            switch (id)
            {
                case StructID.INNER2Id:
                    INNER2 sourceINNER2 = Helper.NewINNER2(1, 1.0F, "some string");
                    INNER2 cloneINNER2 = Helper.NewINNER2(1, 1.0F, "some string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValINNER2...");
                    if (!MarshalStructAsParam_AsExpByValINNER2(sourceINNER2))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValINNER2.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateINNER2(sourceINNER2, cloneINNER2, "MarshalStructAsParam_AsExpByValINNER2"))
                    {
                        failures++;
                    }
                    break;
                case StructID.InnerExplicitId:
                    InnerExplicit sourceInnerExplicit = new InnerExplicit();
                    sourceInnerExplicit.f1 = 1;
                    sourceInnerExplicit.f3 = "some string";
                    InnerExplicit cloneInnerExplicit = new InnerExplicit();
                    cloneInnerExplicit.f1 = 1;
                    cloneInnerExplicit.f3 = "some string";

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValInnerExplicit...");
                    if (!MarshalStructAsParam_AsExpByValInnerExplicit(sourceInnerExplicit))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValInnerExplicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerExplicit(sourceInnerExplicit, cloneInnerExplicit, "MarshalStructAsParam_AsExpByValInnerExplicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.InnerArrayExplicitId:
                    InnerArrayExplicit sourceInnerArrayExplicit = Helper.NewInnerArrayExplicit(1, 1.0F, "some string1", "some string2");
                    InnerArrayExplicit cloneInnerArrayExplicit = Helper.NewInnerArrayExplicit(1, 1.0F, "some string1", "some string2");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValInnerArrayExplicit...");
                    if (!MarshalStructAsParam_AsExpByValInnerArrayExplicit(sourceInnerArrayExplicit))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValInnerArrayExplicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerArrayExplicit(sourceInnerArrayExplicit, cloneInnerArrayExplicit, "MarshalStructAsParam_AsExpByValInnerArrayExplicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.OUTER3Id:
                    OUTER3 sourceOUTER3 = Helper.NewOUTER3(1, 1.0F, "some string", "some string");
                    OUTER3 cloneOUTER3 = Helper.NewOUTER3(1, 1.0F, "some string", "some string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValOUTER3...");
                    if (!MarshalStructAsParam_AsExpByValOUTER3(sourceOUTER3))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValOUTER3.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateOUTER3(sourceOUTER3, cloneOUTER3, "MarshalStructAsParam_AsExpByValOUTER3"))
                    {
                        failures++;
                    }
                    break;
                case StructID.UId:
                    U sourceU = Helper.NewU(Int32.MinValue, UInt32.MaxValue, new IntPtr(-32), new UIntPtr(32), short.MinValue, ushort.MaxValue, byte.MinValue, sbyte.MaxValue, long.MinValue, ulong.MaxValue, 32.0F, 3.2);
                    U cloneU = Helper.NewU(Int32.MinValue, UInt32.MaxValue, new IntPtr(-32), new UIntPtr(32), short.MinValue, ushort.MaxValue, byte.MinValue, sbyte.MaxValue, long.MinValue, ulong.MaxValue, 32.0F, 3.2);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValU...");
                    if (!MarshalStructAsParam_AsExpByValU(sourceU))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValU.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateU(sourceU, cloneU, "MarshalStructAsParam_AsExpByValU"))
                    {
                        failures++;
                    }
                    break;
                case StructID.ByteStructPack2ExplicitId:
                    ByteStructPack2Explicit source_bspe = Helper.NewByteStructPack2Explicit(32, 32);
                    ByteStructPack2Explicit clone_bspe = Helper.NewByteStructPack2Explicit(32, 32);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValByteStructPack2Explicit...");
                    if (!MarshalStructAsParam_AsExpByValByteStructPack2Explicit(source_bspe))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValByteStructPack2Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateByteStructPack2Explicit(source_bspe, clone_bspe, "MarshalStructAsParam_AsExpByValByteStructPack2Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.ShortStructPack4ExplicitId:
                    ShortStructPack4Explicit source_sspe = Helper.NewShortStructPack4Explicit(32, 32);
                    ShortStructPack4Explicit clone_sspe = Helper.NewShortStructPack4Explicit(32, 32);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValShortStructPack4Explicit...");
                    if (!MarshalStructAsParam_AsExpByValShortStructPack4Explicit(source_sspe))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValShortStructPack4Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateShortStructPack4Explicit(source_sspe, clone_sspe, "MarshalStructAsParam_AsExpByValShortStructPack4Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.IntStructPack8ExplicitId:
                    IntStructPack8Explicit source_ispe = Helper.NewIntStructPack8Explicit(32, 32);
                    IntStructPack8Explicit clone_ispe = Helper.NewIntStructPack8Explicit(32, 32);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValIntStructPack8Explicit...");
                    if (!MarshalStructAsParam_AsExpByValIntStructPack8Explicit(source_ispe))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValIntStructPack8Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateIntStructPack8Explicit(source_ispe, clone_ispe, "MarshalStructAsParam_AsExpByValIntStructPack8Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.LongStructPack16ExplicitId:
                    LongStructPack16Explicit sourceLongStructPack16Explicit = Helper.NewLongStructPack16Explicit(32, 32);
                    LongStructPack16Explicit cloneLongStructPack16Explicit = Helper.NewLongStructPack16Explicit(32, 32);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValLongStructPack16Explicit...");
                    if (!MarshalStructAsParam_AsExpByValLongStructPack16Explicit(sourceLongStructPack16Explicit))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValLongStructPack16Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateLongStructPack16Explicit(sourceLongStructPack16Explicit, cloneLongStructPack16Explicit, "MarshalStructAsParam_AsExpByValLongStructPack16Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.OverlappingLongFloatId:
                    OverlappingLongFloat overlappingLongFloat = new OverlappingLongFloat
                    {
                        l = 12345,
                        f = 12.45f
                    };
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValOverlappingLongFloat...");
                    if (!MarshalStructAsParam_AsExpByValOverlappingLongFloat(overlappingLongFloat, overlappingLongFloat.l))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValOverlappingLongFloat. Expected:True;Actual:False");
                        failures++;
                    }
                    OverlappingLongFloat2 overlappingLongFloat2 = new OverlappingLongFloat2
                    {
                        l = 12345,
                        f = 12.45f
                    };
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValOverlappingLongFloat (Reversed field order)...");
                    if (!MarshalStructAsParam_AsExpByValOverlappingLongFloat(overlappingLongFloat2, overlappingLongFloat.l))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValOverlappingLongFloat. Expected:True;Actual:False");
                        failures++;
                    }
                    break;
                case StructID.OverlappingMultipleEightbyteId:
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValOverlappingMultipleEightByte...");
                    OverlappingMultipleEightbyte overlappingMultipleEightbyte = new OverlappingMultipleEightbyte
                    {
                        arr = new float[3] { 1f, 400f, 623289f},
                        i = 1234
                    };
                    if (!MarshalStructAsParam_AsExpByValOverlappingMultipleEightByte(
                            overlappingMultipleEightbyte,
                            overlappingMultipleEightbyte.arr[0],
                            overlappingMultipleEightbyte.arr[1],
                            overlappingMultipleEightbyte.arr[2]))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValOverlappingMultipleEightByte. Expected True;Actual:False");
                        failures++;
                    }
                    break;
                case StructID.HFAId:
                    OverlappingHFA hfa = new OverlappingHFA
                    {
                        hfa = new HFA
                        {
                            f1 = 2.0f,
                            f2 = 10.5f,
                            f3 = 15.2f,
                            f4 = 0.12f
                        }
                    };

                    float expected = hfa.hfa.f1 * hfa.hfa.f2 * hfa.hfa.f3 * hfa.hfa.f4;
                    float actual;

                    Console.WriteLine("\tCalling ProductHFA with Explicit HFA.");
                    actual = ProductHFA(hfa.explicitHfa);
                    if (expected != actual)
                    {
                        Console.WriteLine($"\tFAILED! Expected {expected}. Actual {actual}");
                        failures++;
                    }
                    Console.WriteLine("\tCalling ProductHFA with Explicit Fixed HFA.");
                    actual = ProductHFA(hfa.explicitFixedHfa);
                    if (expected != actual)
                    {
                        Console.WriteLine($"\tFAILED! Expected {expected}. Actual {actual}");
                        failures++;
                    }
                    Console.WriteLine("\tCalling ProductHFA with Overlapping HFA.");
                    actual = ProductHFA(hfa);
                    if (expected != actual)
                    {
                        Console.WriteLine($"\tFAILED! Expected {expected}. Actual {actual}");
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
    private static void MarshalStructAsParam_AsExpByRef(StructID id)
    {
        try
        {
            switch (id)
            {
                case StructID.INNER2Id:
                    INNER2 sourceINNER2 = Helper.NewINNER2(1, 1.0F, "some string");
                    INNER2 changeINNER2 = Helper.NewINNER2(77, 77.0F, "changed string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefINNER2...");
                    if (!MarshalStructAsParam_AsExpByRefINNER2(ref sourceINNER2))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefINNER2.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateINNER2(sourceINNER2, changeINNER2, "MarshalStructAsParam_AsExpByRefINNER2"))
                    {
                        failures++;
                    }
                    break;
                case StructID.InnerExplicitId:
                    InnerExplicit sourceInnerExplicit = new InnerExplicit();
                    sourceInnerExplicit.f1 = 1;
                    sourceInnerExplicit.f3 = "some string";
                    InnerExplicit changeInnerExplicit = new InnerExplicit();
                    changeInnerExplicit.f1 = 77;
                    changeInnerExplicit.f3 = "changed string";

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefInnerExplicit...");
                    if (!MarshalStructAsParam_AsExpByRefInnerExplicit(ref sourceInnerExplicit))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefInnerExplicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerExplicit(sourceInnerExplicit, changeInnerExplicit, "MarshalStructAsParam_AsExpByRefInnerExplicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.InnerArrayExplicitId:
                    InnerArrayExplicit sourceInnerArrayExplicit = Helper.NewInnerArrayExplicit(1, 1.0F, "some string1", "some string2");
                    InnerArrayExplicit changeInnerArrayExplicit = Helper.NewInnerArrayExplicit(77, 77.0F, "change string1", "change string2");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefInnerArrayExplicit...");
                    if (!MarshalStructAsParam_AsExpByRefInnerArrayExplicit(ref sourceInnerArrayExplicit))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefInnerArrayExplicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerArrayExplicit(sourceInnerArrayExplicit, changeInnerArrayExplicit, "MarshalStructAsParam_AsExpByRefInnerArrayExplicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.OUTER3Id:
                    OUTER3 sourceOUTER3 = Helper.NewOUTER3(1, 1.0F, "some string", "some string");
                    OUTER3 changeOUTER3 = Helper.NewOUTER3(77, 77.0F, "changed string", "changed string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefOUTER3...");
                    if (!MarshalStructAsParam_AsExpByRefOUTER3(ref sourceOUTER3))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefOUTER3.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateOUTER3(sourceOUTER3, changeOUTER3, "MarshalStructAsParam_AsExpByRefOUTER3"))
                    {
                        failures++;
                    }
                    break;
                case StructID.UId:
                    U sourceU = Helper.NewU(Int32.MinValue, UInt32.MaxValue, new IntPtr(-32), new UIntPtr(32), short.MinValue, ushort.MaxValue, byte.MinValue, sbyte.MaxValue, long.MinValue, ulong.MaxValue, 32.0F, 3.2);
                    U changeU = Helper.NewU(Int32.MaxValue, UInt32.MinValue, new IntPtr(-64), new UIntPtr(64), short.MaxValue, ushort.MinValue, byte.MaxValue, sbyte.MinValue, long.MaxValue, ulong.MinValue, 64.0F, 6.4);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefU...");
                    if (!MarshalStructAsParam_AsExpByRefU(ref sourceU))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefU.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateU(sourceU, changeU, "MarshalStructAsParam_AsExpByRefU"))
                    {
                        failures++;
                    }
                    break;
                case StructID.ByteStructPack2ExplicitId:
                    ByteStructPack2Explicit source_bspe = Helper.NewByteStructPack2Explicit(32, 32);
                    ByteStructPack2Explicit change_bspe = Helper.NewByteStructPack2Explicit(64, 64);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefByteStructPack2Explicit...");
                    if (!MarshalStructAsParam_AsExpByRefByteStructPack2Explicit(ref source_bspe))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefByteStructPack2Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateByteStructPack2Explicit(source_bspe, change_bspe, "MarshalStructAsParam_AsExpByRefByteStructPack2Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.ShortStructPack4ExplicitId:
                    ShortStructPack4Explicit source_sspe = Helper.NewShortStructPack4Explicit(32, 32);
                    ShortStructPack4Explicit change_sspe = Helper.NewShortStructPack4Explicit(64, 64);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefShortStructPack4Explicit...");
                    if (!MarshalStructAsParam_AsExpByRefShortStructPack4Explicit(ref source_sspe))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefShortStructPack4Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateShortStructPack4Explicit(source_sspe, change_sspe, "MarshalStructAsParam_AsExpByRefShortStructPack4Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.IntStructPack8ExplicitId:
                    IntStructPack8Explicit source_ispe = Helper.NewIntStructPack8Explicit(32, 32);
                    IntStructPack8Explicit change_ispe = Helper.NewIntStructPack8Explicit(64, 64);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefIntStructPack8Explicit...");
                    if (!MarshalStructAsParam_AsExpByRefIntStructPack8Explicit(ref source_ispe))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefIntStructPack8Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateIntStructPack8Explicit(source_ispe, change_ispe, "MarshalStructAsParam_AsExpByRefIntStructPack8Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.LongStructPack16ExplicitId:
                    LongStructPack16Explicit sourceLongStructPack16Explicit = Helper.NewLongStructPack16Explicit(32, 32);
                    LongStructPack16Explicit changeLongStructPack16Explicit = Helper.NewLongStructPack16Explicit(64, 64);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefLongStructPack16Explicit...");
                    if (!MarshalStructAsParam_AsExpByRefLongStructPack16Explicit(ref sourceLongStructPack16Explicit))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefLongStructPack16Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateLongStructPack16Explicit(sourceLongStructPack16Explicit, changeLongStructPack16Explicit, "MarshalStructAsParam_AsExpByRefLongStructPack16Explicit"))
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
    private static void MarshalStructAsParam_AsExpByValIn(StructID id)
    {
        try
        {
            switch (id)
            {
                case StructID.INNER2Id:
                    INNER2 sourceINNER2 = Helper.NewINNER2(1, 1.0F, "some string");
                    INNER2 cloneINNER2 = Helper.NewINNER2(1, 1.0F, "some string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValInINNER2...");
                    if (!MarshalStructAsParam_AsExpByValInINNER2(sourceINNER2))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValInINNER2.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateINNER2(sourceINNER2, cloneINNER2, "MarshalStructAsParam_AsExpByValInINNER2"))
                    {
                        failures++;
                    }
                    break;
                case StructID.InnerExplicitId:
                    InnerExplicit sourceInnerExplicit = new InnerExplicit();
                    sourceInnerExplicit.f1 = 1;
                    sourceInnerExplicit.f3 = "some string";
                    InnerExplicit cloneInnerExplicit = new InnerExplicit();
                    cloneInnerExplicit.f1 = 1;
                    cloneInnerExplicit.f3 = "some string";

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValInInnerExplicit...");
                    if (!MarshalStructAsParam_AsExpByValInInnerExplicit(sourceInnerExplicit))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValInInnerExplicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerExplicit(sourceInnerExplicit, cloneInnerExplicit, "MarshalStructAsParam_AsExpByValInInnerExplicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.InnerArrayExplicitId:
                    InnerArrayExplicit sourceInnerArrayExplicit = Helper.NewInnerArrayExplicit(1, 1.0F, "some string1", "some string2");
                    InnerArrayExplicit cloneInnerArrayExplicit = Helper.NewInnerArrayExplicit(1, 1.0F, "some string1", "some string2");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValInInnerArrayExplicit...");
                    if (!MarshalStructAsParam_AsExpByValInInnerArrayExplicit(sourceInnerArrayExplicit))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValInInnerArrayExplicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerArrayExplicit(sourceInnerArrayExplicit, cloneInnerArrayExplicit, "MarshalStructAsParam_AsExpByValInInnerArrayExplicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.OUTER3Id:
                    OUTER3 sourceOUTER3 = Helper.NewOUTER3(1, 1.0F, "some string", "some string");
                    OUTER3 cloneOUTER3 = Helper.NewOUTER3(1, 1.0F, "some string", "some string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValInOUTER3...");
                    if (!MarshalStructAsParam_AsExpByValInOUTER3(sourceOUTER3))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValInOUTER3.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateOUTER3(sourceOUTER3, cloneOUTER3, "MarshalStructAsParam_AsExpByValInOUTER3"))
                    {
                        failures++;
                    }
                    break;
                case StructID.UId:
                    U sourceU = Helper.NewU(Int32.MinValue, UInt32.MaxValue, new IntPtr(-32), new UIntPtr(32), short.MinValue, ushort.MaxValue, byte.MinValue, sbyte.MaxValue, long.MinValue, ulong.MaxValue, 32.0F, 3.2);
                    U cloneU = Helper.NewU(Int32.MinValue, UInt32.MaxValue, new IntPtr(-32), new UIntPtr(32), short.MinValue, ushort.MaxValue, byte.MinValue, sbyte.MaxValue, long.MinValue, ulong.MaxValue, 32.0F, 3.2);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValInU...");
                    if (!MarshalStructAsParam_AsExpByValInU(sourceU))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValInU.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateU(sourceU, cloneU, "MarshalStructAsParam_AsExpByValInU"))
                    {
                        failures++;
                    }
                    break;
                case StructID.ByteStructPack2ExplicitId:
                    ByteStructPack2Explicit source_bspe = Helper.NewByteStructPack2Explicit(32, 32);
                    ByteStructPack2Explicit clone_bspe = Helper.NewByteStructPack2Explicit(32, 32);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValInByteStructPack2Explicit...");
                    if (!MarshalStructAsParam_AsExpByValInByteStructPack2Explicit(source_bspe))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValInByteStructPack2Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateByteStructPack2Explicit(source_bspe, clone_bspe, "MarshalStructAsParam_AsExpByValInByteStructPack2Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.ShortStructPack4ExplicitId:
                    ShortStructPack4Explicit source_sspe = Helper.NewShortStructPack4Explicit(32, 32);
                    ShortStructPack4Explicit clone_sspe = Helper.NewShortStructPack4Explicit(32, 32);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValInShortStructPack4Explicit...");
                    if (!MarshalStructAsParam_AsExpByValInShortStructPack4Explicit(source_sspe))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValInShortStructPack4Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateShortStructPack4Explicit(source_sspe, clone_sspe, "MarshalStructAsParam_AsExpByValInShortStructPack4Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.IntStructPack8ExplicitId:
                    IntStructPack8Explicit source_ispe = Helper.NewIntStructPack8Explicit(32, 32);
                    IntStructPack8Explicit clone_ispe = Helper.NewIntStructPack8Explicit(32, 32);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValInIntStructPack8Explicit...");
                    if (!MarshalStructAsParam_AsExpByValInIntStructPack8Explicit(source_ispe))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValInIntStructPack8Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateIntStructPack8Explicit(source_ispe, clone_ispe, "MarshalStructAsParam_AsExpByValInIntStructPack8Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.LongStructPack16ExplicitId:
                    LongStructPack16Explicit sourceLongStructPack16Explicit = Helper.NewLongStructPack16Explicit(32, 32);
                    LongStructPack16Explicit cloneLongStructPack16Explicit = Helper.NewLongStructPack16Explicit(32, 32);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValInLongStructPack16Explicit...");
                    if (!MarshalStructAsParam_AsExpByValInLongStructPack16Explicit(sourceLongStructPack16Explicit))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValInLongStructPack16Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateLongStructPack16Explicit(sourceLongStructPack16Explicit, cloneLongStructPack16Explicit, "MarshalStructAsParam_AsExpByValInLongStructPack16Explicit"))
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
    private static void MarshalStructAsParam_AsExpByRefIn(StructID id)
    {
        try
        {
            switch (id)
            {
                case StructID.INNER2Id:
                    INNER2 sourceINNER2 = Helper.NewINNER2(1, 1.0F, "some string");
                    INNER2 changeINNER2 = Helper.NewINNER2(1, 1.0F, "some string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefInINNER2...");
                    if (!MarshalStructAsParam_AsExpByRefInINNER2(ref sourceINNER2))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefInINNER2.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateINNER2(sourceINNER2, changeINNER2, "MarshalStructAsParam_AsExpByRefInINNER2"))
                    {
                        failures++;
                    }
                    break;
                case StructID.InnerExplicitId:
                    InnerExplicit sourceInnerExplicit = new InnerExplicit();
                    sourceInnerExplicit.f1 = 1;
                    sourceInnerExplicit.f3 = "some string";
                    InnerExplicit changeInnerExplicit = new InnerExplicit();
                    changeInnerExplicit.f1 = 1;
                    changeInnerExplicit.f3 = "some string";

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefInInnerExplicit...");
                    if (!MarshalStructAsParam_AsExpByRefInInnerExplicit(ref sourceInnerExplicit))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefInInnerExplicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerExplicit(sourceInnerExplicit, changeInnerExplicit, "MarshalStructAsParam_AsExpByRefInInnerExplicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.InnerArrayExplicitId:
                    InnerArrayExplicit sourceInnerArrayExplicit = Helper.NewInnerArrayExplicit(1, 1.0F, "some string1", "some string2");
                    InnerArrayExplicit changeInnerArrayExplicit = Helper.NewInnerArrayExplicit(1, 1.0F, "some string1", "some string2");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefInInnerArrayExplicit...");
                    if (!MarshalStructAsParam_AsExpByRefInInnerArrayExplicit(ref sourceInnerArrayExplicit))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefInInnerArrayExplicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerArrayExplicit(sourceInnerArrayExplicit, changeInnerArrayExplicit, "MarshalStructAsParam_AsExpByRefInInnerArrayExplicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.OUTER3Id:
                    OUTER3 sourceOUTER3 = Helper.NewOUTER3(1, 1.0F, "some string", "some string");
                    OUTER3 changeOUTER3 = Helper.NewOUTER3(1, 1.0F, "some string", "some string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefInOUTER3...");
                    if (!MarshalStructAsParam_AsExpByRefInOUTER3(ref sourceOUTER3))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefInOUTER3.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateOUTER3(sourceOUTER3, changeOUTER3, "MarshalStructAsParam_AsExpByRefInOUTER3"))
                    {
                        failures++;
                    }
                    break;
                case StructID.UId:
                    U sourceU = Helper.NewU(Int32.MinValue, UInt32.MaxValue, new IntPtr(-32), new UIntPtr(32), short.MinValue, ushort.MaxValue, byte.MinValue, sbyte.MaxValue, long.MinValue, ulong.MaxValue, 32.0F, 3.2);
                    U changeU = Helper.NewU(Int32.MaxValue, UInt32.MinValue, new IntPtr(-64), new UIntPtr(64), short.MaxValue, ushort.MinValue, byte.MaxValue, sbyte.MinValue, long.MaxValue, ulong.MinValue, 64.0F, 6.4);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefInU...");
                    if (!MarshalStructAsParam_AsExpByRefInU(ref sourceU))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefInU.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateU(sourceU, changeU, "MarshalStructAsParam_AsExpByRefInU"))
                    {
                        failures++;
                    }
                    break;
                case StructID.ByteStructPack2ExplicitId:
                    ByteStructPack2Explicit source_bspe = Helper.NewByteStructPack2Explicit(32, 32);
                    ByteStructPack2Explicit change_bspe = Helper.NewByteStructPack2Explicit(64, 64);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefInByteStructPack2Explicit...");
                    if (!MarshalStructAsParam_AsExpByRefInByteStructPack2Explicit(ref source_bspe))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefInByteStructPack2Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateByteStructPack2Explicit(source_bspe, change_bspe, "MarshalStructAsParam_AsExpByRefInByteStructPack2Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.ShortStructPack4ExplicitId:
                    ShortStructPack4Explicit source_sspe = Helper.NewShortStructPack4Explicit(32, 32);
                    ShortStructPack4Explicit change_sspe = Helper.NewShortStructPack4Explicit(64, 64);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefInShortStructPack4Explicit...");
                    if (!MarshalStructAsParam_AsExpByRefInShortStructPack4Explicit(ref source_sspe))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefInShortStructPack4Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateShortStructPack4Explicit(source_sspe, change_sspe, "MarshalStructAsParam_AsExpByRefInShortStructPack4Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.IntStructPack8ExplicitId:
                    IntStructPack8Explicit source_ispe = Helper.NewIntStructPack8Explicit(32, 32);
                    IntStructPack8Explicit change_ispe = Helper.NewIntStructPack8Explicit(64, 64);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefInIntStructPack8Explicit...");
                    if (!MarshalStructAsParam_AsExpByRefInIntStructPack8Explicit(ref source_ispe))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefInIntStructPack8Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateIntStructPack8Explicit(source_ispe, change_ispe, "MarshalStructAsParam_AsExpByRefInIntStructPack8Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.LongStructPack16ExplicitId:
                    LongStructPack16Explicit sourceLongStructPack16Explicit = Helper.NewLongStructPack16Explicit(32, 32);
                    LongStructPack16Explicit changeLongStructPack16Explicit = Helper.NewLongStructPack16Explicit(64, 64);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefInLongStructPack16Explicit...");
                    if (!MarshalStructAsParam_AsExpByRefInLongStructPack16Explicit(ref sourceLongStructPack16Explicit))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefInLongStructPack16Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateLongStructPack16Explicit(sourceLongStructPack16Explicit, changeLongStructPack16Explicit, "MarshalStructAsParam_AsExpByRefInLongStructPack16Explicit"))
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
    private static void MarshalStructAsParam_AsExpByValOut(StructID id)
    {
        try
        {
            switch (id)
            {
                case StructID.INNER2Id:
                    INNER2 sourceINNER2 = Helper.NewINNER2(1, 1.0F, "some string");
                    INNER2 cloneINNER2 = Helper.NewINNER2(1, 1.0F, "some string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValOutINNER2...");
                    if (!MarshalStructAsParam_AsExpByValOutINNER2(sourceINNER2))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValOutINNER2.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateINNER2(sourceINNER2, cloneINNER2, "MarshalStructAsParam_AsExpByValOutINNER2"))
                    {
                        failures++;
                    }
                    break;
                case StructID.InnerExplicitId:
                    InnerExplicit sourceInnerExplicit = new InnerExplicit();
                    sourceInnerExplicit.f1 = 1;
                    sourceInnerExplicit.f3 = "some string";
                    InnerExplicit cloneInnerExplicit = new InnerExplicit();
                    cloneInnerExplicit.f1 = 1;
                    cloneInnerExplicit.f3 = "some string";

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValOutInnerExplicit...");
                    if (!MarshalStructAsParam_AsExpByValOutInnerExplicit(sourceInnerExplicit))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValOutInnerExplicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerExplicit(sourceInnerExplicit, cloneInnerExplicit, "MarshalStructAsParam_AsExpByValOutInnerExplicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.InnerArrayExplicitId:
                    InnerArrayExplicit sourceInnerArrayExplicit = Helper.NewInnerArrayExplicit(1, 1.0F, "some string1", "some string2");
                    InnerArrayExplicit cloneInnerArrayExplicit = Helper.NewInnerArrayExplicit(1, 1.0F, "some string1", "some string2");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValOutInnerArrayExplicit...");
                    if (!MarshalStructAsParam_AsExpByValOutInnerArrayExplicit(sourceInnerArrayExplicit))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValOutInnerArrayExplicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerArrayExplicit(sourceInnerArrayExplicit, cloneInnerArrayExplicit, "MarshalStructAsParam_AsExpByValOutInnerArrayExplicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.OUTER3Id:
                    OUTER3 sourceOUTER3 = Helper.NewOUTER3(1, 1.0F, "some string", "some string");
                    OUTER3 cloneOUTER3 = Helper.NewOUTER3(1, 1.0F, "some string", "some string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValOutOUTER3...");
                    if (!MarshalStructAsParam_AsExpByValOutOUTER3(sourceOUTER3))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValOutOUTER3.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateOUTER3(sourceOUTER3, cloneOUTER3, "MarshalStructAsParam_AsExpByValOutOUTER3"))
                    {
                        failures++;
                    }
                    break;
                case StructID.UId:
                    U sourceU = Helper.NewU(Int32.MinValue, UInt32.MaxValue, new IntPtr(-32), new UIntPtr(32), short.MinValue, ushort.MaxValue, byte.MinValue, sbyte.MaxValue, long.MinValue, ulong.MaxValue, 32.0F, 3.2);
                    U cloneU = Helper.NewU(Int32.MinValue, UInt32.MaxValue, new IntPtr(-32), new UIntPtr(32), short.MinValue, ushort.MaxValue, byte.MinValue, sbyte.MaxValue, long.MinValue, ulong.MaxValue, 32.0F, 3.2);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValOutU...");
                    if (!MarshalStructAsParam_AsExpByValOutU(sourceU))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValOutU.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateU(sourceU, cloneU, "MarshalStructAsParam_AsExpByValOutU"))
                    {
                        failures++;
                    }
                    break;
                case StructID.ByteStructPack2ExplicitId:
                    ByteStructPack2Explicit source_bspe = Helper.NewByteStructPack2Explicit(32, 32);
                    ByteStructPack2Explicit clone_bspe = Helper.NewByteStructPack2Explicit(32, 32);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValOutByteStructPack2Explicit...");
                    if (!MarshalStructAsParam_AsExpByValOutByteStructPack2Explicit(source_bspe))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValOutByteStructPack2Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateByteStructPack2Explicit(source_bspe, clone_bspe, "MarshalStructAsParam_AsExpByValOutByteStructPack2Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.ShortStructPack4ExplicitId:
                    ShortStructPack4Explicit source_sspe = Helper.NewShortStructPack4Explicit(32, 32);
                    ShortStructPack4Explicit clone_sspe = Helper.NewShortStructPack4Explicit(32, 32);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValOutShortStructPack4Explicit...");
                    if (!MarshalStructAsParam_AsExpByValOutShortStructPack4Explicit(source_sspe))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValOutShortStructPack4Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateShortStructPack4Explicit(source_sspe, clone_sspe, "MarshalStructAsParam_AsExpByValOutShortStructPack4Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.IntStructPack8ExplicitId:
                    IntStructPack8Explicit source_ispe = Helper.NewIntStructPack8Explicit(32, 32);
                    IntStructPack8Explicit clone_ispe = Helper.NewIntStructPack8Explicit(32, 32);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValOutIntStructPack8Explicit...");
                    if (!MarshalStructAsParam_AsExpByValOutIntStructPack8Explicit(source_ispe))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValOutIntStructPack8Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateIntStructPack8Explicit(source_ispe, clone_ispe, "MarshalStructAsParam_AsExpByValOutIntStructPack8Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.LongStructPack16ExplicitId:
                    LongStructPack16Explicit sourceLongStructPack16Explicit = Helper.NewLongStructPack16Explicit(32, 32);
                    LongStructPack16Explicit cloneLongStructPack16Explicit = Helper.NewLongStructPack16Explicit(32, 32);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValOutLongStructPack16Explicit...");
                    if (!MarshalStructAsParam_AsExpByValOutLongStructPack16Explicit(sourceLongStructPack16Explicit))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValOutLongStructPack16Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateLongStructPack16Explicit(sourceLongStructPack16Explicit, cloneLongStructPack16Explicit, "MarshalStructAsParam_AsExpByValOutLongStructPack16Explicit"))
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
    private static void MarshalStructAsParam_AsExpByRefOut(StructID id)
    {
        try
        {
            switch (id)
            {
                case StructID.INNER2Id:
                    INNER2 sourceINNER2 = Helper.NewINNER2(1, 1.0F, "some string");
                    INNER2 changeINNER2 = Helper.NewINNER2(77, 77.0F, "changed string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefOutINNER2...");
                    if (!MarshalStructAsParam_AsExpByRefOutINNER2(out sourceINNER2))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefOutINNER2.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateINNER2(sourceINNER2, changeINNER2, "MarshalStructAsParam_AsExpByRefOutINNER2"))
                    {
                        failures++;
                    }
                    break;
                case StructID.InnerExplicitId:
                    InnerExplicit sourceInnerExplicit = new InnerExplicit();
                    sourceInnerExplicit.f1 = 1;
                    sourceInnerExplicit.f3 = "some string";
                    InnerExplicit changeInnerExplicit = new InnerExplicit();
                    changeInnerExplicit.f1 = 77;
                    changeInnerExplicit.f3 = "changed string";

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefOutInnerExplicit...");
                    if (!MarshalStructAsParam_AsExpByRefOutInnerExplicit(out sourceInnerExplicit))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefOutInnerExplicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerExplicit(sourceInnerExplicit, changeInnerExplicit, "MarshalStructAsParam_AsExpByRefOutInnerExplicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.InnerArrayExplicitId:
                    InnerArrayExplicit sourceInnerArrayExplicit = Helper.NewInnerArrayExplicit(1, 1.0F, "some string1", "some string2");
                    InnerArrayExplicit changeInnerArrayExplicit = Helper.NewInnerArrayExplicit(77, 77.0F, "change string1", "change string2");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefOutInnerArrayExplicit...");
                    if (!MarshalStructAsParam_AsExpByRefOutInnerArrayExplicit(out sourceInnerArrayExplicit))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefOutInnerArrayExplicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerArrayExplicit(sourceInnerArrayExplicit, changeInnerArrayExplicit, "MarshalStructAsParam_AsExpByRefOutInnerArrayExplicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.OUTER3Id:
                    OUTER3 sourceOUTER3 = Helper.NewOUTER3(1, 1.0F, "some string", "some string");
                    OUTER3 changeOUTER3 = Helper.NewOUTER3(77, 77.0F, "changed string", "changed string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefOutOUTER3...");
                    if (!MarshalStructAsParam_AsExpByRefOutOUTER3(out sourceOUTER3))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefOutOUTER3.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateOUTER3(sourceOUTER3, changeOUTER3, "MarshalStructAsParam_AsExpByRefOutOUTER3"))
                    {
                        failures++;
                    }
                    break;
                case StructID.UId:
                    U sourceU = Helper.NewU(Int32.MinValue, UInt32.MaxValue, new IntPtr(-32), new UIntPtr(32), short.MinValue, ushort.MaxValue, byte.MinValue, sbyte.MaxValue, long.MinValue, ulong.MaxValue, 32.0F, 3.2);
                    U changeU = Helper.NewU(Int32.MaxValue, UInt32.MinValue, new IntPtr(-64), new UIntPtr(64), short.MaxValue, ushort.MinValue, byte.MaxValue, sbyte.MinValue, long.MaxValue, ulong.MinValue, 64.0F, 6.4);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefOutU...");
                    if (!MarshalStructAsParam_AsExpByRefOutU(out sourceU))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefOutU.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateU(sourceU, changeU, "MarshalStructAsParam_AsExpByRefOutU"))
                    {
                        failures++;
                    }
                    break;
                case StructID.ByteStructPack2ExplicitId:
                    ByteStructPack2Explicit source_bspe = Helper.NewByteStructPack2Explicit(32, 32);
                    ByteStructPack2Explicit change_bspe = Helper.NewByteStructPack2Explicit(64, 64);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefOutByteStructPack2Explicit...");
                    if (!MarshalStructAsParam_AsExpByRefOutByteStructPack2Explicit(out source_bspe))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefOutByteStructPack2Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateByteStructPack2Explicit(source_bspe, change_bspe, "MarshalStructAsParam_AsExpByRefOutByteStructPack2Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.ShortStructPack4ExplicitId:
                    ShortStructPack4Explicit source_sspe = Helper.NewShortStructPack4Explicit(32, 32);
                    ShortStructPack4Explicit change_sspe = Helper.NewShortStructPack4Explicit(64, 64);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefOutShortStructPack4Explicit...");
                    if (!MarshalStructAsParam_AsExpByRefOutShortStructPack4Explicit(out source_sspe))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefOutShortStructPack4Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateShortStructPack4Explicit(source_sspe, change_sspe, "MarshalStructAsParam_AsExpByRefOutShortStructPack4Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.IntStructPack8ExplicitId:
                    IntStructPack8Explicit source_ispe = Helper.NewIntStructPack8Explicit(32, 32);
                    IntStructPack8Explicit change_ispe = Helper.NewIntStructPack8Explicit(64, 64);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefOutIntStructPack8Explicit...");
                    if (!MarshalStructAsParam_AsExpByRefOutIntStructPack8Explicit(out source_ispe))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefOutIntStructPack8Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateIntStructPack8Explicit(source_ispe, change_ispe, "MarshalStructAsParam_AsExpByRefOutIntStructPack8Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.LongStructPack16ExplicitId:
                    LongStructPack16Explicit sourceLongStructPack16Explicit = Helper.NewLongStructPack16Explicit(32, 32);
                    LongStructPack16Explicit changeLongStructPack16Explicit = Helper.NewLongStructPack16Explicit(64, 64);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefOutLongStructPack16Explicit...");
                    if (!MarshalStructAsParam_AsExpByRefOutLongStructPack16Explicit(out sourceLongStructPack16Explicit))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefOutLongStructPack16Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateLongStructPack16Explicit(sourceLongStructPack16Explicit, changeLongStructPack16Explicit, "MarshalStructAsParam_AsExpByRefOutLongStructPack16Explicit"))
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
    private static void MarshalStructAsParam_AsExpByValInOut(StructID id)
    {
        try
        {
            switch (id)
            {
                case StructID.INNER2Id:
                    INNER2 sourceINNER2 = Helper.NewINNER2(1, 1.0F, "some string");
                    INNER2 cloneINNER2 = Helper.NewINNER2(1, 1.0F, "some string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValInOutINNER2...");
                    if (!MarshalStructAsParam_AsExpByValInOutINNER2(sourceINNER2))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValInOutINNER2.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateINNER2(sourceINNER2, cloneINNER2, "MarshalStructAsParam_AsExpByValInOutINNER2"))
                    {
                        failures++;
                    }
                    break;
                case StructID.InnerExplicitId:
                    InnerExplicit sourceInnerExplicit = new InnerExplicit();
                    sourceInnerExplicit.f1 = 1;
                    sourceInnerExplicit.f3 = "some string";
                    InnerExplicit cloneInnerExplicit = new InnerExplicit();
                    cloneInnerExplicit.f1 = 1;
                    cloneInnerExplicit.f3 = "some string";

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValInOutInnerExplicit...");
                    if (!MarshalStructAsParam_AsExpByValInOutInnerExplicit(sourceInnerExplicit))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValInOutInnerExplicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerExplicit(sourceInnerExplicit, cloneInnerExplicit, "MarshalStructAsParam_AsExpByValInOutInnerExplicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.InnerArrayExplicitId:
                    InnerArrayExplicit sourceInnerArrayExplicit = Helper.NewInnerArrayExplicit(1, 1.0F, "some string1", "some string2");
                    InnerArrayExplicit cloneInnerArrayExplicit = Helper.NewInnerArrayExplicit(1, 1.0F, "some string1", "some string2");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValInOutInnerArrayExplicit...");
                    if (!MarshalStructAsParam_AsExpByValInOutInnerArrayExplicit(sourceInnerArrayExplicit))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValInOutInnerArrayExplicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerArrayExplicit(sourceInnerArrayExplicit, cloneInnerArrayExplicit, "MarshalStructAsParam_AsExpByValInOutInnerArrayExplicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.OUTER3Id:
                    OUTER3 sourceOUTER3 = Helper.NewOUTER3(1, 1.0F, "some string", "some string");
                    OUTER3 cloneOUTER3 = Helper.NewOUTER3(1, 1.0F, "some string", "some string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValInOutOUTER3...");
                    if (!MarshalStructAsParam_AsExpByValInOutOUTER3(sourceOUTER3))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValInOutOUTER3.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateOUTER3(sourceOUTER3, cloneOUTER3, "MarshalStructAsParam_AsExpByValInOutOUTER3"))
                    {
                        failures++;
                    }
                    break;
                case StructID.UId:
                    U sourceU = Helper.NewU(Int32.MinValue, UInt32.MaxValue, new IntPtr(-32), new UIntPtr(32), short.MinValue, ushort.MaxValue, byte.MinValue, sbyte.MaxValue, long.MinValue, ulong.MaxValue, 32.0F, 3.2);
                    U cloneU = Helper.NewU(Int32.MinValue, UInt32.MaxValue, new IntPtr(-32), new UIntPtr(32), short.MinValue, ushort.MaxValue, byte.MinValue, sbyte.MaxValue, long.MinValue, ulong.MaxValue, 32.0F, 3.2);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValInOutU...");
                    if (!MarshalStructAsParam_AsExpByValInOutU(sourceU))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValInOutU.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateU(sourceU, cloneU, "MarshalStructAsParam_AsExpByValInOutU"))
                    {
                        failures++;
                    }
                    break;
                case StructID.ByteStructPack2ExplicitId:
                    ByteStructPack2Explicit source_bspe = Helper.NewByteStructPack2Explicit(32, 32);
                    ByteStructPack2Explicit clone_bspe = Helper.NewByteStructPack2Explicit(32, 32);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValInOutByteStructPack2Explicit...");
                    if (!MarshalStructAsParam_AsExpByValInOutByteStructPack2Explicit(source_bspe))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValInOutByteStructPack2Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateByteStructPack2Explicit(source_bspe, clone_bspe, "MarshalStructAsParam_AsExpByValInOutByteStructPack2Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.ShortStructPack4ExplicitId:
                    ShortStructPack4Explicit source_sspe = Helper.NewShortStructPack4Explicit(32, 32);
                    ShortStructPack4Explicit clone_sspe = Helper.NewShortStructPack4Explicit(32, 32);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValInOutShortStructPack4Explicit...");
                    if (!MarshalStructAsParam_AsExpByValInOutShortStructPack4Explicit(source_sspe))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValInOutShortStructPack4Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateShortStructPack4Explicit(source_sspe, clone_sspe, "MarshalStructAsParam_AsExpByValInOutShortStructPack4Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.IntStructPack8ExplicitId:
                    IntStructPack8Explicit source_ispe = Helper.NewIntStructPack8Explicit(32, 32);
                    IntStructPack8Explicit clone_ispe = Helper.NewIntStructPack8Explicit(32, 32);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValInOutIntStructPack8Explicit...");
                    if (!MarshalStructAsParam_AsExpByValInOutIntStructPack8Explicit(source_ispe))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValInOutIntStructPack8Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateIntStructPack8Explicit(source_ispe, clone_ispe, "MarshalStructAsParam_AsExpByValInOutIntStructPack8Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.LongStructPack16ExplicitId:
                    LongStructPack16Explicit sourceLongStructPack16Explicit = Helper.NewLongStructPack16Explicit(32, 32);
                    LongStructPack16Explicit cloneLongStructPack16Explicit = Helper.NewLongStructPack16Explicit(32, 32);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByValInOutLongStructPack16Explicit...");
                    if (!MarshalStructAsParam_AsExpByValInOutLongStructPack16Explicit(sourceLongStructPack16Explicit))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByValInOutLongStructPack16Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateLongStructPack16Explicit(sourceLongStructPack16Explicit, cloneLongStructPack16Explicit, "MarshalStructAsParam_AsExpByValInOutLongStructPack16Explicit"))
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
    private static void MarshalStructAsParam_AsExpByRefInOut(StructID id)
    {
        try
        {
            switch (id)
            {
                case StructID.INNER2Id:
                    INNER2 sourceINNER2 = Helper.NewINNER2(1, 1.0F, "some string");
                    INNER2 changeINNER2 = Helper.NewINNER2(77, 77.0F, "changed string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefInOutINNER2...");
                    if (!MarshalStructAsParam_AsExpByRefInOutINNER2(ref sourceINNER2))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefInOutINNER2.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateINNER2(sourceINNER2, changeINNER2, "MarshalStructAsParam_AsExpByRefInOutINNER2"))
                    {
                        failures++;
                    }
                    break;
                case StructID.InnerExplicitId:
                    InnerExplicit sourceInnerExplicit = new InnerExplicit();
                    sourceInnerExplicit.f1 = 1;
                    sourceInnerExplicit.f3 = "some string";
                    InnerExplicit changeInnerExplicit = new InnerExplicit();
                    changeInnerExplicit.f1 = 77;
                    changeInnerExplicit.f3 = "changed string";

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefInOutInnerExplicit...");
                    if (!MarshalStructAsParam_AsExpByRefInOutInnerExplicit(ref sourceInnerExplicit))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefInOutInnerExplicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerExplicit(sourceInnerExplicit, changeInnerExplicit, "MarshalStructAsParam_AsExpByRefInOutInnerExplicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.InnerArrayExplicitId:
                    InnerArrayExplicit sourceInnerArrayExplicit = Helper.NewInnerArrayExplicit(1, 1.0F, "some string1", "some string2");
                    InnerArrayExplicit changeInnerArrayExplicit = Helper.NewInnerArrayExplicit(77, 77.0F, "change string1", "change string2");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefInOutInnerArrayExplicit...");
                    if (!MarshalStructAsParam_AsExpByRefInOutInnerArrayExplicit(ref sourceInnerArrayExplicit))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefInOutInnerArrayExplicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateInnerArrayExplicit(sourceInnerArrayExplicit, changeInnerArrayExplicit, "MarshalStructAsParam_AsExpByRefInOutInnerArrayExplicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.OUTER3Id:
                    OUTER3 sourceOUTER3 = Helper.NewOUTER3(1, 1.0F, "some string", "some string");
                    OUTER3 changeOUTER3 = Helper.NewOUTER3(77, 77.0F, "changed string", "changed string");

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefInOutOUTER3...");
                    if (!MarshalStructAsParam_AsExpByRefInOutOUTER3(ref sourceOUTER3))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefInOutOUTER3.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateOUTER3(sourceOUTER3, changeOUTER3, "MarshalStructAsParam_AsExpByRefInOutOUTER3"))
                    {
                        failures++;
                    }
                    break;
                case StructID.UId:
                    U sourceU = Helper.NewU(Int32.MinValue, UInt32.MaxValue, new IntPtr(-32), new UIntPtr(32), short.MinValue, ushort.MaxValue, byte.MinValue, sbyte.MaxValue, long.MinValue, ulong.MaxValue, 32.0F, 3.2);
                    U changeU = Helper.NewU(Int32.MaxValue, UInt32.MinValue, new IntPtr(-64), new UIntPtr(64), short.MaxValue, ushort.MinValue, byte.MaxValue, sbyte.MinValue, long.MaxValue, ulong.MinValue, 64.0F, 6.4);

                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefInOutU...");
                    if (!MarshalStructAsParam_AsExpByRefInOutU(ref sourceU))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefInOutU.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateU(sourceU, changeU, "MarshalStructAsParam_AsExpByRefInOutU"))
                    {
                        failures++;
                    }
                    break;
                case StructID.ByteStructPack2ExplicitId:
                    ByteStructPack2Explicit source_bspe = Helper.NewByteStructPack2Explicit(32, 32);
                    ByteStructPack2Explicit change_bspe = Helper.NewByteStructPack2Explicit(64, 64);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefInOutByteStructPack2Explicit...");
                    if (!MarshalStructAsParam_AsExpByRefInOutByteStructPack2Explicit(ref source_bspe))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefInOutByteStructPack2Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateByteStructPack2Explicit(source_bspe, change_bspe, "MarshalStructAsParam_AsExpByRefInOutByteStructPack2Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.ShortStructPack4ExplicitId:
                    ShortStructPack4Explicit source_sspe = Helper.NewShortStructPack4Explicit(32, 32);
                    ShortStructPack4Explicit change_sspe = Helper.NewShortStructPack4Explicit(64, 64);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefInOutShortStructPack4Explicit...");
                    if (!MarshalStructAsParam_AsExpByRefInOutShortStructPack4Explicit(ref source_sspe))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefInOutShortStructPack4Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateShortStructPack4Explicit(source_sspe, change_sspe, "MarshalStructAsParam_AsExpByRefInOutShortStructPack4Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.IntStructPack8ExplicitId:
                    IntStructPack8Explicit source_ispe = Helper.NewIntStructPack8Explicit(32, 32);
                    IntStructPack8Explicit change_ispe = Helper.NewIntStructPack8Explicit(64, 64);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefInOutIntStructPack8Explicit...");
                    if (!MarshalStructAsParam_AsExpByRefInOutIntStructPack8Explicit(ref source_ispe))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefInOutIntStructPack8Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateIntStructPack8Explicit(source_ispe, change_ispe, "MarshalStructAsParam_AsExpByRefInOutIntStructPack8Explicit"))
                    {
                        failures++;
                    }
                    break;
                case StructID.LongStructPack16ExplicitId:
                    LongStructPack16Explicit sourceLongStructPack16Explicit = Helper.NewLongStructPack16Explicit(32, 32);
                    LongStructPack16Explicit changeLongStructPack16Explicit = Helper.NewLongStructPack16Explicit(64, 64);
                    Console.WriteLine("\tCalling MarshalStructAsParam_AsExpByRefInOutLongStructPack16Explicit...");
                    if (!MarshalStructAsParam_AsExpByRefInOutLongStructPack16Explicit(ref sourceLongStructPack16Explicit))
                    {
                        Console.WriteLine("\tFAILED! Managed to Native failed in MarshalStructAsParam_AsExpByRefInOutLongStructPack16Explicit.Expected:True;Actual:False");
                        failures++;
                    }
                    if (!Helper.ValidateLongStructPack16Explicit(sourceLongStructPack16Explicit, changeLongStructPack16Explicit, "MarshalStructAsParam_AsExpByRefInOutLongStructPack16Explicit"))
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
    private static void RunMarshalStructAsParamAsExpByVal()
    {
        Console.WriteLine("\nVerify marshal Explicit layout struct as param as ByVal");
        MarshalStructAsParam_AsExpByVal(StructID.INNER2Id);
        MarshalStructAsParam_AsExpByVal(StructID.InnerExplicitId);
        MarshalStructAsParam_AsExpByVal(StructID.InnerArrayExplicitId);
        MarshalStructAsParam_AsExpByVal(StructID.OUTER3Id);
        MarshalStructAsParam_AsExpByVal(StructID.UId);
        MarshalStructAsParam_AsExpByVal(StructID.ByteStructPack2ExplicitId);
        MarshalStructAsParam_AsExpByVal(StructID.ShortStructPack4ExplicitId);
        MarshalStructAsParam_AsExpByVal(StructID.IntStructPack8ExplicitId);
        MarshalStructAsParam_AsExpByVal(StructID.LongStructPack16ExplicitId);
        MarshalStructAsParam_AsExpByVal(StructID.OverlappingLongFloatId);
        MarshalStructAsParam_AsExpByVal(StructID.OverlappingMultipleEightbyteId);
        MarshalStructAsParam_AsExpByVal(StructID.HFAId);
    }

    [SecuritySafeCritical]
    private static void RunMarshalStructAsParamAsExpByRef()
    {
        Console.WriteLine("\nVerify marshal Explicit layout struct as param as ByRef");
        MarshalStructAsParam_AsExpByRef(StructID.INNER2Id);
        MarshalStructAsParam_AsExpByRef(StructID.InnerExplicitId);
        MarshalStructAsParam_AsExpByRef(StructID.InnerArrayExplicitId);
        MarshalStructAsParam_AsExpByRef(StructID.OUTER3Id);
        MarshalStructAsParam_AsExpByRef(StructID.UId);
        MarshalStructAsParam_AsExpByRef(StructID.ByteStructPack2ExplicitId);
        MarshalStructAsParam_AsExpByRef(StructID.ShortStructPack4ExplicitId);
        MarshalStructAsParam_AsExpByRef(StructID.IntStructPack8ExplicitId);
        MarshalStructAsParam_AsExpByRef(StructID.LongStructPack16ExplicitId);
    }

    [SecuritySafeCritical]
    private static void RunMarshalStructAsParamAsExpByValIn()
    {
        Console.WriteLine("\nVerify marshal Explicit layout struct as param as ByValIn");
        MarshalStructAsParam_AsExpByValIn(StructID.INNER2Id);
        MarshalStructAsParam_AsExpByValIn(StructID.InnerExplicitId);
        MarshalStructAsParam_AsExpByValIn(StructID.InnerArrayExplicitId);
        MarshalStructAsParam_AsExpByValIn(StructID.OUTER3Id);
        MarshalStructAsParam_AsExpByValIn(StructID.UId);
        MarshalStructAsParam_AsExpByValIn(StructID.ByteStructPack2ExplicitId);
        MarshalStructAsParam_AsExpByValIn(StructID.ShortStructPack4ExplicitId);
        MarshalStructAsParam_AsExpByValIn(StructID.IntStructPack8ExplicitId);
        MarshalStructAsParam_AsExpByValIn(StructID.LongStructPack16ExplicitId);
    }

    [SecuritySafeCritical]
    private static void RunMarshalStructAsParamAsExpByRefIn()
    {
        Console.WriteLine("\nVerify marshal Explicit layout struct as param as ByRefIn");
        MarshalStructAsParam_AsExpByRefIn(StructID.INNER2Id);
        MarshalStructAsParam_AsExpByRefIn(StructID.InnerExplicitId);
        MarshalStructAsParam_AsExpByRefIn(StructID.InnerArrayExplicitId);
        MarshalStructAsParam_AsExpByRefIn(StructID.OUTER3Id);
        MarshalStructAsParam_AsExpByRefIn(StructID.UId);
        MarshalStructAsParam_AsExpByRefIn(StructID.ByteStructPack2ExplicitId);
        MarshalStructAsParam_AsExpByRefIn(StructID.ShortStructPack4ExplicitId);
        MarshalStructAsParam_AsExpByRefIn(StructID.IntStructPack8ExplicitId);
        MarshalStructAsParam_AsExpByRefIn(StructID.LongStructPack16ExplicitId);
    }

    [SecuritySafeCritical]
    private static void RunMarshalStructAsParamAsExpByValOut()
    {
        Console.WriteLine("\nVerify marshal Explicit layout struct as param as ByValOut");
        MarshalStructAsParam_AsExpByValOut(StructID.INNER2Id);
        MarshalStructAsParam_AsExpByValOut(StructID.InnerExplicitId);
        MarshalStructAsParam_AsExpByValOut(StructID.InnerArrayExplicitId);
        MarshalStructAsParam_AsExpByValOut(StructID.OUTER3Id);
        MarshalStructAsParam_AsExpByValOut(StructID.UId);
        MarshalStructAsParam_AsExpByValOut(StructID.ByteStructPack2ExplicitId);
        MarshalStructAsParam_AsExpByValOut(StructID.ShortStructPack4ExplicitId);
        MarshalStructAsParam_AsExpByValOut(StructID.IntStructPack8ExplicitId);
        MarshalStructAsParam_AsExpByValOut(StructID.LongStructPack16ExplicitId);
    }

    [SecuritySafeCritical]
    private static void RunMarshalStructAsParamAsExpByRefOut()
    {
        Console.WriteLine("\nVerify marshal Explicit layout struct as param as ByRefOut");
        MarshalStructAsParam_AsExpByRefOut(StructID.INNER2Id);
        MarshalStructAsParam_AsExpByRefOut(StructID.InnerExplicitId);
        MarshalStructAsParam_AsExpByRefOut(StructID.InnerArrayExplicitId);
        MarshalStructAsParam_AsExpByRefOut(StructID.OUTER3Id);
        MarshalStructAsParam_AsExpByRefOut(StructID.UId);
        MarshalStructAsParam_AsExpByRefOut(StructID.ByteStructPack2ExplicitId);
        MarshalStructAsParam_AsExpByRefOut(StructID.ShortStructPack4ExplicitId);
        MarshalStructAsParam_AsExpByRefOut(StructID.IntStructPack8ExplicitId);
        MarshalStructAsParam_AsExpByRefOut(StructID.LongStructPack16ExplicitId);
    }

    [SecuritySafeCritical]
    private static void RunMarshalStructAsParamAsExpByValInOut()
    {
        Console.WriteLine("\nVerify marshal Explicit layout struct as param as ByValInOut");
        MarshalStructAsParam_AsExpByValInOut(StructID.INNER2Id);
        MarshalStructAsParam_AsExpByValInOut(StructID.InnerExplicitId);
        MarshalStructAsParam_AsExpByValInOut(StructID.InnerArrayExplicitId);
        MarshalStructAsParam_AsExpByValInOut(StructID.OUTER3Id);
        MarshalStructAsParam_AsExpByValInOut(StructID.UId);
        MarshalStructAsParam_AsExpByValInOut(StructID.ByteStructPack2ExplicitId);
        MarshalStructAsParam_AsExpByValInOut(StructID.ShortStructPack4ExplicitId);
        MarshalStructAsParam_AsExpByValInOut(StructID.IntStructPack8ExplicitId);
        MarshalStructAsParam_AsExpByValInOut(StructID.LongStructPack16ExplicitId);
    }

    [SecuritySafeCritical]
    private static void RunMarshalStructAsParamAsExpByRefInOut()
    {
        Console.WriteLine("\nVerify marshal Explicit layout struct as param as ByRefInOut");
        MarshalStructAsParam_AsExpByRefInOut(StructID.INNER2Id);
        MarshalStructAsParam_AsExpByRefInOut(StructID.InnerExplicitId);
        MarshalStructAsParam_AsExpByRefInOut(StructID.InnerArrayExplicitId);
        MarshalStructAsParam_AsExpByRefInOut(StructID.OUTER3Id);
        MarshalStructAsParam_AsExpByRefInOut(StructID.UId);
        MarshalStructAsParam_AsExpByRefInOut(StructID.ByteStructPack2ExplicitId);
        MarshalStructAsParam_AsExpByRefInOut(StructID.ShortStructPack4ExplicitId);
        MarshalStructAsParam_AsExpByRefInOut(StructID.IntStructPack8ExplicitId);
        MarshalStructAsParam_AsExpByRefInOut(StructID.LongStructPack16ExplicitId);
    }

    private static void RunMarshalStructAsReturn()
    {
        Console.WriteLine("\nVerify marshal Explicit layout struct as return.");

        LongStructPack16Explicit longStruct = GetLongStruct(123456, 78910);
        if(longStruct.l1 != 123456 || longStruct.l2 != 78910)
        {
            Console.WriteLine("Failed to return LongStructPack16Explicit.");
            failures++;
        }

        IntStructPack8Explicit intStruct = GetIntStruct(12345, 678910);
        if(intStruct.i1 != 12345 || intStruct.i2 != 678910)
        {
            Console.WriteLine("Failed to return IntStructPack8Explicit.");
            failures++;
        }
    }
}
