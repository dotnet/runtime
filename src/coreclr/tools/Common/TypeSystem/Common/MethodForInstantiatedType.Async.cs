// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    public sealed partial class MethodForInstantiatedType
    {
        public override AsyncMethodKind AsyncMethodKind
        {
            get
            {
                return _typicalMethodDef.AsyncMethodKind;
            }
        }
    }
}
