// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using ILLink.Shared;

namespace Mono.Linker.Steps
{
	public class CheckSuppressionsDispatcher : SubStepsDispatcher
	{
		public CheckSuppressionsDispatcher () : base (new List<ISubStep> { new CheckSuppressionsStep () })
		{

		}

		public override void Process (LinkContext context)
		{
			base.Process (context);
			var redundantSuppressions = context.Suppressions.GetUnusedSuppressions ();

			// Suppressions targeting warning caused by anything but the linker should not be reported.
			// Suppressions targeting RedundantSuppression warning should not be reported.
			redundantSuppressions = redundantSuppressions
				.Where (suppression => ((DiagnosticId) suppression.SuppressMessageInfo.Id).GetDiagnosticCategory () == DiagnosticCategory.Trimming)
				.Where (suppression => ((DiagnosticId) suppression.SuppressMessageInfo.Id) != DiagnosticId.RedundantSuppression);

			foreach (var suppression in redundantSuppressions) {
				var source = context.Suppressions.GetSuppressionOrigin (suppression);

				context.LogWarning (new MessageOrigin (source), DiagnosticId.RedundantSuppression, $"IL{suppression.SuppressMessageInfo.Id:0000}");
			}
		}
	}
}
