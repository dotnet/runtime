// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime.Serialization.Formatters.Binary
{
    internal sealed class BinaryAssembly : IStreamable
    {
        internal int _assemId;
        internal string? _assemblyString;

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
            Debug.Assert(_assemblyString != null);
            output.WriteString(_assemblyString);
        }

        public void Read(BinaryParser input)
        {
            _assemId = input.ReadInt32();
            _assemblyString = input.ReadString();
        }
    }
}
