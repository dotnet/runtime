// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The maps of a method context, one for each kind of JIT-EE interface call; each is a packet of the .mc format.

namespace SuperPmi;

public partial class MethodContext
{
    // The maps are serialized in this order.
    internal static readonly MapInfo[] s_maps =
    [
        new MapInfo(0, "AllocPgoInstrumentationBySchema", PacketId.AllocPgoInstrumentationBySchema, static () => new LightWeightMap<ulong, Agnostic_AllocPgoInstrumentationBySchema>()),
        new MapInfo(1, "GetPgoInstrumentationResults", PacketId.GetPgoInstrumentationResults, static () => new LightWeightMap<ulong, Agnostic_GetPgoInstrumentationResults>()),
        new MapInfo(2, "AsCorInfoType", PacketId.AsCorInfoType, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(3, "CanAccessClass", PacketId.CanAccessClass, static () => new LightWeightMap<Agnostic_CanAccessClassIn, Agnostic_CanAccessClassOut>()),
        new MapInfo(4, "CanCast", PacketId.CanCast, static () => new LightWeightMap<DLDL, uint>()),
        new MapInfo(5, "CanInline", PacketId.CanInline, static () => new LightWeightMap<DLDL, Agnostic_CanInline>()),
        new MapInfo(6, "CanTailCall", PacketId.CanTailCall, static () => new LightWeightMap<Agnostic_CanTailCall, uint>()),
        new MapInfo(7, "CheckMethodModifier", PacketId.CheckMethodModifier, static () => new LightWeightMap<Agnostic_CheckMethodModifier, uint>()),
        new MapInfo(8, "CompareTypesForCast", PacketId.CompareTypesForCast, static () => new LightWeightMap<DLDL, uint>()),
        new MapInfo(9, "CompareTypesForEquality", PacketId.CompareTypesForEquality, static () => new LightWeightMap<DLDL, uint>()),
        new MapInfo(10, "CompileMethod", PacketId.CompileMethod, static () => new LightWeightMap<uint, Agnostic_CompileMethod>()),
        new MapInfo(11, "ConstructStringLiteral", PacketId.ConstructStringLiteral, static () => new LightWeightMap<DLD, DLD>()),
        new MapInfo(12, "ConvertPInvokeCalliToCall", PacketId.ConvertPInvokeCalliToCall, static () => new LightWeightMap<DLD, DLDL>()),
        new MapInfo(13, "EmbedClassHandle", PacketId.EmbedClassHandle, static () => new LightWeightMap<ulong, DLDL>()),
        new MapInfo(14, "EmbedFieldHandle", PacketId.EmbedFieldHandle, static () => new LightWeightMap<ulong, DLDL>()),
        new MapInfo(15, "EmbedGenericHandle", PacketId.EmbedGenericHandle, static () => new LightWeightMap<Agnostic_EmbedGenericHandle, Agnostic_CORINFO_GENERICHANDLE_RESULT>()),
        new MapInfo(16, "EmbedMethodHandle", PacketId.EmbedMethodHandle, static () => new LightWeightMap<ulong, DLDL>()),
        new MapInfo(17, "EmbedModuleHandle", PacketId.EmbedModuleHandle, static () => new LightWeightMap<ulong, DLDL>()),
        new MapInfo(18, "GetThreadLocalFieldInfo", PacketId.GetThreadLocalFieldInfo, static () => new LightWeightMap<DLD, uint>()),
        new MapInfo(19, "GetThreadLocalStaticBlocksInfo", PacketId.GetThreadLocalStaticBlocksInfo, static () => new LightWeightMap<uint, Agnostic_GetThreadLocalStaticBlocksInfo>()),
        new MapInfo(20, "GetThreadLocalStaticInfo_NativeAOT", PacketId.GetThreadLocalStaticInfo_NativeAOT, static () => new LightWeightMap<ulong, Agnostic_GetThreadStaticInfo_NativeAOT>()),
        new MapInfo(21, "EmptyStringLiteral", PacketId.EmptyStringLiteral, static () => new DenseLightWeightMap<DLD>()),
        new MapInfo(22, "ErrorList", PacketId.ErrorList, static () => new DenseLightWeightMap<uint>()),
        new MapInfo(23, "FindCallSiteSig", PacketId.FindCallSiteSig, static () => new LightWeightMap<Agnostic_FindCallSiteSig, Agnostic_CORINFO_SIG_INFO>()),
        new MapInfo(24, "FindSig", PacketId.FindSig, static () => new LightWeightMap<Agnostic_FindSig, Agnostic_CORINFO_SIG_INFO>()),
        new MapInfo(25, "GetAddressOfPInvokeTarget", PacketId.GetAddressOfPInvokeTarget, static () => new LightWeightMap<ulong, DLD>()),
        new MapInfo(26, "GetAddrOfCaptureThreadGlobal", PacketId.GetAddrOfCaptureThreadGlobal, static () => new LightWeightMap<uint, DLDL>()),
        new MapInfo(27, "GetArgClass", PacketId.GetArgClass, static () => new LightWeightMap<Agnostic_GetArgClass_Key, Agnostic_GetArgClass_Value>()),
        new MapInfo(28, "GetArgNext", PacketId.GetArgNext, static () => new LightWeightMap<ulong, ulong>()),
        new MapInfo(29, "GetArgType", PacketId.GetArgType, static () => new LightWeightMap<Agnostic_GetArgType_Key, Agnostic_GetArgType_Value>()),
        new MapInfo(30, "GetExactClasses", PacketId.GetExactClasses, static () => new LightWeightMap<DLD, Agnostic_GetExactClassesResult>()),
        new MapInfo(31, "GetArrayInitializationData", PacketId.GetArrayInitializationData, static () => new LightWeightMap<DLD, ulong>()),
        new MapInfo(32, "GetArrayRank", PacketId.GetArrayRank, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(33, "GetArrayIntrinsicID", PacketId.GetArrayIntrinsicID, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(34, "GetBoundaries", PacketId.GetBoundaries, static () => new LightWeightMap<ulong, Agnostic_GetBoundaries>()),
        new MapInfo(35, "GetBoxHelper", PacketId.GetBoxHelper, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(36, "GetBuiltinClass", PacketId.GetBuiltinClass, static () => new LightWeightMap<uint, ulong>()),
        new MapInfo(37, "GetCallInfo", PacketId.GetCallInfo, static () => new LightWeightMap<Agnostic_GetCallInfo, Agnostic_CORINFO_CALL_INFO>()),
        new MapInfo(38, "GetCastingHelper", PacketId.GetCastingHelper, static () => new LightWeightMap<Agnostic_GetCastingHelper, uint>()),
        new MapInfo(39, "GetChildType", PacketId.GetChildType, static () => new LightWeightMap<ulong, DLD>()),
        new MapInfo(40, "GetClassAlignmentRequirement", PacketId.GetClassAlignmentRequirement, static () => new LightWeightMap<DLD, uint>()),
        new MapInfo(41, "GetClassAttribs", PacketId.GetClassAttribs, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(42, "GetClassStaticDynamicInfo", PacketId.GetClassStaticDynamicInfo, static () => new LightWeightMap<ulong, DLD>()),
        new MapInfo(43, "GetClassThreadStaticDynamicInfo", PacketId.GetClassThreadStaticDynamicInfo, static () => new LightWeightMap<ulong, DLD>()),
        new MapInfo(44, "GetClassGClayout", PacketId.GetClassGClayout, static () => new LightWeightMap<ulong, Agnostic_GetClassGClayout>()),
        new MapInfo(45, "GetClassModuleIdForStatics", PacketId.GetClassModuleIdForStatics, static () => new LightWeightMap<ulong, Agnostic_GetClassModuleIdForStatics>()),
        new MapInfo(46, "GetIsClassInitedFlagAddress", PacketId.GetIsClassInitedFlagAddress, static () => new LightWeightMap<ulong, Agnostic_GetIsClassInitedFlagAddress>()),
        new MapInfo(47, "GetStaticBaseAddress", PacketId.GetStaticBaseAddress, static () => new LightWeightMap<DLD, Agnostic_GetStaticBaseAddress>()),
        new MapInfo(48, "GetClassNameFromMetadata", PacketId.GetClassNameFromMetadata, static () => new LightWeightMap<DLD, DD>()),
        new MapInfo(49, "GetTypeInstantiationArgument", PacketId.GetTypeInstantiationArgument, static () => new LightWeightMap<DLD, ulong>()),
        new MapInfo(50, "GetMethodInstantiationArgument", PacketId.GetMethodInstantiationArgument, static () => new LightWeightMap<DLD, ulong>()),
        new MapInfo(51, "GetClassNumInstanceFields", PacketId.GetClassNumInstanceFields, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(52, "GetClassSize", PacketId.GetClassSize, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(53, "GetHeapClassSize", PacketId.GetHeapClassSize, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(54, "CanAllocateOnStack", PacketId.CanAllocateOnStack, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(55, "GetCookieForInterpreterCalliSig", PacketId.GetCookieForInterpreterCalliSig, static () => new LightWeightMap<GetCookieForInterpreterCalliSigValue, DLDL>()),
        new MapInfo(56, "GetDefaultComparerClass", PacketId.GetDefaultComparerClass, static () => new LightWeightMap<ulong, ulong>()),
        new MapInfo(57, "GetDefaultEqualityComparerClass", PacketId.GetDefaultEqualityComparerClass, static () => new LightWeightMap<ulong, ulong>()),
        new MapInfo(58, "GetSZArrayHelperEnumeratorClass", PacketId.GetSZArrayHelperEnumeratorClass, static () => new LightWeightMap<ulong, ulong>()),
        new MapInfo(59, "GetDelegateCtor", PacketId.GetDelegateCtor, static () => new LightWeightMap<Agnostic_GetDelegateCtorIn, Agnostic_GetDelegateCtorOut>()),
        new MapInfo(60, "GetEEInfo", PacketId.GetEEInfo, static () => new LightWeightMap<uint, Agnostic_CORINFO_EE_INFO>()),
        new MapInfo(61, "GetAsyncInfo", PacketId.GetAsyncInfo, static () => new LightWeightMap<uint, Agnostic_CORINFO_ASYNC_INFO>()),
        new MapInfo(62, "GetAwaitReturnCall", PacketId.GetAwaitReturnCall, static () => new LightWeightMap<ulong, Agnostic_GetAwaitReturnCallResult>()),
        new MapInfo(63, "GetAwaitAwaiterInContinuationCall", PacketId.GetAwaitAwaiterInContinuationCall, static () => new LightWeightMap<Agnostic_GetAwaitAwaiterInContinuationCall, Agnostic_GetAwaitReturnCallResult>()),
        new MapInfo(64, "GetEHinfo", PacketId.GetEHinfo, static () => new LightWeightMap<DLD, Agnostic_CORINFO_EH_CLAUSE>()),
        new MapInfo(65, "GetStaticFieldContent", PacketId.GetStaticFieldContent, static () => new LightWeightMap<DLDDD, DD>()),
        new MapInfo(66, "GetObjectContent", PacketId.GetObjectContent, static () => new LightWeightMap<DLDD, DD>()),
        new MapInfo(67, "GetStaticFieldCurrentClass", PacketId.GetStaticFieldCurrentClass, static () => new LightWeightMap<DLD, Agnostic_GetStaticFieldCurrentClass>()),
        new MapInfo(68, "GetFieldClass", PacketId.GetFieldClass, static () => new LightWeightMap<ulong, ulong>()),
        new MapInfo(69, "GetFieldInClass", PacketId.GetFieldInClass, static () => new LightWeightMap<DLD, ulong>()),
        new MapInfo(70, "GetTypeLayout", PacketId.GetTypeLayout, static () => new LightWeightMap<ulong, Agnostic_GetTypeLayoutResult>()),
        new MapInfo(71, "GetFieldInfo", PacketId.GetFieldInfo, static () => new LightWeightMap<Agnostic_GetFieldInfo, Agnostic_CORINFO_FIELD_INFO>()),
        new MapInfo(72, "GetFieldOffset", PacketId.GetFieldOffset, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(73, "GetFieldThreadLocalStoreID", PacketId.GetFieldThreadLocalStoreID, static () => new LightWeightMap<ulong, DLD>()),
        new MapInfo(74, "GetFieldType", PacketId.GetFieldType, static () => new LightWeightMap<DLDL, DLD>()),
        new MapInfo(75, "GetFunctionEntryPoint", PacketId.GetFunctionEntryPoint, static () => new LightWeightMap<DLD, DLD>()),
        new MapInfo(76, "GetFunctionFixedEntryPoint", PacketId.GetFunctionFixedEntryPoint, static () => new LightWeightMap<ulong, Agnostic_CORINFO_CONST_LOOKUP>()),
        new MapInfo(77, "GetGSCookie", PacketId.GetGSCookie, static () => new LightWeightMap<uint, DLDL>()),
        new MapInfo(78, "GetHelperFtn", PacketId.GetHelperFtn, static () => new LightWeightMap<uint, Agnostic_GetHelperFtn>()),
        new MapInfo(79, "GetHFAType", PacketId.GetHFAType, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(80, "GetIntConfigValue", PacketId.GetIntConfigValue, static () => new LightWeightMap<Agnostic_ConfigIntInfo, uint>()),
        new MapInfo(81, "GetJitFlags", PacketId.GetJitFlags, static () => new LightWeightMap<uint, DD>()),
        new MapInfo(82, "GetJustMyCodeHandle", PacketId.GetJustMyCodeHandle, static () => new LightWeightMap<ulong, DLDL>()),
        new MapInfo(83, "GetLocationOfThisType", PacketId.GetLocationOfThisType, static () => new LightWeightMap<ulong, Agnostic_CORINFO_LOOKUP_KIND>()),
        new MapInfo(84, "IsIntrinsic", PacketId.IsIntrinsic, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(85, "NotifyMethodInfoUsage", PacketId.NotifyMethodInfoUsage, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(86, "NotifyInstructionSetUsage", PacketId.NotifyInstructionSetUsage, static () => new LightWeightMap<DD, uint>()),
        new MapInfo(87, "GetMethodAttribs", PacketId.GetMethodAttribs, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(88, "GetClassAssemblyName", PacketId.GetClassAssemblyName, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(89, "GetMethodClass", PacketId.GetMethodClass, static () => new LightWeightMap<ulong, ulong>()),
        new MapInfo(90, "GetMethodDefFromMethod", PacketId.GetMethodDefFromMethod, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(91, "GetMethodHash", PacketId.GetMethodHash, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(92, "GetMethodInfo", PacketId.GetMethodInfo, static () => new LightWeightMap<DLDL, Agnostic_GetMethodInfo>()),
        new MapInfo(93, "HaveSameMethodDefinition", PacketId.HaveSameMethodDefinition, static () => new LightWeightMap<DLDL, uint>()),
        new MapInfo(94, "GetTypeDefinition", PacketId.GetTypeDefinition, static () => new LightWeightMap<ulong, ulong>()),
        new MapInfo(95, "GetMethodNameFromMetadata", PacketId.GetMethodNameFromMetadata, static () => new LightWeightMap<Agnostic_CORINFO_METHODNAME_TOKENin, Agnostic_CORINFO_METHODNAME_TOKENout>()),
        new MapInfo(96, "GetMethodSig", PacketId.GetMethodSig, static () => new LightWeightMap<DLDL, Agnostic_CORINFO_SIG_INFO>()),
        new MapInfo(97, "GetMethodVTableOffset", PacketId.GetMethodVTableOffset, static () => new LightWeightMap<ulong, DDD>()),
        new MapInfo(98, "GetNewArrHelper", PacketId.GetNewArrHelper, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(99, "GetNewHelper", PacketId.GetNewHelper, static () => new LightWeightMap<ulong, DDD>()),
        new MapInfo(100, "GetOSRInfo", PacketId.GetOSRInfo, static () => new LightWeightMap<uint, Agnostic_GetOSRInfo>()),
        new MapInfo(101, "GetParentType", PacketId.GetParentType, static () => new LightWeightMap<ulong, ulong>()),
        new MapInfo(102, "GetProfilingHandle", PacketId.GetProfilingHandle, static () => new LightWeightMap<uint, Agnostic_GetProfilingHandle>()),
        new MapInfo(103, "GetReadyToRunHelper", PacketId.GetReadyToRunHelper, static () => new LightWeightMap<GetReadyToRunHelper_TOKENin, GetReadyToRunHelper_TOKENout>()),
        new MapInfo(104, "GetReadyToRunDelegateCtorHelper", PacketId.GetReadyToRunDelegateCtorHelper, static () => new LightWeightMap<GetReadyToRunDelegateCtorHelper_TOKENIn, Agnostic_CORINFO_LOOKUP>()),
        new MapInfo(105, "GetRelocTypeHint", PacketId.GetRelocTypeHint, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(106, "GetExpectedTargetArchitecture", PacketId.GetExpectedTargetArchitecture, static () => new LightWeightMap<uint, uint>()),
        new MapInfo(107, "GetSharedCCtorHelper", PacketId.GetSharedCCtorHelper, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(108, "GetStringConfigValue", PacketId.GetStringConfigValue, static () => new LightWeightMap<uint, uint>()),
        new MapInfo(109, "GetSystemVAmd64PassStructInRegisterDescriptor", PacketId.GetSystemVAmd64PassStructInRegisterDescriptor, static () => new LightWeightMap<ulong, Agnostic_GetSystemVAmd64PassStructInRegisterDescriptor>()),
        new MapInfo(110, "GetSwiftLowering", PacketId.GetSwiftLowering, static () => new LightWeightMap<ulong, Agnostic_GetSwiftLowering>()),
        new MapInfo(111, "GetFpStructLowering", PacketId.GetFpStructLowering, static () => new LightWeightMap<ulong, Agnostic_GetFpStructLowering>()),
        new MapInfo(112, "GetWasmLowering", PacketId.GetWasmLowering, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(113, "GetTailCallHelpers", PacketId.GetTailCallHelpers, static () => new LightWeightMap<Agnostic_GetTailCallHelpers, Agnostic_CORINFO_TAILCALL_HELPERS>()),
        new MapInfo(114, "GetAsyncResumptionStub", PacketId.GetAsyncResumptionStub, static () => new LightWeightMap<uint, DLDL>()),
        new MapInfo(115, "GetContinuationType", PacketId.GetContinuationType, static () => new LightWeightMap<Agnostic_GetContinuationTypeIn, ulong>()),
        new MapInfo(116, "UpdateEntryPointForTailCall", PacketId.UpdateEntryPointForTailCall, static () => new LightWeightMap<Agnostic_CORINFO_CONST_LOOKUP, Agnostic_CORINFO_CONST_LOOKUP>()),
        new MapInfo(117, "GetSpecialCopyHelper", PacketId.GetSpecialCopyHelper, static () => new LightWeightMap<ulong, ulong>()),
        new MapInfo(118, "GetThreadTLSIndex", PacketId.GetThreadTLSIndex, static () => new LightWeightMap<uint, DLD>()),
        new MapInfo(119, "GetTokenTypeAsHandle", PacketId.GetTokenTypeAsHandle, static () => new LightWeightMap<GetTokenTypeAsHandleValue, ulong>()),
        new MapInfo(120, "GetTypeForBox", PacketId.GetTypeForBox, static () => new LightWeightMap<ulong, ulong>()),
        new MapInfo(121, "GetTypeForPrimitiveValueClass", PacketId.GetTypeForPrimitiveValueClass, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(122, "GetTypeForPrimitiveNumericClass", PacketId.GetTypeForPrimitiveNumericClass, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(123, "GetAsyncOtherVariant", PacketId.GetAsyncOtherVariant, static () => new LightWeightMap<ulong, DLD>()),
        new MapInfo(124, "GetUnBoxHelper", PacketId.GetUnBoxHelper, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(125, "GetRuntimeTypePointer", PacketId.GetRuntimeTypePointer, static () => new LightWeightMap<ulong, ulong>()),
        new MapInfo(126, "IsObjectImmutable", PacketId.IsObjectImmutable, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(127, "GetStringChar", PacketId.GetStringChar, static () => new LightWeightMap<DLD, DD>()),
        new MapInfo(128, "GetObjectType", PacketId.GetObjectType, static () => new LightWeightMap<ulong, ulong>()),
        new MapInfo(129, "GetVarArgsHandle", PacketId.GetVarArgsHandle, static () => new LightWeightMap<GetVarArgsHandleValue, DLDL>()),
        new MapInfo(130, "GetVars", PacketId.GetVars, static () => new LightWeightMap<ulong, Agnostic_GetVars>()),
        new MapInfo(131, "InitClass", PacketId.InitClass, static () => new LightWeightMap<Agnostic_InitClass, uint>()),
        new MapInfo(132, "IsDelegateCreationAllowed", PacketId.IsDelegateCreationAllowed, static () => new LightWeightMap<DLDL, uint>()),
        new MapInfo(133, "IsFieldStatic", PacketId.IsFieldStatic, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(134, "GetArrayOrStringLength", PacketId.GetArrayOrStringLength, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(135, "ExpandRawHandleIntrinsic", PacketId.ExpandRawHandleIntrinsic, static () => new LightWeightMap<Agnostic_ExpandRawHandleIntrinsic, Agnostic_CORINFO_GENERICHANDLE_RESULT>()),
        new MapInfo(136, "IsIntrinsicType", PacketId.IsIntrinsicType, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(137, "IsSDArray", PacketId.IsSDArray, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(138, "GetStringLiteral", PacketId.GetStringLiteral, static () => new LightWeightMap<DLDDD, DD>()),
        new MapInfo(139, "PrintObjectDescription", PacketId.PrintObjectDescription, static () => new LightWeightMap<ulong, Agnostic_PrintResult>()),
        new MapInfo(140, "PrintClassName", PacketId.PrintClassName, static () => new LightWeightMap<ulong, Agnostic_PrintResult>()),
        new MapInfo(141, "PrintFieldName", PacketId.PrintFieldName, static () => new LightWeightMap<ulong, Agnostic_PrintResult>()),
        new MapInfo(142, "PrintMethodName", PacketId.PrintMethodName, static () => new LightWeightMap<ulong, Agnostic_PrintResult>()),
        new MapInfo(143, "IsValueClass", PacketId.IsValueClass, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(144, "IsMoreSpecificType", PacketId.IsMoreSpecificType, static () => new LightWeightMap<DLDL, uint>()),
        new MapInfo(145, "IsExactType", PacketId.IsExactType, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(146, "IsGenericType", PacketId.IsGenericType, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(147, "IsNullableType", PacketId.IsNullableType, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(148, "IsEnum", PacketId.IsEnum, static () => new LightWeightMap<ulong, DLD>()),
        new MapInfo(149, "PInvokeMarshalingRequired", PacketId.PInvokeMarshalingRequired, static () => new LightWeightMap<MethodOrSigInfoValue, uint>()),
        new MapInfo(150, "ResolveToken", PacketId.ResolveToken, static () => new LightWeightMap<Agnostic_CORINFO_RESOLVED_TOKENin, ResolveTokenValue>()),
        new MapInfo(151, "ResolveVirtualMethod", PacketId.ResolveVirtualMethod, static () => new LightWeightMap<Agnostic_ResolveVirtualMethodKey, Agnostic_ResolveVirtualMethodResult>()),
        new MapInfo(152, "SatisfiesMethodConstraints", PacketId.SatisfiesMethodConstraints, static () => new LightWeightMap<DLDL, uint>()),
        new MapInfo(153, "GetUnmanagedCallConv", PacketId.GetUnmanagedCallConv, static () => new LightWeightMap<MethodOrSigInfoValue, DD>()),
        new MapInfo(154, "DoesFieldBelongToClass", PacketId.DoesFieldBelongToClass, static () => new LightWeightMap<DLDL, uint>()),
        new MapInfo(155, "SigInstHandleMap", PacketId.SigInstHandleMap, static () => new DenseLightWeightMap<ulong>()),
        new MapInfo(156, "GetWasmTypeSymbol", PacketId.GetWasmTypeSymbol, static () => new LightWeightMap<Agnostic_GetWasmTypeSymbol, ulong>()),
        new MapInfo(157, "GetAddressAlignment", PacketId.GetAddressAlignment, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(158, "CanValueClassInstancePointerEscape", PacketId.CanValueClassInstancePointerEscape, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(159, "GetWasmWellKnownGlobals", PacketId.GetWasmWellKnownGlobals, static () => new LightWeightMap<uint, Agnostic_CORINFO_WASM_WELLKNOWN_GLOBALS>()),
        new MapInfo(160, "IsByRefLike", PacketId.IsByRefLike, static () => new LightWeightMap<ulong, uint>()),
        new MapInfo(161, "IsSharedInst", PacketId.IsSharedInst, static () => new LightWeightMap<ulong, uint>()),
    ];

    private readonly LightWeightMapBuffer[] _maps = new LightWeightMapBuffer[s_maps.Length];

    public LightWeightMap<ulong, Agnostic_AllocPgoInstrumentationBySchema> AllocPgoInstrumentationBySchema { get => (LightWeightMap<ulong, Agnostic_AllocPgoInstrumentationBySchema>)_maps[0]; set => _maps[0] = value; }
    public LightWeightMap<ulong, Agnostic_GetPgoInstrumentationResults> GetPgoInstrumentationResults { get => (LightWeightMap<ulong, Agnostic_GetPgoInstrumentationResults>)_maps[1]; set => _maps[1] = value; }
    public LightWeightMap<ulong, uint> AsCorInfoType { get => (LightWeightMap<ulong, uint>)_maps[2]; set => _maps[2] = value; }
    public LightWeightMap<Agnostic_CanAccessClassIn, Agnostic_CanAccessClassOut> CanAccessClass { get => (LightWeightMap<Agnostic_CanAccessClassIn, Agnostic_CanAccessClassOut>)_maps[3]; set => _maps[3] = value; }
    public LightWeightMap<DLDL, uint> CanCast { get => (LightWeightMap<DLDL, uint>)_maps[4]; set => _maps[4] = value; }
    public LightWeightMap<DLDL, Agnostic_CanInline> CanInline { get => (LightWeightMap<DLDL, Agnostic_CanInline>)_maps[5]; set => _maps[5] = value; }
    public LightWeightMap<Agnostic_CanTailCall, uint> CanTailCall { get => (LightWeightMap<Agnostic_CanTailCall, uint>)_maps[6]; set => _maps[6] = value; }
    public LightWeightMap<Agnostic_CheckMethodModifier, uint> CheckMethodModifier { get => (LightWeightMap<Agnostic_CheckMethodModifier, uint>)_maps[7]; set => _maps[7] = value; }
    public LightWeightMap<DLDL, uint> CompareTypesForCast { get => (LightWeightMap<DLDL, uint>)_maps[8]; set => _maps[8] = value; }
    public LightWeightMap<DLDL, uint> CompareTypesForEquality { get => (LightWeightMap<DLDL, uint>)_maps[9]; set => _maps[9] = value; }
    public LightWeightMap<uint, Agnostic_CompileMethod> CompileMethod { get => (LightWeightMap<uint, Agnostic_CompileMethod>)_maps[10]; set => _maps[10] = value; }
    public LightWeightMap<DLD, DLD> ConstructStringLiteral { get => (LightWeightMap<DLD, DLD>)_maps[11]; set => _maps[11] = value; }
    public LightWeightMap<DLD, DLDL> ConvertPInvokeCalliToCall { get => (LightWeightMap<DLD, DLDL>)_maps[12]; set => _maps[12] = value; }
    public LightWeightMap<ulong, DLDL> EmbedClassHandle { get => (LightWeightMap<ulong, DLDL>)_maps[13]; set => _maps[13] = value; }
    public LightWeightMap<ulong, DLDL> EmbedFieldHandle { get => (LightWeightMap<ulong, DLDL>)_maps[14]; set => _maps[14] = value; }
    public LightWeightMap<Agnostic_EmbedGenericHandle, Agnostic_CORINFO_GENERICHANDLE_RESULT> EmbedGenericHandle { get => (LightWeightMap<Agnostic_EmbedGenericHandle, Agnostic_CORINFO_GENERICHANDLE_RESULT>)_maps[15]; set => _maps[15] = value; }
    public LightWeightMap<ulong, DLDL> EmbedMethodHandle { get => (LightWeightMap<ulong, DLDL>)_maps[16]; set => _maps[16] = value; }
    public LightWeightMap<ulong, DLDL> EmbedModuleHandle { get => (LightWeightMap<ulong, DLDL>)_maps[17]; set => _maps[17] = value; }
    public LightWeightMap<DLD, uint> GetThreadLocalFieldInfo { get => (LightWeightMap<DLD, uint>)_maps[18]; set => _maps[18] = value; }
    public LightWeightMap<uint, Agnostic_GetThreadLocalStaticBlocksInfo> GetThreadLocalStaticBlocksInfo { get => (LightWeightMap<uint, Agnostic_GetThreadLocalStaticBlocksInfo>)_maps[19]; set => _maps[19] = value; }
    public LightWeightMap<ulong, Agnostic_GetThreadStaticInfo_NativeAOT> GetThreadLocalStaticInfo_NativeAOT { get => (LightWeightMap<ulong, Agnostic_GetThreadStaticInfo_NativeAOT>)_maps[20]; set => _maps[20] = value; }
    public DenseLightWeightMap<DLD> EmptyStringLiteral { get => (DenseLightWeightMap<DLD>)_maps[21]; set => _maps[21] = value; }
    public DenseLightWeightMap<uint> ErrorList { get => (DenseLightWeightMap<uint>)_maps[22]; set => _maps[22] = value; }
    public LightWeightMap<Agnostic_FindCallSiteSig, Agnostic_CORINFO_SIG_INFO> FindCallSiteSig { get => (LightWeightMap<Agnostic_FindCallSiteSig, Agnostic_CORINFO_SIG_INFO>)_maps[23]; set => _maps[23] = value; }
    public LightWeightMap<Agnostic_FindSig, Agnostic_CORINFO_SIG_INFO> FindSig { get => (LightWeightMap<Agnostic_FindSig, Agnostic_CORINFO_SIG_INFO>)_maps[24]; set => _maps[24] = value; }
    public LightWeightMap<ulong, DLD> GetAddressOfPInvokeTarget { get => (LightWeightMap<ulong, DLD>)_maps[25]; set => _maps[25] = value; }
    public LightWeightMap<uint, DLDL> GetAddrOfCaptureThreadGlobal { get => (LightWeightMap<uint, DLDL>)_maps[26]; set => _maps[26] = value; }
    public LightWeightMap<Agnostic_GetArgClass_Key, Agnostic_GetArgClass_Value> GetArgClass { get => (LightWeightMap<Agnostic_GetArgClass_Key, Agnostic_GetArgClass_Value>)_maps[27]; set => _maps[27] = value; }
    public LightWeightMap<ulong, ulong> GetArgNext { get => (LightWeightMap<ulong, ulong>)_maps[28]; set => _maps[28] = value; }
    public LightWeightMap<Agnostic_GetArgType_Key, Agnostic_GetArgType_Value> GetArgType { get => (LightWeightMap<Agnostic_GetArgType_Key, Agnostic_GetArgType_Value>)_maps[29]; set => _maps[29] = value; }
    public LightWeightMap<DLD, Agnostic_GetExactClassesResult> GetExactClasses { get => (LightWeightMap<DLD, Agnostic_GetExactClassesResult>)_maps[30]; set => _maps[30] = value; }
    public LightWeightMap<DLD, ulong> GetArrayInitializationData { get => (LightWeightMap<DLD, ulong>)_maps[31]; set => _maps[31] = value; }
    public LightWeightMap<ulong, uint> GetArrayRank { get => (LightWeightMap<ulong, uint>)_maps[32]; set => _maps[32] = value; }
    public LightWeightMap<ulong, uint> GetArrayIntrinsicID { get => (LightWeightMap<ulong, uint>)_maps[33]; set => _maps[33] = value; }
    public LightWeightMap<ulong, Agnostic_GetBoundaries> GetBoundaries { get => (LightWeightMap<ulong, Agnostic_GetBoundaries>)_maps[34]; set => _maps[34] = value; }
    public LightWeightMap<ulong, uint> GetBoxHelper { get => (LightWeightMap<ulong, uint>)_maps[35]; set => _maps[35] = value; }
    public LightWeightMap<uint, ulong> GetBuiltinClass { get => (LightWeightMap<uint, ulong>)_maps[36]; set => _maps[36] = value; }
    public LightWeightMap<Agnostic_GetCallInfo, Agnostic_CORINFO_CALL_INFO> GetCallInfo { get => (LightWeightMap<Agnostic_GetCallInfo, Agnostic_CORINFO_CALL_INFO>)_maps[37]; set => _maps[37] = value; }
    public LightWeightMap<Agnostic_GetCastingHelper, uint> GetCastingHelper { get => (LightWeightMap<Agnostic_GetCastingHelper, uint>)_maps[38]; set => _maps[38] = value; }
    public LightWeightMap<ulong, DLD> GetChildType { get => (LightWeightMap<ulong, DLD>)_maps[39]; set => _maps[39] = value; }
    public LightWeightMap<DLD, uint> GetClassAlignmentRequirement { get => (LightWeightMap<DLD, uint>)_maps[40]; set => _maps[40] = value; }
    public LightWeightMap<ulong, uint> GetClassAttribs { get => (LightWeightMap<ulong, uint>)_maps[41]; set => _maps[41] = value; }
    public LightWeightMap<ulong, DLD> GetClassStaticDynamicInfo { get => (LightWeightMap<ulong, DLD>)_maps[42]; set => _maps[42] = value; }
    public LightWeightMap<ulong, DLD> GetClassThreadStaticDynamicInfo { get => (LightWeightMap<ulong, DLD>)_maps[43]; set => _maps[43] = value; }
    public LightWeightMap<ulong, Agnostic_GetClassGClayout> GetClassGClayout { get => (LightWeightMap<ulong, Agnostic_GetClassGClayout>)_maps[44]; set => _maps[44] = value; }
    public LightWeightMap<ulong, Agnostic_GetClassModuleIdForStatics> GetClassModuleIdForStatics { get => (LightWeightMap<ulong, Agnostic_GetClassModuleIdForStatics>)_maps[45]; set => _maps[45] = value; }
    public LightWeightMap<ulong, Agnostic_GetIsClassInitedFlagAddress> GetIsClassInitedFlagAddress { get => (LightWeightMap<ulong, Agnostic_GetIsClassInitedFlagAddress>)_maps[46]; set => _maps[46] = value; }
    public LightWeightMap<DLD, Agnostic_GetStaticBaseAddress> GetStaticBaseAddress { get => (LightWeightMap<DLD, Agnostic_GetStaticBaseAddress>)_maps[47]; set => _maps[47] = value; }
    public LightWeightMap<DLD, DD> GetClassNameFromMetadata { get => (LightWeightMap<DLD, DD>)_maps[48]; set => _maps[48] = value; }
    public LightWeightMap<DLD, ulong> GetTypeInstantiationArgument { get => (LightWeightMap<DLD, ulong>)_maps[49]; set => _maps[49] = value; }
    public LightWeightMap<DLD, ulong> GetMethodInstantiationArgument { get => (LightWeightMap<DLD, ulong>)_maps[50]; set => _maps[50] = value; }
    public LightWeightMap<ulong, uint> GetClassNumInstanceFields { get => (LightWeightMap<ulong, uint>)_maps[51]; set => _maps[51] = value; }
    public LightWeightMap<ulong, uint> GetClassSize { get => (LightWeightMap<ulong, uint>)_maps[52]; set => _maps[52] = value; }
    public LightWeightMap<ulong, uint> GetHeapClassSize { get => (LightWeightMap<ulong, uint>)_maps[53]; set => _maps[53] = value; }
    public LightWeightMap<ulong, uint> CanAllocateOnStack { get => (LightWeightMap<ulong, uint>)_maps[54]; set => _maps[54] = value; }
    public LightWeightMap<GetCookieForInterpreterCalliSigValue, DLDL> GetCookieForInterpreterCalliSig { get => (LightWeightMap<GetCookieForInterpreterCalliSigValue, DLDL>)_maps[55]; set => _maps[55] = value; }
    public LightWeightMap<ulong, ulong> GetDefaultComparerClass { get => (LightWeightMap<ulong, ulong>)_maps[56]; set => _maps[56] = value; }
    public LightWeightMap<ulong, ulong> GetDefaultEqualityComparerClass { get => (LightWeightMap<ulong, ulong>)_maps[57]; set => _maps[57] = value; }
    public LightWeightMap<ulong, ulong> GetSZArrayHelperEnumeratorClass { get => (LightWeightMap<ulong, ulong>)_maps[58]; set => _maps[58] = value; }
    public LightWeightMap<Agnostic_GetDelegateCtorIn, Agnostic_GetDelegateCtorOut> GetDelegateCtor { get => (LightWeightMap<Agnostic_GetDelegateCtorIn, Agnostic_GetDelegateCtorOut>)_maps[59]; set => _maps[59] = value; }
    public LightWeightMap<uint, Agnostic_CORINFO_EE_INFO> GetEEInfo { get => (LightWeightMap<uint, Agnostic_CORINFO_EE_INFO>)_maps[60]; set => _maps[60] = value; }
    public LightWeightMap<uint, Agnostic_CORINFO_ASYNC_INFO> GetAsyncInfo { get => (LightWeightMap<uint, Agnostic_CORINFO_ASYNC_INFO>)_maps[61]; set => _maps[61] = value; }
    public LightWeightMap<ulong, Agnostic_GetAwaitReturnCallResult> GetAwaitReturnCall { get => (LightWeightMap<ulong, Agnostic_GetAwaitReturnCallResult>)_maps[62]; set => _maps[62] = value; }
    public LightWeightMap<Agnostic_GetAwaitAwaiterInContinuationCall, Agnostic_GetAwaitReturnCallResult> GetAwaitAwaiterInContinuationCall { get => (LightWeightMap<Agnostic_GetAwaitAwaiterInContinuationCall, Agnostic_GetAwaitReturnCallResult>)_maps[63]; set => _maps[63] = value; }
    public LightWeightMap<DLD, Agnostic_CORINFO_EH_CLAUSE> GetEHinfo { get => (LightWeightMap<DLD, Agnostic_CORINFO_EH_CLAUSE>)_maps[64]; set => _maps[64] = value; }
    public LightWeightMap<DLDDD, DD> GetStaticFieldContent { get => (LightWeightMap<DLDDD, DD>)_maps[65]; set => _maps[65] = value; }
    public LightWeightMap<DLDD, DD> GetObjectContent { get => (LightWeightMap<DLDD, DD>)_maps[66]; set => _maps[66] = value; }
    public LightWeightMap<DLD, Agnostic_GetStaticFieldCurrentClass> GetStaticFieldCurrentClass { get => (LightWeightMap<DLD, Agnostic_GetStaticFieldCurrentClass>)_maps[67]; set => _maps[67] = value; }
    public LightWeightMap<ulong, ulong> GetFieldClass { get => (LightWeightMap<ulong, ulong>)_maps[68]; set => _maps[68] = value; }
    public LightWeightMap<DLD, ulong> GetFieldInClass { get => (LightWeightMap<DLD, ulong>)_maps[69]; set => _maps[69] = value; }
    public LightWeightMap<ulong, Agnostic_GetTypeLayoutResult> GetTypeLayout { get => (LightWeightMap<ulong, Agnostic_GetTypeLayoutResult>)_maps[70]; set => _maps[70] = value; }
    public LightWeightMap<Agnostic_GetFieldInfo, Agnostic_CORINFO_FIELD_INFO> GetFieldInfo { get => (LightWeightMap<Agnostic_GetFieldInfo, Agnostic_CORINFO_FIELD_INFO>)_maps[71]; set => _maps[71] = value; }
    public LightWeightMap<ulong, uint> GetFieldOffset { get => (LightWeightMap<ulong, uint>)_maps[72]; set => _maps[72] = value; }
    public LightWeightMap<ulong, DLD> GetFieldThreadLocalStoreID { get => (LightWeightMap<ulong, DLD>)_maps[73]; set => _maps[73] = value; }
    public LightWeightMap<DLDL, DLD> GetFieldType { get => (LightWeightMap<DLDL, DLD>)_maps[74]; set => _maps[74] = value; }
    public LightWeightMap<DLD, DLD> GetFunctionEntryPoint { get => (LightWeightMap<DLD, DLD>)_maps[75]; set => _maps[75] = value; }
    public LightWeightMap<ulong, Agnostic_CORINFO_CONST_LOOKUP> GetFunctionFixedEntryPoint { get => (LightWeightMap<ulong, Agnostic_CORINFO_CONST_LOOKUP>)_maps[76]; set => _maps[76] = value; }
    public LightWeightMap<uint, DLDL> GetGSCookie { get => (LightWeightMap<uint, DLDL>)_maps[77]; set => _maps[77] = value; }
    public LightWeightMap<uint, Agnostic_GetHelperFtn> GetHelperFtn { get => (LightWeightMap<uint, Agnostic_GetHelperFtn>)_maps[78]; set => _maps[78] = value; }
    public LightWeightMap<ulong, uint> GetHFAType { get => (LightWeightMap<ulong, uint>)_maps[79]; set => _maps[79] = value; }
    public LightWeightMap<Agnostic_ConfigIntInfo, uint> GetIntConfigValue { get => (LightWeightMap<Agnostic_ConfigIntInfo, uint>)_maps[80]; set => _maps[80] = value; }
    public LightWeightMap<uint, DD> GetJitFlags { get => (LightWeightMap<uint, DD>)_maps[81]; set => _maps[81] = value; }
    public LightWeightMap<ulong, DLDL> GetJustMyCodeHandle { get => (LightWeightMap<ulong, DLDL>)_maps[82]; set => _maps[82] = value; }
    public LightWeightMap<ulong, Agnostic_CORINFO_LOOKUP_KIND> GetLocationOfThisType { get => (LightWeightMap<ulong, Agnostic_CORINFO_LOOKUP_KIND>)_maps[83]; set => _maps[83] = value; }
    public LightWeightMap<ulong, uint> IsIntrinsic { get => (LightWeightMap<ulong, uint>)_maps[84]; set => _maps[84] = value; }
    public LightWeightMap<ulong, uint> NotifyMethodInfoUsage { get => (LightWeightMap<ulong, uint>)_maps[85]; set => _maps[85] = value; }
    public LightWeightMap<DD, uint> NotifyInstructionSetUsage { get => (LightWeightMap<DD, uint>)_maps[86]; set => _maps[86] = value; }
    public LightWeightMap<ulong, uint> GetMethodAttribs { get => (LightWeightMap<ulong, uint>)_maps[87]; set => _maps[87] = value; }
    public LightWeightMap<ulong, uint> GetClassAssemblyName { get => (LightWeightMap<ulong, uint>)_maps[88]; set => _maps[88] = value; }
    public LightWeightMap<ulong, ulong> GetMethodClass { get => (LightWeightMap<ulong, ulong>)_maps[89]; set => _maps[89] = value; }
    public LightWeightMap<ulong, uint> GetMethodDefFromMethod { get => (LightWeightMap<ulong, uint>)_maps[90]; set => _maps[90] = value; }
    public LightWeightMap<ulong, uint> GetMethodHash { get => (LightWeightMap<ulong, uint>)_maps[91]; set => _maps[91] = value; }
    public LightWeightMap<DLDL, Agnostic_GetMethodInfo> GetMethodInfo { get => (LightWeightMap<DLDL, Agnostic_GetMethodInfo>)_maps[92]; set => _maps[92] = value; }
    public LightWeightMap<DLDL, uint> HaveSameMethodDefinition { get => (LightWeightMap<DLDL, uint>)_maps[93]; set => _maps[93] = value; }
    public LightWeightMap<ulong, ulong> GetTypeDefinition { get => (LightWeightMap<ulong, ulong>)_maps[94]; set => _maps[94] = value; }
    public LightWeightMap<Agnostic_CORINFO_METHODNAME_TOKENin, Agnostic_CORINFO_METHODNAME_TOKENout> GetMethodNameFromMetadata { get => (LightWeightMap<Agnostic_CORINFO_METHODNAME_TOKENin, Agnostic_CORINFO_METHODNAME_TOKENout>)_maps[95]; set => _maps[95] = value; }
    public LightWeightMap<DLDL, Agnostic_CORINFO_SIG_INFO> GetMethodSig { get => (LightWeightMap<DLDL, Agnostic_CORINFO_SIG_INFO>)_maps[96]; set => _maps[96] = value; }
    public LightWeightMap<ulong, DDD> GetMethodVTableOffset { get => (LightWeightMap<ulong, DDD>)_maps[97]; set => _maps[97] = value; }
    public LightWeightMap<ulong, uint> GetNewArrHelper { get => (LightWeightMap<ulong, uint>)_maps[98]; set => _maps[98] = value; }
    public LightWeightMap<ulong, DDD> GetNewHelper { get => (LightWeightMap<ulong, DDD>)_maps[99]; set => _maps[99] = value; }
    public LightWeightMap<uint, Agnostic_GetOSRInfo> GetOSRInfo { get => (LightWeightMap<uint, Agnostic_GetOSRInfo>)_maps[100]; set => _maps[100] = value; }
    public LightWeightMap<ulong, ulong> GetParentType { get => (LightWeightMap<ulong, ulong>)_maps[101]; set => _maps[101] = value; }
    public LightWeightMap<uint, Agnostic_GetProfilingHandle> GetProfilingHandle { get => (LightWeightMap<uint, Agnostic_GetProfilingHandle>)_maps[102]; set => _maps[102] = value; }
    public LightWeightMap<GetReadyToRunHelper_TOKENin, GetReadyToRunHelper_TOKENout> GetReadyToRunHelper { get => (LightWeightMap<GetReadyToRunHelper_TOKENin, GetReadyToRunHelper_TOKENout>)_maps[103]; set => _maps[103] = value; }
    public LightWeightMap<GetReadyToRunDelegateCtorHelper_TOKENIn, Agnostic_CORINFO_LOOKUP> GetReadyToRunDelegateCtorHelper { get => (LightWeightMap<GetReadyToRunDelegateCtorHelper_TOKENIn, Agnostic_CORINFO_LOOKUP>)_maps[104]; set => _maps[104] = value; }
    public LightWeightMap<ulong, uint> GetRelocTypeHint { get => (LightWeightMap<ulong, uint>)_maps[105]; set => _maps[105] = value; }
    public LightWeightMap<uint, uint> GetExpectedTargetArchitecture { get => (LightWeightMap<uint, uint>)_maps[106]; set => _maps[106] = value; }
    public LightWeightMap<ulong, uint> GetSharedCCtorHelper { get => (LightWeightMap<ulong, uint>)_maps[107]; set => _maps[107] = value; }
    public LightWeightMap<uint, uint> GetStringConfigValue { get => (LightWeightMap<uint, uint>)_maps[108]; set => _maps[108] = value; }
    public LightWeightMap<ulong, Agnostic_GetSystemVAmd64PassStructInRegisterDescriptor> GetSystemVAmd64PassStructInRegisterDescriptor { get => (LightWeightMap<ulong, Agnostic_GetSystemVAmd64PassStructInRegisterDescriptor>)_maps[109]; set => _maps[109] = value; }
    public LightWeightMap<ulong, Agnostic_GetSwiftLowering> GetSwiftLowering { get => (LightWeightMap<ulong, Agnostic_GetSwiftLowering>)_maps[110]; set => _maps[110] = value; }
    public LightWeightMap<ulong, Agnostic_GetFpStructLowering> GetFpStructLowering { get => (LightWeightMap<ulong, Agnostic_GetFpStructLowering>)_maps[111]; set => _maps[111] = value; }
    public LightWeightMap<ulong, uint> GetWasmLowering { get => (LightWeightMap<ulong, uint>)_maps[112]; set => _maps[112] = value; }
    public LightWeightMap<Agnostic_GetTailCallHelpers, Agnostic_CORINFO_TAILCALL_HELPERS> GetTailCallHelpers { get => (LightWeightMap<Agnostic_GetTailCallHelpers, Agnostic_CORINFO_TAILCALL_HELPERS>)_maps[113]; set => _maps[113] = value; }
    public LightWeightMap<uint, DLDL> GetAsyncResumptionStub { get => (LightWeightMap<uint, DLDL>)_maps[114]; set => _maps[114] = value; }
    public LightWeightMap<Agnostic_GetContinuationTypeIn, ulong> GetContinuationType { get => (LightWeightMap<Agnostic_GetContinuationTypeIn, ulong>)_maps[115]; set => _maps[115] = value; }
    public LightWeightMap<Agnostic_CORINFO_CONST_LOOKUP, Agnostic_CORINFO_CONST_LOOKUP> UpdateEntryPointForTailCall { get => (LightWeightMap<Agnostic_CORINFO_CONST_LOOKUP, Agnostic_CORINFO_CONST_LOOKUP>)_maps[116]; set => _maps[116] = value; }
    public LightWeightMap<ulong, ulong> GetSpecialCopyHelper { get => (LightWeightMap<ulong, ulong>)_maps[117]; set => _maps[117] = value; }
    public LightWeightMap<uint, DLD> GetThreadTLSIndex { get => (LightWeightMap<uint, DLD>)_maps[118]; set => _maps[118] = value; }
    public LightWeightMap<GetTokenTypeAsHandleValue, ulong> GetTokenTypeAsHandle { get => (LightWeightMap<GetTokenTypeAsHandleValue, ulong>)_maps[119]; set => _maps[119] = value; }
    public LightWeightMap<ulong, ulong> GetTypeForBox { get => (LightWeightMap<ulong, ulong>)_maps[120]; set => _maps[120] = value; }
    public LightWeightMap<ulong, uint> GetTypeForPrimitiveValueClass { get => (LightWeightMap<ulong, uint>)_maps[121]; set => _maps[121] = value; }
    public LightWeightMap<ulong, uint> GetTypeForPrimitiveNumericClass { get => (LightWeightMap<ulong, uint>)_maps[122]; set => _maps[122] = value; }
    public LightWeightMap<ulong, DLD> GetAsyncOtherVariant { get => (LightWeightMap<ulong, DLD>)_maps[123]; set => _maps[123] = value; }
    public LightWeightMap<ulong, uint> GetUnBoxHelper { get => (LightWeightMap<ulong, uint>)_maps[124]; set => _maps[124] = value; }
    public LightWeightMap<ulong, ulong> GetRuntimeTypePointer { get => (LightWeightMap<ulong, ulong>)_maps[125]; set => _maps[125] = value; }
    public LightWeightMap<ulong, uint> IsObjectImmutable { get => (LightWeightMap<ulong, uint>)_maps[126]; set => _maps[126] = value; }
    public LightWeightMap<DLD, DD> GetStringChar { get => (LightWeightMap<DLD, DD>)_maps[127]; set => _maps[127] = value; }
    public LightWeightMap<ulong, ulong> GetObjectType { get => (LightWeightMap<ulong, ulong>)_maps[128]; set => _maps[128] = value; }
    public LightWeightMap<GetVarArgsHandleValue, DLDL> GetVarArgsHandle { get => (LightWeightMap<GetVarArgsHandleValue, DLDL>)_maps[129]; set => _maps[129] = value; }
    public LightWeightMap<ulong, Agnostic_GetVars> GetVars { get => (LightWeightMap<ulong, Agnostic_GetVars>)_maps[130]; set => _maps[130] = value; }
    public LightWeightMap<Agnostic_InitClass, uint> InitClass { get => (LightWeightMap<Agnostic_InitClass, uint>)_maps[131]; set => _maps[131] = value; }
    public LightWeightMap<DLDL, uint> IsDelegateCreationAllowed { get => (LightWeightMap<DLDL, uint>)_maps[132]; set => _maps[132] = value; }
    public LightWeightMap<ulong, uint> IsFieldStatic { get => (LightWeightMap<ulong, uint>)_maps[133]; set => _maps[133] = value; }
    public LightWeightMap<ulong, uint> GetArrayOrStringLength { get => (LightWeightMap<ulong, uint>)_maps[134]; set => _maps[134] = value; }
    public LightWeightMap<Agnostic_ExpandRawHandleIntrinsic, Agnostic_CORINFO_GENERICHANDLE_RESULT> ExpandRawHandleIntrinsic { get => (LightWeightMap<Agnostic_ExpandRawHandleIntrinsic, Agnostic_CORINFO_GENERICHANDLE_RESULT>)_maps[135]; set => _maps[135] = value; }
    public LightWeightMap<ulong, uint> IsIntrinsicType { get => (LightWeightMap<ulong, uint>)_maps[136]; set => _maps[136] = value; }
    public LightWeightMap<ulong, uint> IsSDArray { get => (LightWeightMap<ulong, uint>)_maps[137]; set => _maps[137] = value; }
    public LightWeightMap<DLDDD, DD> GetStringLiteral { get => (LightWeightMap<DLDDD, DD>)_maps[138]; set => _maps[138] = value; }
    public LightWeightMap<ulong, Agnostic_PrintResult> PrintObjectDescription { get => (LightWeightMap<ulong, Agnostic_PrintResult>)_maps[139]; set => _maps[139] = value; }
    public LightWeightMap<ulong, Agnostic_PrintResult> PrintClassName { get => (LightWeightMap<ulong, Agnostic_PrintResult>)_maps[140]; set => _maps[140] = value; }
    public LightWeightMap<ulong, Agnostic_PrintResult> PrintFieldName { get => (LightWeightMap<ulong, Agnostic_PrintResult>)_maps[141]; set => _maps[141] = value; }
    public LightWeightMap<ulong, Agnostic_PrintResult> PrintMethodName { get => (LightWeightMap<ulong, Agnostic_PrintResult>)_maps[142]; set => _maps[142] = value; }
    public LightWeightMap<ulong, uint> IsValueClass { get => (LightWeightMap<ulong, uint>)_maps[143]; set => _maps[143] = value; }
    public LightWeightMap<DLDL, uint> IsMoreSpecificType { get => (LightWeightMap<DLDL, uint>)_maps[144]; set => _maps[144] = value; }
    public LightWeightMap<ulong, uint> IsExactType { get => (LightWeightMap<ulong, uint>)_maps[145]; set => _maps[145] = value; }
    public LightWeightMap<ulong, uint> IsGenericType { get => (LightWeightMap<ulong, uint>)_maps[146]; set => _maps[146] = value; }
    public LightWeightMap<ulong, uint> IsNullableType { get => (LightWeightMap<ulong, uint>)_maps[147]; set => _maps[147] = value; }
    public LightWeightMap<ulong, DLD> IsEnum { get => (LightWeightMap<ulong, DLD>)_maps[148]; set => _maps[148] = value; }
    public LightWeightMap<MethodOrSigInfoValue, uint> PInvokeMarshalingRequired { get => (LightWeightMap<MethodOrSigInfoValue, uint>)_maps[149]; set => _maps[149] = value; }
    public LightWeightMap<Agnostic_CORINFO_RESOLVED_TOKENin, ResolveTokenValue> ResolveToken { get => (LightWeightMap<Agnostic_CORINFO_RESOLVED_TOKENin, ResolveTokenValue>)_maps[150]; set => _maps[150] = value; }
    public LightWeightMap<Agnostic_ResolveVirtualMethodKey, Agnostic_ResolveVirtualMethodResult> ResolveVirtualMethod { get => (LightWeightMap<Agnostic_ResolveVirtualMethodKey, Agnostic_ResolveVirtualMethodResult>)_maps[151]; set => _maps[151] = value; }
    public LightWeightMap<DLDL, uint> SatisfiesMethodConstraints { get => (LightWeightMap<DLDL, uint>)_maps[152]; set => _maps[152] = value; }
    public LightWeightMap<MethodOrSigInfoValue, DD> GetUnmanagedCallConv { get => (LightWeightMap<MethodOrSigInfoValue, DD>)_maps[153]; set => _maps[153] = value; }
    public LightWeightMap<DLDL, uint> DoesFieldBelongToClass { get => (LightWeightMap<DLDL, uint>)_maps[154]; set => _maps[154] = value; }
    public DenseLightWeightMap<ulong> SigInstHandleMap { get => (DenseLightWeightMap<ulong>)_maps[155]; set => _maps[155] = value; }
    public LightWeightMap<Agnostic_GetWasmTypeSymbol, ulong> GetWasmTypeSymbol { get => (LightWeightMap<Agnostic_GetWasmTypeSymbol, ulong>)_maps[156]; set => _maps[156] = value; }
    public LightWeightMap<ulong, uint> GetAddressAlignment { get => (LightWeightMap<ulong, uint>)_maps[157]; set => _maps[157] = value; }
    public LightWeightMap<ulong, uint> CanValueClassInstancePointerEscape { get => (LightWeightMap<ulong, uint>)_maps[158]; set => _maps[158] = value; }
    public LightWeightMap<uint, Agnostic_CORINFO_WASM_WELLKNOWN_GLOBALS> GetWasmWellKnownGlobals { get => (LightWeightMap<uint, Agnostic_CORINFO_WASM_WELLKNOWN_GLOBALS>)_maps[159]; set => _maps[159] = value; }
    public LightWeightMap<ulong, uint> IsByRefLike { get => (LightWeightMap<ulong, uint>)_maps[160]; set => _maps[160] = value; }
    public LightWeightMap<ulong, uint> IsSharedInst { get => (LightWeightMap<ulong, uint>)_maps[161]; set => _maps[161] = value; }

    internal void dumpMaps()
    {
        MapDumper.Dump(nameof(AllocPgoInstrumentationBySchema), AllocPgoInstrumentationBySchema, (k, v) => dmpAllocPgoInstrumentationBySchema(k, v));
        MapDumper.Dump(nameof(GetPgoInstrumentationResults), GetPgoInstrumentationResults, (k, v) => dmpGetPgoInstrumentationResults(k, v));
        MapDumper.Dump(nameof(AsCorInfoType), AsCorInfoType, (k, v) => dmpAsCorInfoType(k, v));
        MapDumper.Dump(nameof(CanAccessClass), CanAccessClass, (k, v) => dmpCanAccessClass(k, v));
        MapDumper.Dump(nameof(CanCast), CanCast, (k, v) => dmpCanCast(k, v));
        MapDumper.Dump(nameof(CanInline), CanInline, (k, v) => dmpCanInline(k, v));
        MapDumper.Dump(nameof(CanTailCall), CanTailCall, (k, v) => dmpCanTailCall(k, v));
        MapDumper.Dump(nameof(CheckMethodModifier), CheckMethodModifier, (k, v) => dmpCheckMethodModifier(k, v));
        MapDumper.Dump(nameof(CompareTypesForCast), CompareTypesForCast, (k, v) => dmpCompareTypesForCast(k, v));
        MapDumper.Dump(nameof(CompareTypesForEquality), CompareTypesForEquality, (k, v) => dmpCompareTypesForEquality(k, v));
        MapDumper.Dump(nameof(CompileMethod), CompileMethod, (k, v) => dmpCompileMethod(k, v));
        MapDumper.Dump(nameof(ConstructStringLiteral), ConstructStringLiteral, (k, v) => dmpConstructStringLiteral(k, v));
        MapDumper.Dump(nameof(ConvertPInvokeCalliToCall), ConvertPInvokeCalliToCall, (k, v) => dmpConvertPInvokeCalliToCall(k, v));
        MapDumper.Dump(nameof(EmbedClassHandle), EmbedClassHandle, (k, v) => dmpEmbedClassHandle(k, v));
        MapDumper.Dump(nameof(EmbedFieldHandle), EmbedFieldHandle, (k, v) => dmpEmbedFieldHandle(k, v));
        MapDumper.Dump(nameof(EmbedGenericHandle), EmbedGenericHandle, (k, v) => dmpEmbedGenericHandle(k, v));
        MapDumper.Dump(nameof(EmbedMethodHandle), EmbedMethodHandle, (k, v) => dmpEmbedMethodHandle(k, v));
        MapDumper.Dump(nameof(EmbedModuleHandle), EmbedModuleHandle, (k, v) => dmpEmbedModuleHandle(k, v));
        MapDumper.Dump(nameof(GetThreadLocalFieldInfo), GetThreadLocalFieldInfo, (k, v) => dmpGetThreadLocalFieldInfo(k, v));
        MapDumper.Dump(nameof(GetThreadLocalStaticBlocksInfo), GetThreadLocalStaticBlocksInfo, (k, v) => dmpGetThreadLocalStaticBlocksInfo(k, v));
        MapDumper.Dump(nameof(GetThreadLocalStaticInfo_NativeAOT), GetThreadLocalStaticInfo_NativeAOT, (k, v) => dmpGetThreadLocalStaticInfo_NativeAOT(k, v));
        MapDumper.Dump(nameof(EmptyStringLiteral), EmptyStringLiteral, (k, v) => dmpEmptyStringLiteral(k, v));
        MapDumper.Dump(nameof(ErrorList), ErrorList, (k, v) => dmpErrorList(k, v));
        MapDumper.Dump(nameof(FindCallSiteSig), FindCallSiteSig, (k, v) => dmpFindCallSiteSig(k, v));
        MapDumper.Dump(nameof(FindSig), FindSig, (k, v) => dmpFindSig(k, v));
        MapDumper.Dump(nameof(GetAddressOfPInvokeTarget), GetAddressOfPInvokeTarget, (k, v) => dmpGetAddressOfPInvokeTarget(k, v));
        MapDumper.Dump(nameof(GetAddrOfCaptureThreadGlobal), GetAddrOfCaptureThreadGlobal, (k, v) => dmpGetAddrOfCaptureThreadGlobal(k, v));
        MapDumper.Dump(nameof(GetArgClass), GetArgClass, (k, v) => dmpGetArgClass(k, v));
        MapDumper.Dump(nameof(GetArgNext), GetArgNext, (k, v) => dmpGetArgNext(k, v));
        MapDumper.Dump(nameof(GetArgType), GetArgType, (k, v) => dmpGetArgType(k, v));
        MapDumper.Dump(nameof(GetExactClasses), GetExactClasses, (k, v) => dmpGetExactClasses(k, v));
        MapDumper.Dump(nameof(GetArrayInitializationData), GetArrayInitializationData, (k, v) => dmpGetArrayInitializationData(k, v));
        MapDumper.Dump(nameof(GetArrayRank), GetArrayRank, (k, v) => dmpGetArrayRank(k, v));
        MapDumper.Dump(nameof(GetArrayIntrinsicID), GetArrayIntrinsicID, (k, v) => dmpGetArrayIntrinsicID(k, v));
        MapDumper.Dump(nameof(GetBoundaries), GetBoundaries, (k, v) => dmpGetBoundaries(k, v));
        MapDumper.Dump(nameof(GetBoxHelper), GetBoxHelper, (k, v) => dmpGetBoxHelper(k, v));
        MapDumper.Dump(nameof(GetBuiltinClass), GetBuiltinClass, (k, v) => dmpGetBuiltinClass(k, v));
        MapDumper.Dump(nameof(GetCallInfo), GetCallInfo, (k, v) => dmpGetCallInfo(k, v));
        MapDumper.Dump(nameof(GetCastingHelper), GetCastingHelper, (k, v) => dmpGetCastingHelper(k, v));
        MapDumper.Dump(nameof(GetChildType), GetChildType, (k, v) => dmpGetChildType(k, v));
        MapDumper.Dump(nameof(GetClassAlignmentRequirement), GetClassAlignmentRequirement, (k, v) => dmpGetClassAlignmentRequirement(k, v));
        MapDumper.Dump(nameof(GetClassAttribs), GetClassAttribs, (k, v) => dmpGetClassAttribs(k, v));
        MapDumper.Dump(nameof(GetClassStaticDynamicInfo), GetClassStaticDynamicInfo, (k, v) => dmpGetClassStaticDynamicInfo(k, v));
        MapDumper.Dump(nameof(GetClassThreadStaticDynamicInfo), GetClassThreadStaticDynamicInfo, (k, v) => dmpGetClassThreadStaticDynamicInfo(k, v));
        MapDumper.Dump(nameof(GetClassGClayout), GetClassGClayout, (k, v) => dmpGetClassGClayout(k, v));
        MapDumper.Dump(nameof(GetClassModuleIdForStatics), GetClassModuleIdForStatics, (k, v) => dmpGetClassModuleIdForStatics(k, v));
        MapDumper.Dump(nameof(GetIsClassInitedFlagAddress), GetIsClassInitedFlagAddress, (k, v) => dmpGetIsClassInitedFlagAddress(k, v));
        MapDumper.Dump(nameof(GetStaticBaseAddress), GetStaticBaseAddress, (k, v) => dmpGetStaticBaseAddress(k, v));
        MapDumper.Dump(nameof(GetClassNameFromMetadata), GetClassNameFromMetadata, (k, v) => dmpGetClassNameFromMetadata(k, v));
        MapDumper.Dump(nameof(GetTypeInstantiationArgument), GetTypeInstantiationArgument, (k, v) => dmpGetTypeInstantiationArgument(k, v));
        MapDumper.Dump(nameof(GetMethodInstantiationArgument), GetMethodInstantiationArgument, (k, v) => dmpGetMethodInstantiationArgument(k, v));
        MapDumper.Dump(nameof(GetClassNumInstanceFields), GetClassNumInstanceFields, (k, v) => dmpGetClassNumInstanceFields(k, v));
        MapDumper.Dump(nameof(GetClassSize), GetClassSize, (k, v) => dmpGetClassSize(k, v));
        MapDumper.Dump(nameof(GetHeapClassSize), GetHeapClassSize, (k, v) => dmpGetHeapClassSize(k, v));
        MapDumper.Dump(nameof(CanAllocateOnStack), CanAllocateOnStack, (k, v) => dmpCanAllocateOnStack(k, v));
        MapDumper.Dump(nameof(GetCookieForInterpreterCalliSig), GetCookieForInterpreterCalliSig, (k, v) => dmpGetCookieForInterpreterCalliSig(k, v));
        MapDumper.Dump(nameof(GetDefaultComparerClass), GetDefaultComparerClass, (k, v) => dmpGetDefaultComparerClass(k, v));
        MapDumper.Dump(nameof(GetDefaultEqualityComparerClass), GetDefaultEqualityComparerClass, (k, v) => dmpGetDefaultEqualityComparerClass(k, v));
        MapDumper.Dump(nameof(GetSZArrayHelperEnumeratorClass), GetSZArrayHelperEnumeratorClass, (k, v) => dmpGetSZArrayHelperEnumeratorClass(k, v));
        MapDumper.Dump(nameof(GetDelegateCtor), GetDelegateCtor, (k, v) => dmpGetDelegateCtor(k, v));
        MapDumper.Dump(nameof(GetEEInfo), GetEEInfo, (k, v) => dmpGetEEInfo(k, v));
        MapDumper.Dump(nameof(GetAsyncInfo), GetAsyncInfo, (k, v) => dmpGetAsyncInfo(k, v));
        MapDumper.Dump(nameof(GetAwaitReturnCall), GetAwaitReturnCall, (k, v) => dmpGetAwaitReturnCall(k, v));
        MapDumper.Dump(nameof(GetAwaitAwaiterInContinuationCall), GetAwaitAwaiterInContinuationCall, (k, v) => dmpGetAwaitAwaiterInContinuationCall(k, v));
        MapDumper.Dump(nameof(GetEHinfo), GetEHinfo, (k, v) => dmpGetEHinfo(k, v));
        MapDumper.Dump(nameof(GetStaticFieldContent), GetStaticFieldContent, (k, v) => dmpGetStaticFieldContent(k, v));
        MapDumper.Dump(nameof(GetObjectContent), GetObjectContent, (k, v) => dmpGetObjectContent(k, v));
        MapDumper.Dump(nameof(GetStaticFieldCurrentClass), GetStaticFieldCurrentClass, (k, v) => dmpGetStaticFieldCurrentClass(k, v));
        MapDumper.Dump(nameof(GetFieldClass), GetFieldClass, (k, v) => dmpGetFieldClass(k, v));
        MapDumper.Dump(nameof(GetFieldInClass), GetFieldInClass, (k, v) => dmpGetFieldInClass(k, v));
        MapDumper.Dump(nameof(GetTypeLayout), GetTypeLayout, (k, v) => dmpGetTypeLayout(k, v));
        MapDumper.Dump(nameof(GetFieldInfo), GetFieldInfo, (k, v) => dmpGetFieldInfo(k, v));
        MapDumper.Dump(nameof(GetFieldOffset), GetFieldOffset, (k, v) => dmpGetFieldOffset(k, v));
        MapDumper.Dump(nameof(GetFieldThreadLocalStoreID), GetFieldThreadLocalStoreID, (k, v) => dmpGetFieldThreadLocalStoreID(k, v));
        MapDumper.Dump(nameof(GetFieldType), GetFieldType, (k, v) => dmpGetFieldType(k, v));
        MapDumper.Dump(nameof(GetFunctionEntryPoint), GetFunctionEntryPoint, (k, v) => dmpGetFunctionEntryPoint(k, v));
        MapDumper.Dump(nameof(GetFunctionFixedEntryPoint), GetFunctionFixedEntryPoint, (k, v) => dmpGetFunctionFixedEntryPoint(k, v));
        MapDumper.Dump(nameof(GetGSCookie), GetGSCookie, (k, v) => dmpGetGSCookie(k, v));
        MapDumper.Dump(nameof(GetHelperFtn), GetHelperFtn, (k, v) => dmpGetHelperFtn(k, v));
        MapDumper.Dump(nameof(GetHFAType), GetHFAType, (k, v) => dmpGetHFAType(k, v));
        MapDumper.Dump(nameof(GetIntConfigValue), GetIntConfigValue, (k, v) => dmpGetIntConfigValue(k, v));
        MapDumper.Dump(nameof(GetJitFlags), GetJitFlags, (k, v) => dmpGetJitFlags(k, v));
        MapDumper.Dump(nameof(GetJustMyCodeHandle), GetJustMyCodeHandle, (k, v) => dmpGetJustMyCodeHandle(k, v));
        MapDumper.Dump(nameof(GetLocationOfThisType), GetLocationOfThisType, (k, v) => dmpGetLocationOfThisType(k, v));
        MapDumper.Dump(nameof(IsIntrinsic), IsIntrinsic, (k, v) => dmpIsIntrinsic(k, v));
        MapDumper.Dump(nameof(NotifyMethodInfoUsage), NotifyMethodInfoUsage, (k, v) => dmpNotifyMethodInfoUsage(k, v));
        MapDumper.Dump(nameof(NotifyInstructionSetUsage), NotifyInstructionSetUsage, (k, v) => dmpNotifyInstructionSetUsage(k, v));
        MapDumper.Dump(nameof(GetMethodAttribs), GetMethodAttribs, (k, v) => dmpGetMethodAttribs(k, v));
        MapDumper.Dump(nameof(GetClassAssemblyName), GetClassAssemblyName, (k, v) => dmpGetClassAssemblyName(k, v));
        MapDumper.Dump(nameof(GetMethodClass), GetMethodClass, (k, v) => dmpGetMethodClass(k, v));
        MapDumper.Dump(nameof(GetMethodDefFromMethod), GetMethodDefFromMethod, (k, v) => dmpGetMethodDefFromMethod(k, v));
        MapDumper.Dump(nameof(GetMethodHash), GetMethodHash, (k, v) => dmpGetMethodHash(k, v));
        MapDumper.Dump(nameof(GetMethodInfo), GetMethodInfo, (k, v) => dmpGetMethodInfo(k, v));
        MapDumper.Dump(nameof(HaveSameMethodDefinition), HaveSameMethodDefinition, (k, v) => dmpHaveSameMethodDefinition(k, v));
        MapDumper.Dump(nameof(GetTypeDefinition), GetTypeDefinition, (k, v) => dmpGetTypeDefinition(k, v));
        MapDumper.Dump(nameof(GetMethodNameFromMetadata), GetMethodNameFromMetadata, (k, v) => dmpGetMethodNameFromMetadata(k, v));
        MapDumper.Dump(nameof(GetMethodSig), GetMethodSig, (k, v) => dmpGetMethodSig(k, v));
        MapDumper.Dump(nameof(GetMethodVTableOffset), GetMethodVTableOffset, (k, v) => dmpGetMethodVTableOffset(k, v));
        MapDumper.Dump(nameof(GetNewArrHelper), GetNewArrHelper, (k, v) => dmpGetNewArrHelper(k, v));
        MapDumper.Dump(nameof(GetNewHelper), GetNewHelper, (k, v) => dmpGetNewHelper(k, v));
        MapDumper.Dump(nameof(GetOSRInfo), GetOSRInfo, (k, v) => dmpGetOSRInfo(k, v));
        MapDumper.Dump(nameof(GetParentType), GetParentType, (k, v) => dmpGetParentType(k, v));
        MapDumper.Dump(nameof(GetProfilingHandle), GetProfilingHandle, (k, v) => dmpGetProfilingHandle(k, v));
        MapDumper.Dump(nameof(GetReadyToRunHelper), GetReadyToRunHelper, (k, v) => dmpGetReadyToRunHelper(k, v));
        MapDumper.Dump(nameof(GetReadyToRunDelegateCtorHelper), GetReadyToRunDelegateCtorHelper, (k, v) => dmpGetReadyToRunDelegateCtorHelper(k, v));
        MapDumper.Dump(nameof(GetRelocTypeHint), GetRelocTypeHint, (k, v) => dmpGetRelocTypeHint(k, v));
        MapDumper.Dump(nameof(GetExpectedTargetArchitecture), GetExpectedTargetArchitecture, (k, v) => dmpGetExpectedTargetArchitecture(k, v));
        MapDumper.Dump(nameof(GetSharedCCtorHelper), GetSharedCCtorHelper, (k, v) => dmpGetSharedCCtorHelper(k, v));
        MapDumper.Dump(nameof(GetStringConfigValue), GetStringConfigValue, (k, v) => dmpGetStringConfigValue(k, v));
        MapDumper.Dump(nameof(GetSystemVAmd64PassStructInRegisterDescriptor), GetSystemVAmd64PassStructInRegisterDescriptor, (k, v) => dmpGetSystemVAmd64PassStructInRegisterDescriptor(k, v));
        MapDumper.Dump(nameof(GetSwiftLowering), GetSwiftLowering, (k, v) => dmpGetSwiftLowering(k, v));
        MapDumper.Dump(nameof(GetFpStructLowering), GetFpStructLowering, (k, v) => dmpGetFpStructLowering(k, v));
        MapDumper.Dump(nameof(GetWasmLowering), GetWasmLowering, (k, v) => dmpGetWasmLowering(k, v));
        MapDumper.Dump(nameof(GetTailCallHelpers), GetTailCallHelpers, (k, v) => dmpGetTailCallHelpers(k, v));
        MapDumper.Dump(nameof(GetAsyncResumptionStub), GetAsyncResumptionStub, (k, v) => dmpGetAsyncResumptionStub(k, v));
        MapDumper.Dump(nameof(GetContinuationType), GetContinuationType, (k, v) => dmpGetContinuationType(k, v));
        MapDumper.Dump(nameof(UpdateEntryPointForTailCall), UpdateEntryPointForTailCall, (k, v) => dmpUpdateEntryPointForTailCall(k, v));
        MapDumper.Dump(nameof(GetSpecialCopyHelper), GetSpecialCopyHelper, (k, v) => dmpGetSpecialCopyHelper(k, v));
        MapDumper.Dump(nameof(GetThreadTLSIndex), GetThreadTLSIndex, (k, v) => dmpGetThreadTLSIndex(k, v));
        MapDumper.Dump(nameof(GetTokenTypeAsHandle), GetTokenTypeAsHandle, (k, v) => dmpGetTokenTypeAsHandle(k, v));
        MapDumper.Dump(nameof(GetTypeForBox), GetTypeForBox, (k, v) => dmpGetTypeForBox(k, v));
        MapDumper.Dump(nameof(GetTypeForPrimitiveValueClass), GetTypeForPrimitiveValueClass, (k, v) => dmpGetTypeForPrimitiveValueClass(k, v));
        MapDumper.Dump(nameof(GetTypeForPrimitiveNumericClass), GetTypeForPrimitiveNumericClass, (k, v) => dmpGetTypeForPrimitiveNumericClass(k, v));
        MapDumper.Dump(nameof(GetAsyncOtherVariant), GetAsyncOtherVariant, (k, v) => dmpGetAsyncOtherVariant(k, v));
        MapDumper.Dump(nameof(GetUnBoxHelper), GetUnBoxHelper, (k, v) => dmpGetUnBoxHelper(k, v));
        MapDumper.Dump(nameof(GetRuntimeTypePointer), GetRuntimeTypePointer, (k, v) => dmpGetRuntimeTypePointer(k, v));
        MapDumper.Dump(nameof(IsObjectImmutable), IsObjectImmutable, (k, v) => dmpIsObjectImmutable(k, v));
        MapDumper.Dump(nameof(GetStringChar), GetStringChar, (k, v) => dmpGetStringChar(k, v));
        MapDumper.Dump(nameof(GetObjectType), GetObjectType, (k, v) => dmpGetObjectType(k, v));
        MapDumper.Dump(nameof(GetVarArgsHandle), GetVarArgsHandle, (k, v) => dmpGetVarArgsHandle(k, v));
        MapDumper.Dump(nameof(GetVars), GetVars, (k, v) => dmpGetVars(k, v));
        MapDumper.Dump(nameof(InitClass), InitClass, (k, v) => dmpInitClass(k, v));
        MapDumper.Dump(nameof(IsDelegateCreationAllowed), IsDelegateCreationAllowed, (k, v) => dmpIsDelegateCreationAllowed(k, v));
        MapDumper.Dump(nameof(IsFieldStatic), IsFieldStatic, (k, v) => dmpIsFieldStatic(k, v));
        MapDumper.Dump(nameof(GetArrayOrStringLength), GetArrayOrStringLength, (k, v) => dmpGetArrayOrStringLength(k, v));
        MapDumper.Dump(nameof(ExpandRawHandleIntrinsic), ExpandRawHandleIntrinsic, (k, v) => dmpExpandRawHandleIntrinsic(k, v));
        MapDumper.Dump(nameof(IsIntrinsicType), IsIntrinsicType, (k, v) => dmpIsIntrinsicType(k, v));
        MapDumper.Dump(nameof(IsSDArray), IsSDArray, (k, v) => dmpIsSDArray(k, v));
        MapDumper.Dump(nameof(GetStringLiteral), GetStringLiteral, (k, v) => dmpGetStringLiteral(k, v));
        MapDumper.Dump(nameof(PrintObjectDescription), PrintObjectDescription, (k, v) => dmpPrintObjectDescription(k, v));
        MapDumper.Dump(nameof(PrintClassName), PrintClassName, (k, v) => dmpPrintClassName(k, v));
        MapDumper.Dump(nameof(PrintFieldName), PrintFieldName, (k, v) => dmpPrintFieldName(k, v));
        MapDumper.Dump(nameof(PrintMethodName), PrintMethodName, (k, v) => dmpPrintMethodName(k, v));
        MapDumper.Dump(nameof(IsValueClass), IsValueClass, (k, v) => dmpIsValueClass(k, v));
        MapDumper.Dump(nameof(IsMoreSpecificType), IsMoreSpecificType, (k, v) => dmpIsMoreSpecificType(k, v));
        MapDumper.Dump(nameof(IsExactType), IsExactType, (k, v) => dmpIsExactType(k, v));
        MapDumper.Dump(nameof(IsGenericType), IsGenericType, (k, v) => dmpIsGenericType(k, v));
        MapDumper.Dump(nameof(IsNullableType), IsNullableType, (k, v) => dmpIsNullableType(k, v));
        MapDumper.Dump(nameof(IsEnum), IsEnum, (k, v) => dmpIsEnum(k, v));
        MapDumper.Dump(nameof(PInvokeMarshalingRequired), PInvokeMarshalingRequired, (k, v) => dmpPInvokeMarshalingRequired(k, v));
        MapDumper.Dump(nameof(ResolveToken), ResolveToken, (k, v) => dmpResolveToken(k, v));
        MapDumper.Dump(nameof(ResolveVirtualMethod), ResolveVirtualMethod, (k, v) => dmpResolveVirtualMethod(k, v));
        MapDumper.Dump(nameof(SatisfiesMethodConstraints), SatisfiesMethodConstraints, (k, v) => dmpSatisfiesMethodConstraints(k, v));
        MapDumper.Dump(nameof(GetUnmanagedCallConv), GetUnmanagedCallConv, (k, v) => dmpGetUnmanagedCallConv(k, v));
        MapDumper.Dump(nameof(DoesFieldBelongToClass), DoesFieldBelongToClass, (k, v) => dmpDoesFieldBelongToClass(k, v));
        MapDumper.Dump(nameof(SigInstHandleMap), SigInstHandleMap, (k, v) => dmpSigInstHandleMap(k, v));
        MapDumper.Dump(nameof(GetWasmTypeSymbol), GetWasmTypeSymbol, (k, v) => dmpGetWasmTypeSymbol(k, v));
        MapDumper.Dump(nameof(GetAddressAlignment), GetAddressAlignment, (k, v) => dmpGetAddressAlignment(k, v));
        MapDumper.Dump(nameof(CanValueClassInstancePointerEscape), CanValueClassInstancePointerEscape, (k, v) => dmpCanValueClassInstancePointerEscape(k, v));
        MapDumper.Dump(nameof(GetWasmWellKnownGlobals), GetWasmWellKnownGlobals, (k, v) => dmpGetWasmWellKnownGlobals(k, v));
        MapDumper.Dump(nameof(IsByRefLike), IsByRefLike, (k, v) => dmpIsByRefLike(k, v));
        MapDumper.Dump(nameof(IsSharedInst), IsSharedInst, (k, v) => dmpIsSharedInst(k, v));
    }
}
