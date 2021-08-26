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
		public UnrecognizedReflectionAccessPatternAttribute (
			Type reflectionMethodType,
			string reflectionMethodName,
			Type[] reflectionMethodParameters,
			string[] message = null,
			string messageCode = null,
			Type returnType = null,
			string genericParameter = null)
		{
		}

		public UnrecognizedReflectionAccessPatternAttribute (
			Type reflectionMethodType,
			string reflectionMethodName,
			string[] reflectionMethodParameters = null,
			string[] message = null,
			string messageCode = null,
			Type returnType = null,
			string genericParameter = null)
		{
		}
	}
}
