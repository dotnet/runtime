// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.Security.Permissions
{
    public sealed class KeyContainerPermissionAccessEntryEnumerator : IEnumerator
    {
        public KeyContainerPermissionAccessEntry Current { get; }
        object IEnumerator.Current { get; }
        public bool MoveNext() { return false; }
        public void Reset() { }
    }
}
