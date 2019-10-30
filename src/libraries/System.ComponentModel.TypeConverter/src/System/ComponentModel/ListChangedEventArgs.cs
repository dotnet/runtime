// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "member", Target = "System.ComponentModel.ListChangedEventArgs..ctor(System.ComponentModel.ListChangedType,System.Int32,System.ComponentModel.PropertyDescriptor)")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "member", Target = "System.ComponentModel.ListChangedEventArgs..ctor(System.ComponentModel.ListChangedType,System.ComponentModel.PropertyDescriptor)")]

namespace System.ComponentModel
{
    public class ListChangedEventArgs : EventArgs
    {
        public ListChangedEventArgs(ListChangedType listChangedType, int newIndex) : this(listChangedType, newIndex, -1)
        {
        }

        public ListChangedEventArgs(ListChangedType listChangedType, int newIndex, PropertyDescriptor propDesc) : this(listChangedType, newIndex)
        {
            PropertyDescriptor = propDesc;
            OldIndex = newIndex;
        }

        public ListChangedEventArgs(ListChangedType listChangedType, PropertyDescriptor propDesc)
        {
            ListChangedType = listChangedType;
            PropertyDescriptor = propDesc;
        }

        public ListChangedEventArgs(ListChangedType listChangedType, int newIndex, int oldIndex)
        {
            ListChangedType = listChangedType;
            NewIndex = newIndex;
            OldIndex = oldIndex;
        }

        public ListChangedType ListChangedType { get; }

        public int NewIndex { get; }

        public int OldIndex { get; }

        public PropertyDescriptor PropertyDescriptor { get; }
    }
}
