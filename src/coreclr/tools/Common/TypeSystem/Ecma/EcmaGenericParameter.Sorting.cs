// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata.Ecma335;

namespace Internal.TypeSystem.Ecma
{
    // Functionality related to deterministic ordering of types
    partial class EcmaGenericParameter
    {
        protected internal override int ClassCode => -1548417824;

        protected internal override int CompareToImpl(TypeDesc other, TypeSystemComparer comparer)
        {
            var otherType = (EcmaGenericParameter)other;
            int result = _module.MetadataReader.GetToken(_handle) - otherType._module.MetadataReader.GetToken(otherType._handle);
            if (result != 0)
                return result;

            return _module.CompareTo(otherType._module);
        }
    }
}
