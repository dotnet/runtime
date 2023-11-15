// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using ILLink.Shared.DataFlow;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	// Represents the feature conditions checked in a conditional expression,
	// such as
	//     Debug.Assert (RuntimeFeatures.IsDynamicCodeSupported)
	// or
	//     if (!RuntimeFeatures.IsDynamicCodeSupported)
	// For now, this is only designed to track the built-in "features"/"capabilities"
	// like RuntimeFeatures.IsDynamicCodeSupported, where a true return value
	// indicates that a feature/capability is available.
	public record struct FeatureChecksValue : IEquatable<FeatureChecksValue>, INegate<FeatureChecksValue>, IDeepCopyValue<FeatureChecksValue>
	{
		// "Top" of a FeatureChecksValue should be "all features enabled and all features disabled".
		// This is the identity of the union operator. Therefore we track FeatureContext which can represent this.
		public FeatureContext EnabledFeatures;
		public FeatureContext DisabledFeatures;

		public static FeatureChecksValue None = new FeatureChecksValue (FeatureContext.None, FeatureContext.None);

		public FeatureChecksValue (string enabledFeature)
		{
			EnabledFeatures = new FeatureContext (new ValueSet<string> (enabledFeature));
			DisabledFeatures = new FeatureContext (ValueSet<string>.Empty);
		}

		private FeatureChecksValue (ValueSet<string> enabled, ValueSet<string> disabled)
		{
			EnabledFeatures = new FeatureContext (enabled);
			DisabledFeatures = new FeatureContext (disabled);
		}

		private FeatureChecksValue (FeatureContext enabled, FeatureContext disabled)
		{
			EnabledFeatures = enabled;
			DisabledFeatures = disabled;
		}

		public FeatureChecksValue Union (FeatureChecksValue other)
		{
			return new FeatureChecksValue (
				EnabledFeatures.Union (other.EnabledFeatures),
				DisabledFeatures.Union (other.DisabledFeatures));
		}

		public FeatureChecksValue Intersection (FeatureChecksValue other)
		{
			// Intersection with None should naturally give back None.
			return new FeatureChecksValue (
				EnabledFeatures.Intersection (other.EnabledFeatures),
				DisabledFeatures.Intersection (other.DisabledFeatures));
		}

		public FeatureChecksValue Negate ()
		{
			return new FeatureChecksValue (DisabledFeatures.DeepCopy (), EnabledFeatures.DeepCopy ());
		}

		public FeatureChecksValue DeepCopy ()
		{
			return new FeatureChecksValue (EnabledFeatures.DeepCopy (), DisabledFeatures.DeepCopy ());
		}

	}

	public readonly struct FeatureChecksLattice : ILattice<FeatureChecksValue>
	{
		public FeatureChecksLattice () { }

		// The top value is the identity of meet (intersection), the check where all features are asserted
		// to be simultaneously enabled and disabled.
		public FeatureChecksValue Top => default;

		// We are interested in feature checks which assert that features are known to be enabled or disabled
		// for all paths through the CFG, so the meet operator is set intersection.
		public FeatureChecksValue Meet (FeatureChecksValue left, FeatureChecksValue right) => left.Intersection (right);
	}
}
