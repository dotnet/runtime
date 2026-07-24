// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CdacUsageGraph.Model;
using Microsoft.CodeAnalysis;
using CdacUsageGraph.Semantic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Operations;

namespace CdacUsageGraph.Discovery;

/// <summary>
/// Phase B (part 2): parses <c>CoreCLRContracts.Register&lt;IContract&gt;("cN", t =&gt; new Impl(t))</c>
/// into the authoritative (interface, version) -&gt; implementation map. Handles multi-impl lambdas
/// (e.g. the <c>IGCInfo</c> arch switch) by taking every constructed in-assembly type.
/// </summary>
internal static class ContractRegistrationParser
{
    public static IReadOnlyList<ContractRegistration> Parse(CSharpCompilation compilation)
    {
        SymbolEqualityComparer comparer = SymbolEqualityComparer.Default;
        List<ContractRegistration> registrations = new List<ContractRegistration>();
        INamedTypeSymbol? contractRegistry = compilation.GetTypeByMetadataName(
            CdacSymbols.ContractRegistryMetadataName);
        INamedTypeSymbol? iContract = compilation.GetTypeByMetadataName(
            CdacSymbols.IContractMetadataName);

        INamedTypeSymbol? coreContracts = compilation.GetTypeByMetadataName(
            CdacSymbols.CoreCLRContractsMetadataName);
        if (coreContracts is not null && contractRegistry is not null && iContract is not null)
        {
            foreach (IMethodSymbol reg in coreContracts.GetMembers().OfType<IMethodSymbol>())
            {
                SyntaxReference? sref = reg.DeclaringSyntaxReferences.FirstOrDefault();
                if (sref is null)
                    continue;
                SyntaxNode syntax = sref.GetSyntax();
                SemanticModel model = compilation.GetSemanticModel(syntax.SyntaxTree);
                IOperation? op = model.GetOperation(syntax);
                if (op is null)
                    continue;

                foreach (IInvocationOperation inv in op.DescendantsAndSelf().OfType<IInvocationOperation>())
                {
                    if (inv.TargetMethod.Name != CdacSymbols.ContractRegistrationMethodName)
                        continue;
                    if (inv.Instance?.Type is not INamedTypeSymbol receiver ||
                        !compilation.IsAssignableTo(receiver, contractRegistry))
                        continue;
                    if (inv.TargetMethod.TypeArguments.Length != 1)
                        continue;
                    if (inv.TargetMethod.TypeArguments[0] is not INamedTypeSymbol iface)
                        continue;
                    if (!compilation.IsAssignableTo(iface, iContract))
                        continue;

                    string? version = inv.Arguments
                        .Select(a => a.Value)
                        .Select(v => v is IConversionOperation c ? c.Operand : v)
                        .Select(v => v.ConstantValue is { HasValue: true, Value: string s } ? s : null)
                        .FirstOrDefault(s => s is not null);
                    if (version is null)
                        continue;

                    foreach (IObjectCreationOperation create in inv.Descendants().OfType<IObjectCreationOperation>())
                    {
                        if (create.Type is INamedTypeSymbol impl &&
                            create.Constructor is IMethodSymbol constructor &&
                            comparer.Equals(impl.ContainingAssembly, compilation.Assembly) &&
                            compilation.IsAssignableTo(impl, iface) &&
                            compilation.IsAssignableTo(impl, iContract))
                        {
                            registrations.Add(new ContractRegistration(
                                new ContractVersion(new ContractInterface(iface.Name), version),
                                iface,
                                impl,
                                constructor));
                        }
                    }
                }
            }
        }

        // Deduplicate on (contract, version, impl name), preserving source order.
        return registrations
            .GroupBy(r => (r.Label, r.Impl.Name))
            .Select(g => g.First())
            .ToList();
    }

}
