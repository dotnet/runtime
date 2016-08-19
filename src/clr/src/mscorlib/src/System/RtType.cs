// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Implements System.RuntimeType
//
// ======================================================================================


using System;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Globalization;
using System.Threading;
using System.Diagnostics;
using System.Security.Permissions;
using System.Collections;
using System.Collections.Generic;
using System.Runtime;
using System.Runtime.Serialization;    
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Runtime.Remoting;
#if FEATURE_REMOTING
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Activation;
using System.Runtime.Remoting.Metadata;
#endif
using MdSigCallingConvention = System.Signature.MdSigCallingConvention;
using RuntimeTypeCache = System.RuntimeType.RuntimeTypeCache;
using System.Runtime.InteropServices;
using DebuggerStepThroughAttribute = System.Diagnostics.DebuggerStepThroughAttribute;
using MdToken = System.Reflection.MetadataToken;
using System.Runtime.Versioning;
using System.Diagnostics.Contracts;

namespace System 
{
    // this is a work around to get the concept of a calli. It's not as fast but it would be interesting to 
    // see how it compares to the current implementation. 
    // This delegate will disappear at some point in favor of calli 

    internal delegate void CtorDelegate(Object instance);

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
    internal class RuntimeType : 
        System.Reflection.TypeInfo, ISerializable, ICloneable
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
                    return EmptyArray<T>.Value;
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

        internal class RuntimeTypeCache
        {
            private const int MAXNAMELEN = 1024;

            #region Definitions
            internal enum CacheType
            {
                Method,
                Constructor,
                Field,
                Property,
                Event,
                Interface,
                NestedType
            }

            private struct Filter
            {
                private Utf8String m_name;
                private MemberListType m_listType;
                private uint m_nameHash;

                [System.Security.SecurityCritical]  // auto-generated
                public unsafe Filter(byte* pUtf8Name, int cUtf8Name, MemberListType listType)
                {
                    this.m_name = new Utf8String((void*) pUtf8Name, cUtf8Name);
                    this.m_listType = listType;
                    this.m_nameHash = 0;

                    if (RequiresStringComparison())
                    {
                        m_nameHash = m_name.HashCaseInsensitive();
                    }
                }

                public bool Match(Utf8String name) 
                {
                    bool retVal = true;
                    
                    if (m_listType == MemberListType.CaseSensitive)
                        retVal = m_name.Equals(name);
                    else if (m_listType == MemberListType.CaseInsensitive)
                        retVal = m_name.EqualsCaseInsensitive(name);

                    // Currently the callers of UsesStringComparison assume that if it returns false
                    // then the match always succeeds and can be skipped.  Assert that this is maintained.
                    Contract.Assert(retVal || RequiresStringComparison());

                    return retVal;
                }

                // Does the current match type require a string comparison?
                // If not, we know Match will always return true and the call can be skipped
                // If so, we know we can have a valid hash to check against from GetHashToMatch
                public bool RequiresStringComparison()
                {
                    return (m_listType == MemberListType.CaseSensitive) ||
                           (m_listType == MemberListType.CaseInsensitive);
                }

                public bool CaseSensitive()
                {
                    return (m_listType == MemberListType.CaseSensitive);
                }

                public uint GetHashToMatch()
                {
                    Contract.Assert(RequiresStringComparison());

                    return m_nameHash;
                }
            }

            private class MemberInfoCache<T> where T : MemberInfo
            {
                #region Private Data Members

                // MemberInfo caches
                private CerHashtable<string, T[]> m_csMemberInfos;
                private CerHashtable<string, T[]> m_cisMemberInfos;
                // List of MemberInfos given out. When m_cacheComplete is false, it may have null entries at the end to avoid
                // reallocating the list every time a new entry is added.
                private T[] m_allMembers;
                private bool m_cacheComplete;

                // This is the strong reference back to the cache
                private RuntimeTypeCache m_runtimeTypeCache;
                #endregion

                #region Constructor
#if MDA_SUPPORTED
                [System.Security.SecuritySafeCritical]  // auto-generated
#endif
                internal MemberInfoCache(RuntimeTypeCache runtimeTypeCache)
                {
#if MDA_SUPPORTED
                    Mda.MemberInfoCacheCreation();
#endif
                    m_runtimeTypeCache = runtimeTypeCache;
                }

