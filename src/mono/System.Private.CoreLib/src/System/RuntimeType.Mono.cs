// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Globalization;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace System
{
    // Keep this in sync with FormatFlags defined in typestring.h
    internal enum TypeNameFormatFlags
    {
        FormatBasic = 0x00000000, // Not a bitmask, simply the tersest flag settings possible
        FormatNamespace = 0x00000001, // Include namespace and/or enclosing class names in type names
        FormatFullInst = 0x00000002, // Include namespace and assembly in generic types (regardless of other flag settings)
        FormatAssembly = 0x00000004, // Include assembly display name in type names
        FormatSignature = 0x00000008, // Include signature in method names
        FormatNoVersion = 0x00000010, // Suppress version and culture information in all assembly names
#if _DEBUG
        FormatDebug         = 0x00000020, // For debug printing of types only
#endif
        FormatAngleBrackets = 0x00000040, // Whether generic types are C<T> or C[T]
        FormatStubInfo = 0x00000080, // Include stub info like {unbox-stub}
        FormatGenericParam = 0x00000100, // Use !name and !!name for generic type and method parameters

        // If we want to be able to distinguish between overloads whose parameter types have the same name but come from different assemblies,
        // we can add FormatAssembly | FormatNoVersion to FormatSerialization. But we are omitting it because it is not a useful scenario
        // and including the assembly name will normally increase the size of the serialized data and also decrease the performance.
        FormatSerialization = FormatNamespace |
                              FormatGenericParam |
                              FormatFullInst
    }

    internal partial class RuntimeType
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
        private struct ListBuilder<T> where T : class?
        {
            private T[]? _items;
            private T _item;
            private int _count;
            private int _capacity;

            public ListBuilder(int capacity)
            {
                _items = null;
                _item = null!;
                _count = 0;
                _capacity = capacity;
            }

            public T this[int index]
            {
                get
                {
                    Debug.Assert(index < Count);
                    return (_items != null) ? _items[index] : _item;
                }
            }

            public T[] ToArray()
            {
                if (_count == 0)
                    return Array.Empty<T>();
                if (_count == 1)
                    return new T[1] { _item };

                Array.Resize(ref _items, _count);
                _capacity = _count;
                return _items;
            }

            public void CopyTo(object?[] array, int index)
            {
                if (_count == 0)
                    return;

                if (_count == 1)
                {
                    array[index] = _item;
                    return;
                }

                Array.Copy(_items!, 0, array, index, _count);
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

                    _items![_count] = item;
                }
                _count++;
            }
        }

        #endregion

        #region Static Members

        #region Internal

        [RequiresUnreferencedCode("Types might be removed")]
        internal static RuntimeType? GetType(string typeName, bool throwOnError, bool ignoreCase,
            ref StackCrawlMark stackMark)
        {
            ArgumentNullException.ThrowIfNull(typeName);

            return RuntimeTypeHandle.GetTypeByName(
                typeName, throwOnError, ignoreCase, ref stackMark);
        }

        private static void SplitName(string? fullname, out string? name, out string? ns)
        {
            name = null;
            ns = null;

            if (fullname == null)
                return;

            // Get namespace
            int nsDelimiter = fullname.LastIndexOf('.');
            if (nsDelimiter != -1)
            {
                ns = fullname.Substring(0, nsDelimiter);
                int nameLength = fullname.Length - ns.Length - 1;
                if (nameLength != 0)
                    name = fullname.Substring(nsDelimiter + 1, nameLength);
                else
                    name = "";
                Debug.Assert(fullname.Equals(ns + "." + name));
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
            BindingFlags bindingFlags, ref string? name, bool allowPrefixLookup, out bool prefixLookup,
            out bool ignoreCase, out MemberListType listType)
        {
            prefixLookup = false;
            ignoreCase = false;

            if (name != null)
            {
                if ((bindingFlags & BindingFlags.IgnoreCase) != 0)
                {
                    name = name.ToLowerInvariant();
                    ignoreCase = true;
                    listType = MemberListType.CaseInsensitive;
                }
                else
                {
                    listType = MemberListType.CaseSensitive;
                }

                if (allowPrefixLookup && name.EndsWith('*'))
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
        private static void FilterHelper(BindingFlags bindingFlags, ref string? name, out bool ignoreCase, out MemberListType listType)
        {
            FilterHelper(bindingFlags, ref name, false, out _, out ignoreCase, out listType);
        }

        // Only called by GetXXXCandidates, GetInterfaces, and GetNestedTypes when FilterHelper has set "prefixLookup" to true.
        // Most of the plural GetXXX methods allow prefix lookups while the singular GetXXX methods mostly do not.
        private static bool FilterApplyPrefixLookup(MemberInfo memberInfo, string? name, bool ignoreCase)
        {
            Debug.Assert(name != null);

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
            string? name, bool prefixLookup)
        {
            #region Preconditions
            Debug.Assert(memberInfo != null);
            Debug.Assert(name == null || (bindingFlags & BindingFlags.IgnoreCase) == 0 || (name.ToLowerInvariant().Equals(name)));
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

            bool isInherited = !ReferenceEquals(memberInfo.DeclaringType, memberInfo.ReflectedType);

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
            if (prefixLookup && !FilterApplyPrefixLookup(memberInfo, name, (bindingFlags & BindingFlags.IgnoreCase) != 0))
            {
                return false;
            }
            #endregion

            #region Asymmetries
            // @Asymmetry - Internal, inherited, instance, non-protected, non-virtual, non-abstract members returned
            //              iff BindingFlags !DeclaredOnly, Instance and Public are present except for fields
            if (((bindingFlags & BindingFlags.DeclaredOnly) == 0) &&        // DeclaredOnly not present
                 isInherited &&                                            // Is inherited Member

                (isNonProtectedInternal) &&                                 // Is non-protected internal member
                ((bindingFlags & BindingFlags.NonPublic) != 0) &&           // BindingFlag.NonPublic present

                (!isStatic) &&                                              // Is instance member
                ((bindingFlags & BindingFlags.Instance) != 0))              // BindingFlag.Instance present
            {
                MethodInfo? methodInfo = memberInfo as MethodInfo;

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
            Type type, BindingFlags bindingFlags, string? name, bool prefixLookup, string? ns)
        {
            Debug.Assert(type is RuntimeType);

            bool isPublic = type.IsNestedPublic || type.IsPublic;
            bool isStatic = false;

            if (!FilterApplyBase(type, bindingFlags, isPublic, type.IsNestedAssembly, isStatic, name, prefixLookup))
                return false;

            if (ns != null && ns != type.Namespace)
                return false;

            return true;
        }


        private static bool FilterApplyMethodInfo(
            RuntimeMethodInfo method, BindingFlags bindingFlags, CallingConventions callConv, Type[]? argumentTypes)
        {
            // Optimization: Pre-Calculate the method binding flags to avoid casting.
            return FilterApplyMethodBase(method, bindingFlags, callConv, argumentTypes);
        }

        private static bool FilterApplyConstructorInfo(
            RuntimeConstructorInfo constructor, BindingFlags bindingFlags, CallingConventions callConv, Type[]? argumentTypes)
        {
            // Optimization: Pre-Calculate the method binding flags to avoid casting.
            return FilterApplyMethodBase(constructor, bindingFlags, callConv, argumentTypes);
        }

        // Used by GetMethodCandidates/GetConstructorCandidates, InvokeMember, and CreateInstanceImpl to perform the necessary filtering.
        // Should only be called by FilterApplyMethodInfo and FilterApplyConstructorInfo.
        private static bool FilterApplyMethodBase(
            MethodBase methodBase, BindingFlags bindingFlags, CallingConventions callConv, Type[]? argumentTypes)
        {
            Debug.Assert(methodBase != null);

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
                            Debug.Assert((callConv & CallingConventions.VarArgs) != 0);
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
                        if (parameterInfos.Length == 0)
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
                            for (int i = 0; i < parameterInfos.Length; i++)
                            {
                                // a null argument type implies a null arg which is always a perfect match
                                if (argumentTypes[i] is not null && !argumentTypes[i].MatchesParameterTypeExactly(parameterInfos[i]))
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

        private static readonly RuntimeType ObjectType = (RuntimeType)typeof(object);
        private static readonly RuntimeType StringType = (RuntimeType)typeof(string);

        #endregion

        #region Constructor
        internal RuntimeType() { throw new NotSupportedException(); }
        #endregion

        #region Type Overrides

        #region Get XXXInfo Candidates
        private ListBuilder<MethodInfo> GetMethodCandidates(
            string? name, BindingFlags bindingAttr, CallingConventions callConv,
            Type[]? types, int genericParamCount, bool allowPrefixLookup)
        {
            bool prefixLookup, ignoreCase;
            MemberListType listType;
            FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out ignoreCase, out listType);

            RuntimeMethodInfo[] cache = GetMethodsByName(name, bindingAttr, listType, this);
            ListBuilder<MethodInfo> candidates = new ListBuilder<MethodInfo>(cache.Length);

            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeMethodInfo methodInfo = cache[i];
                if (genericParamCount != -1)
                {
                    bool is_generic = methodInfo.IsGenericMethod;
                    if (genericParamCount == 0 && is_generic)
                        continue;
                    else if (genericParamCount > 0 && !is_generic)
                        continue;
                    Type[]? args = methodInfo.GetGenericArguments();
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
            string? name, BindingFlags bindingAttr, CallingConventions callConv,
            Type[]? types, bool allowPrefixLookup)
        {
            bool prefixLookup, ignoreCase;
            FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out ignoreCase, out _);

            if (!string.IsNullOrEmpty(name) && name != ConstructorInfo.ConstructorName && name != ConstructorInfo.TypeConstructorName)
                return new ListBuilder<ConstructorInfo>(0);
            RuntimeConstructorInfo[] cache = GetConstructors_internal(bindingAttr, this);
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
            string? name, BindingFlags bindingAttr, Type[]? types, bool allowPrefixLookup)
        {
            bool prefixLookup, ignoreCase;
            MemberListType listType;
            FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out ignoreCase, out listType);

            RuntimePropertyInfo[] cache = GetPropertiesByName(name, bindingAttr, listType, this);
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

        private ListBuilder<EventInfo> GetEventCandidates(string? name, BindingFlags bindingAttr, bool allowPrefixLookup)
        {
            bool prefixLookup, ignoreCase;
            MemberListType listType;
            FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out ignoreCase, out listType);

            RuntimeEventInfo[] cache = GetEvents_internal(name, listType, this);
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

        private ListBuilder<FieldInfo> GetFieldCandidates(string? name, BindingFlags bindingAttr, bool allowPrefixLookup)
        {
            bool prefixLookup, ignoreCase;
            MemberListType listType;
            FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out ignoreCase, out listType);

            RuntimeFieldInfo[] cache = GetFields_internal(name, bindingAttr, listType, this);

            ListBuilder<FieldInfo> candidates = new ListBuilder<FieldInfo>(cache.Length);
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeFieldInfo fieldInfo = cache[i];
                if ((!prefixLookup || FilterApplyPrefixLookup(fieldInfo, name, ignoreCase)))
                {
                    candidates.Add(fieldInfo);
                }
            }

            return candidates;
        }

        private ListBuilder<Type> GetNestedTypeCandidates(string? fullname, BindingFlags bindingAttr, bool allowPrefixLookup)
        {
            bool prefixLookup;
            bindingAttr &= ~BindingFlags.Static;
            string? name, ns;
            MemberListType listType;
            SplitName(fullname, out name, out ns);
            FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out _, out listType);

            RuntimeType[] cache = GetNestedTypes_internal(name, bindingAttr, listType);
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
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            return GetMethodCandidates(null, bindingAttr, CallingConventions.Any, null, -1, false).ToArray();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            return GetConstructorCandidates(null, bindingAttr, CallingConventions.Any, null, false).ToArray();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            return GetPropertyCandidates(null, bindingAttr, null, false).ToArray();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            return GetEventCandidates(null, bindingAttr, false).ToArray();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            return GetFieldCandidates(null, bindingAttr, false).ToArray();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            return GetNestedTypeCandidates(null, bindingAttr, false).ToArray();
        }

        [DynamicallyAccessedMembers(GetAllMembers)]
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
            Debug.Assert(i == members.Length);

            return members;
        }

        #endregion

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
        {
            return GetMethodImpl(name, -1, bindingAttr, binder, callConvention, types, modifiers);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected override MethodInfo? GetMethodImpl(string name, int genericParamCount,
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
                        if (!System.DefaultBinder.CompareMethodSig(methodInfo, firstCandidate))
                            throw ThrowHelper.GetAmbiguousMatchException(firstCandidate);
                    }

                    // All the methods have the exact same name and sig so return the most derived one.
                    return System.DefaultBinder.FindMostDerivedNewSlotMeth(candidates.ToArray(), candidates.Count) as MethodInfo;
                }
            }

            binder ??= DefaultBinder;

            return binder.SelectMethod(bindingAttr, candidates.ToArray(), types, modifiers) as MethodInfo;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        protected override ConstructorInfo? GetConstructorImpl(
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
                return System.DefaultBinder.ExactBinding(candidates.ToArray(), types) as ConstructorInfo;

            binder ??= DefaultBinder;

            return binder.SelectMethod(bindingAttr, candidates.ToArray(), types, modifiers) as ConstructorInfo;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        protected override PropertyInfo? GetPropertyImpl(
            string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers)
        {
            ArgumentNullException.ThrowIfNull(name);

            ListBuilder<PropertyInfo> candidates = GetPropertyCandidates(name, bindingAttr, types, false);

            if (candidates.Count == 0)
                return null;

            if (types == null || types.Length == 0)
            {
                // no arguments
                PropertyInfo firstCandidate = candidates[0];

                if (candidates.Count == 1)
                {
                    if (returnType is not null && !returnType.IsEquivalentTo(firstCandidate.PropertyType))
                        return null;

                    return firstCandidate;
                }
                else
                {
                    if (returnType is null)
                        // if we are here we have no args or property type to select over and we have more than one property with that name
                        throw ThrowHelper.GetAmbiguousMatchException(firstCandidate);
                }
            }

            if ((bindingAttr & BindingFlags.ExactBinding) != 0)
                return System.DefaultBinder.ExactPropertyBinding(candidates.ToArray(), returnType, types);

            binder ??= DefaultBinder;

            return binder.SelectProperty(bindingAttr, candidates.ToArray(), returnType, types, modifiers);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo? GetEvent(string name, BindingFlags bindingAttr)
        {
            ArgumentNullException.ThrowIfNull(name);

            MemberListType listType;
            FilterHelper(bindingAttr, ref name!, out _, out listType);

            RuntimeEventInfo[] cache = GetEvents_internal(name, listType, this);
            EventInfo? match = null;

            bindingAttr ^= BindingFlags.DeclaredOnly;

            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeEventInfo eventInfo = cache[i];
                if ((bindingAttr & eventInfo.BindingFlags) == eventInfo.BindingFlags)
                {
                    if (match != null)
                        throw ThrowHelper.GetAmbiguousMatchException(match);

                    match = eventInfo;
                }
            }

            return match;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo? GetField(string name, BindingFlags bindingAttr)
        {
            ArgumentNullException.ThrowIfNull(name);

            MemberListType listType;
            FilterHelper(bindingAttr, ref name!, out _, out listType);

            RuntimeFieldInfo[] cache = GetFields_internal(name, bindingAttr, listType, this);
            FieldInfo? match = null;

            bool multipleStaticFieldMatches = false;

            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeFieldInfo fieldInfo = cache[i];
                {
                    if (match != null)
                    {
                        if (ReferenceEquals(fieldInfo.DeclaringType, match.DeclaringType))
                            throw ThrowHelper.GetAmbiguousMatchException(match);

                        if (match.DeclaringType!.IsInterface && fieldInfo.DeclaringType!.IsInterface)
                            multipleStaticFieldMatches = true;
                    }

                    if (match == null || fieldInfo.DeclaringType!.IsSubclassOf(match.DeclaringType!) || match.DeclaringType!.IsInterface)
                        match = fieldInfo;
                }
            }

            if (multipleStaticFieldMatches && match!.DeclaringType!.IsInterface)
                throw ThrowHelper.GetAmbiguousMatchException(match);

            return match;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2063:UnrecognizedReflectionPattern",
            Justification = "Trimming makes sure that interfaces are fully preserved, so the Interfaces annotation is transitive." +
                            "The cache doesn't carry the necessary annotation since it returns an array type," +
                            "so the analysis complains that the returned value doesn't have the necessary annotation.")]
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type? GetInterface(string fullname, bool ignoreCase)
        {
            ArgumentNullException.ThrowIfNull(fullname);

            BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.NonPublic;

            bindingAttr &= ~BindingFlags.Static;

            if (ignoreCase)
                bindingAttr |= BindingFlags.IgnoreCase;

            string? name, ns;
            SplitName(fullname, out name, out ns);
            FilterHelper(bindingAttr, ref name, out ignoreCase, out _);

            List<RuntimeType>? list = null;
            StringComparison nameComparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            foreach (RuntimeType t in GetInterfaces())
            {
                if (!string.Equals(t.Name, name, nameComparison))
                {
                    continue;
                }

                list ??= new List<RuntimeType>(2);

                list.Add(t);
            }

            if (list == null)
                return null;

            RuntimeType[]? cache = list.ToArray();
            RuntimeType? match = null;

            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeType iface = cache[i];
                if (FilterApplyType(iface, bindingAttr, name, false, ns))
                {
                    if (match != null)
                        throw ThrowHelper.GetAmbiguousMatchException(match);

                    match = iface;
                }
            }

            return match;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type? GetNestedType(string fullname, BindingFlags bindingAttr)
        {
            ArgumentNullException.ThrowIfNull(fullname);

            bindingAttr &= ~BindingFlags.Static;
            string? name, ns;
            MemberListType listType;
            SplitName(fullname, out name, out ns);
            FilterHelper(bindingAttr, ref name, out _, out listType);
            RuntimeType[] cache = GetNestedTypes_internal(name, bindingAttr, listType);
            RuntimeType? match = null;

            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeType nestedType = cache[i];
                if (FilterApplyType(nestedType, bindingAttr, name, false, ns))
                {
                    if (match != null)
                        throw ThrowHelper.GetAmbiguousMatchException(match);

                    match = nestedType;
                }
            }

            return match;
        }

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
        {
            ArgumentNullException.ThrowIfNull(name);

            ListBuilder<MethodInfo> methods = default;
            ListBuilder<ConstructorInfo> constructors = default;
            ListBuilder<PropertyInfo> properties = default;
            ListBuilder<EventInfo> events = default;
            ListBuilder<FieldInfo> fields = default;
            ListBuilder<Type> nestedTypes = default;

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
            Debug.Assert(i == compressMembers.Length);

            return compressMembers;
        }

        public override MemberInfo GetMemberWithSameMetadataDefinitionAs(MemberInfo member)
        {
            ArgumentNullException.ThrowIfNull(member);

            RuntimeType? runtimeType = this;
            while (runtimeType != null)
            {
                MemberInfo? result = member.MemberType switch
                {
                    MemberTypes.Method => GetMethodWithSameMetadataDefinitionAs(runtimeType, member),
                    MemberTypes.Constructor => GetConstructorWithSameMetadataDefinitionAs(runtimeType, member),
                    MemberTypes.Property => GetPropertyWithSameMetadataDefinitionAs(runtimeType, member),
                    MemberTypes.Field => GetFieldWithSameMetadataDefinitionAs(runtimeType, member),
                    MemberTypes.Event => GetEventWithSameMetadataDefinitionAs(runtimeType, member),
                    MemberTypes.NestedType => GetNestedTypeWithSameMetadataDefinitionAs(runtimeType, member),
                    _ => null
                };

                if (result != null)
                {
                    return result;
                }

                runtimeType = runtimeType.GetBaseType();
            }

            throw CreateGetMemberWithSameMetadataDefinitionAsNotFoundException(member);
        }

        private const BindingFlags GetMemberWithSameMetadataDefinitionAsBindingFlags =
            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static MethodInfo? GetMethodWithSameMetadataDefinitionAs(RuntimeType runtimeType, MemberInfo methodInfo)
        {
            ListBuilder<MethodInfo> methods = runtimeType.GetMethodCandidates(methodInfo.Name, GetMemberWithSameMetadataDefinitionAsBindingFlags, CallingConventions.Any, null, -1, allowPrefixLookup: false);

            for (int i = 0; i < methods.Count; i++)
            {
                MethodInfo candidate = methods[i];
                if (candidate.HasSameMetadataDefinitionAs(methodInfo))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static ConstructorInfo? GetConstructorWithSameMetadataDefinitionAs(RuntimeType runtimeType, MemberInfo constructorInfo)
        {
            ListBuilder<ConstructorInfo> ctors = runtimeType.GetConstructorCandidates(null, GetMemberWithSameMetadataDefinitionAsBindingFlags, CallingConventions.Any, null, allowPrefixLookup: false);

            for (int i = 0; i < ctors.Count; i++)
            {
                ConstructorInfo candidate = ctors[i];
                if (candidate.HasSameMetadataDefinitionAs(constructorInfo))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static PropertyInfo? GetPropertyWithSameMetadataDefinitionAs(RuntimeType runtimeType, MemberInfo propertyInfo)
        {
            ListBuilder<PropertyInfo> properties = runtimeType.GetPropertyCandidates(propertyInfo.Name, GetMemberWithSameMetadataDefinitionAsBindingFlags, null, allowPrefixLookup: false);

            for (int i = 0; i < properties.Count; i++)
            {
                PropertyInfo candidate = properties[i];
                if (candidate.HasSameMetadataDefinitionAs(propertyInfo))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static FieldInfo? GetFieldWithSameMetadataDefinitionAs(RuntimeType runtimeType, MemberInfo fieldInfo)
        {
            ListBuilder<FieldInfo> fields = runtimeType.GetFieldCandidates(fieldInfo.Name, GetMemberWithSameMetadataDefinitionAsBindingFlags, allowPrefixLookup: false);

            for (int i = 0; i < fields.Count; i++)
            {
                FieldInfo candidate = fields[i];
                if (candidate.HasSameMetadataDefinitionAs(fieldInfo))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static EventInfo? GetEventWithSameMetadataDefinitionAs(RuntimeType runtimeType, MemberInfo eventInfo)
        {
            ListBuilder<EventInfo> events = runtimeType.GetEventCandidates(null, GetMemberWithSameMetadataDefinitionAsBindingFlags, allowPrefixLookup: false);

            for (int i = 0; i < events.Count; i++)
            {
                EventInfo candidate = events[i];
                if (candidate.HasSameMetadataDefinitionAs(eventInfo))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static Type? GetNestedTypeWithSameMetadataDefinitionAs(RuntimeType runtimeType, MemberInfo nestedType)
        {
            ListBuilder<Type> nestedTypes = runtimeType.GetNestedTypeCandidates(nestedType.Name, GetMemberWithSameMetadataDefinitionAsBindingFlags, allowPrefixLookup: false);

            for (int i = 0; i < nestedTypes.Count; i++)
            {
                Type candidate = nestedTypes[i];
                if (candidate.HasSameMetadataDefinitionAs(nestedType))
                {
                    return candidate;
                }
            }

            return null;
        }
        #endregion

        #region Hierarchy

        // Reflexive, symmetric, transitive.
        public override bool IsEquivalentTo(Type? other)
        {
            RuntimeType? otherRtType = other as RuntimeType;
            if (otherRtType is null)
                return false;

            if (otherRtType == this)
                return true;

            // It's not worth trying to perform further checks in managed
            // as they would lead to FCalls anyway.
            return RuntimeTypeHandle.IsEquivalentTo(this, otherRtType);
        }

        #endregion

        #region Attributes

        internal CorElementType GetCorElementType()
        {
            TypeCache cache = Cache;
            if ((cache.Cached & (int)TypeCacheEntries.CorElementType) != 0)
                return cache.CorElementType;

            var type = this;
            cache.CorElementType = RuntimeTypeHandle.GetCorElementType (new QCallTypeHandle(ref type));
            Interlocked.MemoryBarrier ();
            UpdateCached(TypeCacheEntries.CorElementType);
            return cache.CorElementType;
        }

        internal TypeAttributes GetAttributes()
        {
            TypeCache cache = Cache;
            if ((cache.Cached & (int)TypeCacheEntries.TypeAttributes) != 0)
                return cache.TypeAttributes;

            var type = this;
            cache.TypeAttributes = RuntimeTypeHandle.GetAttributes(new QCallTypeHandle(ref type));
            Interlocked.MemoryBarrier ();
            UpdateCached(TypeCacheEntries.TypeAttributes);
            return cache.TypeAttributes;
        }

        internal bool IsDelegate()
        {
            TypeCache cache = Cache;
            if ((cache.Cached & (int)TypeCacheEntries.IsDelegate) != 0)
                return (cache.Flags & (int)TypeCacheEntries.IsDelegate) != 0;

            bool res = GetBaseType() == typeof(System.MulticastDelegate);
            CacheFlag(TypeCacheEntries.IsDelegate, res);
            return res;
        }

        protected override bool IsValueTypeImpl()
        {
            TypeCache cache = Cache;
            if ((cache.Cached & (int)TypeCacheEntries.IsValueType) != 0)
                return (cache.Flags & (int)TypeCacheEntries.IsValueType) != 0;

            // We need to return true for generic parameters with the ValueType constraint.
            // So we cannot use the faster RuntimeTypeHandle.IsValueType because it returns
            // false for all generic parameters.
            bool res = false;
            if (!(this == typeof(ValueType) || this == typeof(Enum)))
                res = IsSubclassOf(typeof(ValueType));
            CacheFlag(TypeCacheEntries.IsValueType, res);
            return res;
        }

        // Returns true for generic parameters with the Enum constraint.
        public override bool IsEnum => GetBaseType() == EnumType;

        // Returns true for actual enum types only.
        internal bool IsActualEnum
        {
            get
            {
                TypeCache cache = Cache;
                if ((cache.Cached & (int)TypeCacheEntries.IsActualEnum) != 0)
                    return (cache.Flags & (int)TypeCacheEntries.IsActualEnum) != 0;
                bool res = !IsGenericParameter && RuntimeTypeHandle.GetBaseType(this) == EnumType;
                CacheFlag(TypeCacheEntries.IsActualEnum, res);
                return res;
            }
        }

        public override bool IsConstructedGenericType => IsGenericType && !IsGenericTypeDefinition;

        public override bool IsGenericType
        {
            get
            {
                TypeCache cache = Cache;
                if ((cache.Cached & (int)TypeCacheEntries.IsGenericType) != 0)
                    return (cache.Flags & (int)TypeCacheEntries.IsGenericType) != 0;
                bool res = RuntimeTypeHandle.HasInstantiation(this);
                CacheFlag(TypeCacheEntries.IsGenericType, res);
                return res;
            }
        }

        public override bool IsGenericTypeDefinition
        {
            get
            {
                TypeCache cache = Cache;
                if ((cache.Cached & (int)TypeCacheEntries.IsGenericTypeDef) != 0)
                    return (cache.Flags & (int)TypeCacheEntries.IsGenericTypeDef) != 0;
                bool res = RuntimeTypeHandle.IsGenericTypeDefinition(this);
                CacheFlag(TypeCacheEntries.IsGenericTypeDef, res);
                return res;
            }
        }

        public override Type GetGenericTypeDefinition()
        {
            if (!IsGenericType)
                throw new InvalidOperationException(SR.InvalidOperation_NotGenericType);

            return RuntimeTypeHandle.GetGenericTypeDefinition(this);
        }

        public override GenericParameterAttributes GenericParameterAttributes
        {
            get
            {
                if (!IsGenericParameter)
                    throw new InvalidOperationException(SR.Arg_NotGenericParameter);

                return GetGenericParameterAttributes();
            }
        }

        #endregion

        #region Generics

        internal RuntimeType[] GetGenericArgumentsInternal()
        {
            RuntimeType[]? res = null;
            var this_type = this;
            GetGenericArgumentsInternal(new QCallTypeHandle(ref this_type), ObjectHandleOnStack.Create(ref res), true);
            return res!;
        }

        public override Type[] GetGenericArguments()
        {
            Type[]? types = null;
            var this_type = this;
            GetGenericArgumentsInternal(new QCallTypeHandle(ref this_type), ObjectHandleOnStack.Create(ref types), false);
            return types ?? Type.EmptyTypes;
        }

        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override Type MakeGenericType(Type[] instantiation)
        {
            ArgumentNullException.ThrowIfNull(instantiation);

            RuntimeType[] instantiationRuntimeType = new RuntimeType[instantiation.Length];

            if (!IsGenericTypeDefinition)
                throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericTypeDefinition, this));

            RuntimeType[] genericParameters = GetGenericArgumentsInternal();

            if (genericParameters.Length != instantiation.Length)
                throw new ArgumentException(SR.Argument_GenericArgsCount, nameof(instantiation));

            for (int i = 0; i < instantiation.Length; i++)
            {
                Type instantiationElem = instantiation[i];
                if (instantiationElem == null)
                    throw new ArgumentNullException();

                RuntimeType? rtInstantiationElem = instantiationElem as RuntimeType;

                if (rtInstantiationElem == null)
                {
                    if (instantiationElem.IsSignatureType)
                        return MakeGenericSignatureType(this, instantiation);
                    Type[] instantiationCopy = new Type[instantiation.Length];
                    for (int iCopy = 0; iCopy < instantiation.Length; iCopy++)
                        instantiationCopy[iCopy] = instantiation[iCopy];
                    instantiation = instantiationCopy;
                    if (!RuntimeFeature.IsDynamicCodeSupported)
                        throw new PlatformNotSupportedException();
                    return System.Reflection.Emit.TypeBuilderInstantiation.MakeGenericType(this, instantiation);
                }

                instantiationRuntimeType[i] = rtInstantiationElem;
            }

            SanityCheckGenericArguments(instantiationRuntimeType, genericParameters);

            Type? ret = null;
            MakeGenericType(this, instantiationRuntimeType, ObjectHandleOnStack.Create(ref ret));
            if (ret == null)
                throw new TypeLoadException();
            return ret;
        }

        public override int GenericParameterPosition
        {
            get
            {
                if (!IsGenericParameter)
                    throw new InvalidOperationException(SR.Arg_NotGenericParameter);

                var this_type = this;
                return GetGenericParameterPosition(new QCallTypeHandle(ref this_type));
            }
        }

        #endregion

        public static bool operator ==(RuntimeType? left, RuntimeType? right)
        {
            return ReferenceEquals(left, right);
        }

        public static bool operator !=(RuntimeType? left, RuntimeType? right)
        {
            return !ReferenceEquals(left, right);
        }

        #region Legacy Internal
        private void CreateInstanceCheckThis()
        {
            if (ContainsGenericParameters)
                throw new ArgumentException(SR.Format(SR.Acc_CreateGenericEx, this));

            Type elementType = GetRootElementType();

            if (ReferenceEquals(elementType, typeof(ArgIterator)))
                throw new NotSupportedException(SR.Acc_CreateArgIterator);

            if (ReferenceEquals(elementType, typeof(void)))
                throw new NotSupportedException(SR.Acc_CreateVoid);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2082:UnrecognizedReflectionPattern",
            Justification = "Implementation detail of Activator that linker intrinsically recognizes")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2085:UnrecognizedReflectionPattern",
            Justification = "Implementation detail of Activator that linker intrinsically recognizes")]
        internal object? CreateInstanceImpl(
            BindingFlags bindingAttr, Binder? binder, object?[]? args, CultureInfo? culture)
        {
            CreateInstanceCheckThis();

            object? server;

            try
            {
                try
                {
                    args ??= Array.Empty<object>();

                    int argCnt = args.Length;

                    // Without a binder we need to do use the default binder...
                    binder ??= DefaultBinder;

                    // deal with the __COMObject case first. It is very special because from a reflection point of view it has no ctors
                    // so a call to GetMemberCons would fail
                    bool publicOnly = (bindingAttr & BindingFlags.NonPublic) == 0;
                    bool wrapExceptions = (bindingAttr & BindingFlags.DoNotWrapExceptions) == 0;
                    if (argCnt == 0 && (bindingAttr & BindingFlags.Public) != 0 && (bindingAttr & BindingFlags.Instance) != 0
                        && (IsValueType))
                    {
                        server = CreateInstanceDefaultCtor(publicOnly, wrapExceptions);
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
                                argsType[i] = args[i]!.GetType();
                            }
                        }

                        for (int i = 0; i < candidates.Length; i++)
                        {
                            if (FilterApplyConstructorInfo((RuntimeConstructorInfo)candidates[i], bindingAttr, CallingConventions.Any, argsType))
                                matches.Add(candidates[i]);
                        }

                        MethodBase[]? cons = new MethodBase[matches.Count];
                        matches.CopyTo(cons);
                        if (cons != null && cons.Length == 0)
                            cons = null;

                        if (cons == null)
                        {
                            throw new MissingMethodException(SR.Format(SR.MissingConstructor_Name, FullName));
                        }

                        MethodBase? invokeMethod;
                        object? state = null;

                        try
                        {
                            invokeMethod = binder.BindToMethod(bindingAttr, cons, ref args, null, culture, null, out state);
                        }
                        catch (MissingMethodException) { invokeMethod = null; }

                        if (invokeMethod == null)
                        {
                            throw new MissingMethodException(SR.Format(SR.MissingConstructor_Name, FullName));
                        }

                        if (invokeMethod.GetParametersNoCopy().Length == 0)
                        {
                            if (args.Length != 0)
                            {

                                Debug.Assert((invokeMethod.CallingConvention & CallingConventions.VarArgs) ==
                                                 CallingConventions.VarArgs);
                                throw new NotSupportedException(SR.NotSupported_CallToVarArg);
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
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        internal object? CreateInstanceDefaultCtor(bool publicOnly, bool wrapExceptions)
        {
            if (IsByRefLike)
                throw new NotSupportedException(SR.NotSupported_ByRefLike);

            CreateInstanceCheckThis();

            return CreateInstanceMono(!publicOnly, wrapExceptions);
        }

        // Specialized version of the above for Activator.CreateInstance<T>()
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        internal object? CreateInstanceOfT()
        {
            return CreateInstanceMono(false, true);
        }

        #endregion

        private TypeCache? cache;

        internal TypeCache Cache =>
            Volatile.Read(ref cache) ??
            Interlocked.CompareExchange(ref cache, new TypeCache(), null) ??
            cache;

        [Flags]
        // Types of entries cached in TypeCache
        private enum TypeCacheEntries {
            IsGenericTypeDef = 1,
            IsDelegate = 2,
            IsValueType = 4,
            IsActualEnum = 8,
            IsGenericType = 16,
            CorElementType = 32,
            TypeAttributes = 64,
            DefaultCtor = 128
        }

        internal sealed class TypeCache
        {
            public object? EnumInfo;
            public TypeCode TypeCode;
            // this is the displayed form: special characters
            // ,+*&*[]\ in the identifier portions of the names
            // have been escaped with a leading backslash (\)
            public string? full_name;
            public RuntimeConstructorInfo? default_ctor;
            public CorElementType CorElementType;
            public TypeAttributes TypeAttributes;
            // TypeCacheEntries, accessed using CAS
            public int Flags;
            // TypeCacheEntries, accessed using CAS
            public int Cached;
        }

        private void UpdateCached(TypeCacheEntries entry)
        {
            TypeCache cache = Cache;
            int oldCached = cache.Cached;
            int newCached = oldCached | (int)entry;
            // This CAS will ensure ordering with the the store into the cache
            // If this fails, we will just take the slowpath again
            Interlocked.CompareExchange(ref cache.Cached, newCached, oldCached);
        }

        private void CacheFlag(TypeCacheEntries flag, bool value)
        {
            TypeCache cache = Cache;
            int oldFlags = cache.Flags;
            int newFlags = value ? (oldFlags | (int)flag) : oldFlags;
            // If this fails, we will just take the slowpath again
            if (Interlocked.CompareExchange(ref cache.Flags, newFlags, oldFlags) == oldFlags)
                UpdateCached(flag);
        }

        internal RuntimeType(object obj)
        {
            throw new NotImplementedException();
        }

        internal RuntimeConstructorInfo? GetDefaultConstructor()
        {
            TypeCache cache = Cache;
            RuntimeConstructorInfo? ctor = null;

            if ((cache.Cached & (int)TypeCacheEntries.DefaultCtor) != 0)
                return cache.default_ctor;

            ListBuilder<ConstructorInfo> ctors = GetConstructorCandidates(
                null,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, CallingConventions.Any,
                Type.EmptyTypes, false);

            if (ctors.Count == 1)
                cache.default_ctor = ctor = (RuntimeConstructorInfo)ctors[0];
            Interlocked.MemoryBarrier ();

            // Note down even if we found no constructors
            UpdateCached(TypeCacheEntries.DefaultCtor);

            return ctor;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern MemberInfo GetCorrespondingInflatedMethod(QCallTypeHandle type, MemberInfo generic);

        internal override MethodInfo GetMethod(MethodInfo fromNoninstanciated)
        {
            ArgumentNullException.ThrowIfNull(fromNoninstanciated);
            var this_type = this;
            return (MethodInfo)GetCorrespondingInflatedMethod(new QCallTypeHandle(ref this_type), fromNoninstanciated);
        }

        internal override ConstructorInfo GetConstructor(ConstructorInfo fromNoninstanciated)
        {
            ArgumentNullException.ThrowIfNull(fromNoninstanciated);
            var this_type = this;
            return (ConstructorInfo)GetCorrespondingInflatedMethod(new QCallTypeHandle(ref this_type), fromNoninstanciated);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2085:UnrecognizedReflectionPattern",
            Justification = "We already have a FieldInfo so this will succeed")]
        internal override FieldInfo GetField(FieldInfo fromNoninstanciated)
        {
            /* create sensible flags from given FieldInfo */
            BindingFlags flags = fromNoninstanciated.IsStatic ? BindingFlags.Static : BindingFlags.Instance;
            flags |= fromNoninstanciated.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic;
            return GetField(fromNoninstanciated.Name, flags)!;
        }

        private string? GetDefaultMemberName()
        {
            object[] att = GetCustomAttributes(typeof(DefaultMemberAttribute), true);
            return att.Length != 0 ? ((DefaultMemberAttribute)att[0]).MemberName : null;
        }

        private object? CreateInstanceMono(bool nonPublic, bool wrapExceptions)
        {
            RuntimeConstructorInfo? ctor = GetDefaultConstructor();
            if (!nonPublic && ctor != null && !ctor.IsPublic)
            {
                throw new MissingMethodException(SR.Format(SR.Arg_NoDefCTor, this));
            }

            if (ctor == null)
            {
                Type elementType = this.GetRootElementType();
                if (ReferenceEquals(elementType, typeof(TypedReference)) || ReferenceEquals(elementType, typeof(RuntimeArgumentHandle)))
                    throw new NotSupportedException(SR.NotSupported_ContainsStackPtr);

                if (IsValueType)
                {
                    var this_type = this;
                    return CreateInstanceInternal(new QCallTypeHandle(ref this_type));
                }

                throw new MissingMethodException(SR.Format(SR.Arg_NoDefCTor, this));
            }

            // TODO: .net does more checks in unmanaged land in RuntimeTypeHandle::CreateInstance
            if (IsAbstract)
            {
                throw new MissingMethodException(SR.Format(SR.Acc_CreateAbstEx, this));
            }

            return ctor.Invoker.InvokeWithNoArgs(obj: null, wrapExceptions ? BindingFlags.Default : BindingFlags.DoNotWrapExceptions);
        }

        // FIXME Reuse with coreclr
        private CheckValueStatus TryChangeTypeSpecial(
            ref object value)
        {
            if (IsEnum)
            {
                Type? type = Enum.GetUnderlyingType(this);
                if (type == value.GetType())
                    return CheckValueStatus.Success;

                object? res = IsConvertibleToPrimitiveType(value, type);
                if (res != null)
                {
                    value = res;
                    return CheckValueStatus.Success;
                }
            }
            else if (IsPrimitive)
            {
                object? res = IsConvertibleToPrimitiveType(value, this);
                if (res != null)
                {
                    value = res;
                    return CheckValueStatus.Success;
                }
            }
            else if (IsPointer)
            {
                if (value is IntPtr or UIntPtr)
                    return CheckValueStatus.Success;

                if (value is Pointer pointer)
                {
                    Type pointerType = pointer.GetPointerType();
                    if (pointerType == this)
                    {
                        value = pointer.GetPointerValue();
                        return CheckValueStatus.Success;
                    }
                }
            }
            else if (IsFunctionPointer)
            {
                if (value is IntPtr or UIntPtr)
                    return CheckValueStatus.Success;

                value = (IntPtr)value;
                return CheckValueStatus.Success;
            }

            return CheckValueStatus.ArgumentException;
        }

        // Binder uses some incompatible conversion rules. For example
        // int value cannot be used with decimal parameter but in other
        // ways it's more flexible than normal convertor, for example
        // long value can be used with int based enum
        private static object? IsConvertibleToPrimitiveType(object value, Type targetType)
        {
            Type? type = value.GetType();
            if (type.IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
                if (type == targetType)
                    return value;
            }

            TypeCode from = GetTypeCode(type);
            TypeCode to = GetTypeCode(targetType);

            switch (to)
            {
                case TypeCode.Char:
                    switch (from)
                    {
                        case TypeCode.Byte:
                            return (char)(byte)value;
                        case TypeCode.UInt16:
                            return value;
                    }
                    break;
                case TypeCode.Int16:
                    switch (from)
                    {
                        case TypeCode.Byte:
                            return (short)(byte)value;
                        case TypeCode.SByte:
                            return (short)(sbyte)value;
                    }
                    break;
                case TypeCode.UInt16:
                    switch (from)
                    {
                        case TypeCode.Byte:
                            return (ushort)(byte)value;
                        case TypeCode.Char:
                            return value;
                    }
                    break;
                case TypeCode.Int32:
                    switch (from)
                    {
                        case TypeCode.Byte:
                            return (int)(byte)value;
                        case TypeCode.SByte:
                            return (int)(sbyte)value;
                        case TypeCode.Char:
                            return (int)(char)value;
                        case TypeCode.Int16:
                            return (int)(short)value;
                        case TypeCode.UInt16:
                            return (int)(ushort)value;
                    }
                    break;
                case TypeCode.UInt32:
                    switch (from)
                    {
                        case TypeCode.Byte:
                            return (uint)(byte)value;
                        case TypeCode.Char:
                            return (uint)(char)value;
                        case TypeCode.UInt16:
                            return (uint)(ushort)value;
                    }
                    break;
                case TypeCode.Int64:
                    switch (from)
                    {
                        case TypeCode.Byte:
                            return (long)(byte)value;
                        case TypeCode.SByte:
                            return (long)(sbyte)value;
                        case TypeCode.Int16:
                            return (long)(short)value;
                        case TypeCode.Char:
                            return (long)(char)value;
                        case TypeCode.UInt16:
                            return (long)(ushort)value;
                        case TypeCode.Int32:
                            return (long)(int)value;
                        case TypeCode.UInt32:
                            return (long)(uint)value;
                    }
                    break;
                case TypeCode.UInt64:
                    switch (from)
                    {
                        case TypeCode.Byte:
                            return (ulong)(byte)value;
                        case TypeCode.Char:
                            return (ulong)(char)value;
                        case TypeCode.UInt16:
                            return (ulong)(ushort)value;
                        case TypeCode.UInt32:
                            return (ulong)(uint)value;
                    }
                    break;
                case TypeCode.Single:
                    switch (from)
                    {
                        case TypeCode.Byte:
                            return (float)(byte)value;
                        case TypeCode.SByte:
                            return (float)(sbyte)value;
                        case TypeCode.Int16:
                            return (float)(short)value;
                        case TypeCode.Char:
                            return (float)(char)value;
                        case TypeCode.UInt16:
                            return (float)(ushort)value;
                        case TypeCode.Int32:
                            return (float)(int)value;
                        case TypeCode.UInt32:
                            return (float)(uint)value;
                        case TypeCode.Int64:
                            return (float)(long)value;
                        case TypeCode.UInt64:
                            return (float)(ulong)value;
                    }
                    break;
                case TypeCode.Double:
                    switch (from)
                    {
                        case TypeCode.Byte:
                            return (double)(byte)value;
                        case TypeCode.SByte:
                            return (double)(sbyte)value;
                        case TypeCode.Char:
                            return (double)(char)value;
                        case TypeCode.Int16:
                            return (double)(short)value;
                        case TypeCode.UInt16:
                            return (double)(ushort)value;
                        case TypeCode.Int32:
                            return (double)(int)value;
                        case TypeCode.UInt32:
                            return (double)(uint)value;
                        case TypeCode.Int64:
                            return (double)(long)value;
                        case TypeCode.UInt64:
                            return (double)(ulong)value;
                        case TypeCode.Single:
                            return (double)(float)value;
                    }
                    break;
            }

            // Everything else is rejected
            return null;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void make_array_type(QCallTypeHandle type, int rank, ObjectHandleOnStack res);

        public override Type MakeArrayType()
        {
            Type? type = null;
            var base_type = this;
            make_array_type(new QCallTypeHandle(ref base_type), 0, ObjectHandleOnStack.Create(ref type));
            return type!;
        }

        public override Type MakeArrayType(int rank)
        {
            if (rank < 1)
                throw new IndexOutOfRangeException();

            Type? type = null;
            var base_type = this;
            make_array_type(new QCallTypeHandle(ref base_type), rank, ObjectHandleOnStack.Create(ref type));
            return type!;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void make_byref_type(QCallTypeHandle type, ObjectHandleOnStack res);

        public override Type MakeByRefType()
        {
            if (IsByRef)
                throw new TypeLoadException(SR.Format(SR.CannotCreateByRefOfByRef, GetType()));
            Type? type = null;
            var base_type = this;
            make_byref_type(new QCallTypeHandle(ref base_type), ObjectHandleOnStack.Create(ref type));
            return type!;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void make_pointer_type(QCallTypeHandle type, ObjectHandleOnStack res);

        public override Type MakePointerType()
        {
            if (IsByRef)
                throw new TypeLoadException(SR.Format(SR.CannotCreatePointerOfByRef, GetType()));
            Type? type = null;
            var base_type = this;
            make_pointer_type(new QCallTypeHandle(ref base_type), ObjectHandleOnStack.Create(ref type));
            return type!;
        }

        public override StructLayoutAttribute? StructLayoutAttribute
        {
            get
            {
                return GetStructLayoutAttribute();
            }
        }

        public override bool ContainsGenericParameters
        {
            get
            {
                if (IsGenericParameter)
                    return true;

                if (IsGenericType)
                {
                    foreach (Type arg in GetGenericArguments())
                        if (arg.ContainsGenericParameters)
                            return true;
                }

                if (HasElementType)
                    return GetElementType().ContainsGenericParameters;

                if (IsFunctionPointer)
                {
                    if (GetFunctionPointerReturnType().ContainsGenericParameters)
                        return true;

                    foreach (Type arg in GetFunctionPointerParameterTypes())
                        if (arg.ContainsGenericParameters)
                            return true;
                }

                return false;
            }
        }

        public override Type[] GetGenericParameterConstraints()
        {
            if (!IsGenericParameter)
                throw new InvalidOperationException(SR.Arg_NotGenericParameter);

            var paramInfo = new Mono.RuntimeGenericParamInfoHandle(RuntimeTypeHandle.GetGenericParameterInfo(this));
            Type[] constraints = paramInfo.Constraints;

            return constraints ?? Type.EmptyTypes;
        }

        internal static object CreateInstanceForAnotherGenericParameter(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type genericType,
            RuntimeType genericArgument,
            RuntimeType? genericArgument2 = null)
        {
            RuntimeType? gt = null;
            if (genericArgument2 != null)
                MakeGenericType(genericType, new Type[] { genericArgument, genericArgument2 }, ObjectHandleOnStack.Create(ref gt));
            else
                MakeGenericType(genericType, new Type[] { genericArgument }, ObjectHandleOnStack.Create(ref gt));
            RuntimeConstructorInfo? ctor = gt!.GetDefaultConstructor();

            // CreateInstanceForAnotherGenericParameter requires type to have a public parameterless constructor so it can be annotated for trimming without preserving private constructors.
            if (ctor is null || !ctor.IsPublic)
                throw new MissingMethodException(SR.Format(SR.Arg_NoDefCTor, gt!));

            return ctor.Invoker.InvokeWithNoArgs(obj: null, invokeAttr: default)!;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void MakeGenericType(Type gt, Type[] types, ObjectHandleOnStack res);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr GetMethodsByName_native(QCallTypeHandle type, IntPtr namePtr, BindingFlags bindingAttr, MemberListType listType);

        internal RuntimeMethodInfo[] GetMethodsByName(string? name, BindingFlags bindingAttr, MemberListType listType, RuntimeType reflectedType)
        {
            var this_type = this;
            var refh = new RuntimeTypeHandle(reflectedType);
            using (var namePtr = new Mono.SafeStringMarshal(name))
            using (var h = new Mono.SafeGPtrArrayHandle(GetMethodsByName_native(new QCallTypeHandle(ref this_type), namePtr.Value, bindingAttr, listType)))
            {
                int n = h.Length;
                var a = new RuntimeMethodInfo[n];
                for (int i = 0; i < n; i++)
                {
                    var mh = new RuntimeMethodHandle(h[i]);
                    a[i] = (RuntimeMethodInfo)RuntimeMethodInfo.GetMethodFromHandleNoGenericCheck(mh, refh);
                }
                return a;
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr GetPropertiesByName_native(QCallTypeHandle type, IntPtr name, BindingFlags bindingAttr, MemberListType listType);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr GetConstructors_native(QCallTypeHandle type, BindingFlags bindingAttr);

        private RuntimeConstructorInfo[] GetConstructors_internal(BindingFlags bindingAttr, RuntimeType reflectedType)
        {
            var refh = new RuntimeTypeHandle(reflectedType);
            var this_type = this;
            using (var h = new Mono.SafeGPtrArrayHandle(GetConstructors_native(new QCallTypeHandle(ref this_type), bindingAttr)))
            {
                int n = h.Length;
                var a = new RuntimeConstructorInfo[n];
                for (int i = 0; i < n; i++)
                {
                    var mh = new RuntimeMethodHandle(h[i]);
                    a[i] = (RuntimeConstructorInfo)RuntimeMethodInfo.GetMethodFromHandleNoGenericCheck(mh, refh);
                }
                return a;
            }
        }

        private RuntimePropertyInfo[] GetPropertiesByName(string? name, BindingFlags bindingAttr, MemberListType listType, RuntimeType reflectedType)
        {
            var refh = new RuntimeTypeHandle(reflectedType);
            var this_type = this;
            using (var namePtr = new Mono.SafeStringMarshal(name))
            using (var h = new Mono.SafeGPtrArrayHandle(GetPropertiesByName_native(new QCallTypeHandle(ref this_type), namePtr.Value, bindingAttr, listType)))
            {
                int n = h.Length;
                var a = new RuntimePropertyInfo[n];
                for (int i = 0; i < n; i++)
                {
                    var ph = new Mono.RuntimePropertyHandle(h[i]);
                    a[i] = (RuntimePropertyInfo)RuntimePropertyInfo.GetPropertyFromHandle(ph, refh);
                }
                return a;
            }
        }

        public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType)
        {
            if (IsGenericParameter)
                throw new InvalidOperationException(SR.Arg_GenericParameter);

            ArgumentNullException.ThrowIfNull(interfaceType);

            RuntimeType? ifaceRtType = interfaceType as RuntimeType;

            if (ifaceRtType == null)
                throw new ArgumentException(SR.Argument_MustBeRuntimeType, nameof(interfaceType));

            InterfaceMapping res;
            if (!interfaceType.IsInterface)
                throw new ArgumentException(SR.Arg_MustBeInterface, nameof(interfaceType));
            if (IsInterface)
                throw new ArgumentException(SR.Argument_InterfaceMap);
            var this_type = this;
            res.TargetType = this;
            res.InterfaceType = interfaceType;
            GetInterfaceMapData(new QCallTypeHandle(ref this_type), new QCallTypeHandle(ref ifaceRtType), out res.TargetMethods, out res.InterfaceMethods);
            if (res.TargetMethods == null)
                throw new ArgumentException(SR.Arg_NotFoundIFace, nameof(interfaceType));

            return res;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetInterfaceMapData(QCallTypeHandle t, QCallTypeHandle iface, out MethodInfo[] targets, out MethodInfo[] methods);

        public override Guid GUID
        {
            get
            {
                object[] att = GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), true);
                if (att.Length == 0)
                    return Guid.Empty;
                return new Guid(((System.Runtime.InteropServices.GuidAttribute)att[0]).Value);
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetPacking(QCallTypeHandle type, out int packing, out int size);

        internal void GetPacking(out int packing, out int size)
        {
            var this_type = this;
            GetPacking(new QCallTypeHandle(ref this_type), out packing, out size);
        }

        public override string ToString()
        {
            return getFullName(false, false);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern object CreateInstanceInternal(QCallTypeHandle type);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void AllocateValueType(QCallTypeHandle type, object? value, ObjectHandleOnStack res);

        internal static object AllocateValueType(RuntimeType type, object? value)
        {
            object? res = null;
            AllocateValueType(new QCallTypeHandle(ref type), value, ObjectHandleOnStack.Create(ref res));
            return res!;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetDeclaringMethod(QCallTypeHandle type, ObjectHandleOnStack res);

        public override MethodBase? DeclaringMethod
        {
            get
            {
                var this_type = this;
                MethodBase? res = null;
                GetDeclaringMethod(new QCallTypeHandle(ref this_type), ObjectHandleOnStack.Create(ref res));
                return res;
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void getFullName(QCallTypeHandle type, ObjectHandleOnStack res, bool full_name, bool assembly_qualified);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetGenericArgumentsInternal(QCallTypeHandle type, ObjectHandleOnStack res, bool runtimeArray);

        internal string getFullName(bool full_name, bool assembly_qualified)
        {
            var this_type = this;
            string? res = null;
            getFullName(new QCallTypeHandle(ref this_type), ObjectHandleOnStack.Create(ref res), full_name, assembly_qualified);
            return res!;
        }

        private GenericParameterAttributes GetGenericParameterAttributes()
        {
            return (new Mono.RuntimeGenericParamInfoHandle(RuntimeTypeHandle.GetGenericParameterInfo(this))).Attributes;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int GetGenericParameterPosition(QCallTypeHandle type);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr GetEvents_native(QCallTypeHandle type, IntPtr name, MemberListType listType);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr GetFields_native(QCallTypeHandle type, IntPtr name, BindingFlags bindingAttr, MemberListType listType);

        private RuntimeFieldInfo[] GetFields_internal(string? name, BindingFlags bindingAttr, MemberListType listType, RuntimeType reflectedType)
        {
            var refh = new RuntimeTypeHandle(reflectedType);
            var this_type = this;
            using (var namePtr = new Mono.SafeStringMarshal(name))
            using (var h = new Mono.SafeGPtrArrayHandle(GetFields_native(new QCallTypeHandle(ref this_type), namePtr.Value, bindingAttr, listType)))
            {
                int n = h.Length;
                var a = new RuntimeFieldInfo[n];
                for (int i = 0; i < n; i++)
                {
                    var fh = new RuntimeFieldHandle(h[i]);
                    a[i] = (RuntimeFieldInfo)FieldInfo.GetFieldFromHandle(fh, refh);
                }
                return a;
            }
        }

        private RuntimeEventInfo[] GetEvents_internal(string? name, MemberListType listType, RuntimeType reflectedType)
        {
            var refh = new RuntimeTypeHandle(reflectedType);
            var this_type = this;
            using (var namePtr = new Mono.SafeStringMarshal(name))
            using (var h = new Mono.SafeGPtrArrayHandle(GetEvents_native(new QCallTypeHandle(ref this_type), namePtr.Value, listType)))
            {
                int n = h.Length;
                var a = new RuntimeEventInfo[n];
                for (int i = 0; i < n; i++)
                {
                    var eh = new Mono.RuntimeEventHandle(h[i]);
                    a[i] = (RuntimeEventInfo)RuntimeEventInfo.GetEventFromHandle(eh, refh);
                }
                return a;
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetInterfaces(QCallTypeHandle type, ObjectHandleOnStack res);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type[] GetInterfaces()
        {
            var this_type = this;
            Type[]? res = null;
            GetInterfaces(new QCallTypeHandle(ref this_type), ObjectHandleOnStack.Create(ref res));
            return res!;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern IntPtr GetNestedTypes_native(QCallTypeHandle type, IntPtr name, BindingFlags bindingAttr, MemberListType listType);

        private RuntimeType[] GetNestedTypes_internal(string? displayName, BindingFlags bindingAttr, MemberListType listType)
        {
            string? internalName = null;
            if (displayName != null)
                internalName = displayName;
            var this_type = this;
            using (var namePtr = new Mono.SafeStringMarshal(internalName))
            using (var h = new Mono.SafeGPtrArrayHandle(GetNestedTypes_native(new QCallTypeHandle(ref this_type), namePtr.Value, bindingAttr, listType)))
            {
                int n = h.Length;
                var a = new RuntimeType[n];
                for (int i = 0; i < n; i++)
                {
                    var th = new RuntimeTypeHandle(h[i]);
                    a[i] = (RuntimeType)GetTypeFromHandle(th)!;
                }
                return a;
            }
        }

        public override string? AssemblyQualifiedName
        {
            get
            {
                return getFullName(true, true);
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetDeclaringType(QCallTypeHandle type, ObjectHandleOnStack res);

        public override Type? DeclaringType
        {
            get
            {
                var this_type = this;
                Type? res = null;
                GetDeclaringType(new QCallTypeHandle(ref this_type), ObjectHandleOnStack.Create(ref res));
                return res;
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetName(QCallTypeHandle type, ObjectHandleOnStack res);

        public override string Name
        {
            get
            {
                var this_type = this;
                string? res = null;
                GetName(new QCallTypeHandle(ref this_type), ObjectHandleOnStack.Create(ref res));
                return res!;
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetNamespace(QCallTypeHandle type, ObjectHandleOnStack res);

        public override string Namespace
        {
            get
            {
                var this_type = this;
                string? res = null;
                GetNamespace(new QCallTypeHandle(ref this_type), ObjectHandleOnStack.Create(ref res));
                return res!;
            }
        }

        public override string? FullName
        {
            get
            {
                // See https://github.com/mono/mono/issues/18180 and
                // https://github.com/dotnet/runtime/blob/69e114c1abf91241a0eeecf1ecceab4711b8aa62/src/coreclr/System.Private.CoreLib/src/System/RuntimeType.CoreCLR.cs#L1505-L1509
                if (ContainsGenericParameters && !GetRootElementType().IsGenericTypeDefinition)
                    return null;

                string? fullName;
                TypeCache? cache = Cache;
                if ((fullName = cache.full_name) == null)
                    fullName = cache.full_name = getFullName(true, false);

                return fullName;
            }
        }

        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other) => HasSameMetadataDefinitionAsCore<RuntimeType>(other);

        internal bool IsNullableOfT => Nullable.GetUnderlyingType(this) != null;

        public override bool IsSZArray
        {
            get
            {
                return RuntimeTypeHandle.IsSzArray(this);
            }
        }

        public override bool IsFunctionPointer => RuntimeTypeHandle.IsFunctionPointer(this);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool IsUnmanagedFunctionPointerInternal(QCallTypeHandle type);
        internal static bool IsUnmanagedFunctionPointerInternal(RuntimeType type)
        {
            return IsUnmanagedFunctionPointerInternal(new QCallTypeHandle(ref type));
        }

        public override bool IsUnmanagedFunctionPointer => IsUnmanagedFunctionPointerInternal(this);

        public override Type[] GetFunctionPointerParameterTypes()
        {
            Type[] parameters = FunctionPointerReturnAndParameterTypes(this, false);
            return parameters.Length == 0 ? EmptyTypes : parameters;
        }

        public override Type GetFunctionPointerReturnType()
        {
            return FunctionPointerReturnAndParameterTypes(this, true)[0];
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern IntPtr FunctionPointerReturnAndParameterTypes(QCallTypeHandle type);
        internal static Type[] FunctionPointerReturnAndParameterTypes(RuntimeType type, bool returnType)
        {
            if (!RuntimeTypeHandle.IsFunctionPointer(type))
            {
                throw new InvalidOperationException(SR.InvalidOperation_NotFunctionPointer);
            }

            using (var arrayOfTypeHandles = new Mono.SafeGPtrArrayHandle(FunctionPointerReturnAndParameterTypes(new QCallTypeHandle(ref type))))
            {
                int typeNum = returnType ? 1 : arrayOfTypeHandles.Length - 1;
                var fPtrReturnAndParameterTypes = new Type[typeNum];

                if (returnType)
                {
                    var typeHandle = new RuntimeTypeHandle(arrayOfTypeHandles[0]);
                    fPtrReturnAndParameterTypes[0] = (RuntimeType)GetTypeFromHandle(typeHandle)!;
                }
                else
                {
                    for (int i = 1; i < typeNum + 1; i++)
                    {
                        var typeHandle = new RuntimeTypeHandle(arrayOfTypeHandles[i]);
                        fPtrReturnAndParameterTypes[i-1] = (RuntimeType)GetTypeFromHandle(typeHandle)!;
                    }
                }
                return fPtrReturnAndParameterTypes;
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern Type[] GetFunctionPointerTypeModifiers(QCallTypeHandle type, int position, bool optional);

        internal static Type[] GetFunctionPointerTypeModifiers(RuntimeType type, int position, bool optional) => GetFunctionPointerTypeModifiers(new QCallTypeHandle(ref type), position, optional) ?? Type.EmptyTypes;

        public Type[] GetCustomModifiersFromFunctionPointer(int position, bool optional) => GetFunctionPointerTypeModifiers(this, position, optional);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern byte GetCallingConventionFromFunctionPointerInternal(QCallTypeHandle type);

        internal static byte GetCallingConventionFromFunctionPointerInternal(RuntimeType type) => GetCallingConventionFromFunctionPointerInternal(new QCallTypeHandle(ref type));

        public SignatureCallingConvention GetCallingConventionFromFunctionPointer() => (SignatureCallingConvention)GetCallingConventionFromFunctionPointerInternal(this);

        public override Type[] GetFunctionPointerCallingConventions()
        {
            if (!RuntimeTypeHandle.IsFunctionPointer(this))
                throw new InvalidOperationException(SR.InvalidOperation_NotFunctionPointer);

            // Requires a modified type to return the modifiers.
            return EmptyTypes;
        }

        internal override bool IsUserType
        {
            get
            {
                return false;
            }
        }

        public override bool IsSubclassOf(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            RuntimeType? rtType = type as RuntimeType;
            if (rtType == null)
                return false;

            return RuntimeTypeHandle.IsSubclassOf(this, rtType);
        }

        internal StructLayoutAttribute? GetStructLayoutAttribute()
        {
            if (IsInterface || HasElementType || IsGenericParameter)
                return null;

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

            GetPacking(out int pack, out int size);

            return new StructLayoutAttribute(layoutKind) { Pack = pack, Size = size, CharSet = charSet };
        }
    }
}
