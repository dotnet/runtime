// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Mono.Linker.Steps
{

	/// <summary>
	/// This API supports the product infrastructure and is not intended to be used directly from your code.
	/// Extensibility point for custom logic that run during MarkStep, for marked members.
	/// </summary>
	public interface IMarkHandler
	{
		/// <summary>
		/// Initialize is called at the beginning of MarkStep. This should be
		/// used to perform global setup, and register callbacks through the
		/// MarkContext.Register* methods) to be called when pieces of IL are marked.
		/// </summary>
		void Initialize (LinkContext context, MarkContext markContext);
	}
}
