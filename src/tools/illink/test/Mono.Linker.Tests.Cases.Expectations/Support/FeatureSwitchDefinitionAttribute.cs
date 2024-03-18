// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.CodeAnalysis
{
	[AttributeUsage(AttributeTargets.Property, Inherited = false)]
	public sealed class FeatureSwitchDefinitionAttribute : Attribute
	{
		public string SwitchName { get; }

		public FeatureSwitchDefinitionAttribute (string switchName)
		{
			SwitchName = switchName;
		}
	}
}
