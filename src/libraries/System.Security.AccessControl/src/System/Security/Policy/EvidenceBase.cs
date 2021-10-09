// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace System.Security.Policy
{
    public abstract partial class EvidenceBase
    {
        protected EvidenceBase() { }
        public virtual EvidenceBase? Clone() { return default(EvidenceBase); }
    }
}
