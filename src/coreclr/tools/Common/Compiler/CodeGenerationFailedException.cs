// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;

namespace ILCompiler
{
    public class CodeGenerationFailedException : InternalCompilerErrorException
    {
        private const string MessageText = "Code generation failed for method '{0}'";

        public MethodDesc Method { get; }

        public CodeGenerationFailedException(MethodDesc method)
            : this(method, null)
        {
        }

        public CodeGenerationFailedException(MethodDesc method, Exception inner)
            : base(string.Format(MessageText, method), inner)
        {
            Method = method;
        }
    }
}
