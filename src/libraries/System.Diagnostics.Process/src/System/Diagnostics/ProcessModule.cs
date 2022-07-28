// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Diagnostics
{
    /// <devdoc>
    ///     A process module component represents a DLL or EXE loaded into
    ///     a particular process.  Using this component, you can determine
    ///     information about the module.
    /// </devdoc>
    [Designer("System.Diagnostics.Design.ProcessModuleDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ProcessModule : Component
    {
        private readonly string _fileName;
        private readonly string _moduleName;
        private FileVersionInfo? _fileVersionInfo;

        internal ProcessModule(string fileName, string moduleName)
        {
            _fileName = fileName;
            _moduleName = moduleName;
        }

        /// <devdoc>
        ///     Returns the name of the Module.
        /// </devdoc>
        public string ModuleName => _moduleName;

        /// <devdoc>
        ///     Returns the full file path for the location of the module.
        /// </devdoc>
        public string FileName => _fileName;

        /// <devdoc>
        ///     Returns the memory address that the module was loaded at.
        /// </devdoc>
        public IntPtr BaseAddress { get; internal set; }

        /// <devdoc>
        ///     Returns the amount of memory required to load the module.  This does
        ///     not include any additional memory allocations made by the module once
        ///     it is running; it only includes the size of the static code and data
        ///     in the module file.
        /// </devdoc>
        public int ModuleMemorySize { get; internal set; }

        /// <devdoc>
        ///     Returns the memory address for function that runs when the module is
        ///     loaded and run.
        /// </devdoc>
        public IntPtr EntryPointAddress { get; internal set; }

        /// <devdoc>
        ///     Returns version information about the module.
        /// </devdoc>
        public FileVersionInfo FileVersionInfo => _fileVersionInfo ??= FileVersionInfo.GetVersionInfo(_fileName);

        public override string ToString() => $"{base.ToString()} ({ModuleName})";
    }
}
