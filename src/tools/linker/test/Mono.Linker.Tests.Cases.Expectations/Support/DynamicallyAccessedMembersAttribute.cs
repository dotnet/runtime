// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices
{
	[Flags]
	public enum DynamicallyAccessedMemberKinds
	{
		DefaultConstructor = 0b00000000_00000001,
		PublicConstructors = 0b00000000_00000011,
		Constructors = 0b00000000_00000111,
		PublicMethods = 0b00000000_00001000,
		Methods = 0b00000000_00011000,
		PublicFields = 0b00000000_00100000,
		Fields = 0b00000000_01100000,
		PublicNestedTypes = 0b00000000_10000000,
		NestedTypes = 0b00000001_10000000,
		PublicProperties = 0b00000010_00000000,
		Properties = 0b00000110_00000000,
		PublicEvents = 0b00001000_00000000,
		Events = 0b00011000_00000000,
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Method, AllowMultiple = false)]
	public class DynamicallyAccessedMembersAttribute : Attribute
	{
		public DynamicallyAccessedMemberKinds MemberKinds { get; }

		public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberKinds memberKinds)
		{
			MemberKinds = memberKinds;
		}
	}
}
