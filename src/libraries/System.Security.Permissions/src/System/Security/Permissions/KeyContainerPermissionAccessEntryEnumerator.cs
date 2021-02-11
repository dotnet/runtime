// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.Security.Permissions
{
#if NET50_OBSOLETIONS
    [Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
    public sealed class KeyContainerPermissionAccessEntryEnumerator : IEnumerator
    {
        public KeyContainerPermissionAccessEntry Current { get; }
        object IEnumerator.Current { get; }
        public bool MoveNext() { return false; }
        public void Reset() { }
    }
}
