// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Mono.Linker.Steps
{
	public abstract class SubStepsDispatcher : IStep
	{
		protected SubStepsDispatcher () => throw null;

		protected SubStepsDispatcher (IEnumerable<ISubStep> subSteps) => throw null;

		public static void Add (ISubStep substep) => throw null;

		void IStep.Process (LinkContext context) => throw null;
	}
}