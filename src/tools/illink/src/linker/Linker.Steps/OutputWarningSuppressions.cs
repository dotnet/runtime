// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Mono.Linker.Steps
{
	public class OutputWarningSuppressions : BaseStep
	{
		protected override bool ConditionToProcess ()
		{
			return Context.WarningSuppressionWriter?.IsEmpty == false;
		}

		protected override void Process ()
		{
			CheckOutputDirectory ();
			Context.WarningSuppressionWriter?.OutputSuppressions (Context.OutputDirectory);
		}

		void CheckOutputDirectory ()
		{
			if (Directory.Exists (Context.OutputDirectory))
				return;

			Directory.CreateDirectory (Context.OutputDirectory);
		}
	}
}