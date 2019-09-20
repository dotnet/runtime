// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.TypeSystem.Ecma;
using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// Provides compilation group for a library that compiles everything in the input IL module.
    /// </summary>
    public class ReadyToRunRootProvider : ICompilationRootProvider
    {
        private EcmaModule _module;
        private ProfileData _profileData;

        public ReadyToRunRootProvider(EcmaModule module, ProfileDataManager profileDataManager)
        {
            _module = module;
            _profileData = profileDataManager.GetDataForModuleDesc(module);
        }

        public void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            foreach (var methodProfileInfo in _profileData.GetAllMethodProfileData())
            {
                if (!methodProfileInfo.Flags.HasFlag(MethodProfilingDataFlags.ExcludeHotMethodCode) &&
                    !methodProfileInfo.Flags.HasFlag(MethodProfilingDataFlags.ExcludeColdMethodCode))
                {
                    try
                    {
                        MethodDesc method = methodProfileInfo.Method;

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

                        CheckCanGenerateMethod(method);
                        rootProvider.AddCompilationRoot(method, "Profile triggered method");
                    }
                    catch (TypeSystemException)
                    {
                        // Individual methods can fail to load types referenced in their signatures.
                        // Skip them in library mode since they're not going to be callable.
                        continue;
                    }
                }
            }

            if (!_profileData.PartialNGen)
            {
                foreach (TypeDesc type in _module.GetAllTypes())
                {
                    try
                    {
                        rootProvider.AddCompilationRoot(type, "Library module type");
                    }
                    catch (TypeSystemException)
                    {
                        // Swallow type load exceptions while rooting
                        continue;
                    }

                    // If this is not a generic definition, root all methods
                    if (!type.HasInstantiation)
                    {
                        RootMethods(type, "Library module method", rootProvider);
                    }
                }
            }
        }

        private void RootMethods(TypeDesc type, string reason, IRootingServiceProvider rootProvider)
        {
            foreach (MethodDesc method in type.GetAllMethods())
            {
                // Skip methods with no IL and uninstantiated generic methods
                if (method.IsAbstract || method.HasInstantiation)
                    continue;

                if (method.IsInternalCall)
                    continue;

                try
                {
                    CheckCanGenerateMethod(method);
                    rootProvider.AddCompilationRoot(method, reason);
                }
                catch (TypeSystemException)
                {
                    // Individual methods can fail to load types referenced in their signatures.
                    // Skip them in library mode since they're not going to be callable.
                    continue;
                }
            }
        }

        /// <summary>
        /// Validates that it will be possible to generate '<paramref name="method"/>' based on the types 
        /// in its signature. Unresolvable types in a method's signature prevent RyuJIT from generating
        /// even a stubbed out throwing implementation.
        /// </summary>
        public static void CheckCanGenerateMethod(MethodDesc method)
        {
            MethodSignature signature = method.Signature;

            // Vararg methods are not supported in .NET Core
            if ((signature.Flags & MethodSignatureFlags.UnmanagedCallingConventionMask) == MethodSignatureFlags.CallingConventionVarargs)
                ThrowHelper.ThrowBadImageFormatException();

            CheckTypeCanBeUsedInSignature(signature.ReturnType);

            for (int i = 0; i < signature.Length; i++)
            {
                CheckTypeCanBeUsedInSignature(signature[i]);
            }
        }

        private static void CheckTypeCanBeUsedInSignature(TypeDesc type)
        {
            MetadataType defType = type as MetadataType;

            if (defType != null)
            {
                defType.ComputeTypeContainsGCPointers();
            }
        }
    }
}
