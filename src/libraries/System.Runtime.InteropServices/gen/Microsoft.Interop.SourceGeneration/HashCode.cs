// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Interop
{
    public static class HashCode
    {
        public static int Combine(params object[] values)
        {
            int hash = 31;
            foreach (object value in values)
            {
                hash = hash * 29 + value.GetHashCode();
            }
            return hash;
        }
    }
}
