// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        PM_DECIMAL_OPDECREMENT,
        PM_DECIMAL_OPINCREMENT,
        PM_DECIMAL_OPUNARYMINUS,

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
        PM_EXPRESSION_NEW_TYPE,
        PM_EXPRESSION_QUOTE,

        PM_EXPRESSION_NOT,
        PM_EXPRESSION_NOT_USER_DEFINED,


        PM_EXPRESSION_NEWARRAYINIT,
        PM_EXPRESSION_PROPERTY,


        PM_EXPRESSION_INVOKE,

        PM_G_OPTIONAL_CTOR,
        PM_G_OPTIONAL_GETVALUE,

        PM_STRING_CONCAT_OBJECT_2,
        PM_STRING_CONCAT_OBJECT_3,
        PM_STRING_CONCAT_STRING_2,
        PM_STRING_OPEQUALITY,
        PM_STRING_OPINEQUALITY,

        PM_COUNT
    }

    // enum identifying all predefined properties used in the C# compiler
    // Naming convention is PREDEFMETH.PM_ <Predefined CType> _ < Predefined Name of Property>
    // Keep this list sorted by containing type and name.
    internal enum PREDEFPROP
    {
        PP_G_OPTIONAL_VALUE,
        PP_COUNT,
    };

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
        SIG_SZ_ARRAY                            // must be followed by signature type of array elements
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

        public PredefinedMethodInfo(PREDEFMETH method, PredefinedType type, PredefinedName name, MethodCallingConventionEnum callingConvention, ACCESS access, int cTypeVars, int[] signature)
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

        public PredefinedPropertyInfo(PREDEFPROP property, PredefinedName name, PREDEFMETH getter)
        {
            this.property = property;
            this.name = name;
            this.getter = getter;
        }
    }

    // Loads and caches predefined members.
    // Also finds constructors on delegate types.
    internal static class PredefinedMembers
    {
        private static readonly MethodSymbol[] _methods = new MethodSymbol[(int)PREDEFMETH.PM_COUNT];
        private static readonly PropertySymbol[] _properties = new PropertySymbol[(int)PREDEFPROP.PP_COUNT];

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static PropertySymbol LoadProperty(PREDEFPROP property)
        {
            PredefinedPropertyInfo info = GetPropInfo(property);
            return LoadProperty(property, NameManager.GetPredefinedName(info.name), info.getter);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static PropertySymbol LoadProperty(
            PREDEFPROP predefProp,
            Name propertyName,
            PREDEFMETH propertyGetter)
        {
            Debug.Assert(propertyName != null);
            Debug.Assert(propertyGetter >= 0 && propertyGetter < PREDEFMETH.PM_COUNT);

            SymbolTable.AddPredefinedPropertyToSymbolTable(
                GetPredefAgg(GetPropPredefType(predefProp)), propertyName);
            MethodSymbol getter = GetMethod(propertyGetter);

            getter.SetMethKind(MethodKindEnum.PropAccessor);
            PropertySymbol property = getter.getProperty();
            Debug.Assert(property != null);
            return property;
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static AggregateSymbol GetPredefAgg(PredefinedType pt) => SymbolLoader.GetPredefAgg(pt);

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static CType LoadTypeFromSignature(int[] signature, ref int indexIntoSignatures, TypeArray classTyVars)
        {
            Debug.Assert(signature != null);

            MethodSignatureEnum current = (MethodSignatureEnum)signature[indexIntoSignatures];
            indexIntoSignatures++;

            switch (current)
            {
                case MethodSignatureEnum.SIG_SZ_ARRAY:
                    return TypeManager.GetArray(LoadTypeFromSignature(signature, ref indexIntoSignatures, classTyVars), 1, true);

                case MethodSignatureEnum.SIG_METH_TYVAR:
                    return TypeManager.GetStdMethTypeVar(signature[indexIntoSignatures++]);

                case MethodSignatureEnum.SIG_CLASS_TYVAR:
                    return classTyVars[signature[indexIntoSignatures++]];

                case (MethodSignatureEnum)PredefinedType.PT_VOID:
                    return VoidType.Instance;

                default:
                    Debug.Assert(current >= 0 && (int)current < (int)PredefinedType.PT_COUNT);
                    AggregateSymbol agg = GetPredefAgg((PredefinedType)current);
                    int typeCount = agg.GetTypeVars().Count;
                    if (typeCount == 0)
                    {
                        return TypeManager.GetAggregate(agg, TypeArray.Empty);
                    }

                    CType[] typeArgs = new CType[typeCount];
                    for (int iTypeArg = 0; iTypeArg < typeArgs.Length; iTypeArg++)
                    {
                        typeArgs[iTypeArg] = LoadTypeFromSignature(signature, ref indexIntoSignatures, classTyVars);
                    }

                    return TypeManager.GetAggregate(agg, TypeArray.Allocate(typeArgs));
            }
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static TypeArray LoadTypeArrayFromSignature(int[] signature, ref int indexIntoSignatures, TypeArray classTyVars)
        {
            Debug.Assert(signature != null);

            int count = signature[indexIntoSignatures];
            indexIntoSignatures++;

            Debug.Assert(count >= 0);

            CType[] ptypes = new CType[count];
            for (int i = 0; i < ptypes.Length; i++)
            {
                ptypes[i] = LoadTypeFromSignature(signature, ref indexIntoSignatures, classTyVars);
            }

            return TypeArray.Allocate(ptypes);
        }

#if DEBUG
        static PredefinedMembers()
        {
            // validate the tables
            for (int i = 0; i < (int)PREDEFMETH.PM_COUNT; i++)
            {
                Debug.Assert((int)GetMethInfo((PREDEFMETH)i).method == i);
            }
            for (int i = 0; i < (int)PREDEFPROP.PP_COUNT; i++)
            {
                Debug.Assert((int)GetPropInfo((PREDEFPROP)i).property == i);
            }
        }
#endif

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        public static PropertySymbol GetProperty(PREDEFPROP property)
        {
            Debug.Assert(property >= 0 && property < PREDEFPROP.PP_COUNT);
            return _properties[(int)property] ??= LoadProperty(property);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        public static MethodSymbol GetMethod(PREDEFMETH method)
        {
            Debug.Assert(method >= 0 && method < PREDEFMETH.PM_COUNT);
            return _methods[(int)method] ??= LoadMethod(method);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static MethodSymbol LoadMethod(
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
            Debug.Assert(type != null);
            TypeArray classTyVars = type.GetTypeVarsAll();

            int index = 0;
            CType returnType = LoadTypeFromSignature(signature, ref index, classTyVars);
            Debug.Assert(returnType != null);

            TypeArray argumentTypes = LoadTypeArrayFromSignature(signature, ref index, classTyVars);
            Debug.Assert(argumentTypes != null);

            MethodSymbol ret = LookupMethodWhileLoading(type, cMethodTyVars, methodName, methodAccess, isStatic, isVirtual, returnType, argumentTypes);

            if (ret == null)
            {
                SymbolTable.AddPredefinedMethodToSymbolTable(type, methodName);
                ret = LookupMethodWhileLoading(type, cMethodTyVars, methodName, methodAccess, isStatic, isVirtual, returnType, argumentTypes);
            }
            return ret;
        }

        private static MethodSymbol LookupMethodWhileLoading(AggregateSymbol type, int cMethodTyVars, Name methodName, ACCESS methodAccess, bool isStatic, bool isVirtual, CType returnType, TypeArray argumentTypes)
        {
            for (Symbol sym = SymbolLoader.LookupAggMember(methodName, type, symbmask_t.MASK_ALL);
                 sym != null;
                 sym = sym.LookupNext(symbmask_t.MASK_ALL))
            {
                if (sym is MethodSymbol methsym)
                {
                    if ((methsym.GetAccess() == methodAccess || methodAccess == ACCESS.ACC_UNKNOWN) &&
                        methsym.isStatic == isStatic &&
                        methsym.isVirtual == isVirtual &&
                        methsym.typeVars.Count == cMethodTyVars &&
                        TypeManager.SubstEqualTypes(methsym.RetType, returnType, null, methsym.typeVars, true) &&
                        TypeManager.SubstEqualTypeArrays(methsym.Params, argumentTypes, null, methsym.typeVars))
                    {
                        return methsym;
                    }
                }
            }
            return null;
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static MethodSymbol LoadMethod(PREDEFMETH method)
        {
            PredefinedMethodInfo info = GetMethInfo(method);
            return LoadMethod(
                        GetPredefAgg(info.type),
                        info.signature,
                        info.cTypeVars,
                        NameManager.GetPredefinedName(info.name),
                        info.access,
                        info.callingConvention == MethodCallingConventionEnum.Static,
                        info.callingConvention == MethodCallingConventionEnum.Virtual);
        }

        private static PREDEFMETH GetPropGetter(PREDEFPROP property)
        {
            PREDEFMETH result = GetPropInfo(property).getter;

            // getters are MethodRequiredEnum.Required
            Debug.Assert(result >= 0 && result < PREDEFMETH.PM_COUNT);

            return result;
        }

        private static PredefinedType GetPropPredefType(PREDEFPROP property)
        {
            return GetMethInfo(GetPropGetter(property)).type;
        }

        // the list of predefined property definitions.
        // This list must be in the same order as the PREDEFPROP enum.
        private static readonly PredefinedPropertyInfo[] s_predefinedProperties = {
            new PredefinedPropertyInfo(PREDEFPROP.PP_G_OPTIONAL_VALUE,  PredefinedName.PN_CAP_VALUE,    PREDEFMETH.PM_G_OPTIONAL_GETVALUE)
        };

        private static PredefinedPropertyInfo GetPropInfo(PREDEFPROP property)
        {
            Debug.Assert(property >= 0 && property < PREDEFPROP.PP_COUNT);
            Debug.Assert(s_predefinedProperties[(int)property].property == property);

            return s_predefinedProperties[(int)property];
        }

        private static PredefinedMethodInfo GetMethInfo(PREDEFMETH method)
        {
            Debug.Assert(method >= 0 && method < PREDEFMETH.PM_COUNT);
            Debug.Assert(s_predefinedMethods[(int)method].method == method);

            return s_predefinedMethods[(int)method];
        }

        // the list of predefined method definitions.
        // This list must be in the same order as the PREDEFMETH enum.
        private static readonly PredefinedMethodInfo[] s_predefinedMethods = new PredefinedMethodInfo[(int)PREDEFMETH.PM_COUNT] {
            new PredefinedMethodInfo(   PREDEFMETH.PM_DECIMAL_OPDECREMENT,                             PredefinedType.PT_DECIMAL,             PredefinedName.PN_OPDECREMENT,             MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_DECIMAL, 1, (int)PredefinedType.PT_DECIMAL  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DECIMAL_OPINCREMENT,                             PredefinedType.PT_DECIMAL,             PredefinedName.PN_OPINCREMENT,             MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_DECIMAL, 1, (int)PredefinedType.PT_DECIMAL  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DECIMAL_OPUNARYMINUS,                            PredefinedType.PT_DECIMAL,             PredefinedName.PN_OPUNARYMINUS,            MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_DECIMAL, 1, (int)PredefinedType.PT_DECIMAL  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DELEGATE_COMBINE,                                PredefinedType.PT_DELEGATE,            PredefinedName.PN_COMBINE,                 MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_DELEGATE, 2, (int)PredefinedType.PT_DELEGATE, (int)PredefinedType.PT_DELEGATE  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DELEGATE_OPEQUALITY,                             PredefinedType.PT_DELEGATE,            PredefinedName.PN_OPEQUALITY,              MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BOOL, 2, (int)PredefinedType.PT_DELEGATE, (int)PredefinedType.PT_DELEGATE  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DELEGATE_OPINEQUALITY,                           PredefinedType.PT_DELEGATE,            PredefinedName.PN_OPINEQUALITY,            MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BOOL, 2, (int)PredefinedType.PT_DELEGATE, (int)PredefinedType.PT_DELEGATE  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_DELEGATE_REMOVE,                                 PredefinedType.PT_DELEGATE,            PredefinedName.PN_REMOVE,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_DELEGATE, 2, (int)PredefinedType.PT_DELEGATE, (int)PredefinedType.PT_DELEGATE  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_ADD,                                  PredefinedType.PT_EXPRESSION,          PredefinedName.PN_ADD,                     MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_ADD_USER_DEFINED,                     PredefinedType.PT_EXPRESSION,          PredefinedName.PN_ADD,                     MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_ADDCHECKED,                           PredefinedType.PT_EXPRESSION,          PredefinedName.PN_ADDCHECKED,              MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_ADDCHECKED_USER_DEFINED,              PredefinedType.PT_EXPRESSION,          PredefinedName.PN_ADDCHECKED,              MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_AND,                                  PredefinedType.PT_EXPRESSION,          PredefinedName.PN_AND,                     MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_AND_USER_DEFINED,                     PredefinedType.PT_EXPRESSION,          PredefinedName.PN_AND,                     MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_ANDALSO,                              PredefinedType.PT_EXPRESSION,          PredefinedName.PN_ANDALSO,                 MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_ANDALSO_USER_DEFINED,                 PredefinedType.PT_EXPRESSION,          PredefinedName.PN_ANDALSO,                 MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_ARRAYINDEX,                           PredefinedType.PT_EXPRESSION,          PredefinedName.PN_ARRAYINDEX,              MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_ARRAYINDEX2,                          PredefinedType.PT_EXPRESSION,          PredefinedName.PN_ARRAYINDEX,              MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_METHODCALLEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)MethodSignatureEnum.SIG_SZ_ARRAY, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_ASSIGN,                               PredefinedType.PT_EXPRESSION,          PredefinedName.PN_ASSIGN,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_CONSTANT_OBJECT_TYPE,                 PredefinedType.PT_EXPRESSION,          PredefinedName.PN_CONSTANT,                MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_CONSTANTEXPRESSION, 2, (int)PredefinedType.PT_OBJECT, (int)PredefinedType.PT_TYPE  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_CONVERT,                              PredefinedType.PT_EXPRESSION,          PredefinedName.PN_CONVERT,                 MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_TYPE  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_CONVERT_USER_DEFINED,                 PredefinedType.PT_EXPRESSION,          PredefinedName.PN_CONVERT,                 MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_TYPE, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED,                       PredefinedType.PT_EXPRESSION,          PredefinedName.PN_CONVERTCHECKED,          MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_TYPE  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED_USER_DEFINED,          PredefinedType.PT_EXPRESSION,          PredefinedName.PN_CONVERTCHECKED,          MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_TYPE, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_DIVIDE,                               PredefinedType.PT_EXPRESSION,          PredefinedName.PN_DIVIDE,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_DIVIDE_USER_DEFINED,                  PredefinedType.PT_EXPRESSION,          PredefinedName.PN_DIVIDE,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_EQUAL,                                PredefinedType.PT_EXPRESSION,          PredefinedName.PN_EQUAL,                   MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_EQUAL_USER_DEFINED,                   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_EQUAL,                   MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 4, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_BOOL, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR,                          PredefinedType.PT_EXPRESSION,          PredefinedName.PN_EXCLUSIVEOR,             MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR_USER_DEFINED,             PredefinedType.PT_EXPRESSION,          PredefinedName.PN_EXCLUSIVEOR,             MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_FIELD,                                PredefinedType.PT_EXPRESSION,          PredefinedName.PN_CAP_FIELD,               MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_MEMBEREXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_FIELDINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_GREATERTHAN,                          PredefinedType.PT_EXPRESSION,          PredefinedName.PN_GREATERTHAN,             MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_GREATERTHAN_USER_DEFINED,             PredefinedType.PT_EXPRESSION,          PredefinedName.PN_GREATERTHAN,             MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 4, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_BOOL, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL,                   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_GREATERTHANOREQUAL,      MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL_USER_DEFINED,      PredefinedType.PT_EXPRESSION,          PredefinedName.PN_GREATERTHANOREQUAL,      MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 4, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_BOOL, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_LAMBDA,                               PredefinedType.PT_EXPRESSION,          PredefinedName.PN_LAMBDA,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     1,  new int[] { (int)PredefinedType.PT_G_EXPRESSION, (int)MethodSignatureEnum.SIG_METH_TYVAR, 0, 2, (int)PredefinedType.PT_EXPRESSION, (int)MethodSignatureEnum.SIG_SZ_ARRAY, (int)PredefinedType.PT_PARAMETEREXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_LEFTSHIFT,                            PredefinedType.PT_EXPRESSION,          PredefinedName.PN_LEFTSHIFT,               MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_LEFTSHIFT_USER_DEFINED,               PredefinedType.PT_EXPRESSION,          PredefinedName.PN_LEFTSHIFT,               MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_LESSTHAN,                             PredefinedType.PT_EXPRESSION,          PredefinedName.PN_LESSTHAN,                MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_LESSTHAN_USER_DEFINED,                PredefinedType.PT_EXPRESSION,          PredefinedName.PN_LESSTHAN,                MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 4, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_BOOL, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL,                      PredefinedType.PT_EXPRESSION,          PredefinedName.PN_LESSTHANOREQUAL,         MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL_USER_DEFINED,         PredefinedType.PT_EXPRESSION,          PredefinedName.PN_LESSTHANOREQUAL,         MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 4, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_BOOL, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_MODULO,                               PredefinedType.PT_EXPRESSION,          PredefinedName.PN_MODULO,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_MODULO_USER_DEFINED,                  PredefinedType.PT_EXPRESSION,          PredefinedName.PN_MODULO,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_MULTIPLY,                             PredefinedType.PT_EXPRESSION,          PredefinedName.PN_MULTIPLY,                MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_MULTIPLY_USER_DEFINED,                PredefinedType.PT_EXPRESSION,          PredefinedName.PN_MULTIPLY,                MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED,                      PredefinedType.PT_EXPRESSION,          PredefinedName.PN_MULTIPLYCHECKED,         MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED_USER_DEFINED,         PredefinedType.PT_EXPRESSION,          PredefinedName.PN_MULTIPLYCHECKED,         MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_NOTEQUAL,                             PredefinedType.PT_EXPRESSION,          PredefinedName.PN_NOTEQUAL,                MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_NOTEQUAL_USER_DEFINED,                PredefinedType.PT_EXPRESSION,          PredefinedName.PN_NOTEQUAL,                MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 4, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_BOOL, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_OR,                                   PredefinedType.PT_EXPRESSION,          PredefinedName.PN_OR,                      MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_OR_USER_DEFINED,                      PredefinedType.PT_EXPRESSION,          PredefinedName.PN_OR,                      MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_ORELSE,                               PredefinedType.PT_EXPRESSION,          PredefinedName.PN_ORELSE,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_ORELSE_USER_DEFINED,                  PredefinedType.PT_EXPRESSION,          PredefinedName.PN_ORELSE,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_PARAMETER,                            PredefinedType.PT_EXPRESSION,          PredefinedName.PN_PARAMETER,               MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_PARAMETEREXPRESSION, 2, (int)PredefinedType.PT_TYPE, (int)PredefinedType.PT_STRING  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT,                           PredefinedType.PT_EXPRESSION,          PredefinedName.PN_RIGHTSHIFT,              MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT_USER_DEFINED,              PredefinedType.PT_EXPRESSION,          PredefinedName.PN_RIGHTSHIFT,              MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_SUBTRACT,                             PredefinedType.PT_EXPRESSION,          PredefinedName.PN_SUBTRACT,                MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_SUBTRACT_USER_DEFINED,                PredefinedType.PT_EXPRESSION,          PredefinedName.PN_SUBTRACT,                MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED,                      PredefinedType.PT_EXPRESSION,          PredefinedName.PN_SUBTRACTCHECKED,         MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED_USER_DEFINED,         PredefinedType.PT_EXPRESSION,          PredefinedName.PN_SUBTRACTCHECKED,         MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BINARYEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_UNARYPLUS_USER_DEFINED,               PredefinedType.PT_EXPRESSION,          PredefinedName.PN_PLUS,                    MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_NEGATE,                               PredefinedType.PT_EXPRESSION,          PredefinedName.PN_NEGATE,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 1, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_NEGATE_USER_DEFINED,                  PredefinedType.PT_EXPRESSION,          PredefinedName.PN_NEGATE,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_NEGATECHECKED,                        PredefinedType.PT_EXPRESSION,          PredefinedName.PN_NEGATECHECKED,           MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 1, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_NEGATECHECKED_USER_DEFINED,           PredefinedType.PT_EXPRESSION,          PredefinedName.PN_NEGATECHECKED,           MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_CALL,                                 PredefinedType.PT_EXPRESSION,          PredefinedName.PN_CALL,                    MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_METHODCALLEXPRESSION, 3, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO, (int)MethodSignatureEnum.SIG_SZ_ARRAY, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_NEW,                                  PredefinedType.PT_EXPRESSION,          PredefinedName.PN_NEW,                     MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_NEWEXPRESSION, 2, (int)PredefinedType.PT_CONSTRUCTORINFO, (int)MethodSignatureEnum.SIG_SZ_ARRAY, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_NEW_TYPE,                             PredefinedType.PT_EXPRESSION,          PredefinedName.PN_NEW,                     MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_NEWEXPRESSION, 1, (int)PredefinedType.PT_TYPE  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_QUOTE,                                PredefinedType.PT_EXPRESSION,          PredefinedName.PN_QUOTE,                   MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 1, (int)PredefinedType.PT_EXPRESSION  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_NOT,                                  PredefinedType.PT_EXPRESSION,          PredefinedName.PN_NOT,                     MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 1, (int)PredefinedType.PT_EXPRESSION }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_NOT_USER_DEFINED,                     PredefinedType.PT_EXPRESSION,          PredefinedName.PN_NOT,                     MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_UNARYEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_METHODINFO }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_NEWARRAYINIT,                         PredefinedType.PT_EXPRESSION,          PredefinedName.PN_NEWARRAYINIT,            MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_NEWARRAYEXPRESSION, 2, (int)PredefinedType.PT_TYPE, (int)MethodSignatureEnum.SIG_SZ_ARRAY, (int)PredefinedType.PT_EXPRESSION }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_PROPERTY,                             PredefinedType.PT_EXPRESSION,          PredefinedName.PN_EXPRESSION_PROPERTY,     MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_MEMBEREXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)PredefinedType.PT_PROPERTYINFO }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_EXPRESSION_INVOKE,                               PredefinedType.PT_EXPRESSION,          PredefinedName.PN_INVOKE,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_INVOCATIONEXPRESSION, 2, (int)PredefinedType.PT_EXPRESSION, (int)MethodSignatureEnum.SIG_SZ_ARRAY, (int)PredefinedType.PT_EXPRESSION }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_G_OPTIONAL_CTOR,                                 PredefinedType.PT_G_OPTIONAL,          PredefinedName.PN_CTOR,                    MethodCallingConventionEnum.Instance,   ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_VOID, 1, (int)MethodSignatureEnum.SIG_CLASS_TYVAR, 0  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_G_OPTIONAL_GETVALUE,                             PredefinedType.PT_G_OPTIONAL,          PredefinedName.PN_GETVALUE,                MethodCallingConventionEnum.Instance,   ACCESS.ACC_PUBLIC,     0,  new int[] { (int)MethodSignatureEnum.SIG_CLASS_TYVAR, 0, 0  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_STRING_CONCAT_OBJECT_2,                          PredefinedType.PT_STRING,              PredefinedName.PN_CONCAT,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_STRING, 2, (int)PredefinedType.PT_OBJECT, (int)PredefinedType.PT_OBJECT  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_STRING_CONCAT_OBJECT_3,                          PredefinedType.PT_STRING,              PredefinedName.PN_CONCAT,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_STRING, 3, (int)PredefinedType.PT_OBJECT, (int)PredefinedType.PT_OBJECT, (int)PredefinedType.PT_OBJECT  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_STRING_CONCAT_STRING_2,                          PredefinedType.PT_STRING,              PredefinedName.PN_CONCAT,                  MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_STRING, 2, (int)PredefinedType.PT_STRING, (int)PredefinedType.PT_STRING  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_STRING_OPEQUALITY,                               PredefinedType.PT_STRING,              PredefinedName.PN_OPEQUALITY,              MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BOOL, 2, (int)PredefinedType.PT_STRING, (int)PredefinedType.PT_STRING  }),
            new PredefinedMethodInfo(   PREDEFMETH.PM_STRING_OPINEQUALITY,                             PredefinedType.PT_STRING,              PredefinedName.PN_OPINEQUALITY,            MethodCallingConventionEnum.Static,     ACCESS.ACC_PUBLIC,     0,  new int[] { (int)PredefinedType.PT_BOOL, 2, (int)PredefinedType.PT_STRING, (int)PredefinedType.PT_STRING  }),
        };
    }
}
