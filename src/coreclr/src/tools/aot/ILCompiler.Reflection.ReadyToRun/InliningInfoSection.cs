// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace ILCompiler.Reflection.ReadyToRun
{
    public class InliningInfoSection
    {
        private readonly ReadyToRunReader _r2r;
        private readonly int _startOffset;
        private readonly int _endOffset;

        public InliningInfoSection(ReadyToRunReader reader, int offset, int endOffset)
        {
            _r2r = reader;
            _startOffset = offset;
            _endOffset = endOffset;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            int iiOffset = _startOffset;
            int sizeOfInlineIndex = NativeReader.ReadInt32(_r2r.Image, ref iiOffset);
            int inlineIndexEndOffset = iiOffset + sizeOfInlineIndex;
            while (iiOffset < inlineIndexEndOffset)
            {
                int inlineeRid = NativeReader.ReadInt32(_r2r.Image, ref iiOffset);
                int inlinersOffset = NativeReader.ReadInt32(_r2r.Image, ref iiOffset);
                sb.AppendLine($"Inliners for inlinee {RidToMethodDef(inlineeRid):X8}:");
                var inlinersReader = new NibbleReader(_r2r.Image, inlineIndexEndOffset + inlinersOffset);
                uint sameModuleCount = inlinersReader.ReadUInt();

                int baseRid = 0;
                for (uint i = 0; i < sameModuleCount; i++)
                {
                    int currentRid = baseRid + (int)inlinersReader.ReadUInt();
                    sb.AppendLine($"  {RidToMethodDef(currentRid):X8}");
                    baseRid = currentRid;
                }
            }

            return sb.ToString();
        }

        static int RidToMethodDef(int rid) => MetadataTokens.GetToken(MetadataTokens.MethodDefinitionHandle(rid));
    }
}
