// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Numerics.Hashing;
using System.Reflection;
using System.Linq.Expressions;
using System.Diagnostics;
using System.Composition.Debugging;
using System.Composition.TypedParts.ActivationFeatures;
using System.Composition.Hosting.Core;
using System.Composition.Convention;
using System.Composition.Hosting;

namespace System.Composition.TypedParts.Discovery
{
    [DebuggerDisplay("{PartType.Name}")]
    [DebuggerTypeProxy(typeof(DiscoveredPartDebuggerProxy))]
    internal sealed class DiscoveredPart
    {
        private readonly TypeInfo _partType;
        private readonly AttributedModelProvider _attributeContext;
        private readonly ICollection<DiscoveredExport> _exports = new List<DiscoveredExport>();
        private readonly ActivationFeature[] _activationFeatures;
        private readonly Lazy<IDictionary<string, object>> _partMetadata;

        // This is unbounded so potentially a source of memory consumption,
        // but in reality unlikely to be a problem.
        private readonly IList<Type[]> _appliedArguments = new List<Type[]>();

        // Lazily initialised among potentially many exports
        private ConstructorInfo _constructor;
        private CompositeActivator _partActivator;

        private static readonly IDictionary<string, object> s_noMetadata = new Dictionary<string, object>();
        private static readonly MethodInfo s_activatorInvoke = typeof(CompositeActivator).GetTypeInfo().GetDeclaredMethod("Invoke");

        private DiscoveredPart(
            TypeInfo partType,
            AttributedModelProvider attributeContext,
            ActivationFeature[] activationFeatures,
            Lazy<IDictionary<string, object>> partMetadata)
        {
            _partType = partType;
            _attributeContext = attributeContext;
            _activationFeatures = activationFeatures;
            _partMetadata = partMetadata;
        }

        public DiscoveredPart(
            TypeInfo partType,
            AttributedModelProvider attributeContext,
            ActivationFeature[] activationFeatures)
        {
            _partType = partType;
            _attributeContext = attributeContext;
            _activationFeatures = activationFeatures;
            _partMetadata = new Lazy<IDictionary<string, object>>(() => GetPartMetadata(partType));
        }

        public TypeInfo PartType { get { return _partType; } }

        public bool IsShared { get { return ContractHelpers.IsShared(_partMetadata.Value); } }

        public void AddDiscoveredExport(DiscoveredExport export)
        {
            _exports.Add(export);
            export.Part = this;
        }

        public CompositionDependency[] GetDependencies(DependencyAccessor definitionAccessor)
        {
            return GetPartActivatorDependencies(definitionAccessor)
                .Concat(_activationFeatures
                    .SelectMany(feature => feature.GetDependencies(_partType, definitionAccessor)))
                .Where(a => a != null)
                .ToArray();
        }

        private IEnumerable<CompositionDependency> GetPartActivatorDependencies(DependencyAccessor definitionAccessor)
        {
            var partTypeAsType = _partType.AsType();

            if (_constructor == null)
            {
                foreach (var c in _partType.DeclaredConstructors.Where(ci => ci.IsPublic && !(ci.IsStatic)))
                {
                    if (_attributeContext.GetDeclaredAttribute<ImportingConstructorAttribute>(partTypeAsType, c) != null)
                    {
                        if (_constructor != null)
                        {
                            string message = SR.Format(SR.DiscoveredPart_MultipleImportingConstructorsFound, _partType);
                            throw new CompositionFailedException(message);
                        }

                        _constructor = c;
                    }
                }

                if (_constructor == null && _partType.IsGenericType)
                {
                    _constructor = GetConstructorInfoFromGenericType(_partType);
                }

                if (_constructor == null)
                    _constructor = _partType.DeclaredConstructors
                        .FirstOrDefault(ci => ci.IsPublic && !(ci.IsStatic || ci.GetParameters().Any()));

                if (_constructor == null)
                {
                    string message = SR.Format(SR.DiscoveredPart_NoImportingConstructorsFound, _partType);
                    throw new CompositionFailedException(message);
                }
            }

            var cps = _constructor.GetParameters();

            for (var i = 0; i < cps.Length; ++i)
            {
                var pi = cps[i];
                var site = new ParameterImportSite(pi);

                var importInfo = ContractHelpers.GetImportInfo(pi.ParameterType, _attributeContext.GetDeclaredAttributes(partTypeAsType, pi), site);
                if (!importInfo.AllowDefault)
                {
                    yield return definitionAccessor.ResolveRequiredDependency(site, importInfo.Contract, true);
                }
                else
                {
                    CompositionDependency optional;
                    if (definitionAccessor.TryResolveOptionalDependency(site, importInfo.Contract, true, out optional))
                        yield return optional;
                }
            }
        }

