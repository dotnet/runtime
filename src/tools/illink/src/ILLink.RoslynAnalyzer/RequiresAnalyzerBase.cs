// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.RoslynAnalyzer.TrimAnalysis;
using ILLink.Shared;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.RoslynAnalyzer
{
    public abstract class RequiresAnalyzerBase : DiagnosticAnalyzer
    {
        private protected abstract string RequiresAttributeName { get; }

        internal abstract string RequiresAttributeFullyQualifiedName { get; }

        private protected abstract DiagnosticTargets AnalyzerDiagnosticTargets { get; }

        private protected abstract DiagnosticDescriptor RequiresDiagnosticRule { get; }

        private protected abstract DiagnosticId RequiresDiagnosticId { get; }

        private protected abstract DiagnosticDescriptor RequiresAttributeMismatch { get; }
        private protected abstract DiagnosticDescriptor RequiresOnStaticCtor { get; }
        private protected abstract DiagnosticDescriptor RequiresOnEntryPoint { get; }

        internal virtual void ProcessGenericInstantiation(
            ITypeSymbol typeArgument,
            ITypeParameterSymbol typeParameter,
            FeatureContext featureContext,
            TypeNameResolver typeNameResolver,
            ISymbol owningSymbol,
            Location location,
            Action<Diagnostic>? reportDiagnostic)
        {
            // Check constructor constraint (new())
            if (typeParameter.HasConstructorConstraint)
            {
                // Check if this type has a public parameterless constructor
                var namedTypeSymbol = typeArgument as INamedTypeSymbol;
                var publicParameterlessConstructor = namedTypeSymbol?.InstanceConstructors.FirstOrDefault(c => c.Parameters.IsEmpty && c.DeclaredAccessibility == Accessibility.Public);

                if (publicParameterlessConstructor != null)
                {
                    var diagnosticContext = new DiagnosticContext(location, reportDiagnostic);
                    CheckAndCreateRequiresDiagnostic(
                        publicParameterlessConstructor,
                        owningSymbol,
                        ImmutableArray<ISymbol>.Empty,
                        diagnosticContext);
                }
            }
        }

        private protected virtual ImmutableArray<(Action<SyntaxNodeAnalysisContext> Action, SyntaxKind[] SyntaxKind)> ExtraSyntaxNodeActions { get; } = ImmutableArray<(Action<SyntaxNodeAnalysisContext> Action, SyntaxKind[] SyntaxKind)>.Empty;
        private protected virtual ImmutableArray<(Action<SymbolAnalysisContext> Action, SymbolKind[] SymbolKind)> ExtraSymbolActions { get; } = ImmutableArray<(Action<SymbolAnalysisContext> Action, SymbolKind[] SymbolKind)>.Empty;
        private protected virtual ImmutableArray<Action<CompilationAnalysisContext>> ExtraCompilationActions { get; } = ImmutableArray<Action<CompilationAnalysisContext>>.Empty;

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            if (!System.Diagnostics.Debugger.IsAttached)
                context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(context =>
            {
                var compilation = context.Compilation;
                if (!IsAnalyzerEnabled(context.Options))
                    return;

                var incompatibleMembers = GetSpecialIncompatibleMembers(compilation);
                context.RegisterSymbolAction(symbolAnalysisContext =>
                {
                    var methodSymbol = (IMethodSymbol)symbolAnalysisContext.Symbol;

                    if (methodSymbol.HasAttribute(RequiresAttributeName))
                    {
                        if (methodSymbol.IsStaticConstructor())
                            ReportRequiresOnStaticCtorDiagnostic(symbolAnalysisContext, methodSymbol);

                        if (methodSymbol.IsEntryPoint(symbolAnalysisContext.Compilation) || methodSymbol.IsUnmanagedCallersOnlyEntryPoint())
                            ReportRequiresOnEntryPointDiagnostic(symbolAnalysisContext, methodSymbol);
                    }

                    CheckMatchingAttributesInOverrides(symbolAnalysisContext, methodSymbol);
                }, SymbolKind.Method);

                context.RegisterSymbolAction(symbolAnalysisContext =>
                {
                    var typeSymbol = (INamedTypeSymbol)symbolAnalysisContext.Symbol;
                    CheckMatchingAttributesInInterfaces(symbolAnalysisContext, typeSymbol);
                }, SymbolKind.NamedType);

                foreach (var extraSyntaxNodeAction in ExtraSyntaxNodeActions)
                    context.RegisterSyntaxNodeAction(extraSyntaxNodeAction.Action, extraSyntaxNodeAction.SyntaxKind);

                // Register the implicit base constructor analysis for all analyzers
                context.RegisterSymbolAction(AnalyzeImplicitBaseCtor, SymbolKind.NamedType);

                foreach (var extraSymbolAction in ExtraSymbolActions)
                    context.RegisterSymbolAction(extraSymbolAction.Action, extraSymbolAction.SymbolKind);

                void CheckMatchingAttributesInOverrides(
                    SymbolAnalysisContext symbolAnalysisContext,
                    ISymbol member)
                {
                    if ((member.IsVirtual || member.IsOverride) && member.TryGetOverriddenMember(out var overriddenMember) && HasMismatchingAttributes(member, overriddenMember))
                        ReportMismatchInAttributesDiagnostic(symbolAnalysisContext, member, overriddenMember);
                }

                void CheckMatchingAttributesInInterfaces(
                    SymbolAnalysisContext symbolAnalysisContext,
                    INamedTypeSymbol type)
                {
                    foreach (var memberpair in type.GetMemberInterfaceImplementationPairs())
                    {
                        var implementationType = memberpair.ImplementationMember switch
                        {
                            IMethodSymbol method => method.ContainingType,
                            IPropertySymbol property => property.ContainingType,
                            IEventSymbol @event => @event.ContainingType,
                            _ => throw new NotSupportedException()
                        };
                        ISymbol origin = memberpair.ImplementationMember;

                        // If this type implements an interface method through a base class, the origin of the warning is this type,
                        // not the member on the base class.
                        if (!implementationType.IsInterface() && !SymbolEqualityComparer.Default.Equals(implementationType, type))
                            origin = type;

                        if (HasMismatchingAttributes(memberpair.InterfaceMember, memberpair.ImplementationMember))
                        {
                            ReportMismatchInAttributesDiagnostic(symbolAnalysisContext, memberpair.ImplementationMember, memberpair.InterfaceMember, isInterface: true, origin);
                        }
                    }
                }
            });

            foreach (var extraCompilationAction in ExtraCompilationActions)
                context.RegisterCompilationAction(extraCompilationAction);
        }

        internal void CheckAndCreateRequiresDiagnostic(
            ISymbol member,
            ISymbol containingSymbol,
            ImmutableArray<ISymbol> incompatibleMembers,
            in DiagnosticContext diagnosticContext)
        {
            // Do not emit any diagnostic if caller is annotated with the attribute too.
            if (containingSymbol.IsInRequiresScope(RequiresAttributeName, out _))
                return;

            if (CreateSpecialIncompatibleMembersDiagnostic(incompatibleMembers, member, diagnosticContext))
                return;

            // Warn on the most derived base method taking into account covariant returns
            while (member is IMethodSymbol method && method.OverriddenMethod != null && SymbolEqualityComparer.Default.Equals(method.ReturnType, method.OverriddenMethod.ReturnType))
                member = method.OverriddenMethod;

            if (!member.DoesMemberRequire(RequiresAttributeName, out var requiresAttribute))
                return;

            if (!VerifyAttributeArguments(requiresAttribute))
                return;

            CreateRequiresDiagnostic(member, requiresAttribute, diagnosticContext);
        }

        private void AnalyzeImplicitBaseCtor(SymbolAnalysisContext context)
        {
            var typeSymbol = (INamedTypeSymbol)context.Symbol;

            if (typeSymbol.TypeKind != TypeKind.Class || typeSymbol.BaseType == null)
                return;

            if (typeSymbol.InstanceConstructors.Length != 1 || !typeSymbol.InstanceConstructors[0].IsImplicitlyDeclared)
                return;

            var implicitCtor = typeSymbol.InstanceConstructors[0];

            var baseCtor = typeSymbol.BaseType.InstanceConstructors.FirstOrDefault(ctor => ctor.Parameters.IsEmpty);
            if (baseCtor == null)
                return;

            var diagnosticContext = new DiagnosticContext(
                typeSymbol.Locations[0],
                context.ReportDiagnostic);

            CheckAndCreateRequiresDiagnostic(
                baseCtor,
                implicitCtor,
                ImmutableArray<ISymbol>.Empty,
                diagnosticContext);

            var dataFlowAnalyzerContext = DataFlowAnalyzerContext.Create(context.Options, context.Compilation, ImmutableArray.Create(this));
            var typeNameResolver = new TypeNameResolver(context.Compilation);
            var genericArgumentDataFlow = new GenericArgumentDataFlow(this, FeatureContext.None, typeNameResolver, implicitCtor, typeSymbol.Locations[0], context.ReportDiagnostic);
            if (typeSymbol.BaseType is INamedTypeSymbol baseType)
                genericArgumentDataFlow.ProcessGenericArgumentDataFlow(baseType);

            foreach (var interfaceType in typeSymbol.Interfaces)
                genericArgumentDataFlow.ProcessGenericArgumentDataFlow(interfaceType);
        }

        [Flags]
        protected enum DiagnosticTargets
        {
            MethodOrConstructor = 0x0001,
            Property = 0x0002,
            Field = 0x0004,
            Event = 0x0008,
            Class = 0x0010,
            All = MethodOrConstructor | Property | Field | Event | Class
        }

        /// <summary>
        /// Finds the symbol of the caller to the current operation, helps to find out the symbol in cases where the operation passes
        /// through a lambda or a local function.
        /// </summary>
        /// <param name="operationContext">Analyzer operation context to retrieve the current operation.</param>
        /// <param name="targets">Scope of the attribute to search for callers.</param>
        /// <returns>The symbol of the caller to the operation</returns>
        protected static ISymbol FindContainingSymbol(OperationAnalysisContext operationContext, DiagnosticTargets targets)
        {
            var parent = operationContext.Operation.Parent;
            while (parent is not null)
            {
                switch (parent)
                {
                    case IAnonymousFunctionOperation lambda:
                        return lambda.Symbol;

                    case ILocalFunctionOperation local when targets.HasFlag(DiagnosticTargets.MethodOrConstructor):
                        return local.Symbol;

                    case IMethodBodyBaseOperation when targets.HasFlag(DiagnosticTargets.MethodOrConstructor):
                    case IPropertyReferenceOperation when targets.HasFlag(DiagnosticTargets.Property):
                    case IFieldReferenceOperation when targets.HasFlag(DiagnosticTargets.Field):
                    case IEventReferenceOperation when targets.HasFlag(DiagnosticTargets.Event):
                        return operationContext.ContainingSymbol;

                    default:
                        parent = parent.Parent;
                        break;
                }
            }

            return operationContext.ContainingSymbol;
        }

        /// <summary>
        /// Creates a Requires diagnostic message based on the attribute data and RequiresDiagnosticRule.
        /// </summary>
        /// <param name="operationContext">Analyzer operation context to be able to report the diagnostic.</param>
        /// <param name="member">Information about the member that generated the diagnostic.</param>
        /// <param name="requiresAttribute">Requires attribute data to print attribute arguments.</param>
        private void CreateRequiresDiagnostic(ISymbol member, AttributeData requiresAttribute, in DiagnosticContext diagnosticContext)
        {
            var message = GetMessageFromAttribute(requiresAttribute);
            var url = GetUrlFromAttribute(requiresAttribute);
            diagnosticContext.AddDiagnostic(RequiresDiagnosticId, member.GetDisplayName(), message, url);
        }

        private void ReportRequiresOnStaticCtorDiagnostic(SymbolAnalysisContext symbolAnalysisContext, IMethodSymbol ctor)
        {
            symbolAnalysisContext.ReportDiagnostic(Diagnostic.Create(
                RequiresOnStaticCtor,
                ctor.Locations[0],
                ctor.GetDisplayName()));
        }

        private void ReportRequiresOnEntryPointDiagnostic(SymbolAnalysisContext symbolAnalysisContext, IMethodSymbol entryPoint)
        {
            symbolAnalysisContext.ReportDiagnostic(Diagnostic.Create(
                RequiresOnEntryPoint,
                entryPoint.Locations[0],
                entryPoint.GetDisplayName()));
        }

        private void ReportMismatchInAttributesDiagnostic(SymbolAnalysisContext symbolAnalysisContext, ISymbol member, ISymbol baseMember, bool isInterface = false, ISymbol? origin = null)
        {
            origin ??= member;
            string message = MessageFormat.FormatRequiresAttributeMismatch(member.HasAttribute(RequiresAttributeName), isInterface, RequiresAttributeName, member.GetDisplayName(), baseMember.GetDisplayName());
            symbolAnalysisContext.ReportDiagnostic(Diagnostic.Create(
                RequiresAttributeMismatch,
                origin.Locations[0],
                message));
        }

        private bool HasMismatchingAttributes(ISymbol member1, ISymbol member2)
        {
            bool member1CreatesRequirement = member1.DoesMemberRequire(RequiresAttributeName, out _);
            bool member2CreatesRequirement = member2.DoesMemberRequire(RequiresAttributeName, out _);
            bool member1FulfillsRequirement = member1.IsInRequiresScope(RequiresAttributeName);
            bool member2FulfillsRequirement = member2.IsInRequiresScope(RequiresAttributeName);
            return (member1CreatesRequirement && !member2FulfillsRequirement) || (member2CreatesRequirement && !member1FulfillsRequirement);
        }

        protected abstract string GetMessageFromAttribute(AttributeData requiresAttribute);

        public static string GetUrlFromAttribute(AttributeData? requiresAttribute)
        {
            var url = requiresAttribute?.NamedArguments.FirstOrDefault(na => na.Key == "Url").Value.Value?.ToString();
            return MessageFormat.FormatRequiresAttributeUrlArg(url);
        }

        /// <summary>
        /// This method verifies that the arguments in an attribute have certain structure.
        /// </summary>
        /// <param name="attribute">Attribute data to compare.</param>
        /// <returns>True if the validation was successfull; otherwise, returns false.</returns>
        protected abstract bool VerifyAttributeArguments(AttributeData attribute);

        /// <summary>
        /// Compares the member against a list of incompatible members, if the member exist in the list then it generates a custom diagnostic declared inside the function.
        /// </summary>
        /// <param name="operationContext">Analyzer operation context.</param>
        /// <param name="specialIncompatibleMembers">List of incompatible members.</param>
        /// <param name="member">Member to compare.</param>
        /// <returns>True if the function generated a diagnostic; otherwise, returns false</returns>
        protected virtual bool CreateSpecialIncompatibleMembersDiagnostic(
            ImmutableArray<ISymbol> specialIncompatibleMembers,
            ISymbol member,
            in DiagnosticContext diagnosticContext)
        {
            return false;
        }

        /// <summary>
        /// Creates a list of special incompatible members that can be used later on by the analyzer to generate diagnostics
        /// </summary>
        /// <param name="compilation">Compilation to search for members</param>
        /// <returns>A list of special incomptaible members</returns>
        internal virtual ImmutableArray<ISymbol> GetSpecialIncompatibleMembers(Compilation compilation) => default;

        /// <summary>
        /// Verifies that the MSBuild requirements to run the analyzer are fulfilled
        /// </summary>
        /// <param name="options">Analyzer options</param>
        /// <returns>True if the requirements to run the analyzer are met; otherwise, returns false</returns>
        internal abstract bool IsAnalyzerEnabled(AnalyzerOptions options);

        // Check whether a given property serves as a check for the "feature" or "capability" associated with the attribute
        // understood by this analyzer. For now, this is only designed to support checks like
        // RuntimeFeatures.IsDynamicCodeSupported, where a true return value indicates that the feature is supported.
        // This doesn't support more general cases such as:
        // - false return value indicating that a feature is supported
        // - feature settings supplied by the project
        // - custom feature checks defined in library code
        private protected virtual bool IsRequiresCheck(IPropertySymbol propertySymbol, Compilation compilation) => false;

        internal static bool IsAnnotatedFeatureGuard(IPropertySymbol propertySymbol, string featureName)
        {
            // Only respect FeatureGuardAttribute on static boolean properties.
            if (!propertySymbol.IsStatic || propertySymbol.Type.SpecialType != SpecialType.System_Boolean || propertySymbol.SetMethod != null)
                return false;

            ValueSet<string> featureCheckAnnotations = propertySymbol.GetFeatureGuardAnnotations();
            return featureCheckAnnotations.Contains(featureName);
        }

        internal bool IsFeatureGuard(IPropertySymbol propertySymbol, Compilation compilation)
        {
            return IsAnnotatedFeatureGuard(propertySymbol, RequiresAttributeFullyQualifiedName)
                || IsRequiresCheck(propertySymbol, compilation);
        }

        internal void CheckAndCreateRequiresDiagnostic(
            IOperation operation,
            ISymbol member,
            ISymbol owningSymbol,
            DataFlowAnalyzerContext context,
            FeatureContext featureContext,
            in DiagnosticContext diagnosticContext)
        {
            // Warnings are not emitted if the featureContext says the feature is available.
            if (featureContext.IsEnabled(RequiresAttributeFullyQualifiedName))
                return;

            ISymbol containingSymbol = operation.FindContainingSymbol(owningSymbol);

            var incompatibleMembers = context.GetSpecialIncompatibleMembers(this);
            CheckAndCreateRequiresDiagnostic(
                member,
                containingSymbol,
                incompatibleMembers,
                diagnosticContext);
        }

        internal virtual bool IsIntrinsicallyHandled(
            IMethodSymbol calledMethod,
            MultiValue instance,
            ImmutableArray<MultiValue> arguments
            )
        {
            return false;
        }

        protected void CheckReferencedAssemblies(
            CompilationAnalysisContext context,
            string msbuildPropertyName,
            string assemblyMetadataName,
            DiagnosticDescriptor diagnosticDescriptor)
        {
            var options = context.Options;
            if (!IsAnalyzerEnabled(options))
                return;

            if (!options.IsMSBuildPropertyValueTrue(msbuildPropertyName))
                return;

            foreach (var reference in context.Compilation.References)
            {
                var refAssembly = context.Compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
                if (refAssembly is null)
                    continue;

                var assemblyMetadata = refAssembly.GetAttributes().FirstOrDefault(attr =>
                    attr.AttributeClass?.Name == "AssemblyMetadataAttribute" &&
                    attr.ConstructorArguments.Length == 2 &&
                    attr.ConstructorArguments[0].Value?.ToString() == assemblyMetadataName &&
                    string.Equals(attr.ConstructorArguments[1].Value?.ToString(), "True", StringComparison.OrdinalIgnoreCase));

                if (assemblyMetadata is null)
                {
                    var diag = Diagnostic.Create(diagnosticDescriptor, Location.None, refAssembly.Name);
                    context.ReportDiagnostic(diag);
                }
            }
        }
    }
}
