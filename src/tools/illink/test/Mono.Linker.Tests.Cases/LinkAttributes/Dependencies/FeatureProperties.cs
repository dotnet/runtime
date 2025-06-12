// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Mono.Linker.Tests.Cases.LinkAttributes.Dependencies
{
	public class FeatureProperties
	{
		[FeatureSwitchDefinition ("FeatureSwitch")]
		public static bool FeatureSwitchDefinition => Removed ();

		[FeatureGuard (typeof (RequiresUnreferencedCodeAttribute))]
		public static bool FeatureGuard => Removed ();

		[FeatureSwitchDefinition ("StubbedFeatureSwitch")]
		public static bool StubbedFeatureSwitch => Removed ();

		static bool Removed () => true;
	}
}