                [System.Security.SecuritySafeCritical]  // auto-generated
                internal MethodBase AddMethod(RuntimeType declaringType, RuntimeMethodHandleInternal method, CacheType cacheType)
                {
                    T[] list = null;
                    MethodAttributes methodAttributes = RuntimeMethodHandle.GetAttributes(method);
                    bool isPublic = (methodAttributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;
                    bool isStatic = (methodAttributes & MethodAttributes.Static) != 0;
                    bool isInherited = declaringType != ReflectedType;
                    BindingFlags bindingFlags = RuntimeType.FilterPreCalculate(isPublic, isInherited, isStatic);
                    switch (cacheType)
                    {
                    case CacheType.Method:
                        list = (T[])(object)new RuntimeMethodInfo[1] {
                            new RuntimeMethodInfo(method, declaringType, m_runtimeTypeCache, methodAttributes, bindingFlags, null)
                        };
                        break;
                    case CacheType.Constructor:
                        list = (T[])(object)new RuntimeConstructorInfo[1] { 
                            new RuntimeConstructorInfo(method, declaringType, m_runtimeTypeCache, methodAttributes, bindingFlags) 
                        };
                        break;
                    }

                    Insert(ref list, null, MemberListType.HandleToInfo);

                    return (MethodBase)(object)list[0];
                }

                [System.Security.SecuritySafeCritical]  // auto-generated
                internal FieldInfo AddField(RuntimeFieldHandleInternal field)
                {
                    // create the runtime field info
                    FieldAttributes fieldAttributes = RuntimeFieldHandle.GetAttributes(field);
                    bool isPublic = (fieldAttributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public;
                    bool isStatic = (fieldAttributes & FieldAttributes.Static) != 0;
                    RuntimeType approxDeclaringType = RuntimeFieldHandle.GetApproxDeclaringType(field);
                    bool isInherited = RuntimeFieldHandle.AcquiresContextFromThis(field) ?
                        !RuntimeTypeHandle.CompareCanonicalHandles(approxDeclaringType, ReflectedType) :
                        approxDeclaringType != ReflectedType;

                    BindingFlags bindingFlags = RuntimeType.FilterPreCalculate(isPublic, isInherited, isStatic);

                    T[] list = (T[])(object)new RuntimeFieldInfo[1] { 
                        new RtFieldInfo(field, ReflectedType, m_runtimeTypeCache, bindingFlags)
                    };

                    Insert(ref list, null, MemberListType.HandleToInfo);

                    return (FieldInfo)(object)list[0];
                }

                [System.Security.SecuritySafeCritical]  // auto-generated
                private unsafe T[] Populate(string name, MemberListType listType, CacheType cacheType)
                {
                    T[] list = null;

                    if (name == null || name.Length == 0 ||
                        (cacheType == CacheType.Constructor && name.FirstChar != '.' && name.FirstChar != '*'))
                    {
                        list = GetListByName(null, 0, null, 0, listType, cacheType);
                    }
                    else
                    {
                        int cNameLen = name.Length;
                        fixed (char* pName = name)
                        {
                            int cUtf8Name = Encoding.UTF8.GetByteCount(pName, cNameLen);
                            // allocating on the stack is faster than allocating on the GC heap
                            // but we surely don't want to cause a stack overflow
                            // no one should be looking for a member whose name is longer than 1024
                            if (cUtf8Name > MAXNAMELEN)
                            {
                                fixed (byte* pUtf8Name = new byte[cUtf8Name])
                                {
                                    list = GetListByName(pName, cNameLen, pUtf8Name, cUtf8Name, listType, cacheType);
                                }
                            }
                            else
                            {
                                byte* pUtf8Name = stackalloc byte[cUtf8Name];
                                list = GetListByName(pName, cNameLen, pUtf8Name, cUtf8Name, listType, cacheType);
                            }
                        }
                    }

                    Insert(ref list, name, listType);

                    return list;
                }

                [System.Security.SecurityCritical]  // auto-generated
                private unsafe T[] GetListByName(char* pName, int cNameLen, byte* pUtf8Name, int cUtf8Name, MemberListType listType, CacheType cacheType)
                {
                    if (cNameLen != 0)
                        Encoding.UTF8.GetBytes(pName, cNameLen, pUtf8Name, cUtf8Name);

                    Filter filter = new Filter(pUtf8Name, cUtf8Name, listType);
                    Object list = null;

                    switch (cacheType)
                    {
                        case CacheType.Method:
                            list = PopulateMethods(filter);
                            break;
                        case CacheType.Field:
                            list = PopulateFields(filter);
                            break;
                        case CacheType.Constructor:
                            list = PopulateConstructors(filter);
                            break;
                        case CacheType.Property:
                            list = PopulateProperties(filter);
                            break;
                        case CacheType.Event:
                            list = PopulateEvents(filter);
                            break;
                        case CacheType.NestedType:
                            list = PopulateNestedClasses(filter);
                            break;
                        case CacheType.Interface:
                            list = PopulateInterfaces(filter);
                            break;
                        default:
                            BCLDebug.Assert(false, "Invalid CacheType");
                            break;
                    }

                    return (T[])list;
                }

                // May replace the list with a new one if certain cache
                // lookups succeed.  Also, may modify the contents of the list
                // after merging these new data structures with cached ones.
                [System.Security.SecuritySafeCritical]  // auto-generated
                [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
                internal void Insert(ref T[] list, string name, MemberListType listType)
                {
                    bool lockTaken = false;

                    RuntimeHelpers.PrepareConstrainedRegions();
                    try 
                    { 
                        Monitor.Enter(this, ref lockTaken);

                        switch (listType)
                        {
                            case MemberListType.CaseSensitive:
                                {
                                    // Ensure we always return a list that has 
                                    // been merged with the global list.
                                    T[] cachedList = m_csMemberInfos[name];
                                    if (cachedList == null)
                                    {
                                        MergeWithGlobalList(list);
                                        m_csMemberInfos[name] = list;
                                    }
                                    else
                                        list = cachedList;
                                }
                                break;

                            case MemberListType.CaseInsensitive:
                                {
                                    // Ensure we always return a list that has 
                                    // been merged with the global list.
                                    T[] cachedList = m_cisMemberInfos[name];
                                    if (cachedList == null)
                                    {
                                        MergeWithGlobalList(list);
                                        m_cisMemberInfos[name] = list;
                                    }
                                    else
                                        list = cachedList;
                                }
                                break;

                            case MemberListType.All:
                                if (!m_cacheComplete)
                                {
                                    MergeWithGlobalList(list);

                                    // Trim null entries at the end of m_allMembers array
                                    int memberCount = m_allMembers.Length;
                                    while (memberCount > 0)
                                    {
                                        if (m_allMembers[memberCount-1] != null)
                                            break;
                                        memberCount--;
                                    }
                                    Array.Resize(ref m_allMembers, memberCount);

                                    Volatile.Write(ref m_cacheComplete, true);
                                }
                                else
                                    list = m_allMembers;
                                break;

                            default:
                                MergeWithGlobalList(list);
                                break;
                        }
                    }
                    finally
                    {
                        if (lockTaken)
                        {
                            Monitor.Exit(this);
                        }
                    }
                }

                // Modifies the existing list.
                [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
                private void MergeWithGlobalList(T[] list)
                {
                    T[] cachedMembers = m_allMembers;

                    if (cachedMembers == null)
                    {
                        m_allMembers = list;
                        return;
                    }

                    int cachedCount = cachedMembers.Length;
                    int freeSlotIndex = 0;

                    for (int i = 0; i < list.Length; i++)
                    {
                        T newMemberInfo = list[i];
                        bool foundInCache = false;

                        int cachedIndex;
                        for (cachedIndex = 0; cachedIndex < cachedCount; cachedIndex++)
                        {
                            T cachedMemberInfo = cachedMembers[cachedIndex];
                            if (cachedMemberInfo == null)
                                break;

                            if (newMemberInfo.CacheEquals(cachedMemberInfo))
                            {
                                list[i] = cachedMemberInfo;
                                foundInCache = true;
                                break;
                            }
                        }

                        if (!foundInCache)
                        {
                            if (freeSlotIndex == 0)
                                freeSlotIndex = cachedIndex;

                            if (freeSlotIndex >= cachedMembers.Length)
                            {
                                int newSize;
                                if (m_cacheComplete)
                                {
                                    //
                                    // In theory, we should never add more elements to the cache when it is complete.
                                    //
                                    // Unfortunately, we shipped with bugs that cause changes of the complete cache (DevDiv #339308).
                                    // Grow the list by exactly one element in this case to avoid null entries at the end.
                                    //

                                    Contract.Assert(false);

                                    newSize = cachedMembers.Length + 1;
                                }
                                else
                                {
                                    newSize = Math.Max(Math.Max(4, 2 * cachedMembers.Length), list.Length);
                                }

                                // Use different variable for ref argument to Array.Resize to allow enregistration of cachedMembers by the JIT
                                T[] cachedMembers2 = cachedMembers;
                                Array.Resize(ref cachedMembers2, newSize);
                                cachedMembers = cachedMembers2;
                            }

                            Contract.Assert(cachedMembers[freeSlotIndex] == null);
                            cachedMembers[freeSlotIndex] = newMemberInfo;
                            freeSlotIndex++;
                        }
                    }

                    m_allMembers = cachedMembers;
                }
                #endregion

                #region Population Logic

                [System.Security.SecuritySafeCritical]  // auto-generated
                private unsafe RuntimeMethodInfo[] PopulateMethods(Filter filter)
                {
                    ListBuilder<RuntimeMethodInfo> list = new ListBuilder<RuntimeMethodInfo>();

                    RuntimeType declaringType = ReflectedType;
                    Contract.Assert(declaringType != null);

                    if (RuntimeTypeHandle.IsInterface(declaringType))
                    {
                        #region IsInterface

                        foreach (RuntimeMethodHandleInternal methodHandle in RuntimeTypeHandle.GetIntroducedMethods(declaringType))
                        {
                            if (filter.RequiresStringComparison())
                            {
                                if (!RuntimeMethodHandle.MatchesNameHash(methodHandle, filter.GetHashToMatch()))
                                {
                                    Contract.Assert(!filter.Match(RuntimeMethodHandle.GetUtf8Name(methodHandle)));
                                    continue;
                                }

                                if (!filter.Match(RuntimeMethodHandle.GetUtf8Name(methodHandle)))
                                    continue;
                            }

                            #region Loop through all methods on the interface
                            Contract.Assert(!methodHandle.IsNullHandle());
                            // Except for .ctor, .cctor, IL_STUB*, and static methods, all interface methods should be abstract, virtual, and non-RTSpecialName.
                            // Note that this assumption will become invalid when we add support for non-abstract or static methods on interfaces.
                            Contract.Assert(
                                (RuntimeMethodHandle.GetAttributes(methodHandle) & (MethodAttributes.RTSpecialName | MethodAttributes.Abstract | MethodAttributes.Virtual)) == (MethodAttributes.Abstract | MethodAttributes.Virtual) ||
                                (RuntimeMethodHandle.GetAttributes(methodHandle) & MethodAttributes.Static) == MethodAttributes.Static ||
                                RuntimeMethodHandle.GetName(methodHandle).Equals(".ctor") ||
                                RuntimeMethodHandle.GetName(methodHandle).Equals(".cctor") ||
                                RuntimeMethodHandle.GetName(methodHandle).StartsWith("IL_STUB", StringComparison.Ordinal));

                            #region Calculate Binding Flags
                            MethodAttributes methodAttributes = RuntimeMethodHandle.GetAttributes(methodHandle);
                            bool isPublic = (methodAttributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;
                            bool isStatic = (methodAttributes & MethodAttributes.Static) != 0;
                            bool isInherited = false;
                            BindingFlags bindingFlags = RuntimeType.FilterPreCalculate(isPublic, isInherited, isStatic);
                            #endregion

                            if ((methodAttributes & MethodAttributes.RTSpecialName) != 0)
                                continue;

                            // get the unboxing stub or instantiating stub if needed
                            RuntimeMethodHandleInternal instantiatedHandle = RuntimeMethodHandle.GetStubIfNeeded(methodHandle, declaringType, null);

                            RuntimeMethodInfo runtimeMethodInfo = new RuntimeMethodInfo(
                            instantiatedHandle, declaringType, m_runtimeTypeCache, methodAttributes, bindingFlags, null);

                            list.Add(runtimeMethodInfo);
                            #endregion  
                        }
                        #endregion
                    }
                    else
                    {
                        #region IsClass or GenericParameter
                        while(RuntimeTypeHandle.IsGenericVariable(declaringType))
                            declaringType = declaringType.GetBaseType();

                        bool* overrides = stackalloc bool[RuntimeTypeHandle.GetNumVirtuals(declaringType)];
                        bool isValueType = declaringType.IsValueType;

                        do
                        {
                            int vtableSlots = RuntimeTypeHandle.GetNumVirtuals(declaringType);

                            foreach (RuntimeMethodHandleInternal methodHandle in RuntimeTypeHandle.GetIntroducedMethods(declaringType))
                            {
                                if (filter.RequiresStringComparison())
                                {
                                    if (!RuntimeMethodHandle.MatchesNameHash(methodHandle, filter.GetHashToMatch()))
                                    {
                                        Contract.Assert(!filter.Match(RuntimeMethodHandle.GetUtf8Name(methodHandle)));
                                        continue;
                                    }

                                    if (!filter.Match(RuntimeMethodHandle.GetUtf8Name(methodHandle)))
                                        continue;
                                }

                                #region Loop through all methods on the current type
                                Contract.Assert(!methodHandle.IsNullHandle());

                                MethodAttributes methodAttributes = RuntimeMethodHandle.GetAttributes(methodHandle);
                                MethodAttributes methodAccess = methodAttributes & MethodAttributes.MemberAccessMask;

                                #region Continue if this is a constructor
                                Contract.Assert(
                                    (RuntimeMethodHandle.GetAttributes(methodHandle) & MethodAttributes.RTSpecialName) == 0 ||
                                    RuntimeMethodHandle.GetName(methodHandle).Equals(".ctor") ||
                                    RuntimeMethodHandle.GetName(methodHandle).Equals(".cctor"));

                                if ((methodAttributes & MethodAttributes.RTSpecialName) != 0)
                                    continue;
                                #endregion

                                #region Continue if this is a private declared on a base type
                                bool isVirtual = false;
                                int methodSlot = 0;
                                if ((methodAttributes & MethodAttributes.Virtual) != 0)
                                {
                                    // only virtual if actually in the vtableslot range, but GetSlot will
                                    // assert if an EnC method, which can't be virtual, so narrow down first
                                    // before calling GetSlot
                                    methodSlot = RuntimeMethodHandle.GetSlot(methodHandle);
                                    isVirtual = (methodSlot < vtableSlots);
                                }

                                bool isInherited = declaringType != ReflectedType;

                                bool isPrivate = methodAccess == MethodAttributes.Private;
                                if (isInherited && isPrivate && !isVirtual)
                                    continue;

                                #endregion

                                #region Continue if this is a virtual and is already overridden
                                if (isVirtual)
                                {
                                    Contract.Assert(
                                        (methodAttributes & MethodAttributes.Abstract) != 0 ||
                                        (methodAttributes & MethodAttributes.Virtual) != 0 ||
                                        RuntimeMethodHandle.GetDeclaringType(methodHandle) != declaringType);

                                    if (overrides[methodSlot] == true)
                                        continue;

                                    overrides[methodSlot] = true;
                                }
                                else if (isValueType)
                                {
                                    if ((methodAttributes & (MethodAttributes.Virtual | MethodAttributes.Abstract)) != 0)
                                        continue;
                                }
                                else
                                {
                                    Contract.Assert((methodAttributes & (MethodAttributes.Virtual | MethodAttributes.Abstract)) == 0);
                                }
                                #endregion

                                #region Calculate Binding Flags
                                bool isPublic = methodAccess == MethodAttributes.Public;
                                bool isStatic = (methodAttributes & MethodAttributes.Static) != 0;
                                BindingFlags bindingFlags = RuntimeType.FilterPreCalculate(isPublic, isInherited, isStatic);
                                #endregion

                                // get the unboxing stub or instantiating stub if needed
                                RuntimeMethodHandleInternal instantiatedHandle = RuntimeMethodHandle.GetStubIfNeeded(methodHandle, declaringType, null);

                                RuntimeMethodInfo runtimeMethodInfo = new RuntimeMethodInfo(
                                instantiatedHandle, declaringType, m_runtimeTypeCache, methodAttributes, bindingFlags, null);

                                list.Add(runtimeMethodInfo);
                                #endregion
                            }

                            declaringType = RuntimeTypeHandle.GetBaseType(declaringType);
                        } while (declaringType != null);
                        #endregion
                    }

                    return list.ToArray();
                }

                [System.Security.SecuritySafeCritical]  // auto-generated
                private RuntimeConstructorInfo[] PopulateConstructors(Filter filter)
                {
                    if (ReflectedType.IsGenericParameter)
                    {
                        return EmptyArray<RuntimeConstructorInfo>.Value;
                    }

                    ListBuilder<RuntimeConstructorInfo> list = new ListBuilder<RuntimeConstructorInfo>();

                    RuntimeType declaringType= ReflectedType;

                    foreach (RuntimeMethodHandleInternal methodHandle in RuntimeTypeHandle.GetIntroducedMethods(declaringType))
                    {
                        if (filter.RequiresStringComparison())
                        {
                            if (!RuntimeMethodHandle.MatchesNameHash(methodHandle, filter.GetHashToMatch()))
                            {
                                Contract.Assert(!filter.Match(RuntimeMethodHandle.GetUtf8Name(methodHandle)));
                                continue;
                            }

                            if (!filter.Match(RuntimeMethodHandle.GetUtf8Name(methodHandle)))
                                continue;
                        }

                        MethodAttributes methodAttributes = RuntimeMethodHandle.GetAttributes(methodHandle);

                        Contract.Assert(!methodHandle.IsNullHandle());

                        if ((methodAttributes & MethodAttributes.RTSpecialName) == 0)
                            continue;

                        // Constructors should not be virtual or abstract
                        Contract.Assert(
                            (methodAttributes & MethodAttributes.Abstract) == 0 &&
                            (methodAttributes & MethodAttributes.Virtual) == 0);

                        #region Calculate Binding Flags
                        bool isPublic = (methodAttributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;
                        bool isStatic = (methodAttributes & MethodAttributes.Static) != 0;
                        bool isInherited = false;
                        BindingFlags bindingFlags = RuntimeType.FilterPreCalculate(isPublic, isInherited, isStatic);
                        #endregion

                        // get the unboxing stub or instantiating stub if needed
                        RuntimeMethodHandleInternal instantiatedHandle = RuntimeMethodHandle.GetStubIfNeeded(methodHandle, declaringType, null);

                        RuntimeConstructorInfo runtimeConstructorInfo =
                        new RuntimeConstructorInfo(instantiatedHandle, ReflectedType, m_runtimeTypeCache, methodAttributes, bindingFlags);

                        list.Add(runtimeConstructorInfo);
                    }

                    return list.ToArray();
                }

                [System.Security.SecuritySafeCritical]  // auto-generated
                private unsafe RuntimeFieldInfo[] PopulateFields(Filter filter)
                {
                    ListBuilder<RuntimeFieldInfo> list = new ListBuilder<RuntimeFieldInfo>();
                    
                    RuntimeType declaringType = ReflectedType;

                    #region Populate all static, instance and literal fields
                    while(RuntimeTypeHandle.IsGenericVariable(declaringType))
                        declaringType = declaringType.GetBaseType();

                    while(declaringType != null)
                    {
                        PopulateRtFields(filter, declaringType, ref list);

                        PopulateLiteralFields(filter, declaringType, ref list);

                        declaringType = RuntimeTypeHandle.GetBaseType(declaringType);
                    }
                    #endregion

                    #region Populate Literal Fields on Interfaces
                    if (ReflectedType.IsGenericParameter)
                    {
                        Type[] interfaces = ReflectedType.BaseType.GetInterfaces();
                        
                        for (int i = 0; i < interfaces.Length; i++)
                        {
                            // Populate literal fields defined on any of the interfaces implemented by the declaring type
                            PopulateLiteralFields(filter, (RuntimeType)interfaces[i], ref list);
                            PopulateRtFields(filter, (RuntimeType)interfaces[i], ref list);
                        }                    
                    }
                    else
                    {
                        Type[] interfaces = RuntimeTypeHandle.GetInterfaces(ReflectedType);

                        if (interfaces != null)
                        {
                            for (int i = 0; i < interfaces.Length; i++)
                            {
                                // Populate literal fields defined on any of the interfaces implemented by the declaring type
                                PopulateLiteralFields(filter, (RuntimeType)interfaces[i], ref list);
                                PopulateRtFields(filter, (RuntimeType)interfaces[i], ref list);
                            }                    
                        }
                    }
                    #endregion

                    return list.ToArray();
                }

                [System.Security.SecuritySafeCritical]  // auto-generated
                private unsafe void PopulateRtFields(Filter filter, RuntimeType declaringType, ref ListBuilder<RuntimeFieldInfo> list)
                {
                    IntPtr* pResult = stackalloc IntPtr[64];
                    int count = 64;
                    
                    if (!RuntimeTypeHandle.GetFields(declaringType, pResult, &count))
                    {
                        fixed(IntPtr* pBigResult = new IntPtr[count])
                        {
                            RuntimeTypeHandle.GetFields(declaringType, pBigResult, &count);
                            PopulateRtFields(filter, pBigResult, count, declaringType, ref list);
                        }
                    }
                    else if (count > 0)
                    {
                        PopulateRtFields(filter, pResult, count, declaringType, ref list);
                    }
                }
                    
                [System.Security.SecurityCritical]  // auto-generated
                private unsafe void PopulateRtFields(Filter filter,
                    IntPtr* ppFieldHandles, int count, RuntimeType declaringType, ref ListBuilder<RuntimeFieldInfo> list)
                {
                    Contract.Requires(declaringType != null);
                    Contract.Requires(ReflectedType != null);

                    bool needsStaticFieldForGeneric = RuntimeTypeHandle.HasInstantiation(declaringType) && !RuntimeTypeHandle.ContainsGenericVariables(declaringType);
                    bool isInherited = declaringType != ReflectedType;

                    for(int i = 0; i < count; i ++)
                    {
                        RuntimeFieldHandleInternal runtimeFieldHandle = new RuntimeFieldHandleInternal(ppFieldHandles[i]);

                        if (filter.RequiresStringComparison())
                        {
                            if (!RuntimeFieldHandle.MatchesNameHash(runtimeFieldHandle, filter.GetHashToMatch()))
                            {
                                Contract.Assert(!filter.Match(RuntimeFieldHandle.GetUtf8Name(runtimeFieldHandle)));
                                continue;
                            }

                            if (!filter.Match(RuntimeFieldHandle.GetUtf8Name(runtimeFieldHandle)))
                                continue;
                        }

                        Contract.Assert(!runtimeFieldHandle.IsNullHandle());

                        FieldAttributes fieldAttributes = RuntimeFieldHandle.GetAttributes(runtimeFieldHandle);
                        FieldAttributes fieldAccess = fieldAttributes & FieldAttributes.FieldAccessMask;

                        if (isInherited)
                        {
                            if (fieldAccess == FieldAttributes.Private)
                                continue;
                        }

                        #region Calculate Binding Flags
                        bool isPublic = fieldAccess == FieldAttributes.Public;
                        bool isStatic = (fieldAttributes & FieldAttributes.Static) != 0;
                        BindingFlags bindingFlags = RuntimeType.FilterPreCalculate(isPublic, isInherited, isStatic);
                        #endregion                        
                        
                        // correct the FieldDesc if needed
                        if (needsStaticFieldForGeneric && isStatic)
                            runtimeFieldHandle = RuntimeFieldHandle.GetStaticFieldForGenericType(runtimeFieldHandle, declaringType);

                        RuntimeFieldInfo runtimeFieldInfo = 
                            new RtFieldInfo(runtimeFieldHandle, declaringType, m_runtimeTypeCache, bindingFlags);

                        list.Add(runtimeFieldInfo);
                    }
                }

                [System.Security.SecuritySafeCritical]  // auto-generated
                private unsafe void PopulateLiteralFields(Filter filter, RuntimeType declaringType, ref ListBuilder<RuntimeFieldInfo> list)
                {
                    Contract.Requires(declaringType != null);
                    Contract.Requires(ReflectedType != null);

                    int tkDeclaringType = RuntimeTypeHandle.GetToken(declaringType);

                    // Our policy is that TypeDescs do not have metadata tokens
                    if (MdToken.IsNullToken(tkDeclaringType))
                        return;

                    MetadataImport scope = RuntimeTypeHandle.GetMetadataImport(declaringType);

                    MetadataEnumResult tkFields;
                    scope.EnumFields(tkDeclaringType, out tkFields);

                    for (int i = 0; i < tkFields.Length; i++)
                    {
                        int tkField = tkFields[i];
                        Contract.Assert(MdToken.IsTokenOfType(tkField, MetadataTokenType.FieldDef));
                        Contract.Assert(!MdToken.IsNullToken(tkField));

                        FieldAttributes fieldAttributes;
                        scope.GetFieldDefProps(tkField, out fieldAttributes);

                        FieldAttributes fieldAccess = fieldAttributes & FieldAttributes.FieldAccessMask;

                        if ((fieldAttributes & FieldAttributes.Literal) != 0)
                        {
                            bool isInherited = declaringType != ReflectedType;
                            if (isInherited)
                            {
                                bool isPrivate = fieldAccess == FieldAttributes.Private;
                                if (isPrivate)
                                    continue;
                            }

                            if (filter.RequiresStringComparison())
                            {
                                Utf8String name;
                                name = scope.GetName(tkField);

                                if (!filter.Match(name))
                                    continue;
                            }

                            #region Calculate Binding Flags
                            bool isPublic = fieldAccess == FieldAttributes.Public;
                            bool isStatic = (fieldAttributes & FieldAttributes.Static) != 0;
                            BindingFlags bindingFlags = RuntimeType.FilterPreCalculate(isPublic, isInherited, isStatic);
                            #endregion                                              
                        
                            RuntimeFieldInfo runtimeFieldInfo =
                            new MdFieldInfo(tkField, fieldAttributes, declaringType.GetTypeHandleInternal(), m_runtimeTypeCache, bindingFlags);

                            list.Add(runtimeFieldInfo);
                        }
                    }
                }

                private static void AddElementTypes(Type template, IList<Type> types)
                {   
                    if (!template.HasElementType)
                        return;
                    
                    AddElementTypes(template.GetElementType(), types);
                    
                    for (int i = 0; i < types.Count; i ++)
                    {
                        if (template.IsArray)
                        {
                            if (template.IsSzArray)
                                types[i] = types[i].MakeArrayType();
                            else 
                                types[i] = types[i].MakeArrayType(template.GetArrayRank());
                        }
                        else if (template.IsPointer)
                        {
                            types[i] = types[i].MakePointerType();
                        }
                    }
                }

                private void AddSpecialInterface(ref ListBuilder<RuntimeType> list, Filter filter, RuntimeType iList, bool addSubInterface)
                {
                    if (iList.IsAssignableFrom(ReflectedType))
                    {
                        if (filter.Match(RuntimeTypeHandle.GetUtf8Name(iList)))
                            list.Add(iList);

                        if (addSubInterface)
                        {
                            Type[] iFaces = iList.GetInterfaces();
                            for (int j = 0; j < iFaces.Length; j++)
                            {
                                RuntimeType iFace = (RuntimeType)iFaces[j];
                                if (iFace.IsGenericType && filter.Match(RuntimeTypeHandle.GetUtf8Name(iFace)))
                                    list.Add(iFace);
                            }
                        }
                    }

                }

                [System.Security.SecuritySafeCritical]  // auto-generated
                private RuntimeType[] PopulateInterfaces(Filter filter)
                {
                    ListBuilder<RuntimeType> list = new ListBuilder<RuntimeType>();

                    RuntimeType declaringType = ReflectedType;

                    if (!RuntimeTypeHandle.IsGenericVariable(declaringType))
                    {
                        Type[] ifaces = RuntimeTypeHandle.GetInterfaces(declaringType);

                        if (ifaces != null)
                        {
                            for (int i = 0; i < ifaces.Length; i++)
                            {
                                RuntimeType interfaceType = (RuntimeType)ifaces[i];

                                if (filter.RequiresStringComparison())
                                {
                                    if (!filter.Match(RuntimeTypeHandle.GetUtf8Name(interfaceType)))
                                        continue;
                                }
                            
                                Contract.Assert(interfaceType.IsInterface);
                                list.Add(interfaceType);
                            }
                        }

                        if (ReflectedType.IsSzArray)
                        {
                            RuntimeType arrayType = (RuntimeType)ReflectedType.GetElementType();
                            
                            if (!arrayType.IsPointer)
                            {
                                AddSpecialInterface(ref list, filter, (RuntimeType)typeof(IList<>).MakeGenericType(arrayType), true);
                                
                                // To avoid adding a duplicate IEnumerable<T>, we don't add the sub interfaces of IReadOnlyList.
                                // Instead, we add IReadOnlyCollection<T> separately.
                                AddSpecialInterface(ref list, filter, (RuntimeType)typeof(IReadOnlyList<>).MakeGenericType(arrayType), false);
                                AddSpecialInterface(ref list, filter, (RuntimeType)typeof(IReadOnlyCollection<>).MakeGenericType(arrayType), false);
                            }
                        }
                    }
                    else
                    {
                        List<RuntimeType> al = new List<RuntimeType>();

                        // Get all constraints
                        Type[] constraints = declaringType.GetGenericParameterConstraints();

                        // Populate transitive closure of all interfaces in constraint set
                        for (int i = 0; i < constraints.Length; i++)
                        {
                            RuntimeType constraint = (RuntimeType)constraints[i];
                            if (constraint.IsInterface)
                                al.Add(constraint);

                            Type[] temp = constraint.GetInterfaces();
                            for (int j = 0; j < temp.Length; j++)
                                al.Add(temp[j] as RuntimeType);
                        }

                        // Remove duplicates
                        Dictionary<RuntimeType, RuntimeType> ht = new Dictionary<RuntimeType, RuntimeType>();
                        for (int i = 0; i < al.Count; i++)
                        {
                            RuntimeType constraint = al[i];
                            if (!ht.ContainsKey(constraint))
                                ht[constraint] = constraint;
                        }

                        RuntimeType[] interfaces = new RuntimeType[ht.Values.Count];
                        ht.Values.CopyTo(interfaces, 0);

                        // Populate link-list
                        for (int i = 0; i < interfaces.Length; i++)
                        {
                            if (filter.RequiresStringComparison())
                            {
                                if (!filter.Match(RuntimeTypeHandle.GetUtf8Name(interfaces[i])))
                                    continue;
                            }

                            list.Add(interfaces[i]);
                        }
                    }

                    return list.ToArray();
                }

                [System.Security.SecuritySafeCritical]  // auto-generated
                private unsafe RuntimeType[] PopulateNestedClasses(Filter filter)
                {
                    RuntimeType declaringType = ReflectedType;

                    while (RuntimeTypeHandle.IsGenericVariable(declaringType))
                    {
                        declaringType = declaringType.GetBaseType();
                    }

                    int tkEnclosingType = RuntimeTypeHandle.GetToken(declaringType);

                    // For example, TypeDescs do not have metadata tokens
                    if (MdToken.IsNullToken(tkEnclosingType))
                        return EmptyArray<RuntimeType>.Value;

                    ListBuilder<RuntimeType> list = new ListBuilder<RuntimeType>();

                    RuntimeModule moduleHandle = RuntimeTypeHandle.GetModule(declaringType);
                    MetadataImport scope = ModuleHandle.GetMetadataImport(moduleHandle);

                    MetadataEnumResult tkNestedClasses;
                    scope.EnumNestedTypes(tkEnclosingType, out tkNestedClasses);

                    for (int i = 0; i < tkNestedClasses.Length; i++)
                    {
                        RuntimeType nestedType = null;

                        try
                        {
                            nestedType = ModuleHandle.ResolveTypeHandleInternal(moduleHandle, tkNestedClasses[i], null, null);
                        }
                        catch(System.TypeLoadException)
                        {
                            // In a reflection emit scenario, we may have a token for a class which 
                            // has not been baked and hence cannot be loaded. 
                            continue;
                        }

                        if (filter.RequiresStringComparison())
                        {
                            if (!filter.Match(RuntimeTypeHandle.GetUtf8Name(nestedType)))
                                continue;
                        }

                        list.Add(nestedType);
                    }

                    return list.ToArray();
                }
                
                [System.Security.SecuritySafeCritical]  // auto-generated
                private unsafe RuntimeEventInfo[] PopulateEvents(Filter filter)
                {
                    Contract.Requires(ReflectedType != null);

                    // Do not create the dictionary if we are filtering the properties by name already
                    Dictionary<String, RuntimeEventInfo> csEventInfos = filter.CaseSensitive() ? null :
                        new Dictionary<String, RuntimeEventInfo>();

                    RuntimeType declaringType = ReflectedType;
                    ListBuilder<RuntimeEventInfo> list = new ListBuilder<RuntimeEventInfo>();

                    if (!RuntimeTypeHandle.IsInterface(declaringType))
                    {
                        while(RuntimeTypeHandle.IsGenericVariable(declaringType))
                            declaringType = declaringType.GetBaseType();

                        // Populate associates off of the class hierarchy
                        while(declaringType != null)
                        {
                            PopulateEvents(filter, declaringType, csEventInfos, ref list);
                            declaringType = RuntimeTypeHandle.GetBaseType(declaringType);
                        }
                    }
                    else
                    {
                        // Populate associates for this interface 
                        PopulateEvents(filter, declaringType, csEventInfos, ref list);
                    }

                    return list.ToArray();
                }

                [System.Security.SecuritySafeCritical]  // auto-generated
                private unsafe void PopulateEvents(
                    Filter filter, RuntimeType declaringType, Dictionary<String, RuntimeEventInfo> csEventInfos, ref ListBuilder<RuntimeEventInfo> list)
                {
                    int tkDeclaringType = RuntimeTypeHandle.GetToken(declaringType);

                    // Arrays, Pointers, ByRef types and others generated only the fly by the RT do not have tokens.
                    if (MdToken.IsNullToken(tkDeclaringType))
                        return;

                    MetadataImport scope = RuntimeTypeHandle.GetMetadataImport(declaringType);

                    MetadataEnumResult tkEvents;
                    scope.EnumEvents(tkDeclaringType, out tkEvents);

                    for (int i = 0; i < tkEvents.Length; i++)
                    {
                        int tkEvent = tkEvents[i];
                        bool isPrivate;

                        Contract.Assert(!MdToken.IsNullToken(tkEvent));
                        Contract.Assert(MdToken.IsTokenOfType(tkEvent, MetadataTokenType.Event));

                        if (filter.RequiresStringComparison())
                        {
                            Utf8String name;
                            name = scope.GetName(tkEvent);

                            if (!filter.Match(name))
                                continue;
                        }

                        RuntimeEventInfo eventInfo = new RuntimeEventInfo(
                            tkEvent, declaringType, m_runtimeTypeCache, out isPrivate);

                        #region Remove Inherited Privates
                        if (declaringType != m_runtimeTypeCache.GetRuntimeType() && isPrivate)
                            continue;
                        #endregion

                        #region Remove Duplicates
                        if (csEventInfos != null)
                        {
                            string name = eventInfo.Name;

                            if (csEventInfos.GetValueOrDefault(name) != null)
                                continue;

                            csEventInfos[name] = eventInfo;
                        }
                        else
                        {
                            if (list.Count > 0)
                                break;
                        }
                        #endregion

                        list.Add(eventInfo);
                    }
                }

                [System.Security.SecuritySafeCritical]  // auto-generated
                private unsafe RuntimePropertyInfo[] PopulateProperties(Filter filter)
                {
                    Contract.Requires(ReflectedType != null);

                    // m_csMemberInfos can be null at this point. It will be initialized when Insert
                    // is called in Populate after this returns.

                    RuntimeType declaringType = ReflectedType;
                    Contract.Assert(declaringType != null);

                    ListBuilder<RuntimePropertyInfo> list = new ListBuilder<RuntimePropertyInfo>();

                    if (!RuntimeTypeHandle.IsInterface(declaringType))
                    {
                        while(RuntimeTypeHandle.IsGenericVariable(declaringType))
                            declaringType = declaringType.GetBaseType();

                        // Do not create the dictionary if we are filtering the properties by name already
                        Dictionary<String, List<RuntimePropertyInfo>> csPropertyInfos = filter.CaseSensitive() ? null :
                            new Dictionary<String, List<RuntimePropertyInfo>>();

                        // All elements automatically initialized to false.
                        bool[] usedSlots = new bool[RuntimeTypeHandle.GetNumVirtuals(declaringType)];

                        // Populate associates off of the class hierarchy
                        do
                        {
                            PopulateProperties(filter, declaringType, csPropertyInfos, usedSlots, ref list);
                            declaringType = RuntimeTypeHandle.GetBaseType(declaringType);
                        } while (declaringType != null);
                    }
                    else
                    {
                        // Populate associates for this interface 
                        PopulateProperties(filter, declaringType, null, null, ref list);
                    }

                    return list.ToArray();
                }

                [System.Security.SecuritySafeCritical]  // auto-generated
                private unsafe void PopulateProperties(
                    Filter filter, 
                    RuntimeType declaringType, 
                    Dictionary<String, List<RuntimePropertyInfo>> csPropertyInfos,
                    bool[] usedSlots,
                    ref ListBuilder<RuntimePropertyInfo> list)
                {
                    int tkDeclaringType = RuntimeTypeHandle.GetToken(declaringType);

                    // Arrays, Pointers, ByRef types and others generated only the fly by the RT do not have tokens.
                    if (MdToken.IsNullToken(tkDeclaringType))
                        return;

                    MetadataImport scope = RuntimeTypeHandle.GetMetadataImport(declaringType);

                    MetadataEnumResult tkProperties;
                    scope.EnumProperties(tkDeclaringType, out tkProperties);

                    RuntimeModule declaringModuleHandle = RuntimeTypeHandle.GetModule(declaringType);

                    int numVirtuals = RuntimeTypeHandle.GetNumVirtuals(declaringType);

                    Contract.Assert((declaringType.IsInterface && usedSlots == null && csPropertyInfos == null) ||
                                    (!declaringType.IsInterface && usedSlots != null && usedSlots.Length >= numVirtuals));

                    for (int i = 0; i < tkProperties.Length; i++)
                    {
                        int tkProperty = tkProperties[i];
                        bool isPrivate;

                        Contract.Assert(!MdToken.IsNullToken(tkProperty));
                        Contract.Assert(MdToken.IsTokenOfType(tkProperty, MetadataTokenType.Property));

                        if (filter.RequiresStringComparison())
                        {
                            if (!ModuleHandle.ContainsPropertyMatchingHash(declaringModuleHandle, tkProperty, filter.GetHashToMatch()))
                            {
                                Contract.Assert(!filter.Match(declaringType.GetRuntimeModule().MetadataImport.GetName(tkProperty)));
                                continue;
                            }

                            Utf8String name;
                            name = declaringType.GetRuntimeModule().MetadataImport.GetName(tkProperty);

                            if (!filter.Match(name))
                                continue;
                        }

                        RuntimePropertyInfo propertyInfo =
                            new RuntimePropertyInfo(
                            tkProperty, declaringType, m_runtimeTypeCache, out isPrivate);

                        // If this is a class, not an interface
                        if (usedSlots != null)
                        {
                            #region Remove Privates
                            if (declaringType != ReflectedType && isPrivate)
                                continue;
                            #endregion

                            #region Duplicate check based on vtable slots

                            // The inheritance of properties are defined by the inheritance of their 
                            // getters and setters.
                            // A property on a base type is "overriden" by a property on a sub type
                            // if the getter/setter of the latter occupies the same vtable slot as 
                            // the getter/setter of the former.

                            MethodInfo associateMethod = propertyInfo.GetGetMethod();
                            if (associateMethod == null)
                            {
                                // We only need to examine the setter if a getter doesn't exist.
                                // It is not logical for the getter to be virtual but not the setter.
                                associateMethod = propertyInfo.GetSetMethod();
                            }

                            if (associateMethod != null)
                            {
                                int slot = RuntimeMethodHandle.GetSlot((RuntimeMethodInfo)associateMethod);

                                if (slot < numVirtuals)
                                {
                                    Contract.Assert(associateMethod.IsVirtual);
                                    if (usedSlots[slot] == true)
                                        continue;
                                    else
                                        usedSlots[slot] = true;
                                }
                            }
                            #endregion

                            #region Duplicate check based on name and signature

                            // For backward compatibility, even if the vtable slots don't match, we will still treat
                            // a property as duplicate if the names and signatures match.

                            if (csPropertyInfos != null)
                            {
                                string name = propertyInfo.Name;

                                List<RuntimePropertyInfo> cache = csPropertyInfos.GetValueOrDefault(name);

                                if (cache == null)
                                {
                                    cache = new List<RuntimePropertyInfo>(1);
                                    csPropertyInfos[name] = cache;
                                }

                                for (int j = 0; j < cache.Count; j++)
                                {
                                    if (propertyInfo.EqualsSig(cache[j]))
                                    {
                                        cache = null;
                                        break;
                                    }
                                }

                                if (cache == null)
                                    continue;

                                cache.Add(propertyInfo);
                            }
                            else
                            {
                                bool duplicate = false;

                                for (int j = 0; j < list.Count; j++)
                                {
                                    if (propertyInfo.EqualsSig(list[j]))
                                    {
                                        duplicate = true;
                                        break;
                                    }
                                }

                                if (duplicate)
                                    continue;
                            }
                            #endregion
                        }

                        list.Add(propertyInfo);
                    }
                }
                #endregion

                #region NonPrivate Members
                internal T[] GetMemberList(MemberListType listType, string name, CacheType cacheType)
                {
                    T[] list = null;

                    switch(listType)
                    {
                        case MemberListType.CaseSensitive:
                            list = m_csMemberInfos[name];
                            if (list != null)
                                return list;

                            return Populate(name, listType, cacheType);

                        case MemberListType.CaseInsensitive:
                            list = m_cisMemberInfos[name];
                            if (list != null)
                                return list;

                            return Populate(name, listType, cacheType);

                        default:
                            Contract.Assert(listType == MemberListType.All);
                            if (Volatile.Read(ref m_cacheComplete))
                                return m_allMembers;

                            return Populate(null, listType, cacheType);
                    }
                }

                internal RuntimeType ReflectedType
                {
                    get
                    {
                        return m_runtimeTypeCache.GetRuntimeType();
                    }
                }
                #endregion
            }
            #endregion

            #region Private Data Members
            private RuntimeType m_runtimeType;
            private RuntimeType m_enclosingType;
            private TypeCode m_typeCode;
            private string m_name;
            private string m_fullname;
            private string m_toString;
            private string m_namespace;
            private string m_serializationname;
            private bool m_isGlobal;
            private bool m_bIsDomainInitialized;
            private MemberInfoCache<RuntimeMethodInfo> m_methodInfoCache;
            private MemberInfoCache<RuntimeConstructorInfo> m_constructorInfoCache;
            private MemberInfoCache<RuntimeFieldInfo> m_fieldInfoCache;
            private MemberInfoCache<RuntimeType> m_interfaceCache;
            private MemberInfoCache<RuntimeType> m_nestedClassesCache;
            private MemberInfoCache<RuntimePropertyInfo> m_propertyInfoCache;
            private MemberInfoCache<RuntimeEventInfo> m_eventInfoCache;
            private static CerHashtable<RuntimeMethodInfo, RuntimeMethodInfo> s_methodInstantiations;
            private static Object s_methodInstantiationsLock;
#if !FEATURE_CORECLR
            private RuntimeConstructorInfo m_serializationCtor;
#endif
            private string m_defaultMemberName;
            private Object m_genericCache; // Generic cache for rare scenario specific data. It is used to cache Enum names and values.
            #endregion

            #region Constructor
            internal RuntimeTypeCache(RuntimeType runtimeType)
            {
                m_typeCode = TypeCode.Empty;
                m_runtimeType = runtimeType;
                m_isGlobal = RuntimeTypeHandle.GetModule(runtimeType).RuntimeType == runtimeType;
            }
            #endregion

            #region Private Members
            private string ConstructName(ref string name, TypeNameFormatFlags formatFlags)
            {
                if (name == null) 
                {
                    name = new RuntimeTypeHandle(m_runtimeType).ConstructName(formatFlags);
                }
                return name;
            }

            private T[] GetMemberList<T>(ref MemberInfoCache<T> m_cache, MemberListType listType, string name, CacheType cacheType) 
                where T : MemberInfo
            {
                MemberInfoCache<T> existingCache = GetMemberCache<T>(ref m_cache);
                return existingCache.GetMemberList(listType, name, cacheType);
            }

            private MemberInfoCache<T> GetMemberCache<T>(ref MemberInfoCache<T> m_cache) 
                where T : MemberInfo
            {
                MemberInfoCache<T> existingCache = m_cache;

                if (existingCache == null)
                {
                    MemberInfoCache<T> newCache = new MemberInfoCache<T>(this);
                    existingCache = Interlocked.CompareExchange(ref m_cache, newCache, null);
                    if (existingCache == null)
                        existingCache = newCache;
                }

                return existingCache;
            }
            #endregion

            #region Internal Members

            internal Object GenericCache
            {
                get { return m_genericCache; }
                set { m_genericCache = value; }
            }

            internal bool DomainInitialized
            {
                get { return m_bIsDomainInitialized; }
                set { m_bIsDomainInitialized = value; }
            }
            
            internal string GetName(TypeNameKind kind)
            {
                switch (kind)
                {
                    case TypeNameKind.Name:
                        // No namespace, full instantiation, and assembly.
                        return ConstructName(ref m_name, TypeNameFormatFlags.FormatBasic);

                    case TypeNameKind.FullName:
                        // We exclude the types that contain generic parameters because their names cannot be roundtripped.
                        // We allow generic type definitions (and their refs, ptrs, and arrays) because their names can be roundtriped.
                        // Theoretically generic types instantiated with generic type definitions can be roundtripped, e.g. List`1<Dictionary`2>.
                        // But these kind of types are useless, rare, and hard to identity. We would need to recursively examine all the 
                        // generic arguments with the same criteria. We will exclude them unless we see a real user scenario.
                        if (!m_runtimeType.GetRootElementType().IsGenericTypeDefinition && m_runtimeType.ContainsGenericParameters)
                            return null;

                        // No assembly.
                        return ConstructName(ref m_fullname, TypeNameFormatFlags.FormatNamespace | TypeNameFormatFlags.FormatFullInst);

                    case TypeNameKind.ToString:
                        // No full instantiation and assembly.
                        return ConstructName(ref m_toString, TypeNameFormatFlags.FormatNamespace);

                    case TypeNameKind.SerializationName:
                        // Use FormatGenericParam in serialization. Otherwise we won't be able 
                        // to distinguish between a generic parameter and a normal type with the same name.
                        // e.g. Foo<T>.Bar(T t), the parameter type T could be !1 or a real type named "T".
                        // Excluding the version number in the assembly name for VTS.
                        return ConstructName(ref m_serializationname, TypeNameFormatFlags.FormatSerialization);

                    default:
                        throw new InvalidOperationException();
                }
            }

            [System.Security.SecuritySafeCritical]
            internal unsafe string GetNameSpace()
            {
                // @Optimization - Use ConstructName to populate m_namespace
                if (m_namespace == null)
                {   
                    Type type = m_runtimeType;
                    type = type.GetRootElementType();
                    
                    while (type.IsNested)                       
                        type = type.DeclaringType;

                    m_namespace = RuntimeTypeHandle.GetMetadataImport((RuntimeType)type).GetNamespace(type.MetadataToken).ToString();
                }

                return m_namespace;
            }

            internal TypeCode TypeCode
            {
                get { return m_typeCode; }
                set { m_typeCode = value; } 
            }

            [System.Security.SecuritySafeCritical]  // auto-generated
            internal unsafe RuntimeType GetEnclosingType()
            { 
                if (m_enclosingType == null)
                {
                    // Use void as a marker of null enclosing type
                    RuntimeType enclosingType = RuntimeTypeHandle.GetDeclaringType(GetRuntimeType());
                    Contract.Assert(enclosingType != typeof(void));
                    m_enclosingType = enclosingType ?? (RuntimeType)typeof(void);
                }

                return (m_enclosingType == typeof(void)) ? null : m_enclosingType;
            }

            internal RuntimeType GetRuntimeType()
            {
                return m_runtimeType;
            }

            internal bool IsGlobal 
            { 
                [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
                get { return m_isGlobal; } 
            }

            internal void InvalidateCachedNestedType()
            {
                m_nestedClassesCache = null;
            }

#if !FEATURE_CORECLR
            internal RuntimeConstructorInfo GetSerializationCtor()
            {
                if (m_serializationCtor == null)
                {
                    if (s_SICtorParamTypes == null)
                        s_SICtorParamTypes = new Type[] { typeof(SerializationInfo), typeof(StreamingContext) };

                    m_serializationCtor = m_runtimeType.GetConstructor(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        CallingConventions.Any,
                        s_SICtorParamTypes,
                        null) as RuntimeConstructorInfo;
                }

                return m_serializationCtor;
            }
#endif //!FEATURE_CORECLR

            internal string GetDefaultMemberName()
            {
                if (m_defaultMemberName == null)
                {
                    CustomAttributeData attr = null;
                    Type DefaultMemberAttrType = typeof(DefaultMemberAttribute);
                    for (RuntimeType t = m_runtimeType; t != null; t = t.GetBaseType())
                    {
                        IList<CustomAttributeData> attrs = CustomAttributeData.GetCustomAttributes(t);
                        for (int i = 0; i < attrs.Count; i++)
                        {
                            if (Object.ReferenceEquals(attrs[i].Constructor.DeclaringType, DefaultMemberAttrType))
                            {
                                attr = attrs[i];
                                break;
                            }
                        }

                        if (attr != null)
                        {
                            m_defaultMemberName = attr.ConstructorArguments[0].Value as string;
                            break;
                        }
                    }
                }

                return m_defaultMemberName;
            }
            #endregion

            #region Caches Accessors
            [System.Security.SecurityCritical]  // auto-generated
            internal MethodInfo GetGenericMethodInfo(RuntimeMethodHandleInternal genericMethod)
            {
                LoaderAllocator la = RuntimeMethodHandle.GetLoaderAllocator(genericMethod);

                RuntimeMethodInfo rmi = new RuntimeMethodInfo(
                    genericMethod, RuntimeMethodHandle.GetDeclaringType(genericMethod), this,
                    RuntimeMethodHandle.GetAttributes(genericMethod), (BindingFlags)(-1), la);

                RuntimeMethodInfo crmi;
                if (la != null)
                {
                    crmi = la.m_methodInstantiations[rmi];
                }
                else
                {
                    crmi = s_methodInstantiations[rmi];
                }
                if (crmi != null)
                    return crmi;

                if (s_methodInstantiationsLock == null)
                    Interlocked.CompareExchange(ref s_methodInstantiationsLock, new Object(), null);

                bool lockTaken = false;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    Monitor.Enter(s_methodInstantiationsLock, ref lockTaken);

                    if (la != null)
                    {
                        crmi = la.m_methodInstantiations[rmi];
                        if (crmi != null)
                            return crmi;
                        la.m_methodInstantiations[rmi] = rmi;
                    }
                    else
                    {
                        crmi = s_methodInstantiations[rmi];
                        if (crmi != null)
                            return crmi;
                        s_methodInstantiations[rmi] = rmi;
                    }
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(s_methodInstantiationsLock);
                    }
                }

                return rmi;
            }

            internal RuntimeMethodInfo[] GetMethodList(MemberListType listType, string name) 
            { 
                return GetMemberList<RuntimeMethodInfo>(ref m_methodInfoCache, listType, name, CacheType.Method);
            }

            internal RuntimeConstructorInfo[] GetConstructorList(MemberListType listType, string name)
            { 
                return GetMemberList<RuntimeConstructorInfo>(ref m_constructorInfoCache, listType, name, CacheType.Constructor);
            }

            internal RuntimePropertyInfo[] GetPropertyList(MemberListType listType, string name)
            { 
                return GetMemberList<RuntimePropertyInfo>(ref m_propertyInfoCache, listType, name, CacheType.Property);
            }

            internal RuntimeEventInfo[] GetEventList(MemberListType listType, string name)
            { 
                return GetMemberList<RuntimeEventInfo>(ref m_eventInfoCache, listType, name, CacheType.Event);
            }

            internal RuntimeFieldInfo[] GetFieldList(MemberListType listType, string name)
            { 
                return GetMemberList<RuntimeFieldInfo>(ref m_fieldInfoCache, listType, name, CacheType.Field);
            }

            internal RuntimeType[] GetInterfaceList(MemberListType listType, string name)
            { 
                return GetMemberList<RuntimeType>(ref m_interfaceCache, listType, name, CacheType.Interface);
            }

            internal RuntimeType[] GetNestedTypeList(MemberListType listType, string name)
            { 
                return GetMemberList<RuntimeType>(ref m_nestedClassesCache, listType, name, CacheType.NestedType);
            }

            internal MethodBase GetMethod(RuntimeType declaringType, RuntimeMethodHandleInternal method) 
            { 
                GetMemberCache<RuntimeMethodInfo>(ref m_methodInfoCache);
                return m_methodInfoCache.AddMethod(declaringType, method, CacheType.Method);
            }

            internal MethodBase GetConstructor(RuntimeType declaringType, RuntimeMethodHandleInternal constructor)
            { 
                GetMemberCache<RuntimeConstructorInfo>(ref m_constructorInfoCache);
                return m_constructorInfoCache.AddMethod(declaringType, constructor, CacheType.Constructor);
            }

            internal FieldInfo GetField(RuntimeFieldHandleInternal field)
            { 
                GetMemberCache<RuntimeFieldInfo>(ref m_fieldInfoCache);
                return m_fieldInfoCache.AddField(field);
            }

            #endregion
        }
        #endregion

#if FEATURE_REMOTING
        #region Legacy Remoting Cache
        // The size of CachedData is accounted for by BaseObjectWithCachedData in object.h.
        // This member is currently being used by Remoting for caching remoting data. If you
        // need to cache data here, talk to the Remoting team to work out a mechanism, so that
        // both caching systems can happily work together.
        private RemotingTypeCachedData m_cachedData;

        internal RemotingTypeCachedData RemotingCache
        {
            get
            {
                // This grabs an internal copy of m_cachedData and uses
                // that instead of looking at m_cachedData directly because
                // the cache may get cleared asynchronously.  This prevents
                // us from having to take a lock.
                RemotingTypeCachedData cache = m_cachedData;
                if (cache == null)
                {
                    cache = new RemotingTypeCachedData(this);
                    RemotingTypeCachedData ret = Interlocked.CompareExchange(ref m_cachedData, cache, null);
                    if (ret != null)
                        cache = ret;
                }
                return cache;
            }
        }
        #endregion
#endif //FEATURE_REMOTING

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

        internal static MethodBase GetMethodBase(RuntimeModule scope, int typeMetadataToken)
        {
            return GetMethodBase(ModuleHandle.ResolveMethodHandleInternal(scope, typeMetadataToken));
        }

        internal static MethodBase GetMethodBase(IRuntimeMethodInfo methodHandle)
        {
            return GetMethodBase(null, methodHandle);
        }

        [System.Security.SecuritySafeCritical]
        internal static MethodBase GetMethodBase(RuntimeType reflectedType, IRuntimeMethodInfo methodHandle)
        {
            MethodBase retval = RuntimeType.GetMethodBase(reflectedType, methodHandle.Value);
            GC.KeepAlive(methodHandle);
            return retval;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe static MethodBase GetMethodBase(RuntimeType reflectedType, RuntimeMethodHandleInternal methodHandle)
        {
            Contract.Assert(!methodHandle.IsNullHandle());

            if (RuntimeMethodHandle.IsDynamicMethod(methodHandle))
            {
                Resolver resolver = RuntimeMethodHandle.GetResolver(methodHandle);

                if (resolver != null)
                    return resolver.GetDynamicMethod();

                return null;
            }

            // verify the type/method relationship
            RuntimeType declaredType = RuntimeMethodHandle.GetDeclaringType(methodHandle);

            RuntimeType[] methodInstantiation = null;

            if (reflectedType == null)
                reflectedType = declaredType as RuntimeType;

            if (reflectedType != declaredType && !reflectedType.IsSubclassOf(declaredType))
            {
                // object[] is assignable from string[].
                if (reflectedType.IsArray)
                {
                    // The whole purpose of this chunk of code is not only for error checking.
                    // GetMember has a side effect of populating the member cache of reflectedType,
                    // doing so will ensure we construct the correct MethodInfo/ConstructorInfo objects.
                    // Without this the reflectedType.Cache.GetMethod call below may return a MethodInfo
                    // object whose ReflectedType is string[] and DeclaringType is object[]. That would
                    // be (arguabally) incorrect because string[] is not a subclass of object[].
                    MethodBase[] methodBases = reflectedType.GetMember(
                        RuntimeMethodHandle.GetName(methodHandle), MemberTypes.Constructor | MemberTypes.Method,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) as MethodBase[];

                    bool loaderAssuredCompatible = false;
                    for (int i = 0; i < methodBases.Length; i++)
                    {
                        IRuntimeMethodInfo rmi = (IRuntimeMethodInfo)methodBases[i];
                        if (rmi.Value.Value == methodHandle.Value)
                            loaderAssuredCompatible = true;
                    }

                    if (!loaderAssuredCompatible)
                        throw new ArgumentException(String.Format(
                            CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveMethodHandle"),
                            reflectedType.ToString(), declaredType.ToString()));
                }
                // Action<in string> is assignable from, but not a subclass of Action<in object>.
                else if (declaredType.IsGenericType)
                {
                    // ignoring instantiation is the ReflectedType a subtype of the DeclaringType
                    RuntimeType declaringDefinition = (RuntimeType)declaredType.GetGenericTypeDefinition();

                    RuntimeType baseType = reflectedType;

                    while (baseType != null)
                    {
                        RuntimeType baseDefinition = baseType;

                        if (baseDefinition.IsGenericType && !baseType.IsGenericTypeDefinition)
                            baseDefinition = (RuntimeType)baseDefinition.GetGenericTypeDefinition();

                        if (baseDefinition == declaringDefinition)
                            break;

                        baseType = baseType.GetBaseType();
                    }

                    if (baseType == null)
                    {
                        // ignoring instantiation is the ReflectedType is not a subtype of the DeclaringType
                        throw new ArgumentException(String.Format(
                            CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveMethodHandle"),
                            reflectedType.ToString(), declaredType.ToString()));
                    }

                    // remap the method to same method on the subclass ReflectedType
                    declaredType = baseType;

                    // if the original methodHandle was the definition then we don't need to rebind generic method arguments
                    // because all RuntimeMethodHandles retrieved off of the canonical method table are definitions. That's 
                    // why for everything else we need to rebind the generic method arguments. 
                    if (!RuntimeMethodHandle.IsGenericMethodDefinition(methodHandle))
                    {
                        methodInstantiation = RuntimeMethodHandle.GetMethodInstantiationInternal(methodHandle);
                    }

                    // lookup via v-table slot the RuntimeMethodHandle on the new declaring type
                    methodHandle = RuntimeMethodHandle.GetMethodFromCanonical(methodHandle, declaredType);
                }
                else if (!declaredType.IsAssignableFrom(reflectedType))
                {
                    // declaredType is not Array, not generic, and not assignable from reflectedType
                    throw new ArgumentException(String.Format(
                        CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveMethodHandle"),
                        reflectedType.ToString(), declaredType.ToString()));
                }
            }

            // If methodInstantiation is not null, GetStubIfNeeded will rebind the generic method arguments
            // if declaredType is an instantiated generic type and methodHandle is not generic, get the instantiated MethodDesc (if needed)
            // if declaredType is a value type, get the unboxing stub (if needed)

            // this is so that our behavior here is consistent with that of Type.GetMethod
            // See MemberInfoCache<RuntimeConstructorInfo>.PopulateMethods and MemberInfoCache<RuntimeMethodInfoInfo>.PopulateConstructors

            methodHandle = RuntimeMethodHandle.GetStubIfNeeded(methodHandle, declaredType, methodInstantiation);
            MethodBase retval;

            if (RuntimeMethodHandle.IsConstructor(methodHandle))
            {
                // Constructor case: constructors cannot be generic
                retval = reflectedType.Cache.GetConstructor(declaredType, methodHandle);
            }
            else
            {
                // Method case
                if (RuntimeMethodHandle.HasMethodInstantiation(methodHandle) && !RuntimeMethodHandle.IsGenericMethodDefinition(methodHandle))
                    retval = reflectedType.Cache.GetGenericMethodInfo(methodHandle);
                else
                    retval = reflectedType.Cache.GetMethod(declaredType, methodHandle);
            }

            GC.KeepAlive(methodInstantiation);
            return retval;
        }

        internal Object GenericCache
        {
            get { return Cache.GenericCache; }
            set { Cache.GenericCache = value; }
        }

        internal bool DomainInitialized
        {
            get { return Cache.DomainInitialized; }
            set { Cache.DomainInitialized = value; }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe static FieldInfo GetFieldInfo(IRuntimeFieldInfo fieldHandle)
        {
            return GetFieldInfo(RuntimeFieldHandle.GetApproxDeclaringType(fieldHandle), fieldHandle);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe static FieldInfo GetFieldInfo(RuntimeType reflectedType, IRuntimeFieldInfo field)
        {
            RuntimeFieldHandleInternal fieldHandle = field.Value;

            // verify the type/method relationship
            if (reflectedType == null) 
            {
                reflectedType = RuntimeFieldHandle.GetApproxDeclaringType(fieldHandle);
            }
            else
            {
                RuntimeType declaredType = RuntimeFieldHandle.GetApproxDeclaringType(fieldHandle);
                if (reflectedType != declaredType)
                {
                    if (!RuntimeFieldHandle.AcquiresContextFromThis(fieldHandle) ||
                        !RuntimeTypeHandle.CompareCanonicalHandles(declaredType, reflectedType))
                    {
                        throw new ArgumentException(String.Format(
                            CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveFieldHandle"),
                            reflectedType.ToString(),
                            declaredType.ToString()));
                    }
                }
            }

            FieldInfo retVal = reflectedType.Cache.GetField(fieldHandle);
            GC.KeepAlive(field);
            return retVal;
        }

        // Called internally
        private unsafe static PropertyInfo GetPropertyInfo(RuntimeType reflectedType, int tkProperty)
        {
            RuntimePropertyInfo property = null;
            RuntimePropertyInfo[] candidates = 
                reflectedType.Cache.GetPropertyList(MemberListType.All, null);

            for (int i = 0; i < candidates.Length; i++)
            {
                property = candidates[i];
                if (property.MetadataToken == tkProperty)
                    return property;
            }

            Contract.Assume(false, "Unreachable code");
            throw new SystemException();
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
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static void ValidateGenericArguments(MemberInfo definition, RuntimeType[] genericArguments, Exception e)
        {
            RuntimeType[] typeContext = null;
            RuntimeType[] methodContext = null;
            RuntimeType[] genericParamters = null;
        
            if (definition is Type)
            {
                RuntimeType genericTypeDefinition = (RuntimeType)definition;
                genericParamters = genericTypeDefinition.GetGenericArgumentsInternal();               
                typeContext = genericArguments;
            }
            else
            {
                RuntimeMethodInfo genericMethodDefinition = (RuntimeMethodInfo)definition;
                genericParamters = genericMethodDefinition.GetGenericArgumentsInternal();
                methodContext = genericArguments;
                
                RuntimeType declaringType = (RuntimeType)genericMethodDefinition.DeclaringType;
                if (declaringType != null)
                {
                    typeContext = declaringType.GetTypeHandleInternal().GetInstantiationInternal();
                }
            }
            
            for (int i = 0; i < genericArguments.Length; i++)
            {
                Type genericArgument = genericArguments[i];
                Type genericParameter = genericParamters[i];

                if (!RuntimeTypeHandle.SatisfiesConstraints(genericParameter.GetTypeHandleInternal().GetTypeChecked(),
                    typeContext, methodContext, genericArgument.GetTypeHandleInternal().GetTypeChecked()))
                {
                    throw new ArgumentException(
                        Environment.GetResourceString("Argument_GenConstraintViolation", 
                        i.ToString(CultureInfo.CurrentCulture), genericArgument.ToString(), definition.ToString(), genericParameter.ToString()), e);
                }
            }
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

            if (!RuntimeType.FilterApplyBase(type, bindingFlags, isPublic, type.IsNestedAssembly, isStatic, name, prefixLookup))
                return false;

            if (ns != null && !type.Namespace.Equals(ns))
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

            #region Apply Base Filter
            if ((bindingFlags & methodFlags) != methodFlags)
                return false;
            #endregion

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
                                if ((object)argumentTypes[i] != null && !Object.ReferenceEquals(parameterInfos[i].ParameterType, argumentTypes[i]))
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
        private object m_keepalive; // This will be filled with a LoaderAllocator reference when this RuntimeType represents a collectible type
        private IntPtr m_cache;
        internal IntPtr m_handle;

#if FEATURE_APPX
        private INVOCATION_FLAGS m_invocationFlags;

        internal bool IsNonW8PFrameworkAPI()
        {
            if (IsGenericParameter)
                return false;

            if (HasElementType)
                return ((RuntimeType)GetElementType()).IsNonW8PFrameworkAPI();

            if (IsSimpleTypeNonW8PFrameworkAPI())
                return true;

            if (IsGenericType && !IsGenericTypeDefinition)
            {
                foreach (Type t in GetGenericArguments())
                {
                    if (((RuntimeType)t).IsNonW8PFrameworkAPI())
                        return true;
                }
            }

            return false;
        }

        private bool IsSimpleTypeNonW8PFrameworkAPI()
        {
            RuntimeAssembly rtAssembly = GetRuntimeAssembly();
            if (rtAssembly.IsFrameworkAssembly())
            {
                int ctorToken = rtAssembly.InvocableAttributeCtorToken;
                if (System.Reflection.MetadataToken.IsNullToken(ctorToken) ||
                    !CustomAttribute.IsAttributeDefined(GetRuntimeModule(), MetadataToken, ctorToken))
                    return true;
            }

            return false;
        }

        internal INVOCATION_FLAGS InvocationFlags
        {
            get
            {
                if ((m_invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED) == 0)
                {
                    INVOCATION_FLAGS invocationFlags = INVOCATION_FLAGS.INVOCATION_FLAGS_UNKNOWN;

                    if (AppDomain.ProfileAPICheck && IsNonW8PFrameworkAPI())
                        invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API;

                    m_invocationFlags = invocationFlags | INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED;
                }

                return m_invocationFlags;
            }
        }
#endif // FEATURE_APPX

        internal static readonly RuntimeType ValueType = (RuntimeType)typeof(System.ValueType);
        internal static readonly RuntimeType EnumType = (RuntimeType)typeof(System.Enum);

        private static readonly RuntimeType ObjectType = (RuntimeType)typeof(System.Object);
        private static readonly RuntimeType StringType = (RuntimeType)typeof(System.String);
        private static readonly RuntimeType DelegateType = (RuntimeType)typeof(System.Delegate);

        private static Type[] s_SICtorParamTypes;
        #endregion

        #region Constructor
        internal RuntimeType() { throw new NotSupportedException(); }
        #endregion

        #region Private\Internal Members
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal override bool CacheEquals(object o)
        {
            RuntimeType m = o as RuntimeType;

            if (m == null)
                return false;

            return m.m_handle.Equals(m_handle);
        }

        private RuntimeTypeCache Cache
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                if (m_cache.IsNull())
                {
                    IntPtr newgcHandle = new RuntimeTypeHandle(this).GetGCHandle(GCHandleType.WeakTrackResurrection);
                    IntPtr gcHandle = Interlocked.CompareExchange(ref m_cache, newgcHandle, (IntPtr)0);
                    // Leak the handle if the type is collectible. It will be reclaimed when
                    // the type goes away.
                    if (!gcHandle.IsNull() && !IsCollectible()) 
                        GCHandle.InternalFree(newgcHandle);
                }

                RuntimeTypeCache cache = GCHandle.InternalGet(m_cache) as RuntimeTypeCache;
                if (cache == null) 
                {
                    cache = new RuntimeTypeCache(this);
                    RuntimeTypeCache existingCache = GCHandle.InternalCompareExchange(m_cache, cache, null, false) as RuntimeTypeCache;
                    if (existingCache != null) 
                        cache = existingCache;
                }

                Contract.Assert(cache != null);
                return cache;
            }
        }

        internal bool IsSpecialSerializableType()
        {
            RuntimeType rt = this;
            do
            {
                // In all sane cases we only need to compare the direct level base type with
                // System.Enum and System.MulticastDelegate. However, a generic argument can
                // have a base type constraint that is Delegate or even a real delegate type.
                // Let's maintain compatibility and return true for them.
                if (rt == RuntimeType.DelegateType || rt == RuntimeType.EnumType)
                    return true;

                rt = rt.GetBaseType();
            } while (rt != null);

            return false;
        }

        private string GetDefaultMemberName()
        {
            return Cache.GetDefaultMemberName();
        }

#if !FEATURE_CORECLR
        internal RuntimeConstructorInfo GetSerializationCtor()
        {
            return Cache.GetSerializationCtor();
        }
#endif
        #endregion

        #region Type Overrides

        #region Get XXXInfo Candidates
        private ListBuilder<MethodInfo> GetMethodCandidates(
            String name, BindingFlags bindingAttr, CallingConventions callConv,
            Type[] types, bool allowPrefixLookup)
        {
            bool prefixLookup, ignoreCase;
            MemberListType listType;
            RuntimeType.FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out ignoreCase, out listType);

            RuntimeMethodInfo[] cache = Cache.GetMethodList(listType, name);

            ListBuilder<MethodInfo> candidates = new ListBuilder<MethodInfo>(cache.Length);
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeMethodInfo methodInfo = cache[i];
                if (FilterApplyMethodInfo(methodInfo, bindingAttr, callConv, types) &&
                    (!prefixLookup || RuntimeType.FilterApplyPrefixLookup(methodInfo, name, ignoreCase)))
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
            RuntimeType.FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out ignoreCase, out listType);

            RuntimeConstructorInfo[] cache = Cache.GetConstructorList(listType, name);

            ListBuilder<ConstructorInfo> candidates = new ListBuilder<ConstructorInfo>(cache.Length);
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeConstructorInfo constructorInfo = cache[i];
                if (FilterApplyConstructorInfo(constructorInfo, bindingAttr, callConv, types) &&
                    (!prefixLookup || RuntimeType.FilterApplyPrefixLookup(constructorInfo, name, ignoreCase)))
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
            RuntimeType.FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out ignoreCase, out listType);

            RuntimePropertyInfo[] cache = Cache.GetPropertyList(listType, name);

            bindingAttr ^= BindingFlags.DeclaredOnly;

            ListBuilder<PropertyInfo> candidates = new ListBuilder<PropertyInfo>(cache.Length);
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimePropertyInfo propertyInfo = cache[i];
                if ((bindingAttr & propertyInfo.BindingFlags) == propertyInfo.BindingFlags &&
                    (!prefixLookup || RuntimeType.FilterApplyPrefixLookup(propertyInfo, name, ignoreCase)) &&
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
            RuntimeType.FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out ignoreCase, out listType);

            RuntimeEventInfo[] cache = Cache.GetEventList(listType, name);

            bindingAttr ^= BindingFlags.DeclaredOnly;

            ListBuilder<EventInfo> candidates = new ListBuilder<EventInfo>(cache.Length);
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeEventInfo eventInfo = cache[i];
                if ((bindingAttr & eventInfo.BindingFlags) == eventInfo.BindingFlags &&
                    (!prefixLookup || RuntimeType.FilterApplyPrefixLookup(eventInfo, name, ignoreCase)))
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
            RuntimeType.FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out ignoreCase, out listType);

            RuntimeFieldInfo[] cache = Cache.GetFieldList(listType, name);

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
            RuntimeType.FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out ignoreCase, out listType);

            RuntimeType[] cache = Cache.GetNestedTypeList(listType, name);

            ListBuilder<Type> candidates = new ListBuilder<Type>(cache.Length);
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeType nestedClass = cache[i];
                if (RuntimeType.FilterApplyType(nestedClass, bindingAttr, name, prefixLookup, ns))
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
            return GetMethodCandidates(null, bindingAttr, CallingConventions.Any, null, false).ToArray();
        }

[System.Runtime.InteropServices.ComVisible(true)]
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

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override Type[] GetInterfaces()
        {
              RuntimeType[] candidates = this.Cache.GetInterfaceList(MemberListType.All, null);
              Type[] interfaces = new Type[candidates.Length];
              for (int i = 0; i < candidates.Length; i++)
                  JitHelpers.UnsafeSetArrayElement(interfaces, i, candidates[i]);

              return interfaces;
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            return GetNestedTypeCandidates(null, bindingAttr, false).ToArray();
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            ListBuilder<MethodInfo> methods = GetMethodCandidates(null, bindingAttr, CallingConventions.Any, null, false);
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

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override InterfaceMapping GetInterfaceMap(Type ifaceType)
        {
            if (IsGenericParameter)
                throw new InvalidOperationException(Environment.GetResourceString("Arg_GenericParameter"));
        
            if ((object)ifaceType == null)
                throw new ArgumentNullException("ifaceType");
            Contract.EndContractBlock();

            RuntimeType ifaceRtType = ifaceType as RuntimeType;

            if (ifaceRtType == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"), "ifaceType");

            RuntimeTypeHandle ifaceRtTypeHandle = ifaceRtType.GetTypeHandleInternal();

            GetTypeHandleInternal().VerifyInterfaceIsImplemented(ifaceRtTypeHandle);
            Contract.Assert(ifaceType.IsInterface);  // VerifyInterfaceIsImplemented enforces this invariant
            Contract.Assert(!IsInterface); // VerifyInterfaceIsImplemented enforces this invariant

            // SZArrays implement the methods on IList`1, IEnumerable`1, and ICollection`1 with
            // SZArrayHelper and some runtime magic. We don't have accurate interface maps for them.
            if (IsSzArray && ifaceType.IsGenericType)
                throw new ArgumentException(Environment.GetResourceString("Argument_ArrayGetInterfaceMap"));

            int ifaceInstanceMethodCount = RuntimeTypeHandle.GetNumVirtuals(ifaceRtType);

            InterfaceMapping im;
            im.InterfaceType = ifaceType;
            im.TargetType = this;
            im.InterfaceMethods = new MethodInfo[ifaceInstanceMethodCount];
            im.TargetMethods = new MethodInfo[ifaceInstanceMethodCount];

            for (int i = 0; i < ifaceInstanceMethodCount; i++)
            {
                RuntimeMethodHandleInternal ifaceRtMethodHandle = RuntimeTypeHandle.GetMethodAt(ifaceRtType, i);

                // GetMethodBase will convert this to the instantiating/unboxing stub if necessary
                MethodBase ifaceMethodBase = RuntimeType.GetMethodBase(ifaceRtType, ifaceRtMethodHandle);
                Contract.Assert(ifaceMethodBase is RuntimeMethodInfo);
                im.InterfaceMethods[i] = (MethodInfo)ifaceMethodBase;

                // If the slot is -1, then virtual stub dispatch is active.
                int slot = GetTypeHandleInternal().GetInterfaceMethodImplementationSlot(ifaceRtTypeHandle, ifaceRtMethodHandle);

                if (slot == -1) continue;

                RuntimeMethodHandleInternal classRtMethodHandle = RuntimeTypeHandle.GetMethodAt(this, slot);

                // GetMethodBase will convert this to the instantiating/unboxing stub if necessary
                MethodBase rtTypeMethodBase = RuntimeType.GetMethodBase(this, classRtMethodHandle);
                // a class may not implement all the methods of an interface (abstract class) so null is a valid value 
                Contract.Assert(rtTypeMethodBase == null || rtTypeMethodBase is RuntimeMethodInfo);
                im.TargetMethods[i] = (MethodInfo)rtTypeMethodBase;
            }

            return im;
        }
        #endregion

        #region Find XXXInfo
        protected override MethodInfo GetMethodImpl(
            String name, BindingFlags bindingAttr, Binder binder, CallingConventions callConv, 
            Type[] types, ParameterModifier[] modifiers) 
        {       
            ListBuilder<MethodInfo> candidates = GetMethodCandidates(name, bindingAttr, callConv, types, false);

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
                        if (!System.DefaultBinder.CompareMethodSigAndName(methodInfo, firstCandidate))
                        {
                            throw new AmbiguousMatchException(Environment.GetResourceString("Arg_AmbiguousMatchException"));
                        }
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
            BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, 
            Type[] types, ParameterModifier[] modifiers) 
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
            String name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) 
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
            RuntimeType.FilterHelper(bindingAttr, ref name, out ignoreCase, out listType);
            
            RuntimeEventInfo[] cache = Cache.GetEventList(listType, name);
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
            RuntimeType.FilterHelper(bindingAttr, ref name, out ignoreCase, out listType);

            RuntimeFieldInfo[] cache = Cache.GetFieldList(listType, name);
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
            RuntimeType.FilterHelper(bindingAttr, ref name, out ignoreCase, out listType);

            RuntimeType[] cache = Cache.GetInterfaceList(listType, name);            

            RuntimeType match = null;

            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeType iface = cache[i];
                if (RuntimeType.FilterApplyType(iface, bindingAttr, name, false, ns))
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
            RuntimeType.FilterHelper(bindingAttr, ref name, out ignoreCase, out listType);

            RuntimeType[] cache = Cache.GetNestedTypeList(listType, name);

            RuntimeType match = null;

            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeType nestedType = cache[i];
                if (RuntimeType.FilterApplyType(nestedType, bindingAttr, name, false, ns))
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
                methods = GetMethodCandidates(name, bindingAttr, CallingConventions.Any, null, true);
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

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal sealed override RuntimeTypeHandle GetTypeHandleInternal()
        {
            return new RuntimeTypeHandle(this);
        }

        [System.Security.SecuritySafeCritical]
        internal bool IsCollectible()
        {
            return RuntimeTypeHandle.IsCollectible(GetTypeHandleInternal());
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        protected override TypeCode GetTypeCodeImpl() 
        {
            TypeCode typeCode = Cache.TypeCode;

            if (typeCode != TypeCode.Empty)
                return typeCode;

            CorElementType corElementType = RuntimeTypeHandle.GetCorElementType(this);
            switch (corElementType) 
            {
                case CorElementType.Boolean:
                    typeCode = TypeCode.Boolean; break;
                case CorElementType.Char:
                    typeCode = TypeCode.Char; break;
                case CorElementType.I1:
                    typeCode = TypeCode.SByte; break;
                case CorElementType.U1:
                    typeCode = TypeCode.Byte; break;
                case CorElementType.I2:
                    typeCode = TypeCode.Int16; break;
                case CorElementType.U2:
                    typeCode = TypeCode.UInt16; break;
                case CorElementType.I4:
                    typeCode = TypeCode.Int32; break;
                case CorElementType.U4:
                    typeCode = TypeCode.UInt32; break;
                case CorElementType.I8:
                    typeCode = TypeCode.Int64; break;
                case CorElementType.U8:
                    typeCode = TypeCode.UInt64; break;
                case CorElementType.R4:
                    typeCode = TypeCode.Single; break;
                case CorElementType.R8:
                    typeCode = TypeCode.Double; break;
                case CorElementType.String:
                    typeCode = TypeCode.String; break;
                case CorElementType.ValueType:
                    if (this == Convert.ConvertTypes[(int)TypeCode.Decimal])
                        typeCode = TypeCode.Decimal;
                    else if (this == Convert.ConvertTypes[(int)TypeCode.DateTime])
                        typeCode = TypeCode.DateTime;
                    else if (this.IsEnum)
                        typeCode = Type.GetTypeCode(Enum.GetUnderlyingType(this));
                    else
                        typeCode = TypeCode.Object;                    
                    break;
                default:
                    if (this == Convert.ConvertTypes[(int)TypeCode.DBNull])
                        typeCode = TypeCode.DBNull;
                    else if (this == Convert.ConvertTypes[(int)TypeCode.String])
                        typeCode = TypeCode.String;
                    else
                        typeCode = TypeCode.Object;
                    break;
            }

            Cache.TypeCode = typeCode;

            return typeCode;
        }

        public override MethodBase DeclaringMethod
        {
            get
            {
                if (!IsGenericParameter)
                    throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericParameter"));
                Contract.EndContractBlock();

                IRuntimeMethodInfo declaringMethod = RuntimeTypeHandle.GetDeclaringMethod(this);

                if (declaringMethod == null)
                    return null;

                return GetMethodBase(RuntimeMethodHandle.GetDeclaringType(declaringMethod), declaringMethod);
            }
        }
        #endregion

        #region Hierarchy
        [System.Security.SecuritySafeCritical]  // auto-generated
        public override bool IsInstanceOfType(Object o)
        {
            return RuntimeTypeHandle.IsInstanceOfType(this, o);
        }

        [System.Runtime.InteropServices.ComVisible(true)]
        [Pure]
        public override bool IsSubclassOf(Type type) 
        {
            if ((object)type == null)
                throw new ArgumentNullException("type");
            Contract.EndContractBlock();
            RuntimeType rtType = type as RuntimeType;
            if (rtType == null)
                return false;

            RuntimeType baseType = GetBaseType();

            while (baseType != null)
            {
                if (baseType == rtType)
                    return true;

                baseType = baseType.GetBaseType();
            }

            // pretty much everything is a subclass of object, even interfaces
            // notice that interfaces are really odd because they do not have a BaseType
            // yet IsSubclassOf(typeof(object)) returns true
            if (rtType == RuntimeType.ObjectType && rtType != this) 
                return true;

            return false;
        }

        public override bool IsAssignableFrom(System.Reflection.TypeInfo typeInfo){
            if(typeInfo==null) return false;
            return IsAssignableFrom(typeInfo.AsType());
        }

        public override bool IsAssignableFrom(Type c)
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

            // Special case for TypeBuilder to be backward-compatible.
            if (c is System.Reflection.Emit.TypeBuilder)
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

            // For anything else we return false.
            return false;
        }

#if !FEATURE_CORECLR
        // Reflexive, symmetric, transitive.
        public override bool IsEquivalentTo(Type other)
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
#endif // FEATURE_CORECLR

        public override Type BaseType 
        {
            get 
            {
                return GetBaseType();
            }
        }

        private RuntimeType GetBaseType()
        {
            if (IsInterface)
                return null;

            if (RuntimeTypeHandle.IsGenericVariable(this))
            {
                Type[] constraints = GetGenericParameterConstraints();

                RuntimeType baseType = RuntimeType.ObjectType;

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

                if (baseType == RuntimeType.ObjectType)
                {
                    GenericParameterAttributes special;
                    special = GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;
                    if ((special & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
                        baseType = RuntimeType.ValueType;
                }

                return baseType;
            }

            return RuntimeTypeHandle.GetBaseType(this);
        }

        public override Type UnderlyingSystemType 
        {
            get 
            {
                return this;
            }
        }
        #endregion

        #region Name
        public override String FullName 
        {
            get 
            {
                return GetCachedName(TypeNameKind.FullName);
            }
        }

        public override String AssemblyQualifiedName 
        {
            get 
            {
                string fullname = FullName;

                // FullName is null if this type contains generic parameters but is not a generic type definition.
                if (fullname == null)
                    return null;
                
                return Assembly.CreateQualifiedName(this.Assembly.FullName, fullname); 
            }
        }

        public override String Namespace 
        {
            get 
            {
                string ns = Cache.GetNameSpace();
                
                if (ns == null || ns.Length == 0)
                    return null;

                return ns;
            }
        }
        #endregion

        #region Attributes
        [System.Security.SecuritySafeCritical]  // auto-generated
        protected override TypeAttributes GetAttributeFlagsImpl() 
        {
            return RuntimeTypeHandle.GetAttributes(this);
        }

        public override Guid GUID 
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get 
            {
                Guid result = new Guid ();
                GetGUID(ref result);
                return result;
            }
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void GetGUID(ref Guid result);

        [System.Security.SecuritySafeCritical]  // auto-generated
        protected override bool IsContextfulImpl() 
        {
            return RuntimeTypeHandle.IsContextful(this);
        }

        /*
        protected override bool IsMarshalByRefImpl() 
        {
            return GetTypeHandleInternal().IsMarshalByRef();
        }
        */

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

#if FEATURE_COMINTEROP
        [SecuritySafeCritical]
        internal override bool IsWindowsRuntimeObjectImpl()
        {
            return IsWindowsRuntimeObjectType(this);
        }

        [SecuritySafeCritical]
        internal override bool IsExportedToWindowsRuntimeImpl()
        {
            return IsTypeExportedToWindowsRuntime(this);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityCritical]
        private static extern bool IsWindowsRuntimeObjectType(RuntimeType type);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityCritical]
        private static extern bool IsTypeExportedToWindowsRuntime(RuntimeType type);

#endif // FEATURE_COMINTEROP

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal override bool HasProxyAttributeImpl() 
        {
            return RuntimeTypeHandle.HasProxyAttribute(this);
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

#if !FEATURE_CORECLR
        public override bool IsEnum
        {
            get
            {
                return GetBaseType() == RuntimeType.EnumType; 
            }
        }
#endif

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

                GenericParameterAttributes attributes;

                RuntimeTypeHandle.GetMetadataImport(this).GetGenericParamProps(MetadataToken, out attributes);

                return attributes;
            }
        }

        public override bool IsSecurityCritical 
        {
            get { return new RuntimeTypeHandle(this).IsSecurityCritical(); } 
        }
        public override bool IsSecuritySafeCritical
        {
            get { return new RuntimeTypeHandle(this).IsSecuritySafeCritical(); }
        }
        public override bool IsSecurityTransparent
        {
            get { return new RuntimeTypeHandle(this).IsSecurityTransparent(); }
        }
        #endregion

        #region Arrays
        internal override bool IsSzArray
        {
            get 
            {
                return RuntimeTypeHandle.IsSzArray(this);
            }
        }

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
            Array ret = Array.UnsafeCreateInstance(this, values.Length);

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
            if (valueType == RuntimeType.StringType)
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

            ulong ulValue = Enum.ToUInt64(value);

            return Enum.GetEnumName(this, ulValue);
        }
        #endregion

        #region Generics
        internal RuntimeType[] GetGenericArgumentsInternal()
        {
            return GetRootElementType().GetTypeHandleInternal().GetInstantiationInternal();
        }

        public override Type[] GetGenericArguments() 
        {
            Type[] types = GetRootElementType().GetTypeHandleInternal().GetInstantiationPublic();

            if (types == null)
                types = EmptyArray<Type>.Value;

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
                    Type[] instantiationCopy = new Type[instantiation.Length];
                    for (int iCopy = 0; iCopy < instantiation.Length; iCopy++)
                        instantiationCopy[iCopy] = instantiation[iCopy];
                    instantiation = instantiationCopy;
                    return System.Reflection.Emit.TypeBuilderInstantiation.MakeGenericType(this, instantiation);
                }

                instantiationRuntimeType[i] = rtInstantiationElem;
            }

