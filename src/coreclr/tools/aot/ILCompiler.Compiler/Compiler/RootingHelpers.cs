// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using ILCompiler.DependencyAnalysis;

using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;

namespace ILCompiler
{
    public class RootingHelpers
    {
        public static bool TryRootType(IRootingServiceProvider rootProvider, TypeDesc type, bool rootBaseTypes, string reason)
        {
            try
            {
                RootType(rootProvider, type, rootBaseTypes, reason);
                return true;
            }
            catch (TypeSystemException)
            {
                return false;
            }
        }

        public static void RootType(IRootingServiceProvider rootProvider, TypeDesc type, bool rootBaseTypes, string reason)
        {
            rootProvider.AddReflectionRoot(type, reason);

            // Instantiate generic types over something that will be useful at runtime
            if (type.IsGenericDefinition)
            {
                Instantiation inst = TypeExtensions.GetInstantiationThatMeetsConstraints(type.Instantiation, allowCanon: true);
                if (inst.IsNull)
                    return;

                type = ((MetadataType)type).MakeInstantiatedType(inst);

                rootProvider.AddReflectionRoot(type, reason);
            }

            if (rootBaseTypes)
            {
                TypeDesc baseType = type.BaseType;
                if (baseType != null)
                {
                    RootType(rootProvider, baseType.NormalizeInstantiation(), rootBaseTypes, reason);
                }
            }

            if (type.IsDefType)
            {
                foreach (var method in type.ConvertToCanonForm(CanonicalFormKind.Specific).GetMethods())
                {
                    if (method.HasInstantiation)
                    {
                        Instantiation inst = TypeExtensions.GetInstantiationThatMeetsConstraints(method.Instantiation, allowCanon: true);
                        if (inst.IsNull)
                            continue;

                        TryRootMethod(rootProvider, method.MakeInstantiatedMethod(inst), reason);
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
                dependencies.Add(factory.ReflectedMethod(typicalMethod), reason);
            }

            // If there's any genericness involved, try to create a fitting instantiation that would be usable at runtime.
            // This is not a complete solution to the problem.
            // If we ever decide that MakeGenericType/MakeGenericMethod should simply be considered unsafe, this code can be deleted
            // and instantiations that are not fully closed can be ignored.
            if (method.OwningType.IsGenericDefinition || method.OwningType.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable: true))
            {
                TypeDesc owningType = method.OwningType.GetTypeDefinition();
                Instantiation inst = TypeExtensions.GetInstantiationThatMeetsConstraints(owningType.Instantiation, allowCanon: true);
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

                Instantiation inst = TypeExtensions.GetInstantiationThatMeetsConstraints(method.Instantiation, allowCanon: true);
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
            dependencies.Add(factory.ReflectedMethod(method.GetCanonMethodTarget(CanonicalFormKind.Specific)), reason);

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
                dependencies.Add(factory.ReflectedField(typicalField), reason);
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

            dependencies.Add(factory.ReflectedField(field), reason);

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

                dependencies.Add(factory.ReflectedType(type), reason);

                // If there's any unknown genericness involved, try to create a fitting instantiation that would be usable at runtime.
                // This is not a complete solution to the problem.
                // If we ever decide that MakeGenericType/MakeGenericMethod should simply be considered unsafe, this code can be deleted
                // and instantiations that are not fully closed can be ignored.
                if (type.IsGenericDefinition)
                {
                    Instantiation inst = TypeExtensions.GetInstantiationThatMeetsConstraints(type.Instantiation, allowCanon: true);
                    if (!inst.IsNull)
                    {
                        dependencies.Add(factory.ReflectedType(((MetadataType)type).MakeInstantiatedType(inst)), reason);
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
