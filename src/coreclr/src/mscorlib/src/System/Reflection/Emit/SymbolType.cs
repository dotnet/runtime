// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// 

namespace System.Reflection.Emit 
{
    using System.Runtime.InteropServices;
    using System;
    using System.Reflection;
    using System.Diagnostics.Contracts;
    using CultureInfo = System.Globalization.CultureInfo;

    [Serializable]
    internal enum TypeKind
    {
        IsArray   = 1,
        IsPointer = 2,
        IsByRef   = 3,
    }

    // This is a kind of Type object that will represent the compound expression of a parameter type or field type.
    internal sealed class SymbolType : TypeInfo
    {
        public override bool IsAssignableFrom(System.Reflection.TypeInfo typeInfo){
            if(typeInfo==null) return false;            
            return IsAssignableFrom(typeInfo.AsType());
        }

        #region Static Members
        internal static Type FormCompoundType(char[] bFormat, Type baseType, int curIndex)
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

            if (bFormat == null || curIndex == bFormat.Length)
            {
                // we have consumed all of the format string
                return baseType;
            }

              
             

            if (bFormat[curIndex] == '&')
            {
                // ByRef case

                symbolType = new SymbolType(TypeKind.IsByRef);
                symbolType.SetFormat(bFormat, curIndex, 1);
                curIndex++;
                
                if (curIndex != bFormat.Length)
                    // ByRef has to be the last char!!
                    throw new ArgumentException(Environment.GetResourceString("Argument_BadSigFormat"));

                symbolType.SetElementType(baseType);
                return symbolType;
            }

            if (bFormat[curIndex] == '[')
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
                
                while (bFormat[curIndex] != ']')
                {
                    if (bFormat[curIndex] == '*')
                    {
                        symbolType.m_isSzArray = false;
                        curIndex++;                        
                    }
                    // consume, one dimension at a time
                    if ((bFormat[curIndex] >= '0' && bFormat[curIndex] <= '9') || bFormat[curIndex] == '-')
                    {
                        bool isNegative = false;
                        if (bFormat[curIndex] == '-')
                        {
                            isNegative = true;
                            curIndex++;
                        }

                        // lower bound is specified. Consume the low bound
                        while (bFormat[curIndex] >= '0' && bFormat[curIndex] <= '9')
                        {
                            iLowerBound = iLowerBound * 10;
                            iLowerBound += bFormat[curIndex] - '0';
                            curIndex++;
                        }

                        if (isNegative)
                        {
                            iLowerBound = 0 - iLowerBound;
                        }

                        // set the upper bound to be less than LowerBound to indicate that upper bound it not specified yet!
                        iUpperBound = iLowerBound - 1;

                    }
                    if (bFormat[curIndex] == '.')
                    {                       
                        // upper bound is specified

                        // skip over ".."
                        curIndex++;
                        if (bFormat[curIndex] != '.')
                        {
                            // bad format!! Throw exception
                            throw new ArgumentException(Environment.GetResourceString("Argument_BadSigFormat"));
                        }

                        curIndex++;
                        // consume the upper bound
                        if ((bFormat[curIndex] >= '0' && bFormat[curIndex] <= '9') || bFormat[curIndex] == '-')
                        {
                            bool isNegative = false;
                            iUpperBound = 0;
                            if (bFormat[curIndex] == '-')
                            {
                                isNegative = true;
                                curIndex++;
                            }

                            // lower bound is specified. Consume the low bound
                            while (bFormat[curIndex] >= '0' && bFormat[curIndex] <= '9')
                            {
                                iUpperBound = iUpperBound * 10;
                                iUpperBound += bFormat[curIndex] - '0';
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
                                throw new ArgumentException(Environment.GetResourceString("Argument_BadSigFormat"));
                            }
                        }
                    }

                    if (bFormat[curIndex] == ',')
                    {
                        // We have more dimension to deal with.
                        // now set the lower bound, the size, and increase the dimension count!
                        curIndex++;
                        symbolType.SetBounds(iLowerBound, iUpperBound);

                        // clear the lower and upper bound information for next dimension
                        iLowerBound = 0;
                        iUpperBound = -1;
                    }
                    else if (bFormat[curIndex] != ']')
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_BadSigFormat"));
                    }
                }
                
                // The last dimension information
                symbolType.SetBounds(iLowerBound, iUpperBound);

                // skip over ']'
                curIndex++;

                symbolType.SetFormat(bFormat, startIndex, curIndex - startIndex);

