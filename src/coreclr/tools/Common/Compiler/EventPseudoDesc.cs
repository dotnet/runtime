// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    /// <summary>
    /// An "EventDesc" to describe events. Represents an event within the compiler.
    /// This is not a real type system entity. In particular, these are not interned.
    /// </summary>
    public class EventPseudoDesc : TypeSystemEntity
    {
        private readonly EcmaType _type;
        private readonly EventDefinitionHandle _handle;

        private EventDefinition Definition => _type.MetadataReader.GetEventDefinition(_handle);

        public MethodDesc AddMethod
        {
            get
            {
                MethodDefinitionHandle adder = Definition.GetAccessors().Adder;
                return adder.IsNil ? null : _type.EcmaModule.GetMethod(adder);
            }
        }

        public MethodDesc RemoveMethod
        {
            get
            {
                MethodDefinitionHandle setter = Definition.GetAccessors().Remover;
                return setter.IsNil ? null : _type.EcmaModule.GetMethod(setter);
            }
        }

        public MethodDesc RaiseMethod
        {
            get
            {
                MethodDefinitionHandle raiser = Definition.GetAccessors().Raiser;
                return raiser.IsNil ? null : _type.EcmaModule.GetMethod(raiser);
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

        public EventPseudoDesc(EcmaType type, EventDefinitionHandle handle)
        {
            _type = type;
            _handle = handle;
        }

        public override TypeSystemContext Context => _type.Context;

        #region Do not use these
        public override bool Equals(object obj) => throw new NotImplementedException();
        public override int GetHashCode() => throw new NotImplementedException();
        public static bool operator ==(EventPseudoDesc a, EventPseudoDesc b) => throw new NotImplementedException();
        public static bool operator !=(EventPseudoDesc a, EventPseudoDesc b) => throw new NotImplementedException();
        #endregion
    }
}
