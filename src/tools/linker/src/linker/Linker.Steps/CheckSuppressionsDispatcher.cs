// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using ILLink.Shared;
using Mono.Cecil;

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
				.Where (suppression => ((DiagnosticId) suppression.suppressMessageInfo.Id).GetDiagnosticCategory () == DiagnosticCategory.Trimming)
				.Where (suppression => ((DiagnosticId) suppression.suppressMessageInfo.Id) != DiagnosticId.RedundantSuppression);

			foreach (var (provider, suppressMessageInfo) in redundantSuppressions) {
				var source = GetSuppresionProvider (provider);

				context.LogWarning (new MessageOrigin (source), DiagnosticId.RedundantSuppression, $"IL{suppressMessageInfo.Id:0000}");
			}
		}

		private static ICustomAttributeProvider GetSuppresionProvider (ICustomAttributeProvider provider) => provider is ModuleDefinition module ? module.Assembly : provider;
	}
}
