// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Internal.TypeSystem.Interop
{
    public static class MarshalUtils
    {
        /// <summary>
        /// Returns true if this is a type that doesn't require marshalling.
        /// </summary>
        public static bool IsBlittableType(TypeDesc type)
        {
            type = type.UnderlyingType;

            if (type.IsValueType)
            {
                if (type.IsPrimitive)
                {
                    // All primitive types except char and bool are blittable
                    TypeFlags category = type.Category;
                    if (category == TypeFlags.Boolean || category == TypeFlags.Char)
                        return false;

                    return true;
                }

                foreach (FieldDesc field in type.GetFields())
                {
                    if (field.IsStatic)
                        continue;

                    TypeDesc fieldType = field.FieldType;

                    // TODO: we should also reject fields that specify custom marshalling
                    if (!MarshalUtils.IsBlittableType(fieldType))
                    {
                        // This field can still be blittable if it's a Char and marshals as Unicode
                        var owningType = field.OwningType as MetadataType;
                        if (owningType == null)
                            return false;

                        if (fieldType.Category != TypeFlags.Char ||
                            owningType.PInvokeStringFormat == PInvokeStringFormat.AnsiClass)
                            return false;
                    }
                }
                return true;
            }

            if (type.IsPointer || type.IsFunctionPointer)
                return true;

            return false;
        }
    }
}
