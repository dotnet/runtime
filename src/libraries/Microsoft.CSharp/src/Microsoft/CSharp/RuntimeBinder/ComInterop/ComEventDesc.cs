// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.CSharp.RuntimeBinder.ComInterop
{
    internal sealed class ComEventDesc
    {
        public Guid SourceIID;
        public int Dispid;
    };
}
