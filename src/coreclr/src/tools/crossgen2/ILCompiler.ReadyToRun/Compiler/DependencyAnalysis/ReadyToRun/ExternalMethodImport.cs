
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.JitInterface;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class ExternalMethodImport : DelayLoadHelperImport, IMethodNode
    {
        private readonly MethodWithToken _method;

        private readonly SignatureContext _signatureContext;

        public ExternalMethodImport(
            ReadyToRunCodegenNodeFactory factory,
            ReadyToRunFixupKind fixupKind,
            MethodWithToken method,
            bool isUnboxingStub,
            bool isInstantiatingStub,
            SignatureContext signatureContext)
            : base(
                  factory,
                  factory.MethodImports,
                  ReadyToRunHelper.DelayLoad_MethodCall,
                  factory.MethodSignature(
                      fixupKind,
                      method,
                      isUnboxingStub,
                      isInstantiatingStub,
                      signatureContext))
        {
            _method = method;
            _signatureContext = signatureContext;
        }

        public MethodDesc Method => _method.Method;

        public override int ClassCode => 458823351;
    }
}
