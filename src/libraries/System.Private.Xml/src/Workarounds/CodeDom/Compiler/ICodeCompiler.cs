// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Compiler
{
    using System.Diagnostics;
    using System.IO;

    /// <devdoc>
    ///    <para>
    ///       Provides a
    ///       code compilation
    ///       interface.
    ///    </para>
    /// </devdoc>
    internal interface ICodeCompiler
    {
        /// <devdoc>
        ///    <para>
        ///       Creates an assembly based on options, with the information from the compile units
        ///    </para>
        /// </devdoc>
        CompilerResults CompileAssemblyFromDom(CompilerParameters options, CodeCompileUnit compilationUnit);

        /// <devdoc>
        ///    <para>
        ///       Creates an assembly based on options, with the contents of
        ///       fileName.
        ///    </para>
        /// </devdoc>
        CompilerResults CompileAssemblyFromFile(CompilerParameters options, string fileName);

        /// <devdoc>
        ///    <para>
        ///       Creates an assembly based on options, with the information from
        ///       source.
        ///    </para>
        /// </devdoc>
        CompilerResults CompileAssemblyFromSource(CompilerParameters options, string source);

        /// <devdoc>
        ///    <para>
        ///       Compiles an assembly based on the specified options and
        ///       information.
        ///    </para>
        /// </devdoc>
        CompilerResults CompileAssemblyFromDomBatch(CompilerParameters options, CodeCompileUnit[] compilationUnits);

        /// <devdoc>
        ///    <para>
        ///       Compiles
        ///       an
        ///       assembly based on the specified options and contents of the specified
        ///       filenames.
        ///    </para>
        /// </devdoc>
        CompilerResults CompileAssemblyFromFileBatch(CompilerParameters options, string[] fileNames);

        /// <devdoc>
        ///    <para>
        ///       Compiles an assembly based on the specified options and information from the specified
        ///       sources.
        ///    </para>
        /// </devdoc>
        CompilerResults CompileAssemblyFromSourceBatch(CompilerParameters options, string[] sources);
    }
}