                // set the base type of array
                symbolType.SetElementType(baseType);
                return FormCompoundType(bFormat, symbolType, curIndex);
            }
            else if (bFormat[curIndex] == '*')
            {
                // pointer type.

                symbolType = new SymbolType(TypeKind.IsPointer);
                symbolType.SetFormat(bFormat, curIndex, 1);
                curIndex++;
                symbolType.SetElementType(baseType);
                return FormCompoundType(bFormat, symbolType, curIndex);
            }

            return null;
        }

        #endregion

        #region Data Members
        internal TypeKind       m_typeKind;
        internal Type           m_baseType;
        internal int            m_cRank;        // count of dimension
        // If LowerBound and UpperBound is equal, that means one element. 
        // If UpperBound is less than LowerBound, then the size is not specified.
        internal int[]          m_iaLowerBound;
        internal int[]          m_iaUpperBound; // count of dimension
        private char[]          m_bFormat;      // format string to form the full name.
        private bool            m_isSzArray = true;
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
            if (baseType == null)
                throw new ArgumentNullException("baseType");
            Contract.EndContractBlock();

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
                int[]  iaTemp = new int[m_cRank * 2];
                Array.Copy(m_iaLowerBound, iaTemp, m_cRank);
                m_iaLowerBound = iaTemp;            
                Array.Copy(m_iaUpperBound, iaTemp, m_cRank);
                m_iaUpperBound = iaTemp;            
            }

            m_iaLowerBound[m_cRank] = lower;
            m_iaUpperBound[m_cRank] = upper;
            m_cRank++;
        }

        internal void SetFormat(char[] bFormat, int curIndex, int length)
        {
            // Cache the text display format for this SymbolType

            char[] bFormatTemp = new char[length];
            Array.Copy(bFormat, curIndex, bFormatTemp, 0, length);
            m_bFormat = bFormatTemp;
        }
        #endregion
        
        #region Type Overrides
        internal override bool IsSzArray 
        { 
            get 
            { 
                if (m_cRank > 1)
                    return false;
                
                return m_isSzArray;
            }
        }

        public override Type MakePointerType() 
        { 
            return SymbolType.FormCompoundType((new String(m_bFormat) + "*").ToCharArray(), m_baseType, 0);
        }

        public override Type MakeByRefType() 
        { 
            return SymbolType.FormCompoundType((new String(m_bFormat) + "&").ToCharArray(), m_baseType, 0);
        }
        
        public override Type MakeArrayType() 
        { 
            return SymbolType.FormCompoundType((new String(m_bFormat) + "[]").ToCharArray(), m_baseType, 0);
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
            SymbolType st = SymbolType.FormCompoundType((new String(m_bFormat) + s).ToCharArray(), m_baseType, 0) as SymbolType;
            return st;
        }

        public override int GetArrayRank()
        {
            if (!IsArray)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride"));
            Contract.EndContractBlock();

            return m_cRank;
        }
        
        public override Guid GUID 
        {
            get { throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType")); }
        }

        public override Object InvokeMember(String name, BindingFlags invokeAttr, Binder binder, Object target, 
            Object[] args, ParameterModifier[] modifiers, CultureInfo culture, String[] namedParameters)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));      
        }

        public override Module Module 
        {
            get 
            {
                Type baseType;

                for (baseType = m_baseType; baseType is SymbolType; baseType = ((SymbolType) baseType).m_baseType);

                return baseType.Module;
            }
        }
        public override Assembly Assembly 
        {
            get 
            {
                Type baseType;

                for (baseType = m_baseType; baseType is SymbolType; baseType = ((SymbolType) baseType).m_baseType);

                return baseType.Assembly;
            }
        }
                
        public override RuntimeTypeHandle TypeHandle 
        {
             get { throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType")); }     
        }
            
        public override String Name 
        {
            get 
            { 
                Type baseType;
                String sFormat = new String(m_bFormat);

                for (baseType = m_baseType; baseType is SymbolType; baseType = ((SymbolType)baseType).m_baseType)
                    sFormat = new String(((SymbolType)baseType).m_bFormat) + sFormat;

                return baseType.Name + sFormat;
            }
        }
                
        public override String FullName 
        {
            get 
            { 
                return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.FullName);
            }
        }

        public override String AssemblyQualifiedName 
        {
            get 
            { 
                return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.AssemblyQualifiedName);
            }
        }

        public override String ToString()
        {            
                return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.ToString);
        }
    
        public override String Namespace 
        {
            get { return m_baseType.Namespace; }
        }
    
        public override Type BaseType 
        {
             
            get { return typeof(System.Array); }
        }
        
        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr,Binder binder,
                CallingConventions callConvention, Type[] types,ParameterModifier[] modifiers)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));      
        }
        
[System.Runtime.InteropServices.ComVisible(true)]
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));      
        }
        
        protected override MethodInfo GetMethodImpl(String name,BindingFlags bindingAttr,Binder binder,
                CallingConventions callConvention, Type[] types,ParameterModifier[] modifiers)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));      
        }
    
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));      
        }
    
        public override FieldInfo GetField(String name, BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));      
        }
        
        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));      
        }
    
        public override Type GetInterface(String name,bool ignoreCase)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));      
        }
    
        public override Type[] GetInterfaces()
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));      
        }
    
        public override EventInfo GetEvent(String name,BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));      
        }
    
        public override EventInfo[] GetEvents()
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));      
        }
    
        protected override PropertyInfo GetPropertyImpl(String name, BindingFlags bindingAttr, Binder binder, 
                Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));      
        }
    
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));      
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));      
        }
   
        public override Type GetNestedType(String name, BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));      
        }

        public override MemberInfo[] GetMember(String name,  MemberTypes type, BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));      
        }
        
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));      
        }

[System.Runtime.InteropServices.ComVisible(true)]
        public override InterfaceMapping GetInterfaceMap(Type interfaceType)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));      
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));      
        }
    
        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            // Return the attribute flags of the base type?
            Type baseType;
            for (baseType = m_baseType; baseType is SymbolType; baseType = ((SymbolType)baseType).m_baseType);
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

        public override bool IsConstructedGenericType
        {
            get
            {
                return false;
            }
        }

        public override Type GetElementType()
        {
            return m_baseType;
        }
        
        protected override bool HasElementTypeImpl()
        {
            return m_baseType != null;
        }
    
        public override Type UnderlyingSystemType 
        {
             
            get { return this; }
        }
            
        public override Object[] GetCustomAttributes(bool inherit)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));      
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));      
        }

        public override bool IsDefined (Type attributeType, bool inherit)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonReflectedType"));      
        }
        #endregion
    }
}

















