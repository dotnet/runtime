// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Internal;
using Microsoft.Internal.Collections;
using System.Diagnostics;

namespace System.ComponentModel.Composition.ReflectionModel
{
    internal class ReflectionComposablePart : ComposablePart, ICompositionElement
    {
        private readonly ReflectionComposablePartDefinition _definition;
        private volatile Dictionary<ImportDefinition, object?>? _importValues;
        private volatile Dictionary<ImportDefinition, ImportingItem>? _importsCache;
        private volatile Dictionary<int, ExportingMember>? _exportsCache;
        private volatile bool _invokeImportsSatisfied = true;
        private bool _initialCompositionComplete;
        private volatile object? _cachedInstance;
        private readonly object _lock = new object();

        public ReflectionComposablePart(ReflectionComposablePartDefinition definition)
        {
            Requires.NotNull(definition, nameof(definition));

            _definition = definition;
        }

        public ReflectionComposablePart(ReflectionComposablePartDefinition definition, object attributedPart)
        {
            Requires.NotNull(definition, nameof(definition));
            Requires.NotNull(attributedPart, nameof(attributedPart));

            _definition = definition;

            if (attributedPart is ValueType)
            {
                throw new ArgumentException(SR.ArgumentValueType, nameof(attributedPart));
            }
            _cachedInstance = attributedPart;
        }

        protected virtual void EnsureRunning()
        {
        }
        protected void RequiresRunning()
        {
            EnsureRunning();
        }

        protected virtual void ReleaseInstanceIfNecessary(object? instance)
        {
        }

        private Dictionary<ImportDefinition, object?> ImportValues
        {
            get
            {
                var value = _importValues;
                if (value == null)
                {
                    lock (_lock)
                    {
                        value = _importValues;
                        if (value == null)
                        {
                            value = new Dictionary<ImportDefinition, object?>();
                            _importValues = value;
                        }
                    }
                }
                return value;
            }
        }

        private Dictionary<ImportDefinition, ImportingItem> ImportsCache
        {
            get
            {
                var value = _importsCache;
                if (value == null)
                {
                    lock (_lock)
                    {
                        if (value == null)
                        {
                            value = new Dictionary<ImportDefinition, ImportingItem>();
                            _importsCache = value;
                        }
                    }
                }
                return value;
            }
        }

        protected object? CachedInstance
        {
            get
            {
                lock (_lock)
                {
                    return _cachedInstance;
                }
            }
        }

        public ReflectionComposablePartDefinition Definition
        {
            get
            {
                RequiresRunning();
                return _definition;
            }
        }

        public override IDictionary<string, object?> Metadata
        {
            get
            {
                RequiresRunning();
                return Definition.Metadata;
            }
        }

        public sealed override IEnumerable<ImportDefinition> ImportDefinitions
        {
            get
            {
                RequiresRunning();
                return Definition.ImportDefinitions;
            }
        }

        public sealed override IEnumerable<ExportDefinition> ExportDefinitions
        {
            get
            {
                RequiresRunning();
                return Definition.ExportDefinitions;
            }
        }

        string ICompositionElement.DisplayName
        {
            get { return GetDisplayName(); }
        }

        ICompositionElement ICompositionElement.Origin
        {
            get { return Definition; }
        }

        // This is the ONLY method which is not executed under the ImportEngine composition lock.
        // We need to protect all state that is accesses
        public override object? GetExportedValue(ExportDefinition definition)
        {
            RequiresRunning();
            // given the implementation of the ImportEngine, this iwll be called under a lock if the part is still being composed
            // This is only called outside of the lock when the part is fully composed
            // based on that we only protect:
            // _exportsCache - and thus all calls to GetExportingMemberFromDefinition
            // access to _importValues
            // access to _initialCompositionComplete
            // access to _instance
            Requires.NotNull(definition, nameof(definition));

            ExportingMember? member = null;
            lock (_lock)
            {
                member = GetExportingMemberFromDefinition(definition);
                if (member == null)
                {
                    throw ExceptionBuilder.CreateExportDefinitionNotOnThisComposablePart(nameof(definition));
                }
                EnsureGettable();
            }

            return GetExportedValue(member);
        }

