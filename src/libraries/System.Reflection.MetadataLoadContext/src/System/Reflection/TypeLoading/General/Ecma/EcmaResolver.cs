// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection.Metadata;

namespace System.Reflection.TypeLoading.Ecma
{
    /// <summary>
    /// These are the official entrypoints that code should use to resolve metadata tokens.
    /// </summary>
    internal static class EcmaResolver
    {
        public static RoType ResolveTypeDefRefOrSpec(this EntityHandle handle, EcmaModule module, in TypeContext typeContext)
        {
            Debug.Assert(!handle.IsNil);
            Debug.Assert(module != null);

            HandleKind kind = handle.Kind;
            switch (kind)
            {
                case HandleKind.TypeDefinition:
                    return ((TypeDefinitionHandle)handle).ResolveTypeDef(module);

                case HandleKind.TypeReference:
                    return ((TypeReferenceHandle)handle).ResolveTypeRef(module);

                case HandleKind.TypeSpecification:
                    return ((TypeSpecificationHandle)handle).ResolveTypeSpec(module, typeContext);

                default:
                    throw new BadImageFormatException();
            }
        }

        public static EcmaDefinitionType ResolveTypeDef(this TypeDefinitionHandle handle, EcmaModule module)
        {
            Debug.Assert(!handle.IsNil);
            Debug.Assert(module != null);

            return module.TypeDefTable.GetOrAdd(handle, module, s_resolveTypeDef);
        }

        private static readonly Func<EntityHandle, EcmaModule, EcmaDefinitionType> s_resolveTypeDef =
            (h, m) => new EcmaDefinitionType((TypeDefinitionHandle)h, m);

        public static RoDefinitionType ResolveTypeRef(this TypeReferenceHandle handle, EcmaModule module)
        {
            Debug.Assert(!handle.IsNil);
            Debug.Assert(module != null);

            return module.TypeRefTable.GetOrAdd(handle, module, s_resolveTypeRef);
        }

        private static readonly Func<EntityHandle, EcmaModule, RoDefinitionType> s_resolveTypeRef =
            (h, m) => ComputeTypeRefResolution((TypeReferenceHandle)h, m);

        private static RoDefinitionType ComputeTypeRefResolution(TypeReferenceHandle handle, EcmaModule module)
        {
            MetadataReader reader = module.Reader;
            TypeReference tr = handle.GetTypeReference(reader);
            ReadOnlySpan<byte> ns = tr.Namespace.AsReadOnlySpan(reader);
            ReadOnlySpan<byte> name = tr.Name.AsReadOnlySpan(reader);

            EntityHandle scope = tr.ResolutionScope;
            if (scope.IsNil)
            {
                // Special case for non-prime Modules - the type is somewhere in the Assembly. Technically, we're supposed
                // to walk the manifest module's ExportedType table for non-forwarder entries that have a matching name and 
                // namespace (Ecma-355 11.22.38).
                //
                // Pragmatically speaking, searching the entire assembly should get us the same result and avoids writing a significant
                // code path that will get almost no test coverage as this is an obscure case not produced by mainstream tools..
                RoDefinitionType type = module.GetEcmaAssembly().GetTypeCore(ns, name, ignoreCase: false, out Exception e);
                if (type == null)
                    throw e;
                return type;
            }

            HandleKind scopeKind = scope.Kind;
            switch (scopeKind)
            {
                case HandleKind.AssemblyReference:
                    {
                        AssemblyReferenceHandle arh = (AssemblyReferenceHandle)scope;
                        RoAssembly assembly = arh.ResolveAssembly(module);
                        RoDefinitionType type = assembly.GetTypeCore(ns, name, ignoreCase: false, out Exception e);
                        if (type == null)
                            throw e;
                        return type;
                    }

                case HandleKind.TypeReference:
                    {
                        RoDefinitionType outerType = ((TypeReferenceHandle)scope).ResolveTypeRef(module);
                        RoDefinitionType nestedType = outerType.GetNestedTypeCore(name);
                        return nestedType ?? throw new TypeLoadException(SR.Format(SR.Format(SR.TypeNotFound, outerType.ToString() + "[]", outerType.Assembly.FullName)));
                    }

                case HandleKind.ModuleDefinition:
                    {
                        RoDefinitionType type = module.GetTypeCore(ns, name, ignoreCase: false, out Exception e);
                        if (type == null)
                            throw e;
                        return type;
                    }

                case HandleKind.ModuleReference:
                    {
                        string moduleName = ((ModuleReferenceHandle)scope).GetModuleReference(module.Reader).Name.GetString(module.Reader);
                        RoModule targetModule = module.GetRoAssembly().GetRoModule(moduleName);
                        if (targetModule == null)
                            throw new BadImageFormatException(SR.Format(SR.BadImageFormat_TypeRefModuleNotInManifest, module.Assembly.FullName, $"0x{handle.GetToken():x8}"));

                        RoDefinitionType type = targetModule.GetTypeCore(ns, name, ignoreCase: false, out Exception e);
                        if (type == null)
                            throw e;
                        return type;
                    }

                default:
                    throw new BadImageFormatException(SR.Format(SR.BadImageFormat_TypeRefBadScopeType, module.Assembly.FullName, $"0x{handle.GetToken():x8}"));
            }
        }

