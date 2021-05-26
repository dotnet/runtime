// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Specialized;

namespace System.Runtime.Serialization
{
    internal class CodeObject
    {
        private IDictionary? _userData;

        public CodeObject() { }

        public IDictionary UserData => _userData ?? (_userData = new ListDictionary());
    }
}
