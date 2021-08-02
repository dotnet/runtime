// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Internal.TypeSystem
{
    public ref struct ExplicitLayoutValidator
    {
        private const int FieldLayoutSegmentSize = 1024;

        private enum FieldLayoutTag : byte
        {
            Empty,
            NonORef,
            ORef,
        }

        private readonly int _pointerSize;

        // Break FieldLayout data storage into an initial Span, which is stack allocated, and thus
        // doesn't use GC memory, and an array of arrays for data beyond the first segment of field
        // layout data. This allows the typical explicit layout structure to not require memory allocation
        // and larger structures to avoid allocation for unused portions of the structure
        // and when a large structure is in use, then the memory allocated will not be a huge buffer
        // which is vulnerable to memory fragmentation problems on 32bit platforms.
        //
        // Ideally, for large structures this would be an interval tree of some sort, but the large structure
        // code is rarely exercised, and thus I expect would be likely to contain bugs if such a structure
        // were only implemented for this feature.
        private readonly FieldLayoutTag[][] _fieldLayout;
        private readonly Span<FieldLayoutTag> _firstFieldLayoutSegment;

        private readonly MetadataType _typeBeingValidated;

        private ExplicitLayoutValidator(MetadataType type, int typeSizeInBytes, Span<FieldLayoutTag> firstSegment)
        {
            _typeBeingValidated = type;
            _pointerSize = type.Context.Target.PointerSize;
            _firstFieldLayoutSegment = firstSegment;
            if (typeSizeInBytes > firstSegment.Length)
            {
                _fieldLayout = new FieldLayoutTag[((typeSizeInBytes - firstSegment.Length) / FieldLayoutSegmentSize) + 1][];
            }
            else
            {
                _fieldLayout = null;
            }
        }

        public static void Validate(MetadataType type, ComputedInstanceFieldLayout layout)
        {
            int typeSizeInBytes = layout.ByteCountUnaligned.AsInt;
            Span<FieldLayoutTag> firstSegment = stackalloc FieldLayoutTag[Math.Min(typeSizeInBytes, FieldLayoutSegmentSize)];

            ExplicitLayoutValidator validator = new ExplicitLayoutValidator(type, typeSizeInBytes, firstSegment);

            foreach (FieldAndOffset fieldAndOffset in layout.Offsets)
            {
                validator.AddToFieldLayout(fieldAndOffset.Offset.AsInt, fieldAndOffset.Field.FieldType);
            }
        }

        private void AddToFieldLayout(int offset, TypeDesc fieldType)
        {
            if (fieldType.IsGCPointer)
            {
                if (offset % _pointerSize != 0)
                {
                    // Misaligned ORef
                    ThrowFieldLayoutError(offset);
                }
                SetFieldLayout(offset, _pointerSize, FieldLayoutTag.ORef);
            }
            else if (fieldType.IsPointer || fieldType.IsFunctionPointer)
            {
                SetFieldLayout(offset, _pointerSize, FieldLayoutTag.NonORef);
            }
            else if (fieldType.IsValueType)
            {
                if (fieldType.IsByRefLike && offset % _pointerSize != 0)
                {
                    // Misaligned ByRefLike
                    ThrowFieldLayoutError(offset);
                }

                MetadataType mdType = (MetadataType)fieldType;
                int fieldSize = mdType.InstanceByteCountUnaligned.AsInt;
                if (!mdType.ContainsGCPointers)
                {
                    // Plain value type, mark the entire range as NonORef
                    SetFieldLayout(offset, fieldSize, FieldLayoutTag.NonORef);
                }
                else
                {
                    if (offset % _pointerSize != 0)
                    {
                        // Misaligned struct with GC pointers
                        ThrowFieldLayoutError(offset);
                    }

                    bool[] fieldORefMap = new bool[fieldSize];
                    MarkORefLocations(mdType, fieldORefMap, offset: 0);
                    for (int index = 0; index < fieldSize; index++)
                    {
                        SetFieldLayout(offset + index, fieldORefMap[index] ? FieldLayoutTag.ORef : FieldLayoutTag.NonORef);
                    }
                }
            }
            else if (fieldType.IsByRef)
            {
                if (offset % _pointerSize != 0)
                {
                    // Misaligned pointer field
                    ThrowFieldLayoutError(offset);
                }
                SetFieldLayout(offset, _pointerSize, FieldLayoutTag.NonORef);
            }
            else
            {
                Debug.Assert(false, fieldType.ToString());
            }
        }

        private void MarkORefLocations(MetadataType type, bool[] orefMap, int offset)
        {
            // Recurse into struct fields
            foreach (FieldDesc field in type.GetFields())
            {
                if (!field.IsStatic)
                {
                    int fieldOffset = offset + field.Offset.AsInt;
                    if (field.FieldType.IsGCPointer)
                    {
                        for (int index = 0; index < _pointerSize; index++)
                        {
                            orefMap[fieldOffset + index] = true;
                        }
                    }
                    else if (field.FieldType.IsValueType)
                    {
                        MetadataType mdFieldType = (MetadataType)field.FieldType;
                        if (mdFieldType.ContainsGCPointers)
                        {
                            MarkORefLocations(mdFieldType, orefMap, fieldOffset);
                        }
                    }
                }
            }
        }

        private void SetFieldLayout(int offset, int count, FieldLayoutTag tag)
        {
            for (int index = 0; index < count; index++)
            {
                SetFieldLayout(offset + index, tag);
            }
        }

        private int GetSegmentIndex(int offset, out int segmentInternalIndex)
        {
            segmentInternalIndex = offset % FieldLayoutSegmentSize;
            return (offset - _firstFieldLayoutSegment.Length) / FieldLayoutSegmentSize;
        }

        private void SetFieldLayout(int offset, FieldLayoutTag tag)
        {
            FieldLayoutTag existingTag;
            Span<FieldLayoutTag> segment;
            int segmentInternalIndex = offset;
            
            if (offset >= FieldLayoutSegmentSize)
            {
                segment = _firstFieldLayoutSegment;
            }
            else
            {
                int segmentIndex = GetSegmentIndex(offset, out segmentInternalIndex);
                if (_fieldLayout[segmentIndex] == null)
                {
                    _fieldLayout[segmentIndex] = new FieldLayoutTag[FieldLayoutSegmentSize];
                }
                segment = _fieldLayout[segmentIndex];
            }

            existingTag = segment[segmentInternalIndex];

            if (existingTag != tag)
            {
                if (existingTag != FieldLayoutTag.Empty)
                {
                    ThrowFieldLayoutError(offset);
                }
                segment[segmentInternalIndex] = tag;
            }
        }

        private void ThrowFieldLayoutError(int offset)
        {
            ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadExplicitLayout, _typeBeingValidated, offset.ToStringInvariant());
        }
    }
}
