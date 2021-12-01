// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Internal.Collections;

namespace System.ComponentModel.Composition.ReflectionModel
{
    internal sealed class ReflectionComposablePartDefinition : ComposablePartDefinition, ICompositionElement
    {
        private readonly IReflectionPartCreationInfo _creationInfo;

        private volatile ImportDefinition[]? _imports;
        private volatile ExportDefinition[]? _exports;
        private volatile IDictionary<string, object?>? _metadata;
        private volatile ConstructorInfo? _constructor;
        private readonly object _lock = new object();

        public ReflectionComposablePartDefinition(IReflectionPartCreationInfo creationInfo)
        {
            if (creationInfo == null)
            {
                throw new ArgumentNullException(nameof(creationInfo));
            }
            _creationInfo = creationInfo;
        }

        public Type GetPartType()
        {
            return _creationInfo.GetPartType();
        }

        public Lazy<Type> GetLazyPartType()
        {
            return _creationInfo.GetLazyPartType();
        }

        public ConstructorInfo? GetConstructor()
        {
            if (_constructor == null)
            {
                ConstructorInfo? constructor = _creationInfo.GetConstructor();
                lock (_lock)
                {
                    if (_constructor == null)
                    {
                        _constructor = constructor;
                    }
                }
            }

            return _constructor;
        }

        private ExportDefinition[] ExportDefinitionsInternal
        {
            get
            {
                if (_exports == null)
                {
                    ExportDefinition[] exports = _creationInfo.GetExports().ToArray();
                    lock (_lock)
                    {
                        if (_exports == null)
                        {
                            _exports = exports;
                        }
                    }
                }
                return _exports;
            }
        }

        public override IEnumerable<ExportDefinition> ExportDefinitions
        {
            get
            {
                return ExportDefinitionsInternal;
            }
        }

        public override IEnumerable<ImportDefinition> ImportDefinitions
        {
            get
            {
                if (_imports == null)
                {
                    ImportDefinition[] imports = _creationInfo.GetImports().ToArray();
                    lock (_lock)
                    {
                        if (_imports == null)
                        {
                            _imports = imports;
                        }
                    }
                }
                return _imports;
            }
        }

        public override IDictionary<string, object?> Metadata
        {
            get
            {
                if (_metadata == null)
                {
                    IDictionary<string, object?> metadata = _creationInfo.GetMetadata().AsReadOnly();
                    lock (_lock)
                    {
                        if (_metadata == null)
                        {
                            _metadata = metadata;
                        }
                    }
                }
                return _metadata;
            }
        }

        internal bool IsDisposalRequired
        {
            get
            {
                return _creationInfo.IsDisposalRequired;
            }
        }

        public override ComposablePart CreatePart()
        {
            if (IsDisposalRequired)
            {
                return new DisposableReflectionComposablePart(this);
            }
            else
            {
                return new ReflectionComposablePart(this);
            }
        }

        internal override ComposablePartDefinition? GetGenericPartDefinition()
        {
            if (_creationInfo is GenericSpecializationPartCreationInfo genericCreationInfo)
            {
                return genericCreationInfo.OriginalPart;
            }

            return null;
        }

