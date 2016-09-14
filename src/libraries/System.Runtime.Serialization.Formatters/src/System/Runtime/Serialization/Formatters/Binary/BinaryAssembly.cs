// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization.Formatters.Binary
{
    internal sealed class BinaryAssembly : IStreamable
    {
        internal int _assemId;
        internal string _assemblyString;

        internal BinaryAssembly() { }

        internal void Set(int assemId, string assemblyString)
        {
            _assemId = assemId;
            _assemblyString = assemblyString;
        }

        public void Write(BinaryFormatterWriter output)
        {
            output.WriteByte((byte)BinaryHeaderEnum.Assembly);
            output.WriteInt32(_assemId);
            output.WriteString(_assemblyString);
        }

        public void Read(BinaryParser input)
        {
            _assemId = input.ReadInt32();
            _assemblyString = input.ReadString();
        }
    }
}
