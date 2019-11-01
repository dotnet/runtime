// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Internal.TypeSystem
{
    public struct ExplicitLayoutValidator
    {
        private enum FieldLayoutTag : byte
        {
            Empty,
            NonORef,
            ORef,
        }

        private readonly int _pointerSize;

        private readonly FieldLayoutTag[] _fieldLayout;

        private readonly MetadataType _typeBeingValidated;

        private ExplicitLayoutValidator(MetadataType type, int typeSizeInBytes)
        {
            _typeBeingValidated = type;
            _pointerSize = type.Context.Target.PointerSize;
            _fieldLayout = new FieldLayoutTag[typeSizeInBytes];
        }

        public static void Validate(MetadataType type, ComputedInstanceFieldLayout layout)
        {
            ExplicitLayoutValidator validator = new ExplicitLayoutValidator(type, layout.ByteCountUnaligned.AsInt);
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

        private void SetFieldLayout(int offset, FieldLayoutTag tag)
        {
            FieldLayoutTag existingTag = _fieldLayout[offset];
            if (existingTag != tag)
            {
                if (existingTag != FieldLayoutTag.Empty)
                {
                    ThrowFieldLayoutError(offset);
                }
                _fieldLayout[offset] = tag;
            }
        }

        private void ThrowFieldLayoutError(int offset)
        {
            ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadExplicitLayout, _typeBeingValidated, offset.ToStringInvariant());
        }
    }
}
