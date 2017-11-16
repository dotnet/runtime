// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    /////////////////////////////////////////////////////////////////////////////////
    // Encapsulates a type list, including its size.

    internal sealed class TypeArray
    {
        public TypeArray(CType[] types)
        {
            Debug.Assert(types != null);
            Items = types;
        }

        public int Count => Items.Length;

        public CType[] Items { get; }

        public CType this[int i] => Items[i];

        [Conditional("DEBUG")]
        public void AssertValid()
        {
            Debug.Assert(Count >= 0);
            Debug.Assert(Items.All(item => item != null));
        }

        public void CopyItems(int i, int c, CType[] dest) => Array.Copy(Items, i, dest, 0, c);
    }
}
