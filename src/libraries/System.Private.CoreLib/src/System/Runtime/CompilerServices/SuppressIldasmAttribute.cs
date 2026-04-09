// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    [Obsolete(Obsoletions.SuppressIldasmAttributeMessage, DiagnosticId = Obsoletions.SuppressIldasmAttributeDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module)]
    public sealed class SuppressIldasmAttribute : Attribute
    {
        public SuppressIldasmAttribute() { }
    }
}
