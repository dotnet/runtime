// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// 

namespace System.Reflection.Emit {
    using System;
    using System.Reflection;
    using System.Security;
    using System.Security.Permissions;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Collections.Generic;
    using CultureInfo = System.Globalization.CultureInfo;
    using System.Threading;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;


    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum PackingSize
    {
        Unspecified                 = 0,
        Size1                       = 1,
        Size2                       = 2,
        Size4                       = 4,
        Size8                       = 8,
        Size16                      = 16,
        Size32                      = 32,
        Size64                      = 64,
        Size128                     = 128,
    }

    [HostProtection(MayLeakOnAbort = true)]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(_TypeBuilder))]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class TypeBuilder : TypeInfo, _TypeBuilder
    {
        public override bool IsAssignableFrom(System.Reflection.TypeInfo typeInfo){
            if(typeInfo==null) return false;            
            return IsAssignableFrom(typeInfo.AsType());
        }

        #region Declarations
        private class CustAttr
        {
            private ConstructorInfo m_con;
            private byte[] m_binaryAttribute;
            private CustomAttributeBuilder m_customBuilder;
            
            public CustAttr(ConstructorInfo con, byte[] binaryAttribute)
            {
                if (con == null)
                    throw new ArgumentNullException("con");

                if (binaryAttribute == null)
                    throw new ArgumentNullException("binaryAttribute");
                Contract.EndContractBlock();

                m_con = con;
                m_binaryAttribute = binaryAttribute;
            }

            public CustAttr(CustomAttributeBuilder customBuilder)
            {
                if (customBuilder == null)
                    throw new ArgumentNullException("customBuilder");
                Contract.EndContractBlock();

                m_customBuilder = customBuilder;
            }

            [System.Security.SecurityCritical]  // auto-generated
            public void Bake(ModuleBuilder module, int token)
            {
                if (m_customBuilder == null)
                {
                    TypeBuilder.DefineCustomAttribute(module, token, module.GetConstructorToken(m_con).Token,
                        m_binaryAttribute, false, false);
                }
                else
                {
                    m_customBuilder.CreateCustomAttribute(module, token);
                }
            }
        }
        #endregion
        
        #region Public Static Methods
        public static MethodInfo GetMethod(Type type, MethodInfo method)
        {
            if (!(type is TypeBuilder) && !(type is TypeBuilderInstantiation))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeTypeBuilder"));

            // The following checks establishes invariants that more simply put require type to be generic and 
            // method to be a generic method definition declared on the generic type definition of type.
            // To create generic method G<Foo>.M<Bar> these invariants require that G<Foo>.M<S> be created by calling 
            // this function followed by MakeGenericMethod on the resulting MethodInfo to finally get G<Foo>.M<Bar>.
            // We could also allow G<T>.M<Bar> to be created before G<Foo>.M<Bar> (BindGenParm followed by this method) 
            // if we wanted to but that just complicates things so these checks are designed to prevent that scenario.
            
            if (method.IsGenericMethod && !method.IsGenericMethodDefinition)
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedGenericMethodDefinition"), "method");
        
            if (method.DeclaringType == null || !method.DeclaringType.IsGenericTypeDefinition)
                throw new ArgumentException(Environment.GetResourceString("Argument_MethodNeedGenericDeclaringType"), "method");
        
            if (type.GetGenericTypeDefinition() != method.DeclaringType)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidMethodDeclaringType"), "type");
            Contract.EndContractBlock();

            // The following converts from Type or TypeBuilder of G<T> to TypeBuilderInstantiation G<T>. These types
            // both logically represent the same thing. The runtime displays a similar convention by having 
            // G<M>.M() be encoded by a typeSpec whose parent is the typeDef for G<M> and whose instantiation is also G<M>.
            if (type.IsGenericTypeDefinition)
                type = type.MakeGenericType(type.GetGenericArguments());
        
            if (!(type is TypeBuilderInstantiation))
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), "type");

            return MethodOnTypeBuilderInstantiation.GetMethod(method, type as TypeBuilderInstantiation);
        }
        public static ConstructorInfo GetConstructor(Type type, ConstructorInfo constructor)
        {            
            if (!(type is TypeBuilder) && !(type is TypeBuilderInstantiation))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeTypeBuilder"));

            if (!constructor.DeclaringType.IsGenericTypeDefinition)
                throw new ArgumentException(Environment.GetResourceString("Argument_ConstructorNeedGenericDeclaringType"), "constructor");
            Contract.EndContractBlock();
        
            if (!(type is TypeBuilderInstantiation))
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), "type");

            // TypeBuilder G<T> ==> TypeBuilderInstantiation G<T>
            if (type is TypeBuilder && type.IsGenericTypeDefinition)
                type = type.MakeGenericType(type.GetGenericArguments());

            if (type.GetGenericTypeDefinition() != constructor.DeclaringType)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidConstructorDeclaringType"), "type");

            return ConstructorOnTypeBuilderInstantiation.GetConstructor(constructor, type as TypeBuilderInstantiation);
        }
        public static FieldInfo GetField(Type type, FieldInfo field)
        {
            if (!(type is TypeBuilder) && !(type is TypeBuilderInstantiation))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeTypeBuilder"));

            if (!field.DeclaringType.IsGenericTypeDefinition)
                throw new ArgumentException(Environment.GetResourceString("Argument_FieldNeedGenericDeclaringType"), "field");
            Contract.EndContractBlock();
        
            if (!(type is TypeBuilderInstantiation))
                throw new ArgumentException(Environment.GetResourceString("Argument_NeedNonGenericType"), "type");

            // TypeBuilder G<T> ==> TypeBuilderInstantiation G<T>
            if (type is TypeBuilder && type.IsGenericTypeDefinition)
                type = type.MakeGenericType(type.GetGenericArguments());

            if (type.GetGenericTypeDefinition() != field.DeclaringType)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFieldDeclaringType"), "type");

            return FieldOnTypeBuilderInstantiation.GetField(field, type as TypeBuilderInstantiation);
        }
        #endregion

        #region Public Const
        public const int UnspecifiedTypeSize = 0;
        #endregion

        #region Private Static FCalls
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
        private static extern void SetParentType(RuntimeModule module, int tdTypeDef, int tkParent);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
        private static extern void AddInterfaceImpl(RuntimeModule module, int tdTypeDef, int tkInterface);
        #endregion

        #region Internal Static FCalls
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
        internal static extern int DefineMethod(RuntimeModule module, int tkParent, String name, byte[] signature, int sigLength, 
            MethodAttributes attributes);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
        internal static extern int DefineMethodSpec(RuntimeModule module, int tkParent, byte[] signature, int sigLength);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
        internal static extern int DefineField(RuntimeModule module, int tkParent, String name, byte[] signature, int sigLength, 
            FieldAttributes attributes);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
        private static extern void SetMethodIL(RuntimeModule module, int tk, bool isInitLocals,  
            byte[] body, int bodyLength,
            byte[] LocalSig, int sigLength, 
            int maxStackSize,
            ExceptionHandler[] exceptions, int numExceptions, 
            int [] tokenFixups, int numTokenFixups);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
        private static extern void DefineCustomAttribute(RuntimeModule module, int tkAssociate, int tkConstructor, 
            byte[] attr, int attrLength, bool toDisk, bool updateCompilerFlags);

        [System.Security.SecurityCritical]  // auto-generated
        internal static void DefineCustomAttribute(ModuleBuilder module, int tkAssociate, int tkConstructor,
            byte[] attr, bool toDisk, bool updateCompilerFlags)
        {
            byte[] localAttr = null;

            if (attr != null)
            {
                localAttr = new byte[attr.Length];
                Buffer.BlockCopy(attr, 0, localAttr, 0, attr.Length);
            }

            DefineCustomAttribute(module.GetNativeHandle(), tkAssociate, tkConstructor, 
                localAttr, (localAttr != null) ? localAttr.Length : 0, toDisk, updateCompilerFlags);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
        internal static extern void SetPInvokeData(RuntimeModule module, String DllName, String name, int token, int linkFlags);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
        internal static extern int DefineProperty(RuntimeModule module, int tkParent, String name, PropertyAttributes attributes,
            byte[] signature, int sigLength);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
        internal static extern int DefineEvent(RuntimeModule module, int tkParent, String name, EventAttributes attributes, int tkEventType);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
        internal static extern void DefineMethodSemantics(RuntimeModule module, int tkAssociation, 
            MethodSemanticsAttributes semantics, int tkMethod);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
        internal static extern void DefineMethodImpl(RuntimeModule module, int tkType, int tkBody, int tkDecl);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
        internal static extern void SetMethodImpl(RuntimeModule module, int tkMethod, MethodImplAttributes MethodImplAttributes);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
        internal static extern int SetParamInfo(RuntimeModule module, int tkMethod, int iSequence, 
            ParameterAttributes iParamAttributes, String strParamName);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
        internal static extern int GetTokenFromSig(RuntimeModule module, byte[] signature, int sigLength);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
        internal static extern void SetFieldLayoutOffset(RuntimeModule module, int fdToken, int iOffset);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
        internal static extern void SetClassLayout(RuntimeModule module, int tk, PackingSize iPackingSize, int iTypeSize);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
        internal static extern void SetFieldMarshal(RuntimeModule module, int tk, byte[] ubMarshal, int ubSize);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
        private static extern unsafe void SetConstantValue(RuntimeModule module, int tk, int corType, void* pValue);

#if FEATURE_CAS_POLICY
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
        internal static extern void AddDeclarativeSecurity(RuntimeModule module, int parent, SecurityAction action, byte[] blob, int cb);
#endif
        #endregion

        #region Internal\Private Static Members
        private static bool IsPublicComType(Type type)
        {
            // Internal Helper to determine if a type should be added to ComType table.
            // A top level type should be added if it is Public.
            // A nested type should be added if the top most enclosing type is Public 
            //      and all the enclosing types are NestedPublic

            Type enclosingType = type.DeclaringType;
            if (enclosingType != null)
            {
                if (IsPublicComType(enclosingType))
                {
                    if ((type.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic)
                    {
                        return true;
                    }
                }
            }
            else
            {
                if ((type.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public)
                {
                    return true;
                }
            }

            return false;
        }

        [Pure]
        internal static bool IsTypeEqual(Type t1, Type t2)
        {
            // Maybe we are lucky that they are equal in the first place
            if (t1 == t2)
                return true;
            TypeBuilder tb1 = null;
            TypeBuilder tb2 = null;  
            Type runtimeType1 = null;              
            Type runtimeType2 = null;    
            
            // set up the runtimeType and TypeBuilder type corresponding to t1 and t2
            if (t1 is TypeBuilder)
            {
                tb1 =(TypeBuilder)t1;
                // This will be null if it is not baked.
                runtimeType1 = tb1.m_bakedRuntimeType;
            }
            else
            {
                runtimeType1 = t1;
            }

            if (t2 is TypeBuilder)
            {
                tb2 =(TypeBuilder)t2;
                // This will be null if it is not baked.
                runtimeType2 = tb2.m_bakedRuntimeType;
            }
            else
            {
                runtimeType2 = t2;
            }
                
            // If the type builder view is eqaul then it is equal                
            if (tb1 != null && tb2 != null && Object.ReferenceEquals(tb1, tb2))
                return true;

            // if the runtimetype view is eqaul than it is equal                
            if (runtimeType1 != null && runtimeType2 != null && runtimeType1 == runtimeType2)                
                return true;

            return false;                
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static unsafe void SetConstantValue(ModuleBuilder module, int tk, Type destType, Object value)
        {
            // This is a helper function that is used by ParameterBuilder, PropertyBuilder,
            // and FieldBuilder to validate a default value and save it in the meta-data.

            if (value != null)
            {
                Type type = value.GetType();

                // We should allow setting a constant value on a ByRef parameter
                if (destType.IsByRef)
                    destType = destType.GetElementType();

                if (destType.IsEnum)
                {
                    //                                   |  UnderlyingSystemType     |  Enum.GetUnderlyingType() |  IsEnum
                    // ----------------------------------|---------------------------|---------------------------|---------
                    // runtime Enum Type                 |  self                     |  underlying type of enum  |  TRUE
                    // EnumBuilder                       |  underlying type of enum  |  underlying type of enum* |  TRUE
                    // TypeBuilder of enum types**       |  underlying type of enum  |  Exception                |  TRUE
                    // TypeBuilder of enum types (baked) |  runtime enum type        |  Exception                |  TRUE

                    //  *: the behavior of Enum.GetUnderlyingType(EnumBuilder) might change in the future
                    //     so let's not depend on it.
                    // **: created with System.Enum as the parent type.

                    // The above behaviors might not be the most consistent but we have to live with them.

                    Type underlyingType;
                    EnumBuilder enumBldr;
                    TypeBuilder typeBldr;
                    if ((enumBldr = destType as EnumBuilder) != null)
                    {
                        underlyingType = enumBldr.GetEnumUnderlyingType();

                        // The constant value supplied should match either the baked enum type or its underlying type
                        // we don't need to compare it with the EnumBuilder itself because you can never have an object of that type
                        if (type != enumBldr.m_typeBuilder.m_bakedRuntimeType && type != underlyingType)
                            throw new ArgumentException(Environment.GetResourceString("Argument_ConstantDoesntMatch"));
                    }
                    else if ((typeBldr = destType as TypeBuilder) != null)
                    {
                        underlyingType = typeBldr.m_enumUnderlyingType;

                        // The constant value supplied should match either the baked enum type or its underlying type
                        // typeBldr.m_enumUnderlyingType is null if the user hasn't created a "value__" field on the enum
                        if (underlyingType == null || (type != typeBldr.UnderlyingSystemType && type != underlyingType))
                            throw new ArgumentException(Environment.GetResourceString("Argument_ConstantDoesntMatch"));
                    }
                    else // must be a runtime Enum Type
                    {
                        Contract.Assert(destType is RuntimeType, "destType is not a runtime type, an EnumBuilder, or a TypeBuilder.");

                        underlyingType = Enum.GetUnderlyingType(destType);

                        // The constant value supplied should match either the enum itself or its underlying type
                        if (type != destType && type != underlyingType)
                            throw new ArgumentException(Environment.GetResourceString("Argument_ConstantDoesntMatch"));
                    }

                    type = underlyingType;
                }
                else
                {
                    // Note that it is non CLS compliant if destType != type. But RefEmit never guarantees CLS-Compliance.
                    if (!destType.IsAssignableFrom(type))
                        throw new ArgumentException(Environment.GetResourceString("Argument_ConstantDoesntMatch"));
                }
                        
                CorElementType corType = RuntimeTypeHandle.GetCorElementType((RuntimeType)type);

                switch (corType)
                {
                    case CorElementType.I1:
                    case CorElementType.U1:
                    case CorElementType.Boolean:
                    case CorElementType.I2:
                    case CorElementType.U2:
                    case CorElementType.Char:
                    case CorElementType.I4:
                    case CorElementType.U4:
                    case CorElementType.R4:
                    case CorElementType.I8:
                    case CorElementType.U8:
                    case CorElementType.R8:
                        fixed (byte* pData = &JitHelpers.GetPinningHelper(value).m_data)
                            SetConstantValue(module.GetNativeHandle(), tk, (int)corType, pData);
                        break;

                    default:
                        if (type == typeof(String))
                        {
                            fixed (char* pString = (string)value)
                                SetConstantValue(module.GetNativeHandle(), tk, (int)CorElementType.String, pString);
                        }
                        else if (type == typeof(DateTime))
                        {
                            //date is a I8 representation
                            long ticks = ((DateTime)value).Ticks;
                            SetConstantValue(module.GetNativeHandle(), tk, (int)CorElementType.I8, &ticks);
                        }
                        else
                        {
                            throw new ArgumentException(Environment.GetResourceString("Argument_ConstantNotSupported", type.ToString()));
                        }
                        break;
                }
            }
            else
            {
                if (destType.IsValueType)
                {
                    // nullable types can hold null value.
                    if (!(destType.IsGenericType && destType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                        throw new ArgumentException(Environment.GetResourceString("Argument_ConstantNull"));
                }

                SetConstantValue(module.GetNativeHandle(), tk, (int)CorElementType.Class, null);
            }
        }

        #endregion

        #region Private Data Members
        private List<CustAttr> m_ca;
        private TypeToken m_tdType; 
        private ModuleBuilder m_module;
        private String m_strName;
        private String m_strNameSpace;
        private String m_strFullQualName;
        private Type m_typeParent;
        private List<Type> m_typeInterfaces;
        private TypeAttributes m_iAttr;
        private GenericParameterAttributes m_genParamAttributes;
        internal List<MethodBuilder> m_listMethods;
        internal int m_lastTokenizedMethod;
        private int m_constructorCount;
        private int m_iTypeSize;
        private PackingSize m_iPackingSize;
        private TypeBuilder m_DeclaringType;

        // We cannot store this on EnumBuilder because users can define enum types manually using TypeBuilder.
        private Type m_enumUnderlyingType;
        internal bool m_isHiddenGlobalType;
        private bool m_hasBeenCreated;
        private RuntimeType m_bakedRuntimeType;

        private int m_genParamPos;
        private GenericTypeParameterBuilder[] m_inst;
        private bool m_bIsGenParam;
        private MethodBuilder m_declMeth;
        private TypeBuilder m_genTypeDef;
        #endregion

        #region Constructor
        // ctor for the global (module) type
        internal TypeBuilder(ModuleBuilder module)
        {
            m_tdType = new TypeToken((int)MetadataTokenType.TypeDef);
            m_isHiddenGlobalType = true;
            m_module = (ModuleBuilder)module;
            m_listMethods = new List<MethodBuilder>();
            // No token has been created so let's initialize it to -1
            // The first time we call MethodBuilder.GetToken this will incremented.
            m_lastTokenizedMethod = -1;
        }

        // ctor for generic method parameter
        internal TypeBuilder(string szName, int genParamPos, MethodBuilder declMeth)
        {
            Contract.Requires(declMeth != null);
            m_declMeth = declMeth;
            m_DeclaringType =m_declMeth.GetTypeBuilder();
            m_module =declMeth.GetModuleBuilder();
            InitAsGenericParam(szName, genParamPos);
        }

        // ctor for generic type parameter
        private TypeBuilder(string szName, int genParamPos, TypeBuilder declType)
        {
            Contract.Requires(declType != null);
            m_DeclaringType = declType;
            m_module =declType.GetModuleBuilder();
            InitAsGenericParam(szName, genParamPos);
        }

        private void InitAsGenericParam(string szName, int genParamPos)
        {
            m_strName = szName;
            m_genParamPos = genParamPos;
            m_bIsGenParam = true;
            m_typeInterfaces = new List<Type>();
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal TypeBuilder(
            String name,
            TypeAttributes attr,
            Type parent,
            Type[] interfaces,
            ModuleBuilder module,
            PackingSize iPackingSize,
            int iTypeSize, 
            TypeBuilder enclosingType)
        {
            Init(name, attr, parent, interfaces, module, iPackingSize, iTypeSize, enclosingType);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void Init(String fullname, TypeAttributes attr, Type parent, Type[] interfaces, ModuleBuilder module,
            PackingSize iPackingSize, int iTypeSize, TypeBuilder enclosingType)
        {
            if (fullname == null)
                throw new ArgumentNullException("fullname");

            if (fullname.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "fullname");

            if (fullname[0] == '\0')
                throw new ArgumentException(Environment.GetResourceString("Argument_IllegalName"), "fullname");


            if (fullname.Length > 1023)
                throw new ArgumentException(Environment.GetResourceString("Argument_TypeNameTooLong"), "fullname");
            Contract.EndContractBlock();

            int i;
            m_module = module;
            m_DeclaringType = enclosingType;
            AssemblyBuilder containingAssem = m_module.ContainingAssemblyBuilder;

            // cannot have two types within the same assembly of the same name
            containingAssem.m_assemblyData.CheckTypeNameConflict(fullname, enclosingType);

            if (enclosingType != null)
            {
                // Nested Type should have nested attribute set.
                // If we are renumbering TypeAttributes' bit, we need to change the logic here.
                if (((attr & TypeAttributes.VisibilityMask) == TypeAttributes.Public) ||((attr & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic))
                    throw new ArgumentException(Environment.GetResourceString("Argument_BadNestedTypeFlags"), "attr");
            }

            int[] interfaceTokens = null;
            if (interfaces != null)
            {
                for(i = 0; i < interfaces.Length; i++)
                {
                    if (interfaces[i] == null)
                    {
                        // cannot contain null in the interface list
                        throw new ArgumentNullException("interfaces");
                    }
                }
                interfaceTokens = new int[interfaces.Length + 1];
                for(i = 0; i < interfaces.Length; i++)
                {
                    interfaceTokens[i] = m_module.GetTypeTokenInternal(interfaces[i]).Token;
                }
            }

            int iLast = fullname.LastIndexOf('.');
            if (iLast == -1 || iLast == 0)
            {
                // no name space
                m_strNameSpace = String.Empty;
                m_strName = fullname;
            }
            else
            {
                // split the name space
                m_strNameSpace = fullname.Substring(0, iLast);
                m_strName = fullname.Substring(iLast + 1);
            }

            VerifyTypeAttributes(attr);

            m_iAttr = attr;

            SetParent(parent);

            m_listMethods = new List<MethodBuilder>();
            m_lastTokenizedMethod = -1;

            SetInterfaces(interfaces);

            int tkParent = 0;
            if (m_typeParent != null)
                tkParent = m_module.GetTypeTokenInternal(m_typeParent).Token;

            int tkEnclosingType = 0;
            if (enclosingType != null)
            {
                tkEnclosingType = enclosingType.m_tdType.Token;
            }

            m_tdType = new TypeToken(DefineType(m_module.GetNativeHandle(),
                fullname, tkParent, m_iAttr, tkEnclosingType, interfaceTokens));

            m_iPackingSize = iPackingSize;
            m_iTypeSize = iTypeSize;
            if ((m_iPackingSize != 0) ||(m_iTypeSize != 0))
                SetClassLayout(GetModuleBuilder().GetNativeHandle(), m_tdType.Token, m_iPackingSize, m_iTypeSize);

#if !FEATURE_CORECLR
            // If the type is public and it is contained in a assemblyBuilder,
            // update the public COMType list.
            if (IsPublicComType(this))
            {
                if (containingAssem.IsPersistable() && m_module.IsTransient() == false)
                {
                    // This will throw InvalidOperationException if the assembly has been saved
                    // Ideally we should reject all emit operations if the assembly has been saved,
                    // but that would be a breaking change for some. Currently you cannot define 
                    // modules and public types, but you can still define private types and global methods.
                    containingAssem.m_assemblyData.AddPublicComType(this);
                }

                // Now add the type to the ExportedType table
                if (!m_module.Equals(containingAssem.ManifestModule))
                    containingAssem.DefineExportedTypeInMemory(this, m_module.m_moduleData.FileToken, m_tdType.Token);
            }
#endif
            m_module.AddType(FullName, this);
        }

        #endregion

        #region Private Members
        [System.Security.SecurityCritical]  // auto-generated
        private MethodBuilder DefinePInvokeMethodHelper(
            String name, String dllName, String importName, MethodAttributes attributes, CallingConventions callingConvention, 
            Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers,
            Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers,
            CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            CheckContext(returnType);
            CheckContext(returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes);
            CheckContext(parameterTypeRequiredCustomModifiers);
            CheckContext(parameterTypeOptionalCustomModifiers);

            AppDomain.CheckDefinePInvokeSupported();

            lock (SyncRoot)
            {
                return DefinePInvokeMethodHelperNoLock(name, dllName, importName, attributes, callingConvention, 
                                                       returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers,
                                                       parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers,
                                                       nativeCallConv, nativeCharSet);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private MethodBuilder DefinePInvokeMethodHelperNoLock(
            String name, String dllName, String importName, MethodAttributes attributes, CallingConventions callingConvention, 
            Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers,
            Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers,
            CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");

            if (dllName == null)
                throw new ArgumentNullException("dllName");

            if (dllName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "dllName");

            if (importName == null)
                throw new ArgumentNullException("importName");

            if (importName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "importName");

            if ((attributes & MethodAttributes.Abstract) != 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_BadPInvokeMethod"));
            Contract.EndContractBlock();

            if ((m_iAttr & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface)
                throw new ArgumentException(Environment.GetResourceString("Argument_BadPInvokeOnInterface"));

            ThrowIfCreated();

            attributes = attributes | MethodAttributes.PinvokeImpl;
            MethodBuilder method = new MethodBuilder(name, attributes, callingConvention, 
                returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers,
                parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers,
                m_module, this, false);

            //The signature grabbing code has to be up here or the signature won't be finished
            //and our equals check won't work.
            int sigLength;
            byte[] sigBytes = method.GetMethodSignature().InternalGetSignature(out sigLength);

            if (m_listMethods.Contains(method))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_MethodRedefined"));
            }
            m_listMethods.Add(method);

            MethodToken token = method.GetToken();
            
            int linkFlags = 0;
            switch(nativeCallConv)
            {
                case CallingConvention.Winapi:
                    linkFlags =(int)PInvokeMap.CallConvWinapi;
                    break;
                case CallingConvention.Cdecl:
                    linkFlags =(int)PInvokeMap.CallConvCdecl;
                    break;
                case CallingConvention.StdCall:
                    linkFlags =(int)PInvokeMap.CallConvStdcall;
                    break;
                case CallingConvention.ThisCall:
                    linkFlags =(int)PInvokeMap.CallConvThiscall;
                    break;
                case CallingConvention.FastCall:
                    linkFlags =(int)PInvokeMap.CallConvFastcall;
                    break;
            }
            switch(nativeCharSet)
            {
                case CharSet.None:
                    linkFlags |=(int)PInvokeMap.CharSetNotSpec;
                    break;
                case CharSet.Ansi:
                    linkFlags |=(int)PInvokeMap.CharSetAnsi;
                    break;
                case CharSet.Unicode:
                    linkFlags |=(int)PInvokeMap.CharSetUnicode;
                    break;
                case CharSet.Auto:
                    linkFlags |=(int)PInvokeMap.CharSetAuto;
                    break;
            }
            
            SetPInvokeData(m_module.GetNativeHandle(),
                dllName,
                importName,
                token.Token,
                linkFlags);
            method.SetToken(token);

            return method;
        }

        [System.Security.SecurityCritical]  // auto-generated
        private FieldBuilder DefineDataHelper(String name, byte[] data, int size, FieldAttributes attributes)
        {
            String strValueClassName;
            TypeBuilder valueClassType;
            FieldBuilder fdBuilder;
            TypeAttributes typeAttributes;

            if (name == null)
                throw new ArgumentNullException("name");

            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");

            if (size <= 0 || size >= 0x003f0000)
                throw new ArgumentException(Environment.GetResourceString("Argument_BadSizeForData"));
            Contract.EndContractBlock();

            ThrowIfCreated();

            // form the value class name
            strValueClassName = ModuleBuilderData.MULTI_BYTE_VALUE_CLASS + size.ToString();

            // Is this already defined in this module?
            Type temp = m_module.FindTypeBuilderWithName(strValueClassName, false);
            valueClassType = temp as TypeBuilder;

            if (valueClassType == null)
            {
                typeAttributes = TypeAttributes.Public | TypeAttributes.ExplicitLayout | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.AnsiClass;

                // Define the backing value class
                valueClassType = m_module.DefineType(strValueClassName, typeAttributes, typeof(System.ValueType), PackingSize.Size1, size);
                valueClassType.CreateType();
            }

            fdBuilder = DefineField(name, valueClassType,(attributes | FieldAttributes.Static));

            // now we need to set the RVA
            fdBuilder.SetData(data, size);
            return fdBuilder;
        }

        private void VerifyTypeAttributes(TypeAttributes attr)
        {
            // Verify attr consistency for Nesting or otherwise.
            if (DeclaringType == null)
            {
                // Not a nested class.
                if (((attr & TypeAttributes.VisibilityMask) != TypeAttributes.NotPublic) &&((attr & TypeAttributes.VisibilityMask) != TypeAttributes.Public))
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_BadTypeAttrNestedVisibilityOnNonNestedType"));
                }
            }
            else
            {
                // Nested class.
                if (((attr & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic) ||((attr & TypeAttributes.VisibilityMask) == TypeAttributes.Public))
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_BadTypeAttrNonNestedVisibilityNestedType"));
                }
            }

            // Verify that the layout mask is valid.
            if (((attr & TypeAttributes.LayoutMask) != TypeAttributes.AutoLayout) &&((attr & TypeAttributes.LayoutMask) != TypeAttributes.SequentialLayout) &&((attr & TypeAttributes.LayoutMask) != TypeAttributes.ExplicitLayout))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_BadTypeAttrInvalidLayout"));
            }

            // Check if the user attempted to set any reserved bits.
            if ((attr & TypeAttributes.ReservedMask) != 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_BadTypeAttrReservedBitsSet"));
            }
        }

        [Pure]
        public bool IsCreated()
        { 
            return m_hasBeenCreated;
        }
        
        #endregion

        #region FCalls
        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
        private extern static int DefineType(RuntimeModule module,
            String fullname, int tkParent, TypeAttributes attributes, int tkEnclosingType, int[] interfaceTokens);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
        private extern static int DefineGenericParam(RuntimeModule module,
            String name, int tkParent, GenericParameterAttributes attributes, int position, int[] constraints);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
		[SuppressUnmanagedCodeSecurity]
        private static extern void TermCreateClass(RuntimeModule module, int tk, ObjectHandleOnStack type);
        #endregion

        #region Internal Methods
        internal void ThrowIfCreated()
        {
            if (IsCreated())
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_TypeHasBeenCreated"));
        }

        internal object SyncRoot
        {
            get
            {
                return m_module.SyncRoot;
            }
        }

        internal ModuleBuilder GetModuleBuilder()
        {
            return m_module;
        }

        internal RuntimeType BakedRuntimeType
        {
            get
            {
                return m_bakedRuntimeType;
            }
        }

        internal void SetGenParamAttributes(GenericParameterAttributes genericParameterAttributes)
        {
            m_genParamAttributes = genericParameterAttributes;
        }
        
        internal void SetGenParamCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            CustAttr ca = new CustAttr(con, binaryAttribute);

            lock(SyncRoot)
            {
                SetGenParamCustomAttributeNoLock(ca);
            }
        }

        internal void SetGenParamCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            CustAttr ca = new CustAttr(customBuilder);

            lock(SyncRoot)
            {
                SetGenParamCustomAttributeNoLock(ca);
            }
        }

        private void SetGenParamCustomAttributeNoLock(CustAttr ca)
        {
            if (m_ca == null)
                m_ca = new List<TypeBuilder.CustAttr>();
        
            m_ca.Add(ca);
        }
        #endregion

        #region Object Overrides
        public override String ToString()
        {
                return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.ToString);
        }

        #endregion

        #region MemberInfo Overrides
        public override Type DeclaringType 
        {
            get { return m_DeclaringType; }
        }

        public override Type ReflectedType 
        {
            // Return the class that was used to obtain this field.
            
            get { return m_DeclaringType; }
        }

        public override String Name 
        {
            get { return m_strName; }
        }

        public override Module Module 
        {
            get { return GetModuleBuilder(); }
        }

        internal int MetadataTokenInternal
        {
            get { return m_tdType.Token; }
        }

        #endregion

        #region Type Overrides
        public override Guid GUID 
        {
            get 
            {
                if (!IsCreated())
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
                Contract.EndContractBlock();

                return m_bakedRuntimeType.GUID;
            }
        }

        public override Object InvokeMember(String name, BindingFlags invokeAttr, Binder binder, Object target,
            Object[] args, ParameterModifier[] modifiers, CultureInfo culture, String[] namedParameters)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();

            return m_bakedRuntimeType.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
        }

        public override Assembly Assembly 
        {
            get { return m_module.Assembly; }
        }

        public override RuntimeTypeHandle TypeHandle 
        {
             
            get { throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule")); }
        }

        public override String FullName 
        {
            get 
            { 
                if (m_strFullQualName == null)
                    m_strFullQualName = TypeNameBuilder.ToString(this, TypeNameBuilder.Format.FullName);

                return m_strFullQualName;
            }
        }

        public override String Namespace 
        {
            get { return m_strNameSpace; }
        }

        public override String AssemblyQualifiedName 
        {
            get 
            {                
                return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.AssemblyQualifiedName);
            }
        }

        public override Type BaseType 
        {
            get{ return m_typeParent; }
        }

        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr,Binder binder,
                CallingConventions callConvention, Type[] types,ParameterModifier[] modifiers)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();

            return m_bakedRuntimeType.GetConstructor(bindingAttr, binder, callConvention, types, modifiers);
        }

        [System.Runtime.InteropServices.ComVisible(true)]
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();

            return m_bakedRuntimeType.GetConstructors(bindingAttr);
        }

        protected override MethodInfo GetMethodImpl(String name,BindingFlags bindingAttr,Binder binder,
                CallingConventions callConvention, Type[] types,ParameterModifier[] modifiers)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();

            if (types == null)
            {
                return m_bakedRuntimeType.GetMethod(name, bindingAttr);
            }
            else
            {
                return m_bakedRuntimeType.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
            }
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();

            return m_bakedRuntimeType.GetMethods(bindingAttr);
        }

        public override FieldInfo GetField(String name, BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();

            return m_bakedRuntimeType.GetField(name, bindingAttr);
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();

            return m_bakedRuntimeType.GetFields(bindingAttr);
        }

        public override Type GetInterface(String name,bool ignoreCase)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();
            
            return m_bakedRuntimeType.GetInterface(name, ignoreCase);
        }

        public override Type[] GetInterfaces()
        {
            if (m_bakedRuntimeType != null)
            {
                return m_bakedRuntimeType.GetInterfaces();
            }

            if (m_typeInterfaces == null)
            {
                return EmptyArray<Type>.Value;
            }

            return m_typeInterfaces.ToArray();
        }

        public override EventInfo GetEvent(String name,BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();

            return m_bakedRuntimeType.GetEvent(name, bindingAttr);
        }

        public override EventInfo[] GetEvents()
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();

            return m_bakedRuntimeType.GetEvents();
        }

        protected override PropertyInfo GetPropertyImpl(String name, BindingFlags bindingAttr, Binder binder,
                Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();

            return m_bakedRuntimeType.GetProperties(bindingAttr);
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();

            return m_bakedRuntimeType.GetNestedTypes(bindingAttr);
        }

        public override Type GetNestedType(String name, BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();

            return m_bakedRuntimeType.GetNestedType(name,bindingAttr);
        }

        public override MemberInfo[] GetMember(String name, MemberTypes type, BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();

            return m_bakedRuntimeType.GetMember(name, type, bindingAttr);
        }

        [System.Runtime.InteropServices.ComVisible(true)]
        public override InterfaceMapping GetInterfaceMap(Type interfaceType)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();

            return m_bakedRuntimeType.GetInterfaceMap(interfaceType);
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();

            return m_bakedRuntimeType.GetEvents(bindingAttr);
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();

            return m_bakedRuntimeType.GetMembers(bindingAttr);
        }
        
        public override bool IsAssignableFrom(Type c)
        {
            if (TypeBuilder.IsTypeEqual(c, this))
                return true;
        
            Type fromRuntimeType = null;
            TypeBuilder fromTypeBuilder = c as TypeBuilder;
            
            if (fromTypeBuilder != null)
                fromRuntimeType = fromTypeBuilder.m_bakedRuntimeType;
            else
                fromRuntimeType = c;
                
            if (fromRuntimeType != null && fromRuntimeType is RuntimeType)
            {
                // fromType is baked. So if this type is not baked, it cannot be assignable to!
                if (m_bakedRuntimeType == null)
                    return false;
                    
                // since toType is also baked, delegate to the base
                return m_bakedRuntimeType.IsAssignableFrom(fromRuntimeType);
            }
            
            // So if c is not a runtimeType nor TypeBuilder. We don't know how to deal with it. 
            // return false then.
            if (fromTypeBuilder == null)
                return false;
                                 
            // If fromTypeBuilder is a subclass of this class, then c can be cast to this type.
            if (fromTypeBuilder.IsSubclassOf(this))
                return true;
                
            if (this.IsInterface == false)
                return false;
                                                                                  
            // now is This type a base type on one of the interface impl?
            Type[] interfaces = fromTypeBuilder.GetInterfaces();
            for(int i = 0; i < interfaces.Length; i++)
            {
                // unfortunately, IsSubclassOf does not cover the case when they are the same type.
                if (TypeBuilder.IsTypeEqual(interfaces[i], this))
                    return true;
            
                if (interfaces[i].IsSubclassOf(this))
                    return true;
            }
            return false;                                                                               
        }        

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            return m_iAttr;
        }

        protected override bool IsArrayImpl()
        {
            return false;
        }
        protected override bool IsByRefImpl()
        {
            return false;
        }
        protected override bool IsPointerImpl()
        {
            return false;
        }
        protected override bool IsPrimitiveImpl()
        {
            return false;
        }

        protected override bool IsCOMObjectImpl()
        {
            return((GetAttributeFlagsImpl() & TypeAttributes.Import) != 0) ? true : false;
        }

        public override Type GetElementType()
        {
            
            // You will never have to deal with a TypeBuilder if you are just referring to arrays.
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        protected override bool HasElementTypeImpl()
        {
            return false;
        }

        public override bool IsSecurityCritical
        {
            get
            {
                if (!IsCreated())
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
                Contract.EndContractBlock();

                return m_bakedRuntimeType.IsSecurityCritical;
            }
        }

        public override bool IsSecuritySafeCritical
        {
            get
            {
                if (!IsCreated())
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
                Contract.EndContractBlock();

                return m_bakedRuntimeType.IsSecuritySafeCritical;
            }
        }

        public override bool IsSecurityTransparent
        {
            get
            {
                if (!IsCreated())
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
                Contract.EndContractBlock();

                return m_bakedRuntimeType.IsSecurityTransparent;
            }
        }

        [System.Runtime.InteropServices.ComVisible(true)]
        [Pure]
        public override bool IsSubclassOf(Type c)
        {
            Type p = this;

            if (TypeBuilder.IsTypeEqual(p, c))
                return false;

            p = p.BaseType; 
               
            while(p != null) 
            {
                if (TypeBuilder.IsTypeEqual(p, c))
                    return true;

                p = p.BaseType;
            }

            return false;
        }
        
        public override Type UnderlyingSystemType 
        {
            get 
            {
                if (m_bakedRuntimeType != null)
                    return m_bakedRuntimeType;

                if (IsEnum)
                {
                    if (m_enumUnderlyingType == null)
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NoUnderlyingTypeOnEnum"));
                    
                    return m_enumUnderlyingType;                       
                }
                else
                {
                    return this;
                }
            }
        }

        public override Type MakePointerType() 
        { 
            return SymbolType.FormCompoundType("*", this, 0); 
        }

        public override Type MakeByRefType() 
        {
            return SymbolType.FormCompoundType("&", this, 0);
        }

        public override Type MakeArrayType() 
        {
            return SymbolType.FormCompoundType("[]", this, 0);
        }

        public override Type MakeArrayType(int rank) 
        {
            if (rank <= 0)
                throw new IndexOutOfRangeException();
            Contract.EndContractBlock();

            string szrank = "";
            if (rank == 1)
            {
                szrank = "*";
            }
            else 
            {
                for(int i = 1; i < rank; i++)
                    szrank += ",";
            }

            string s = String.Format(CultureInfo.InvariantCulture, "[{0}]", szrank); // [,,]
            return SymbolType.FormCompoundType(s, this, 0);
        }

        #endregion

        #region ICustomAttributeProvider Implementation
        [System.Security.SecuritySafeCritical]  // auto-generated
        public override Object[] GetCustomAttributes(bool inherit)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));
            Contract.EndContractBlock();

            return CustomAttribute.GetCustomAttributes(m_bakedRuntimeType, typeof(object) as RuntimeType, inherit);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));

            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();

            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"),"attributeType");

            return CustomAttribute.GetCustomAttributes(m_bakedRuntimeType, attributeRuntimeType, inherit);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (!IsCreated())
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_TypeNotYetCreated"));

            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();

            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"),"caType");

            return CustomAttribute.IsDefined(m_bakedRuntimeType, attributeRuntimeType, inherit);
        }

        #endregion

        #region Public Member
        
        #region DefineType
        public override GenericParameterAttributes GenericParameterAttributes { get { return m_genParamAttributes; } }

        internal void SetInterfaces(params Type[] interfaces) 
        { 
            ThrowIfCreated();

            m_typeInterfaces = new List<Type>();
            if (interfaces != null)
            {
                m_typeInterfaces.AddRange(interfaces);
            }
        }


        public GenericTypeParameterBuilder[] DefineGenericParameters(params string[] names)
        {
            if (names == null)
                throw new ArgumentNullException("names");

            if (names.Length == 0)
                throw new ArgumentException();
            Contract.EndContractBlock();
           
            for (int i = 0; i < names.Length; i ++)
                if (names[i] == null)
                    throw new ArgumentNullException("names");

            if (m_inst != null)
                throw new InvalidOperationException();

            m_inst = new GenericTypeParameterBuilder[names.Length];
            for(int i = 0; i < names.Length; i ++)
                m_inst[i] = new GenericTypeParameterBuilder(new TypeBuilder(names[i], i, this));

            return m_inst;
        }

		
		public override Type MakeGenericType(params Type[] typeArguments)
		{
            CheckContext(typeArguments);
        
            return TypeBuilderInstantiation.MakeGenericType(this, typeArguments); 
        }
		
        public override Type[] GetGenericArguments() { return m_inst; }
        // If a TypeBuilder is generic, it must be a generic type definition
        // All instantiated generic types are TypeBuilderInstantiation.
        public override bool IsGenericTypeDefinition { get { return IsGenericType; } }
       	public override bool IsGenericType { get { return m_inst != null; } }
        public override bool IsGenericParameter { get { return m_bIsGenParam; } }
        public override bool IsConstructedGenericType { get { return false; } }

        public override int GenericParameterPosition { get { return m_genParamPos; } }
        public override MethodBase DeclaringMethod { get { return m_declMeth; } }
        public override Type GetGenericTypeDefinition() { if (IsGenericTypeDefinition) return this; if (m_genTypeDef == null) throw new InvalidOperationException(); return m_genTypeDef; }
        #endregion

        #region Define Method
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void DefineMethodOverride(MethodInfo methodInfoBody, MethodInfo methodInfoDeclaration)
        {
            lock(SyncRoot)
            {
                DefineMethodOverrideNoLock(methodInfoBody, methodInfoDeclaration);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void DefineMethodOverrideNoLock(MethodInfo methodInfoBody, MethodInfo methodInfoDeclaration)
        {
            if (methodInfoBody == null)
                throw new ArgumentNullException("methodInfoBody");

            if (methodInfoDeclaration == null)
                throw new ArgumentNullException("methodInfoDeclaration");
            Contract.EndContractBlock();

            ThrowIfCreated();
                                                                
            if (!object.ReferenceEquals(methodInfoBody.DeclaringType, this))
                // Loader restriction: body method has to be from this class
                throw new ArgumentException(Environment.GetResourceString("ArgumentException_BadMethodImplBody"));
            
            MethodToken     tkBody;
            MethodToken     tkDecl;

            tkBody = m_module.GetMethodTokenInternal(methodInfoBody);
            tkDecl = m_module.GetMethodTokenInternal(methodInfoDeclaration);

            DefineMethodImpl(m_module.GetNativeHandle(), m_tdType.Token, tkBody.Token, tkDecl.Token);
        }

        public MethodBuilder DefineMethod(String name, MethodAttributes attributes, Type returnType, Type[] parameterTypes)
        {
            Contract.Ensures(Contract.Result<MethodBuilder>() != null);

            return DefineMethod(name, attributes, CallingConventions.Standard, returnType, parameterTypes);
        }

        public MethodBuilder DefineMethod(String name, MethodAttributes attributes)
        {
            Contract.Ensures(Contract.Result<MethodBuilder>() != null);

            return DefineMethod(name, attributes, CallingConventions.Standard, null, null);
        }

        public MethodBuilder DefineMethod(String name, MethodAttributes attributes, CallingConventions callingConvention)
        {
            Contract.Ensures(Contract.Result<MethodBuilder>() != null);

            return DefineMethod(name, attributes, callingConvention, null, null);
        }

        public MethodBuilder DefineMethod(String name, MethodAttributes attributes, CallingConventions callingConvention,
            Type returnType, Type[] parameterTypes)
        {
            Contract.Ensures(Contract.Result<MethodBuilder>() != null);

            return DefineMethod(name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null);
        }
        
        public MethodBuilder DefineMethod(String name, MethodAttributes attributes, CallingConventions callingConvention,
            Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers,
            Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers)
        {
            Contract.Ensures(Contract.Result<MethodBuilder>() != null);

            lock(SyncRoot)
            {
                return DefineMethodNoLock(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, 
                                          returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, 
                                          parameterTypeOptionalCustomModifiers);
            }
        }
            
        private MethodBuilder DefineMethodNoLock(String name, MethodAttributes attributes, CallingConventions callingConvention,
            Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers,
            Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
            Contract.Ensures(Contract.Result<MethodBuilder>() != null);
            Contract.EndContractBlock();

            CheckContext(returnType);
            CheckContext(returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes);
            CheckContext(parameterTypeRequiredCustomModifiers);
            CheckContext(parameterTypeOptionalCustomModifiers);

            if (parameterTypes != null)
            {
                if (parameterTypeOptionalCustomModifiers != null && parameterTypeOptionalCustomModifiers.Length != parameterTypes.Length)
                    throw new ArgumentException(Environment.GetResourceString("Argument_MismatchedArrays", "parameterTypeOptionalCustomModifiers", "parameterTypes"));

                if (parameterTypeRequiredCustomModifiers != null && parameterTypeRequiredCustomModifiers.Length != parameterTypes.Length)
                    throw new ArgumentException(Environment.GetResourceString("Argument_MismatchedArrays", "parameterTypeRequiredCustomModifiers", "parameterTypes"));
            }

            ThrowIfCreated();

            if (!m_isHiddenGlobalType)
            {
                if (((m_iAttr & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface) &&
                   (attributes & MethodAttributes.Abstract) == 0 &&(attributes & MethodAttributes.Static) == 0)
                    throw new ArgumentException(Environment.GetResourceString("Argument_BadAttributeOnInterfaceMethod"));               
            }

            // pass in Method attributes
            MethodBuilder method = new MethodBuilder(
                name, attributes, callingConvention, 
                returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers,
                parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers, 
                m_module, this, false);

            if (!m_isHiddenGlobalType)
            {
                //If this method is declared to be a constructor, increment our constructor count.
                if ((method.Attributes & MethodAttributes.SpecialName) != 0 && method.Name.Equals(ConstructorInfo.ConstructorName)) 
                {
                    m_constructorCount++;
                }
            }

            m_listMethods.Add(method);

            return method;
        }

        #endregion

        #region Define Constructor
        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.InteropServices.ComVisible(true)]
        public ConstructorBuilder DefineTypeInitializer()
        {
            lock(SyncRoot)
            {
                return DefineTypeInitializerNoLock();
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private ConstructorBuilder DefineTypeInitializerNoLock()
        {
            ThrowIfCreated();

            // change the attributes and the class constructor's name
            MethodAttributes attr = MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName;

            ConstructorBuilder constBuilder = new ConstructorBuilder(
                ConstructorInfo.TypeConstructorName, attr, CallingConventions.Standard, null, m_module, this);

            return constBuilder;
        }

        [System.Runtime.InteropServices.ComVisible(true)]
        public ConstructorBuilder DefineDefaultConstructor(MethodAttributes attributes)
        {
            if ((m_iAttr & TypeAttributes.Interface) == TypeAttributes.Interface)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ConstructorNotAllowedOnInterface"));
            }

            lock(SyncRoot)
            {
                return DefineDefaultConstructorNoLock(attributes);
            }
        }

        private ConstructorBuilder DefineDefaultConstructorNoLock(MethodAttributes attributes)
        {
            ConstructorBuilder constBuilder;

            // get the parent class's default constructor
            // We really don't want(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic) here.  We really want
            // constructors visible from the subclass, but that is not currently
            // available in BindingFlags.  This more open binding is open to
            // runtime binding failures(like if we resolve to a private
            // constructor).
            ConstructorInfo con = null;

            if (m_typeParent is TypeBuilderInstantiation)
            {
                Type genericTypeDefinition = m_typeParent.GetGenericTypeDefinition();

                if (genericTypeDefinition is TypeBuilder)
                    genericTypeDefinition = ((TypeBuilder)genericTypeDefinition).m_bakedRuntimeType;

                if (genericTypeDefinition == null)
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));

                Type inst = genericTypeDefinition.MakeGenericType(m_typeParent.GetGenericArguments());

                if (inst is TypeBuilderInstantiation)
                    con = TypeBuilder.GetConstructor(inst, genericTypeDefinition.GetConstructor(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null));
                else                
                    con = inst.GetConstructor(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            }

            if (con == null)
            {
                con = m_typeParent.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            }

            if (con == null)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_NoParentDefaultConstructor"));

            // Define the constructor Builder
            constBuilder = DefineConstructor(attributes, CallingConventions.Standard, null);
            m_constructorCount++;

            // generate the code to call the parent's default constructor
            ILGenerator il = constBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call,con);
            il.Emit(OpCodes.Ret);

            constBuilder.m_isDefaultConstructor = true;
            return constBuilder;
        }

        [System.Runtime.InteropServices.ComVisible(true)]
        public ConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions callingConvention, Type[] parameterTypes)
        {
            return DefineConstructor(attributes, callingConvention, parameterTypes, null, null);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.InteropServices.ComVisible(true)]
        public ConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions callingConvention, 
            Type[] parameterTypes, Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers)
        {
            if ((m_iAttr & TypeAttributes.Interface) == TypeAttributes.Interface && (attributes & MethodAttributes.Static) != MethodAttributes.Static)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ConstructorNotAllowedOnInterface"));
            }

            lock(SyncRoot)
            {
                return DefineConstructorNoLock(attributes, callingConvention, parameterTypes, requiredCustomModifiers, optionalCustomModifiers);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private ConstructorBuilder DefineConstructorNoLock(MethodAttributes attributes, CallingConventions callingConvention, 
            Type[] parameterTypes, Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers)
        {
            CheckContext(parameterTypes);
            CheckContext(requiredCustomModifiers);
            CheckContext(optionalCustomModifiers);

            ThrowIfCreated();

            String name;

            if ((attributes & MethodAttributes.Static) == 0)
            {
                name = ConstructorInfo.ConstructorName;
            }
            else
            {
                name = ConstructorInfo.TypeConstructorName;
            }

            attributes = attributes | MethodAttributes.SpecialName;

            ConstructorBuilder constBuilder = 
                new ConstructorBuilder(name, attributes, callingConvention, 
                    parameterTypes, requiredCustomModifiers, optionalCustomModifiers, m_module, this);

            m_constructorCount++;

            return constBuilder;
        }

        #endregion

        #region Define PInvoke
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#else
        [System.Security.SecuritySafeCritical]
#endif
        public MethodBuilder DefinePInvokeMethod(String name, String dllName, MethodAttributes attributes,
            CallingConventions callingConvention, Type returnType, Type[] parameterTypes,
            CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            MethodBuilder method = DefinePInvokeMethodHelper(
                name, dllName, name, attributes, callingConvention, returnType, null, null, 
                parameterTypes, null, null, nativeCallConv, nativeCharSet);
            return method;
        }

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#else
        [System.Security.SecuritySafeCritical]
#endif
        public MethodBuilder DefinePInvokeMethod(String name, String dllName, String entryName, MethodAttributes attributes, 
            CallingConventions callingConvention, Type returnType, Type[] parameterTypes, 
            CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            MethodBuilder method = DefinePInvokeMethodHelper(
                name, dllName, entryName, attributes, callingConvention, returnType, null, null, 
                parameterTypes, null, null, nativeCallConv, nativeCharSet);
            return method;
        }

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#else
        [System.Security.SecuritySafeCritical]
#endif
        public MethodBuilder DefinePInvokeMethod(String name, String dllName, String entryName, MethodAttributes attributes,
            CallingConventions callingConvention, 
            Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers,
            Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers,
            CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            MethodBuilder method = DefinePInvokeMethodHelper(
            name, dllName, entryName, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, 
            parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers, nativeCallConv, nativeCharSet);
            return method;
        }

        #endregion

        #region Define Nested Type
        [System.Security.SecuritySafeCritical]  // auto-generated
        public TypeBuilder DefineNestedType(String name)
        {
            lock(SyncRoot)
            {
                return DefineNestedTypeNoLock(name, TypeAttributes.NestedPrivate, null, null, PackingSize.Unspecified, UnspecifiedTypeSize);
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.InteropServices.ComVisible(true)]
        public TypeBuilder DefineNestedType(String name, TypeAttributes attr, Type parent, Type[] interfaces)
        {
            lock(SyncRoot)
            {
                // Why do we only call CheckContext here? Why don't we call it in the other overloads?
                CheckContext(parent);
                CheckContext(interfaces);

                return DefineNestedTypeNoLock(name, attr, parent, interfaces, PackingSize.Unspecified, UnspecifiedTypeSize);
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public TypeBuilder DefineNestedType(String name, TypeAttributes attr, Type parent)
        {
            lock(SyncRoot)
            {
                return DefineNestedTypeNoLock(name, attr, parent, null, PackingSize.Unspecified, UnspecifiedTypeSize);
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public TypeBuilder DefineNestedType(String name, TypeAttributes attr)
        {
            lock(SyncRoot)
            {
                return DefineNestedTypeNoLock(name, attr, null, null, PackingSize.Unspecified, UnspecifiedTypeSize);
            }
        }

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#else
        [System.Security.SecuritySafeCritical]
#endif
        public TypeBuilder DefineNestedType(String name, TypeAttributes attr, Type parent, int typeSize)
        {
            lock(SyncRoot)
            {
                return DefineNestedTypeNoLock(name, attr, parent, null, PackingSize.Unspecified, typeSize);
            }
        }

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#else
        [System.Security.SecuritySafeCritical]
#endif
        public TypeBuilder DefineNestedType(String name, TypeAttributes attr, Type parent, PackingSize packSize)
        {
            lock(SyncRoot)
            {
                return DefineNestedTypeNoLock(name, attr, parent, null, packSize, UnspecifiedTypeSize);
            }
        }

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#else
        [System.Security.SecuritySafeCritical]
#endif
        public TypeBuilder DefineNestedType(String name, TypeAttributes attr, Type parent, PackingSize packSize, int typeSize)
        {
            lock (SyncRoot)
            {
                return DefineNestedTypeNoLock(name, attr, parent, null, packSize, typeSize);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private TypeBuilder DefineNestedTypeNoLock(String name, TypeAttributes attr, Type parent, Type[] interfaces, PackingSize packSize, int typeSize)
        {
            return new TypeBuilder(name, attr, parent, interfaces, m_module, packSize, typeSize, this);
        }

        #endregion

        #region Define Field
        public FieldBuilder DefineField(String fieldName, Type type, FieldAttributes attributes) 
        {
            return DefineField(fieldName, type, null, null, attributes);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public FieldBuilder DefineField(String fieldName, Type type, Type[] requiredCustomModifiers, 
            Type[] optionalCustomModifiers, FieldAttributes attributes) 
        {
            lock(SyncRoot)
            {
                return DefineFieldNoLock(fieldName, type, requiredCustomModifiers, optionalCustomModifiers, attributes);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private FieldBuilder DefineFieldNoLock(String fieldName, Type type, Type[] requiredCustomModifiers, 
            Type[] optionalCustomModifiers, FieldAttributes attributes) 
        {
            ThrowIfCreated();
            CheckContext(type);
            CheckContext(requiredCustomModifiers);

            if (m_enumUnderlyingType == null && IsEnum == true)
            {
                if ((attributes & FieldAttributes.Static) == 0)
                {
                    // remember the underlying type for enum type
                    m_enumUnderlyingType = type;
                }                   
            }

            return new FieldBuilder(this, fieldName, type, requiredCustomModifiers, optionalCustomModifiers, attributes);
        }

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#else
        [System.Security.SecuritySafeCritical]
#endif
        public FieldBuilder DefineInitializedData(String name, byte[] data, FieldAttributes attributes)
        {
            lock(SyncRoot)
            {
                return DefineInitializedDataNoLock(name, data, attributes);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private FieldBuilder DefineInitializedDataNoLock(String name, byte[] data, FieldAttributes attributes)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            Contract.EndContractBlock();

            // This method will define an initialized Data in .sdata.
            // We will create a fake TypeDef to represent the data with size. This TypeDef
            // will be the signature for the Field.

            return DefineDataHelper(name, data, data.Length, attributes);
        }

#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#else
        [System.Security.SecuritySafeCritical]
#endif
        public FieldBuilder DefineUninitializedData(String name, int size, FieldAttributes attributes)
        {
            lock(SyncRoot)
            {
                return DefineUninitializedDataNoLock(name, size, attributes);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private FieldBuilder DefineUninitializedDataNoLock(String name, int size, FieldAttributes attributes)
        {
            // This method will define an uninitialized Data in .sdata.
            // We will create a fake TypeDef to represent the data with size. This TypeDef
            // will be the signature for the Field.
            return DefineDataHelper(name, null, size, attributes);
        }

        #endregion

        #region Define Properties and Events
        public PropertyBuilder DefineProperty(String name, PropertyAttributes attributes, Type returnType, Type[] parameterTypes)
        {
            return DefineProperty(name, attributes, returnType, null, null, parameterTypes, null, null); 
        }

        public PropertyBuilder DefineProperty(String name, PropertyAttributes attributes, 
            CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
        {
            return DefineProperty(name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null); 
        }


        public PropertyBuilder DefineProperty(String name, PropertyAttributes attributes, 
            Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, 
            Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers)
        {
            return DefineProperty(name, attributes, (CallingConventions)0, returnType, 
                returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, 
                parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers); 
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public PropertyBuilder DefineProperty(String name, PropertyAttributes attributes, CallingConventions callingConvention, 
            Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, 
            Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers)
        {
            lock(SyncRoot)
            {
                return DefinePropertyNoLock(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, 
                                            parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private PropertyBuilder DefinePropertyNoLock(String name, PropertyAttributes attributes, CallingConventions callingConvention,
            Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, 
            Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
            Contract.EndContractBlock();

            CheckContext(returnType);
            CheckContext(returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes);
            CheckContext(parameterTypeRequiredCustomModifiers);
            CheckContext(parameterTypeOptionalCustomModifiers);

            SignatureHelper sigHelper;
            int         sigLength;
            byte[]      sigBytes;

            ThrowIfCreated();

            // get the signature in SignatureHelper form
            sigHelper = SignatureHelper.GetPropertySigHelper(
                m_module, callingConvention,
                returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers,
                parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);

            // get the signature in byte form
            sigBytes = sigHelper.InternalGetSignature(out sigLength);

            PropertyToken prToken = new PropertyToken(DefineProperty(
                m_module.GetNativeHandle(),
                m_tdType.Token,
                name,
                attributes,
                sigBytes,
                sigLength));

            // create the property builder now.
            return new PropertyBuilder(
                    m_module,
                    name,
                    sigHelper,
                    attributes,
                    returnType,
                    prToken,
                    this);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public EventBuilder DefineEvent(String name, EventAttributes attributes, Type eventtype)
        {
            lock(SyncRoot)
            {
                return DefineEventNoLock(name, attributes, eventtype);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private EventBuilder DefineEventNoLock(String name, EventAttributes attributes, Type eventtype)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
            if (name[0] == '\0')
                throw new ArgumentException(Environment.GetResourceString("Argument_IllegalName"), "name");
            Contract.EndContractBlock();

            int tkType;
            EventToken      evToken;
            
            CheckContext(eventtype);

            ThrowIfCreated();

            tkType = m_module.GetTypeTokenInternal( eventtype ).Token;

            // Internal helpers to define property records
            evToken = new EventToken(DefineEvent(
                m_module.GetNativeHandle(),
                m_tdType.Token,
                name,
                attributes,
                tkType));

            // create the property builder now.
            return new EventBuilder(
                    m_module,
                    name,
                    attributes,
                    //tkType,
                    this,
                    evToken);
        }

        #endregion

        #region Create Type

        [System.Security.SecuritySafeCritical]  // auto-generated
        public TypeInfo CreateTypeInfo()
        {
            lock (SyncRoot)
            {
                return CreateTypeNoLock();
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public Type CreateType()
        {
            lock (SyncRoot)
            {
                return CreateTypeNoLock();
            }
        }

        internal void CheckContext(params Type[][] typess)
        {
            m_module.CheckContext(typess);            
        }
        internal void CheckContext(params Type[] types)
        {
            m_module.CheckContext(types);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private TypeInfo CreateTypeNoLock()
        {
            if (IsCreated())
                return m_bakedRuntimeType;

            ThrowIfCreated();

            if (m_typeInterfaces == null)
                m_typeInterfaces = new List<Type>();

            int[] interfaceTokens = new int[m_typeInterfaces.Count];
            for(int i = 0; i < m_typeInterfaces.Count; i++)
            {
                interfaceTokens[i] = m_module.GetTypeTokenInternal(m_typeInterfaces[i]).Token;
            }

            int tkParent = 0;
            if (m_typeParent != null)
                tkParent = m_module.GetTypeTokenInternal(m_typeParent).Token;

            if (IsGenericParameter)
            {
                int[] constraints; // Array of token constrains terminated by null token

                if (m_typeParent != null)
                {
                    constraints = new int[m_typeInterfaces.Count + 2];
                    constraints[constraints.Length - 2] = tkParent;
                }
                else
                {
                    constraints = new int[m_typeInterfaces.Count + 1];
                }

                for (int i = 0; i < m_typeInterfaces.Count; i++)
                {
                    constraints[i] = m_module.GetTypeTokenInternal(m_typeInterfaces[i]).Token;
                }

                int declMember = m_declMeth == null ? m_DeclaringType.m_tdType.Token : m_declMeth.GetToken().Token;
                m_tdType = new TypeToken(DefineGenericParam(m_module.GetNativeHandle(),
                    m_strName, declMember, m_genParamAttributes, m_genParamPos, constraints));

                if (m_ca != null)
                {
                    foreach (CustAttr ca in m_ca)
                        ca.Bake(m_module, MetadataTokenInternal);
                }

                m_hasBeenCreated = true;

                // Baking a generic parameter does not put sufficient information into the metadata to actually be able to load it as a type,
                // the associated generic type/method needs to be baked first. So we return this rather than the baked type.
                return this;
            }
            else
            {
                // Check for global typebuilder
                if (((m_tdType.Token & 0x00FFFFFF) != 0) && ((tkParent & 0x00FFFFFF) != 0))
                    SetParentType(m_module.GetNativeHandle(), m_tdType.Token, tkParent);
            
                if (m_inst != null)
                    foreach (Type tb in m_inst)
                        if (tb is GenericTypeParameterBuilder)
                            ((GenericTypeParameterBuilder)tb).m_type.CreateType();
            }

            byte [] body;
            MethodAttributes methodAttrs;
                            
            if (!m_isHiddenGlobalType)
            {
                // create a public default constructor if this class has no constructor.
                // except if the type is Interface, ValueType, Enum, or a static class.
                if (m_constructorCount == 0 && ((m_iAttr & TypeAttributes.Interface) == 0) && !IsValueType && ((m_iAttr & (TypeAttributes.Abstract | TypeAttributes.Sealed)) != (TypeAttributes.Abstract | TypeAttributes.Sealed)))
                {
                    DefineDefaultConstructor(MethodAttributes.Public);
                }
            }

            int size = m_listMethods.Count;

            for(int i = 0; i < size; i++)
            {
                MethodBuilder meth = m_listMethods[i];


                if (meth.IsGenericMethodDefinition)
                    meth.GetToken(); // Doubles as "CreateMethod" for MethodBuilder -- analagous to CreateType()

                methodAttrs = meth.Attributes;

                // Any of these flags in the implemenation flags is set, we will not attach the IL method body
                if (((meth.GetMethodImplementationFlags() &(MethodImplAttributes.CodeTypeMask|MethodImplAttributes.PreserveSig|MethodImplAttributes.Unmanaged)) != MethodImplAttributes.IL) ||
                    ((methodAttrs & MethodAttributes.PinvokeImpl) !=(MethodAttributes) 0))
                {
                    continue;
                }

                int sigLength;
                byte[] localSig = meth.GetLocalSignature(out sigLength);
                 
                // Check that they haven't declared an abstract method on a non-abstract class
                if (((methodAttrs & MethodAttributes.Abstract) != 0) &&((m_iAttr & TypeAttributes.Abstract) == 0))
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_BadTypeAttributesNotAbstract"));
                }

                body = meth.GetBody();

                // If this is an abstract method or an interface, we don't need to set the IL.

                if ((methodAttrs & MethodAttributes.Abstract) != 0)
                {
                    // We won't check on Interface because we can have class static initializer on interface.
                    // We will just let EE or validator to catch the problem.

                    //((m_iAttr & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface))

                    if (body != null)
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_BadMethodBody"));
                }
                else if (body == null || body.Length == 0)
                {
                    // If it's not an abstract or an interface, set the IL.
                    if (meth.m_ilGenerator != null)
                    {
                        // we need to bake the method here.
                        meth.CreateMethodBodyHelper(meth.GetILGenerator());
                    }

                    body = meth.GetBody();

                    if ((body == null || body.Length == 0) && !meth.m_canBeRuntimeImpl)
                        throw new InvalidOperationException(
                            Environment.GetResourceString("InvalidOperation_BadEmptyMethodBody", meth.Name) ); 
                }

                int maxStack = meth.GetMaxStack();

                ExceptionHandler[] exceptions = meth.GetExceptionHandlers();
                int[] tokenFixups = meth.GetTokenFixups();

                SetMethodIL(m_module.GetNativeHandle(), meth.GetToken().Token, meth.InitLocals, 
                    body, (body != null) ? body.Length : 0,
                    localSig, sigLength, maxStack,
                    exceptions, (exceptions != null) ? exceptions.Length : 0,
                    tokenFixups, (tokenFixups != null) ? tokenFixups.Length : 0);

                if (m_module.ContainingAssemblyBuilder.m_assemblyData.m_access == AssemblyBuilderAccess.Run)
                {
                    // if we don't need the data structures to build the method any more
                    // throw them away.
                    meth.ReleaseBakedStructures();
                }
            }

            m_hasBeenCreated = true;

            // Terminate the process.
            RuntimeType cls = null;
            TermCreateClass(m_module.GetNativeHandle(), m_tdType.Token, JitHelpers.GetObjectHandleOnStack(ref cls));

            if (!m_isHiddenGlobalType)
            {
                m_bakedRuntimeType = cls;

                // if this type is a nested type, we need to invalidate the cached nested runtime type on the nesting type
                if (m_DeclaringType != null && m_DeclaringType.m_bakedRuntimeType != null)
                {
                   m_DeclaringType.m_bakedRuntimeType.InvalidateCachedNestedType();
                }

                return cls;
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region Misc
        public int Size
        {
            get { return m_iTypeSize; }
        }
        
        public PackingSize PackingSize 
        {
            get { return m_iPackingSize; }
        }

        public void SetParent(Type parent)
        {
            ThrowIfCreated();

            if (parent != null)
            {
                CheckContext(parent);

                if (parent.IsInterface)
                    throw new ArgumentException(Environment.GetResourceString("Argument_CannotSetParentToInterface"));

                m_typeParent = parent;
            }
            else
            {
                if ((m_iAttr & TypeAttributes.Interface) != TypeAttributes.Interface)
                {
                    m_typeParent = typeof(Object);
                }
                else
                {
                    if ((m_iAttr & TypeAttributes.Abstract) == 0)
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_BadInterfaceNotAbstract"));

                    // there is no extends for interface class
                    m_typeParent = null;
                }
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.InteropServices.ComVisible(true)]
        public void AddInterfaceImplementation(Type interfaceType)
        {
            if (interfaceType == null)
            {
                throw new ArgumentNullException("interfaceType");
            }
            Contract.EndContractBlock();

            CheckContext(interfaceType);
            
            ThrowIfCreated();

            TypeToken tkInterface = m_module.GetTypeTokenInternal(interfaceType);
            AddInterfaceImpl(m_module.GetNativeHandle(), m_tdType.Token, tkInterface.Token);

            m_typeInterfaces.Add(interfaceType);
        }

#if FEATURE_CAS_POLICY
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void AddDeclarativeSecurity(SecurityAction action, PermissionSet pset)
        {
            lock(SyncRoot)
            {
                AddDeclarativeSecurityNoLock(action, pset);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void AddDeclarativeSecurityNoLock(SecurityAction action, PermissionSet pset)
        {
            if (pset == null)
                throw new ArgumentNullException("pset");

#pragma warning disable 618
            if (!Enum.IsDefined(typeof(SecurityAction), action) ||
                action == SecurityAction.RequestMinimum ||
                action == SecurityAction.RequestOptional ||
                action == SecurityAction.RequestRefuse)
            {
                throw new ArgumentOutOfRangeException("action");
            }
#pragma warning restore 618

            Contract.EndContractBlock();

            ThrowIfCreated();

            // Translate permission set into serialized format(uses standard binary serialization format).
            byte[] blob = null;
            int length = 0;
            if (!pset.IsEmpty())
            {
                blob = pset.EncodeXml();
                length = blob.Length;
            }

            // Write the blob into the metadata.
            AddDeclarativeSecurity(m_module.GetNativeHandle(), m_tdType.Token, action, blob, length);
        }
#endif // FEATURE_CAS_POLICY

public TypeToken TypeToken 
        {
            get 
            {
                if (IsGenericParameter)
                    ThrowIfCreated();

                return m_tdType; 
            }
        }


#if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
#else
        [System.Security.SecuritySafeCritical]
#endif
        [System.Runtime.InteropServices.ComVisible(true)]
        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            if (con == null)
                throw new ArgumentNullException("con");

            if (binaryAttribute == null)
                throw new ArgumentNullException("binaryAttribute");
            Contract.EndContractBlock();

            TypeBuilder.DefineCustomAttribute(m_module, m_tdType.Token, ((ModuleBuilder)m_module).GetConstructorToken(con).Token,
                binaryAttribute, false, false);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder == null)
                throw new ArgumentNullException("customBuilder");
            Contract.EndContractBlock();

            customBuilder.CreateCustomAttribute((ModuleBuilder)m_module, m_tdType.Token);
        }

        #endregion

        #endregion

#if !FEATURE_CORECLR
        void _TypeBuilder.GetTypeInfoCount(out uint pcTInfo)
        {
            throw new NotImplementedException();
        }

        void _TypeBuilder.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
        {
            throw new NotImplementedException();
        }

        void _TypeBuilder.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
        {
            throw new NotImplementedException();
        }

        void _TypeBuilder.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
        {
            throw new NotImplementedException();
        }
#endif
    }
}
