// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable enable

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Attribute used to indicate a source generator should create a function for marshalling
    /// arguments instead of relying on the runtime to generate an equivalent marshalling function at run-time.
    /// </summary>
    /// <remarks>
    /// This attribute is meaningless if the source generator associated with it is not enabled.
    /// The current built-in source generator only supports C# and only supplies an implementation when
    /// applied to static, partial, non-generic methods.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
#pragma warning disable CS0436 // Type conflicts with imported type
                               // Some assemblies that target downlevel have InternalsVisibleTo to their test assembiles.
                               // As this is only used in this repo and isn't a problem in shipping code,
                               // just disable the duplicate type warning.
    internal
#endif
    sealed class LibraryImportAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryImportAttribute"/>.
        /// </summary>
        /// <param name="libraryName">Name of the library containing the import.</param>
        public LibraryImportAttribute(string libraryName)
        {
            LibraryName = libraryName;
        }

        /// <summary>
        /// Gets the name of the library containing the import.
        /// </summary>
        public string LibraryName { get; }

        /// <summary>
        /// Gets or sets the name of the entry point to be called.
        /// </summary>
        public string? EntryPoint { get; set; }

        /// <summary>
        /// Gets or sets how to marshal string arguments to the method.
        /// </summary>
        /// <remarks>
        /// If this field is set to a value other than <see cref="StringMarshalling.Custom" />,
        /// <see cref="StringMarshallingCustomType" /> must not be specified.
        /// </remarks>
        public StringMarshalling StringMarshalling { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Type"/> used to control how string arguments to the method are marshalled.
        /// </summary>
        /// <remarks>
        /// If this field is specified, <see cref="StringMarshalling" /> must not be specified
        /// or must be set to <see cref="StringMarshalling.Custom" />.
        /// </remarks>
        public Type? StringMarshallingCustomType { get; set; }

        /// <summary>
        /// Gets or sets whether the callee sets an error (SetLastError on Windows or errno
        /// on other platforms) before returning from the attributed method.
        /// </summary>
        public bool SetLastError { get; set; }
    }
}
