// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    /// <summary>
    /// A "PropertyDesc" to describe properties. Represents a property within the compiler.
    /// This is not a real type system entity. In particular, these are not interned.
    /// </summary>
    public class PropertyPseudoDesc : TypeSystemEntity
    {
        private readonly EcmaType _type;
        private readonly PropertyDefinitionHandle _handle;

        private PropertyDefinition Definition => _type.MetadataReader.GetPropertyDefinition(_handle);

        public PropertySignature Signature =>
            new EcmaSignatureParser(_type.EcmaModule, _type.MetadataReader.GetBlobReader(Definition.Signature), NotFoundBehavior.Throw)
            .ParsePropertySignature();

        public MethodDesc GetMethod
        {
            get
            {
                MethodDefinitionHandle getter = Definition.GetAccessors().Getter;
                return getter.IsNil ? null : _type.EcmaModule.GetMethod(getter);
            }
        }

        public MethodDesc SetMethod
        {
            get
            {
                MethodDefinitionHandle setter = Definition.GetAccessors().Setter;
                return setter.IsNil ? null : _type.EcmaModule.GetMethod(setter);
            }
        }

        public CustomAttributeHandleCollection GetCustomAttributes
        {
            get
            {
                return Definition.GetCustomAttributes();
            }
        }

        public MetadataType OwningType
        {
            get
            {
                return _type;
            }
        }

        public string Name
        {
            get
            {
                return _type.MetadataReader.GetString(Definition.Name);
            }
        }

        public PropertyPseudoDesc(EcmaType type, PropertyDefinitionHandle handle)
        {
            _type = type;
            _handle = handle;
        }

        public override TypeSystemContext Context => _type.Context;

        #region Do not use these
        public override bool Equals(object obj) => throw new NotImplementedException();
        public override int GetHashCode() => throw new NotImplementedException();
        public static bool operator ==(PropertyPseudoDesc a, PropertyPseudoDesc b) => throw new NotImplementedException();
        public static bool operator !=(PropertyPseudoDesc a, PropertyPseudoDesc b) => throw new NotImplementedException();
        #endregion
    }
}
