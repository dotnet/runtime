// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata.Ecma335;

namespace Internal.TypeSystem.Ecma
{
    // Functionality related to deterministic ordering of types and members
    public partial class EcmaField
    {
        protected internal override int ClassCode => 44626835;

        protected internal override int CompareToImpl(FieldDesc other, TypeSystemComparer comparer)
        {
            var otherField = (EcmaField)other;

            EcmaModule module = _type.EcmaModule;
            EcmaModule otherModule = otherField._type.EcmaModule;

            int result = module.MetadataReader.GetToken(_handle) - otherModule.MetadataReader.GetToken(otherField._handle);
            if (result != 0)
                return result;

            return module.CompareTo(otherModule);
        }
    }
}
