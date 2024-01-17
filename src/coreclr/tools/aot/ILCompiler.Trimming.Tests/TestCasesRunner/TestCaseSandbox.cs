// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCases;

namespace Mono.Linker.Tests.TestCasesRunner
{
	partial class TestCaseSandbox
	{
		private const string _linkerAssemblyPath = "";//typeof (Trimmer).Assembly.Location;

		private static partial NPath GetArtifactsTestPath ()
		{
			// Converts paths like /root-folder/runtime/artifacts/bin/Mono.Linker.Tests/x64/Debug/Mono.Linker.Tests.dll
			// to /root-folder/runtime/artifacts/bin/ILLink.testcases/
			string artifacts = (string) AppContext.GetData ("Mono.Linker.Tests.ArtifactsBinDir")!;
			string tests = Path.Combine (artifacts, "ILLink.testcases");
			return new NPath (tests);
		}
	}
}
