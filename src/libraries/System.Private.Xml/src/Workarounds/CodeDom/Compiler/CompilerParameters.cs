// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Compiler
{
    using System;
    using System.CodeDom;
    using System.Collections;
    using System.Collections.Specialized;
    using Microsoft.Win32;
    using Microsoft.Win32.SafeHandles;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using Evidence = System.Object;

    /// <devdoc>
    ///    <para>
    ///       Represents the parameters used in to invoke the compiler.
    ///    </para>
    /// </devdoc>
    internal class CompilerParameters
    {
        private string _coreAssemblyFileName = String.Empty;

        private StringCollection _assemblyNames = new StringCollection();

        private StringCollection _embeddedResources = new StringCollection();
        private StringCollection _linkedResources = new StringCollection();

        private string _outputName;
        private string _mainClass;
        private bool _generateInMemory = false;
        private bool _includeDebugInformation = false;
        private int _warningLevel = -1;  // -1 means not set (use compiler default)
        private string _compilerOptions;
        private string _win32Resource;
        private bool _treatWarningsAsErrors = false;
        private bool _generateExecutable = false;
        private TempFileCollection _tempFiles;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.Compiler.CompilerParameters'/>.
        ///    </para>
        /// </devdoc>
        public CompilerParameters() :
            this(null, null)
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.Compiler.CompilerParameters'/> using the specified
        ///       assembly names.
        ///    </para>
        /// </devdoc>
        public CompilerParameters(string[] assemblyNames) :
            this(assemblyNames, null, false)
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.Compiler.CompilerParameters'/> using the specified
        ///       assembly names and output name.
        ///    </para>
        /// </devdoc>
        public CompilerParameters(string[] assemblyNames, string outputName) :
            this(assemblyNames, outputName, false)
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.Compiler.CompilerParameters'/> using the specified
        ///       assembly names, output name and a whether to include debug information flag.
        ///    </para>
        /// </devdoc>
        public CompilerParameters(string[] assemblyNames, string outputName, bool includeDebugInformation)
        {
            if (assemblyNames != null)
            {
                ReferencedAssemblies.AddRange(assemblyNames);
            }
            _outputName = outputName;
            _includeDebugInformation = includeDebugInformation;
        }


        /// <summary>
        /// The "core" or "standard" assembly that contains basic types such as <code>Object</code>, <code>Int32</code> and the like
        /// that is to be used for the compilation.<br />
        /// If the value of this property is an empty string (or <code>null</code>), the default core assembly will be used by the
        /// compiler (depending on the compiler version this may be <code>mscorlib.dll</code> or <code>System.Runtime.dll</code> in
        /// a Framework or reference assembly directory).<br />
        /// If the value of this property is not empty, CodeDOM will emit compiler options to not reference <em>any</em> assemblies
        /// implicitly during compilation. It will also explicitly reference the assembly file specified in this property.<br />
        /// For compilers that only implicitly reference the "core" or "standard" assembly by default, this option can be used on its own.
        /// For compilers that implicitly reference more assemblies on top of the "core" / "standard" assembly, using this option may require
        /// specifying additional entries in the <code>System.CodeDom.Compiler.<bold>ReferencedAssemblies</bold></code> collection.<br />
        /// Note: An <code>ICodeCompiler</code> / <code>CoodeDomProvider</code> implementation may choose to ignore this property.
        /// </summary>
        public string CoreAssemblyFileName
        {
            get
            {
                return _coreAssemblyFileName;
            }
            set
            {
                _coreAssemblyFileName = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets whether to generate an executable.
        ///    </para>
        /// </devdoc>
        public bool GenerateExecutable
        {
            get
            {
                return _generateExecutable;
            }
            set
            {
                _generateExecutable = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets whether to generate in memory.
        ///    </para>
        /// </devdoc>
        public bool GenerateInMemory
        {
            get
            {
                return _generateInMemory;
            }
            set
            {
                _generateInMemory = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the assemblies referenced by the source to compile.
        ///    </para>
        /// </devdoc>
        public StringCollection ReferencedAssemblies
        {
            get
            {
                return _assemblyNames;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the main class.
        ///    </para>
        /// </devdoc>
        public string MainClass
        {
            get
            {
                return _mainClass;
            }
            set
            {
                _mainClass = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the output assembly.
        ///    </para>
        /// </devdoc>
        public string OutputAssembly
        {
            get
            {
                return _outputName;
            }
            set
            {
                _outputName = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the temp files.
        ///    </para>
        /// </devdoc>
        public TempFileCollection TempFiles
        {
            get
            {
                if (_tempFiles == null)
                    _tempFiles = new TempFileCollection();
                return _tempFiles;
            }
            set
            {
                _tempFiles = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets whether to include debug information in the compiled
        ///       executable.
        ///    </para>
        /// </devdoc>
        public bool IncludeDebugInformation
        {
            get
            {
                return _includeDebugInformation;
            }
            set
            {
                _includeDebugInformation = value;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool TreatWarningsAsErrors
        {
            get
            {
                return _treatWarningsAsErrors;
            }
            set
            {
                _treatWarningsAsErrors = value;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int WarningLevel
        {
            get
            {
                return _warningLevel;
            }
            set
            {
                _warningLevel = value;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string CompilerOptions
        {
            get
            {
                return _compilerOptions;
            }
            set
            {
                _compilerOptions = value;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string Win32Resource
        {
            get
            {
                return _win32Resource;
            }
            set
            {
                _win32Resource = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the resources to be compiled into the target
        ///    </para>
        /// </devdoc>
        [ComVisible(false)]
        public StringCollection EmbeddedResources
        {
            get
            {
                return _embeddedResources;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the linked resources
        ///    </para>
        /// </devdoc>
        [ComVisible(false)]
        public StringCollection LinkedResources
        {
            get
            {
                return _linkedResources;
            }
        }
    }
}
