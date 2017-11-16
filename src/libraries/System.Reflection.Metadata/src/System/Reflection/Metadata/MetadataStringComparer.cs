// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Provides string comparison helpers to query strings in metadata while
    /// avoiding allocation where possible.
    /// </summary>
    ///
    /// <remarks>
    /// No allocation is performed unless both the handle argument and the
    /// value argument contain non-ascii text.
    ///
    /// Obtain instances using <see cref="MetadataReader.StringComparer"/>.
    ///
    /// A default-initialized instance is useless and behaves as a null reference.
    ///
    /// The code is optimized such that there is no additional overhead in
    /// re-obtaining a comparer over hoisting it in to a local.
    /// 
    /// That is to say that a construct like:
    ///
    /// <code>
    /// if (reader.StringComparer.Equals(typeDef.Namespace, "System") &amp;&amp; 
    ///     reader.StringComparer.Equals(typeDef.Name, "Object")
    /// {
    ///     // found System.Object
    /// }
    /// </code>
    /// 
    /// is no less efficient than:
    /// 
    /// <code>
    /// var comparer = reader.StringComparer;
    /// if (comparer.Equals(typeDef.Namespace, "System") &amp;&amp;
    ///     comparer.Equals(typeDef.Name, "Object")
    /// {
    ///     // found System.Object
    /// }
    /// </code>
    ///
    /// The choice between them is therefore one of style and not performance.
    /// </remarks>
    public readonly struct MetadataStringComparer
    {
        private readonly MetadataReader _reader;

        internal MetadataStringComparer(MetadataReader reader)
        {
            Debug.Assert(reader != null);
            _reader = reader;
        }

        public bool Equals(StringHandle handle, string value)
        {
            return Equals(handle, value, ignoreCase: false);
        }

        public bool Equals(StringHandle handle, string value, bool ignoreCase)
        {
            if (value == null)
            {
                Throw.ValueArgumentNull();
            }

            return _reader.StringHeap.Equals(handle, value, _reader.UTF8Decoder, ignoreCase);
        }

        public bool Equals(NamespaceDefinitionHandle handle, string value)
        {
            return Equals(handle, value, ignoreCase: false);
        }

        public bool Equals(NamespaceDefinitionHandle handle, string value, bool ignoreCase)
        {
            if (value == null)
            {
                Throw.ValueArgumentNull();
            }

            if (handle.HasFullName)
            {
                return _reader.StringHeap.Equals(handle.GetFullName(), value, _reader.UTF8Decoder, ignoreCase);
            }

            return value == _reader.NamespaceCache.GetFullName(handle);
        }

        public bool Equals(DocumentNameBlobHandle handle, string value)
        {
            return Equals(handle, value, ignoreCase: false);
        }

        public bool Equals(DocumentNameBlobHandle handle, string value, bool ignoreCase)
        {
            if (value == null)
            {
                Throw.ValueArgumentNull();
            }

            return _reader.BlobHeap.DocumentNameEquals(handle, value, ignoreCase);
        }

        public bool StartsWith(StringHandle handle, string value)
        {
            return StartsWith(handle, value, ignoreCase: false);
        }

        public bool StartsWith(StringHandle handle, string value, bool ignoreCase)
        {
            if (value == null)
            {
                Throw.ValueArgumentNull();
            }

            return _reader.StringHeap.StartsWith(handle, value, _reader.UTF8Decoder, ignoreCase);
        }
    }
}
