// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.Marshalling
{
    public enum MarshalDirection
    {
        ManagedToUnmanaged = 0,
        UnmanagedToManaged = 1,
        Bidirectional = 2
    }
}
