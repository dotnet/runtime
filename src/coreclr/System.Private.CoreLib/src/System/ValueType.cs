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
using System.Threading;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public abstract class ValueType
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
            if (CanCompareBits(this))
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

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool CanCompareBits(object obj);

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

        private static int s_seed;

        internal static int GetHashCodeOfPtr(IntPtr ptr)
        {
            int hashCode = (int)ptr;

            if (hashCode == 0)
            {
                return 0;
            }

            int seed = s_seed;

            // Initialize s_seed lazily
            if (seed == 0)
            {
                // We use the first non-0 pointer as the seed, all hashcodes will be based off that.
                // This is to make sure that we only reveal relative memory addresses and never absolute ones.
                seed = hashCode;
                Interlocked.CompareExchange(ref s_seed, seed, 0);
                seed = s_seed;
            }

            Debug.Assert(s_seed != 0);
            return hashCode - seed;
        }

        public override string? ToString()
        {
            return this.GetType().ToString();
        }
    }
}
