// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CdacUsageGraph.Model;
using CdacUsageGraph.Semantic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace CdacUsageGraph.Analysis;

/// <summary>
/// Phase C/D: the forward interprocedural walk. Seeds from each contract implementation's members
/// (methods, constructors, and field/property initializers), propagates a <see cref="ContractVersion"/>
/// through callees, constructed helpers, base/generic-base classes and static-abstract dispatch,
/// and collects per-field usage into an immutable <see cref="UsageGraph"/>.
/// </summary>
internal sealed class UsageWalker
{
    private readonly CSharpCompilation _compilation;
    private readonly DataTypeIndex _index;
    private readonly CdacAttributeMatcher _attributes;
    private readonly CdacSymbolMatcher _symbols;
    private readonly StringEvaluator _strings;
    private readonly SymbolEqualityComparer _cmp = SymbolEqualityComparer.Default;

    private readonly Queue<WorkItem> _queue = new();
    private readonly HashSet<WorkItem> _visited;
    private readonly Dictionary<ContractVersion, HashSet<INamedTypeSymbol>> _constructedTypes = [];
    private readonly Dictionary<ContractVersion, List<PendingDispatch>> _pendingDispatches = [];
    private readonly Dictionary<ContractVersion, HashSet<INamedTypeSymbol>> _returnedInterfaces = [];
    private readonly UsageCollector _collector;

    public UsageWalker(
        CSharpCompilation compilation,
        DataTypeIndex index)
    {
        _compilation = compilation;
        _index = index;
        _collector = new UsageCollector(index);
        _attributes = new CdacAttributeMatcher(compilation);
        _symbols = new CdacSymbolMatcher(compilation);
        _strings = new StringEvaluator(compilation);
        _visited = new HashSet<WorkItem>(new WorkItemComparer(_cmp));
    }

    internal UsageGraph Walk(IReadOnlyList<ContractRegistration> registrations, string cdacRoot)
    {
        foreach (ContractRegistration reg in registrations)
        {
            _collector.RecordContract(reg.Label);
            AddConstructedType(reg.Label, reg.Impl);
            if (!_returnedInterfaces.TryGetValue(
                    reg.Label,
                    out HashSet<INamedTypeSymbol>? returnedInterfaces))
            {
                returnedInterfaces = new HashSet<INamedTypeSymbol>(_cmp);
                _returnedInterfaces.Add(reg.Label, returnedInterfaces);
            }
            returnedInterfaces.UnionWith(ContractEntryPoints.GetReturnedInterfaces(reg));
            Dictionary<ITypeParameterSymbol, ITypeSymbol> seed =
                GenericDispatch.BuildSubstitutions(
                    reg.Impl,
                    new Dictionary<ITypeParameterSymbol, ITypeSymbol>(_cmp),
                    _cmp);
            foreach (ISymbol m in ContractEntryPoints.Get(reg))
                _queue.Enqueue(new WorkItem(
                    m,
                    reg.Label,
                    seed));
        }

        while (_queue.Count > 0)
        {
            WorkItem item = _queue.Dequeue();
            if (!_visited.Add(item))
                continue;

            foreach (IOperation body in GetMemberOperations(item.Member))
            {
                new BodyWalker(
                    this,
                    item.Label,
                    item.Subst).Visit(body);
            }
        }

        return _collector.Build(cdacRoot, _index.Count);
    }

    // ---- per-operation handlers (called by the nested BodyWalker) --------------------------

