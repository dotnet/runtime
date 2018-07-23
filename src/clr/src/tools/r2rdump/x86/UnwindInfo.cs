// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Xml.Serialization;

namespace R2RDump.x86
{
    public class UnwindInfo : BaseUnwindInfo
    {
        public uint FunctionLength { get; set; }

        public UnwindInfo() { }

        public UnwindInfo(byte[] image, int offset)
        {
            FunctionLength = NativeReader.DecodeUnsignedGc(image, ref offset);
            Size = sizeof(int);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"\tFunctionLength: {FunctionLength}");
            return sb.ToString();
        }
    }
}
