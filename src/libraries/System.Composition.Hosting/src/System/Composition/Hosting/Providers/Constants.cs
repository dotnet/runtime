// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Composition.Hosting.Providers
{
    /// <summary>
    /// Metadata keys used to tie programming model entities into their back-end hosting implementations.
    /// </summary>
    internal static class Constants
    {
        /// <summary>
        /// The sharing boundary implemented by an import.
        /// </summary>
        public const string SharingBoundaryImportMetadataConstraintName = "SharingBoundaryNames";

        /// <summary>
        /// Marks an import as "many".
        /// </summary>
        public const string ImportManyImportMetadataConstraintName = "IsImportMany";
    }
}
