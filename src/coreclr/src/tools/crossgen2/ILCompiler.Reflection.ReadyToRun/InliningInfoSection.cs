// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    public class InliningInfoSection2
    {
        private readonly ReadyToRunReader _r2r;
        private readonly int _startOffset;
        private readonly int _endOffset;

        public InliningInfoSection2(ReadyToRunReader reader, int offset, int endOffset)
        {
            _r2r = reader;
            _startOffset = offset;
            _endOffset = endOffset;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            NativeParser parser = new NativeParser(_r2r.Image, (uint)_startOffset);
            NativeHashtable hashtable = new NativeHashtable(_r2r.Image, parser, (uint)_endOffset);

            var enumerator = hashtable.EnumerateAllEntries();

            NativeParser curParser = enumerator.GetNext();
            while (!curParser.IsNull())
            {
                int count = (int)curParser.GetUnsigned();
                int inlineeTokenAndFlag = (int)curParser.GetUnsigned();
                count--;
                int inlineeToken = RidToMethodDef(inlineeTokenAndFlag >> 1);
                if ((inlineeTokenAndFlag & 1) != 0)
                {
                    uint module = curParser.GetUnsigned();
                    count--;
                    string moduleName = _r2r.GetReferenceAssemblyName((int)module);
                    sb.AppendLine($"Inliners for inlinee {inlineeToken:X8} (module {moduleName}):");
                }
                else
                {
                    sb.AppendLine($"Inliners for inlinee {inlineeToken:X8}:");
                }

                while (count > 0)
                {
                    int inlinerTokenAndFlag = (int)curParser.GetUnsigned();
                    count--;
                    int inlinerToken = RidToMethodDef(inlinerTokenAndFlag >> 1);

                    if ((inlinerTokenAndFlag & 1) != 0)
                    {
                        uint module = curParser.GetUnsigned();
                        count--;
                        string moduleName = _r2r.GetReferenceAssemblyName((int)module);
                        sb.AppendLine($"  {inlinerToken:X8} (module {moduleName})");
                    }
                    else
                    {
                        sb.AppendLine($" {inlinerToken:X8}");
                    }

                }

                curParser = enumerator.GetNext();
            }

            return sb.ToString();
        }

        static int RidToMethodDef(int rid) => MetadataTokens.GetToken(MetadataTokens.MethodDefinitionHandle(rid));
    }
}
