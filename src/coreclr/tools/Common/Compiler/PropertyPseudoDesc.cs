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

        public PropertyDefinitionHandle Handle
        {
            get
            {
                return _handle;
            }
        }

        public PropertyPseudoDesc(EcmaType type, PropertyDefinitionHandle handle)
        {
            _type = type;
            _handle = handle;
        }

        public override TypeSystemContext Context => _type.Context;

        public override bool Equals(object obj) => obj is not PropertyPseudoDesc property ? false : this == property;

        public override int GetHashCode() => _type.GetHashCode() ^ _handle.GetHashCode();

        public static bool operator ==(PropertyPseudoDesc a, PropertyPseudoDesc b) => a._type == b._type && a._handle == b._handle;

        public static bool operator !=(PropertyPseudoDesc a, PropertyPseudoDesc b) => !(a == b);
    }
}
