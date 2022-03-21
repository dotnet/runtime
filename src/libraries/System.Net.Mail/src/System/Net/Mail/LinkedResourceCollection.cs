// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.ObjectModel;

namespace System.Net.Mail
{
    public sealed class LinkedResourceCollection : Collection<LinkedResource>, IDisposable
    {
        private bool _disposed;
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
            ObjectDisposedException.ThrowIf(_disposed, this);

            base.RemoveItem(index);
        }

        protected override void ClearItems()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            base.ClearItems();
        }

        protected override void SetItem(int index, LinkedResource item)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(item);
            base.SetItem(index, item);
        }

        protected override void InsertItem(int index, LinkedResource item)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(item);
            base.InsertItem(index, item);
        }
    }
}
