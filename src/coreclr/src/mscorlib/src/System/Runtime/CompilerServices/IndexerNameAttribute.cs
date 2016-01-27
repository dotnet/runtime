// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices
{
    using System;

[Serializable]
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class IndexerNameAttribute: Attribute
    {
        public IndexerNameAttribute(String indexerName)
        {}
    }
}
