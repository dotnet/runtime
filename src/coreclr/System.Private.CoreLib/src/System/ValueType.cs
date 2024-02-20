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

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public abstract partial class ValueType
    {
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "Trimmed fields don't make a difference for equality")]
        public override unsafe bool Equals([NotNullWhen(true)] object? obj)
        {
            if (null == obj)
            {
                return false;
            }

            if (GetType() != obj.GetType())
            {
                return false;
            }

            // if there are no GC references in this object we can avoid reflection
            // and do a fast memcmp
            if (CanCompareBitsOrUseFastGetHashCode(RuntimeHelpers.GetMethodTable(obj))) // MethodTable kept alive by access to object below
            {
                return SpanHelpers.SequenceEqual(
                    ref RuntimeHelpers.GetRawData(this),
                    ref RuntimeHelpers.GetRawData(obj),
                    RuntimeHelpers.GetMethodTable(this)->GetNumInstanceFieldBytes());
            }

            FieldInfo[] thisFields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            for (int i = 0; i < thisFields.Length; i++)
            {
                object? thisResult = thisFields[i].GetValue(this);
                object? thatResult = thisFields[i].GetValue(obj);

                if (thisResult == null)
                {
                    if (thatResult != null)
                        return false;
                }
                else
                if (!thisResult.Equals(thatResult))
                {
                    return false;
                }
            }

            return true;
        }

        // Return true if the valuetype does not contain pointer, is tightly packed,
        // does not have floating point number field and does not override Equals method.
        private static unsafe bool CanCompareBitsOrUseFastGetHashCode(MethodTable* pMT)
        {
            MethodTableAuxiliaryData* pAuxData = pMT->AuxiliaryData;

            if (pAuxData->HasCheckedCanCompareBitsOrUseFastGetHashCode)
            {
                return pAuxData->CanCompareBitsOrUseFastGetHashCode;
            }

            return CanCompareBitsOrUseFastGetHashCodeHelper(pMT);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "MethodTable_CanCompareBitsOrUseFastGetHashCode")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static unsafe partial bool CanCompareBitsOrUseFastGetHashCodeHelper(MethodTable* pMT);

        /*=================================GetHashCode==================================
        **Action: Our algorithm for returning the hashcode is a little bit complex.  We look
        **        for the first non-static field and get its hashcode.  If the type has no
        **        non-static fields, we return the hashcode of the type.  We can't take the
        **        hashcode of a static member because if that member is of the same type as
        **        the original type, we'll end up in an infinite loop.
        **Returns: The hashcode for the type.
        **Arguments: None.
        **Exceptions: None.
        ==============================================================================*/
        public override unsafe int GetHashCode()
        {
            // The default implementation of GetHashCode() for all value types.
            // Note that this implementation reveals the value of the fields.
            // So if the value type contains any sensitive information it should
            // implement its own GetHashCode().

            MethodTable* pMT = RuntimeHelpers.GetMethodTable(this);
            ref byte rawData = ref RuntimeHelpers.GetRawData(this);
            HashCode hashCode = default;

            // To get less colliding and more evenly distributed hash codes,
            // we munge the class index into the hashcode
            hashCode.Add((IntPtr)pMT);

            if (CanCompareBitsOrUseFastGetHashCode(pMT))
            {
                // this is a struct with no refs and no "strange" offsets
                uint size = pMT->GetNumInstanceFieldBytes();
                hashCode.AddBytes(MemoryMarshal.CreateReadOnlySpan(ref rawData, (int)size));
            }
            else
            {
                object thisRef = this;
                switch (GetHashCodeStrategy(pMT, ObjectHandleOnStack.Create(ref thisRef), out uint fieldOffset, out uint fieldSize, out delegate* managed<ref byte, int> getHashCodeMethod))
                {
                    case ValueTypeHashCodeStrategy.ReferenceField:
                        hashCode.Add(Unsafe.As<byte, object>(ref Unsafe.AddByteOffset(ref rawData, fieldOffset)).GetHashCode());
                        break;

                    case ValueTypeHashCodeStrategy.DoubleField:
                        hashCode.Add(Unsafe.As<byte, double>(ref Unsafe.AddByteOffset(ref rawData, fieldOffset)).GetHashCode());
                        break;

                    case ValueTypeHashCodeStrategy.SingleField:
                        hashCode.Add(Unsafe.As<byte, float>(ref Unsafe.AddByteOffset(ref rawData, fieldOffset)).GetHashCode());
                        break;

                    case ValueTypeHashCodeStrategy.FastGetHashCode:
                        Debug.Assert(fieldSize != 0);
                        hashCode.AddBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AddByteOffset(ref rawData, fieldOffset), (int)fieldSize));
                        break;

                    case ValueTypeHashCodeStrategy.ValueTypeOverride:
                        Debug.Assert(getHashCodeMethod != null);
                        hashCode.Add(getHashCodeMethod(ref Unsafe.AddByteOffset(ref rawData, fieldOffset)));
                        break;
                }
            }

            return hashCode.ToHashCode();
        }

        // Must match the definition in src\vm\comutilnative.cpp
        private enum ValueTypeHashCodeStrategy
        {
            None,
            ReferenceField,
            DoubleField,
            SingleField,
            FastGetHashCode,
            ValueTypeOverride,
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ValueType_GetHashCodeStrategy")]
        private static unsafe partial ValueTypeHashCodeStrategy GetHashCodeStrategy(
            MethodTable* pMT, ObjectHandleOnStack objHandle, out uint fieldOffset, out uint fieldSize, out delegate* managed<ref byte, int> getHashCodeMethod);

        public override string? ToString()
        {
            return this.GetType().ToString();
        }
    }
}