    private void HandleInvocation(
        IInvocationOperation inv,
        ContractVersion label,
        Dictionary<ITypeParameterSymbol, ITypeSymbol> subst)
    {
        foreach (ITypeSymbol ta in inv.TargetMethod.TypeArguments)
        {
            ITypeSymbol r = GenericDispatch.Resolve(ta, subst);
            if (_index.TryGetDataType(r, out DataDescriptorType genericDataType))
            {
                _collector.RecordType(label, genericDataType);
                EnqueueDataFactory(genericDataType, label, subst);
            }
        }

        IMethodSymbol callee = inv.TargetMethod;
        if (TryHandleDataDescriptorDependencies(callee, label, subst))
            return;
        if (TryHandleStaticReference(callee, label))
            return;
        if (TryHandleGlobalRead(inv, label))
            return;
        if (IsCrossContractInvocation(callee, label))
        {
            EnqueueEscapingCallbacks(inv, label, subst);
        }
        if (RequiresDynamicDispatch(callee) &&
            ShouldFollowDynamicDispatch(callee, label))
        {
            AddPendingDispatch(
                callee,
                label,
                subst);
        }
        else if (_cmp.Equals(
            callee.OriginalDefinition.ContainingAssembly,
            _compilation.Assembly) &&
            !callee.IsAbstract &&
            callee.ContainingType.TypeKind != TypeKind.Interface)
        {
            EnqueueMember(
                callee,
                label,
                subst);
        }

        // Static-abstract / type-parameter dispatch (e.g. TImpl.StubPrecode_GetMethodDesc(...)):
        // resolve the receiver type parameter to its concrete substitution and enqueue the impl.
        IMethodSymbol? concrete = GenericDispatch.ResolveStaticAbstractTarget(
            _compilation,
            inv,
            subst);
        if (concrete is not null &&
            !TryHandleDataDescriptorDependencies(concrete, label, subst))
        {
            EnqueueMember(
                concrete,
                label,
                subst);
        }
    }

    private bool TryHandleDataDescriptorDependencies(
        IMethodSymbol method,
        ContractVersion label,
        Dictionary<ITypeParameterSymbol, ITypeSymbol> subst)
    {
        ITypeSymbol containingType = GenericDispatch.Resolve(method.ContainingType, subst);
        if (!_index.TryGetDataType(containingType, out DataDescriptorType dataType))
        {
            return false;
        }

        if (!_attributes.TryGetDescriptorDependencies(method, out DataDescriptorDependencies dependencies))
            return false;

        _collector.RecordDependencies(label, dataType, dependencies);
        return true;
    }

    private bool TryHandleStaticReference(
        IMethodSymbol method,
        ContractVersion label)
    {
        if (!_attributes.TryGetStaticReferenceFieldName(method, out string fieldName) ||
            !_index.TryGetDataType(method.ContainingType, out DataDescriptorType dataType))
        {
            return false;
        }

        foreach (string typeName in dataType.Names)
        {
            _collector.RecordGlobal(
                label,
                $"{typeName}.{fieldName}",
                "pointer",
                isOptional: true);
        }
        return true;
    }

    private bool TryHandleGlobalRead(
        IInvocationOperation invocation,
        ContractVersion label)
    {
        IMethodSymbol method = invocation.TargetMethod;
        bool isOptional;
        string? type;
        if (!_symbols.TryGetGlobalRead(method, out GlobalReadKind kind))
            return false;

        switch (kind)
        {
            case GlobalReadKind.Pointer:
                type = "pointer";
                isOptional = false;
                break;
            case GlobalReadKind.OptionalPointer:
                type = "pointer";
                isOptional = true;
                break;
            case GlobalReadKind.String:
                type = "string";
                isOptional = false;
                break;
            case GlobalReadKind.OptionalString:
                type = "string";
                isOptional = true;
                break;
            case GlobalReadKind.Generic:
                type = NativeTypeName.FromType(method.TypeArguments[0]);
                isOptional = false;
                break;
            case GlobalReadKind.OptionalGeneric:
                type = NativeTypeName.FromType(method.TypeArguments[0]);
                isOptional = true;
                break;
            default:
                return false;
        }

        IArgumentOperation? nameArgument = invocation.Arguments.FirstOrDefault(
            argument => argument.Parameter?.Ordinal == 0);
        if (nameArgument is null)
            return false;

        foreach (string name in _strings.Evaluate(nameArgument.Value))
            _collector.RecordGlobal(label, name, type, isOptional);
        return true;
    }

    private void EnqueueDataFactory(
        DataDescriptorType dataType,
        ContractVersion label,
        Dictionary<ITypeParameterSymbol, ITypeSymbol> subst)
    {
        if (GenericDispatch.FindDataFactory(dataType.Symbol) is IMethodSymbol factory)
            EnqueueMember(factory, label, subst);
    }

