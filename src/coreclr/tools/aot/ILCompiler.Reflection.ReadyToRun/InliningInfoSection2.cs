// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace ILCompiler.Reflection.ReadyToRun
{
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
                int inlineeRidAndFlag = (int)curParser.GetUnsigned();
                count--;
                int inlineeToken = RidToMethodDef(inlineeRidAndFlag >> 1);
                if ((inlineeRidAndFlag & 1) != 0)
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

                int currentRid = 0;
                while (count > 0)
                {
                    int inlinerDeltaAndFlag = (int)curParser.GetUnsigned();
                    count--;
                    int inlinerDelta = inlinerDeltaAndFlag >> 1;
                    currentRid += inlinerDelta;
                    int inlinerToken = RidToMethodDef(currentRid);

                    if ((inlinerDeltaAndFlag & 1) != 0)
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
