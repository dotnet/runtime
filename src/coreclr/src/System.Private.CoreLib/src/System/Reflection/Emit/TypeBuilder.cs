// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using CultureInfo = System.Globalization.CultureInfo;

namespace System.Reflection.Emit
{
    public sealed class TypeBuilder : TypeInfo
    {
        public override bool IsAssignableFrom([NotNullWhen(true)] TypeInfo? typeInfo)
        {
            if (typeInfo == null) return false;
            return IsAssignableFrom(typeInfo.AsType());
        }

        #region Declarations
        private class CustAttr
        {
            private readonly ConstructorInfo? m_con;
            private readonly byte[]? m_binaryAttribute;
            private readonly CustomAttributeBuilder? m_customBuilder;

            public CustAttr(ConstructorInfo con, byte[] binaryAttribute)
            {
                if (con is null)
                    throw new ArgumentNullException(nameof(con));

                if (binaryAttribute is null)
                    throw new ArgumentNullException(nameof(binaryAttribute));

                m_con = con;
                m_binaryAttribute = binaryAttribute;
            }

            public CustAttr(CustomAttributeBuilder customBuilder)
            {
                if (customBuilder is null)
                    throw new ArgumentNullException(nameof(customBuilder));

                m_customBuilder = customBuilder;
            }