            RuntimeType[] genericParameters = GetGenericArgumentsInternal();

            SanityCheckGenericArguments(instantiationRuntimeType, genericParameters);

            Type ret = null;
            try 
            {
                ret = new RuntimeTypeHandle(this).Instantiate(instantiationRuntimeType);
            }
            catch (TypeLoadException e)
            {
                ValidateGenericArguments(this, instantiationRuntimeType, e);
                throw e;
            }

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
            
                return new RuntimeTypeHandle(this).GetGenericVariableIndex(); 
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

        public override bool ContainsGenericParameters
        {
            get { return GetRootElementType().GetTypeHandleInternal().ContainsGenericVariables(); } 
        }

        public override Type[] GetGenericParameterConstraints()
        {
            if (!IsGenericParameter)
                throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericParameter"));
            Contract.EndContractBlock();

            Type[] constraints = new RuntimeTypeHandle(this).GetConstraints();

            if (constraints == null)
                constraints = EmptyArray<Type>.Value;

            return constraints;
        }
        #endregion

        #region Misc
        [System.Security.SecuritySafeCritical]  // auto-generated
        public override Type MakePointerType() { return new RuntimeTypeHandle(this).MakePointer(); }
        public override Type MakeByRefType() { return new RuntimeTypeHandle(this).MakeByRef(); }
        public override Type MakeArrayType() { return new RuntimeTypeHandle(this).MakeSZArray(); }
        public override Type MakeArrayType(int rank) 
        {
            if (rank <= 0)
                throw new IndexOutOfRangeException();
            Contract.EndContractBlock();

            return new RuntimeTypeHandle(this).MakeArray(rank); 
        }
        public override StructLayoutAttribute StructLayoutAttribute
        {
            [System.Security.SecuritySafeCritical] // overrides transparent public member             
            get 
            { 
                return (StructLayoutAttribute)StructLayoutAttribute.GetCustomAttribute(this); 
            } 
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

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static private extern bool CanValueSpecialCast(RuntimeType valueType, RuntimeType targetType);
        
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static private extern Object AllocateValueType(RuntimeType type, object value, bool fForceTypeChange); 

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe Object CheckValue(Object value, Binder binder, CultureInfo culture, BindingFlags invokeAttr) 
        {
            // this method is used by invocation in reflection to check whether a value can be assigned to type.
            if (IsInstanceOfType(value))
            {
                // Since this cannot be a generic parameter, we use RuntimeTypeHandle.IsValueType here
                // because it is faster than RuntimeType.IsValueType
                Contract.Assert(!IsGenericParameter);

                Type type = null;

#if FEATURE_REMOTING
                // For the remoting objects Object.GetType goes through proxy. Avoid the proxy call and just get
                // the type directly. It is necessary to support proxies that do not handle GetType.
                RealProxy realProxy = System.Runtime.Remoting.RemotingServices.GetRealProxy(value);

                if (realProxy != null)
                {
                    type = realProxy.GetProxiedType();
                }
                else
                {
                    type = value.GetType();
                }
#else
                type = value.GetType();
#endif

                if (!Object.ReferenceEquals(type, this) && RuntimeTypeHandle.IsValueType(this))
                {
                    // must be an equivalent type, re-box to the target type
                    return AllocateValueType(this, value, true);
                }
                else
                {
                    return value;
                }
            }

            // if this is a ByRef get the element type and check if it's compatible
            bool isByRef = IsByRef;
            if (isByRef) 
            {
                RuntimeType elementType = RuntimeTypeHandle.GetElementType(this);
                if (elementType.IsInstanceOfType(value) || value == null) 
                {
                    // need to create an instance of the ByRef if null was provided, but only if primitive, enum or value type
                    return AllocateValueType(elementType, value, false);
                }
            }
            else if (value == null) 
                return value;
            else if (this == s_typedRef)
                // everything works for a typedref
                return value;

            // check the strange ones courtesy of reflection:
            // - implicit cast between primitives
            // - enum treated as underlying type
            // - IntPtr and System.Reflection.Pointer to pointer types
            bool needsSpecialCast = IsPointer || IsEnum || IsPrimitive;
            if (needsSpecialCast) 
            {
                RuntimeType valueType;
                Pointer pointer = value as Pointer;
                if (pointer != null) 
                    valueType = pointer.GetPointerType();
                else
                    valueType = (RuntimeType)value.GetType();

                if (CanValueSpecialCast(valueType, this))
                {
                    if (pointer != null) 
                        return pointer.GetPointerValue();
                    else
                        return value;
                }
            }
            
            if ((invokeAttr & BindingFlags.ExactBinding) == BindingFlags.ExactBinding)
                throw new ArgumentException(String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Arg_ObjObjEx"), value.GetType(), this));

            return TryChangeType(value, binder, culture, needsSpecialCast);
        }

        // Factored out of CheckValue to reduce code complexity.
        [System.Security.SecurityCritical]
        private Object TryChangeType(Object value, Binder binder, CultureInfo culture, bool needsSpecialCast)
        {
            if (binder != null && binder != Type.DefaultBinder) 
            {
                value = binder.ChangeType(value, this, culture); 
                if (IsInstanceOfType(value)) 
                    return value;
                // if this is a ByRef get the element type and check if it's compatible
                if (IsByRef) 
                {
                    RuntimeType elementType = RuntimeTypeHandle.GetElementType(this);
                    if (elementType.IsInstanceOfType(value) || value == null) 
                        return AllocateValueType(elementType, value, false);
                }
                else if (value == null) 
                    return value;
                if (needsSpecialCast) 
                {
                    RuntimeType valueType;
                    Pointer pointer = value as Pointer;
                    if (pointer != null) 
                        valueType = pointer.GetPointerType();
                    else
                        valueType = (RuntimeType)value.GetType();

                    if (CanValueSpecialCast(valueType, this)) 
                    {
                        if (pointer != null) 
                            return pointer.GetPointerValue();
                        else
                            return value;
                    }
                }
            }

            throw new ArgumentException(String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Arg_ObjObjEx"), value.GetType(), this));
        }

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
                members = EmptyArray<MemberInfo>.Value;

            return members;
        }

#if FEATURE_COMINTEROP
        [System.Security.SecuritySafeCritical]  // auto-generated
#endif
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override Object InvokeMember(
            String name, BindingFlags bindingFlags, Binder binder, Object target, 
            Object[] providedArgs, ParameterModifier[] modifiers, CultureInfo culture, String[] namedParams) 
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

            #region COM Interop
#if FEATURE_COMINTEROP && FEATURE_USE_LCID
            if (target != null && target.GetType().IsCOMObject)
            {
                #region Preconditions
                if ((bindingFlags & ClassicBindingMask) == 0)
                    throw new ArgumentException(Environment.GetResourceString("Arg_COMAccess"), "bindingFlags");

                if ((bindingFlags & BindingFlags.GetProperty) != 0 && (bindingFlags & ClassicBindingMask & ~(BindingFlags.GetProperty | BindingFlags.InvokeMethod)) != 0)
                    throw new ArgumentException(Environment.GetResourceString("Arg_PropSetGet"), "bindingFlags");

                if ((bindingFlags & BindingFlags.InvokeMethod) != 0 && (bindingFlags & ClassicBindingMask & ~(BindingFlags.GetProperty | BindingFlags.InvokeMethod)) != 0)
                    throw new ArgumentException(Environment.GetResourceString("Arg_PropSetInvoke"), "bindingFlags");

                if ((bindingFlags & BindingFlags.SetProperty) != 0 && (bindingFlags & ClassicBindingMask & ~BindingFlags.SetProperty) != 0)
                    throw new ArgumentException(Environment.GetResourceString("Arg_COMPropSetPut"), "bindingFlags");

                if ((bindingFlags & BindingFlags.PutDispProperty) != 0 && (bindingFlags & ClassicBindingMask & ~BindingFlags.PutDispProperty) != 0)
                    throw new ArgumentException(Environment.GetResourceString("Arg_COMPropSetPut"), "bindingFlags");

                if ((bindingFlags & BindingFlags.PutRefDispProperty) != 0 && (bindingFlags & ClassicBindingMask & ~BindingFlags.PutRefDispProperty) != 0)
                    throw new ArgumentException(Environment.GetResourceString("Arg_COMPropSetPut"), "bindingFlags");
                #endregion

#if FEATURE_REMOTING
                if(!RemotingServices.IsTransparentProxy(target))
#endif
                {
                    #region Non-TransparentProxy case
                    if (name == null)
                        throw new ArgumentNullException("name");

                    bool[] isByRef = modifiers == null ? null : modifiers[0].IsByRefArray;
                    
                    // pass LCID_ENGLISH_US if no explicit culture is specified to match the behavior of VB
                    int lcid = (culture == null ? 0x0409 : culture.LCID);

                    return InvokeDispMethod(name, bindingFlags, target, providedArgs, isByRef, lcid, namedParams);
                    #endregion
                }
#if FEATURE_REMOTING
                else
                {
                    #region TransparentProxy case
                    return ((MarshalByRefObject)target).InvokeMember(name, bindingFlags, binder, providedArgs, modifiers, culture, namedParams);
                    #endregion
                }
#endif // FEATURE_REMOTING
            }
#endif // FEATURE_COMINTEROP && FEATURE_USE_LCID
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

            bool bDefaultBinder = (binder == DefaultBinder);
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
                        providedArgs = EmptyArray<Object>.Value;

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

        #endregion

        #region Object Overrides
        [Pure]
        public override bool Equals(object obj)
        {
            // ComObjects are identified by the instance of the Type object and not the TypeHandle.
            return obj == (object)this;
        }

        public override int GetHashCode() 
        {
            return RuntimeHelpers.GetHashCode(this);
        }

#if !FEATURE_CORECLR
        public static bool operator ==(RuntimeType left, RuntimeType right)
        {
            return object.ReferenceEquals(left, right);
        }

        public static bool operator !=(RuntimeType left, RuntimeType right)
        {
            return !object.ReferenceEquals(left, right);
        }
#endif // !FEATURE_CORECLR

        public override String ToString() 
        {
            return GetCachedName(TypeNameKind.ToString);
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

            UnitySerializationHolder.GetUnitySerializationInfo(info, this);
        }
        #endregion

        #region ICustomAttributeProvider
        [System.Security.SecuritySafeCritical]  // auto-generated
        public override Object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, RuntimeType.ObjectType, inherit);
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
        public override String Name 
        {
            get 
            {
                return GetCachedName(TypeNameKind.Name);
            }
        }

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

        private string GetCachedName(TypeNameKind kind)
        {
            return Cache.GetName(kind);
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

        public override Type DeclaringType 
        {
            get 
            {
                return Cache.GetEnclosingType();
            }
        }

        public override Type ReflectedType 
        {
            get 
            {
                return DeclaringType;
            }
        }

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
            BindingFlags bindingAttr, Binder binder, Object[] args, CultureInfo culture, Object[] activationAttributes, ref StackCrawlMark stackMark)
        {            
            CreateInstanceCheckThis();
            
            Object server = null;

            try
            {
                try
                {
                    // Store the activation attributes in thread local storage.
                    // These attributes are later picked up by specialized 
                    // activation services like remote activation services to
                    // influence the activation.
#if FEATURE_REMOTING                                    
                    if(null != activationAttributes)
                    {
                        ActivationServices.PushActivationAttributes(this, activationAttributes);
                    }
#endif                    
                    
                    if (args == null)
                        args = EmptyArray<Object>.Value;

                    int argCnt = args.Length;

                    // Without a binder we need to do use the default binder...
                    if (binder == null)
                        binder = DefaultBinder;

                    // deal with the __COMObject case first. It is very special because from a reflection point of view it has no ctors
                    // so a call to GetMemberCons would fail
                    if (argCnt == 0 && (bindingAttr & BindingFlags.Public) != 0 && (bindingAttr & BindingFlags.Instance) != 0
                        && (IsGenericCOMObjectImpl() || IsValueType)) 
                    {
                        server = CreateInstanceDefaultCtor((bindingAttr & BindingFlags.NonPublic) == 0 , false, true, ref stackMark);
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
                            // Null out activation attributes before throwing exception
#if FEATURE_REMOTING                                            
                            if(null != activationAttributes)
                            {
                                ActivationServices.PopActivationAttributes(this);
                                activationAttributes = null;
                            }
#endif                            
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
#if FEATURE_REMOTING                                            
                            // Null out activation attributes before throwing exception
                            if(null != activationAttributes)
                            {
                                ActivationServices.PopActivationAttributes(this);
                                activationAttributes = null;
                            }                  
#endif                                
                            throw new MissingMethodException(Environment.GetResourceString("MissingConstructor_Name", FullName));
                        }

                        // If we're creating a delegate, we're about to call a
                        // constructor taking an integer to represent a target
                        // method. Since this is very difficult (and expensive)
                        // to verify, we're just going to demand UnmanagedCode
                        // permission before allowing this. Partially trusted
                        // clients can instead use Delegate.CreateDelegate,
                        // which allows specification of the target method via
                        // name or MethodInfo.
                        //if (isDelegate)
                        if (RuntimeType.DelegateType.IsAssignableFrom(invokeMethod.DeclaringType))
                        {
#if FEATURE_CORECLR
                            // In CoreCLR, CAS is not exposed externally. So what we really are looking
                            // for is to see if the external caller of this API is transparent or not.
                            // We get that information from the fact that a Demand will succeed only if
                            // the external caller is not transparent. 
                            try
                            {
#pragma warning disable 618
                                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
#pragma warning restore 618
                            }
                            catch
                            {
                                throw new NotSupportedException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("NotSupported_DelegateCreationFromPT")));
                            }
#else // FEATURE_CORECLR
                            new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
#endif // FEATURE_CORECLR
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
                            server = Activator.CreateInstance(this, true);
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
#if FEATURE_REMOTING                
                    // Reset the TLS to null
                    if(null != activationAttributes)
                    {
                          ActivationServices.PopActivationAttributes(this);
                          activationAttributes = null;
                    }
#endif                    
                }
            }
            catch (Exception)
            {
                throw;
            }
            
            //Console.WriteLine(server);
            return server;                                
        }

