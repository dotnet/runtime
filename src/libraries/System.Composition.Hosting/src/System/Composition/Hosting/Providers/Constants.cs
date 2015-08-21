// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