            public void Bake(ModuleBuilder module, int token)
            {
                if (m_customBuilder == null)
                {
                    Debug.Assert(m_con != null);
                    DefineCustomAttribute(module, token, module.GetConstructorToken(m_con).Token,
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
                throw new ArgumentException(SR.Argument_MustBeTypeBuilder);

            // The following checks establishes invariants that more simply put require type to be generic and
            // method to be a generic method definition declared on the generic type definition of type.
            // To create generic method G<Foo>.M<Bar> these invariants require that G<Foo>.M<S> be created by calling
            // this function followed by MakeGenericMethod on the resulting MethodInfo to finally get G<Foo>.M<Bar>.
            // We could also allow G<T>.M<Bar> to be created before G<Foo>.M<Bar> (BindGenParm followed by this method)
            // if we wanted to but that just complicates things so these checks are designed to prevent that scenario.

            if (method.IsGenericMethod && !method.IsGenericMethodDefinition)
                throw new ArgumentException(SR.Argument_NeedGenericMethodDefinition, nameof(method));

            if (method.DeclaringType == null || !method.DeclaringType.IsGenericTypeDefinition)
                throw new ArgumentException(SR.Argument_MethodNeedGenericDeclaringType, nameof(method));

            if (type.GetGenericTypeDefinition() != method.DeclaringType)
                throw new ArgumentException(SR.Argument_InvalidMethodDeclaringType, nameof(type));

            // The following converts from Type or TypeBuilder of G<T> to TypeBuilderInstantiation G<T>. These types
            // both logically represent the same thing. The runtime displays a similar convention by having
            // G<M>.M() be encoded by a typeSpec whose parent is the typeDef for G<M> and whose instantiation is also G<M>.
            if (type.IsGenericTypeDefinition)
                type = type.MakeGenericType(type.GetGenericArguments());

            if (!(type is TypeBuilderInstantiation))
                throw new ArgumentException(SR.Argument_NeedNonGenericType, nameof(type));

            return MethodOnTypeBuilderInstantiation.GetMethod(method, (type as TypeBuilderInstantiation)!);
        }
        public static ConstructorInfo GetConstructor(Type type, ConstructorInfo constructor)
        {
            if (!(type is TypeBuilder) && !(type is TypeBuilderInstantiation))
                throw new ArgumentException(SR.Argument_MustBeTypeBuilder);

            if (!constructor.DeclaringType!.IsGenericTypeDefinition)
                throw new ArgumentException(SR.Argument_ConstructorNeedGenericDeclaringType, nameof(constructor));

            if (!(type is TypeBuilderInstantiation))
                throw new ArgumentException(SR.Argument_NeedNonGenericType, nameof(type));

            // TypeBuilder G<T> ==> TypeBuilderInstantiation G<T>
            if (type is TypeBuilder && type.IsGenericTypeDefinition)
                type = type.MakeGenericType(type.GetGenericArguments());

            if (type.GetGenericTypeDefinition() != constructor.DeclaringType)
                throw new ArgumentException(SR.Argument_InvalidConstructorDeclaringType, nameof(type));

            return ConstructorOnTypeBuilderInstantiation.GetConstructor(constructor, (type as TypeBuilderInstantiation)!);
        }
        public static FieldInfo GetField(Type type, FieldInfo field)
        {
            if (!(type is TypeBuilder) && !(type is TypeBuilderInstantiation))
                throw new ArgumentException(SR.Argument_MustBeTypeBuilder);

            if (!field.DeclaringType!.IsGenericTypeDefinition)
                throw new ArgumentException(SR.Argument_FieldNeedGenericDeclaringType, nameof(field));

            if (!(type is TypeBuilderInstantiation))
                throw new ArgumentException(SR.Argument_NeedNonGenericType, nameof(type));

            // TypeBuilder G<T> ==> TypeBuilderInstantiation G<T>
            if (type is TypeBuilder && type.IsGenericTypeDefinition)
                type = type.MakeGenericType(type.GetGenericArguments());

            if (type.GetGenericTypeDefinition() != field.DeclaringType)
                throw new ArgumentException(SR.Argument_InvalidFieldDeclaringType, nameof(type));

            return FieldOnTypeBuilderInstantiation.GetField(field, (type as TypeBuilderInstantiation)!);
        }
        #endregion

        #region Public Const
        public const int UnspecifiedTypeSize = 0;
        #endregion

        #region Private Static FCalls
        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void SetParentType(QCallModule module, int tdTypeDef, int tkParent);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void AddInterfaceImpl(QCallModule module, int tdTypeDef, int tkInterface);
        #endregion

        #region Internal Static FCalls
        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern int DefineMethod(QCallModule module, int tkParent, string name, byte[] signature, int sigLength,
            MethodAttributes attributes);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern int DefineMethodSpec(QCallModule module, int tkParent, byte[] signature, int sigLength);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern int DefineField(QCallModule module, int tkParent, string name, byte[] signature, int sigLength,
            FieldAttributes attributes);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void SetMethodIL(QCallModule module, int tk, bool isInitLocals,
            byte[]? body, int bodyLength,
            byte[] LocalSig, int sigLength,
            int maxStackSize,
            ExceptionHandler[]? exceptions, int numExceptions,
            int[]? tokenFixups, int numTokenFixups);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void DefineCustomAttribute(QCallModule module, int tkAssociate, int tkConstructor,
            byte[]? attr, int attrLength, bool toDisk, bool updateCompilerFlags);

        internal static void DefineCustomAttribute(ModuleBuilder module, int tkAssociate, int tkConstructor,
            byte[]? attr, bool toDisk, bool updateCompilerFlags)
        {
            byte[]? localAttr = null;

            if (attr != null)
            {
                localAttr = new byte[attr.Length];
                Buffer.BlockCopy(attr, 0, localAttr, 0, attr.Length);
            }

            DefineCustomAttribute(new QCallModule(ref module), tkAssociate, tkConstructor,
                localAttr, (localAttr != null) ? localAttr.Length : 0, toDisk, updateCompilerFlags);
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern int DefineProperty(QCallModule module, int tkParent, string name, PropertyAttributes attributes,
            byte[] signature, int sigLength);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern int DefineEvent(QCallModule module, int tkParent, string name, EventAttributes attributes, int tkEventType);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern void DefineMethodSemantics(QCallModule module, int tkAssociation,
            MethodSemanticsAttributes semantics, int tkMethod);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern void DefineMethodImpl(QCallModule module, int tkType, int tkBody, int tkDecl);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern void SetMethodImpl(QCallModule module, int tkMethod, MethodImplAttributes MethodImplAttributes);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern int SetParamInfo(QCallModule module, int tkMethod, int iSequence,
            ParameterAttributes iParamAttributes, string? strParamName);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern int GetTokenFromSig(QCallModule module, byte[] signature, int sigLength);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern void SetFieldLayoutOffset(QCallModule module, int fdToken, int iOffset);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern void SetClassLayout(QCallModule module, int tk, PackingSize iPackingSize, int iTypeSize);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern unsafe void SetConstantValue(QCallModule module, int tk, int corType, void* pValue);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void SetPInvokeData(QCallModule module, string DllName, string name, int token, int linkFlags);

        #endregion
        #region Internal\Private Static Members

        internal static bool IsTypeEqual(Type? t1, Type? t2)
        {
            // Maybe we are lucky that they are equal in the first place
            if (t1 == t2)
                return true;
            TypeBuilder? tb1 = null;
            TypeBuilder? tb2 = null;
            Type? runtimeType1;
            Type? runtimeType2;

            // set up the runtimeType and TypeBuilder type corresponding to t1 and t2
            if (t1 is TypeBuilder)
            {
                tb1 = (TypeBuilder)t1;
                // This will be null if it is not baked.
                runtimeType1 = tb1.m_bakedRuntimeType;
            }
            else
            {
                runtimeType1 = t1;
            }

            if (t2 is TypeBuilder)
            {
                tb2 = (TypeBuilder)t2;
                // This will be null if it is not baked.
                runtimeType2 = tb2.m_bakedRuntimeType;
            }
            else
            {
                runtimeType2 = t2;
            }

            // If the type builder view is equal then it is equal
            if (tb1 != null && tb2 != null && ReferenceEquals(tb1, tb2))
                return true;

            // if the runtimetype view is eqaul than it is equal
            if (runtimeType1 != null && runtimeType2 != null && runtimeType1 == runtimeType2)
                return true;

            return false;
        }

        internal static unsafe void SetConstantValue(ModuleBuilder module, int tk, Type destType, object? value)
        {
            // This is a helper function that is used by ParameterBuilder, PropertyBuilder,
            // and FieldBuilder to validate a default value and save it in the meta-data.

            if (value != null)
            {
                Type type = value.GetType();

                // We should allow setting a constant value on a ByRef parameter
                if (destType.IsByRef)
                    destType = destType.GetElementType()!;

                // Convert nullable types to their underlying type.
                // This is necessary for nullable enum types to pass the IsEnum check that's coming next.
                destType = Nullable.GetUnderlyingType(destType) ?? destType;

                if (destType.IsEnum)
                {
                    // |                                   |  UnderlyingSystemType     |  Enum.GetUnderlyingType() |  IsEnum
                    // |-----------------------------------|---------------------------|---------------------------|---------
                    // | runtime Enum Type                 |  self                     |  underlying type of enum  |  TRUE
                    // | EnumBuilder                       |  underlying type of enum  |  underlying type of enum* |  TRUE
                    // | TypeBuilder of enum types**       |  underlying type of enum  |  Exception                |  TRUE
                    // | TypeBuilder of enum types (baked) |  runtime enum type        |  Exception                |  TRUE

                    // *: the behavior of Enum.GetUnderlyingType(EnumBuilder) might change in the future
                    //     so let's not depend on it.
                    // **: created with System.Enum as the parent type.

                    // The above behaviors might not be the most consistent but we have to live with them.

                    Type? underlyingType;
                    if (destType is EnumBuilder enumBldr)
                    {
                        underlyingType = enumBldr.GetEnumUnderlyingType();

                        // The constant value supplied should match either the baked enum type or its underlying type
                        // we don't need to compare it with the EnumBuilder itself because you can never have an object of that type
                        if (type != enumBldr.m_typeBuilder.m_bakedRuntimeType && type != underlyingType)
                            throw new ArgumentException(SR.Argument_ConstantDoesntMatch);
                    }
                    else if (destType is TypeBuilder typeBldr)
                    {
                        underlyingType = typeBldr.m_enumUnderlyingType;

                        // The constant value supplied should match either the baked enum type or its underlying type
                        // typeBldr.m_enumUnderlyingType is null if the user hasn't created a "value__" field on the enum
                        if (underlyingType == null || (type != typeBldr.UnderlyingSystemType && type != underlyingType))
                            throw new ArgumentException(SR.Argument_ConstantDoesntMatch);
                    }
                    else // must be a runtime Enum Type
                    {
                        Debug.Assert(destType is RuntimeType, "destType is not a runtime type, an EnumBuilder, or a TypeBuilder.");

                        underlyingType = Enum.GetUnderlyingType(destType);

                        // The constant value supplied should match either the enum itself or its underlying type
                        if (type != destType && type != underlyingType)
                            throw new ArgumentException(SR.Argument_ConstantDoesntMatch);
                    }

                    type = underlyingType;
                }
                else
                {
                    // Note that it is non CLS compliant if destType != type. But RefEmit never guarantees CLS-Compliance.
                    if (!destType.IsAssignableFrom(type))
                        throw new ArgumentException(SR.Argument_ConstantDoesntMatch);
                }

                CorElementType corType = RuntimeTypeHandle.GetCorElementType((RuntimeType)type);

                switch (corType)
                {
                    case CorElementType.ELEMENT_TYPE_I1:
                    case CorElementType.ELEMENT_TYPE_U1:
                    case CorElementType.ELEMENT_TYPE_BOOLEAN:
                    case CorElementType.ELEMENT_TYPE_I2:
                    case CorElementType.ELEMENT_TYPE_U2:
                    case CorElementType.ELEMENT_TYPE_CHAR:
                    case CorElementType.ELEMENT_TYPE_I4:
                    case CorElementType.ELEMENT_TYPE_U4:
                    case CorElementType.ELEMENT_TYPE_R4:
                    case CorElementType.ELEMENT_TYPE_I8:
                    case CorElementType.ELEMENT_TYPE_U8:
                    case CorElementType.ELEMENT_TYPE_R8:
                        fixed (byte* pData = &value.GetRawData())
                            SetConstantValue(new QCallModule(ref module), tk, (int)corType, pData);
                        break;

                    default:
                        if (type == typeof(string))
                        {
                            fixed (char* pString = (string)value)
                                SetConstantValue(new QCallModule(ref module), tk, (int)CorElementType.ELEMENT_TYPE_STRING, pString);
                        }
                        else if (type == typeof(DateTime))
                        {
                            // date is a I8 representation
                            long ticks = ((DateTime)value).Ticks;
                            SetConstantValue(new QCallModule(ref module), tk, (int)CorElementType.ELEMENT_TYPE_I8, &ticks);
                        }
                        else
                        {
                            throw new ArgumentException(SR.Format(SR.Argument_ConstantNotSupported, type));
                        }
                        break;
                }
            }
            else
            {
                // A null default value in metadata is permissible even for non-nullable value types.
                // (See ECMA-335 II.15.4.1.4 "The .param directive" and II.22.9 "Constant" for details.)
                // This is how the Roslyn compilers generally encode `default(TValueType)` default values.

                SetConstantValue(new QCallModule(ref module), tk, (int)CorElementType.ELEMENT_TYPE_CLASS, null);
            }
        }

        #endregion

        #region Private Data Members
        private List<CustAttr>? m_ca;
        private TypeToken m_tdType;
        private readonly ModuleBuilder m_module;
        private readonly string? m_strName;
        private readonly string? m_strNameSpace;
        private string? m_strFullQualName;

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        private Type? m_typeParent;

        private List<Type>? m_typeInterfaces;
        private readonly TypeAttributes m_iAttr;
        private GenericParameterAttributes m_genParamAttributes;
        internal List<MethodBuilder>? m_listMethods;
        internal int m_lastTokenizedMethod;
        private int m_constructorCount;
        private readonly int m_iTypeSize;
        private readonly PackingSize m_iPackingSize;
        private readonly TypeBuilder? m_DeclaringType;

        // We cannot store this on EnumBuilder because users can define enum types manually using TypeBuilder.
        private Type? m_enumUnderlyingType;
        internal bool m_isHiddenGlobalType;
        private bool m_hasBeenCreated;

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        private RuntimeType m_bakedRuntimeType = null!;

        private readonly int m_genParamPos;
        private GenericTypeParameterBuilder[]? m_inst;
        private readonly bool m_bIsGenParam;
        private readonly MethodBuilder? m_declMeth;
        private readonly TypeBuilder? m_genTypeDef;
        #endregion

        #region Constructor
        // ctor for the global (module) type
        internal TypeBuilder(ModuleBuilder module)
        {
            m_tdType = new TypeToken((int)MetadataTokenType.TypeDef);
            m_isHiddenGlobalType = true;
            m_module = module;
            m_listMethods = new List<MethodBuilder>();
            // No token has been created so let's initialize it to -1
            // The first time we call MethodBuilder.GetToken this will incremented.
            m_lastTokenizedMethod = -1;
        }

        // ctor for generic method parameter
        internal TypeBuilder(string szName, int genParamPos, MethodBuilder declMeth)
        {
            m_strName = szName;
            m_genParamPos = genParamPos;
            m_bIsGenParam = true;
            m_typeInterfaces = new List<Type>();

            Debug.Assert(declMeth != null);
            m_declMeth = declMeth;
            m_DeclaringType = m_declMeth.GetTypeBuilder();
            m_module = declMeth.GetModuleBuilder();
        }

        // ctor for generic type parameter
        private TypeBuilder(string szName, int genParamPos, TypeBuilder declType)
        {
            m_strName = szName;
            m_genParamPos = genParamPos;
            m_bIsGenParam = true;
            m_typeInterfaces = new List<Type>();

            Debug.Assert(declType != null);
            m_DeclaringType = declType;
            m_module = declType.GetModuleBuilder();
        }

        internal TypeBuilder(
            string fullname, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, Type[]? interfaces, ModuleBuilder module,
            PackingSize iPackingSize, int iTypeSize, TypeBuilder? enclosingType)
        {
            if (fullname == null)
                throw new ArgumentNullException(nameof(fullname));

            if (fullname.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyName, nameof(fullname));

            if (fullname[0] == '\0')
                throw new ArgumentException(SR.Argument_IllegalName, nameof(fullname));

            if (fullname.Length > 1023)
                throw new ArgumentException(SR.Argument_TypeNameTooLong, nameof(fullname));

            int i;
            m_module = module;
            m_DeclaringType = enclosingType;
            AssemblyBuilder containingAssem = m_module.ContainingAssemblyBuilder;

            // cannot have two types within the same assembly of the same name
            containingAssem._assemblyData.CheckTypeNameConflict(fullname, enclosingType);

            if (enclosingType != null)
            {
                // Nested Type should have nested attribute set.
                // If we are renumbering TypeAttributes' bit, we need to change the logic here.
                if (((attr & TypeAttributes.VisibilityMask) == TypeAttributes.Public) || ((attr & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic))
                    throw new ArgumentException(SR.Argument_BadNestedTypeFlags, nameof(attr));
            }

            int[]? interfaceTokens = null;
            if (interfaces != null)
            {
                for (i = 0; i < interfaces.Length; i++)
                {
                    if (interfaces[i] == null)
                    {
                        // cannot contain null in the interface list
                        throw new ArgumentNullException(nameof(interfaces));
                    }
                }
                interfaceTokens = new int[interfaces.Length + 1];
                for (i = 0; i < interfaces.Length; i++)
                {
                    interfaceTokens[i] = m_module.GetTypeTokenInternal(interfaces[i]).Token;
                }
            }

            int iLast = fullname.LastIndexOf('.');
            if (iLast == -1 || iLast == 0)
            {
                // no name space
                m_strNameSpace = string.Empty;
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

            m_tdType = new TypeToken(DefineType(new QCallModule(ref module),
                fullname, tkParent, m_iAttr, tkEnclosingType, interfaceTokens!));

            m_iPackingSize = iPackingSize;
            m_iTypeSize = iTypeSize;
            if ((m_iPackingSize != 0) || (m_iTypeSize != 0))
                SetClassLayout(new QCallModule(ref module), m_tdType.Token, m_iPackingSize, m_iTypeSize);

            m_module.AddType(FullName!, this);
        }

        #endregion
        #region Private Members
        private FieldBuilder DefineDataHelper(string name, byte[]? data, int size, FieldAttributes attributes)
        {
            string strValueClassName;
            TypeBuilder? valueClassType;
            FieldBuilder fdBuilder;
            TypeAttributes typeAttributes;

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (name.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyName, nameof(name));

            if (size <= 0 || size >= 0x003f0000)
                throw new ArgumentException(SR.Argument_BadSizeForData);

            ThrowIfCreated();

            // form the value class name
            strValueClassName = ModuleBuilderData.MultiByteValueClass + size.ToString();

            // Is this already defined in this module?
            Type? temp = m_module.FindTypeBuilderWithName(strValueClassName, false);
            valueClassType = temp as TypeBuilder;

            if (valueClassType == null)
            {
                typeAttributes = TypeAttributes.Public | TypeAttributes.ExplicitLayout | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.AnsiClass;

                // Define the backing value class
                valueClassType = m_module.DefineType(strValueClassName, typeAttributes, typeof(System.ValueType), PackingSize.Size1, size);
                valueClassType.CreateType();
            }

            fdBuilder = DefineField(name, valueClassType, attributes | FieldAttributes.Static);

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
                if (((attr & TypeAttributes.VisibilityMask) != TypeAttributes.NotPublic) && ((attr & TypeAttributes.VisibilityMask) != TypeAttributes.Public))
                {
                    throw new ArgumentException(SR.Argument_BadTypeAttrNestedVisibilityOnNonNestedType);
                }
            }
            else
            {
                // Nested class.
                if (((attr & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic) || ((attr & TypeAttributes.VisibilityMask) == TypeAttributes.Public))
                {
                    throw new ArgumentException(SR.Argument_BadTypeAttrNonNestedVisibilityNestedType);
                }
            }

            // Verify that the layout mask is valid.
            if (((attr & TypeAttributes.LayoutMask) != TypeAttributes.AutoLayout) && ((attr & TypeAttributes.LayoutMask) != TypeAttributes.SequentialLayout) && ((attr & TypeAttributes.LayoutMask) != TypeAttributes.ExplicitLayout))
            {
                throw new ArgumentException(SR.Argument_BadTypeAttrInvalidLayout);
            }

            // Check if the user attempted to set any reserved bits.
            if ((attr & TypeAttributes.ReservedMask) != 0)
            {
                throw new ArgumentException(SR.Argument_BadTypeAttrReservedBitsSet);
            }
        }

        public bool IsCreated()
        {
            return m_hasBeenCreated;
        }
        #endregion

        #region FCalls
        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern int DefineType(QCallModule module,
            string fullname, int tkParent, TypeAttributes attributes, int tkEnclosingType, int[] interfaceTokens);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern int DefineGenericParam(QCallModule module,
            string name, int tkParent, GenericParameterAttributes attributes, int position, int[] constraints);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void TermCreateClass(QCallModule module, int tk, ObjectHandleOnStack type);
        #endregion

        #region Internal Methods
        internal void ThrowIfCreated()
        {
            if (IsCreated())
                throw new InvalidOperationException(SR.InvalidOperation_TypeHasBeenCreated);
        }

        internal object SyncRoot => m_module.SyncRoot;

        internal ModuleBuilder GetModuleBuilder()
        {
            return m_module;
        }

        internal RuntimeType BakedRuntimeType => m_bakedRuntimeType;

        internal void SetGenParamAttributes(GenericParameterAttributes genericParameterAttributes)
        {
            m_genParamAttributes = genericParameterAttributes;
        }

        internal void SetGenParamCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            CustAttr ca = new CustAttr(con, binaryAttribute);

            lock (SyncRoot)
            {
                SetGenParamCustomAttributeNoLock(ca);
            }
        }

        internal void SetGenParamCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            CustAttr ca = new CustAttr(customBuilder);

            lock (SyncRoot)
            {
                SetGenParamCustomAttributeNoLock(ca);
            }
        }

        private void SetGenParamCustomAttributeNoLock(CustAttr ca)
        {
            m_ca ??= new List<TypeBuilder.CustAttr>();
            m_ca.Add(ca);
        }
        #endregion

        #region Object Overrides
        public override string ToString()
        {
            return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.ToString)!;
        }

        #endregion

        #region MemberInfo Overrides
        public override Type? DeclaringType => m_DeclaringType;

        public override Type? ReflectedType => m_DeclaringType;

        public override string Name =>
                // one of the constructors allows this to be null but it is only used internally without accessing Name
                m_strName!;

        public override Module Module => GetModuleBuilder();

        public override bool IsByRefLike => false;

        internal int MetadataTokenInternal => m_tdType.Token;

        #endregion

        #region Type Overrides
        public override Guid GUID
        {
            get
            {
                if (!IsCreated())
                    throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

                return m_bakedRuntimeType.GUID;
            }
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public override object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target,
            object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters)
        {
            if (!IsCreated())
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

            return m_bakedRuntimeType.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
        }

        public override Assembly Assembly => m_module.Assembly;

        public override RuntimeTypeHandle TypeHandle => throw new NotSupportedException(SR.NotSupported_DynamicModule);

        public override string? FullName => m_strFullQualName ??= TypeNameBuilder.ToString(this, TypeNameBuilder.Format.FullName);

        public override string? Namespace => m_strNameSpace;

        public override string? AssemblyQualifiedName => TypeNameBuilder.ToString(this, TypeNameBuilder.Format.AssemblyQualifiedName);

        public override Type? BaseType => m_typeParent;

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder,
                CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers)
        {
            if (!IsCreated())
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

            return m_bakedRuntimeType.GetConstructor(bindingAttr, binder, callConvention, types, modifiers);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

            return m_bakedRuntimeType.GetConstructors(bindingAttr);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder,
                CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
        {
            if (!IsCreated())
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

            if (types == null)
            {
                return m_bakedRuntimeType.GetMethod(name, bindingAttr);
            }
            else
            {
                return m_bakedRuntimeType.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
            }
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

            return m_bakedRuntimeType.GetMethods(bindingAttr);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo? GetField(string name, BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

            return m_bakedRuntimeType.GetField(name, bindingAttr);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

            return m_bakedRuntimeType.GetFields(bindingAttr);
        }

        public override Type? GetInterface(string name, bool ignoreCase)
        {
            if (!IsCreated())
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

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
                return Array.Empty<Type>();
            }

            return m_typeInterfaces.ToArray();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo? GetEvent(string name, BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

            return m_bakedRuntimeType.GetEvent(name, bindingAttr);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
        public override EventInfo[] GetEvents()
        {
            if (!IsCreated())
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

            return m_bakedRuntimeType.GetEvents();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder,
                Type? returnType, Type[]? types, ParameterModifier[]? modifiers)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

            return m_bakedRuntimeType.GetProperties(bindingAttr);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

            return m_bakedRuntimeType.GetNestedTypes(bindingAttr);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type? GetNestedType(string name, BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

            return m_bakedRuntimeType.GetNestedType(name, bindingAttr);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

            return m_bakedRuntimeType.GetMember(name, type, bindingAttr);
        }

        public override InterfaceMapping GetInterfaceMap(Type interfaceType)
        {
            if (!IsCreated())
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

            return m_bakedRuntimeType.GetInterfaceMap(interfaceType);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

            return m_bakedRuntimeType.GetEvents(bindingAttr);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

            return m_bakedRuntimeType.GetMembers(bindingAttr);
        }

        public override bool IsAssignableFrom([NotNullWhen(true)] Type? c)
        {
            if (IsTypeEqual(c, this))
                return true;

            Type? fromRuntimeType;
            TypeBuilder? fromTypeBuilder = c as TypeBuilder;

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

            if (!IsInterface)
                return false;

            // now is This type a base type on one of the interface impl?
            Type[] interfaces = fromTypeBuilder.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                // unfortunately, IsSubclassOf does not cover the case when they are the same type.
                if (IsTypeEqual(interfaces[i], this))
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

        public override bool IsTypeDefinition => true;

        public override bool IsSZArray => false;

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
            return ((GetAttributeFlagsImpl() & TypeAttributes.Import) != 0) ? true : false;
        }

        public override Type GetElementType()
        {
            // You will never have to deal with a TypeBuilder if you are just referring to arrays.
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        protected override bool HasElementTypeImpl()
        {
            return false;
        }

        public override bool IsSecurityCritical => true;

        public override bool IsSecuritySafeCritical => false;

        public override bool IsSecurityTransparent => false;

        public override bool IsSubclassOf(Type c)
        {
            Type? p = this;

            if (IsTypeEqual(p, c))
                return false;

            p = p.BaseType;

            while (p != null)
            {
                if (IsTypeEqual(p, c))
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
                        throw new InvalidOperationException(SR.InvalidOperation_NoUnderlyingTypeOnEnum);

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
            return SymbolType.FormCompoundType("*", this, 0)!;
        }

        public override Type MakeByRefType()
        {
            return SymbolType.FormCompoundType("&", this, 0)!;
        }

        public override Type MakeArrayType()
        {
            return SymbolType.FormCompoundType("[]", this, 0)!;
        }

        public override Type MakeArrayType(int rank)
        {
            string s = GetRankString(rank);
            return SymbolType.FormCompoundType(s, this, 0)!;
        }

        #endregion

        #region ICustomAttributeProvider Implementation
        public override object[] GetCustomAttributes(bool inherit)
        {
            if (!IsCreated())
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

            return CustomAttribute.GetCustomAttributes(m_bakedRuntimeType, (typeof(object) as RuntimeType)!, inherit);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (!IsCreated())
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));

            RuntimeType? attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.GetCustomAttributes(m_bakedRuntimeType, attributeRuntimeType, inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (!IsCreated())
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));

            RuntimeType? attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.IsDefined(m_bakedRuntimeType, attributeRuntimeType, inherit);
        }

        #endregion

        #region Public Member

        #region DefineType
        public override GenericParameterAttributes GenericParameterAttributes => m_genParamAttributes;

        internal void SetInterfaces(params Type[]? interfaces)
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
                throw new ArgumentNullException(nameof(names));

            if (names.Length == 0)
                throw new ArgumentException(SR.Arg_EmptyArray, nameof(names));

            for (int i = 0; i < names.Length; i++)
                if (names[i] == null)
                    throw new ArgumentNullException(nameof(names));

            if (m_inst != null)
                throw new InvalidOperationException();

            m_inst = new GenericTypeParameterBuilder[names.Length];
            for (int i = 0; i < names.Length; i++)
                m_inst[i] = new GenericTypeParameterBuilder(new TypeBuilder(names[i], i, this));

            return m_inst;
        }

        public override Type MakeGenericType(params Type[] typeArguments)
        {
            CheckContext(typeArguments);

            return TypeBuilderInstantiation.MakeGenericType(this, typeArguments);
        }

        public override Type[] GetGenericArguments() => m_inst ?? Array.Empty<Type>();

        // If a TypeBuilder is generic, it must be a generic type definition
        // All instantiated generic types are TypeBuilderInstantiation.
        public override bool IsGenericTypeDefinition => IsGenericType;
        public override bool IsGenericType => m_inst != null;
        public override bool IsGenericParameter => m_bIsGenParam;
        public override bool IsConstructedGenericType => false;

        public override int GenericParameterPosition => m_genParamPos;
        public override MethodBase? DeclaringMethod => m_declMeth;
        public override Type GetGenericTypeDefinition() { if (IsGenericTypeDefinition) return this; if (m_genTypeDef == null) throw new InvalidOperationException(); return m_genTypeDef; }
        #endregion

        #region Define Method
        public void DefineMethodOverride(MethodInfo methodInfoBody, MethodInfo methodInfoDeclaration)
        {
            lock (SyncRoot)
            {
                DefineMethodOverrideNoLock(methodInfoBody, methodInfoDeclaration);
            }
        }

        private void DefineMethodOverrideNoLock(MethodInfo methodInfoBody, MethodInfo methodInfoDeclaration)
        {
            if (methodInfoBody == null)
                throw new ArgumentNullException(nameof(methodInfoBody));

            if (methodInfoDeclaration == null)
                throw new ArgumentNullException(nameof(methodInfoDeclaration));

            ThrowIfCreated();

            if (!ReferenceEquals(methodInfoBody.DeclaringType, this))
                // Loader restriction: body method has to be from this class
                throw new ArgumentException(SR.ArgumentException_BadMethodImplBody);

            MethodToken tkBody = m_module.GetMethodTokenInternal(methodInfoBody);
            MethodToken tkDecl = m_module.GetMethodTokenInternal(methodInfoDeclaration);

            ModuleBuilder module = m_module;
            DefineMethodImpl(new QCallModule(ref module), m_tdType.Token, tkBody.Token, tkDecl.Token);
        }

        public MethodBuilder DefineMethod(string name, MethodAttributes attributes, Type? returnType, Type[]? parameterTypes)
        {
            return DefineMethod(name, attributes, CallingConventions.Standard, returnType, parameterTypes);
        }

        public MethodBuilder DefineMethod(string name, MethodAttributes attributes)
        {
            return DefineMethod(name, attributes, CallingConventions.Standard, null, null);
        }

        public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention)
        {
            return DefineMethod(name, attributes, callingConvention, null, null);
        }

        public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? parameterTypes)
        {
            return DefineMethod(name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null);
        }

