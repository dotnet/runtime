// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

using AssemblyName = System.Reflection.AssemblyName;
using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    // TODO: This file is pretty much a line-by-line port of C++ code to parse CA type name strings from NUTC.
    //       It's a stopgap solution.
    //       This should be replaced with type name parser in System.Reflection.Metadata once it starts shipping.

    public static class CustomAttributeTypeNameParser
    {
        /// <summary>
        /// Parses the string '<paramref name="name"/>' and returns the type corresponding to the parsed type name.
        /// The type name string should be in the 'SerString' format as defined by the ECMA-335 standard.
        /// This is the inverse of what <see cref="CustomAttributeTypeNameFormatter"/> does.
        /// </summary>
        public static TypeDesc GetTypeByCustomAttributeTypeName(this ModuleDesc module, string name, bool throwIfNotFound = true, Func<string, ModuleDesc, bool, MetadataType> resolver = null)
        {
            TypeDesc loadedType;

            StringBuilder genericTypeDefName = new StringBuilder(name.Length);

            var ch = name.Begin();
            var nameEnd = name.End();

            for (; ch < nameEnd; ++ch)
            {
                // Always pass escaped characters through.
                if (ch.Current == '\\')
                {
                    genericTypeDefName.Append(ch.Current);
                    ++ch;
                    if (ch < nameEnd)
                    {
                        genericTypeDefName.Append(ch.Current);
                    }
                    continue;
                }

                // The type def name ends if

                // The start of a generic argument list
                if (ch.Current == '[')
                    break;

                // Indication that the type is a pointer
                if (ch.Current == '*')
                    break;

                // Indication that the type is a reference
                if (ch.Current == '&')
                    break;

                // A comma that indicates that the rest of the name is an assembly reference
                if (ch.Current == ',')
                    break;

                genericTypeDefName.Append(ch.Current);
            }

            ModuleDesc homeModule = module;
            AssemblyName homeAssembly = FindAssemblyIfNamePresent(name);
            if (homeAssembly != null)
            {
                homeModule = module.Context.ResolveAssembly(homeAssembly, throwIfNotFound);
                if (homeModule == null)
                    return null;
            }
            MetadataType typeDef = resolver != null ? resolver(genericTypeDefName.ToString(), homeModule, throwIfNotFound) :
                ResolveCustomAttributeTypeDefinitionName(genericTypeDefName.ToString(), homeModule, throwIfNotFound);
            if (typeDef == null)
                return null;

            ArrayBuilder<TypeDesc> genericArgs = new ArrayBuilder<TypeDesc>();

            // Followed by generic instantiation parameters (but check for the array case)
            if (ch < nameEnd && ch.Current == '[' && (ch + 1) < nameEnd && (ch + 1).Current != ']' && (ch + 1).Current != ',')
            {
                ch++; // truncate the '['
                var genericInstantiationEnd = ch + ReadTypeArgument(ch, nameEnd, true);  // find the end of the instantiation list
                while (ch < genericInstantiationEnd)
                {
                    if (ch.Current == ',')
                        ch++;

                    int argLen = ReadTypeArgument(ch, name.End(), false);
                    string typeArgName;
                    if (ch.Current == '[')
                    {
                        // This type argument name is stringified,
                        // we need to remove the [] from around it
                        ch++;
                        typeArgName = StringIterator.Substring(ch, ch + (argLen - 2));
                        ch += argLen - 1;
                    }
                    else
                    {
                        typeArgName = StringIterator.Substring(ch, ch + argLen);
                        ch += argLen;
                    }

                    TypeDesc argType = module.GetTypeByCustomAttributeTypeName(typeArgName, throwIfNotFound, resolver);
                    if (argType == null)
                        return null;
                    genericArgs.Add(argType);
                }

                Debug.Assert(ch == genericInstantiationEnd);
                ch++;

                loadedType = typeDef.MakeInstantiatedType(genericArgs.ToArray());
            }
            else
            {
                // Non-generic type
                loadedType = typeDef;
            }

            // At this point the characters following may be any number of * characters to indicate pointer depth
            while (ch < nameEnd)
            {
                if (ch.Current == '*')
                {
                    loadedType = loadedType.MakePointerType();
                }
                else
                {
                    break;
                }
                ch++;
            }

            // Followed by any number of "[]" or "[,*]" pairs to indicate arrays
            int commasSeen = 0;
            bool bracketSeen = false;
            while (ch < nameEnd)
            {
                if (ch.Current == '[')
                {
                    ch++;
                    commasSeen = 0;
                    bracketSeen = true;
                }
                else if (ch.Current == ']')
                {
                    if (!bracketSeen)
                        break;

                    ch++;
                    if (commasSeen == 0)
                    {
                        loadedType = loadedType.MakeArrayType();
                    }
                    else
                    {
                        loadedType = loadedType.MakeArrayType(commasSeen + 1);
                    }

                    bracketSeen = false;
                }
                else if (ch.Current == ',')
                {
                    if (!bracketSeen)
                        break;
                    ch++;
                    commasSeen++;
                }
                else
                {
                    break;
                }
            }

            // Followed by at most one & character to indicate a byref.
            if (ch < nameEnd)
            {
                if (ch.Current == '&')
                {
                    loadedType = loadedType.MakeByRefType();
                    ch++;
                }
            }

            return loadedType;
        }


        public static MetadataType ResolveCustomAttributeTypeDefinitionName(string name, ModuleDesc module, bool throwIfNotFound)
        {
            MetadataType containingType = null;
            StringBuilder typeName = new StringBuilder(name.Length);
            bool escaped = false;
            for (var c = name.Begin(); c < name.End(); c++)
            {
                if (c.Current == '\\' && !escaped)
                {
                    escaped = true;
                    continue;
                }

                if (escaped)
                {
                    escaped = false;
                    typeName.Append(c.Current);
                    continue;
                }

                if (c.Current == ',')
                {
                    break;
                }

                if (c.Current == '[' || c.Current == '*' || c.Current == '&')
                {
                    break;
                }

                if (c.Current == '+')
                {
                    if (containingType != null)
                    {
                        MetadataType outerType = containingType;
                        containingType = outerType.GetNestedType(typeName.ToString());
                        if (containingType == null)
                        {
                            if (throwIfNotFound)
                                ThrowHelper.ThrowTypeLoadException(typeName.ToString(), outerType.Module);
                            
                            return null;
                        }
                    }
                    else
                    {
                        containingType = module.GetType(typeName.ToString(), throwIfNotFound);
                        if (containingType == null)
                            return null;
                    }
                    typeName.Length = 0;
                    continue;
                }

                typeName.Append(c.Current);
            }

            if (containingType != null)
            {
                MetadataType type = containingType.GetNestedType(typeName.ToString());
                if ((type == null) && throwIfNotFound)
                    ThrowHelper.ThrowTypeLoadException(typeName.ToString(), containingType.Module);

                return type;
            }

            return module.GetType(typeName.ToString(), throwIfNotFound);
        }

        private static MetadataType GetType(this ModuleDesc module, string fullName, bool throwIfNotFound = true)
        {
            string namespaceName;
            string typeName;
            int split = fullName.LastIndexOf('.');
            if (split < 0)
            {
                namespaceName = "";
                typeName = fullName;
            }
            else
            {
                namespaceName = fullName.Substring(0, split);
                typeName = fullName.Substring(split + 1);
            }
            return module.GetType(namespaceName, typeName, throwIfNotFound ? NotFoundBehavior.Throw : NotFoundBehavior.ReturnNull);
        }

        private static AssemblyName FindAssemblyIfNamePresent(string name)
        {
            AssemblyName result = null;
            var endOfType = name.Begin() + ReadTypeArgument(name.Begin(), name.End(), false);
            if (endOfType < name.End() && endOfType.Current == ',')
            {
                // There is an assembly name here
                int foundCommas = 0;
                var endOfAssemblyName = endOfType;
                for (var ch = endOfType + 1; ch < name.End(); ch++)
                {
                    if (foundCommas == 3)
                    {
                        // We're now eating the public key token, looking for the end of the name,
                        // or a right bracket
                        if (ch.Current == ']' || ch.Current == ',')
                        {
                            endOfAssemblyName = ch - 1;
                            break;
                        }
                    }

                    if (ch.Current == ',')
                    {
                        foundCommas++;
                    }
                }
                if (endOfAssemblyName == endOfType)
                {
                    endOfAssemblyName = name.End();
                }

                // eat the comma
                endOfType++;
                for (; endOfType < endOfAssemblyName; ++endOfType)
                {
                    // trim off spaces
                    if (endOfType.Current != ' ')
                        break;
                }
                result = new AssemblyName(StringIterator.Substring(endOfType, endOfAssemblyName));
            }
            return result;
        }

        private static int ReadTypeArgument(StringIterator strBegin, StringIterator strEnd, bool ignoreComma)
        {
            int level = 0;
            int length = 0;
            for (var c = strBegin; c < strEnd; c++)
            {
                if (c.Current == '\\')
                {
                    length++;
                    if ((c + 1) < strEnd)
                    {
                        c++;
                        length++;
                    }
                    continue;
                }
                if (c.Current == '[')
                {
                    level++;
                }
                else if (c.Current == ']')
                {
                    if (level == 0)
                        break;
                    level--;
                }
                else if (!ignoreComma && (c.Current == ','))
                {
                    if (level == 0)
                        break;
                }

                length++;
            }

            return length;
        }

        #region C++ string iterator compatibility shim

        private static StringIterator Begin(this string s)
        {
            return new StringIterator(s, 0);
        }

        private static StringIterator End(this string s)
        {
            return new StringIterator(s, s.Length);
        }

        struct StringIterator
        {
            private string _string;
            private int _index;

            public char Current
            {
                get
                {
                    return _string[_index];
                }
            }

            public StringIterator(string s, int index)
            {
                Debug.Assert(index <= s.Length);
                _string = s;
                _index = index;
            }

            public static string Substring(StringIterator it1, StringIterator it2)
            {
                Debug.Assert(Object.ReferenceEquals(it1._string, it2._string));
                return it1._string.Substring(it1._index, it2._index - it1._index);
            }

            public static StringIterator operator++(StringIterator it)
            {
                return new StringIterator(it._string, ++it._index);
            }

            public static bool operator <(StringIterator it1, StringIterator it2)
            {
                Debug.Assert(Object.ReferenceEquals(it1._string, it2._string));
                return it1._index < it2._index;
            }

            public static bool operator >(StringIterator it1, StringIterator it2)
            {
                Debug.Assert(Object.ReferenceEquals(it1._string, it2._string));
                return it1._index > it2._index;
            }

            public static StringIterator operator+(StringIterator it, int val)
            {
                return new StringIterator(it._string, it._index + val);
            }

            public static StringIterator operator-(StringIterator it, int val)
            {
                return new StringIterator(it._string, it._index - val);
            }

            public static bool operator==(StringIterator it1, StringIterator it2)
            {
                Debug.Assert(Object.ReferenceEquals(it1._string, it2._string));
                return it1._index == it2._index;
            }

            public static bool operator !=(StringIterator it1, StringIterator it2)
            {
                Debug.Assert(Object.ReferenceEquals(it1._string, it2._string));
                return it1._index != it2._index;
            }

            public override bool Equals(object obj)
            {
                throw new NotImplementedException();
            }

            public override int GetHashCode()
            {
                throw new NotImplementedException();
            }
        }
        #endregion
    }
}
