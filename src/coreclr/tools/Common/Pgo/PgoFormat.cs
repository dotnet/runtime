// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using ILCompiler;

namespace Internal.Pgo
{
    public enum PgoInstrumentationKind
    {
        // This must be kept in sync with PgoInstrumentationKind in corjit.h
        // New InstrumentationKinds should recieve corresponding merging logic in
        // PgoSchemeMergeComparer and the MergeInSchemaElem functions below

        // Schema data types
        None = 0,
        FourByte = 1,
        EightByte = 2,
        TypeHandle = 3,

        // Mask of all schema data types
        MarshalMask = 0xF,

        // ExcessAlignment
        Align4Byte = 0x10,
        Align8Byte = 0x20,
        AlignPointer = 0x30,

        // Mask of all schema data types
        AlignMask = 0x30,

        DescriptorMin = 0x40,

        Done = None, // All instrumentation schemas must end with a record which is "Done"
        BasicBlockIntCount = (DescriptorMin * 1) | FourByte, // basic block counter using unsigned 4 byte int
        BasicBlockLongCount = (DescriptorMin * 1) | EightByte, // basic block counter using unsigned 8 byte int
        TypeHandleHistogramCount = (DescriptorMin * 2) | FourByte | AlignPointer, // 4 byte counter that is part of a type histogram
        TypeHandleHistogramTypeHandle = (DescriptorMin * 3) | TypeHandle, // TypeHandle that is part of a type histogram
        Version = (DescriptorMin * 4) | None, // Version is encoded in the Other field of the schema
        NumRuns = (DescriptorMin * 5) | None, // Number of runs is encoded in the Other field of the schema
        EdgeIntCount = (DescriptorMin * 6) | FourByte, // edge counter using unsigned 4 byte int
        EdgeLongCount = (DescriptorMin * 6) | EightByte, // edge counter using unsigned 8 byte int
        GetLikelyClass = (DescriptorMin * 7) | TypeHandle, // Compressed get likely class data
    }

    public interface IPgoSchemaDataLoader<TType>
    {
        TType TypeFromLong(long input);
    }

    public interface IPgoEncodedValueEmitter<TType>
    {
        void EmitType(TType type, TType previousValue);
        void EmitLong(long value, long previousValue);
        bool EmitDone();
    }

    public struct PgoSchemaElem
    {
        public PgoInstrumentationKind InstrumentationKind;
        public int ILOffset;
        public int Count;
        public int Other;

        // Data is normally stored as a long
        // but for Types/Method/Fields/conditions where Count > 1
        // the DataObject field is used instead
        public long DataLong;
        public Array DataObject;

        public bool DataHeldInDataLong => (Count == 1 &&
                            (((InstrumentationKind & PgoInstrumentationKind.MarshalMask) == PgoInstrumentationKind.FourByte) ||
                            ((InstrumentationKind & PgoInstrumentationKind.MarshalMask) == PgoInstrumentationKind.EightByte)));
    }

    public class PgoProcessor
    {
        private enum InstrumentationDataProcessingState
        {
            Done = 0,
            ILOffset = 0x1,
            Type = 0x2,
            Count = 0x4,
            Other = 0x8,
            UpdateProcessMask = 0xF,
            UpdateProcessMaskFlag = 0x100,
        }

        private const long SIGN_MASK_ONEBYTE_64BIT = unchecked((long)0xffffffffffffffc0L);
        private const long SIGN_MASK_TWOBYTE_64BIT = unchecked((long)0xffffffffffffe000L);
        private const long SIGN_MASK_FOURBYTE_64BIT = unchecked((long)0xffffffff80000000L);

        public class PgoEncodedCompressedIntParser : IEnumerable<long>, IEnumerator<long>
        {
            long _current;
            byte[] _bytes;

            public PgoEncodedCompressedIntParser(byte[] bytes, int startOffset)
            {
                _bytes = bytes;
                Offset = startOffset;
            }

            public int Offset { get; private set; }

            public long Current => _current;