        public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
        {
            lock (SyncRoot)
            {
                return DefineMethodNoLock(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers,
                                          returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers,
                                          parameterTypeOptionalCustomModifiers);
            }
        }

        private MethodBuilder DefineMethodNoLock(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (name.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyName, nameof(name));

            CheckContext(returnType);
            CheckContext(returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes);
            CheckContext(parameterTypeRequiredCustomModifiers);
            CheckContext(parameterTypeOptionalCustomModifiers);

            if (parameterTypes != null)
            {
                if (parameterTypeOptionalCustomModifiers != null && parameterTypeOptionalCustomModifiers.Length != parameterTypes.Length)
                    throw new ArgumentException(SR.Format(SR.Argument_MismatchedArrays, nameof(parameterTypeOptionalCustomModifiers), nameof(parameterTypes)));

                if (parameterTypeRequiredCustomModifiers != null && parameterTypeRequiredCustomModifiers.Length != parameterTypes.Length)
                    throw new ArgumentException(SR.Format(SR.Argument_MismatchedArrays, nameof(parameterTypeRequiredCustomModifiers), nameof(parameterTypes)));
            }

            ThrowIfCreated();

#if !FEATURE_DEFAULT_INTERFACES
            if (!m_isHiddenGlobalType)
            {
                if (((m_iAttr & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface) &&
                   (attributes & MethodAttributes.Abstract) == 0 && (attributes & MethodAttributes.Static) == 0)
                    throw new ArgumentException(SR.Argument_BadAttributeOnInterfaceMethod);
            }
#endif

            // pass in Method attributes
            MethodBuilder method = new MethodBuilder(
                name, attributes, callingConvention,
                returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers,
                parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers,
                m_module, this);

            if (!m_isHiddenGlobalType)
            {
                // If this method is declared to be a constructor, increment our constructor count.
                if ((method.Attributes & MethodAttributes.SpecialName) != 0 && method.Name.Equals(ConstructorInfo.ConstructorName))
                {
                    m_constructorCount++;
                }
            }

            m_listMethods!.Add(method);

            return method;
        }

        public MethodBuilder DefinePInvokeMethod(string name, string dllName, MethodAttributes attributes,
            CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes,
            CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            MethodBuilder method = DefinePInvokeMethodHelper(
                name, dllName, name, attributes, callingConvention, returnType, null, null,
                parameterTypes, null, null, nativeCallConv, nativeCharSet);
            return method;
        }

        public MethodBuilder DefinePInvokeMethod(string name, string dllName, string entryName, MethodAttributes attributes,
            CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes,
            CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            MethodBuilder method = DefinePInvokeMethodHelper(
                name, dllName, entryName, attributes, callingConvention, returnType, null, null,
                parameterTypes, null, null, nativeCallConv, nativeCharSet);
            return method;
        }

        public MethodBuilder DefinePInvokeMethod(string name, string dllName, string entryName, MethodAttributes attributes,
            CallingConventions callingConvention,
            Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers,
            CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            MethodBuilder method = DefinePInvokeMethodHelper(
            name, dllName, entryName, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers,
            parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers, nativeCallConv, nativeCharSet);
            return method;
        }

        private MethodBuilder DefinePInvokeMethodHelper(
            string name, string dllName, string importName, MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers,
            CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            CheckContext(returnType);
            CheckContext(returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes);
            CheckContext(parameterTypeRequiredCustomModifiers);
            CheckContext(parameterTypeOptionalCustomModifiers);

            lock (SyncRoot)
            {
                if (name == null)
                    throw new ArgumentNullException(nameof(name));

                if (name.Length == 0)
                    throw new ArgumentException(SR.Argument_EmptyName, nameof(name));

                if (dllName == null)
                    throw new ArgumentNullException(nameof(dllName));

                if (dllName.Length == 0)
                    throw new ArgumentException(SR.Argument_EmptyName, nameof(dllName));

                if (importName == null)
                    throw new ArgumentNullException(nameof(importName));

                if (importName.Length == 0)
                    throw new ArgumentException(SR.Argument_EmptyName, nameof(importName));

                if ((attributes & MethodAttributes.Abstract) != 0)
                    throw new ArgumentException(SR.Argument_BadPInvokeMethod);

                if ((m_iAttr & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface)
                    throw new ArgumentException(SR.Argument_BadPInvokeOnInterface);

                ThrowIfCreated();

                attributes |= MethodAttributes.PinvokeImpl;
                MethodBuilder method = new MethodBuilder(name, attributes, callingConvention,
                    returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers,
                    parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers,
                    m_module, this);

                // The signature grabbing code has to be up here or the signature won't be finished
                // and our equals check won't work.
                _ = method.GetMethodSignature().InternalGetSignature(out _);

                if (m_listMethods!.Contains(method))
                {
                    throw new ArgumentException(SR.Argument_MethodRedefined);
                }
                m_listMethods.Add(method);

                MethodToken token = method.GetToken();

                int linkFlags = 0;
                switch (nativeCallConv)
                {
                    case CallingConvention.Winapi:
                        linkFlags = (int)PInvokeAttributes.CallConvWinapi;
                        break;
                    case CallingConvention.Cdecl:
                        linkFlags = (int)PInvokeAttributes.CallConvCdecl;
                        break;
                    case CallingConvention.StdCall:
                        linkFlags = (int)PInvokeAttributes.CallConvStdcall;
                        break;
                    case CallingConvention.ThisCall:
                        linkFlags = (int)PInvokeAttributes.CallConvThiscall;
                        break;
                    case CallingConvention.FastCall:
                        linkFlags = (int)PInvokeAttributes.CallConvFastcall;
                        break;
                }
                switch (nativeCharSet)
                {
                    case CharSet.None:
                        linkFlags |= (int)PInvokeAttributes.CharSetNotSpec;
                        break;
                    case CharSet.Ansi:
                        linkFlags |= (int)PInvokeAttributes.CharSetAnsi;
                        break;
                    case CharSet.Unicode:
                        linkFlags |= (int)PInvokeAttributes.CharSetUnicode;
                        break;
                    case CharSet.Auto:
                        linkFlags |= (int)PInvokeAttributes.CharSetAuto;
                        break;
                }

                ModuleBuilder module = m_module;
                SetPInvokeData(new QCallModule(ref module),
                    dllName,
                    importName,
                    token.Token,
                    linkFlags);
                method.SetToken(token);

                return method;
            }
        }
        #endregion

        #region Define Constructor
        public ConstructorBuilder DefineTypeInitializer()
        {
            lock (SyncRoot)
            {
                return DefineTypeInitializerNoLock();
            }
        }

        private ConstructorBuilder DefineTypeInitializerNoLock()
        {
            ThrowIfCreated();

            // change the attributes and the class constructor's name
            const MethodAttributes attr = MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName;

            ConstructorBuilder constBuilder = new ConstructorBuilder(
                ConstructorInfo.TypeConstructorName, attr, CallingConventions.Standard, null, m_module, this);

            return constBuilder;
        }

        public ConstructorBuilder DefineDefaultConstructor(MethodAttributes attributes)
        {
            if ((m_iAttr & TypeAttributes.Interface) == TypeAttributes.Interface)
            {
                throw new InvalidOperationException(SR.InvalidOperation_ConstructorNotAllowedOnInterface);
            }

            lock (SyncRoot)
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
            ConstructorInfo? con = null;

            if (m_typeParent is TypeBuilderInstantiation)
            {
                Type? genericTypeDefinition = m_typeParent.GetGenericTypeDefinition();

                if (genericTypeDefinition is TypeBuilder)
                    genericTypeDefinition = ((TypeBuilder)genericTypeDefinition).m_bakedRuntimeType;

                if (genericTypeDefinition == null)
                    throw new NotSupportedException(SR.NotSupported_DynamicModule);

                Type inst = genericTypeDefinition.MakeGenericType(m_typeParent.GetGenericArguments());

                if (inst is TypeBuilderInstantiation)
                    con = GetConstructor(inst, genericTypeDefinition.GetConstructor(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, EmptyTypes, null)!);
                else
                    con = inst.GetConstructor(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, EmptyTypes, null);
            }

            if (con == null)
            {
                con = m_typeParent!.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, EmptyTypes, null);
            }

            if (con == null)
                throw new NotSupportedException(SR.NotSupported_NoParentDefaultConstructor);

            // Define the constructor Builder
            constBuilder = DefineConstructor(attributes, CallingConventions.Standard, null);
            m_constructorCount++;

            // generate the code to call the parent's default constructor
            ILGenerator il = constBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, con);
            il.Emit(OpCodes.Ret);

            constBuilder.m_isDefaultConstructor = true;
            return constBuilder;
        }

