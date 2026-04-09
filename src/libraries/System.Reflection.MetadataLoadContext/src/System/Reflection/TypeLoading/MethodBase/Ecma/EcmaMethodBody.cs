// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection.Metadata;

namespace System.Reflection.TypeLoading.Ecma
{
    internal sealed class EcmaMethodBody : RoMethodBody
    {
        private readonly IRoMethodBase _roMethodBase;

        internal EcmaMethodBody(IRoMethodBase roMethodBase, MethodBodyBlock methodBodyBlock)
            : base()
        {
            _roMethodBase = roMethodBase;
            _neverAccessThisExceptThroughBlockProperty = methodBodyBlock;
        }

        public sealed override bool InitLocals => Block.LocalVariablesInitialized;
        public sealed override int MaxStackSize => Block.MaxStack;
        public sealed override int LocalSignatureMetadataToken => Block.LocalSignature.GetToken();

        protected sealed override byte[]? ComputeIL() => Block.GetILBytes();

        public sealed override IList<LocalVariableInfo> LocalVariables
        {
            get
            {
                MetadataReader reader = Reader;
                EcmaPinnedTypeProvider typeProvider = new EcmaPinnedTypeProvider(GetEcmaModule());
                StandaloneSignatureHandle sigHandle = Block.LocalSignature;
                if (sigHandle.IsNil)
                    return Array.Empty<LocalVariableInfo>();

                ImmutableArray<RoType> sig = sigHandle.GetStandaloneSignature(reader).DecodeLocalSignature(typeProvider, TypeContext);
                int count = sig.Length;
                LocalVariableInfo[] lvis = count != 0 ? new LocalVariableInfo[count] : Array.Empty<LocalVariableInfo>();
                for (int i = 0; i < count; i++)
                {
                    bool isPinned = false;
                    RoType localType = sig[i];
                    if (localType is RoPinnedType)
                    {
                        isPinned = true;
                        localType = localType.SkipTypeWrappers();
                    }

                    lvis[i] = new RoLocalVariableInfo(localIndex: i, isPinned: isPinned, localType: localType);
                }
                return Array.AsReadOnly(lvis);
            }
        }

        public sealed override IList<ExceptionHandlingClause> ExceptionHandlingClauses
        {
            get
            {
                ImmutableArray<ExceptionRegion> regions = Block.ExceptionRegions;
                int count = regions.Length;
                ExceptionHandlingClause[] clauses = count != 0 ? new ExceptionHandlingClause[count] : Array.Empty<ExceptionHandlingClause>();
                for (int i = 0; i < count; i++)
                {
                    EntityHandle catchTypeHandle = regions[i].CatchType;
                    RoType? catchType = catchTypeHandle.IsNil ? null : catchTypeHandle.ResolveTypeDefRefOrSpec(GetEcmaModule(), TypeContext);
                    clauses[i] = new RoExceptionHandlingClause(
                        catchType: catchType,
                        flags: regions[i].Kind.ToExceptionHandlingClauseOptions(),
                        filterOffset: regions[i].FilterOffset,
                        tryOffset: regions[i].TryOffset,
                        tryLength: regions[i].TryLength,
                        handlerOffset: regions[i].HandlerOffset,
                        handlerLength: regions[i].HandlerLength
                    );
                }
                return new ReadOnlyCollection<ExceptionHandlingClause>(clauses);
            }
        }

        private TypeContext TypeContext => _roMethodBase.TypeContext;

        private EcmaModule GetEcmaModule() => (EcmaModule)(_roMethodBase.MethodBase.Module);
        private MetadataReader Reader => GetEcmaModule().Reader;
        private MetadataLoadContext Loader => GetEcmaModule().Loader;

        private ref readonly MethodBodyBlock Block { get { Loader.DisposeCheck(); return ref _neverAccessThisExceptThroughBlockProperty; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] // Block from debugger watch windows so they don't AV the debugged process.
        private readonly MethodBodyBlock _neverAccessThisExceptThroughBlockProperty;
    }
}
