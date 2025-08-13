// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Internal.Runtime;

namespace System
{
    // CONTRACT with Runtime
    // The Object type is one of the primitives understood by the compilers and runtime
    // Data Contract: Single field of type MethodTable*

    public unsafe class Object
    {
        // CS0649: Field '{blah}' is never assigned to, and will always have its default value
#pragma warning disable 649
        private MethodTable* m_pEEType;
#pragma warning restore

        // Creates a new instance of an Object.
        public Object()
        {
        }

#pragma warning disable CA1821 // Remove empty Finalizers
        ~Object()
        {
        }
#pragma warning restore CA1821

        public virtual bool Equals(object o)
        {
            return false;
        }

        public virtual int GetHashCode()
        {
            return 0;
        }

        internal MethodTable* GetMethodTable() => m_pEEType;

        [StructLayout(LayoutKind.Sequential)]
        private class RawData
        {
            public byte Data;
        }

        internal ref byte GetRawData()
        {
            return ref Unsafe.As<RawData>(this).Data;
        }

        /// <summary>
        /// Return size of all data (excluding ObjHeader and MethodTable*).
        /// Note that for strings/arrays this would include the Length as well.
        /// </summary>
        internal uint GetRawObjectDataSize()
        {
            return GetMethodTable()->BaseSize - (uint)sizeof(ObjHeader) - (uint)sizeof(MethodTable*);
        }
    }
}
