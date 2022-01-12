// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

//
// Types in this file are used for generated p/invokes (docs/design/features/source-generator-pinvokes.md).
//
namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Indicates that method will be generated at compile time and invoke into an unmanaged library entry point
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
#if DLLIMPORT_GENERATOR_TEST
    public
#else
    internal
#endif
    sealed class GeneratedDllImportAttribute : Attribute
    {
        public CharSet CharSet { get; set; }
        public string? EntryPoint { get; set; }
        public bool ExactSpelling { get; set; }
        public bool PreserveSig { get; set; }
        public bool SetLastError { get; set; }

        public GeneratedDllImportAttribute(string dllName)
        {
            this.Value = dllName;
        }

        public string Value { get; private set; }
    }
}