    private void HandleObjectCreation(
        IObjectCreationOperation oc,
        ContractVersion label,
        Dictionary<ITypeParameterSymbol, ITypeSymbol> subst)
    {
        if (oc.Type is not INamedTypeSymbol ct)
            return;
        AddConstructedType(label, ct);
        if (_index.IsDataType(ct))
        {
            _ = _index.TryGetDataType(ct, out DataDescriptorType dataType);
            _collector.RecordType(label, dataType);
            if (oc.Constructor is not null)
                EnqueueMember(oc.Constructor, label, subst);
        }
        else if (_cmp.Equals(ct.OriginalDefinition.ContainingAssembly, _compilation.Assembly))
        {
            // Construction reaches the selected constructor and the type's initializers. Other
            // helper methods become reachable through invocation edges.
            Dictionary<ITypeParameterSymbol, ITypeSymbol> sub =
                GenericDispatch.BuildSubstitutions(ct, subst, _cmp);
            EnqueueReturnedInterfaceMembers(ct, label, sub);
            foreach (ISymbol m in ConstructionEntryPoints.Get(
                ct,
                oc.Constructor))
            {
                _queue.Enqueue(new WorkItem(
                    m,
                    label,
                    sub));
            }
        }
    }

    private void EnqueueReturnedInterfaceMembers(
        INamedTypeSymbol type,
        ContractVersion label,
        Dictionary<ITypeParameterSymbol, ITypeSymbol> subst)
    {
        if (!_returnedInterfaces.TryGetValue(label, out HashSet<INamedTypeSymbol>? interfaces))
            return;

        foreach (INamedTypeSymbol @interface in type.AllInterfaces)
        {
            if (!interfaces.Contains(@interface.OriginalDefinition))
                continue;

            foreach (ISymbol member in @interface.GetMembers())
            {
                switch (member)
                {
                    case IMethodSymbol method:
                        EnqueueInterfaceImplementation(method);
                        break;
                    case IPropertySymbol property:
                        if (property.GetMethod is not null)
                            EnqueueInterfaceImplementation(property.GetMethod);
                        if (property.SetMethod is not null)
                            EnqueueInterfaceImplementation(property.SetMethod);
                        break;
                }
            }
        }

        void EnqueueInterfaceImplementation(ISymbol interfaceMember)
        {
            if (type.FindImplementationForInterfaceMember(interfaceMember) is not IMethodSymbol implementation)
                return;

            EnqueueMember(
                GenericDispatch.FindVirtualImplementation(type, implementation) ?? implementation,
                label,
                subst);
        }
    }

    private void HandlePropertyReference(
        IPropertyReferenceOperation pr,
        ContractVersion label,
        Dictionary<ITypeParameterSymbol, ITypeSymbol> subst)
    {
        if (_index.TryGetDataType(pr.Property.ContainingType, out DataDescriptorType directDataType))
        {
            HandleDataMember(pr.Property, directDataType, label, subst);
        }
        else if (pr.Property.ContainingType is { TypeKind: TypeKind.Interface } iface)
        {
            // A read through an interface implemented by Data types (e.g. an IExceptionClauseData
            // local that may hold either R2RExceptionClause or EEExceptionClause at runtime). We
            // can't know the concrete type statically, so conservatively apply every implementing
            // Data type's dependency metadata for the member.
            foreach (DataDescriptorType dataType in _index.DataTypesImplementing(iface))
            {
                if (dataType.Symbol.FindImplementationForInterfaceMember(pr.Property.OriginalDefinition) is IPropertySymbol impl)
                    HandleDataMember(impl, dataType, label, subst);
            }
        }
        else if (_symbols.IsContractRegistry(pr.Property.ContainingType))
        {
            // _target.Contracts.<X> -- a contract dependency (goes in "Contracts used", not data
            // descriptors). The callee's own reads are not attributed here (interface throw-stub body).
            _collector.RecordContractUsed(
                label,
                new ContractInterface($"I{pr.Property.Name}"));
        }
    }

