// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
	public class RecognizedReflectionAccessPatternAttribute : BaseExpectedLinkedBehaviorAttribute
	{
		// The default .ctor has a special meaning - don't validate any specifically recognized reflection access patterns
		// but it will trigger the overall validation that all unrecognized patterns are expected.
		public RecognizedReflectionAccessPatternAttribute ()
		{
		}

		public RecognizedReflectionAccessPatternAttribute (Type reflectionMethodType, string reflectionMethodName, Type[] reflectionMethodParameters,
			Type accessedItemType, string accessedItemName, Type[] accessedItemParameters)
		{
			if (reflectionMethodType == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethodType));
			if (reflectionMethodName == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethodName));
			if (reflectionMethodParameters == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethodParameters));

			if (accessedItemType == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (accessedItemType));
			if (accessedItemName == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (accessedItemName));
		}

		public RecognizedReflectionAccessPatternAttribute (Type reflectionMethodType, string reflectionMethodName, Type[] reflectionMethodParameters,
			Type accessedItemType, string accessedItemName, string[] accessedItemParameters = null)
		{
			if (reflectionMethodType == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethodType));
			if (reflectionMethodName == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethodName));
			if (reflectionMethodParameters == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethodParameters));

			if (accessedItemType == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (accessedItemType));
			if (accessedItemName == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (accessedItemName));
		}
	}
}
