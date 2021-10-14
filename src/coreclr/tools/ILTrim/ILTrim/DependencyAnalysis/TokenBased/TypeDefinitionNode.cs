// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;

using Internal.TypeSystem.Ecma;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents an entry in the TypeDef metadata table.
    /// </summary>
    public sealed class TypeDefinitionNode : TokenBasedNode
    {
        public TypeDefinitionNode(EcmaModule module, TypeDefinitionHandle handle)
            : base(module, handle)
        {
        }

        private TypeDefinitionHandle Handle => (TypeDefinitionHandle)_handle;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            yield return new DependencyListEntry(factory.ModuleDefinition(_module), "Owning module");

            TypeDefinition typeDef = _module.MetadataReader.GetTypeDefinition(Handle);
            if (!typeDef.BaseType.IsNil)
            {
                yield return new(factory.GetNodeForToken(_module, typeDef.BaseType), "Base type of a type");
            }

            foreach (var parameter in typeDef.GetGenericParameters())
            {
                yield return new(factory.GenericParameter(_module, parameter), "Generic parameter of type");
            }

            foreach (CustomAttributeHandle customAttribute in typeDef.GetCustomAttributes())
            {
                yield return new(factory.CustomAttribute(_module, customAttribute), "Custom attribute of a type");
            }

            if (typeDef.IsNested)
            {
                yield return new DependencyListEntry(factory.TypeDefinition(_module, typeDef.GetDeclaringType()), "Declaring type of a type");
            }

            if (typeDef.Attributes.HasFlag(TypeAttributes.SequentialLayout) || typeDef.Attributes.HasFlag(TypeAttributes.ExplicitLayout))
            {
                // TODO: Postpone marking instance fields on reference types until the type is allocated (i.e. until we have a ConstructedTypeNode for it in the system).
                foreach (var fieldHandle in typeDef.GetFields())
                {
                    var fieldDef = _module.MetadataReader.GetFieldDefinition(fieldHandle);
                    if (!fieldDef.Attributes.HasFlag(FieldAttributes.Static))
                    {
                        yield return new DependencyListEntry(factory.FieldDefinition(_module, fieldHandle), "Instance field of a type with sequential or explicit layout");
                    }
                }
            }

            var ecmaType = (EcmaType)_module.GetObject(_handle);
            if (ecmaType.IsValueType)
            {
                // It's difficult to track where a valuetype gets boxed so consider always constructed
                // for now (it's on par with IL Linker).
                yield return new(factory.ConstructedType(ecmaType), "Implicitly constructed valuetype");
            }
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;
            TypeDefinition typeDef = reader.GetTypeDefinition(Handle);

            var builder = writeContext.MetadataBuilder;

            if (typeDef.IsNested)
                builder.AddNestedType((TypeDefinitionHandle)writeContext.TokenMap.MapToken(Handle), (TypeDefinitionHandle)writeContext.TokenMap.MapToken(typeDef.GetDeclaringType()));

            // Adding PropertyMap entries when writing types ensures that the PropertyMap table has the same
            // order as the TypeDefinition table. This allows us to use the same logic in MapTypePropertyList
            // as we have for fields and methods. However, this depends on the properties being written in the
            // same order as their types which will only be the case if the input assembly had properties sorted
            // by type.
            // TODO: Make this work with properties that aren't sorted in the same order as the TypeDef table
            // (for example by sorting properties by type before emitting them, or by saving PropertyMap rows
            // in the same order as tehy were in the input assembly.)
            PropertyDefinitionHandle propertyHandle = writeContext.TokenMap.MapTypePropertyList(Handle);
            if (!propertyHandle.IsNil)
                builder.AddPropertyMap(Handle, propertyHandle);

            var typeDefHandle = builder.AddTypeDefinition(typeDef.Attributes,
                builder.GetOrAddString(reader.GetString(typeDef.Namespace)),
                builder.GetOrAddString(reader.GetString(typeDef.Name)),
                writeContext.TokenMap.MapToken(typeDef.BaseType),
                writeContext.TokenMap.MapTypeFieldList(Handle),
                writeContext.TokenMap.MapTypeMethodList(Handle));

            var typeLayout = typeDef.GetLayout();
            if (!typeLayout.IsDefault)
                builder.AddTypeLayout(typeDefHandle, (ushort)typeLayout.PackingSize, (uint)typeLayout.Size);

            return typeDefHandle;
        }

        public override string ToString()
        {
            MetadataReader reader = _module.MetadataReader;
            return reader.GetString(reader.GetTypeDefinition(Handle).Name);
        }
    }
}
