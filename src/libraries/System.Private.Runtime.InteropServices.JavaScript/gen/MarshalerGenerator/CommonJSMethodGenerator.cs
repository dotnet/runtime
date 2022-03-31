// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace JavaScript.MarshalerGenerator
{
    internal class CommonJSMethodGenerator
    {
        public StringBuilder prolog;
        public MethodDeclarationSyntax MethodSyntax;
        public TypeDeclarationSyntax TypeSyntax;
        public AttributeSyntax AttributeSyntax;
        public IMethodSymbol MethodSymbol;
        public IMethodSymbol AttributeSymbol;
        public AttributeData JSAttributeData;
        public JSMarshalerSig[] ParemeterSignatures;
        public JSMarshalerSig ReturnSignature;
        public MarshalerSelector MarshalerSelector;
        public string BoundFunctionName;
        public string AssemblyName;

        public ITypeSymbol ReturnType => MethodSymbol.ReturnType;
        public TypeSyntax ReturnTypeSyntax => ReturnType.AsTypeSyntax();
        public string MethodName => MethodSymbol.Name;
        public bool HasCustomMarshalers => JSAttributeData.ConstructorArguments.Length > 1;
        public bool IsVoidMethod => ReturnType.SpecialType == SpecialType.System_Void;

        public void SelectMarshalers(Compilation compilation)
        {
            JSMarshalerMetadata[] customMarshalers = null;
            if (HasCustomMarshalers)
            {
                ImmutableArray<TypedConstant> marshalerTypes = JSAttributeData.ConstructorArguments[1].Values;
                customMarshalers = marshalerTypes.Select(mt => ExtractMarshalerMeta(compilation, mt)).ToArray();
            }

            MarshalerSelector = new MarshalerSelector(compilation);
            ReturnSignature = MarshalerSelector.GetArgumentSignature(prolog, customMarshalers, MethodSymbol.ReturnType);
            for (int i = 0; i < MethodSymbol.Parameters.Length; i++)
            {
                IParameterSymbol arg = MethodSymbol.Parameters[i];
                ParemeterSignatures[i] = MarshalerSelector.GetArgumentSignature(prolog, customMarshalers, arg.Type);
            }
            AssemblyName = compilation.AssemblyName;
        }

        protected ArgumentSyntax CreateMarshallersSyntax()
        {
            ArgumentSyntax marshallersArg;
            List<ITypeSymbol> marshalersTypes = HasCustomMarshalers
                ? JSAttributeData.ConstructorArguments[1].Values.Select(a => (ITypeSymbol)a.Value).ToList()
                : new List<ITypeSymbol?>();

            if (ReturnSignature.IsAuto)
            {
                marshalersTypes.Add(ReturnSignature.MarshalerType);
            }
            marshalersTypes.AddRange(ParemeterSignatures.Where(s => s.IsAuto).Select(s => s.MarshalerType));

            if (marshalersTypes.Count > 0)
            {
                var marshalerInstances = marshalersTypes.Distinct(SymbolEqualityComparer.Default).Cast<ITypeSymbol>().Select(t =>
                {
                    return ObjectCreationExpression(t.AsTypeSyntax()).WithArgumentList(ArgumentList());
                });
                marshallersArg = Argument(ImplicitArrayCreationExpression(InitializerExpression(SyntaxKind.ArrayInitializerExpression, SeparatedList<ExpressionSyntax>(marshalerInstances))));
            }
            else
            {
                marshallersArg = Argument(LiteralExpression(SyntaxKind.NullLiteralExpression));
            }

            return marshallersArg;
        }

        protected static TypeDeclarationSyntax CreateTypeDeclarationWithoutTrivia(TypeDeclarationSyntax typeDeclaration)
        {
            var mods = AddToModifiers(StripTriviaFromModifiers(typeDeclaration.Modifiers), SyntaxKind.UnsafeKeyword);
            return TypeDeclaration(typeDeclaration.Kind(), typeDeclaration.Identifier)
                .WithModifiers(mods);
        }

        protected static SyntaxTokenList AddToModifiers(SyntaxTokenList modifiers, SyntaxKind modifierToAdd)
        {
            if (modifiers.IndexOf(modifierToAdd) >= 0)
                return modifiers;

            int idx = modifiers.IndexOf(SyntaxKind.PartialKeyword);
            return idx >= 0
                ? modifiers.Insert(idx, Token(modifierToAdd))
                : modifiers.Add(Token(modifierToAdd));
        }

        protected static SyntaxTokenList StripTriviaFromModifiers(SyntaxTokenList tokenList)
        {
            SyntaxToken[] strippedTokens = new SyntaxToken[tokenList.Count];
            for (int i = 0; i < tokenList.Count; i++)
            {
                strippedTokens[i] = tokenList[i].WithoutTrivia();
            }
            return new SyntaxTokenList(strippedTokens);
        }

        protected JSMarshalerMetadata ExtractMarshalerMeta(Compilation compilation, TypedConstant mt)
        {
            try
            {
                INamedTypeSymbol? marshalerType = compilation.GetTypeByMetadataName(mt.Value.ToString());
                ITypeSymbol marshaledType = marshalerType.BaseType.TypeArguments[0];

                var hasAfterJs = marshalerType.GetMembers("AfterToJavaScript").Length > 0;

                return new JSMarshalerMetadata
                {
                    MarshalerType = marshalerType,
                    MarshaledType = marshaledType,
                    ToManagedMethod = "MarshalToManaged",
                    ToJsMethod = "MarshalToJs",
                    AfterToJsMethod = hasAfterJs ? "AfterMarshalToJs" : null,
                };
            }
            catch (Exception ex)
            {
                prolog.AppendLine($"Failed when processing {mt.Value} \n" + ex.Message);
                return null;
            }
        }
    }
}
