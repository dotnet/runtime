// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

namespace System.Diagnostics
{
    /// <devdoc>
    ///     A process module component represents a DLL or EXE loaded into
    ///     a particular process.  Using this component, you can determine
    ///     information about the module.
    /// </devdoc>
    public class ProcessModule : Component
    {
        private FileVersionInfo _fileVersionInfo;

        internal ProcessModule() { }

        /// <devdoc>
        ///     Returns the name of the Module.
        /// </devdoc>
        public string ModuleName { get; internal set; }

        /// <devdoc>
        ///     Returns the full file path for the location of the module.
        /// </devdoc>
        public string FileName { get; internal set; }

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
        public FileVersionInfo FileVersionInfo => _fileVersionInfo ?? (_fileVersionInfo = FileVersionInfo.GetVersionInfo(FileName));

        public override string ToString() => $"{base.ToString()} ({ModuleName})";
    }
}
