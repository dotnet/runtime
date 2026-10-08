// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.NET.HostModel.Bundle
{
    /// <summary>
    /// Describes the contents of a bundle.
    /// </summary>
    public sealed class BundleContents
    {
        /// <summary>
        /// The host binary that serves as the bundle container.
        /// </summary>
        public FileSpec Host { get; }

        /// <summary>
        /// Files that will be embedded in the bundle.
        /// </summary>
        public IReadOnlyList<FileSpec> IncludedFiles { get; }

        /// <summary>
        /// Files that are excluded from the bundle and should be published alongside the host.
        /// </summary>
        public IReadOnlyList<FileSpec> ExcludedFiles { get; }

        internal (FileSpec Spec, FileType Type)[] TypedIncludedFiles { get; }

        internal BundleContents(FileSpec host, (FileSpec Spec, FileType Type)[] includedFiles, FileSpec[] excludedFiles)
        {
            Host = host;
            TypedIncludedFiles = includedFiles;
            IncludedFiles = System.Array.ConvertAll(includedFiles, x => x.Spec);
            ExcludedFiles = excludedFiles;
        }
    }
}
