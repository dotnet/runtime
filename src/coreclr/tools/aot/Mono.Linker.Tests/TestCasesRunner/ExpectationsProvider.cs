// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public static class ExpectationsProvider
	{

		public static bool IsAssemblyAssertion (CustomAttribute attr)
		{
			return attr.AttributeType.Name == nameof (KeptAssemblyAttribute) ||
				attr.AttributeType.Name == nameof (RemovedAssemblyAttribute) ||
				attr.AttributeType.Name == nameof (SetupLinkerActionAttribute) ||
				attr.AttributeType.Name == nameof (SetupLinkerTrimModeAttribute);
		}

		public static bool IsSymbolAssertion (CustomAttribute attr)
		{
			return attr.AttributeType.Name == nameof (KeptSymbolsAttribute) || attr.AttributeType.Name == nameof (RemovedSymbolsAttribute);
		}
	}
}