        // the cache entry
        class ActivatorCacheEntry
        {
            // the type to cache
            internal readonly RuntimeType m_type;
            // the delegate containing the call to the ctor, will be replaced by an IntPtr to feed a calli with
            internal volatile CtorDelegate m_ctor;
            internal readonly RuntimeMethodHandleInternal m_hCtorMethodHandle;
            internal readonly MethodAttributes m_ctorAttributes;
            // Is a security check needed before this constructor is invoked?
            internal readonly bool m_bNeedSecurityCheck;
            // Lazy initialization was performed
            internal volatile bool m_bFullyInitialized;

            [System.Security.SecurityCritical]
            internal ActivatorCacheEntry(RuntimeType t, RuntimeMethodHandleInternal rmh, bool bNeedSecurityCheck)
            {
                m_type = t;
                m_bNeedSecurityCheck = bNeedSecurityCheck;
                m_hCtorMethodHandle = rmh;
                if (!m_hCtorMethodHandle.IsNullHandle())
                    m_ctorAttributes = RuntimeMethodHandle.GetAttributes(m_hCtorMethodHandle);
            }
        }

        //ActivatorCache
        class ActivatorCache
        {
            const int CACHE_SIZE = 16;
            volatile int hash_counter; //Counter for wrap around
            readonly ActivatorCacheEntry[] cache = new ActivatorCacheEntry[CACHE_SIZE];

