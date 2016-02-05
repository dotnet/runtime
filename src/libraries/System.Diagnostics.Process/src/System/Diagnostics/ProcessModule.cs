// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.Diagnostics
{
    /// <devdoc>
    ///     A process module component represents a DLL or EXE loaded into
    ///     a particular process.  Using this component, you can determine
    ///     information about the module.
    /// </devdoc>
    public class ProcessModule
    {
        private readonly ModuleInfo _moduleInfo;
#if FEATURE_FILEVERSIONINFO
        private FileVersionInfo _fileVersionInfo;
#endif

        /// <devdoc>
        ///     Initialize the module.
        /// </devdoc>
        /// <internalonly/>
        internal ProcessModule(ModuleInfo moduleInfo)
        {
            _moduleInfo = moduleInfo;
        }

        /// <devdoc>
        ///     Returns the name of the Module.
        /// </devdoc>
        public string ModuleName
        {
            get
            {
                return _moduleInfo._baseName;
            }
        }

        /// <devdoc>
        ///     Returns the full file path for the location of the module.
        /// </devdoc>
        public string FileName
        {
            get
            {
                return _moduleInfo._fileName;
            }
        }

        /// <devdoc>
        ///     Returns the memory address that the module was loaded at.
        /// </devdoc>
        public IntPtr BaseAddress
        {
            get
            {
                return _moduleInfo._baseOfDll;
            }
        }

        /// <devdoc>
        ///     Returns the amount of memory required to load the module.  This does
        ///     not include any additional memory allocations made by the module once
        ///     it is running; it only includes the size of the static code and data
        ///     in the module file.
        /// </devdoc>
        public int ModuleMemorySize
        {
            get
            {
                return _moduleInfo._sizeOfImage;
            }
        }

        /// <devdoc>
        ///     Returns the memory address for function that runs when the module is
        ///     loaded and run.
        /// </devdoc>
        public IntPtr EntryPointAddress
        {
            get
            {
                return _moduleInfo._entryPoint;
            }
        }

#if FEATURE_FILEVERSIONINFO
        /// <devdoc>
        ///     Returns version information about the module.
        /// </devdoc>
        public FileVersionInfo FileVersionInfo
        {
            [ResourceExposure(ResourceScope.Machine)]  // Let's review callers - why do they want this?
            [ResourceConsumption(ResourceScope.Machine)]
            get
            {
                if (_fileVersionInfo == null)
                    _fileVersionInfo = FileVersionInfo.GetVersionInfo(FileName);
                return _fileVersionInfo;
            }
        }
#endif

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0} ({1})", base.ToString(), ModuleName);
        }
    }
}
