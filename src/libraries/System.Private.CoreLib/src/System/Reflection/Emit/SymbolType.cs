// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
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
    internal sealed partial class SymbolType : TypeInfo
    {
        #region Data Members
        #region Fields need to be kept in order
        // For Mono runtime its important to keep this declaration order in sync with MonoReflectionSymbolType struct in object-internals.h
        internal Type _baseType = null!;
        internal TypeKind _typeKind;
        internal int _rank;        // count of dimension
        #endregion
        // If LowerBound and UpperBound is equal, that means one element.
        // If UpperBound is less than LowerBound, then the size is not specified.
        internal int[] _iaLowerBound;
        internal int[] _iaUpperBound; // count of dimension
        private string? _format;      // format string to form the full name.
        private bool _isSzArray = true;
        #endregion

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

                symbolType = new SymbolType(baseType, TypeKind.IsByRef);
                symbolType.SetFormat(format, curIndex, 1);
                curIndex++;

                if (curIndex != format.Length)
                    // ByRef has to be the last char!!
                    throw new ArgumentException(SR.Argument_BadSigFormat);

                return symbolType;
            }

            if (format[curIndex] == '[')
            {
                // Array type.
                symbolType = new SymbolType(baseType, TypeKind.IsArray);
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
                        symbolType._isSzArray = false;
                        curIndex++;
                    }
                    // consume, one dimension at a time
                    if (char.IsAsciiDigit(format[curIndex]) || format[curIndex] == '-')
                    {
                        bool isNegative = false;
                        if (format[curIndex] == '-')
                        {
                            isNegative = true;
                            curIndex++;
                        }

                        // lower bound is specified. Consume the low bound
                        while (char.IsAsciiDigit(format[curIndex]))
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
                        if (char.IsAsciiDigit(format[curIndex]) || format[curIndex] == '-')
                        {
                            bool isNegative = false;
                            iUpperBound = 0;
                            if (format[curIndex] == '-')
                            {
                                isNegative = true;
                                curIndex++;
                            }

                            // lower bound is specified. Consume the low bound
                            while (char.IsAsciiDigit(format[curIndex]))
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

                return FormCompoundType(format, symbolType, curIndex);
            }
            else if (format[curIndex] == '*')
            {
                // pointer type.

                symbolType = new SymbolType(baseType, TypeKind.IsPointer);
                symbolType.SetFormat(format, curIndex, 1);
                curIndex++;
                return FormCompoundType(format, symbolType, curIndex);
            }

            return null;
        }

        #endregion

        #region Constructor
        internal SymbolType(Type baseType, TypeKind typeKind)
        {
            ArgumentNullException.ThrowIfNull(baseType);

            _baseType = baseType;
            _typeKind = typeKind;
            _iaLowerBound = new int[4];
            _iaUpperBound = new int[4];
        }

        #endregion

        #region Internal Members
        private void SetBounds(int lower, int upper)
        {
            // Increase the rank, set lower and upper bound

            if (lower != 0 || upper != -1)
                _isSzArray = false;

            if (_iaLowerBound.Length <= _rank)
            {
                // resize the bound array
                int[] iaTemp = new int[_rank * 2];
                Array.Copy(_iaLowerBound, iaTemp, _rank);
                _iaLowerBound = iaTemp;
                Array.Copy(_iaUpperBound, iaTemp, _rank);
                _iaUpperBound = iaTemp;
            }

            _iaLowerBound[_rank] = lower;
            _iaUpperBound[_rank] = upper;
            _rank++;
        }

        internal void SetFormat(string format, int curIndex, int length)
        {
            // Cache the text display format for this SymbolType

            _format = format.Substring(curIndex, length);
        }
        #endregion

        #region Type Overrides

        public override bool IsTypeDefinition => false;

        public override bool IsSZArray => _rank <= 1 && _isSzArray;

        public override Type MakePointerType()
        {
            return FormCompoundType(_format + "*", _baseType, 0)!;
        }

        public override Type MakeByRefType()
        {
            return FormCompoundType(_format + "&", _baseType, 0)!;
        }

        [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        public override Type MakeArrayType()
        {
            return FormCompoundType(_format + "[]", _baseType, 0)!;
        }

        [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        public override Type MakeArrayType(int rank)
        {
            string s = FormatRank(rank);
            SymbolType? st = FormCompoundType(_format + s, _baseType, 0) as SymbolType;
            return st!;
        }

        internal static string FormatRank(int rank)
        {
            if (rank <= 0)
            {
                throw new IndexOutOfRangeException();
            }

            return rank == 1 ? "[*]" : "[" + new string(',', rank - 1) + "]";
        }

        public override int GetArrayRank()
        {
            if (!IsArray)
                throw new ArgumentException(SR.Argument_HasToBeArrayClass);

            return _rank;
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

                for (baseType = _baseType; baseType is SymbolType; baseType = ((SymbolType)baseType)._baseType) ;

                return baseType.Module;
            }
        }
        public override Assembly Assembly
        {
            get
            {
                Type baseType;

                for (baseType = _baseType; baseType is SymbolType; baseType = ((SymbolType)baseType)._baseType) ;

                return baseType.Assembly;
            }
        }

        public override RuntimeTypeHandle TypeHandle => throw new NotSupportedException(SR.NotSupported_NonReflectedType);

        public override string Name
        {
            get
            {
                Type baseType;
                string? sFormat = _format;

                for (baseType = _baseType; baseType is SymbolType; baseType = ((SymbolType)baseType)._baseType)
                    sFormat = ((SymbolType)baseType)._format + sFormat;

                return baseType.Name + sFormat;
            }
        }

        public override string? FullName => TypeNameBuilder.ToString(this, TypeNameBuilder.Format.FullName);

        public override string? AssemblyQualifiedName => TypeNameBuilder.ToString(this, TypeNameBuilder.Format.AssemblyQualifiedName);

        public override string ToString()
        {
            return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.ToString)!;
        }

        public override string? Namespace => _baseType.Namespace;

        public override Type BaseType => typeof(Array);

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

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type GetInterface(string name, bool ignoreCase)
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
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

        [DynamicallyAccessedMembers(GetAllMembersInternal)]
        public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }

        [DynamicallyAccessedMembers(GetAllMembersInternal)]
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            throw new NotSupportedException(SR.NotSupported_NonReflectedType);
        }

        private const DynamicallyAccessedMemberTypes GetAllMembersInternal = DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields |
            DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods |
            DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents |
            DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties |
            DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors |
            DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes;

        public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType)
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
            for (baseType = _baseType; baseType is SymbolType; baseType = ((SymbolType)baseType)._baseType) ;
            return baseType.Attributes;
        }

        protected override bool IsArrayImpl()
        {
            return _typeKind == TypeKind.IsArray;
        }

        protected override bool IsPointerImpl()
        {
            return _typeKind == TypeKind.IsPointer;
        }

        protected override bool IsByRefImpl()
        {
            return _typeKind == TypeKind.IsByRef;
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
            return _baseType;
        }

        protected override bool HasElementTypeImpl()
        {
            return _baseType != null;
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
