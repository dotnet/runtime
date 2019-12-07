using System.Runtime.CompilerServices;
using TestType = System.Int32;

public class ConstantFoldingTestsBase
{
    public const TestType Const1 = 8;
    public const TestType Const2 = 1027;

    [MethodImpl(MethodImplOptions.NoInlining)] public TestType GetConst1() => Const1;
    [MethodImpl(MethodImplOptions.NoInlining)] public TestType GetConst2() => Const2;
}

// ((x op icon1) op icon2)
public class ConstantFoldingTests1 : ConstantFoldingTestsBase
{
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_1_cns(TestType x) => ((x + Const1) + Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_1_var(TestType x) => ((x + GetConst1()) + GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_2_cns(TestType x) => ((x + Const1) - Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_2_var(TestType x) => ((x + GetConst1()) - GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_3_cns(TestType x) => ((x + Const1) & Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_3_var(TestType x) => ((x + GetConst1()) & GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_4_cns(TestType x) => ((x + Const1) | Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_4_var(TestType x) => ((x + GetConst1()) | GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_5_cns(TestType x) => ((x + Const1) ^ Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_5_var(TestType x) => ((x + GetConst1()) ^ GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_6_cns(TestType x) => ((x + Const1) >> Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_6_var(TestType x) => ((x + GetConst1()) >> GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_7_cns(TestType x) => ((x + Const1) << Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_7_var(TestType x) => ((x + GetConst1()) << GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_8_cns(TestType x) => ((x + Const1) * Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_8_var(TestType x) => ((x + GetConst1()) * GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_9_cns(TestType x) => ((x + Const1) / Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_9_var(TestType x) => ((x + GetConst1()) / GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_10_cns(TestType x) => ((x - Const1) + Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_10_var(TestType x) => ((x - GetConst1()) + GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_11_cns(TestType x) => ((x - Const1) - Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_11_var(TestType x) => ((x - GetConst1()) - GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_12_cns(TestType x) => ((x - Const1) & Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_12_var(TestType x) => ((x - GetConst1()) & GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_13_cns(TestType x) => ((x - Const1) | Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_13_var(TestType x) => ((x - GetConst1()) | GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_14_cns(TestType x) => ((x - Const1) ^ Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_14_var(TestType x) => ((x - GetConst1()) ^ GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_15_cns(TestType x) => ((x - Const1) >> Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_15_var(TestType x) => ((x - GetConst1()) >> GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_16_cns(TestType x) => ((x - Const1) << Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_16_var(TestType x) => ((x - GetConst1()) << GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_17_cns(TestType x) => ((x - Const1) * Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_17_var(TestType x) => ((x - GetConst1()) * GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_18_cns(TestType x) => ((x - Const1) / Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_18_var(TestType x) => ((x - GetConst1()) / GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_19_cns(TestType x) => ((x & Const1) + Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_19_var(TestType x) => ((x & GetConst1()) + GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_20_cns(TestType x) => ((x & Const1) - Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_20_var(TestType x) => ((x & GetConst1()) - GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_21_cns(TestType x) => ((x & Const1) & Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_21_var(TestType x) => ((x & GetConst1()) & GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_22_cns(TestType x) => ((x & Const1) | Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_22_var(TestType x) => ((x & GetConst1()) | GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_23_cns(TestType x) => ((x & Const1) ^ Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_23_var(TestType x) => ((x & GetConst1()) ^ GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_24_cns(TestType x) => ((x & Const1) >> Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_24_var(TestType x) => ((x & GetConst1()) >> GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_25_cns(TestType x) => ((x & Const1) << Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_25_var(TestType x) => ((x & GetConst1()) << GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_26_cns(TestType x) => ((x & Const1) * Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_26_var(TestType x) => ((x & GetConst1()) * GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_27_cns(TestType x) => ((x & Const1) / Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_27_var(TestType x) => ((x & GetConst1()) / GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_28_cns(TestType x) => ((x | Const1) + Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_28_var(TestType x) => ((x | GetConst1()) + GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_29_cns(TestType x) => ((x | Const1) - Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_29_var(TestType x) => ((x | GetConst1()) - GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_30_cns(TestType x) => ((x | Const1) & Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_30_var(TestType x) => ((x | GetConst1()) & GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_31_cns(TestType x) => ((x | Const1) | Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_31_var(TestType x) => ((x | GetConst1()) | GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_32_cns(TestType x) => ((x | Const1) ^ Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_32_var(TestType x) => ((x | GetConst1()) ^ GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_33_cns(TestType x) => ((x | Const1) >> Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_33_var(TestType x) => ((x | GetConst1()) >> GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_34_cns(TestType x) => ((x | Const1) << Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_34_var(TestType x) => ((x | GetConst1()) << GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_35_cns(TestType x) => ((x | Const1) * Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_35_var(TestType x) => ((x | GetConst1()) * GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_36_cns(TestType x) => ((x | Const1) / Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_36_var(TestType x) => ((x | GetConst1()) / GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_37_cns(TestType x) => ((x ^ Const1) + Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_37_var(TestType x) => ((x ^ GetConst1()) + GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_38_cns(TestType x) => ((x ^ Const1) - Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_38_var(TestType x) => ((x ^ GetConst1()) - GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_39_cns(TestType x) => ((x ^ Const1) & Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_39_var(TestType x) => ((x ^ GetConst1()) & GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_40_cns(TestType x) => ((x ^ Const1) | Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_40_var(TestType x) => ((x ^ GetConst1()) | GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_41_cns(TestType x) => ((x ^ Const1) ^ Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_41_var(TestType x) => ((x ^ GetConst1()) ^ GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_42_cns(TestType x) => ((x ^ Const1) >> Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_42_var(TestType x) => ((x ^ GetConst1()) >> GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_43_cns(TestType x) => ((x ^ Const1) << Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_43_var(TestType x) => ((x ^ GetConst1()) << GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_44_cns(TestType x) => ((x ^ Const1) * Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_44_var(TestType x) => ((x ^ GetConst1()) * GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_45_cns(TestType x) => ((x ^ Const1) / Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_45_var(TestType x) => ((x ^ GetConst1()) / GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_46_cns(TestType x) => ((x >> Const1) + Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_46_var(TestType x) => ((x >> GetConst1()) + GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_47_cns(TestType x) => ((x >> Const1) - Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_47_var(TestType x) => ((x >> GetConst1()) - GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_48_cns(TestType x) => ((x >> Const1) & Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_48_var(TestType x) => ((x >> GetConst1()) & GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_49_cns(TestType x) => ((x >> Const1) | Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_49_var(TestType x) => ((x >> GetConst1()) | GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_50_cns(TestType x) => ((x >> Const1) ^ Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_50_var(TestType x) => ((x >> GetConst1()) ^ GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_51_cns(TestType x) => ((x >> Const1) >> Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_51_var(TestType x) => ((x >> GetConst1()) >> GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_52_cns(TestType x) => ((x >> Const1) << Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_52_var(TestType x) => ((x >> GetConst1()) << GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_53_cns(TestType x) => ((x >> Const1) * Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_53_var(TestType x) => ((x >> GetConst1()) * GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_54_cns(TestType x) => ((x >> Const1) / Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_54_var(TestType x) => ((x >> GetConst1()) / GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_55_cns(TestType x) => ((x << Const1) + Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_55_var(TestType x) => ((x << GetConst1()) + GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_56_cns(TestType x) => ((x << Const1) - Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_56_var(TestType x) => ((x << GetConst1()) - GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_57_cns(TestType x) => ((x << Const1) & Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_57_var(TestType x) => ((x << GetConst1()) & GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_58_cns(TestType x) => ((x << Const1) | Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_58_var(TestType x) => ((x << GetConst1()) | GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_59_cns(TestType x) => ((x << Const1) ^ Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_59_var(TestType x) => ((x << GetConst1()) ^ GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_60_cns(TestType x) => ((x << Const1) >> Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_60_var(TestType x) => ((x << GetConst1()) >> GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_61_cns(TestType x) => ((x << Const1) << Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_61_var(TestType x) => ((x << GetConst1()) << GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_62_cns(TestType x) => ((x << Const1) * Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_62_var(TestType x) => ((x << GetConst1()) * GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_63_cns(TestType x) => ((x << Const1) / Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_63_var(TestType x) => ((x << GetConst1()) / GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_64_cns(TestType x) => ((x * Const1) + Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_64_var(TestType x) => ((x * GetConst1()) + GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_65_cns(TestType x) => ((x * Const1) - Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_65_var(TestType x) => ((x * GetConst1()) - GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_66_cns(TestType x) => ((x * Const1) & Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_66_var(TestType x) => ((x * GetConst1()) & GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_67_cns(TestType x) => ((x * Const1) | Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_67_var(TestType x) => ((x * GetConst1()) | GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_68_cns(TestType x) => ((x * Const1) ^ Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_68_var(TestType x) => ((x * GetConst1()) ^ GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_69_cns(TestType x) => ((x * Const1) >> Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_69_var(TestType x) => ((x * GetConst1()) >> GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_70_cns(TestType x) => ((x * Const1) << Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_70_var(TestType x) => ((x * GetConst1()) << GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_71_cns(TestType x) => ((x * Const1) * Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_71_var(TestType x) => ((x * GetConst1()) * GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_72_cns(TestType x) => ((x * Const1) / Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_72_var(TestType x) => ((x * GetConst1()) / GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_73_cns(TestType x) => ((x / Const1) + Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_73_var(TestType x) => ((x / GetConst1()) + GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_74_cns(TestType x) => ((x / Const1) - Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_74_var(TestType x) => ((x / GetConst1()) - GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_75_cns(TestType x) => ((x / Const1) & Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_75_var(TestType x) => ((x / GetConst1()) & GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_76_cns(TestType x) => ((x / Const1) | Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_76_var(TestType x) => ((x / GetConst1()) | GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_77_cns(TestType x) => ((x / Const1) ^ Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_77_var(TestType x) => ((x / GetConst1()) ^ GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_78_cns(TestType x) => ((x / Const1) >> Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_78_var(TestType x) => ((x / GetConst1()) >> GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_79_cns(TestType x) => ((x / Const1) << Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_79_var(TestType x) => ((x / GetConst1()) << GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_80_cns(TestType x) => ((x / Const1) * Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_80_var(TestType x) => ((x / GetConst1()) * GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_81_cns(TestType x) => ((x / Const1) / Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_81_var(TestType x) => ((x / GetConst1()) / GetConst2());

    public int RunTests()
    {
        int failures = 0;
        if (Case_1_cns(0) != Case_1_var(0)) failures++;
        if (Case_1_cns(42) != Case_1_var(42)) failures++;
        if (Case_1_cns(TestType.MinValue) != Case_1_var(TestType.MinValue)) failures++;
        if (Case_1_cns(TestType.MaxValue) != Case_1_var(TestType.MaxValue)) failures++;
        if (Case_2_cns(0) != Case_2_var(0)) failures++;
        if (Case_2_cns(42) != Case_2_var(42)) failures++;
        if (Case_2_cns(TestType.MinValue) != Case_2_var(TestType.MinValue)) failures++;
        if (Case_2_cns(TestType.MaxValue) != Case_2_var(TestType.MaxValue)) failures++;
        if (Case_3_cns(0) != Case_3_var(0)) failures++;
        if (Case_3_cns(42) != Case_3_var(42)) failures++;
        if (Case_3_cns(TestType.MinValue) != Case_3_var(TestType.MinValue)) failures++;
        if (Case_3_cns(TestType.MaxValue) != Case_3_var(TestType.MaxValue)) failures++;
        if (Case_4_cns(0) != Case_4_var(0)) failures++;
        if (Case_4_cns(42) != Case_4_var(42)) failures++;
        if (Case_4_cns(TestType.MinValue) != Case_4_var(TestType.MinValue)) failures++;
        if (Case_4_cns(TestType.MaxValue) != Case_4_var(TestType.MaxValue)) failures++;
        if (Case_5_cns(0) != Case_5_var(0)) failures++;
        if (Case_5_cns(42) != Case_5_var(42)) failures++;
        if (Case_5_cns(TestType.MinValue) != Case_5_var(TestType.MinValue)) failures++;
        if (Case_5_cns(TestType.MaxValue) != Case_5_var(TestType.MaxValue)) failures++;
        if (Case_6_cns(0) != Case_6_var(0)) failures++;
        if (Case_6_cns(42) != Case_6_var(42)) failures++;
        if (Case_6_cns(TestType.MinValue) != Case_6_var(TestType.MinValue)) failures++;
        if (Case_6_cns(TestType.MaxValue) != Case_6_var(TestType.MaxValue)) failures++;
        if (Case_7_cns(0) != Case_7_var(0)) failures++;
        if (Case_7_cns(42) != Case_7_var(42)) failures++;
        if (Case_7_cns(TestType.MinValue) != Case_7_var(TestType.MinValue)) failures++;
        if (Case_7_cns(TestType.MaxValue) != Case_7_var(TestType.MaxValue)) failures++;
        if (Case_8_cns(0) != Case_8_var(0)) failures++;
        if (Case_8_cns(42) != Case_8_var(42)) failures++;
        if (Case_8_cns(TestType.MinValue) != Case_8_var(TestType.MinValue)) failures++;
        if (Case_8_cns(TestType.MaxValue) != Case_8_var(TestType.MaxValue)) failures++;
        if (Case_9_cns(0) != Case_9_var(0)) failures++;
        if (Case_9_cns(42) != Case_9_var(42)) failures++;
        if (Case_9_cns(TestType.MinValue) != Case_9_var(TestType.MinValue)) failures++;
        if (Case_9_cns(TestType.MaxValue) != Case_9_var(TestType.MaxValue)) failures++;
        if (Case_10_cns(0) != Case_10_var(0)) failures++;
        if (Case_10_cns(42) != Case_10_var(42)) failures++;
        if (Case_10_cns(TestType.MinValue) != Case_10_var(TestType.MinValue)) failures++;
        if (Case_10_cns(TestType.MaxValue) != Case_10_var(TestType.MaxValue)) failures++;
        if (Case_11_cns(0) != Case_11_var(0)) failures++;
        if (Case_11_cns(42) != Case_11_var(42)) failures++;
        if (Case_11_cns(TestType.MinValue) != Case_11_var(TestType.MinValue)) failures++;
        if (Case_11_cns(TestType.MaxValue) != Case_11_var(TestType.MaxValue)) failures++;
        if (Case_12_cns(0) != Case_12_var(0)) failures++;
        if (Case_12_cns(42) != Case_12_var(42)) failures++;
        if (Case_12_cns(TestType.MinValue) != Case_12_var(TestType.MinValue)) failures++;
        if (Case_12_cns(TestType.MaxValue) != Case_12_var(TestType.MaxValue)) failures++;
        if (Case_13_cns(0) != Case_13_var(0)) failures++;
        if (Case_13_cns(42) != Case_13_var(42)) failures++;
        if (Case_13_cns(TestType.MinValue) != Case_13_var(TestType.MinValue)) failures++;
        if (Case_13_cns(TestType.MaxValue) != Case_13_var(TestType.MaxValue)) failures++;
        if (Case_14_cns(0) != Case_14_var(0)) failures++;
        if (Case_14_cns(42) != Case_14_var(42)) failures++;
        if (Case_14_cns(TestType.MinValue) != Case_14_var(TestType.MinValue)) failures++;
        if (Case_14_cns(TestType.MaxValue) != Case_14_var(TestType.MaxValue)) failures++;
        if (Case_15_cns(0) != Case_15_var(0)) failures++;
        if (Case_15_cns(42) != Case_15_var(42)) failures++;
        if (Case_15_cns(TestType.MinValue) != Case_15_var(TestType.MinValue)) failures++;
        if (Case_15_cns(TestType.MaxValue) != Case_15_var(TestType.MaxValue)) failures++;
        if (Case_16_cns(0) != Case_16_var(0)) failures++;
        if (Case_16_cns(42) != Case_16_var(42)) failures++;
        if (Case_16_cns(TestType.MinValue) != Case_16_var(TestType.MinValue)) failures++;
        if (Case_16_cns(TestType.MaxValue) != Case_16_var(TestType.MaxValue)) failures++;
        if (Case_17_cns(0) != Case_17_var(0)) failures++;
        if (Case_17_cns(42) != Case_17_var(42)) failures++;
        if (Case_17_cns(TestType.MinValue) != Case_17_var(TestType.MinValue)) failures++;
        if (Case_17_cns(TestType.MaxValue) != Case_17_var(TestType.MaxValue)) failures++;
        if (Case_18_cns(0) != Case_18_var(0)) failures++;
        if (Case_18_cns(42) != Case_18_var(42)) failures++;
        if (Case_18_cns(TestType.MinValue) != Case_18_var(TestType.MinValue)) failures++;
        if (Case_18_cns(TestType.MaxValue) != Case_18_var(TestType.MaxValue)) failures++;
        if (Case_19_cns(0) != Case_19_var(0)) failures++;
        if (Case_19_cns(42) != Case_19_var(42)) failures++;
        if (Case_19_cns(TestType.MinValue) != Case_19_var(TestType.MinValue)) failures++;
        if (Case_19_cns(TestType.MaxValue) != Case_19_var(TestType.MaxValue)) failures++;
        if (Case_20_cns(0) != Case_20_var(0)) failures++;
        if (Case_20_cns(42) != Case_20_var(42)) failures++;
        if (Case_20_cns(TestType.MinValue) != Case_20_var(TestType.MinValue)) failures++;
        if (Case_20_cns(TestType.MaxValue) != Case_20_var(TestType.MaxValue)) failures++;
        if (Case_21_cns(0) != Case_21_var(0)) failures++;
        if (Case_21_cns(42) != Case_21_var(42)) failures++;
        if (Case_21_cns(TestType.MinValue) != Case_21_var(TestType.MinValue)) failures++;
        if (Case_21_cns(TestType.MaxValue) != Case_21_var(TestType.MaxValue)) failures++;
        if (Case_22_cns(0) != Case_22_var(0)) failures++;
        if (Case_22_cns(42) != Case_22_var(42)) failures++;
        if (Case_22_cns(TestType.MinValue) != Case_22_var(TestType.MinValue)) failures++;
        if (Case_22_cns(TestType.MaxValue) != Case_22_var(TestType.MaxValue)) failures++;
        if (Case_23_cns(0) != Case_23_var(0)) failures++;
        if (Case_23_cns(42) != Case_23_var(42)) failures++;
        if (Case_23_cns(TestType.MinValue) != Case_23_var(TestType.MinValue)) failures++;
        if (Case_23_cns(TestType.MaxValue) != Case_23_var(TestType.MaxValue)) failures++;
        if (Case_24_cns(0) != Case_24_var(0)) failures++;
        if (Case_24_cns(42) != Case_24_var(42)) failures++;
        if (Case_24_cns(TestType.MinValue) != Case_24_var(TestType.MinValue)) failures++;
        if (Case_24_cns(TestType.MaxValue) != Case_24_var(TestType.MaxValue)) failures++;
        if (Case_25_cns(0) != Case_25_var(0)) failures++;
        if (Case_25_cns(42) != Case_25_var(42)) failures++;
        if (Case_25_cns(TestType.MinValue) != Case_25_var(TestType.MinValue)) failures++;
        if (Case_25_cns(TestType.MaxValue) != Case_25_var(TestType.MaxValue)) failures++;
        if (Case_26_cns(0) != Case_26_var(0)) failures++;
        if (Case_26_cns(42) != Case_26_var(42)) failures++;
        if (Case_26_cns(TestType.MinValue) != Case_26_var(TestType.MinValue)) failures++;
        if (Case_26_cns(TestType.MaxValue) != Case_26_var(TestType.MaxValue)) failures++;
        if (Case_27_cns(0) != Case_27_var(0)) failures++;
        if (Case_27_cns(42) != Case_27_var(42)) failures++;
        if (Case_27_cns(TestType.MinValue) != Case_27_var(TestType.MinValue)) failures++;
        if (Case_27_cns(TestType.MaxValue) != Case_27_var(TestType.MaxValue)) failures++;
        if (Case_28_cns(0) != Case_28_var(0)) failures++;
        if (Case_28_cns(42) != Case_28_var(42)) failures++;
        if (Case_28_cns(TestType.MinValue) != Case_28_var(TestType.MinValue)) failures++;
        if (Case_28_cns(TestType.MaxValue) != Case_28_var(TestType.MaxValue)) failures++;
        if (Case_29_cns(0) != Case_29_var(0)) failures++;
        if (Case_29_cns(42) != Case_29_var(42)) failures++;
        if (Case_29_cns(TestType.MinValue) != Case_29_var(TestType.MinValue)) failures++;
        if (Case_29_cns(TestType.MaxValue) != Case_29_var(TestType.MaxValue)) failures++;
        if (Case_30_cns(0) != Case_30_var(0)) failures++;
        if (Case_30_cns(42) != Case_30_var(42)) failures++;
        if (Case_30_cns(TestType.MinValue) != Case_30_var(TestType.MinValue)) failures++;
        if (Case_30_cns(TestType.MaxValue) != Case_30_var(TestType.MaxValue)) failures++;
        if (Case_31_cns(0) != Case_31_var(0)) failures++;
        if (Case_31_cns(42) != Case_31_var(42)) failures++;
        if (Case_31_cns(TestType.MinValue) != Case_31_var(TestType.MinValue)) failures++;
        if (Case_31_cns(TestType.MaxValue) != Case_31_var(TestType.MaxValue)) failures++;
        if (Case_32_cns(0) != Case_32_var(0)) failures++;
        if (Case_32_cns(42) != Case_32_var(42)) failures++;
        if (Case_32_cns(TestType.MinValue) != Case_32_var(TestType.MinValue)) failures++;
        if (Case_32_cns(TestType.MaxValue) != Case_32_var(TestType.MaxValue)) failures++;
        if (Case_33_cns(0) != Case_33_var(0)) failures++;
        if (Case_33_cns(42) != Case_33_var(42)) failures++;
        if (Case_33_cns(TestType.MinValue) != Case_33_var(TestType.MinValue)) failures++;
        if (Case_33_cns(TestType.MaxValue) != Case_33_var(TestType.MaxValue)) failures++;
        if (Case_34_cns(0) != Case_34_var(0)) failures++;
        if (Case_34_cns(42) != Case_34_var(42)) failures++;
        if (Case_34_cns(TestType.MinValue) != Case_34_var(TestType.MinValue)) failures++;
        if (Case_34_cns(TestType.MaxValue) != Case_34_var(TestType.MaxValue)) failures++;
        if (Case_35_cns(0) != Case_35_var(0)) failures++;
        if (Case_35_cns(42) != Case_35_var(42)) failures++;
        if (Case_35_cns(TestType.MinValue) != Case_35_var(TestType.MinValue)) failures++;
        if (Case_35_cns(TestType.MaxValue) != Case_35_var(TestType.MaxValue)) failures++;
        if (Case_36_cns(0) != Case_36_var(0)) failures++;
        if (Case_36_cns(42) != Case_36_var(42)) failures++;
        if (Case_36_cns(TestType.MinValue) != Case_36_var(TestType.MinValue)) failures++;
        if (Case_36_cns(TestType.MaxValue) != Case_36_var(TestType.MaxValue)) failures++;
        if (Case_37_cns(0) != Case_37_var(0)) failures++;
        if (Case_37_cns(42) != Case_37_var(42)) failures++;
        if (Case_37_cns(TestType.MinValue) != Case_37_var(TestType.MinValue)) failures++;
        if (Case_37_cns(TestType.MaxValue) != Case_37_var(TestType.MaxValue)) failures++;
        if (Case_38_cns(0) != Case_38_var(0)) failures++;
        if (Case_38_cns(42) != Case_38_var(42)) failures++;
        if (Case_38_cns(TestType.MinValue) != Case_38_var(TestType.MinValue)) failures++;
        if (Case_38_cns(TestType.MaxValue) != Case_38_var(TestType.MaxValue)) failures++;
        if (Case_39_cns(0) != Case_39_var(0)) failures++;
        if (Case_39_cns(42) != Case_39_var(42)) failures++;
        if (Case_39_cns(TestType.MinValue) != Case_39_var(TestType.MinValue)) failures++;
        if (Case_39_cns(TestType.MaxValue) != Case_39_var(TestType.MaxValue)) failures++;
        if (Case_40_cns(0) != Case_40_var(0)) failures++;
        if (Case_40_cns(42) != Case_40_var(42)) failures++;
        if (Case_40_cns(TestType.MinValue) != Case_40_var(TestType.MinValue)) failures++;
        if (Case_40_cns(TestType.MaxValue) != Case_40_var(TestType.MaxValue)) failures++;
        if (Case_41_cns(0) != Case_41_var(0)) failures++;
        if (Case_41_cns(42) != Case_41_var(42)) failures++;
        if (Case_41_cns(TestType.MinValue) != Case_41_var(TestType.MinValue)) failures++;
        if (Case_41_cns(TestType.MaxValue) != Case_41_var(TestType.MaxValue)) failures++;
        if (Case_42_cns(0) != Case_42_var(0)) failures++;
        if (Case_42_cns(42) != Case_42_var(42)) failures++;
        if (Case_42_cns(TestType.MinValue) != Case_42_var(TestType.MinValue)) failures++;
        if (Case_42_cns(TestType.MaxValue) != Case_42_var(TestType.MaxValue)) failures++;
        if (Case_43_cns(0) != Case_43_var(0)) failures++;
        if (Case_43_cns(42) != Case_43_var(42)) failures++;
        if (Case_43_cns(TestType.MinValue) != Case_43_var(TestType.MinValue)) failures++;
        if (Case_43_cns(TestType.MaxValue) != Case_43_var(TestType.MaxValue)) failures++;
        if (Case_44_cns(0) != Case_44_var(0)) failures++;
        if (Case_44_cns(42) != Case_44_var(42)) failures++;
        if (Case_44_cns(TestType.MinValue) != Case_44_var(TestType.MinValue)) failures++;
        if (Case_44_cns(TestType.MaxValue) != Case_44_var(TestType.MaxValue)) failures++;
        if (Case_45_cns(0) != Case_45_var(0)) failures++;
        if (Case_45_cns(42) != Case_45_var(42)) failures++;
        if (Case_45_cns(TestType.MinValue) != Case_45_var(TestType.MinValue)) failures++;
        if (Case_45_cns(TestType.MaxValue) != Case_45_var(TestType.MaxValue)) failures++;
        if (Case_46_cns(0) != Case_46_var(0)) failures++;
        if (Case_46_cns(42) != Case_46_var(42)) failures++;
        if (Case_46_cns(TestType.MinValue) != Case_46_var(TestType.MinValue)) failures++;
        if (Case_46_cns(TestType.MaxValue) != Case_46_var(TestType.MaxValue)) failures++;
        if (Case_47_cns(0) != Case_47_var(0)) failures++;
        if (Case_47_cns(42) != Case_47_var(42)) failures++;
        if (Case_47_cns(TestType.MinValue) != Case_47_var(TestType.MinValue)) failures++;
        if (Case_47_cns(TestType.MaxValue) != Case_47_var(TestType.MaxValue)) failures++;
        if (Case_48_cns(0) != Case_48_var(0)) failures++;
        if (Case_48_cns(42) != Case_48_var(42)) failures++;
        if (Case_48_cns(TestType.MinValue) != Case_48_var(TestType.MinValue)) failures++;
        if (Case_48_cns(TestType.MaxValue) != Case_48_var(TestType.MaxValue)) failures++;
        if (Case_49_cns(0) != Case_49_var(0)) failures++;
        if (Case_49_cns(42) != Case_49_var(42)) failures++;
        if (Case_49_cns(TestType.MinValue) != Case_49_var(TestType.MinValue)) failures++;
        if (Case_49_cns(TestType.MaxValue) != Case_49_var(TestType.MaxValue)) failures++;
        if (Case_50_cns(0) != Case_50_var(0)) failures++;
        if (Case_50_cns(42) != Case_50_var(42)) failures++;
        if (Case_50_cns(TestType.MinValue) != Case_50_var(TestType.MinValue)) failures++;
        if (Case_50_cns(TestType.MaxValue) != Case_50_var(TestType.MaxValue)) failures++;
        if (Case_51_cns(0) != Case_51_var(0)) failures++;
        if (Case_51_cns(42) != Case_51_var(42)) failures++;
        if (Case_51_cns(TestType.MinValue) != Case_51_var(TestType.MinValue)) failures++;
        if (Case_51_cns(TestType.MaxValue) != Case_51_var(TestType.MaxValue)) failures++;
        if (Case_52_cns(0) != Case_52_var(0)) failures++;
        if (Case_52_cns(42) != Case_52_var(42)) failures++;
        if (Case_52_cns(TestType.MinValue) != Case_52_var(TestType.MinValue)) failures++;
        if (Case_52_cns(TestType.MaxValue) != Case_52_var(TestType.MaxValue)) failures++;
        if (Case_53_cns(0) != Case_53_var(0)) failures++;
        if (Case_53_cns(42) != Case_53_var(42)) failures++;
        if (Case_53_cns(TestType.MinValue) != Case_53_var(TestType.MinValue)) failures++;
        if (Case_53_cns(TestType.MaxValue) != Case_53_var(TestType.MaxValue)) failures++;
        if (Case_54_cns(0) != Case_54_var(0)) failures++;
        if (Case_54_cns(42) != Case_54_var(42)) failures++;
        if (Case_54_cns(TestType.MinValue) != Case_54_var(TestType.MinValue)) failures++;
        if (Case_54_cns(TestType.MaxValue) != Case_54_var(TestType.MaxValue)) failures++;
        if (Case_55_cns(0) != Case_55_var(0)) failures++;
        if (Case_55_cns(42) != Case_55_var(42)) failures++;
        if (Case_55_cns(TestType.MinValue) != Case_55_var(TestType.MinValue)) failures++;
        if (Case_55_cns(TestType.MaxValue) != Case_55_var(TestType.MaxValue)) failures++;
        if (Case_56_cns(0) != Case_56_var(0)) failures++;
        if (Case_56_cns(42) != Case_56_var(42)) failures++;
        if (Case_56_cns(TestType.MinValue) != Case_56_var(TestType.MinValue)) failures++;
        if (Case_56_cns(TestType.MaxValue) != Case_56_var(TestType.MaxValue)) failures++;
        if (Case_57_cns(0) != Case_57_var(0)) failures++;
        if (Case_57_cns(42) != Case_57_var(42)) failures++;
        if (Case_57_cns(TestType.MinValue) != Case_57_var(TestType.MinValue)) failures++;
        if (Case_57_cns(TestType.MaxValue) != Case_57_var(TestType.MaxValue)) failures++;
        if (Case_58_cns(0) != Case_58_var(0)) failures++;
        if (Case_58_cns(42) != Case_58_var(42)) failures++;
        if (Case_58_cns(TestType.MinValue) != Case_58_var(TestType.MinValue)) failures++;
        if (Case_58_cns(TestType.MaxValue) != Case_58_var(TestType.MaxValue)) failures++;
        if (Case_59_cns(0) != Case_59_var(0)) failures++;
        if (Case_59_cns(42) != Case_59_var(42)) failures++;
        if (Case_59_cns(TestType.MinValue) != Case_59_var(TestType.MinValue)) failures++;
        if (Case_59_cns(TestType.MaxValue) != Case_59_var(TestType.MaxValue)) failures++;
        if (Case_60_cns(0) != Case_60_var(0)) failures++;
        if (Case_60_cns(42) != Case_60_var(42)) failures++;
        if (Case_60_cns(TestType.MinValue) != Case_60_var(TestType.MinValue)) failures++;
        if (Case_60_cns(TestType.MaxValue) != Case_60_var(TestType.MaxValue)) failures++;
        if (Case_61_cns(0) != Case_61_var(0)) failures++;
        if (Case_61_cns(42) != Case_61_var(42)) failures++;
        if (Case_61_cns(TestType.MinValue) != Case_61_var(TestType.MinValue)) failures++;
        if (Case_61_cns(TestType.MaxValue) != Case_61_var(TestType.MaxValue)) failures++;
        if (Case_62_cns(0) != Case_62_var(0)) failures++;
        if (Case_62_cns(42) != Case_62_var(42)) failures++;
        if (Case_62_cns(TestType.MinValue) != Case_62_var(TestType.MinValue)) failures++;
        if (Case_62_cns(TestType.MaxValue) != Case_62_var(TestType.MaxValue)) failures++;
        if (Case_63_cns(0) != Case_63_var(0)) failures++;
        if (Case_63_cns(42) != Case_63_var(42)) failures++;
        if (Case_63_cns(TestType.MinValue) != Case_63_var(TestType.MinValue)) failures++;
        if (Case_63_cns(TestType.MaxValue) != Case_63_var(TestType.MaxValue)) failures++;
        if (Case_64_cns(0) != Case_64_var(0)) failures++;
        if (Case_64_cns(42) != Case_64_var(42)) failures++;
        if (Case_64_cns(TestType.MinValue) != Case_64_var(TestType.MinValue)) failures++;
        if (Case_64_cns(TestType.MaxValue) != Case_64_var(TestType.MaxValue)) failures++;
        if (Case_65_cns(0) != Case_65_var(0)) failures++;
        if (Case_65_cns(42) != Case_65_var(42)) failures++;
        if (Case_65_cns(TestType.MinValue) != Case_65_var(TestType.MinValue)) failures++;
        if (Case_65_cns(TestType.MaxValue) != Case_65_var(TestType.MaxValue)) failures++;
        if (Case_66_cns(0) != Case_66_var(0)) failures++;
        if (Case_66_cns(42) != Case_66_var(42)) failures++;
        if (Case_66_cns(TestType.MinValue) != Case_66_var(TestType.MinValue)) failures++;
        if (Case_66_cns(TestType.MaxValue) != Case_66_var(TestType.MaxValue)) failures++;
        if (Case_67_cns(0) != Case_67_var(0)) failures++;
        if (Case_67_cns(42) != Case_67_var(42)) failures++;
        if (Case_67_cns(TestType.MinValue) != Case_67_var(TestType.MinValue)) failures++;
        if (Case_67_cns(TestType.MaxValue) != Case_67_var(TestType.MaxValue)) failures++;
        if (Case_68_cns(0) != Case_68_var(0)) failures++;
        if (Case_68_cns(42) != Case_68_var(42)) failures++;
        if (Case_68_cns(TestType.MinValue) != Case_68_var(TestType.MinValue)) failures++;
        if (Case_68_cns(TestType.MaxValue) != Case_68_var(TestType.MaxValue)) failures++;
        if (Case_69_cns(0) != Case_69_var(0)) failures++;
        if (Case_69_cns(42) != Case_69_var(42)) failures++;
        if (Case_69_cns(TestType.MinValue) != Case_69_var(TestType.MinValue)) failures++;
        if (Case_69_cns(TestType.MaxValue) != Case_69_var(TestType.MaxValue)) failures++;
        if (Case_70_cns(0) != Case_70_var(0)) failures++;
        if (Case_70_cns(42) != Case_70_var(42)) failures++;
        if (Case_70_cns(TestType.MinValue) != Case_70_var(TestType.MinValue)) failures++;
        if (Case_70_cns(TestType.MaxValue) != Case_70_var(TestType.MaxValue)) failures++;
        if (Case_71_cns(0) != Case_71_var(0)) failures++;
        if (Case_71_cns(42) != Case_71_var(42)) failures++;
        if (Case_71_cns(TestType.MinValue) != Case_71_var(TestType.MinValue)) failures++;
        if (Case_71_cns(TestType.MaxValue) != Case_71_var(TestType.MaxValue)) failures++;
        if (Case_72_cns(0) != Case_72_var(0)) failures++;
        if (Case_72_cns(42) != Case_72_var(42)) failures++;
        if (Case_72_cns(TestType.MinValue) != Case_72_var(TestType.MinValue)) failures++;
        if (Case_72_cns(TestType.MaxValue) != Case_72_var(TestType.MaxValue)) failures++;
        if (Case_73_cns(0) != Case_73_var(0)) failures++;
        if (Case_73_cns(42) != Case_73_var(42)) failures++;
        if (Case_73_cns(TestType.MinValue) != Case_73_var(TestType.MinValue)) failures++;
        if (Case_73_cns(TestType.MaxValue) != Case_73_var(TestType.MaxValue)) failures++;
        if (Case_74_cns(0) != Case_74_var(0)) failures++;
        if (Case_74_cns(42) != Case_74_var(42)) failures++;
        if (Case_74_cns(TestType.MinValue) != Case_74_var(TestType.MinValue)) failures++;
        if (Case_74_cns(TestType.MaxValue) != Case_74_var(TestType.MaxValue)) failures++;
        if (Case_75_cns(0) != Case_75_var(0)) failures++;
        if (Case_75_cns(42) != Case_75_var(42)) failures++;
        if (Case_75_cns(TestType.MinValue) != Case_75_var(TestType.MinValue)) failures++;
        if (Case_75_cns(TestType.MaxValue) != Case_75_var(TestType.MaxValue)) failures++;
        if (Case_76_cns(0) != Case_76_var(0)) failures++;
        if (Case_76_cns(42) != Case_76_var(42)) failures++;
        if (Case_76_cns(TestType.MinValue) != Case_76_var(TestType.MinValue)) failures++;
        if (Case_76_cns(TestType.MaxValue) != Case_76_var(TestType.MaxValue)) failures++;
        if (Case_77_cns(0) != Case_77_var(0)) failures++;
        if (Case_77_cns(42) != Case_77_var(42)) failures++;
        if (Case_77_cns(TestType.MinValue) != Case_77_var(TestType.MinValue)) failures++;
        if (Case_77_cns(TestType.MaxValue) != Case_77_var(TestType.MaxValue)) failures++;
        if (Case_78_cns(0) != Case_78_var(0)) failures++;
        if (Case_78_cns(42) != Case_78_var(42)) failures++;
        if (Case_78_cns(TestType.MinValue) != Case_78_var(TestType.MinValue)) failures++;
        if (Case_78_cns(TestType.MaxValue) != Case_78_var(TestType.MaxValue)) failures++;
        if (Case_79_cns(0) != Case_79_var(0)) failures++;
        if (Case_79_cns(42) != Case_79_var(42)) failures++;
        if (Case_79_cns(TestType.MinValue) != Case_79_var(TestType.MinValue)) failures++;
        if (Case_79_cns(TestType.MaxValue) != Case_79_var(TestType.MaxValue)) failures++;
        if (Case_80_cns(0) != Case_80_var(0)) failures++;
        if (Case_80_cns(42) != Case_80_var(42)) failures++;
        if (Case_80_cns(TestType.MinValue) != Case_80_var(TestType.MinValue)) failures++;
        if (Case_80_cns(TestType.MaxValue) != Case_80_var(TestType.MaxValue)) failures++;
        if (Case_81_cns(0) != Case_81_var(0)) failures++;
        if (Case_81_cns(42) != Case_81_var(42)) failures++;
        if (Case_81_cns(TestType.MinValue) != Case_81_var(TestType.MinValue)) failures++;
        if (Case_81_cns(TestType.MaxValue) != Case_81_var(TestType.MaxValue)) failures++;
        return failures;
    }
}

// x op icon1==icon2
public class ConstantFoldingTests2 : ConstantFoldingTestsBase
{
    [MethodImpl(MethodImplOptions.NoInlining)] bool Case_1_cns(TestType x) => ((x + Const1) == Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] bool Case_1_var(TestType x) => ((x + GetConst1()) == GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] bool Case_2_cns(TestType x) => ((x - Const1) == Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] bool Case_2_var(TestType x) => ((x - GetConst1()) == GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] bool Case_3_cns(TestType x) => ((x & Const1) == Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] bool Case_3_var(TestType x) => ((x & GetConst1()) == GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] bool Case_4_cns(TestType x) => ((x | Const1) == Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] bool Case_4_var(TestType x) => ((x | GetConst1()) == GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] bool Case_5_cns(TestType x) => ((x ^ Const1) == Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] bool Case_5_var(TestType x) => ((x ^ GetConst1()) == GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] bool Case_6_cns(TestType x) => ((x >> Const1) == Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] bool Case_6_var(TestType x) => ((x >> GetConst1()) == GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] bool Case_7_cns(TestType x) => ((x << Const1) == Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] bool Case_7_var(TestType x) => ((x << GetConst1()) == GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] bool Case_8_cns(TestType x) => ((x * Const1) == Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] bool Case_8_var(TestType x) => ((x * GetConst1()) == GetConst2());

    [MethodImpl(MethodImplOptions.NoInlining)] bool Case_9_cns(TestType x) => ((x / Const1) == Const2);
    [MethodImpl(MethodImplOptions.NoInlining)] bool Case_9_var(TestType x) => ((x / GetConst1()) == GetConst2());

    public int RunTests()
    {
        int failures = 0;
        if (Case_1_cns(0) != Case_1_var(0)) failures++;
        if (Case_1_cns(42) != Case_1_var(42)) failures++;
        if (Case_1_cns(TestType.MinValue) != Case_1_var(TestType.MinValue)) failures++;
        if (Case_1_cns(TestType.MaxValue) != Case_1_var(TestType.MaxValue)) failures++;
        if (Case_2_cns(0) != Case_2_var(0)) failures++;
        if (Case_2_cns(42) != Case_2_var(42)) failures++;
        if (Case_2_cns(TestType.MinValue) != Case_2_var(TestType.MinValue)) failures++;
        if (Case_2_cns(TestType.MaxValue) != Case_2_var(TestType.MaxValue)) failures++;
        if (Case_3_cns(0) != Case_3_var(0)) failures++;
        if (Case_3_cns(42) != Case_3_var(42)) failures++;
        if (Case_3_cns(TestType.MinValue) != Case_3_var(TestType.MinValue)) failures++;
        if (Case_3_cns(TestType.MaxValue) != Case_3_var(TestType.MaxValue)) failures++;
        if (Case_4_cns(0) != Case_4_var(0)) failures++;
        if (Case_4_cns(42) != Case_4_var(42)) failures++;
        if (Case_4_cns(TestType.MinValue) != Case_4_var(TestType.MinValue)) failures++;
        if (Case_4_cns(TestType.MaxValue) != Case_4_var(TestType.MaxValue)) failures++;
        if (Case_5_cns(0) != Case_5_var(0)) failures++;
        if (Case_5_cns(42) != Case_5_var(42)) failures++;
        if (Case_5_cns(TestType.MinValue) != Case_5_var(TestType.MinValue)) failures++;
        if (Case_5_cns(TestType.MaxValue) != Case_5_var(TestType.MaxValue)) failures++;
        if (Case_6_cns(0) != Case_6_var(0)) failures++;
        if (Case_6_cns(42) != Case_6_var(42)) failures++;
        if (Case_6_cns(TestType.MinValue) != Case_6_var(TestType.MinValue)) failures++;
        if (Case_6_cns(TestType.MaxValue) != Case_6_var(TestType.MaxValue)) failures++;
        if (Case_7_cns(0) != Case_7_var(0)) failures++;
        if (Case_7_cns(42) != Case_7_var(42)) failures++;
        if (Case_7_cns(TestType.MinValue) != Case_7_var(TestType.MinValue)) failures++;
        if (Case_7_cns(TestType.MaxValue) != Case_7_var(TestType.MaxValue)) failures++;
        if (Case_8_cns(0) != Case_8_var(0)) failures++;
        if (Case_8_cns(42) != Case_8_var(42)) failures++;
        if (Case_8_cns(TestType.MinValue) != Case_8_var(TestType.MinValue)) failures++;
        if (Case_8_cns(TestType.MaxValue) != Case_8_var(TestType.MaxValue)) failures++;
        if (Case_9_cns(0) != Case_9_var(0)) failures++;
        if (Case_9_cns(42) != Case_9_var(42)) failures++;
        if (Case_9_cns(TestType.MinValue) != Case_9_var(TestType.MinValue)) failures++;
        if (Case_9_cns(TestType.MaxValue) != Case_9_var(TestType.MaxValue)) failures++;
        return failures;
    }
}

// ((x+icon1)+(y+icon2))
public class ConstantFoldingTests3 : ConstantFoldingTestsBase
{
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_1_cns(TestType x, TestType y) => ((x + Const1) + (y + Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_1_var(TestType x, TestType y) => ((x + GetConst1()) + (y + GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_2_cns(TestType x, TestType y) => ((x + Const1) + (y - Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_2_var(TestType x, TestType y) => ((x + GetConst1()) + (y - GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_3_cns(TestType x, TestType y) => ((x + Const1) + (y & Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_3_var(TestType x, TestType y) => ((x + GetConst1()) + (y & GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_4_cns(TestType x, TestType y) => ((x + Const1) + (y | Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_4_var(TestType x, TestType y) => ((x + GetConst1()) + (y | GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_5_cns(TestType x, TestType y) => ((x + Const1) + (y ^ Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_5_var(TestType x, TestType y) => ((x + GetConst1()) + (y ^ GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_6_cns(TestType x, TestType y) => ((x + Const1) + (y >> Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_6_var(TestType x, TestType y) => ((x + GetConst1()) + (y >> GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_7_cns(TestType x, TestType y) => ((x + Const1) + (y << Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_7_var(TestType x, TestType y) => ((x + GetConst1()) + (y << GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_8_cns(TestType x, TestType y) => ((x + Const1) + (y * Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_8_var(TestType x, TestType y) => ((x + GetConst1()) + (y * GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_9_cns(TestType x, TestType y) => ((x + Const1) + (y / Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_9_var(TestType x, TestType y) => ((x + GetConst1()) + (y / GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_10_cns(TestType x, TestType y) => ((x - Const1) + (y + Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_10_var(TestType x, TestType y) => ((x - GetConst1()) + (y + GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_11_cns(TestType x, TestType y) => ((x - Const1) + (y - Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_11_var(TestType x, TestType y) => ((x - GetConst1()) + (y - GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_12_cns(TestType x, TestType y) => ((x - Const1) + (y & Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_12_var(TestType x, TestType y) => ((x - GetConst1()) + (y & GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_13_cns(TestType x, TestType y) => ((x - Const1) + (y | Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_13_var(TestType x, TestType y) => ((x - GetConst1()) + (y | GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_14_cns(TestType x, TestType y) => ((x - Const1) + (y ^ Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_14_var(TestType x, TestType y) => ((x - GetConst1()) + (y ^ GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_15_cns(TestType x, TestType y) => ((x - Const1) + (y >> Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_15_var(TestType x, TestType y) => ((x - GetConst1()) + (y >> GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_16_cns(TestType x, TestType y) => ((x - Const1) + (y << Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_16_var(TestType x, TestType y) => ((x - GetConst1()) + (y << GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_17_cns(TestType x, TestType y) => ((x - Const1) + (y * Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_17_var(TestType x, TestType y) => ((x - GetConst1()) + (y * GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_18_cns(TestType x, TestType y) => ((x - Const1) + (y / Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_18_var(TestType x, TestType y) => ((x - GetConst1()) + (y / GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_19_cns(TestType x, TestType y) => ((x & Const1) + (y + Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_19_var(TestType x, TestType y) => ((x & GetConst1()) + (y + GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_20_cns(TestType x, TestType y) => ((x & Const1) + (y - Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_20_var(TestType x, TestType y) => ((x & GetConst1()) + (y - GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_21_cns(TestType x, TestType y) => ((x & Const1) + (y & Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_21_var(TestType x, TestType y) => ((x & GetConst1()) + (y & GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_22_cns(TestType x, TestType y) => ((x & Const1) + (y | Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_22_var(TestType x, TestType y) => ((x & GetConst1()) + (y | GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_23_cns(TestType x, TestType y) => ((x & Const1) + (y ^ Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_23_var(TestType x, TestType y) => ((x & GetConst1()) + (y ^ GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_24_cns(TestType x, TestType y) => ((x & Const1) + (y >> Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_24_var(TestType x, TestType y) => ((x & GetConst1()) + (y >> GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_25_cns(TestType x, TestType y) => ((x & Const1) + (y << Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_25_var(TestType x, TestType y) => ((x & GetConst1()) + (y << GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_26_cns(TestType x, TestType y) => ((x & Const1) + (y * Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_26_var(TestType x, TestType y) => ((x & GetConst1()) + (y * GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_27_cns(TestType x, TestType y) => ((x & Const1) + (y / Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_27_var(TestType x, TestType y) => ((x & GetConst1()) + (y / GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_28_cns(TestType x, TestType y) => ((x | Const1) + (y + Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_28_var(TestType x, TestType y) => ((x | GetConst1()) + (y + GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_29_cns(TestType x, TestType y) => ((x | Const1) + (y - Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_29_var(TestType x, TestType y) => ((x | GetConst1()) + (y - GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_30_cns(TestType x, TestType y) => ((x | Const1) + (y & Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_30_var(TestType x, TestType y) => ((x | GetConst1()) + (y & GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_31_cns(TestType x, TestType y) => ((x | Const1) + (y | Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_31_var(TestType x, TestType y) => ((x | GetConst1()) + (y | GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_32_cns(TestType x, TestType y) => ((x | Const1) + (y ^ Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_32_var(TestType x, TestType y) => ((x | GetConst1()) + (y ^ GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_33_cns(TestType x, TestType y) => ((x | Const1) + (y >> Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_33_var(TestType x, TestType y) => ((x | GetConst1()) + (y >> GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_34_cns(TestType x, TestType y) => ((x | Const1) + (y << Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_34_var(TestType x, TestType y) => ((x | GetConst1()) + (y << GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_35_cns(TestType x, TestType y) => ((x | Const1) + (y * Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_35_var(TestType x, TestType y) => ((x | GetConst1()) + (y * GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_36_cns(TestType x, TestType y) => ((x | Const1) + (y / Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_36_var(TestType x, TestType y) => ((x | GetConst1()) + (y / GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_37_cns(TestType x, TestType y) => ((x ^ Const1) + (y + Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_37_var(TestType x, TestType y) => ((x ^ GetConst1()) + (y + GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_38_cns(TestType x, TestType y) => ((x ^ Const1) + (y - Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_38_var(TestType x, TestType y) => ((x ^ GetConst1()) + (y - GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_39_cns(TestType x, TestType y) => ((x ^ Const1) + (y & Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_39_var(TestType x, TestType y) => ((x ^ GetConst1()) + (y & GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_40_cns(TestType x, TestType y) => ((x ^ Const1) + (y | Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_40_var(TestType x, TestType y) => ((x ^ GetConst1()) + (y | GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_41_cns(TestType x, TestType y) => ((x ^ Const1) + (y ^ Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_41_var(TestType x, TestType y) => ((x ^ GetConst1()) + (y ^ GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_42_cns(TestType x, TestType y) => ((x ^ Const1) + (y >> Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_42_var(TestType x, TestType y) => ((x ^ GetConst1()) + (y >> GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_43_cns(TestType x, TestType y) => ((x ^ Const1) + (y << Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_43_var(TestType x, TestType y) => ((x ^ GetConst1()) + (y << GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_44_cns(TestType x, TestType y) => ((x ^ Const1) + (y * Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_44_var(TestType x, TestType y) => ((x ^ GetConst1()) + (y * GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_45_cns(TestType x, TestType y) => ((x ^ Const1) + (y / Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_45_var(TestType x, TestType y) => ((x ^ GetConst1()) + (y / GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_46_cns(TestType x, TestType y) => ((x >> Const1) + (y + Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_46_var(TestType x, TestType y) => ((x >> GetConst1()) + (y + GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_47_cns(TestType x, TestType y) => ((x >> Const1) + (y - Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_47_var(TestType x, TestType y) => ((x >> GetConst1()) + (y - GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_48_cns(TestType x, TestType y) => ((x >> Const1) + (y & Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_48_var(TestType x, TestType y) => ((x >> GetConst1()) + (y & GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_49_cns(TestType x, TestType y) => ((x >> Const1) + (y | Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_49_var(TestType x, TestType y) => ((x >> GetConst1()) + (y | GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_50_cns(TestType x, TestType y) => ((x >> Const1) + (y ^ Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_50_var(TestType x, TestType y) => ((x >> GetConst1()) + (y ^ GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_51_cns(TestType x, TestType y) => ((x >> Const1) + (y >> Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_51_var(TestType x, TestType y) => ((x >> GetConst1()) + (y >> GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_52_cns(TestType x, TestType y) => ((x >> Const1) + (y << Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_52_var(TestType x, TestType y) => ((x >> GetConst1()) + (y << GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_53_cns(TestType x, TestType y) => ((x >> Const1) + (y * Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_53_var(TestType x, TestType y) => ((x >> GetConst1()) + (y * GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_54_cns(TestType x, TestType y) => ((x >> Const1) + (y / Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_54_var(TestType x, TestType y) => ((x >> GetConst1()) + (y / GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_55_cns(TestType x, TestType y) => ((x << Const1) + (y + Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_55_var(TestType x, TestType y) => ((x << GetConst1()) + (y + GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_56_cns(TestType x, TestType y) => ((x << Const1) + (y - Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_56_var(TestType x, TestType y) => ((x << GetConst1()) + (y - GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_57_cns(TestType x, TestType y) => ((x << Const1) + (y & Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_57_var(TestType x, TestType y) => ((x << GetConst1()) + (y & GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_58_cns(TestType x, TestType y) => ((x << Const1) + (y | Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_58_var(TestType x, TestType y) => ((x << GetConst1()) + (y | GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_59_cns(TestType x, TestType y) => ((x << Const1) + (y ^ Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_59_var(TestType x, TestType y) => ((x << GetConst1()) + (y ^ GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_60_cns(TestType x, TestType y) => ((x << Const1) + (y >> Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_60_var(TestType x, TestType y) => ((x << GetConst1()) + (y >> GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_61_cns(TestType x, TestType y) => ((x << Const1) + (y << Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_61_var(TestType x, TestType y) => ((x << GetConst1()) + (y << GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_62_cns(TestType x, TestType y) => ((x << Const1) + (y * Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_62_var(TestType x, TestType y) => ((x << GetConst1()) + (y * GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_63_cns(TestType x, TestType y) => ((x << Const1) + (y / Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_63_var(TestType x, TestType y) => ((x << GetConst1()) + (y / GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_64_cns(TestType x, TestType y) => ((x * Const1) + (y + Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_64_var(TestType x, TestType y) => ((x * GetConst1()) + (y + GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_65_cns(TestType x, TestType y) => ((x * Const1) + (y - Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_65_var(TestType x, TestType y) => ((x * GetConst1()) + (y - GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_66_cns(TestType x, TestType y) => ((x * Const1) + (y & Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_66_var(TestType x, TestType y) => ((x * GetConst1()) + (y & GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_67_cns(TestType x, TestType y) => ((x * Const1) + (y | Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_67_var(TestType x, TestType y) => ((x * GetConst1()) + (y | GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_68_cns(TestType x, TestType y) => ((x * Const1) + (y ^ Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_68_var(TestType x, TestType y) => ((x * GetConst1()) + (y ^ GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_69_cns(TestType x, TestType y) => ((x * Const1) + (y >> Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_69_var(TestType x, TestType y) => ((x * GetConst1()) + (y >> GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_70_cns(TestType x, TestType y) => ((x * Const1) + (y << Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_70_var(TestType x, TestType y) => ((x * GetConst1()) + (y << GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_71_cns(TestType x, TestType y) => ((x * Const1) + (y * Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_71_var(TestType x, TestType y) => ((x * GetConst1()) + (y * GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_72_cns(TestType x, TestType y) => ((x * Const1) + (y / Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_72_var(TestType x, TestType y) => ((x * GetConst1()) + (y / GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_73_cns(TestType x, TestType y) => ((x / Const1) + (y + Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_73_var(TestType x, TestType y) => ((x / GetConst1()) + (y + GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_74_cns(TestType x, TestType y) => ((x / Const1) + (y - Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_74_var(TestType x, TestType y) => ((x / GetConst1()) + (y - GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_75_cns(TestType x, TestType y) => ((x / Const1) + (y & Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_75_var(TestType x, TestType y) => ((x / GetConst1()) + (y & GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_76_cns(TestType x, TestType y) => ((x / Const1) + (y | Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_76_var(TestType x, TestType y) => ((x / GetConst1()) + (y | GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_77_cns(TestType x, TestType y) => ((x / Const1) + (y ^ Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_77_var(TestType x, TestType y) => ((x / GetConst1()) + (y ^ GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_78_cns(TestType x, TestType y) => ((x / Const1) + (y >> Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_78_var(TestType x, TestType y) => ((x / GetConst1()) + (y >> GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_79_cns(TestType x, TestType y) => ((x / Const1) + (y << Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_79_var(TestType x, TestType y) => ((x / GetConst1()) + (y << GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_80_cns(TestType x, TestType y) => ((x / Const1) + (y * Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_80_var(TestType x, TestType y) => ((x / GetConst1()) + (y * GetConst2()));

    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_81_cns(TestType x, TestType y) => ((x / Const1) + (y / Const2));
    [MethodImpl(MethodImplOptions.NoInlining)] TestType Case_81_var(TestType x, TestType y) => ((x / GetConst1()) + (y / GetConst2()));

    public int RunTests()
    {
        int failures = 0;
        if (Case_1_cns(0, 0) != Case_1_var(0, 0)) failures++;
        if (Case_1_cns(42, 42) != Case_1_var(42, 42)) failures++;
        if (Case_1_cns(TestType.MinValue, TestType.MinValue) != Case_1_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_1_cns(TestType.MaxValue, TestType.MaxValue) != Case_1_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_1_cns(TestType.MaxValue, TestType.MinValue) != Case_1_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_1_cns(TestType.MinValue, TestType.MaxValue) != Case_1_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_2_cns(0, 0) != Case_2_var(0, 0)) failures++;
        if (Case_2_cns(42, 42) != Case_2_var(42, 42)) failures++;
        if (Case_2_cns(TestType.MinValue, TestType.MinValue) != Case_2_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_2_cns(TestType.MaxValue, TestType.MaxValue) != Case_2_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_2_cns(TestType.MaxValue, TestType.MinValue) != Case_2_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_2_cns(TestType.MinValue, TestType.MaxValue) != Case_2_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_3_cns(0, 0) != Case_3_var(0, 0)) failures++;
        if (Case_3_cns(42, 42) != Case_3_var(42, 42)) failures++;
        if (Case_3_cns(TestType.MinValue, TestType.MinValue) != Case_3_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_3_cns(TestType.MaxValue, TestType.MaxValue) != Case_3_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_3_cns(TestType.MaxValue, TestType.MinValue) != Case_3_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_3_cns(TestType.MinValue, TestType.MaxValue) != Case_3_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_4_cns(0, 0) != Case_4_var(0, 0)) failures++;
        if (Case_4_cns(42, 42) != Case_4_var(42, 42)) failures++;
        if (Case_4_cns(TestType.MinValue, TestType.MinValue) != Case_4_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_4_cns(TestType.MaxValue, TestType.MaxValue) != Case_4_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_4_cns(TestType.MaxValue, TestType.MinValue) != Case_4_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_4_cns(TestType.MinValue, TestType.MaxValue) != Case_4_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_5_cns(0, 0) != Case_5_var(0, 0)) failures++;
        if (Case_5_cns(42, 42) != Case_5_var(42, 42)) failures++;
        if (Case_5_cns(TestType.MinValue, TestType.MinValue) != Case_5_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_5_cns(TestType.MaxValue, TestType.MaxValue) != Case_5_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_5_cns(TestType.MaxValue, TestType.MinValue) != Case_5_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_5_cns(TestType.MinValue, TestType.MaxValue) != Case_5_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_6_cns(0, 0) != Case_6_var(0, 0)) failures++;
        if (Case_6_cns(42, 42) != Case_6_var(42, 42)) failures++;
        if (Case_6_cns(TestType.MinValue, TestType.MinValue) != Case_6_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_6_cns(TestType.MaxValue, TestType.MaxValue) != Case_6_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_6_cns(TestType.MaxValue, TestType.MinValue) != Case_6_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_6_cns(TestType.MinValue, TestType.MaxValue) != Case_6_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_7_cns(0, 0) != Case_7_var(0, 0)) failures++;
        if (Case_7_cns(42, 42) != Case_7_var(42, 42)) failures++;
        if (Case_7_cns(TestType.MinValue, TestType.MinValue) != Case_7_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_7_cns(TestType.MaxValue, TestType.MaxValue) != Case_7_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_7_cns(TestType.MaxValue, TestType.MinValue) != Case_7_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_7_cns(TestType.MinValue, TestType.MaxValue) != Case_7_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_8_cns(0, 0) != Case_8_var(0, 0)) failures++;
        if (Case_8_cns(42, 42) != Case_8_var(42, 42)) failures++;
        if (Case_8_cns(TestType.MinValue, TestType.MinValue) != Case_8_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_8_cns(TestType.MaxValue, TestType.MaxValue) != Case_8_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_8_cns(TestType.MaxValue, TestType.MinValue) != Case_8_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_8_cns(TestType.MinValue, TestType.MaxValue) != Case_8_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_9_cns(0, 0) != Case_9_var(0, 0)) failures++;
        if (Case_9_cns(42, 42) != Case_9_var(42, 42)) failures++;
        if (Case_9_cns(TestType.MinValue, TestType.MinValue) != Case_9_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_9_cns(TestType.MaxValue, TestType.MaxValue) != Case_9_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_9_cns(TestType.MaxValue, TestType.MinValue) != Case_9_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_9_cns(TestType.MinValue, TestType.MaxValue) != Case_9_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_10_cns(0, 0) != Case_10_var(0, 0)) failures++;
        if (Case_10_cns(42, 42) != Case_10_var(42, 42)) failures++;
        if (Case_10_cns(TestType.MinValue, TestType.MinValue) != Case_10_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_10_cns(TestType.MaxValue, TestType.MaxValue) != Case_10_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_10_cns(TestType.MaxValue, TestType.MinValue) != Case_10_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_10_cns(TestType.MinValue, TestType.MaxValue) != Case_10_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_11_cns(0, 0) != Case_11_var(0, 0)) failures++;
        if (Case_11_cns(42, 42) != Case_11_var(42, 42)) failures++;
        if (Case_11_cns(TestType.MinValue, TestType.MinValue) != Case_11_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_11_cns(TestType.MaxValue, TestType.MaxValue) != Case_11_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_11_cns(TestType.MaxValue, TestType.MinValue) != Case_11_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_11_cns(TestType.MinValue, TestType.MaxValue) != Case_11_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_12_cns(0, 0) != Case_12_var(0, 0)) failures++;
        if (Case_12_cns(42, 42) != Case_12_var(42, 42)) failures++;
        if (Case_12_cns(TestType.MinValue, TestType.MinValue) != Case_12_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_12_cns(TestType.MaxValue, TestType.MaxValue) != Case_12_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_12_cns(TestType.MaxValue, TestType.MinValue) != Case_12_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_12_cns(TestType.MinValue, TestType.MaxValue) != Case_12_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_13_cns(0, 0) != Case_13_var(0, 0)) failures++;
        if (Case_13_cns(42, 42) != Case_13_var(42, 42)) failures++;
        if (Case_13_cns(TestType.MinValue, TestType.MinValue) != Case_13_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_13_cns(TestType.MaxValue, TestType.MaxValue) != Case_13_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_13_cns(TestType.MaxValue, TestType.MinValue) != Case_13_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_13_cns(TestType.MinValue, TestType.MaxValue) != Case_13_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_14_cns(0, 0) != Case_14_var(0, 0)) failures++;
        if (Case_14_cns(42, 42) != Case_14_var(42, 42)) failures++;
        if (Case_14_cns(TestType.MinValue, TestType.MinValue) != Case_14_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_14_cns(TestType.MaxValue, TestType.MaxValue) != Case_14_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_14_cns(TestType.MaxValue, TestType.MinValue) != Case_14_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_14_cns(TestType.MinValue, TestType.MaxValue) != Case_14_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_15_cns(0, 0) != Case_15_var(0, 0)) failures++;
        if (Case_15_cns(42, 42) != Case_15_var(42, 42)) failures++;
        if (Case_15_cns(TestType.MinValue, TestType.MinValue) != Case_15_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_15_cns(TestType.MaxValue, TestType.MaxValue) != Case_15_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_15_cns(TestType.MaxValue, TestType.MinValue) != Case_15_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_15_cns(TestType.MinValue, TestType.MaxValue) != Case_15_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_16_cns(0, 0) != Case_16_var(0, 0)) failures++;
        if (Case_16_cns(42, 42) != Case_16_var(42, 42)) failures++;
        if (Case_16_cns(TestType.MinValue, TestType.MinValue) != Case_16_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_16_cns(TestType.MaxValue, TestType.MaxValue) != Case_16_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_16_cns(TestType.MaxValue, TestType.MinValue) != Case_16_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_16_cns(TestType.MinValue, TestType.MaxValue) != Case_16_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_17_cns(0, 0) != Case_17_var(0, 0)) failures++;
        if (Case_17_cns(42, 42) != Case_17_var(42, 42)) failures++;
        if (Case_17_cns(TestType.MinValue, TestType.MinValue) != Case_17_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_17_cns(TestType.MaxValue, TestType.MaxValue) != Case_17_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_17_cns(TestType.MaxValue, TestType.MinValue) != Case_17_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_17_cns(TestType.MinValue, TestType.MaxValue) != Case_17_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_18_cns(0, 0) != Case_18_var(0, 0)) failures++;
        if (Case_18_cns(42, 42) != Case_18_var(42, 42)) failures++;
        if (Case_18_cns(TestType.MinValue, TestType.MinValue) != Case_18_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_18_cns(TestType.MaxValue, TestType.MaxValue) != Case_18_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_18_cns(TestType.MaxValue, TestType.MinValue) != Case_18_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_18_cns(TestType.MinValue, TestType.MaxValue) != Case_18_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_19_cns(0, 0) != Case_19_var(0, 0)) failures++;
        if (Case_19_cns(42, 42) != Case_19_var(42, 42)) failures++;
        if (Case_19_cns(TestType.MinValue, TestType.MinValue) != Case_19_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_19_cns(TestType.MaxValue, TestType.MaxValue) != Case_19_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_19_cns(TestType.MaxValue, TestType.MinValue) != Case_19_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_19_cns(TestType.MinValue, TestType.MaxValue) != Case_19_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_20_cns(0, 0) != Case_20_var(0, 0)) failures++;
        if (Case_20_cns(42, 42) != Case_20_var(42, 42)) failures++;
        if (Case_20_cns(TestType.MinValue, TestType.MinValue) != Case_20_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_20_cns(TestType.MaxValue, TestType.MaxValue) != Case_20_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_20_cns(TestType.MaxValue, TestType.MinValue) != Case_20_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_20_cns(TestType.MinValue, TestType.MaxValue) != Case_20_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_21_cns(0, 0) != Case_21_var(0, 0)) failures++;
        if (Case_21_cns(42, 42) != Case_21_var(42, 42)) failures++;
        if (Case_21_cns(TestType.MinValue, TestType.MinValue) != Case_21_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_21_cns(TestType.MaxValue, TestType.MaxValue) != Case_21_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_21_cns(TestType.MaxValue, TestType.MinValue) != Case_21_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_21_cns(TestType.MinValue, TestType.MaxValue) != Case_21_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_22_cns(0, 0) != Case_22_var(0, 0)) failures++;
        if (Case_22_cns(42, 42) != Case_22_var(42, 42)) failures++;
        if (Case_22_cns(TestType.MinValue, TestType.MinValue) != Case_22_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_22_cns(TestType.MaxValue, TestType.MaxValue) != Case_22_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_22_cns(TestType.MaxValue, TestType.MinValue) != Case_22_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_22_cns(TestType.MinValue, TestType.MaxValue) != Case_22_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_23_cns(0, 0) != Case_23_var(0, 0)) failures++;
        if (Case_23_cns(42, 42) != Case_23_var(42, 42)) failures++;
        if (Case_23_cns(TestType.MinValue, TestType.MinValue) != Case_23_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_23_cns(TestType.MaxValue, TestType.MaxValue) != Case_23_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_23_cns(TestType.MaxValue, TestType.MinValue) != Case_23_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_23_cns(TestType.MinValue, TestType.MaxValue) != Case_23_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_24_cns(0, 0) != Case_24_var(0, 0)) failures++;
        if (Case_24_cns(42, 42) != Case_24_var(42, 42)) failures++;
        if (Case_24_cns(TestType.MinValue, TestType.MinValue) != Case_24_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_24_cns(TestType.MaxValue, TestType.MaxValue) != Case_24_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_24_cns(TestType.MaxValue, TestType.MinValue) != Case_24_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_24_cns(TestType.MinValue, TestType.MaxValue) != Case_24_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_25_cns(0, 0) != Case_25_var(0, 0)) failures++;
        if (Case_25_cns(42, 42) != Case_25_var(42, 42)) failures++;
        if (Case_25_cns(TestType.MinValue, TestType.MinValue) != Case_25_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_25_cns(TestType.MaxValue, TestType.MaxValue) != Case_25_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_25_cns(TestType.MaxValue, TestType.MinValue) != Case_25_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_25_cns(TestType.MinValue, TestType.MaxValue) != Case_25_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_26_cns(0, 0) != Case_26_var(0, 0)) failures++;
        if (Case_26_cns(42, 42) != Case_26_var(42, 42)) failures++;
        if (Case_26_cns(TestType.MinValue, TestType.MinValue) != Case_26_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_26_cns(TestType.MaxValue, TestType.MaxValue) != Case_26_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_26_cns(TestType.MaxValue, TestType.MinValue) != Case_26_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_26_cns(TestType.MinValue, TestType.MaxValue) != Case_26_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_27_cns(0, 0) != Case_27_var(0, 0)) failures++;
        if (Case_27_cns(42, 42) != Case_27_var(42, 42)) failures++;
        if (Case_27_cns(TestType.MinValue, TestType.MinValue) != Case_27_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_27_cns(TestType.MaxValue, TestType.MaxValue) != Case_27_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_27_cns(TestType.MaxValue, TestType.MinValue) != Case_27_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_27_cns(TestType.MinValue, TestType.MaxValue) != Case_27_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_28_cns(0, 0) != Case_28_var(0, 0)) failures++;
        if (Case_28_cns(42, 42) != Case_28_var(42, 42)) failures++;
        if (Case_28_cns(TestType.MinValue, TestType.MinValue) != Case_28_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_28_cns(TestType.MaxValue, TestType.MaxValue) != Case_28_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_28_cns(TestType.MaxValue, TestType.MinValue) != Case_28_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_28_cns(TestType.MinValue, TestType.MaxValue) != Case_28_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_29_cns(0, 0) != Case_29_var(0, 0)) failures++;
        if (Case_29_cns(42, 42) != Case_29_var(42, 42)) failures++;
        if (Case_29_cns(TestType.MinValue, TestType.MinValue) != Case_29_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_29_cns(TestType.MaxValue, TestType.MaxValue) != Case_29_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_29_cns(TestType.MaxValue, TestType.MinValue) != Case_29_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_29_cns(TestType.MinValue, TestType.MaxValue) != Case_29_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_30_cns(0, 0) != Case_30_var(0, 0)) failures++;
        if (Case_30_cns(42, 42) != Case_30_var(42, 42)) failures++;
        if (Case_30_cns(TestType.MinValue, TestType.MinValue) != Case_30_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_30_cns(TestType.MaxValue, TestType.MaxValue) != Case_30_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_30_cns(TestType.MaxValue, TestType.MinValue) != Case_30_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_30_cns(TestType.MinValue, TestType.MaxValue) != Case_30_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_31_cns(0, 0) != Case_31_var(0, 0)) failures++;
        if (Case_31_cns(42, 42) != Case_31_var(42, 42)) failures++;
        if (Case_31_cns(TestType.MinValue, TestType.MinValue) != Case_31_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_31_cns(TestType.MaxValue, TestType.MaxValue) != Case_31_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_31_cns(TestType.MaxValue, TestType.MinValue) != Case_31_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_31_cns(TestType.MinValue, TestType.MaxValue) != Case_31_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_32_cns(0, 0) != Case_32_var(0, 0)) failures++;
        if (Case_32_cns(42, 42) != Case_32_var(42, 42)) failures++;
        if (Case_32_cns(TestType.MinValue, TestType.MinValue) != Case_32_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_32_cns(TestType.MaxValue, TestType.MaxValue) != Case_32_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_32_cns(TestType.MaxValue, TestType.MinValue) != Case_32_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_32_cns(TestType.MinValue, TestType.MaxValue) != Case_32_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_33_cns(0, 0) != Case_33_var(0, 0)) failures++;
        if (Case_33_cns(42, 42) != Case_33_var(42, 42)) failures++;
        if (Case_33_cns(TestType.MinValue, TestType.MinValue) != Case_33_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_33_cns(TestType.MaxValue, TestType.MaxValue) != Case_33_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_33_cns(TestType.MaxValue, TestType.MinValue) != Case_33_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_33_cns(TestType.MinValue, TestType.MaxValue) != Case_33_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_34_cns(0, 0) != Case_34_var(0, 0)) failures++;
        if (Case_34_cns(42, 42) != Case_34_var(42, 42)) failures++;
        if (Case_34_cns(TestType.MinValue, TestType.MinValue) != Case_34_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_34_cns(TestType.MaxValue, TestType.MaxValue) != Case_34_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_34_cns(TestType.MaxValue, TestType.MinValue) != Case_34_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_34_cns(TestType.MinValue, TestType.MaxValue) != Case_34_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_35_cns(0, 0) != Case_35_var(0, 0)) failures++;
        if (Case_35_cns(42, 42) != Case_35_var(42, 42)) failures++;
        if (Case_35_cns(TestType.MinValue, TestType.MinValue) != Case_35_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_35_cns(TestType.MaxValue, TestType.MaxValue) != Case_35_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_35_cns(TestType.MaxValue, TestType.MinValue) != Case_35_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_35_cns(TestType.MinValue, TestType.MaxValue) != Case_35_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_36_cns(0, 0) != Case_36_var(0, 0)) failures++;
        if (Case_36_cns(42, 42) != Case_36_var(42, 42)) failures++;
        if (Case_36_cns(TestType.MinValue, TestType.MinValue) != Case_36_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_36_cns(TestType.MaxValue, TestType.MaxValue) != Case_36_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_36_cns(TestType.MaxValue, TestType.MinValue) != Case_36_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_36_cns(TestType.MinValue, TestType.MaxValue) != Case_36_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_37_cns(0, 0) != Case_37_var(0, 0)) failures++;
        if (Case_37_cns(42, 42) != Case_37_var(42, 42)) failures++;
        if (Case_37_cns(TestType.MinValue, TestType.MinValue) != Case_37_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_37_cns(TestType.MaxValue, TestType.MaxValue) != Case_37_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_37_cns(TestType.MaxValue, TestType.MinValue) != Case_37_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_37_cns(TestType.MinValue, TestType.MaxValue) != Case_37_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_38_cns(0, 0) != Case_38_var(0, 0)) failures++;
        if (Case_38_cns(42, 42) != Case_38_var(42, 42)) failures++;
        if (Case_38_cns(TestType.MinValue, TestType.MinValue) != Case_38_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_38_cns(TestType.MaxValue, TestType.MaxValue) != Case_38_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_38_cns(TestType.MaxValue, TestType.MinValue) != Case_38_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_38_cns(TestType.MinValue, TestType.MaxValue) != Case_38_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_39_cns(0, 0) != Case_39_var(0, 0)) failures++;
        if (Case_39_cns(42, 42) != Case_39_var(42, 42)) failures++;
        if (Case_39_cns(TestType.MinValue, TestType.MinValue) != Case_39_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_39_cns(TestType.MaxValue, TestType.MaxValue) != Case_39_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_39_cns(TestType.MaxValue, TestType.MinValue) != Case_39_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_39_cns(TestType.MinValue, TestType.MaxValue) != Case_39_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_40_cns(0, 0) != Case_40_var(0, 0)) failures++;
        if (Case_40_cns(42, 42) != Case_40_var(42, 42)) failures++;
        if (Case_40_cns(TestType.MinValue, TestType.MinValue) != Case_40_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_40_cns(TestType.MaxValue, TestType.MaxValue) != Case_40_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_40_cns(TestType.MaxValue, TestType.MinValue) != Case_40_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_40_cns(TestType.MinValue, TestType.MaxValue) != Case_40_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_41_cns(0, 0) != Case_41_var(0, 0)) failures++;
        if (Case_41_cns(42, 42) != Case_41_var(42, 42)) failures++;
        if (Case_41_cns(TestType.MinValue, TestType.MinValue) != Case_41_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_41_cns(TestType.MaxValue, TestType.MaxValue) != Case_41_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_41_cns(TestType.MaxValue, TestType.MinValue) != Case_41_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_41_cns(TestType.MinValue, TestType.MaxValue) != Case_41_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_42_cns(0, 0) != Case_42_var(0, 0)) failures++;
        if (Case_42_cns(42, 42) != Case_42_var(42, 42)) failures++;
        if (Case_42_cns(TestType.MinValue, TestType.MinValue) != Case_42_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_42_cns(TestType.MaxValue, TestType.MaxValue) != Case_42_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_42_cns(TestType.MaxValue, TestType.MinValue) != Case_42_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_42_cns(TestType.MinValue, TestType.MaxValue) != Case_42_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_43_cns(0, 0) != Case_43_var(0, 0)) failures++;
        if (Case_43_cns(42, 42) != Case_43_var(42, 42)) failures++;
        if (Case_43_cns(TestType.MinValue, TestType.MinValue) != Case_43_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_43_cns(TestType.MaxValue, TestType.MaxValue) != Case_43_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_43_cns(TestType.MaxValue, TestType.MinValue) != Case_43_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_43_cns(TestType.MinValue, TestType.MaxValue) != Case_43_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_44_cns(0, 0) != Case_44_var(0, 0)) failures++;
        if (Case_44_cns(42, 42) != Case_44_var(42, 42)) failures++;
        if (Case_44_cns(TestType.MinValue, TestType.MinValue) != Case_44_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_44_cns(TestType.MaxValue, TestType.MaxValue) != Case_44_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_44_cns(TestType.MaxValue, TestType.MinValue) != Case_44_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_44_cns(TestType.MinValue, TestType.MaxValue) != Case_44_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_45_cns(0, 0) != Case_45_var(0, 0)) failures++;
        if (Case_45_cns(42, 42) != Case_45_var(42, 42)) failures++;
        if (Case_45_cns(TestType.MinValue, TestType.MinValue) != Case_45_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_45_cns(TestType.MaxValue, TestType.MaxValue) != Case_45_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_45_cns(TestType.MaxValue, TestType.MinValue) != Case_45_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_45_cns(TestType.MinValue, TestType.MaxValue) != Case_45_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_46_cns(0, 0) != Case_46_var(0, 0)) failures++;
        if (Case_46_cns(42, 42) != Case_46_var(42, 42)) failures++;
        if (Case_46_cns(TestType.MinValue, TestType.MinValue) != Case_46_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_46_cns(TestType.MaxValue, TestType.MaxValue) != Case_46_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_46_cns(TestType.MaxValue, TestType.MinValue) != Case_46_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_46_cns(TestType.MinValue, TestType.MaxValue) != Case_46_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_47_cns(0, 0) != Case_47_var(0, 0)) failures++;
        if (Case_47_cns(42, 42) != Case_47_var(42, 42)) failures++;
        if (Case_47_cns(TestType.MinValue, TestType.MinValue) != Case_47_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_47_cns(TestType.MaxValue, TestType.MaxValue) != Case_47_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_47_cns(TestType.MaxValue, TestType.MinValue) != Case_47_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_47_cns(TestType.MinValue, TestType.MaxValue) != Case_47_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_48_cns(0, 0) != Case_48_var(0, 0)) failures++;
        if (Case_48_cns(42, 42) != Case_48_var(42, 42)) failures++;
        if (Case_48_cns(TestType.MinValue, TestType.MinValue) != Case_48_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_48_cns(TestType.MaxValue, TestType.MaxValue) != Case_48_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_48_cns(TestType.MaxValue, TestType.MinValue) != Case_48_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_48_cns(TestType.MinValue, TestType.MaxValue) != Case_48_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_49_cns(0, 0) != Case_49_var(0, 0)) failures++;
        if (Case_49_cns(42, 42) != Case_49_var(42, 42)) failures++;
        if (Case_49_cns(TestType.MinValue, TestType.MinValue) != Case_49_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_49_cns(TestType.MaxValue, TestType.MaxValue) != Case_49_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_49_cns(TestType.MaxValue, TestType.MinValue) != Case_49_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_49_cns(TestType.MinValue, TestType.MaxValue) != Case_49_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_50_cns(0, 0) != Case_50_var(0, 0)) failures++;
        if (Case_50_cns(42, 42) != Case_50_var(42, 42)) failures++;
        if (Case_50_cns(TestType.MinValue, TestType.MinValue) != Case_50_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_50_cns(TestType.MaxValue, TestType.MaxValue) != Case_50_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_50_cns(TestType.MaxValue, TestType.MinValue) != Case_50_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_50_cns(TestType.MinValue, TestType.MaxValue) != Case_50_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_51_cns(0, 0) != Case_51_var(0, 0)) failures++;
        if (Case_51_cns(42, 42) != Case_51_var(42, 42)) failures++;
        if (Case_51_cns(TestType.MinValue, TestType.MinValue) != Case_51_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_51_cns(TestType.MaxValue, TestType.MaxValue) != Case_51_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_51_cns(TestType.MaxValue, TestType.MinValue) != Case_51_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_51_cns(TestType.MinValue, TestType.MaxValue) != Case_51_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_52_cns(0, 0) != Case_52_var(0, 0)) failures++;
        if (Case_52_cns(42, 42) != Case_52_var(42, 42)) failures++;
        if (Case_52_cns(TestType.MinValue, TestType.MinValue) != Case_52_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_52_cns(TestType.MaxValue, TestType.MaxValue) != Case_52_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_52_cns(TestType.MaxValue, TestType.MinValue) != Case_52_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_52_cns(TestType.MinValue, TestType.MaxValue) != Case_52_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_53_cns(0, 0) != Case_53_var(0, 0)) failures++;
        if (Case_53_cns(42, 42) != Case_53_var(42, 42)) failures++;
        if (Case_53_cns(TestType.MinValue, TestType.MinValue) != Case_53_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_53_cns(TestType.MaxValue, TestType.MaxValue) != Case_53_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_53_cns(TestType.MaxValue, TestType.MinValue) != Case_53_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_53_cns(TestType.MinValue, TestType.MaxValue) != Case_53_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_54_cns(0, 0) != Case_54_var(0, 0)) failures++;
        if (Case_54_cns(42, 42) != Case_54_var(42, 42)) failures++;
        if (Case_54_cns(TestType.MinValue, TestType.MinValue) != Case_54_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_54_cns(TestType.MaxValue, TestType.MaxValue) != Case_54_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_54_cns(TestType.MaxValue, TestType.MinValue) != Case_54_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_54_cns(TestType.MinValue, TestType.MaxValue) != Case_54_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_55_cns(0, 0) != Case_55_var(0, 0)) failures++;
        if (Case_55_cns(42, 42) != Case_55_var(42, 42)) failures++;
        if (Case_55_cns(TestType.MinValue, TestType.MinValue) != Case_55_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_55_cns(TestType.MaxValue, TestType.MaxValue) != Case_55_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_55_cns(TestType.MaxValue, TestType.MinValue) != Case_55_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_55_cns(TestType.MinValue, TestType.MaxValue) != Case_55_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_56_cns(0, 0) != Case_56_var(0, 0)) failures++;
        if (Case_56_cns(42, 42) != Case_56_var(42, 42)) failures++;
        if (Case_56_cns(TestType.MinValue, TestType.MinValue) != Case_56_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_56_cns(TestType.MaxValue, TestType.MaxValue) != Case_56_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_56_cns(TestType.MaxValue, TestType.MinValue) != Case_56_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_56_cns(TestType.MinValue, TestType.MaxValue) != Case_56_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_57_cns(0, 0) != Case_57_var(0, 0)) failures++;
        if (Case_57_cns(42, 42) != Case_57_var(42, 42)) failures++;
        if (Case_57_cns(TestType.MinValue, TestType.MinValue) != Case_57_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_57_cns(TestType.MaxValue, TestType.MaxValue) != Case_57_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_57_cns(TestType.MaxValue, TestType.MinValue) != Case_57_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_57_cns(TestType.MinValue, TestType.MaxValue) != Case_57_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_58_cns(0, 0) != Case_58_var(0, 0)) failures++;
        if (Case_58_cns(42, 42) != Case_58_var(42, 42)) failures++;
        if (Case_58_cns(TestType.MinValue, TestType.MinValue) != Case_58_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_58_cns(TestType.MaxValue, TestType.MaxValue) != Case_58_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_58_cns(TestType.MaxValue, TestType.MinValue) != Case_58_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_58_cns(TestType.MinValue, TestType.MaxValue) != Case_58_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_59_cns(0, 0) != Case_59_var(0, 0)) failures++;
        if (Case_59_cns(42, 42) != Case_59_var(42, 42)) failures++;
        if (Case_59_cns(TestType.MinValue, TestType.MinValue) != Case_59_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_59_cns(TestType.MaxValue, TestType.MaxValue) != Case_59_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_59_cns(TestType.MaxValue, TestType.MinValue) != Case_59_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_59_cns(TestType.MinValue, TestType.MaxValue) != Case_59_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_60_cns(0, 0) != Case_60_var(0, 0)) failures++;
        if (Case_60_cns(42, 42) != Case_60_var(42, 42)) failures++;
        if (Case_60_cns(TestType.MinValue, TestType.MinValue) != Case_60_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_60_cns(TestType.MaxValue, TestType.MaxValue) != Case_60_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_60_cns(TestType.MaxValue, TestType.MinValue) != Case_60_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_60_cns(TestType.MinValue, TestType.MaxValue) != Case_60_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_61_cns(0, 0) != Case_61_var(0, 0)) failures++;
        if (Case_61_cns(42, 42) != Case_61_var(42, 42)) failures++;
        if (Case_61_cns(TestType.MinValue, TestType.MinValue) != Case_61_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_61_cns(TestType.MaxValue, TestType.MaxValue) != Case_61_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_61_cns(TestType.MaxValue, TestType.MinValue) != Case_61_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_61_cns(TestType.MinValue, TestType.MaxValue) != Case_61_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_62_cns(0, 0) != Case_62_var(0, 0)) failures++;
        if (Case_62_cns(42, 42) != Case_62_var(42, 42)) failures++;
        if (Case_62_cns(TestType.MinValue, TestType.MinValue) != Case_62_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_62_cns(TestType.MaxValue, TestType.MaxValue) != Case_62_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_62_cns(TestType.MaxValue, TestType.MinValue) != Case_62_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_62_cns(TestType.MinValue, TestType.MaxValue) != Case_62_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_63_cns(0, 0) != Case_63_var(0, 0)) failures++;
        if (Case_63_cns(42, 42) != Case_63_var(42, 42)) failures++;
        if (Case_63_cns(TestType.MinValue, TestType.MinValue) != Case_63_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_63_cns(TestType.MaxValue, TestType.MaxValue) != Case_63_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_63_cns(TestType.MaxValue, TestType.MinValue) != Case_63_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_63_cns(TestType.MinValue, TestType.MaxValue) != Case_63_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_64_cns(0, 0) != Case_64_var(0, 0)) failures++;
        if (Case_64_cns(42, 42) != Case_64_var(42, 42)) failures++;
        if (Case_64_cns(TestType.MinValue, TestType.MinValue) != Case_64_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_64_cns(TestType.MaxValue, TestType.MaxValue) != Case_64_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_64_cns(TestType.MaxValue, TestType.MinValue) != Case_64_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_64_cns(TestType.MinValue, TestType.MaxValue) != Case_64_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_65_cns(0, 0) != Case_65_var(0, 0)) failures++;
        if (Case_65_cns(42, 42) != Case_65_var(42, 42)) failures++;
        if (Case_65_cns(TestType.MinValue, TestType.MinValue) != Case_65_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_65_cns(TestType.MaxValue, TestType.MaxValue) != Case_65_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_65_cns(TestType.MaxValue, TestType.MinValue) != Case_65_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_65_cns(TestType.MinValue, TestType.MaxValue) != Case_65_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_66_cns(0, 0) != Case_66_var(0, 0)) failures++;
        if (Case_66_cns(42, 42) != Case_66_var(42, 42)) failures++;
        if (Case_66_cns(TestType.MinValue, TestType.MinValue) != Case_66_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_66_cns(TestType.MaxValue, TestType.MaxValue) != Case_66_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_66_cns(TestType.MaxValue, TestType.MinValue) != Case_66_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_66_cns(TestType.MinValue, TestType.MaxValue) != Case_66_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_67_cns(0, 0) != Case_67_var(0, 0)) failures++;
        if (Case_67_cns(42, 42) != Case_67_var(42, 42)) failures++;
        if (Case_67_cns(TestType.MinValue, TestType.MinValue) != Case_67_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_67_cns(TestType.MaxValue, TestType.MaxValue) != Case_67_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_67_cns(TestType.MaxValue, TestType.MinValue) != Case_67_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_67_cns(TestType.MinValue, TestType.MaxValue) != Case_67_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_68_cns(0, 0) != Case_68_var(0, 0)) failures++;
        if (Case_68_cns(42, 42) != Case_68_var(42, 42)) failures++;
        if (Case_68_cns(TestType.MinValue, TestType.MinValue) != Case_68_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_68_cns(TestType.MaxValue, TestType.MaxValue) != Case_68_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_68_cns(TestType.MaxValue, TestType.MinValue) != Case_68_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_68_cns(TestType.MinValue, TestType.MaxValue) != Case_68_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_69_cns(0, 0) != Case_69_var(0, 0)) failures++;
        if (Case_69_cns(42, 42) != Case_69_var(42, 42)) failures++;
        if (Case_69_cns(TestType.MinValue, TestType.MinValue) != Case_69_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_69_cns(TestType.MaxValue, TestType.MaxValue) != Case_69_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_69_cns(TestType.MaxValue, TestType.MinValue) != Case_69_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_69_cns(TestType.MinValue, TestType.MaxValue) != Case_69_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_70_cns(0, 0) != Case_70_var(0, 0)) failures++;
        if (Case_70_cns(42, 42) != Case_70_var(42, 42)) failures++;
        if (Case_70_cns(TestType.MinValue, TestType.MinValue) != Case_70_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_70_cns(TestType.MaxValue, TestType.MaxValue) != Case_70_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_70_cns(TestType.MaxValue, TestType.MinValue) != Case_70_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_70_cns(TestType.MinValue, TestType.MaxValue) != Case_70_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_71_cns(0, 0) != Case_71_var(0, 0)) failures++;
        if (Case_71_cns(42, 42) != Case_71_var(42, 42)) failures++;
        if (Case_71_cns(TestType.MinValue, TestType.MinValue) != Case_71_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_71_cns(TestType.MaxValue, TestType.MaxValue) != Case_71_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_71_cns(TestType.MaxValue, TestType.MinValue) != Case_71_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_71_cns(TestType.MinValue, TestType.MaxValue) != Case_71_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_72_cns(0, 0) != Case_72_var(0, 0)) failures++;
        if (Case_72_cns(42, 42) != Case_72_var(42, 42)) failures++;
        if (Case_72_cns(TestType.MinValue, TestType.MinValue) != Case_72_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_72_cns(TestType.MaxValue, TestType.MaxValue) != Case_72_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_72_cns(TestType.MaxValue, TestType.MinValue) != Case_72_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_72_cns(TestType.MinValue, TestType.MaxValue) != Case_72_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_73_cns(0, 0) != Case_73_var(0, 0)) failures++;
        if (Case_73_cns(42, 42) != Case_73_var(42, 42)) failures++;
        if (Case_73_cns(TestType.MinValue, TestType.MinValue) != Case_73_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_73_cns(TestType.MaxValue, TestType.MaxValue) != Case_73_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_73_cns(TestType.MaxValue, TestType.MinValue) != Case_73_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_73_cns(TestType.MinValue, TestType.MaxValue) != Case_73_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_74_cns(0, 0) != Case_74_var(0, 0)) failures++;
        if (Case_74_cns(42, 42) != Case_74_var(42, 42)) failures++;
        if (Case_74_cns(TestType.MinValue, TestType.MinValue) != Case_74_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_74_cns(TestType.MaxValue, TestType.MaxValue) != Case_74_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_74_cns(TestType.MaxValue, TestType.MinValue) != Case_74_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_74_cns(TestType.MinValue, TestType.MaxValue) != Case_74_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_75_cns(0, 0) != Case_75_var(0, 0)) failures++;
        if (Case_75_cns(42, 42) != Case_75_var(42, 42)) failures++;
        if (Case_75_cns(TestType.MinValue, TestType.MinValue) != Case_75_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_75_cns(TestType.MaxValue, TestType.MaxValue) != Case_75_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_75_cns(TestType.MaxValue, TestType.MinValue) != Case_75_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_75_cns(TestType.MinValue, TestType.MaxValue) != Case_75_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_76_cns(0, 0) != Case_76_var(0, 0)) failures++;
        if (Case_76_cns(42, 42) != Case_76_var(42, 42)) failures++;
        if (Case_76_cns(TestType.MinValue, TestType.MinValue) != Case_76_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_76_cns(TestType.MaxValue, TestType.MaxValue) != Case_76_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_76_cns(TestType.MaxValue, TestType.MinValue) != Case_76_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_76_cns(TestType.MinValue, TestType.MaxValue) != Case_76_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_77_cns(0, 0) != Case_77_var(0, 0)) failures++;
        if (Case_77_cns(42, 42) != Case_77_var(42, 42)) failures++;
        if (Case_77_cns(TestType.MinValue, TestType.MinValue) != Case_77_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_77_cns(TestType.MaxValue, TestType.MaxValue) != Case_77_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_77_cns(TestType.MaxValue, TestType.MinValue) != Case_77_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_77_cns(TestType.MinValue, TestType.MaxValue) != Case_77_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_78_cns(0, 0) != Case_78_var(0, 0)) failures++;
        if (Case_78_cns(42, 42) != Case_78_var(42, 42)) failures++;
        if (Case_78_cns(TestType.MinValue, TestType.MinValue) != Case_78_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_78_cns(TestType.MaxValue, TestType.MaxValue) != Case_78_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_78_cns(TestType.MaxValue, TestType.MinValue) != Case_78_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_78_cns(TestType.MinValue, TestType.MaxValue) != Case_78_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_79_cns(0, 0) != Case_79_var(0, 0)) failures++;
        if (Case_79_cns(42, 42) != Case_79_var(42, 42)) failures++;
        if (Case_79_cns(TestType.MinValue, TestType.MinValue) != Case_79_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_79_cns(TestType.MaxValue, TestType.MaxValue) != Case_79_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_79_cns(TestType.MaxValue, TestType.MinValue) != Case_79_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_79_cns(TestType.MinValue, TestType.MaxValue) != Case_79_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_80_cns(0, 0) != Case_80_var(0, 0)) failures++;
        if (Case_80_cns(42, 42) != Case_80_var(42, 42)) failures++;
        if (Case_80_cns(TestType.MinValue, TestType.MinValue) != Case_80_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_80_cns(TestType.MaxValue, TestType.MaxValue) != Case_80_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_80_cns(TestType.MaxValue, TestType.MinValue) != Case_80_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_80_cns(TestType.MinValue, TestType.MaxValue) != Case_80_var(TestType.MinValue, TestType.MaxValue)) failures++;
        if (Case_81_cns(0, 0) != Case_81_var(0, 0)) failures++;
        if (Case_81_cns(42, 42) != Case_81_var(42, 42)) failures++;
        if (Case_81_cns(TestType.MinValue, TestType.MinValue) != Case_81_var(TestType.MinValue, TestType.MinValue)) failures++;
        if (Case_81_cns(TestType.MaxValue, TestType.MaxValue) != Case_81_var(TestType.MaxValue, TestType.MaxValue)) failures++;
        if (Case_81_cns(TestType.MaxValue, TestType.MinValue) != Case_81_var(TestType.MaxValue, TestType.MinValue)) failures++;
        if (Case_81_cns(TestType.MinValue, TestType.MaxValue) != Case_81_var(TestType.MinValue, TestType.MaxValue)) failures++;
        return failures;
    }
}