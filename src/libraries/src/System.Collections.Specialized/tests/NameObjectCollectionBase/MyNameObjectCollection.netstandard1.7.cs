// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Collections.Specialized.Tests
{
    public partial class MyNameObjectCollection : NameObjectCollectionBase
    {
#pragma warning disable CS0618 // Type or member is obsolete
        public MyNameObjectCollection(IHashCodeProvider hashProvider, IComparer comparer) : base(hashProvider, comparer) { }
        public MyNameObjectCollection(int capacity, IHashCodeProvider hashProvider, IComparer comparer) : base(capacity, hashProvider, comparer) { }
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
