// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.CodeDom.Compiler
{
    public interface ICodeGenerator
    {
        bool IsValidIdentifier(string value);
        void ValidateIdentifier(string value);
        string CreateEscapedIdentifier(string value);
        string CreateValidIdentifier(string value);
        string GetTypeOutput(CodeTypeReference type);
        bool Supports(GeneratorSupport supports);
        void GenerateCodeFromExpression(CodeExpression e, TextWriter w, CodeGeneratorOptions o);
        void GenerateCodeFromStatement(CodeStatement e, TextWriter w, CodeGeneratorOptions o);
        void GenerateCodeFromNamespace(CodeNamespace e, TextWriter w, CodeGeneratorOptions o);
        void GenerateCodeFromCompileUnit(CodeCompileUnit e, TextWriter w, CodeGeneratorOptions o);
        void GenerateCodeFromType(CodeTypeDeclaration e, TextWriter w, CodeGeneratorOptions o);
    }
}
