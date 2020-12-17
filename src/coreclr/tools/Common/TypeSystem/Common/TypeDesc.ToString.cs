// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    partial class TypeDesc
    {
        public override string ToString()
        {
            return DebugNameFormatter.Instance.FormatName(this, DebugNameFormatter.FormatOptions.Default);
        }
    }
}
