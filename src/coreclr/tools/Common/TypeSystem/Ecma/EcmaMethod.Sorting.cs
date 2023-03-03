// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata.Ecma335;

namespace Internal.TypeSystem.Ecma
{
    // Functionality related to deterministic ordering of types
    public partial class EcmaMethod
    {
        protected internal override int ClassCode => 1419431046;

        protected internal override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            var otherMethod = (EcmaMethod)other;

            EcmaModule module = _type.EcmaModule;
            EcmaModule otherModule = otherMethod._type.EcmaModule;

            // Sort by module in preference to by token. This will place methods of the same type near each other
            // even when working with several modules
            int result = module.CompareTo(otherModule);
            if (result != 0)
                return result;

            return module.MetadataReader.GetToken(_handle) - otherModule.MetadataReader.GetToken(otherMethod._handle);
        }
    }
}
