// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Provides zero-allocation string comparison helpers to query strings in metadata.
    /// </summary>
    ///
    /// <remarks>
    /// Obtain instances using <see cref="MetadataReader.StringComparer"/>.
    ///
    /// A default-initialized instance is useless and behaves as a null reference.
    ///
    /// The code is optimized such that there is no additional overhead in
    /// re-obtaining a a comparer over hoisting it in to a local.
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
    public struct MetadataStringComparer
    {
        private readonly MetadataReader reader;

        internal MetadataStringComparer(MetadataReader reader)
        {
            Debug.Assert(reader != null);
            this.reader = reader;
        }

        public bool Equals(StringHandle handle, string value)
        {
            if (value == null)
            {
                ThrowValueArgumentNull();
            }

            return reader.StringStream.Equals(handle, value, reader.utf8Decoder);
        }

        public bool Equals(NamespaceDefinitionHandle handle, string value)
        {
            if (value == null)
            {
                ThrowValueArgumentNull();
            }

            if (handle.HasFullName)
            {
                return reader.StringStream.Equals(handle.GetFullName(), value, reader.utf8Decoder);
            }

            return value == reader.namespaceCache.GetFullName(handle);
        }

        public bool StartsWith(StringHandle handle, string value)
        {
            if (value == null)
            {
                ThrowValueArgumentNull();
            }

            return reader.StringStream.StartsWith(handle, value, reader.utf8Decoder);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowValueArgumentNull()
        {
            throw new ArgumentNullException("value");
        }
    }
}
