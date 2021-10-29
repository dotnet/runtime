// Licensed to the.NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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