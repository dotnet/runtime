// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Internal.TypeSystem
{
    public static class ExplicitLayoutValidator
    {
        private enum FieldLayoutTag : byte
        {
            Empty,
            NonORef,
            ORef,
            ByRef,
        }

        private sealed class Validator(MetadataType type) : FieldLayoutIntervalCalculator<FieldLayoutTag>(type.Context.Target.PointerSize)
        {
            // We want to mark empty intervals as having NonORef data.
            protected override FieldLayoutTag EmptyIntervalData => FieldLayoutTag.NonORef;

            protected override bool IntervalsHaveCompatibleTags(FieldLayoutTag existingTag, FieldLayoutTag nextTag) => existingTag == nextTag;

            protected override FieldLayoutInterval CombineIntervals(FieldLayoutInterval firstInterval, FieldLayoutInterval nextInterval)
            {
                if (!IntervalsHaveCompatibleTags(firstInterval.Tag, nextInterval.Tag))
                    ThrowFieldLayoutError(nextInterval.Start);

                firstInterval.EndSentinel = nextInterval.EndSentinel;
                return firstInterval;
            }

            protected override FieldLayoutTag GetIntervalDataForType(int offset, TypeDesc fieldType)
            {
                if (fieldType.IsGCPointer)
                {
                    if (offset % PointerSize != 0)
                    {
                        // Misaligned ORef
                        ThrowFieldLayoutError(offset);
                    }
                    return FieldLayoutTag.ORef;
                }
                else if (fieldType.IsByRef)
                {
                    if (offset % PointerSize != 0)
                    {
                        // Misaligned ByRef
                        ThrowFieldLayoutError(offset);
                    }
                    return FieldLayoutTag.ByRef;
                }
                else if (fieldType.IsPointer || fieldType.IsFunctionPointer)
                {
                    return FieldLayoutTag.NonORef;
                }
                else if (fieldType.IsValueType)
                {
                    MetadataType mdType = (MetadataType)fieldType;
                    if (!mdType.ContainsGCPointers && !mdType.IsByRefLike)
                    {
                        // Plain value type, mark the entire range as NonORef
                        return FieldLayoutTag.NonORef;
                    }
                    Debug.Fail("We should recurse on value types with GC pointers or ByRefLike types");
                    return FieldLayoutTag.Empty;
                }
                else
                {
                    return FieldLayoutTag.Empty;
                }
            }

            protected override bool NeedsRecursiveLayout(int offset, TypeDesc fieldType)
            {
                if (!fieldType.IsValueType || !((MetadataType)fieldType).ContainsGCPointers && !fieldType.IsByRefLike)
                {
                    return false;
                }

                if (offset % PointerSize != 0)
                {
                    // Misaligned struct with GC pointers or ByRef
                    ThrowFieldLayoutError(offset);
                }

                return true;
            }

            private void ThrowFieldLayoutError(int offset)
            {
                ThrowHelper.ThrowTypeLoadException(ExceptionStringID.ClassLoadExplicitLayout, type, offset.ToStringInvariant());
            }
        }

        public static void Validate(MetadataType type, ComputedInstanceFieldLayout layout)
        {
            Validator validator = new(type);
            foreach (FieldAndOffset fieldAndOffset in layout.Offsets)
            {
                validator.AddToFieldLayout(fieldAndOffset.Offset.AsInt, fieldAndOffset.Field.FieldType);
            }
        }
    }
}
