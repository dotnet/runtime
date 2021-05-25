// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Indicates that the specified member requires assembly files to be on disk.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor |
                    AttributeTargets.Event |
                    AttributeTargets.Method |
                    AttributeTargets.Property,
                    Inherited = false,
                    AllowMultiple = false)]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
    internal
#endif
    sealed class RequiresAssemblyFilesAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequiresAssemblyFilesAttribute"/> class.
        /// </summary>
        public RequiresAssemblyFilesAttribute() { }

        /// <summary>
        /// Gets or sets an optional message that contains information about the need for
        /// assembly files to be on disk.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets an optional URL that contains more information about the member,
        /// why it requires assembly files to be on disk, and what options a consumer has
        /// to deal with it.
        /// </summary>
        public string? Url { get; set; }
    }
}
