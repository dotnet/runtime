// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections;
using System.Collections.Specialized;

#if smolloy_codedom_stubbed
namespace System.CodeDom.Stubs
#elif smolloy_codedom_partial_internal
namespace System.Runtime.Serialization
#elif smolloy_codedom_full_internalish
namespace System.Runtime.Serialization.CodeDom
#else // CODEDOM
namespace System.CodeDom
#endif
{
#if smolloy_codedom_partial_internal
    internal class CodeObject
#else // smolloy_codedom_stubbed || smolloy_codedom_full_internalish || CODEDOM
    public class CodeObject
#endif
    {
        private IDictionary? _userData;

        public CodeObject() { }

        public IDictionary UserData => _userData ??= new ListDictionary();
    }
}
