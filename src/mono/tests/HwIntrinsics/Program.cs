using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

class HWIntrinsicsTests
{
    static void Main ()
    {
        bool generateReferenceData = false; // enable to update SRI-reference-data.txt (e.g. on CoreCLR for Mono)
        int failures = 0;
        string refDataFile = Path.Combine (Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location), "../../SRI-reference-data.txt");
        string [] refData = null;

        if (generateReferenceData)
            File.Delete (refDataFile);
        else
            refData = File.ReadAllLines (refDataFile);

        int sse1Methods = typeof (Sse).GetMethods ().Length;
        int sse2Methods = typeof (Sse2).GetMethods ().Length;
        int sse3Methods = typeof (Sse3).GetMethods ().Length;
        int ssse3Methods = typeof (Ssse3).GetMethods ().Length;
        int sse41Methods = typeof (Sse41).GetMethods ().Length;
        int sse42Methods = typeof (Sse42).GetMethods ().Length;

        Console.WriteLine ($"Sse:   {sse1Methods} methods");
        Console.WriteLine ($"Sse2:  {sse2Methods} methods");
        Console.WriteLine ($"Sse3:  {sse3Methods} methods");
        Console.WriteLine ($"Ssse3: {ssse3Methods} methods");
        Console.WriteLine ($"Sse41: {sse41Methods} methods");
        Console.WriteLine ($"Sse42: {sse42Methods} methods");
        Console.WriteLine ();

        if (sse1Methods != 92)
            throw new Exception ("New changes in Sse (don't forget to update simd-intrinsics.c if necessary)");
        if (sse2Methods != 302)
            throw new Exception ("New changes in Sse2 (don't forget to update simd-intrinsics.c if necessary)");
        if (sse3Methods != 23)
            throw new Exception ("New changes in Sse3 (don't forget to update simd-intrinsics.c if necessary)");
        if (ssse3Methods != 29)
            throw new Exception ("New changes in Ssse3 (don't forget to update simd-intrinsics.c if necessary)");
        if (sse41Methods != 144)
            throw new Exception ("New changes in Sse41 (don't forget to update simd-intrinsics.c if necessary)");
        if (sse42Methods != 9)
            throw new Exception ("New changes in Sse42 (don't forget to update simd-intrinsics.c if necessary)");

        int skipped = 0;
        var tests = new SseTests ();
        foreach (var method in typeof (SseTests).GetMethods ()
            .OrderBy (m => m.Name) // TODO: the default order is different in Mono
            .Where (m => m.GetParameters ().Length == 0 && m.DeclaringType == typeof (SseTests)))
        {
            try
            {
                // clear test data each iteration
                tests.ReloadArrays ();
                var obj = method.Invoke (tests, null);
                string array2Data = tests.GetArray2Data ();
                string actualResult = $"{method.Name}: {obj}. array2: ({array2Data})\n";
                if (generateReferenceData)
                {
                    File.AppendAllText (refDataFile, actualResult);
                }
                else
                {
                    var expectedResult = refData.FirstOrDefault (l => l.StartsWith (method.Name));
                    if (expectedResult.Trim ('\r', '\n', ' ') != actualResult.Trim ('\r', '\n', ' '))
                    {
                        Console.WriteLine ($"[FAIL] Expected: {expectedResult}, Actual: {actualResult}");
                        failures++;
                    }
                }
            }
            catch (TargetInvocationException e)
            {
                if (e.GetBaseException () is PlatformNotSupportedException) // Not supported by Mono yet
                {
                    skipped++;
                    continue;
                }
                throw;
            }
        }

        Console.WriteLine ();
        Console.WriteLine ("Skipped: " + skipped);
        Console.WriteLine ("Done.");

        if (failures != 0)
            throw new Exception ("Test failed.");
    }
}

public unsafe class SseTests
{
    Random rand = new Random (42);
    float* pArray1Float = null;
    float* pArray2Float = null;
    double* pArray1Double = null;
    double* pArray2Double = null;
    byte* pArray1 = null;
    byte* pArray2 = null;

    // some SRI methods require memory to be aligned to 16bytes boundary (in case of Vector128)
    static T* AllocateAligned<T> (int elements) where T : unmanaged
    {
        IntPtr ptr = Marshal.AllocHGlobal (elements * sizeof (T) + 15);
        return (T*)(16 * (((long)ptr + 15) / 16));
    }

    public void ReloadArrays ()
    {
        if (pArray1Float == null)
        {
            pArray1Float = AllocateAligned<float> (4);
            pArray2Float = AllocateAligned<float> (4);
            pArray1Double = AllocateAligned<double> (2);
            pArray2Double = AllocateAligned<double> (2);
            pArray1 = AllocateAligned<byte> (16);
            pArray2 = AllocateAligned<byte> (16);
        }

        for (byte i = 0; i < 16; i++)
        {
            pArray1 [i] = i;
            pArray2 [i] = 0;
        }

        for (int i = 0; i < 4; i++)
        {
            pArray1Float [i] = i / 2f;
            pArray2Float [i] = 0;
        }

        for (int i = 0; i < 2; i++)
        {
            pArray1Double [i] = i / 3f;
            pArray2Double [i] = 0;
        }
    }

    private bool IsEmpty<T> (T* array) where T : unmanaged
    {
        for (var i = 0; i < Vector128<T>.Count; i++)
        {
            var item = array [i];
            if (item is float f && f != 0.0f)
                return false;
            if (item is double d && d != 0.0f)
                return false;
            if (item is byte b && b != 0)
                return false;
        }
        return true;
    }

    private string PrintArray<T> (T* array) where T : unmanaged
    {
        var sb = new StringBuilder ();
        sb.Append ("<");
        for (var i = 0; i < Vector128<T>.Count; i++)
            sb.Append (array [i]).Append (", ");
        sb.Append (">");
        return sb.ToString ().Replace (", >", ">");
    }

    public string GetArray2Data ()
    {
        if (!IsEmpty (pArray2))
            return PrintArray (pArray2);
        if (!IsEmpty (pArray2Float))
            return PrintArray (pArray2Float);
        if (!IsEmpty (pArray2Double))
            return PrintArray (pArray2Double);
        return "";
    }

    [MethodImpl (MethodImplOptions.NoInlining)]
    Vector128<T> GetV128<T> () where T : unmanaged
    {
        int randMult = 1;
        int rand0_10 = rand.Next (0, 11);
        if (rand0_10 % 3 == 0)
            randMult = -1;
        else if (rand0_10 > 8)
            randMult = 0;

        if (typeof(T) == typeof (float))
        {
            return Vector128.Create (
                (float)(rand.Next () * randMult) / 2.0f,
                (float)(rand.Next () * randMult) / 3.0f,
                (float)(rand.Next () * randMult) / 4.0f,
                (float)(rand.Next () * randMult) / 5.0f).As<float, T> ();
        }
        else if (typeof(T) == typeof (double))
        {
            return Vector128.Create (
                (double)(rand.Next () * randMult),
                (double)(rand.Next () * randMult)).As<double, T> ();
        }

        return Vector128.Create (
            rand.Next (0, int.MaxValue) * randMult,
            rand.Next (0, int.MaxValue) * randMult,
            rand.Next (0, int.MaxValue) * randMult,
            rand.Next (0, int.MaxValue) * randMult).As<int, T> ();
    }

    T Get<T> ()
    {
        int randMult = 1;
        int rand0_10 = rand.Next (0, 11);
        if (rand0_10 % 3 == 0)
            randMult = -1;
        else if (rand0_10 > 8)
            randMult = 0;

        if (typeof (T) == typeof (float))
            return (T)(object)(float)((rand.Next() * randMult) / 2.0f);
        if (typeof (T) == typeof (double))
            return (T)(object)(double)((rand.Next() * randMult) / 2.0);
        if (typeof (T) == typeof (byte))
            return (T)(object)(byte)((rand.Next (byte.MinValue, byte.MaxValue + 1)));
        if (typeof (T) == typeof (sbyte))
            return (T)(object)(sbyte)((rand.Next (sbyte.MinValue, sbyte.MaxValue + 1)));
        if (typeof (T) == typeof (short))
            return (T)(object)(short)((rand.Next (short.MinValue, short.MaxValue + 1)));
        if (typeof (T) == typeof (ushort))
            return (T)(object)(ushort)((rand.Next (ushort.MinValue, ushort.MaxValue + 1)));
        if (typeof (T) == typeof (int))
            return (T)(object)(int)((rand.Next (int.MinValue, int.MaxValue)));
        if (typeof (T) == typeof (uint))
            return (T)(object)(uint)((rand.Next (int.MinValue, int.MaxValue)));
        if (typeof (T) == typeof (long))
            return (T)(object)(long)((rand.Next (int.MinValue, int.MaxValue)));
        if (typeof (T) == typeof (ulong))
            return (T)(object)(ulong)((rand.Next (int.MinValue, int.MaxValue)));
        throw new NotSupportedException ();
    }

