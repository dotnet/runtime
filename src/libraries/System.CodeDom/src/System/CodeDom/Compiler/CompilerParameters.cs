// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;

namespace System.CodeDom.Compiler
{
    public partial class CompilerParameters
    {
        private TempFileCollection _tempFiles;

        public CompilerParameters() : this(null, null)
        {
        }

        public CompilerParameters(string[] assemblyNames) :
            this(assemblyNames, null, false)
        {
        }

        public CompilerParameters(string[] assemblyNames, string outputName) :
            this(assemblyNames, outputName, false)
        {
        }

        public CompilerParameters(string[] assemblyNames, string outputName, bool includeDebugInformation)
        {
            if (assemblyNames != null)
            {
                ReferencedAssemblies.AddRange(assemblyNames);
            }
            OutputAssembly = outputName;
            IncludeDebugInformation = includeDebugInformation;
        }


        /// <summary>
        /// The "core" or "standard" assembly that contains basic types such as <code>Object</code>, <code>Int32</code> and the like
        /// that is to be used for the compilation.<br />
        /// If the value of this property is an empty string (or <code>null</code>), the default core assembly will be used by the
        /// compiler (depending on the compiler version this may be <code>mscorlib.dll</code> or <code>System.Runtime.dll</code> in
        /// a Framework or reference assembly directory).<br />
        /// If the value of this property is not empty, CodeDom will emit compiler options to not reference <em>any</em> assemblies
        /// implicitly during compilation. It will also explicitly reference the assembly file specified in this property.<br />
        /// For compilers that only implicitly reference the "core" or "standard" assembly by default, this option can be used on its own.
        /// For compilers that implicitly reference more assemblies on top of the "core" / "standard" assembly, using this option may require
        /// specifying additional entries in the <code>System.CodeDom.Compiler.<bold>ReferencedAssemblies</bold></code> collection.<br />
        /// Note: An <code>ICodeCompiler</code> / <code>CoodeDomProvider</code> implementation may choose to ignore this property.
        /// </summary>
        public string CoreAssemblyFileName { get; set; } = string.Empty;

        public bool GenerateExecutable { get; set; }

        public bool GenerateInMemory { get; set; }

        public StringCollection ReferencedAssemblies { get; } = new StringCollection();
        public string MainClass { get; set; }

        public string OutputAssembly { get; set; }

        public TempFileCollection TempFiles
        {
            get => _tempFiles ??= new TempFileCollection();
            set => _tempFiles = value;
        }

        public bool IncludeDebugInformation { get; set; }

        public bool TreatWarningsAsErrors { get; set; }

        public int WarningLevel { get; set; } = -1; // use compiler default

        public string CompilerOptions { get; set; }

        public string Win32Resource { get; set; }

        public StringCollection EmbeddedResources { get; } = new StringCollection();

        public StringCollection LinkedResources { get; } = new StringCollection();

        public IntPtr UserToken { get; set; }
    }
}
