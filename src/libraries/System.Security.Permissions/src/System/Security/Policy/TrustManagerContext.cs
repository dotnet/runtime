// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Policy
{
    public partial class TrustManagerContext
    {
        public TrustManagerContext() { }
        public TrustManagerContext(TrustManagerUIContext uiContext) { }
        public virtual bool IgnorePersistedDecision { get; set; }
        public virtual bool KeepAlive { get; set; }
        public virtual bool NoPrompt { get; set; }
        public virtual bool Persist { get; set; }
        public virtual TrustManagerUIContext UIContext { get; set; }
    }
    public enum TrustManagerUIContext
    {
        Install = 0,
        Run = 2,
        Upgrade = 1,
    }
}