            object IEnumerator.Current => throw new NotImplementedException();

            public PgoEncodedCompressedIntParser GetEnumerator() => this;

            public bool MoveNext()
            {
                byte[] bytes = _bytes;
                int offset = Offset;
                if (offset >= _bytes.Length)
                    return false;

                // This logic is a variant on CorSigUncompressSignedInt which allows for the full range of an int64_t
                long signedInt;
                if ((bytes[offset] & 0x80) == 0x0) // 0??? ????
                {
                    int shiftedInt = bytes[offset];
                    signedInt = shiftedInt >> 1;
                    if ((shiftedInt & 1) != 0)
                    {
                        signedInt |= SIGN_MASK_ONEBYTE_64BIT;
                    }
                    offset += 1;
                }
                else if ((bytes[offset] & 0xC0) == 0x80) // 10?? ????
                {
                    int shiftedInt = (bytes[offset] & 0x3f) << 8 | bytes[offset + 1];
                    signedInt = shiftedInt >> 1;
                    if ((shiftedInt & 1) != 0)
                        signedInt |= SIGN_MASK_TWOBYTE_64BIT;

                    offset += 2;
                }
                else if ((bytes[offset]) == 0xC1) // 8 byte specifier
                {
                    signedInt = (((long)bytes[offset + 1]) << 56) |
                                (((long)bytes[offset + 2]) << 48) |
                                (((long)bytes[offset + 3]) << 40) |
                                (((long)bytes[offset + 4]) << 32) |
                                (((long)bytes[offset + 5]) << 24) |
                                (((long)bytes[offset + 6]) << 16) |
                                (((long)bytes[offset + 7]) << 8) |
                                ((long)bytes[offset + 8]);
                    offset += 9;
                }
                else
                {
                    signedInt = (((int)bytes[offset + 1]) << 24) |
                                (((int)bytes[offset + 2]) << 16) |
                                (((int)bytes[offset + 3]) << 8) |
                                ((int)bytes[offset + 4]);
                    offset += 5;
                }

                Offset = offset;
                _current = signedInt;
                return true;
            }

            void IDisposable.Dispose() { }
            IEnumerator<long> IEnumerable<long>.GetEnumerator() => this;
            IEnumerator IEnumerable.GetEnumerator() => this;
            void IEnumerator.Reset() => throw new NotImplementedException();
        }

        public static IEnumerable<byte> PgoEncodedCompressedLongGenerator(IEnumerable<long> input)
        {
            foreach (long value in input)
            {
                byte isSigned = 0;
                // This function is modeled on CorSigCompressSignedInt, but differs in that
                // it handles arbitrary int64 values, not just a subset
                if (value < 0)
                    isSigned = 1;

                if ((value & SIGN_MASK_ONEBYTE_64BIT) == 0 || (value & SIGN_MASK_ONEBYTE_64BIT) == SIGN_MASK_ONEBYTE_64BIT)
                {
                    yield return (byte)((byte)((value & ~SIGN_MASK_ONEBYTE_64BIT) << 1 | isSigned));
                }
                else if ((value & SIGN_MASK_TWOBYTE_64BIT) == 0 || (value & SIGN_MASK_TWOBYTE_64BIT) == SIGN_MASK_TWOBYTE_64BIT)
                {
                    int iData = (int)((value & ~SIGN_MASK_TWOBYTE_64BIT) << 1 | isSigned);
                    yield return (byte)((iData >> 8) | 0x80);
                    yield return (byte)(iData & 0xff);
                }
                else if ((value & SIGN_MASK_FOURBYTE_64BIT) == 0 || (value & SIGN_MASK_FOURBYTE_64BIT) == SIGN_MASK_FOURBYTE_64BIT)
                {
                    // Unlike CorSigCompressSignedInt, this just writes a header byte
                    // then 4 bytes, ignoring the whole signed bit detail
                    yield return 0xC0;
                    yield return (byte)((value >> 24) & 0xff);
                    yield return (byte)((value >> 16) & 0xff);
                    yield return (byte)((value >> 8) & 0xff);
                    yield return (byte)((value >> 0) & 0xff);
                }
                else
                {
                    // Unlike CorSigCompressSignedInt, this just writes a header byte
                    // then 8 bytes, ignoring the whole signed bit detail
                    yield return 0xC1;
                    yield return (byte)((value >> 56) & 0xff);
                    yield return (byte)((value >> 48) & 0xff);
                    yield return (byte)((value >> 40) & 0xff);
                    yield return (byte)((value >> 32) & 0xff);
                    yield return (byte)((value >> 24) & 0xff);
                    yield return (byte)((value >> 16) & 0xff);
                    yield return (byte)((value >> 8) & 0xff);
                    yield return (byte)((value >> 0) & 0xff);
                }
            }
        }

