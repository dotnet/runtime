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
    public abstract partial class TypeBuilder
    {
        #region Public Static Methods
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern",
            Justification = "MakeGenericType is only called on a TypeBuilder which is not subject to trimming")]
        public static MethodInfo GetMethod(Type type, MethodInfo method)
        {
            if (type is not TypeBuilder && type is not TypeBuilderInstantiation)
                throw new ArgumentException(SR.Argument_MustBeTypeBuilder, nameof(type));

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

            if (type is not TypeBuilderInstantiation typeBuilderInstantiation)
                throw new ArgumentException(SR.Argument_NeedNonGenericType, nameof(type));

            return MethodOnTypeBuilderInstantiation.GetMethod(method, typeBuilderInstantiation);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern",
            Justification = "MakeGenericType is only called on a TypeBuilder which is not subject to trimming")]
        public static ConstructorInfo GetConstructor(Type type, ConstructorInfo constructor)
        {
            if (type is not TypeBuilder && type is not TypeBuilderInstantiation)
                throw new ArgumentException(SR.Argument_MustBeTypeBuilder, nameof(type));

            if (!constructor.DeclaringType!.IsGenericTypeDefinition)
                throw new ArgumentException(SR.Argument_ConstructorNeedGenericDeclaringType, nameof(constructor));

            if (type.GetGenericTypeDefinition() != constructor.DeclaringType)
                throw new ArgumentException(SR.Argument_InvalidConstructorDeclaringType, nameof(type));

            // TypeBuilder G<T> ==> TypeBuilderInstantiation G<T>
            if (type.IsGenericTypeDefinition)
                type = type.MakeGenericType(type.GetGenericArguments());

            if (type is not TypeBuilderInstantiation typeBuilderInstantiation)
                throw new ArgumentException(SR.Argument_NeedNonGenericType, nameof(type));

            return ConstructorOnTypeBuilderInstantiation.GetConstructor(constructor, typeBuilderInstantiation);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern",
            Justification = "MakeGenericType is only called on a TypeBuilder which is not subject to trimming")]
        public static FieldInfo GetField(Type type, FieldInfo field)
        {
            if (type is not TypeBuilder and not TypeBuilderInstantiation)
                throw new ArgumentException(SR.Argument_MustBeTypeBuilder, nameof(type));

            if (!field.DeclaringType!.IsGenericTypeDefinition)
                throw new ArgumentException(SR.Argument_FieldNeedGenericDeclaringType, nameof(field));

            if (type.GetGenericTypeDefinition() != field.DeclaringType)
                throw new ArgumentException(SR.Argument_InvalidFieldDeclaringType, nameof(type));

            // TypeBuilder G<T> ==> TypeBuilderInstantiation G<T>
            if (type.IsGenericTypeDefinition)
                type = type.MakeGenericType(type.GetGenericArguments());

            if (type is not TypeBuilderInstantiation typeBuilderInstantiation)
                throw new ArgumentException(SR.Argument_NeedNonGenericType, nameof(type));

            return FieldOnTypeBuilderInstantiation.GetField(field, typeBuilderInstantiation);
        }
        #endregion
    }

    internal sealed partial class RuntimeTypeBuilder : TypeBuilder
    {
        public override bool IsAssignableFrom([NotNullWhen(true)] TypeInfo? typeInfo)
        {
            if (typeInfo == null) return false;
            return IsAssignableFrom(typeInfo.AsType());
        }

        #region Declarations
        private sealed class CustAttr
        {
            private readonly ConstructorInfo? m_con;
            private readonly byte[]? m_binaryAttribute;
            private readonly CustomAttributeBuilder? m_customBuilder;

            public CustAttr(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
            {
                ArgumentNullException.ThrowIfNull(con);

                m_con = con;
                m_binaryAttribute = binaryAttribute.ToArray();
            }

            public CustAttr(CustomAttributeBuilder customBuilder)
            {
                ArgumentNullException.ThrowIfNull(customBuilder);

                m_customBuilder = customBuilder;
            }

            public void Bake(RuntimeModuleBuilder module, int token)
            {
                if (m_customBuilder == null)
                {
                    Debug.Assert(m_con != null);
                    DefineCustomAttribute(module, token, module.GetMethodMetadataToken(m_con),
                        m_binaryAttribute);
                }
                else
                {
                    m_customBuilder.CreateCustomAttribute(module, token);
                }
            }
        }
        #endregion

        #region Private Static FCalls
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeBuilder_SetParentType")]
        private static partial void SetParentType(QCallModule module, int tdTypeDef, int tkParent);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeBuilder_AddInterfaceImpl")]
        private static partial void AddInterfaceImpl(QCallModule module, int tdTypeDef, int tkInterface);
        #endregion

        #region Internal Static FCalls
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeBuilder_DefineMethod", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int DefineMethod(QCallModule module, int tkParent, string name, byte[] signature, int sigLength,
            MethodAttributes attributes);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeBuilder_DefineMethodSpec")]
        internal static partial int DefineMethodSpec(QCallModule module, int tkParent, byte[] signature, int sigLength);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeBuilder_DefineField", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int DefineField(QCallModule module, int tkParent, string name, byte[] signature, int sigLength,
            FieldAttributes attributes);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeBuilder_SetMethodIL")]
        private static partial void SetMethodIL(QCallModule module, int tk, [MarshalAs(UnmanagedType.Bool)] bool isInitLocals,
            byte[]? body, int bodyLength,
            byte[] LocalSig, int sigLength,
            int maxStackSize,
            ExceptionHandler[]? exceptions, int numExceptions,
            int[]? tokenFixups, int numTokenFixups);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeBuilder_DefineCustomAttribute")]
        private static partial void DefineCustomAttribute(QCallModule module, int tkAssociate, int tkConstructor,
            ReadOnlySpan<byte> attr, int attrLength);

        internal static void DefineCustomAttribute(RuntimeModuleBuilder module, int tkAssociate, int tkConstructor,
            ReadOnlySpan<byte> attr)
        {
            DefineCustomAttribute(new QCallModule(ref module), tkAssociate, tkConstructor,
                attr, attr.Length);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeBuilder_DefineProperty", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int DefineProperty(QCallModule module, int tkParent, string name, PropertyAttributes attributes,
            byte[] signature, int sigLength);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeBuilder_DefineEvent", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int DefineEvent(QCallModule module, int tkParent, string name, EventAttributes attributes, int tkEventType);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeBuilder_DefineMethodSemantics")]
        internal static partial void DefineMethodSemantics(QCallModule module, int tkAssociation,
            MethodSemanticsAttributes semantics, int tkMethod);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeBuilder_DefineMethodImpl")]
        internal static partial void DefineMethodImpl(QCallModule module, int tkType, int tkBody, int tkDecl);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeBuilder_SetMethodImpl")]
        internal static partial void SetMethodImpl(QCallModule module, int tkMethod, MethodImplAttributes MethodImplAttributes);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeBuilder_SetParamInfo", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int SetParamInfo(QCallModule module, int tkMethod, int iSequence,
            ParameterAttributes iParamAttributes, string? strParamName);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeBuilder_GetTokenFromSig")]
        internal static partial int GetTokenFromSig(QCallModule module, byte[] signature, int sigLength);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeBuilder_SetFieldLayoutOffset")]
        internal static partial void SetFieldLayoutOffset(QCallModule module, int fdToken, int iOffset);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeBuilder_SetClassLayout")]
        internal static partial void SetClassLayout(QCallModule module, int tk, PackingSize iPackingSize, int iTypeSize);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeBuilder_SetConstantValue")]
        private static unsafe partial void SetConstantValue(QCallModule module, int tk, int corType, void* pValue);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeBuilder_SetPInvokeData", StringMarshalling = StringMarshalling.Utf16)]
        private static partial void SetPInvokeData(QCallModule module, string DllName, string name, int token, int linkFlags);

        #endregion
        #region Internal\Private Static Members

        internal static bool IsTypeEqual(Type? t1, Type? t2)
        {
            // Maybe we are lucky that they are equal in the first place
            if (t1 == t2)
                return true;
            RuntimeTypeBuilder? tb1 = null;
            RuntimeTypeBuilder? tb2 = null;
            Type? runtimeType1;
            Type? runtimeType2;

            // set up the runtimeType and TypeBuilder type corresponding to t1 and t2
            if (t1 is RuntimeTypeBuilder)
            {
                tb1 = (RuntimeTypeBuilder)t1;
                // This will be null if it is not baked.
                runtimeType1 = tb1.m_bakedRuntimeType;
            }
            else
            {
                runtimeType1 = t1;
            }

            if (t2 is RuntimeTypeBuilder)
            {
                tb2 = (RuntimeTypeBuilder)t2;
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

        internal static unsafe void SetConstantValue(RuntimeModuleBuilder module, int tk, Type destType, object? value)
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
                    if (destType is RuntimeEnumBuilder enumBldr)
                    {
                        underlyingType = enumBldr.GetEnumUnderlyingType();

                        // The constant value supplied should match either the baked enum type or its underlying type
                        // we don't need to compare it with the EnumBuilder itself because you can never have an object of that type
                        if (type != enumBldr.m_typeBuilder.m_bakedRuntimeType && type != underlyingType)
                            throw new ArgumentException(SR.Argument_ConstantDoesntMatch);
                    }
                    else if (destType is RuntimeTypeBuilder typeBldr)
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
        private int m_tdType;
        private readonly RuntimeModuleBuilder m_module;
        private readonly string? m_strName;
        private readonly string? m_strNameSpace;
        private string? m_strFullQualName;

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        private Type? m_typeParent;

        private List<Type>? m_typeInterfaces;
        private readonly TypeAttributes m_iAttr;
        private GenericParameterAttributes m_genParamAttributes;
        internal List<RuntimeMethodBuilder>? m_listMethods;
        internal int m_lastTokenizedMethod;
        private int m_constructorCount;
        private readonly int m_iTypeSize;
        private readonly PackingSize m_iPackingSize;
        private readonly RuntimeTypeBuilder? m_DeclaringType;

        // We cannot store this on EnumBuilder because users can define enum types manually using TypeBuilder.
        private Type? m_enumUnderlyingType;
        internal bool m_isHiddenGlobalType;
        private bool m_hasBeenCreated;

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        private RuntimeType m_bakedRuntimeType = null!;

        private readonly int m_genParamPos;
        private RuntimeGenericTypeParameterBuilder[]? m_inst;
        private readonly bool m_bIsGenParam;
        private readonly RuntimeMethodBuilder? m_declMeth;
        private readonly RuntimeTypeBuilder? m_genTypeDef;
        #endregion

        #region Constructor
        // ctor for the global (module) type
        internal RuntimeTypeBuilder(RuntimeModuleBuilder module)
        {
            m_tdType = ((int)MetadataTokenType.TypeDef);
            m_isHiddenGlobalType = true;
            m_module = module;
            m_listMethods = new List<RuntimeMethodBuilder>();
            // No token has been created so let's initialize it to -1
            // The first time we call MethodBuilder.GetToken this will incremented.
            m_lastTokenizedMethod = -1;
        }

        // ctor for generic method parameter
        internal RuntimeTypeBuilder(string szName, int genParamPos, RuntimeMethodBuilder declMeth)
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
        private RuntimeTypeBuilder(string szName, int genParamPos, RuntimeTypeBuilder declType)
        {
            m_strName = szName;
            m_genParamPos = genParamPos;
            m_bIsGenParam = true;
            m_typeInterfaces = new List<Type>();

            Debug.Assert(declType != null);
            m_DeclaringType = declType;
            m_module = declType.GetModuleBuilder();
        }

        internal RuntimeTypeBuilder(
            string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, Type[]? interfaces, RuntimeModuleBuilder module,
            PackingSize iPackingSize, int iTypeSize, RuntimeTypeBuilder? enclosingType)
        {
            if (name[0] == '\0')
                throw new ArgumentException(SR.Argument_IllegalName, nameof(name));

            if (name.Length > 1023)
                throw new ArgumentException(SR.Argument_TypeNameTooLong, nameof(name));

            int i;
            m_module = module;
            m_DeclaringType = enclosingType;
            RuntimeAssemblyBuilder containingAssem = m_module.ContainingAssemblyBuilder;

            // cannot have two types within the same assembly of the same name
            containingAssem.CheckTypeNameConflict(name, enclosingType);

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
                interfaceTokens = new int[interfaces.Length + 1];
                for (i = 0; i < interfaces.Length; i++)
                {
                    // cannot contain null in the interface list
                    ArgumentNullException.ThrowIfNull(interfaces[i], nameof(interfaces));
                    interfaceTokens[i] = m_module.GetTypeTokenInternal(interfaces[i]);
                }
            }

            int iLast = name.LastIndexOf('.');
            if (iLast <= 0)
            {
                // no name space
                m_strNameSpace = string.Empty;
                m_strName = name;
            }
            else
            {
                // split the name space
                m_strNameSpace = name.Substring(0, iLast);
                m_strName = name.Substring(iLast + 1);
            }

            VerifyTypeAttributes(attr);

            m_iAttr = attr;

            SetParent(parent);

            m_listMethods = new List<RuntimeMethodBuilder>();
            m_lastTokenizedMethod = -1;

            SetInterfaces(interfaces);

            int tkParent = 0;
            if (m_typeParent != null)
                tkParent = m_module.GetTypeTokenInternal(m_typeParent);

            int tkEnclosingType = 0;
            if (enclosingType != null)
            {
                tkEnclosingType = enclosingType.m_tdType;
            }

            m_tdType = DefineType(new QCallModule(ref module),
                name, tkParent, m_iAttr, tkEnclosingType, interfaceTokens!);

            m_iPackingSize = iPackingSize;
            m_iTypeSize = iTypeSize;
            if ((m_iPackingSize != 0) || (m_iTypeSize != 0))
                SetClassLayout(new QCallModule(ref module), m_tdType, m_iPackingSize, m_iTypeSize);

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

            ArgumentException.ThrowIfNullOrEmpty(name);

            if (size <= 0 || size >= 0x003f0000)
                throw new ArgumentException(SR.Argument_BadSizeForData);

            ThrowIfCreated();

            // form the value class name
            strValueClassName = $"$ArrayType${size}";

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
            ((RuntimeFieldBuilder)fdBuilder).SetData(data, size);
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

        protected override bool IsCreatedCore()
        {
            return m_hasBeenCreated;
        }
        #endregion

        #region FCalls
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeBuilder_DefineType", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int DefineType(QCallModule module,
            string fullname, int tkParent, TypeAttributes attributes, int tkEnclosingType, int[] interfaceTokens);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeBuilder_DefineGenericParam", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int DefineGenericParam(QCallModule module,
            string name, int tkParent, GenericParameterAttributes attributes, int position, int[] constraints);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TypeBuilder_TermCreateClass")]
        private static partial void TermCreateClass(QCallModule module, int tk, ObjectHandleOnStack type);
        #endregion

        #region Internal Methods
        internal void ThrowIfCreated()
        {
            if (IsCreated())
                throw new InvalidOperationException(SR.InvalidOperation_TypeHasBeenCreated);
        }

        internal object SyncRoot => m_module.SyncRoot;

        internal RuntimeModuleBuilder GetModuleBuilder()
        {
            return m_module;
        }

        internal RuntimeType BakedRuntimeType => m_bakedRuntimeType;

        internal void SetGenParamAttributes(GenericParameterAttributes genericParameterAttributes)
        {
            m_genParamAttributes = genericParameterAttributes;
        }

        internal void SetGenParamCustomAttribute(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
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
            m_ca ??= new List<CustAttr>();
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

        public override int MetadataToken => m_tdType;

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

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type? GetInterface(string name, bool ignoreCase)
        {
            if (!IsCreated())
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

            return m_bakedRuntimeType.GetInterface(name, ignoreCase);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type[] GetInterfaces()
        {
            if (m_bakedRuntimeType != null)
            {
                return m_bakedRuntimeType.GetInterfaces();
            }

            if (m_typeInterfaces == null)
            {
                return Type.EmptyTypes;
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

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

            return m_bakedRuntimeType.GetMember(name, type, bindingAttr);
        }

        public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType)
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

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            if (!IsCreated())
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

            return m_bakedRuntimeType.GetMembers(bindingAttr);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "The GetInterfaces technically requires all interfaces to be preserved" +
                "But in this case it acts only on TypeBuilder which is never trimmed (as it's runtime created).")]
        public override bool IsAssignableFrom([NotNullWhen(true)] Type? c)
        {
            if (IsTypeEqual(c, this))
                return true;

            Type? fromRuntimeType;
            RuntimeTypeBuilder? fromTypeBuilder = c as RuntimeTypeBuilder;

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

            ArgumentNullException.ThrowIfNull(attributeType);

            if (attributeType.UnderlyingSystemType is not RuntimeType attributeRuntimeType)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.GetCustomAttributes(m_bakedRuntimeType, attributeRuntimeType, inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (!IsCreated())
                throw new NotSupportedException(SR.NotSupported_TypeNotYetCreated);

            ArgumentNullException.ThrowIfNull(attributeType);

            if (attributeType.UnderlyingSystemType is not RuntimeType attributeRuntimeType)
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

        protected override GenericTypeParameterBuilder[] DefineGenericParametersCore(params string[] names)
        {
            if (m_inst != null)
            {
                throw new InvalidOperationException();
            }

            m_inst = new RuntimeGenericTypeParameterBuilder[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                ArgumentNullException.ThrowIfNull(name, nameof(names));
                m_inst[i] = new RuntimeGenericTypeParameterBuilder(new RuntimeTypeBuilder(name, i, this));
            }

            return m_inst;
        }

        public override Type[] GetGenericArguments() => m_inst ?? Type.EmptyTypes;

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
        protected override void DefineMethodOverrideCore(MethodInfo methodInfoBody, MethodInfo methodInfoDeclaration)
        {
            lock (SyncRoot)
            {
                ThrowIfCreated();

                if (!ReferenceEquals(methodInfoBody.DeclaringType, this))
                    // Loader restriction: body method has to be from this class
                    throw new ArgumentException(SR.ArgumentException_BadMethodImplBody);

                int tkBody = m_module.GetMethodMetadataToken(methodInfoBody);
                int tkDecl = m_module.GetMethodMetadataToken(methodInfoDeclaration);

                RuntimeModuleBuilder module = m_module;
                DefineMethodImpl(new QCallModule(ref module), m_tdType, tkBody, tkDecl);
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2082:UnrecognizedReflectionPattern",
            Justification = "Reflection.Emit is not subject to trimming")]
        protected override MethodBuilder DefineMethodCore(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
        {
            lock (SyncRoot)
            {
                ThrowIfCreated();

                // pass in Method attributes
                RuntimeMethodBuilder method = new RuntimeMethodBuilder(
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
        }

        [RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        protected override MethodBuilder DefinePInvokeMethodCore(string name, string dllName, string entryName, MethodAttributes attributes,
            CallingConventions callingConvention,
            Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers,
            CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            lock (SyncRoot)
            {
                if ((attributes & MethodAttributes.Abstract) != 0)
                    throw new ArgumentException(SR.Argument_BadPInvokeMethod);

                if ((m_iAttr & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface)
                    throw new ArgumentException(SR.Argument_BadPInvokeOnInterface);

                ThrowIfCreated();

                attributes |= MethodAttributes.PinvokeImpl;
                RuntimeMethodBuilder method = new RuntimeMethodBuilder(name, attributes, callingConvention,
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

                int token = method.MetadataToken;

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

                RuntimeModuleBuilder module = m_module;
                SetPInvokeData(new QCallModule(ref module),
                    dllName,
                    entryName,
                    token,
                    linkFlags);

                method.SetToken(token);

                return method;
            }
        }
        #endregion

        #region Define Constructor
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2082:UnrecognizedReflectionPattern",
            Justification = "Reflection.Emit is not subject to trimming")]
        protected override ConstructorBuilder DefineTypeInitializerCore()
        {
            lock (SyncRoot)
            {
                ThrowIfCreated();

                // change the attributes and the class constructor's name
                const MethodAttributes attr = MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName;

                ConstructorBuilder constBuilder = new RuntimeConstructorBuilder(
                    ConstructorInfo.TypeConstructorName, attr, CallingConventions.Standard, null, m_module, this);

                return constBuilder;
            }
        }

        protected override ConstructorBuilder DefineDefaultConstructorCore(MethodAttributes attributes)
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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern",
            Justification = "MakeGenericType is only called on a TypeBuilderInstantiation which is not subject to trimming")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "GetConstructor is only called on a TypeBuilderInstantiation which is not subject to trimming")]
        private RuntimeConstructorBuilder DefineDefaultConstructorNoLock(MethodAttributes attributes)
        {
            RuntimeConstructorBuilder constBuilder;

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

                if (genericTypeDefinition is RuntimeTypeBuilder rtBuilder)
                    genericTypeDefinition = rtBuilder.m_bakedRuntimeType;

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

            con ??= m_typeParent!.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, EmptyTypes, null);

            if (con == null)
                throw new NotSupportedException(SR.NotSupported_NoParentDefaultConstructor);

            // Define the constructor Builder
            constBuilder = (RuntimeConstructorBuilder)DefineConstructor(attributes, CallingConventions.Standard, null);
            m_constructorCount++;

            // generate the code to call the parent's default constructor
            ILGenerator il = constBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, con);
            il.Emit(OpCodes.Ret);

            constBuilder.m_isDefaultConstructor = true;
            return constBuilder;
        }

        protected override ConstructorBuilder DefineConstructorCore(MethodAttributes attributes, CallingConventions callingConvention,
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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2082:UnrecognizedReflectionPattern",
            Justification = "Reflection.Emit is not subject to trimming")]
        private ConstructorBuilder DefineConstructorNoLock(MethodAttributes attributes, CallingConventions callingConvention,
            Type[]? parameterTypes, Type[][]? requiredCustomModifiers, Type[][]? optionalCustomModifiers)
        {
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
                new RuntimeConstructorBuilder(name, attributes, callingConvention,
                    parameterTypes, requiredCustomModifiers, optionalCustomModifiers, m_module, this);

            m_constructorCount++;

            return constBuilder;
        }

        #endregion

        #region Define Nested Type
        protected override TypeBuilder DefineNestedTypeCore(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, Type[]? interfaces, PackingSize packSize, int typeSize)
        {
            lock (SyncRoot)
            {
                return new RuntimeTypeBuilder(name, attr, parent, interfaces, m_module, packSize, typeSize, this);
            }
        }

        #endregion

        #region Define Field
        protected override FieldBuilder DefineFieldCore(string fieldName, Type type, Type[]? requiredCustomModifiers,
            Type[]? optionalCustomModifiers, FieldAttributes attributes)
        {
            lock (SyncRoot)
            {
                ThrowIfCreated();

                if (m_enumUnderlyingType == null && IsEnum)
                {
                    if ((attributes & FieldAttributes.Static) == 0)
                    {
                        // remember the underlying type for enum type
                        m_enumUnderlyingType = type;
                    }
                }

                return new RuntimeFieldBuilder(this, fieldName, type, requiredCustomModifiers, optionalCustomModifiers, attributes);
            }
        }

        protected override FieldBuilder DefineInitializedDataCore(string name, byte[] data, FieldAttributes attributes)
        {
            lock (SyncRoot)
            {
                // This method will define an initialized Data in .sdata.
                // We will create a fake TypeDef to represent the data with size. This TypeDef
                // will be the signature for the Field.

                return DefineDataHelper(name, data, data.Length, attributes);
            }
        }

        protected override FieldBuilder DefineUninitializedDataCore(string name, int size, FieldAttributes attributes)
        {
            lock (SyncRoot)
            {
                // This method will define an uninitialized Data in .sdata.
                // We will create a fake TypeDef to represent the data with size. This TypeDef
                // will be the signature for the Field.
                return DefineDataHelper(name, null, size, attributes);
            }
        }

        #endregion

        #region Define Properties and Events

        protected override PropertyBuilder DefinePropertyCore(string name, PropertyAttributes attributes, CallingConventions callingConvention,
            Type returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
        {
            lock (SyncRoot)
            {
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

                RuntimeModuleBuilder module = m_module;

                int prToken = DefineProperty(
                    new QCallModule(ref module),
                    m_tdType,
                    name,
                    attributes,
                    sigBytes,
                    sigLength);

                // create the property builder now.
                return new RuntimePropertyBuilder(
                        m_module,
                        name,
                        attributes,
                        returnType,
                        prToken,
                        this);
            }
        }

        protected override EventBuilder DefineEventCore(string name, EventAttributes attributes, Type eventtype)
        {
            if (name[0] == '\0')
            {
                throw new ArgumentException(SR.Argument_IllegalName, nameof(name));
            }

            lock (SyncRoot)
            {
                int tkType;
                int evToken;

                ThrowIfCreated();

                tkType = m_module.GetTypeTokenInternal(eventtype);

                // Internal helpers to define property records
                RuntimeModuleBuilder module = m_module;
                evToken = DefineEvent(
                    new QCallModule(ref module),
                    m_tdType,
                    name,
                    attributes,
                    tkType);

                // create the property builder now.
                return new RuntimeEventBuilder(
                        m_module,
                        name,
                        attributes,
                        this,
                        evToken);
            }
        }

        #endregion

        #region Create Type

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        protected override TypeInfo CreateTypeInfoCore()
        {
            TypeInfo? typeInfo = CreateTypeInfoImpl();
            Debug.Assert(m_isHiddenGlobalType || typeInfo != null);
            return typeInfo!;
        }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        internal TypeInfo? CreateTypeInfoImpl()
        {
            lock (SyncRoot)
            {
                return CreateTypeNoLock();
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2083:UnrecognizedReflectionPattern",
            Justification = "Reflection.Emit is not subject to trimming")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2068:UnrecognizedReflectionPattern",
            Justification = "Reflection.Emit is not subject to trimming")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2069:UnrecognizedReflectionPattern",
            Justification = "Reflection.Emit is not subject to trimming")]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        private TypeInfo? CreateTypeNoLock()
        {
            if (IsCreated())
                return m_bakedRuntimeType;

            m_typeInterfaces ??= new List<Type>();

            int[] interfaceTokens = new int[m_typeInterfaces.Count];
            for (int i = 0; i < m_typeInterfaces.Count; i++)
            {
                interfaceTokens[i] = m_module.GetTypeTokenInternal(m_typeInterfaces[i]);
            }

            int tkParent = 0;
            if (m_typeParent != null)
            {
                tkParent = m_module.GetTypeTokenInternal(m_typeParent);
            }

            RuntimeModuleBuilder module = m_module;

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
                    constraints[i] = m_module.GetTypeTokenInternal(m_typeInterfaces[i]);
                }

                int declMember = m_declMeth == null ? m_DeclaringType!.m_tdType : m_declMeth.MetadataToken;
                m_tdType = DefineGenericParam(new QCallModule(ref module),
                    m_strName!, declMember, m_genParamAttributes, m_genParamPos, constraints);

                if (m_ca != null)
                {
                    foreach (CustAttr ca in m_ca)
                        ca.Bake(m_module, MetadataToken);
                }

                m_hasBeenCreated = true;

                // Baking a generic parameter does not put sufficient information into the metadata to actually be able to load it as a type,
                // the associated generic type/method needs to be baked first. So we return this rather than the baked type.
                return this;
            }
            else
            {
                // Check for global typebuilder
                if (((m_tdType & 0x00FFFFFF) != 0) && ((tkParent & 0x00FFFFFF) != 0))
                {
                    SetParentType(new QCallModule(ref module), m_tdType, tkParent);
                }

                if (m_inst != null)
                {
                    foreach (RuntimeGenericTypeParameterBuilder tb in m_inst)
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
                RuntimeMethodBuilder meth = m_listMethods[i];

                if (meth.IsGenericMethodDefinition)
                {
                    _ = meth.MetadataToken; // Doubles as "CreateMethod" for MethodBuilder -- analogous to CreateType()
                }

                MethodAttributes methodAttrs = meth.Attributes;

                // Any of these flags in the implementation flags is set, we will not attach the IL method body
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

                SetMethodIL(new QCallModule(ref module), meth.MetadataToken, meth.InitLocals,
                    body, (body != null) ? body.Length : 0,
                    localSig, sigLength, maxStack,
                    exceptions, (exceptions != null) ? exceptions.Length : 0,
                    tokenFixups, (tokenFixups != null) ? tokenFixups.Length : 0);

                if (m_module.ContainingAssemblyBuilder._access == AssemblyBuilderAccess.Run)
                {
                    // if we don't need the data structures to build the method any more
                    // throw them away.
                    meth.ReleaseBakedStructures();
                }
            }

            m_hasBeenCreated = true;

            // Terminate the process.
            RuntimeType? cls = null;
            TermCreateClass(new QCallModule(ref module), m_tdType, ObjectHandleOnStack.Create(ref cls));

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
        protected override int SizeCore => m_iTypeSize;

        protected override PackingSize PackingSizeCore => m_iPackingSize;

        protected override void SetParentCore([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent)
        {
            ThrowIfCreated();

            if (parent != null)
            {
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

        protected override void AddInterfaceImplementationCore([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type interfaceType)
        {
            ThrowIfCreated();

            int tkInterface = m_module.GetTypeTokenInternal(interfaceType);
            RuntimeModuleBuilder module = m_module;
            AddInterfaceImpl(new QCallModule(ref module), m_tdType, tkInterface);

            m_typeInterfaces!.Add(interfaceType);
        }

        internal int TypeToken
        {
            get
            {
                if (IsGenericParameter)
                    ThrowIfCreated();

                return m_tdType;
            }
        }

        internal void SetCustomAttribute(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
        {
            SetCustomAttributeCore(con, binaryAttribute);
        }

        protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
        {
            DefineCustomAttribute(m_module, m_tdType, m_module.GetMethodMetadataToken(con), binaryAttribute);
        }

        #endregion

        #endregion
    }
}
