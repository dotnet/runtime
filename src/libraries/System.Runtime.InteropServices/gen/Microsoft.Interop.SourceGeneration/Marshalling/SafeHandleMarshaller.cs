﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    public sealed class SafeHandleMarshaller : IMarshallingGenerator
    {
        public bool IsSupported(TargetFramework target, Version version)
        {
            return target is TargetFramework.Net && version.Major >= 6;
        }

        public ManagedTypeInfo AsNativeType(TypePositionInfo info)
        {
            return SpecialTypeInfo.IntPtr;
        }

        public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info)
        {
            return info.IsByRef ? SignatureBehavior.PointerToNativeType : SignatureBehavior.NativeType;
        }

        public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context)
        {
            return info.IsByRef ? ValueBoundaryBehavior.AddressOfNativeIdentifier : ValueBoundaryBehavior.NativeIdentifier;
        }

        public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            // The high level logic (note that the parameter may be in, out or both):
            // 1) If this is an input parameter we need to AddRef the SafeHandle.
            // 2) If this is an output parameter we need to preallocate a SafeHandle to wrap the new native handle value. We
            //    must allocate this before the native call to avoid a failure point when we already have a native resource
            //    allocated. We must allocate a new SafeHandle even if we have one on input since both input and output native
            //    handles need to be tracked and released by a SafeHandle.
            // 3) Initialize a local IntPtr that will be passed to the native call. If we have an input SafeHandle the value
            //    comes from there otherwise we get it from the new SafeHandle (which is guaranteed to be initialized to an
            //    invalid handle value).
            // 4) If this is a out parameter we also store the original handle value (that we just computed above) in a local
            //    variable.
            // 5) If we successfully AddRef'd the incoming SafeHandle, we need to Release it before we return.
            // 6) After the native call, if this is an output parameter and the handle value we passed to native differs from
            //    the local copy we made then the new handle value is written into the output SafeHandle and that SafeHandle
            //    is propagated back to the caller.

            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            string addRefdIdentifier = $"{managedIdentifier}__addRefd";
            string newHandleObjectIdentifier = info.IsManagedReturnPosition
                ? managedIdentifier
                : $"{managedIdentifier}__newHandle";
            string handleValueBackupIdentifier = $"{nativeIdentifier}__original";
            switch (context.CurrentStage)
            {
                case StubCodeContext.Stage.Setup:
                    if (!info.IsManagedReturnPosition && info.RefKind != RefKind.Out)
                    {
                        yield return LocalDeclarationStatement(
                                                VariableDeclaration(
                                                    PredefinedType(Token(SyntaxKind.BoolKeyword)),
                                                    SingletonSeparatedList(
                                                        VariableDeclarator(addRefdIdentifier)
                                                        .WithInitializer(EqualsValueClause(LiteralExpression(SyntaxKind.FalseLiteralExpression))))));
                    }

                    ExpressionSyntax safeHandleCreationExpression = ((SafeHandleMarshallingInfo)info.MarshallingAttributeInfo).AccessibleDefaultConstructor
                        ? ObjectCreationExpression(info.ManagedType.Syntax, ArgumentList(), initializer: null)
                        : CastExpression(
                            info.ManagedType.Syntax,
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    TypeSyntaxes.System_Activator,
                                    IdentifierName("CreateInstance")))
                            .WithArgumentList(
                                ArgumentList(
                                    SeparatedList(
                                        new[]{
                                            Argument(
                                                TypeOfExpression(
                                                    info.ManagedType.Syntax)),
                                            Argument(
                                                LiteralExpression(
                                                    SyntaxKind.TrueLiteralExpression))
                                            .WithNameColon(
                                                NameColon(
                                                    IdentifierName("nonPublic")))
                                        }))));

                    if (info.IsManagedReturnPosition)
                    {
                        yield return ExpressionStatement(
                            AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(managedIdentifier),
                                safeHandleCreationExpression
                                ));
                    }
                    else if (info.IsByRef && info.RefKind != RefKind.In)
                    {
                        // We create the new handle in the Setup phase
                        // so we eliminate the possible failure points during unmarshalling, where we would
                        // leak the handle if we failed to create the handle.
                        yield return LocalDeclarationStatement(
                            VariableDeclaration(
                                info.ManagedType.Syntax,
                                SingletonSeparatedList(
                                    VariableDeclarator(newHandleObjectIdentifier)
                                    .WithInitializer(EqualsValueClause(safeHandleCreationExpression)))));
                        if (info.RefKind != RefKind.Out)
                        {
                            yield return LocalDeclarationStatement(
                                VariableDeclaration(
                                    AsNativeType(info).Syntax,
                                    SingletonSeparatedList(
                                        VariableDeclarator(handleValueBackupIdentifier)
                                        .WithInitializer(EqualsValueClause(
                                            InvocationExpression(
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName(newHandleObjectIdentifier),
                                                    IdentifierName(nameof(SafeHandle.DangerousGetHandle))),
                                                ArgumentList()))))));
                        }
                    }
                    break;
                case StubCodeContext.Stage.Marshal:
                    if (!info.IsManagedReturnPosition && info.RefKind != RefKind.Out)
                    {
                        // <managedIdentifier>.DangerousAddRef(ref <addRefdIdentifier>);
                        yield return ExpressionStatement(
                            InvocationExpression(
                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName(managedIdentifier),
                                    IdentifierName(nameof(SafeHandle.DangerousAddRef))),
                                ArgumentList(SingletonSeparatedList(
                                    Argument(IdentifierName(addRefdIdentifier))
                                        .WithRefKindKeyword(Token(SyntaxKind.RefKeyword))))));


                        ExpressionSyntax assignHandleToNativeExpression =
                            AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(nativeIdentifier),
                                InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(managedIdentifier),
                                        IdentifierName(nameof(SafeHandle.DangerousGetHandle))),
                                    ArgumentList()));
                        if (info.IsByRef && info.RefKind != RefKind.In)
                        {
                            yield return ExpressionStatement(
                                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                    IdentifierName(handleValueBackupIdentifier),
                                    assignHandleToNativeExpression));
                        }
                        else
                        {
                            yield return ExpressionStatement(assignHandleToNativeExpression);
                        }
                    }
                    break;
                case StubCodeContext.Stage.GuaranteedUnmarshal:
                    StatementSyntax unmarshalStatement = ExpressionStatement(
                        InvocationExpression(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                TypeSyntaxes.System_Runtime_InteropServices_Marshal,
                                IdentifierName("InitHandle")),
                            ArgumentList(SeparatedList(
                                new[]
                                {
                                    Argument(IdentifierName(newHandleObjectIdentifier)),
                                    Argument(IdentifierName(nativeIdentifier))
                                }))));

                    if (info.IsManagedReturnPosition)
                    {
                        yield return unmarshalStatement;
                    }
                    else if (info.RefKind == RefKind.Out)
                    {
                        yield return unmarshalStatement;
                        yield return ExpressionStatement(
                            AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(managedIdentifier),
                                IdentifierName(newHandleObjectIdentifier)));
                    }
                    else if (info.RefKind == RefKind.Ref)
                    {
                        // Decrement refcount on original SafeHandle if we addrefd
                        yield return IfStatement(
                            IdentifierName(addRefdIdentifier),
                            ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName(managedIdentifier),
                                        IdentifierName(nameof(SafeHandle.DangerousRelease))),
                                    ArgumentList())));

                        // Do not unmarshal the handle if the value didn't change.
                        yield return IfStatement(
                            BinaryExpression(SyntaxKind.NotEqualsExpression,
                                IdentifierName(handleValueBackupIdentifier),
                                IdentifierName(nativeIdentifier)),
                            Block(
                                unmarshalStatement,
                                ExpressionStatement(
                                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                        IdentifierName(managedIdentifier),
                                        IdentifierName(newHandleObjectIdentifier)))));
                    }
                    break;
                case StubCodeContext.Stage.CleanupCallerAllocated:
                    if (!info.IsManagedReturnPosition && (!info.IsByRef || info.RefKind == RefKind.In))
                    {
                        yield return IfStatement(
                            IdentifierName(addRefdIdentifier),
                            ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName(managedIdentifier),
                                    IdentifierName(nameof(SafeHandle.DangerousRelease))),
                                    ArgumentList())));
                    }
                    break;
                default:
                    break;
            }
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;

        public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, StubCodeContext context, out GeneratorDiagnostic? diagnostic)
            => ByValueMarshalKindSupportDescriptor.Default.GetSupport(marshalKind, info, context, out diagnostic);
    }
}
