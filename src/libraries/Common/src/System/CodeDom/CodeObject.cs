// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private IDictionary _userData = null;

        public CodeObject() { }

        public IDictionary UserData => _userData ?? (_userData = new ListDictionary());
    }
}
