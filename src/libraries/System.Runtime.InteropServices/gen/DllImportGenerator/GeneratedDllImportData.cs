using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Interop
{
    /// <summary>
    /// Flags used to indicate members on GeneratedDllImport attribute.
    /// </summary>
    [Flags]
    public enum DllImportMember
    {
        None = 0,
        BestFitMapping = 1 << 0,
        CallingConvention = 1 << 1,
        CharSet = 1 << 2,
        EntryPoint = 1 << 3,
        ExactSpelling = 1 << 4,
        PreserveSig = 1 << 5,
        SetLastError = 1 << 6,
        ThrowOnUnmappableChar = 1 << 7,
        All = ~None
    }

    /// <summary>
    /// GeneratedDllImportAttribute data
    /// </summary>
    /// <remarks>
    /// The names of these members map directly to those on the
    /// DllImportAttribute and should not be changed.
    /// </remarks>
    public sealed record GeneratedDllImportData(string ModuleName)
    {
        /// <summary>
        /// Value set by the user on the original declaration.
        /// </summary>
        public DllImportMember IsUserDefined { get; init; }
        public bool BestFitMapping { get; init; }
        public CallingConvention CallingConvention { get; init; }
        public CharSet CharSet { get; init; }
        public string? EntryPoint { get; init; }
        public bool ExactSpelling { get; init; }
        public bool PreserveSig { get; init; }
        public bool SetLastError { get; init; }
        public bool ThrowOnUnmappableChar { get; init; }
    }
}
