// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Internal.TypeSystem
{
    public struct MethodImplRecord
    {
        public readonly MethodDesc Decl;
        public readonly MethodDesc Body;

        public MethodImplRecord(MethodDesc decl, MethodDesc body)
        {
            Decl = decl;
            Body = body;
        }
    }

    // MethodImpl api surface for types.
    public partial class MetadataType
    {
        /// <summary>
        /// Compute an array of all MethodImpls that pertain to overriding virtual (non-interface methods) on this type.
        /// May be expensive.
        /// </summary>
        protected abstract MethodImplRecord[] ComputeVirtualMethodImplsForType();

        private MethodImplRecord[] _allVirtualMethodImplsForType;
        /// <summary>
        /// Get an array of all MethodImpls that pertain to overriding virtual (non-interface methods) on this type.
        /// Expected to cache results so this api can be used repeatedly.
        /// </summary>
        public MethodImplRecord[] VirtualMethodImplsForType
        {
            get
            {
                _allVirtualMethodImplsForType ??= ComputeVirtualMethodImplsForType();

                return _allVirtualMethodImplsForType;
            }
        }

        /// <summary>
        /// Get an array of MethodImpls where the Decl method matches by name with the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public abstract MethodImplRecord[] FindMethodsImplWithMatchingDeclName(string name);
    }
}
