// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    using System.Diagnostics;
    using System;
    using Microsoft.Win32;
    using System.Collections;
    using System.Runtime.InteropServices;
    using System.Globalization;
    using System.Reflection;

    [
        ComVisible(true),
        FlagsAttribute
    ]
    internal enum CodeTypeReferenceOptions
    {
        GlobalReference = 0x00000001,
        GenericTypeParameter = 0x00000002
    }

    /// <devdoc>
    ///    <para>
    ///       Represents a Type
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeTypeReference : CodeObject
    {
        private string _baseType;
        private bool _isInterface;
        private int _arrayRank;
        private CodeTypeReference _arrayElementType;
        private CodeTypeReferenceCollection _typeArguments;
        private CodeTypeReferenceOptions _referenceOptions;
        private bool _needsFixup = false;

        public CodeTypeReference()
        {
            _baseType = string.Empty;
            _arrayRank = 0;
            _arrayElementType = null;
        }

        public CodeTypeReference(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (type.IsArray)
            {
                _arrayRank = type.GetArrayRank();
                _arrayElementType = new CodeTypeReference(type.GetElementType());
                _baseType = null;
            }
            else
            {
                InitializeFromType(type);
                _arrayRank = 0;
                _arrayElementType = null;
            }

            _isInterface = type.GetTypeInfo().IsInterface;
        }

        public CodeTypeReference(Type type, CodeTypeReferenceOptions codeTypeReferenceOption) : this(type)
        {
            _referenceOptions = codeTypeReferenceOption;
        }

        public CodeTypeReference(String typeName, CodeTypeReferenceOptions codeTypeReferenceOption)
        {
            Initialize(typeName, codeTypeReferenceOption);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>

        // We support the reflection format for generice type name.
        // The format is like:
        //
        public CodeTypeReference(string typeName)
        {
            Initialize(typeName);
        }

        private void InitializeFromType(Type type)
        {
            _baseType = type.Name;
            if (!type.IsGenericParameter)
            {
                Type currentType = type;
                while (currentType.IsNested)
                {
                    currentType = currentType.DeclaringType;
                    _baseType = currentType.Name + "+" + _baseType;
                }
                if (!String.IsNullOrEmpty(type.Namespace))
                    _baseType = type.Namespace + "." + _baseType;
            }

            // pick up the type arguments from an instantiated generic type but not an open one    
            if (type.GetTypeInfo().IsGenericType && !type.GetTypeInfo().ContainsGenericParameters)
            {
                Type[] genericArgs = type.GetGenericArguments();
                for (int i = 0; i < genericArgs.Length; i++)
                {
                    TypeArguments.Add(new CodeTypeReference(genericArgs[i]));
                }
            }
            else if (!type.GetTypeInfo().IsGenericTypeDefinition)
            {
                // if the user handed us a non-generic type, but later
                // appends generic type arguments, we'll pretend
                // it's a generic type for their sake - this is good for
                // them if they pass in System.Nullable class when they
                // meant the System.Nullable<T> value type.
                _needsFixup = true;
            }
        }

        private void Initialize(string typeName)
        {
            Initialize(typeName, _referenceOptions);
        }

        private void Initialize(string typeName, CodeTypeReferenceOptions options)
        {
            Options = options;
            if (typeName == null || typeName.Length == 0)
            {
                typeName = typeof(void).FullName;
                _baseType = typeName;
                _arrayRank = 0;
                _arrayElementType = null;
                return;
            }

            typeName = RipOffAssemblyInformationFromTypeName(typeName);

            int end = typeName.Length - 1;
            int current = end;
            _needsFixup = true;      // default to true, and if we find arity or generic type args, we'll clear the flag.

            // Scan the entire string for valid array tails and store ranks for array tails
            // we found in a queue.
            Queue q = new Queue();
            while (current >= 0)
            {
                int rank = 1;
                if (typeName[current--] == ']')
                {
                    while (current >= 0 && typeName[current] == ',')
                    {
                        rank++;
                        current--;
                    }

                    if (current >= 0 && typeName[current] == '[')
                    { // found a valid array tail
                        q.Enqueue(rank);
                        current--;
                        end = current;
                        continue;
                    }
                }
                break;
            }

            // Try find generic type arguments
            current = end;
            ArrayList typeArgumentList = new ArrayList();
            Stack subTypeNames = new Stack();
            if (current > 0 && typeName[current--] == ']')
            {
                _needsFixup = false;
                int unmatchedRightBrackets = 1;
                int subTypeNameEndIndex = end;

                // Try find the matching '[', if we can't find it, we will not try to parse the string
                while (current >= 0)
                {
                    if (typeName[current] == '[')
                    {
                        // break if we found matched brackets
                        if (--unmatchedRightBrackets == 0) break;
                    }
                    else if (typeName[current] == ']')
                    {
                        ++unmatchedRightBrackets;
                    }
                    else if (typeName[current] == ',' && unmatchedRightBrackets == 1)
                    {
                        //
                        // Type name can contain nested generic types. Following is an example:
                        // System.Collections.Generic.Dictionary`2[[System.String, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089], 
                        //          [System.Collections.Generic.List`1[[System.Int32, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], 
                        //           mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]
                        // 
                        // Spliltting by ',' won't work. We need to do first-level split by ','. 
                        //
                        if (current + 1 < subTypeNameEndIndex)
                        {
                            subTypeNames.Push(typeName.Substring(current + 1, subTypeNameEndIndex - current - 1));
                        }

                        subTypeNameEndIndex = current;
                    }
                    --current;
                }

                if (current > 0 && (end - current - 1) > 0)
                {
                    // push the last generic type argument name if there is any
                    if (current + 1 < subTypeNameEndIndex)
                    {
                        subTypeNames.Push(typeName.Substring(current + 1, subTypeNameEndIndex - current - 1));
                    }

                    // we found matched brackets and the brackets contains some characters.                    
                    while (subTypeNames.Count > 0)
                    {
                        String name = RipOffAssemblyInformationFromTypeName((string)subTypeNames.Pop());
                        typeArgumentList.Add(new CodeTypeReference(name));
                    }
                    end = current - 1;
                }
            }

            if (end < 0)
            {  // this can happen if we have some string like "[...]"
                _baseType = typeName;
                return;
            }

            if (q.Count > 0)
            {
                CodeTypeReference type = new CodeTypeReference(typeName.Substring(0, end + 1), Options);

                for (int i = 0; i < typeArgumentList.Count; i++)
                {
                    type.TypeArguments.Add((CodeTypeReference)typeArgumentList[i]);
                }

                while (q.Count > 1)
                {
                    type = new CodeTypeReference(type, (int)q.Dequeue());
                }

                // we don't need to create a new CodeTypeReference for the last one.
                Debug.Assert(q.Count == 1, "We should have one and only one in the rank queue.");
                _baseType = null;
                _arrayRank = (int)q.Dequeue();
                _arrayElementType = type;
            }
            else if (typeArgumentList.Count > 0)
            {
                for (int i = 0; i < typeArgumentList.Count; i++)
                {
                    TypeArguments.Add((CodeTypeReference)typeArgumentList[i]);
                }

                _baseType = typeName.Substring(0, end + 1);
            }
            else
            {
                _baseType = typeName;
            }

            // Now see if we have some arity.  baseType could be null if this is an array type. 
            if (_baseType != null && _baseType.IndexOf('`') != -1)
                _needsFixup = false;
        }

        public CodeTypeReference(string typeName, params CodeTypeReference[] typeArguments) : this(typeName)
        {
            if (typeArguments != null && typeArguments.Length > 0)
            {
                TypeArguments.AddRange(typeArguments);
            }
        }

        public CodeTypeReference(CodeTypeParameter typeParameter) :
                this((typeParameter == null) ? (string)null : typeParameter.Name)
        {
            _referenceOptions = CodeTypeReferenceOptions.GenericTypeParameter;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeTypeReference(string baseType, int rank)
        {
            _baseType = null;
            _arrayRank = rank;
            _arrayElementType = new CodeTypeReference(baseType);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeTypeReference(CodeTypeReference arrayType, int rank)
        {
            _baseType = null;
            _arrayRank = rank;
            _arrayElementType = arrayType;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeTypeReference ArrayElementType
        {
            get
            {
                return _arrayElementType;
            }
            set
            {
                _arrayElementType = value;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int ArrayRank
        {
            get
            {
                return _arrayRank;
            }
            set
            {
                _arrayRank = value;
            }
        }

        internal int NestedArrayDepth
        {
            get
            {
                if (_arrayElementType == null)
                    return 0;

                return 1 + _arrayElementType.NestedArrayDepth;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public string BaseType
        {
            get
            {
                if (_arrayRank > 0 && _arrayElementType != null)
                {
                    return _arrayElementType.BaseType;
                }
                if (String.IsNullOrEmpty(_baseType))
                    return string.Empty;

                string returnType = _baseType;
                if (_needsFixup && TypeArguments.Count > 0)
                    returnType = returnType + '`' + TypeArguments.Count.ToString(CultureInfo.InvariantCulture);

                return returnType;
            }
            set
            {
                _baseType = value;
                Initialize(_baseType);
            }
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public CodeTypeReferenceOptions Options
        {
            get { return _referenceOptions; }
            set { _referenceOptions = value; }
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public CodeTypeReferenceCollection TypeArguments
        {
            get
            {
                if (_arrayRank > 0 && _arrayElementType != null)
                {
                    return _arrayElementType.TypeArguments;
                }

                if (_typeArguments == null)
                {
                    _typeArguments = new CodeTypeReferenceCollection();
                }
                return _typeArguments;
            }
        }

        internal bool IsInterface
        {
            get
            {
                // Note that this only works correctly if the Type ctor was used. Otherwise, it's always false.
                return _isInterface;
            }
        }

        //
        // The string for generic type argument might contain assembly information and square bracket pair.
        // There might be leading spaces in front the type name.
        // Following function will rip off assembly information and brackets 
        // Following is an example:
        // " [System.Collections.Generic.List[[System.String, mscorlib, Version=2.0.0.0, Culture=neutral,
        //   PublicKeyToken=b77a5c561934e089]], mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]"
        //
        private string RipOffAssemblyInformationFromTypeName(string typeName)
        {
            int start = 0;
            int end = typeName.Length - 1;
            string result = typeName;

            // skip white space in the beginning
            while (start < typeName.Length && Char.IsWhiteSpace(typeName[start])) start++;
            while (end >= 0 && Char.IsWhiteSpace(typeName[end])) end--;

            if (start < end)
            {
                if (typeName[start] == '[' && typeName[end] == ']')
                {
                    start++;
                    end--;
                }

                // if we still have a ] at the end, there's no assembly info. 
                if (typeName[end] != ']')
                {
                    int commaCount = 0;
                    for (int index = end; index >= start; index--)
                    {
                        if (typeName[index] == ',')
                        {
                            commaCount++;
                            if (commaCount == 4)
                            {
                                result = typeName.Substring(start, index - start);
                                break;
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}

