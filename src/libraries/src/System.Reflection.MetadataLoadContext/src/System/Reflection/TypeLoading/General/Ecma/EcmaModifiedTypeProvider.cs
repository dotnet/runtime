// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.TypeLoading.Ecma
{
    //
    // A special purpose ISignatureTypeProvider for the GetCustomModifiers() api. Unlike the normal one, this one produces RoModifiedTypes
    // when custom modifiers are present.
    //
    internal sealed class EcmaModifiedTypeProvider : EcmaWrappedTypeProvider
    {
        internal EcmaModifiedTypeProvider(EcmaModule module)
            : base(module)
        {
        }

        public sealed override RoType GetModifiedType(RoType modifier, RoType unmodifiedType, bool isRequired) => new RoModifiedType(modifier.SkipTypeWrappers(), unmodifiedType, isRequired);
        public sealed override RoType GetPinnedType(RoType elementType) => elementType;
    }
}
