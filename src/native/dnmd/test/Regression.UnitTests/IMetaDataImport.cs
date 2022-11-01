
using System.Runtime.InteropServices;

namespace Regression.UnitTests
{
    [ComImport]
    [Guid("7DAC8207-D3AE-4c75-9B67-92801A497D44")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMetaDataImport
    {
        [PreserveSig]
        void CloseEnum(IntPtr hEnum);

        [PreserveSig]
        int CountEnum(IntPtr hEnum, out uint count);

        [PreserveSig]
        int ResetEnum(IntPtr hEnum, uint ulPos);

        [PreserveSig]
        int EnumTypeDefs(ref IntPtr phEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]uint[] rTypeDefs, int cMax, out uint pcTypeDefs);

        [PreserveSig]
        int EnumInterfaceImpls(ref IntPtr phEnum, uint td, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]uint[] rImpls, int cMax, out uint pcImpls);

        [PreserveSig]
        int EnumTypeRefs(ref IntPtr phEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]uint[] rTypeDefs, int cMax, out uint pcTypeRefs);

        [PreserveSig]
        int FindTypeDefByName([MarshalAs(UnmanagedType.LPWStr)]string szTypeDef, uint tkEnclosingClass, out uint ptd);

        [PreserveSig]
        int GetScopeProps([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]char[] szName, uint cchName, out uint pchName, ref Guid pmvid);

        [PreserveSig]
        int GetModuleFromScope(out uint pmd);

        [PreserveSig]
        int GetTypeDefProps(uint td, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]char[] szTypeDef, uint cchTypeDef, out uint pchTypeDef, out uint pdwTypeDefFlags, out uint ptkExtends);

        [PreserveSig]
        int GetInterfaceImplProps(uint iiImpl, out uint pClass, out uint ptkIface);

        [PreserveSig]
        int GetTypeRefProps(uint tr, out uint ptkResolutionScope, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]char[] szName, uint cchName, out uint pchName);

        [PreserveSig]
        int ResolveTypeRef(uint tr, ref Guid riid, [MarshalAs(UnmanagedType.Interface)]out object ppIScope, out uint ptd);

        [PreserveSig]
        int EnumMembers(ref IntPtr phEnum, uint cl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]uint[] rMembers, int cMax, out uint pcTokens);

        [PreserveSig]
        int EnumMembersWithName(ref IntPtr phEnum, uint cl, [MarshalAs(UnmanagedType.LPWStr)]string szName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]uint[] rMembers, int cMax, out uint pcTokens);

        [PreserveSig]
        int EnumMethods(ref IntPtr phEnum, uint cl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]uint[] rMethods, int cMax, out uint pcTokens);

        [PreserveSig]
        int EnumMethodsWithName(ref IntPtr phEnum, uint cl, [MarshalAs(UnmanagedType.LPWStr)]string szName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]uint[] rMethods, int cMax, out uint pcTokens);

        [PreserveSig]
        int EnumFields(ref IntPtr phEnum, uint cl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]uint[] rFields, int cMax, out uint pcTokens);

        [PreserveSig]
        int EnumFieldsWithName(ref IntPtr phEnum, uint cl, [MarshalAs(UnmanagedType.LPWStr)]string szName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]uint[] rFields, int cMax, out uint pcTokens);

        [PreserveSig]
        int EnumParams(ref IntPtr phEnum, uint mb, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]uint[] rParams, int cMax, out uint pcTokens);

        [PreserveSig]
        int EnumMemberRefs(ref IntPtr phEnum, uint tkParent, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]uint[] rMemberRefs, int cMax, out uint pcTokens);

        [PreserveSig]
        int EnumMethodImpls(ref IntPtr phEnum, uint td, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]uint[] rMethodBody, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]uint[] rMethodDecl, int cMax, out uint pcTokens);

        [PreserveSig]
        int EnumPermissionSets(ref IntPtr phEnum, uint tk, uint dwActions, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]uint[] rPermission, int cMax, out uint pcTokens);

        [PreserveSig]
        int FindMember(uint td, [MarshalAs(UnmanagedType.LPWStr)]string szName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]byte[] pvSigBlob, uint cbSigBlob, out uint pmb);

        [PreserveSig]
        int FindMethod(uint td, [MarshalAs(UnmanagedType.LPWStr)]string szName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]byte[] pvSigBlob, uint cbSigBlob, out uint pmb);

        [PreserveSig]
        int FindField(uint td, [MarshalAs(UnmanagedType.LPWStr)]string szName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]byte[] pvSigBlob, uint cbSigBlob, out uint pmb);

        [PreserveSig]
        int FindMemberRef(uint td, [MarshalAs(UnmanagedType.LPWStr)]string szName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]byte[] pvSigBlob, int cbSigBlob, out uint pmr);

        [PreserveSig]
        int GetMethodProps(uint mb, out uint pClass, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]char[] szMethod, uint cchMethod, out uint pchMethod, out uint pdwAttr, out IntPtr ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out uint pdwImplFlags);

        [PreserveSig]
        int GetMemberRefProps(uint mr, out uint ptk, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]char[] szMember, uint cchMember, out uint pchMember, out IntPtr ppvSigBlob, out uint pbSigBlob);

        [PreserveSig]
        int EnumProperties(ref IntPtr phEnum, uint td, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]uint[] rProperties, int cMax, out uint pcProperties);

        [PreserveSig]
        int EnumEvents(ref IntPtr phEnum, uint td, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]uint[] rEvents, int cMax, out uint pcEvents);

        [PreserveSig]
        int GetEventProps(uint ev, out uint pClass, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]char[] szEvent, uint cchEvent, out uint pchEvent, out uint pdwEventFlags, out uint ptkEventType, out uint pmdAddOn, out uint pmdRemoveOn, out uint pmdFire, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 10)]uint[] rmdOtherMethod, int cMax, out uint pcOtherMethod);

        [PreserveSig]
        int EnumMethodSemantics(ref IntPtr phEnum, uint mb, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]uint[] rEventProp, int cMax, out uint pcEventProp);

        [PreserveSig]
        int GetMethodSemantics(uint mb, uint tkEventProp, out uint pdwSemanticsFlags);

        [PreserveSig]
        int GetClassLayout(uint td, out uint pdwPackSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]long[] rFieldOffset, int cMax, out uint pcFieldOffset, out uint pulClassSize);

        [PreserveSig]
        int GetFieldMarshal(uint tk, out IntPtr ppvNativeType, out uint pcbNativeType);

        [PreserveSig]
        int GetRVA(uint tk, out uint pulCodeRVA, out uint pdwImplFlags);

        [PreserveSig]
        int GetPermissionSetProps(uint pm, out uint pdwAction, out IntPtr ppvPermission, out uint pcbPermission);

        [PreserveSig]
        int GetSigFromToken(uint mdSig, out IntPtr ppvSig, out uint pcbSig);

        [PreserveSig]
        int GetModuleRefProps(uint mur, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]char[] szName, uint cchName, out uint pchName);

        [PreserveSig]
        int EnumModuleRefs(ref IntPtr phEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]uint[] rModuleRefs, int cMax, out uint pcModuleRefs);

        [PreserveSig]
        int GetTypeSpecFromToken(uint typespec, out IntPtr ppvSig, out uint pcbSig);

        [PreserveSig]
        int GetNameFromToken(uint tk, out IntPtr pszUtf8NamePtr);

        [PreserveSig]
        int EnumUnresolvedMethods(ref uint phEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]uint[] rMethods, int cMax, out uint pcTokens);

        [PreserveSig]
        int GetUserString(uint stk, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] char[] szString, uint cchString, out uint pchString);

        [PreserveSig]
        int GetPinvokeMap(uint tk, out uint pdwMappingFlags, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]char[] szImportName, uint cchImportName, out uint pchImportName, out uint pmrImportDLL);

        [PreserveSig]
        int EnumSignatures(ref IntPtr phEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]uint[] rSignatures, int cMax, out uint pcSignatures);

        [PreserveSig]
        int EnumTypeSpecs(ref IntPtr phEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]uint[] rTypeSpecs, int cMax, out uint pcTypeSpecs);

        [PreserveSig]
        int EnumUserStrings(ref IntPtr phEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]uint[] rStrings, int cMax, out uint pcStrings);

        [PreserveSig]
        int GetParamForMethodIndex(uint md, uint ulParamSeq, out uint ppd);

        [PreserveSig]
        int EnumCustomAttributes(ref IntPtr phEnum, uint tk, uint tkType, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]uint[] rCustomAttributes, int cMax, out uint pcCustomAttributes);

        [PreserveSig]
        int GetCustomAttributeProps(uint cv, out uint ptkObj, out uint ptkType, out IntPtr ppBlob, out uint pcbSize);

        [PreserveSig]
        int FindTypeRef(uint tkResolutionScope, [MarshalAs(UnmanagedType.LPWStr)]string szName, out uint ptr);

        [PreserveSig]
        int GetMemberProps(uint mb, out uint pClass, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]char[] szMember, uint cchMember, out uint pchMember, out uint pdwAttr, out IntPtr ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out uint pdwImplFlags, out uint pdwCPlusTypeFlag, out IntPtr ppValue, out uint pcchValue);

        [PreserveSig]
        int GetFieldProps(uint mb, out uint pClass, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]char[] szField, uint cchField, out uint pchField, out uint pdwAttr, out IntPtr ppvSigBlob, out uint pcbSigBlob, out uint pdwCPlusTypeFlag, out IntPtr ppValue, out uint pcchValue);

        [PreserveSig]
        int GetPropertyProps(uint prop, out uint pClass, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]char[] szProperty, uint cchProperty, out uint pchProperty, out uint pdwPropFlags, out IntPtr ppvSig, out uint pbSig, out uint pdwCPlusTypeFlag, out IntPtr ppDefaultValue, out uint pcchDefaultValue, out uint pmdSetter, out uint pmdGetter, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 13)]uint[] rmdOtherMethod, int cMax, out uint pcOtherMethod);

        [PreserveSig]
        int GetParamProps(uint tk, out uint pmd, out uint pulSequence, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]char[] szName, uint cchName, out uint pchName, out uint pdwAttr, out uint pdwCPlusTypeFlag, out IntPtr ppValue, out uint pcchValue);

        [PreserveSig]
        int GetCustomAttributeByName(uint tkObj, [MarshalAs(UnmanagedType.LPWStr)]string szName, out IntPtr ppData, out uint pcbData);

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Bool)]
        bool IsValidToken(uint tk);

        [PreserveSig]
        int GetNestedClassProps(uint tdNestedClass, out uint ptdEnclosingClass);

        [PreserveSig]
        int GetNativeCallConvFromSig(IntPtr pvSig, uint cbSig, out uint pCallConv);

        [PreserveSig]
        int IsGlobal(uint pd, out uint pbGlobal);
    }
}
