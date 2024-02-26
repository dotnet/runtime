// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
** Purpose: Base class for all value classes.
**
**
===========================================================*/

using System.Diagnostics.CodeAnalysis;
using System.Runtime;
using System.Runtime.CompilerServices;

using Internal.Runtime;

using Debug = System.Diagnostics.Debug;

namespace System
{
    // CONTRACT with Runtime
    // Place holder type for type hierarchy, Compiler/Runtime requires this class
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public abstract class ValueType
    {
        public override string? ToString()
        {
            return this.GetType().ToString();
        }

        private const int UseFastHelper = -1;
        private const int GetNumFields = -1;

        // An override of this method will be injected by the compiler into all valuetypes that cannot be compared
        // using a simple memory comparison.
        // This API is a bit awkward because we want to avoid burning more than one vtable slot on this.
        // When index == GetNumFields, this method is expected to return the number of fields of this
        // valuetype. Otherwise, it returns the offset and type handle of the index-th field on this type.
        internal virtual unsafe int __GetFieldHelper(int index, out MethodTable* mt)
        {
            // Value types that don't override this method will use the fast path that looks at bytes, not fields.
            Debug.Assert(index == GetNumFields);
            mt = default;
            return UseFastHelper;
        }

        public override unsafe bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null || obj.GetMethodTable() != this.GetMethodTable())
                return false;

            int numFields = __GetFieldHelper(GetNumFields, out _);

            ref byte thisRawData = ref this.GetRawData();
            ref byte thatRawData = ref obj.GetRawData();

            if (numFields == UseFastHelper)
            {
                // Sanity check - if there are GC references, we should not be comparing bytes
                Debug.Assert(!this.GetMethodTable()->ContainsGCPointers);

                // Compare the memory
                int valueTypeSize = (int)this.GetMethodTable()->ValueTypeSize;
                return SpanHelpers.SequenceEqual(ref thisRawData, ref thatRawData, valueTypeSize);
            }
            else
            {
                // Foreach field, box and call the Equals method.
                for (int i = 0; i < numFields; i++)
                {
                    int fieldOffset = __GetFieldHelper(i, out MethodTable* fieldType);

                    // Fetch the value of the field on both types
                    object thisField = RuntimeImports.RhBoxAny(ref Unsafe.Add(ref thisRawData, fieldOffset), fieldType);
                    object thatField = RuntimeImports.RhBoxAny(ref Unsafe.Add(ref thatRawData, fieldOffset), fieldType);

                    // Compare the fields
                    if (thisField == null)
                    {
                        if (thatField != null)
                            return false;
                    }
                    else if (!thisField.Equals(thatField))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public override unsafe int GetHashCode()
        {
            HashCode hashCode = default;
            hashCode.Add((IntPtr)this.GetMethodTable());

            int numFields = __GetFieldHelper(GetNumFields, out _);

            if (numFields == UseFastHelper)
                hashCode.AddBytes(GetSpanForField(this.GetMethodTable(), ref this.GetRawData()));
            else
                RegularGetValueTypeHashCode(ref hashCode, ref this.GetRawData(), numFields);

            return hashCode.ToHashCode();
        }

        private static unsafe ReadOnlySpan<byte> GetSpanForField(MethodTable* type, ref byte data)
        {
            // Sanity check - if there are GC references, we should not be hashing bytes
            Debug.Assert(!type->ContainsGCPointers);
            return new ReadOnlySpan<byte>(ref data, (int)type->ValueTypeSize);
        }

        private unsafe void RegularGetValueTypeHashCode(ref HashCode hashCode, ref byte data, int numFields)
        {
            // We only take the hashcode for the first non-null field. That's what the CLR does.
            for (int i = 0; i < numFields; i++)
            {
                int fieldOffset = __GetFieldHelper(i, out MethodTable* fieldType);
                ref byte fieldData = ref Unsafe.Add(ref data, fieldOffset);

                Debug.Assert(!fieldType->IsPointer && !fieldType->IsFunctionPointer);

                if (fieldType->ElementType == EETypeElementType.Single)
                {
                    hashCode.Add(Unsafe.As<byte, float>(ref fieldData));
                }
                else if (fieldType->ElementType == EETypeElementType.Double)
                {
                    hashCode.Add(Unsafe.As<byte, double>(ref fieldData));
                }
                else if (fieldType->IsPrimitive)
                {
                    hashCode.AddBytes(GetSpanForField(fieldType, ref fieldData));
                }
                else if (fieldType->IsValueType)
                {
                    // We have no option but to box since this value type could have
                    // GC pointers (we could find out if we want though), or fields of type Double/Single (we can't
                    // really find out). Double/Single have weird requirements around -0.0 and +0.0.
                    // If this boxing becomes a problem, we could build a piece of infrastructure that determines the slot
                    // of __GetFieldHelper, decodes the unboxing stub pointed to by the slot to the real target
                    // (we already have that part), and calls the entrypoint that expects a byref `this`, and use the
                    // data to decide between calling fast or regular hashcode helper.
                    var fieldValue = (ValueType)RuntimeImports.RhBox(fieldType, ref fieldData);
                    if (fieldValue != null)
                    {
                        hashCode.Add(fieldValue);
                    }
                    else
                    {
                        // nullable type with no value, try next
                        continue;
                    }
                }
                else
                {
                    object fieldValue = Unsafe.As<byte, object>(ref fieldData);
                    if (fieldValue != null)
                    {
                        hashCode.Add(fieldValue);
                    }
                    else
                    {
                        // null object reference, try next
                        continue;
                    }
                }
                break;
            }
        }
    }
}
