// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    internal static class VirtualMethodPointerStubGenerator
    {
        internal const string NativeThisParameterIdentifier = "__this";
        internal const string VirtualMethodTableIdentifier = "__vtable";
        internal const string VirtualMethodTarget = "__target";
        private const string ManagedThisParameterIdentifier = "@this";

        public static (GeneratedComMember, ImmutableArray<DiagnosticInfo>) GenerateManagedToNativeStub(
            SourceAvailableIncrementalMethodStubGenerationContext methodStub,
            Func<EnvironmentFlags, MarshalDirection, IMarshallingGeneratorResolver> generatorResolverCreator)
        {
            var diagnostics = new GeneratorDiagnosticsBag(new DiagnosticDescriptorProvider(), methodStub.DiagnosticLocation, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.ComInterfaceGenerator.SR));

            ImmutableArray<TypePositionInfo> elements = methodStub.VtableIndexData.ImplicitThisParameter
                ? AddManagedToUnmanagedImplicitThis(methodStub)
                : methodStub.SignatureContext.ElementTypeInformation;

            var stubGenerator = new ManagedToNativeStubGenerator(
                elements,
                methodStub.VtableIndexData.SetLastError,
                diagnostics,
                generatorResolverCreator(methodStub.EnvironmentFlags, MarshalDirection.ManagedToUnmanaged),
                new CodeEmitOptions(SkipInit: true));

            string functionPointerType = stubGenerator.GenerateTargetMethodSignatureData()
                .GetFunctionPointerType(methodStub.CallingConvention.Array);
            var writer = new IndentedTextWriter();
            using (writer.WriteBlock())
            {
                writer.WriteLine($"var ({NativeThisParameterIdentifier}, {VirtualMethodTableIdentifier}) = (({TypeNames.GlobalAlias}{TypeNames.IUnmanagedVirtualMethodTableProvider})this).GetVirtualMethodTableInfoForKey(typeof({methodStub.TypeKeyOwner.FullTypeName}));");
                writer.WriteLine($"var {VirtualMethodTarget} = (({functionPointerType}){VirtualMethodTableIdentifier}[{methodStub.VtableIndexData.Index}]);");
                stubGenerator.GenerateStubBody(writer, VirtualMethodTarget);
            }

            return (
                new GeneratedComMember(
                    methodStub.MemberKind,
                    methodStub.TemplateName,
                    GetManagedSignature(methodStub),
                    methodStub.SignatureContext.AdditionalAttributes.ToSequenceEqual(),
                    writer.ToString(),
                    string.Join(" ", methodStub.StubMethodSyntaxTemplate.Modifiers)),
                methodStub.Diagnostics.Array.AddRange(diagnostics.Diagnostics));
        }

        internal static GeneratedMethodSignature GetManagedSignature(IncrementalMethodStubGenerationContext methodStub)
        {
            ImmutableArray<GeneratedParameter> parameters = methodStub.SignatureContext.StubParameters.ToImmutableArray();
            string returnType = methodStub.SignatureContext.StubReturnType;
            if (methodStub.MemberKind.IsAccessorSetter())
            {
                returnType = parameters[parameters.Length - 1].Type;
                parameters = parameters.RemoveAt(parameters.Length - 1);
            }

            return new GeneratedMethodSignature(parameters, returnType);
        }

        public static (GeneratedComMember, ImmutableArray<DiagnosticInfo>) GenerateNativeToManagedStub(
            SourceAvailableIncrementalMethodStubGenerationContext methodStub,
            Func<EnvironmentFlags, MarshalDirection, IMarshallingGeneratorResolver> generatorResolverCreator)
        {
            var diagnostics = new GeneratorDiagnosticsBag(new DiagnosticDescriptorProvider(), methodStub.DiagnosticLocation, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.ComInterfaceGenerator.SR));

            var stubGenerator = new UnmanagedToManagedStubGenerator(
                AddUnmanagedToManagedImplicitElementInfos(methodStub),
                diagnostics,
                generatorResolverCreator(methodStub.EnvironmentFlags, MarshalDirection.UnmanagedToManaged));

            string body;
            if (methodStub.MemberKind.IsPropertyOrIndexerAccessor())
            {
                bool isSetter = methodStub.MemberKind.IsAccessorSetter();
                body = methodStub.MemberKind.IsIndexerAccessor()
                    ? stubGenerator.GenerateStubBodyForIndexer(ManagedThisParameterIdentifier, isSetter)
                    : stubGenerator.GenerateStubBodyForProperty($"{ManagedThisParameterIdentifier}.{methodStub.TemplateName}", isSetter);
            }
            else
            {
                Debug.Assert(methodStub.MemberKind is StubMemberKind.Method);
                body = stubGenerator.GenerateStubBodyForMethod($"{ManagedThisParameterIdentifier}.{methodStub.StubMethodSyntaxTemplate.Identifier}");
            }

            string unmanagedCallersOnlyAttribute = TypeNames.GlobalAlias + TypeNames.UnmanagedCallersOnlyAttribute;
            if (methodStub.CallingConvention.Array.Length != 0)
            {
                unmanagedCallersOnlyAttribute += "(CallConvs = new[] { "
                    + string.Join(", ", methodStub.CallingConvention.Select(static convention => $"typeof(global::System.Runtime.CompilerServices.CallConv{convention})"))
                    + " })";
            }

            return (
                new GeneratedComMember(
                    StubMemberKind.Method,
                    methodStub.AbiMethodIdentifier,
                    stubGenerator.GenerateAbiMethodSignatureData(),
                    ImmutableArray.Create(unmanagedCallersOnlyAttribute).ToSequenceEqual(),
                    body,
                    "internal static"),
                methodStub.Diagnostics.Array.AddRange(diagnostics.Diagnostics));
        }

        private static ImmutableArray<TypePositionInfo> AddManagedToUnmanagedImplicitThis(SourceAvailableIncrementalMethodStubGenerationContext methodStub)
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
                TypePositionInfo unmanagedToManagedElement = element;
                if (unmanagedToManagedElement.IsErrorHandlingPosition)
                {
                    unmanagedToManagedElement = unmanagedToManagedElement with
                    {
                        IsErrorHandlingPosition = false,
                    };
                }

                elements.Add(unmanagedToManagedElement with
                {
                    NativeIndex = TypePositionInfo.IncrementIndex(unmanagedToManagedElement.NativeIndex)
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
                        NativeIndex = TypePositionInfo.ReturnIndex,
                        IsErrorHandlingPosition = true,
                    });
            }

            return elements.ToImmutable();
        }

        public static string GenerateUnmanagedFunctionPointerTypeForMethod(
            IncrementalMethodStubGenerationContext method,
            Func<EnvironmentFlags, MarshalDirection, IMarshallingGeneratorResolver> generatorResolverCreator)
        {
            var diagnostics = new GeneratorDiagnosticsBag(new DiagnosticDescriptorProvider(), method.DiagnosticLocation, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.ComInterfaceGenerator.SR));
            var stubGenerator = new UnmanagedToManagedStubGenerator(
                AddUnmanagedToManagedImplicitElementInfos(method),
                diagnostics,
                generatorResolverCreator(method.EnvironmentFlags, MarshalDirection.UnmanagedToManaged));

            return stubGenerator.GenerateAbiMethodSignatureData().GetFunctionPointerType(method.CallingConvention.Array);
        }

        public static ImmutableArray<string> GetCallingConventionsFromAttributes(
            AttributeData? suppressGCTransitionAttribute,
            AttributeData? unmanagedCallConvAttribute,
            ImmutableArray<string> defaultCallingConventions)
        {
            var callingConventions = ImmutableArray.CreateBuilder<string>();
            if (suppressGCTransitionAttribute is not null)
            {
                callingConventions.Add("SuppressGCTransition");
            }

            // UnmanagedCallConvAttribute overrides the default calling convention rules.
            if (unmanagedCallConvAttribute is not null)
            {
                foreach (KeyValuePair<string, TypedConstant> arg in unmanagedCallConvAttribute.NamedArguments)
                {
                    if (arg.Key == "CallConvs")
                    {
                        foreach (TypedConstant callConv in arg.Value.Values)
                        {
                            ITypeSymbol callConvSymbol = (ITypeSymbol)callConv.Value!;
                            if (callConvSymbol.Name.StartsWith("CallConv", StringComparison.Ordinal))
                            {
                                callingConventions.Add(callConvSymbol.Name.Substring("CallConv".Length));
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
