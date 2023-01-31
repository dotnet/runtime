// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using ILCompiler.Dataflow;

using ILLink.Shared;

#nullable enable

namespace ILCompiler.Logging
{
    public class UnconditionalSuppressMessageAttributeState
    {
        internal const string ScopeProperty = "Scope";
        internal const string TargetProperty = "Target";
        internal const string MessageIdProperty = "MessageId";

        internal const string UnconditionalSuppressMessageAttributeNamespace = "System.Diagnostics.CodeAnalysis";
        internal const string UnconditionalSuppressMessageAttributeName = "UnconditionalSuppressMessageAttribute";

        public class Suppression
        {
            public SuppressMessageInfo SuppressMessageInfo { get; }
            public bool Used { get; set; }
            public CustomAttributeValue<TypeDesc> OriginAttribute { get; }
            public TypeSystemEntity Provider { get; }

            public Suppression(SuppressMessageInfo suppressMessageInfo, CustomAttributeValue<TypeDesc> originAttribute, TypeSystemEntity provider)
            {
                SuppressMessageInfo = suppressMessageInfo;
                OriginAttribute = originAttribute;
                Provider = provider;
            }
        }

        private sealed class EntitySuppressions
        {
            public readonly TypeSystemEntity Entity;
            public readonly ImmutableDictionary<int, Suppression> Suppressions;

            public EntitySuppressions(TypeSystemEntity entity, ImmutableDictionary<int, Suppression> suppressions)
            {
                Entity = entity;
                Suppressions = suppressions;
            }
        }

        private sealed class AssemblySuppressions : LockFreeReaderHashtable<TypeSystemEntity, EntitySuppressions>
        {
            public readonly EcmaAssembly Assembly;
            private readonly Logger _logger;

            public AssemblySuppressions(EcmaAssembly assembly, Logger logger)
            {
                Assembly = assembly;
                _logger = logger;
            }

            protected override bool CompareKeyToValue(TypeSystemEntity key, EntitySuppressions value) => key == value.Entity;
            protected override bool CompareValueToValue(EntitySuppressions value1, EntitySuppressions value2) => value1.Entity == value2.Entity;
            protected override int GetKeyHashCode(TypeSystemEntity key) => key.GetHashCode();
            protected override int GetValueHashCode(EntitySuppressions value) => value.Entity.GetHashCode();
            protected override EntitySuppressions CreateValueFromKey(TypeSystemEntity key)
            {
                Debug.Fail("Do NOT call GetOrCreateValue, instead call TryGetSuppressions");
                return new EntitySuppressions(key, null);
            }

            public bool TryGetSuppressions(TypeSystemEntity key, [NotNullWhen(true)] out IReadOnlyDictionary<int, Suppression>? suppressions)
            {
                if (TryGetValue(key, out var entitySuppressions))
                {
                    suppressions = entitySuppressions.Suppressions;
                    return true;
                }

                suppressions = null;

                // Assumes that all module/assembly level suppressions are already applied along with suppressions
                // for the members they target. So we can only get here if the entity is not targeted by assembly/module
                // level suppressions.

                if (key is ModuleDesc)
                {
                    // If there are no module level suppressions already initialized, then there are none.
                    return false;
                }

                IEnumerable<Suppression> decodedSuppressions = DecodeSuppressions(key);
                ImmutableDictionary<int, Suppression>.Builder? builder = null;
                foreach (Suppression suppression in decodedSuppressions)
                {
                    builder ??= ImmutableDictionary.CreateBuilder<int, Suppression>();
                    AddSuppressionToBuilder(builder, suppression, _logger);
                }

                if (builder == null)
                    return false;

                entitySuppressions = new EntitySuppressions(key, builder.ToImmutable());
                if (!TryAdd(entitySuppressions))
                {
                    // The value should be there (TryAdd failed above), so this should never call CreateValueFromKey
                    entitySuppressions = GetOrCreateValue(key);
                }

                suppressions = entitySuppressions.Suppressions;
                return true;
            }
        }

        private sealed class SuppressionsHashTable : LockFreeReaderHashtable<EcmaAssembly, AssemblySuppressions>
        {
            private readonly Logger _logger;

