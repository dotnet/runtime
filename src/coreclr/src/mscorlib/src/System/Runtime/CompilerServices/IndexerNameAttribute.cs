// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Runtime.CompilerServices
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public sealed class IndexerNameAttribute : Attribute
    {
        public IndexerNameAttribute(String indexerName)
        { }
    }
}
