// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCases;

namespace Mono.Linker.Tests.TestCasesRunner
{
	partial class TestCaseSandbox
	{

		static readonly string _linkerAssemblyPath = typeof (Driver).Assembly.Location;

		private static partial NPath GetArtifactsTestPath ()
		{
			// Converts paths like /root-folder/linker/artifacts/bin/Mono.Linker.Tests/Debug/<tfm>/illink.dll
			// to /root-folder/linker/artifacts/testcases/
			string artifacts = Path.GetFullPath (Path.Combine (Path.GetDirectoryName (_linkerAssemblyPath), "..", "..", "..", ".."));
			string tests = Path.Combine (artifacts, "testcases");
			return new NPath (tests);
		}
	}
}
