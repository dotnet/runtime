// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.TypeSystem;

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

        public abstract bool GeneratesPInvoke(MethodDesc method);
    }
}
