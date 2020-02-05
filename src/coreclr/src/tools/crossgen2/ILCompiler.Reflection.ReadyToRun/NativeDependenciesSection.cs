// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace ILCompiler.Reflection.ReadyToRun
{
    public class NativeDependenciesSection
    {
        private readonly ReadyToRunReader _r2r;
        private readonly int _startOffset;
        private readonly int _endOffset;

        public NativeDependenciesSection(ReadyToRunReader reader, int offset, int endOffset)
        {
            _r2r = reader;
            _startOffset = offset;
            _endOffset = endOffset;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            byte[] guidBytes = new byte[16];

            int iiOffset = _startOffset;
            while (iiOffset < _endOffset)
            {
                int moduleId = NativeReader.ReadInt32(_r2r.Image, ref iiOffset);
                string moduleName = _r2r.GetReferenceAssemblyName(moduleId);
                Array.Copy(_r2r.Image, iiOffset, guidBytes, 0, guidBytes.Length);
                iiOffset += guidBytes.Length;
                Guid mvid = new Guid(guidBytes);
                sb.AppendLine($"{moduleName}: {mvid}");
            }

            return sb.ToString();
        }
    }
}
