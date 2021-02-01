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
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public abstract class ValueType
    {
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "Trimmed fields don't make a difference for equality")]
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (null == obj)
            {
                return false;
            }
            Type thisType = this.GetType();
            Type thatType = obj.GetType();

            if (thatType != thisType)
            {
                return false;
            }

            object thisObj = (object)this;
            object? thisResult, thatResult;

            // if there are no GC references in this object we can avoid reflection
            // and do a fast memcmp
            if (CanCompareBits(this))
                return FastEqualsCheck(thisObj, obj);

            FieldInfo[] thisFields = thisType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            for (int i = 0; i < thisFields.Length; i++)
            {
                thisResult = thisFields[i].GetValue(thisObj);
                thatResult = thisFields[i].GetValue(obj);

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

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool CanCompareBits(object obj);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool FastEqualsCheck(object a, object b);

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
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern override int GetHashCode();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern int GetHashCodeOfPtr(IntPtr ptr);

        public override string? ToString()
        {
            return this.GetType().ToString();
        }
    }
}