            volatile ConstructorInfo     delegateCtorInfo;
            volatile PermissionSet       delegateCreatePermissions;

            private void InitializeDelegateCreator() {
                // No synchronization needed here. In the worst case we create extra garbage
                PermissionSet ps = new PermissionSet(PermissionState.None);
                ps.AddPermission(new ReflectionPermission(ReflectionPermissionFlag.MemberAccess));
#pragma warning disable 618
                ps.AddPermission(new SecurityPermission(SecurityPermissionFlag.UnmanagedCode));
#pragma warning restore 618
                delegateCreatePermissions = ps;

                ConstructorInfo ctorInfo = typeof(CtorDelegate).GetConstructor(new Type[] {typeof(Object), typeof(IntPtr)});
                delegateCtorInfo = ctorInfo; // this assignment should be last
            }

            [System.Security.SecuritySafeCritical]  // auto-generated
            private void InitializeCacheEntry(ActivatorCacheEntry ace)
            {
                if (!ace.m_type.IsValueType)
                {
                    Contract.Assert(!ace.m_hCtorMethodHandle.IsNullHandle(), "Expected the default ctor method handle for a reference type.");
                    
                    if (delegateCtorInfo == null)
                        InitializeDelegateCreator();
                    delegateCreatePermissions.Assert();

                    // No synchronization needed here. In the worst case we create extra garbage
                    CtorDelegate ctor = (CtorDelegate)delegateCtorInfo.Invoke(new Object[] { null, RuntimeMethodHandle.GetFunctionPointer(ace.m_hCtorMethodHandle) });
                    ace.m_ctor = ctor;
                }
                ace.m_bFullyInitialized = true;
            }

