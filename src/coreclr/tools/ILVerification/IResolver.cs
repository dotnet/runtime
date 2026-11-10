// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace ILVerify
{
    /// <summary>
    /// Provides a <see cref="PEReader"/> for each assembly and module that a <see cref="Verifier"/> needs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The verifier calls the resolver when the system module is set and during verification, including while a
    /// sequence returned by a <c>Verify</c> method is enumerated. The verifier can request the same assembly or module
    /// more than once.
    /// </para>
    /// <para>
    /// The verifier identifies modules by <see cref="PEReader"/> instance. A different instance for the same assembly
    /// becomes a second module whose types are distinct from those of the first. This can produce wrong verification
    /// results. The same applies to the assembly being verified: if the verifier also asks for it by name, the
    /// resolver must return the same <see cref="PEReader"/> instance that is passed to <c>Verify</c>. For example,
    /// when <c>System.Private.CoreLib</c> is both the system module and the assembly being verified,
    /// <see cref="Verifier.SetSystemModuleName"/> asks for it by name before verification starts.
    /// </para>
    /// <para>
    /// See <see cref="Verifier"/> for how long the returned readers must stay usable; the verifier does not dispose
    /// them.
    /// </para>
    /// </remarks>
    public interface IResolver
    {
        /// <summary>
        /// Resolve assembly to PEReader. Repeated requests for the same assembly must return the same instance.
        /// </summary>
        PEReader ResolveAssembly(AssemblyNameInfo assemblyName);

        /// <summary>
        /// Resolve module to PEReader. Repeated requests for the same module of the same referencing assembly must
        /// return the same instance.
        /// </summary>
        PEReader ResolveModule(AssemblyNameInfo referencingAssembly, string fileName);
    }
}