            public SuppressionsHashTable(Logger logger)
            {
                _logger = logger;
            }

            protected override bool CompareKeyToValue(EcmaAssembly key, AssemblySuppressions value) => key == value.Assembly;
            protected override bool CompareValueToValue(AssemblySuppressions value1, AssemblySuppressions value2) => value1.Assembly == value2.Assembly;
            protected override int GetKeyHashCode(EcmaAssembly key) => key.GetHashCode();
            protected override int GetValueHashCode(AssemblySuppressions value) => value.Assembly.GetHashCode();

            protected override AssemblySuppressions CreateValueFromKey(EcmaAssembly key)
            {
                Debug.Fail("Do NOT call GetOrCreateValue, instead call GetSuppressions");
                return new AssemblySuppressions(key, _logger);
            }

            /// <summary>
            /// Can't use GetOrCreateValue because of possible recursion where trying to populate the warnings
            /// can itself produce warnings.
            /// </summary>
            public AssemblySuppressions GetSuppressions(EcmaAssembly key)
            {
                if (TryGetValue(key, out AssemblySuppressions assemblySuppressions))
                    return assemblySuppressions;

                // We have to populate all assembly/module level suppressions. But since those target some entities
                // we also have to fully populate suppressions for those entities as suppressions per entity are
                // immutable (we can only construct them once per entity)

                assemblySuppressions = new(key, _logger);
                List<(DiagnosticId, string?[])> warnings = new();

                foreach (var entityGlobalSuppressions in DecodeAssemblyAndModuleSuppressions(key, warnings).GroupBy(suppression => suppression.Provider))
                {
                    ImmutableDictionary<int, Suppression>.Builder? builder = null;
                    foreach (var suppression in entityGlobalSuppressions)
                    {
                        builder ??= ImmutableDictionary.CreateBuilder<int, Suppression>();
                        AddSuppressionToBuilder(builder, suppression, _logger);
                    }

                    // Now add the suppressions on the entity itself
                    if (entityGlobalSuppressions.Key is not EcmaModule)
                    {
                        foreach (var suppression in DecodeSuppressions(entityGlobalSuppressions.Key))
                        {
                            builder ??= ImmutableDictionary.CreateBuilder<int, Suppression>();
                            AddSuppressionToBuilder(builder, suppression, _logger);
                        }
                    }

                    if (builder is not null)
                    {
                        // This should always succeed - we didn't return the assembly suppressions to anyone so no races
                        assemblySuppressions.TryAdd(new EntitySuppressions(entityGlobalSuppressions.Key, builder.ToImmutable()));
                    }
                }

                if (TryAdd(assemblySuppressions))
                {
                    // It's OK to log warnings now because the suppressions for this assembly are already in the hashtable
                    // so when this will perform the lookup, it will find them there.
                    foreach (var warning in warnings)
                    {
                        _logger.LogWarning(key, warning.Item1, warning.Item2);
                    }

                    return assemblySuppressions;
                }
                else
                {
                    // The value should be there (TryAdd failed above), so this should never call CreateValueFromKey
                    return GetOrCreateValue(key);
                }
            }
        }

        private static void AddSuppressionToBuilder(ImmutableDictionary<int, Suppression>.Builder builder, Suppression suppression, Logger logger)
        {
            if (!builder.TryAdd(suppression.SuppressMessageInfo.Id, suppression))
            {
                string? elementName = suppression.Provider.GetDisplayName() ?? suppression.Provider.ToString();
                logger.LogMessage($"Element '{elementName}' has more than one unconditional suppression. Note that only one is used.");
            }
        }

        private readonly SuppressionsHashTable _assemblySuppressions;
        private readonly CompilerGeneratedState? _compilerGeneratedState;
        private readonly Logger _logger;

        public UnconditionalSuppressMessageAttributeState(CompilerGeneratedState? compilerGeneratedState, Logger logger)
        {
            _assemblySuppressions = new SuppressionsHashTable(logger);
            _compilerGeneratedState = compilerGeneratedState;
            _logger = logger;
        }

