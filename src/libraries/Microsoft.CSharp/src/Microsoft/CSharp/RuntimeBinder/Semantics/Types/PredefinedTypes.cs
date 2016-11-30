// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal enum CorElementType
    {
        ELEMENT_TYPE_U1,
        ELEMENT_TYPE_I2,
        ELEMENT_TYPE_I4,
        ELEMENT_TYPE_I8,
        ELEMENT_TYPE_R4,
        ELEMENT_TYPE_R8,
        ELEMENT_TYPE_CHAR,
        ELEMENT_TYPE_BOOLEAN,
        ELEMENT_TYPE_I1,
        ELEMENT_TYPE_U2,
        ELEMENT_TYPE_U4,
        ELEMENT_TYPE_U8,
        ELEMENT_TYPE_I,
        ELEMENT_TYPE_U,
        ELEMENT_TYPE_OBJECT,
        ELEMENT_TYPE_STRING,
        ELEMENT_TYPE_TYPEDBYREF,
        ELEMENT_TYPE_CLASS,
        ELEMENT_TYPE_VALUETYPE,
        ELEMENT_TYPE_END
    }


    internal class PredefinedTypes
    {
        private SymbolTable _runtimeBinderSymbolTable;
        private readonly BSYMMGR _pBSymmgr;
        private AggregateSymbol[] _predefSyms;    // array of predefined symbol types.
        private KAID _aidMsCorLib;        // The assembly ID for all predefined types.

        public PredefinedTypes(BSYMMGR pBSymmgr)
        {
            _pBSymmgr = pBSymmgr;
            _aidMsCorLib = KAID.kaidNil;
            _runtimeBinderSymbolTable = null;
        }

        // We want to delay load the predef syms as needed.
        private AggregateSymbol DelayLoadPredefSym(PredefinedType pt)
        {
            CType type = _runtimeBinderSymbolTable.GetCTypeFromType(PredefinedTypeFacts.GetAssociatedSystemType(pt));
            AggregateSymbol sym = type.getAggregate();

            // If we failed to load this thing, we have problems.
            if (sym == null)
            {
                return null;
            }
            return InitializePredefinedType(sym, pt);
        }

        internal static AggregateSymbol InitializePredefinedType(AggregateSymbol sym, PredefinedType pt)
        {
            sym.SetPredefined(true);
            sym.SetPredefType(pt);
            sym.SetSkipUDOps(pt <= PredefinedType.PT_ENUM && pt != PredefinedType.PT_INTPTR && pt != PredefinedType.PT_UINTPTR && pt != PredefinedType.PT_TYPE);

            return sym;
        }

        public bool Init(ErrorHandling errorContext, SymbolTable symtable)
        {
            _runtimeBinderSymbolTable = symtable;
            Debug.Assert(_pBSymmgr != null);

#if !CSEE
            Debug.Assert(_predefSyms == null);
#else // CSEE
            Debug.Assert(predefSyms == null || aidMsCorLib != KAID.kaidNil);
#endif // CSEE

            if (_aidMsCorLib == KAID.kaidNil)
            {
                // If we haven't found mscorlib yet, first look for System.Object. Then use its assembly as
                // the location for all other pre-defined types.
                AggregateSymbol aggObj = FindPredefinedType(errorContext, PredefinedTypeFacts.GetName(PredefinedType.PT_OBJECT), KAID.kaidGlobal, AggKindEnum.Class, 0, true);
                if (aggObj == null)
                    return false;
                _aidMsCorLib = aggObj.GetAssemblyID();
            }

            _predefSyms = new AggregateSymbol[(int)PredefinedType.PT_COUNT];
            Debug.Assert(_predefSyms != null);

            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // finds an existing declaration for a predefined type.
        // returns null on failure. If isRequired is true, an error message is also 
        // given.

        private static readonly char[] s_nameSeparators = new char[] { '.' };

        private AggregateSymbol FindPredefinedType(ErrorHandling errorContext, string pszType, KAID aid, AggKindEnum aggKind, int arity, bool isRequired)
        {
            Debug.Assert(!string.IsNullOrEmpty(pszType)); // Shouldn't be the empty string!

            NamespaceOrAggregateSymbol bagCur = _pBSymmgr.GetRootNS();
            Name name = null;

            string[] nameParts = pszType.Split(s_nameSeparators);
            for (int i = 0, n = nameParts.Length; i < n; i++)
            {
                name = _pBSymmgr.GetNameManager().Add(nameParts[i]);

                if (i == n - 1)
                {
                    // This is the last component. Handle it special below.
                    break;
                }

                // first search for an outer type which is also predefined
                // this must be first because we always create a namespace for
                // outer names, even for nested types
                AggregateSymbol aggNext = _pBSymmgr.LookupGlobalSymCore(name, bagCur, symbmask_t.MASK_AggregateSymbol).AsAggregateSymbol();
                if (aggNext != null && aggNext.InAlias(aid) && aggNext.IsPredefined())
                {
                    bagCur = aggNext;
                }
                else
                {
                    // ... if no outer type, then search for namespaces
                    NamespaceSymbol nsNext = _pBSymmgr.LookupGlobalSymCore(name, bagCur, symbmask_t.MASK_NamespaceSymbol).AsNamespaceSymbol();
                    bool bIsInAlias = true;
                    if (nsNext == null)
                    {
                        bIsInAlias = false;
                    }
                    else
                    {
                        bIsInAlias = nsNext.InAlias(aid);
                    }
                    if (!bIsInAlias)
                    {
                        // Didn't find the namespace in this aid.
                        if (isRequired)
                        {
                            errorContext.Error(ErrorCode.ERR_PredefinedTypeNotFound, pszType);
                        }
                        return null;
                    }
                    bagCur = nsNext;
                }
            }

            AggregateSymbol aggAmbig;
            AggregateSymbol aggBad;
            AggregateSymbol aggFound = FindPredefinedTypeCore(name, bagCur, aid, aggKind, arity, out aggAmbig, out aggBad);

            if (aggFound == null)
            {
                // Didn't find the AggregateSymbol.
                if (aggBad != null && (isRequired || aid == KAID.kaidGlobal && aggBad.IsSource()))
                    errorContext.ErrorRef(ErrorCode.ERR_PredefinedTypeBadType, aggBad);
                else if (isRequired)
                    errorContext.Error(ErrorCode.ERR_PredefinedTypeNotFound, pszType);
                return null;
            }

            if (aggAmbig == null && aid != KAID.kaidGlobal)
            {
                // Look in kaidGlobal to make sure there isn't a conflicting one.
                AggregateSymbol tmp;
                AggregateSymbol agg2 = FindPredefinedTypeCore(name, bagCur, KAID.kaidGlobal, aggKind, arity, out aggAmbig, out tmp);
                Debug.Assert(agg2 != null);
                if (agg2 != aggFound)
                    aggAmbig = agg2;
            }

            return aggFound;
        }

        private AggregateSymbol FindPredefinedTypeCore(Name name, NamespaceOrAggregateSymbol bag, KAID aid, AggKindEnum aggKind, int arity,
                out AggregateSymbol paggAmbig, out AggregateSymbol paggBad)
        {
            AggregateSymbol aggFound = null;
            paggAmbig = null;
            paggBad = null;

            for (AggregateSymbol aggCur = _pBSymmgr.LookupGlobalSymCore(name, bag, symbmask_t.MASK_AggregateSymbol).AsAggregateSymbol();
                 aggCur != null;
                 aggCur = BSYMMGR.LookupNextSym(aggCur, bag, symbmask_t.MASK_AggregateSymbol).AsAggregateSymbol())
            {
                if (!aggCur.InAlias(aid) || aggCur.GetTypeVarsAll().size != arity)
                {
                    continue;
                }
                if (aggCur.AggKind() != aggKind)
                {
                    if (paggBad == null)
                    {
                        paggBad = aggCur;
                    }
                    continue;
                }
                if (aggFound != null)
                {
                    Debug.Assert(paggAmbig == null);
                    paggAmbig = aggCur;
                    break;
                }
                aggFound = aggCur;
                if (paggAmbig == null)
                {
                    break;
                }
            }

            return aggFound;
        }

        public void ReportMissingPredefTypeError(ErrorHandling errorContext, PredefinedType pt)
        {
            Debug.Assert(_pBSymmgr != null);
            Debug.Assert(_predefSyms != null);
            Debug.Assert((PredefinedType)0 <= pt && pt < PredefinedType.PT_COUNT && _predefSyms[(int)pt] == null);

            // We do not assert that !predefTypeInfo[pt].isRequired because if the user is defining
            // their own MSCorLib and is defining a required PredefType, they'll run into this error
            // and we need to allow it to go through.

            errorContext.Error(ErrorCode.ERR_PredefinedTypeNotFound, PredefinedTypeFacts.GetName(pt));
        }

        public AggregateSymbol GetReqPredefAgg(PredefinedType pt)
        {
            if (!PredefinedTypeFacts.IsRequired(pt)) throw Error.InternalCompilerError();
            if (_predefSyms[(int)pt] == null)
            {
                // Delay load this thing.
                _predefSyms[(int)pt] = DelayLoadPredefSym(pt);
            }
            return _predefSyms[(int)pt];
        }

        public AggregateSymbol GetOptPredefAgg(PredefinedType pt)
        {
            if (_predefSyms[(int)pt] == null)
            {
                // Delay load this thing.
                _predefSyms[(int)pt] = DelayLoadPredefSym(pt);
            }

            Debug.Assert(_predefSyms != null);
            return _predefSyms[(int)pt];
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Some of the predefined types have built-in names, like "int" or "string" or
        // "object". This return the nice name if one exists; otherwise null is 
        // returned.

        public static string GetNiceName(PredefinedType pt)
        {
            return PredefinedTypeFacts.GetNiceName(pt);
        }

        public static string GetNiceName(AggregateSymbol type)
        {
            if (type.IsPredefined())
                return GetNiceName(type.GetPredefType());
            else
                return null;
        }

        public static string GetFullName(PredefinedType pt)
        {
            return PredefinedTypeFacts.GetName(pt);
        }

        public static bool isRequired(PredefinedType pt)
        {
            return PredefinedTypeFacts.IsRequired(pt);
        }
    }

    internal static class PredefinedTypeFacts
    {
        internal static string GetName(PredefinedType type)
        {
            return s_pdTypes[(int)type].name;
        }

        internal static bool IsRequired(PredefinedType type)
        {
            return s_pdTypes[(int)type].required;
        }

        internal static FUNDTYPE GetFundType(PredefinedType type)
        {
            return s_pdTypes[(int)type].fundType;
        }

        internal static Type GetAssociatedSystemType(PredefinedType type)
        {
            return s_pdTypes[(int)type].AssociatedSystemType;
        }

        internal static bool IsSimpleType(PredefinedType type)
        {
            switch (type)
            {
                case PredefinedType.PT_BYTE:
                case PredefinedType.PT_SHORT:
                case PredefinedType.PT_INT:
                case PredefinedType.PT_LONG:
                case PredefinedType.PT_FLOAT:
                case PredefinedType.PT_DOUBLE:
                case PredefinedType.PT_DECIMAL:
                case PredefinedType.PT_CHAR:
                case PredefinedType.PT_BOOL:
                case PredefinedType.PT_SBYTE:
                case PredefinedType.PT_USHORT:
                case PredefinedType.PT_UINT:
                case PredefinedType.PT_ULONG:
                    return true;
                default:
                    return false;
            }
        }

        internal static bool IsNumericType(PredefinedType type)
        {
            switch (type)
            {
                case PredefinedType.PT_BYTE:
                case PredefinedType.PT_SHORT:
                case PredefinedType.PT_INT:
                case PredefinedType.PT_LONG:
                case PredefinedType.PT_FLOAT:
                case PredefinedType.PT_DOUBLE:
                case PredefinedType.PT_DECIMAL:
                case PredefinedType.PT_SBYTE:
                case PredefinedType.PT_USHORT:
                case PredefinedType.PT_UINT:
                case PredefinedType.PT_ULONG:
                    return true;
                default:
                    return false;
            }
        }

        internal static string GetNiceName(PredefinedType type)
        {
            switch (type)
            {
                case PredefinedType.PT_BYTE:
                    return "byte";
                case PredefinedType.PT_SHORT:
                    return "short";
                case PredefinedType.PT_INT:
                    return "int";
                case PredefinedType.PT_LONG:
                    return "long";
                case PredefinedType.PT_FLOAT:
                    return "float";
                case PredefinedType.PT_DOUBLE:
                    return "double";
                case PredefinedType.PT_DECIMAL:
                    return "decimal";
                case PredefinedType.PT_CHAR:
                    return "char";
                case PredefinedType.PT_BOOL:
                    return "bool";
                case PredefinedType.PT_SBYTE:
                    return "sbyte";
                case PredefinedType.PT_USHORT:
                    return "ushort";
                case PredefinedType.PT_UINT:
                    return "uint";
                case PredefinedType.PT_ULONG:
                    return "ulong";
                case PredefinedType.PT_OBJECT:
                    return "object";
                case PredefinedType.PT_STRING:
                    return "string";
                default:
                    return null;
            }
        }

        internal static bool IsPredefinedType(string name)
        {
            return s_pdTypeNames.ContainsKey(name);
        }

        internal static PredefinedType GetPredefTypeIndex(string name)
        {
            return s_pdTypeNames[name];
        }

        private class PredefinedTypeInfo
        {
            internal readonly PredefinedType type;
            internal readonly string name;
            internal readonly bool required;
            internal readonly FUNDTYPE fundType;
            internal readonly Type AssociatedSystemType;

            internal PredefinedTypeInfo(PredefinedType type, Type associatedSystemType, string name, bool required, int arity, AggKindEnum aggKind, FUNDTYPE fundType, bool inMscorlib)
            {
                this.type = type;
                this.name = name;
                this.required = required;
                this.fundType = fundType;
                AssociatedSystemType = associatedSystemType;
            }

            internal PredefinedTypeInfo(PredefinedType type, Type associatedSystemType, string name, bool required, int arity, bool inMscorlib)
                : this(type, associatedSystemType, name, required, arity, AggKindEnum.Class, FUNDTYPE.FT_REF, inMscorlib)
            {
            }
        }

        private static readonly PredefinedTypeInfo[] s_pdTypes = new PredefinedTypeInfo[] {
            new PredefinedTypeInfo(PredefinedType.PT_BYTE,   typeof(Byte), "System.Byte", true, 0, AggKindEnum.Struct, FUNDTYPE.FT_U1, true),
            new PredefinedTypeInfo(PredefinedType.PT_SHORT,  typeof(Int16), "System.Int16", true, 0, AggKindEnum.Struct, FUNDTYPE.FT_I2, true),
            new PredefinedTypeInfo(PredefinedType.PT_INT,    typeof(Int32), "System.Int32", true, 0, AggKindEnum.Struct, FUNDTYPE.FT_I4, true),
            new PredefinedTypeInfo(PredefinedType.PT_LONG,   typeof(Int64), "System.Int64", true, 0, AggKindEnum.Struct, FUNDTYPE.FT_I8, true),
            new PredefinedTypeInfo(PredefinedType.PT_FLOAT,  typeof(Single), "System.Single", true, 0, AggKindEnum.Struct, FUNDTYPE.FT_R4, true),
            new PredefinedTypeInfo(PredefinedType.PT_DOUBLE, typeof(Double), "System.Double", true, 0, AggKindEnum.Struct, FUNDTYPE.FT_R8, true),
            new PredefinedTypeInfo(PredefinedType.PT_DECIMAL, typeof(Decimal), "System.Decimal", false, 0, AggKindEnum.Struct, FUNDTYPE.FT_STRUCT, true),
            new PredefinedTypeInfo(PredefinedType.PT_CHAR,   typeof(Char), "System.Char", true, 0, AggKindEnum.Struct, FUNDTYPE.FT_U2, true),
            new PredefinedTypeInfo(PredefinedType.PT_BOOL,   typeof(Boolean), "System.Boolean", true, 0, AggKindEnum.Struct, FUNDTYPE.FT_I1, true),
            new PredefinedTypeInfo(PredefinedType.PT_SBYTE,  typeof(SByte), "System.SByte", true, 0, AggKindEnum.Struct, FUNDTYPE.FT_I1, true),
            new PredefinedTypeInfo(PredefinedType.PT_USHORT, typeof(UInt16), "System.UInt16", true, 0, AggKindEnum.Struct, FUNDTYPE.FT_U2, true),
            new PredefinedTypeInfo(PredefinedType.PT_UINT,   typeof(UInt32), "System.UInt32", true, 0, AggKindEnum.Struct, FUNDTYPE.FT_U4, true),
            new PredefinedTypeInfo(PredefinedType.PT_ULONG,  typeof(UInt64), "System.UInt64", true, 0, AggKindEnum.Struct, FUNDTYPE.FT_U8, true),
            new PredefinedTypeInfo(PredefinedType.PT_INTPTR,  typeof(IntPtr), "System.IntPtr", true, 0, AggKindEnum.Struct, FUNDTYPE.FT_STRUCT, true),
            new PredefinedTypeInfo(PredefinedType.PT_UINTPTR, typeof(UIntPtr), "System.UIntPtr", true, 0, AggKindEnum.Struct, FUNDTYPE.FT_STRUCT, true),
            new PredefinedTypeInfo(PredefinedType.PT_OBJECT, typeof(Object), "System.Object", true, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_STRING, typeof(String), "System.String", true, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_DELEGATE, typeof(Delegate), "System.Delegate", true, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_MULTIDEL, typeof(MulticastDelegate), "System.MulticastDelegate", true, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_ARRAY,   typeof(Array), "System.Array", true, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_EXCEPTION, typeof(Exception), "System.Exception", true, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_TYPE, typeof(Type), "System.Type", true, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_MONITOR, typeof(System.Threading.Monitor), "System.Threading.Monitor", true, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_VALUE,   typeof(ValueType), "System.ValueType", true, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_ENUM,    typeof(Enum), "System.Enum", true, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_DATETIME,    typeof(DateTime), "System.DateTime", true, 0, AggKindEnum.Struct, FUNDTYPE.FT_STRUCT, true),
            new PredefinedTypeInfo(PredefinedType.PT_DEBUGGABLEATTRIBUTE, typeof(DebuggableAttribute), "System.Diagnostics.DebuggableAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_DEBUGGABLEATTRIBUTE_DEBUGGINGMODES, typeof(DebuggableAttribute.DebuggingModes), "System.Diagnostics.DebuggableAttribute.DebuggingModes", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_IN,            typeof(System.Runtime.InteropServices.InAttribute), "System.Runtime.InteropServices.InAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_OUT,           typeof(System.Runtime.InteropServices.OutAttribute), "System.Runtime.InteropServices.OutAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_ATTRIBUTE, typeof(Attribute), "System.Attribute", true, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_ATTRIBUTEUSAGE, typeof(AttributeUsageAttribute), "System.AttributeUsageAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_ATTRIBUTETARGETS, typeof(AttributeTargets), "System.AttributeTargets", false, 0, AggKindEnum.Enum, FUNDTYPE.FT_STRUCT, true),
            new PredefinedTypeInfo(PredefinedType.PT_OBSOLETE, typeof(ObsoleteAttribute), "System.ObsoleteAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_CONDITIONAL, typeof(ConditionalAttribute), "System.Diagnostics.ConditionalAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_CLSCOMPLIANT, typeof(CLSCompliantAttribute), "System.CLSCompliantAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_GUID, typeof(System.Runtime.InteropServices.GuidAttribute), "System.Runtime.InteropServices.GuidAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_DEFAULTMEMBER, typeof(System.Reflection.DefaultMemberAttribute), "System.Reflection.DefaultMemberAttribute", true, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_PARAMS, typeof(ParamArrayAttribute), "System.ParamArrayAttribute", true, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_COMIMPORT, typeof(System.Runtime.InteropServices.ComImportAttribute), "System.Runtime.InteropServices.ComImportAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_FIELDOFFSET, typeof(System.Runtime.InteropServices.FieldOffsetAttribute), "System.Runtime.InteropServices.FieldOffsetAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_STRUCTLAYOUT, typeof(System.Runtime.InteropServices.StructLayoutAttribute), "System.Runtime.InteropServices.StructLayoutAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_LAYOUTKIND, typeof(System.Runtime.InteropServices.LayoutKind), "System.Runtime.InteropServices.LayoutKind", false, 0, AggKindEnum.Enum, FUNDTYPE.FT_STRUCT, true),
            new PredefinedTypeInfo(PredefinedType.PT_MARSHALAS, typeof(System.Runtime.InteropServices.MarshalAsAttribute), "System.Runtime.InteropServices.MarshalAsAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_DLLIMPORT, typeof(System.Runtime.InteropServices.DllImportAttribute), "System.Runtime.InteropServices.DllImportAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_INDEXERNAME, typeof(System.Runtime.CompilerServices.IndexerNameAttribute), "System.Runtime.CompilerServices.IndexerNameAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_DECIMALCONSTANT, typeof(System.Runtime.CompilerServices.DecimalConstantAttribute), "System.Runtime.CompilerServices.DecimalConstantAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_DEFAULTVALUE, typeof(System.Runtime.InteropServices.DefaultParameterValueAttribute), "System.Runtime.InteropServices.DefaultParameterValueAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_UNMANAGEDFUNCTIONPOINTER, typeof(System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute), "System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_CALLINGCONVENTION, typeof(System.Runtime.InteropServices.CallingConvention), "System.Runtime.InteropServices.CallingConvention", false, 0, AggKindEnum.Enum, FUNDTYPE.FT_I4, true),
            new PredefinedTypeInfo(PredefinedType.PT_CHARSET, typeof(System.Runtime.InteropServices.CharSet), "System.Runtime.InteropServices.CharSet", false, 0, AggKindEnum.Enum, FUNDTYPE.FT_STRUCT, true),
            new PredefinedTypeInfo(PredefinedType.PT_TYPEHANDLE, typeof(RuntimeTypeHandle), "System.RuntimeTypeHandle", true, 0, AggKindEnum.Struct, FUNDTYPE.FT_STRUCT, true),
            new PredefinedTypeInfo(PredefinedType.PT_FIELDHANDLE, typeof(RuntimeFieldHandle), "System.RuntimeFieldHandle", true, 0, AggKindEnum.Struct, FUNDTYPE.FT_STRUCT, true),
            new PredefinedTypeInfo(PredefinedType.PT_METHODHANDLE, typeof(RuntimeMethodHandle), "System.RuntimeMethodHandle", false, 0, AggKindEnum.Struct, FUNDTYPE.FT_STRUCT, true),
            new PredefinedTypeInfo(PredefinedType.PT_G_DICTIONARY, typeof(Dictionary<,>), "System.Collections.Generic.Dictionary`2", false, 2, true),
            new PredefinedTypeInfo(PredefinedType.PT_IASYNCRESULT, typeof(IAsyncResult), "System.IAsyncResult", false, 0, AggKindEnum.Interface, FUNDTYPE.FT_REF, true),
            new PredefinedTypeInfo(PredefinedType.PT_ASYNCCBDEL, typeof(AsyncCallback), "System.AsyncCallback",  false, 0, AggKindEnum.Delegate, FUNDTYPE.FT_REF, true),
            new PredefinedTypeInfo(PredefinedType.PT_IDISPOSABLE, typeof(IDisposable), "System.IDisposable",   true, 0, AggKindEnum.Interface, FUNDTYPE.FT_REF, true),
            new PredefinedTypeInfo(PredefinedType.PT_IENUMERABLE, typeof(System.Collections.IEnumerable), "System.Collections.IEnumerable", true, 0, AggKindEnum.Interface, FUNDTYPE.FT_REF, true),
            new PredefinedTypeInfo(PredefinedType.PT_IENUMERATOR, typeof(System.Collections.IEnumerator), "System.Collections.IEnumerator", true, 0, AggKindEnum.Interface, FUNDTYPE.FT_REF, true),
            new PredefinedTypeInfo(PredefinedType.PT_SYSTEMVOID, typeof(void), "System.Void", true, 0, AggKindEnum.Struct, FUNDTYPE.FT_STRUCT, true),
            new PredefinedTypeInfo(PredefinedType.PT_RUNTIMEHELPERS, typeof(System.Runtime.CompilerServices.RuntimeHelpers), "System.Runtime.CompilerServices.RuntimeHelpers", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_VOLATILEMOD, typeof(System.Runtime.CompilerServices.IsVolatile), "System.Runtime.CompilerServices.IsVolatile", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_COCLASS,    typeof(System.Runtime.InteropServices.CoClassAttribute), "System.Runtime.InteropServices.CoClassAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_ACTIVATOR,  typeof(Activator), "System.Activator",  false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_G_IENUMERABLE, typeof(IEnumerable<>), "System.Collections.Generic.IEnumerable`1", false, 1, AggKindEnum.Interface, FUNDTYPE.FT_REF, true),
            new PredefinedTypeInfo(PredefinedType.PT_G_IENUMERATOR, typeof(IEnumerator<>), "System.Collections.Generic.IEnumerator`1", false, 1, AggKindEnum.Interface, FUNDTYPE.FT_REF, true),
            new PredefinedTypeInfo(PredefinedType.PT_G_OPTIONAL, typeof(Nullable<>), "System.Nullable`1",  false, 1, AggKindEnum.Struct, FUNDTYPE.FT_STRUCT, true),
            new PredefinedTypeInfo(PredefinedType.PT_FIXEDBUFFER, typeof(System.Runtime.CompilerServices.FixedBufferAttribute), "System.Runtime.CompilerServices.FixedBufferAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_DEFAULTCHARSET, typeof(System.Runtime.InteropServices.DefaultCharSetAttribute), "System.Runtime.InteropServices.DefaultCharSetAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_COMPILATIONRELAXATIONS, typeof(System.Runtime.CompilerServices.CompilationRelaxationsAttribute), "System.Runtime.CompilerServices.CompilationRelaxationsAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_RUNTIMECOMPATIBILITY, typeof(System.Runtime.CompilerServices.RuntimeCompatibilityAttribute), "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_FRIENDASSEMBLY, typeof(System.Runtime.CompilerServices.InternalsVisibleToAttribute), "System.Runtime.CompilerServices.InternalsVisibleToAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_DEBUGGERHIDDEN, typeof(DebuggerHiddenAttribute), "System.Diagnostics.DebuggerHiddenAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_TYPEFORWARDER, typeof(System.Runtime.CompilerServices.TypeForwardedToAttribute), "System.Runtime.CompilerServices.TypeForwardedToAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_KEYFILE, typeof(System.Reflection.AssemblyKeyFileAttribute), "System.Reflection.AssemblyKeyFileAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_KEYNAME, typeof(System.Reflection.AssemblyKeyNameAttribute), "System.Reflection.AssemblyKeyNameAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_DELAYSIGN, typeof(System.Reflection.AssemblyDelaySignAttribute), "System.Reflection.AssemblyDelaySignAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_NOTSUPPORTEDEXCEPTION, typeof(NotSupportedException), "System.NotSupportedException", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_COMPILERGENERATED, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), "System.Runtime.CompilerServices.CompilerGeneratedAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_UNSAFEVALUETYPE, typeof(System.Runtime.CompilerServices.UnsafeValueTypeAttribute), "System.Runtime.CompilerServices.UnsafeValueTypeAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_ASSEMBLYFLAGS, typeof(System.Reflection.AssemblyFlagsAttribute), "System.Reflection.AssemblyFlagsAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_ASSEMBLYVERSION, typeof(System.Reflection.AssemblyVersionAttribute), "System.Reflection.AssemblyVersionAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_ASSEMBLYCULTURE, typeof(System.Reflection.AssemblyCultureAttribute), "System.Reflection.AssemblyCultureAttribute", false, 0, true),
            // LINQ
            new PredefinedTypeInfo(PredefinedType.PT_G_IQUERYABLE, typeof(System.Linq.IQueryable<>), "System.Linq.IQueryable`1", false, 1, AggKindEnum.Interface, FUNDTYPE.FT_REF, false),
            new PredefinedTypeInfo(PredefinedType.PT_IQUERYABLE, typeof(System.Linq.IQueryable), "System.Linq.IQueryable", false, 0, AggKindEnum.Interface, FUNDTYPE.FT_REF, false),
            new PredefinedTypeInfo(PredefinedType.PT_STRINGBUILDER, typeof(System.Text.StringBuilder), "System.Text.StringBuilder", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_G_ICOLLECTION, typeof(ICollection<>), "System.Collections.Generic.ICollection`1", false, 1, AggKindEnum.Interface, FUNDTYPE.FT_REF, true),
            new PredefinedTypeInfo(PredefinedType.PT_G_ILIST, typeof(IList<>), "System.Collections.Generic.IList`1", false, 1, AggKindEnum.Interface, FUNDTYPE.FT_REF, true),
            new PredefinedTypeInfo(PredefinedType.PT_EXTENSION, typeof(System.Runtime.CompilerServices.ExtensionAttribute), "System.Runtime.CompilerServices.ExtensionAttribute", false, 0, false),
            new PredefinedTypeInfo(PredefinedType.PT_G_EXPRESSION, typeof(System.Linq.Expressions.Expression<>), "System.Linq.Expressions.Expression`1", false, 1, false),
            new PredefinedTypeInfo(PredefinedType.PT_EXPRESSION, typeof(System.Linq.Expressions.Expression), "System.Linq.Expressions.Expression", false, 0, false),
            new PredefinedTypeInfo(PredefinedType.PT_LAMBDAEXPRESSION, typeof(System.Linq.Expressions.LambdaExpression), "System.Linq.Expressions.LambdaExpression", false, 0, false),
            new PredefinedTypeInfo(PredefinedType.PT_BINARYEXPRESSION, typeof(System.Linq.Expressions.BinaryExpression), "System.Linq.Expressions.BinaryExpression", false, 0, false),
            new PredefinedTypeInfo(PredefinedType.PT_UNARYEXPRESSION, typeof(System.Linq.Expressions.UnaryExpression), "System.Linq.Expressions.UnaryExpression", false, 0, false),
            new PredefinedTypeInfo(PredefinedType.PT_CONDITIONALEXPRESSION, typeof(System.Linq.Expressions.ConditionalExpression), "System.Linq.Expressions.ConditionalExpression", false, 0, false),
            new PredefinedTypeInfo(PredefinedType.PT_CONSTANTEXPRESSION, typeof(System.Linq.Expressions.ConstantExpression), "System.Linq.Expressions.ConstantExpression", false, 0, false),
            new PredefinedTypeInfo(PredefinedType.PT_PARAMETEREXPRESSION, typeof(System.Linq.Expressions.ParameterExpression), "System.Linq.Expressions.ParameterExpression", false, 0, false),
            new PredefinedTypeInfo(PredefinedType.PT_MEMBEREXPRESSION, typeof(System.Linq.Expressions.MemberExpression), "System.Linq.Expressions.MemberExpression", false, 0, false),
            new PredefinedTypeInfo(PredefinedType.PT_METHODCALLEXPRESSION, typeof(System.Linq.Expressions.MethodCallExpression), "System.Linq.Expressions.MethodCallExpression", false, 0, false),
            new PredefinedTypeInfo(PredefinedType.PT_NEWEXPRESSION, typeof(System.Linq.Expressions.NewExpression), "System.Linq.Expressions.NewExpression", false, 0, false),
            new PredefinedTypeInfo(PredefinedType.PT_BINDING, typeof(System.Linq.Expressions.MemberBinding), "System.Linq.Expressions.MemberBinding", false, 0, false),
            new PredefinedTypeInfo(PredefinedType.PT_MEMBERINITEXPRESSION, typeof(System.Linq.Expressions.MemberInitExpression), "System.Linq.Expressions.MemberInitExpression", false, 0, false),
            new PredefinedTypeInfo(PredefinedType.PT_LISTINITEXPRESSION, typeof(System.Linq.Expressions.ListInitExpression), "System.Linq.Expressions.ListInitExpression", false, 0, false),
            new PredefinedTypeInfo(PredefinedType.PT_TYPEBINARYEXPRESSION, typeof(System.Linq.Expressions.TypeBinaryExpression), "System.Linq.Expressions.TypeBinaryExpression", false, 0, false),
            new PredefinedTypeInfo(PredefinedType.PT_NEWARRAYEXPRESSION, typeof(System.Linq.Expressions.NewArrayExpression), "System.Linq.Expressions.NewArrayExpression", false, 0, false),
            new PredefinedTypeInfo(PredefinedType.PT_MEMBERASSIGNMENT, typeof(System.Linq.Expressions.MemberAssignment), "System.Linq.Expressions.MemberAssignment", false, 0, false),
            new PredefinedTypeInfo(PredefinedType.PT_MEMBERLISTBINDING, typeof(System.Linq.Expressions.MemberListBinding), "System.Linq.Expressions.MemberListBinding", false, 0, false),
            new PredefinedTypeInfo(PredefinedType.PT_MEMBERMEMBERBINDING, typeof(System.Linq.Expressions.MemberMemberBinding), "System.Linq.Expressions.MemberMemberBinding", false, 0, false),
            new PredefinedTypeInfo(PredefinedType.PT_INVOCATIONEXPRESSION, typeof(System.Linq.Expressions.InvocationExpression), "System.Linq.Expressions.InvocationExpression", false, 0, false),
            new PredefinedTypeInfo(PredefinedType.PT_FIELDINFO, typeof(System.Reflection.FieldInfo), "System.Reflection.FieldInfo", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_METHODINFO, typeof(System.Reflection.MethodInfo), "System.Reflection.MethodInfo", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_CONSTRUCTORINFO, typeof(System.Reflection.ConstructorInfo), "System.Reflection.ConstructorInfo", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_PROPERTYINFO, typeof(System.Reflection.PropertyInfo), "System.Reflection.PropertyInfo", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_METHODBASE, typeof(System.Reflection.MethodBase), "System.Reflection.MethodBase", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_MEMBERINFO, typeof(System.Reflection.MemberInfo), "System.Reflection.MemberInfo", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_DEBUGGERDISPLAY, typeof(DebuggerDisplayAttribute), "System.Diagnostics.DebuggerDisplayAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_DEBUGGERBROWSABLE, typeof(DebuggerBrowsableAttribute), "System.Diagnostics.DebuggerBrowsableAttribute", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_DEBUGGERBROWSABLESTATE, typeof(DebuggerBrowsableState), "System.Diagnostics.DebuggerBrowsableState", false, 0, AggKindEnum.Enum, FUNDTYPE.FT_I4, true),
            new PredefinedTypeInfo(PredefinedType.PT_G_EQUALITYCOMPARER, typeof(EqualityComparer<>), "System.Collections.Generic.EqualityComparer`1", false, 1, true),
            new PredefinedTypeInfo(PredefinedType.PT_ELEMENTINITIALIZER, typeof(System.Linq.Expressions.ElementInit), "System.Linq.Expressions.ElementInit", false, 0, false),

            new PredefinedTypeInfo(PredefinedType.PT_MISSING, typeof(System.Reflection.Missing), "System.Reflection.Missing", false, 0, true),
            new PredefinedTypeInfo(PredefinedType.PT_G_IREADONLYLIST, typeof(IReadOnlyList<>), "System.Collections.Generic.IReadOnlyList`1", false, 1, AggKindEnum.Interface, FUNDTYPE.FT_REF, false),
            new PredefinedTypeInfo(PredefinedType.PT_G_IREADONLYCOLLECTION, typeof(IReadOnlyCollection<>), "System.Collections.Generic.IReadOnlyCollection`1", false, 1, AggKindEnum.Interface, FUNDTYPE.FT_REF, false),
        };

        private static readonly Dictionary<string, PredefinedType> s_pdTypeNames = CreatePredefinedTypeFacts();

        private static Dictionary<string, PredefinedType> CreatePredefinedTypeFacts()
        {
            var pdTypeNames = new Dictionary<string, PredefinedType>((int)PredefinedType.PT_COUNT);
#if DEBUG
            for (int i = 0; i < (int)PredefinedType.PT_COUNT; i++)
            {
                Debug.Assert(s_pdTypes[i].type == (PredefinedType)i);
            }
#endif
            for (int i = 0; i < (int)PredefinedType.PT_COUNT; i++)
            {
                pdTypeNames.Add(s_pdTypes[i].name, (PredefinedType)i);
            }
            return pdTypeNames;
        }
    }
}
