// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using System;

namespace Microsoft.Extensions.Options.Generators
{
    internal sealed class DiagDescriptors : DiagDescriptorsBase
    {
        private const string Category = "Microsoft.Extensions.Options.SourceGeneration";

        public static DiagnosticDescriptor CantUseWithGenericTypes { get; } = Make(
            id: "SYSLIB1201",
            title: SR.CantUseWithGenericTypesTitle,
            messageFormat: SR.CantUseWithGenericTypesMessage,
            category: Category);

        public static DiagnosticDescriptor NoEligibleMember { get; } = Make(
            id: "SYSLIB1202",
            title: SR.NoEligibleMemberTitle,
            messageFormat: SR.NoEligibleMemberMessage,
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning);

        public static DiagnosticDescriptor NoEligibleMembersFromValidator { get; } = Make(
            id: "SYSLIB1203",
            title: SR.NoEligibleMembersFromValidatorTitle,
            messageFormat: SR.NoEligibleMembersFromValidatorMessage,
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning);

        public static DiagnosticDescriptor DoesntImplementIValidateOptions { get; } = Make(
            id: "SYSLIB1204",
            title: SR.DoesntImplementIValidateOptionsTitle,
            messageFormat: SR.DoesntImplementIValidateOptionsMessage,
            category: Category);

        public static DiagnosticDescriptor AlreadyImplementsValidateMethod { get; } = Make(
            id: "SYSLIB1205",
            title: SR.AlreadyImplementsValidateMethodTitle,
            messageFormat: SR.AlreadyImplementsValidateMethodMessage,
            category: Category);

        public static DiagnosticDescriptor MemberIsInaccessible { get; } = Make(
            id: "SYSLIB1206",
            title: SR.MemberIsInaccessibleTitle,
            messageFormat: SR.MemberIsInaccessibleMessage,
            category: Category);

        public static DiagnosticDescriptor NotEnumerableType { get; } = Make(
            id: "SYSLIB1207",
            title: SR.NotEnumerableTypeTitle,
            messageFormat: SR.NotEnumerableTypeMessage,
            category: Category);

        public static DiagnosticDescriptor ValidatorsNeedSimpleConstructor { get; } = Make(
            id: "SYSLIB1208",
            title: SR.ValidatorsNeedSimpleConstructorTitle,
            messageFormat: SR.ValidatorsNeedSimpleConstructorMessage,
            category: Category);

        public static DiagnosticDescriptor CantBeStaticClass { get; } = Make(
            id: "SYSLIB1209",
            title: SR.CantBeStaticClassTitle,
            messageFormat: SR.CantBeStaticClassMessage,
            category: Category);

        public static DiagnosticDescriptor NullValidatorType { get; } = Make(
            id: "SYSLIB1210",
            title: SR.NullValidatorTypeTitle,
            messageFormat: SR.NullValidatorTypeMessage,
            category: Category);

        public static DiagnosticDescriptor CircularTypeReferences { get; } = Make(
            id: "SYSLIB1211",
            title: SR.CircularTypeReferencesTitle,
            messageFormat: SR.CircularTypeReferencesMessage,
            category: Category);

        public static DiagnosticDescriptor PotentiallyMissingTransitiveValidation { get; } = Make(
            id: "SYSLIB1212",
            title: SR.PotentiallyMissingTransitiveValidationTitle,
            messageFormat: SR.PotentiallyMissingTransitiveValidationMessage,
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning);

        public static DiagnosticDescriptor PotentiallyMissingEnumerableValidation { get; } = Make(
            id: "SYSLIB1213",
            title: SR.PotentiallyMissingEnumerableValidationTitle,
            messageFormat: SR.PotentiallyMissingEnumerableValidationMessage,
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning);

        public static DiagnosticDescriptor CantValidateStaticOrConstMember { get; } = Make(
            id: "SYSLIB1214",
            title: SR.CantValidateStaticOrConstMemberTitle,
            messageFormat: SR.CantValidateStaticOrConstMemberMessage,
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning);

        public static DiagnosticDescriptor InaccessibleValidationAttribute { get; } = Make(
            id: "SYSLIB1215",
            title: SR.InaccessibleValidationAttributeTitle,
            messageFormat: SR.InaccessibleValidationAttributeMessage,
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info);

        public static DiagnosticDescriptor OptionsUnsupportedLanguageVersion { get; } = Make(
            id: "SYSLIB1216",
            title: SR.OptionsUnsupportedLanguageVersionTitle,
            messageFormat: SR.OptionsUnsupportedLanguageVersionMessage,
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error);

        public static DiagnosticDescriptor IncompatibleWithTypeForValidationAttribute { get; } = Make(
            id: "SYSLIB1217",
            title: SR.TypeCannotBeUsedWithTheValidationAttributeTitle,
            messageFormat: SR.TypeCannotBeUsedWithTheValidationAttributeMessage,
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning);
    }
}
