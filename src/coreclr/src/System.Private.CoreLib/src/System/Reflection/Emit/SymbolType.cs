// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using CultureInfo = System.Globalization.CultureInfo;

namespace System.Reflection.Emit
{
    internal enum TypeKind
    {
        IsArray = 1,
        IsPointer = 2,
        IsByRef = 3,
    }

    // This is a kind of Type object that will represent the compound expression of a parameter type or field type.
    internal sealed class SymbolType : TypeInfo
    {
        public override bool IsAssignableFrom([NotNullWhen(true)] TypeInfo? typeInfo)
        {
            if (typeInfo == null) return false;
            return IsAssignableFrom(typeInfo.AsType());
        }

        #region Static Members
        internal static Type? FormCompoundType(string? format, Type baseType, int curIndex)
        {
            // This function takes a string to describe the compound type, such as "[,][]", and a baseType.
            //
            // Example: [2..4]  - one dimension array with lower bound 2 and size of 3
            // Example: [3, 5, 6] - three dimension array with lower bound 3, 5, 6
            // Example: [-3, ] [] - one dimensional array of two dimensional array (with lower bound -3 for
            //          the first dimension)
            // Example: []* - pointer to a one dimensional array
            // Example: *[] - one dimensional array. The element type is a pointer to the baseType
            // Example: []& - ByRef of a single dimensional array. Only one & is allowed and it must appear the last!
            // Example: [?] - Array with unknown bound

            SymbolType symbolType;
            int iLowerBound;
            int iUpperBound;

            if (format == null || curIndex == format.Length)
            {
                // we have consumed all of the format string
                return baseType;
            }




            if (format[curIndex] == '&')
            {
                // ByRef case

                symbolType = new SymbolType(TypeKind.IsByRef);
                symbolType.SetFormat(format, curIndex, 1);
                curIndex++;

                if (curIndex != format.Length)
                    // ByRef has to be the last char!!
                    throw new ArgumentException(SR.Argument_BadSigFormat);

                symbolType.SetElementType(baseType);
                return symbolType;
            }

            if (format[curIndex] == '[')
            {
                // Array type.
                symbolType = new SymbolType(TypeKind.IsArray);
                int startIndex = curIndex;
                curIndex++;

                iLowerBound = 0;
                iUpperBound = -1;

                // Example: [2..4]  - one dimension array with lower bound 2 and size of 3
                // Example: [3, 5, 6] - three dimension array with lower bound 3, 5, 6
                // Example: [-3, ] [] - one dimensional array of two dimensional array (with lower bound -3 sepcified)

                while (format[curIndex] != ']')
                {
                    if (format[curIndex] == '*')
                    {
                        symbolType.m_isSzArray = false;
                        curIndex++;
                    }
                    // consume, one dimension at a time
                    if ((format[curIndex] >= '0' && format[curIndex] <= '9') || format[curIndex] == '-')
                    {
                        bool isNegative = false;
                        if (format[curIndex] == '-')
                        {
                            isNegative = true;
                            curIndex++;
                        }

                        // lower bound is specified. Consume the low bound
                        while (format[curIndex] >= '0' && format[curIndex] <= '9')
                        {
                            iLowerBound *= 10;
                            iLowerBound += format[curIndex] - '0';
                            curIndex++;
                        }

                        if (isNegative)
                        {
                            iLowerBound = 0 - iLowerBound;
                        }

                        // set the upper bound to be less than LowerBound to indicate that upper bound it not specified yet!
                        iUpperBound = iLowerBound - 1;
                    }
                    if (format[curIndex] == '.')
                    {
                        // upper bound is specified

                        // skip over ".."
                        curIndex++;
                        if (format[curIndex] != '.')
                        {
                            // bad format!! Throw exception
                            throw new ArgumentException(SR.Argument_BadSigFormat);
                        }

                        curIndex++;
                        // consume the upper bound
                        if ((format[curIndex] >= '0' && format[curIndex] <= '9') || format[curIndex] == '-')
                        {
                            bool isNegative = false;
                            iUpperBound = 0;
                            if (format[curIndex] == '-')
                            {
                                isNegative = true;
                                curIndex++;
                            }

                            // lower bound is specified. Consume the low bound
                            while (format[curIndex] >= '0' && format[curIndex] <= '9')
                            {
                                iUpperBound *= 10;
                                iUpperBound += format[curIndex] - '0';
                                curIndex++;
                            }
                            if (isNegative)
                            {
                                iUpperBound = 0 - iUpperBound;
                            }
                            if (iUpperBound < iLowerBound)
                            {
                                // User specified upper bound less than lower bound, this is an error.
                                // Throw error exception.
                                throw new ArgumentException(SR.Argument_BadSigFormat);
                            }
                        }
                    }

                    if (format[curIndex] == ',')
                    {
                        // We have more dimension to deal with.
                        // now set the lower bound, the size, and increase the dimension count!
                        curIndex++;
                        symbolType.SetBounds(iLowerBound, iUpperBound);

                        // clear the lower and upper bound information for next dimension
                        iLowerBound = 0;
                        iUpperBound = -1;
                    }
                    else if (format[curIndex] != ']')
                    {
                        throw new ArgumentException(SR.Argument_BadSigFormat);
                    }
                }

                // The last dimension information
                symbolType.SetBounds(iLowerBound, iUpperBound);

                // skip over ']'
                curIndex++;

                symbolType.SetFormat(format, startIndex, curIndex - startIndex);

                // set the base type of array
                symbolType.SetElementType(baseType);
                return FormCompoundType(format, symbolType, curIndex);
            }
            else if (format[curIndex] == '*')
            {
                // pointer type.

                symbolType = new SymbolType(TypeKind.IsPointer);
                symbolType.SetFormat(format, curIndex, 1);
                curIndex++;
                symbolType.SetElementType(baseType);
                return FormCompoundType(format, symbolType, curIndex);
            }

            return null;
        }

