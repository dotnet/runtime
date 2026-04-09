// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.ObjectModel;

namespace System.Net.Mail
{
    /// <summary>
    /// Summary description for AttachmentCollection.
    /// </summary>
    public sealed class AttachmentCollection : Collection<Attachment>, IDisposable
    {
        private bool _disposed;
        internal AttachmentCollection() { }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            foreach (Attachment attachment in this)
            {
                attachment.Dispose();
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

        protected override void SetItem(int index, Attachment item)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(item);
            base.SetItem(index, item);
        }

        protected override void InsertItem(int index, Attachment item)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(item);
            base.InsertItem(index, item);
        }
    }
}
