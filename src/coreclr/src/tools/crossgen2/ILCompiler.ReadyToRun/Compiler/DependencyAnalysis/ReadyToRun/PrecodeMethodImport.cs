
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using Internal.JitInterface;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class PrecodeMethodImport : PrecodeHelperImport, IMethodNode
    {
        private readonly MethodWithGCInfo _localMethod;

        public PrecodeMethodImport(
            ReadyToRunCodegenNodeFactory factory,
            ReadyToRunFixupKind fixupKind,
            MethodWithToken method,
            MethodWithGCInfo localMethod,
            bool isUnboxingStub,
            bool isInstantiatingStub,
            SignatureContext signatureContext) :
            base (
                factory,
                factory.MethodSignature(
                      fixupKind,
                      method,
                      isUnboxingStub,
                      isInstantiatingStub,
                      signatureContext)
            )
        {
            _localMethod = localMethod;
        }

        public MethodDesc Method => _localMethod.Method;

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
            yield return new DependencyListEntry(_localMethod, "Precode Method Import");
        }
    }
}
