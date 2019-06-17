// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Globalization;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.Serialization;    
using System.Runtime.CompilerServices;
using System.Security;
using DebuggerStepThroughAttribute = System.Diagnostics.DebuggerStepThroughAttribute;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using CustomAttribute=System.MonoCustomAttrs;

namespace System 
{
    // Keep this in sync with FormatFlags defined in typestring.h
    internal enum TypeNameFormatFlags
    {
        FormatBasic         = 0x00000000, // Not a bitmask, simply the tersest flag settings possible
        FormatNamespace     = 0x00000001, // Include namespace and/or enclosing class names in type names
        FormatFullInst      = 0x00000002, // Include namespace and assembly in generic types (regardless of other flag settings)
        FormatAssembly      = 0x00000004, // Include assembly display name in type names
        FormatSignature     = 0x00000008, // Include signature in method names
        FormatNoVersion     = 0x00000010, // Suppress version and culture information in all assembly names
#if _DEBUG
        FormatDebug         = 0x00000020, // For debug printing of types only
#endif
        FormatAngleBrackets = 0x00000040, // Whether generic types are C<T> or C[T]
        FormatStubInfo      = 0x00000080, // Include stub info like {unbox-stub}
        FormatGenericParam  = 0x00000100, // Use !name and !!name for generic type and method parameters

        // If we want to be able to distinguish between overloads whose parameter types have the same name but come from different assemblies,
        // we can add FormatAssembly | FormatNoVersion to FormatSerialization. But we are omitting it because it is not a useful scenario
        // and including the assembly name will normally increase the size of the serialized data and also decrease the performance.
        FormatSerialization = FormatNamespace |
                              FormatGenericParam |
                              FormatFullInst
    }

    internal enum TypeNameKind
    {
        Name,
        ToString,
        SerializationName,
        FullName,
    }

    [Serializable]
    internal class RuntimeType : TypeInfo, ISerializable, ICloneable
    {
        #region Definitions

        internal enum MemberListType
        {
            All,
            CaseSensitive,
            CaseInsensitive,
            HandleToInfo
        }

        // Helper to build lists of MemberInfos. Special cased to avoid allocations for lists of one element.
        private struct ListBuilder<T> where T : class
        {
            T[] _items;
            T _item;
            int _count;
            int _capacity;

            public ListBuilder(int capacity)
            {
                _items = null;
                _item = null;
                _count = 0;
                _capacity = capacity;
            }

            public T this[int index]
            {
                get
                {
                    Contract.Requires(index < Count);
                    return (_items != null) ? _items[index] : _item;
                }
            }

            public T[] ToArray()
            {
                if (_count == 0)
                    return Array.Empty<T> ();
                if (_count == 1)
                    return new T[1] { _item };

                Array.Resize(ref _items, _count);
                _capacity = _count;
                return _items;
            }

            public void CopyTo(Object[] array, int index)
            {
                if (_count == 0)
                    return;

                if (_count == 1)
                {
                    array[index] = _item;
                    return;
                }

                Array.Copy(_items, 0, array, index, _count);
            }

            public int Count
            {
                get
                {
                    return _count;
                }
            }

            public void Add(T item)
            {
                if (_count == 0)
                {
                    _item = item;
                }
                else                
                {
                    if (_count == 1)
                    {
                        if (_capacity < 2)
                            _capacity = 4;
                        _items = new T[_capacity];
                        _items[0] = _item;
                    }
                    else
                    if (_capacity == _count)
                    {
                        int newCapacity = 2 * _capacity;
                        Array.Resize(ref _items, newCapacity);
                        _capacity = newCapacity;
                    }

                    _items[_count] = item;
                }
                _count++;
            }
        }

        #endregion

        #region Static Members

        #region Internal

        internal static RuntimeType GetType(String typeName, bool throwOnError, bool ignoreCase, bool reflectionOnly,
            ref StackCrawlMark stackMark)
        {
            if (typeName == null)
                throw new ArgumentNullException("typeName");
            Contract.EndContractBlock();

            return RuntimeTypeHandle.GetTypeByName(
                typeName, throwOnError, ignoreCase, reflectionOnly, ref stackMark, false);
        }
        
        private static void ThrowIfTypeNeverValidGenericArgument(RuntimeType type)
        {
            if (type.IsPointer || type.IsByRef || type == typeof(void))
                throw new ArgumentException(
                    Environment.GetResourceString("Argument_NeverValidGenericArgument", type.ToString()));
        }
        
        internal static void SanityCheckGenericArguments(RuntimeType[] genericArguments, RuntimeType[] genericParamters)
        {
            if (genericArguments == null)
                throw new ArgumentNullException();
            Contract.EndContractBlock();

            for(int i = 0; i < genericArguments.Length; i++)
            {                
                if (genericArguments[i] == null)
                    throw new ArgumentNullException();
                
                ThrowIfTypeNeverValidGenericArgument(genericArguments[i]);
            }

            if (genericArguments.Length != genericParamters.Length)
                throw new ArgumentException(
                    Environment.GetResourceString("Argument_NotEnoughGenArguments", genericArguments.Length, genericParamters.Length));
        }

        private static void SplitName(string fullname, out string name, out string ns)
        {
            name = null;
            ns = null;

            if (fullname == null)
                return;

            // Get namespace
            int nsDelimiter = fullname.LastIndexOf(".", StringComparison.Ordinal);
            if (nsDelimiter != -1 )     
            {
                ns = fullname.Substring(0, nsDelimiter);
                int nameLength = fullname.Length - ns.Length - 1;
                if (nameLength != 0)
                    name = fullname.Substring(nsDelimiter + 1, nameLength);
                else
                    name = "";
                Contract.Assert(fullname.Equals(ns + "." + name));
            }
            else
            {
                name = fullname;
            }

        }
        #endregion

        #region Filters
        internal static BindingFlags FilterPreCalculate(bool isPublic, bool isInherited, bool isStatic)
        {
            BindingFlags bindingFlags = isPublic ? BindingFlags.Public : BindingFlags.NonPublic;

            if (isInherited) 
            {   
                // We arrange things so the DeclaredOnly flag means "include inherited members"
                bindingFlags |= BindingFlags.DeclaredOnly; 

                if (isStatic)
                {
                    bindingFlags |= BindingFlags.Static | BindingFlags.FlattenHierarchy;
                }
                else
                {
                    bindingFlags |= BindingFlags.Instance;
                }
            }
            else
            {
                if (isStatic)
                {
                    bindingFlags |= BindingFlags.Static;
                }
                else
                {
                    bindingFlags |= BindingFlags.Instance;
                }
            }

            return bindingFlags;
        }

        // Calculate prefixLookup, ignoreCase, and listType for use by GetXXXCandidates
        private static void FilterHelper(
            BindingFlags bindingFlags, ref string name, bool allowPrefixLookup, out bool prefixLookup, 
            out bool ignoreCase, out MemberListType listType)
        {
            prefixLookup = false;
            ignoreCase = false;

            if (name != null)
            {
                if ((bindingFlags & BindingFlags.IgnoreCase) != 0)
                {
                    name = name.ToLower(CultureInfo.InvariantCulture);
                    ignoreCase = true;
                    listType = MemberListType.CaseInsensitive;
                }
                else
                {
                    listType = MemberListType.CaseSensitive;
                }

                if (allowPrefixLookup && name.EndsWith("*", StringComparison.Ordinal))
                {
                    // We set prefixLookup to true if name ends with a "*".
                    // We will also set listType to All so that all members are included in 
                    // the candidates which are later filtered by FilterApplyPrefixLookup.
                    name = name.Substring(0, name.Length - 1);
                    prefixLookup = true;
                    listType = MemberListType.All;
                }
            }
            else
            {
                listType = MemberListType.All;
            }
        }

        // Used by the singular GetXXX APIs (Event, Field, Interface, NestedType) where prefixLookup is not supported.
        private static void FilterHelper(BindingFlags bindingFlags, ref string name, out bool ignoreCase, out MemberListType listType)
        {
            bool prefixLookup;
            FilterHelper(bindingFlags, ref name, false, out prefixLookup, out ignoreCase, out listType);
        }

