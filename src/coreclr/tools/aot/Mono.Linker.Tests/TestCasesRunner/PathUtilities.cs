// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public static class PathUtilities
	{
#if NET7_0
		public const string TFMDirectoryName = "net7.0";
#elif NET6_0
		public const string TFMDirectoryName = "net6.0";
#else
#error "Unknown TFM"
#endif

		public static string GetTestsSourceRootDirectory ([CallerFilePath] string? thisFile = null) =>
			Path.GetFullPath ((string) AppContext.GetData ("Mono.Linker.Tests.NativeAotDir")!);

		public static string GetTestAssemblyPath (string assemblyName)
		{
			var artifactsBinDirectory = (string) AppContext.GetData ("Mono.Linker.Tests.ArtifactsBinDir")!;
			var targetArch = (string) AppContext.GetData ("Mono.Linker.Tests.TargetArchitecture")!;
			var configuration = (string) AppContext.GetData ("Mono.Linker.Tests.Configuration")!;
			return Path.GetFullPath (Path.Combine (artifactsBinDirectory, assemblyName, targetArch, configuration, $"{assemblyName}.dll"));
		}
	}
}
