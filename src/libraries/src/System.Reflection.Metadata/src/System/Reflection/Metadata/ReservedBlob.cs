// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Represents a handle and a corresponding blob on a metadata heap that was reserved for future content update.
    /// </summary>
    public readonly struct ReservedBlob<THandle>
        where THandle : struct
    {
        public THandle Handle { get; }
        public Blob Content { get; }

        internal ReservedBlob(THandle handle, Blob content)
        {
            Handle = handle;
            Content = content;
        }

        /// <summary>
        /// Returns a <see cref="BlobWriter"/> to be used to update the content.
        /// </summary>
        public BlobWriter CreateWriter() => new BlobWriter(Content);
    }
}
