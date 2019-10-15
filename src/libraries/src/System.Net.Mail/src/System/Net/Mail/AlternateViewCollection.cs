// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;

namespace System.Net.Mail
{
    public sealed class AlternateViewCollection : Collection<AlternateView>, IDisposable
    {
        private bool _disposed = false;

        internal AlternateViewCollection()
        { }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            foreach (AlternateView view in this)
            {
                view.Dispose();
            }
            Clear();
            _disposed = true;
        }

        protected override void RemoveItem(int index)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            base.RemoveItem(index);
        }

        protected override void ClearItems()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            base.ClearItems();
        }


        protected override void SetItem(int index, AlternateView item)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }


            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            base.SetItem(index, item);
        }

        protected override void InsertItem(int index, AlternateView item)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            base.InsertItem(index, item);
        }
    }
}
