// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.Drawing.Design
{
    /// <summary>
    /// A collection that stores <see cref='System.Drawing.Design.ToolboxItem'/> objects.
    /// </summary>
    public sealed class ToolboxItemCollection : ReadOnlyCollectionBase
    {
        /// <summary>
        /// Initializes a new instance of <see cref='System.Drawing.Design.ToolboxItemCollection'/> based on another <see cref='System.Drawing.Design.ToolboxItemCollection'/>.
        /// </summary>
        public ToolboxItemCollection(ToolboxItemCollection value)
        {
            InnerList.AddRange(value);
        }

        /// <summary>
        /// Initializes a new instance of <see cref='System.Drawing.Design.ToolboxItemCollection'/> containing any array of <see cref='System.Drawing.Design.ToolboxItem'/> objects.
        /// </summary>
        public ToolboxItemCollection(ToolboxItem[] value)
        {
            InnerList.AddRange(value);
        }

        /// <summary>
        /// Represents the entry at the specified index of the <see cref='System.Drawing.Design.ToolboxItem'/>.
        /// </summary>
        public ToolboxItem this[int index]
        {
            get
            {
                return ((ToolboxItem)(InnerList[index]));
            }
        }

        /// <summary>
        /// Gets a value indicating whether the 
        /// <see cref='System.Drawing.Design.ToolboxItemCollection'/> contains the specified <see cref='System.Drawing.Design.ToolboxItem'/>.
        /// </summary>
        public bool Contains(ToolboxItem value)
        {
            return InnerList.Contains(value);
        }

        /// <summary>
        /// Copies the <see cref='System.Drawing.Design.ToolboxItemCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the
        ///    specified index.
        /// </summary>
        public void CopyTo(ToolboxItem[] array, int index)
        {
            InnerList.CopyTo(array, index);
        }

        /// <summary>
        /// Returns the index of a <see cref='System.Drawing.Design.ToolboxItem'/> in
        /// the <see cref='System.Drawing.Design.ToolboxItemCollection'/> .
        /// </summary>
        public int IndexOf(ToolboxItem value)
        {
            return InnerList.IndexOf(value);
        }
    }
}
