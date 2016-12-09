// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///    <para>
    ///       Provides a read-only collection of documents.
    ///    </para>
    /// </summary>
    public class DesignerCollection : ICollection
    {
        private IList _designers;

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.Design.DesignerCollection'/> class
        ///       that stores an array with a pointer to each <see cref='System.ComponentModel.Design.IDesignerHost'/>
        ///       for each document in the collection.
        ///    </para>
        /// </summary>
        public DesignerCollection(IDesignerHost[] designers)
        {
            if (designers != null)
            {
                _designers = new ArrayList(designers);
            }
            else
            {
                _designers = new ArrayList();
            }
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.Design.DesignerCollection'/> class
        ///       that stores an array with a pointer to each <see cref='System.ComponentModel.Design.IDesignerHost'/>
        ///       for each document in the collection.
        ///    </para>
        /// </summary>
        public DesignerCollection(IList designers)
        {
            _designers = designers;
        }

        /// <summary>
        ///    <para>Gets or
        ///       sets the number
        ///       of documents in the collection.</para>
        /// </summary>
        public int Count => _designers.Count;

        /// <summary>
        ///    <para> Gets
        ///       or sets the document at the specified index.</para>
        /// </summary>
        public virtual IDesignerHost this[int index] => (IDesignerHost)_designers[index];

        /// <summary>
        ///    <para>Creates and retrieves a new enumerator for this collection.</para>
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            return _designers.GetEnumerator();
        }

        /// <internalonly/>
        int ICollection.Count => Count;

        /// <internalonly/>
        bool ICollection.IsSynchronized => false;

        /// <internalonly/>
        object ICollection.SyncRoot => null;

        /// <internalonly/>
        void ICollection.CopyTo(Array array, int index)
        {
            _designers.CopyTo(array, index);
        }

        /// <internalonly/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

