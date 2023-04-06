// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis;
using System.Diagnostics;

namespace Microsoft.Interop
{
    internal static class VirtualMethodPointerStubGenerator
    {
        public static (MethodDeclarationSyntax, ImmutableArray<Diagnostic>) GenerateManagedToNativeStub(
            IncrementalMethodStubGenerationContext methodStub)
        {
            var diagnostics = new GeneratorDiagnostics();

            // Generate stub code
            var stubGenerator = new ManagedToNativeVTableMethodGenerator(
                methodStub.ManagedToUnmanagedGeneratorFactory.Key.TargetFramework,
                methodStub.ManagedToUnmanagedGeneratorFactory.Key.TargetFrameworkVersion,
                methodStub.SignatureContext.ElementTypeInformation,
                methodStub.VtableIndexData.SetLastError,
                methodStub.VtableIndexData.ImplicitThisParameter,
                (elementInfo, ex) =>
                {
                    diagnostics.ReportMarshallingNotSupported(methodStub.DiagnosticLocation, elementInfo, ex.NotSupportedDetails);
                },
                methodStub.ManagedToUnmanagedGeneratorFactory.GeneratorFactory);

            BlockSyntax code = stubGenerator.GenerateStubBody(
                methodStub.VtableIndexData.Index,
                methodStub.CallingConvention.Array,
                methodStub.TypeKeyOwner.Syntax);

            // The owner type will always be an interface type, so the syntax will always be a NameSyntax as it's the name of a named type
            // with no additional decorators.
            Debug.Assert(methodStub.TypeKeyOwner.Syntax is NameSyntax);

            return (
                PrintGeneratedSource(
                    methodStub.StubMethodSyntaxTemplate,
                    methodStub.SignatureContext,
                    code)
                    .WithExplicitInterfaceSpecifier(ExplicitInterfaceSpecifier((NameSyntax)methodStub.TypeKeyOwner.Syntax)),
                methodStub.Diagnostics.Array.AddRange(diagnostics.Diagnostics));
        }
        private static MethodDeclarationSyntax PrintGeneratedSource(
            ContainingSyntax stubMethodSyntax,
            SignatureContext stub,
            BlockSyntax stubCode)
        {
            // Create stub function
            return MethodDeclaration(stub.StubReturnType, stubMethodSyntax.Identifier)
                .AddAttributeLists(stub.AdditionalAttributes.ToArray())
                .WithModifiers(stubMethodSyntax.Modifiers.StripTriviaFromTokens())
                .WithParameterList(ParameterList(SeparatedList(stub.StubParameters)))
                .WithBody(stubCode);
        }

        private const string ThisParameterIdentifier = "@this";

        public static (MethodDeclarationSyntax, ImmutableArray<Diagnostic>) GenerateNativeToManagedStub(
            IncrementalMethodStubGenerationContext methodStub)
        {
            var diagnostics = new GeneratorDiagnostics();
            ImmutableArray<TypePositionInfo> elements = AddImplicitElementInfos(methodStub);

            // Generate stub code
            var stubGenerator = new UnmanagedToManagedStubGenerator(
                methodStub.UnmanagedToManagedGeneratorFactory.Key.TargetFramework,
                methodStub.UnmanagedToManagedGeneratorFactory.Key.TargetFrameworkVersion,
                elements,
                (elementInfo, ex) =>
                {
                    diagnostics.ReportMarshallingNotSupported(methodStub.DiagnosticLocation, elementInfo, ex.NotSupportedDetails);
                },
                methodStub.UnmanagedToManagedGeneratorFactory.GeneratorFactory);

            BlockSyntax code = stubGenerator.GenerateStubBody(
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(ThisParameterIdentifier),
                    IdentifierName(methodStub.StubMethodSyntaxTemplate.Identifier)));

            (ParameterListSyntax unmanagedParameterList, TypeSyntax returnType, _) = stubGenerator.GenerateAbiMethodSignatureData();

            AttributeSyntax unmanagedCallersOnlyAttribute = Attribute(
                ParseName(TypeNames.UnmanagedCallersOnlyAttribute));

            if (methodStub.CallingConvention.Array.Length != 0)
            {
                unmanagedCallersOnlyAttribute = unmanagedCallersOnlyAttribute.AddArgumentListArguments(
                    AttributeArgument(
                        ImplicitArrayCreationExpression(
                            InitializerExpression(SyntaxKind.CollectionInitializerExpression,
                                SeparatedList<ExpressionSyntax>(
                                    methodStub.CallingConvention.Array.Select(callConv => TypeOfExpression(ParseName($"System.Runtime.CompilerServices.CallConv{callConv.Name.ValueText}")))))))
                    .WithNameEquals(NameEquals(IdentifierName("CallConvs"))));
            }

