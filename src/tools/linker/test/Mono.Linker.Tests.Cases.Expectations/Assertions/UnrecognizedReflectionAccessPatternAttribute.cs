// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (
		AttributeTargets.Method |
		AttributeTargets.Constructor |
		AttributeTargets.Class |
		AttributeTargets.Field |
		AttributeTargets.Property,
		AllowMultiple = true,
		Inherited = false)]
	public class UnrecognizedReflectionAccessPatternAttribute : BaseExpectedLinkedBehaviorAttribute
	{
		public UnrecognizedReflectionAccessPatternAttribute (Type reflectionMethodType, string reflectionMethodName, Type[] reflectionMethodParameters,
			string message = null,
			Type returnType = null)
		{
			if (reflectionMethodType == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethodType));
			if (reflectionMethodName == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethodName));
			if (reflectionMethodParameters == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethodParameters));

			if (message == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (message));
		}

		public UnrecognizedReflectionAccessPatternAttribute (Type reflectionMethodType, string reflectionMethodName,
			string[] reflectionMethodParameters = null,
			string message = null,
			Type returnType = null)
		{
			if (reflectionMethodType == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethodType));
			if (reflectionMethodName == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethodName));
			if (reflectionMethodParameters == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethodParameters));

			if (message == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (message));
		}
	}
}