    private void HandleDataMember(
        ISymbol member,
        DataDescriptorType dataType,
        ContractVersion label,
        Dictionary<ITypeParameterSymbol, ITypeSymbol> subst)
    {
        _collector.RecordType(label, dataType);
        if (_attributes.TryGetDescriptorDependencies(member, out DataDescriptorDependencies dependencies))
        {
            _collector.RecordDependencies(label, dataType, dependencies);
        }
        else if (member is IPropertySymbol { GetMethod: { } getter })
        {
            EnqueueMember(getter, label, subst);
        }
    }

    private void HandleMethodReference(
        IMethodReferenceOperation methodReference,
        ContractVersion label,
        Dictionary<ITypeParameterSymbol, ITypeSymbol> substitutions)
    {
        IMethodSymbol method = methodReference.Method;
        if (_cmp.Equals(
            method.OriginalDefinition.ContainingAssembly,
            _compilation.Assembly))
        {
            EnqueueMember(
                method,
                label,
                substitutions);
        }
    }

    // ---- member enumeration & body resolution ----------------------------------------------

    // The IOperation(s) to walk for a member: a method body, or a field/property initializer value.
    private IEnumerable<IOperation> GetMemberOperations(ISymbol member)
    {
        foreach (SyntaxReference sref in member.DeclaringSyntaxReferences)
        {
            SyntaxNode syntax = sref.GetSyntax();
            SemanticModel model = _compilation.GetSemanticModel(syntax.SyntaxTree);

            switch (member)
            {
                case IMethodSymbol:
                    if (model.GetOperation(syntax) is IOperation methodOp)
                        yield return methodOp;
                    break;
                case IFieldSymbol when syntax is VariableDeclaratorSyntax { Initializer.Value: { } fieldValue }:
                    if (model.GetOperation(fieldValue) is IOperation fieldOp)
                        yield return fieldOp;
                    break;
                case IPropertySymbol when syntax is PropertyDeclarationSyntax pds:
                    if (_index.IsDataType(member.ContainingType))
                        break;
                    if (pds.Initializer?.Value is { } initValue && model.GetOperation(initValue) is { } initOp)
                        yield return initOp;
                    if (pds.ExpressionBody?.Expression is { } exprValue && model.GetOperation(exprValue) is { } exprOp)
                        yield return exprOp;
                    if (pds.AccessorList is not { } accessors)
                        break;
                    foreach (AccessorDeclarationSyntax accessor in accessors.Accessors.Where(a =>
                        a.Kind() == SyntaxKind.GetAccessorDeclaration))
                    {
                        SyntaxNode? body = (SyntaxNode?)accessor.Body ?? accessor.ExpressionBody?.Expression;
                        if (body is not null && model.GetOperation(body) is { } accessorOp)
                            yield return accessorOp;
                    }
                    break;
            }
        }
    }

    // ---- interprocedural machinery ---------------------------------------------------------

    private void EnqueueMember(
        IMethodSymbol callee,
        ContractVersion label,
        Dictionary<ITypeParameterSymbol, ITypeSymbol> outer)
    {
        IMethodSymbol implementation = callee.PartialImplementationPart ?? callee;
        IMethodSymbol def = implementation.OriginalDefinition;
        Dictionary<ITypeParameterSymbol, ITypeSymbol> sub = new Dictionary<ITypeParameterSymbol, ITypeSymbol>(_cmp);
        if (implementation.ContainingType is INamedTypeSymbol ct)
            foreach (KeyValuePair<ITypeParameterSymbol, ITypeSymbol> kv in
                GenericDispatch.BuildSubstitutions(ct, outer, _cmp))
                sub[kv.Key] = kv.Value;
        for (int i = 0; i < def.TypeParameters.Length && i < implementation.TypeArguments.Length; i++)
            sub[def.TypeParameters[i]] = GenericDispatch.Resolve(
                implementation.TypeArguments[i],
                outer);
        _queue.Enqueue(new WorkItem(
            def,
            label,
            sub));
    }

    private static bool RequiresDynamicDispatch(IMethodSymbol method) =>
        !method.IsStatic &&
        (method.IsAbstract ||
            method.IsVirtual ||
            method.ContainingType.TypeKind == TypeKind.Interface);

