// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler
{
    public abstract partial class CompilationModuleGroup
    {
        /// <summary>
        /// If true, "type" is in the set of input assemblies being compiled
        /// </summary>
        public abstract bool ContainsType(TypeDesc type);
        /// <summary>
        /// If true, "method" is in the set of input assemblies being compiled
        /// </summary>
        public abstract bool ContainsMethodBody(MethodDesc method, bool unboxingStub);
        /// <summary>
        /// Decide whether a given call may get inlined by JIT.
        /// </summary>
        /// <param name="callerMethod">Calling method the assembly code of is about to receive the callee code</param>
        /// <param name="calleeMethod">The called method to be inlined into the caller</param>
        public virtual bool CanInline(MethodDesc callerMethod, MethodDesc calleeMethod) => true;
    }
}
