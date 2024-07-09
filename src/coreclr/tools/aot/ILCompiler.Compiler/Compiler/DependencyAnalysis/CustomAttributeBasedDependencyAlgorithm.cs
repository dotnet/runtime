// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;
using MethodAttributes = System.Reflection.MethodAttributes;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Computes the list of dependencies that are necessary to generate metadata for a custom attribute, but also the dependencies to
    /// make the custom attributes usable by the reflection stack at runtime.
    /// </summary>
    internal static class CustomAttributeBasedDependencyAlgorithm
    {
        public static bool CanOptimizeAttributeArtifacts(NodeFactory factory) => factory.CompilationModuleGroup.IsSingleFileCompilation;

        public static void AddDependenciesDueToCustomAttributes(ref DependencyList dependencies, NodeFactory factory, EcmaMethod method)
        {
            MetadataReader reader = method.MetadataReader;
            MethodDefinitionHandle methodHandle = method.Handle;
            MethodDefinition methodDef = reader.GetMethodDefinition(methodHandle);

            // Handle custom attributes on the method
            AddDependenciesDueToCustomAttributes(ref dependencies, factory, method.Module, methodDef.GetCustomAttributes(), method);

            // Handle custom attributes on method parameters
            foreach (ParameterHandle parameterHandle in methodDef.GetParameters())
            {
                Parameter parameter = reader.GetParameter(parameterHandle);
                AddDependenciesDueToCustomAttributes(ref dependencies, factory, method.Module, parameter.GetCustomAttributes(), method);
            }

            // Handle custom attributes on generic method parameters
            foreach (GenericParameterHandle genericParameterHandle in methodDef.GetGenericParameters())
            {
                GenericParameter parameter = reader.GetGenericParameter(genericParameterHandle);
                AddDependenciesDueToCustomAttributes(ref dependencies, factory, method.Module, parameter.GetCustomAttributes(), method);
            }

            // We don't model properties and events as separate entities within the compiler, so ensuring
            // we can generate custom attributes for the associated events and properties from here
            // is as good as any other place.
            //
            // As a performance optimization, we look for associated events and properties only
            // if the method is SpecialName. This is required for CLS compliance and compilers we
            // care about emit accessors like this.
            if ((methodDef.Attributes & MethodAttributes.SpecialName) != 0)
            {
                TypeDefinition declaringType = reader.GetTypeDefinition(methodDef.GetDeclaringType());

                foreach (PropertyDefinitionHandle propertyHandle in declaringType.GetProperties())
                {
                    PropertyDefinition property = reader.GetPropertyDefinition(propertyHandle);
                    PropertyAccessors accessors = property.GetAccessors();

                    if (accessors.Getter == methodHandle || accessors.Setter == methodHandle)
                        AddDependenciesDueToCustomAttributes(ref dependencies, factory, method.Module, property.GetCustomAttributes(), new PropertyPseudoDesc((EcmaType)method.OwningType, propertyHandle));
                }

                foreach (EventDefinitionHandle eventHandle in declaringType.GetEvents())
                {
                    EventDefinition @event = reader.GetEventDefinition(eventHandle);
                    EventAccessors accessors = @event.GetAccessors();

                    if (accessors.Adder == methodHandle || accessors.Remover == methodHandle || accessors.Raiser == methodHandle)
                        AddDependenciesDueToCustomAttributes(ref dependencies, factory, method.Module, @event.GetCustomAttributes(), new EventPseudoDesc((EcmaType)method.OwningType, eventHandle));
                }
            }
        }

        public static void AddDependenciesDueToCustomAttributes(ref DependencyList dependencies, NodeFactory factory, EcmaType type)
        {
            MetadataReader reader = type.MetadataReader;
            TypeDefinition typeDef = reader.GetTypeDefinition(type.Handle);
            AddDependenciesDueToCustomAttributes(ref dependencies, factory, type.EcmaModule, typeDef.GetCustomAttributes(), type);

            // Handle custom attributes on generic type parameters
            foreach (GenericParameterHandle genericParameterHandle in typeDef.GetGenericParameters())
            {
                GenericParameter parameter = reader.GetGenericParameter(genericParameterHandle);
                AddDependenciesDueToCustomAttributes(ref dependencies, factory, type.EcmaModule, parameter.GetCustomAttributes(), type);
            }
        }

        public static void AddDependenciesDueToCustomAttributes(ref DependencyList dependencies, NodeFactory factory, EcmaField field)
        {
            FieldDefinition fieldDef = field.MetadataReader.GetFieldDefinition(field.Handle);
            AddDependenciesDueToCustomAttributes(ref dependencies, factory, field.Module, fieldDef.GetCustomAttributes(), field);
        }

        public static void AddDependenciesDueToCustomAttributes(ref DependencyList dependencies, NodeFactory factory, EcmaAssembly assembly)
        {
            AssemblyDefinition asmDef = assembly.MetadataReader.GetAssemblyDefinition();
            AddDependenciesDueToCustomAttributes(ref dependencies, factory, assembly, asmDef.GetCustomAttributes(), assembly);

            ModuleDefinition moduleDef = assembly.MetadataReader.GetModuleDefinition();
            AddDependenciesDueToCustomAttributes(ref dependencies, factory, assembly, moduleDef.GetCustomAttributes(), assembly);
        }

        private static void AddDependenciesDueToCustomAttributes(ref DependencyList dependencies, NodeFactory factory, EcmaModule module, CustomAttributeHandleCollection attributeHandles, TypeSystemEntity parent)
        {
            MetadataReader reader = module.MetadataReader;
            var mdManager = (UsageBasedMetadataManager)factory.MetadataManager;
            var attributeTypeProvider = new CustomAttributeTypeProvider(module);

            bool metadataOnlyDependencies = CanOptimizeAttributeArtifacts(factory);

            foreach (CustomAttributeHandle caHandle in attributeHandles)
            {
                CustomAttribute attribute = reader.GetCustomAttribute(caHandle);

                try
                {
                    MethodDesc constructor = module.GetMethod(attribute.Constructor);

                    if (!mdManager.GeneratesAttributeMetadata(constructor.OwningType))
                        continue;

                    if (mdManager.IsReflectionBlocked(constructor))
                        continue;

                    CustomAttributeValue<TypeDesc> decodedValue = attribute.DecodeValue(attributeTypeProvider);

                    // Make a new list in case we need to abort.
                    var caDependencies = factory.MetadataManager.GetDependenciesForCustomAttribute(factory, constructor, decodedValue, parent) ?? new DependencyList();

                    if (metadataOnlyDependencies)
                        caDependencies.Add(factory.MethodMetadata(constructor.GetTypicalMethodDefinition()), "Attribute constructor");
                    else
                        caDependencies.Add(factory.ReflectedMethod(constructor.GetCanonMethodTarget(CanonicalFormKind.Specific)), "Attribute constructor");

                    if (AddDependenciesFromCustomAttributeBlob(caDependencies, factory, constructor.OwningType, decodedValue, metadataOnlyDependencies))
                    {
                        dependencies ??= new DependencyList();
                        dependencies.AddRange(caDependencies);
                        dependencies.Add(factory.CustomAttributeMetadata(new ReflectableCustomAttribute(module, caHandle)), "Attribute metadata");
                    }
                }
                catch (TypeSystemException)
                {
                    // We could end up seeing an exception here for a multitude of reasons:
                    // * Attribute ctor doesn't resolve
                    // * There's a typeof() that refers to something that can't be loaded
                    // * Attribute refers to a non-existing field
                    // * Etc.
                    //
                    // If we really wanted to, we could probably come up with a way to still make this
                    // work with the same failure modes at runtime as the CLR, but it might not be
                    // worth the hassle: the input was invalid. The most important thing is that we
                    // don't crash the compilation.
                }
                catch (BadImageFormatException)
                {
                    // System.Reflection.Metadata will throw BadImageFormatException if the blob is malformed.
                    // This can happen if e.g. underlying type of an enum changes between versions and
                    // we can no longer decode the custom attribute blob.
                }
            }
        }

        public static void AddDependenciesDueToCustomAttributeActivation(ref DependencyList dependencies, NodeFactory factory, EcmaModule module, CustomAttributeHandle attributeHandle)
        {
            MetadataReader reader = module.MetadataReader;
            CustomAttribute attribute = reader.GetCustomAttribute(attributeHandle);

            dependencies ??= new DependencyList();

            MethodDesc constructor = module.GetMethod(attribute.Constructor);
            dependencies.Add(factory.ReflectedMethod(constructor.GetCanonMethodTarget(CanonicalFormKind.Specific)), "Attribute constructor");

            var attributeTypeProvider = new CustomAttributeTypeProvider(module);

            CustomAttributeValue<TypeDesc> decodedValue = attribute.DecodeValue(attributeTypeProvider);
            AddDependenciesFromCustomAttributeBlob(dependencies, factory, constructor.OwningType, decodedValue, metadataOnly: false);
        }

        private static bool AddDependenciesFromCustomAttributeBlob(DependencyList dependencies, NodeFactory factory, TypeDesc attributeType, CustomAttributeValue<TypeDesc> value, bool metadataOnly)
        {
            foreach (CustomAttributeTypedArgument<TypeDesc> decodedArgument in value.FixedArguments)
            {
                if (!AddDependenciesFromCustomAttributeArgument(dependencies, factory, decodedArgument.Type, decodedArgument.Value, metadataOnly))
                    return false;
            }

            foreach (CustomAttributeNamedArgument<TypeDesc> decodedArgument in value.NamedArguments)
            {
                if (decodedArgument.Kind == CustomAttributeNamedArgumentKind.Field)
                {
                    if (!AddDependenciesFromField(dependencies, factory, attributeType, decodedArgument.Name, metadataOnly))
                        return false;
                }
                else
                {
                    Debug.Assert(decodedArgument.Kind == CustomAttributeNamedArgumentKind.Property);

                    // Reflection will need to reflection-invoke the setter at runtime.
                    if (!AddDependenciesFromPropertySetter(dependencies, factory, attributeType, decodedArgument.Name, metadataOnly))
                        return false;
                }

                if (!AddDependenciesFromCustomAttributeArgument(dependencies, factory, decodedArgument.Type, decodedArgument.Value, metadataOnly))
                    return false;
            }

            return true;
        }

        private static bool AddDependenciesFromField(DependencyList dependencies, NodeFactory factory, TypeDesc attributeType, string fieldName, bool metadataOnly)
        {
            FieldDesc field = attributeType.GetField(fieldName);
            if (field is not null)
            {
                if (factory.MetadataManager.IsReflectionBlocked(field))
                    return false;

                if (metadataOnly)
                    dependencies.Add(factory.FieldMetadata(field), "Custom attribute blob");
                else
                    dependencies.Add(factory.ReflectedField(field), "Custom attribute blob");

                return true;
            }

            // Haven't found it in current type. Check the base type.
            TypeDesc baseType = attributeType.BaseType;

            if (baseType != null)
                return AddDependenciesFromField(dependencies, factory, baseType, fieldName, metadataOnly);

            // Not found. This is bad metadata that will result in a runtime failure, but we shouldn't fail the compilation.
            return true;
        }

        private static bool AddDependenciesFromPropertySetter(DependencyList dependencies, NodeFactory factory, TypeDesc attributeType, string propertyName, bool metadataOnly)
        {
            EcmaType attributeTypeDefinition = (EcmaType)attributeType.GetTypeDefinition();

            MetadataReader reader = attributeTypeDefinition.MetadataReader;
            var typeDefinition = reader.GetTypeDefinition(attributeTypeDefinition.Handle);

            foreach (PropertyDefinitionHandle propDefHandle in typeDefinition.GetProperties())
            {
                PropertyDefinition propDef = reader.GetPropertyDefinition(propDefHandle);
                if (reader.StringComparer.Equals(propDef.Name, propertyName))
                {
                    PropertyAccessors accessors = propDef.GetAccessors();

                    if (!accessors.Setter.IsNil)
                    {
                        MethodDesc setterMethod = (MethodDesc)attributeTypeDefinition.EcmaModule.GetObject(accessors.Setter);
                        if (factory.MetadataManager.IsReflectionBlocked(setterMethod))
                            return false;

                        // Method on a generic attribute
                        if (attributeType != attributeTypeDefinition)
                        {
                            setterMethod = factory.TypeSystemContext.GetMethodForInstantiatedType(setterMethod, (InstantiatedType)attributeType);
                        }

                        if (metadataOnly)
                            dependencies.Add(factory.MethodMetadata(setterMethod.GetTypicalMethodDefinition()), "Custom attribute blob");
                        else
                            dependencies.Add(factory.ReflectedMethod(setterMethod.GetCanonMethodTarget(CanonicalFormKind.Specific)), "Custom attribute blob");
                    }

                    return true;
                }
            }

            // Haven't found it in current type. Check the base type.
            TypeDesc baseType = attributeType.BaseType;

            if (baseType != null)
                return AddDependenciesFromPropertySetter(dependencies, factory, baseType, propertyName, metadataOnly);

            // Not found. This is bad metadata that will result in a runtime failure, but we shouldn't fail the compilation.
            return true;
        }

        private static bool AddDependenciesFromCustomAttributeArgument(DependencyList dependencies, NodeFactory factory, TypeDesc type, object value, bool metadataOnly)
        {
            // If this is an initializer that refers to e.g. a blocked enum, we can't encode this attribute.
            if (factory.MetadataManager.IsReflectionBlocked(type))
                return false;

            // Reflection will need to be able to allocate this type at runtime
            // (e.g. this could be an array that needs to be allocated, or an enum that needs to be boxed).
            if (metadataOnly)
                TypeMetadataNode.GetMetadataDependencies(ref dependencies, factory, type, "Custom attribute blob");
            else
                dependencies.Add(factory.ReflectedType(type), "Custom attribute blob");

            if (type.UnderlyingType.IsPrimitive || type.IsString || value == null)
                return true;

            if (type.IsSzArray)
            {
                TypeDesc elementType = ((ArrayType)type).ElementType;
                if (elementType.UnderlyingType.IsPrimitive || elementType.IsString)
                    return true;

                foreach (CustomAttributeTypedArgument<TypeDesc> arrayElement in (ImmutableArray<CustomAttributeTypedArgument<TypeDesc>>)value)
                {
                    if (!AddDependenciesFromCustomAttributeArgument(dependencies, factory, arrayElement.Type, arrayElement.Value, metadataOnly))
                        return false;
                }

                return true;
            }

            // typeof() should be the only remaining option.

            Debug.Assert(value is TypeDesc);

            TypeDesc typeofType = (TypeDesc)value;

            if (factory.MetadataManager.IsReflectionBlocked(typeofType))
                return false;

            // Grab the metadata nodes that will be necessary to represent the typeof in the metadata blob
            TypeMetadataNode.GetMetadataDependencies(ref dependencies, factory, typeofType, "Custom attribute blob");
            return true;
        }
    }
}
