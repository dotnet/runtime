// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.CorConstants;

using ILCompiler.Win32Resources;

namespace ILCompiler.IBC
{
    internal class IBCProfileParser
    {
        private readonly List<ModuleDesc> _possibleReferenceModules;

        public IBCProfileParser(Logger logger, IEnumerable<ModuleDesc> possibleReferenceModules)
        {
            _logger = logger;
            _possibleReferenceModules = new List<ModuleDesc>(possibleReferenceModules);
        }

        private readonly Logger _logger;

        public ProfileData ParseIBCDataFromModule(EcmaModule ecmaModule)
        {
            ResourceData peResources = new ResourceData(ecmaModule);
            byte[] ibcDataSection = peResources.FindResource("PROFILE_DATA", "IBC", 0);
            if (ibcDataSection == null)
            {
                // If we don't have profile data, return empty ProfileData object
                return EmptyProfileData.Singleton;
            }

            var reader = new IBCDataReader();
            int pos = 0;
            bool basicBlocksOnly = false;
            AssemblyData parsedData = reader.Read(ibcDataSection, ref pos, out bool minified);
            if (parsedData.FormatMajorVersion == 1)
                throw new Exception("Unsupported V1 IBC Format");

            Dictionary<IBCBlobKey, BlobEntry> blobs = GetIBCBlobs(parsedData.BlobStream, out HashSet<uint> ignoredIbcMethodSpecTokens);

            List<MethodProfileData> methodProfileData = new List<MethodProfileData>();

            IBCModule ibcModule = new IBCModule(ecmaModule, blobs);
            // Parse the token lists
            IBCData.SectionIteratorKind iteratorKind = basicBlocksOnly ? IBCData.SectionIteratorKind.BasicBlocks : IBCData.SectionIteratorKind.TokenFlags;
            foreach (SectionFormat section in IBCData.SectionIterator(iteratorKind))
            {
                if (!parsedData.Tokens.TryGetValue(section, out List<TokenData> TokenList) ||
                    TokenList.Count == 0)
                {
                    continue;
                }

                // In V1 and minified V3+ files, tokens aren't stored with a
                // scenario mask. In minified V3+ files, it can be treated as
                // anything nonzero--minified files make no guarantee about
                // preserving scenario information, but the flags must be left
                // alone.

                const uint scenarioMaskIfMissing = 1u;
                HashSet<MethodDesc> methodsFoundInData = new HashSet<MethodDesc>();

                foreach (TokenData entry in TokenList)
                {
                    //
                    // Discard any token list entries which refer to the ParamMethodSpec blob stream entries
                    // (if any) which were thrown away above.  Note that the MethodProfilingData token list is
                    // the only place anywhere in the IBC data which can ever contain an embedded ibcMethodSpec
                    // token.
                    //

                    if (section == SectionFormat.MethodProfilingData)
                    {
                        if (ignoredIbcMethodSpecTokens.Contains(entry.Token))
                            continue;
                    }

                    uint scenarioMask = entry.ScenarioMask ?? scenarioMaskIfMissing;

                    // scenarioMask will be 0 in unprocessed or V1 IBC data.
                    if (scenarioMask == 0)
                    {
                        // TODO Compute RunOnce and RunNever from basic block data
                        scenarioMask = scenarioMaskIfMissing;
                        /*                        Debug.Assert(fullScenarioMask == 1, "Token entry not owned by one scenario");
                                                // We have to compute the RunOnceMethod and RunNeverMethod flags.
                                                entry.Flags = result.GetFlags(entry.Flags, section, entry.Token);
                                                scenarioMask = defaultScenarioMask;*/
                    }

                    //                    Debug.Assert(((~fullScenarioMask & scenarioMask) == 0), "Illegal scenarios mask");

                    MethodDesc associatedMethod = null;

                    switch (Cor.Macros.TypeFromToken(entry.Token))
                    {
                        case CorTokenType.mdtMethodDef:
                        case CorTokenType.mdtMemberRef:
                        case CorTokenType.mdtMethodSpec:
                            object metadataObject = ecmaModule.GetObject(System.Reflection.Metadata.Ecma335.MetadataTokens.EntityHandle((int)entry.Token), NotFoundBehavior.ReturnNull);
                            if (metadataObject is MethodDesc)
                            {
                                associatedMethod = (MethodDesc)metadataObject;
                            }
                            else
                            {
                                if (_logger.IsVerbose)
                                    _logger.Writer.WriteLine($"Token {(int)entry.Token:x} does not refer to a method");
                            }
                            break;

                        case CorTokenType.ibcMethodSpec:
                            {
                                if (!blobs.TryGetValue(new IBCBlobKey(entry.Token, BlobType.ParamMethodSpec), out BlobEntry blobEntry))
                                    throw new Exception($"Missing blob entry for ibcMethodSpec {entry.Token:x}");
                                BlobEntry.SignatureEntry paramSignatureEntry = blobEntry as BlobEntry.SignatureEntry;
                                if (paramSignatureEntry == null)
                                    throw new Exception($"Blob entry for {entry.Token:x} is invalid");
                                unsafe
                                {
                                    fixed (byte* pb = &paramSignatureEntry.Signature[0])
                                    {
                                        BlobReader br = new BlobReader(pb, paramSignatureEntry.Signature.Length);
                                        try
                                        {
                                            associatedMethod = GetSigMethodInstantiationFromIBCMethodSpec(ibcModule, br);
                                        }
                                        catch
                                        {
                                            associatedMethod = null;
                                        }
                                    }
                                }
                            }
                            break;
                    }

                    if (associatedMethod != null)
                    {
                        if (methodsFoundInData.Add(associatedMethod))
                        {
                            methodProfileData.Add(new MethodProfileData(associatedMethod, (MethodProfilingDataFlags)entry.Flags, 0, null, scenarioMask, null));
                        }
                        else
                        {
                            if (_logger.IsVerbose)
                                _logger.Writer.WriteLine($"Multiple copies of data for method '{associatedMethod}' found.");
                        }
                    }
                }
            }

            return new IBCProfileData(null, parsedData.PartialNGen, methodProfileData);
        }

