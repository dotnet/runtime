// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Internal
{
    internal static class StringComparers
    {
        public static StringComparer ContractName
        {
            get { return StringComparer.Ordinal; }
        }
        
        public static StringComparer MetadataKeyNames
        {
            get { return StringComparer.Ordinal; }
        }
    }
}