        public bool IsSuppressed(int id, MessageOrigin warningOrigin)
        {
            // Check for suppressions on both the suppression context as well as the original member
            // (if they're different). This is to correctly handle compiler generated code
            // which needs to use suppressions from both the compiler generated scope
            // as well as the original user defined method.

            TypeSystemEntity? provider = warningOrigin.MemberDefinition;
            if (provider == null)
                return false;

            if (IsSuppressed(id, provider))
                return true;

            if (_compilerGeneratedState != null)
            {
                while (_compilerGeneratedState.TryGetOwningMethodForCompilerGeneratedMember(provider, out MethodDesc? owningMethod))
                {
                    Debug.Assert(owningMethod != provider);
                    if (IsSuppressed(id, owningMethod))
                        return true;
                    provider = owningMethod;
                }
            }

            return false;
        }

        private bool IsSuppressed(int id, TypeSystemEntity? warningOrigin)
        {
            if (warningOrigin == null)
                return false;

            TypeSystemEntity? warningOriginMember = warningOrigin;
            while (warningOriginMember != null)
            {
                if (IsSuppressedOnElement(id, warningOriginMember))
                    return true;

                if (warningOriginMember is MethodDesc method)
                {
                    if (method.GetPropertyForAccessor() is { } property)
                    {
                        Debug.Assert(property.OwningType == method.OwningType);
                        warningOriginMember = property;
                        continue;
                    }
                    else if (method.GetEventForAccessor() is { } @event)
                    {
                        Debug.Assert(@event.OwningType == method.OwningType);
                        warningOriginMember = @event;
                        continue;
                    }
                }

                warningOriginMember = warningOriginMember.GetOwningType();
            }

            ModuleDesc? module = GetModuleFromProvider(warningOrigin);
            if (module == null)
                return false;

            // Check if there's an assembly or module level suppression.
            if (IsSuppressedOnElement(id, module))
                return true;

            return false;
        }

        private bool IsSuppressedOnElement(int id, TypeSystemEntity provider)
        {
            if (TryGetSuppressionsForProvider(provider, out var suppressions))
            {
                if (suppressions != null && suppressions.TryGetValue(id, out var suppression))
                {
                    suppression.Used = true;
                    return true;
                }
            }

            return false;
        }

        private bool TryGetSuppressionsForProvider(TypeSystemEntity? provider, out IReadOnlyDictionary<int, Suppression>? suppressions)
        {
            suppressions = null;
            if (provider == null)
                return false;

            if (GetModuleFromProvider(provider) is not EcmaAssembly assembly)
                return false;

            var assemblySuppressions = _assemblySuppressions.GetSuppressions(assembly);
            return assemblySuppressions.TryGetSuppressions(provider, out suppressions);
        }

        private static bool TryDecodeSuppressMessageAttributeData(CustomAttributeValue<TypeDesc> attribute, out SuppressMessageInfo info)
        {
            info = default;

            // We need at least the Category and Id to decode the warning to suppress.
            // The only UnconditionalSuppressMessageAttribute constructor requires those two parameters.
            if (attribute.FixedArguments.Length < 2)
            {
                return false;
            }

            // Ignore the category parameter because it does not identify the warning
            // and category information can be obtained from warnings themselves.
            // We only support warnings with code pattern IL####.
            if (!(attribute.FixedArguments[1].Value is string warningId) ||
                warningId.Length < 6 ||
                !warningId.StartsWith("IL", StringComparison.Ordinal) ||
                !int.TryParse(warningId.AsSpan(2, 4), out info.Id))
            {
                return false;
            }

            if (warningId.Length > 6 && warningId[6] != ':')
                return false;

            foreach (var p in attribute.NamedArguments)
            {
                switch (p.Name)
                {
                    case ScopeProperty when p.Value is string scope:
                        info.Scope = scope;
                        break;
                    case TargetProperty when p.Value is string target:
                        info.Target = target;
                        break;
                    case MessageIdProperty when p.Value is string messageId:
                        info.MessageId = messageId;
                        break;
                }
            }

            return true;
        }

        public static ModuleDesc? GetModuleFromProvider(TypeSystemEntity provider)
        {
            switch (provider)
            {
                case ModuleDesc module:
                    return module;
                case MetadataType type:
                    return type.Module;
                default:
                    return (provider.GetOwningType() as MetadataType)?.Module;
            }
        }

