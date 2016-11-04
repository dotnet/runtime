// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///    <para>[To be supplied.]</para>
    /// </summary>
    public class DesignerVerbCollection : CollectionBase
    {
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public DesignerVerbCollection()
        {
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public DesignerVerbCollection(DesignerVerb[] value)
        {
            AddRange(value);
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public DesignerVerb this[int index]
        {
            get
            {
                return (DesignerVerb)(List[index]);
            }
            set
            {
                List[index] = value;
            }
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public int Add(DesignerVerb value)
        {
            return List.Add(value);
        }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public void AddRange(DesignerVerb[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            for (int i = 0; ((i) < (value.Length)); i = ((i) + (1)))
            {
                Add(value[i]);
            }
        }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public void AddRange(DesignerVerbCollection value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            int currentCount = value.Count;
            for (int i = 0; i < currentCount; i = ((i) + (1)))
            {
                Add(value[i]);
            }
        }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public void Insert(int index, DesignerVerb value)
        {
            List.Insert(index, value);
        }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public int IndexOf(DesignerVerb value)
        {
            return List.IndexOf(value);
        }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public bool Contains(DesignerVerb value)
        {
            return List.Contains(value);
        }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public void Remove(DesignerVerb value)
        {
            List.Remove(value);
        }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public void CopyTo(DesignerVerb[] array, int index)
        {
            List.CopyTo(array, index);
        }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        protected override void OnSet(int index, object oldValue, object newValue)
        {
        }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        protected override void OnInsert(int index, object value)
        {
        }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        protected override void OnClear()
        {
        }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        protected override void OnRemove(int index, object value)
        {
        }
        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        protected override void OnValidate(object value)
        {
            Debug.Assert(value != null, "Don't add null verbs!");
        }
    }
}