        public static IEnumerable<PgoSchemaElem> ParsePgoData<TType>(IPgoSchemaDataLoader<TType> dataProvider, IEnumerable<long> inputDataStream, bool longsAreCompressed)
        {
            int dataCountToRead = 0;
            PgoSchemaElem curSchema = default(PgoSchemaElem);
            InstrumentationDataProcessingState processingState = InstrumentationDataProcessingState.UpdateProcessMaskFlag;
            long lastDataValue = 0;
            long lastTypeValue = 0;

            foreach (long value in inputDataStream)
            {
                if (dataCountToRead > 0)
                {
                    if (curSchema.DataHeldInDataLong)
                    {
                        if (longsAreCompressed)
                            lastDataValue += value;
                        else
                            lastDataValue = value;
                        curSchema.DataLong = lastDataValue;
                    }
                    else
                    {
                        int dataIndex = curSchema.Count - dataCountToRead;
                        switch (curSchema.InstrumentationKind & PgoInstrumentationKind.MarshalMask)
                        {
                            case PgoInstrumentationKind.FourByte:
                                if (longsAreCompressed)
                                    lastDataValue += value;
                                else
                                    lastDataValue = value;
                                ((int[])curSchema.DataObject)[dataIndex] = checked((int)lastDataValue);
                                break;

                            case PgoInstrumentationKind.EightByte:
                                if (longsAreCompressed)
                                    lastDataValue += value;
                                else
                                    lastDataValue = value;
                                ((long[])curSchema.DataObject)[dataIndex] = lastDataValue;
                                break;

                            case PgoInstrumentationKind.TypeHandle:
                                if (longsAreCompressed)
                                    lastTypeValue += value;
                                else
                                    lastTypeValue = value;
                                ((TType[])curSchema.DataObject)[dataIndex] = dataProvider.TypeFromLong(lastTypeValue);
                                break;
                        }
                    }
                    dataCountToRead--;
                    if (dataCountToRead == 0)
                    {
                        yield return curSchema;
                        curSchema.DataLong = 0;
                        curSchema.DataObject = null;
                    }
                    continue;
                }

                if (processingState == InstrumentationDataProcessingState.UpdateProcessMaskFlag)
                {
                    processingState = (InstrumentationDataProcessingState)value;
                    continue;
                }

                if ((processingState & InstrumentationDataProcessingState.ILOffset) == InstrumentationDataProcessingState.ILOffset)
                {
                    if (longsAreCompressed)
                        curSchema.ILOffset = checked((int)(value + (long)curSchema.ILOffset));
                    else
                        curSchema.ILOffset = checked((int)value);

                    processingState = processingState & ~InstrumentationDataProcessingState.ILOffset;
                }
                else if ((processingState & InstrumentationDataProcessingState.Type) == InstrumentationDataProcessingState.Type)
                {
                    if (longsAreCompressed)
                        curSchema.InstrumentationKind = (PgoInstrumentationKind)(((int)(curSchema.InstrumentationKind)) + checked((int)value));
                    else
                        curSchema.InstrumentationKind = (PgoInstrumentationKind)value;

                    processingState = processingState & ~InstrumentationDataProcessingState.Type;
                }
                else if ((processingState & InstrumentationDataProcessingState.Count) == InstrumentationDataProcessingState.Count)
                {
                    if (longsAreCompressed)
                        curSchema.Count = checked((int)(value + (long)curSchema.Count));
                    else
                        curSchema.Count = checked((int)value);
                    processingState = processingState & ~InstrumentationDataProcessingState.Count;
                }
                else if ((processingState & InstrumentationDataProcessingState.Other) == InstrumentationDataProcessingState.Other)
                {
                    if (longsAreCompressed)
                        curSchema.Other = checked((int)(value + (long)curSchema.Other));
                    else
                        curSchema.Other = checked((int)value);
                    processingState = processingState & ~InstrumentationDataProcessingState.Other;
                }

                if (processingState == InstrumentationDataProcessingState.Done)
                {
                    processingState = InstrumentationDataProcessingState.UpdateProcessMaskFlag;

                    if (curSchema.InstrumentationKind == PgoInstrumentationKind.Done)
                    {
                        yield break;
                    }

                    switch (curSchema.InstrumentationKind & PgoInstrumentationKind.MarshalMask)
                    {
                        case PgoInstrumentationKind.None:
                            yield return curSchema;
                            break;

                        case PgoInstrumentationKind.FourByte:
                            if (curSchema.Count > 1)
                            {
                                curSchema.DataObject = new int[curSchema.Count];
                            }
                            dataCountToRead = curSchema.Count;
                            break;
                        case PgoInstrumentationKind.EightByte:
                            if (curSchema.Count > 1)
                            {
                                curSchema.DataObject = new long[curSchema.Count];
                            }
                            dataCountToRead = curSchema.Count;
                            break;
                        case PgoInstrumentationKind.TypeHandle:
                            curSchema.DataObject = new TType[curSchema.Count];
                            dataCountToRead = curSchema.Count;
                            break;
                        default:
                            throw new Exception("Unknown Type");
                    }
                }
            }

            throw new Exception("Partial Instrumentation Data");
        }