            internal ActivatorCacheEntry GetEntry(RuntimeType t)
            {
                int index = hash_counter;
                for(int i = 0; i < CACHE_SIZE; i++)
                {
                    ActivatorCacheEntry ace = Volatile.Read(ref cache[index]);
                    if (ace != null && ace.m_type == t) //check for type match..
                    {
                        if (!ace.m_bFullyInitialized)
                            InitializeCacheEntry(ace);
                        return ace;
                    }
                    index = (index+1)&(ActivatorCache.CACHE_SIZE-1);
                }
                return null;
            }

            internal void SetEntry(ActivatorCacheEntry ace)
            {
                // fill the the array backwards to hit the most recently filled entries first in GetEntry
                int index = (hash_counter-1)&(ActivatorCache.CACHE_SIZE-1);
                hash_counter = index;
                Volatile.Write(ref cache[index], ace);
            }
        }

        private static volatile ActivatorCache s_ActivatorCache;

        // the slow path of CreateInstanceDefaultCtor
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal Object CreateInstanceSlow(bool publicOnly, bool skipCheckThis, bool fillCache, ref StackCrawlMark stackMark)
        {
            RuntimeMethodHandleInternal runtime_ctor = default(RuntimeMethodHandleInternal);
            bool bNeedSecurityCheck = true;
            bool bCanBeCached = false;
            bool bSecurityCheckOff = false;

            if (!skipCheckThis)
                CreateInstanceCheckThis();

            if (!fillCache)
                bSecurityCheckOff = true;

#if FEATURE_APPX
            INVOCATION_FLAGS invocationFlags = InvocationFlags;
            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
            {
                RuntimeAssembly caller = RuntimeAssembly.GetExecutingAssembly(ref stackMark);
                if (caller != null && !caller.IsSafeForReflection())
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", this.FullName));
                
                // Allow it because the caller is framework code, but don't cache the result
                // because we need to do the stack walk every time this type is instantiated.
                bSecurityCheckOff = false;
                bCanBeCached = false;
            }
#endif
#if FEATURE_CORECLR
            bSecurityCheckOff = true;       // CoreCLR does not use security at all.   
#endif

            Object instance = RuntimeTypeHandle.CreateInstance(this, publicOnly, bSecurityCheckOff, ref bCanBeCached, ref runtime_ctor, ref bNeedSecurityCheck);

            if (bCanBeCached && fillCache)
            {
                ActivatorCache activatorCache = s_ActivatorCache;
                if (activatorCache == null)
                {
                    // No synchronization needed here. In the worst case we create extra garbage
                    activatorCache = new ActivatorCache();
                    s_ActivatorCache = activatorCache;
                }

                // cache the ctor
                ActivatorCacheEntry ace = new ActivatorCacheEntry(this, runtime_ctor, bNeedSecurityCheck);
                activatorCache.SetEntry(ace);
            }

            return instance;
        }