        public ConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions callingConvention, Type[]? parameterTypes)
        {
            return DefineConstructor(attributes, callingConvention, parameterTypes, null, null);
        }

        public ConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions callingConvention,
            Type[]? parameterTypes, Type[][]? requiredCustomModifiers, Type[][]? optionalCustomModifiers)
        {
            if ((m_iAttr & TypeAttributes.Interface) == TypeAttributes.Interface && (attributes & MethodAttributes.Static) != MethodAttributes.Static)
            {
                throw new InvalidOperationException(SR.InvalidOperation_ConstructorNotAllowedOnInterface);
            }

            lock (SyncRoot)
            {
                return DefineConstructorNoLock(attributes, callingConvention, parameterTypes, requiredCustomModifiers, optionalCustomModifiers);
            }
        }

        private ConstructorBuilder DefineConstructorNoLock(MethodAttributes attributes, CallingConventions callingConvention,
            Type[]? parameterTypes, Type[][]? requiredCustomModifiers, Type[][]? optionalCustomModifiers)
        {
            CheckContext(parameterTypes);
            CheckContext(requiredCustomModifiers);
            CheckContext(optionalCustomModifiers);

            ThrowIfCreated();

            string name;

            if ((attributes & MethodAttributes.Static) == 0)
            {
                name = ConstructorInfo.ConstructorName;
            }
            else
            {
                name = ConstructorInfo.TypeConstructorName;
            }

            attributes |= MethodAttributes.SpecialName;

            ConstructorBuilder constBuilder =
                new ConstructorBuilder(name, attributes, callingConvention,
                    parameterTypes, requiredCustomModifiers, optionalCustomModifiers, m_module, this);

            m_constructorCount++;

            return constBuilder;
        }

