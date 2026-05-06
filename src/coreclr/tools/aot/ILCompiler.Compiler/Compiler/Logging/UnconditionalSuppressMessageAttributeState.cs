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

        private sealed class AssemblyWarningsReportedHashtable : LockFreeReaderHashtable<EcmaAssembly, EcmaAssembly>
        {
            protected override bool CompareKeyToValue(EcmaAssembly key, EcmaAssembly value) => key == value;
            protected override bool CompareValueToValue(EcmaAssembly value1, EcmaAssembly value2) => value1 == value2;
            protected override EcmaAssembly CreateValueFromKey(EcmaAssembly key) => throw new NotImplementedException();
            protected override int GetKeyHashCode(EcmaAssembly key) => key.GetHashCode();
            protected override int GetValueHashCode(EcmaAssembly value) => value.GetHashCode();
        }

        private readonly CompilerGeneratedState? _compilerGeneratedState;
        private readonly Logger _logger;
        private readonly AssemblyWarningsReportedHashtable _assemblyWarningsReportedHashtable;

        public UnconditionalSuppressMessageAttributeState(CompilerGeneratedState? compilerGeneratedState, Logger logger)
        {
            _compilerGeneratedState = compilerGeneratedState;
            _logger = logger;
            _assemblyWarningsReportedHashtable = new();
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

            ModuleDesc? module = GetModuleFromProvider(warningOrigin);
            if (module is not EcmaAssembly ecmaAssembly)
                return false;

            // Only report the warnings if they were not reported already for this assembly
            List<(DiagnosticId, string?[])>? generatedWarnings = null;
            if (_assemblyWarningsReportedHashtable.TryAdd(ecmaAssembly))
                generatedWarnings = new();

            IEnumerable<Suppression>? moduleSuppressions = DecodeAssemblyAndModuleSuppressions(ecmaAssembly, generatedWarnings);

            if (generatedWarnings is not null)
            {
                foreach (var warning in generatedWarnings)
                {
                    _logger.LogWarning(ecmaAssembly, warning.Item1, warning.Item2);
                }
            }

            TypeSystemEntity? warningOriginMember = warningOrigin;
            while (warningOriginMember != null)
            {
                if (IsSuppressedOnElement(id, warningOriginMember, moduleSuppressions))
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

            // Check if there's an assembly or module level suppression.
            // Note that moduleSuppressions contains both assembly and module level suppressions all modified to target the module as the provider
            if (IsSuppressedOnElement(id, module, moduleSuppressions))
                return true;

            return false;
        }

        private static bool IsSuppressedOnElement(int id, TypeSystemEntity provider, IEnumerable<Suppression>? moduleSuppressions)
        {
            if (provider is not ModuleDesc)
            {
                foreach (var suppression in DecodeSuppressions(provider))
                {
                    if (suppression.SuppressMessageInfo.Id == id)
                        return true;
                }
            }

            if (moduleSuppressions is not null)
            {
                foreach (var suppression in moduleSuppressions)
                {
                    if (suppression.Provider == provider && suppression.SuppressMessageInfo.Id == id)
                        return true;
                }
            }

            return false;
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

        private static List<Suppression>? DecodeAssemblyAndModuleSuppressions(EcmaAssembly ecmaAssembly, List<(DiagnosticId, string?[])>? warnings)
        {
            List<Suppression>? suppressions = null;
            DecodeGlobalSuppressions(
                ecmaAssembly,
                ecmaAssembly.GetDecodedCustomAttributes(UnconditionalSuppressMessageAttributeNamespace, UnconditionalSuppressMessageAttributeName),
                ref suppressions,
                warnings);

            DecodeGlobalSuppressions(
                ecmaAssembly,
                ecmaAssembly.GetDecodedCustomAttributesForModule(UnconditionalSuppressMessageAttributeNamespace, UnconditionalSuppressMessageAttributeName),
                ref suppressions,
                warnings);

            return suppressions;
        }

        private static void DecodeGlobalSuppressions(
            EcmaAssembly module,
            IEnumerable<CustomAttributeValue<TypeDesc>> attributes,
            ref List<Suppression>? suppressions,
            List<(DiagnosticId, string?[])>? warnings)
        {
            foreach (CustomAttributeValue<TypeDesc> instance in attributes)
            {
                if (!TryDecodeSuppressMessageAttributeData(instance, out SuppressMessageInfo info))
                    continue;

                var scope = info.Scope?.ToLowerInvariant();
                if (info.Target == null && (scope == "module" || scope == null))
                {
                    suppressions ??= new();
                    suppressions.Add(new Suppression(info, originAttribute: instance, module));
                    continue;
                }

                switch (scope)
                {
                    case "module":
                        suppressions ??= new();
                        suppressions.Add(new Suppression(info, originAttribute: instance, module));
                        break;

                    case "type":
                    case "member":
                        if (info.Target == null)
                            break;

                        foreach (var result in DocumentationSignatureParser.GetMembersForDocumentationSignature(info.Target, module))
                        {
                            suppressions ??= new();
                            suppressions.Add(new Suppression(info, originAttribute: instance, result));
                        }

                        break;
                    default:
                        warnings?.Add((DiagnosticId.InvalidScopeInUnconditionalSuppressMessage, new string?[] { info.Scope ?? "", module.GetName().Name, info.Target ?? "" }));
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
