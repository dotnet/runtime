// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.TypeSystem.Ecma;
using Internal.TypeSystem;
using Internal.JitInterface;
using System.Reflection.Metadata;

namespace ILCompiler
{
    /// <summary>
    /// Roots all possibly-visible methods in the input IL module.
    /// </summary>
    public class ReadyToRunVisibilityRootProvider : ICompilationRootProvider
    {
        private EcmaModule _module;
        private InstructionSetSupport _instructionSetSupport;

        public ReadyToRunVisibilityRootProvider(EcmaModule module)
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
                    typeWithMethods = ReadyToRunLibraryRootProvider.InstantiateIfPossible(type);
                    if (typeWithMethods == null)
                        continue;
                }

                RootMethods(typeWithMethods, "Library module method", rootProvider, ((EcmaAssembly)_module.Assembly).HasAssemblyCustomAttribute("System.Runtime.CompilerServices", "InternalsVisibleToAttribute"));
            }

            if (_module.EntryPoint is not null)
            {
                rootProvider.AddCompilationRoot(_module.EntryPoint, rootMinimalDependencies: false, $"{_module.Assembly.GetName()} Main Method");
            }
        }

        private void RootMethods(MetadataType type, string reason, IRootingServiceProvider rootProvider, bool anyInternalsVisibleTo)
        {
            MethodImplRecord[] methodImplRecords = GetAllMethodImplRecordsForType((EcmaType)type.GetTypeDefinition());
            foreach (MethodDesc method in type.GetAllMethods())
            {
                // Skip methods with no IL
                if (method.IsAbstract)
                    continue;

                if (method.IsInternalCall)
                    continue;

                // If the method is not visible outside the assembly, then do not root the method.
                // It will be rooted by any callers that require it and do not inline it.
                if (!method.IsStaticConstructor
                    && method.GetTypicalMethodDefinition() is EcmaMethod ecma
                    && !ecma.GetEffectiveVisibility().IsExposedOutsideOfThisAssembly(anyInternalsVisibleTo))
                {
                    // If a method itself is not visible outside the assembly, but it implements a method that is,
                    // we want to root it as it could be called from outside the assembly.
                    // Since instance method overriding does not always require a MethodImpl record (it can be omitted when both the name and signature match)
                    // we will also root any methods that are virtual and do not have any MethodImpl records as it is difficult to determine all methods a method
                    // overrides or implements and we don't need to be perfect here.
                    bool anyMethodImplRecordsForMethod = false;
                    bool implementsOrOverridesVisibleMethod = false;
                    foreach (var record in methodImplRecords)
                    {
                        if (record.Body == ecma)
                        {
                            anyMethodImplRecordsForMethod = true;
                            implementsOrOverridesVisibleMethod = record.Decl.GetTypicalMethodDefinition() is EcmaMethod decl
                                && decl.GetEffectiveVisibility().IsExposedOutsideOfThisAssembly(anyInternalsVisibleTo);
                            if (implementsOrOverridesVisibleMethod)
                            {
                                break;
                            }
                        }
                    }
                    if (anyMethodImplRecordsForMethod && !implementsOrOverridesVisibleMethod)
                    {
                        continue;
                    }
                    if (!anyMethodImplRecordsForMethod && !method.IsVirtual)
                    {
                        continue;
                    }
                }

                MethodDesc methodToRoot = method;
                if (method.HasInstantiation)
                {
                    methodToRoot = ReadyToRunLibraryRootProvider.InstantiateIfPossible(method);

                    if (methodToRoot == null)
                        continue;
                }

                try
                {
                    if (!CorInfoImpl.ShouldSkipCompilation(_instructionSetSupport, method))
                    {
                        ReadyToRunLibraryRootProvider.CheckCanGenerateMethod(methodToRoot);
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

        private MethodImplRecord[] GetAllMethodImplRecordsForType(EcmaType type)
        {
            ArrayBuilder<MethodImplRecord> records = default;
            MetadataReader metadataReader = type.MetadataReader;
            TypeDefinition definition = metadataReader.GetTypeDefinition(type.Handle);

            foreach (var methodImplHandle in definition.GetMethodImplementations())
            {
                MethodImplementation methodImpl = metadataReader.GetMethodImplementation(methodImplHandle);

                records.Add(new MethodImplRecord(
                    _module.GetMethod(methodImpl.MethodDeclaration),
                   _module.GetMethod(methodImpl.MethodBody)
                ));
            }
            return records.ToArray();
        }

        /// <summary>
        /// Determine if the visibility-based root provider should be used for the given module.
        /// </summary>
        /// <param name="module">The module</param>
        /// <returns><c>true</c> if the module should use the visibility-based root provider; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// We will use a visibility-based root provider for modules that are marked as trimmable.
        /// Trimmable assemblies are less likely to use private reflection, which the visibility-based root provider
        /// doesn't track well.
        /// </remarks>
        public static bool UseVisibilityBasedRootProvider(EcmaModule module)
        {
            EcmaAssembly assembly = (EcmaAssembly)module.Assembly;

            foreach (var assemblyMetadata in assembly.GetDecodedCustomAttributes("System.Reflection", "AssemblyMetadataAttribute"))
            {
                if ((string)assemblyMetadata.FixedArguments[0].Value == "IsTrimmable")
                {
                    return bool.TryParse((string)assemblyMetadata.FixedArguments[1].Value, out bool isTrimmable) && isTrimmable;
                }
            }
            return false;
        }
    }
}
