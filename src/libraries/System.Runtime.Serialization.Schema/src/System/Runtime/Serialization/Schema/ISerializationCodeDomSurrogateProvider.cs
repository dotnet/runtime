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
        /// A method to processes the type that has been generated from imported schema.
        /// </summary>
        CodeTypeDeclaration ProcessImportedType(CodeTypeDeclaration typeDeclaration, CodeCompileUnit compileUnit);
    }
}
