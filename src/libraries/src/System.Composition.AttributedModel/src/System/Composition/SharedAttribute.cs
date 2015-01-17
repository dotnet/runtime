// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Composition
{
    /// <summary>
    /// Marks a part as being constrainted to sharing within the named boundary.
    /// </summary>
    /// <example>
    /// [Export,
    ///  Shared("HttpRequest")]
    /// public class HttpResponseWriter { }
    /// </example>
    /// <seealso cref="SharingBoundaryAttribute"/>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class SharedAttribute : PartMetadataAttribute
    {
        private const string SharingBoundaryPartMetadataName = "SharingBoundary";

        /// <summary>
        /// Mark a part as globally shared.
        /// </summary>
        public SharedAttribute()
            : base(SharingBoundaryPartMetadataName, null)
        {
        }

        /// <summary>
        /// Construct a <see cref="SharedAttribute"/> for the specified
        /// boundary name.
        /// </summary>
        /// <param name="sharingBoundaryName">The boundary outside of which this part is inaccessible.</param>
        public SharedAttribute(string sharingBoundaryName)
            : base(SharingBoundaryPartMetadataName, sharingBoundaryName)
        {
        }

        /// <summary>
        /// he boundary outside of which this part is inaccessible.
        /// </summary>
        public string SharingBoundary { get { return (string)base.Value; } }
    }
}
