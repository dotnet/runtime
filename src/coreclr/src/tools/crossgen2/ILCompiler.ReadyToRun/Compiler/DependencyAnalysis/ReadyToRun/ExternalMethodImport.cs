
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.ReadyToRunConstants;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class ExternalMethodImport : DelayLoadHelperImport, IMethodNode
    {
        private readonly MethodWithToken _method;

        public ExternalMethodImport(
            NodeFactory factory,
            ReadyToRunFixupKind fixupKind,
            MethodWithToken method,
            bool isUnboxingStub,
            bool isInstantiatingStub)
            : base(
                  factory,
                  factory.MethodImports,
                  ReadyToRunHelper.DelayLoad_MethodCall,
                  factory.MethodSignature(
                      fixupKind,
                      method,
                      isUnboxingStub,
                      isInstantiatingStub))
        {
            _method = method;
        }

        public MethodDesc Method => _method.Method;

        public override int ClassCode => 458823351;

        // This is just here in case of future extension (_method is already compared in the base CompareToImpl)
        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return base.CompareToImpl(other, comparer);
        }
    }
}
