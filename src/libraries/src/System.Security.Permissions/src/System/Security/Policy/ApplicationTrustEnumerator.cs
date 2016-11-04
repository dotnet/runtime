// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.Security.Policy
{
    public sealed partial class ApplicationTrustEnumerator : IEnumerator
    {
        internal ApplicationTrustEnumerator() { }
        public ApplicationTrust Current { get { return default(ApplicationTrust); } }
        object IEnumerator.Current { get { return null; } }
        public bool MoveNext() { return false; }
        public void Reset() { }
    }
}
