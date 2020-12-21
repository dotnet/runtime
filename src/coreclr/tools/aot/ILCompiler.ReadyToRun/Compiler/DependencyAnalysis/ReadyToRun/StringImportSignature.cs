// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;
using Internal.ReadyToRunConstants;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class StringImportSignature : Signature
    {
        private readonly ModuleToken _token;

        public StringImportSignature(ModuleToken token)
        {
            _token = token;
        }

        public override int ClassCode => 324832559;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataSignatureBuilder dataBuilder = new ObjectDataSignatureBuilder();

            if (!relocsOnly)
            {
                dataBuilder.AddSymbol(this);

                dataBuilder.EmitFixup(factory, ReadyToRunFixupKind.StringHandle, _token.Module, factory.SignatureContext);
                dataBuilder.EmitUInt(_token.TokenRid);
            }

            return dataBuilder.ToObjectData();
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append("StringImportSignature: " + _token.ToString());
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            StringImportSignature otherNode = (StringImportSignature)other;
            return _token.CompareTo(otherNode._token);
        }
    }
}