        #endregion

        #region Data Members
        internal TypeKind m_typeKind;
        internal Type m_baseType = null!;
        internal int m_cRank;        // count of dimension
        // If LowerBound and UpperBound is equal, that means one element.
        // If UpperBound is less than LowerBound, then the size is not specified.
        internal int[] m_iaLowerBound;
        internal int[] m_iaUpperBound; // count of dimension
        private string? m_format;      // format string to form the full name.
        private bool m_isSzArray = true;
        #endregion

        #region Constructor
        internal SymbolType(TypeKind typeKind)
        {
            m_typeKind = typeKind;
            m_iaLowerBound = new int[4];
            m_iaUpperBound = new int[4];
        }

        #endregion

        #region Internal Members
        internal void SetElementType(Type baseType)
        {
            if (baseType is null)
                throw new ArgumentNullException(nameof(baseType));

            m_baseType = baseType;
        }

        private void SetBounds(int lower, int upper)
        {
            // Increase the rank, set lower and upper bound

            if (lower != 0 || upper != -1)
                m_isSzArray = false;

            if (m_iaLowerBound.Length <= m_cRank)
            {
                // resize the bound array
                int[] iaTemp = new int[m_cRank * 2];
                Array.Copy(m_iaLowerBound, iaTemp, m_cRank);
                m_iaLowerBound = iaTemp;
                Array.Copy(m_iaUpperBound, iaTemp, m_cRank);
                m_iaUpperBound = iaTemp;
            }

            m_iaLowerBound[m_cRank] = lower;
            m_iaUpperBound[m_cRank] = upper;
            m_cRank++;
        }

        internal void SetFormat(string format, int curIndex, int length)
        {
            // Cache the text display format for this SymbolType

            m_format = format.Substring(curIndex, length);
        }
        #endregion

        #region Type Overrides

        public override bool IsTypeDefinition => false;

