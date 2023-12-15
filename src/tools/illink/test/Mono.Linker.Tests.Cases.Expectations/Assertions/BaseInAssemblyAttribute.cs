// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	public abstract class BaseInAssemblyAttribute : BaseExpectedLinkedBehaviorAttribute
	{
		/// <summary>
		/// By default the behavior should be preserved by all platforms
		/// This property can override that by setting only the platforms
		/// which are expected to preserve the desired behavior.
		/// </summary>
		public Tool Tool { get; set; } = Tool.TrimmerAnalyzerAndNativeAot;
	}
}
