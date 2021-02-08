// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

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
        /// Gets a value indicating whether GC transition should be suppressed on the given p/invoke.
        /// </summary>
        public static bool IsSuppressGCTransition(this MethodDesc method)
        {
            Debug.Assert(method.IsPInvoke);

            if (method is Internal.IL.Stubs.PInvokeTargetNativeMethod rawPinvoke)
                method = rawPinvoke.Target;

            return method.HasCustomAttribute("System.Runtime.InteropServices", "SuppressGCTransitionAttribute");
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
