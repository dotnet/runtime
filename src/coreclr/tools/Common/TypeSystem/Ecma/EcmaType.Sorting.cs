// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata.Ecma335;

namespace Internal.TypeSystem.Ecma
{
    // Functionality related to deterministic ordering of types
    partial class EcmaType
    {
        protected internal override int ClassCode => 1340416537;

        protected internal override int CompareToImpl(TypeDesc other, TypeSystemComparer comparer)
        {
            // Sort by module in preference to by token. This will place types from the same module near each other
            // even when working with several modules.
            var otherType = (EcmaType)other;
            int result = _module.CompareTo(otherType._module);
            if (result != 0)
                return result;

            return _module.MetadataReader.GetToken(_handle) - otherType._module.MetadataReader.GetToken(otherType._handle);
        }
    }
}
