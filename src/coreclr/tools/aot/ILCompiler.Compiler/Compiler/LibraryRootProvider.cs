// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem.Ecma;
using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// Provides compilation group for a library that compiles everything in the input IL module.
    /// </summary>
    public class LibraryRootProvider : ICompilationRootProvider
    {
        private EcmaModule _module;

        public LibraryRootProvider(EcmaModule module)
        {
            _module = module;
        }

        public void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            foreach (TypeDesc type in _module.GetAllTypes())
            {
                try
                {
                    rootProvider.AddCompilationRoot(type, "Library module type");
                }
                catch (TypeSystemException)
                {
                    // TODO: fail compilation if a switch was passed

                    // Swallow type load exceptions while rooting
                    continue;

                    // TODO: Log as a warning
                }

                // If this is not a generic definition, root all methods
                if (!type.HasInstantiation)
                {
                    RootMethods(type, "Library module method", rootProvider);
                    rootProvider.RootThreadStaticBaseForType(type, "Library module type statics");
                    rootProvider.RootGCStaticBaseForType(type, "Library module type statics");
                    rootProvider.RootNonGCStaticBaseForType(type, "Library module type statics");
                }
            }
        }

        private static void RootMethods(TypeDesc type, string reason, IRootingServiceProvider rootProvider)
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
                    // TODO: fail compilation if a switch was passed

                    // Individual methods can fail to load types referenced in their signatures.
                    // Skip them in library mode since they're not going to be callable.
                    continue;

                    // TODO: Log as a warning
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

            defType?.ComputeTypeContainsGCPointers();
        }
    }
}