    private bool ShouldFollowDynamicDispatch(
        IMethodSymbol method,
        ContractVersion label)
    {
        INamedTypeSymbol? containingType = method.ContainingType;
        if (containingType?.TypeKind != TypeKind.Interface)
            return true;

        bool isContract = _symbols.IsContract(containingType);
        return !isContract || containingType.Name == label.Interface.Name;
    }

    private bool IsCrossContractInvocation(
        IMethodSymbol method,
        ContractVersion label)
    {
        INamedTypeSymbol? containingType = method.ContainingType;
        return containingType?.TypeKind == TypeKind.Interface &&
            containingType.Name != label.Interface.Name &&
            _symbols.IsContract(containingType);
    }

    private void EnqueueEscapingCallbacks(
        IInvocationOperation invocation,
        ContractVersion label,
        Dictionary<ITypeParameterSymbol, ITypeSymbol> substitutions)
    {
        foreach (IArgumentOperation argument in invocation.Arguments)
        {
            if (argument.Parameter?.Type is not INamedTypeSymbol
                {
                    TypeKind: TypeKind.Interface,
                } callbackInterface ||
                _symbols.IsContract(callbackInterface))
            {
                continue;
            }

            ITypeSymbol? operandType = OperationInspector.Unwrap(argument.Value).Type;
            if (operandType is INamedTypeSymbol concreteType &&
                _cmp.Equals(
                    concreteType.OriginalDefinition.ContainingAssembly,
                    _compilation.Assembly))
            {
                EnqueueInterfaceMembers(
                    concreteType,
                    callbackInterface,
                    label,
                    substitutions);
            }

            if (_constructedTypes.TryGetValue(
                label,
                out HashSet<INamedTypeSymbol>? constructedTypes))
            {
                foreach (INamedTypeSymbol constructedType in constructedTypes)
                {
                    EnqueueInterfaceMembers(
                        constructedType,
                        callbackInterface,
                        label,
                        substitutions);
                }
            }
        }
    }

    private void EnqueueInterfaceMembers(
        INamedTypeSymbol concreteType,
        INamedTypeSymbol callbackInterface,
        ContractVersion label,
        Dictionary<ITypeParameterSymbol, ITypeSymbol> substitutions)
    {
        if (!CanDispatchTo(concreteType, callbackInterface))
            return;

        foreach (IMethodSymbol interfaceMethod in callbackInterface.GetMembers()
            .OfType<IMethodSymbol>())
        {
            IMethodSymbol? implementation =
                GenericDispatch.FindInterfaceImplementation(
                    concreteType,
                    interfaceMethod);
            if (implementation is not null)
            {
                EnqueueMember(
                    implementation,
                    label,
                    substitutions);
            }
        }
    }

    private void AddConstructedType(ContractVersion label, INamedTypeSymbol type)
    {
        if (!_constructedTypes.TryGetValue(label, out HashSet<INamedTypeSymbol>? types))
        {
            _constructedTypes[label] = types =
                new HashSet<INamedTypeSymbol>(_cmp);
        }
        if (!types.Add(type))
            return;

        if (_pendingDispatches.TryGetValue(label, out List<PendingDispatch>? pending))
        {
            foreach (PendingDispatch dispatch in pending)
                EnqueueDispatchTarget(type, dispatch);
        }
    }

    private void AddPendingDispatch(
        IMethodSymbol method,
        ContractVersion label,
        Dictionary<ITypeParameterSymbol, ITypeSymbol> substitutions)
    {
        PendingDispatch dispatch = new(
            method,
            label,
            new Dictionary<ITypeParameterSymbol, ITypeSymbol>(substitutions, _cmp));
        if (!_pendingDispatches.TryGetValue(label, out List<PendingDispatch>? pending))
            _pendingDispatches[label] = pending = [];
        pending.Add(dispatch);

        if (_constructedTypes.TryGetValue(label, out HashSet<INamedTypeSymbol>? types))
        {
            foreach (INamedTypeSymbol type in types)
                EnqueueDispatchTarget(type, dispatch);
        }
    }

