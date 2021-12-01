// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.JitInterface;
using Internal.Text;
using Internal.TypeSystem;
using Internal.ReadyToRunConstants;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class PrecodeMethodImport : PrecodeHelperImport, IMethodNode
    {
        private readonly MethodWithGCInfo _localMethod;
        private readonly MethodWithToken _method;

        public PrecodeMethodImport(
            NodeFactory factory,
            ReadyToRunFixupKind fixupKind,
            MethodWithToken method,
            MethodWithGCInfo localMethod,
            bool isInstantiatingStub) :
            base (
                factory,
                factory.MethodSignature(
                      fixupKind,
                      method,
                      isInstantiatingStub)
            )
        {
            _localMethod = localMethod;
            _method = method;
        }

        public MethodDesc Method => _method.Method;

        public MethodWithGCInfo MethodCodeNode => _localMethod;
        
        public override int ClassCode => 30624770;

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("PrecodeMethodImport -> ");
            base.AppendMangledName(nameMangler, sb);
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            foreach (DependencyListEntry entry in base.GetStaticDependencies(factory))
            {
                yield return entry;
            }
            if (_localMethod != null)
                yield return new DependencyListEntry(_localMethod, "Precode Method Import");
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            if ((_localMethod != null) && (((PrecodeMethodImport)other)._localMethod != null))
            {
                int result = comparer.Compare(_localMethod, ((PrecodeMethodImport)other)._localMethod);
                if (result != 0)
                    return result;
            }
            else if (_localMethod != null)
                return 1;
            else if (((PrecodeMethodImport)other)._localMethod != null)
                return -1;

            return base.CompareToImpl(other, comparer);
        }
    }
}
