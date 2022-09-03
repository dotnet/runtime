// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    /// <summary>
    /// Represents a metadata blocking policy that doesn't block any metadata.
    /// </summary>
    public sealed class NoMetadataBlockingPolicy : MetadataBlockingPolicy
    {
        public override bool IsBlocked(MetadataType type) => !(type is EcmaType);

        public override bool IsBlocked(FieldDesc field)
        {
            if (field is not EcmaField ecmaField)
                return true;

            // Avoid exposing the MethodTable field
            if (ecmaField.OwningType.IsObject)
                return true;

            return false;
        }

        private MetadataType _arrayOfTType;
        private MetadataType InitializeArrayOfTType(TypeSystemEntity contextEntity)
        {
            _arrayOfTType = contextEntity.Context.SystemModule.GetType("System", "Array`1");
            return _arrayOfTType;
        }
        private MetadataType GetArrayOfTType(TypeSystemEntity contextEntity)
        {
            if (_arrayOfTType != null)
            {
                return _arrayOfTType;
            }
            return InitializeArrayOfTType(contextEntity);
        }

        public override bool IsBlocked(MethodDesc method)
        {
            if (method is EcmaMethod ecmaMethod)
            {
                // Methods on Array`1<T> are implementation details that implement the generic interfaces on
                // arrays. They should not generate metadata or be reflection invokable.
                // We can get rid of this special casing if we make these methods stop being regular EcmaMethods
                // with Array<T> as their owning type
                if (ecmaMethod.OwningType == GetArrayOfTType(ecmaMethod))
                    return true;

                // Also don't expose the ValueType.__GetFieldOverride method.
                if (ecmaMethod.Name == Internal.IL.Stubs.ValueTypeGetFieldHelperMethodOverride.MetadataName
                    && ecmaMethod.OwningType.IsWellKnownType(WellKnownType.ValueType))
                    return true;

                return false;
            }

            return true;
        }
    }
}
