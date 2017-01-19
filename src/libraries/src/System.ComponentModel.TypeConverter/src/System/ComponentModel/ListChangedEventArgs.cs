// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Security.Permissions;
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "member", Target = "System.ComponentModel.ListChangedEventArgs..ctor(System.ComponentModel.ListChangedType,System.Int32,System.ComponentModel.PropertyDescriptor)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "member", Target = "System.ComponentModel.ListChangedEventArgs..ctor(System.ComponentModel.ListChangedType,System.ComponentModel.PropertyDescriptor)")]

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>[To be supplied.]</para>
    /// </summary>
    public class ListChangedEventArgs : EventArgs
    {
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public ListChangedEventArgs(ListChangedType listChangedType, int newIndex) : this(listChangedType, newIndex, -1)
        {
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public ListChangedEventArgs(ListChangedType listChangedType, int newIndex, PropertyDescriptor propDesc) : this(listChangedType, newIndex)
        {
            PropertyDescriptor = propDesc;
            OldIndex = newIndex;
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public ListChangedEventArgs(ListChangedType listChangedType, PropertyDescriptor propDesc)
        {
            Debug.Assert(listChangedType != ListChangedType.Reset, "this constructor is used only for changes in the list MetaData");
            Debug.Assert(listChangedType != ListChangedType.ItemAdded, "this constructor is used only for changes in the list MetaData");
            Debug.Assert(listChangedType != ListChangedType.ItemDeleted, "this constructor is used only for changes in the list MetaData");
            Debug.Assert(listChangedType != ListChangedType.ItemChanged, "this constructor is used only for changes in the list MetaData");

            ListChangedType = listChangedType;
            PropertyDescriptor = propDesc;
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public ListChangedEventArgs(ListChangedType listChangedType, int newIndex, int oldIndex)
        {
            Debug.Assert(listChangedType != ListChangedType.PropertyDescriptorAdded, "this constructor is used only for item changed in the list");
            Debug.Assert(listChangedType != ListChangedType.PropertyDescriptorDeleted, "this constructor is used only for item changed in the list");
            Debug.Assert(listChangedType != ListChangedType.PropertyDescriptorChanged, "this constructor is used only for item changed in the list");
            ListChangedType = listChangedType;
            NewIndex = newIndex;
            OldIndex = oldIndex;
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public ListChangedType ListChangedType { get; }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public int NewIndex { get; }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public int OldIndex { get; }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public PropertyDescriptor PropertyDescriptor { get; }
    }
}