        // Helper to invoke the default (parameterless) ctor.
        // fillCache is set in the SL2/3 compat mode or when called from Marshal.PtrToStructure.
        [System.Security.SecuritySafeCritical]  // auto-generated
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        internal Object CreateInstanceDefaultCtor(bool publicOnly, bool skipCheckThis, bool fillCache, ref StackCrawlMark stackMark)
        {
            if (GetType() == typeof(ReflectionOnlyType))
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAllowedInReflectionOnly"));

            ActivatorCache activatorCache = s_ActivatorCache;
            if (activatorCache != null)
            {
                ActivatorCacheEntry ace = activatorCache.GetEntry(this);
                if (ace != null)
                {
                    if (publicOnly)
                    {
                        if (ace.m_ctor != null && 
                            (ace.m_ctorAttributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public)
                        {
                            throw new MissingMethodException(Environment.GetResourceString("Arg_NoDefCTor"));
                        }
                    }
                            
                    // Allocate empty object
                    Object instance = RuntimeTypeHandle.Allocate(this);
                    
                    // if m_ctor is null, this type doesn't have a default ctor
                    Contract.Assert(ace.m_ctor != null || this.IsValueType);

                    if (ace.m_ctor != null)
                    {
#if !FEATURE_CORECLR
                        // Perform security checks if needed
                        if (ace.m_bNeedSecurityCheck)
                            RuntimeMethodHandle.PerformSecurityCheck(instance, ace.m_hCtorMethodHandle, this, (uint)INVOCATION_FLAGS.INVOCATION_FLAGS_CONSTRUCTOR_INVOKE);
#endif

                        // Call ctor (value types wont have any)
                        try
                        {
                            ace.m_ctor(instance);
                        }
                        catch (Exception e)
                        {
                            throw new TargetInvocationException(e);
                        }
                    }
                    return instance;
                }
            }
            return CreateInstanceSlow(publicOnly, skipCheckThis, fillCache, ref stackMark);
        }

        internal void InvalidateCachedNestedType()
        {
            Cache.InvalidateCachedNestedType();
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal bool IsGenericCOMObjectImpl()
        {
            return RuntimeTypeHandle.IsComObject(this, true);
        }
        #endregion

        #region Legacy Static Internal
        [System.Security.SecurityCritical]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern Object _CreateEnum(RuntimeType enumType, long value);
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static Object CreateEnum(RuntimeType enumType, long value)
        {
            return _CreateEnum(enumType, value);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern Object InvokeDispMethod(
            String name, BindingFlags invokeAttr, Object target, Object[] args,
            bool[] byrefModifiers, int culture, String[] namedParameters);

#if FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern Type GetTypeFromProgIDImpl(String progID, String server, bool throwOnError);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern Type GetTypeFromCLSIDImpl(Guid clsid, String server, bool throwOnError);
#else // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
        internal static Type GetTypeFromProgIDImpl(String progID, String server, bool throwOnError)
        {
            throw new NotImplementedException("CoreCLR_REMOVED -- Unmanaged activation removed");
        }

        internal static Type GetTypeFromCLSIDImpl(Guid clsid, String server, bool throwOnError)
        {
            throw new NotImplementedException("CoreCLR_REMOVED -- Unmanaged activation removed"); 
        }
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION

        #endregion

        #region COM
#if FEATURE_COMINTEROP && FEATURE_REMOTING
        [System.Security.SecuritySafeCritical]  // auto-generated
        private Object ForwardCallToInvokeMember(String memberName, BindingFlags flags, Object target, int[] aWrapperTypes, ref MessageData msgData)
        {
            ParameterModifier[] aParamMod = null;
            Object ret = null;

            // Allocate a new message
            Message reqMsg = new Message();
            reqMsg.InitFields(msgData);

            // Retrieve the required information from the message object.
            MethodInfo meth = (MethodInfo)reqMsg.GetMethodBase();
            Object[] aArgs = reqMsg.Args;
            int cArgs = aArgs.Length;

            // Retrieve information from the method we are invoking on.
            ParameterInfo[] aParams = meth.GetParametersNoCopy();

            // If we have arguments, then set the byref flags to true for byref arguments. 
            // We also wrap the arguments that require wrapping.
            if (cArgs > 0)
            {
                ParameterModifier paramMod = new ParameterModifier(cArgs);
                for (int i = 0; i < cArgs; i++)
                {
                    if (aParams[i].ParameterType.IsByRef)
                        paramMod[i] = true;
                }

                aParamMod = new ParameterModifier[1];
                aParamMod[0] = paramMod;

                if (aWrapperTypes != null)
                    WrapArgsForInvokeCall(aArgs, aWrapperTypes);
            }
            
            // If the method has a void return type, then set the IgnoreReturn binding flag.
            if (Object.ReferenceEquals(meth.ReturnType, typeof(void)))
                flags |= BindingFlags.IgnoreReturn;

            try
            {
                // Invoke the method using InvokeMember().
                ret = InvokeMember(memberName, flags, null, target, aArgs, aParamMod, null, null);
            }
            catch (TargetInvocationException e)
            {
                // For target invocation exceptions, we need to unwrap the inner exception and
                // re-throw it.
                throw e.InnerException;
            }

            // Convert each byref argument that is not of the proper type to
            // the parameter type using the OleAutBinder.
            for (int i = 0; i < cArgs; i++)
            {
                if (aParamMod[0][i] && aArgs[i] != null)
                {
                    // The parameter is byref.
                    Type paramType = aParams[i].ParameterType.GetElementType();
                    if (!Object.ReferenceEquals(paramType, aArgs[i].GetType()))
                        aArgs[i] = ForwardCallBinder.ChangeType(aArgs[i], paramType, null);
                }
            }

            // If the return type is not of the proper type, then convert it
            // to the proper type using the OleAutBinder.
            if (ret != null)
            {
                Type retType = meth.ReturnType;
                if (!Object.ReferenceEquals(retType, ret.GetType()))
                    ret = ForwardCallBinder.ChangeType(ret, retType, null);
            }

            // Propagate the out parameters
            RealProxy.PropagateOutParameters(reqMsg, aArgs, ret);

            // Return the value returned by the InvokeMember call.
            return ret;
        }

        [SecuritySafeCritical]
        private void WrapArgsForInvokeCall(Object[] aArgs, int[] aWrapperTypes)
        {
            int cArgs = aArgs.Length;
            for (int i = 0; i < cArgs; i++)
            {
                if (aWrapperTypes[i] == 0)
                    continue;

                if (((DispatchWrapperType)aWrapperTypes[i] & DispatchWrapperType.SafeArray) != 0)
                {
                    Type wrapperType = null;
                    bool isString = false;

                    // Determine the type of wrapper to use.
                    switch ((DispatchWrapperType)aWrapperTypes[i] & ~DispatchWrapperType.SafeArray)
                    {
                        case DispatchWrapperType.Unknown:
                            wrapperType = typeof(UnknownWrapper);
                            break;
                        case DispatchWrapperType.Dispatch:
                            wrapperType = typeof(DispatchWrapper);
                            break;
                        case DispatchWrapperType.Error:   
                            wrapperType = typeof(ErrorWrapper);
                            break;
                        case DispatchWrapperType.Currency:
                            wrapperType = typeof(CurrencyWrapper);
                            break;
                        case DispatchWrapperType.BStr:
                            wrapperType = typeof(BStrWrapper);
                            isString = true;
                            break;
                        default:
                            Contract.Assert(false, "[RuntimeType.WrapArgsForInvokeCall]Invalid safe array wrapper type specified.");
                            break;
                    }

                    // Allocate the new array of wrappers.
                    Array oldArray = (Array)aArgs[i];
                    int numElems = oldArray.Length;
                    Object[] newArray = (Object[])Array.UnsafeCreateInstance(wrapperType, numElems);

                    // Retrieve the ConstructorInfo for the wrapper type.
                    ConstructorInfo wrapperCons;
                    if(isString)
                    {
                         wrapperCons = wrapperType.GetConstructor(new Type[] {typeof(String)});
                    }
                    else
                    {
                         wrapperCons = wrapperType.GetConstructor(new Type[] {typeof(Object)});
                    }
                
                    // Wrap each of the elements of the array.
                    for (int currElem = 0; currElem < numElems; currElem++)
                    {
                        if(isString)
                        {
                            newArray[currElem] = wrapperCons.Invoke(new Object[] {(String)oldArray.GetValue(currElem)});
                        }
                        else
                        {
                            newArray[currElem] = wrapperCons.Invoke(new Object[] {oldArray.GetValue(currElem)});
                        }
                    }

                    // Update the argument.
                    aArgs[i] = newArray;
                }
                else
                {                           
                    // Determine the wrapper to use and then wrap the argument.
                    switch ((DispatchWrapperType)aWrapperTypes[i])
                    {
                        case DispatchWrapperType.Unknown:
                            aArgs[i] = new UnknownWrapper(aArgs[i]);
                            break;
                        case DispatchWrapperType.Dispatch:
                            aArgs[i] = new DispatchWrapper(aArgs[i]);
                            break;
                        case DispatchWrapperType.Error:   
                            aArgs[i] = new ErrorWrapper(aArgs[i]);
                            break;
                        case DispatchWrapperType.Currency:
                            aArgs[i] = new CurrencyWrapper(aArgs[i]);
                            break;
                        case DispatchWrapperType.BStr:
                            aArgs[i] = new BStrWrapper((String)aArgs[i]);
                            break;
                        default:
                            Contract.Assert(false, "[RuntimeType.WrapArgsForInvokeCall]Invalid wrapper type specified.");
                            break;
                    }
                }
            }
        }

        private OleAutBinder ForwardCallBinder 
        {
            get 
            {
                // Synchronization is not required.
                if (s_ForwardCallBinder == null)
                    s_ForwardCallBinder = new OleAutBinder();

                return s_ForwardCallBinder;
            }
        }

        [Flags]
        private enum DispatchWrapperType : int
        {
            // This enum must stay in sync with the DispatchWrapperType enum defined in MLInfo.h
            Unknown         = 0x00000001,
            Dispatch        = 0x00000002,
            Record          = 0x00000004,
            Error           = 0x00000008,
            Currency        = 0x00000010,
            BStr            = 0x00000020,
            SafeArray       = 0x00010000
        }

        private static volatile OleAutBinder s_ForwardCallBinder;
#endif // FEATURE_COMINTEROP && FEATURE_REMOTING
        #endregion
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

    #region Library
    internal unsafe struct Utf8String
    {
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe bool EqualsCaseSensitive(void* szLhs, void* szRhs, int cSz);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern unsafe bool EqualsCaseInsensitive(void* szLhs, void* szRhs, int cSz);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern unsafe uint HashCaseInsensitive(void* sz, int cSz);

        [System.Security.SecurityCritical]  // auto-generated
        private static int GetUtf8StringByteLength(void* pUtf8String)
        {
            int len = 0;

            unsafe
            {
                byte* pItr = (byte*)pUtf8String;

                while (*pItr != 0)
                {
                    len++;
                    pItr++;
                }
            }

            return len;
        }

        [SecurityCritical]
        private void* m_pStringHeap;        // This is the raw UTF8 string.
        private int m_StringHeapByteLength;

        [System.Security.SecurityCritical]  // auto-generated
        internal Utf8String(void* pStringHeap)
        {
            m_pStringHeap = pStringHeap;
            if (pStringHeap != null)
            {
                m_StringHeapByteLength = GetUtf8StringByteLength(pStringHeap);
            }
            else
            {
                m_StringHeapByteLength = 0;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe Utf8String(void* pUtf8String, int cUtf8String)
        {
            m_pStringHeap = pUtf8String;
            m_StringHeapByteLength = cUtf8String;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe bool Equals(Utf8String s)
        {
            if (m_pStringHeap == null)
            {
                return s.m_StringHeapByteLength == 0;
            }
            if ((s.m_StringHeapByteLength == m_StringHeapByteLength) && (m_StringHeapByteLength != 0))
            {
                return Utf8String.EqualsCaseSensitive(s.m_pStringHeap, m_pStringHeap, m_StringHeapByteLength);
            }
            return false;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe bool EqualsCaseInsensitive(Utf8String s)
        {
            if (m_pStringHeap == null)
            {
                return s.m_StringHeapByteLength == 0;
            }
            if ((s.m_StringHeapByteLength == m_StringHeapByteLength) && (m_StringHeapByteLength != 0))
            {
                return Utf8String.EqualsCaseInsensitive(s.m_pStringHeap, m_pStringHeap, m_StringHeapByteLength);
            }
            return false;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe uint HashCaseInsensitive()
        {
            return Utf8String.HashCaseInsensitive(m_pStringHeap, m_StringHeapByteLength);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override string ToString()
        {
            unsafe
            {
                byte* buf = stackalloc byte[m_StringHeapByteLength];
                byte* pItr = (byte*)m_pStringHeap;

                for (int currentPos = 0; currentPos < m_StringHeapByteLength; currentPos++)
                {
                    buf[currentPos] = *pItr;
                    pItr++;
                }

                if (m_StringHeapByteLength == 0)
                    return "";

                int cResult = Encoding.UTF8.GetCharCount(buf, m_StringHeapByteLength);
                char* result = stackalloc char[cResult];
                Encoding.UTF8.GetChars(buf, m_StringHeapByteLength, result, cResult);
                return new string(result, 0, cResult);
            }
        }
    }
    #endregion
}

namespace System.Reflection
{
    // Reliable hashtable thread safe for multiple readers and single writer. Note that the reliability goes together with thread
    // safety. Thread safety for multiple readers requires atomic update of the state that also makes makes the table
    // reliable in the presence of asynchronous exceptions.
    internal struct CerHashtable<K, V> where K : class
    {
        private class Table
        {
            // Note that m_keys and m_values arrays are immutable to allow lock-free reads. A new instance 
            // of CerHashtable has to be allocated to grow the size of the hashtable.
            internal K[] m_keys;
            internal V[] m_values;
            internal int m_count;

            internal Table(int size)
            {
                size = HashHelpers.GetPrime(size);
                m_keys = new K[size];
                m_values = new V[size];
            }

            internal void Insert(K key, V value)
            {

                int hashcode = GetHashCodeHelper(key);
                if (hashcode < 0)
                    hashcode = ~hashcode;

                K[] keys = m_keys;
                int index = hashcode % keys.Length;

                while (true)
                {
                    K hit = keys[index];

                    if (hit == null)
                    {
                        m_count++;
                        m_values[index] = value;

                        // This volatile write has to be last. It is going to publish the result atomically.
                        //
                        // Note that incrementing the count or setting the value does not do any harm without setting the key. The inconsistency will be ignored 
                        // and it will go away completely during next rehash.
                        Volatile.Write(ref keys[index], key);

                        break;
                    }
                    else
                    {
                        Contract.Assert(!hit.Equals(key), "Key was already in CerHashtable!  Potential race condition (or bug) in the Reflection cache?");

                        index++;
                        if (index >= keys.Length)
                            index -= keys.Length;
                    }
                }
            }
        }

        private Table m_Table;

        private const int MinSize = 7;

        private static int GetHashCodeHelper(K key)
        {
            string sKey = key as string;

            // For strings we don't want the key to differ across domains as CerHashtable might be shared.
            if(sKey == null)
            {
                return key.GetHashCode(); 

            }
            else
            {
                return sKey.GetLegacyNonRandomizedHashCode();
            }               
        }

        private void Rehash(int newSize)
        {
            Table newTable = new Table(newSize);

            Table oldTable = m_Table;
            if (oldTable != null)
            {
                K[] keys = oldTable.m_keys;
                V[] values = oldTable.m_values;

                for (int i = 0; i < keys.Length; i++)
                {
                    K key = keys[i];

                    if (key != null)
                    {
                        newTable.Insert(key, values[i]);
                    }
                }
            }

            // Publish the new table atomically
            Volatile.Write(ref m_Table, newTable);
        }

        internal V this[K key]
        {
            set
            {
                Table table = m_Table;

                if (table != null)
                {
                    int requiredSize = 2 * (table.m_count + 1);
                    if (requiredSize >= table.m_keys.Length)
                        Rehash(requiredSize);
                }
                else
                {
                    Rehash(MinSize);
                }

                m_Table.Insert(key, value);
            }
            get
            {
                Table table = Volatile.Read(ref m_Table);
                if (table == null)
                    return default(V);

                int hashcode = GetHashCodeHelper(key);
                if (hashcode < 0)
                    hashcode = ~hashcode;

                K[] keys = table.m_keys;
                int index = hashcode % keys.Length;

                while (true)
                {
                    // This volatile read has to be first. It is reading the atomically published result.
                    K hit = Volatile.Read(ref keys[index]);

                    if (hit != null)
                    {
                        if (hit.Equals(key))
                            return table.m_values[index];

                        index++;
                        if (index >= keys.Length)
                            index -= keys.Length;
                    }
                    else
                    {
                        return default(V);
                    }
                }
            }
        }
    }
}
