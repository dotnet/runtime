// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;
using System.Runtime.CompilerServices;

using Internal.Runtime;

namespace System
{
    // CONTRACT with Runtime
    // The Object type is one of the primitives understood by the compilers and runtime
    // Data Contract: Single field of type MethodTable*
    public unsafe partial class Object
    {
        // CS0649: Field '{blah}' is never assigned to, and will always have its default value
#pragma warning disable 649
        [NonSerialized]
        internal MethodTable* m_pEEType;
#pragma warning restore

        [Intrinsic]
        public Type GetType()
        {
            return Type.GetTypeFromEETypePtr(this.GetEETypePtr());
        }

        [Intrinsic]
        protected object MemberwiseClone()
        {
            return RuntimeImports.RhMemberwiseClone(this);
        }
    }
}