        public struct IBCBlobKey : IEquatable<IBCBlobKey>
        {
            public IBCBlobKey(uint token, BlobType type)
            {
                Token = token;
                Type = type;
            }

            public readonly uint Token;
            public readonly BlobType Type;
            public override int GetHashCode()
            {
                return (int)(Token ^ (((uint)Type) << 4));
            }
            public override bool Equals(object obj)
            {
                if (!(obj is IBCBlobKey))
                    return false;
                return Equals((IBCBlobKey)obj);
            }

            public bool Equals(IBCBlobKey other)
            {
                return (other.Token == Token) && (other.Type == Type);
            }
        }

        private static Dictionary<IBCBlobKey, BlobEntry> GetIBCBlobs(List<BlobEntry> inputBlobs, out HashSet<uint> ignoredIbcMethodSpecTokens)
        {
            Dictionary<IBCBlobKey, BlobEntry> blobs = new Dictionary<IBCBlobKey, BlobEntry>();
            ignoredIbcMethodSpecTokens = new HashSet<uint>();

            if (inputBlobs != null)
            {
                foreach (BlobEntry blob in inputBlobs)
                {
                    bool ignore = false;

                    // Some blob types require special processing
                    switch (blob.Type)
                    {
                        case BlobType.ParamTypeSpec:
                        case BlobType.ParamMethodSpec:
                            if ((Cor.Macros.TypeFromToken(blob.Token) == CorTokenType.ibcTypeSpec) ||
                                (Cor.Macros.TypeFromToken(blob.Token) == CorTokenType.ibcMethodSpec))
                            {
                                if (blob.Type == BlobType.ParamMethodSpec)
                                {
                                    //
                                    // In the ParamMethodSpec case, the full signature was always originally encoded by
                                    // ZapSig::EncodeMethod and therefore always starts with an owning type signature
                                    // originally encoded by ZapSig::GetSignatureForTypeHandle.
                                    //
                                    // If the owning type is one of the well-known primitive types, then the owning type
                                    // signature ends up being written in "flattened" form (e.g., if the owning type is
                                    // System.String, a single ELEMENT_TYPE_STRING byte is written as the owning type
                                    // signature).  This only happens when the ParamMethodSpec describes a generic method
                                    // defined directly on one of the primitive types.  Since .NET 4.0, mscorlib has included
                                    // exactly two such methods (System.String::Join<T> and System.String::Concat<T>), and
                                    // contained no such methods prior to .NET 4.0.
                                    //
                                    // In all other cases, ZapSig::GetSignatureForTypeHandle writes the owning type signature
                                    // in a "standard" form which always starts with bytes that match the following grammar:
                                    //
                                    //      [ELEMENT_TYPE_MODULE_ZAPSIG {Index}] [ELEMENT_TYPE_GENERICINST] ELEMENT_TYPE_(CLASS|VALUETYPE) {Token}
                                    //
                                    // IBCMerge only supports the standard form.  Specifically, if the flattened form is
                                    // ever processed, updateParamSig and remapParamSig are unable to reliably determine
                                    // whether the subsequent method token should be interpreted as a MethodDef or as an
                                    // ibcMethodSpec (because the flattened form "hides" the explicit ELEMENT_TYPE_MODULE_ZAPSIG
                                    // byte that is normally used to determine how to interpret the method token).
                                    //
                                    // Probe the leading bytes of the signature and ignore this ParamMethodSpec if the leading
                                    // bytes prove that the owning type signature was NOT encoded in the standard form.
                                    //

                                    byte[] signature = ((BlobEntry.SignatureEntry)blob).Signature;
                                    bool owningTypeSignatureAppearsToBeEncodedInStandardForm = false;

                                    if (signature.Length >= 2)
                                    {
                                        CorElementType leadingByte = (CorElementType)signature[0];

                                        if ((leadingByte == CorElementType.ELEMENT_TYPE_MODULE_ZAPSIG) ||
                                            (leadingByte == CorElementType.ELEMENT_TYPE_GENERICINST) ||
                                            (leadingByte == CorElementType.ELEMENT_TYPE_CLASS) ||
                                            (leadingByte == CorElementType.ELEMENT_TYPE_VALUETYPE))
                                        {
                                            owningTypeSignatureAppearsToBeEncodedInStandardForm = true;
                                        }
                                    }

                                    if (!owningTypeSignatureAppearsToBeEncodedInStandardForm)
                                    {
                                        ignoredIbcMethodSpecTokens.Add(blob.Token);
                                        ignore = true;
                                    }
                                }
                            }
                            else
                            {
                                // We have old V2 IBC data (ignore it)
                                ignore = true;
                            }
                            break;

                        case BlobType.ExternalNamespaceDef:
                        case BlobType.ExternalTypeDef:
                        case BlobType.ExternalSignatureDef:
                        case BlobType.ExternalMethodDef:
                            break;

                        case BlobType.MetadataStringPool:
                        case BlobType.MetadataGuidPool:
                        case BlobType.MetadataBlobPool:
                        case BlobType.MetadataUserStringPool:
                            // These blob types should not be carried forward.
                            ignore = true;
                            break;
                    }

                    if (!ignore)
                    {
                        blobs.Add(new IBCBlobKey(blob.Token, blob.Type), blob);
                    }
                }
            }
            return blobs;
        }

