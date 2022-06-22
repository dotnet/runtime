// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.TypeSystem;

using ILCompiler.DependencyAnalysis;

using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;
using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public class RootingHelpers
    {
        public static bool TryRootType(IRootingServiceProvider rootProvider, TypeDesc type, string reason)
        {
            try
            {
                RootType(rootProvider, type, reason);
                return true;
            }
            catch (TypeSystemException)
            {
                return false;
            }
        }

        public static void RootType(IRootingServiceProvider rootProvider, TypeDesc type, string reason)
        {
            rootProvider.AddCompilationRoot(type, reason);

            InstantiatedType fallbackNonCanonicalOwningType = null;

            // Instantiate generic types over something that will be useful at runtime
            if (type.IsGenericDefinition)
            {
                Instantiation canonInst = TypeExtensions.GetInstantiationThatMeetsConstraints(type.Instantiation, allowCanon: true);
                if (canonInst.IsNull)
                    return;

                Instantiation concreteInst = TypeExtensions.GetInstantiationThatMeetsConstraints(type.Instantiation, allowCanon: false);
                if (!concreteInst.IsNull)
                    fallbackNonCanonicalOwningType = ((MetadataType)type).MakeInstantiatedType(concreteInst);

                type = ((MetadataType)type).MakeInstantiatedType(canonInst);

                rootProvider.AddCompilationRoot(type, reason);
            }

            // Also root base types. This is so that we make methods on the base types callable.
            // This helps in cases like "class Foo : Bar<int> { }" where we discover new
            // generic instantiations.
            TypeDesc baseType = type.BaseType;
            if (baseType != null)
            {
                RootType(rootProvider, baseType.NormalizeInstantiation(), reason);
            }

            if (type.IsDefType)
            {
                foreach (var method in type.GetMethods())
                {
                    if (method.HasInstantiation)
                    {
                        // Make a non-canonical instantiation.
                        // We currently have a file format limitation that requires generic methods to be concrete.
                        // A rooted canonical method body is not visible to the reflection mapping tables.
                        Instantiation inst = TypeExtensions.GetInstantiationThatMeetsConstraints(method.Instantiation, allowCanon: false);

                        if (inst.IsNull)
                        {
                            // Can't root anything useful
                        }
                        else if (!method.OwningType.IsCanonicalSubtype(CanonicalFormKind.Any))
                        {
                            // Owning type is not canonical, can use the instantiation directly.
                            TryRootMethod(rootProvider, method.MakeInstantiatedMethod(inst), reason);
                        }
                        else if (fallbackNonCanonicalOwningType != null)
                        {
                            // We have a fallback non-canonical type we can root a body on
                            MethodDesc alternateMethod = method.Context.GetMethodForInstantiatedType(method.GetTypicalMethodDefinition(), fallbackNonCanonicalOwningType);
                            TryRootMethod(rootProvider, alternateMethod.MakeInstantiatedMethod(inst), reason);
                        }
                    }
                    else
                    {
                        TryRootMethod(rootProvider, method, reason);
                    }
                }

                foreach (FieldDesc field in type.GetFields())
                {
                    TryRootField(rootProvider, field, reason);
                }
            }
        }

        public static bool TryRootMethod(IRootingServiceProvider rootProvider, MethodDesc method, string reason)
        {
            try
            {
                RootMethod(rootProvider, method, reason);
                return true;
            }
            catch (TypeSystemException)
            {
                return false;
            }
        }

        public static void RootMethod(IRootingServiceProvider rootProvider, MethodDesc method, string reason)
        {
            // Make sure we're not putting something into the graph that will crash later.
            LibraryRootProvider.CheckCanGenerateMethod(method);

            rootProvider.AddReflectionRoot(method, reason);
        }

        public static bool TryRootField(IRootingServiceProvider rootProvider, FieldDesc field, string reason)
        {
            try
            {
                RootField(rootProvider, field, reason);
                return true;
            }
            catch (TypeSystemException)
            {
                return false;
            }
        }

        public static void RootField(IRootingServiceProvider rootProvider, FieldDesc field, string reason)
        {
            // Make sure we're not putting something into the graph that will crash later.
            if (field.IsLiteral)
            {
                // Nothing to check
            }
            else if (field.IsStatic)
            {
                field.OwningType.ComputeStaticFieldLayout(StaticLayoutKind.StaticRegionSizes);
            }
            else
            {
                field.OwningType.ComputeInstanceLayout(InstanceLayoutKind.TypeOnly);
            }

            rootProvider.AddReflectionRoot(field, reason);
        }

        public static bool TryGetDependenciesForReflectedMethod(ref DependencyList dependencies, NodeFactory factory, MethodDesc method, string reason)
        {
            MethodDesc typicalMethod = method.GetTypicalMethodDefinition();
            if (factory.MetadataManager.IsReflectionBlocked(typicalMethod))
            {
                return false;
            }

            // If this is a generic method, make sure we at minimum have the metadata
            // for it. This hedges against the risk that we fail to figure out a code body
            // for it below.
            if (typicalMethod.IsGenericMethodDefinition || typicalMethod.OwningType.IsGenericDefinition)
            {
                dependencies ??= new DependencyList();
                dependencies.Add(factory.ReflectableMethod(typicalMethod), reason);
            }

            // If there's any genericness involved, try to create a fitting instantiation that would be usable at runtime.
            // This is not a complete solution to the problem.
            // If we ever decide that MakeGenericType/MakeGenericMethod should simply be considered unsafe, this code can be deleted
            // and instantiations that are not fully closed can be ignored.
            if (method.OwningType.IsGenericDefinition || method.OwningType.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable: true))
            {
                TypeDesc owningType = method.OwningType.GetTypeDefinition();
                Instantiation inst = TypeExtensions.GetInstantiationThatMeetsConstraints(owningType.Instantiation, allowCanon: !method.HasInstantiation);
                if (inst.IsNull)
                {
                    return false;
                }

                method = method.Context.GetMethodForInstantiatedType(
                    method.GetTypicalMethodDefinition(),
                    ((MetadataType)owningType).MakeInstantiatedType(inst));
            }

            if (method.IsGenericMethodDefinition || method.Instantiation.ContainsSignatureVariables())
            {
                method = method.GetMethodDefinition();

                Instantiation inst = TypeExtensions.GetInstantiationThatMeetsConstraints(method.Instantiation, allowCanon: false);
                if (inst.IsNull)
                {
                    return false;
                }

                method = method.MakeInstantiatedMethod(inst);
            }

            try
            {
                // Make sure we're not putting something into the graph that will crash later.
                LibraryRootProvider.CheckCanGenerateMethod(method);
            }
            catch (TypeSystemException)
            {
                return false;
            }

            dependencies ??= new DependencyList();
            dependencies.Add(factory.ReflectableMethod(method), reason);

            return true;
        }

        public static bool TryGetDependenciesForReflectedField(ref DependencyList dependencies, NodeFactory factory, FieldDesc field, string reason)
        {
            FieldDesc typicalField = field.GetTypicalFieldDefinition();
            if (factory.MetadataManager.IsReflectionBlocked(typicalField))
            {
                return false;
            }

            dependencies ??= new DependencyList();

            // If this is a field on generic type, make sure we at minimum have the metadata
            // for it. This hedges against the risk that we fail to figure out an instantiated base
            // for it below.
            if (typicalField.OwningType.HasInstantiation)
            {
                dependencies.Add(factory.ReflectableField(typicalField), reason);
            }

            // If there's any genericness involved, try to create a fitting instantiation that would be usable at runtime.
            // This is not a complete solution to the problem.
            // If we ever decide that MakeGenericType/MakeGenericMethod should simply be considered unsafe, this code can be deleted
            // and instantiations that are not fully closed can be ignored.
            if (field.OwningType.IsGenericDefinition || field.OwningType.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable: true))
            {
                TypeDesc owningType = field.OwningType.GetTypeDefinition();
                Instantiation inst = TypeExtensions.GetInstantiationThatMeetsConstraints(owningType.Instantiation, allowCanon: true);
                if (inst.IsNull)
                {
                    return false;
                }

                field = field.Context.GetFieldForInstantiatedType(
                    field.GetTypicalFieldDefinition(),
                    ((MetadataType)owningType).MakeInstantiatedType(inst));
            }

            dependencies.Add(factory.ReflectableField(field), reason);

            return true;
        }

        public static bool TryGetDependenciesForReflectedType(ref DependencyList dependencies, NodeFactory factory, TypeDesc type, string reason)
        {
            try
            {
                // Instantiations with signature variables are not helpful - just use the definition.
                if (type.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable: true))
                {
                    type = type.GetTypeDefinition();
                }

                if (factory.MetadataManager.IsReflectionBlocked(type))
                {
                    return false;
                }

                dependencies ??= new DependencyList();

                dependencies.Add(factory.MaximallyConstructableType(type), reason);

                // If there's any unknown genericness involved, try to create a fitting instantiation that would be usable at runtime.
                // This is not a complete solution to the problem.
                // If we ever decide that MakeGenericType/MakeGenericMethod should simply be considered unsafe, this code can be deleted
                // and instantiations that are not fully closed can be ignored.
                if (type.IsGenericDefinition)
                {
                    Instantiation inst = TypeExtensions.GetInstantiationThatMeetsConstraints(type.Instantiation, allowCanon: true);
                    if (!inst.IsNull)
                    {
                        dependencies.Add(factory.MaximallyConstructableType(((MetadataType)type).MakeInstantiatedType(inst)), reason);
                    }
                }
            }
            catch (TypeSystemException)
            {
                return false;
            }

            return true;
        }
    }
}
