// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Compiler
{
    using System.Diagnostics;
    using System.IO;

    /// <devdoc>
    ///    <para>
    ///       Provides an
    ///       interface for code generation.
    ///    </para>
    /// </devdoc>
    internal interface ICodeGenerator
    {
        /// <devdoc>
        ///    <para>
        ///       Gets a value indicating whether
        ///       the specified value is a valid identifier for this language.
        ///    </para>
        /// </devdoc>
        bool IsValidIdentifier(string value);

        /// <devdoc>
        ///    <para>
        ///       Throws an exception if value is not a valid identifier.
        ///    </para>
        /// </devdoc>
        void ValidateIdentifier(string value);

        string CreateEscapedIdentifier(string value);

        string CreateValidIdentifier(string value);

        string GetTypeOutput(CodeTypeReference type);

        bool Supports(GeneratorSupport supports);
    }
}
