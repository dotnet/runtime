// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Specialized;

namespace System.Configuration
{
    internal sealed class ReadOnlyNameValueCollection : NameValueCollection
    {

        internal ReadOnlyNameValueCollection(IEqualityComparer equalityComparer) : base(equalityComparer)
        {
        }

        internal ReadOnlyNameValueCollection(ReadOnlyNameValueCollection value) : base(value)
        {
        }

        internal void SetReadOnly()
        {
            IsReadOnly = true;
        }
    }

}