        #endregion

        #region Define Nested Type
        public TypeBuilder DefineNestedType(string name)
        {
            lock (SyncRoot)
            {
                return DefineNestedTypeNoLock(name, TypeAttributes.NestedPrivate, null, null, PackingSize.Unspecified, UnspecifiedTypeSize);
            }
        }

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, Type[]? interfaces)
        {
            lock (SyncRoot)
            {
                // Why do we only call CheckContext here? Why don't we call it in the other overloads?
                CheckContext(parent);
                CheckContext(interfaces);

                return DefineNestedTypeNoLock(name, attr, parent, interfaces, PackingSize.Unspecified, UnspecifiedTypeSize);
            }
        }

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent)
        {
            lock (SyncRoot)
            {
                return DefineNestedTypeNoLock(name, attr, parent, null, PackingSize.Unspecified, UnspecifiedTypeSize);
            }
        }

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr)
        {
            lock (SyncRoot)
            {
                return DefineNestedTypeNoLock(name, attr, null, null, PackingSize.Unspecified, UnspecifiedTypeSize);
            }
        }

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, int typeSize)
        {
            lock (SyncRoot)
            {
                return DefineNestedTypeNoLock(name, attr, parent, null, PackingSize.Unspecified, typeSize);
            }
        }

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, PackingSize packSize)
        {
            lock (SyncRoot)
            {
                return DefineNestedTypeNoLock(name, attr, parent, null, packSize, UnspecifiedTypeSize);
            }
        }

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, PackingSize packSize, int typeSize)
        {
            lock (SyncRoot)
            {
                return DefineNestedTypeNoLock(name, attr, parent, null, packSize, typeSize);
            }
        }

        private TypeBuilder DefineNestedTypeNoLock(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, Type[]? interfaces, PackingSize packSize, int typeSize)
        {
            return new TypeBuilder(name, attr, parent, interfaces, m_module, packSize, typeSize, this);
        }

        #endregion

        #region Define Field
        public FieldBuilder DefineField(string fieldName, Type type, FieldAttributes attributes)
        {
            return DefineField(fieldName, type, null, null, attributes);
        }

        public FieldBuilder DefineField(string fieldName, Type type, Type[]? requiredCustomModifiers,
            Type[]? optionalCustomModifiers, FieldAttributes attributes)
        {
            lock (SyncRoot)
            {
                return DefineFieldNoLock(fieldName, type, requiredCustomModifiers, optionalCustomModifiers, attributes);
            }
        }

        private FieldBuilder DefineFieldNoLock(string fieldName, Type type, Type[]? requiredCustomModifiers,
            Type[]? optionalCustomModifiers, FieldAttributes attributes)
        {
            ThrowIfCreated();
            CheckContext(type);
            CheckContext(requiredCustomModifiers);

            if (m_enumUnderlyingType == null && IsEnum)
            {
                if ((attributes & FieldAttributes.Static) == 0)
                {
                    // remember the underlying type for enum type
                    m_enumUnderlyingType = type;
                }
            }

            return new FieldBuilder(this, fieldName, type, requiredCustomModifiers, optionalCustomModifiers, attributes);
        }

        public FieldBuilder DefineInitializedData(string name, byte[] data, FieldAttributes attributes)
        {
            lock (SyncRoot)
            {
                return DefineInitializedDataNoLock(name, data, attributes);
            }
        }

        private FieldBuilder DefineInitializedDataNoLock(string name, byte[] data, FieldAttributes attributes)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // This method will define an initialized Data in .sdata.
            // We will create a fake TypeDef to represent the data with size. This TypeDef
            // will be the signature for the Field.

            return DefineDataHelper(name, data, data.Length, attributes);
        }

        public FieldBuilder DefineUninitializedData(string name, int size, FieldAttributes attributes)
        {
            lock (SyncRoot)
            {
                return DefineUninitializedDataNoLock(name, size, attributes);
            }
        }

        private FieldBuilder DefineUninitializedDataNoLock(string name, int size, FieldAttributes attributes)
        {
            // This method will define an uninitialized Data in .sdata.
            // We will create a fake TypeDef to represent the data with size. This TypeDef
            // will be the signature for the Field.
            return DefineDataHelper(name, null, size, attributes);
        }

        #endregion

        #region Define Properties and Events
        public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes, Type returnType, Type[]? parameterTypes)
        {
            return DefineProperty(name, attributes, returnType, null, null, parameterTypes, null, null);
        }

        public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes,
            CallingConventions callingConvention, Type returnType, Type[]? parameterTypes)
        {
            return DefineProperty(name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null);
        }

        public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes,
            Type returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
        {
            return DefineProperty(name, attributes, (CallingConventions)0, returnType,
                returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers,
                parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);
        }

        public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes, CallingConventions callingConvention,
            Type returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
        {
            lock (SyncRoot)
            {
                return DefinePropertyNoLock(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers,
                                            parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);
            }
        }

        private PropertyBuilder DefinePropertyNoLock(string name, PropertyAttributes attributes, CallingConventions callingConvention,
            Type returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (name.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyName, nameof(name));

            CheckContext(returnType);
            CheckContext(returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes);
            CheckContext(parameterTypeRequiredCustomModifiers);
            CheckContext(parameterTypeOptionalCustomModifiers);

            SignatureHelper sigHelper;
            byte[] sigBytes;

            ThrowIfCreated();

            // get the signature in SignatureHelper form
            sigHelper = SignatureHelper.GetPropertySigHelper(
                m_module, callingConvention,
                returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers,
                parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);

            // get the signature in byte form
            sigBytes = sigHelper.InternalGetSignature(out int sigLength);

            ModuleBuilder module = m_module;

            PropertyToken prToken = new PropertyToken(DefineProperty(
                new QCallModule(ref module),
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

        public EventBuilder DefineEvent(string name, EventAttributes attributes, Type eventtype)
        {
            lock (SyncRoot)
            {
                return DefineEventNoLock(name, attributes, eventtype);
            }
        }

        private EventBuilder DefineEventNoLock(string name, EventAttributes attributes, Type eventtype)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (name.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyName, nameof(name));
            if (name[0] == '\0')
                throw new ArgumentException(SR.Argument_IllegalName, nameof(name));

            int tkType;
            EventToken evToken;

            CheckContext(eventtype);

            ThrowIfCreated();

            tkType = m_module.GetTypeTokenInternal(eventtype).Token;

            // Internal helpers to define property records
            ModuleBuilder module = m_module;
            evToken = new EventToken(DefineEvent(
                new QCallModule(ref module),
                m_tdType.Token,
                name,
                attributes,
                tkType));

            // create the property builder now.
            return new EventBuilder(
                    m_module,
                    name,
                    attributes,
                    // tkType,
                    this,
                    evToken);
        }

        #endregion

        #region Create Type

        public TypeInfo? CreateTypeInfo()
        {
            lock (SyncRoot)
            {
                return CreateTypeNoLock();
            }
        }

        public Type? CreateType()
        {
            lock (SyncRoot)
            {
                return CreateTypeNoLock();
            }
        }

        internal void CheckContext(params Type[]?[]? typess)
        {
            m_module.CheckContext(typess);
        }
        internal void CheckContext(params Type?[]? types)
        {
            m_module.CheckContext(types);
        }

        private TypeInfo? CreateTypeNoLock()
        {
            if (IsCreated())
                return m_bakedRuntimeType;

            m_typeInterfaces ??= new List<Type>();

            int[] interfaceTokens = new int[m_typeInterfaces.Count];
            for (int i = 0; i < m_typeInterfaces.Count; i++)
            {
                interfaceTokens[i] = m_module.GetTypeTokenInternal(m_typeInterfaces[i]).Token;
            }

            int tkParent = 0;
            if (m_typeParent != null)
                tkParent = m_module.GetTypeTokenInternal(m_typeParent).Token;

            ModuleBuilder module = m_module;

            if (IsGenericParameter)
            {
                int[] constraints; // Array of token constrains terminated by null token

                if (m_typeParent != null)
                {
                    constraints = new int[m_typeInterfaces.Count + 2];
                    constraints[^2] = tkParent;
                }
                else
                {
                    constraints = new int[m_typeInterfaces.Count + 1];
                }

                for (int i = 0; i < m_typeInterfaces.Count; i++)
                {
                    constraints[i] = m_module.GetTypeTokenInternal(m_typeInterfaces[i]).Token;
                }

                int declMember = m_declMeth == null ? m_DeclaringType!.m_tdType.Token : m_declMeth.GetToken().Token;
                m_tdType = new TypeToken(DefineGenericParam(new QCallModule(ref module),
                    m_strName!, declMember, m_genParamAttributes, m_genParamPos, constraints));

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
                {
                    SetParentType(new QCallModule(ref module), m_tdType.Token, tkParent);
                }

                if (m_inst != null)
                {
                    foreach (GenericTypeParameterBuilder tb in m_inst)
                    {
                        tb.m_type.CreateType();
                    }
                }
            }

            if (!m_isHiddenGlobalType)
            {
                // create a public default constructor if this class has no constructor.
                // except if the type is Interface, ValueType, Enum, or a static class.
                if (m_constructorCount == 0 && ((m_iAttr & TypeAttributes.Interface) == 0) && !IsValueType && ((m_iAttr & (TypeAttributes.Abstract | TypeAttributes.Sealed)) != (TypeAttributes.Abstract | TypeAttributes.Sealed)))
                {
                    DefineDefaultConstructor(MethodAttributes.Public);
                }
            }

            int size = m_listMethods!.Count;

            for (int i = 0; i < size; i++)
            {
                MethodBuilder meth = m_listMethods[i];

                if (meth.IsGenericMethodDefinition)
                    meth.GetToken(); // Doubles as "CreateMethod" for MethodBuilder -- analogous to CreateType()

                MethodAttributes methodAttrs = meth.Attributes;

                // Any of these flags in the implemenation flags is set, we will not attach the IL method body
                if (((meth.GetMethodImplementationFlags() & (MethodImplAttributes.CodeTypeMask | MethodImplAttributes.PreserveSig | MethodImplAttributes.Unmanaged)) != MethodImplAttributes.IL) ||
                    ((methodAttrs & MethodAttributes.PinvokeImpl) != (MethodAttributes)0))
                {
                    continue;
                }

                byte[] localSig = meth.GetLocalSignature(out int sigLength);

                // Check that they haven't declared an abstract method on a non-abstract class
                if (((methodAttrs & MethodAttributes.Abstract) != 0) && ((m_iAttr & TypeAttributes.Abstract) == 0))
                {
                    throw new InvalidOperationException(SR.InvalidOperation_BadTypeAttributesNotAbstract);
                }

                byte[]? body = meth.GetBody();

                // If this is an abstract method or an interface, we don't need to set the IL.

                if ((methodAttrs & MethodAttributes.Abstract) != 0)
                {
                    // We won't check on Interface because we can have class static initializer on interface.
                    // We will just let EE or validator to catch the problem.

                    // ((m_iAttr & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface))

                    if (body != null)
                        throw new InvalidOperationException(SR.Format(SR.InvalidOperation_BadMethodBody, meth.Name));
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
                            SR.Format(SR.InvalidOperation_BadEmptyMethodBody, meth.Name));
                }

                int maxStack = meth.GetMaxStack();

                ExceptionHandler[]? exceptions = meth.GetExceptionHandlers();
                int[]? tokenFixups = meth.GetTokenFixups();

                SetMethodIL(new QCallModule(ref module), meth.GetToken().Token, meth.InitLocals,
                    body, (body != null) ? body.Length : 0,
                    localSig, sigLength, maxStack,
                    exceptions, (exceptions != null) ? exceptions.Length : 0,
                    tokenFixups, (tokenFixups != null) ? tokenFixups.Length : 0);

                if (m_module.ContainingAssemblyBuilder._assemblyData._access == AssemblyBuilderAccess.Run)
                {
                    // if we don't need the data structures to build the method any more
                    // throw them away.
                    meth.ReleaseBakedStructures();
                }
            }

            m_hasBeenCreated = true;

            // Terminate the process.
            RuntimeType? cls = null;
            TermCreateClass(new QCallModule(ref module), m_tdType.Token, ObjectHandleOnStack.Create(ref cls));

            if (!m_isHiddenGlobalType)
            {
                m_bakedRuntimeType = cls!;

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
        public int Size => m_iTypeSize;

        public PackingSize PackingSize => m_iPackingSize;

        public void SetParent([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent)
        {
            ThrowIfCreated();

            if (parent != null)
            {
                CheckContext(parent);

                if (parent.IsInterface)
                    throw new ArgumentException(SR.Argument_CannotSetParentToInterface);

                m_typeParent = parent;
            }
            else
            {
                if ((m_iAttr & TypeAttributes.Interface) != TypeAttributes.Interface)
                {
                    m_typeParent = typeof(object);
                }
                else
                {
                    if ((m_iAttr & TypeAttributes.Abstract) == 0)
                        throw new InvalidOperationException(SR.InvalidOperation_BadInterfaceNotAbstract);

                    // there is no extends for interface class
                    m_typeParent = null;
                }
            }
        }

        public void AddInterfaceImplementation([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type interfaceType)
        {
            if (interfaceType == null)
            {
                throw new ArgumentNullException(nameof(interfaceType));
            }

            CheckContext(interfaceType);

            ThrowIfCreated();

            TypeToken tkInterface = m_module.GetTypeTokenInternal(interfaceType);
            ModuleBuilder module = m_module;
            AddInterfaceImpl(new QCallModule(ref module), m_tdType.Token, tkInterface.Token);

            m_typeInterfaces!.Add(interfaceType);
        }

        public TypeToken TypeToken
        {
            get
            {
                if (IsGenericParameter)
                    ThrowIfCreated();

                return m_tdType;
            }
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            if (con == null)
                throw new ArgumentNullException(nameof(con));

            if (binaryAttribute == null)
                throw new ArgumentNullException(nameof(binaryAttribute));

            DefineCustomAttribute(m_module, m_tdType.Token, ((ModuleBuilder)m_module).GetConstructorToken(con).Token,
                binaryAttribute, false, false);
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder == null)
                throw new ArgumentNullException(nameof(customBuilder));

            customBuilder.CreateCustomAttribute((ModuleBuilder)m_module, m_tdType.Token);
        }

        #endregion

        #endregion
    }
}
