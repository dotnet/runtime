// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Metadata.Ecma335
{
    public enum FunctionPointerAttributes
    {
        None = SignatureAttributes.None,
        HasThis = SignatureAttributes.Instance,
        HasExplicitThis = SignatureAttributes.Instance | SignatureAttributes.ExplicitThis
    }
}
