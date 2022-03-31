// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Runtime.CompilerServices;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public static class PathUtilities
	{
#if DEBUG
		public const string ConfigDirectoryName = "Debug";
#else
		public const string ConfigDirectoryName = "Release";
#endif

#if NET6_0
		public const string TFMDirectoryName = "net6.0";
#elif NET5_0
		public const string TFMDirectoryName = "net5.0";
#elif NETCOREAPP3_0
		public const string TFMDirectoryName = "netcoreapp3.0";
#elif NET471
		public const string TFMDirectoryName = "net471";
#else
#error "Unknown TFM"
#endif

		public static string GetTestsSourceRootDirectory ([CallerFilePath] string thisFile = null)
		{
#if NETCOREAPP
			// Deterministic builds sanitize source paths, so CallerFilePathAttribute gives an incorrect path.
			// Instead, get the testcase dll based on the working directory of the test runner.

			// working directory is artifacts/bin/Mono.Linker.Tests/<config>/<tfm>
			var artifactsBinDir = Path.Combine (Directory.GetCurrentDirectory (), "..", "..", "..");
			return Path.GetFullPath (Path.Combine (artifactsBinDir, "..", "..", "test"));
#else
			var thisDirectory = Path.GetDirectoryName (thisFile);
			return Path.GetFullPath (Path.Combine (thisDirectory, "..", ".."));
#endif
		}

		public static string GetTestAssemblyPath (string assemblyName)
		{
#if NETCOREAPP
			return Path.GetFullPath (Path.Combine (GetTestsSourceRootDirectory (), "..", "artifacts", "bin", assemblyName, ConfigDirectoryName, TFMDirectoryName, $"{assemblyName}.dll"));
#else
			return Path.GetFullPath (Path.Combine (GetTestsSourceRootDirectory (), assemblyName, "bin", ConfigDirectoryName, TFMDirectoryName, $"{assemblyName}.dll"));
#endif
		}
	}
}
