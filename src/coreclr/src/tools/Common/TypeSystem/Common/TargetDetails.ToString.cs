// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    partial class TargetDetails
    {
        public override string ToString()
        {
            return $"{Architecture}-{OperatingSystem}-{Abi}";
        }
    }
}
