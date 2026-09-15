// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

public unsafe class LikelyClassTests
{
    private const int ILOffset = 42;
    private const string JitLibrary = "clrjit";

    public static int Main()
    {
        foreach (bool types in new[] { true, false })
        {
            TruncatedHistogramReportsActualLikelihoods(types);
            SingleRecordReportsActualLikelihood(types);
            CompleteHistogramNormalizesRoundingError(types);
            UnknownHandlesKeepResidualLikelihood(types);
        }

        return 100;
    }

    private static void TruncatedHistogramReportsActualLikelihoods(bool types)
    {
        nint[] histogram = new nint[10];
        for (int i = 0; i < histogram.Length; i++)
        {
            histogram[i] = 100 + i;
        }

        LikelyClassMethodRecord[] records = new LikelyClassMethodRecord[5];

        AssertEqual((uint)records.Length, GetLikelyRecords(types, histogram, records));
        for (int i = 0; i < records.Length; i++)
        {
            AssertEqual(10u, records[i].Likelihood);
        }
    }

    private static void SingleRecordReportsActualLikelihood(bool types)
    {
        nint[] histogram = [100, 100, 200, 200, 300, 300, 400, 400];
        LikelyClassMethodRecord[] records = new LikelyClassMethodRecord[1];

        AssertEqual(1u, GetLikelyRecords(types, histogram, records));
        AssertEqual(25u, records[0].Likelihood);
    }

    private static void CompleteHistogramNormalizesRoundingError(bool types)
    {
        nint[] histogram = [100, 100, 100, 200, 200, 300];
        LikelyClassMethodRecord[] records = new LikelyClassMethodRecord[3];

        AssertEqual((uint)records.Length, GetLikelyRecords(types, histogram, records));
        AssertEqual((nint)100, records[0].Handle);
        AssertEqual((nint)200, records[1].Handle);
        AssertEqual((nint)300, records[2].Handle);
        AssertEqual(51u, records[0].Likelihood);
        AssertEqual(33u, records[1].Likelihood);
        AssertEqual(16u, records[2].Likelihood);
    }

    private static void UnknownHandlesKeepResidualLikelihood(bool types)
    {
        nint[] histogram = [100, 100, 100, 200, 200, 1];
        LikelyClassMethodRecord[] records = new LikelyClassMethodRecord[2];

        AssertEqual((uint)records.Length, GetLikelyRecords(types, histogram, records));
        AssertEqual((nint)100, records[0].Handle);
        AssertEqual((nint)200, records[1].Handle);
        AssertEqual(50u, records[0].Likelihood);
        AssertEqual(33u, records[1].Likelihood);
    }

    private static void AssertEqual(uint expected, uint actual)
    {
        if (actual != expected)
        {
            throw new InvalidOperationException($"Expected {expected}, actual {actual}.");
        }
    }

    private static void AssertEqual(nint expected, nint actual)
    {
        if (actual != expected)
        {
            throw new InvalidOperationException($"Expected {expected}, actual {actual}.");
        }
    }

    private static uint GetLikelyRecords(
        bool types,
        nint[] histogram,
        LikelyClassMethodRecord[] records)
    {
        PgoInstrumentationKind histogramKind = types
            ? PgoInstrumentationKind.HandleHistogramTypes
            : PgoInstrumentationKind.HandleHistogramMethods;
        PgoInstrumentationSchema[] schema =
        [
            new()
            {
                InstrumentationKind = PgoInstrumentationKind.HandleHistogramIntCount,
                ILOffset = ILOffset,
                Count = 1,
            },
            new()
            {
                InstrumentationKind = histogramKind,
                ILOffset = ILOffset,
                Count = histogram.Length,
            },
        ];

        fixed (nint* pHistogram = histogram)
        fixed (LikelyClassMethodRecord* pRecords = records)
        fixed (PgoInstrumentationSchema* pSchema = schema)
        {
            if (types)
            {
                return GetLikelyClasses(
                    pRecords,
                    (uint)records.Length,
                    pSchema,
                    (uint)schema.Length,
                    (byte*)pHistogram,
                    ILOffset);
            }

            return GetLikelyMethods(
                pRecords,
                (uint)records.Length,
                pSchema,
                (uint)schema.Length,
                (byte*)pHistogram,
                ILOffset);
        }
    }

    [DllImport(JitLibrary, EntryPoint = "getLikelyClasses")]
    private static extern uint GetLikelyClasses(
        LikelyClassMethodRecord* records,
        uint maxRecords,
        PgoInstrumentationSchema* schema,
        uint schemaCount,
        byte* instrumentationData,
        int ilOffset);

    [DllImport(JitLibrary, EntryPoint = "getLikelyMethods")]
    private static extern uint GetLikelyMethods(
        LikelyClassMethodRecord* records,
        uint maxRecords,
        PgoInstrumentationSchema* schema,
        uint schemaCount,
        byte* instrumentationData,
        int ilOffset);

    private enum PgoInstrumentationKind
    {
        FourByte = 1,
        TypeHandle = 3,
        MethodHandle = 4,
        AlignPointer = 0x30,
        DescriptorMin = 0x40,
        HandleHistogramIntCount = (DescriptorMin * 2) | FourByte | AlignPointer,
        HandleHistogramTypes = (DescriptorMin * 3) | TypeHandle,
        HandleHistogramMethods = (DescriptorMin * 3) | MethodHandle,
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PgoInstrumentationSchema
    {
        public nuint Offset;
        public PgoInstrumentationKind InstrumentationKind;
        public int ILOffset;
        public int Count;
        public int Other;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LikelyClassMethodRecord
    {
        public nint Handle;
        public uint Likelihood;
    }
}