        // Only called by GetXXXCandidates, GetInterfaces, and GetNestedTypes when FilterHelper has set "prefixLookup" to true.
        // Most of the plural GetXXX methods allow prefix lookups while the singular GetXXX methods mostly do not.
        private static bool FilterApplyPrefixLookup(MemberInfo memberInfo, string name, bool ignoreCase)
        {
            Contract.Assert(name != null);

            if (ignoreCase)
            {
                if (!memberInfo.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            else
            {
                if (!memberInfo.Name.StartsWith(name, StringComparison.Ordinal))
                    return false;
            }            

            return true;
        }


        // Used by FilterApplyType to perform all the filtering based on name and BindingFlags
        private static bool FilterApplyBase(
            MemberInfo memberInfo, BindingFlags bindingFlags, bool isPublic, bool isNonProtectedInternal, bool isStatic,
            string name, bool prefixLookup)
        {
            #region Preconditions
            Contract.Requires(memberInfo != null);
            Contract.Requires(name == null || (bindingFlags & BindingFlags.IgnoreCase) == 0 || (name.ToLower(CultureInfo.InvariantCulture).Equals(name)));
            #endregion

            #region Filter by Public & Private
            if (isPublic)
            {
                if ((bindingFlags & BindingFlags.Public) == 0)
                    return false;
            }
            else
            {
                if ((bindingFlags & BindingFlags.NonPublic) == 0)
                    return false;
            }
            #endregion

            bool isInherited = !Object.ReferenceEquals(memberInfo.DeclaringType, memberInfo.ReflectedType);

            #region Filter by DeclaredOnly
            if ((bindingFlags & BindingFlags.DeclaredOnly) != 0 && isInherited)
                return false;
            #endregion

            #region Filter by Static & Instance
            if (memberInfo.MemberType != MemberTypes.TypeInfo && 
                memberInfo.MemberType != MemberTypes.NestedType)
            {
                if (isStatic)
                {
                    if ((bindingFlags & BindingFlags.FlattenHierarchy) == 0 && isInherited)
                        return false;

                    if ((bindingFlags & BindingFlags.Static) == 0)
                        return false;
                }
                else
                {
                    if ((bindingFlags & BindingFlags.Instance) == 0)
                        return false;
                }
            }
            #endregion

            #region Filter by name wrt prefixLookup and implicitly by case sensitivity
            if (prefixLookup == true)
            {
                if (!FilterApplyPrefixLookup(memberInfo, name, (bindingFlags & BindingFlags.IgnoreCase) != 0))
                    return false;
            }
            #endregion

            #region Asymmetries
            // @Asymmetry - Internal, inherited, instance, non-protected, non-virtual, non-abstract members returned 
            //              iff BindingFlags !DeclaredOnly, Instance and Public are present except for fields
            if (((bindingFlags & BindingFlags.DeclaredOnly) == 0) &&        // DeclaredOnly not present
                 isInherited  &&                                            // Is inherited Member
    
                (isNonProtectedInternal) &&                                 // Is non-protected internal member
                ((bindingFlags & BindingFlags.NonPublic) != 0) &&           // BindingFlag.NonPublic present 

                (!isStatic) &&                                              // Is instance member
                ((bindingFlags & BindingFlags.Instance) != 0))              // BindingFlag.Instance present 
            {
                MethodInfo methodInfo = memberInfo as MethodInfo;

                if (methodInfo == null)
                    return false;

                if (!methodInfo.IsVirtual && !methodInfo.IsAbstract)
                    return false;
            }
            #endregion

            return true;
        }


        // Used by GetInterface and GetNestedType(s) which don't need parameter type filtering.
        private static bool FilterApplyType(
            Type type, BindingFlags bindingFlags, string name, bool prefixLookup, string ns)
        {
            Contract.Requires((object)type != null);
            Contract.Assert(type is RuntimeType);

            bool isPublic = type.IsNestedPublic || type.IsPublic;
            bool isStatic = false;

            if (!FilterApplyBase(type, bindingFlags, isPublic, type.IsNestedAssembly, isStatic, name, prefixLookup))
                return false;

            if (ns != null && ns != type.Namespace)
                return false;

            return true;
        }


        private static bool FilterApplyMethodInfo(
            RuntimeMethodInfo method, BindingFlags bindingFlags, CallingConventions callConv, Type[] argumentTypes)
        {
            // Optimization: Pre-Calculate the method binding flags to avoid casting.
            return FilterApplyMethodBase(method, method.BindingFlags, bindingFlags, callConv, argumentTypes);
        }

        private static bool FilterApplyConstructorInfo(
            RuntimeConstructorInfo constructor, BindingFlags bindingFlags, CallingConventions callConv, Type[] argumentTypes)
        {
            // Optimization: Pre-Calculate the method binding flags to avoid casting.
            return FilterApplyMethodBase(constructor, constructor.BindingFlags, bindingFlags, callConv, argumentTypes);
        }

        // Used by GetMethodCandidates/GetConstructorCandidates, InvokeMember, and CreateInstanceImpl to perform the necessary filtering.
        // Should only be called by FilterApplyMethodInfo and FilterApplyConstructorInfo.
        private static bool FilterApplyMethodBase(
            MethodBase methodBase, BindingFlags methodFlags, BindingFlags bindingFlags, CallingConventions callConv, Type[] argumentTypes)
        {
            Contract.Requires(methodBase != null);

            bindingFlags ^= BindingFlags.DeclaredOnly;
            #region Check CallingConvention
            if ((callConv & CallingConventions.Any) == 0)
            {
                if ((callConv & CallingConventions.VarArgs) != 0 && 
                    (methodBase.CallingConvention & CallingConventions.VarArgs) == 0)
                    return false;

                if ((callConv & CallingConventions.Standard) != 0 && 
                    (methodBase.CallingConvention & CallingConventions.Standard) == 0)
                    return false;
            }
            #endregion

            #region If argumentTypes supplied
            if (argumentTypes != null)
            {
                ParameterInfo[] parameterInfos = methodBase.GetParametersNoCopy();

                if (argumentTypes.Length != parameterInfos.Length)
                {
                    #region Invoke Member, Get\Set & Create Instance specific case
                    // If the number of supplied arguments differs than the number in the signature AND
                    // we are not filtering for a dynamic call -- InvokeMethod or CreateInstance -- filter out the method.
                    if ((bindingFlags & 
                        (BindingFlags.InvokeMethod | BindingFlags.CreateInstance | BindingFlags.GetProperty | BindingFlags.SetProperty)) == 0)
                        return false;
                    
                    bool testForParamArray = false;
                    bool excessSuppliedArguments = argumentTypes.Length > parameterInfos.Length;

                    if (excessSuppliedArguments) 
                    { // more supplied arguments than parameters, additional arguments could be vararg
                        #region Varargs
                        // If method is not vararg, additional arguments can not be passed as vararg
                        if ((methodBase.CallingConvention & CallingConventions.VarArgs) == 0)
                        {
                            testForParamArray = true;
                        }
                        else 
                        {
                            // If Binding flags did not include varargs we would have filtered this vararg method.
                            // This Invariant established during callConv check.
                            Contract.Assert((callConv & CallingConventions.VarArgs) != 0);
                        }
                        #endregion
                    }
                    else 
                    {// fewer supplied arguments than parameters, missing arguments could be optional
                        #region OptionalParamBinding
                        if ((bindingFlags & BindingFlags.OptionalParamBinding) == 0)
                        {
                            testForParamArray = true;
                        }
                        else
                        {
                            // From our existing code, our policy here is that if a parameterInfo 
                            // is optional then all subsequent parameterInfos shall be optional. 

                            // Thus, iff the first parameterInfo is not optional then this MethodInfo is no longer a canidate.
                            if (!parameterInfos[argumentTypes.Length].IsOptional)
                                testForParamArray = true;
                        }
                        #endregion
                    }

                    #region ParamArray
                    if (testForParamArray)
                    {
                        if  (parameterInfos.Length == 0)
                            return false;

                        // The last argument of the signature could be a param array. 
                        bool shortByMoreThanOneSuppliedArgument = argumentTypes.Length < parameterInfos.Length - 1;

                        if (shortByMoreThanOneSuppliedArgument)
                            return false;

                        ParameterInfo lastParameter = parameterInfos[parameterInfos.Length - 1];

                        if (!lastParameter.ParameterType.IsArray)
                            return false;

                        if (!lastParameter.IsDefined(typeof(ParamArrayAttribute), false))
                            return false;
                    }
                    #endregion

                    #endregion
                }
                else
                {
                    #region Exact Binding
                    if ((bindingFlags & BindingFlags.ExactBinding) != 0)
                    {
                        // Legacy behavior is to ignore ExactBinding when InvokeMember is specified.
                        // Why filter by InvokeMember? If the answer is we leave this to the binder then why not leave
                        // all the rest of this  to the binder too? Further, what other semanitc would the binder
                        // use for BindingFlags.ExactBinding besides this one? Further, why not include CreateInstance 
                        // in this if statement? That's just InvokeMethod with a constructor, right?
                        if ((bindingFlags & (BindingFlags.InvokeMethod)) == 0)
                        {
                            for(int i = 0; i < parameterInfos.Length; i ++)
                            {
                                // a null argument type implies a null arg which is always a perfect match
                                if ((object)argumentTypes[i] != null && !argumentTypes[i].MatchesParameterTypeExactly(parameterInfos[i]))
                                    return false;
                            }
                        }
                    }
                    #endregion
                }
            }
            #endregion
        
            return true;
        }

        #endregion

        #endregion

        #region Private Data Members

        internal static readonly RuntimeType ValueType = (RuntimeType)typeof(System.ValueType);
        internal static readonly RuntimeType EnumType = (RuntimeType)typeof(System.Enum);

        private static readonly RuntimeType ObjectType = (RuntimeType)typeof(System.Object);
        private static readonly RuntimeType StringType = (RuntimeType)typeof(System.String);
        private static readonly RuntimeType DelegateType = (RuntimeType)typeof(System.Delegate);

        #endregion

        #region Constructor
        internal RuntimeType() { throw new NotSupportedException(); }
        #endregion

        #region Type Overrides

        #region Get XXXInfo Candidates
        private ListBuilder<MethodInfo> GetMethodCandidates(
            String name, BindingFlags bindingAttr, CallingConventions callConv,
            Type[] types, int genericParamCount, bool allowPrefixLookup)
        {
            bool prefixLookup, ignoreCase;
            MemberListType listType;
            FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out ignoreCase, out listType);

            RuntimeMethodInfo[] cache = GetMethodsByName (name, bindingAttr, listType, this);
            ListBuilder<MethodInfo> candidates = new ListBuilder<MethodInfo>(cache.Length);

            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeMethodInfo methodInfo = cache[i];
				if (genericParamCount != -1) {
					bool is_generic = methodInfo.IsGenericMethod;
					if (genericParamCount == 0 && is_generic)
						continue;
					else if (genericParamCount > 0 && !is_generic)
						continue;
					var args = methodInfo.GetGenericArguments ();
					if (args.Length != genericParamCount)
						continue;
				}
                if (FilterApplyMethodInfo(methodInfo, bindingAttr, callConv, types) &&
                    (!prefixLookup || FilterApplyPrefixLookup(methodInfo, name, ignoreCase)))
                {
                    candidates.Add(methodInfo);
                }
            }

            return candidates;
        }

        private ListBuilder<ConstructorInfo> GetConstructorCandidates(
            string name, BindingFlags bindingAttr, CallingConventions callConv, 
            Type[] types, bool allowPrefixLookup)
        {
            bool prefixLookup, ignoreCase;
            MemberListType listType;
            FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out ignoreCase, out listType);

            if (!string.IsNullOrEmpty (name) && name != ConstructorInfo.ConstructorName && name != ConstructorInfo.TypeConstructorName)
                return new ListBuilder<ConstructorInfo> (0);
            RuntimeConstructorInfo[] cache = GetConstructors_internal (bindingAttr, this);
            ListBuilder<ConstructorInfo> candidates = new ListBuilder<ConstructorInfo>(cache.Length);
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeConstructorInfo constructorInfo = cache[i];
                if (FilterApplyConstructorInfo(constructorInfo, bindingAttr, callConv, types) &&
                    (!prefixLookup || FilterApplyPrefixLookup(constructorInfo, name, ignoreCase)))
                {                    
                    candidates.Add(constructorInfo);
                }
            }

