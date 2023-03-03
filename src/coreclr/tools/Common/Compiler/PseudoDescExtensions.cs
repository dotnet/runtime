// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public static class PseudoDescExtensions
    {
        public static PropertyPseudoDesc GetPropertyForAccessor(this MethodDesc accessor)
        {
            if (accessor.GetTypicalMethodDefinition() is not EcmaMethod ecmaAccessor)
                return null;

            var type = (EcmaType)ecmaAccessor.OwningType;
            var reader = type.MetadataReader;
            foreach (var propertyHandle in reader.GetTypeDefinition(type.Handle).GetProperties())
            {
                var accessors = reader.GetPropertyDefinition(propertyHandle).GetAccessors();
                if (ecmaAccessor.Handle == accessors.Getter
                    || ecmaAccessor.Handle == accessors.Setter)
                {
                    return new PropertyPseudoDesc(type, propertyHandle);
                }
            }

            return null;
        }

        public static EventPseudoDesc GetEventForAccessor(this MethodDesc accessor)
        {
            if (accessor.GetTypicalMethodDefinition() is not EcmaMethod ecmaAccessor)
                return null;

            var type = (EcmaType)ecmaAccessor.OwningType;
            var reader = type.MetadataReader;
            foreach (var eventHandle in reader.GetTypeDefinition(type.Handle).GetEvents())
            {
                var accessors = reader.GetEventDefinition(eventHandle).GetAccessors();
                if (ecmaAccessor.Handle == accessors.Adder
                    || ecmaAccessor.Handle == accessors.Remover
                    || ecmaAccessor.Handle == accessors.Raiser)
                {
                    return new EventPseudoDesc(type, eventHandle);
                }
            }

            return null;
        }
    }
}
