// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Metadata.NativeFormat.Writer;

using Cts = Internal.TypeSystem;
using Debug = System.Diagnostics.Debug;
using Ecma = System.Reflection.Metadata;
using MetadataTokens = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace ILCompiler.Metadata
{
    // Support for stripping the names of types and members from the metadata (see IMetadataPolicy.GeneratesNames).
    //
    // Names that are stripped are replaced with the empty string, which the metadata reader users treat as
    // "name not available" (ECMA-335 doesn't allow empty names for types, methods, fields, properties and events).
    // Records that only differ by their name would no longer be distinguishable by the writer, so the records get
    // a discriminator that is derived from the metadata token of the entity.
    internal partial class Transform<TPolicy>
    {
        private Cts.MetadataType _attributeType;

        private ConstantStringValue HandleName(string name, bool stripName)
            => HandleString(stripName ? string.Empty : name);

        // Types from modules that strip names are all placed into the root namespace.
        private NamespaceDefinition HandleNamespaceDefinition(Cts.MetadataType type)
            => HandleNamespaceDefinition(type.Module, StripsName(type) ? string.Empty : type.GetNamespace());

        private NamespaceReference HandleNamespaceReference(Cts.MetadataType type)
            => HandleNamespaceReference(type.Module, StripsName(type) ? string.Empty : type.GetNamespace());

        private bool IsAttributeType(Cts.TypeDesc type)
        {
            _attributeType ??= type.Context.SystemModule.GetType("System"u8, "Attribute"u8);
            return Cts.CastingHelper.CanCastTo(type, _attributeType);
        }

        private bool StripsName(Cts.MetadataType type)
        {
            Debug.Assert(type.IsTypeDefinition);
            return !_policy.GeneratesNames(type.Module);
        }

        private bool StripsName(Cts.MethodDesc method)
        {
            Cts.MetadataType owningType = (Cts.MetadataType)method.OwningType;
            if (_policy.GeneratesNames(owningType.Module))
                return false;

            // The runtime finds constructors by name.
            if (method.IsConstructor || method.IsStaticConstructor)
                return false;

            // The runtime finds the Invoke method of delegates by name.
            if (owningType.IsDelegate && method.GetName() == "Invoke")
                return false;

            // The runtime matches generic virtual methods (e.g. interface methods and their implementations)
            // by name and signature.
            if (method.IsVirtual && method.HasInstantiation)
                return false;

            return true;
        }

        private bool StripsName(Cts.FieldDesc field)
        {
            Cts.MetadataType owningType = (Cts.MetadataType)field.OwningType;
            if (_policy.GeneratesNames(owningType.Module))
                return false;

            // Enum.ToString, Enum.Parse and friends need the names of the enum members.
            if (field.IsLiteral && owningType.IsEnum)
                return false;

            // Named arguments of custom attributes are resolved by name.
            if (!field.IsStatic && IsAttributeType(owningType))
                return false;

            return true;
        }

        private bool StripsPropertyName(Cts.Ecma.EcmaType owningType)
        {
            // Named arguments of custom attributes are resolved by name.
            return !_policy.GeneratesNames(owningType.Module) && !IsAttributeType(owningType);
        }

        // The discriminators below only need to be unique within the entities that can be compared
        // by the writer, and be stable across compilations. Types are distinguished by their row number in
        // the module, and members by their position in the member list of the declaring type. This keeps members
        // with the same shape in different types poolable.

        private static int GetDiscriminator(Cts.MetadataType type)
            => type is Cts.Ecma.EcmaType ecmaType ? MetadataTokens.GetRowNumber(ecmaType.Handle) : 0;

        private static int GetDiscriminator(Cts.MethodDesc method)
        {
            Cts.Ecma.EcmaMethod ecmaMethod = (Cts.Ecma.EcmaMethod)method.GetTypicalMethodDefinition();
            Cts.Ecma.EcmaType owningType = (Cts.Ecma.EcmaType)ecmaMethod.OwningType;
            Ecma.MethodDefinitionHandleCollection.Enumerator methods
                = owningType.MetadataReader.GetTypeDefinition(owningType.Handle).GetMethods().GetEnumerator();
            methods.MoveNext();
            return MetadataTokens.GetRowNumber(ecmaMethod.Handle) - MetadataTokens.GetRowNumber(methods.Current) + 1;
        }

        private static int GetDiscriminator(Cts.FieldDesc field)
        {
            Cts.Ecma.EcmaField ecmaField = (Cts.Ecma.EcmaField)field.GetTypicalFieldDefinition();
            Cts.Ecma.EcmaType owningType = (Cts.Ecma.EcmaType)ecmaField.OwningType;
            Ecma.FieldDefinitionHandleCollection.Enumerator fields
                = owningType.MetadataReader.GetTypeDefinition(owningType.Handle).GetFields().GetEnumerator();
            fields.MoveNext();
            return MetadataTokens.GetRowNumber(ecmaField.Handle) - MetadataTokens.GetRowNumber(fields.Current) + 1;
        }

        private static int GetDiscriminator(Cts.Ecma.EcmaType owningType, Ecma.PropertyDefinitionHandle property)
        {
            Ecma.PropertyDefinitionHandleCollection.Enumerator properties
                = owningType.MetadataReader.GetTypeDefinition(owningType.Handle).GetProperties().GetEnumerator();
            properties.MoveNext();
            return MetadataTokens.GetRowNumber(property) - MetadataTokens.GetRowNumber(properties.Current) + 1;
        }

        private static int GetDiscriminator(Cts.Ecma.EcmaType owningType, Ecma.EventDefinitionHandle eventHandle)
        {
            Ecma.EventDefinitionHandleCollection.Enumerator events
                = owningType.MetadataReader.GetTypeDefinition(owningType.Handle).GetEvents().GetEnumerator();
            events.MoveNext();
            return MetadataTokens.GetRowNumber(eventHandle) - MetadataTokens.GetRowNumber(events.Current) + 1;
        }
    }
}