        // LookupIbcTypeToken and (possibly) find the module associated with it.
        // externalModule may be null if the exact assembly isn't known
        private uint LookupIbcTypeToken(ref EcmaModule externalModule, uint ibcToken, Dictionary<IBCBlobKey, BlobEntry> blobs)
        {
            if (!blobs.TryGetValue(new IBCBlobKey(ibcToken, BlobType.ExternalTypeDef), out BlobEntry externalTypeDefBlob))
            {
                if (_logger.IsVerbose)
                    _logger.Writer.WriteLine($"Ibc TypeToken {ibcToken:x} unable to find external typedef");
                return Cor.Macros.RidToToken(0, CorTokenType.mdtTypeDef); // Nil TypeDef token
            }

            var typeEntry = (BlobEntry.ExternalTypeEntry)externalTypeDefBlob;

            string typeNamespace = "";
            string typeName = Encoding.UTF8.GetString(typeEntry.Name, 0, typeEntry.Name.Length - 1 /* these strings are null terminated */);
            TypeDefinitionHandle enclosingType = default;
            if (!Cor.Macros.IsNilToken(typeEntry.NamespaceToken))
            {
                if (!Cor.Macros.IsNilToken(typeEntry.NestedClassToken))
                {
                    // Do not support typedef with namespace that is nested
                    throw new Exception($"Ibc TypeToken {ibcToken:x} has both Namespace and NestedClass tokens");
                }

                uint nameSpaceToken = typeEntry.NamespaceToken;
                if (Cor.Macros.TypeFromToken(nameSpaceToken) != CorTokenType.ibcExternalNamespace)
                    throw new Exception($"Ibc TypeToken {ibcToken:x} has Namespace tokens that is not a ibcExternalNamespace");


                if (!blobs.TryGetValue(new IBCBlobKey(nameSpaceToken, BlobType.ExternalNamespaceDef), out BlobEntry namespaceEntryBlob))
                {
                    if (_logger.IsVerbose)
                        _logger.Writer.WriteLine($"Ibc TypeToken {ibcToken:x} unable to find external namespace blob '{nameSpaceToken:x}");
                    return Cor.Macros.RidToToken(0, CorTokenType.mdtTypeDef); // Nil TypeDef token
                }

                var namespaceEntry = (BlobEntry.ExternalNamespaceEntry)namespaceEntryBlob;
                typeNamespace = Encoding.UTF8.GetString(namespaceEntry.Name, 0, namespaceEntry.Name.Length - 1 /* these strings are null terminated */);
            }
            else if (!Cor.Macros.IsNilToken(typeEntry.NestedClassToken))
            {
                uint enclosingTypeTokenValue = LookupIbcTypeToken(ref externalModule, typeEntry.NestedClassToken, blobs);
                if (Cor.Macros.TypeFromToken(enclosingTypeTokenValue) != CorTokenType.mdtTypeDef)
                    throw new Exception($"Ibc TypeToken {ibcToken:x} has NestedClass token which does not resolve to a type definition");

                enclosingType = MetadataTokens.TypeDefinitionHandle((int)Cor.Macros.RidFromToken(enclosingTypeTokenValue));
                if (enclosingType.IsNil && _logger.IsVerbose)
                    _logger.Writer.WriteLine($"Ibc TypeToken {ibcToken:x} has NestedClass token which resolves to a nil token");
            }

            if (enclosingType.IsNil)
            {
                EcmaType foundType = null;
                if (externalModule == null)
                {
                    // Lookup actual module scenario.
                    foreach (ModuleDesc m in _possibleReferenceModules)
                    {
                        if (!(m is EcmaModule))
                            continue;

                        foundType = (EcmaType)m.GetType(typeNamespace, typeName, NotFoundBehavior.ReturnNull);
                        if (foundType != null)
                        {
                            externalModule = foundType.EcmaModule;
                            break;
                        }
                    }
                }
                else
                {
                    foundType = (EcmaType)externalModule.GetType(typeNamespace, typeName, NotFoundBehavior.ReturnNull);
                }

                if (foundType == null)
                {
                    if (_logger.IsVerbose)
                        _logger.Writer.WriteLine($"Ibc TypeToken {ibcToken:x} has type token which resolves to a nil token");
                    return Cor.Macros.RidToToken(0, CorTokenType.mdtTypeDef); // Nil TypeDef token
                }

                return (uint)externalModule.MetadataReader.GetToken(foundType.Handle);
            }
            else
            {
                TypeDefinition nestedClassDefinition = externalModule.MetadataReader.GetTypeDefinition(enclosingType);
                MetadataStringComparer stringComparer = externalModule.MetadataReader.StringComparer;
                foreach (TypeDefinitionHandle tdNested in nestedClassDefinition.GetNestedTypes())
                {
                    TypeDefinition candidateClassDefinition = externalModule.MetadataReader.GetTypeDefinition(tdNested);
                    if (stringComparer.Equals(candidateClassDefinition.Name, typeName))
                    {
                        return (uint)externalModule.MetadataReader.GetToken(tdNested);
                    }
                }

                if (_logger.IsVerbose)
                    _logger.Writer.WriteLine($"Ibc TypeToken {ibcToken:x} unable to find nested type '{typeName}' on type '{externalModule.MetadataReader.GetToken(enclosingType):x}'");
                return Cor.Macros.RidToToken(0, CorTokenType.mdtTypeDef); // Nil TypeDef token
            }
        }

