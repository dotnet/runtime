// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Policy
{
    [Serializable]
    public abstract partial class EvidenceBase
    {
        protected EvidenceBase() { }
        public virtual EvidenceBase Clone() { return default(EvidenceBase); }
    }
}
