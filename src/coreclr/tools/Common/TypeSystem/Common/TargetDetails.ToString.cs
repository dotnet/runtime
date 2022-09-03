// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    public partial class TargetDetails
    {
        public override string ToString()
        {
            return $"{Architecture}-{OperatingSystem}-{Abi}";
        }
    }
}