    public Vector128<Single> Sse_Add_0() => Sse.Add(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_AddScalar_1() => Sse.AddScalar(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_And_2() => Sse.And(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_AndNot_3() => Sse.AndNot(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_CompareEqual_4() => Sse.CompareEqual(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_CompareGreaterThan_5() => Sse.CompareGreaterThan(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_CompareGreaterThanOrEqual_6() => Sse.CompareGreaterThanOrEqual(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_CompareLessThan_7() => Sse.CompareLessThan(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_CompareLessThanOrEqual_8() => Sse.CompareLessThanOrEqual(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_CompareNotEqual_9() => Sse.CompareNotEqual(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_CompareNotGreaterThan_10() => Sse.CompareNotGreaterThan(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_CompareNotGreaterThanOrEqual_11() => Sse.CompareNotGreaterThanOrEqual(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_CompareNotLessThan_12() => Sse.CompareNotLessThan(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_CompareNotLessThanOrEqual_13() => Sse.CompareNotLessThanOrEqual(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_CompareOrdered_14() => Sse.CompareOrdered(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_CompareScalarEqual_15() => Sse.CompareScalarEqual(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_CompareScalarGreaterThan_16() => Sse.CompareScalarGreaterThan(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_CompareScalarGreaterThanOrEqual_17() => Sse.CompareScalarGreaterThanOrEqual(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_CompareScalarLessThan_18() => Sse.CompareScalarLessThan(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_CompareScalarLessThanOrEqual_19() => Sse.CompareScalarLessThanOrEqual(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_CompareScalarNotEqual_20() => Sse.CompareScalarNotEqual(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_CompareScalarNotGreaterThan_21() => Sse.CompareScalarNotGreaterThan(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_CompareScalarNotGreaterThanOrEqual_22() => Sse.CompareScalarNotGreaterThanOrEqual(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_CompareScalarNotLessThan_23() => Sse.CompareScalarNotLessThan(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_CompareScalarNotLessThanOrEqual_24() => Sse.CompareScalarNotLessThanOrEqual(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_CompareScalarOrdered_25() => Sse.CompareScalarOrdered(GetV128<Single>(), GetV128<Single>());
    public Boolean Sse_CompareScalarOrderedEqual_26() => Sse.CompareScalarOrderedEqual(GetV128<Single>(), GetV128<Single>());
    public Boolean Sse_CompareScalarOrderedGreaterThan_27() => Sse.CompareScalarOrderedGreaterThan(GetV128<Single>(), GetV128<Single>());
    public Boolean Sse_CompareScalarOrderedGreaterThanOrEqual_28() => Sse.CompareScalarOrderedGreaterThanOrEqual(GetV128<Single>(), GetV128<Single>());
    public Boolean Sse_CompareScalarOrderedLessThan_29() => Sse.CompareScalarOrderedLessThan(GetV128<Single>(), GetV128<Single>());
    public Boolean Sse_CompareScalarOrderedLessThanOrEqual_30() => Sse.CompareScalarOrderedLessThanOrEqual(GetV128<Single>(), GetV128<Single>());
    public Boolean Sse_CompareScalarOrderedNotEqual_31() => Sse.CompareScalarOrderedNotEqual(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_CompareScalarUnordered_32() => Sse.CompareScalarUnordered(GetV128<Single>(), GetV128<Single>());
    public Boolean Sse_CompareScalarUnorderedEqual_33() => Sse.CompareScalarUnorderedEqual(GetV128<Single>(), GetV128<Single>());
    public Boolean Sse_CompareScalarUnorderedGreaterThan_34() => Sse.CompareScalarUnorderedGreaterThan(GetV128<Single>(), GetV128<Single>());
    public Boolean Sse_CompareScalarUnorderedGreaterThanOrEqual_35() => Sse.CompareScalarUnorderedGreaterThanOrEqual(GetV128<Single>(), GetV128<Single>());
    public Boolean Sse_CompareScalarUnorderedLessThan_36() => Sse.CompareScalarUnorderedLessThan(GetV128<Single>(), GetV128<Single>());
    public Boolean Sse_CompareScalarUnorderedLessThanOrEqual_37() => Sse.CompareScalarUnorderedLessThanOrEqual(GetV128<Single>(), GetV128<Single>());
    public Boolean Sse_CompareScalarUnorderedNotEqual_38() => Sse.CompareScalarUnorderedNotEqual(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_CompareUnordered_39() => Sse.CompareUnordered(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_ConvertScalarToVector128Single_40() => Sse.ConvertScalarToVector128Single(GetV128<Single>(), Get<System.Int32>());
    public Int32 Sse_ConvertToInt32_41() => Sse.ConvertToInt32(GetV128<Single>());
    public Int32 Sse_ConvertToInt32WithTruncation_42() => Sse.ConvertToInt32WithTruncation(GetV128<Single>());
    public Vector128<Single> Sse_Divide_43() => Sse.Divide(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_DivideScalar_44() => Sse.DivideScalar(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_LoadAlignedVector128_46() => Sse.LoadAlignedVector128((pArray1Float));
    public Vector128<Single> Sse_LoadHigh_47() => Sse.LoadHigh(GetV128<Single>(), pArray1Float);
    public Vector128<Single> Sse_LoadLow_48() => Sse.LoadLow(GetV128<Single>(), pArray1Float);
    public Vector128<Single> Sse_LoadScalarVector128_49() => Sse.LoadScalarVector128(pArray1Float);
    public Vector128<Single> Sse_LoadVector128_50() => Sse.LoadVector128(pArray1Float);
    public Vector128<Single> Sse_Max_51() => Sse.Max(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_MaxScalar_52() => Sse.MaxScalar(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_Min_53() => Sse.Min(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_MinScalar_54() => Sse.MinScalar(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_MoveHighToLow_55() => Sse.MoveHighToLow(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_MoveLowToHigh_56() => Sse.MoveLowToHigh(GetV128<Single>(), GetV128<Single>());
    public Int32 Sse_MoveMask_57() => Sse.MoveMask(GetV128<Single>());
    public Vector128<Single> Sse_MoveScalar_58() => Sse.MoveScalar(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_Multiply_59() => Sse.Multiply(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_MultiplyScalar_60() => Sse.MultiplyScalar(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_Or_61() => Sse.Or(GetV128<Single>(), GetV128<Single>());
    public void Sse_Prefetch0_62() => Sse.Prefetch0(pArray1);
    public void Sse_Prefetch1_63() => Sse.Prefetch1(pArray1);
    public void Sse_Prefetch2_64() => Sse.Prefetch2(pArray1);
    public void Sse_PrefetchNonTemporal_65() => Sse.PrefetchNonTemporal(pArray1);
    public Vector128<Single> Sse_Reciprocal_66() => Sse.Reciprocal(GetV128<Single>());
    public Vector128<Single> Sse_ReciprocalScalar_67() => Sse.ReciprocalScalar(GetV128<Single>());
    public Vector128<Single> Sse_ReciprocalScalar_68() => Sse.ReciprocalScalar(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_ReciprocalSqrt_69() => Sse.ReciprocalSqrt(GetV128<Single>());
    public Vector128<Single> Sse_ReciprocalSqrtScalar_70() => Sse.ReciprocalSqrtScalar(GetV128<Single>());
    public Vector128<Single> Sse_ReciprocalSqrtScalar_71() => Sse.ReciprocalSqrtScalar(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_Shuffle_72() => Sse.Shuffle(GetV128<Single>(), GetV128<Single>(), 22);
    public Vector128<Single> Sse_Sqrt_73() => Sse.Sqrt(GetV128<Single>());
    public Vector128<Single> Sse_SqrtScalar_74() => Sse.SqrtScalar(GetV128<Single>());
    public Vector128<Single> Sse_SqrtScalar_75() => Sse.SqrtScalar(GetV128<Single>(), GetV128<Single>());
    public void Sse_Store_76() => Sse.Store(pArray2Float, GetV128<Single>());
    public void Sse_StoreAligned_77() => Sse.StoreAligned((pArray2Float), GetV128<Single>());
    public void Sse_StoreAlignedNonTemporal_78() => Sse.StoreAlignedNonTemporal((pArray2Float), GetV128<Single>());
    public void Sse_StoreFence_79() => Sse.StoreFence();
    public void Sse_StoreHigh_80() => Sse.StoreHigh(pArray2Float, GetV128<Single>());
    public void Sse_StoreLow_81() => Sse.StoreLow(pArray2Float, GetV128<Single>());
    public void Sse_StoreScalar_82() => Sse.StoreScalar(pArray2Float, GetV128<Single>());
    public Vector128<Single> Sse_Subtract_83() => Sse.Subtract(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_SubtractScalar_84() => Sse.SubtractScalar(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_UnpackHigh_85() => Sse.UnpackHigh(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_UnpackLow_86() => Sse.UnpackLow(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse_Xor_87() => Sse.Xor(GetV128<Single>(), GetV128<Single>());
    public Vector128<Byte> Sse2_Add_0() => Sse2.Add(GetV128<Byte>(), GetV128<Byte>());
    public Vector128<SByte> Sse2_Add_1() => Sse2.Add(GetV128<SByte>(), GetV128<SByte>());
    public Vector128<Int16> Sse2_Add_2() => Sse2.Add(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<UInt16> Sse2_Add_3() => Sse2.Add(GetV128<UInt16>(), GetV128<UInt16>());
    public Vector128<Int32> Sse2_Add_4() => Sse2.Add(GetV128<Int32>(), GetV128<Int32>());
    public Vector128<UInt32> Sse2_Add_5() => Sse2.Add(GetV128<UInt32>(), GetV128<UInt32>());
    public Vector128<Int64> Sse2_Add_6() => Sse2.Add(GetV128<Int64>(), GetV128<Int64>());
    public Vector128<UInt64> Sse2_Add_7() => Sse2.Add(GetV128<UInt64>(), GetV128<UInt64>());
    public Vector128<Double> Sse2_Add_8() => Sse2.Add(GetV128<Double>(), GetV128<Double>());
    public Vector128<SByte> Sse2_AddSaturate_9() => Sse2.AddSaturate(GetV128<SByte>(), GetV128<SByte>());
    public Vector128<Byte> Sse2_AddSaturate_10() => Sse2.AddSaturate(GetV128<Byte>(), GetV128<Byte>());
    public Vector128<Int16> Sse2_AddSaturate_11() => Sse2.AddSaturate(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<UInt16> Sse2_AddSaturate_12() => Sse2.AddSaturate(GetV128<UInt16>(), GetV128<UInt16>());
    public Vector128<Double> Sse2_AddScalar_13() => Sse2.AddScalar(GetV128<Double>(), GetV128<Double>());
    public Vector128<Byte> Sse2_And_14() => Sse2.And(GetV128<Byte>(), GetV128<Byte>());
    public Vector128<SByte> Sse2_And_15() => Sse2.And(GetV128<SByte>(), GetV128<SByte>());
    public Vector128<Int16> Sse2_And_16() => Sse2.And(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<UInt16> Sse2_And_17() => Sse2.And(GetV128<UInt16>(), GetV128<UInt16>());
    public Vector128<Int32> Sse2_And_18() => Sse2.And(GetV128<Int32>(), GetV128<Int32>());
    public Vector128<UInt32> Sse2_And_19() => Sse2.And(GetV128<UInt32>(), GetV128<UInt32>());
    public Vector128<Int64> Sse2_And_20() => Sse2.And(GetV128<Int64>(), GetV128<Int64>());
    public Vector128<UInt64> Sse2_And_21() => Sse2.And(GetV128<UInt64>(), GetV128<UInt64>());
    public Vector128<Double> Sse2_And_22() => Sse2.And(GetV128<Double>(), GetV128<Double>());
    public Vector128<Byte> Sse2_AndNot_23() => Sse2.AndNot(GetV128<Byte>(), GetV128<Byte>());
    public Vector128<SByte> Sse2_AndNot_24() => Sse2.AndNot(GetV128<SByte>(), GetV128<SByte>());
    public Vector128<Int16> Sse2_AndNot_25() => Sse2.AndNot(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<UInt16> Sse2_AndNot_26() => Sse2.AndNot(GetV128<UInt16>(), GetV128<UInt16>());
    public Vector128<Int32> Sse2_AndNot_27() => Sse2.AndNot(GetV128<Int32>(), GetV128<Int32>());
    public Vector128<UInt32> Sse2_AndNot_28() => Sse2.AndNot(GetV128<UInt32>(), GetV128<UInt32>());
    public Vector128<Int64> Sse2_AndNot_29() => Sse2.AndNot(GetV128<Int64>(), GetV128<Int64>());
    public Vector128<UInt64> Sse2_AndNot_30() => Sse2.AndNot(GetV128<UInt64>(), GetV128<UInt64>());
    public Vector128<Double> Sse2_AndNot_31() => Sse2.AndNot(GetV128<Double>(), GetV128<Double>());
    public Vector128<Byte> Sse2_Average_32() => Sse2.Average(GetV128<Byte>(), GetV128<Byte>());
    public Vector128<UInt16> Sse2_Average_33() => Sse2.Average(GetV128<UInt16>(), GetV128<UInt16>());
    public Vector128<SByte> Sse2_CompareEqual_34() => Sse2.CompareEqual(GetV128<SByte>(), GetV128<SByte>());
    public Vector128<Byte> Sse2_CompareEqual_35() => Sse2.CompareEqual(GetV128<Byte>(), GetV128<Byte>());
    public Vector128<Int16> Sse2_CompareEqual_36() => Sse2.CompareEqual(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<UInt16> Sse2_CompareEqual_37() => Sse2.CompareEqual(GetV128<UInt16>(), GetV128<UInt16>());
    public Vector128<Int32> Sse2_CompareEqual_38() => Sse2.CompareEqual(GetV128<Int32>(), GetV128<Int32>());
    public Vector128<UInt32> Sse2_CompareEqual_39() => Sse2.CompareEqual(GetV128<UInt32>(), GetV128<UInt32>());
    public Vector128<Double> Sse2_CompareEqual_40() => Sse2.CompareEqual(GetV128<Double>(), GetV128<Double>());
    public Vector128<SByte> Sse2_CompareGreaterThan_41() => Sse2.CompareGreaterThan(GetV128<SByte>(), GetV128<SByte>());
    public Vector128<Int16> Sse2_CompareGreaterThan_42() => Sse2.CompareGreaterThan(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<Int32> Sse2_CompareGreaterThan_43() => Sse2.CompareGreaterThan(GetV128<Int32>(), GetV128<Int32>());
    public Vector128<Double> Sse2_CompareGreaterThan_44() => Sse2.CompareGreaterThan(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_CompareGreaterThanOrEqual_45() => Sse2.CompareGreaterThanOrEqual(GetV128<Double>(), GetV128<Double>());
    public Vector128<SByte> Sse2_CompareLessThan_46() => Sse2.CompareLessThan(GetV128<SByte>(), GetV128<SByte>());
    public Vector128<Int16> Sse2_CompareLessThan_47() => Sse2.CompareLessThan(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<Int32> Sse2_CompareLessThan_48() => Sse2.CompareLessThan(GetV128<Int32>(), GetV128<Int32>());
    public Vector128<Double> Sse2_CompareLessThan_49() => Sse2.CompareLessThan(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_CompareLessThanOrEqual_50() => Sse2.CompareLessThanOrEqual(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_CompareNotEqual_51() => Sse2.CompareNotEqual(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_CompareNotGreaterThan_52() => Sse2.CompareNotGreaterThan(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_CompareNotGreaterThanOrEqual_53() => Sse2.CompareNotGreaterThanOrEqual(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_CompareNotLessThan_54() => Sse2.CompareNotLessThan(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_CompareNotLessThanOrEqual_55() => Sse2.CompareNotLessThanOrEqual(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_CompareOrdered_56() => Sse2.CompareOrdered(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_CompareScalarEqual_57() => Sse2.CompareScalarEqual(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_CompareScalarGreaterThan_58() => Sse2.CompareScalarGreaterThan(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_CompareScalarGreaterThanOrEqual_59() => Sse2.CompareScalarGreaterThanOrEqual(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_CompareScalarLessThan_60() => Sse2.CompareScalarLessThan(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_CompareScalarLessThanOrEqual_61() => Sse2.CompareScalarLessThanOrEqual(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_CompareScalarNotEqual_62() => Sse2.CompareScalarNotEqual(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_CompareScalarNotGreaterThan_63() => Sse2.CompareScalarNotGreaterThan(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_CompareScalarNotGreaterThanOrEqual_64() => Sse2.CompareScalarNotGreaterThanOrEqual(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_CompareScalarNotLessThan_65() => Sse2.CompareScalarNotLessThan(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_CompareScalarNotLessThanOrEqual_66() => Sse2.CompareScalarNotLessThanOrEqual(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_CompareScalarOrdered_67() => Sse2.CompareScalarOrdered(GetV128<Double>(), GetV128<Double>());
    public Boolean Sse2_CompareScalarOrderedEqual_68() => Sse2.CompareScalarOrderedEqual(GetV128<Double>(), GetV128<Double>());
    public Boolean Sse2_CompareScalarOrderedGreaterThan_69() => Sse2.CompareScalarOrderedGreaterThan(GetV128<Double>(), GetV128<Double>());
    public Boolean Sse2_CompareScalarOrderedGreaterThanOrEqual_70() => Sse2.CompareScalarOrderedGreaterThanOrEqual(GetV128<Double>(), GetV128<Double>());
    public Boolean Sse2_CompareScalarOrderedLessThan_71() => Sse2.CompareScalarOrderedLessThan(GetV128<Double>(), GetV128<Double>());
    public Boolean Sse2_CompareScalarOrderedLessThanOrEqual_72() => Sse2.CompareScalarOrderedLessThanOrEqual(GetV128<Double>(), GetV128<Double>());
    public Boolean Sse2_CompareScalarOrderedNotEqual_73() => Sse2.CompareScalarOrderedNotEqual(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_CompareScalarUnordered_74() => Sse2.CompareScalarUnordered(GetV128<Double>(), GetV128<Double>());
    public Boolean Sse2_CompareScalarUnorderedEqual_75() => Sse2.CompareScalarUnorderedEqual(GetV128<Double>(), GetV128<Double>());
    public Boolean Sse2_CompareScalarUnorderedGreaterThan_76() => Sse2.CompareScalarUnorderedGreaterThan(GetV128<Double>(), GetV128<Double>());
    public Boolean Sse2_CompareScalarUnorderedGreaterThanOrEqual_77() => Sse2.CompareScalarUnorderedGreaterThanOrEqual(GetV128<Double>(), GetV128<Double>());
    public Boolean Sse2_CompareScalarUnorderedLessThan_78() => Sse2.CompareScalarUnorderedLessThan(GetV128<Double>(), GetV128<Double>());
    public Boolean Sse2_CompareScalarUnorderedLessThanOrEqual_79() => Sse2.CompareScalarUnorderedLessThanOrEqual(GetV128<Double>(), GetV128<Double>());
    public Boolean Sse2_CompareScalarUnorderedNotEqual_80() => Sse2.CompareScalarUnorderedNotEqual(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_CompareUnordered_81() => Sse2.CompareUnordered(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_ConvertScalarToVector128Double_82() => Sse2.ConvertScalarToVector128Double(GetV128<Double>(), Get<System.Int32>());
    public Vector128<Double> Sse2_ConvertScalarToVector128Double_83() => Sse2.ConvertScalarToVector128Double(GetV128<Double>(), GetV128<Single>());
    public Vector128<Int32> Sse2_ConvertScalarToVector128Int32_84() => Sse2.ConvertScalarToVector128Int32(Get<System.Int32>());
    public Vector128<Single> Sse2_ConvertScalarToVector128Single_85() => Sse2.ConvertScalarToVector128Single(GetV128<Single>(), GetV128<Double>());
    public Vector128<UInt32> Sse2_ConvertScalarToVector128UInt32_86() => Sse2.ConvertScalarToVector128UInt32(Get<System.UInt32>());
    public Int32 Sse2_ConvertToInt32_87() => Sse2.ConvertToInt32(GetV128<Double>());
    public Int32 Sse2_ConvertToInt32_88() => Sse2.ConvertToInt32(GetV128<Int32>());
    public Int32 Sse2_ConvertToInt32WithTruncation_89() => Sse2.ConvertToInt32WithTruncation(GetV128<Double>());
    public UInt32 Sse2_ConvertToUInt32_90() => Sse2.ConvertToUInt32(GetV128<UInt32>());
    public Vector128<Double> Sse2_ConvertToVector128Double_91() => Sse2.ConvertToVector128Double(GetV128<Int32>());
    public Vector128<Double> Sse2_ConvertToVector128Double_92() => Sse2.ConvertToVector128Double(GetV128<Single>());
    public Vector128<Int32> Sse2_ConvertToVector128Int32_93() => Sse2.ConvertToVector128Int32(GetV128<Single>());
    public Vector128<Int32> Sse2_ConvertToVector128Int32_94() => Sse2.ConvertToVector128Int32(GetV128<Double>());
    public Vector128<Int32> Sse2_ConvertToVector128Int32WithTruncation_95() => Sse2.ConvertToVector128Int32WithTruncation(GetV128<Single>());
    public Vector128<Int32> Sse2_ConvertToVector128Int32WithTruncation_96() => Sse2.ConvertToVector128Int32WithTruncation(GetV128<Double>());
    public Vector128<Single> Sse2_ConvertToVector128Single_97() => Sse2.ConvertToVector128Single(GetV128<Int32>());
    public Vector128<Single> Sse2_ConvertToVector128Single_98() => Sse2.ConvertToVector128Single(GetV128<Double>());
    public Vector128<Double> Sse2_Divide_99() => Sse2.Divide(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_DivideScalar_100() => Sse2.DivideScalar(GetV128<Double>(), GetV128<Double>());
    public UInt16 Sse2_Extract_101() => Sse2.Extract(GetV128<UInt16>(), Get<System.Byte>());
    public Vector128<Int16> Sse2_Insert_103() => Sse2.Insert(GetV128<Int16>(), Get<System.Int16>(), Get<System.Byte>());
    public Vector128<UInt16> Sse2_Insert_104() => Sse2.Insert(GetV128<UInt16>(), Get<System.UInt16>(), Get<System.Byte>());
    public Vector128<SByte> Sse2_LoadAlignedVector128_105() => Sse2.LoadAlignedVector128(((sbyte*)pArray1));
    public Vector128<Byte> Sse2_LoadAlignedVector128_106() => Sse2.LoadAlignedVector128(((byte*)pArray1));
    public Vector128<Int16> Sse2_LoadAlignedVector128_107() => Sse2.LoadAlignedVector128(((short*)pArray1));
    public Vector128<UInt16> Sse2_LoadAlignedVector128_108() => Sse2.LoadAlignedVector128(((ushort*)pArray1));
    public Vector128<Int32> Sse2_LoadAlignedVector128_109() => Sse2.LoadAlignedVector128(((int*)pArray1));
    public Vector128<UInt32> Sse2_LoadAlignedVector128_110() => Sse2.LoadAlignedVector128(((uint*)pArray1));
    public Vector128<Int64> Sse2_LoadAlignedVector128_111() => Sse2.LoadAlignedVector128(((long*)pArray1));
    public Vector128<UInt64> Sse2_LoadAlignedVector128_112() => Sse2.LoadAlignedVector128(((ulong*)pArray1));
    public Vector128<Double> Sse2_LoadAlignedVector128_113() => Sse2.LoadAlignedVector128((pArray1Double));
    public void Sse2_LoadFence_114() => Sse2.LoadFence();
    public Vector128<Double> Sse2_LoadHigh_115() => Sse2.LoadHigh(GetV128<Double>(), pArray1Double);
    public Vector128<Double> Sse2_LoadLow_116() => Sse2.LoadLow(GetV128<Double>(), pArray1Double);
    public Vector128<Double> Sse2_LoadScalarVector128_117() => Sse2.LoadScalarVector128(pArray1Double);
    public Vector128<Int32> Sse2_LoadScalarVector128_118() => Sse2.LoadScalarVector128((int*)pArray1);
    public Vector128<UInt32> Sse2_LoadScalarVector128_119() => Sse2.LoadScalarVector128((uint*)pArray1);
    public Vector128<Int64> Sse2_LoadScalarVector128_120() => Sse2.LoadScalarVector128((long*)pArray1);
    public Vector128<UInt64> Sse2_LoadScalarVector128_121() => Sse2.LoadScalarVector128((ulong*)pArray1);
    public Vector128<SByte> Sse2_LoadVector128_122() => Sse2.LoadVector128((sbyte*)pArray1);
    public Vector128<Byte> Sse2_LoadVector128_123() => Sse2.LoadVector128((byte*)pArray1);
    public Vector128<Int16> Sse2_LoadVector128_124() => Sse2.LoadVector128((short*)pArray1);
    public Vector128<UInt16> Sse2_LoadVector128_125() => Sse2.LoadVector128((ushort*)pArray1);
    public Vector128<Int32> Sse2_LoadVector128_126() => Sse2.LoadVector128((int*)pArray1);
    public Vector128<UInt32> Sse2_LoadVector128_127() => Sse2.LoadVector128((uint*)pArray1);
    public Vector128<Int64> Sse2_LoadVector128_128() => Sse2.LoadVector128((long*)pArray1);
    public Vector128<UInt64> Sse2_LoadVector128_129() => Sse2.LoadVector128((ulong*)pArray1);
    public Vector128<Double> Sse2_LoadVector128_130() => Sse2.LoadVector128(pArray1Double);
    public void Sse2_MaskMove_131() => Sse2.MaskMove(GetV128<SByte>(), GetV128<SByte>(), (sbyte*)pArray1);
    public void Sse2_MaskMove_132() => Sse2.MaskMove(GetV128<Byte>(), GetV128<Byte>(), (byte*)pArray1);
    public Vector128<Byte> Sse2_Max_133() => Sse2.Max(GetV128<Byte>(), GetV128<Byte>());
    public Vector128<Int16> Sse2_Max_134() => Sse2.Max(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<Double> Sse2_Max_135() => Sse2.Max(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_MaxScalar_136() => Sse2.MaxScalar(GetV128<Double>(), GetV128<Double>());
    public void Sse2_MemoryFence_137() => Sse2.MemoryFence();
    public Vector128<Byte> Sse2_Min_138() => Sse2.Min(GetV128<Byte>(), GetV128<Byte>());
    public Vector128<Int16> Sse2_Min_139() => Sse2.Min(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<Double> Sse2_Min_140() => Sse2.Min(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse2_MinScalar_141() => Sse2.MinScalar(GetV128<Double>(), GetV128<Double>());
    public Int32 Sse2_MoveMask_142() => Sse2.MoveMask(GetV128<SByte>());
    public Int32 Sse2_MoveMask_143() => Sse2.MoveMask(GetV128<Byte>());
    public Int32 Sse2_MoveMask_144() => Sse2.MoveMask(GetV128<Double>());
    public Vector128<Double> Sse2_MoveScalar_145() => Sse2.MoveScalar(GetV128<Double>(), GetV128<Double>());
    public Vector128<Int64> Sse2_MoveScalar_146() => Sse2.MoveScalar(GetV128<Int64>());
    public Vector128<UInt64> Sse2_MoveScalar_147() => Sse2.MoveScalar(GetV128<UInt64>());
    public Vector128<UInt64> Sse2_Multiply_148() => Sse2.Multiply(GetV128<UInt32>(), GetV128<UInt32>());
    public Vector128<Double> Sse2_Multiply_149() => Sse2.Multiply(GetV128<Double>(), GetV128<Double>());
    public Vector128<Int32> Sse2_MultiplyAddAdjacent_150() => Sse2.MultiplyAddAdjacent(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<Int16> Sse2_MultiplyHigh_151() => Sse2.MultiplyHigh(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<UInt16> Sse2_MultiplyHigh_152() => Sse2.MultiplyHigh(GetV128<UInt16>(), GetV128<UInt16>());
    public Vector128<Int16> Sse2_MultiplyLow_153() => Sse2.MultiplyLow(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<UInt16> Sse2_MultiplyLow_154() => Sse2.MultiplyLow(GetV128<UInt16>(), GetV128<UInt16>());
    public Vector128<Double> Sse2_MultiplyScalar_155() => Sse2.MultiplyScalar(GetV128<Double>(), GetV128<Double>());
    public Vector128<Byte> Sse2_Or_156() => Sse2.Or(GetV128<Byte>(), GetV128<Byte>());
    public Vector128<SByte> Sse2_Or_157() => Sse2.Or(GetV128<SByte>(), GetV128<SByte>());
    public Vector128<Int16> Sse2_Or_158() => Sse2.Or(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<UInt16> Sse2_Or_159() => Sse2.Or(GetV128<UInt16>(), GetV128<UInt16>());
    public Vector128<Int32> Sse2_Or_160() => Sse2.Or(GetV128<Int32>(), GetV128<Int32>());
    public Vector128<UInt32> Sse2_Or_161() => Sse2.Or(GetV128<UInt32>(), GetV128<UInt32>());
    public Vector128<Int64> Sse2_Or_162() => Sse2.Or(GetV128<Int64>(), GetV128<Int64>());
    public Vector128<UInt64> Sse2_Or_163() => Sse2.Or(GetV128<UInt64>(), GetV128<UInt64>());
    public Vector128<Double> Sse2_Or_164() => Sse2.Or(GetV128<Double>(), GetV128<Double>());
    public Vector128<SByte> Sse2_PackSignedSaturate_165() => Sse2.PackSignedSaturate(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<Int16> Sse2_PackSignedSaturate_166() => Sse2.PackSignedSaturate(GetV128<Int32>(), GetV128<Int32>());
    public Vector128<Byte> Sse2_PackUnsignedSaturate_167() => Sse2.PackUnsignedSaturate(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<Int16> Sse2_ShiftLeftLogical_168() => Sse2.ShiftLeftLogical(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<UInt16> Sse2_ShiftLeftLogical_169() => Sse2.ShiftLeftLogical(GetV128<UInt16>(), GetV128<UInt16>());
    public Vector128<Int32> Sse2_ShiftLeftLogical_170() => Sse2.ShiftLeftLogical(GetV128<Int32>(), GetV128<Int32>());
    public Vector128<UInt32> Sse2_ShiftLeftLogical_171() => Sse2.ShiftLeftLogical(GetV128<UInt32>(), GetV128<UInt32>());
    public Vector128<Int64> Sse2_ShiftLeftLogical_172() => Sse2.ShiftLeftLogical(GetV128<Int64>(), GetV128<Int64>());
    public Vector128<UInt64> Sse2_ShiftLeftLogical_173() => Sse2.ShiftLeftLogical(GetV128<UInt64>(), GetV128<UInt64>());
    public Vector128<Int16> Sse2_ShiftLeftLogical_174() => Sse2.ShiftLeftLogical(GetV128<Int16>(), Get<System.Byte>());
    public Vector128<UInt16> Sse2_ShiftLeftLogical_175() => Sse2.ShiftLeftLogical(GetV128<UInt16>(), Get<System.Byte>());
    public Vector128<Int32> Sse2_ShiftLeftLogical_176() => Sse2.ShiftLeftLogical(GetV128<Int32>(), Get<System.Byte>());
    public Vector128<UInt32> Sse2_ShiftLeftLogical_177() => Sse2.ShiftLeftLogical(GetV128<UInt32>(), Get<System.Byte>());
    public Vector128<Int64> Sse2_ShiftLeftLogical_178() => Sse2.ShiftLeftLogical(GetV128<Int64>(), Get<System.Byte>());
    public Vector128<UInt64> Sse2_ShiftLeftLogical_179() => Sse2.ShiftLeftLogical(GetV128<UInt64>(), Get<System.Byte>());
    public Vector128<SByte> Sse2_ShiftLeftLogical128BitLane_180() => Sse2.ShiftLeftLogical128BitLane(GetV128<SByte>(), Get<System.Byte>());
    public Vector128<Byte> Sse2_ShiftLeftLogical128BitLane_181() => Sse2.ShiftLeftLogical128BitLane(GetV128<Byte>(), Get<System.Byte>());
    public Vector128<Int16> Sse2_ShiftLeftLogical128BitLane_182() => Sse2.ShiftLeftLogical128BitLane(GetV128<Int16>(), Get<System.Byte>());
    public Vector128<UInt16> Sse2_ShiftLeftLogical128BitLane_183() => Sse2.ShiftLeftLogical128BitLane(GetV128<UInt16>(), Get<System.Byte>());
    public Vector128<Int32> Sse2_ShiftLeftLogical128BitLane_184() => Sse2.ShiftLeftLogical128BitLane(GetV128<Int32>(), Get<System.Byte>());
    public Vector128<UInt32> Sse2_ShiftLeftLogical128BitLane_185() => Sse2.ShiftLeftLogical128BitLane(GetV128<UInt32>(), Get<System.Byte>());
    public Vector128<Int64> Sse2_ShiftLeftLogical128BitLane_186() => Sse2.ShiftLeftLogical128BitLane(GetV128<Int64>(), Get<System.Byte>());
    public Vector128<UInt64> Sse2_ShiftLeftLogical128BitLane_187() => Sse2.ShiftLeftLogical128BitLane(GetV128<UInt64>(), Get<System.Byte>());
    public Vector128<Int16> Sse2_ShiftRightArithmetic_188() => Sse2.ShiftRightArithmetic(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<Int32> Sse2_ShiftRightArithmetic_189() => Sse2.ShiftRightArithmetic(GetV128<Int32>(), GetV128<Int32>());
    public Vector128<Int16> Sse2_ShiftRightArithmetic_190() => Sse2.ShiftRightArithmetic(GetV128<Int16>(), Get<System.Byte>());
    public Vector128<Int32> Sse2_ShiftRightArithmetic_191() => Sse2.ShiftRightArithmetic(GetV128<Int32>(), Get<System.Byte>());
    public Vector128<Int16> Sse2_ShiftRightLogical_192() => Sse2.ShiftRightLogical(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<UInt16> Sse2_ShiftRightLogical_193() => Sse2.ShiftRightLogical(GetV128<UInt16>(), GetV128<UInt16>());
    public Vector128<Int32> Sse2_ShiftRightLogical_194() => Sse2.ShiftRightLogical(GetV128<Int32>(), GetV128<Int32>());
    public Vector128<UInt32> Sse2_ShiftRightLogical_195() => Sse2.ShiftRightLogical(GetV128<UInt32>(), GetV128<UInt32>());
    public Vector128<Int64> Sse2_ShiftRightLogical_196() => Sse2.ShiftRightLogical(GetV128<Int64>(), GetV128<Int64>());
    public Vector128<UInt64> Sse2_ShiftRightLogical_197() => Sse2.ShiftRightLogical(GetV128<UInt64>(), GetV128<UInt64>());
    public Vector128<Int16> Sse2_ShiftRightLogical_198() => Sse2.ShiftRightLogical(GetV128<Int16>(), Get<System.Byte>());
    public Vector128<UInt16> Sse2_ShiftRightLogical_199() => Sse2.ShiftRightLogical(GetV128<UInt16>(), Get<System.Byte>());
    public Vector128<Int32> Sse2_ShiftRightLogical_200() => Sse2.ShiftRightLogical(GetV128<Int32>(), Get<System.Byte>());
    public Vector128<UInt32> Sse2_ShiftRightLogical_201() => Sse2.ShiftRightLogical(GetV128<UInt32>(), Get<System.Byte>());
    public Vector128<Int64> Sse2_ShiftRightLogical_202() => Sse2.ShiftRightLogical(GetV128<Int64>(), Get<System.Byte>());
    public Vector128<UInt64> Sse2_ShiftRightLogical_203() => Sse2.ShiftRightLogical(GetV128<UInt64>(), Get<System.Byte>());
    public Vector128<SByte> Sse2_ShiftRightLogical128BitLane_204() => Sse2.ShiftRightLogical128BitLane(GetV128<SByte>(), Get<System.Byte>());
    public Vector128<Byte> Sse2_ShiftRightLogical128BitLane_205() => Sse2.ShiftRightLogical128BitLane(GetV128<Byte>(), Get<System.Byte>());
    public Vector128<Int16> Sse2_ShiftRightLogical128BitLane_206() => Sse2.ShiftRightLogical128BitLane(GetV128<Int16>(), Get<System.Byte>());
    public Vector128<UInt16> Sse2_ShiftRightLogical128BitLane_207() => Sse2.ShiftRightLogical128BitLane(GetV128<UInt16>(), Get<System.Byte>());
    public Vector128<Int32> Sse2_ShiftRightLogical128BitLane_208() => Sse2.ShiftRightLogical128BitLane(GetV128<Int32>(), Get<System.Byte>());
    public Vector128<UInt32> Sse2_ShiftRightLogical128BitLane_209() => Sse2.ShiftRightLogical128BitLane(GetV128<UInt32>(), Get<System.Byte>());
    public Vector128<Int64> Sse2_ShiftRightLogical128BitLane_210() => Sse2.ShiftRightLogical128BitLane(GetV128<Int64>(), Get<System.Byte>());
    public Vector128<UInt64> Sse2_ShiftRightLogical128BitLane_211() => Sse2.ShiftRightLogical128BitLane(GetV128<UInt64>(), Get<System.Byte>());
    public Vector128<UInt32> Sse2_Shuffle_212() => Sse2.Shuffle(GetV128<UInt32>(), 12);
    public Vector128<Double> Sse2_Shuffle_213() => Sse2.Shuffle(GetV128<Double>(), GetV128<Double>(), 3);
    public Vector128<Int32> Sse2_Shuffle_214() => Sse2.Shuffle(GetV128<Int32>(), 42);
    public Vector128<Int16> Sse2_ShuffleHigh_215() => Sse2.ShuffleHigh(GetV128<Int16>(), 23);
    public Vector128<UInt16> Sse2_ShuffleHigh_216() => Sse2.ShuffleHigh(GetV128<UInt16>(), 12);
    public Vector128<Int16> Sse2_ShuffleLow_217() => Sse2.ShuffleLow(GetV128<Int16>(), 5);
    public Vector128<UInt16> Sse2_ShuffleLow_218() => Sse2.ShuffleLow(GetV128<UInt16>(), 3);
    public Vector128<Double> Sse2_Sqrt_219() => Sse2.Sqrt(GetV128<Double>());
    public Vector128<Double> Sse2_SqrtScalar_220() => Sse2.SqrtScalar(GetV128<Double>());
    public Vector128<Double> Sse2_SqrtScalar_221() => Sse2.SqrtScalar(GetV128<Double>(), GetV128<Double>());
    public void Sse2_Store_222() => Sse2.Store((sbyte*)pArray2, GetV128<SByte>());
    public void Sse2_Store_223() => Sse2.Store((byte*)pArray2, GetV128<Byte>());
    public void Sse2_Store_224() => Sse2.Store((short*)pArray2, GetV128<Int16>());
    public void Sse2_Store_225() => Sse2.Store((ushort*)pArray2, GetV128<UInt16>());
    public void Sse2_Store_226() => Sse2.Store((int*)pArray2, GetV128<Int32>());
    public void Sse2_Store_227() => Sse2.Store((uint*)pArray2, GetV128<UInt32>());
    public void Sse2_Store_228() => Sse2.Store((long*)pArray2, GetV128<Int64>());
    public void Sse2_Store_229() => Sse2.Store((ulong*)pArray2, GetV128<UInt64>());
    public void Sse2_Store_230() => Sse2.Store(pArray1Double, GetV128<Double>());
    public void Sse2_StoreAligned_231() => Sse2.StoreAligned(((sbyte*)pArray2), GetV128<SByte>());
    public void Sse2_StoreAligned_232() => Sse2.StoreAligned(((byte*)pArray2), GetV128<Byte>());
    public void Sse2_StoreAligned_233() => Sse2.StoreAligned(((short*)pArray2), GetV128<Int16>());
    public void Sse2_StoreAligned_234() => Sse2.StoreAligned(((ushort*)pArray2), GetV128<UInt16>());
    public void Sse2_StoreAligned_235() => Sse2.StoreAligned(((int*)pArray2), GetV128<Int32>());
    public void Sse2_StoreAligned_236() => Sse2.StoreAligned(((uint*)pArray2), GetV128<UInt32>());
    public void Sse2_StoreAligned_237() => Sse2.StoreAligned(((long*)pArray2), GetV128<Int64>());
    public void Sse2_StoreAligned_238() => Sse2.StoreAligned(((ulong*)pArray2), GetV128<UInt64>());
    public void Sse2_StoreAligned_239() => Sse2.StoreAligned((pArray2Double), GetV128<Double>());
    public void Sse2_StoreAlignedNonTemporal_240() => Sse2.StoreAlignedNonTemporal(((sbyte*)pArray2), GetV128<SByte>());
    public void Sse2_StoreAlignedNonTemporal_241() => Sse2.StoreAlignedNonTemporal(((byte*)pArray2), GetV128<Byte>());
    public void Sse2_StoreAlignedNonTemporal_242() => Sse2.StoreAlignedNonTemporal(((short*)pArray2), GetV128<Int16>());
    public void Sse2_StoreAlignedNonTemporal_243() => Sse2.StoreAlignedNonTemporal(((ushort*)pArray2), GetV128<UInt16>());
    public void Sse2_StoreAlignedNonTemporal_244() => Sse2.StoreAlignedNonTemporal(((int*)pArray2), GetV128<Int32>());
    public void Sse2_StoreAlignedNonTemporal_245() => Sse2.StoreAlignedNonTemporal(((uint*)pArray2), GetV128<UInt32>());
    public void Sse2_StoreAlignedNonTemporal_246() => Sse2.StoreAlignedNonTemporal(((long*)pArray2), GetV128<Int64>());
    public void Sse2_StoreAlignedNonTemporal_247() => Sse2.StoreAlignedNonTemporal(((ulong*)pArray2), GetV128<UInt64>());
    public void Sse2_StoreAlignedNonTemporal_248() => Sse2.StoreAlignedNonTemporal((pArray2Double), GetV128<Double>());
    public void Sse2_StoreHigh_249() => Sse2.StoreHigh(pArray2Double, GetV128<Double>());
    public void Sse2_StoreLow_250() => Sse2.StoreLow(pArray2Double, GetV128<Double>());
    public void Sse2_StoreNonTemporal_251() => Sse2.StoreNonTemporal((int*)pArray2, Get<System.Int32>());
    public void Sse2_StoreNonTemporal_252() => Sse2.StoreNonTemporal((uint*)pArray2, Get<System.UInt32>());
    public void Sse2_StoreScalar_253() => Sse2.StoreScalar(pArray2Double, GetV128<Double>());
    public void Sse2_StoreScalar_254() => Sse2.StoreScalar((long*)pArray2, GetV128<Int64>());
    public void Sse2_StoreScalar_255() => Sse2.StoreScalar((ulong*)pArray2, GetV128<UInt64>());
    public Vector128<Byte> Sse2_Subtract_256() => Sse2.Subtract(GetV128<Byte>(), GetV128<Byte>());
    public Vector128<SByte> Sse2_Subtract_257() => Sse2.Subtract(GetV128<SByte>(), GetV128<SByte>());
    public Vector128<Int16> Sse2_Subtract_258() => Sse2.Subtract(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<UInt16> Sse2_Subtract_259() => Sse2.Subtract(GetV128<UInt16>(), GetV128<UInt16>());
    public Vector128<Int32> Sse2_Subtract_260() => Sse2.Subtract(GetV128<Int32>(), GetV128<Int32>());
    public Vector128<UInt32> Sse2_Subtract_261() => Sse2.Subtract(GetV128<UInt32>(), GetV128<UInt32>());
    public Vector128<Int64> Sse2_Subtract_262() => Sse2.Subtract(GetV128<Int64>(), GetV128<Int64>());
    public Vector128<UInt64> Sse2_Subtract_263() => Sse2.Subtract(GetV128<UInt64>(), GetV128<UInt64>());
    public Vector128<Double> Sse2_Subtract_264() => Sse2.Subtract(GetV128<Double>(), GetV128<Double>());
    public Vector128<SByte> Sse2_SubtractSaturate_265() => Sse2.SubtractSaturate(GetV128<SByte>(), GetV128<SByte>());
    public Vector128<Int16> Sse2_SubtractSaturate_266() => Sse2.SubtractSaturate(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<Byte> Sse2_SubtractSaturate_267() => Sse2.SubtractSaturate(GetV128<Byte>(), GetV128<Byte>());
    public Vector128<UInt16> Sse2_SubtractSaturate_268() => Sse2.SubtractSaturate(GetV128<UInt16>(), GetV128<UInt16>());
    public Vector128<Double> Sse2_SubtractScalar_269() => Sse2.SubtractScalar(GetV128<Double>(), GetV128<Double>());
    public Vector128<UInt16> Sse2_SumAbsoluteDifferences_270() => Sse2.SumAbsoluteDifferences(GetV128<Byte>(), GetV128<Byte>());
    public Vector128<Byte> Sse2_UnpackHigh_271() => Sse2.UnpackHigh(GetV128<Byte>(), GetV128<Byte>());
    public Vector128<SByte> Sse2_UnpackHigh_272() => Sse2.UnpackHigh(GetV128<SByte>(), GetV128<SByte>());
    public Vector128<Int16> Sse2_UnpackHigh_273() => Sse2.UnpackHigh(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<UInt16> Sse2_UnpackHigh_274() => Sse2.UnpackHigh(GetV128<UInt16>(), GetV128<UInt16>());
    public Vector128<Int32> Sse2_UnpackHigh_275() => Sse2.UnpackHigh(GetV128<Int32>(), GetV128<Int32>());
    public Vector128<UInt32> Sse2_UnpackHigh_276() => Sse2.UnpackHigh(GetV128<UInt32>(), GetV128<UInt32>());
    public Vector128<Int64> Sse2_UnpackHigh_277() => Sse2.UnpackHigh(GetV128<Int64>(), GetV128<Int64>());
    public Vector128<UInt64> Sse2_UnpackHigh_278() => Sse2.UnpackHigh(GetV128<UInt64>(), GetV128<UInt64>());
    public Vector128<Double> Sse2_UnpackHigh_279() => Sse2.UnpackHigh(GetV128<Double>(), GetV128<Double>());
    public Vector128<Byte> Sse2_UnpackLow_280() => Sse2.UnpackLow(GetV128<Byte>(), GetV128<Byte>());
    public Vector128<SByte> Sse2_UnpackLow_281() => Sse2.UnpackLow(GetV128<SByte>(), GetV128<SByte>());
    public Vector128<Int16> Sse2_UnpackLow_282() => Sse2.UnpackLow(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<UInt16> Sse2_UnpackLow_283() => Sse2.UnpackLow(GetV128<UInt16>(), GetV128<UInt16>());
    public Vector128<Int32> Sse2_UnpackLow_284() => Sse2.UnpackLow(GetV128<Int32>(), GetV128<Int32>());
    public Vector128<UInt32> Sse2_UnpackLow_285() => Sse2.UnpackLow(GetV128<UInt32>(), GetV128<UInt32>());
    public Vector128<Int64> Sse2_UnpackLow_286() => Sse2.UnpackLow(GetV128<Int64>(), GetV128<Int64>());
    public Vector128<UInt64> Sse2_UnpackLow_287() => Sse2.UnpackLow(GetV128<UInt64>(), GetV128<UInt64>());
    public Vector128<Double> Sse2_UnpackLow_288() => Sse2.UnpackLow(GetV128<Double>(), GetV128<Double>());
    public Vector128<Byte> Sse2_Xor_289() => Sse2.Xor(GetV128<Byte>(), GetV128<Byte>());
    public Vector128<SByte> Sse2_Xor_290() => Sse2.Xor(GetV128<SByte>(), GetV128<SByte>());
    public Vector128<Int16> Sse2_Xor_291() => Sse2.Xor(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<UInt16> Sse2_Xor_292() => Sse2.Xor(GetV128<UInt16>(), GetV128<UInt16>());
    public Vector128<Int32> Sse2_Xor_293() => Sse2.Xor(GetV128<Int32>(), GetV128<Int32>());
    public Vector128<UInt32> Sse2_Xor_294() => Sse2.Xor(GetV128<UInt32>(), GetV128<UInt32>());
    public Vector128<Int64> Sse2_Xor_295() => Sse2.Xor(GetV128<Int64>(), GetV128<Int64>());
    public Vector128<UInt64> Sse2_Xor_296() => Sse2.Xor(GetV128<UInt64>(), GetV128<UInt64>());
    public Vector128<Double> Sse2_Xor_297() => Sse2.Xor(GetV128<Double>(), GetV128<Double>());
    public Vector128<Single> Sse3_AddSubtract_0() => Sse3.AddSubtract(GetV128<Single>(), GetV128<Single>());
    public Vector128<Double> Sse3_AddSubtract_1() => Sse3.AddSubtract(GetV128<Double>(), GetV128<Double>());
    public Vector128<Single> Sse3_HorizontalAdd_3() => Sse3.HorizontalAdd(GetV128<Single>(), GetV128<Single>());
    public Vector128<Double> Sse3_HorizontalAdd_4() => Sse3.HorizontalAdd(GetV128<Double>(), GetV128<Double>());
    public Vector128<Single> Sse3_HorizontalSubtract_5() => Sse3.HorizontalSubtract(GetV128<Single>(), GetV128<Single>());
    public Vector128<Double> Sse3_HorizontalSubtract_6() => Sse3.HorizontalSubtract(GetV128<Double>(), GetV128<Double>());
    public Vector128<Double> Sse3_LoadAndDuplicateToVector128_7() => Sse3.LoadAndDuplicateToVector128(pArray1Double);
    public Vector128<SByte> Sse3_LoadDquVector128_8() => Sse3.LoadDquVector128((sbyte*)pArray1);
    public Vector128<Byte> Sse3_LoadDquVector128_9() => Sse3.LoadDquVector128((byte*)pArray1);
    public Vector128<Int16> Sse3_LoadDquVector128_10() => Sse3.LoadDquVector128((short*)pArray1);
    public Vector128<UInt16> Sse3_LoadDquVector128_11() => Sse3.LoadDquVector128((ushort*)pArray1);
    public Vector128<Int32> Sse3_LoadDquVector128_12() => Sse3.LoadDquVector128((int*)pArray1);
    public Vector128<UInt32> Sse3_LoadDquVector128_13() => Sse3.LoadDquVector128((uint*)pArray1);
    public Vector128<Int64> Sse3_LoadDquVector128_14() => Sse3.LoadDquVector128((long*)pArray1);
    public Vector128<UInt64> Sse3_LoadDquVector128_15() => Sse3.LoadDquVector128((ulong*)pArray1);
    public Vector128<Double> Sse3_MoveAndDuplicate_16() => Sse3.MoveAndDuplicate(GetV128<Double>());
    public Vector128<Single> Sse3_MoveHighAndDuplicate_17() => Sse3.MoveHighAndDuplicate(GetV128<Single>());
    public Vector128<Single> Sse3_MoveLowAndDuplicate_18() => Sse3.MoveLowAndDuplicate(GetV128<Single>());
    public Vector128<Byte> Ssse3_Abs_0() => Ssse3.Abs(GetV128<SByte>());
    public Vector128<UInt16> Ssse3_Abs_1() => Ssse3.Abs(GetV128<Int16>());
    public Vector128<UInt32> Ssse3_Abs_2() => Ssse3.Abs(GetV128<Int32>());
    public Vector128<SByte> Ssse3_AlignRight_3() => Ssse3.AlignRight(GetV128<SByte>(), GetV128<SByte>(), Get<System.Byte>());
    public Vector128<Byte> Ssse3_AlignRight_4() => Ssse3.AlignRight(GetV128<Byte>(), GetV128<Byte>(), Get<System.Byte>());
    public Vector128<Int16> Ssse3_AlignRight_5() => Ssse3.AlignRight(GetV128<Int16>(), GetV128<Int16>(), Get<System.Byte>());
    public Vector128<UInt16> Ssse3_AlignRight_6() => Ssse3.AlignRight(GetV128<UInt16>(), GetV128<UInt16>(), Get<System.Byte>());
    public Vector128<Int32> Ssse3_AlignRight_7() => Ssse3.AlignRight(GetV128<Int32>(), GetV128<Int32>(), Get<System.Byte>());
    public Vector128<UInt32> Ssse3_AlignRight_8() => Ssse3.AlignRight(GetV128<UInt32>(), GetV128<UInt32>(), Get<System.Byte>());
    public Vector128<Int64> Ssse3_AlignRight_9() => Ssse3.AlignRight(GetV128<Int64>(), GetV128<Int64>(), Get<System.Byte>());
    public Vector128<UInt64> Ssse3_AlignRight_10() => Ssse3.AlignRight(GetV128<UInt64>(), GetV128<UInt64>(), Get<System.Byte>());
    public Vector128<Int16> Ssse3_HorizontalAdd_12() => Ssse3.HorizontalAdd(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<Int32> Ssse3_HorizontalAdd_13() => Ssse3.HorizontalAdd(GetV128<Int32>(), GetV128<Int32>());
    public Vector128<Int16> Ssse3_HorizontalAddSaturate_14() => Ssse3.HorizontalAddSaturate(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<Int16> Ssse3_HorizontalSubtract_15() => Ssse3.HorizontalSubtract(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<Int32> Ssse3_HorizontalSubtract_16() => Ssse3.HorizontalSubtract(GetV128<Int32>(), GetV128<Int32>());
    public Vector128<Int16> Ssse3_HorizontalSubtractSaturate_17() => Ssse3.HorizontalSubtractSaturate(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<Int16> Ssse3_MultiplyAddAdjacent_18() => Ssse3.MultiplyAddAdjacent(GetV128<Byte>(), GetV128<SByte>());
    public Vector128<Int16> Ssse3_MultiplyHighRoundScale_19() => Ssse3.MultiplyHighRoundScale(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<SByte> Ssse3_Shuffle_20() => Ssse3.Shuffle(GetV128<SByte>(), GetV128<SByte>());
    public Vector128<Byte> Ssse3_Shuffle_21() => Ssse3.Shuffle(GetV128<Byte>(), GetV128<Byte>());
    public Vector128<SByte> Ssse3_Sign_22() => Ssse3.Sign(GetV128<SByte>(), GetV128<SByte>());
    public Vector128<Int16> Ssse3_Sign_23() => Ssse3.Sign(GetV128<Int16>(), GetV128<Int16>());
    public Vector128<Int32> Ssse3_Sign_24() => Ssse3.Sign(GetV128<Int32>(), GetV128<Int32>());
    public Vector128<Int16> Sse41_Blend_0() => Sse41.Blend(GetV128<Int16>(), GetV128<Int16>(), Get<System.Byte>());
    public Vector128<UInt16> Sse41_Blend_1() => Sse41.Blend(GetV128<UInt16>(), GetV128<UInt16>(), Get<System.Byte>());
    public Vector128<Single> Sse41_Blend_2() => Sse41.Blend(GetV128<Single>(), GetV128<Single>(), Get<System.Byte>());
    public Vector128<Double> Sse41_Blend_3() => Sse41.Blend(GetV128<Double>(), GetV128<Double>(), Get<System.Byte>());
    public Vector128<SByte> Sse41_BlendVariable_4() => Sse41.BlendVariable(GetV128<SByte>(), GetV128<SByte>(), GetV128<SByte>());
    public Vector128<Byte> Sse41_BlendVariable_5() => Sse41.BlendVariable(GetV128<Byte>(), GetV128<Byte>(), GetV128<Byte>());
    public Vector128<Int16> Sse41_BlendVariable_6() => Sse41.BlendVariable(GetV128<Int16>(), GetV128<Int16>(), GetV128<Int16>());
    public Vector128<UInt16> Sse41_BlendVariable_7() => Sse41.BlendVariable(GetV128<UInt16>(), GetV128<UInt16>(), GetV128<UInt16>());
    public Vector128<Int32> Sse41_BlendVariable_8() => Sse41.BlendVariable(GetV128<Int32>(), GetV128<Int32>(), GetV128<Int32>());
    public Vector128<UInt32> Sse41_BlendVariable_9() => Sse41.BlendVariable(GetV128<UInt32>(), GetV128<UInt32>(), GetV128<UInt32>());
    public Vector128<Int64> Sse41_BlendVariable_10() => Sse41.BlendVariable(GetV128<Int64>(), GetV128<Int64>(), GetV128<Int64>());
    public Vector128<UInt64> Sse41_BlendVariable_11() => Sse41.BlendVariable(GetV128<UInt64>(), GetV128<UInt64>(), GetV128<UInt64>());
    public Vector128<Single> Sse41_BlendVariable_12() => Sse41.BlendVariable(GetV128<Single>(), GetV128<Single>(), GetV128<Single>());
    public Vector128<Double> Sse41_BlendVariable_13() => Sse41.BlendVariable(GetV128<Double>(), GetV128<Double>(), GetV128<Double>());
    public Vector128<Single> Sse41_Ceiling_14() => Sse41.Ceiling(GetV128<Single>());
    public Vector128<Double> Sse41_Ceiling_15() => Sse41.Ceiling(GetV128<Double>());
    public Vector128<Double> Sse41_CeilingScalar_16() => Sse41.CeilingScalar(GetV128<Double>());
    public Vector128<Single> Sse41_CeilingScalar_17() => Sse41.CeilingScalar(GetV128<Single>());
    public Vector128<Double> Sse41_CeilingScalar_18() => Sse41.CeilingScalar(GetV128<Double>(), GetV128<Double>());
    public Vector128<Single> Sse41_CeilingScalar_19() => Sse41.CeilingScalar(GetV128<Single>(), GetV128<Single>());
    public Vector128<Int64> Sse41_CompareEqual_20() => Sse41.CompareEqual(GetV128<Int64>(), GetV128<Int64>());
    public Vector128<UInt64> Sse41_CompareEqual_21() => Sse41.CompareEqual(GetV128<UInt64>(), GetV128<UInt64>());
    public Vector128<Int16> Sse41_ConvertToVector128Int16_22() => Sse41.ConvertToVector128Int16(GetV128<SByte>());
    public Vector128<Int16> Sse41_ConvertToVector128Int16_23() => Sse41.ConvertToVector128Int16(GetV128<Byte>());
    public Vector128<Int16> Sse41_ConvertToVector128Int16_24() => Sse41.ConvertToVector128Int16((sbyte*)pArray1);
    public Vector128<Int16> Sse41_ConvertToVector128Int16_25() => Sse41.ConvertToVector128Int16((byte*)pArray1);
    public Vector128<Int32> Sse41_ConvertToVector128Int32_26() => Sse41.ConvertToVector128Int32(GetV128<SByte>());
    public Vector128<Int32> Sse41_ConvertToVector128Int32_27() => Sse41.ConvertToVector128Int32(GetV128<Byte>());
    public Vector128<Int32> Sse41_ConvertToVector128Int32_28() => Sse41.ConvertToVector128Int32(GetV128<Int16>());
    public Vector128<Int32> Sse41_ConvertToVector128Int32_29() => Sse41.ConvertToVector128Int32(GetV128<UInt16>());
    public Vector128<Int32> Sse41_ConvertToVector128Int32_30() => Sse41.ConvertToVector128Int32((sbyte*)pArray1);
    public Vector128<Int32> Sse41_ConvertToVector128Int32_31() => Sse41.ConvertToVector128Int32((byte*)pArray1);
    public Vector128<Int32> Sse41_ConvertToVector128Int32_32() => Sse41.ConvertToVector128Int32((short*)pArray1);
    public Vector128<Int32> Sse41_ConvertToVector128Int32_33() => Sse41.ConvertToVector128Int32((ushort*)pArray1);
    public Vector128<Int64> Sse41_ConvertToVector128Int64_34() => Sse41.ConvertToVector128Int64(GetV128<SByte>());
    public Vector128<Int64> Sse41_ConvertToVector128Int64_35() => Sse41.ConvertToVector128Int64(GetV128<Byte>());
    public Vector128<Int64> Sse41_ConvertToVector128Int64_36() => Sse41.ConvertToVector128Int64(GetV128<Int16>());
    public Vector128<Int64> Sse41_ConvertToVector128Int64_37() => Sse41.ConvertToVector128Int64(GetV128<UInt16>());
    public Vector128<Int64> Sse41_ConvertToVector128Int64_38() => Sse41.ConvertToVector128Int64(GetV128<Int32>());
    public Vector128<Int64> Sse41_ConvertToVector128Int64_39() => Sse41.ConvertToVector128Int64(GetV128<UInt32>());
    public Vector128<Int64> Sse41_ConvertToVector128Int64_40() => Sse41.ConvertToVector128Int64((sbyte*)pArray1);
    public Vector128<Int64> Sse41_ConvertToVector128Int64_41() => Sse41.ConvertToVector128Int64((byte*)pArray1);
    public Vector128<Int64> Sse41_ConvertToVector128Int64_42() => Sse41.ConvertToVector128Int64((short*)pArray1);
    public Vector128<Int64> Sse41_ConvertToVector128Int64_43() => Sse41.ConvertToVector128Int64((ushort*)pArray1);
    public Vector128<Int64> Sse41_ConvertToVector128Int64_44() => Sse41.ConvertToVector128Int64((int*)pArray1);
    public Vector128<Int64> Sse41_ConvertToVector128Int64_45() => Sse41.ConvertToVector128Int64((uint*)pArray1);
    public Vector128<Single> Sse41_DotProduct_46() => Sse41.DotProduct(GetV128<Single>(), GetV128<Single>(), Get<System.Byte>());
    public Vector128<Double> Sse41_DotProduct_47() => Sse41.DotProduct(GetV128<Double>(), GetV128<Double>(), Get<System.Byte>());
    public Byte Sse41_Extract_48() => Sse41.Extract(GetV128<Byte>(), Get<System.Byte>());
    public Int32 Sse41_Extract_49() => Sse41.Extract(GetV128<Int32>(), Get<System.Byte>());
    public UInt32 Sse41_Extract_50() => Sse41.Extract(GetV128<UInt32>(), Get<System.Byte>());
    public Single Sse41_Extract_51() => Sse41.Extract(GetV128<Single>(), Get<System.Byte>());
    public Vector128<Single> Sse41_Floor_52() => Sse41.Floor(GetV128<Single>());
    public Vector128<Double> Sse41_Floor_53() => Sse41.Floor(GetV128<Double>());
    public Vector128<Double> Sse41_FloorScalar_54() => Sse41.FloorScalar(GetV128<Double>());
    public Vector128<Single> Sse41_FloorScalar_55() => Sse41.FloorScalar(GetV128<Single>());
    public Vector128<Double> Sse41_FloorScalar_56() => Sse41.FloorScalar(GetV128<Double>(), GetV128<Double>());
    public Vector128<Single> Sse41_FloorScalar_57() => Sse41.FloorScalar(GetV128<Single>(), GetV128<Single>());
    public Vector128<SByte> Sse41_Insert_59() => Sse41.Insert(GetV128<SByte>(), Get<System.SByte>(), 4);
    public Vector128<Byte> Sse41_Insert_60() => Sse41.Insert(GetV128<Byte>(), Get<System.Byte>(), 3);
    public Vector128<Int32> Sse41_Insert_61() => Sse41.Insert(GetV128<Int32>(), Get<System.Int32>(), 2);
    public Vector128<UInt32> Sse41_Insert_62() => Sse41.Insert(GetV128<UInt32>(), Get<System.UInt32>(), 1);
    public Vector128<Single> Sse41_Insert_63() => Sse41.Insert(GetV128<Single>(), GetV128<Single>(), 0x10);
    public Vector128<SByte> Sse41_LoadAlignedVector128NonTemporal_64() => Sse41.LoadAlignedVector128NonTemporal(((sbyte*)pArray1));
    public Vector128<Byte> Sse41_LoadAlignedVector128NonTemporal_65() => Sse41.LoadAlignedVector128NonTemporal(((byte*)pArray1));
    public Vector128<Int16> Sse41_LoadAlignedVector128NonTemporal_66() => Sse41.LoadAlignedVector128NonTemporal(((short*)pArray1));
    public Vector128<UInt16> Sse41_LoadAlignedVector128NonTemporal_67() => Sse41.LoadAlignedVector128NonTemporal(((ushort*)pArray1));
    public Vector128<Int32> Sse41_LoadAlignedVector128NonTemporal_68() => Sse41.LoadAlignedVector128NonTemporal(((int*)pArray1));
    public Vector128<UInt32> Sse41_LoadAlignedVector128NonTemporal_69() => Sse41.LoadAlignedVector128NonTemporal(((uint*)pArray1));
    public Vector128<Int64> Sse41_LoadAlignedVector128NonTemporal_70() => Sse41.LoadAlignedVector128NonTemporal(((long*)pArray1));
    public Vector128<UInt64> Sse41_LoadAlignedVector128NonTemporal_71() => Sse41.LoadAlignedVector128NonTemporal(((ulong*)pArray1));
    public Vector128<SByte> Sse41_Max_72() => Sse41.Max(GetV128<SByte>(), GetV128<SByte>());
    public Vector128<UInt16> Sse41_Max_73() => Sse41.Max(GetV128<UInt16>(), GetV128<UInt16>());
    public Vector128<Int32> Sse41_Max_74() => Sse41.Max(GetV128<Int32>(), GetV128<Int32>());
    public Vector128<UInt32> Sse41_Max_75() => Sse41.Max(GetV128<UInt32>(), GetV128<UInt32>());
    public Vector128<SByte> Sse41_Min_76() => Sse41.Min(GetV128<SByte>(), GetV128<SByte>());
    public Vector128<UInt16> Sse41_Min_77() => Sse41.Min(GetV128<UInt16>(), GetV128<UInt16>());
    public Vector128<Int32> Sse41_Min_78() => Sse41.Min(GetV128<Int32>(), GetV128<Int32>());
    public Vector128<UInt32> Sse41_Min_79() => Sse41.Min(GetV128<UInt32>(), GetV128<UInt32>());
    public Vector128<UInt16> Sse41_MinHorizontal_80() => Sse41.MinHorizontal(GetV128<UInt16>());
    public Vector128<UInt16> Sse41_MultipleSumAbsoluteDifferences_81() => Sse41.MultipleSumAbsoluteDifferences(GetV128<Byte>(), GetV128<Byte>(), Get<System.Byte>());
    public Vector128<Int64> Sse41_Multiply_82() => Sse41.Multiply(GetV128<Int32>(), GetV128<Int32>());
    public Vector128<Int32> Sse41_MultiplyLow_83() => Sse41.MultiplyLow(GetV128<Int32>(), GetV128<Int32>());
    public Vector128<UInt32> Sse41_MultiplyLow_84() => Sse41.MultiplyLow(GetV128<UInt32>(), GetV128<UInt32>());
    public Vector128<UInt16> Sse41_PackUnsignedSaturate_85() => Sse41.PackUnsignedSaturate(GetV128<Int32>(), GetV128<Int32>());
    public Vector128<Double> Sse41_RoundCurrentDirection_86() => Sse41.RoundCurrentDirection(GetV128<Double>());
    public Vector128<Single> Sse41_RoundCurrentDirection_87() => Sse41.RoundCurrentDirection(GetV128<Single>());
    public Vector128<Double> Sse41_RoundCurrentDirectionScalar_88() => Sse41.RoundCurrentDirectionScalar(GetV128<Double>());
    public Vector128<Double> Sse41_RoundCurrentDirectionScalar_89() => Sse41.RoundCurrentDirectionScalar(GetV128<Double>(), GetV128<Double>());
    public Vector128<Single> Sse41_RoundCurrentDirectionScalar_90() => Sse41.RoundCurrentDirectionScalar(GetV128<Single>());
    public Vector128<Single> Sse41_RoundCurrentDirectionScalar_91() => Sse41.RoundCurrentDirectionScalar(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse41_RoundToNearestInteger_92() => Sse41.RoundToNearestInteger(GetV128<Single>());
    public Vector128<Double> Sse41_RoundToNearestInteger_93() => Sse41.RoundToNearestInteger(GetV128<Double>());
    public Vector128<Double> Sse41_RoundToNearestIntegerScalar_94() => Sse41.RoundToNearestIntegerScalar(GetV128<Double>());
    public Vector128<Double> Sse41_RoundToNearestIntegerScalar_95() => Sse41.RoundToNearestIntegerScalar(GetV128<Double>(), GetV128<Double>());
    public Vector128<Single> Sse41_RoundToNearestIntegerScalar_96() => Sse41.RoundToNearestIntegerScalar(GetV128<Single>());
    public Vector128<Single> Sse41_RoundToNearestIntegerScalar_97() => Sse41.RoundToNearestIntegerScalar(GetV128<Single>(), GetV128<Single>());
    public Vector128<Single> Sse41_RoundToNegativeInfinity_98() => Sse41.RoundToNegativeInfinity(GetV128<Single>());
    public Vector128<Double> Sse41_RoundToNegativeInfinity_99() => Sse41.RoundToNegativeInfinity(GetV128<Double>());
    public Vector128<Double> Sse41_RoundToNegativeInfinityScalar_100() => Sse41.RoundToNegativeInfinityScalar(GetV128<Double>());
    public Vector128<Double> Sse41_RoundToNegativeInfinityScalar_101() => Sse41.RoundToNegativeInfinityScalar(GetV128<Double>(), GetV128<Double>());
    public Vector128<Single> Sse41_RoundToNegativeInfinityScalar_102() => Sse41.RoundToNegativeInfinityScalar(GetV128<Single>());
    public Vector128<Single> Sse41_RoundToNegativeInfinityScalar_103() => Sse41.RoundToNegativeInfinityScalar(GetV128<Single>(), GetV128<Single>());
    public Vector128<Double> Sse41_RoundToPositiveInfinity_104() => Sse41.RoundToPositiveInfinity(GetV128<Double>());
    public Vector128<Single> Sse41_RoundToPositiveInfinity_105() => Sse41.RoundToPositiveInfinity(GetV128<Single>());
    public Vector128<Double> Sse41_RoundToPositiveInfinityScalar_106() => Sse41.RoundToPositiveInfinityScalar(GetV128<Double>());
    public Vector128<Double> Sse41_RoundToPositiveInfinityScalar_107() => Sse41.RoundToPositiveInfinityScalar(GetV128<Double>(), GetV128<Double>());
    public Vector128<Single> Sse41_RoundToPositiveInfinityScalar_108() => Sse41.RoundToPositiveInfinityScalar(GetV128<Single>());
    public Vector128<Single> Sse41_RoundToPositiveInfinityScalar_109() => Sse41.RoundToPositiveInfinityScalar(GetV128<Single>(), GetV128<Single>());
    public Vector128<Double> Sse41_RoundToZero_110() => Sse41.RoundToZero(GetV128<Double>());
    public Vector128<Single> Sse41_RoundToZero_111() => Sse41.RoundToZero(GetV128<Single>());
    public Vector128<Double> Sse41_RoundToZeroScalar_112() => Sse41.RoundToZeroScalar(GetV128<Double>());
    public Vector128<Double> Sse41_RoundToZeroScalar_113() => Sse41.RoundToZeroScalar(GetV128<Double>(), GetV128<Double>());
    public Vector128<Single> Sse41_RoundToZeroScalar_114() => Sse41.RoundToZeroScalar(GetV128<Single>());
    public Vector128<Single> Sse41_RoundToZeroScalar_115() => Sse41.RoundToZeroScalar(GetV128<Single>(), GetV128<Single>());
    public Boolean Sse41_TestC_116() => Sse41.TestC(GetV128<SByte>(), GetV128<SByte>());
    public Boolean Sse41_TestC_117() => Sse41.TestC(GetV128<Byte>(), GetV128<Byte>());
    public Boolean Sse41_TestC_118() => Sse41.TestC(GetV128<Int16>(), GetV128<Int16>());
    public Boolean Sse41_TestC_119() => Sse41.TestC(GetV128<UInt16>(), GetV128<UInt16>());
    public Boolean Sse41_TestC_120() => Sse41.TestC(GetV128<Int32>(), GetV128<Int32>());
    public Boolean Sse41_TestC_121() => Sse41.TestC(GetV128<UInt32>(), GetV128<UInt32>());
    public Boolean Sse41_TestC_122() => Sse41.TestC(GetV128<Int64>(), GetV128<Int64>());
    public Boolean Sse41_TestC_123() => Sse41.TestC(GetV128<UInt64>(), GetV128<UInt64>());
    public Boolean Sse41_TestNotZAndNotC_124() => Sse41.TestNotZAndNotC(GetV128<SByte>(), GetV128<SByte>());
    public Boolean Sse41_TestNotZAndNotC_125() => Sse41.TestNotZAndNotC(GetV128<Byte>(), GetV128<Byte>());
    public Boolean Sse41_TestNotZAndNotC_126() => Sse41.TestNotZAndNotC(GetV128<Int16>(), GetV128<Int16>());
    public Boolean Sse41_TestNotZAndNotC_127() => Sse41.TestNotZAndNotC(GetV128<UInt16>(), GetV128<UInt16>());
    public Boolean Sse41_TestNotZAndNotC_128() => Sse41.TestNotZAndNotC(GetV128<Int32>(), GetV128<Int32>());
    public Boolean Sse41_TestNotZAndNotC_129() => Sse41.TestNotZAndNotC(GetV128<UInt32>(), GetV128<UInt32>());
    public Boolean Sse41_TestNotZAndNotC_130() => Sse41.TestNotZAndNotC(GetV128<Int64>(), GetV128<Int64>());
    public Boolean Sse41_TestNotZAndNotC_131() => Sse41.TestNotZAndNotC(GetV128<UInt64>(), GetV128<UInt64>());
    public Boolean Sse41_TestZ_132() => Sse41.TestZ(GetV128<SByte>(), GetV128<SByte>());
    public Boolean Sse41_TestZ_133() => Sse41.TestZ(GetV128<Byte>(), GetV128<Byte>());
    public Boolean Sse41_TestZ_134() => Sse41.TestZ(GetV128<Int16>(), GetV128<Int16>());
    public Boolean Sse41_TestZ_135() => Sse41.TestZ(GetV128<UInt16>(), GetV128<UInt16>());
    public Boolean Sse41_TestZ_136() => Sse41.TestZ(GetV128<Int32>(), GetV128<Int32>());
    public Boolean Sse41_TestZ_137() => Sse41.TestZ(GetV128<UInt32>(), GetV128<UInt32>());
    public Boolean Sse41_TestZ_138() => Sse41.TestZ(GetV128<Int64>(), GetV128<Int64>());
    public Boolean Sse41_TestZ_139() => Sse41.TestZ(GetV128<UInt64>(), GetV128<UInt64>());
    public Vector128<Int64> Sse42_CompareGreaterThan_0() => Sse42.CompareGreaterThan(GetV128<Int64>(), GetV128<Int64>());
    public UInt32 Sse42_Crc32_1() => Sse42.Crc32(Get<System.UInt32>(), Get<System.Byte>());
    public UInt32 Sse42_Crc32_2() => Sse42.Crc32(Get<System.UInt32>(), Get<System.UInt16>());
    public UInt32 Sse42_Crc32_3() => Sse42.Crc32(Get<System.UInt32>(), Get<System.UInt32>());

    public Vector128<byte> Vector128_Create_1() => Vector128.Create(Get<byte>());
    public Vector128<sbyte> Vector128_Create_2() => Vector128.Create(Get<sbyte>());
    public Vector128<short> Vector128_Create_3() => Vector128.Create(Get<short>());
    public Vector128<ushort> Vector128_Create_4() => Vector128.Create(Get<ushort>());
    public Vector128<int> Vector128_Create_5() => Vector128.Create(Get<int>());
    public Vector128<uint> Vector128_Create_6() => Vector128.Create(Get<uint>());
    public Vector128<long> Vector128_Create_7() => Vector128.Create(Get<long>());
    public Vector128<ulong> Vector128_Create_8() => Vector128.Create(Get<ulong>());
    public Vector128<float> Vector128_Create_9() => Vector128.Create(Get<float>());
    public Vector128<double> Vector128_Create_10() => Vector128.Create(Get<double>());
    public Vector128<double> Vector128_Create_11() => Vector128.Create(Get<double>(), Get<double>());
    public Vector128<long> Vector128_Create_12() => Vector128.Create(Get<long>(), Get<long>());
    public Vector128<ulong> Vector128_Create_13() => Vector128.Create(Get<ulong>(), Get<ulong>());
    public Vector128<float> Vector128_Create_14() => Vector128.Create(Get<float>(), Get<float>(), Get<float>(), Get<float>());
    public Vector128<int> Vector128_Create_15() => Vector128.Create(Get<int>(), Get<int>(), Get<int>(), Get<int>());
    public Vector128<uint> Vector128_Create_16() => Vector128.Create(Get<uint>(), Get<uint>(), Get<uint>(), Get<uint>());
    public Vector128<ushort> Vector128_Create_17() => Vector128.Create(Get<ushort>(), Get<ushort>(), Get<ushort>(), Get<ushort>(), Get<ushort>(), Get<ushort>(), Get<ushort>(), Get<ushort>());
    public Vector128<short> Vector128_Create_18() => Vector128.Create(Get<short>(), Get<short>(), Get<short>(), Get<short>(), Get<short>(), Get<short>(), Get<short>(), Get<short>());
    public Vector128<sbyte> Vector128_Create_20() => Vector128.Create(Get<sbyte>(), Get<sbyte>(), Get<sbyte>(), Get<sbyte>(), Get<sbyte>(), Get<sbyte>(), Get<sbyte>(), Get<sbyte>(), Get<sbyte>(), Get<sbyte>(), Get<sbyte>(), Get<sbyte>(), Get<sbyte>(), Get<sbyte>(), Get<sbyte>(), Get<sbyte>());
    
    public int Vector128_CreateScalarUnsafe_1() => Sse2.Add(Vector128.CreateScalarUnsafe(42), Vector128.CreateScalarUnsafe(8)).GetElement(0);
    public double Vector128_CreateScalarUnsafe_2() => Sse2.Add(Vector128.CreateScalarUnsafe(42.0), Vector128.CreateScalarUnsafe(8.0)).GetElement(0);

    public Vector128<byte> Vector128_int_to_byte() => Vector128_Create_15().AsByte();
    public Vector128<ulong> Vector128_int_to_ulong() => Vector128_Create_15().AsUInt64();
    public Vector128<int> Vector128_byte_to_int() => Vector128_Create_20().AsInt32();
}