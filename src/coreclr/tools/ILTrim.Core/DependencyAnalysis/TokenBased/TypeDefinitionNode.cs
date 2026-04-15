// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;

using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis
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
            DependencyList dependencies = new DependencyList();

            dependencies.Add(factory.ModuleDefinition(_module), "Owning module");

            TypeDefinition typeDef = _module.MetadataReader.GetTypeDefinition(Handle);
            if (!typeDef.BaseType.IsNil)
            {
                dependencies.Add(factory.GetNodeForTypeToken(_module, typeDef.BaseType), "Base type of a type");
            }

            foreach (var parameter in typeDef.GetGenericParameters())
            {
                dependencies.Add(factory.GenericParameter(_module, parameter), "Generic parameter of type");
            }

            CustomAttributeNode.AddDependenciesDueToCustomAttributes(ref dependencies, factory, _module, typeDef.GetCustomAttributes());

            if (typeDef.IsNested)
            {
                dependencies.Add(factory.TypeDefinition(_module, typeDef.GetDeclaringType()), "Declaring type of a type");
            }

            var type = _module.GetType(Handle);
            if (type.IsDelegate)
            {
                var invokeMethod = type.GetMethod("Invoke"u8, null) as EcmaMethod;
                if (invokeMethod != null)
                    dependencies.Add(factory.MethodDefinition(_module, invokeMethod.Handle), "Delegate invoke");

                var ctorMethod = type.GetMethod(".ctor"u8, null) as EcmaMethod;
                if (ctorMethod != null)
                    dependencies.Add(factory.MethodDefinition(_module, ctorMethod.Handle), "Delegate ctor");
            }

            if (type.IsEnum)
            {
                foreach (var field in typeDef.GetFields())
                {
                    dependencies.Add(factory.FieldDefinition(_module, field), "Field of enum type");
                }
            }

            if (type.GetStaticConstructor() is EcmaMethod cctor)
            {
                dependencies.Add(factory.MethodDefinition(_module, cctor.Handle), "Static constructor");
            }

            if (typeDef.Attributes.HasFlag(TypeAttributes.SequentialLayout) || typeDef.Attributes.HasFlag(TypeAttributes.ExplicitLayout))
            {
                // TODO: Postpone marking instance fields on reference types until the type is allocated (i.e. until we have a ConstructedTypeNode for it in the system).
                foreach (var fieldHandle in typeDef.GetFields())
                {
                    var fieldDef = _module.MetadataReader.GetFieldDefinition(fieldHandle);
                    if (!fieldDef.Attributes.HasFlag(FieldAttributes.Static))
                    {
                        dependencies.Add(factory.FieldDefinition(_module, fieldHandle), "Instance field of a type with sequential or explicit layout");
                    }
                }
            }

            var ecmaType = (EcmaType)_module.GetObject(_handle);
            if (ecmaType.IsValueType)
            {
                // It's difficult to track where a valuetype gets boxed so consider always constructed
                // for now (it's on par with IL Linker).
                dependencies.Add(factory.ConstructedType(ecmaType), "Implicitly constructed valuetype");
            }

            return dependencies;
        }

        public override bool HasConditionalStaticDependencies
        {
            get
            {
                return _module.MetadataReader.GetTypeDefinition(Handle).GetInterfaceImplementations().Count > 0;
            }
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory)
        {
            MetadataReader reader = _module.MetadataReader;
            TypeDefinition typeDef = reader.GetTypeDefinition(Handle);

            foreach (InterfaceImplementationHandle intfImplHandle in typeDef.GetInterfaceImplementations())
            {
                InterfaceImplementation intfImpl = reader.GetInterfaceImplementation(intfImplHandle);
                EcmaType interfaceType = _module.TryGetType(intfImpl.Interface)?.GetTypeDefinition() as EcmaType;
                if (interfaceType != null)
                {
                    yield return new(
                        factory.GetNodeForTypeToken(_module, intfImpl.Interface),
                        factory.InterfaceUse(interfaceType),
                        "Implemented interface");
                }
            }
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;
            TypeDefinition typeDef = reader.GetTypeDefinition(Handle);

            var builder = writeContext.MetadataBuilder;

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
                builder.AddPropertyMap((TypeDefinitionHandle)writeContext.TokenMap.MapToken(Handle), propertyHandle);

            EventDefinitionHandle eventHandle = writeContext.TokenMap.MapTypeEventList(Handle);
            if (!eventHandle.IsNil)
                builder.AddEventMap((TypeDefinitionHandle)writeContext.TokenMap.MapToken(Handle), eventHandle);

            TypeDefinitionHandle outputHandle = builder.AddTypeDefinition(typeDef.Attributes,
                builder.GetOrAddString(reader.GetString(typeDef.Namespace)),
                builder.GetOrAddString(reader.GetString(typeDef.Name)),
                writeContext.TokenMap.MapToken(typeDef.BaseType),
                writeContext.TokenMap.MapTypeFieldList(Handle),
                writeContext.TokenMap.MapTypeMethodList(Handle));

            if (typeDef.IsNested)
                builder.AddNestedType(outputHandle, (TypeDefinitionHandle)writeContext.TokenMap.MapToken(typeDef.GetDeclaringType()));

            var typeLayout = typeDef.GetLayout();
            if (!typeLayout.IsDefault)
                builder.AddTypeLayout(outputHandle, (ushort)typeLayout.PackingSize, (uint)typeLayout.Size);

            foreach (InterfaceImplementationHandle intfImplHandle in typeDef.GetInterfaceImplementations())
            {
                InterfaceImplementation intfImpl = reader.GetInterfaceImplementation(intfImplHandle);
                EcmaType interfaceType = _module.TryGetType(intfImpl.Interface)?.GetTypeDefinition() as EcmaType;
                if (interfaceType != null && writeContext.Factory.InterfaceUse(interfaceType).Marked)
                {
                    builder.AddInterfaceImplementation(outputHandle,
                        writeContext.TokenMap.MapToken(intfImpl.Interface));
                }
            }

            return outputHandle;
        }

        public override string ToString()
        {
            MetadataReader reader = _module.MetadataReader;
            return reader.GetString(reader.GetTypeDefinition(Handle).Name);
        }
    }
}
