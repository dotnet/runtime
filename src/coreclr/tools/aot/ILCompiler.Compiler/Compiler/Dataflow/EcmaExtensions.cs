// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using ILLink.Shared.TypeSystemProxy;

using MethodAttributes = System.Reflection.MethodAttributes;
using FieldAttributes = System.Reflection.FieldAttributes;
using TypeAttributes = System.Reflection.TypeAttributes;
using Debug = System.Diagnostics.Debug;

namespace ILCompiler.Dataflow
{
    internal static class EcmaExtensions
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

        public static ReferenceKind ParameterReferenceKind(this MethodDesc method, int index)
        {
            if (!method.Signature.IsStatic)
            {
                if (index == 0)
                {
                    return method.OwningType.IsValueType ? ReferenceKind.Ref : ReferenceKind.None;
                }

                index--;
            }

            if (!method.Signature[index].IsByRef)
                return ReferenceKind.None;

            // Parameter metadata index 0 is for return parameter
            foreach (var parameterMetadata in method.GetParameterMetadata())
            {
                if (parameterMetadata.Index != index + 1)
                    continue;

                if (parameterMetadata.In)
                    return ReferenceKind.In;
                if (parameterMetadata.Out)
                    return ReferenceKind.Out;
                return ReferenceKind.Ref;
            }

            return ReferenceKind.None;
        }

        public static bool IsByRefOrPointer(this TypeDesc type)
            => type.IsByRef || type.IsPointer;

        public static TypeDesc GetOwningType(this TypeSystemEntity entity)
        {
            return entity switch
            {
                MethodDesc method => method.OwningType,
                FieldDesc field => field.OwningType,
                MetadataType type => type.ContainingType,
                PropertyPseudoDesc property => property.OwningType,
                EventPseudoDesc @event => @event.OwningType,
                _ => throw new NotImplementedException("Unexpected type system entity")
            };
        }
    }
}