        private uint LookupIbcMethodToken(MetadataType methodMetadataType, uint ibcToken, Dictionary<IBCBlobKey, BlobEntry> blobs)
        {
            var methodEntry = (BlobEntry.ExternalMethodEntry)blobs[new IBCBlobKey(ibcToken, BlobType.ExternalMethodDef)];
            var signatureEntry = (BlobEntry.ExternalSignatureEntry)blobs[new IBCBlobKey(methodEntry.SignatureToken, BlobType.ExternalSignatureDef)];

            string methodName = Encoding.UTF8.GetString(methodEntry.Name);


            var ecmaType = (EcmaType)methodMetadataType.GetTypeDefinition();

            EcmaModule ecmaModule = ecmaType.EcmaModule;
            var lookupClassTokenTypeDef = (int)LookupIbcTypeToken(ref ecmaModule, methodEntry.ClassToken, blobs);
            if (lookupClassTokenTypeDef != ecmaType.MetadataReader.GetToken(ecmaType.Handle))
                throw new Exception($"Ibc MethodToken {ibcToken:x} incosistent classToken '{ibcToken:x}' with specified exact type '{ecmaType}'");

            foreach (MethodDesc method in ecmaType.GetMethods())
            {
                if (method.Name == methodName)
                {
                    EcmaMethod ecmaCandidateMethod = method as EcmaMethod;
                    if (ecmaCandidateMethod == null)
                        continue;

                    MetadataReader metadataReader = ecmaCandidateMethod.MetadataReader;
                    BlobReader signatureReader = metadataReader.GetBlobReader(metadataReader.GetMethodDefinition(ecmaCandidateMethod.Handle).Signature);

                    // Compare for equality
                    if (signatureReader.RemainingBytes != signatureEntry.Signature.Length)
                        continue;
                    for (int i = 0; i < signatureEntry.Signature.Length; i++)
                    {
                        if (signatureReader.ReadByte() != signatureEntry.Signature[i])
                            continue;
                    }

                    // TODO, consider implementing the fuzzy matching that CrossGen implements
                    // It will be necessary for handling version tolerant ibc parsing of generics

                    // Exact match
                    return (uint)MetadataTokens.GetToken(ecmaCandidateMethod.Handle);
                }
            }

            if (_logger.IsVerbose)
                _logger.Writer.WriteLine("Warning: Unable to find exact match for candidate external method");
            return 0;
        }


