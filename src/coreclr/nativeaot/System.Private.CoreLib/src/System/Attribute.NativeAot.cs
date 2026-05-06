// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime;
using System.Runtime.CompilerServices;

using Internal.Runtime;

namespace System
{
    public abstract partial class Attribute
    {
        // An override of this method will be injected by the compiler into all attributes that have fields.
        // This API is a bit awkward because we want to avoid burning more than one vtable slot on this.
        // When index is negative, this method is expected to return the number of fields of this
        // valuetype. Otherwise, it returns the offset and type handle of the index-th field on this type.
        internal virtual unsafe int __GetFieldHelper(int index, out MethodTable* mt)
        {
            Debug.Assert(index < 0);
            mt = default;
            return 0;
        }

        public override unsafe bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null)
                return false;

            if (this.GetType() != obj.GetType())
                return false;

            int numFields = __GetFieldHelper(-1, out _);

            ref byte thisRawData = ref this.GetRawData();
            ref byte thatRawData = ref obj.GetRawData();

            for (int i = 0; i < numFields; i++)
            {
                int fieldOffset = __GetFieldHelper(i, out MethodTable* fieldType);

                Debug.Assert(!fieldType->IsPointer && !fieldType->IsFunctionPointer);

                // Fetch the value of the field on both types
                object thisResult = RuntimeImports.RhBoxAny(ref Unsafe.Add(ref thisRawData, fieldOffset), fieldType);
                object thatResult = RuntimeImports.RhBoxAny(ref Unsafe.Add(ref thatRawData, fieldOffset), fieldType);

                if (!AreFieldValuesEqual(thisResult, thatResult))
                {
                    return false;
                }
            }

            return true;
        }

        public override unsafe int GetHashCode()
        {
            int numFields = __GetFieldHelper(-1, out _);

            ref byte thisRawData = ref this.GetRawData();

            object? vThis = null;

            for (int i = 0; i < numFields; i++)
            {
                int fieldOffset = __GetFieldHelper(i, out MethodTable* fieldType);

                Debug.Assert(!fieldType->IsPointer && !fieldType->IsFunctionPointer);

                object? fieldValue = RuntimeImports.RhBoxAny(ref Unsafe.Add(ref thisRawData, fieldOffset), fieldType);

                // The hashcode of an array ignores the contents of the array, so it can produce
                // different hashcodes for arrays with the same contents.
                // Since we do deep comparisons of arrays in Equals(), this means Equals and GetHashCode will
                // be inconsistent for arrays. Therefore, we ignore hashes of arrays.
                if (fieldValue != null && !fieldValue.GetType().IsArray)
                    vThis = fieldValue;

                if (vThis != null)
                    break;
            }

            if (vThis != null)
                return vThis.GetHashCode();

            // Matches the reflection-based implementation in other runtimes
            return typeof(Attribute).GetHashCode();
        }
    }
}
