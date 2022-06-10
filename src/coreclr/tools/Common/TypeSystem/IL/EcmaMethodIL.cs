// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace Internal.IL
{
    public sealed partial class EcmaMethodIL : MethodIL
    {
        private readonly EcmaModule _module;
        private readonly EcmaMethod _method;
        private readonly MethodBodyBlock _methodBody;

        // Cached values
        private byte[] _ilBytes;
        private LocalVariableDefinition[] _locals;
        private ILExceptionRegion[] _ilExceptionRegions;

        public static EcmaMethodIL Create(EcmaMethod method)
        {
            var rva = method.MetadataReader.GetMethodDefinition(method.Handle).RelativeVirtualAddress;
            if (rva == 0)
                return null;
            return new EcmaMethodIL(method, rva);
        }

        private EcmaMethodIL(EcmaMethod method, int rva)
        {
            _method = method;
            _module = method.Module;
            _methodBody = _module.PEReader.GetMethodBody(rva);
        }

        public EcmaModule Module
        {
            get
            {
                return _module;
            }
        }

        public override MethodDesc OwningMethod
        {
            get
            {
                return _method;
            }
        }

        public override byte[] GetILBytes()
        {
            if (_ilBytes != null)
                return _ilBytes;

            Interlocked.CompareExchange(ref _ilBytes, _methodBody.GetILBytes(), null);
            return _ilBytes;
        }

        public override bool IsInitLocals
        {
            get
            {
                return _methodBody.LocalVariablesInitialized;
            }
        }

        public override int MaxStack
        {
            get
            {
                return _methodBody.MaxStack;
            }
        }

        public override LocalVariableDefinition[] GetLocals()
        {
            if (_locals != null)
                return _locals;

            var metadataReader = _module.MetadataReader;
            var localSignature = _methodBody.LocalSignature;
            if (localSignature.IsNil)
                return Array.Empty<LocalVariableDefinition>();
            BlobReader signatureReader = metadataReader.GetBlobReader(metadataReader.GetStandaloneSignature(localSignature).Signature);

            EcmaSignatureParser parser = new EcmaSignatureParser(_module, signatureReader, NotFoundBehavior.Throw);
            LocalVariableDefinition[] locals = parser.ParseLocalsSignature();

            Interlocked.CompareExchange(ref _locals, locals, null);
            return _locals;
        }

        public override ILExceptionRegion[] GetExceptionRegions()
        {
            if (_ilExceptionRegions != null)
                return _ilExceptionRegions;

            ImmutableArray<ExceptionRegion> exceptionRegions = _methodBody.ExceptionRegions;
            ILExceptionRegion[] ilExceptionRegions;

            int length = exceptionRegions.Length;
            if (length == 0)
            {
                ilExceptionRegions = Array.Empty<ILExceptionRegion>();
            }
            else
            {
                ilExceptionRegions = new ILExceptionRegion[length];
                for (int i = 0; i < length; i++)
                {
                    var exceptionRegion = exceptionRegions[i];

                    ilExceptionRegions[i] = new ILExceptionRegion(
                        (ILExceptionRegionKind)exceptionRegion.Kind, // assumes that ILExceptionRegionKind and ExceptionRegionKind enums are in sync
                        exceptionRegion.TryOffset,
                        exceptionRegion.TryLength,
                        exceptionRegion.HandlerOffset,
                        exceptionRegion.HandlerLength,
                        MetadataTokens.GetToken(exceptionRegion.CatchType),
                        exceptionRegion.FilterOffset);
                }
            }

            Interlocked.CompareExchange(ref _ilExceptionRegions, ilExceptionRegions, null);
            return _ilExceptionRegions;
        }

        public override object GetObject(int token, NotFoundBehavior notFoundBehavior = NotFoundBehavior.Throw)
        {
            // UserStrings cannot be wrapped in EntityHandle
            if ((token & 0xFF000000) == 0x70000000)
                return _module.GetUserString(MetadataTokens.UserStringHandle(token));

            return _module.GetObject(MetadataTokens.EntityHandle(token), notFoundBehavior);
        }
    }

    public sealed partial class EcmaMethodILScope : MethodILScope
    {
        private readonly EcmaModule _module;
        private readonly EcmaMethod _method;

        public static EcmaMethodILScope Create(EcmaMethod method)
        {
            return new EcmaMethodILScope(method);
        }

        private EcmaMethodILScope(EcmaMethod method)
        {
            _method = method;
            _module = method.Module;
        }

        public EcmaModule Module
        {
            get
            {
                return _module;
            }
        }

        public override MethodDesc OwningMethod
        {
            get
            {
                return _method;
            }
        }

        public override object GetObject(int token, NotFoundBehavior notFoundBehavior = NotFoundBehavior.Throw)
        {
            // UserStrings cannot be wrapped in EntityHandle
            if ((token & 0xFF000000) == 0x70000000)
                return _module.GetUserString(MetadataTokens.UserStringHandle(token));

            return _module.GetObject(MetadataTokens.EntityHandle(token), notFoundBehavior);
        }
    }
}
