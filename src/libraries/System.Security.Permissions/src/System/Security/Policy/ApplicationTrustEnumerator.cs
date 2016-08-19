// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Policy
{
    public sealed partial class ApplicationTrustEnumerator : System.Collections.IEnumerator
    {
        internal ApplicationTrustEnumerator() { }
        public System.Security.Policy.ApplicationTrust Current { get { return default(System.Security.Policy.ApplicationTrust); } }
        object System.Collections.IEnumerator.Current { get { return null; } }
        public bool MoveNext() { return false; }
        public void Reset() { }
    }
}
