// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Indicates that method will be generated at compile time and invoke into an unmanaged library entry point
    /// </summary>
    /// <remarks>
    /// IL linker/trimming currently has special handling of P/Invokes (pinvokeimpl):
    ///   - https://github.com/mono/linker/blob/bfab847356063d21eb15e79f2b6c03df5bd6ef3d/src/linker/Linker.Steps/MarkStep.cs#L2623
    /// We may want to make the linker aware of this attribute as well.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal sealed class GeneratedDllImportAttribute : Attribute
    {
        public bool BestFitMapping { get; set; }
        public CallingConvention CallingConvention { get; set; }
        public CharSet CharSet { get; set; }
        public string? EntryPoint { get; set; }
        public bool ExactSpelling { get; set; }
        public bool PreserveSig { get; set; }
        public bool SetLastError { get; set; }
        public bool ThrowOnUnmappableChar { get; set; }

        public GeneratedDllImportAttribute(string dllName)
        {
            this.Value = dllName;
        }

        public string Value { get; private set; }
    }
}