        private static IEnumerable<Suppression> DecodeSuppressions(TypeSystemEntity provider)
        {
            Debug.Assert(provider is not ModuleDesc);

            foreach (CustomAttributeValue<TypeDesc> ca in GetDecodedCustomAttributes(provider, UnconditionalSuppressMessageAttributeNamespace, UnconditionalSuppressMessageAttributeName))
            {
                if (!TryDecodeSuppressMessageAttributeData(ca, out var info))
                    continue;

                yield return new Suppression(info, originAttribute: ca, provider);
            }
        }

        private static IEnumerable<Suppression> DecodeAssemblyAndModuleSuppressions(EcmaAssembly ecmaAssembly, List<(DiagnosticId, string?[])> warnings)
        {
            return DecodeGlobalSuppressions(
                ecmaAssembly,
                ecmaAssembly.GetDecodedCustomAttributes(UnconditionalSuppressMessageAttributeNamespace, UnconditionalSuppressMessageAttributeName)
                    .Concat(ecmaAssembly.GetDecodedCustomAttributesForModule(UnconditionalSuppressMessageAttributeNamespace, UnconditionalSuppressMessageAttributeName)),
                warnings);
        }

        private static IEnumerable<Suppression> DecodeGlobalSuppressions(
            EcmaAssembly module,
            IEnumerable<CustomAttributeValue<TypeDesc>> attributes,
            List<(DiagnosticId, string?[])> warnings)
        {
            foreach (CustomAttributeValue<TypeDesc> instance in attributes)
            {
                if (!TryDecodeSuppressMessageAttributeData(instance, out SuppressMessageInfo info))
                    continue;

                var scope = info.Scope?.ToLowerInvariant();
                if (info.Target == null && (scope == "module" || scope == null))
                {
                    yield return new Suppression(info, originAttribute: instance, module);
                    continue;
                }

                switch (scope)
                {
                    case "module":
                        yield return new Suppression(info, originAttribute: instance, module);
                        break;

                    case "type":
                    case "member":
                        if (info.Target == null)
                            break;

                        foreach (var result in DocumentationSignatureParser.GetMembersForDocumentationSignature(info.Target, module))
                            yield return new Suppression(info, originAttribute: instance, result);

                        break;
                    default:
                        warnings.Add((DiagnosticId.InvalidScopeInUnconditionalSuppressMessage, new string?[] { info.Scope ?? "", module.GetName().Name, info.Target ?? "" }));
                        break;
                }
            }
        }

        private static IEnumerable<CustomAttributeValue<TypeDesc>> GetDecodedCustomAttributes(TypeSystemEntity entity, string attributeNamespace, string attributeName)
        {
            switch (entity)
            {
                case MethodDesc method:
                    if (method.GetTypicalMethodDefinition() is not EcmaMethod ecmaMethod)
                        return Enumerable.Empty<CustomAttributeValue<TypeDesc>>();
                    return ecmaMethod.GetDecodedCustomAttributes(attributeNamespace, attributeName);
                case MetadataType type:
                    if (type.GetTypeDefinition() is not EcmaType ecmaType)
                        return Enumerable.Empty<CustomAttributeValue<TypeDesc>>();
                    return ecmaType.GetDecodedCustomAttributes(attributeNamespace, attributeName);
                case FieldDesc field:
                    if (field.GetTypicalFieldDefinition() is not EcmaField ecmaField)
                        return Enumerable.Empty<CustomAttributeValue<TypeDesc>>();
                    return ecmaField.GetDecodedCustomAttributes(attributeNamespace, attributeName);
                case PropertyPseudoDesc property:
                    return property.GetDecodedCustomAttributes(attributeNamespace, attributeName);
                case EventPseudoDesc @event:
                    return @event.GetDecodedCustomAttributes(attributeNamespace, attributeName);
                default:
                    Debug.Fail("Trying to operate with unsupported TypeSystemEntity " + entity.GetType().ToString());
                    return Enumerable.Empty<CustomAttributeValue<TypeDesc>>();
            }
        }
    }
}
