// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;
using CORINFO_DEVIRTUALIZATION_DETAIL = Internal.JitInterface.CORINFO_DEVIRTUALIZATION_DETAIL;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class DevirtualizationManager : ILCompiler.DevirtualizationManager
    {
        private readonly CompilationModuleGroup _compilationModuleGroup;

        public DevirtualizationManager(CompilationModuleGroup compilationModuleGroup)
        {
            _compilationModuleGroup = compilationModuleGroup;
        }

        public override bool IsEffectivelySealed(TypeDesc type)
        {
            return _compilationModuleGroup.VersionsWithType(type) && base.IsEffectivelySealed(type);
        }

        public override bool IsEffectivelySealed(MethodDesc method)
        {
            return _compilationModuleGroup.VersionsWithMethodBody(method) && base.IsEffectivelySealed(method);
        }

        protected override MethodDesc ResolveVirtualMethod(MethodDesc declMethod, DefType implType, out CORINFO_DEVIRTUALIZATION_DETAIL devirtualizationDetail)
        {
            devirtualizationDetail = CORINFO_DEVIRTUALIZATION_DETAIL.CORINFO_DEVIRTUALIZATION_UNKNOWN;

            // Versioning resiliency rules here are complex
            // Decl method checking
            // 1. If the declMethod is a class method, then we do not need to check if it is within the version bubble with a VersionsWithCode check
            //    but the metadata for the open definition must be within the bubble, or the decl method is in the direct parent type
            //    of a type which is in the version bubble relative to the implType.
            // 2. If the declMethod is an interface method, we can allow it if interface type is defined within the version
            //    bubble, or if the implementation type hierarchy is entirely within the version bubble (excluding System.Object and System.ValueType).
            // 3. At all times the declMethod must be representable as a token. That check is handled internally in the
            //    jit interface logic after the logic that executes here.
            //
            // ImplType checking
            // 1. At all times the metadata definition of the implementation type must version with the application.
            // 2. Additionally, the exact implementation type must be representable within the R2R image (this is checked via VersionsWithTypeReference
            //
            // Result method checking
            // 1. Ensure that the resolved result versions with the code, or is the decl method
            // 2. Devirtualizing to a default interface method is not currently considered to be useful, and how to check for version
            //    resilience has not yet been analyzed.
            // 3. When checking that the resolved result versions with the code, validate that all of the types
            //    From implType to the owning type of resolved result method also version with the code.

            bool declMethodCheckFailed;
            var firstTypeInImplTypeHierarchyNotInVersionBubble = FindVersionBubbleEdge(_compilationModuleGroup, implType, out TypeDesc lastTypeInHierarchyInVersionBubble);
            if (!declMethod.OwningType.IsInterface)
            {
                if (_compilationModuleGroup.VersionsWithType(declMethod.OwningType.GetTypeDefinition()))
                {
                    declMethodCheckFailed = false;
                }
                else
                {
                    if (firstTypeInImplTypeHierarchyNotInVersionBubble != declMethod.OwningType)
                    {
                        devirtualizationDetail = CORINFO_DEVIRTUALIZATION_DETAIL.CORINFO_DEVIRTUALIZATION_FAILED_BUBBLE_CLASS_DECL;
                        declMethodCheckFailed = true;
                    }
                    else
                    {
                        declMethodCheckFailed = false;
                    }
                }
            }
            else
            {
                if (_compilationModuleGroup.VersionsWithType(declMethod.OwningType.GetTypeDefinition()))
                {
                    declMethodCheckFailed = false;
                }
                else
                {
                    if (firstTypeInImplTypeHierarchyNotInVersionBubble == null || implType.IsValueType || firstTypeInImplTypeHierarchyNotInVersionBubble.IsObject)
                        declMethodCheckFailed = false;
                    else
                    {
                        devirtualizationDetail = CORINFO_DEVIRTUALIZATION_DETAIL.CORINFO_DEVIRTUALIZATION_FAILED_BUBBLE_INTERFACE_DECL;
                        declMethodCheckFailed = true;
                    }
                }
            }

            if (declMethodCheckFailed)
                return null;

            // Impl type check
            if (!_compilationModuleGroup.VersionsWithType(implType.GetTypeDefinition()))
            {
                devirtualizationDetail = CORINFO_DEVIRTUALIZATION_DETAIL.CORINFO_DEVIRTUALIZATION_FAILED_BUBBLE_IMPL;
                return null;
            }

            if (!_compilationModuleGroup.VersionsWithTypeReference(implType))
            {
                devirtualizationDetail = CORINFO_DEVIRTUALIZATION_DETAIL.CORINFO_DEVIRTUALIZATION_FAILED_BUBBLE_IMPL_NOT_REFERENCEABLE;
                return null;
            }

            /**
                * It is possible for us to hit a scenario where a type implements
                * the same interface more than once due to generic instantiations.
                *
                * In some instances of those cases, the VirtualMethodAlgorithm
                * does not produce identical output as CoreCLR would, leading to
                * behavioral differences in compiled outputs.
                *
                * Instead of fixing the algorithm (in which the work to fix it is
                * tracked in https://github.com/dotnet/corert/issues/208), the
                * following duplication detection algorithm will detect the case and
                * refuse to devirtualize for those scenarios.
                */
            if (declMethod.OwningType.IsInterface)
            {
                DefType[] implTypeRuntimeInterfaces = implType.RuntimeInterfaces;
                for (int i = 0; i < implTypeRuntimeInterfaces.Length; i++)
                {
                    for (int j = i + 1; j < implTypeRuntimeInterfaces.Length; j++)
                    {
                        if (implTypeRuntimeInterfaces[i] == implTypeRuntimeInterfaces[j])
                        {
                            devirtualizationDetail = CORINFO_DEVIRTUALIZATION_DETAIL.CORINFO_DEVIRTUALIZATION_FAILED_DUPLICATE_INTERFACE;
                            return null;
                        }
                    }
                }
            }


            if (declMethod.OwningType.IsInterface)
            {
                // Check for ComImport class, as we don't support devirtualization of ComImport classes
                // Run this check on all platforms, to avoid possible future versioning problems if we implement
                // COM on other architectures.
                if (!implType.IsObject)
                {
                    TypeDesc typeThatDerivesFromObject = implType;
                    while(!typeThatDerivesFromObject.BaseType.IsObject)
                    {
                        typeThatDerivesFromObject = typeThatDerivesFromObject.BaseType;
                    }

                    if (typeThatDerivesFromObject is Internal.TypeSystem.Ecma.EcmaType ecmaType)
                    {
                        if ((ecmaType.Attributes & System.Reflection.TypeAttributes.Import) != 0)
                        {
                            devirtualizationDetail = CORINFO_DEVIRTUALIZATION_DETAIL.CORINFO_DEVIRTUALIZATION_FAILED_COM;
                            return null;
                        }
                    }
                }
            }

            MethodDesc resolvedVirtualMethod = base.ResolveVirtualMethod(declMethod, implType, out devirtualizationDetail);

            if (resolvedVirtualMethod != null)
            {
                // Validate that the inheritance chain for resolution is within version bubble
                // The rule is somewhat tricky here.
                // If the resolved method is the declMethod, then only types which derive from the
                // OwningType of the decl method need to be within the version bubble.
                //
                // If not, then then all the types from the implType to the Owning type of the resolved
                // virtual method must be within the version bubble.
                if (firstTypeInImplTypeHierarchyNotInVersionBubble == null)
                {
                    // The entire type hierarchy of the implType is within the version bubble, and there is no more to check
                    return resolvedVirtualMethod;
                }

                if (declMethod == resolvedVirtualMethod && firstTypeInImplTypeHierarchyNotInVersionBubble == declMethod.OwningType)
                {
                    // Exact match for use of decl method check
                    return resolvedVirtualMethod;
                }

                // Ensure that declMethod is implemented on a type within the type hierarchy that is within the version bubble
                for (TypeDesc typeExamine = resolvedVirtualMethod.OwningType; typeExamine != null; typeExamine = typeExamine.BaseType)
                {
                    if (typeExamine == lastTypeInHierarchyInVersionBubble)
                    {
                        return resolvedVirtualMethod;
                    }
                }
                devirtualizationDetail = CORINFO_DEVIRTUALIZATION_DETAIL.CORINFO_DEVIRTUALIZATION_FAILED_BUBBLE;
            }

            // Cannot devirtualize, as we can't resolve to a target.
            return null;

            // This function returns the type where the metadata is not in the version bubble of the application, and has an out parameter
            // which is the last type examined before that is found via a base type walk.
            static TypeDesc FindVersionBubbleEdge(CompilationModuleGroup compilationModuleGroup, TypeDesc type, out TypeDesc lastTypeInVersionBubble)
            {
                lastTypeInVersionBubble = null;
                while (compilationModuleGroup.VersionsWithType(type.GetTypeDefinition()))
                {
                    lastTypeInVersionBubble = type;
                    type = type.BaseType;
                    if (type == null)
                        return null;
                }
                return type;
            }
        }
    }
}
