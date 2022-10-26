// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//
// Main.cs: Main program file of command line utility.
//
// Author:
//   Radek Doulik (rodo@xamarin.com)
//
// Copyright 2015 Xamarin Inc (http://www.xamarin.com).
//

using System;
using LinkerAnalyzer.Core;
using Mono.Options;

namespace LinkerAnalyzer
{
	static class MainClass
	{
		static void Main (string[] args)
		{
			bool showUsage = true;
			bool showAllDeps = false;
			bool showTypeDeps = false;
			string typeName = null;
			bool showRawDeps = false;
			string rawName = null;
			bool showRoots = false;
			bool showStat = false;
			bool showTypes = false;
			bool reduceToTree = false;
			bool verbose = false;
			bool flatDeps = false;
			string linkedPath = null;

			var optionsParser = new OptionSet () {
				{ "a|alldeps", "show all dependencies", v => { showAllDeps = v != null; } },
				{ "h|help", "show this message and exit.", v => showUsage = v != null },
				{ "l|linkedpath=", "sets the linked assemblies directory path. Enables displaying size estimates.", v => { linkedPath = v; } },
				{ "r|rawdeps=", "show raw vertex dependencies. Raw vertex VALUE is in the raw format written by linker to the dependency XML file. VALUE can be regular expression", v => { showRawDeps = v != null; rawName = v; } },
				{ "roots", "show root dependencies.", v => showRoots = v != null },
				{ "stat", "show statistic of loaded dependencies.", v => showStat = v != null },
				{ "tree", "reduce the dependency graph to the tree.", v => reduceToTree = v != null },
				{ "types", "show all types dependencies.", v => showTypes = v != null },
				{ "t|typedeps=", "show type dependencies. The VALUE can be regular expression", v => { showTypeDeps = v != null; typeName = v; } },
				{ "f|flat", "show all dependencies per vertex and their distance", v => flatDeps = v != null },
				{ "v|verbose", "be more verbose. Enables stat and roots options.", v => verbose = v != null },
			};

			if (args.Length > 0) {
				showUsage = false;
				optionsParser.Parse (args);
			}

			if (showUsage) {
				Console.WriteLine ("Usage:\n\n\tillinkanalyzer [Options] <linker-dependency-file.xml.gz>\n\nOptions:\n");
				optionsParser.WriteOptionDescriptions (Console.Out);
				Console.WriteLine ();
				return;
			}

			string dependencyFile = args[args.Length - 1];

			ConsoleDependencyGraph deps = new ConsoleDependencyGraph () { Tree = reduceToTree, FlatDeps = flatDeps };
			deps.Load (dependencyFile);

			if (linkedPath != null) {
				deps.SpaceAnalyzer = new SpaceAnalyzer (linkedPath);
				deps.SpaceAnalyzer.LoadAssemblies (verbose);
			}

			if (verbose) {
				showStat = true;
				showRoots = true;
			}

			if (showStat)
				deps.ShowStat (verbose);
			if (showRoots)
				deps.ShowRoots ();
			if (showRawDeps)
				deps.ShowRawDependencies (rawName);
			if (showTypeDeps)
				deps.ShowTypeDependencies (typeName);
			if (showAllDeps)
				deps.ShowAllDependencies ();
			else if (showTypes)
				deps.ShowTypesDependencies ();
		}
	}
}