        public static void EncodePgoData<TType>(IEnumerable<PgoSchemaElem> schemas, IPgoEncodedValueEmitter<TType> valueEmitter, bool emitAllElementsUnconditionally)
        {
            PgoSchemaElem prevSchema = default(PgoSchemaElem);
            TType prevEmittedType = default(TType);
            long prevEmittedIntData = 0;

            foreach (PgoSchemaElem schema in schemas)
            {
                int ilOffsetDiff = schema.ILOffset - prevSchema.ILOffset;
                int OtherDiff = schema.Other - prevSchema.Other;
                int CountDiff = schema.Count - prevSchema.Count;
                int TypeDiff = (int)schema.InstrumentationKind - (int)prevSchema.InstrumentationKind;

                InstrumentationDataProcessingState modifyMask = (InstrumentationDataProcessingState)0;

                if (!emitAllElementsUnconditionally)
                {
                    if (ilOffsetDiff != 0)
                        modifyMask = modifyMask | InstrumentationDataProcessingState.ILOffset;
                    if (TypeDiff != 0)
                        modifyMask = modifyMask | InstrumentationDataProcessingState.Type;
                    if (CountDiff != 0)
                        modifyMask = modifyMask | InstrumentationDataProcessingState.Count;
                    if (OtherDiff != 0)
                        modifyMask = modifyMask | InstrumentationDataProcessingState.Other;
                }
                else
                {
                    modifyMask = InstrumentationDataProcessingState.ILOffset |
                                 InstrumentationDataProcessingState.Type |
                                 InstrumentationDataProcessingState.Count |
                                 InstrumentationDataProcessingState.Other;
                }

                Debug.Assert(modifyMask != InstrumentationDataProcessingState.Done);

                valueEmitter.EmitLong((long)modifyMask, 0);

                if ((modifyMask & InstrumentationDataProcessingState.ILOffset) == InstrumentationDataProcessingState.ILOffset)
                    valueEmitter.EmitLong(schema.ILOffset, prevSchema.ILOffset);
                if ((modifyMask & InstrumentationDataProcessingState.Type) == InstrumentationDataProcessingState.Type)
                    valueEmitter.EmitLong((long)schema.InstrumentationKind, (long)prevSchema.InstrumentationKind);
                if ((modifyMask & InstrumentationDataProcessingState.Count) == InstrumentationDataProcessingState.Count)
                    valueEmitter.EmitLong(schema.Count, prevSchema.Count);
                if ((modifyMask & InstrumentationDataProcessingState.Other) == InstrumentationDataProcessingState.Other)
                    valueEmitter.EmitLong(schema.Other, prevSchema.Other);

                for (int i = 0; i < schema.Count; i++)
                {
                    switch (schema.InstrumentationKind & PgoInstrumentationKind.MarshalMask)
                    {
                        case PgoInstrumentationKind.None:
                            break;
                        case PgoInstrumentationKind.FourByte:
                            {
                                long valueToEmit;
                                if (schema.Count == 1)
                                {
                                    valueToEmit = schema.DataLong;
                                }
                                else
                                {
                                    valueToEmit = ((int[])schema.DataObject)[i];
                                }
                                valueEmitter.EmitLong(valueToEmit, prevEmittedIntData);
                                prevEmittedIntData = valueToEmit;
                                break;
                            }
                        case PgoInstrumentationKind.EightByte:
                            {
                                long valueToEmit;
                                if (schema.Count == 1)
                                {
                                    valueToEmit = schema.DataLong;
                                }
                                else
                                {
                                    valueToEmit = ((long[])schema.DataObject)[i];
                                }
                                valueEmitter.EmitLong(valueToEmit, prevEmittedIntData);
                                prevEmittedIntData = valueToEmit;
                                break;
                            }
                        case PgoInstrumentationKind.TypeHandle:
                            {
                                TType typeToEmit = ((TType[])schema.DataObject)[i];
                                valueEmitter.EmitType(typeToEmit, prevEmittedType);
                                prevEmittedType = typeToEmit;
                                break;
                            }
                    }
                }

                prevSchema = schema;
            }

            // Emit a "done" schema
            if (!valueEmitter.EmitDone())
            {
                // If EmitDone returns true, no further data needs to be encoded.
                // Otherwise, emit a "Done" schema
                valueEmitter.EmitLong((long)InstrumentationDataProcessingState.Type, 0);
                valueEmitter.EmitLong((long)PgoInstrumentationKind.Done, (long)prevSchema.InstrumentationKind);
            }
        }


