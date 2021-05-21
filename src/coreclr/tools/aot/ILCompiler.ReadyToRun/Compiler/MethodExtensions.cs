// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    static class MethodExtensions
    {
        /// <summary>
        /// Returns true if <paramref name="method"/> is an actual native entrypoint.
        /// There's a distinction between when a method reports it's a PInvoke in the metadata
        /// versus how it's treated in the compiler. For many PInvoke methods the compiler will generate
        /// an IL body. The methods with an IL method body shouldn't be treated as PInvoke within the compiler.
        /// </summary>
        public static bool IsRawPInvoke(this MethodDesc method)
        {
            return method.IsPInvoke && (method is Internal.IL.Stubs.PInvokeTargetNativeMethod);
        }

        /// <summary>
        /// Gets a value indicating whether the method has the SuppressGCTransition attribute
        /// </summary>
        public static bool HasSuppressGCTransitionAttribute(this MethodDesc method)
        {
            Debug.Assert(method.IsPInvoke);

            if (method is Internal.IL.Stubs.PInvokeTargetNativeMethod rawPinvoke)
                method = rawPinvoke.Target;

            // Check SuppressGCTransition attribute
            return method.HasCustomAttribute("System.Runtime.InteropServices", "SuppressGCTransitionAttribute");
        }

        /// <summary>
        /// Gets a value indicating whether GC transition should be suppressed on the given p/invoke.
        /// </summary>
        public static bool IsSuppressGCTransition(this MethodDesc method)
        {
            Debug.Assert(method.IsPInvoke);

            // Check SuppressGCTransition attribute
            if (method.HasSuppressGCTransitionAttribute())
                return true;

            MethodSignatureFlags unmanagedCallConv = method.GetPInvokeMethodMetadata().Flags.UnmanagedCallingConvention;
            if (unmanagedCallConv != MethodSignatureFlags.None)
                return false;

            if (!(method is Internal.TypeSystem.Ecma.EcmaMethod ecmaMethod))
                return false;

            // Check UnmanagedCallConv attribute
            System.Reflection.Metadata.CustomAttributeValue<TypeDesc>? unmanagedCallConvAttribute = ecmaMethod.GetDecodedCustomAttribute("System.Runtime.InteropServices", "UnmanagedCallConvAttribute");
            if (unmanagedCallConvAttribute == null)
                return false;

            foreach (DefType defType in Internal.JitInterface.CallConvHelper.EnumerateCallConvsFromAttribute(unmanagedCallConvAttribute.Value))
            {
                if (defType.Name == "CallConvSuppressGCTransition")
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Return true when the method is marked as non-versionable. Non-versionable methods
        /// may be freely inlined into ReadyToRun images even when they don't reside in the
        /// same version bubble as the module being compiled.
        /// </summary>
        /// <param name="method">Method to check</param>
        /// <returns>True when the method is marked as non-versionable, false otherwise.</returns>
        public static bool IsNonVersionable(this MethodDesc method)
        {
            return method.HasCustomAttribute("System.Runtime.Versioning", "NonVersionableAttribute");
        }
    }
}