        private ConstructorInfo GetConstructorInfoFromGenericType(TypeInfo type)
        {
            Type genericPartType = type.GetGenericTypeDefinition();
            TypeInfo genericPartTypeInfo = genericPartType.GetTypeInfo();
            int constructorsCount = genericPartTypeInfo.DeclaredConstructors.Count();
            ConstructorInfo constructor = null;

            for (var index = 0; index < constructorsCount; index++)
            {
                ConstructorInfo constructorInfo = genericPartTypeInfo.DeclaredConstructors.ElementAt(index);

                if (!constructorInfo.IsPublic || constructorInfo.IsStatic) continue;

                if (_attributeContext.GetDeclaredAttribute<ImportingConstructorAttribute>(genericPartType, constructorInfo) != null)
                {
                    if (constructor != null)
                    {
                        string message = SR.Format(SR.DiscoveredPart_MultipleImportingConstructorsFound, type);
                        throw new CompositionFailedException(message);
                    }

                    constructor = type.DeclaredConstructors.ElementAt(index);
                }
            }

            return constructor;
        }

        public CompositeActivator GetActivator(DependencyAccessor definitionAccessor, IEnumerable<CompositionDependency> dependencies)
        {
            if (_partActivator != null) return _partActivator;

            var contextParam = Expression.Parameter(typeof(LifetimeContext), "cc");
            var operationParm = Expression.Parameter(typeof(CompositionOperation), "op");

            var cps = _constructor.GetParameters();
            Expression[] paramActivatorCalls = new Expression[cps.Length];

            var partActivatorDependencies = dependencies
                .Where(dep => dep.Site is ParameterImportSite)
                .ToDictionary(d => ((ParameterImportSite)d.Site).Parameter, ParameterInfoComparer.Instance);

            for (var i = 0; i < cps.Length; ++i)
            {
                var pi = cps[i];
                CompositionDependency dep;

                if (partActivatorDependencies.TryGetValue(pi, out dep))
                {
                    var a = dep.Target.GetDescriptor().Activator;
                    paramActivatorCalls[i] =
                        Expression.Convert(Expression.Call(Expression.Constant(a), s_activatorInvoke, contextParam, operationParm), pi.ParameterType);
                }
                else
                {
                    paramActivatorCalls[i] = Expression.Default(pi.ParameterType);
                }
            }

            Expression body = Expression.Convert(Expression.New(_constructor, paramActivatorCalls), typeof(object));

            var activator = Expression
                .Lambda<CompositeActivator>(body, contextParam, operationParm)
                .Compile();

            foreach (var activationFeature in _activationFeatures)
                activator = activationFeature.RewriteActivator(_partType, activator, _partMetadata.Value, dependencies);

            _partActivator = activator;
            return _partActivator;
        }

        public IDictionary<string, object> GetPartMetadata(TypeInfo partType)
        {
            var partMetadata = new Dictionary<string, object>();
            foreach (var attr in _attributeContext.GetDeclaredAttributes(partType.AsType(), partType))
            {
                if (attr is PartMetadataAttribute)
                {
                    var ma = (PartMetadataAttribute)attr;
                    partMetadata.Add(ma.Name, ma.Value);
                }
            }

            return partMetadata.Count == 0 ? s_noMetadata : partMetadata;
        }

        public bool TryCloseGenericPart(Type[] typeArguments, out DiscoveredPart closed)
        {
            for (int index = 0; index < _partType.GenericTypeParameters.Length; index++)
            {
                foreach (var genericParameterConstraints in _partType.GenericTypeParameters[index].GetTypeInfo().GetGenericParameterConstraints())
                {
                    if (!genericParameterConstraints.GetTypeInfo().IsAssignableFrom(typeArguments[index].GetTypeInfo()))
                    {
                        closed = null;
                        return false;
                    }
                }
            }

            if (_appliedArguments.Any(args => Enumerable.SequenceEqual(args, typeArguments)))
            {
                closed = null;
                return false;
            }

            _appliedArguments.Add(typeArguments);

            var closedType = _partType.MakeGenericType(typeArguments).GetTypeInfo();

            var result = new DiscoveredPart(closedType, _attributeContext, _activationFeatures, _partMetadata);

            foreach (var export in _exports)
            {
                var closedExport = export.CloseGenericExport(closedType, typeArguments);
                result.AddDiscoveredExport(closedExport);
            }

            closed = result;
            return true;
        }

        public IEnumerable<DiscoveredExport> DiscoveredExports { get { return _exports; } }

        // uses the fact that current usage only has comparisons
        // between ParameterInfo objects from the same constructor reference,
        // thus only the position needs to be compared.
        // Equals checks the member reference equality in case usage changes.
        private sealed class ParameterInfoComparer : IEqualityComparer<ParameterInfo>
        {
            public static readonly ParameterInfoComparer Instance = new ParameterInfoComparer();

            public int GetHashCode(ParameterInfo obj)
            {
                return HashHelpers.Combine(obj.Position.GetHashCode(), obj.Member.GetHashCode());
            }

            public bool Equals(ParameterInfo x, ParameterInfo y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                if (x.Position != y.Position)
                {
                    return false;
                }

                if (x.Member != y.Member)
                {
                    return false;
                }

                return true;
            }
        }
    }
}
