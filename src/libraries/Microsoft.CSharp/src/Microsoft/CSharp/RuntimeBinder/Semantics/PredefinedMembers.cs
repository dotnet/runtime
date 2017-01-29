// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // enum identifying all predefined methods used in the C# compiler
    //
    // Naming convention is PREDEFMETH.PM_ <Predefined CType> _ < Predefined Name of Method>
    // if methods can only be disambiguated by signature, then follow the
    // above with _ <argument types>
    //
    // Keep this list sorted by containing type and name.
    internal enum PREDEFMETH
    {
        PM_FIRST = 0,

        PM_ARRAY_GETLENGTH,

        PM_DECIMAL_OPDECREMENT,
        PM_DECIMAL_OPDIVISION,
        PM_DECIMAL_OPEQUALITY,
        PM_DECIMAL_OPGREATERTHAN,
        PM_DECIMAL_OPGREATERTHANOREQUAL,
        PM_DECIMAL_OPINCREMENT,
        PM_DECIMAL_OPINEQUALITY,
        PM_DECIMAL_OPLESSTHAN,
        PM_DECIMAL_OPLESSTHANOREQUAL,
        PM_DECIMAL_OPMINUS,
        PM_DECIMAL_OPMODULUS,
        PM_DECIMAL_OPMULTIPLY,
        PM_DECIMAL_OPPLUS,
        PM_DECIMAL_OPUNARYMINUS,
        PM_DECIMAL_OPUNARYPLUS,

        PM_DELEGATE_COMBINE,
        PM_DELEGATE_OPEQUALITY,
        PM_DELEGATE_OPINEQUALITY,
        PM_DELEGATE_REMOVE,

        PM_EXPRESSION_ADD,
        PM_EXPRESSION_ADD_USER_DEFINED,
        PM_EXPRESSION_ADDCHECKED,
        PM_EXPRESSION_ADDCHECKED_USER_DEFINED,
        PM_EXPRESSION_AND,
        PM_EXPRESSION_AND_USER_DEFINED,
        PM_EXPRESSION_ANDALSO,
        PM_EXPRESSION_ANDALSO_USER_DEFINED,
        PM_EXPRESSION_ARRAYINDEX,
        PM_EXPRESSION_ARRAYINDEX2,
        PM_EXPRESSION_ASSIGN,

        PM_EXPRESSION_CONDITION,

        PM_EXPRESSION_CONSTANT_OBJECT_TYPE,
        PM_EXPRESSION_CONVERT,
        PM_EXPRESSION_CONVERT_USER_DEFINED,
        PM_EXPRESSION_CONVERTCHECKED,
        PM_EXPRESSION_CONVERTCHECKED_USER_DEFINED,
        PM_EXPRESSION_DIVIDE,
        PM_EXPRESSION_DIVIDE_USER_DEFINED,

        PM_EXPRESSION_EQUAL,
        PM_EXPRESSION_EQUAL_USER_DEFINED,
        PM_EXPRESSION_EXCLUSIVEOR,
        PM_EXPRESSION_EXCLUSIVEOR_USER_DEFINED,
        PM_EXPRESSION_FIELD,
        PM_EXPRESSION_GREATERTHAN,
        PM_EXPRESSION_GREATERTHAN_USER_DEFINED,
        PM_EXPRESSION_GREATERTHANOREQUAL,
        PM_EXPRESSION_GREATERTHANOREQUAL_USER_DEFINED,
        PM_EXPRESSION_LAMBDA,

        PM_EXPRESSION_LEFTSHIFT,
        PM_EXPRESSION_LEFTSHIFT_USER_DEFINED,
        PM_EXPRESSION_LESSTHAN,
        PM_EXPRESSION_LESSTHAN_USER_DEFINED,
        PM_EXPRESSION_LESSTHANOREQUAL,
        PM_EXPRESSION_LESSTHANOREQUAL_USER_DEFINED,
        PM_EXPRESSION_MODULO,
        PM_EXPRESSION_MODULO_USER_DEFINED,
        PM_EXPRESSION_MULTIPLY,
        PM_EXPRESSION_MULTIPLY_USER_DEFINED,
        PM_EXPRESSION_MULTIPLYCHECKED,
        PM_EXPRESSION_MULTIPLYCHECKED_USER_DEFINED,
        PM_EXPRESSION_NOTEQUAL,
        PM_EXPRESSION_NOTEQUAL_USER_DEFINED,
        PM_EXPRESSION_OR,
        PM_EXPRESSION_OR_USER_DEFINED,
        PM_EXPRESSION_ORELSE,
        PM_EXPRESSION_ORELSE_USER_DEFINED,
        PM_EXPRESSION_PARAMETER,
        PM_EXPRESSION_RIGHTSHIFT,
        PM_EXPRESSION_RIGHTSHIFT_USER_DEFINED,
        PM_EXPRESSION_SUBTRACT,
        PM_EXPRESSION_SUBTRACT_USER_DEFINED,
        PM_EXPRESSION_SUBTRACTCHECKED,
        PM_EXPRESSION_SUBTRACTCHECKED_USER_DEFINED,
        PM_EXPRESSION_UNARYPLUS_USER_DEFINED,
        PM_EXPRESSION_NEGATE,
        PM_EXPRESSION_NEGATE_USER_DEFINED,
        PM_EXPRESSION_NEGATECHECKED,
        PM_EXPRESSION_NEGATECHECKED_USER_DEFINED,
        PM_EXPRESSION_CALL,
        PM_EXPRESSION_NEW,
        PM_EXPRESSION_NEW_MEMBERS,
        PM_EXPRESSION_NEW_TYPE,
        PM_EXPRESSION_QUOTE,
        PM_EXPRESSION_ARRAYLENGTH,


        PM_EXPRESSION_NOT,
        PM_EXPRESSION_NOT_USER_DEFINED,


        PM_EXPRESSION_NEWARRAYINIT,
        PM_EXPRESSION_PROPERTY,


        PM_EXPRESSION_INVOKE,
        PM_METHODINFO_CREATEDELEGATE_TYPE_OBJECT,

        PM_G_OPTIONAL_CTOR,
        PM_G_OPTIONAL_GETHASVALUE,
        PM_G_OPTIONAL_GETVALUE,
        PM_G_OPTIONAL_GET_VALUE_OR_DEF,

        PM_STRING_CONCAT_OBJECT_1,      // NOTE: these 3 must be sequential. See RealizeConcats
        PM_STRING_CONCAT_OBJECT_2,
        PM_STRING_CONCAT_OBJECT_3,
        PM_STRING_CONCAT_STRING_1,      // NOTE: these 4 must be sequential. See RealizeConcats
        PM_STRING_CONCAT_STRING_2,
        PM_STRING_CONCAT_STRING_3,
        PM_STRING_CONCAT_STRING_4,
        PM_STRING_CONCAT_SZ_OBJECT,
        PM_STRING_CONCAT_SZ_STRING,
        PM_STRING_GETCHARS,
        PM_STRING_GETLENGTH,
        PM_STRING_OPEQUALITY,
        PM_STRING_OPINEQUALITY,

        PM_COUNT
    }

    // enum identifying all predefined properties used in the C# compiler
    // Naming convention is PREDEFMETH.PM_ <Predefined CType> _ < Predefined Name of Property>
    // Keep this list sorted by containing type and name.
    internal enum PREDEFPROP
    {
        PP_FIRST = 0,
        PP_ARRAY_LENGTH,
        PP_G_OPTIONAL_VALUE,
        PP_COUNT,
    };

    internal enum MethodRequiredEnum
    {
        Required,
        Optional
    }

    internal enum MethodCallingConventionEnum
    {
        Static,
        Virtual,
        Instance
    }
    // Enum used to encode a method signature
    // A signature is encoded as a sequence of int values.
    // The grammar for signatures is:
    //
    //  signature
    //      return_type count_of_parameters parameter_types
    //
    //  type
    //      any predefined type (ex: PredefinedType.PT_OBJECT, PredefinedType.PT_VOID)  type_args
    //      MethodSignatureEnum.SIG_CLASS_TYVAR  index_of_class_tyvar
    //      MethodSignatureEnum.SIG_METH_TYVAR  index_of_method_tyvar
    //      MethodSignatureEnum.SIG_SZ_ARRAY type
    //      MethodSignatureEnum.SIG_REF type
    //      MethodSignatureEnum.SIG_OUT type
    //
    internal enum MethodSignatureEnum
    {
        // Values 0 to PredefinedType.PT_VOID are reserved for predefined types in signatures
        // start next value at PredefinedType.PT_VOID + 1,
        SIG_CLASS_TYVAR = (int)PredefinedType.PT_VOID + 1,          // next element in signature is index of class tyvar
        SIG_METH_TYVAR,                         // next element in signature is index of method tyvar
        SIG_SZ_ARRAY,                           // must be followed by signature type of array elements
        SIG_REF,                                // must be followed by signature of ref type
        SIG_OUT,                                // must be followed by signature of out type
    }

    // A description of a method the compiler uses while compiling.
    internal sealed class PredefinedMethodInfo
    {
        public PREDEFMETH method;
        public PredefinedType type;
        public PredefinedName name;
        public MethodCallingConventionEnum callingConvention;
        public ACCESS access;          // ACCESS.ACC_UNKNOWN means any accessibility is ok
        public int cTypeVars;
        public int[] signature;       // Size 8. expand this if a new method has a signature which doesn't fit in the current space

        public PredefinedMethodInfo(PREDEFMETH method, MethodRequiredEnum required, PredefinedType type, PredefinedName name, MethodCallingConventionEnum callingConvention, ACCESS access, int cTypeVars, int[] signature)
        {
            this.method = method;
            this.type = type;
            this.name = name;
            this.callingConvention = callingConvention;
            this.access = access;
            this.cTypeVars = cTypeVars;
            this.signature = signature;
        }
    }


    // A description of a method the compiler uses while compiling.
    internal sealed class PredefinedPropertyInfo
    {
        public PREDEFPROP property;
        public PredefinedName name;
        public PREDEFMETH getter;
        public PREDEFMETH setter;

        public PredefinedPropertyInfo(PREDEFPROP property, MethodRequiredEnum required, PredefinedName name, PREDEFMETH getter, PREDEFMETH setter)
        {
            this.property = property;
            this.name = name;
            this.getter = getter;
            this.setter = setter;
        }
    };

    // Loads and caches predefined members.
    // Also finds constructors on delegate types.
    internal sealed class PredefinedMembers
    {
        private static void RETAILVERIFY(bool f)
        {
            if (!f)
                Debug.Assert(false, "panic!");
        }

        private readonly SymbolLoader _loader;
        internal SymbolTable RuntimeBinderSymbolTable;
        private readonly MethodSymbol[] _methods = new MethodSymbol[(int)PREDEFMETH.PM_COUNT];
        private readonly PropertySymbol[] _properties = new PropertySymbol[(int)PREDEFPROP.PP_COUNT];

        private Name GetMethName(PREDEFMETH method)
        {
            return GetPredefName(GetMethPredefName(method));
        }

        private AggregateSymbol GetMethParent(PREDEFMETH method)
        {
            return GetOptPredefAgg(GetMethPredefType(method));
        }

        // delegate specific helpers
        private MethodSymbol FindDelegateConstructor(AggregateSymbol delegateType, int[] signature)
        {
            Debug.Assert(delegateType != null && delegateType.IsDelegate());
            Debug.Assert(signature != null);

            return LoadMethod(
                                delegateType,
                                signature,
                                0,                          // meth ty vars
                                GetPredefName(PredefinedName.PN_CTOR),
                                ACCESS.ACC_PUBLIC,
                                false,                      // MethodCallingConventionEnum.Static
                                false);                     // MethodCallingConventionEnum.Virtual
        }

        private MethodSymbol FindDelegateConstructor(AggregateSymbol delegateType)
        {
            Debug.Assert(delegateType != null && delegateType.IsDelegate());

            MethodSymbol ctor = FindDelegateConstructor(delegateType, s_DelegateCtorSignature1);
            if (ctor == null)
            {
                ctor = FindDelegateConstructor(delegateType, s_DelegateCtorSignature2);
            }

            return ctor;
        }

        public MethodSymbol FindDelegateConstructor(AggregateSymbol delegateType, bool fReportErrors)
        {
            MethodSymbol ctor = FindDelegateConstructor(delegateType);
            if (ctor == null && fReportErrors)
            {
                GetErrorContext().Error(ErrorCode.ERR_BadDelegateConstructor, delegateType);
            }

            return ctor;
        }

        // property specific helpers
        private PropertySymbol EnsureProperty(PREDEFPROP property)
        {
            RETAILVERIFY((int)property > (int)PREDEFMETH.PM_FIRST && (int)property < (int)PREDEFMETH.PM_COUNT);

            if (_properties[(int)property] == null)
            {
                _properties[(int)property] = LoadProperty(property);
            }
            return _properties[(int)property];
        }
        private PropertySymbol LoadProperty(PREDEFPROP property)
        {
            return LoadProperty(
                        property,
                        GetPropName(property),
                        GetPropGetter(property),
                        GetPropSetter(property));
        }

        private Name GetPropName(PREDEFPROP property)
        {
            return GetPredefName(GetPropPredefName(property));
        }
        private PropertySymbol LoadProperty(
            PREDEFPROP predefProp,
            Name propertyName,
            PREDEFMETH propertyGetter,
            PREDEFMETH propertySetter)
        {
            Debug.Assert(propertyName != null);
            Debug.Assert(propertyGetter > PREDEFMETH.PM_FIRST && propertyGetter < PREDEFMETH.PM_COUNT);
            Debug.Assert(propertySetter > PREDEFMETH.PM_FIRST && propertySetter <= PREDEFMETH.PM_COUNT);

            MethodSymbol getter = GetOptionalMethod(propertyGetter);
            MethodSymbol setter = null;
            if (propertySetter != PREDEFMETH.PM_COUNT)
            {
                setter = GetOptionalMethod(propertySetter);
            }

            if (getter == null && setter == null)
            {
                RuntimeBinderSymbolTable.AddPredefinedPropertyToSymbolTable(GetOptPredefAgg(GetPropPredefType(predefProp)), propertyName);
                getter = GetOptionalMethod(propertyGetter);
                if (propertySetter != PREDEFMETH.PM_COUNT)
                {
                    setter = GetOptionalMethod(propertySetter);
                }
            }

            if (setter != null)
            {
                setter.SetMethKind(MethodKindEnum.PropAccessor);
            }

            PropertySymbol property = null;
            if (getter != null)
            {
                getter.SetMethKind(MethodKindEnum.PropAccessor);
                property = getter.getProperty();

                // Didn't find it, so load it.
                if (property == null)
                {
                    RuntimeBinderSymbolTable.AddPredefinedPropertyToSymbolTable(GetOptPredefAgg(GetPropPredefType(predefProp)), propertyName);
                }
                property = getter.getProperty();
                Debug.Assert(property != null);

                if (property.name != propertyName ||
                    (propertySetter != PREDEFMETH.PM_COUNT &&
                        (setter == null ||
                         !setter.isPropertyAccessor() ||
                         setter.getProperty() != property)) ||
                    property.getBogus())
                {
                    property = null;
                }
            }

            return property;
        }

        private SymbolLoader GetSymbolLoader()
        {
            Debug.Assert(_loader != null);

            return _loader;
        }
        private ErrorHandling GetErrorContext()
        {
            return GetSymbolLoader().GetErrorContext();
        }
        private NameManager GetNameManager()
        {
            return GetSymbolLoader().GetNameManager();
        }
        private TypeManager GetTypeManager()
        {
            return GetSymbolLoader().GetTypeManager();
        }
        private BSYMMGR getBSymmgr()
        {
            return GetSymbolLoader().getBSymmgr();
        }

        private Name GetPredefName(PredefinedName pn)
        {
            return GetNameManager().GetPredefName(pn);
        }
        private AggregateSymbol GetOptPredefAgg(PredefinedType pt)
        {
            return GetSymbolLoader().GetOptPredefAgg(pt);
        }

        private CType LoadTypeFromSignature(int[] signature, ref int indexIntoSignatures, TypeArray classTyVars)
        {
            Debug.Assert(signature != null);

            MethodSignatureEnum current = (MethodSignatureEnum)signature[indexIntoSignatures];
            indexIntoSignatures++;

            switch (current)
            {
                case MethodSignatureEnum.SIG_REF:
                    {
                        CType refType = LoadTypeFromSignature(signature, ref indexIntoSignatures, classTyVars);
                        if (refType == null)
                        {
                            return null;
                        }
                        return GetTypeManager().GetParameterModifier(refType, false);
                    }
                case MethodSignatureEnum.SIG_OUT:
                    {
                        CType outType = LoadTypeFromSignature(signature, ref indexIntoSignatures, classTyVars);
                        if (outType == null)
                        {
                            return null;
                        }
                        return GetTypeManager().GetParameterModifier(outType, true);
                    }
                case MethodSignatureEnum.SIG_SZ_ARRAY:
                    {
                        CType elementType = LoadTypeFromSignature(signature, ref indexIntoSignatures, classTyVars);
                        if (elementType == null)
                        {
                            return null;
                        }
                        return GetTypeManager().GetArray(elementType, 1);
                    }
                case MethodSignatureEnum.SIG_METH_TYVAR:
                    {
                        int index = signature[indexIntoSignatures];
                        indexIntoSignatures++;
                        return GetTypeManager().GetStdMethTypeVar(index);
                    }
                case MethodSignatureEnum.SIG_CLASS_TYVAR:
                    {
                        int index = signature[indexIntoSignatures];
                        indexIntoSignatures++;
                        return classTyVars.Item(index);
                    }
                case (MethodSignatureEnum)PredefinedType.PT_VOID:
                    return GetTypeManager().GetVoid();
                default:
                    {
                        Debug.Assert(current >= 0 && (int)current < (int)PredefinedType.PT_COUNT);
                        AggregateSymbol agg = GetOptPredefAgg((PredefinedType)current);
                        if (agg != null)
                        {
                            CType[] typeArgs = new CType[agg.GetTypeVars().size];
                            for (int iTypeArg = 0; iTypeArg < agg.GetTypeVars().size; iTypeArg++)
                            {
                                typeArgs[iTypeArg] = LoadTypeFromSignature(signature, ref indexIntoSignatures, classTyVars);
                                if (typeArgs[iTypeArg] == null)
                                {
                                    return null;
                                }
                            }
                            AggregateType type = GetTypeManager().GetAggregate(agg, getBSymmgr().AllocParams(agg.GetTypeVars().size, typeArgs));
                            if (type.isPredefType(PredefinedType.PT_G_OPTIONAL))
                            {
                                return GetTypeManager().GetNubFromNullable(type);
                            }

                            return type;
                        }
                    }
                    break;
            }

            return null;
        }
        private TypeArray LoadTypeArrayFromSignature(int[] signature, ref int indexIntoSignatures, TypeArray classTyVars)
        {
            Debug.Assert(signature != null);

            int count = signature[indexIntoSignatures];
            indexIntoSignatures++;

            Debug.Assert(count >= 0);

            CType[] ptypes = new CType[count];
            for (int i = 0; i < count; i++)
            {
                ptypes[i] = LoadTypeFromSignature(signature, ref indexIntoSignatures, classTyVars);
                if (ptypes[i] == null)
                {
                    return null;
                }
            }
            return getBSymmgr().AllocParams(count, ptypes);
        }

        public PredefinedMembers(SymbolLoader loader)
        {
            _loader = loader;
            Debug.Assert(_loader != null);

            _methods = new MethodSymbol[(int)PREDEFMETH.PM_COUNT];
            _properties = new PropertySymbol[(int)PREDEFPROP.PP_COUNT];

#if DEBUG
            // validate the tables
            for (int i = (int)PREDEFMETH.PM_FIRST + 1; i < (int)PREDEFMETH.PM_COUNT; i++)
            {
                Debug.Assert((int)GetMethInfo((PREDEFMETH)i).method == i);
            }
            for (int i = (int)PREDEFPROP.PP_FIRST + 1; i < (int)PREDEFPROP.PP_COUNT; i++)
            {
                Debug.Assert((int)GetPropInfo((PREDEFPROP)i).property == i);
            }
#endif
        }

        public PropertySymbol GetProperty(PREDEFPROP property)  // Reports an error if the property is not found.
        {
            PropertySymbol result = EnsureProperty(property);
            if (result == null)
            {
                ReportError(property);
            }

            return result;
        }

        public MethodSymbol GetMethod(PREDEFMETH method)
        {
            MethodSymbol result = EnsureMethod(method);
            if (result == null)
            {
                ReportError(method);
            }

            return result;
        }

        private MethodSymbol GetOptionalMethod(PREDEFMETH method)
        {
            return EnsureMethod(method);
        }

        private MethodSymbol EnsureMethod(PREDEFMETH method)
        {
            RETAILVERIFY(method > PREDEFMETH.PM_FIRST && method < PREDEFMETH.PM_COUNT);
            if (_methods[(int)method] == null)
            {
                _methods[(int)method] = LoadMethod(method);
            }
            return _methods[(int)method];
        }

        private MethodSymbol LoadMethod(
                        AggregateSymbol type,
                        int[] signature,
                        int cMethodTyVars,
                        Name methodName,
                        ACCESS methodAccess,
                        bool isStatic,
                        bool isVirtual
                        )
        {
            Debug.Assert(signature != null);
            Debug.Assert(cMethodTyVars >= 0);
            Debug.Assert(methodName != null);

            if (type == null)
            {
                return null;
            }
            TypeArray classTyVars = type.GetTypeVarsAll();

            int index = 0;
            CType returnType = LoadTypeFromSignature(signature, ref index, classTyVars);
            if (returnType == null)
            {
                return null;
            }
            TypeArray argumentTypes = LoadTypeArrayFromSignature(signature, ref index, classTyVars);
            if (argumentTypes == null)
            {
                return null;
            }
            TypeArray standardMethodTyVars = GetTypeManager().GetStdMethTyVarArray(cMethodTyVars);

            MethodSymbol ret = LookupMethodWhileLoading(type, cMethodTyVars, methodName, methodAccess, isStatic, isVirtual, returnType, argumentTypes);

            if (ret == null)
            {
                RuntimeBinderSymbolTable.AddPredefinedMethodToSymbolTable(type, methodName);
                ret = LookupMethodWhileLoading(type, cMethodTyVars, methodName, methodAccess, isStatic, isVirtual, returnType, argumentTypes);
            }
            return ret;
        }

        private MethodSymbol LookupMethodWhileLoading(AggregateSymbol type, int cMethodTyVars, Name methodName, ACCESS methodAccess, bool isStatic, bool isVirtual, CType returnType, TypeArray argumentTypes)
        {
            for (Symbol sym = GetSymbolLoader().LookupAggMember(methodName, type, symbmask_t.MASK_ALL);
                 sym != null;
                 sym = GetSymbolLoader().LookupNextSym(sym, type, symbmask_t.MASK_ALL))
            {
                if (sym.IsMethodSymbol())
                {
                    MethodSymbol methsym = sym.AsMethodSymbol();
                    if ((methsym.GetAccess() == methodAccess || methodAccess == ACCESS.ACC_UNKNOWN) &&
                        methsym.isStatic == isStatic &&
                        methsym.isVirtual == isVirtual &&
                        methsym.typeVars.size == cMethodTyVars &&
                        GetTypeManager().SubstEqualTypes(methsym.RetType, returnType, null, methsym.typeVars, SubstTypeFlags.DenormMeth) &&
                        GetTypeManager().SubstEqualTypeArrays(methsym.Params, argumentTypes, (TypeArray)null,
                            methsym.typeVars, SubstTypeFlags.DenormMeth) &&
                        !methsym.getBogus())
                    {
                        return methsym;
                    }
                }
            }
            return null;
        }

        private MethodSymbol LoadMethod(PREDEFMETH method)
        {
            return LoadMethod(
                        GetMethParent(method),
                        GetMethSignature(method),
                        GetMethTyVars(method),
                        GetMethName(method),
                        GetMethAccess(method),
                        IsMethStatic(method),
                        IsMethVirtual(method));
        }

        private void ReportError(PREDEFMETH method)
        {
            ReportError(GetMethPredefType(method), GetMethPredefName(method));
        }

        private void ReportError(PredefinedType type, PredefinedName name)
        {
            GetErrorContext().Error(ErrorCode.ERR_MissingPredefinedMember, PredefinedTypes.GetFullName(type), GetPredefName(name));
        }

        private static readonly int[] s_DelegateCtorSignature1 = { (int)PredefinedType.PT_VOID, 2, (int)PredefinedType.PT_OBJECT, (int)PredefinedType.PT_INTPTR };
        private static readonly int[] s_DelegateCtorSignature2 = { (int)PredefinedType.PT_VOID, 2, (int)PredefinedType.PT_OBJECT, (int)PredefinedType.PT_UINTPTR };

        private static PredefinedName GetPropPredefName(PREDEFPROP property)
        {
            return GetPropInfo(property).name;
        }

        private static PREDEFMETH GetPropGetter(PREDEFPROP property)
        {
            PREDEFMETH result = GetPropInfo(property).getter;

            // getters are MethodRequiredEnum.Required
            Debug.Assert(result > PREDEFMETH.PM_FIRST && result < PREDEFMETH.PM_COUNT);

            return result;
        }

        private static PredefinedType GetPropPredefType(PREDEFPROP property)
        {
            return GetMethInfo(GetPropGetter(property)).type;
        }

        private static PREDEFMETH GetPropSetter(PREDEFPROP property)
        {
            PREDEFMETH result = GetPropInfo(property).setter;

            // setters are not MethodRequiredEnum.Required
            Debug.Assert(result > PREDEFMETH.PM_FIRST && result <= PREDEFMETH.PM_COUNT);

            return GetPropInfo(property).setter;
        }

        private void ReportError(PREDEFPROP property)
        {
            ReportError(GetPropPredefType(property), GetPropPredefName(property));
        }

        // the list of predefined property definitions.
        // This list must be in the same order as the PREDEFPROP enum.
        private static readonly PredefinedPropertyInfo[] s_predefinedProperties = {
            new PredefinedPropertyInfo(   PREDEFPROP.PP_FIRST,                                           MethodRequiredEnum.Optional,   PredefinedName.PN_COUNT,                   PREDEFMETH.PM_COUNT,                                           PREDEFMETH.PM_COUNT  ),

            new PredefinedPropertyInfo(   PREDEFPROP.PP_ARRAY_LENGTH,                                    MethodRequiredEnum.Optional,   PredefinedName.PN_LENGTH,                  PREDEFMETH.PM_ARRAY_GETLENGTH,                                 PREDEFMETH.PM_COUNT  ),
            new PredefinedPropertyInfo(   PREDEFPROP.PP_G_OPTIONAL_VALUE,                                MethodRequiredEnum.Optional,   PredefinedName.PN_CAP_VALUE,               PREDEFMETH.PM_G_OPTIONAL_GETVALUE,                             PREDEFMETH.PM_COUNT  ),
        };

        private static PredefinedPropertyInfo GetPropInfo(PREDEFPROP property)
        {
            RETAILVERIFY(property > PREDEFPROP.PP_FIRST && property < PREDEFPROP.PP_COUNT);
            RETAILVERIFY(s_predefinedProperties[(int)property].property == property);

            return s_predefinedProperties[(int)property];
        }

        private static PredefinedMethodInfo GetMethInfo(PREDEFMETH method)
        {
            RETAILVERIFY(method > PREDEFMETH.PM_FIRST && method < PREDEFMETH.PM_COUNT);
            RETAILVERIFY(s_predefinedMethods[(int)method].method == method);

            return s_predefinedMethods[(int)method];
        }

        private static PredefinedName GetMethPredefName(PREDEFMETH method)
        {
            return GetMethInfo(method).name;
        }

        private static PredefinedType GetMethPredefType(PREDEFMETH method)
        {
            return GetMethInfo(method).type;
        }

        private static bool IsMethStatic(PREDEFMETH method)
        {
            return GetMethInfo(method).callingConvention == MethodCallingConventionEnum.Static;
        }

        private static bool IsMethVirtual(PREDEFMETH method)
        {
            return GetMethInfo(method).callingConvention == MethodCallingConventionEnum.Virtual;
        }

        private static ACCESS GetMethAccess(PREDEFMETH method)
        {
            return GetMethInfo(method).access;
        }

        private static int GetMethTyVars(PREDEFMETH method)
        {
            return GetMethInfo(method).cTypeVars;
        }

        private static int[] GetMethSignature(PREDEFMETH method)
        {
            return GetMethInfo(method).signature;
        }

        // the list of predefined method definitions.
        // This list must be in the same order as the PREDEFMETH enum.
        private static readonly PredefinedMethodInfo[] s_predefinedMethods = new PredefinedMethodInfo[(int)PREDEFMETH.PM_COUNT] {
            new PredefinedMethodInfo(   PREDEFMETH.PM_FIRST,                                           MethodRequiredEnum.Optional,   PredefinedType.PT_COUNT,               PredefinedName.PN_COUNT,                   MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_VOID, 0  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_ARRAY_GETLENGTH,                                 MethodRequiredEnum.Optional,   PredefinedType.PT_ARRAY,               PredefinedName.PN_GETLENGTH,               MethodCallingConventionEnum.Instance,   ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_INT, 0  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DECIMAL_OPDECREMENT,                             MethodRequiredEnum.Optional,   PredefinedType.PT_DECIMAL,             PredefinedName.PN_OPDECREMENT,             MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_DECIMAL, 1, (int)PredefinedType.PT_DECIMAL  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DECIMAL_OPDIVISION,                              MethodRequiredEnum.Optional,   PredefinedType.PT_DECIMAL,             PredefinedName.PN_OPDIVISION,              MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_DECIMAL, 2, (int)PredefinedType.PT_DECIMAL, (int)PredefinedType.PT_DECIMAL  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DECIMAL_OPEQUALITY,                              MethodRequiredEnum.Optional,   PredefinedType.PT_DECIMAL,             PredefinedName.PN_OPEQUALITY,              MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BOOL, 2, (int)PredefinedType.PT_DECIMAL, (int)PredefinedType.PT_DECIMAL  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DECIMAL_OPGREATERTHAN,                           MethodRequiredEnum.Optional,   PredefinedType.PT_DECIMAL,             PredefinedName.PN_OPGREATERTHAN,           MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BOOL, 2, (int)PredefinedType.PT_DECIMAL, (int)PredefinedType.PT_DECIMAL  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DECIMAL_OPGREATERTHANOREQUAL,                    MethodRequiredEnum.Optional,   PredefinedType.PT_DECIMAL,             PredefinedName.PN_OPGREATERTHANOREQUAL,    MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BOOL, 2, (int)PredefinedType.PT_DECIMAL, (int)PredefinedType.PT_DECIMAL  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DECIMAL_OPINCREMENT,                             MethodRequiredEnum.Optional,   PredefinedType.PT_DECIMAL,             PredefinedName.PN_OPINCREMENT,             MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_DECIMAL, 1, (int)PredefinedType.PT_DECIMAL  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DECIMAL_OPINEQUALITY,                            MethodRequiredEnum.Optional,   PredefinedType.PT_DECIMAL,             PredefinedName.PN_OPINEQUALITY,            MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BOOL, 2, (int)PredefinedType.PT_DECIMAL, (int)PredefinedType.PT_DECIMAL  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DECIMAL_OPLESSTHAN,                              MethodRequiredEnum.Optional,   PredefinedType.PT_DECIMAL,             PredefinedName.PN_OPLESSTHAN,              MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BOOL, 2, (int)PredefinedType.PT_DECIMAL, (int)PredefinedType.PT_DECIMAL  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DECIMAL_OPLESSTHANOREQUAL,                       MethodRequiredEnum.Optional,   PredefinedType.PT_DECIMAL,             PredefinedName.PN_OPLESSTHANOREQUAL,       MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BOOL, 2, (int)PredefinedType.PT_DECIMAL, (int)PredefinedType.PT_DECIMAL  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DECIMAL_OPMINUS,                                 MethodRequiredEnum.Optional,   PredefinedType.PT_DECIMAL,             PredefinedName.PN_OPMINUS,                 MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_DECIMAL, 2, (int)PredefinedType.PT_DECIMAL, (int)PredefinedType.PT_DECIMAL  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DECIMAL_OPMODULUS,                               MethodRequiredEnum.Optional,   PredefinedType.PT_DECIMAL,             PredefinedName.PN_OPMODULUS,               MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_DECIMAL, 2, (int)PredefinedType.PT_DECIMAL, (int)PredefinedType.PT_DECIMAL  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DECIMAL_OPMULTIPLY,                              MethodRequiredEnum.Optional,   PredefinedType.PT_DECIMAL,             PredefinedName.PN_OPMULTIPLY,              MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_DECIMAL, 2, (int)PredefinedType.PT_DECIMAL, (int)PredefinedType.PT_DECIMAL  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DECIMAL_OPPLUS,                                  MethodRequiredEnum.Optional,   PredefinedType.PT_DECIMAL,             PredefinedName.PN_OPPLUS,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_DECIMAL, 2, (int)PredefinedType.PT_DECIMAL, (int)PredefinedType.PT_DECIMAL  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DECIMAL_OPUNARYMINUS,                            MethodRequiredEnum.Optional,   PredefinedType.PT_DECIMAL,             PredefinedName.PN_OPUNARYMINUS,            MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_DECIMAL, 1, (int)PredefinedType.PT_DECIMAL  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DECIMAL_OPUNARYPLUS,                             MethodRequiredEnum.Optional,   PredefinedType.PT_DECIMAL,             PredefinedName.PN_OPUNARYPLUS,             MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_DECIMAL, 1, (int)PredefinedType.PT_DECIMAL  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DELEGATE_COMBINE,                                MethodRequiredEnum.Optional,   PredefinedType.PT_DELEGATE,            PredefinedName.PN_COMBINE,                 MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_DELEGATE, 2, (int)PredefinedType.PT_DELEGATE, (int)PredefinedType.PT_DELEGATE  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DELEGATE_OPEQUALITY,                             MethodRequiredEnum.Optional,   PredefinedType.PT_DELEGATE,            PredefinedName.PN_OPEQUALITY,              MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BOOL, 2, (int)PredefinedType.PT_DELEGATE, (int)PredefinedType.PT_DELEGATE  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DELEGATE_OPINEQUALITY,                           MethodRequiredEnum.Optional,   PredefinedType.PT_DELEGATE,            PredefinedName.PN_OPINEQUALITY,            MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BOOL, 2, (int)PredefinedType.PT_DELEGATE, (int)PredefinedType.PT_DELEGATE  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DELEGATE_REMOVE,                                 MethodRequiredEnum.Optional,   PredefinedType.PT_DELEGATE,            PredefinedName.PN_REMOVE,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_DELEGATE, 2, (int)PredefinedType.PT_DELEGATE, (int)PredefinedType.PT_DELEGATE  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_ADD,                                  MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_ADD,                     MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_ADD_USER_DEFINED,                     MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_ADD,                     MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_ADDCHECKED,                           MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_ADDCHECKED,              MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_ADDCHECKED_USER_DEFINED,              MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_ADDCHECKED,              MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_AND,                                  MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_AND,                     MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_AND_USER_DEFINED,                     MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_AND,                     MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_ANDALSO,                              MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_ANDALSO,                 MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_ANDALSO_USER_DEFINED,                 MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_ANDALSO,                 MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_ARRAYINDEX,                           MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_ARRAYINDEX,              MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_ARRAYINDEX2,                          MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_ARRAYINDEX,              MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_METHODCALLEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)MethodSignatureEnum.SIG_SZ_ARRAY, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_ASSIGN,                               MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_ASSIGN,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_CONDITION,                            MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_CONDITION,               MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_CONDITIONALEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_CONSTANT_OBJECT_TYPE,                 MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_CONSTANT,                MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_CONSTANTEXPRESSION, 2, (int)PredefinedType.PT_OBJECT, (int)PredefinedType.PT_TYPE  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_CONVERT,                              MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_CONVERT,                 MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_TYPE  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_CONVERT_USER_DEFINED,                 MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_CONVERT,                 MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_TYPE, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED,                       MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_CONVERTCHECKED,          MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_TYPE  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED_USER_DEFINED,          MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_CONVERTCHECKED,          MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_TYPE, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_DIVIDE,                               MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_DIVIDE,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_DIVIDE_USER_DEFINED,                  MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_DIVIDE,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_EQUAL,                                MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_EQUAL,                   MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_EQUAL_USER_DEFINED,                   MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_EQUAL,                   MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 4, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_BOOL, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR,                          MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_EXCLUSIVEOR,             MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR_USER_DEFINED,             MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_EXCLUSIVEOR,             MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_FIELD,                                MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_CAP_FIELD,               MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_MEMBEREXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_FIELDINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_GREATERTHAN,                          MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_GREATERTHAN,             MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_GREATERTHAN_USER_DEFINED,             MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_GREATERTHAN,             MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 4, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_BOOL, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL,                   MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_GREATERTHANOREQUAL,      MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL_USER_DEFINED,      MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_GREATERTHANOREQUAL,      MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 4, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_BOOL, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_LAMBDA,                               MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_LAMBDA,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     1,  new int[] { (int)PredefinedType.PT_G_EXPRESSION, (int)MethodSignatureEnum.SIG_METH_TYVAR, 0, 2, (int)PredefinedType.PT_EXPRESSION, (int)MethodSignatureEnum.SIG_SZ_ARRAY, (int)PredefinedType.PT_PARAMETEREXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_LEFTSHIFT,                            MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_LEFTSHIFT,               MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_LEFTSHIFT_USER_DEFINED,               MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_LEFTSHIFT,               MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_LESSTHAN,                             MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_LESSTHAN,                MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_LESSTHAN_USER_DEFINED,                MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_LESSTHAN,                MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 4, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_BOOL, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL,                      MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_LESSTHANOREQUAL,         MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL_USER_DEFINED,         MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_LESSTHANOREQUAL,         MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 4, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_BOOL, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_MODULO,                               MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_MODULO,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_MODULO_USER_DEFINED,                  MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_MODULO,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_MULTIPLY,                             MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_MULTIPLY,                MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_MULTIPLY_USER_DEFINED,                MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_MULTIPLY,                MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED,                      MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_MULTIPLYCHECKED,         MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED_USER_DEFINED,         MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_MULTIPLYCHECKED,         MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_NOTEQUAL,                             MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_NOTEQUAL,                MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_NOTEQUAL_USER_DEFINED,                MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_NOTEQUAL,                MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 4, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_BOOL, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_OR,                                   MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_OR,                      MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_OR_USER_DEFINED,                      MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_OR,                      MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_ORELSE,                               MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_ORELSE,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_ORELSE_USER_DEFINED,                  MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_ORELSE,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_PARAMETER,                            MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_PARAMETER,               MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_PARAMETEREXPRESSION, 2, (int)PredefinedType.PT_TYPE, (int)PredefinedType.PT_STRING  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT,                           MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_RIGHTSHIFT,              MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT_USER_DEFINED,              MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_RIGHTSHIFT,              MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_SUBTRACT,                             MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_SUBTRACT,                MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_SUBTRACT_USER_DEFINED,                MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_SUBTRACT,                MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED,                      MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_SUBTRACTCHECKED,         MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED_USER_DEFINED,         MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_SUBTRACTCHECKED,         MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_UNARYPLUS_USER_DEFINED,               MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_PLUS  ,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_NEGATE,                               MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_NEGATE,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 1, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_NEGATE_USER_DEFINED,                  MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_NEGATE,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_NEGATECHECKED,                        MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_NEGATECHECKED,           MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 1, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_NEGATECHECKED_USER_DEFINED,           MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_NEGATECHECKED,           MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_CALL,                                 MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_CALL,                    MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_METHODCALLEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO, (int)MethodSignatureEnum.SIG_SZ_ARRAY, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_NEW,                                  MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_NEW,                     MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_NEWEXPRESSION, 2, (int)PredefinedType.PT_CONSTRUCTORINFO, (int)MethodSignatureEnum.SIG_SZ_ARRAY, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_NEW_MEMBERS,                          MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_NEW,                     MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_NEWEXPRESSION, 3, (int)PredefinedType.PT_CONSTRUCTORINFO, (int)PredefinedType.PT_G_IENUMERABLE, (int)PredefinedType.PT_EXPRESSION, (int)MethodSignatureEnum.SIG_SZ_ARRAY, (int)PredefinedType.PT_MEMBERINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_NEW_TYPE,                             MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_NEW,                     MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_NEWEXPRESSION, 1, (int)PredefinedType.PT_TYPE  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_QUOTE,                                MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_QUOTE,                   MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 1, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_ARRAYLENGTH,                          MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_ARRAYLENGTH,             MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 1, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_NOT,                                  MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_NOT,                     MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 1, (int)PredefinedType.PT_EXPRESSION }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_NOT_USER_DEFINED,                     MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_NOT,                     MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_NEWARRAYINIT,                         MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_NEWARRAYINIT,            MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_NEWARRAYEXPRESSION, 2, (int)PredefinedType.PT_TYPE, (int)MethodSignatureEnum.SIG_SZ_ARRAY, (int)PredefinedType.PT_EXPRESSION }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_PROPERTY,                             MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_EXPRESSION_PROPERTY,     MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_MEMBEREXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_PROPERTYINFO }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_INVOKE,                               MethodRequiredEnum.Optional,   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_INVOKE,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_INVOCATIONEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)MethodSignatureEnum.SIG_SZ_ARRAY, (int)PredefinedType.PT_EXPRESSION }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_METHODINFO_CREATEDELEGATE_TYPE_OBJECT,           MethodRequiredEnum.Optional,   PredefinedType.PT_METHODINFO,          PredefinedName.PN_CREATEDELEGATE,          MethodCallingConventionEnum.Virtual,    ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_DELEGATE, 2, (int)PredefinedType.PT_TYPE, (int)PredefinedType.PT_OBJECT}),
            new PredefinedMethodInfo(   PREDEFMETH.PM_G_OPTIONAL_CTOR,                                 MethodRequiredEnum.Optional,   PredefinedType.PT_G_OPTIONAL,          PredefinedName.PN_CTOR,                    MethodCallingConventionEnum.Instance,   ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_VOID, 1, (int)MethodSignatureEnum.SIG_CLASS_TYVAR, 0  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_G_OPTIONAL_GETHASVALUE,                          MethodRequiredEnum.Optional,   PredefinedType.PT_G_OPTIONAL,          PredefinedName.PN_GETHASVALUE,             MethodCallingConventionEnum.Instance,   ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BOOL, 0  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_G_OPTIONAL_GETVALUE,                             MethodRequiredEnum.Optional,   PredefinedType.PT_G_OPTIONAL,          PredefinedName.PN_GETVALUE,                MethodCallingConventionEnum.Instance,   ACCESS.ACC_PUBLIC,     0,  new int[] { (int)MethodSignatureEnum.SIG_CLASS_TYVAR, 0, 0  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_G_OPTIONAL_GET_VALUE_OR_DEF,                     MethodRequiredEnum.Optional,   PredefinedType.PT_G_OPTIONAL,          PredefinedName.PN_GET_VALUE_OR_DEF,        MethodCallingConventionEnum.Instance,   ACCESS.ACC_PUBLIC,     0,  new int[] { (int)MethodSignatureEnum.SIG_CLASS_TYVAR, 0, 0  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_STRING_CONCAT_OBJECT_1,                          MethodRequiredEnum.Optional,   PredefinedType.PT_STRING,              PredefinedName.PN_CONCAT,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_STRING, 1, (int)PredefinedType.PT_OBJECT  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_STRING_CONCAT_OBJECT_2,                          MethodRequiredEnum.Optional,   PredefinedType.PT_STRING,              PredefinedName.PN_CONCAT,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_STRING, 2, (int)PredefinedType.PT_OBJECT, (int)PredefinedType.PT_OBJECT  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_STRING_CONCAT_OBJECT_3,                          MethodRequiredEnum.Optional,   PredefinedType.PT_STRING,              PredefinedName.PN_CONCAT,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_STRING, 3, (int)PredefinedType.PT_OBJECT, (int)PredefinedType.PT_OBJECT, (int)PredefinedType.PT_OBJECT  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_STRING_CONCAT_STRING_1,                          MethodRequiredEnum.Optional,   PredefinedType.PT_STRING,              PredefinedName.PN_CONCAT,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_STRING, 1, (int)PredefinedType.PT_STRING  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_STRING_CONCAT_STRING_2,                          MethodRequiredEnum.Optional,   PredefinedType.PT_STRING,              PredefinedName.PN_CONCAT,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_STRING, 2, (int)PredefinedType.PT_STRING, (int)PredefinedType.PT_STRING  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_STRING_CONCAT_STRING_3,                          MethodRequiredEnum.Optional,   PredefinedType.PT_STRING,              PredefinedName.PN_CONCAT,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_STRING, 3, (int)PredefinedType.PT_STRING, (int)PredefinedType.PT_STRING, (int)PredefinedType.PT_STRING  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_STRING_CONCAT_STRING_4,                          MethodRequiredEnum.Optional,   PredefinedType.PT_STRING,              PredefinedName.PN_CONCAT,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_STRING, 4, (int)PredefinedType.PT_STRING, (int)PredefinedType.PT_STRING, (int)PredefinedType.PT_STRING, (int)PredefinedType.PT_STRING  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_STRING_CONCAT_SZ_OBJECT,                         MethodRequiredEnum.Optional,   PredefinedType.PT_STRING,              PredefinedName.PN_CONCAT,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_STRING, 1, (int)MethodSignatureEnum.SIG_SZ_ARRAY, (int)PredefinedType.PT_OBJECT  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_STRING_CONCAT_SZ_STRING,                         MethodRequiredEnum.Optional,   PredefinedType.PT_STRING,              PredefinedName.PN_CONCAT,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_STRING, 1, (int)MethodSignatureEnum.SIG_SZ_ARRAY, (int)PredefinedType.PT_STRING  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_STRING_GETCHARS,                                 MethodRequiredEnum.Optional,   PredefinedType.PT_STRING,              PredefinedName.PN_GETCHARS,                MethodCallingConventionEnum.Instance,   ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_CHAR, 1, (int)PredefinedType.PT_INT  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_STRING_GETLENGTH,                                MethodRequiredEnum.Optional,   PredefinedType.PT_STRING,              PredefinedName.PN_GETLENGTH,               MethodCallingConventionEnum.Instance,   ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_INT, 0,  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_STRING_OPEQUALITY,                               MethodRequiredEnum.Optional,   PredefinedType.PT_STRING,              PredefinedName.PN_OPEQUALITY,              MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BOOL, 2, (int)PredefinedType.PT_STRING, (int)PredefinedType.PT_STRING  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_STRING_OPINEQUALITY,                             MethodRequiredEnum.Optional,   PredefinedType.PT_STRING,              PredefinedName.PN_OPINEQUALITY,            MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BOOL, 2, (int)PredefinedType.PT_STRING, (int)PredefinedType.PT_STRING  }),
        };
    }
}