        public override void SetImport(ImportDefinition definition, IEnumerable<Export> exports)
        {
            RequiresRunning();
            Requires.NotNull(definition, nameof(definition));
            Requires.NotNull(exports, nameof(exports));

            ImportingItem? item = GetImportingItemFromDefinition(definition);
            if (item == null)
            {
                throw ExceptionBuilder.CreateImportDefinitionNotOnThisComposablePart(nameof(definition));
            }

            EnsureSettable(definition);

            // Avoid walking over exports many times
            Export[] exportsAsArray = exports.AsArray();
            EnsureCardinality(definition, exportsAsArray);

            SetImport(item, exportsAsArray);
        }

        public override void Activate()
        {
            RequiresRunning();

            SetNonPrerequisiteImports();

            // Whenever we are composed/recomposed notify the instance
            NotifyImportSatisfied();
            lock (_lock)
            {
                _initialCompositionComplete = true;
                _importValues = null;
                _importsCache = null;
            }
        }

        public override string ToString()
        {
            return GetDisplayName();
        }

        private object? GetExportedValue(ExportingMember member)
        {
            object? instance = null;
            if (member.RequiresInstance)
            {   // Only activate the instance if we actually need to

                instance = GetInstanceActivatingIfNeeded();
            }

            return member.GetExportedValue(instance, _lock);
        }

        private void SetImport(ImportingItem item, Export[] exports)
        {
            object? value = item.CastExportsToImportType(exports);

            lock (_lock)
            {
                _invokeImportsSatisfied = true;
                ImportValues[item.Definition] = value;
            }
        }

        private object? GetInstanceActivatingIfNeeded()
        {
            var cachedInstance = _cachedInstance;

            if (cachedInstance != null)
            {
                return cachedInstance;
            }
            else
            {
                ConstructorInfo? constructor = null;
                object?[]? arguments = null;
                // determine whether activation is required, and collect necessary information for activation
                // we need to do that under a lock
                lock (_lock)
                {
                    if (!RequiresActivation())
                    {
                        return null;
                    }

                    constructor = Definition.GetConstructor();
                    if (constructor == null)
                    {
                        throw new ComposablePartException(
                            SR.Format(
                                SR.ReflectionModel_PartConstructorMissing,
                                Definition.GetPartType().FullName),
                            Definition.ToElement());
                    }
                    arguments = GetConstructorArguments();
                }

                // create instance outside of the lock
                object? createdInstance = CreateInstance(constructor, arguments);

                SetPrerequisiteImports();

                // set the created instance
                if (_cachedInstance == null)
                {
                    lock (_lock)
                    {
                        if (_cachedInstance == null)
                        {
                            _cachedInstance = createdInstance;
                            createdInstance = null;
                        }
                    }
                }

                // if the instance has been already set
                if (createdInstance != null)
                {
                    ReleaseInstanceIfNecessary(createdInstance);
                }
            }

            return _cachedInstance;
        }

        private object?[] GetConstructorArguments()
        {
            ReflectionParameterImportDefinition[] parameterImports = ImportDefinitions.OfType<ReflectionParameterImportDefinition>().ToArray();
            object?[] arguments = new object?[parameterImports.Length];

            UseImportedValues(
                parameterImports,
                (import, definition, value) =>
                {
                    if (definition.Cardinality == ImportCardinality.ZeroOrMore && !import.ImportType.IsAssignableCollectionType)
                    {
                        throw new ComposablePartException(
                            SR.Format(
                                SR.ReflectionModel_ImportManyOnParameterCanOnlyBeAssigned,
                                Definition.GetPartType().FullName,
                                definition.ImportingLazyParameter.Value.Name),
                            Definition.ToElement());
                    }

                    arguments[definition.ImportingLazyParameter.Value.Position] = value;
                },
                true);

