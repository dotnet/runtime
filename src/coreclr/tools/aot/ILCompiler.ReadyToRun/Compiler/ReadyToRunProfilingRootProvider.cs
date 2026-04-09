// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Internal.JitInterface;
using Internal.TypeSystem.Ecma;
using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// Roots all methods in the profile data in the module.
    /// </summary>
    public class ReadyToRunProfilingRootProvider : ICompilationRootProvider
    {
        private EcmaModule _module;
        private IEnumerable<MethodDesc> _profileData;
        private InstructionSetSupport _instructionSetSupport;

        public ReadyToRunProfilingRootProvider(EcmaModule module, ProfileDataManager profileDataManager)
        {
            _module = module;
            _profileData = profileDataManager.GetInputProfileDataMethodsForModule(module);
            _instructionSetSupport = ((ReadyToRunCompilerContext)module.Context).InstructionSetSupport;
        }

        public void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            foreach (var method in _profileData)
            {
                try
                {
                    // Validate that this method is fully instantiated
                    if (method.OwningType.IsGenericDefinition || method.OwningType.ContainsSignatureVariables())
                    {
                        continue;
                    }

                    if (method.IsGenericMethodDefinition)
                    {
                        continue;
                    }

                    bool containsSignatureVariables = false;
                    foreach (TypeDesc t in method.Instantiation)
                    {
                        if (t.IsGenericDefinition)
                        {
                            containsSignatureVariables = true;
                            break;
                        }

                        if (t.ContainsSignatureVariables())
                        {
                            containsSignatureVariables = true;
                            break;
                        }
                    }
                    if (containsSignatureVariables)
                        continue;

                    if (!CorInfoImpl.ShouldSkipCompilation(_instructionSetSupport, method))
                    {
                        ReadyToRunLibraryRootProvider.CheckCanGenerateMethod(method);
                        rootProvider.AddCompilationRoot(method, rootMinimalDependencies: true, reason: "Profile triggered method");
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
    }
}
