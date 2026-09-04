// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.ReadyToRunConstants;
using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class DelayLoadMethodImport : DelayLoadHelperImport, IMethodCodeNodeWithTypeSignature
    {
        private readonly MethodWithGCInfo _localMethod;

        public DelayLoadMethodImport(
            NodeFactory factory,
            ReadyToRunFixupKind fixupKind,
            MethodWithToken method,
            MethodWithGCInfo localMethod,
            bool isInstantiatingStub,
            bool isJump)
            : base(
                  factory,
                  factory.MethodImports,
                  ReadyToRunHelper.DelayLoad_MethodCall,
                  factory.MethodSignature(
                      fixupKind,
                      method,
                      isInstantiatingStub),
                  useJumpableStub: isJump)
        {
            _localMethod = localMethod;
            MethodWithToken = method;
        }

        public MethodWithToken MethodWithToken { get; }
        public MethodDesc Method => MethodWithToken.Method;
        public MethodWithGCInfo MethodCodeNode => _localMethod;

        public override int ClassCode => 459923351;

        public override void AddStaticDependencies(DependencySink<NodeFactory> sink, NodeFactory factory)
        {
            base.AddStaticDependencies(sink, factory);
            if (_localMethod != null)
                sink.Add(_localMethod, "Local method import");
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            if ((_localMethod != null) && (((DelayLoadMethodImport)other)._localMethod != null))
            {
                int result = comparer.Compare(_localMethod, ((DelayLoadMethodImport)other)._localMethod);
                if (result != 0)
                    return result;
            }
            else if (_localMethod != null)
                return 1;
            else if (((DelayLoadMethodImport)other)._localMethod != null)
                return -1;

            return base.CompareToImpl(other, comparer);
        }
    }
}
