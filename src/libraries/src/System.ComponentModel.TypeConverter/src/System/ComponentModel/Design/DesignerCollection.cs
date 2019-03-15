// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Provides a read-only collection of documents.
    /// </summary>
    public class DesignerCollection : ICollection
    {
        private IList _designers;

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.Design.DesignerCollection'/> class
        /// that stores an array with a pointer to each <see cref='System.ComponentModel.Design.IDesignerHost'/>
        /// for each document in the collection.
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
        /// Initializes a new instance of the <see cref='System.ComponentModel.Design.DesignerCollection'/> class
        /// that stores an array with a pointer to each <see cref='System.ComponentModel.Design.IDesignerHost'/>
        /// for each document in the collection.
        /// </summary>
        public DesignerCollection(IList designers)
        {
            _designers = designers ?? new ArrayList();
        }

        /// <summary>
        /// Gets or sets the number of documents in the collection.
        /// </summary>
        public int Count => _designers.Count;

        /// <summary>
        /// Gets or sets the document at the specified index.
        /// </summary>
        public virtual IDesignerHost this[int index] => (IDesignerHost)_designers[index];

        /// <summary>
        /// Creates and retrieves a new enumerator for this collection.
        /// </summary>
        public IEnumerator GetEnumerator() => _designers.GetEnumerator();

        int ICollection.Count => Count;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => null;

        void ICollection.CopyTo(Array array, int index) => _designers.CopyTo(array, index);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