        private class PgoSchemaMergeComparer : IComparer<PgoSchemaElem>, IEqualityComparer<PgoSchemaElem>
        {
            public static PgoSchemaMergeComparer Singleton = new PgoSchemaMergeComparer();

            private static bool SchemaMergesItemsWithDifferentOtherFields(PgoInstrumentationKind kind)
            {
                switch (kind)
                {
                    //
                    default:
                        // All non-specified kinds are not distinguishable by Other field
                        return false;
                }
            }

            public int Compare(PgoSchemaElem x, PgoSchemaElem y)
            {
                if (x.ILOffset != y.ILOffset)
                {
                    return x.ILOffset.CompareTo(y.ILOffset);
                }
                if (x.InstrumentationKind != y.InstrumentationKind)
                {
                    return x.InstrumentationKind.CompareTo(y.InstrumentationKind);
                }
                // Some InstrumentationKinds may be compared based on the Other field, some may not
                if (x.Other != y.Other && SchemaMergesItemsWithDifferentOtherFields(x.InstrumentationKind))
                {
                    return x.Other.CompareTo(y.Other);
                }

                return 0;
            }

            public bool Equals(PgoSchemaElem x, PgoSchemaElem y)
            {
                if (x.ILOffset != y.ILOffset)
                    return false;
                if (x.InstrumentationKind != y.InstrumentationKind)
                    return false;
                if (x.InstrumentationKind != y.InstrumentationKind && SchemaMergesItemsWithDifferentOtherFields(x.InstrumentationKind))
                    return false;
                return true;
            }
            int IEqualityComparer<PgoSchemaElem>.GetHashCode(PgoSchemaElem obj) => obj.ILOffset ^ ((int)obj.InstrumentationKind << 20);
        }

