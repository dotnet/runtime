// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security
{
    public abstract partial class SecurityState
    {
        protected SecurityState() { }
        public abstract void EnsureState();
        public bool IsStateAvailable() { return false; }
    }
}
