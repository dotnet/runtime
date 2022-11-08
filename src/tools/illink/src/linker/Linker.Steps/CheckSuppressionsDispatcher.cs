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
				.Where (suppression => ((DiagnosticId) suppression.SuppressMessageInfo.Id).GetDiagnosticCategory () == DiagnosticCategory.Trimming)
				.Where (suppression => ((DiagnosticId) suppression.SuppressMessageInfo.Id) != DiagnosticId.RedundantSuppression)
				.Where (suppression => ProviderIsMarked (suppression.Provider));

			foreach (var suppression in redundantSuppressions) {
				var source = context.Suppressions.GetSuppressionOrigin (suppression);

				context.LogWarning (new MessageOrigin (source), DiagnosticId.RedundantSuppression, $"IL{suppression.SuppressMessageInfo.Id:0000}");
			}

			bool ProviderIsMarked (ICustomAttributeProvider provider)
			{
				if (provider is PropertyDefinition property) {
					return (property.GetMethod != null && context.Annotations.IsMarked (property.GetMethod))
						|| (property.SetMethod != null && context.Annotations.IsMarked (property.SetMethod));
				}

				if (provider is EventDefinition @event) {
					return (@event.AddMethod != null && context.Annotations.IsMarked (@event.AddMethod))
						|| (@event.InvokeMethod != null && context.Annotations.IsMarked (@event.InvokeMethod))
						|| (@event.RemoveMethod != null && context.Annotations.IsMarked (@event.RemoveMethod));
				}

				return context.Annotations.IsMarked (provider);
			}
		}
	}
}
