// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using TLens.Analyzers;

namespace TLens
{
	static class LensesCollection
	{
		public sealed class LensAnalyzerDetails
		{
			public LensAnalyzerDetails (string name, string description, Type analyzerType)
			{
				Name = name;
				Description = description;
				AnalyzerType = analyzerType;
			}

			public string Name { get; }
			public string Description { get; }
			public Type AnalyzerType { get; }

			public bool DefaultSet { get; set; }

			public Analyzer CreateAnalyzer ()
			{
				return (Analyzer) Activator.CreateInstance (AnalyzerType);
			}
		}

		// TODO: Add more analyzers for
		//
		// Virtual methods calls only once with many override
		// Most used/unused attributes
		// Constants passed as arguments
		//
		static readonly LensAnalyzerDetails[] all = new[] {
			new LensAnalyzerDetails ("duplicated-code",
				"Methods which are possible duplicates", typeof (DuplicatedCodeAnalyzer)),
			new LensAnalyzerDetails ("fields-init",
				"Constructors re-initializing fields to default values", typeof (RedundantFieldInitializationAnalyzer)),
			new LensAnalyzerDetails ("fields-unread",
				"Fields that are set but never read", typeof (UnnecessaryFieldsAssignmentAnalyzer)),
			new LensAnalyzerDetails ("ifaces-dispatch",
				"Interfaces which are called sparsely", typeof (InterfaceDispatchAnalyzer)),
			new LensAnalyzerDetails ("ifaces-types",
				"Interfaces with implementation but no type reference", typeof (InterfaceTypeCheckAnalyzers)),
			new LensAnalyzerDetails ("inverted-ctors",
				"Constructors calling same type constructor with default values", typeof (InverterCtorsChainAnalyzer)),
			new LensAnalyzerDetails ("large-arrays",
				"Methods creating large arrays", typeof (LargeStaticArraysAnalyzer)) { DefaultSet = true },
			new LensAnalyzerDetails ("large-cctors",
				"Types with large static contructor", typeof (LargeStaticCtorAnalyzer)),
			new LensAnalyzerDetails ("large-strings",
				"Methods using large strings literals", typeof (LargeStringsAnalyzer)) { DefaultSet = true },
			new LensAnalyzerDetails ("operator-null",
				"User operators used for null check", typeof (UserOperatorCalledForNullCheckAnalyzer)),
			new LensAnalyzerDetails ("single-calls",
				"Methods called sparsely", typeof (LimitedMethodCalls)),
			new LensAnalyzerDetails ("single-construction",
				"Types with limited number of constructions", typeof (TypeInstatiationAnalyzer)) { DefaultSet = true },
			new LensAnalyzerDetails ("unused-param",
				"Methods with unused parameters", typeof (UnusedParametersAnalyzer)),
		};

		public static IEnumerable<LensAnalyzerDetails> All => all;

		public static IEnumerable<Analyzer> AllAnalyzers => all.Select (l => l.CreateAnalyzer ());

		public static IEnumerable<Analyzer> DefaultAnalyzers => all.Where (l => l.DefaultSet).OrderBy (l => l.Name).Select (l => l.CreateAnalyzer ());

		public static Analyzer GetLensByName (string name) => all.FirstOrDefault (l => l.Name == name)?.CreateAnalyzer ();
	}
}
