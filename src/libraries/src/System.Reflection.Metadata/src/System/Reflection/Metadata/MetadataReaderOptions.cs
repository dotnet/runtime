// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata
{
    [Flags]
    public enum MetadataReaderOptions
    {
        /// <summary>
        /// All options are disabled.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// The options that are used when a <see cref="MetadataReader"/> is obtained
        /// via an overload that does not take a <see cref="MetadataReaderOptions"/>
        /// argument.
        /// </summary>
        Default = ApplyWindowsRuntimeProjections,

        /// <summary>
        /// Windows Runtime projections are enabled (on by default).
        /// </summary>
        ApplyWindowsRuntimeProjections = 0x1
    }
}
