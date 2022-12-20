// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;

using Internal.Runtime;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;

using Internal.NativeFormat;
using Internal.TypeSystem;
using Internal.TypeSystem.NoMetadata;

namespace Internal.Runtime.TypeLoader
{
    //
    // Wrap state required by the native layout info parsing in a structure, so that it can be conveniently passed around.
    //
    internal class NativeLayoutInfoLoadContext
    {
        public TypeSystemContext _typeSystemContext;
        public NativeFormatModuleInfo _module;
        private ExternalReferencesTable _staticInfoLookup;
        private ExternalReferencesTable _externalReferencesLookup;
        public Instantiation _typeArgumentHandles;
        public Instantiation _methodArgumentHandles;

        private TypeDesc GetInstantiationType(ref NativeParser parser, uint arity)
        {
            DefType typeDefinition = (DefType)GetType(ref parser);

            TypeDesc[] typeArguments = new TypeDesc[arity];
            for (uint i = 0; i < arity; i++)
                typeArguments[i] = GetType(ref parser);

            return _typeSystemContext.ResolveGenericInstantiation(typeDefinition, new Instantiation(typeArguments));
        }

        private TypeDesc GetModifierType(ref NativeParser parser, TypeModifierKind modifier)
        {
            TypeDesc typeParameter = GetType(ref parser);

            switch (modifier)
            {
                case TypeModifierKind.Array:
                    return _typeSystemContext.GetArrayType(typeParameter);

                case TypeModifierKind.ByRef:
                    return _typeSystemContext.GetByRefType(typeParameter);

                case TypeModifierKind.Pointer:
                    return _typeSystemContext.GetPointerType(typeParameter);

                default:
                    NativeParser.ThrowBadImageFormatException();
                    return null;
            }
        }

        private void InitializeExternalReferencesLookup()
        {
            if (!_externalReferencesLookup.IsInitialized())
            {
                bool success = _externalReferencesLookup.InitializeNativeReferences(_module);
                Debug.Assert(success);
            }
        }

        private IntPtr GetExternalReferencePointer(uint index)
        {
            InitializeExternalReferencesLookup();
            return _externalReferencesLookup.GetIntPtrFromIndex(index);
        }

        internal TypeDesc GetExternalType(uint index)
        {
            InitializeExternalReferencesLookup();
            RuntimeTypeHandle rtth = _externalReferencesLookup.GetRuntimeTypeHandleFromIndex(index);
            return _typeSystemContext.ResolveRuntimeTypeHandle(rtth);
        }

        internal IntPtr GetGCStaticInfo(uint index)
        {
            if (!_staticInfoLookup.IsInitialized())
            {
                bool success = _staticInfoLookup.InitializeNativeStatics(_module);
                Debug.Assert(success);
            }

            return _staticInfoLookup.GetIntPtrFromIndex(index);
        }

        private unsafe TypeDesc GetLookbackType(ref NativeParser parser, uint lookback)
        {
            var lookbackParser = parser.GetLookbackParser(lookback);
            return GetType(ref lookbackParser);
        }

        internal TypeDesc GetType(ref NativeParser parser)
        {
            uint data;
            var kind = parser.GetTypeSignatureKind(out data);

            switch (kind)
            {
                case TypeSignatureKind.Lookback:
                    return GetLookbackType(ref parser, data);

                case TypeSignatureKind.Variable:
                    uint index = data >> 1;
                    return (((data & 0x1) != 0) ? _methodArgumentHandles : _typeArgumentHandles)[checked((int)index)];

                case TypeSignatureKind.Instantiation:
                    return GetInstantiationType(ref parser, data);

                case TypeSignatureKind.Modifier:
                    return GetModifierType(ref parser, (TypeModifierKind)data);

                case TypeSignatureKind.External:
                    return GetExternalType(data);

                case TypeSignatureKind.MultiDimArray:
                    {
                        DefType elementType = (DefType)GetType(ref parser);
                        int rank = (int)data;

                        // Skip encoded bounds and lobounds
                        uint boundsCount = parser.GetUnsigned();
                        while (boundsCount > 0)
                        {
                            parser.GetUnsigned();
                            boundsCount--;
                        }

                        uint loBoundsCount = parser.GetUnsigned();
                        while (loBoundsCount > 0)
                        {
                            parser.GetUnsigned();
                            loBoundsCount--;
                        }

                        return _typeSystemContext.GetArrayType(elementType, rank);
                    }

                case TypeSignatureKind.BuiltIn:
                    return _typeSystemContext.GetWellKnownType((WellKnownType)data);

                case TypeSignatureKind.FunctionPointer:
                    Debug.Fail("NYI!");
                    NativeParser.ThrowBadImageFormatException();
                    return null;

                default:
                    NativeParser.ThrowBadImageFormatException();
                    return null;
            }
        }

        internal MethodDesc GetMethod(ref NativeParser parser, out RuntimeSignature methodNameSig, out RuntimeSignature methodSig)
        {
            MethodFlags flags = (MethodFlags)parser.GetUnsigned();

            IntPtr functionPointer = IntPtr.Zero;
            if ((flags & MethodFlags.HasFunctionPointer) != 0)
                functionPointer = GetExternalReferencePointer(parser.GetUnsigned());

            DefType containingType = (DefType)GetType(ref parser);
            MethodNameAndSignature nameAndSignature = TypeLoaderEnvironment.GetMethodNameAndSignature(ref parser, _module.Handle, out methodNameSig, out methodSig);

            bool unboxingStub = (flags & MethodFlags.IsUnboxingStub) != 0;

            MethodDesc retVal;
            if ((flags & MethodFlags.HasInstantiation) != 0)
            {
                TypeDesc[] typeArguments = GetTypeSequence(ref parser);
                Debug.Assert(typeArguments.Length > 0);
                retVal = this._typeSystemContext.ResolveGenericMethodInstantiation(unboxingStub, containingType, nameAndSignature, new Instantiation(typeArguments), functionPointer, (flags & MethodFlags.FunctionPointerIsUSG) != 0);
            }
            else
            {
                retVal = this._typeSystemContext.ResolveRuntimeMethod(unboxingStub, containingType, nameAndSignature, functionPointer, (flags & MethodFlags.FunctionPointerIsUSG) != 0);
            }

            if ((flags & MethodFlags.FunctionPointerIsUSG) != 0)
            {
                // TODO, consider a change such that if a USG function pointer is passed in, but we have
                // a way to get a non-usg pointer, that may be preferable
                Debug.Assert(retVal.UsgFunctionPointer != IntPtr.Zero);
            }

            return retVal;
        }

        internal MethodDesc GetMethod(ref NativeParser parser)
        {
            return GetMethod(ref parser, out _, out _);
        }

        internal TypeDesc[] GetTypeSequence(ref NativeParser parser)
        {
            uint count = parser.GetSequenceCount();

            TypeDesc[] sequence = new TypeDesc[count];

            for (uint i = 0; i < count; i++)
            {
                sequence[i] = GetType(ref parser);
            }

            return sequence;
        }
    }
}
