// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.JavaScript
{
    public partial struct JavaScriptMarshalerArguments
    {
        internal unsafe void* Buffer;
    }
}

namespace System.Runtime.InteropServices.JavaScript.Private
{
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct JSMarshalerArgumentsHeader
    {
        public JSMarshalerArg Exception;
        public JSMarshalerArg Result;
    }
}