        private class IBCModule
        {
            public IBCModule(EcmaModule ecmaModule, Dictionary<IBCBlobKey, BlobEntry> blobs)
            {
                EcmaModule = ecmaModule;
                Blobs = blobs;
            }

            public readonly EcmaModule EcmaModule;
            public readonly Dictionary<IBCBlobKey, BlobEntry> Blobs;

            public EcmaModule GetModuleFromIndex(int index)
            {
                if (EcmaModule.MetadataReader.GetTableRowCount(TableIndex.AssemblyRef) < index)
                    return null;
                return EcmaModule.GetObject(MetadataTokens.EntityHandle(((int)CorTokenType.mdtAssemblyRef) | index), NotFoundBehavior.ReturnNull) as EcmaModule;
            }
        }

        // Load type from IBC ZapSig. Returns null for cases where the type is legally defined, but is not used in R2R image generation
        private TypeDesc GetSigTypeFromIBCZapSig(IBCModule ibcModule, EcmaModule ecmaModule, BlobReader sig)
        {
            TypeSystemContext context = ibcModule.EcmaModule.Context;

            CorElementType typ = (CorElementType)sig.ReadByte();
            switch (typ)
            {
                case CorElementType.ELEMENT_TYPE_VOID:
                    return context.GetWellKnownType(WellKnownType.Void);
                case CorElementType.ELEMENT_TYPE_BOOLEAN:
                    return context.GetWellKnownType(WellKnownType.Boolean);
                case CorElementType.ELEMENT_TYPE_CHAR:
                    return context.GetWellKnownType(WellKnownType.Char);
                case CorElementType.ELEMENT_TYPE_I: 
                    return context.GetWellKnownType(WellKnownType.IntPtr); 
                case CorElementType.ELEMENT_TYPE_U: 
                    return context.GetWellKnownType(WellKnownType.UIntPtr);
                case CorElementType.ELEMENT_TYPE_I1:
                    return context.GetWellKnownType(WellKnownType.SByte);
                case CorElementType.ELEMENT_TYPE_U1:
                    return context.GetWellKnownType(WellKnownType.Byte);
                case CorElementType.ELEMENT_TYPE_I2:
                    return context.GetWellKnownType(WellKnownType.Int16);
                case CorElementType.ELEMENT_TYPE_U2:
                    return context.GetWellKnownType(WellKnownType.UInt16);
                case CorElementType.ELEMENT_TYPE_I4:
                    return context.GetWellKnownType(WellKnownType.Int32);
                case CorElementType.ELEMENT_TYPE_U4:
                    return context.GetWellKnownType(WellKnownType.UInt32);
                case CorElementType.ELEMENT_TYPE_I8:
                    return context.GetWellKnownType(WellKnownType.Int64);
                case CorElementType.ELEMENT_TYPE_U8:
                    return context.GetWellKnownType(WellKnownType.UInt64);
                case CorElementType.ELEMENT_TYPE_R4:
                    return context.GetWellKnownType(WellKnownType.Single);
                case CorElementType.ELEMENT_TYPE_R8:
                    return context.GetWellKnownType(WellKnownType.Double);
                case CorElementType.ELEMENT_TYPE_STRING:
                    return context.GetWellKnownType(WellKnownType.String);
                case CorElementType.ELEMENT_TYPE_OBJECT:
                    return context.GetWellKnownType(WellKnownType.Object);
                case CorElementType.ELEMENT_TYPE_TYPEDBYREF:
                    return context.GetWellKnownType(WellKnownType.TypedReference);
                case CorElementType.ELEMENT_TYPE_CANON_ZAPSIG:
                    if (!context.SupportsCanon)
                        return null;
                    return context.CanonType;
                case CorElementType.ELEMENT_TYPE_MODULE_ZAPSIG:
                    // If null, then the remote reference is not found. GetSigTypeFromIBCZapSig will search all locations to try and resolve the type
                    EcmaModule remoteModule = ibcModule.GetModuleFromIndex(sig.ReadCompressedInteger());
                    return GetSigTypeFromIBCZapSig(ibcModule, remoteModule, sig);

                case CorElementType.ELEMENT_TYPE_VAR:
                case CorElementType.ELEMENT_TYPE_MVAR:
                    // VAR/MVAR can never appear in a ZapSig
                    throw new Exception("Attempt to parse ELEMENT_TYPE_VAR or ELEMENT_TYPE_MVAR in an IBC ZapSig");

                case CorElementType.ELEMENT_TYPE_GENERICINST:
                    CorElementType genericTyp = (CorElementType)sig.ReadByte();
                    MetadataType genericDefinitionType = LoadTypeFromIBCZapSig(ibcModule, ecmaModule, genericTyp, ref sig);
                    if (genericDefinitionType == null)
                        return null;
                    int numTypeArgs = sig.ReadCompressedInteger();
                    TypeDesc[] typeArgs = new TypeDesc[numTypeArgs];
                    for (int i = 0; i < numTypeArgs; i++)
                    {
                        TypeDesc nextTypeArg = GetSigTypeFromIBCZapSig(ibcModule, ecmaModule, sig);
                        if (nextTypeArg == null)
                            return null;
                        SkipTypeInIBCZapSig(ref sig);
                        typeArgs[i] = nextTypeArg;
                    }
                    return genericDefinitionType.MakeInstantiatedType(new Instantiation(typeArgs));

                case CorElementType.ELEMENT_TYPE_CLASS:
                case CorElementType.ELEMENT_TYPE_VALUETYPE:
                    return LoadTypeFromIBCZapSig(ibcModule, ecmaModule, typ, ref sig);

                case CorElementType.ELEMENT_TYPE_SZARRAY:
                    {
                        TypeDesc arrayElementType = GetSigTypeFromIBCZapSig(ibcModule, ecmaModule, sig);
                        if (arrayElementType == null)
                            return null;
                        return arrayElementType.MakeArrayType();
                    }

                case CorElementType.ELEMENT_TYPE_ARRAY:
                    {
                        TypeDesc arrayElementType = GetSigTypeFromIBCZapSig(ibcModule, ecmaModule, sig);
                        if (arrayElementType == null)
                            return null;
                        SkipTypeInIBCZapSig(ref sig);
                        return arrayElementType.MakeArrayType(sig.ReadCompressedInteger());
                    }

                case CorElementType.ELEMENT_TYPE_PINNED:
                    // Return what follows
                    return GetSigTypeFromIBCZapSig(ibcModule, ecmaModule, sig);

                case CorElementType.ELEMENT_TYPE_BYREF:
                    {
                        TypeDesc byRefToType = GetSigTypeFromIBCZapSig(ibcModule, ecmaModule, sig);
                        if (byRefToType == null)
                            return null;
                        return byRefToType.MakeByRefType();
                    }

                case CorElementType.ELEMENT_TYPE_PTR:
                    {
                        TypeDesc pointerToType = GetSigTypeFromIBCZapSig(ibcModule, ecmaModule, sig);
                        if (pointerToType == null)
                            return null;
                        return pointerToType.MakePointerType();
                    }
                default:
                    throw new Exception($"Invalid element type {typ:x} in IBC ZapSig");
            }
        }

