// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;

namespace Microsoft.Interop.JavaScript
{
    internal sealed class VoidGenerator : BaseJSGenerator
    {
        public VoidGenerator(MarshalerType marshalerType)
            : base(marshalerType, new Forwarder())
        {
        }
    }
}