        internal override bool TryGetExports(ImportDefinition definition, out Tuple<ComposablePartDefinition, ExportDefinition>? singleMatch, out IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>>? multipleMatches)
        {
            if (this.IsGeneric())
            {
                singleMatch = null;
                multipleMatches = null;

                List<Tuple<ComposablePartDefinition, ExportDefinition>>? exports = null;

                var genericParameters = (definition.Metadata.Count > 0) ? definition.Metadata.GetValue<IEnumerable<object>>(CompositionConstants.GenericParametersMetadataName) : null;
                // if and only if generic parameters have been supplied can we attempt to "close" the generic
                if (genericParameters != null)
                {
                    // we only understand types
                    if (TryGetGenericTypeParameters(genericParameters, out Type?[]? genericTypeParameters))
                    {
                        HashSet<ComposablePartDefinition>? candidates = null;
                        ComposablePartDefinition? previousPart = null;

                        // go through all orders of generic parameters that part exports allows
                        foreach (Type[] candidateParameters in GetCandidateParameters(genericTypeParameters!))
                        {
                            if (TryMakeGenericPartDefinition(candidateParameters, out ComposablePartDefinition? candidatePart))
                            {
                                bool alreadyProcessed = false;
                                if (candidates == null)
                                {
                                    if (previousPart != null)
                                    {
                                        if (candidatePart.Equals(previousPart))
                                        {
                                            alreadyProcessed = true;
                                        }
                                        else
                                        {
                                            candidates = new HashSet<ComposablePartDefinition>();
                                            candidates.Add(previousPart);
                                            candidates.Add(candidatePart);
                                        }
                                    }
                                    else
                                    {
                                        previousPart = candidatePart;
                                    }
                                }
                                else
                                {
                                    if (candidates.Contains(candidatePart))
                                    {
                                        alreadyProcessed = true;
                                    }
                                    else
                                    {
                                        candidates.Add(candidatePart);
                                    }
                                }
                                if (!alreadyProcessed)
                                {
                                    if (candidatePart.TryGetExports(definition, out Tuple<ComposablePartDefinition, ExportDefinition>? candidateSingleMatch, out IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>>? candidateMultipleMatches))
                                    {
                                        exports = exports.FastAppendToListAllowNulls(candidateSingleMatch, candidateMultipleMatches);
                                    }
                                }
                            }
                        }
                    }
                }
                if (exports != null)
                {
                    multipleMatches = exports;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return TryGetNonGenericExports(definition, out singleMatch, out multipleMatches);
            }
        }

        // Optimised for local as array case
        private bool TryGetNonGenericExports(ImportDefinition definition, out Tuple<ComposablePartDefinition, ExportDefinition>? singleMatch, out IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>>? multipleMatches)
        {
            singleMatch = null;
            multipleMatches = null;

            List<Tuple<ComposablePartDefinition, ExportDefinition>>? multipleExports = null;
            Tuple<ComposablePartDefinition, ExportDefinition>? singleExport = null;
            bool matchesFound = false;

            foreach (var export in ExportDefinitionsInternal)
            {
                if (definition.IsConstraintSatisfiedBy(export))
                {
                    matchesFound = true;
                    if (singleExport == null)
                    {
                        singleExport = new Tuple<ComposablePartDefinition, ExportDefinition>(this, export);
                    }
                    else
                    {
                        if (multipleExports == null)
                        {
                            multipleExports = new List<Tuple<ComposablePartDefinition, ExportDefinition>>();
                            multipleExports.Add(singleExport);
                        }
                        multipleExports.Add(new Tuple<ComposablePartDefinition, ExportDefinition>(this, export));
                    }
                }
            }

            if (!matchesFound)
            {
                return false;
            }

            if (multipleExports != null)
            {
                multipleMatches = multipleExports;
            }
            else
            {
                singleMatch = singleExport;
            }
            return true;
        }

        private IEnumerable<Type[]> GetCandidateParameters(Type[] genericParameters)
        {
            // we iterate over all exports and find only generic ones. Assuming the arity matches, we reorder the original parameters
            foreach (ExportDefinition export in ExportDefinitionsInternal)
            {
                var genericParametersOrder = export.Metadata.GetValue<int[]>(CompositionConstants.GenericExportParametersOrderMetadataName);
                if ((genericParametersOrder != null) && (genericParametersOrder.Length == genericParameters.Length))
                {
                    yield return GenericServices.Reorder(genericParameters, genericParametersOrder);
                }
            }

        }

        private static bool TryGetGenericTypeParameters(IEnumerable<object> genericParameters, [NotNullWhen(true)] out Type?[]? genericTypeParameters)
        {
            genericTypeParameters = genericParameters as Type[];
            if (genericTypeParameters == null)
            {
                object[] genericParametersAsArray = genericParameters.AsArray();
                genericTypeParameters = new Type[genericParametersAsArray.Length];
                for (int i = 0; i < genericParametersAsArray.Length; i++)
                {
                    genericTypeParameters[i] = genericParametersAsArray[i] as Type;
                    if (genericTypeParameters[i] == null)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        internal bool TryMakeGenericPartDefinition(Type[] genericTypeParameters, [NotNullWhen(true)] out ComposablePartDefinition? genericPartDefinition)
        {
            genericPartDefinition = null;

            if (!GenericSpecializationPartCreationInfo.CanSpecialize(Metadata, genericTypeParameters))
            {
                return false;
            }

            genericPartDefinition = new ReflectionComposablePartDefinition(new GenericSpecializationPartCreationInfo(_creationInfo, this, genericTypeParameters));
            return true;
        }

        string ICompositionElement.DisplayName
        {
            get { return _creationInfo.DisplayName; }
        }

        ICompositionElement? ICompositionElement.Origin
        {
            get { return _creationInfo.Origin; }
        }

        public override string ToString()
        {
            return _creationInfo.DisplayName;
        }

        public override bool Equals(object? obj)
        {
            if (_creationInfo.IsIdentityComparison)
            {
                return object.ReferenceEquals(this, obj);
            }
            else
            {
                return obj is ReflectionComposablePartDefinition that && _creationInfo.Equals(that._creationInfo);
            }
        }

        public override int GetHashCode()
        {
            if (_creationInfo.IsIdentityComparison)
            {
                return base.GetHashCode();
            }
            else
            {
                return _creationInfo.GetHashCode();
            }
        }
    }
}
