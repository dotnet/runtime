// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
	public record struct FeatureChecksValue : INegate<FeatureChecksValue>, IDeepCopyValue<FeatureChecksValue>
	{
		public ValueSet<string> EnabledFeatures;
		public ValueSet<string> DisabledFeatures;

		public static readonly FeatureChecksValue All = new FeatureChecksValue (ValueSet<string>.Unknown, ValueSet<string>.Empty);

		public static FeatureChecksValue None = new FeatureChecksValue (ValueSet<string>.Empty, ValueSet<string>.Empty);

		public FeatureChecksValue (string enabledFeature)
		{
			EnabledFeatures = new ValueSet<string> (enabledFeature);
			DisabledFeatures = ValueSet<string>.Empty;
		}

		private FeatureChecksValue (ValueSet<string> enabled, ValueSet<string> disabled)
		{
			EnabledFeatures = enabled;
			DisabledFeatures = disabled;
		}

		public FeatureChecksValue And (FeatureChecksValue other)
		{
			return new FeatureChecksValue (
				ValueSet<string>.Union (EnabledFeatures.DeepCopy (), other.EnabledFeatures.DeepCopy ()),
				ValueSet<string>.Union (DisabledFeatures.DeepCopy (), other.DisabledFeatures.DeepCopy ()));
		}

		public FeatureChecksValue Or (FeatureChecksValue other)
		{
			return new FeatureChecksValue (
				ValueSet<string>.Intersection (EnabledFeatures.DeepCopy (), other.EnabledFeatures.DeepCopy ()),
				ValueSet<string>.Intersection (DisabledFeatures.DeepCopy (), other.DisabledFeatures.DeepCopy ()));
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
}
