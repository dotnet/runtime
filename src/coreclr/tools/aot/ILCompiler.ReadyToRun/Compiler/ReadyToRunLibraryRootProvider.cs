// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.TypeSystem.Ecma;
using Internal.TypeSystem;
using Internal.JitInterface;

namespace ILCompiler
{
    /// <summary>
    /// Roots all methods in the input IL module.
    /// </summary>
    public class ReadyToRunLibraryRootProvider : ICompilationRootProvider
    {
        private EcmaModule _module;
        private InstructionSetSupport _instructionSetSupport;

        public ReadyToRunLibraryRootProvider(EcmaModule module)
        {
            _module = module;
            _instructionSetSupport = ((ReadyToRunCompilerContext)module.Context).InstructionSetSupport;
        }

        public void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            foreach (MetadataType type in _module.GetAllTypes())
            {
                MetadataType typeWithMethods = type;
                if (type.HasInstantiation)
                {
                    typeWithMethods = InstantiateIfPossible(type);
                    if (typeWithMethods == null)
                        continue;
                }

                RootMethods(typeWithMethods, "Library module method", rootProvider);
            }
        }

        private void RootMethods(MetadataType type, string reason, IRootingServiceProvider rootProvider)
        {
            foreach (MethodDesc method in type.GetAllMethods())
            {
                // Skip methods with no IL
                if (method.IsAbstract)
                    continue;

                if (method.IsInternalCall)
                    continue;

                MethodDesc methodToRoot = method;
                if (method.HasInstantiation)
                {
                    methodToRoot = InstantiateIfPossible(method);

                    if (methodToRoot == null)
                        continue;
                }

                try
                {
                    if (!CorInfoImpl.ShouldSkipCompilation(_instructionSetSupport, method))
                    {
                        DependencyAnalysis.NodeFactory.CheckCanGenerateMethod(methodToRoot);
                        rootProvider.AddCompilationRoot(methodToRoot, rootMinimalDependencies: false, reason: reason);
                    }
                }
                catch (TypeSystemException)
                {
                    // Individual methods can fail to load types referenced in their signatures.
                    // Skip them in library mode since they're not going to be callable.
                    continue;
                }
            }
        }

        private static Instantiation GetInstantiationThatMeetsConstraints(Instantiation definition)
        {
            TypeDesc[] args = new TypeDesc[definition.Length];

            for (int i = 0; i < definition.Length; i++)
            {
                GenericParameterDesc genericParameter = (GenericParameterDesc)definition[i];

                // If the parameter is not constrained to be a valuetype, we can instantiate over __Canon
                if (genericParameter.HasNotNullableValueTypeConstraint)
                {
                    return default;
                }

                args[i] = genericParameter.Context.CanonType;
            }

            return new Instantiation(args);
        }

        public static InstantiatedType InstantiateIfPossible(MetadataType type)
        {
            Instantiation inst = GetInstantiationThatMeetsConstraints(type.Instantiation);
            if (inst.IsNull)
            {
                return null;
            }

            return type.MakeInstantiatedType(inst);
        }

        public static MethodDesc InstantiateIfPossible(MethodDesc method)
        {
            Instantiation inst = GetInstantiationThatMeetsConstraints(method.Instantiation);
            if (inst.IsNull)
            {
                return null;
            }

            return method.MakeInstantiatedMethod(inst);
        }
    }
}
