﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    [Generator]
    public class ComClassGenerator : IIncrementalGenerator
    {
        private sealed record ComClassInfo(string ClassName, ContainingSyntaxContext ContainingSyntaxContext, ContainingSyntax ClassSyntax, SequenceEqualImmutableArray<string> ImplementedInterfacesNames);
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var unsafeCodeIsEnabled = context.CompilationProvider.Select((comp, ct) => comp.Options is CSharpCompilationOptions { AllowUnsafe: true }); // Unsafe code enabled
            // Get all types with the [GeneratedComClassAttribute] attribute.
            var attributedClassesOrDiagnostics = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    TypeNames.GeneratedComClassAttribute,
                    static (node, ct) => node is ClassDeclarationSyntax,
                    static (context, ct) => context)
                .Combine(unsafeCodeIsEnabled)
                .Select((data, ct) =>
                    {
                        var context = data.Left;
                        var unsafeCodeIsEnabled = data.Right;
                        var type = (INamedTypeSymbol)context.TargetSymbol;
                        var syntax = (ClassDeclarationSyntax)context.TargetNode;
                        if (!unsafeCodeIsEnabled)
                        {
                            return DiagnosticOr<ComClassInfo>.From(DiagnosticInfo.Create(GeneratorDiagnostics.RequiresAllowUnsafeBlocks, syntax.Identifier.GetLocation()));
                        }

                        if (!syntax.IsInPartialContext(out _))
                        {
                            return DiagnosticOr<ComClassInfo>.From(
                                DiagnosticInfo.Create(
                                    GeneratorDiagnostics.InvalidAttributedClassMissingPartialModifier,
                                    syntax.Identifier.GetLocation(),
                                    type.ToDisplayString()));
                        }

                        ImmutableArray<string>.Builder names = ImmutableArray.CreateBuilder<string>();
                        foreach (INamedTypeSymbol iface in type.AllInterfaces)
                        {
                            AttributeData? generatedComInterfaceAttribute = iface.GetAttributes().FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == TypeNames.GeneratedComInterfaceAttribute);
                            if (generatedComInterfaceAttribute is not null)
                            {
                                var attributeData = GeneratedComInterfaceCompilationData.GetDataFromAttribute(generatedComInterfaceAttribute);
                                if (attributeData.Options.HasFlag(ComInterfaceOptions.ManagedObjectWrapper))
                                {
                                    names.Add(iface.ToDisplayString());
                                }
                            }
                        }

                        if (names.Count == 0)
                        {
                            return DiagnosticOr<ComClassInfo>.From(DiagnosticInfo.Create(GeneratorDiagnostics.ClassDoesNotImplementAnyGeneratedComInterface,
                                syntax.Identifier.GetLocation(),
                                type.ToDisplayString()));
                        }


                        return DiagnosticOr<ComClassInfo>.From(
                            new ComClassInfo(
                                type.ToDisplayString(),
                                new ContainingSyntaxContext(syntax),
                                new ContainingSyntax(syntax.Modifiers, syntax.Kind(), syntax.Identifier, syntax.TypeParameterList),
                                new(names.ToImmutable())));
                    });

            var attributedClasses = context.FilterAndReportDiagnostics(attributedClassesOrDiagnostics);

            var className = attributedClasses.Select(static (info, ct) => info.ClassName);

            var classInfoType = attributedClasses
                .Select(static (info, ct) => new { info.ClassName, info.ImplementedInterfacesNames })
                .Select(static (info, ct) => GenerateClassInfoType(info.ImplementedInterfacesNames.Array).NormalizeWhitespace());

            var attribute = attributedClasses
                .Select(static (info, ct) => new { info.ContainingSyntaxContext, info.ClassSyntax })
                .Select(static (info, ct) => GenerateClassInfoAttributeOnUserType(info.ContainingSyntaxContext, info.ClassSyntax).NormalizeWhitespace());

            context.RegisterSourceOutput(className.Zip(classInfoType).Zip(attribute), static (context, classInfo) =>
            {
                var ((className, classInfoType), attribute) = classInfo;
                StringWriter writer = new();
                writer.WriteLine("// <auto-generated />");
                writer.WriteLine(classInfoType.ToFullString());
                writer.WriteLine();
                writer.WriteLine(attribute);
                context.AddSource(className, writer.ToString());
            });
        }

        private const string ClassInfoTypeName = "ComClassInformation";

        private static readonly AttributeSyntax s_comExposedClassAttributeTemplate =
            Attribute(
                GenericName(TypeNames.GlobalAlias + TypeNames.ComExposedClassAttribute)
                    .AddTypeArgumentListArguments(
                        IdentifierName(ClassInfoTypeName)));
        private static MemberDeclarationSyntax GenerateClassInfoAttributeOnUserType(ContainingSyntaxContext containingSyntaxContext, ContainingSyntax classSyntax) =>
            containingSyntaxContext.WrapMemberInContainingSyntaxWithUnsafeModifier(
                TypeDeclaration(classSyntax.TypeKind, classSyntax.Identifier)
                    .WithModifiers(classSyntax.Modifiers)
                    .WithTypeParameterList(classSyntax.TypeParameters)
                    .AddAttributeLists(AttributeList(SingletonSeparatedList(s_comExposedClassAttributeTemplate))));
        private static ClassDeclarationSyntax GenerateClassInfoType(ImmutableArray<string> implementedInterfaces)
        {
            const string vtablesField = "s_vtables";
            const string vtablesLocal = "vtables";
            const string detailsTempLocal = "details";
            const string countIdentifier = "count";
            var typeDeclaration = ClassDeclaration(ClassInfoTypeName)
                .AddModifiers(
                    Token(SyntaxKind.FileKeyword),
                    Token(SyntaxKind.SealedKeyword),
                    Token(SyntaxKind.UnsafeKeyword))
                .AddBaseListTypes(SimpleBaseType(TypeSyntaxes.IComExposedClass))
                .AddMembers(
                    FieldDeclaration(
                        VariableDeclaration(
                            PointerType(TypeSyntaxes.System_Runtime_InteropServices_ComWrappers_ComInterfaceEntry),
                            SingletonSeparatedList(VariableDeclarator(vtablesField))))
                    .AddModifiers(
                        Token(SyntaxKind.PrivateKeyword),
                        Token(SyntaxKind.StaticKeyword),
                        Token(SyntaxKind.VolatileKeyword)));
            List<StatementSyntax> vtableInitializationBlock = new()
            {
                // ComInterfaceEntry* vtables = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(<ClassInfoTypeName>), sizeof(ComInterfaceEntry) * <numInterfaces>);
                LocalDeclarationStatement(
                    VariableDeclaration(
                            PointerType(TypeSyntaxes.System_Runtime_InteropServices_ComWrappers_ComInterfaceEntry),
                            SingletonSeparatedList(
                                VariableDeclarator(vtablesLocal)
                                    .WithInitializer(EqualsValueClause(
                                        CastExpression(
                                            PointerType(TypeSyntaxes.System_Runtime_InteropServices_ComWrappers_ComInterfaceEntry),
                                        InvocationExpression(
                                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                TypeSyntaxes.System_Runtime_CompilerServices_RuntimeHelpers,
                                                IdentifierName("AllocateTypeAssociatedMemory")))
                                        .AddArgumentListArguments(
                                            Argument(TypeOfExpression(IdentifierName(ClassInfoTypeName))),
                                            Argument(
                                                BinaryExpression(
                                                    SyntaxKind.MultiplyExpression,
                                                    SizeOfExpression(TypeSyntaxes.System_Runtime_InteropServices_ComWrappers_ComInterfaceEntry),
                                                    LiteralExpression(
                                                        SyntaxKind.NumericLiteralExpression,
                                                        Literal(implementedInterfaces.Length))))))))))),
                // IIUnknownDerivedDetails details;
                LocalDeclarationStatement(
                    VariableDeclaration(
                        TypeSyntaxes.IIUnknownDerivedDetails,
                        SingletonSeparatedList(
                            VariableDeclarator(detailsTempLocal))))
            };
            for (int i = 0; i < implementedInterfaces.Length; i++)
            {
                string ifaceName = implementedInterfaces[i];

                // details = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(typeof(<ifaceName>).TypeHandle);
                vtableInitializationBlock.Add(
                    ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(detailsTempLocal),
                            InvocationExpression(
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        TypeSyntaxes.StrategyBasedComWrappers,
                                        IdentifierName("DefaultIUnknownInterfaceDetailsStrategy")),
                                    IdentifierName("GetIUnknownDerivedDetails")),
                                ArgumentList(
                                    SingletonSeparatedList(
                                        Argument(
                                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                TypeOfExpression(ParseName(ifaceName)),
                                                IdentifierName("TypeHandle")))))))));
                // vtable[i] = new() { IID = details.Iid, Vtable = details.ManagedVirtualMethodTable };
                vtableInitializationBlock.Add(
                    ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            ElementAccessExpression(
                                IdentifierName(vtablesLocal),
                                BracketedArgumentList(
                                    SingletonSeparatedList(
                                        Argument(
                                            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(i)))))),
                            ImplicitObjectCreationExpression(
                                ArgumentList(),
                                InitializerExpression(SyntaxKind.ObjectInitializerExpression,
                                    SeparatedList(
                                        new ExpressionSyntax[]
                                        {
                                            AssignmentExpression(
                                                SyntaxKind.SimpleAssignmentExpression,
                                                IdentifierName("IID"),
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName(detailsTempLocal),
                                                    IdentifierName("Iid"))),
                                            AssignmentExpression(
                                                SyntaxKind.SimpleAssignmentExpression,
                                                IdentifierName("Vtable"),
                                                CastExpression(
                                                    IdentifierName("nint"),
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName(detailsTempLocal),
                                                        IdentifierName("ManagedVirtualMethodTable"))))
                                        }))))));
            }

            // s_vtable = vtable;
            vtableInitializationBlock.Add(
                ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(vtablesField),
                        IdentifierName(vtablesLocal))));

            BlockSyntax getComInterfaceEntriesMethodBody = Block(
                // count = <count>;
                ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(countIdentifier),
                        LiteralExpression(SyntaxKind.NumericLiteralExpression,
                            Literal(implementedInterfaces.Length)))),
                // if (s_vtable == null)
                //   { initializer block }
                IfStatement(
                    BinaryExpression(SyntaxKind.EqualsExpression,
                        IdentifierName(vtablesField),
                        LiteralExpression(SyntaxKind.NullLiteralExpression)),
                    Block(vtableInitializationBlock)),
                // return s_vtable;
                ReturnStatement(IdentifierName(vtablesField)));

            typeDeclaration = typeDeclaration.AddMembers(
                // public static unsafe ComWrappers.ComInterfaceDispatch* GetComInterfaceEntries(out int count)
                // { body }
                MethodDeclaration(
                    PointerType(
                        TypeSyntaxes.System_Runtime_InteropServices_ComWrappers_ComInterfaceEntry),
                    "GetComInterfaceEntries")
                    .AddParameterListParameters(
                        Parameter(Identifier(countIdentifier))
                            .WithType(PredefinedType(Token(SyntaxKind.IntKeyword)))
                            .AddModifiers(Token(SyntaxKind.OutKeyword)))
                    .WithBody(getComInterfaceEntriesMethodBody)
                    .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword)));

            return typeDeclaration;
        }
    }
}
