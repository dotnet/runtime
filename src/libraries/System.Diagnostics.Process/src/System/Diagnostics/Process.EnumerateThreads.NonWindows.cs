// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace System.Diagnostics
{
    public partial class Process
    {
        private ProcessThreadCollection EnumerateThreadsCore()
        {
            return EnumerateThreadsCoreFallback();
        }
    }
}
