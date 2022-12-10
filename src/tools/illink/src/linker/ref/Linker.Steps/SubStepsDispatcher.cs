// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Mono.Linker.Steps
{
	public abstract class SubStepsDispatcher : IStep
	{
		protected SubStepsDispatcher () => throw null;

		protected SubStepsDispatcher (IEnumerable<ISubStep> subSteps) => throw null;

		public void Add (ISubStep substep) => throw null;

		void IStep.Process (LinkContext context) => throw null;
	}
}
