// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Composition
{
    /// <summary>
    /// Marks a part as being constrained to sharing within the named boundary.
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
        public SharedAttribute() : base(SharingBoundaryPartMetadataName, null)
        {
        }

        /// <summary>
        /// Construct a <see cref="SharedAttribute"/> for the specified
        /// boundary name.
        /// </summary>
        /// <param name="sharingBoundaryName">The boundary outside of which this part is inaccessible.</param>
        public SharedAttribute(string sharingBoundaryName) : base(SharingBoundaryPartMetadataName, sharingBoundaryName)
        {
        }

        /// <summary>
        /// The boundary outside of which this part is inaccessible.
        /// </summary>
        public string SharingBoundary => (string)base.Value;
    }
}