        private static void SkipTypeInIBCZapSig(ref BlobReader sig)
        {
            CorElementType typ = (CorElementType)sig.ReadByte();
            switch (typ)
            {
                case CorElementType.ELEMENT_TYPE_VOID:
                case CorElementType.ELEMENT_TYPE_BOOLEAN:
                case CorElementType.ELEMENT_TYPE_CHAR:
                case CorElementType.ELEMENT_TYPE_I:
                case CorElementType.ELEMENT_TYPE_U:
                case CorElementType.ELEMENT_TYPE_I1:
                case CorElementType.ELEMENT_TYPE_U1:
                case CorElementType.ELEMENT_TYPE_I2:
                case CorElementType.ELEMENT_TYPE_U2:
                case CorElementType.ELEMENT_TYPE_I4:
                case CorElementType.ELEMENT_TYPE_U4:
                case CorElementType.ELEMENT_TYPE_I8:
                case CorElementType.ELEMENT_TYPE_U8:
                case CorElementType.ELEMENT_TYPE_R4:
                case CorElementType.ELEMENT_TYPE_R8:
                case CorElementType.ELEMENT_TYPE_STRING:
                case CorElementType.ELEMENT_TYPE_OBJECT:
                case CorElementType.ELEMENT_TYPE_TYPEDBYREF:
                    return;

                case CorElementType.ELEMENT_TYPE_CANON_ZAPSIG:
                    return;
                case CorElementType.ELEMENT_TYPE_MODULE_ZAPSIG:
                    sig.ReadCompressedInteger();
                    SkipTypeInIBCZapSig(ref sig);
                    return;
                case CorElementType.ELEMENT_TYPE_VAR:
                case CorElementType.ELEMENT_TYPE_MVAR:
                    // VAR/MVAR can never appear in a ZapSig
                    throw new Exception("Attempt to parse ELEMENT_TYPE_VAR or ELEMENT_TYPE_MVAR in an IBC ZapSig");

                case CorElementType.ELEMENT_TYPE_GENERICINST:
                    sig.ReadByte();
                    sig.ReadTypeHandle();
                    int numTypeArgs = sig.ReadCompressedInteger();
                    for (int i = 0; i < numTypeArgs; i++)
                    {
                        SkipTypeInIBCZapSig(ref sig);
                    }
                    return;

                case CorElementType.ELEMENT_TYPE_CLASS:
                case CorElementType.ELEMENT_TYPE_VALUETYPE:
                    sig.ReadTypeHandle();
                    return;

                case CorElementType.ELEMENT_TYPE_SZARRAY:
                    SkipTypeInIBCZapSig(ref sig);
                    return;

                case CorElementType.ELEMENT_TYPE_ARRAY:
                    SkipTypeInIBCZapSig(ref sig);
                    sig.ReadCompressedInteger(); // rank
                    int numSizes = sig.ReadCompressedInteger();
                    for (int i = 0; i < numSizes; i++)
                        sig.ReadCompressedInteger();
                    int numLoBounds = sig.ReadCompressedInteger();
                    for (int i = 0; i < numLoBounds; i++)
                        sig.ReadCompressedInteger();
                    return;

                case CorElementType.ELEMENT_TYPE_PINNED:
                case CorElementType.ELEMENT_TYPE_BYREF:
                case CorElementType.ELEMENT_TYPE_PTR:
                    SkipTypeInIBCZapSig(ref sig);
                    return;

                default:
                    throw new Exception($"Invalid element type {typ:x} in IBC ZapSig");
            }
        }

