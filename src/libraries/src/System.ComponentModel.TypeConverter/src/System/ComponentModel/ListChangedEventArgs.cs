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
        private ListChangedType _listChangedType;
        private int _newIndex;
        private int _oldIndex;
        private PropertyDescriptor _propDesc;

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
            _propDesc = propDesc;
            _oldIndex = newIndex;
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

            _listChangedType = listChangedType;
            _propDesc = propDesc;
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public ListChangedEventArgs(ListChangedType listChangedType, int newIndex, int oldIndex)
        {
            Debug.Assert(listChangedType != ListChangedType.PropertyDescriptorAdded, "this constructor is used only for item changed in the list");
            Debug.Assert(listChangedType != ListChangedType.PropertyDescriptorDeleted, "this constructor is used only for item changed in the list");
            Debug.Assert(listChangedType != ListChangedType.PropertyDescriptorChanged, "this constructor is used only for item changed in the list");
            _listChangedType = listChangedType;
            _newIndex = newIndex;
            _oldIndex = oldIndex;
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public ListChangedType ListChangedType
        {
            get
            {
                return _listChangedType;
            }
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public int NewIndex
        {
            get
            {
                return _newIndex;
            }
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public int OldIndex
        {
            get
            {
                return _oldIndex;
            }
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public PropertyDescriptor PropertyDescriptor
        {
            get
            {
                return _propDesc;
            }
        }
    }
}


