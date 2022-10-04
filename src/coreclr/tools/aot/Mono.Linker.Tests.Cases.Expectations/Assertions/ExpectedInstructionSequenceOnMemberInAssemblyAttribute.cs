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
			if (string.IsNullOrEmpty (assemblyFileName))
				throw new ArgumentNullException (nameof (assemblyFileName));
			if (type == null)
				throw new ArgumentNullException (nameof (type));
			if (string.IsNullOrEmpty (memberName))
				throw new ArgumentNullException (nameof (memberName));
			if (opCodes == null)
				throw new ArgumentNullException (nameof (opCodes));
		}

		public ExpectedInstructionSequenceOnMemberInAssemblyAttribute (string assemblyFileName, string typeName, string memberName, string[] opCodes)
		{
			if (string.IsNullOrEmpty (assemblyFileName))
				throw new ArgumentNullException (nameof (assemblyFileName));
			if (string.IsNullOrEmpty (typeName))
				throw new ArgumentNullException (nameof (typeName));
			if (string.IsNullOrEmpty (memberName))
				throw new ArgumentNullException (nameof (memberName));
			if (opCodes == null)
				throw new ArgumentNullException (nameof (opCodes));
		}
	}
}
