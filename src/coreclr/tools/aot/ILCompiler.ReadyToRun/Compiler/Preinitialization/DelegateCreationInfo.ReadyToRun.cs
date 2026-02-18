// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.ReadyToRun;

using Internal.IL;
using Internal.JitInterface;
using Internal.TypeSystem;

namespace ILCompiler
{
    public sealed class DelegateCreationInfo
    {
        public IMethodNode Constructor { get; }

        public MethodDesc PossiblyUnresolvedTargetMethod { get; }

        private bool TargetMethodIsUnboxingThunk
            => PossiblyUnresolvedTargetMethod.OwningType.IsValueType && !PossiblyUnresolvedTargetMethod.Signature.IsStatic;

        public bool TargetNeedsVTableLookup => false;

        public IMethodNode Thunk { get; }

        public TypeDesc DelegateType { get; }

        private DelegateCreationInfo(TypeDesc delegateType, IMethodNode constructor, MethodDesc targetMethod, IMethodNode thunk)
        {
            DelegateType = delegateType;
            Constructor = constructor;
            PossiblyUnresolvedTargetMethod = targetMethod;
            Thunk = thunk;
        }

        public static DelegateCreationInfo Create(TypeDesc delegateType, MethodDesc targetMethod, TypeDesc constrainedType, NodeFactory factory, bool followVirtualDispatch)
        {
            int paramCountTargetMethod = targetMethod.Signature.Length;
            if (!targetMethod.Signature.IsStatic)
            {
                paramCountTargetMethod++;
            }

            MethodSignature delegateSignature = delegateType.GetTypeDefinition().GetKnownMethod("Invoke"u8, null).Signature;
            int paramCountDelegateClosed = delegateSignature.Length + 1;
            bool closed = paramCountDelegateClosed == paramCountTargetMethod;

            if (targetMethod.Signature.IsStatic)
            {
                if (!closed)
                {
                    throw new NotSupportedException("Open static delegates");
                }
            }
            else
            {
                if (!closed)
                    throw new NotImplementedException("Open instance delegates");
            }

            IMethodNode constructorNode = CreateMethodEntrypoint(factory, targetMethod, constrainedType);
            return new DelegateCreationInfo(delegateType, constructorNode, targetMethod, null);
        }

        public ISymbolNode GetTargetNode(NodeFactory factory)
            => factory.ExactCallableAddressTakenAddress(
                PossiblyUnresolvedTargetMethod,
                TargetMethodIsUnboxingThunk);

        private static IMethodNode CreateMethodEntrypoint(NodeFactory factory, MethodDesc method, TypeDesc constrainedType)
        {
            bool unboxingStub = method.OwningType.IsValueType && !method.Signature.IsStatic;

            ModuleToken methodToken = factory.Resolver.GetModuleTokenForMethod(
                method,
                allowDynamicallyCreatedReference: true,
                throwIfNotFound: true);

            IMethodNode methodNode = factory.MethodEntrypoint(
                new MethodWithToken(method, methodToken, constrainedType, unboxing: unboxingStub, context: null),
                isInstantiatingStub: false,
                isPrecodeImportRequired: false,
                isJumpableImportRequired: false);

            Debug.Assert(methodNode != null);
            return methodNode;
        }
    }
}