        private MetadataType LoadTypeFromIBCZapSig(IBCModule ibcModule, EcmaModule ecmaModule, CorElementType typ, ref BlobReader sig)
        {
            switch (typ)
            {
                case CorElementType.ELEMENT_TYPE_CLASS:
                case CorElementType.ELEMENT_TYPE_VALUETYPE:
                    uint token = (uint)ibcModule.EcmaModule.MetadataReader.GetToken(sig.ReadTypeHandle());
                    if (ecmaModule != ibcModule.EcmaModule)
                    {
                        // ibcExternalType tokens are actually encoded as mdtTypeDef tokens in the signature
                        uint rid = Cor.Macros.RidFromToken(token);
                        uint ibcToken = Cor.Macros.TokenFromRid(rid, CorTokenType.ibcExternalType);
                        token = LookupIbcTypeToken(ref ecmaModule, ibcToken, ibcModule.Blobs);
                    }
                    switch (Cor.Macros.TypeFromToken(token))
                    {
                        case CorTokenType.mdtTypeDef:
                        case CorTokenType.mdtTypeRef:
                            // success
                            break;
                        default:
                            throw new Exception("Invalid token found while parsing IBC ZapSig generic instantiation");
                    }
                    if (Cor.Macros.IsNilToken(token))
                        return null;

                    var result = (MetadataType)ecmaModule.GetType(MetadataTokens.EntityHandle((int)token));
                    if ((typ == CorElementType.ELEMENT_TYPE_VALUETYPE) != result.IsValueType)
                    {
                        if (_logger.IsVerbose)
                            _logger.Writer.WriteLine("Mismatch between valuetype and reference type in while parsing generic instantiation");
                        return null;
                    }
                    return result;
                default:
                    if (_logger.IsVerbose)
                        _logger.Writer.WriteLine("Unexpected token type parsing ELEMENT_TYPE_GENERICINST");
                    return null;
            }
        }