            MethodDeclarationSyntax unmanagedToManagedStub =
                MethodDeclaration(returnType, $"ABI_{methodStub.StubMethodSyntaxTemplate.Identifier.Text}")
                .WithModifiers(TokenList(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.StaticKeyword)))
                .WithParameterList(unmanagedParameterList)
                .AddAttributeLists(AttributeList(SingletonSeparatedList(unmanagedCallersOnlyAttribute)))
                .WithBody(code);

            return (
                unmanagedToManagedStub,
                methodStub.Diagnostics.Array.AddRange(diagnostics.Diagnostics));
        }
        private static ImmutableArray<TypePositionInfo> AddImplicitElementInfos(IncrementalMethodStubGenerationContext methodStub)
        {
            ImmutableArray<TypePositionInfo> originalElements = methodStub.SignatureContext.ElementTypeInformation;

            var elements = ImmutableArray.CreateBuilder<TypePositionInfo>(originalElements.Length + 2);

            elements.Add(new TypePositionInfo(methodStub.TypeKeyOwner, methodStub.ManagedThisMarshallingInfo)
            {
                InstanceIdentifier = ThisParameterIdentifier,
                NativeIndex = 0,
            });
            foreach (TypePositionInfo element in originalElements)
            {
                elements.Add(element with
                {
                    NativeIndex = TypePositionInfo.IncrementIndex(element.NativeIndex)
                });
            }

            if (methodStub.ExceptionMarshallingInfo != NoMarshallingInfo.Instance)
            {
                elements.Add(
                    new TypePositionInfo(
                        new ReferenceTypeInfo($"global::{TypeNames.System_Exception}", TypeNames.System_Exception),
                        methodStub.ExceptionMarshallingInfo)
                    {
                        InstanceIdentifier = "__exception",
                        ManagedIndex = TypePositionInfo.ExceptionIndex,
                        NativeIndex = TypePositionInfo.ReturnIndex
                    });
            }

            return elements.ToImmutable();
        }

        public static BlockSyntax GenerateVirtualMethodTableSlotAssignments(IEnumerable<IncrementalMethodStubGenerationContext> vtableMethods, string vtableIdentifier)
        {
            List<StatementSyntax> statements = new();
            foreach (var method in vtableMethods)
            {
                FunctionPointerTypeSyntax functionPointerType = GenerateUnmanagedFunctionPointerTypeForMethod(method);

                // <vtableParameter>[<index>] = (void*)(<functionPointerType>)&ABI_<methodIdentifier>;
                statements.Add(
                    ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            ElementAccessExpression(
                                IdentifierName(vtableIdentifier))
                            .AddArgumentListArguments(Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(method.VtableIndexData.Index)))),
                            CastExpression(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                                CastExpression(functionPointerType,
                                    PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                        IdentifierName($"ABI_{method.StubMethodSyntaxTemplate.Identifier}")))))));
            }

            return Block(statements);
        }

        private static FunctionPointerTypeSyntax GenerateUnmanagedFunctionPointerTypeForMethod(IncrementalMethodStubGenerationContext method)
        {
            var stubGenerator = new UnmanagedToManagedStubGenerator(
                method.UnmanagedToManagedGeneratorFactory.Key.TargetFramework,
                method.UnmanagedToManagedGeneratorFactory.Key.TargetFrameworkVersion,
                AddImplicitElementInfos(method),
                // Swallow diagnostics here since the diagnostics will be reported by the unmanaged->managed stub generation
                (elementInfo, ex) => { },
                method.UnmanagedToManagedGeneratorFactory.GeneratorFactory);

            List<FunctionPointerParameterSyntax> functionPointerParameters = new();
            var (paramList, retType, _) = stubGenerator.GenerateAbiMethodSignatureData();
            functionPointerParameters.AddRange(paramList.Parameters.Select(p => FunctionPointerParameter(p.Type)));
            // We add the return type as the last "parameter" here as that's what the function pointer syntax requires.
            functionPointerParameters.Add(FunctionPointerParameter(retType));

            // delegate* unmanaged<...>
            ImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax> callConv = method.CallingConvention.Array;
            FunctionPointerTypeSyntax functionPointerType = FunctionPointerType(
                    FunctionPointerCallingConvention(Token(SyntaxKind.UnmanagedKeyword), callConv.IsEmpty ? null : FunctionPointerUnmanagedCallingConventionList(SeparatedList(callConv))),
                    FunctionPointerParameterList(SeparatedList(functionPointerParameters)));
            return functionPointerType;
        }
    }
}
