// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;

namespace System.Net.Mail
{
    public sealed class LinkedResourceCollection : Collection<LinkedResource>, IDisposable
    {
        private bool _disposed = false;
        internal LinkedResourceCollection()
        { }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            foreach (LinkedResource resource in this)
            {
                resource.Dispose();
            }
            Clear();
            _disposed = true;
        }

        protected override void RemoveItem(int index)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            base.RemoveItem(index);
        }

        protected override void ClearItems()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            base.ClearItems();
        }

        protected override void SetItem(int index, LinkedResource item)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            base.SetItem(index, item);
        }

        protected override void InsertItem(int index, LinkedResource item)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            base.InsertItem(index, item);
        }
    }
}
