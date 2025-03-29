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
        /// <param name="root">Root method</param>
        /// <param name="callerMethod">Immediate caller of the calleeMethod (either root method, or a method already inlined into the root method)</param>
        /// <param name="calleeMethod">The method to be inlined into root method</param>
        public virtual bool CanInline(MethodDesc root, MethodDesc callerMethod, MethodDesc calleeMethod) => true;
    }
}
