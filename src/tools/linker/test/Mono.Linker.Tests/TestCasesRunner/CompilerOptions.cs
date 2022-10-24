// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Mono.Linker.Tests.Extensions;

namespace Mono.Linker.Tests.TestCasesRunner
{
    public class CompilerOptions
    {
        public NPath OutputPath;
        public NPath[] SourceFiles;
        public string[] Defines;
        public NPath[] References;
        public NPath[] Resources;
        public string[] AdditionalArguments;
        public string CompilerToUse;
    }
}
