// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ILLink.Shared;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	public struct FeatureContext : IEquatable<FeatureContext>, IDeepCopyValue<FeatureContext>
	{
		// The set of features known to be enabled in this context.
		// Unknown represents "all possible features".
		public ValueSet<string> EnabledFeatures;

		public static readonly FeatureContext All = new FeatureContext (ValueSet<string>.Unknown);

		public static readonly FeatureContext None = new FeatureContext (ValueSet<string>.Empty);

		public FeatureContext (ValueSet<string> enabled)
		{
			EnabledFeatures = enabled;
		}

		public bool IsEnabled (string feature)
		{
			return EnabledFeatures.IsUnknown () || EnabledFeatures.Contains (feature);
		}

		public bool Equals (FeatureContext other) => EnabledFeatures == other.EnabledFeatures;
		public override bool Equals (object? obj) => obj is FeatureContext other && Equals (other);
		public override int GetHashCode () => EnabledFeatures.GetHashCode ();

		public static bool operator == (FeatureContext left, FeatureContext right) => left.Equals (right);
		public static bool operator != (FeatureContext left, FeatureContext right) => !left.Equals (right);

		public FeatureContext DeepCopy ()
		{
			return new FeatureContext (EnabledFeatures.DeepCopy ());
		}

		public FeatureContext Intersection (FeatureContext other)
		{
			return new FeatureContext (ValueSet<string>.Intersection (EnabledFeatures, other.EnabledFeatures));
		}

		public FeatureContext Union (FeatureContext other)
		{
			return new FeatureContext (ValueSet<string>.Union (EnabledFeatures, other.EnabledFeatures));
		}
	}

	public readonly struct FeatureContextLattice : ILattice<FeatureContext>
	{
		public FeatureContextLattice () { }

		// The top value is the identity of meet (intersection), the set of all features.
		public FeatureContext Top { get; } = FeatureContext.All;

		// We are interested in features which are known to be enabled for all paths through the CFG,
		// so the meet operator is set intersection.
		public FeatureContext Meet (FeatureContext left, FeatureContext right) => left.Intersection (right);
	}
}
