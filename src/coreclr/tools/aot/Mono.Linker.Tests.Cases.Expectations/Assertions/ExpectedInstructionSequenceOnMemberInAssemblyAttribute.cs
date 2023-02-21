// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
	public class ExpectedInstructionSequenceOnMemberInAssemblyAttribute : BaseInAssemblyAttribute
	{
		public ExpectedInstructionSequenceOnMemberInAssemblyAttribute (string assemblyFileName, Type type, string memberName, string[] opCodes)
		{
			ArgumentException.ThrowIfNullOrEmpty (assemblyFileName);
			ArgumentNullException.ThrowIfNull (type);
			ArgumentException.ThrowIfNullOrEmpty (memberName);
			ArgumentNullException.ThrowIfNull (opCodes);
		}

		public ExpectedInstructionSequenceOnMemberInAssemblyAttribute (string assemblyFileName, string typeName, string memberName, string[] opCodes)
		{
			ArgumentException.ThrowIfNullOrEmpty (assemblyFileName);
			ArgumentException.ThrowIfNullOrEmpty (typeName);
			ArgumentException.ThrowIfNullOrEmpty (memberName);
			ArgumentNullException.ThrowIfNull (opCodes);
		}
	}
}
