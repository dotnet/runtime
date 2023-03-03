using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[module: UnconditionalSuppressMessage ("Test", "IL2072", Scope = "type", Target = "T:Mono.Linker.Tests.Cases.Warnings.WarningSuppression.WarningsInType")]
[module: UnconditionalSuppressMessage ("Test", "IL2072", Scope = "member", Target = "M:Mono.Linker.Tests.Cases.Warnings.WarningSuppression.WarningsInMembers.Method")]
[module: UnconditionalSuppressMessage ("Test", "IL2072", Scope = "member", Target = "M:Mono.Linker.Tests.Cases.Warnings.WarningSuppression.WarningsInMembers.get_Property")]
[module: UnconditionalSuppressMessage ("Test", "IL2072", Scope = "member", Target = "M:Mono.Linker.Tests.Cases.Warnings.WarningSuppression.SuppressWarningsInMembersAndTypesUsingTarget.NestedType.Warning")]
[module: UnconditionalSuppressMessage ("Test", "IL2072", Scope = "member", Target = "M:Mono.Linker.Tests.Cases.Warnings.WarningSuppression.WarningsInMembers.MultipleWarnings")]
[module: UnconditionalSuppressMessage ("Test", "IL2026", Scope = "member", Target = "M:Mono.Linker.Tests.Cases.Warnings.WarningSuppression.WarningsInMembers.MultipleSuppressions")]

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
#if !NETCOREAPP
	[Reference ("System.Core.dll")]
#endif
	[SkipKeptItemsValidation]
	[LogDoesNotContain ("TriggerUnrecognizedPattern()")]
	public class SuppressWarningsInMembersAndTypesUsingTarget
	{
		/// <summary>
		/// This test case checks module level UnconditionalSuppressMessage, primarily using 
		/// System.Linq.Expressions.Expression.(Type type, string methodName, Type[]? typeArguments, params System.Linq.Expressions.Expression[]? arguments)
		/// which has a RUC attribute but is treated as an intrinsic by the trimmer. The test case also has some member level suppressions and its
		/// own RUC method (the IL2026 suppression in the code is due to this)
		/// </summary>
		public static void Main ()
		{
			NestedType.Warning ();
			var warningsInType = new WarningsInType ();
			warningsInType.Warning1 ();
			var warningInNestedType = new WarningsInType.NestedType ();
			warningInNestedType.Warning3 ();

			var warningsInMembers = new WarningsInMembers ();
			warningsInMembers.Method ();
			int propertyThatTriggersWarning = warningsInMembers.Property;

			WarningsInMembers.MultipleWarnings ();
			WarningsInMembers.MultipleSuppressions ();
		}

		public static Type TriggerUnrecognizedPattern ()
		{
			return typeof (SuppressWarningsInMembersAndTypesUsingTarget);
		}

		public class NestedType
		{
			public static void Warning ()
			{
				Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
			}
		}
	}

	public class WarningsInType
	{
		public void Warning1 ()
		{
			Expression.Call (SuppressWarningsInMembersAndTypesUsingTarget.TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
		}

		public void Warning2 ()
		{
			Expression.Call (SuppressWarningsInMembersAndTypesUsingTarget.TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
		}

		public class NestedType
		{
			public void Warning3 ()
			{
				void Warning4 ()
				{
					Expression.Call (SuppressWarningsInMembersAndTypesUsingTarget.TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
				}

				SuppressWarningsInMembersAndTypesUsingTarget.TriggerUnrecognizedPattern ();
				Warning4 ();
			}
		}
	}

	[ExpectedNoWarnings]
	public class WarningsInMembers
	{
		public void Method ()
		{
			Expression.Call (SuppressWarningsInMembersAndTypesUsingTarget.TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
		}

		public int Property {
			get {
				Expression.Call (SuppressWarningsInMembersAndTypesUsingTarget.TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
				return 0;
			}
		}

		[UnconditionalSuppressMessage ("Test", "IL2026")]
		public static void MultipleWarnings ()
		{
			Expression.Call (SuppressWarningsInMembersAndTypesUsingTarget.TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
			RUCMethod ();
		}

		[LogContains ("Element 'Mono.Linker.Tests.Cases.Warnings.WarningSuppression.WarningsInMembers." + nameof (MultipleSuppressions) + "()'" +
			" has more than one unconditional suppression.")]
		[UnconditionalSuppressMessage ("Test", "IL2026")]
		public static void MultipleSuppressions ()
		{
			RUCMethod ();
		}

		[RequiresUnreferencedCode ("--RUCMethod--")]
		static void RUCMethod ()
		{
		}
	}
}
