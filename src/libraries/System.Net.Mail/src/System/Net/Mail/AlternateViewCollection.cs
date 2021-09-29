// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace System.Net.Mail
{
    public sealed class AlternateViewCollection : Collection<AlternateView>, IDisposable
    {
        private bool _disposed;

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
            ObjectDisposedException.ThrowIf(_disposed, this);

            base.RemoveItem(index);
        }

        protected override void ClearItems()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            base.ClearItems();
        }


        protected override void SetItem(int index, AlternateView item)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            base.SetItem(index, item);
        }

        protected override void InsertItem(int index, AlternateView item)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            base.InsertItem(index, item);
        }
    }
}
