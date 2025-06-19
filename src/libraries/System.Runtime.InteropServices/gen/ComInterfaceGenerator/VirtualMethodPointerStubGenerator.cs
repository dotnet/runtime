// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.Interop.SyntaxFactoryExtensions;
using Microsoft.CodeAnalysis;
using System.Diagnostics;

namespace Microsoft.Interop
{
    internal static class VirtualMethodPointerStubGenerator
    {
        private const string NativeThisParameterIdentifier = "__this";
        private const string VirtualMethodTableIdentifier = "__vtable";
        private const string VirtualMethodTarget = "__target";

        public static (MethodDeclarationSyntax, ImmutableArray<DiagnosticInfo>) GenerateManagedToNativeStub(
            IncrementalMethodStubGenerationContext methodStub,
            Func<EnvironmentFlags, MarshalDirection, IMarshallingGeneratorResolver> generatorResolverCreator)
        {
            var diagnostics = new GeneratorDiagnosticsBag(new DiagnosticDescriptorProvider(), methodStub.DiagnosticLocation, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.ComInterfaceGenerator.SR));

            ImmutableArray<TypePositionInfo> elements = methodStub.SignatureContext.ElementTypeInformation;

            if (methodStub.VtableIndexData.ImplicitThisParameter)
            {
                elements = AddManagedToUnmanagedImplicitThis(methodStub);
            }

            // Generate stub code
            var stubGenerator = new ManagedToNativeStubGenerator(
                elements,
                methodStub.VtableIndexData.SetLastError,
                diagnostics,
                generatorResolverCreator(methodStub.EnvironmentFlags, MarshalDirection.ManagedToUnmanaged),
                new CodeEmitOptions(SkipInit: true));

            BlockSyntax code = stubGenerator.GenerateStubBody(VirtualMethodTarget);