        public static RoType ResolveTypeSpec(this TypeSpecificationHandle handle, EcmaModule module, in TypeContext typeContext)
        {
            Debug.Assert(!handle.IsNil);
            Debug.Assert(module != null);

            return handle.GetTypeSpecification(module.Reader).DecodeSignature(module, typeContext);
        }

        public static EcmaGenericParameterType ResolveGenericParameter(this GenericParameterHandle handle, EcmaModule module)
        {
            Debug.Assert(!handle.IsNil);
            Debug.Assert(module != null);

            return module.GenericParamTable.GetOrAdd(handle, module, s_resolveGenericParam);
        }

        private static readonly Func<EntityHandle, EcmaModule, EcmaGenericParameterType> s_resolveGenericParam =
            (EntityHandle h, EcmaModule module) =>
            {
                MetadataReader reader = module.Reader;
                GenericParameterHandle gph = (GenericParameterHandle)h;
                GenericParameter gp = gph.GetGenericParameter(reader);
                HandleKind parentKind = gp.Parent.Kind;
                switch (parentKind)
                {
                    case HandleKind.TypeDefinition:
                        return new EcmaGenericTypeParameterType(gph, module);
                    case HandleKind.MethodDefinition:
                        return new EcmaGenericMethodParameterType(gph, module);
                    default:
                        throw new BadImageFormatException(); // Not a legal token type to be found in a GenericParameter.Parent record.
                }
            };

        public static RoAssembly ResolveAssembly(this AssemblyReferenceHandle handle, EcmaModule module)
        {
            RoAssembly assembly = handle.TryResolveAssembly(module, out Exception e);
            if (assembly == null)
                throw e;
            return assembly;
        }

        public static RoAssembly TryResolveAssembly(this AssemblyReferenceHandle handle, EcmaModule module, out Exception e)
        {
            e = null;
            RoAssembly assembly = handle.ResolveToAssemblyOrExceptionAssembly(module);
            if (assembly is RoExceptionAssembly exceptionAssembly)
            {
                e = exceptionAssembly.Exception;
                return null;
            }
            return assembly;
        }

        public static RoAssembly ResolveToAssemblyOrExceptionAssembly(this AssemblyReferenceHandle handle, EcmaModule module)
        {
            return module.AssemblyRefTable.GetOrAdd(handle, module, s_resolveAssembly);
        }

        private static readonly Func<EntityHandle, EcmaModule, RoAssembly> s_resolveAssembly =
            (h, m) =>
            {
                RoAssemblyName roAssemblyName = ((AssemblyReferenceHandle)h).ToRoAssemblyName(m.Reader);
                return m.Loader.ResolveToAssemblyOrExceptionAssembly(roAssemblyName);
            };

        public static T ResolveMethod<T>(this MethodDefinitionHandle handle, EcmaModule module, in TypeContext typeContext) where T : MethodBase
        {
            MetadataReader reader = module.Reader;
            MethodDefinition methodDefinition = handle.GetMethodDefinition(reader);
            RoInstantiationProviderType declaringType = methodDefinition.GetDeclaringType().ResolveAndSpecializeType(module, typeContext);
            EcmaMethodDecoder decoder = new EcmaMethodDecoder(handle, module);
            if (methodDefinition.IsConstructor(reader))
                return (T)(object)(new RoDefinitionConstructor<EcmaMethodDecoder>(declaringType, decoder));
            else
                return (T)(object)(new RoDefinitionMethod<EcmaMethodDecoder>(declaringType, declaringType, decoder));
        }

        private static RoInstantiationProviderType ResolveAndSpecializeType(this TypeDefinitionHandle handle, EcmaModule module, in TypeContext typeContext)
        {
            RoDefinitionType declaringType = handle.ResolveTypeDef(module);
            if (typeContext.GenericTypeArguments != null && declaringType.IsGenericTypeDefinition)
                return declaringType.GetUniqueConstructedGenericType(typeContext.GenericTypeArguments);
            return declaringType;
        }
    }
}
