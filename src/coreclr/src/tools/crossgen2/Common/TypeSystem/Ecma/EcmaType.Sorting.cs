// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata.Ecma335;

namespace Internal.TypeSystem.Ecma
{
    // Functionality related to determinstic ordering of types
    partial class EcmaType
    {
        protected internal override int ClassCode => 1340416537;

        protected internal override int CompareToImpl(TypeDesc other, TypeSystemComparer comparer)
        {
            var otherType = (EcmaType)other;
            int result = _module.MetadataReader.GetToken(_handle) - otherType._module.MetadataReader.GetToken(otherType._handle);
            if (result != 0)
                return result;

            return _module.CompareTo(otherType._module);
        }
    }
}
