// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace System.Runtime.Serialization
{
    /// <summary>
    /// Represents a DataContract surrogate provider that is capable of modifying generated type code using <see cref="System.CodeDom"/>.
    /// </summary>
    public interface ISerializationCodeDomSurrogateProvider
    {
        /// <summary>
        /// Processes the type that has been generated from the imported schema.
        /// </summary>
        /// <param name="typeDeclaration">A <see cref="CodeTypeDeclaration"/> to process that represents the type declaration generated during schema import.</param>
        /// <param name="compileUnit">The <see cref="CodeCompileUnit"/> that contains the other code generated during schema import.</param>
        /// <returns>A <see cref="CodeTypeDeclaration"/> that contains the processed type.</returns>
        CodeTypeDeclaration ProcessImportedType(CodeTypeDeclaration typeDeclaration, CodeCompileUnit compileUnit);
    }
}
