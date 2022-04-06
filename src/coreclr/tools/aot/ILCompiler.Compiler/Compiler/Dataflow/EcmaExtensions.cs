// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using MethodAttributes = System.Reflection.MethodAttributes;
using FieldAttributes = System.Reflection.FieldAttributes;
using TypeAttributes = System.Reflection.TypeAttributes;
using Debug = System.Diagnostics.Debug;

namespace ILCompiler.Dataflow
{
    static class EcmaExtensions
    {
        public static bool IsPublic(this MethodDesc method)
        {
            return method.GetTypicalMethodDefinition() is EcmaMethod ecmaMethod
                && (ecmaMethod.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;
        }

        public static bool IsPublic(this FieldDesc field)
        {
            return field.GetTypicalFieldDefinition() is EcmaField ecmaField
                && (ecmaField.Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public;  
        }

        public static bool IsPrivate(this MethodDesc method)
        {
            return method.GetTypicalMethodDefinition() is EcmaMethod ecmaMethod
                && (ecmaMethod.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private;
        }

        public static bool IsPrivate(this FieldDesc field)
        {
            return field.GetTypicalFieldDefinition() is EcmaField ecmaField
                && (ecmaField.Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Private;
        }

        public static bool IsNestedPublic(this MetadataType mdType)
        {
            return mdType.GetTypeDefinition() is EcmaType ecmaType
                && (ecmaType.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic;
        }

        public static PropertyPseudoDesc GetProperty(this MetadataType mdType, string name, PropertySignature? signature)
        {
            Debug.Assert(signature == null);

            var type = (EcmaType)mdType.GetTypeDefinition();
            var reader = type.MetadataReader;
            foreach (var propertyHandle in reader.GetTypeDefinition(type.Handle).GetProperties())
            {
                if (reader.StringComparer.Equals(reader.GetPropertyDefinition(propertyHandle).Name, name))
                {
                    return new PropertyPseudoDesc(type, propertyHandle);
                }
            }

            mdType = mdType.MetadataBaseType;
            if (mdType != null)
                return GetProperty(mdType, name, signature);

            return null;
        }

        public static PropertyPseudoDesc GetPropertyForAccessor(this MethodDesc accessor)
        {
            var ecmaAccessor = (EcmaMethod)accessor.GetTypicalMethodDefinition();
            var type = (EcmaType)ecmaAccessor.OwningType;
            var reader = type.MetadataReader;
            var module = type.EcmaModule;
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
    }
}
