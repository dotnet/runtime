// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Extensions;

#nullable disable

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