        public static PgoSchemaElem[] Merge<TType>(ReadOnlySpan<PgoSchemaElem[]> schemasToMerge)
        {
            {
                // The merging algorithm will sort the schema data by iloffset, then by InstrumentationKind
                // From there each individual instrumentation kind shall have a specific merging rule
                Dictionary<PgoSchemaElem, PgoSchemaElem> dataMerger = new Dictionary<PgoSchemaElem, PgoSchemaElem>(PgoSchemaMergeComparer.Singleton);

                foreach (PgoSchemaElem[] schemaSet in schemasToMerge)
                {
                    bool foundNumRuns = false;

                    foreach (PgoSchemaElem schema in schemaSet)
                    {
                        if (schema.InstrumentationKind == PgoInstrumentationKind.NumRuns)
                            foundNumRuns = true;
                        MergeInSchemaElem(dataMerger, schema);
                    }

                    if (!foundNumRuns)
                    {
                        PgoSchemaElem oneRunSchema = new PgoSchemaElem();
                        oneRunSchema.InstrumentationKind = PgoInstrumentationKind.NumRuns;
                        oneRunSchema.ILOffset = 0;
                        oneRunSchema.Other = 1;
                        oneRunSchema.Count = 1;
                        MergeInSchemaElem(dataMerger, oneRunSchema);
                    }
                }

                PgoSchemaElem[] result = new PgoSchemaElem[dataMerger.Count];
                dataMerger.Values.CopyTo(result, 0);
                Array.Sort(result, PgoSchemaMergeComparer.Singleton);
                return result;
            }

            void MergeInSchemaElem(Dictionary<PgoSchemaElem, PgoSchemaElem> dataMerger, PgoSchemaElem schema)
            {
                if (dataMerger.TryGetValue(schema, out var existingSchemaItem))
                {
                    // Actually merge two schema items
                    PgoSchemaElem mergedElem = existingSchemaItem;

                    switch (existingSchemaItem.InstrumentationKind)
                    {
                        case PgoInstrumentationKind.BasicBlockIntCount:
                        case PgoInstrumentationKind.EdgeIntCount:
                        case PgoInstrumentationKind.TypeHandleHistogramCount:
                            if ((existingSchemaItem.Count != 1) || (schema.Count != 1))
                            {
                                throw new Exception("Unable to merge pgo data. Invalid format");
                            }
                            mergedElem.DataLong = existingSchemaItem.DataLong + schema.DataLong;
                            break;

                        case PgoInstrumentationKind.TypeHandleHistogramTypeHandle:
                            {
                                mergedElem.Count = existingSchemaItem.Count + schema.Count;
                                TType[] newMergedTypeArray = new TType[mergedElem.Count];
                                mergedElem.DataObject = newMergedTypeArray;
                                int i = 0;
                                foreach (TType type in (TType[])existingSchemaItem.DataObject)
                                {
                                    newMergedTypeArray[i++] = type;
                                }
                                foreach (TType type in (TType[])schema.DataObject)
                                {
                                    newMergedTypeArray[i++] = type;
                                }
                                break;
                            }

                        case PgoInstrumentationKind.Version:
                            {
                                mergedElem.Other = Math.Max(existingSchemaItem.Other, schema.Other);
                                break;
                            }

                        case PgoInstrumentationKind.NumRuns:
                            {
                                mergedElem.Other = existingSchemaItem.Other + schema.Other;
                                break;
                            }
                    }

                    Debug.Assert(PgoSchemaMergeComparer.Singleton.Compare(schema, mergedElem) == 0);
                    Debug.Assert(PgoSchemaMergeComparer.Singleton.Equals(schema, mergedElem) == true);
                    dataMerger[mergedElem] = mergedElem;
                }
                else
                {
                    dataMerger.Add(schema, schema);
                }
            }
        }
    }
}