            return arguments;
        }

        // always called under a lock
        private bool RequiresActivation()
        {
            // If we have any imports then we need activation
            // (static imports are not supported)
            if (ImportDefinitions.Any())
            {
                return true;
            }

            // If we have any instance exports, then we also
            // need activation.
            return ExportDefinitions.Any(definition =>
            {
                ExportingMember? member = GetExportingMemberFromDefinition(definition);
                Debug.Assert(member != null);
                return member.RequiresInstance;
            });
        }

        // this is called under a lock
        private void EnsureGettable()
        {
            // If we're already composed then we know that
            // all pre-req imports have been satisfied
            if (_initialCompositionComplete)
            {
                return;
            }

            // Make sure all pre-req imports have been set
            foreach (ImportDefinition definition in ImportDefinitions.Where(definition => definition.IsPrerequisite))
            {
                if (_importValues == null || !ImportValues.ContainsKey(definition))
                {
                    throw new InvalidOperationException(SR.Format(
                                                            SR.InvalidOperation_GetExportedValueBeforePrereqImportSet,
                                                            definition.ToElement().DisplayName));
                }
            }
        }

        private void EnsureSettable(ImportDefinition definition)
        {
            lock (_lock)
            {
                if (_initialCompositionComplete && !definition.IsRecomposable)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_DefinitionCannotBeRecomposed);
                }
            }
        }

        private static void EnsureCardinality(ImportDefinition definition, Export[] exports)
        {
            Requires.NullOrNotNullElements(exports, nameof(exports));

            ExportCardinalityCheckResult result = ExportServices.CheckCardinality(definition, exports);

            switch (result)
            {
                case ExportCardinalityCheckResult.NoExports:
                    throw new ArgumentException(SR.Argument_ExportsEmpty, nameof(exports));

                case ExportCardinalityCheckResult.TooManyExports:
                    throw new ArgumentException(SR.Argument_ExportsTooMany, nameof(exports));

                default:
                    if (result != ExportCardinalityCheckResult.Match)
                    {
                        throw new Exception(SR.Diagnostic_InternalExceptionMessage);
                    }
                    break;
            }
        }

        private object CreateInstance(ConstructorInfo constructor, object?[] arguments)
        {
            Exception? exception = null;
            object instance = null!;

            try
            {
                instance = constructor.SafeInvoke(arguments);
            }
            catch (TypeInitializationException ex)
            {
                exception = ex;
            }
            catch (TargetInvocationException ex)
            {
                exception = ex.InnerException;
            }

            if (exception != null)
            {
                throw new ComposablePartException(
                    SR.Format(
                        SR.ReflectionModel_PartConstructorThrewException,
                        Definition.GetPartType().FullName),
                    Definition.ToElement(),
                    exception);
            }

            return instance;
        }

        private void SetNonPrerequisiteImports()
        {
            IEnumerable<ImportDefinition> members = ImportDefinitions.Where(import => !import.IsPrerequisite);

            // NOTE: Dev10 484204 The validation is turned off for post imports because of it broke declarative composition
            UseImportedValues(members, SetExportedValueForImport, false);
        }

        private void SetPrerequisiteImports()
        {
            IEnumerable<ImportDefinition> members = ImportDefinitions.Where(import => import.IsPrerequisite);

            // NOTE: Dev10 484204 The validation is turned off for post imports because of it broke declarative composition
            UseImportedValues(members, SetExportedValueForImport, false);
        }

        private void SetExportedValueForImport(ImportingItem import, ImportDefinition definition, object value)
        {
            ImportingMember importMember = (ImportingMember)import;

            object? instance = GetInstanceActivatingIfNeeded();

            importMember.SetExportedValue(instance, value);
        }

        private void UseImportedValues<TImportDefinition>(IEnumerable<TImportDefinition> definitions, Action<ImportingItem, TImportDefinition, object> useImportValue, bool errorIfMissing)
            where TImportDefinition : ImportDefinition
        {
            var result = CompositionResult.SucceededResult;

            foreach (var definition in definitions)
            {
                ImportingItem? import = GetImportingItemFromDefinition(definition);
                Debug.Assert(import != null);

                if (!TryGetImportValue(definition, out object? value))
                {
                    if (!errorIfMissing)
                    {
                        continue;
                    }

                    if (definition.Cardinality == ImportCardinality.ExactlyOne)
                    {
                        var error = CompositionError.Create(
                            CompositionErrorId.ImportNotSetOnPart,
                            SR.ImportNotSetOnPart,
                            Definition.GetPartType().FullName,
                            definition.ToString());
                        result = result.MergeError(error);
                        continue;
                    }
                    else
                    {
                        value = import.CastExportsToImportType(Array.Empty<Export>());
                    }
                }

                useImportValue(import, definition, value!);
            }

            result.ThrowOnErrors();
        }

        private bool TryGetImportValue(ImportDefinition definition, out object? value)
        {
            lock (_lock)
            {
                if (_importValues != null && ImportValues.TryGetValue(definition, out value))
                {
                    ImportValues.Remove(definition);
                    return true;
                }
            }

            value = null;
            return false;
        }

        private void NotifyImportSatisfied()
        {
            if (_invokeImportsSatisfied)
            {
                IPartImportsSatisfiedNotification? notify = GetInstanceActivatingIfNeeded() as IPartImportsSatisfiedNotification;

                lock (_lock)
                {
                    if (!_invokeImportsSatisfied)
                    {
                        //Already notified on another thread
                        return;
                    }
                    _invokeImportsSatisfied = false;
                }

                if (notify != null)
                {
                    try
                    {
                        notify.OnImportsSatisfied();
                    }
                    catch (Exception exception)
                    {
                        throw new ComposablePartException(
                            SR.Format(
                                SR.ReflectionModel_PartOnImportsSatisfiedThrewException,
                                Definition.GetPartType().FullName),
                            Definition.ToElement(),
                            exception);
                    }
                }
            }
        }

        // this is always called under a lock
        private ExportingMember? GetExportingMemberFromDefinition(ExportDefinition definition)
        {
            ReflectionMemberExportDefinition? reflectionExport = definition as ReflectionMemberExportDefinition;
            if (reflectionExport == null)
            {
                return null;
            }

            int exportIndex = reflectionExport.GetIndex();
            _exportsCache ??= new Dictionary<int, ExportingMember>();
            if (!_exportsCache.TryGetValue(exportIndex, out ExportingMember? result))
            {
                result = GetExportingMember(definition);
                if (result != null)
                {
                    _exportsCache[exportIndex] = result;
                }
            }

            return result;
        }

        private ImportingItem? GetImportingItemFromDefinition(ImportDefinition definition)
        {
            if (!ImportsCache.TryGetValue(definition, out ImportingItem? result))
            {
                result = GetImportingItem(definition);
                if (result != null)
                {
                    ImportsCache[definition] = result;
                }
            }

            return result;
        }

        private static ImportingItem? GetImportingItem(ImportDefinition definition)
        {
            if (definition is ReflectionImportDefinition reflectionDefinition)
            {
                return reflectionDefinition.ToImportingItem();
            }
            // Don't recognize it
            return null;
        }

        private static ExportingMember? GetExportingMember(ExportDefinition definition)
        {
            if (definition is ReflectionMemberExportDefinition exportDefinition)
            {
                return exportDefinition.ToExportingMember();
            }

            // Don't recognize it
            return null;
        }

        private string GetDisplayName()
        {
            return _definition.GetPartType().GetDisplayName();
        }
    }
}
