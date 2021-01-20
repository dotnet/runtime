// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public sealed class RequiresAssemblyFilesAnalyzer : DiagnosticAnalyzer
	{
		public const string IL3002 = nameof (IL3002);
		internal const string RequiresAssemblyFilesAttribute = nameof (RequiresAssemblyFilesAttribute);
		internal const string FullyQualifiedRequiresAssemblyFilesAttribute = "System.Diagnostics.CodeAnalysis." + RequiresAssemblyFilesAttribute;

		static readonly DiagnosticDescriptor s_requiresAssemblyFilesRule = new DiagnosticDescriptor (
			IL3002,
			new LocalizableResourceString (nameof (Resources.RequiresAssemblyFilesTitle),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.RequiresAssemblyFilesMessage),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.SingleFile,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (s_requiresAssemblyFilesRule);

		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);

			context.RegisterCompilationStartAction (context => {
				var compilation = context.Compilation;

				var isSingleFilePublish = context.Options.GetMSBuildPropertyValue (MSBuildPropertyOptionNames.PublishSingleFile, compilation);
				if (!string.Equals (isSingleFilePublish?.Trim (), "true", StringComparison.OrdinalIgnoreCase))
					return;

				var includesAllContent = context.Options.GetMSBuildPropertyValue (MSBuildPropertyOptionNames.IncludeAllContentForSelfExtract, compilation);
				if (string.Equals (includesAllContent?.Trim (), "true", StringComparison.OrdinalIgnoreCase))
					return;

				context.RegisterOperationAction (operationContext => {
					var methodInvocation = (IInvocationOperation) operationContext.Operation;
					CheckCalledMember (operationContext, methodInvocation.TargetMethod);
				}, OperationKind.Invocation);

				context.RegisterOperationAction (operationContext => {
					var objectCreation = (IObjectCreationOperation) operationContext.Operation;
					CheckCalledMember (operationContext, objectCreation.Constructor);
				}, OperationKind.ObjectCreation);

				context.RegisterOperationAction (operationContext => {
					var propAccess = (IPropertyReferenceOperation) operationContext.Operation;
					var prop = propAccess.Property;
					var usageInfo = propAccess.GetValueUsageInfo (prop);
					if (usageInfo.HasFlag (ValueUsageInfo.Read) && prop.GetMethod != null)
						CheckCalledMember (operationContext, prop.GetMethod);

					if (usageInfo.HasFlag (ValueUsageInfo.Write) && prop.SetMethod != null)
						CheckCalledMember (operationContext, prop.SetMethod);

					CheckCalledMember (operationContext, prop);
				}, OperationKind.PropertyReference);

				context.RegisterOperationAction (operationContext => {
					var eventRef = (IEventReferenceOperation) operationContext.Operation;
					CheckCalledMember (operationContext, eventRef.Member);
				}, OperationKind.EventReference);

				static void CheckCalledMember (
					OperationAnalysisContext operationContext,
					ISymbol member)
				{
					// Do not emit any diagnostic if caller is annotated with the attribute too.
					if (operationContext.ContainingSymbol.HasAttribute (RequiresAssemblyFilesAttribute))
						return;

					if (member.TryGetRequiresAssemblyFileAttribute (out AttributeData? requiresAssemblyFilesAttribute)) {
						operationContext.ReportDiagnostic (Diagnostic.Create (
							s_requiresAssemblyFilesRule,
							operationContext.Operation.Syntax.GetLocation (),
							member.OriginalDefinition.ToString (),
							requiresAssemblyFilesAttribute?.NamedArguments.FirstOrDefault (na => na.Key == "Message").Value.Value?.ToString (),
							requiresAssemblyFilesAttribute?.NamedArguments.FirstOrDefault (na => na.Key == "Url").Value.Value?.ToString ()));
					}
				}
			});
		}
	}
}
