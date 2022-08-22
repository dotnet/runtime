// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Extensions;

#nullable disable

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class SetupCompileInfo
	{
		public string OutputName;
		public NPath[] SourceFiles;
		public string[] Defines;
		public string[] References;
		public SourceAndDestinationPair[] Resources;
		public string AdditionalArguments;
		public string CompilerToUse;
		public bool AddAsReference;
		public bool RemoveFromLinkerInput;
		public string OutputSubFolder;
	}
}
