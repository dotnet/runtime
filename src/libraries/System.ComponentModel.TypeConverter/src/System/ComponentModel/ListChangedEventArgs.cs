// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    public class ListChangedEventArgs : EventArgs
    {
        public ListChangedEventArgs(ListChangedType listChangedType, int newIndex) : this(listChangedType, newIndex, -1)
        {
        }

        public ListChangedEventArgs(ListChangedType listChangedType, int newIndex, PropertyDescriptor? propDesc) : this(listChangedType, newIndex)
        {
            PropertyDescriptor = propDesc;
            OldIndex = newIndex;
        }

        public ListChangedEventArgs(ListChangedType listChangedType, PropertyDescriptor? propDesc)
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

        public PropertyDescriptor? PropertyDescriptor { get; }
    }
}