            return candidates;
        }


        private ListBuilder<PropertyInfo> GetPropertyCandidates(
            String name, BindingFlags bindingAttr, Type[] types, bool allowPrefixLookup)
        {           
            bool prefixLookup, ignoreCase;
            MemberListType listType;
            FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out ignoreCase, out listType);

            RuntimePropertyInfo[] cache = GetPropertiesByName (name, bindingAttr, listType, this);
            bindingAttr ^= BindingFlags.DeclaredOnly;

            ListBuilder<PropertyInfo> candidates = new ListBuilder<PropertyInfo>(cache.Length);
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimePropertyInfo propertyInfo = cache[i];
                if ((bindingAttr & propertyInfo.BindingFlags) == propertyInfo.BindingFlags &&
                    (!prefixLookup || FilterApplyPrefixLookup(propertyInfo, name, ignoreCase)) &&
                    (types == null || (propertyInfo.GetIndexParameters().Length == types.Length)))
                {
                    candidates.Add(propertyInfo);
                }
            }

            return candidates;
        }

        private ListBuilder<EventInfo> GetEventCandidates(String name, BindingFlags bindingAttr, bool allowPrefixLookup)
        {
            bool prefixLookup, ignoreCase;
            MemberListType listType;
            FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out ignoreCase, out listType);

            RuntimeEventInfo[] cache = GetEvents_internal (name, bindingAttr, listType, this);
            bindingAttr ^= BindingFlags.DeclaredOnly;

            ListBuilder<EventInfo> candidates = new ListBuilder<EventInfo>(cache.Length);
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeEventInfo eventInfo = cache[i];
                if ((bindingAttr & eventInfo.BindingFlags) == eventInfo.BindingFlags &&
                    (!prefixLookup || FilterApplyPrefixLookup(eventInfo, name, ignoreCase)))
                {
                    candidates.Add(eventInfo);
                }
            }

            return candidates;
        }

        private ListBuilder<FieldInfo> GetFieldCandidates(String name, BindingFlags bindingAttr, bool allowPrefixLookup)
        {
            bool prefixLookup, ignoreCase;
            MemberListType listType;
            FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out ignoreCase, out listType);

            RuntimeFieldInfo[] cache = GetFields_internal (name, bindingAttr, listType, this);
            bindingAttr ^= BindingFlags.DeclaredOnly;

            ListBuilder<FieldInfo> candidates = new ListBuilder<FieldInfo>(cache.Length);
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeFieldInfo fieldInfo = cache[i];
                if ((bindingAttr & fieldInfo.BindingFlags) == fieldInfo.BindingFlags && 
                    (!prefixLookup || FilterApplyPrefixLookup(fieldInfo, name, ignoreCase)))
                {
                    candidates.Add(fieldInfo);
                }
            }

            return candidates;
        }

        private ListBuilder<Type> GetNestedTypeCandidates(String fullname, BindingFlags bindingAttr, bool allowPrefixLookup)
        {
            bool prefixLookup, ignoreCase;
            bindingAttr &= ~BindingFlags.Static;
            string name, ns;
            MemberListType listType;
            SplitName(fullname, out name, out ns);            
            FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out ignoreCase, out listType);

            RuntimeType[] cache = GetNestedTypes_internal (name, bindingAttr, listType);
            ListBuilder<Type> candidates = new ListBuilder<Type>(cache.Length);
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeType nestedClass = cache[i];
                if (FilterApplyType(nestedClass, bindingAttr, name, prefixLookup, ns))
                {
                    candidates.Add(nestedClass);
                }
            }

            return candidates;
        }

        #endregion

        #region Get All XXXInfos
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            return GetMethodCandidates(null, bindingAttr, CallingConventions.Any, null, -1, false).ToArray();
        }

        [ComVisible(true)]
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            return GetConstructorCandidates(null, bindingAttr, CallingConventions.Any, null, false).ToArray();
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            return GetPropertyCandidates(null, bindingAttr, null, false).ToArray();
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            return GetEventCandidates(null, bindingAttr, false).ToArray();
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            return GetFieldCandidates(null, bindingAttr, false).ToArray();
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            return GetNestedTypeCandidates(null, bindingAttr, false).ToArray();
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            ListBuilder<MethodInfo> methods = GetMethodCandidates(null, bindingAttr, CallingConventions.Any, null, -1, false);
            ListBuilder<ConstructorInfo> constructors = GetConstructorCandidates(null, bindingAttr, CallingConventions.Any, null, false);
            ListBuilder<PropertyInfo> properties = GetPropertyCandidates(null, bindingAttr, null, false);
            ListBuilder<EventInfo> events = GetEventCandidates(null, bindingAttr, false);
            ListBuilder<FieldInfo> fields = GetFieldCandidates(null, bindingAttr, false);
            ListBuilder<Type> nestedTypes = GetNestedTypeCandidates(null, bindingAttr, false);
            // Interfaces are excluded from the result of GetMembers

            MemberInfo[] members = new MemberInfo[
                methods.Count +
                constructors.Count +
                properties.Count +
                events.Count +
                fields.Count +
                nestedTypes.Count];

            int i = 0;
            methods.CopyTo(members, i); i += methods.Count;
            constructors.CopyTo(members, i); i += constructors.Count;
            properties.CopyTo(members, i); i += properties.Count;
            events.CopyTo(members, i); i += events.Count;
            fields.CopyTo(members, i); i += fields.Count;
            nestedTypes.CopyTo(members, i); i += nestedTypes.Count;
            Contract.Assert(i == members.Length);

            return members;
        }

        #endregion

        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
		{
			return GetMethodImpl (name, -1, bindingAttr, binder, callConvention, types, modifiers);
		}

        protected override MethodInfo GetMethodImpl(String name, int genericParamCount,
            BindingFlags bindingAttr, Binder? binder, CallingConventions callConv, 
            Type[]? types, ParameterModifier[]? modifiers) 
        {       
            ListBuilder<MethodInfo> candidates = GetMethodCandidates(name, bindingAttr, callConv, types, genericParamCount, false);
            if (candidates.Count == 0) 
                return null;

            if (types == null || types.Length == 0) 
            {
                MethodInfo firstCandidate = candidates[0];

                if (candidates.Count == 1)
                {
                    return firstCandidate;
                }
                else if (types == null) 
                { 
                    for (int j = 1; j < candidates.Count; j++)
                    {
                        MethodInfo methodInfo = candidates[j];
                        if (!System.DefaultBinder.CompareMethodSig (methodInfo, firstCandidate))
                            throw new AmbiguousMatchException(Environment.GetResourceString("Arg_AmbiguousMatchException"));
                    }

                    // All the methods have the exact same name and sig so return the most derived one.
                    return System.DefaultBinder.FindMostDerivedNewSlotMeth(candidates.ToArray(), candidates.Count) as MethodInfo;
                }
            }   

            if (binder == null) 
                binder = DefaultBinder;

            return binder.SelectMethod(bindingAttr, candidates.ToArray(), types, modifiers) as MethodInfo;                  
        }

        protected override ConstructorInfo GetConstructorImpl(
            BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, 
            Type[] types, ParameterModifier[]? modifiers)
        {
            ListBuilder<ConstructorInfo> candidates = GetConstructorCandidates(null, bindingAttr, CallingConventions.Any, types, false);

            if (candidates.Count == 0)
                return null;
            
            if (types.Length == 0 && candidates.Count == 1) 
            {
                ConstructorInfo firstCandidate = candidates[0];

                ParameterInfo[] parameters = firstCandidate.GetParametersNoCopy();
                if (parameters == null || parameters.Length == 0) 
                {
                    return firstCandidate;
                }
            }

            if ((bindingAttr & BindingFlags.ExactBinding) != 0)
                return System.DefaultBinder.ExactBinding(candidates.ToArray(), types, modifiers) as ConstructorInfo;

            if (binder == null)
                binder = DefaultBinder;

            return binder.SelectMethod(bindingAttr, candidates.ToArray(), types, modifiers) as ConstructorInfo;
        }


        protected override PropertyInfo GetPropertyImpl(
            String name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers) 
        {
            if (name == null) throw new ArgumentNullException();
            Contract.EndContractBlock();

            ListBuilder<PropertyInfo> candidates = GetPropertyCandidates(name, bindingAttr, types, false);

            if (candidates.Count == 0)
                return null;
            
            if (types == null || types.Length == 0) 
            {
                // no arguments
                if (candidates.Count == 1) 
                {
                    PropertyInfo firstCandidate = candidates[0];

                    if ((object)returnType != null && !returnType.IsEquivalentTo(firstCandidate.PropertyType))
                        return null;

                    return firstCandidate;
                }
                else 
                {
                    if ((object)returnType == null)
                        // if we are here we have no args or property type to select over and we have more than one property with that name
                        throw new AmbiguousMatchException(Environment.GetResourceString("Arg_AmbiguousMatchException"));
                }
            }
            
            if ((bindingAttr & BindingFlags.ExactBinding) != 0)
                return System.DefaultBinder.ExactPropertyBinding(candidates.ToArray(), returnType, types, modifiers);

            if (binder == null)
                binder = DefaultBinder;
            
            return binder.SelectProperty(bindingAttr, candidates.ToArray(), returnType, types, modifiers);
        }

        public override EventInfo GetEvent(String name, BindingFlags bindingAttr) 
        {
            if (name == null) throw new ArgumentNullException();
            Contract.EndContractBlock();

            bool ignoreCase;
            MemberListType listType;
            FilterHelper(bindingAttr, ref name, out ignoreCase, out listType);

            RuntimeEventInfo[] cache = GetEvents_internal (name, bindingAttr, listType, this);
            EventInfo match = null;

            bindingAttr ^= BindingFlags.DeclaredOnly;

            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeEventInfo eventInfo = cache[i];
                if ((bindingAttr & eventInfo.BindingFlags) == eventInfo.BindingFlags)
                {
                    if (match != null)
                        throw new AmbiguousMatchException(Environment.GetResourceString("Arg_AmbiguousMatchException"));

                    match = eventInfo;
                }
            }

            return match;
        }

        public override FieldInfo GetField(String name, BindingFlags bindingAttr) 
        {
            if (name == null) throw new ArgumentNullException();
            Contract.EndContractBlock();

            bool ignoreCase;
            MemberListType listType;
            FilterHelper(bindingAttr, ref name, out ignoreCase, out listType);

            RuntimeFieldInfo[] cache = GetFields_internal (name, bindingAttr, listType, this);
            FieldInfo match = null;

            bindingAttr ^= BindingFlags.DeclaredOnly;
            bool multipleStaticFieldMatches = false;

            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeFieldInfo fieldInfo = cache[i];
                if ((bindingAttr & fieldInfo.BindingFlags) == fieldInfo.BindingFlags)
                {
                    if (match != null)
                    {
                        if (Object.ReferenceEquals(fieldInfo.DeclaringType, match.DeclaringType))
                            throw new AmbiguousMatchException(Environment.GetResourceString("Arg_AmbiguousMatchException"));

                        if ((match.DeclaringType.IsInterface == true) && (fieldInfo.DeclaringType.IsInterface == true))
                            multipleStaticFieldMatches = true;
                    }
                
                    if (match == null || fieldInfo.DeclaringType.IsSubclassOf(match.DeclaringType) || match.DeclaringType.IsInterface)
                        match = fieldInfo;
                }
            }

            if (multipleStaticFieldMatches && match.DeclaringType.IsInterface)
                throw new AmbiguousMatchException(Environment.GetResourceString("Arg_AmbiguousMatchException"));

            return match;
        }

        public override Type GetInterface(String fullname, bool ignoreCase) 
        {
            if (fullname == null) throw new ArgumentNullException();
            Contract.EndContractBlock();

            BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.NonPublic;
            
            bindingAttr &= ~BindingFlags.Static;

            if (ignoreCase)
                bindingAttr |= BindingFlags.IgnoreCase;

            string name, ns;
            MemberListType listType;
            SplitName(fullname, out name, out ns);            
            FilterHelper(bindingAttr, ref name, out ignoreCase, out listType);

            List<RuntimeType> list = null;
            var nameComparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            foreach (RuntimeType t in GetInterfaces ()) {

                if (!String.Equals(t.Name, name, nameComparison)) {
                       continue;
                }

                if (list == null)
                    list = new List<RuntimeType> (2);

                list.Add (t);
            }

            if (list == null)
                return null;

            var cache = list.ToArray ();
            RuntimeType match = null;

            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeType iface = cache[i];
                if (FilterApplyType(iface, bindingAttr, name, false, ns))
                {
                    if (match != null)
                        throw new AmbiguousMatchException(Environment.GetResourceString("Arg_AmbiguousMatchException"));

                    match = iface;
                }
            }

            return match;
        }

        public override Type GetNestedType(String fullname, BindingFlags bindingAttr) 
        {
            if (fullname == null) throw new ArgumentNullException();
            Contract.EndContractBlock();

            bool ignoreCase;
            bindingAttr &= ~BindingFlags.Static;
            string name, ns;
            MemberListType listType;
            SplitName(fullname, out name, out ns);            
            FilterHelper(bindingAttr, ref name, out ignoreCase, out listType);
            RuntimeType[] cache = GetNestedTypes_internal (name, bindingAttr, listType);
            RuntimeType match = null;

            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeType nestedType = cache[i];
                if (FilterApplyType(nestedType, bindingAttr, name, false, ns))
                {
                    if (match != null)
                        throw new AmbiguousMatchException(Environment.GetResourceString("Arg_AmbiguousMatchException"));

                    match = nestedType;
                }
            }

            return match;
        }

        public override MemberInfo[] GetMember(String name, MemberTypes type, BindingFlags bindingAttr) 
        {
            if (name == null) throw new ArgumentNullException();
            Contract.EndContractBlock();

            ListBuilder<MethodInfo> methods = new ListBuilder<MethodInfo>();
            ListBuilder<ConstructorInfo> constructors = new ListBuilder<ConstructorInfo>();
            ListBuilder<PropertyInfo> properties = new ListBuilder<PropertyInfo>();
            ListBuilder<EventInfo> events = new ListBuilder<EventInfo>();
            ListBuilder<FieldInfo> fields = new ListBuilder<FieldInfo>(); 
            ListBuilder<Type> nestedTypes = new ListBuilder<Type>();

            int totalCount = 0;

            // Methods
            if ((type & MemberTypes.Method) != 0)
            {
                methods = GetMethodCandidates(name, bindingAttr, CallingConventions.Any, null, -1, true);
                if (type == MemberTypes.Method)
                    return methods.ToArray();
                totalCount += methods.Count;
            }

            // Constructors
            if ((type & MemberTypes.Constructor) != 0)
            {
                constructors = GetConstructorCandidates(name, bindingAttr, CallingConventions.Any, null, true);
                if (type == MemberTypes.Constructor)
                    return constructors.ToArray();
                totalCount += constructors.Count;
            }

            // Properties
            if ((type & MemberTypes.Property) != 0)
            {
                properties = GetPropertyCandidates(name, bindingAttr, null, true);
                if (type == MemberTypes.Property)
                    return properties.ToArray();
                totalCount += properties.Count;
            }

            // Events
            if ((type & MemberTypes.Event) != 0)
            {
                events = GetEventCandidates(name, bindingAttr, true);
                if (type == MemberTypes.Event)
                    return events.ToArray();
                totalCount += events.Count;
            }

            // Fields
            if ((type & MemberTypes.Field) != 0)
            {
                fields = GetFieldCandidates(name, bindingAttr, true);
                if (type == MemberTypes.Field)
                    return fields.ToArray();
                totalCount += fields.Count;
            }

            // NestedTypes
            if ((type & (MemberTypes.NestedType | MemberTypes.TypeInfo)) != 0)
            {
                nestedTypes = GetNestedTypeCandidates(name, bindingAttr, true);
                if (type == MemberTypes.NestedType || type == MemberTypes.TypeInfo)
                    return nestedTypes.ToArray();
                totalCount += nestedTypes.Count;
            }

            MemberInfo[] compressMembers = (type == (MemberTypes.Method | MemberTypes.Constructor)) ?
                new MethodBase[totalCount] : new MemberInfo[totalCount];

            int i = 0;
            methods.CopyTo(compressMembers, i); i += methods.Count;
            constructors.CopyTo(compressMembers, i); i += constructors.Count;
            properties.CopyTo(compressMembers, i); i += properties.Count;
            events.CopyTo(compressMembers, i); i += events.Count;
            fields.CopyTo(compressMembers, i); i += fields.Count;
            nestedTypes.CopyTo(compressMembers, i); i += nestedTypes.Count;
            Contract.Assert(i == compressMembers.Length);

            return compressMembers;
        }
        #endregion

        #region Identity

        public override Module Module
        {
            get
            {
                return GetRuntimeModule();
            }
        }

        internal RuntimeModule GetRuntimeModule()
        {
            return RuntimeTypeHandle.GetModule(this);
        }

        public override Assembly Assembly 
        {
            get 
            {
                return GetRuntimeAssembly();
            }
        }

        internal RuntimeAssembly GetRuntimeAssembly()
        {
            return RuntimeTypeHandle.GetAssembly(this);
        }

        public override RuntimeTypeHandle TypeHandle 
        {
            get 
            {
                return new RuntimeTypeHandle(this);
            }
        }
        
        #endregion

        #region Hierarchy
        [System.Security.SecuritySafeCritical]  // auto-generated
        public override bool IsInstanceOfType(Object? o)
        {
            return RuntimeTypeHandle.IsInstanceOfType(this, o);
        }

        public override bool IsAssignableFrom(System.Reflection.TypeInfo? typeInfo){
            if(typeInfo==null) return false;
            return IsAssignableFrom(typeInfo.AsType());
        }

        public override bool IsAssignableFrom(Type? c)
        {
            if ((object)c == null)
                return false;

            if (Object.ReferenceEquals(c, this))
                return true;

            RuntimeType fromType = c.UnderlyingSystemType as RuntimeType;

            // For runtime type, let the VM decide.
            if (fromType != null)
            {
                // both this and c (or their underlying system types) are runtime types
                return RuntimeTypeHandle.CanCastTo(fromType, this);
            }
#if !FULL_AOT_RUNTIME
            // Special case for TypeBuilder to be backward-compatible.
            if (RuntimeFeature.IsDynamicCodeSupported && c is System.Reflection.Emit.TypeBuilder)
            {
                // If c is a subclass of this class, then c can be cast to this type.
                if (c.IsSubclassOf(this))
                    return true;

                if (this.IsInterface)
                {
                    return c.ImplementInterface(this);
                }
                else if (this.IsGenericParameter)
                {
                    Type[] constraints = GetGenericParameterConstraints();
                    for (int i = 0; i < constraints.Length; i++)
                        if (!constraints[i].IsAssignableFrom(c))
                            return false;

                    return true;
                }
            }
#endif
            // For anything else we return false.
            return false;
        }

        // Reflexive, symmetric, transitive.
        public override bool IsEquivalentTo(Type? other)
        {
            RuntimeType otherRtType = other as RuntimeType;
            if ((object)otherRtType == null)
                return false;

            if (otherRtType == this)
                return true;

            // It's not worth trying to perform further checks in managed
            // as they would lead to FCalls anyway.
            return RuntimeTypeHandle.IsEquivalentTo(this, otherRtType);
        }

        public override Type BaseType => GetBaseType();

        private RuntimeType GetBaseType()
        {
            if (IsInterface)
                return null;

            if (RuntimeTypeHandle.IsGenericVariable(this))
            {
                Type[] constraints = GetGenericParameterConstraints();

                RuntimeType baseType = ObjectType;

                for (int i = 0; i < constraints.Length; i++)
                {
                    RuntimeType constraint = (RuntimeType)constraints[i];

                    if (constraint.IsInterface)
                        continue;

                    if (constraint.IsGenericParameter)
                    {
                        GenericParameterAttributes special;
                        special = constraint.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;

                        if ((special & GenericParameterAttributes.ReferenceTypeConstraint) == 0 &&
                            (special & GenericParameterAttributes.NotNullableValueTypeConstraint) == 0)
                            continue;
                    }

                    baseType = constraint;
                }

                if (baseType == ObjectType)
                {
                    GenericParameterAttributes special;
                    special = GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;
                    if ((special & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
                        baseType = ValueType;
                }

                return baseType;
            }

            return RuntimeTypeHandle.GetBaseType(this);
        }

        public override Type UnderlyingSystemType => this;

        #endregion

        #region Attributes
        [System.Security.SecuritySafeCritical]  // auto-generated
        protected override TypeAttributes GetAttributeFlagsImpl() 
        {
            return RuntimeTypeHandle.GetAttributes(this);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        protected override bool IsContextfulImpl() 
        {
            return RuntimeTypeHandle.IsContextful(this);
        }

        protected override bool IsByRefImpl() 
        {
            return RuntimeTypeHandle.IsByRef(this);
        }

        protected override bool IsPrimitiveImpl() 
        {
            return RuntimeTypeHandle.IsPrimitive(this);
        }

        protected override bool IsPointerImpl() 
        {
            return RuntimeTypeHandle.IsPointer(this);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        protected override bool IsCOMObjectImpl() 
        {
            return RuntimeTypeHandle.IsComObject(this, false);
        }

        internal bool IsDelegate()
        {
            return GetBaseType() == typeof(System.MulticastDelegate);
        }

        protected override bool IsValueTypeImpl()
        {
            // We need to return true for generic parameters with the ValueType constraint.
            // So we cannot use the faster RuntimeTypeHandle.IsValueType because it returns 
            // false for all generic parameters.
            if (this == typeof(ValueType) || this == typeof(Enum)) 
                return false;

            return IsSubclassOf(typeof(ValueType));
        }

        public override bool IsEnum => GetBaseType() == EnumType;

        protected override bool HasElementTypeImpl() 
        {
            return RuntimeTypeHandle.HasElementType(this);
        }

        public override GenericParameterAttributes GenericParameterAttributes
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                if (!IsGenericParameter)
                    throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericParameter"));
                Contract.EndContractBlock();

                return GetGenericParameterAttributes ();
            }
        }

        #endregion

        #region Arrays

        protected override bool IsArrayImpl() 
        {
            return RuntimeTypeHandle.IsArray(this);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override int GetArrayRank() 
        {
            if (!IsArrayImpl())
                throw new ArgumentException(Environment.GetResourceString("Argument_HasToBeArrayClass"));

            return RuntimeTypeHandle.GetArrayRank(this);
        }

        public override Type GetElementType() 
        {
            return RuntimeTypeHandle.GetElementType(this);
        }
        #endregion

        #region Enums
        public override string[] GetEnumNames()
        {
            if (!IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
            Contract.EndContractBlock();

            String[] ret = Enum.InternalGetNames(this);

            // Make a copy since we can't hand out the same array since users can modify them
            String[] retVal = new String[ret.Length];

            Array.Copy(ret, retVal, ret.Length);

            return retVal;
        }

        [SecuritySafeCritical]
        public override Array GetEnumValues()
        {
            if (!IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
            Contract.EndContractBlock();

            // Get all of the values
            ulong[] values = Enum.InternalGetValues(this);

            // Create a generic Array
            Array ret = Array.CreateInstance(this, values.Length);

            for (int i = 0; i < values.Length; i++)
            {
                Object val = Enum.ToObject(this, values[i]);
                ret.SetValue(val, i);
            }

            return ret;
        }

        public override Type GetEnumUnderlyingType()
        {
            if (!IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
            Contract.EndContractBlock();

            return Enum.InternalGetUnderlyingType(this);
        }

        public override bool IsEnumDefined(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            Contract.EndContractBlock();

            // Check if both of them are of the same type
            RuntimeType valueType = (RuntimeType)value.GetType();

            // If the value is an Enum then we need to extract the underlying value from it
            if (valueType.IsEnum)
            {
                if (!valueType.IsEquivalentTo(this))
                    throw new ArgumentException(Environment.GetResourceString("Arg_EnumAndObjectMustBeSameType", valueType.ToString(), this.ToString()));

                valueType = (RuntimeType)valueType.GetEnumUnderlyingType();
            }

            // If a string is passed in
            if (valueType == StringType)
            {
                // Get all of the Fields, calling GetHashEntry directly to avoid copying
                string[] names = Enum.InternalGetNames(this);
                if (Array.IndexOf(names, value) >= 0)
                    return true;
                else
                    return false;
            }

            // If an enum or integer value is passed in
            if (Type.IsIntegerType(valueType))
            {
                RuntimeType underlyingType = Enum.InternalGetUnderlyingType(this);
                if (underlyingType != valueType)
                    throw new ArgumentException(Environment.GetResourceString("Arg_EnumUnderlyingTypeAndObjectMustBeSameType", valueType.ToString(), underlyingType.ToString()));

                ulong[] ulValues = Enum.InternalGetValues(this);
                ulong ulValue = Enum.ToUInt64(value);

                return (Array.BinarySearch(ulValues, ulValue) >= 0);
            }
            else
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_UnknownEnumType"));
            }
        }

        public override string GetEnumName(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            Contract.EndContractBlock();

            Type valueType = value.GetType();

            if (!(valueType.IsEnum || IsIntegerType(valueType)))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnumBaseTypeOrEnum"), "value");

            ulong[] ulValues = Enum.InternalGetValues(this);
            ulong ulValue = Enum.ToUInt64(value);
            int index = Array.BinarySearch(ulValues, ulValue);

            if (index >= 0)
            {
                string[] names = Enum.InternalGetNames(this);
                return names[index];
            }

            return null;
        }
        #endregion

        #region Generics

        internal RuntimeType[] GetGenericArgumentsInternal()
        {
            return (RuntimeType[]) GetGenericArgumentsInternal (true);
        }

        public override Type[] GetGenericArguments() 
        {
            Type[] types = GetGenericArgumentsInternal (false);

            if (types == null)
                types = Array.Empty<Type> ();

            return types;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override Type MakeGenericType(Type[] instantiation)
        {
            if (instantiation == null)
                throw new ArgumentNullException("instantiation");
            Contract.EndContractBlock();

            RuntimeType[] instantiationRuntimeType = new RuntimeType[instantiation.Length];

            if (!IsGenericTypeDefinition)
                throw new InvalidOperationException(
                    Environment.GetResourceString("Arg_NotGenericTypeDefinition", this));

            if (GetGenericArguments().Length != instantiation.Length)
                throw new ArgumentException(Environment.GetResourceString("Argument_GenericArgsCount"), "instantiation");

            for (int i = 0; i < instantiation.Length; i ++)
            {
                Type instantiationElem = instantiation[i];
                if (instantiationElem == null)
                    throw new ArgumentNullException();

                RuntimeType rtInstantiationElem = instantiationElem as RuntimeType;

                if (rtInstantiationElem == null)
                {
                    if (instantiationElem.IsSignatureType)
                        return MakeGenericSignatureType (this, instantiation);
                    Type[] instantiationCopy = new Type[instantiation.Length];
                    for (int iCopy = 0; iCopy < instantiation.Length; iCopy++)
                        instantiationCopy[iCopy] = instantiation[iCopy];
                    instantiation = instantiationCopy;
                    
                    throw new NotImplementedException ();
                }

                instantiationRuntimeType[i] = rtInstantiationElem;
            }

            RuntimeType[] genericParameters = GetGenericArgumentsInternal();

            SanityCheckGenericArguments(instantiationRuntimeType, genericParameters);

            Type ret = null;
            ret = MakeGenericType (this, instantiationRuntimeType);
            if (ret == null)
                throw new TypeLoadException ();
            return ret;
        }

        public override bool IsGenericTypeDefinition
        {
            get { return RuntimeTypeHandle.IsGenericTypeDefinition(this); }
        }

        public override bool IsGenericParameter
        {
            get { return RuntimeTypeHandle.IsGenericVariable(this); }
        }

        public override int GenericParameterPosition
        {
            get 
            {
                if (!IsGenericParameter)
                    throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericParameter"));
                Contract.EndContractBlock();
                return GetGenericParameterPosition ();
            }
        }

        public override Type GetGenericTypeDefinition() 
        {
            if (!IsGenericType)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotGenericType"));
            Contract.EndContractBlock();

            return RuntimeTypeHandle.GetGenericTypeDefinition(this);
        }

        public override bool IsGenericType
        {
            get { return RuntimeTypeHandle.HasInstantiation(this); }
        }

        public override bool IsConstructedGenericType
        {
            get { return IsGenericType && !IsGenericTypeDefinition; }
        }

        #endregion

        #region Invoke Member
        private const BindingFlags MemberBindingMask        = (BindingFlags)0x000000FF;
        private const BindingFlags InvocationMask           = (BindingFlags)0x0000FF00;
        private const BindingFlags BinderNonCreateInstance  = BindingFlags.InvokeMethod | BinderGetSetField | BinderGetSetProperty;
        private const BindingFlags BinderGetSetProperty     = BindingFlags.GetProperty | BindingFlags.SetProperty;
        private const BindingFlags BinderSetInvokeProperty  = BindingFlags.InvokeMethod | BindingFlags.SetProperty;
        private const BindingFlags BinderGetSetField        = BindingFlags.GetField | BindingFlags.SetField;
        private const BindingFlags BinderSetInvokeField     = BindingFlags.SetField | BindingFlags.InvokeMethod;
        private const BindingFlags BinderNonFieldGetSet     = (BindingFlags)0x00FFF300;
        private const BindingFlags ClassicBindingMask       = 
            BindingFlags.InvokeMethod | BindingFlags.GetProperty | BindingFlags.SetProperty | 
            BindingFlags.PutDispProperty | BindingFlags.PutRefDispProperty;
        private static RuntimeType s_typedRef = (RuntimeType)typeof(TypedReference);

        // GetDefaultMembers
        // This will return a MemberInfo that has been marked with the [DefaultMemberAttribute]
        public override MemberInfo[] GetDefaultMembers()
        {
            // See if we have cached the default member name
            MemberInfo[] members = null;

            String defaultMemberName = GetDefaultMemberName();
            if (defaultMemberName != null)
            {
                members = GetMember(defaultMemberName);
            }

            if (members == null)
                members = Array.Empty<MemberInfo> ();

            return members;
        }
        
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override Object InvokeMember(
            String name, BindingFlags bindingFlags, Binder? binder, Object? target, 
            Object?[]? providedArgs, ParameterModifier[]? modifiers, CultureInfo? culture, String[]? namedParams) 
        {
            if (IsGenericParameter)
                throw new InvalidOperationException(Environment.GetResourceString("Arg_GenericParameter"));
            Contract.EndContractBlock();
        
            #region Preconditions
            if ((bindingFlags & InvocationMask) == 0)
                // "Must specify binding flags describing the invoke operation required."
                throw new ArgumentException(Environment.GetResourceString("Arg_NoAccessSpec"),"bindingFlags");

            // Provide a default binding mask if none is provided 
            if ((bindingFlags & MemberBindingMask) == 0) 
            {
                bindingFlags |= BindingFlags.Instance | BindingFlags.Public;

                if ((bindingFlags & BindingFlags.CreateInstance) == 0) 
                    bindingFlags |= BindingFlags.Static;
            }

            // There must not be more named parameters than provided arguments
            if (namedParams != null)
            {
                if (providedArgs != null)
                {
                    if (namedParams.Length > providedArgs.Length)
                        // "Named parameter array can not be bigger than argument array."
                        throw new ArgumentException(Environment.GetResourceString("Arg_NamedParamTooBig"), "namedParams");
                }
                else
                {
                    if (namedParams.Length != 0)
                        // "Named parameter array can not be bigger than argument array."
                        throw new ArgumentException(Environment.GetResourceString("Arg_NamedParamTooBig"), "namedParams");
                }
            }
            #endregion

            #region Check that any named paramters are not null
            if (namedParams != null && Array.IndexOf(namedParams, null) != -1)
                // "Named parameter value must not be null."
                throw new ArgumentException(Environment.GetResourceString("Arg_NamedParamNull"),"namedParams");
            #endregion

            int argCnt = (providedArgs != null) ? providedArgs.Length : 0;
            
            #region Get a Binder
            if (binder == null)
                binder = DefaultBinder;

            #endregion
            
            #region Delegate to Activator.CreateInstance
            if ((bindingFlags & BindingFlags.CreateInstance) != 0) 
            {
                if ((bindingFlags & BindingFlags.CreateInstance) != 0 && (bindingFlags & BinderNonCreateInstance) != 0)
                    // "Can not specify both CreateInstance and another access type."
                    throw new ArgumentException(Environment.GetResourceString("Arg_CreatInstAccess"),"bindingFlags");

                return Activator.CreateInstance(this, bindingFlags, binder, providedArgs, culture);
            }
            #endregion

            // PutDispProperty and\or PutRefDispProperty ==> SetProperty.
            if ((bindingFlags & (BindingFlags.PutDispProperty | BindingFlags.PutRefDispProperty)) != 0)
                bindingFlags |= BindingFlags.SetProperty;

            #region Name
            if (name == null)
                throw new ArgumentNullException("name");
                
            if (name.Length == 0 || name.Equals(@"[DISPID=0]")) 
            {
                name = GetDefaultMemberName();

                if (name == null) 
                {
                    // in InvokeMember we always pretend there is a default member if none is provided and we make it ToString
                    name = "ToString";
                }
            }
            #endregion

            #region GetField or SetField
            bool IsGetField = (bindingFlags & BindingFlags.GetField) != 0;
            bool IsSetField = (bindingFlags & BindingFlags.SetField) != 0;

            if (IsGetField || IsSetField)
            {
                #region Preconditions
                if (IsGetField)
                {
                    if (IsSetField)
                        // "Can not specify both Get and Set on a field."
                        throw new ArgumentException(Environment.GetResourceString("Arg_FldSetGet"),"bindingFlags");

                    if ((bindingFlags & BindingFlags.SetProperty) != 0)
                        // "Can not specify both GetField and SetProperty."
                        throw new ArgumentException(Environment.GetResourceString("Arg_FldGetPropSet"),"bindingFlags");
                }
                else
                {
                    Contract.Assert(IsSetField);

                    if (providedArgs == null) 
                        throw new ArgumentNullException("providedArgs");

                    if ((bindingFlags & BindingFlags.GetProperty) != 0)
                        // "Can not specify both SetField and GetProperty."
                        throw new ArgumentException(Environment.GetResourceString("Arg_FldSetPropGet"),"bindingFlags");

                    if ((bindingFlags & BindingFlags.InvokeMethod) != 0)
                        // "Can not specify Set on a Field and Invoke on a method."
                        throw new ArgumentException(Environment.GetResourceString("Arg_FldSetInvoke"),"bindingFlags");
                }
                #endregion
                        
                #region Lookup Field
                FieldInfo selFld = null;                
                FieldInfo[] flds = GetMember(name, MemberTypes.Field, bindingFlags) as FieldInfo[];

                Contract.Assert(flds != null);

                if (flds.Length == 1)
                {
                    selFld = flds[0];
                }
                else if (flds.Length > 0)
                {
                    selFld = binder.BindToField(bindingFlags, flds, IsGetField ? Empty.Value : providedArgs[0], culture);
                }
                #endregion
                
                if (selFld != null) 
                {
                    #region Invocation on a field
                    if (selFld.FieldType.IsArray || Object.ReferenceEquals(selFld.FieldType, typeof(System.Array)))
                    {
                        #region Invocation of an array Field
                        int idxCnt;

                        if ((bindingFlags & BindingFlags.GetField) != 0) 
                        {
                            idxCnt = argCnt;                                                        
                        }
                        else
                        {
                            idxCnt = argCnt - 1;
                        }

                        if (idxCnt > 0) 
                        {
                            // Verify that all of the index values are ints
                            int[] idx = new int[idxCnt];
                            for (int i=0;i<idxCnt;i++) 
                            {
                                try 
                                {
                                    idx[i] = ((IConvertible)providedArgs[i]).ToInt32(null);
                                }
                                catch (InvalidCastException)
                                {
                                    throw new ArgumentException(Environment.GetResourceString("Arg_IndexMustBeInt"));
                                }
                            }
                            
                            // Set or get the value...
                            Array a = (Array) selFld.GetValue(target);
                            
                            // Set or get the value in the array
                            if ((bindingFlags & BindingFlags.GetField) != 0) 
                            {
                                return a.GetValue(idx);
                            }
                            else 
                            {
                                a.SetValue(providedArgs[idxCnt],idx);
                                return null;
                            }                                               
                        }
                        #endregion
                    }
                    
                    if (IsGetField)
                    {
                        #region Get the field value
                        if (argCnt != 0)
                            throw new ArgumentException(Environment.GetResourceString("Arg_FldGetArgErr"),"bindingFlags");

                        return selFld.GetValue(target);
                        #endregion
                    }
                    else
                    {
                        #region Set the field Value
                        if (argCnt != 1)
                            throw new ArgumentException(Environment.GetResourceString("Arg_FldSetArgErr"),"bindingFlags");

                        selFld.SetValue(target,providedArgs[0],bindingFlags,binder,culture);

                        return null;
                        #endregion
                    }
                    #endregion
                }

                if ((bindingFlags & BinderNonFieldGetSet) == 0) 
                    throw new MissingFieldException(FullName, name);
            }
            #endregion                    

            #region Caching Logic
            /*
            bool useCache = false;

            // Note that when we add something to the cache, we are careful to ensure
            // that the actual providedArgs matches the parameters of the method.  Otherwise,
            // some default argument processing has occurred.  We don't want anyone
            // else with the same (insufficient) number of actual arguments to get a
            // cache hit because then they would bypass the default argument processing
            // and the invocation would fail.
            if (bDefaultBinder && namedParams == null && argCnt < 6)
                useCache = true;

            if (useCache)
            {
                MethodBase invokeMethod = GetMethodFromCache (name, bindingFlags, argCnt, providedArgs);

                if (invokeMethod != null)
                    return ((MethodInfo) invokeMethod).Invoke(target, bindingFlags, binder, providedArgs, culture);
            }
            */
            #endregion

            #region Property PreConditions
            // @Legacy - This is RTM behavior
            bool isGetProperty = (bindingFlags & BindingFlags.GetProperty) != 0;
            bool isSetProperty = (bindingFlags & BindingFlags.SetProperty) != 0;

            if (isGetProperty || isSetProperty) 
            {
                #region Preconditions
                if (isGetProperty)
                {
                    Contract.Assert(!IsSetField);

                    if (isSetProperty)
                        throw new ArgumentException(Environment.GetResourceString("Arg_PropSetGet"), "bindingFlags");
                }
                else
                {
                    Contract.Assert(isSetProperty);

                    Contract.Assert(!IsGetField);
                    
                    if ((bindingFlags & BindingFlags.InvokeMethod) != 0)
                        throw new ArgumentException(Environment.GetResourceString("Arg_PropSetInvoke"), "bindingFlags");
                }
                #endregion
            }
            #endregion

            MethodInfo[] finalists = null;
            MethodInfo finalist = null;

            #region BindingFlags.InvokeMethod
            if ((bindingFlags & BindingFlags.InvokeMethod) != 0) 
            {
                #region Lookup Methods
                MethodInfo[] semiFinalists = GetMember(name, MemberTypes.Method, bindingFlags) as MethodInfo[];
                List<MethodInfo> results = null;
                
                for(int i = 0; i < semiFinalists.Length; i ++)
                {
                    MethodInfo semiFinalist = semiFinalists[i];
                    Contract.Assert(semiFinalist != null);

                    if (!FilterApplyMethodInfo((RuntimeMethodInfo)semiFinalist, bindingFlags, CallingConventions.Any, new Type[argCnt]))
                        continue;
                    
                    if (finalist == null)
                    {
                        finalist = semiFinalist;
                    }
                    else
                    {
                        if (results == null)
                        {
                            results = new List<MethodInfo>(semiFinalists.Length);
                            results.Add(finalist);
                        }

                        results.Add(semiFinalist);
                    }
                }
                
                if (results != null)
                {
                    Contract.Assert(results.Count > 1);
                    finalists = new MethodInfo[results.Count];
                    results.CopyTo(finalists);
                }
                #endregion
            }
            #endregion
            
            Contract.Assert(finalists == null || finalist != null);

            #region BindingFlags.GetProperty or BindingFlags.SetProperty
            if (finalist == null && isGetProperty || isSetProperty) 
            {
                #region Lookup Property
                PropertyInfo[] semiFinalists = GetMember(name, MemberTypes.Property, bindingFlags) as PropertyInfo[];                        
                List<MethodInfo> results = null;

                for(int i = 0; i < semiFinalists.Length; i ++)
                {
                    MethodInfo semiFinalist = null;

                    if (isSetProperty)
                    {
                        semiFinalist = semiFinalists[i].GetSetMethod(true);
                    }
                    else
                    {
                        semiFinalist = semiFinalists[i].GetGetMethod(true);
                    }

                    if (semiFinalist == null)
                        continue;

                    if (!FilterApplyMethodInfo((RuntimeMethodInfo)semiFinalist, bindingFlags, CallingConventions.Any, new Type[argCnt]))
                        continue;
                    
                    if (finalist == null)
                    {
                        finalist = semiFinalist;
                    }
                    else
                    {
                        if (results == null)
                        {
                            results = new List<MethodInfo>(semiFinalists.Length);
                            results.Add(finalist);
                        }

                        results.Add(semiFinalist);
                    }
                }

                if (results != null)
                {
                    Contract.Assert(results.Count > 1);
                    finalists = new MethodInfo[results.Count];
                    results.CopyTo(finalists);
                }
                #endregion            
            }
            #endregion

            if (finalist != null) 
            {
                #region Invoke
                if (finalists == null && 
                    argCnt == 0 && 
                    finalist.GetParametersNoCopy().Length == 0 && 
                    (bindingFlags & BindingFlags.OptionalParamBinding) == 0)
                {
                    //if (useCache && argCnt == props[0].GetParameters().Length)
                    //    AddMethodToCache(name, bindingFlags, argCnt, providedArgs, props[0]);

                    return finalist.Invoke(target, bindingFlags, binder, providedArgs, culture);
                }
                
                if (finalists == null)
                    finalists = new MethodInfo[] { finalist };

                if (providedArgs == null)
                        providedArgs = Array.Empty<Object>();

                Object state = null;

                
                MethodBase invokeMethod = null;

                try { invokeMethod = binder.BindToMethod(bindingFlags, finalists, ref providedArgs, modifiers, culture, namedParams, out state); }
                catch(MissingMethodException) { }

                if (invokeMethod == null)
                    throw new MissingMethodException(FullName, name);

                //if (useCache && argCnt == invokeMethod.GetParameters().Length)
                //    AddMethodToCache(name, bindingFlags, argCnt, providedArgs, invokeMethod);

                Object result = ((MethodInfo)invokeMethod).Invoke(target, bindingFlags, binder, providedArgs, culture);

                if (state != null)
                    binder.ReorderArgumentArray(ref providedArgs, state);

                return result;
                #endregion
            }
            
            throw new MissingMethodException(FullName, name);
        }        
        #endregion

        #region Object Overrides
        [Pure]
        public override bool Equals(object? obj)
        {
            // ComObjects are identified by the instance of the Type object and not the TypeHandle.
            return obj == (object)this;
        }

        public override int GetHashCode() 
        {
            return RuntimeHelpers.GetHashCode(this);
        }

        public static bool operator ==(RuntimeType left, RuntimeType right)
        {
            return object.ReferenceEquals(left, right);
        }

        public static bool operator !=(RuntimeType left, RuntimeType right)
        {
            return !object.ReferenceEquals(left, right);
        }
        #endregion

        #region ICloneable
        public Object Clone() 
        {
            return this;
        }
        #endregion

        #region ISerializable
        [System.Security.SecurityCritical]  // auto-generated
        public void GetObjectData(SerializationInfo info, StreamingContext context) 
        {
            if (info==null) 
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();

			throw new NotImplementedException ();
        }
        #endregion

        #region ICustomAttributeProvider
        [System.Security.SecuritySafeCritical]  // auto-generated
        public override Object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, inherit);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if ((object)attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();

            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null) 
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"),"attributeType");

            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType, inherit);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if ((object)attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();

            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null) 
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"),"attributeType");

            return CustomAttribute.IsDefined(this, attributeRuntimeType, inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CustomAttributeData.GetCustomAttributesInternal(this);
        }
        #endregion

        #region MemberInfo Overrides

        // This is used by the ToString() overrides of all reflection types. The legacy behavior has the following problems:
        //  1. Use only Name for nested types, which can be confused with global types and generic parameters of the same name.
        //  2. Use only Name for generic parameters, which can be confused with nested types and global types of the same name.
        //  3. Remove the namespace ("System") for all primitive types, which is not language neutral.
        //  4. MethodBase.ToString() use "ByRef" for byref parameters which is different than Type.ToString().
        //  5. ConstructorInfo.ToString() outputs "Void" as the return type. Why Void?
        // Since it could be a breaking changes to fix these legacy behaviors, we only use the better and more unambiguous format
        // in serialization (MemberInfoSerializationHolder).
        internal override string FormatTypeName(bool serialization)
        {
            if (serialization)
            {
                return GetCachedName(TypeNameKind.SerializationName);
            }
            else
            {
                Type elementType = GetRootElementType();

                // Legacy: this doesn't make sense, why use only Name for nested types but otherwise
                // ToString() which contains namespace.
                if (elementType.IsNested)
                    return Name;

                string typeName = ToString();

                // Legacy: why removing "System"? Is it just because C# has keywords for these types?
                // If so why don't we change it to lower case to match the C# keyword casing?
                if (elementType.IsPrimitive ||
                    elementType == typeof(void) ||
                    elementType == typeof(TypedReference))
                {
                    typeName = typeName.Substring(@"System.".Length);
                }

                return typeName;
            }
        }
        
        public override MemberTypes MemberType 
        {
            get 
            {
                if (this.IsPublic || this.IsNotPublic)
                    return MemberTypes.TypeInfo;
                else
                    return MemberTypes.NestedType;
            }
        }

        public override Type ReflectedType => DeclaringType;

        public override int MetadataToken
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get 
            {
                return RuntimeTypeHandle.GetToken(this);
            }
        }
        #endregion

        #region Legacy Internal
        private void CreateInstanceCheckThis()
        {
            if (this is ReflectionOnlyType)
                throw new ArgumentException(Environment.GetResourceString("Arg_ReflectionOnlyInvoke"));

            if (ContainsGenericParameters)
                throw new ArgumentException(
                    Environment.GetResourceString("Acc_CreateGenericEx", this));
            Contract.EndContractBlock();

            Type elementType = this.GetRootElementType();

            if (Object.ReferenceEquals(elementType, typeof(ArgIterator)))
                throw new NotSupportedException(Environment.GetResourceString("Acc_CreateArgIterator"));

            if (Object.ReferenceEquals(elementType, typeof(void)))
                throw new NotSupportedException(Environment.GetResourceString("Acc_CreateVoid"));
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        internal Object CreateInstanceImpl(
            BindingFlags bindingAttr, Binder binder, Object[] args, CultureInfo culture)
        {            
            CreateInstanceCheckThis();
            
            Object server = null;

            try
            {
                try
                {
                    if (args == null)
                        args = Array.Empty<Object> ();

                    int argCnt = args.Length;

                    // Without a binder we need to do use the default binder...
                    if (binder == null)
                        binder = DefaultBinder;

                    // deal with the __COMObject case first. It is very special because from a reflection point of view it has no ctors
                    // so a call to GetMemberCons would fail
                    bool publicOnly = (bindingAttr & BindingFlags.NonPublic) == 0;
                    bool wrapExceptions = (bindingAttr & BindingFlags.DoNotWrapExceptions) == 0;
                    if (argCnt == 0 && (bindingAttr & BindingFlags.Public) != 0 && (bindingAttr & BindingFlags.Instance) != 0
                        && (IsGenericCOMObjectImpl() || IsValueType)) 
                    {
                        server = CreateInstanceDefaultCtor(publicOnly, false, true, wrapExceptions);
                    }
                    else 
                    {
                        ConstructorInfo[] candidates = GetConstructors(bindingAttr);
                        List<MethodBase> matches = new List<MethodBase>(candidates.Length);

                        // We cannot use Type.GetTypeArray here because some of the args might be null
                        Type[] argsType = new Type[argCnt];
                        for (int i = 0; i < argCnt; i++)
                        {
                            if (args[i] != null)
                            {
                                argsType[i] = args[i].GetType();
                            }
                        }

                        for(int i = 0; i < candidates.Length; i ++)
                        {
                            if (FilterApplyConstructorInfo((RuntimeConstructorInfo)candidates[i], bindingAttr, CallingConventions.Any, argsType))
                                matches.Add(candidates[i]);
                        }

                        MethodBase[] cons = new MethodBase[matches.Count];
                        matches.CopyTo(cons);
                        if (cons != null && cons.Length == 0)
                            cons = null;

                        if (cons == null) 
                        {
                            throw new MissingMethodException(Environment.GetResourceString("MissingConstructor_Name", FullName));
                        }

                        MethodBase invokeMethod;
                        Object state = null;

                        try
                        {
                            invokeMethod = binder.BindToMethod(bindingAttr, cons, ref args, null, culture, null, out state);
                        }
                        catch (MissingMethodException) { invokeMethod = null; }

                        if (invokeMethod == null)
                        {
                            throw new MissingMethodException(Environment.GetResourceString("MissingConstructor_Name", FullName));
                        }

                        if (invokeMethod.GetParametersNoCopy().Length == 0)
                        {
                            if (args.Length != 0)
                            {

                                Contract.Assert((invokeMethod.CallingConvention & CallingConventions.VarArgs) == 
                                                 CallingConventions.VarArgs); 
                                throw new NotSupportedException(String.Format(CultureInfo.CurrentCulture, 
                                    Environment.GetResourceString("NotSupported_CallToVarArg")));
                            }

                            // fast path??
                            server = Activator.CreateInstance(this, nonPublic: true, wrapExceptions: wrapExceptions);
                        }
                        else
                        {
                            server = ((ConstructorInfo)invokeMethod).Invoke(bindingAttr, binder, args, culture);
                            if (state != null)
                                binder.ReorderArgumentArray(ref args, state);
                        }
                    }
                }                    
                finally
                {    
                }
            }
            catch (Exception)
            {
                throw;
            }
            
            //Console.WriteLine(server);
            return server;                                
        }
        
        // Helper to invoke the default (parameterless) ctor.
        // fillCache is set in the SL2/3 compat mode or when called from Marshal.PtrToStructure.
        [System.Security.SecuritySafeCritical]  // auto-generated
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        internal Object CreateInstanceDefaultCtor(bool publicOnly, bool skipCheckThis, bool fillCache, bool wrapExceptions)
        {
			if (IsByRefLike)
				throw new NotSupportedException (SR.NotSupported_ByRefLike);

            if (GetType() == typeof(ReflectionOnlyType))
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAllowedInReflectionOnly"));

            return CreateInstanceSlow(publicOnly, wrapExceptions, skipCheckThis, fillCache);
        }

        #endregion
        
        
#region keep in sync with object-internals.h
		[NonSerialized]
		MonoTypeInfo type_info;
#endregion

		internal Object GenericCache;

		internal RuntimeType (Object obj)
		{
			throw new NotImplementedException ();
		}

		internal RuntimeConstructorInfo GetDefaultConstructor ()
		{
			RuntimeConstructorInfo ctor = null;

			if (type_info == null)
				type_info = new MonoTypeInfo ();
			else
				ctor = type_info.default_ctor;

			if (ctor == null) {
				var ctors = GetConstructors (BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

				for (int i = 0; i < ctors.Length; ++i) {
					if (ctors [i].GetParametersCount () == 0) {
						type_info.default_ctor = ctor = (RuntimeConstructorInfo) ctors [i];
						break;
					}
				}
			}

			return ctor;
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern MethodInfo GetCorrespondingInflatedMethod (MethodInfo generic);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern ConstructorInfo GetCorrespondingInflatedConstructor (ConstructorInfo generic);

		internal override MethodInfo GetMethod (MethodInfo fromNoninstanciated)
		{
			if (fromNoninstanciated == null)
				throw new ArgumentNullException ("fromNoninstanciated");
			return GetCorrespondingInflatedMethod (fromNoninstanciated);
		}

		internal override ConstructorInfo GetConstructor (ConstructorInfo fromNoninstanciated)
		{
			if (fromNoninstanciated == null)
				throw new ArgumentNullException ("fromNoninstanciated");
			return GetCorrespondingInflatedConstructor (fromNoninstanciated);
		}

		internal override FieldInfo GetField (FieldInfo fromNoninstanciated)
		{
			/* create sensible flags from given FieldInfo */
			BindingFlags flags = fromNoninstanciated.IsStatic ? BindingFlags.Static : BindingFlags.Instance;
			flags |= fromNoninstanciated.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic;
			return GetField (fromNoninstanciated.Name, flags);
		}

		string GetDefaultMemberName ()
		{
			object [] att = GetCustomAttributes (typeof (DefaultMemberAttribute), true);
			return att.Length != 0 ? ((DefaultMemberAttribute) att [0]).MemberName : null;
		}

		RuntimeConstructorInfo m_serializationCtor;
		internal RuntimeConstructorInfo GetSerializationCtor()
		{
			if (m_serializationCtor == null) {
				var s_SICtorParamTypes = new Type[] { typeof(SerializationInfo), typeof(StreamingContext) };

				m_serializationCtor = GetConstructor(
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
					null,
					CallingConventions.Any,
					s_SICtorParamTypes,
					null) as RuntimeConstructorInfo;
			}

			return m_serializationCtor;
		}

		internal Object CreateInstanceSlow(bool publicOnly, bool wrapExceptions, bool skipCheckThis, bool fillCache)
		{
			//bool bNeedSecurityCheck = true;
			//bool bCanBeCached = false;
			//bool bSecurityCheckOff = false;

			if (!skipCheckThis)
				CreateInstanceCheckThis();

			//if (!fillCache)
			//	bSecurityCheckOff = true;

			return CreateInstanceMono (!publicOnly, wrapExceptions);
		}

		object CreateInstanceMono (bool nonPublic, bool wrapExceptions)
		{
			var ctor = GetDefaultConstructor ();
			if (!nonPublic && ctor != null && !ctor.IsPublic) {
				throw new MissingMethodException(SR.Format(SR.Arg_NoDefCTor, FullName));
			}

			if (ctor == null) {
				Type elementType = this.GetRootElementType();
				if (ReferenceEquals (elementType, typeof (TypedReference)) || ReferenceEquals (elementType, typeof (RuntimeArgumentHandle)))
					throw new NotSupportedException (Environment.GetResourceString ("NotSupported_ContainsStackPtr"));

				if (IsValueType)
					return CreateInstanceInternal (this);

				throw new MissingMethodException ("Default constructor not found for type " + FullName);
			}

			// TODO: .net does more checks in unmanaged land in RuntimeTypeHandle::CreateInstance
			if (IsAbstract) {
				throw new MissingMethodException ("Cannot create an abstract class '{0}'.", FullName);
			}

			return ctor.InternalInvoke (null, null, wrapExceptions);
		}

		internal Object CheckValue (Object value, Binder binder, CultureInfo culture, BindingFlags invokeAttr)
		{
			bool failed = false;
			var res = TryConvertToType (value, ref failed);
			if (!failed)
				return res;

			if ((invokeAttr & BindingFlags.ExactBinding) == BindingFlags.ExactBinding)
				throw new ArgumentException(String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Arg_ObjObjEx"), value.GetType(), this));

			if (binder != null && binder != Type.DefaultBinder)
				return binder.ChangeType (value, this, culture);

			throw new ArgumentException(String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Arg_ObjObjEx"), value.GetType(), this));
		}

		object TryConvertToType (object value, ref bool failed)
		{
			if (IsInstanceOfType (value)) {
				return value;
			}

			if (IsByRef) {
				var elementType = GetElementType ();
				if (value == null || elementType.IsInstanceOfType (value)) {
					return value;
				}
			}

			if (value == null)
				return value;

			if (IsEnum) {
				var type = Enum.GetUnderlyingType (this);
				if (type == value.GetType ())
					return value;
				var res = IsConvertibleToPrimitiveType (value, this);
				if (res != null)
					return res;
			} else if (IsPrimitive) {
				var res = IsConvertibleToPrimitiveType (value, this);
				if (res != null)
					return res;
			} else if (IsPointer) {
				var vtype = value.GetType ();
				if (vtype == typeof (IntPtr) || vtype == typeof (UIntPtr))
					return value;
				if (value is Pointer pointer) {
					Type pointerType = pointer.GetPointerType ();
					if (pointerType == this)
						return pointer.GetPointerValue ();
				}
			}

			failed = true;
			return null;
		}

		// Binder uses some incompatible conversion rules. For example
		// int value cannot be used with decimal parameter but in other
		// ways it's more flexible than normal convertor, for example
		// long value can be used with int based enum
		static object IsConvertibleToPrimitiveType (object value, Type targetType)
		{
			var type = value.GetType ();
			if (type.IsEnum) {
				type = Enum.GetUnderlyingType (type);
				if (type == targetType)
					return value;
			}

			var from = Type.GetTypeCode (type);
			var to = Type.GetTypeCode (targetType);

			switch (to) {
				case TypeCode.Char:
					switch (from) {
						case TypeCode.Byte:
							return (Char) (Byte) value;
						case TypeCode.UInt16:
							return value;
					}
					break;
				case TypeCode.Int16:
					switch (from) {
						case TypeCode.Byte:
							return (Int16) (Byte) value;
						case TypeCode.SByte:
							return (Int16) (SByte) value;
					}
					break;
				case TypeCode.UInt16:
					switch (from) {
						case TypeCode.Byte:
							return (UInt16) (Byte) value;
						case TypeCode.Char:
							return value;
					}
					break;
				case TypeCode.Int32:
					switch (from) {
						case TypeCode.Byte:
							return (Int32) (Byte) value;
						case TypeCode.SByte:
							return (Int32) (SByte) value;
						case TypeCode.Char:
							return (Int32) (Char) value;
						case TypeCode.Int16:
							return (Int32) (Int16) value;
						case TypeCode.UInt16:
							return (Int32) (UInt16) value;
					}
					break;
				case TypeCode.UInt32:
					switch (from) {
						case TypeCode.Byte:
							return (UInt32) (Byte) value;
						case TypeCode.Char:
							return (UInt32) (Char) value;
						case TypeCode.UInt16:
							return (UInt32) (UInt16) value;
					}
					break;
				case TypeCode.Int64:
					switch (from) {
						case TypeCode.Byte:
							return (Int64) (Byte) value;
						case TypeCode.SByte:
							return (Int64) (SByte) value;
						case TypeCode.Int16:
							return (Int64) (Int16) value;
						case TypeCode.Char:
							return (Int64) (Char) value;
						case TypeCode.UInt16:
							return (Int64) (UInt16) value;
						case TypeCode.Int32:
							return (Int64) (Int32) value;
						case TypeCode.UInt32:
							return (Int64) (UInt32) value;
					}
					break;
				case TypeCode.UInt64:
					switch (from) {
						case TypeCode.Byte:
							return (UInt64) (Byte) value;
						case TypeCode.Char:
							return (UInt64) (Char) value;
						case TypeCode.UInt16:
							return (UInt64) (UInt16) value;
						case TypeCode.UInt32:
							return (UInt64) (UInt32) value;
					}
					break;
				case TypeCode.Single:
					switch (from) {
						case TypeCode.Byte:
							return (Single) (Byte) value;
						case TypeCode.SByte:
							return (Single) (SByte) value;
						case TypeCode.Int16:
							return (Single) (Int16) value;
						case TypeCode.Char:
							return (Single) (Char) value;
						case TypeCode.UInt16:
							return (Single) (UInt16) value;
						case TypeCode.Int32:
							return (Single) (Int32) value;
						case TypeCode.UInt32:
							return (Single) (UInt32) value;
						case TypeCode.Int64:
							return (Single) (Int64) value;
						case TypeCode.UInt64:
							return (Single) (UInt64) value;
					}
					break;
				case TypeCode.Double:
					switch (from) {
						case TypeCode.Byte:
							return (Double) (Byte) value;
						case TypeCode.SByte:
							return (Double) (SByte) value;
						case TypeCode.Char:
							return (Double) (Char) value;
						case TypeCode.Int16:
							return (Double) (Int16) value;
						case TypeCode.UInt16:
							return (Double) (UInt16) value;
						case TypeCode.Int32:
							return (Double) (Int32) value;
						case TypeCode.UInt32:
							return (Double) (UInt32) value;
						case TypeCode.Int64:
							return (Double) (Int64) value;
						case TypeCode.UInt64:
							return (Double) (UInt64) value;
						case TypeCode.Single:
							return (Double) (Single) value;
					}
					break;
			}

			// Everything else is rejected
			return null;
		}

		string GetCachedName (TypeNameKind kind)
		{
			switch (kind) {
			case TypeNameKind.SerializationName:
				return ToString ();
			default:
				throw new NotImplementedException ();
			}
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern Type make_array_type (int rank);

		public override Type MakeArrayType ()
		{
			return make_array_type (0);
		}

		public override Type MakeArrayType (int rank)
		{
			if (rank < 1 || rank > 255)
				throw new IndexOutOfRangeException ();
			return make_array_type (rank);
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern Type make_byref_type ();

		public override Type MakeByRefType ()
		{
			if (IsByRef)
				throw new TypeLoadException ("Can not call MakeByRefType on a ByRef type");
			return make_byref_type ();
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		static extern Type MakePointerType (Type type);

		public override Type MakePointerType ()
		{
			if (IsByRef)
				throw new TypeLoadException ($"Could not load type '{GetType()}' from assembly '{AssemblyQualifiedName}");			
			return MakePointerType (this);
		}

		public override StructLayoutAttribute? StructLayoutAttribute {
			get {
				return GetStructLayoutAttribute ();
			}
		}

		public override bool ContainsGenericParameters {
			get {
				if (IsGenericParameter)
					return true;

				if (IsGenericType) {
					foreach (Type arg in GetGenericArguments ())
						if (arg.ContainsGenericParameters)
							return true;
				}

				if (HasElementType)
					return GetElementType ().ContainsGenericParameters;

				return false;
			}
		}

		public override Type[] GetGenericParameterConstraints ()
		{
			if (!IsGenericParameter)
				throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericParameter"));

			var paramInfo = new Mono.RuntimeGenericParamInfoHandle (RuntimeTypeHandle.GetGenericParameterInfo (this));
			Type[] constraints = paramInfo.Constraints;

			return constraints ?? Array.Empty<Type> ();
		}

		internal static object CreateInstanceForAnotherGenericParameter (Type genericType, RuntimeType genericArgument)
		{
			var gt = (RuntimeType) MakeGenericType (genericType, new Type [] { genericArgument });
			var ctor = gt.GetDefaultConstructor ();
			return ctor.InternalInvoke (null, null, wrapExceptions: true);
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		static extern Type MakeGenericType (Type gt, Type [] types);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		internal extern IntPtr GetMethodsByName_native (IntPtr namePtr, BindingFlags bindingAttr, MemberListType listType);

		internal RuntimeMethodInfo[] GetMethodsByName (string name, BindingFlags bindingAttr, MemberListType listType, RuntimeType reflectedType)
		{
			var refh = new RuntimeTypeHandle (reflectedType);
			using (var namePtr = new Mono.SafeStringMarshal (name))
			using (var h = new Mono.SafeGPtrArrayHandle (GetMethodsByName_native (namePtr.Value, bindingAttr, listType))) {
				var n = h.Length;
				var a = new RuntimeMethodInfo [n];
				for (int i = 0; i < n; i++) {
					var mh = new RuntimeMethodHandle (h[i]);
					a[i] = (RuntimeMethodInfo) RuntimeMethodInfo.GetMethodFromHandleNoGenericCheck (mh, refh);
				}
				return a;
			}
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern IntPtr GetPropertiesByName_native (IntPtr name, BindingFlags bindingAttr, MemberListType listType);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern IntPtr GetConstructors_native (BindingFlags bindingAttr);

		RuntimeConstructorInfo[] GetConstructors_internal (BindingFlags bindingAttr, RuntimeType reflectedType)
		{
			var refh = new RuntimeTypeHandle (reflectedType);
			using (var h = new Mono.SafeGPtrArrayHandle (GetConstructors_native (bindingAttr))) {
				var n = h.Length;
				var a = new RuntimeConstructorInfo [n];
				for (int i = 0; i < n; i++) {
					var mh = new RuntimeMethodHandle (h[i]);
					a[i] = (RuntimeConstructorInfo) RuntimeMethodInfo.GetMethodFromHandleNoGenericCheck (mh, refh);
				}
				return a;
			}
		}

		RuntimePropertyInfo[] GetPropertiesByName (string name, BindingFlags bindingAttr, MemberListType listType, RuntimeType reflectedType)
		{
			var refh = new RuntimeTypeHandle (reflectedType);
			using (var namePtr = new Mono.SafeStringMarshal (name))
			using (var h = new Mono.SafeGPtrArrayHandle (GetPropertiesByName_native (namePtr.Value, bindingAttr, listType))) {
				var n = h.Length;
				var a = new RuntimePropertyInfo [n];
				for (int i = 0; i < n; i++) {
					var ph = new Mono.RuntimePropertyHandle (h[i]);
					a[i] = (RuntimePropertyInfo) RuntimePropertyInfo.GetPropertyFromHandle (ph, refh);
				}
				return a;
			}
		}

		public override InterfaceMapping GetInterfaceMap (Type ifaceType)
		{
			if (IsGenericParameter)
				throw new InvalidOperationException(Environment.GetResourceString("Arg_GenericParameter"));
		
			if ((object)ifaceType == null)
				throw new ArgumentNullException("ifaceType");

			RuntimeType ifaceRtType = ifaceType as RuntimeType;

			if (ifaceRtType == null)
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"), "ifaceType");

			InterfaceMapping res;
			if (!ifaceType.IsInterface)
				throw new ArgumentException ("Argument must be an interface.", "ifaceType");
			if (IsInterface)
				throw new ArgumentException ("'this' type cannot be an interface itself");
			res.TargetType = this;
			res.InterfaceType = ifaceType;
			GetInterfaceMapData (this, ifaceType, out res.TargetMethods, out res.InterfaceMethods);
			if (res.TargetMethods == null)
				throw new ArgumentException ("Interface not found", "ifaceType");

			return res;
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		static extern void GetInterfaceMapData (Type t, Type iface, out MethodInfo[] targets, out MethodInfo[] methods);		

		public override Guid GUID {
			get {
				object[] att = GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), true);
				if (att.Length == 0)
					return Guid.Empty;
				return new Guid(((System.Runtime.InteropServices.GuidAttribute)att[0]).Value);
			}
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		internal extern void GetPacking (out int packing, out int size);

		internal static Type GetTypeFromCLSIDImpl(Guid clsid, String server, bool throwOnError)
		{
			throw new NotImplementedException ("Unmanaged activation removed");
		}

		protected override TypeCode GetTypeCodeImpl ()
		{
			return GetTypeCodeImplInternal (this);
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern static TypeCode GetTypeCodeImplInternal (Type type);		

		internal static Type GetTypeFromProgIDImpl(String progID, String server, bool throwOnError)
		{
			throw new NotImplementedException ("Unmanaged activation is not supported");
		}

		public override string ToString()
		{
			return getFullName (false, false);
		}

		bool IsGenericCOMObjectImpl ()
		{
			return false;
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern object CreateInstanceInternal (Type type);

		public extern override MethodBase? DeclaringMethod {
			[MethodImplAttribute (MethodImplOptions.InternalCall)]
			get;
		}		
		
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		internal extern string getFullName(bool full_name, bool assembly_qualified);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern Type[] GetGenericArgumentsInternal (bool runtimeArray);

		GenericParameterAttributes GetGenericParameterAttributes () {
			return (new Mono.RuntimeGenericParamInfoHandle (RuntimeTypeHandle.GetGenericParameterInfo (this))).Attributes;
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern int GetGenericParameterPosition ();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern IntPtr GetEvents_native (IntPtr name,  MemberListType listType);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern IntPtr GetFields_native (IntPtr name, BindingFlags bindingAttr, MemberListType listType);

		RuntimeFieldInfo[] GetFields_internal (string name, BindingFlags bindingAttr, MemberListType listType, RuntimeType reflectedType)
		{
			var refh = new RuntimeTypeHandle (reflectedType);
			using (var namePtr = new Mono.SafeStringMarshal (name))
			using (var h = new Mono.SafeGPtrArrayHandle (GetFields_native (namePtr.Value, bindingAttr, listType))) {
				int n = h.Length;
				var a = new RuntimeFieldInfo[n];
				for (int i = 0; i < n; i++) {
					var fh = new RuntimeFieldHandle (h[i]);
					a[i] = (RuntimeFieldInfo) FieldInfo.GetFieldFromHandle (fh, refh);
				}
				return a;
			}
		}

		RuntimeEventInfo[] GetEvents_internal (string name, BindingFlags bindingAttr, MemberListType listType, RuntimeType reflectedType)
		{
			var refh = new RuntimeTypeHandle (reflectedType);
			using (var namePtr = new Mono.SafeStringMarshal (name))
			using (var h = new Mono.SafeGPtrArrayHandle (GetEvents_native (namePtr.Value, listType))) {
				int n = h.Length;
				var a = new RuntimeEventInfo[n];
				for (int i = 0; i < n; i++) {
					var eh = new Mono.RuntimeEventHandle (h[i]);
					a[i] = (RuntimeEventInfo) RuntimeEventInfo.GetEventFromHandle (eh, refh);
				}
				return a;
			}
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern override Type[] GetInterfaces();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern IntPtr GetNestedTypes_native (IntPtr name, BindingFlags bindingAttr, MemberListType listType);

		RuntimeType[] GetNestedTypes_internal (string displayName, BindingFlags bindingAttr, MemberListType listType)
		{
			string internalName = null;
			if (displayName != null)
				internalName = displayName;
			using (var namePtr = new Mono.SafeStringMarshal (internalName))
			using (var h = new Mono.SafeGPtrArrayHandle (GetNestedTypes_native (namePtr.Value, bindingAttr, listType))) {
				int n = h.Length;
				var a = new RuntimeType [n];
				for (int i = 0; i < n; i++) {
					var th = new RuntimeTypeHandle (h[i]);
					a[i] = (RuntimeType) Type.GetTypeFromHandle (th);
				}
				return a;
			}
		}

		public override string? AssemblyQualifiedName {
			get {
				return getFullName (true, true);
			}
		}

		public extern override Type? DeclaringType {
			[MethodImplAttribute (MethodImplOptions.InternalCall)]
			get;
		}

		public extern override string Name {
			[MethodImplAttribute (MethodImplOptions.InternalCall)]
			get;
		}

		public extern override string? Namespace {
			[MethodImplAttribute (MethodImplOptions.InternalCall)]
			get;
		}

		public override bool IsSecurityTransparent {
			get { return false; }
		}

		public override bool IsSecurityCritical {
			get { return true; }
		}

		public override bool IsSecuritySafeCritical {
			get { return false; }
		}

		public override string? FullName {
			get {
				// https://bugzilla.xamarin.com/show_bug.cgi?id=57938
				if (IsGenericType && ContainsGenericParameters && !IsGenericTypeDefinition)
					return null;

				string fullName;
				// This doesn't need locking
				if (type_info == null)
					type_info = new MonoTypeInfo ();
				if ((fullName = type_info.full_name) == null)
					fullName = type_info.full_name = getFullName (true, false);

				return fullName;
			}
		}

		public sealed override bool HasSameMetadataDefinitionAs (MemberInfo other) => HasSameMetadataDefinitionAsCore<RuntimeType> (other);	

		public override bool IsSZArray {
			get {
				// TODO: intrinsic
				return IsArray && ReferenceEquals (this, GetElementType ().MakeArrayType ());
			}
		}

		internal override bool IsUserType {
			get {
				return false;
			}
		}

		public override bool IsSubclassOf(Type type)
		{
			if ((object)type == null)
				throw new ArgumentNullException("type");

			RuntimeType rtType = type as RuntimeType;
			if (rtType == null)
				return false;

			return RuntimeTypeHandle.IsSubclassOf (this, rtType);
		}

		public override bool IsByRefLike {
			get {
				return RuntimeTypeHandle.IsByRefLike (this);
			}
		}

		public override bool IsTypeDefinition => RuntimeTypeHandle.IsTypeDefinition (this);

        private const int DEFAULT_PACKING_SIZE = 8;

        internal StructLayoutAttribute GetStructLayoutAttribute ()
        {
            if (IsInterface || HasElementType || IsGenericParameter)
                return null;

            int pack = 0, size = 0;
            LayoutKind layoutKind = LayoutKind.Auto;
            switch (Attributes & TypeAttributes.LayoutMask)
            {
                case TypeAttributes.ExplicitLayout: layoutKind = LayoutKind.Explicit; break;
                case TypeAttributes.AutoLayout: layoutKind = LayoutKind.Auto; break;
                case TypeAttributes.SequentialLayout: layoutKind = LayoutKind.Sequential; break;
                default: break;
            }

            CharSet charSet = CharSet.None;
            switch (Attributes & TypeAttributes.StringFormatMask)
            {
                case TypeAttributes.AnsiClass: charSet = CharSet.Ansi; break;
                case TypeAttributes.AutoClass: charSet = CharSet.Auto; break;
                case TypeAttributes.UnicodeClass: charSet = CharSet.Unicode; break;
                default: break;
            }

            GetPacking (out pack, out size);

            // Metadata parameter checking should not have allowed 0 for packing size.
            // The runtime later converts a packing size of 0 to 8 so do the same here
            // because it's more useful from a user perspective. 
            if (pack == 0)
                pack = DEFAULT_PACKING_SIZE;

            return new StructLayoutAttribute (layoutKind) { Pack = pack, Size = size, CharSet = charSet };
        }
    }

    // this is the introspection only type. This type overrides all the functions with runtime semantics
    // and throws an exception.
    // The idea behind this type is that it relieves RuntimeType from doing honerous checks about ReflectionOnly
    // context.
    // This type should not derive from RuntimeType but it's doing so for convinience.
    // That should not present a security threat though it is risky as a direct call to one of the base method
    // method (RuntimeType) and an instance of this type will work around the reason to have this type in the 
    // first place. However given RuntimeType is not public all its methods are protected and require full trust
    // to be accessed
    [Serializable]
    internal class ReflectionOnlyType : RuntimeType {

        private ReflectionOnlyType() {}

        // always throw
        public override RuntimeTypeHandle TypeHandle 
        {
            get 
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAllowedInReflectionOnly"));
            }
        }
    }
    
    // Contains information about the type which is expensive to compute
    [StructLayout (LayoutKind.Sequential)]
    internal class MonoTypeInfo {
        // this is the displayed form: special characters
        // ,+*&*[]\ in the identifier portions of the names
        // have been escaped with a leading backslash (\)
        public string full_name;
        public RuntimeConstructorInfo default_ctor;
    }
}
