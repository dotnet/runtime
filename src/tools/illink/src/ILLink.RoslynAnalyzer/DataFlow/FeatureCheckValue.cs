// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ILLink.Shared.DataFlow;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	// Represents the feature conditions checked in a conditional expression,
	// such as
	//     Debug.Assert (RuntimeFeatures.IsDynamicCodeSupport)
	// or
	//     if (!RuntimeFeatures.IsDynamicCodeSupported)
	public record struct FeatureCheckValue : INegate<FeatureCheckValue>
	{
		public ValueSet<string> EnabledFeatures;
		public ValueSet<string> DisabledFeatures;

		public FeatureCheckValue (string enabledFeature)
		{
			EnabledFeatures = new ValueSet<string> (enabledFeature);
			DisabledFeatures = ValueSet<string>.Empty;
		}

		private FeatureCheckValue (ValueSet<string> enabled, ValueSet<string> disabled)
		{
			EnabledFeatures = enabled;
			DisabledFeatures = disabled;
		}

		public FeatureCheckValue And (FeatureCheckValue other)
		{
			return new FeatureCheckValue (
				ValueSet<string>.Union (EnabledFeatures.DeepCopy (), other.EnabledFeatures.DeepCopy ()),
				ValueSet<string>.Union (DisabledFeatures.DeepCopy (), other.DisabledFeatures.DeepCopy ()));
		}

		public FeatureCheckValue Or (FeatureCheckValue other)
		{
			return new FeatureCheckValue (
				ValueSet<string>.Intersection (EnabledFeatures.DeepCopy (), other.EnabledFeatures.DeepCopy ()),
				ValueSet<string>.Intersection (DisabledFeatures.DeepCopy (), other.DisabledFeatures.DeepCopy ()));
		}

		public FeatureCheckValue Negate ()
		{
			return new FeatureCheckValue (DisabledFeatures.DeepCopy (), EnabledFeatures.DeepCopy ());
		}
	}
}
