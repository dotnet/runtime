// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;

namespace Internal.TypeSystem
{
    public sealed partial class InstantiatedMethod
    {
        public override AsyncMethodKind AsyncMethodKind
        {
            get
            {
                return _methodDef.AsyncMethodKind;
            }
        }
    }
}
