// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.Interop
{
    [Generator]
    public class ComClassGenerator : IIncrementalGenerator
    {
        private sealed record ComClassInfo(string ClassName, ContainingSyntaxContext ContainingSyntaxContext, ContainingSyntax ClassSyntax, SequenceEqualImmutableArray<string> ImplementedInterfacesNames);
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Get all types with the [GeneratedComClassAttribute] attribute.
            var attributedClasses = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    TypeNames.GeneratedComClassAttribute,
                    static (node, ct) => node is ClassDeclarationSyntax,
                    static (context, ct) =>
                    {
                        var type = (INamedTypeSymbol)context.TargetSymbol;
                        var syntax = (ClassDeclarationSyntax)context.TargetNode;
                        ImmutableArray<string>.Builder names = ImmutableArray.CreateBuilder<string>();
                        foreach (INamedTypeSymbol iface in type.AllInterfaces)
                        {
                            if (iface.GetAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == TypeNames.GeneratedComInterfaceAttribute))
                            {
                                names.Add(iface.ToDisplayString());
                            }
                        }
                        return new ComClassInfo(
                            type.ToDisplayString(),
                            new ContainingSyntaxContext(syntax),
                            new ContainingSyntax(syntax.Modifiers, syntax.Kind(), syntax.Identifier, syntax.TypeParameterList),
                            new(names.ToImmutable()));
                    });

            var classInfoType = attributedClasses.Select(static (info, ct) => GenerateClassInfoType(info)).SelectNormalized();

            var attribute = attributedClasses.Select(static (info, ct) => GenerateClassInfoAttributeOnUserType(info)).SelectNormalized();

            context.RegisterSourceOutput(attributedClasses.Zip(classInfoType).Zip(attribute), static (context, classInfo) =>
            {
                var ((comClassInfo, classInfoType), attribute) = classInfo;
                StringWriter writer = new();
                writer.WriteLine(classInfoType.ToFullString());
                writer.WriteLine();
                writer.WriteLine(attribute);
                context.AddSource(comClassInfo.ClassName, SourceText.From(writer.ToString()));
            });
        }

        private const string ClassInfoTypeName = "ComClassInformation";

        private static readonly AttributeSyntax s_comExposedClassAttributeTemplate =
            Attribute(
                GenericName(TypeNames.ComExposedClassAttribute)
                    .AddTypeArgumentListArguments(
                        IdentifierName(ClassInfoTypeName)));
        private static MemberDeclarationSyntax GenerateClassInfoAttributeOnUserType(ComClassInfo info) =>
            info.ContainingSyntaxContext.WrapMemberInContainingSyntaxWithUnsafeModifier(
                TypeDeclaration(info.ClassSyntax.TypeKind, info.ClassSyntax.Identifier)
                    .WithModifiers(info.ClassSyntax.Modifiers)
                    .WithTypeParameterList(info.ClassSyntax.TypeParameters)
                    .AddAttributeLists(AttributeList(SingletonSeparatedList(s_comExposedClassAttributeTemplate))));
        private static ClassDeclarationSyntax GenerateClassInfoType(ComClassInfo info)
        {
            const string vtablesField = "s_vtables";
            const string vtablesLocal = "vtables";
            const string detailsTempLocal = "details";
            const string countIdentifier = "count";
            var typeDeclaration = ClassDeclaration(ClassInfoTypeName)
                .AddModifiers(Token(SyntaxKind.FileKeyword), Token(SyntaxKind.SealedKeyword))
                .AddBaseListTypes(SimpleBaseType(ParseTypeName(TypeNames.IComExposedClass)))
                .AddMembers(
                    FieldDeclaration(
                        VariableDeclaration(
                            PointerType(
                                ParseTypeName(TypeNames.System_Runtime_InteropServices_ComWrappers_ComInterfaceDispatch)),
                            SingletonSeparatedList(VariableDeclarator(vtablesField))))
                    .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.VolatileKeyword)));
            List<StatementSyntax> vtableInitializationBlock = new()
            {
                // ComInterfaceDispatch* vtables = (ComInterfaceDispatch*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(<className>), sizeof(void*) * <numInterfaces>);
                LocalDeclarationStatement(
                    VariableDeclaration(
                            PointerType(
                                ParseTypeName(TypeNames.System_Runtime_InteropServices_ComWrappers_ComInterfaceDispatch)),
                            SingletonSeparatedList(
                                VariableDeclarator(vtablesLocal)
                                    .WithInitializer(EqualsValueClause(
                                        CastExpression(
                                            PointerType(
                                                ParseTypeName(TypeNames.System_Runtime_InteropServices_ComWrappers_ComInterfaceDispatch)),
                                        InvocationExpression(
                                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                ParseTypeName(TypeNames.System_Runtime_CompilerServices_RuntimeHelpers),
                                                IdentifierName("AllocateTypeAssociatedMemory")))
                                        .AddArgumentListArguments(
                                            Argument(TypeOfExpression(ParseTypeName(info.ClassName))),
                                            Argument(
                                                BinaryExpression(
                                                    SyntaxKind.MultiplyExpression,
                                                    SizeOfExpression(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword)))),
                                                    LiteralExpression(
                                                        SyntaxKind.NumericLiteralExpression,
                                                        Literal(info.ImplementedInterfacesNames.Array.Length))))))))))),
                // IIUnknownDerivedDetails details;
                LocalDeclarationStatement(
                    VariableDeclaration(
                        ParseTypeName(TypeNames.IIUnknownDerivedDetails),
                        SingletonSeparatedList(
                            VariableDeclarator(detailsTempLocal))))
            };
            for (int i = 0; i < info.ImplementedInterfacesNames.Array.Length; i++)
            {
                string ifaceName = info.ImplementedInterfacesNames.Array[i];

                // details = StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(typeof(<ifaceName>).TypeHandle);
                vtableInitializationBlock.Add(
                    ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(detailsTempLocal),
                            InvocationExpression(
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        ParseTypeName(TypeNames.StrategyBasedComWrappers),
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
                                                    IdentifierName("details"),
                                                    IdentifierName("Iid"))),
                                            AssignmentExpression(
                                                SyntaxKind.SimpleAssignmentExpression,
                                                IdentifierName("Vtable"),
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName("details"),
                                                    IdentifierName("ManagedVirtualMethodTable")))
                                        }))))));
            }

            // s_vtable = vtable;
            vtableInitializationBlock.Add(
                ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(vtablesField),
                        IdentifierName(vtablesLocal))));

            BlockSyntax getComInterfaceEntriesMethodBody = Block(
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
                        ParseTypeName(TypeNames.System_Runtime_InteropServices_ComWrappers_ComInterfaceDispatch)),
                    "GetComInterfaceEntries")
                    .AddParameterListParameters(
                        Parameter(Identifier(countIdentifier))
                            .WithType(RefType(PredefinedType(Token(SyntaxKind.IntKeyword)))))
                    .WithBody(getComInterfaceEntriesMethodBody)
                    .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.UnsafeKeyword)));

            return typeDeclaration;
        }
    }
}
