// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.JavaScript.Private
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct JSMarshalerSignatureHeader
    {
        public int ArgumentCount;
        public int ExtraBufferLength;
        public JSMarshalerSig Exception;
        public JSMarshalerSig Result;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 16)]
    public struct JSMarshalerSig
    {
        public IntPtr TypeHandle;
        public int BufferOffset;
        public int BufferLength;
        public int UseRoot;
        public IntPtr MarshallerJSHandle;

        public override string ToString()
        {
            return $"Type: {TypeHandle} BufferOffset: {BufferOffset}, BufferLength:{BufferLength}, UseRoot:{UseRoot}";
        }
    }
}
