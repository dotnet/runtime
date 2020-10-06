// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Specialized;

#if !FEATURE_SERIALIZATION
namespace System.CodeDom
#else
namespace System.Runtime.Serialization
#endif
{
#if !FEATURE_SERIALIZATION
    public class CodeObject
#else
    internal class CodeObject
#endif
    {
        private IDictionary? _userData;

        public CodeObject() { }

        public IDictionary UserData => _userData ?? (_userData = new ListDictionary());
    }
}
