// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.TestCasesRunner
{
	/// <summary>
	/// Stores various customizations which can be added to the linker at runtime,
	/// for example test implementations of certain interfaces.
	/// </summary>
	public class LinkerCustomizations
	{
		public TestDependencyRecorder DependencyRecorder { get; set; }

		public event Action<LinkContext> CustomizeContext;

		public void CustomizeLinkContext (LinkContext context)
		{
			CustomizeContext?.Invoke (context);
		}
	}
}
