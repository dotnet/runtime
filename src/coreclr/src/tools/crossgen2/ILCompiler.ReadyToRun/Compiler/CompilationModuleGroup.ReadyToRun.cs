// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.TypeSystem;
using ILCompiler.DependencyAnalysis.ReadyToRun;

namespace ILCompiler
{
    partial class CompilationModuleGroup
    {
        /// <summary>
        /// If true, the type is fully contained in the current compilation group.
        /// </summary>
        public virtual bool ContainsTypeLayout(TypeDesc type) => ContainsType(type);

        /// <summary>
        /// Returns true when a given type belongs to the same version bubble as the compilation module group.
        /// By default return the same outcome as ContainsType.
        /// </summary>
        /// <param name="typeDesc">Type to check</param>
        /// <returns>True if the given type versions with the current compilation module group</returns>
        public virtual bool VersionsWithType(TypeDesc typeDesc) => ContainsType(typeDesc);

        /// <summary>
        /// Returns true when a given method belongs to the same version bubble as the compilation module group.
        /// By default return the same outcome as ContainsMethodBody.
        /// </summary>
        /// <param name="methodDesc">Method to check</param>
        /// <returns>True if the given method versions with the current compilation module group</returns>
        public virtual bool VersionsWithMethodBody(MethodDesc methodDesc) => ContainsMethodBody(methodDesc, unboxingStub: false);

        /// <summary>
        /// Returns true when a given module belongs to the same version bubble as the compilation module group.
        /// </summary>
        /// <param name="module">Module to check</param>
        /// <returns>True if the given module versions with the current compilation module group</returns>
        public abstract bool VersionsWithModule(ModuleDesc module);

        /// <summary>
        /// Checks if the given PInvoke method can produce a PInvoke stub in the current compilation, depending on the method's
        /// signature and the compilation policy.
        /// </summary>
        /// <param name="method">PInvoke method to check</param>
        /// <returns>Returns true if the given PInvoke method can produce a PInvoke stub in the current compilation</returns>
        public abstract bool GeneratesPInvoke(MethodDesc method);

        /// <summary>
        /// Retrieve the module-based token for a type that is not part of the version bubble of the current compilation.
        /// </summary>
        /// <param name="type">Type to get a module token for</param>
        /// <param name="token">Module-based token for the type</param>
        /// <returns>Returns true the type was referenced by any of the input modules in the current compliation</returns>
        public abstract bool TryGetModuleTokenForExternalType(TypeDesc type, out ModuleToken token);
    }
}
