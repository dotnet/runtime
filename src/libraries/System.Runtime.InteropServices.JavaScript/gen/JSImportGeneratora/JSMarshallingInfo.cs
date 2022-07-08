// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using System.Runtime.InteropServices.JavaScript;

namespace Microsoft.Interop.JavaScript
{
    internal record JSMarshallingInfo : MarshallingInfo
    {
        public MarshallingInfo Inner;
        public JSTypeFlags JSType;
        public JSTypeFlags[] JSTypeArguments;
        public JSMarshallingInfo(MarshallingInfo inner)
        {
            Inner = inner;
        }
        protected JSMarshallingInfo()
        {
            Inner = null;
        }
    }

    internal sealed record JSMissingMarshallingInfo : JSMarshallingInfo
    {
        public JSMissingMarshallingInfo()
        {
            JSType = JSTypeFlags.Missing;
        }
    }
}
