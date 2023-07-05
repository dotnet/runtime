// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Options;
using TLens.Analyzers;

namespace TLens
{
	sealed class Driver
	{
		static int Main (string[] args)
		{
			bool showUsage = false;
			bool error = false;

			var runner = new Runner ();
			var analyzers = new List<Analyzer> ();
			var dirs = new List<string> ();

			var options = new OptionSet {
				{ "l|lens=", "{NAME} of the lens to use. Default subset is used if not specified.",
					l => {
						var lens = LensesCollection.GetLensByName (l);
						if (lens != null)
							analyzers.Add (lens);
						else
						{
							Console.WriteLine ($"Error: Lens name '{l}' does not exist.");
							error = true;
						}
					}},

				{ "d|dir=", "Additional location {PATH} to look for assembly references.",
					l => {
						if (!Directory.Exists (l)) {
							Console.WriteLine ($"Error: Directory '{l}' does not exist.");
							error = true;
						} else {
							dirs.Add (Path.GetFullPath (l));
						}
					}},

				{ "all-lenses", "Uses all lenses available.",
					l => analyzers.AddRange (LensesCollection.AllAnalyzers) },
				{ "h|help", "Show this message and exit.",
					v => showUsage = v != null },
				{ "limit=", "Maximum number of findings reported by lens (defaults to 30).",
					l => runner.MaxAnalyzerResults = int.Parse (l) },
			};

			List<string> files;

			try {
				files = options.Parse (args);
			} catch (OptionException e) {
				Console.WriteLine (e.Message);
				Console.WriteLine ("Try `tlens --help' for more information.");
				return 1;
			}

			if (showUsage) {
				ShowUsage (options);
				return 0;
			}

			if (files.Count == 0) {
				Console.WriteLine ("Error: No input files were specified.");
				return 1;
			}

			if (error)
				return 1;

			foreach (var f in files) {
				var d = Path.GetDirectoryName (Path.GetFullPath (f));
				if (dirs.Contains (d))
					continue;

				dirs.Add (d);
			}

			var resolver = new AssemlyReferenceResolver (dirs.ToArray ());
			var assemblies = LoadFiles (files, resolver);

			// Set default set of lenses if none was used
			if (analyzers.Count == 0) {
				runner.AddAnalyzers (LensesCollection.DefaultAnalyzers);
			} else {
				runner.AddAnalyzers (analyzers);
			}

			runner.Process (assemblies);

			return 0;
		}

		static void ShowUsage (OptionSet options)
		{
			Console.WriteLine ("Trimming Lens");
			Console.WriteLine ("  tlens [options] input-files");
			Console.WriteLine ();
			Console.WriteLine ("Options:");

			options.WriteOptionDescriptions (Console.Out);

			Console.WriteLine ("Lenses Names:");
			foreach (var lense in LensesCollection.All.OrderBy (l => l.Name)) {

				Console.WriteLine ($"  {lense.Name,-27}{lense.Description}");
			}
		}

		static List<AssemblyDefinition> LoadFiles (List<string> files, AssemlyReferenceResolver resolver)
		{
			var assemblies = new List<AssemblyDefinition> ();
			foreach (var file in files) {
				if (!File.Exists (file)) {
					Console.WriteLine ($"File {file} could not be found.");
					continue;
				}

				var ad = AssemblyDefinition.ReadAssembly (file, resolver.ReaderParameters);
				assemblies.Add (ad);
			}

			return assemblies;
		}
	}
}
