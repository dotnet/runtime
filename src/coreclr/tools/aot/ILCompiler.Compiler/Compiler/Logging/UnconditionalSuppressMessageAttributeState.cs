// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private readonly Dictionary<TypeSystemEntity, Dictionary<int, Suppression>> _suppressions;
        private HashSet<IAssemblyDesc> InitializedAssemblies { get; }
        private readonly CompilerGeneratedState? _compilerGeneratedState;
        private readonly Logger _logger;

        public UnconditionalSuppressMessageAttributeState(CompilerGeneratedState? compilerGeneratedState, Logger logger)
        {
            _suppressions = new Dictionary<TypeSystemEntity, Dictionary<int, Suppression>>();
            InitializedAssemblies = new HashSet<IAssemblyDesc>();
            _compilerGeneratedState = compilerGeneratedState;
            _logger = logger;
        }

        private void AddSuppression(Suppression suppression)
        {
            var used = false;
            if (!_suppressions.TryGetValue(suppression.Provider, out var suppressions))
            {
                suppressions = new Dictionary<int, Suppression>();
                _suppressions.Add(suppression.Provider, suppressions);
            }
            else if (suppressions.TryGetValue(suppression.SuppressMessageInfo.Id, out Suppression? value))
            {
                used = value.Used;
                string? elementName = suppression.Provider.GetDisplayName() ?? suppression.Provider.ToString();
                _logger.LogMessage($"Element '{elementName}' has more than one unconditional suppression. Note that only the last one is used.");
            }

            suppression.Used = used;
            suppressions[suppression.SuppressMessageInfo.Id] = suppression;
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

        public void GatherSuppressions(TypeSystemEntity provider)
        {
            TryGetSuppressionsForProvider(provider, out _);
        }

        public IEnumerable<Suppression> GetUnusedSuppressions()
        {
            foreach (var (provider, suppressions) in _suppressions)
            {
                foreach (var (_, suppression) in suppressions)
                {
                    if (!suppression.Used)
                        yield return suppression;
                }
            }
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

        private bool TryGetSuppressionsForProvider(TypeSystemEntity? provider, out Dictionary<int, Suppression>? suppressions)
        {
            suppressions = null;
            if (provider == null)
                return false;

            if (_suppressions.TryGetValue(provider, out suppressions))
                return true;

            // Populate the cache with suppressions for this member. We need to look for suppressions on the
            // member itself, and on the assembly/module.

            var membersToScan = new HashSet<TypeSystemEntity> { { provider } };

            // Gather assembly-level suppressions if we haven't already. To ensure that we always cache
            // complete information for a member, we will also scan for attributes on any other members
            // targeted by the assembly-level suppressions.
            if (GetModuleFromProvider(provider) is EcmaModule module)
            {
                var assembly = module.Assembly;
                if (InitializedAssemblies.Add(assembly))
                {
                    foreach (var suppression in DecodeAssemblyAndModuleSuppressions(module))
                    {
                        AddSuppression(suppression);
                        membersToScan.Add(suppression.Provider);
                    }
                }
            }

            // Populate the cache for this member, and for any members that were targeted by assembly-level
            // suppressions to make sure the cached info is complete.
            foreach (var member in membersToScan)
            {
                if (member is ModuleDesc)
                    continue;
                foreach (var suppression in DecodeSuppressions(member))
                    AddSuppression(suppression);
            }

            return _suppressions.TryGetValue(provider, out suppressions);
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
                !warningId.StartsWith("IL") ||
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

            foreach (var ca in CustomAttributeExtensions.GetDecodedCustomAttributes(provider, UnconditionalSuppressMessageAttributeNamespace, UnconditionalSuppressMessageAttributeName))
            {
                if (!TryDecodeSuppressMessageAttributeData(ca, out var info))
                    continue;

                yield return new Suppression(info, originAttribute: ca, provider);
            }
        }

        private IEnumerable<Suppression> DecodeAssemblyAndModuleSuppressions(ModuleDesc module)
        {
            if (module is not EcmaAssembly ecmaAssembly)
                return Enumerable.Empty<Suppression>();

            return DecodeGlobalSuppressions(
                ecmaAssembly,
                ecmaAssembly.GetDecodedCustomAttributes(UnconditionalSuppressMessageAttributeNamespace, UnconditionalSuppressMessageAttributeName),
                module);
        }

        private IEnumerable<Suppression> DecodeGlobalSuppressions(EcmaAssembly module, IEnumerable<CustomAttributeValue<TypeDesc>> attributes, TypeSystemEntity provider)
        {
            foreach (CustomAttributeValue<TypeDesc> instance in attributes)
            {
                SuppressMessageInfo info;
                if (!TryDecodeSuppressMessageAttributeData(instance, out info))
                    continue;

                var scope = info.Scope?.ToLowerInvariant();
                if (info.Target == null && (scope == "module" || scope == null))
                {
                    yield return new Suppression(info, originAttribute: instance, provider);
                    continue;
                }

                switch (scope)
                {
                    case "module":
                        yield return new Suppression(info, originAttribute: instance, provider);
                        break;

                    case "type":
                    case "member":
                        if (info.Target == null)
                            break;

                        foreach (var result in DocumentationSignatureParser.GetMembersForDocumentationSignature(info.Target, module))
                            yield return new Suppression(info, originAttribute: instance, result);

                        break;
                    default:
                        _logger.LogWarning(module, DiagnosticId.InvalidScopeInUnconditionalSuppressMessage, info.Scope ?? "", module.GetName().Name, info.Target ?? "");
                        break;
                }
            }
        }

        private static bool CanStoreSuppressionsFor(TypeSystemEntity entity)
            => entity switch
            {
                PropertyPseudoDesc => false,
                EventPseudoDesc => false,
                _ => true
            };
    }
}
