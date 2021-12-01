// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    partial class FieldDesc
    {
        public override string ToString()
        {
            return $"{OwningType}.{Name}";
        }
    }
}