    private void EnqueueDispatchTarget(
        INamedTypeSymbol constructedType,
        PendingDispatch dispatch)
    {
        IMethodSymbol method = dispatch.Method;
        INamedTypeSymbol? declaringType = method.ContainingType;
        if (declaringType is null ||
            !CanDispatchTo(constructedType, declaringType))
        {
            return;
        }

        IMethodSymbol? implementation =
            declaringType.TypeKind == TypeKind.Interface
                ? GenericDispatch.FindInterfaceImplementation(
                    constructedType,
                    method)
                : GenericDispatch.FindVirtualImplementation(
                    constructedType,
                    method);
        if (implementation is not null &&
            _cmp.Equals(
                implementation.OriginalDefinition.ContainingAssembly,
                _compilation.Assembly))
        {
            EnqueueMember(
                implementation,
                dispatch.Label,
                dispatch.Substitutions);
        }
    }

    private bool CanDispatchTo(
        INamedTypeSymbol constructedType,
        INamedTypeSymbol declaringType)
    {
        if (_cmp.Equals(
            constructedType.OriginalDefinition,
            declaringType.OriginalDefinition))
        {
            return true;
        }
        if (constructedType.AllInterfaces.Any(@interface =>
            _cmp.Equals(
                @interface.OriginalDefinition,
                declaringType.OriginalDefinition)))
        {
            return true;
        }
        for (INamedTypeSymbol? current = constructedType.BaseType;
            current is not null;
            current = current.BaseType)
        {
            if (_cmp.Equals(
                current.OriginalDefinition,
                declaringType.OriginalDefinition))
            {
                return true;
            }
        }
        return false;
    }

    // ---- nested types ----------------------------------------------------------------------

    private sealed record WorkItem(
        ISymbol Member,
        ContractVersion Label,
        Dictionary<ITypeParameterSymbol, ITypeSymbol> Subst);

    private sealed record PendingDispatch(
        IMethodSymbol Method,
        ContractVersion Label,
        Dictionary<ITypeParameterSymbol, ITypeSymbol> Substitutions);

    /// <summary>Depth-first per-body walker; dispatches back to the owning <see cref="UsageWalker"/>.</summary>
    private sealed class BodyWalker(
        UsageWalker owner,
        ContractVersion label,
        Dictionary<ITypeParameterSymbol, ITypeSymbol> subst)
        : OperationWalker
    {
        public override void VisitInvocation(IInvocationOperation op)
        {
            owner.HandleInvocation(
                op,
                label,
                subst);
            base.VisitInvocation(op);
        }

        public override void VisitObjectCreation(IObjectCreationOperation op)
        {
            owner.HandleObjectCreation(
                op,
                label,
                subst);
            base.VisitObjectCreation(op);
        }

        public override void VisitPropertyReference(IPropertyReferenceOperation op)
        {
            owner.HandlePropertyReference(
                op,
                label,
                subst);
            base.VisitPropertyReference(op);
        }

        public override void VisitMethodReference(IMethodReferenceOperation op)
        {
            owner.HandleMethodReference(op, label, subst);
            base.VisitMethodReference(op);
        }

    }

    private sealed class WorkItemComparer(SymbolEqualityComparer comparer)
        : IEqualityComparer<WorkItem>
    {
        public bool Equals(WorkItem? x, WorkItem? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x is null || y is null ||
                !comparer.Equals(x.Member, y.Member) ||
                x.Label != y.Label ||
                x.Subst.Count != y.Subst.Count)
                return false;

            foreach (KeyValuePair<ITypeParameterSymbol, ITypeSymbol> entry in x.Subst)
            {
                if (!y.Subst.TryGetValue(entry.Key, out ITypeSymbol? other) ||
                    !comparer.Equals(entry.Value, other))
                    return false;
            }
            return true;
        }

        public int GetHashCode(WorkItem item)
        {
            int substitutionsHash = 0;
            foreach (KeyValuePair<ITypeParameterSymbol, ITypeSymbol> entry in item.Subst)
            {
                // XOR makes the aggregate independent of dictionary iteration order.
                substitutionsHash ^= HashCode.Combine(
                    comparer.GetHashCode(entry.Key),
                    comparer.GetHashCode(entry.Value));
            }
            return HashCode.Combine(
                comparer.GetHashCode(item.Member),
                item.Label,
                substitutionsHash);
        }
    }
}