        public override bool IsSZArray => m_cRank <= 1 && m_isSzArray;

        public override Type MakePointerType()
        {
            return SymbolType.FormCompoundType(m_format + "*", m_baseType, 0)!;
        }

        public override Type MakeByRefType()
        {
            return SymbolType.FormCompoundType(m_format + "&", m_baseType, 0)!;
        }

        public override Type MakeArrayType()
        {
            return SymbolType.FormCompoundType(m_format + "[]", m_baseType, 0)!;
        }

        public override Type MakeArrayType(int rank)
        {
            string s = GetRankString(rank);
            SymbolType? st = SymbolType.FormCompoundType(m_format + s, m_baseType, 0) as SymbolType;
            return st!;
        }

        public override int GetArrayRank()
        {
            if (!IsArray)
                throw new NotSupportedException(SR.NotSupported_SubclassOverride);

            return m_cRank;
        }

        public override Guid GUID => throw new NotSupportedException(SR.NotSupported_NonReflectedType);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target,
            object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters)
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }

        public override Module Module
        {
            get
            {
                Type baseType;

                for (baseType = m_baseType; baseType is SymbolType; baseType = ((SymbolType)baseType).m_baseType) ;

                return baseType.Module;
            }
        }
        public override Assembly Assembly
        {
            get
            {
                Type baseType;

                for (baseType = m_baseType; baseType is SymbolType; baseType = ((SymbolType)baseType).m_baseType) ;

                return baseType.Assembly;
            }
        }

        public override RuntimeTypeHandle TypeHandle => throw new NotSupportedException(SR.NotSupported_NonReflectedType);

        public override string Name
        {
            get
            {
                Type baseType;
                string? sFormat = m_format;

                for (baseType = m_baseType; baseType is SymbolType; baseType = ((SymbolType)baseType).m_baseType)
                    sFormat = ((SymbolType)baseType).m_format + sFormat;

                return baseType.Name + sFormat;
            }
        }

        public override string? FullName => TypeNameBuilder.ToString(this, TypeNameBuilder.Format.FullName);

        public override string? AssemblyQualifiedName => TypeNameBuilder.ToString(this, TypeNameBuilder.Format.AssemblyQualifiedName);

        public override string ToString()
        {
            return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.ToString)!;
        }

        public override string? Namespace => m_baseType.Namespace;

        public override Type BaseType => typeof(System.Array);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder? binder,
                CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers)
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder,
                CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }

        public override Type GetInterface(string name, bool ignoreCase)
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }

        public override Type[] GetInterfaces()
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
        public override EventInfo[] GetEvents()
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder,
                Type? returnType, Type[]? types, ParameterModifier[]? modifiers)
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type GetNestedType(string name, BindingFlags bindingAttr)
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }

        public override InterfaceMapping GetInterfaceMap(Type interfaceType)
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            // Return the attribute flags of the base type?
            Type baseType;
            for (baseType = m_baseType; baseType is SymbolType; baseType = ((SymbolType)baseType).m_baseType) ;
            return baseType.Attributes;
        }

        protected override bool IsArrayImpl()
        {
            return m_typeKind == TypeKind.IsArray;
        }

        protected override bool IsPointerImpl()
        {
            return m_typeKind == TypeKind.IsPointer;
        }

        protected override bool IsByRefImpl()
        {
            return m_typeKind == TypeKind.IsByRef;
        }

        protected override bool IsPrimitiveImpl()
        {
            return false;
        }

        protected override bool IsValueTypeImpl()
        {
            return false;
        }

        protected override bool IsCOMObjectImpl()
        {
            return false;
        }

        public override bool IsConstructedGenericType => false;

        public override Type? GetElementType()
        {
            return m_baseType;
        }

        protected override bool HasElementTypeImpl()
        {
            return m_baseType != null;
        }

        public override Type UnderlyingSystemType => this;

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }
        #endregion
    }
}
