// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Compiler
{
    public interface ICodeCompiler
    {
        CompilerResults CompileAssemblyFromDom(CompilerParameters options, CodeCompileUnit compilationUnit);
        CompilerResults CompileAssemblyFromFile(CompilerParameters options, string fileName);
        CompilerResults CompileAssemblyFromSource(CompilerParameters options, string source);
        CompilerResults CompileAssemblyFromDomBatch(CompilerParameters options, CodeCompileUnit[] compilationUnits);
        CompilerResults CompileAssemblyFromFileBatch(CompilerParameters options, string[] fileNames);
        CompilerResults CompileAssemblyFromSourceBatch(CompilerParameters options, string[] sources);
    }
}