            var setupStatements = new List<StatementSyntax>
            {
                // var (<thisParameter>, <virtualMethodTable>) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(<containingTypeName>));
                AssignmentStatement(
                        DeclarationExpression(
                            IdentifierName("var"),
                            ParenthesizedVariableDesignation(
                                SeparatedList<VariableDesignationSyntax>(
                                    new[]{
                                        SingleVariableDesignation(
                                            Identifier(NativeThisParameterIdentifier)),
                                        SingleVariableDesignation(
                                            Identifier(VirtualMethodTableIdentifier))}))),
                        MethodInvocation(
                                ParenthesizedExpression(
                                    CastExpression(
                                        TypeSyntaxes.IUnmanagedVirtualMethodTableProvider,
                                        ThisExpression())),
                                IdentifierName("GetVirtualMethodTableInfoForKey"),
                                Argument(TypeOfExpression(methodStub.TypeKeyOwner.Syntax)))),
                // var <target> = ((<delegateType>)<virtualMethodTable>[<index>]);
                AssignmentStatement(
                    DeclarationExpression(
                            IdentifierName("var"),
                            SingleVariableDesignation(Identifier(VirtualMethodTarget))),
                    CreateFunctionPointerExpression(
                        stubGenerator,
                        IndexExpression(
                            IdentifierName(VirtualMethodTableIdentifier),
                            Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(methodStub.VtableIndexData.Index)))),
                        methodStub.CallingConvention.Array)),
            };

            code = Block(List([
                .. setupStatements,
                code,
            ]));

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

        private static ParenthesizedExpressionSyntax CreateFunctionPointerExpression(
            ManagedToNativeStubGenerator stubGenerator,
            ExpressionSyntax untypedFunctionPointerExpression,
            ImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax> callConv)
        {
            List<FunctionPointerParameterSyntax> functionPointerParameters = [];
            var (paramList, retType, _) = stubGenerator.GenerateTargetMethodSignatureData();
            functionPointerParameters.AddRange(paramList.Parameters.Select(p => FunctionPointerParameter(attributeLists: default, p.Modifiers, p.Type)));
            functionPointerParameters.Add(FunctionPointerParameter(retType));

            // ((delegate* unmanaged<...>)<untypedFunctionPointerExpression>)
            return ParenthesizedExpression(CastExpression(
                FunctionPointerType(
                    FunctionPointerCallingConvention(Token(SyntaxKind.UnmanagedKeyword), callConv.IsEmpty ? null : FunctionPointerUnmanagedCallingConventionList(SeparatedList(callConv))),
                    FunctionPointerParameterList(SeparatedList(functionPointerParameters))),
                untypedFunctionPointerExpression));
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

        private const string ManagedThisParameterIdentifier = "@this";

        public static (MethodDeclarationSyntax, ImmutableArray<DiagnosticInfo>) GenerateNativeToManagedStub(
            IncrementalMethodStubGenerationContext methodStub,
            Func<EnvironmentFlags, MarshalDirection, IMarshallingGeneratorResolver> generatorResolverCreator)
        {
            var diagnostics = new GeneratorDiagnosticsBag(new DiagnosticDescriptorProvider(), methodStub.DiagnosticLocation, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.ComInterfaceGenerator.SR));

            ImmutableArray<TypePositionInfo> elements = AddUnmanagedToManagedImplicitElementInfos(methodStub);

            // Generate stub code
            var stubGenerator = new UnmanagedToManagedStubGenerator(
                elements,
                diagnostics,
                generatorResolverCreator(methodStub.EnvironmentFlags, MarshalDirection.UnmanagedToManaged));

            BlockSyntax code = stubGenerator.GenerateStubBody(
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(ManagedThisParameterIdentifier),
                    IdentifierName(methodStub.StubMethodSyntaxTemplate.Identifier)));

            (ParameterListSyntax unmanagedParameterList, TypeSyntax returnType, _) = stubGenerator.GenerateAbiMethodSignatureData();

            AttributeSyntax unmanagedCallersOnlyAttribute = Attribute(
                NameSyntaxes.UnmanagedCallersOnlyAttribute);

            if (methodStub.CallingConvention.Array.Length != 0)
            {
                unmanagedCallersOnlyAttribute = unmanagedCallersOnlyAttribute.AddArgumentListArguments(
                    AttributeArgument(
                        ImplicitArrayCreationExpression(
                            InitializerExpression(SyntaxKind.CollectionInitializerExpression,
                                SeparatedList<ExpressionSyntax>(
                                    methodStub.CallingConvention.Array.Select(callConv => TypeOfExpression(TypeSyntaxes.CallConv(callConv.Name.ValueText)))))))
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

        private static ImmutableArray<TypePositionInfo> AddManagedToUnmanagedImplicitThis(IncrementalMethodStubGenerationContext methodStub)
        {
            ImmutableArray<TypePositionInfo> originalElements = methodStub.SignatureContext.ElementTypeInformation;

            var elements = ImmutableArray.CreateBuilder<TypePositionInfo>(originalElements.Length + 2);

            elements.Add(new TypePositionInfo(new PointerTypeInfo("void*", "void*", false), methodStub.ManagedThisMarshallingInfo)
            {
                InstanceIdentifier = NativeThisParameterIdentifier,
                NativeIndex = 0,
            });
            foreach (TypePositionInfo element in originalElements)
            {
                elements.Add(element with
                {
                    NativeIndex = TypePositionInfo.IncrementIndex(element.NativeIndex)
                });
            }

            return elements.ToImmutable();
        }

        private static ImmutableArray<TypePositionInfo> AddUnmanagedToManagedImplicitElementInfos(IncrementalMethodStubGenerationContext methodStub)
        {
            ImmutableArray<TypePositionInfo> originalElements = methodStub.SignatureContext.ElementTypeInformation;

            var elements = ImmutableArray.CreateBuilder<TypePositionInfo>(originalElements.Length + 2);

            elements.Add(new TypePositionInfo(methodStub.TypeKeyOwner, methodStub.ManagedThisMarshallingInfo)
            {
                InstanceIdentifier = ManagedThisParameterIdentifier,
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
                        new ReferenceTypeInfo(TypeNames.GlobalAlias + TypeNames.System_Exception, TypeNames.System_Exception),
                        methodStub.ExceptionMarshallingInfo)
                    {
                        InstanceIdentifier = "__exception",
                        ManagedIndex = TypePositionInfo.ExceptionIndex,
                        NativeIndex = TypePositionInfo.ReturnIndex
                    });
            }

            return elements.ToImmutable();
        }

        public static BlockSyntax GenerateVirtualMethodTableSlotAssignments(
            IEnumerable<IncrementalMethodStubGenerationContext> vtableMethods,
            string vtableIdentifier,
            Func<EnvironmentFlags, MarshalDirection, IMarshallingGeneratorResolver> generatorResolverCreator)
        {
            List<StatementSyntax> statements = new();
            foreach (var method in vtableMethods)
            {
                FunctionPointerTypeSyntax functionPointerType = GenerateUnmanagedFunctionPointerTypeForMethod(method, generatorResolverCreator);

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

        public static FunctionPointerTypeSyntax GenerateUnmanagedFunctionPointerTypeForMethod(
            IncrementalMethodStubGenerationContext method,
            Func<EnvironmentFlags, MarshalDirection, IMarshallingGeneratorResolver> generatorResolverCreator)
        {
            var diagnostics = new GeneratorDiagnosticsBag(new DiagnosticDescriptorProvider(), method.DiagnosticLocation, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.ComInterfaceGenerator.SR));

            var stubGenerator = new UnmanagedToManagedStubGenerator(
                AddUnmanagedToManagedImplicitElementInfos(method),
                diagnostics,
                generatorResolverCreator(method.EnvironmentFlags, MarshalDirection.UnmanagedToManaged));

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

        public static ImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax> GenerateCallConvSyntaxFromAttributes(AttributeData? suppressGCTransitionAttribute, AttributeData? unmanagedCallConvAttribute, ImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax> defaultCallingConventions)
        {
            const string CallConvsField = "CallConvs";
            ImmutableArray<FunctionPointerUnmanagedCallingConventionSyntax>.Builder callingConventions = ImmutableArray.CreateBuilder<FunctionPointerUnmanagedCallingConventionSyntax>();

            // We'll always support adding SuppressGCTransition to other calling convention options.
            if (suppressGCTransitionAttribute is not null)
            {
                callingConventions.Add(FunctionPointerUnmanagedCallingConvention(Identifier("SuppressGCTransition")));
            }

            // UnmanagedCallConvAttribute overrides the default calling convention rules.
            if (unmanagedCallConvAttribute is not null)
            {
                foreach (KeyValuePair<string, TypedConstant> arg in unmanagedCallConvAttribute.NamedArguments)
                {
                    if (arg.Key == CallConvsField)
                    {
                        foreach (TypedConstant callConv in arg.Value.Values)
                        {
                            ITypeSymbol callConvSymbol = (ITypeSymbol)callConv.Value!;
                            if (callConvSymbol.Name.StartsWith("CallConv", StringComparison.Ordinal))
                            {
                                callingConventions.Add(FunctionPointerUnmanagedCallingConvention(Identifier(callConvSymbol.Name.Substring("CallConv".Length))));
                            }
                        }
                    }
                }
            }
            else
            {
                callingConventions.AddRange(defaultCallingConventions);
            }
            return callingConventions.ToImmutable();
        }
    }
}
