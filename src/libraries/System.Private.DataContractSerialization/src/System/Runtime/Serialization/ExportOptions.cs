// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace System.Runtime.Serialization
{
    public class ExportOptions
    {
        private Collection<Type>? _knownTypes;

        public Collection<Type> KnownTypes => _knownTypes ??= new Collection<Type>();
    }
}
