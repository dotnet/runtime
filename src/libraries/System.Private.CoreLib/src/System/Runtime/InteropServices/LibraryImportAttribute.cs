// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Attribute used to indicate a Source Generator should create a function for marshaling
    /// arguments instead of relying on the CLR to generate an IL Stub at runtime.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
    internal
#endif
    sealed class LibraryImportAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryImportAttribute"/>.
        /// </summary>
        /// <param name="libraryName">Name of the library containing the import</param>
        public LibraryImportAttribute(string libraryName)
        {
            LibraryName = libraryName;
        }

        /// <summary>
        /// Library to load.
        /// </summary>
        public string LibraryName { get; private set; }

        /// <summary>
        /// Indicates the name of the entry point to be called.
        /// </summary>
        public string? EntryPoint { get; set; }

        /// <summary>
        /// Indicates how to marshal string parameters to the method.
        /// </summary>
        /// <remarks>
        /// If this field is set to a value other than <see cref="StringMarshalling.Custom" />,
        /// <see cref="StringMarshallingCustomType" /> must not be specified.
        /// </remarks>
        public StringMarshalling StringMarshalling { get; set; }

        /// <summary>
        /// Indicates how to marshal string parameters to the method.
        /// </summary>
        /// <remarks>
        /// If this field is specified, <see cref="StringMarshalling" /> must not be specified
        /// or must be set to <see cref="StringMarshalling.Custom" />.
        /// </remarks>
        public Type? StringMarshallingCustomType { get; set; }

        /// <summary>
        /// Indicates whether the callee sents an error (SetLastError on Windows or errorno
        /// on other platforms) before returning from the attributed method.
        /// </summary>
        public bool SetLastError { get; set; }
    }
}
