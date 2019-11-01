// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.JitInterface;
using Internal.Text;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class StringImportSignature : Signature
    {
        private readonly ModuleToken _token;

        private readonly SignatureContext _signatureContext;

        public StringImportSignature(ModuleToken token, SignatureContext signatureContext)
        {
            _token = token;
            _signatureContext = signatureContext;
        }

        public override int ClassCode => 324832559;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ReadyToRunCodegenNodeFactory r2rFactory = (ReadyToRunCodegenNodeFactory)factory;
            ObjectDataSignatureBuilder dataBuilder = new ObjectDataSignatureBuilder();
            dataBuilder.AddSymbol(this);

            dataBuilder.EmitFixup(r2rFactory, ReadyToRunFixupKind.READYTORUN_FIXUP_StringHandle, _token.Module, _signatureContext);
            dataBuilder.EmitUInt(_token.TokenRid);

            return dataBuilder.ToObjectData();
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("StringImportSignature: " + _token.ToString());
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return _token.CompareTo(((StringImportSignature)other)._token);
        }
    }
}