        private MethodDesc GetSigMethodInstantiationFromIBCMethodSpec(IBCModule ibcModule, BlobReader sig)
        {
            TypeDesc methodType = GetSigTypeFromIBCZapSig(ibcModule, ibcModule.EcmaModule, sig);
            if (methodType == null)
                return null;

            SkipTypeInIBCZapSig(ref sig);
            uint flags = (uint)sig.ReadCompressedInteger();
            if (Macros.IsSlotUsedInsteadOfToken(flags))
            {
                int slot = sig.ReadCompressedInteger();
                if (_logger.IsVerbose)
                    _logger.Writer.WriteLine($"Warning: IBC Data for `{methodType}` with slot '{slot}' was ignored");
                return null; // Unsupported case thought to be used only for array methods, which don't really matter for R2R codegen
            }
            else
            {
                // Decode method token

                uint methodRid = (uint)sig.ReadCompressedInteger();
                uint methodToken;
                MetadataType methodMetadataType = (MetadataType)methodType;
                if (ibcModule.EcmaModule == methodMetadataType.Module)
                {
                    methodToken = Cor.Macros.TokenFromRid(methodRid, CorTokenType.mdtMethodDef);
                }
                else
                {
                    uint ibcToken = Cor.Macros.TokenFromRid(methodRid, CorTokenType.ibcExternalMethod);
                    methodToken = LookupIbcMethodToken(methodMetadataType, ibcToken, ibcModule.Blobs);
                    if (Cor.Macros.RidFromToken(methodToken) == 0)
                    {
                        if (_logger.IsVerbose)
                            _logger.Writer.WriteLine($"Warning: External Method Token {ibcToken:x} on '{methodMetadataType}' could not be found.");
                        return null;
                    }
                }

                EcmaModule ecmaModuleOfMethod = ((EcmaType)methodMetadataType.GetTypeDefinition()).EcmaModule;
                MethodDesc ecmaMethod = ecmaModuleOfMethod.GetMethod(MetadataTokens.EntityHandle((int)methodToken));
                MethodDesc methodOnType = methodType.FindMethodOnTypeWithMatchingTypicalMethod(ecmaMethod);

                MethodDesc methodFound = methodOnType;
                if (Macros.IsInstantiationNeeded(flags))
                {
                    int instantiationArgumentCount = methodOnType.Instantiation.Length;
                    Debug.Assert(instantiationArgumentCount > 0);
                    List<TypeDesc> instantiationArguments = new List<TypeDesc>();
                    for (int i = 0; i < instantiationArgumentCount; i++)
                    {
                        TypeDesc instantiationType = GetSigTypeFromIBCZapSig(ibcModule, ibcModule.EcmaModule, sig);
                        if (instantiationType == null)
                            return null;

                        instantiationArguments.Add(instantiationType);
                        SkipTypeInIBCZapSig(ref sig);
                    }

                    methodFound = methodOnType.MakeInstantiatedMethod(new Instantiation(instantiationArguments.ToArray()));
                }

                if (Macros.IsUnboxingStub(flags))
                {
                    if (_logger.IsVerbose)
                        _logger.Writer.WriteLine($"Warning: Skipping IBC data for unboxing stub {methodFound}");
                    return null;
                }

                if (Macros.IsInstantiatingStub(flags))
                {
                    if (_logger.IsVerbose)
                        _logger.Writer.WriteLine($"Warning: Skipping IBC data for instantiating stub {methodFound}");
                    return null;
                }

                return methodFound;
            }
        }
    }
}
