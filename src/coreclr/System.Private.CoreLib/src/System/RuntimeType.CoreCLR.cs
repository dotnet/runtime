// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Internal.Runtime.CompilerServices;
using DebuggerStepThroughAttribute = System.Diagnostics.DebuggerStepThroughAttribute;
using MdToken = System.Reflection.MetadataToken;

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
#if DEBUG
        FormatDebug = 0x00000020, // For debug printing of types only
#endif
        FormatAngleBrackets = 0x00000040, // Whether generic types are C<T> or C[T]
        FormatStubInfo = 0x00000080, // Include stub info like {unbox-stub}
        FormatGenericParam = 0x00000100, // Use !name and !!name for generic type and method parameters
    }

    internal enum TypeNameKind
    {
        Name,
        ToString,
        FullName,
    }

    internal sealed partial class RuntimeType : TypeInfo, ICloneable
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
        internal struct ListBuilder<T> where T : class
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
                return _items!;
            }

            public void CopyTo(object[] array, int index)
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

            public int Count => _count;

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
                    else if (_capacity == _count)
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

        internal sealed class RuntimeTypeCache
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

            private readonly struct Filter
            {
                private readonly MdUtf8String m_name;
                private readonly MemberListType m_listType;
                private readonly uint m_nameHash;

                public unsafe Filter(byte* pUtf8Name, int cUtf8Name, MemberListType listType)
                {
                    m_name = new MdUtf8String(pUtf8Name, cUtf8Name);
                    m_listType = listType;
                    m_nameHash = 0;

                    if (RequiresStringComparison())
                    {
                        m_nameHash = m_name.HashCaseInsensitive();
                    }
                }

                public bool Match(MdUtf8String name)
                {
                    bool retVal = true;

                    if (m_listType == MemberListType.CaseSensitive)
                        retVal = m_name.Equals(name);
                    else if (m_listType == MemberListType.CaseInsensitive)
                        retVal = m_name.EqualsCaseInsensitive(name);

                    // Currently the callers of UsesStringComparison assume that if it returns false
                    // then the match always succeeds and can be skipped.  Assert that this is maintained.
                    Debug.Assert(retVal || RequiresStringComparison());

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

                public bool CaseSensitive() => m_listType == MemberListType.CaseSensitive;

                public uint GetHashToMatch()
                {
                    Debug.Assert(RequiresStringComparison());

                    return m_nameHash;
                }
            }

            private sealed class MemberInfoCache<T> where T : MemberInfo
            {
                #region Private Data Members

                // MemberInfo caches
                private CerHashtable<string, T[]?> m_csMemberInfos;
                private CerHashtable<string, T[]?> m_cisMemberInfos;
                // List of MemberInfos given out. When m_cacheComplete is false, it may have null entries at the end to avoid
                // reallocating the list every time a new entry is added.
                private T[]? m_allMembers;
                private bool m_cacheComplete;

                // This is the strong reference back to the cache
                private readonly RuntimeTypeCache m_runtimeTypeCache;
                #endregion

                #region Constructor

                internal MemberInfoCache(RuntimeTypeCache runtimeTypeCache)
                {
                    m_runtimeTypeCache = runtimeTypeCache;
                }

                internal MethodBase AddMethod(RuntimeType declaringType, RuntimeMethodHandleInternal method, CacheType cacheType)
                {
                    // First, see if we've already cached an RuntimeMethodInfo or
                    // RuntimeConstructorInfo that corresponds to this member. Since another
                    // thread could be updating the backing store at the same time it's
                    // possible that the check below will result in a false negative. That's
                    // ok; we'll handle any concurrency issues in the later call to Insert.

                    T?[]? allMembersLocal = m_allMembers;
                    if (allMembersLocal != null)
                    {
                        // if not a Method or a Constructor, fall through
                        if (cacheType == CacheType.Method)
                        {
                            foreach (T? candidate in allMembersLocal)
                            {
                                if (candidate is null)
                                {
                                    break; // end of list; stop iteration and fall through to slower path
                                }

                                if (candidate is RuntimeMethodInfo candidateRMI && candidateRMI.MethodHandle.Value == method.Value)
                                {
                                    return candidateRMI; // match!
                                }
                            }
                        }
                        else if (cacheType == CacheType.Constructor)
                        {
                            foreach (T? candidate in allMembersLocal)
                            {
                                if (candidate is null)
                                {
                                    break; // end of list; stop iteration and fall through to slower path
                                }

                                if (candidate is RuntimeConstructorInfo candidateRCI && candidateRCI.MethodHandle.Value == method.Value)
                                {
                                    return candidateRCI; // match!
                                }
                            }
                        }
                    }

                    T[] list = null!;
                    MethodAttributes methodAttributes = RuntimeMethodHandle.GetAttributes(method);
                    bool isPublic = (methodAttributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;
                    bool isStatic = (methodAttributes & MethodAttributes.Static) != 0;
                    bool isInherited = declaringType != ReflectedType;
                    BindingFlags bindingFlags = FilterPreCalculate(isPublic, isInherited, isStatic);
                    switch (cacheType)
                    {
                        case CacheType.Method:
                            list = (T[])(object)new RuntimeMethodInfo[1]
                            {
                                new RuntimeMethodInfo(method, declaringType, m_runtimeTypeCache, methodAttributes, bindingFlags, null)
                            };
                            break;

                        case CacheType.Constructor:
                            list = (T[])(object)new RuntimeConstructorInfo[1]
                            {
                                new RuntimeConstructorInfo(method, declaringType, m_runtimeTypeCache, methodAttributes, bindingFlags)
                            };
                            break;
                    }

                    Insert(ref list, null, MemberListType.HandleToInfo);

                    return (MethodBase)(object)list[0];
                }

                internal FieldInfo AddField(RuntimeFieldHandleInternal field)
                {
                    // First, see if we've already cached an RtFieldInfo that corresponds
                    // to this field. Since another thread could be updating the backing
                    // store at the same time it's possible that the check below will
                    // result in a false negative. That's ok; we'll handle any concurrency
                    // issues in the later call to Insert.

                    T?[]? allMembersLocal = m_allMembers;
                    if (allMembersLocal != null)
                    {
                        foreach (T? candidate in allMembersLocal)
                        {
                            if (candidate is null)
                            {
                                break; // end of list; stop iteration and fall through to slower path
                            }

                            if (candidate is RtFieldInfo candidateRtFI && candidateRtFI.GetFieldHandle() == field.Value)
                            {
                                return candidateRtFI; // match!
                            }
                        }
                    }

                    // create the runtime field info
                    FieldAttributes fieldAttributes = RuntimeFieldHandle.GetAttributes(field);
                    bool isPublic = (fieldAttributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public;
                    bool isStatic = (fieldAttributes & FieldAttributes.Static) != 0;
                    RuntimeType approxDeclaringType = RuntimeFieldHandle.GetApproxDeclaringType(field);
                    bool isInherited = RuntimeFieldHandle.AcquiresContextFromThis(field) ?
                        !RuntimeTypeHandle.CompareCanonicalHandles(approxDeclaringType, ReflectedType) :
                        approxDeclaringType != ReflectedType;

                    BindingFlags bindingFlags = FilterPreCalculate(isPublic, isInherited, isStatic);

                    T[] list = (T[])(object)new RuntimeFieldInfo[1] {
                        new RtFieldInfo(field, ReflectedType, m_runtimeTypeCache, bindingFlags)
                    };

                    Insert(ref list, null, MemberListType.HandleToInfo);

                    return (FieldInfo)(object)list[0];
                }

                private unsafe T[] Populate(string? name, MemberListType listType, CacheType cacheType)
                {
                    T[] list;

                    if (string.IsNullOrEmpty(name) ||
                        (cacheType == CacheType.Constructor && name[0] != '.' && name[0] != '*'))
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
                                byte[] utf8Name = new byte[cUtf8Name];
                                fixed (byte* pUtf8Name = &utf8Name[0])
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

                private unsafe T[] GetListByName(char* pName, int cNameLen, byte* pUtf8Name, int cUtf8Name, MemberListType listType, CacheType cacheType)
                {
                    if (cNameLen != 0)
                        Encoding.UTF8.GetBytes(pName, cNameLen, pUtf8Name, cUtf8Name);

                    Filter filter = new Filter(pUtf8Name, cUtf8Name, listType);
                    object list = null!;

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
                            Debug.Fail("Invalid CacheType");
                            break;
                    }

                    return (T[])list;
                }

                // May replace the list with a new one if certain cache
                // lookups succeed.  Also, may modify the contents of the list
                // after merging these new data structures with cached ones.
                internal void Insert(ref T[] list, string? name, MemberListType listType)
                {
                    bool lockTaken = false;

                    try
                    {
                        Monitor.Enter(this, ref lockTaken);

                        switch (listType)
                        {
                            case MemberListType.CaseSensitive:
                                {
                                    // Ensure we always return a list that has
                                    // been merged with the global list.
                                    T[]? cachedList = m_csMemberInfos[name!];
                                    if (cachedList == null)
                                    {
                                        MergeWithGlobalList(list);
                                        m_csMemberInfos[name!] = list;
                                    }
                                    else
                                        list = cachedList;
                                }
                                break;

                            case MemberListType.CaseInsensitive:
                                {
                                    // Ensure we always return a list that has
                                    // been merged with the global list.
                                    T[]? cachedList = m_cisMemberInfos[name!];
                                    if (cachedList == null)
                                    {
                                        MergeWithGlobalList(list);
                                        m_cisMemberInfos[name!] = list;
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
                                    int memberCount = m_allMembers!.Length;
                                    while (memberCount > 0)
                                    {
                                        if (m_allMembers[memberCount - 1] != null)
                                            break;
                                        memberCount--;
                                    }
                                    Array.Resize(ref m_allMembers, memberCount);

                                    Volatile.Write(ref m_cacheComplete, true);
                                }

                                list = m_allMembers!;
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
                private void MergeWithGlobalList(T[] list)
                {
                    T[]? cachedMembers = m_allMembers;

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

                                    Debug.Assert(false);

                                    newSize = cachedMembers.Length + 1;
                                }
                                else
                                {
                                    newSize = Math.Max(Math.Max(4, 2 * cachedMembers.Length), list.Length);
                                }

                                // Use different variable for ref argument to Array.Resize to allow enregistration of cachedMembers by the JIT
                                T[]? cachedMembers2 = cachedMembers;
                                Array.Resize(ref cachedMembers2, newSize);
                                cachedMembers = cachedMembers2;
                            }

                            Debug.Assert(cachedMembers![freeSlotIndex] == null);
                            Volatile.Write(ref cachedMembers[freeSlotIndex], newMemberInfo); // value may be read outside of lock
                            freeSlotIndex++;
                        }
                    }

                    m_allMembers = cachedMembers;
                }
                #endregion

                #region Population Logic

                private unsafe RuntimeMethodInfo[] PopulateMethods(Filter filter)
                {
                    ListBuilder<RuntimeMethodInfo> list = default;

                    RuntimeType declaringType = ReflectedType;
                    Debug.Assert(declaringType != null);

                    if (RuntimeTypeHandle.IsInterface(declaringType))
                    {
                        #region IsInterface

                        foreach (RuntimeMethodHandleInternal methodHandle in RuntimeTypeHandle.GetIntroducedMethods(declaringType))
                        {
                            if (filter.RequiresStringComparison())
                            {
                                if (!RuntimeMethodHandle.MatchesNameHash(methodHandle, filter.GetHashToMatch()))
                                {
                                    Debug.Assert(!filter.Match(RuntimeMethodHandle.GetUtf8Name(methodHandle)));
                                    continue;
                                }

                                if (!filter.Match(RuntimeMethodHandle.GetUtf8Name(methodHandle)))
                                    continue;
                            }

                            #region Loop through all methods on the interface
                            Debug.Assert(!methodHandle.IsNullHandle());

                            MethodAttributes methodAttributes = RuntimeMethodHandle.GetAttributes(methodHandle);

                            #region Continue if this is a constructor
                            Debug.Assert(
                                (RuntimeMethodHandle.GetAttributes(methodHandle) & MethodAttributes.RTSpecialName) == 0 ||
                                RuntimeMethodHandle.GetName(methodHandle).Equals(".cctor"));

                            if ((methodAttributes & MethodAttributes.RTSpecialName) != 0)
                                continue;
                            #endregion

                            #region Calculate Binding Flags
                            bool isPublic = (methodAttributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;
                            bool isStatic = (methodAttributes & MethodAttributes.Static) != 0;
                            BindingFlags bindingFlags = FilterPreCalculate(isPublic, isInherited: false, isStatic);
                            #endregion

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
                        while (RuntimeTypeHandle.IsGenericVariable(declaringType))
                            declaringType = declaringType.GetBaseType()!;

                        int numVirtuals = RuntimeTypeHandle.GetNumVirtuals(declaringType);

                        bool* overrides = stackalloc bool[numVirtuals];
                        new Span<bool>(overrides, numVirtuals).Clear();

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
                                        Debug.Assert(!filter.Match(RuntimeMethodHandle.GetUtf8Name(methodHandle)));
                                        continue;
                                    }

                                    if (!filter.Match(RuntimeMethodHandle.GetUtf8Name(methodHandle)))
                                        continue;
                                }

                                #region Loop through all methods on the current type
                                Debug.Assert(!methodHandle.IsNullHandle());

                                MethodAttributes methodAttributes = RuntimeMethodHandle.GetAttributes(methodHandle);
                                MethodAttributes methodAccess = methodAttributes & MethodAttributes.MemberAccessMask;

                                #region Continue if this is a constructor
                                Debug.Assert(
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
                                    Debug.Assert(
                                        (methodAttributes & MethodAttributes.Abstract) != 0 ||
                                        (methodAttributes & MethodAttributes.Virtual) != 0 ||
                                        RuntimeMethodHandle.GetDeclaringType(methodHandle) != declaringType);

                                    if (overrides[methodSlot])
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
                                    Debug.Assert((methodAttributes & (MethodAttributes.Virtual | MethodAttributes.Abstract)) == 0);
                                }
                                #endregion

                                #region Calculate Binding Flags
                                bool isPublic = methodAccess == MethodAttributes.Public;
                                bool isStatic = (methodAttributes & MethodAttributes.Static) != 0;
                                BindingFlags bindingFlags = FilterPreCalculate(isPublic, isInherited, isStatic);
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

                private RuntimeConstructorInfo[] PopulateConstructors(Filter filter)
                {
                    if (ReflectedType.IsGenericParameter)
                    {
                        return Array.Empty<RuntimeConstructorInfo>();
                    }

                    ListBuilder<RuntimeConstructorInfo> list = default;

                    RuntimeType declaringType = ReflectedType;

                    foreach (RuntimeMethodHandleInternal methodHandle in RuntimeTypeHandle.GetIntroducedMethods(declaringType))
                    {
                        if (filter.RequiresStringComparison())
                        {
                            if (!RuntimeMethodHandle.MatchesNameHash(methodHandle, filter.GetHashToMatch()))
                            {
                                Debug.Assert(!filter.Match(RuntimeMethodHandle.GetUtf8Name(methodHandle)));
                                continue;
                            }

                            if (!filter.Match(RuntimeMethodHandle.GetUtf8Name(methodHandle)))
                                continue;
                        }

                        MethodAttributes methodAttributes = RuntimeMethodHandle.GetAttributes(methodHandle);

                        Debug.Assert(!methodHandle.IsNullHandle());

                        if ((methodAttributes & MethodAttributes.RTSpecialName) == 0)
                            continue;

                        // Constructors should not be virtual or abstract
                        Debug.Assert(
                            (methodAttributes & MethodAttributes.Abstract) == 0 &&
                            (methodAttributes & MethodAttributes.Virtual) == 0);

                        #region Calculate Binding Flags
                        bool isPublic = (methodAttributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;
                        bool isStatic = (methodAttributes & MethodAttributes.Static) != 0;
                        BindingFlags bindingFlags = FilterPreCalculate(isPublic, isInherited: false, isStatic);
                        #endregion

                        // get the unboxing stub or instantiating stub if needed
                        RuntimeMethodHandleInternal instantiatedHandle = RuntimeMethodHandle.GetStubIfNeeded(methodHandle, declaringType, null);

                        RuntimeConstructorInfo runtimeConstructorInfo =
                        new RuntimeConstructorInfo(instantiatedHandle, ReflectedType, m_runtimeTypeCache, methodAttributes, bindingFlags);

                        list.Add(runtimeConstructorInfo);
                    }

                    return list.ToArray();
                }

                [UnconditionalSuppressMessage ("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
                    Justification = "Calls to GetInterfaces technically require all interfaces on ReflectedType" +
                        "But this is not a public API to enumerate reflection items, all the public APIs which do that" +
                        "should be annotated accordingly.")]
                private RuntimeFieldInfo[] PopulateFields(Filter filter)
                {
                    ListBuilder<RuntimeFieldInfo> list = default;

                    RuntimeType declaringType = ReflectedType;

                    #region Populate all static, instance and literal fields
                    while (RuntimeTypeHandle.IsGenericVariable(declaringType))
                        declaringType = declaringType.GetBaseType()!;

                    while (declaringType != null)
                    {
                        PopulateRtFields(filter, declaringType, ref list);

                        PopulateLiteralFields(filter, declaringType, ref list);

                        declaringType = RuntimeTypeHandle.GetBaseType(declaringType);
                    }
                    #endregion

                    #region Populate Literal Fields on Interfaces
                    if (ReflectedType.IsGenericParameter)
                    {
                        Type[] interfaces = ReflectedType.BaseType!.GetInterfaces();

                        for (int i = 0; i < interfaces.Length; i++)
                        {
                            // Populate literal fields defined on any of the interfaces implemented by the declaring type
                            PopulateLiteralFields(filter, (RuntimeType)interfaces[i], ref list);
                            PopulateRtFields(filter, (RuntimeType)interfaces[i], ref list);
                        }
                    }
                    else
                    {
                        Type[]? interfaces = RuntimeTypeHandle.GetInterfaces(ReflectedType);

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

                private unsafe void PopulateRtFields(Filter filter, RuntimeType declaringType, ref ListBuilder<RuntimeFieldInfo> list)
                {
                    IntPtr* pResult = stackalloc IntPtr[64];
                    int count = 64;

                    if (!RuntimeTypeHandle.GetFields(declaringType, pResult, &count))
                    {
                        fixed (IntPtr* pBigResult = new IntPtr[count])
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

                private unsafe void PopulateRtFields(Filter filter,
                    IntPtr* ppFieldHandles, int count, RuntimeType declaringType, ref ListBuilder<RuntimeFieldInfo> list)
                {
                    Debug.Assert(declaringType != null);
                    Debug.Assert(ReflectedType != null);

                    bool needsStaticFieldForGeneric = RuntimeTypeHandle.HasInstantiation(declaringType) && !RuntimeTypeHandle.ContainsGenericVariables(declaringType);
                    bool isInherited = declaringType != ReflectedType;

                    for (int i = 0; i < count; i++)
                    {
                        RuntimeFieldHandleInternal runtimeFieldHandle = new RuntimeFieldHandleInternal(ppFieldHandles[i]);

                        if (filter.RequiresStringComparison())
                        {
                            if (!RuntimeFieldHandle.MatchesNameHash(runtimeFieldHandle, filter.GetHashToMatch()))
                            {
                                Debug.Assert(!filter.Match(RuntimeFieldHandle.GetUtf8Name(runtimeFieldHandle)));
                                continue;
                            }

                            if (!filter.Match(RuntimeFieldHandle.GetUtf8Name(runtimeFieldHandle)))
                                continue;
                        }

                        Debug.Assert(!runtimeFieldHandle.IsNullHandle());

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
                        BindingFlags bindingFlags = FilterPreCalculate(isPublic, isInherited, isStatic);
                        #endregion

                        // correct the FieldDesc if needed
                        if (needsStaticFieldForGeneric && isStatic)
                            runtimeFieldHandle = RuntimeFieldHandle.GetStaticFieldForGenericType(runtimeFieldHandle, declaringType);

                        RuntimeFieldInfo runtimeFieldInfo =
                            new RtFieldInfo(runtimeFieldHandle, declaringType, m_runtimeTypeCache, bindingFlags);

                        list.Add(runtimeFieldInfo);
                    }
                }

                private void PopulateLiteralFields(Filter filter, RuntimeType declaringType, ref ListBuilder<RuntimeFieldInfo> list)
                {
                    Debug.Assert(declaringType != null);
                    Debug.Assert(ReflectedType != null);

                    int tkDeclaringType = RuntimeTypeHandle.GetToken(declaringType);

                    // Our policy is that TypeDescs do not have metadata tokens
                    if (MdToken.IsNullToken(tkDeclaringType))
                        return;

                    MetadataImport scope = RuntimeTypeHandle.GetMetadataImport(declaringType);

                    scope.EnumFields(tkDeclaringType, out MetadataEnumResult tkFields);

                    for (int i = 0; i < tkFields.Length; i++)
                    {
                        int tkField = tkFields[i];
                        Debug.Assert(MdToken.IsTokenOfType(tkField, MetadataTokenType.FieldDef));
                        Debug.Assert(!MdToken.IsNullToken(tkField));

                        scope.GetFieldDefProps(tkField, out FieldAttributes fieldAttributes);

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
                                MdUtf8String name = scope.GetName(tkField);

                                if (!filter.Match(name))
                                    continue;
                            }

                            #region Calculate Binding Flags
                            bool isPublic = fieldAccess == FieldAttributes.Public;
                            bool isStatic = (fieldAttributes & FieldAttributes.Static) != 0;
                            BindingFlags bindingFlags = FilterPreCalculate(isPublic, isInherited, isStatic);
                            #endregion

                            RuntimeFieldInfo runtimeFieldInfo =
                            new MdFieldInfo(tkField, fieldAttributes, declaringType.GetTypeHandleInternal(), m_runtimeTypeCache, bindingFlags);

                            list.Add(runtimeFieldInfo);
                        }
                    }
                }

                private void AddSpecialInterface(
                    ref ListBuilder<RuntimeType> list,
                    Filter filter,
                    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] RuntimeType iList,
                    bool addSubInterface)
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

                [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2065:UnrecognizedReflectionPattern",
                    Justification = "Calls to GetInterfaces technically require all interfaces on ReflectedType" +
                        "But this is not a public API to enumerate reflection items, all the public APIs which do that" +
                        "should be annotated accordingly.")]
                private RuntimeType[] PopulateInterfaces(Filter filter)
                {
                    ListBuilder<RuntimeType> list = default;

                    RuntimeType declaringType = ReflectedType;

                    if (!RuntimeTypeHandle.IsGenericVariable(declaringType))
                    {
                        Type[]? ifaces = RuntimeTypeHandle.GetInterfaces(declaringType);

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

                                Debug.Assert(interfaceType.IsInterface);
                                list.Add(interfaceType);
                            }
                        }

                        if (ReflectedType.IsSZArray)
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
                        var al = new HashSet<RuntimeType>();

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
                                al.Add((RuntimeType)temp[j]);
                        }

                        // Populate list, without duplicates
                        foreach (RuntimeType rt in al)
                        {
                            if (!filter.RequiresStringComparison() || filter.Match(RuntimeTypeHandle.GetUtf8Name(rt)))
                            {
                                list.Add(rt);
                            }
                        }
                    }

                    return list.ToArray();
                }

                [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                    Justification = "Calls to ResolveTypeHandle technically require all types to be kept " +
                        "But this is not a public API to enumerate reflection items, all the public APIs which do that " +
                        "should be annotated accordingly.")]
                private RuntimeType[] PopulateNestedClasses(Filter filter)
                {
                    RuntimeType declaringType = ReflectedType;

                    while (RuntimeTypeHandle.IsGenericVariable(declaringType))
                    {
                        declaringType = declaringType.GetBaseType()!;
                    }

                    int tkEnclosingType = RuntimeTypeHandle.GetToken(declaringType);

                    // For example, TypeDescs do not have metadata tokens
                    if (MdToken.IsNullToken(tkEnclosingType))
                        return Array.Empty<RuntimeType>();

                    ListBuilder<RuntimeType> list = default;

                    ModuleHandle moduleHandle = new ModuleHandle(RuntimeTypeHandle.GetModule(declaringType));
                    MetadataImport scope = ModuleHandle.GetMetadataImport(moduleHandle.GetRuntimeModule());

                    scope.EnumNestedTypes(tkEnclosingType, out MetadataEnumResult tkNestedClasses);

                    for (int i = 0; i < tkNestedClasses.Length; i++)
                    {
                        RuntimeType nestedType;

                        try
                        {
                            nestedType = moduleHandle.ResolveTypeHandle(tkNestedClasses[i]).GetRuntimeType();
                        }
                        catch (System.TypeLoadException)
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

                private RuntimeEventInfo[] PopulateEvents(Filter filter)
                {
                    Debug.Assert(ReflectedType != null);

                    // Do not create the dictionary if we are filtering the properties by name already
                    Dictionary<string, RuntimeEventInfo>? csEventInfos = filter.CaseSensitive() ? null :
                        new Dictionary<string, RuntimeEventInfo>();

                    RuntimeType declaringType = ReflectedType;
                    ListBuilder<RuntimeEventInfo> list = default;

                    if (!RuntimeTypeHandle.IsInterface(declaringType))
                    {
                        while (RuntimeTypeHandle.IsGenericVariable(declaringType))
                            declaringType = declaringType.GetBaseType()!;

                        // Populate associates off of the class hierarchy
                        while (declaringType != null)
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

                private void PopulateEvents(
                    Filter filter, RuntimeType declaringType, Dictionary<string, RuntimeEventInfo>? csEventInfos, ref ListBuilder<RuntimeEventInfo> list)
                {
                    int tkDeclaringType = RuntimeTypeHandle.GetToken(declaringType);

                    // Arrays, Pointers, ByRef types and others generated only the fly by the RT do not have tokens.
                    if (MdToken.IsNullToken(tkDeclaringType))
                        return;

                    MetadataImport scope = RuntimeTypeHandle.GetMetadataImport(declaringType);

                    scope.EnumEvents(tkDeclaringType, out MetadataEnumResult tkEvents);

                    for (int i = 0; i < tkEvents.Length; i++)
                    {
                        int tkEvent = tkEvents[i];

                        Debug.Assert(!MdToken.IsNullToken(tkEvent));
                        Debug.Assert(MdToken.IsTokenOfType(tkEvent, MetadataTokenType.Event));

                        if (filter.RequiresStringComparison())
                        {
                            MdUtf8String name = scope.GetName(tkEvent);

                            if (!filter.Match(name))
                                continue;
                        }

                        RuntimeEventInfo eventInfo = new RuntimeEventInfo(
                            tkEvent, declaringType, m_runtimeTypeCache, out bool isPrivate);

                        #region Remove Inherited Privates
                        if (declaringType != m_runtimeTypeCache.GetRuntimeType() && isPrivate)
                            continue;
                        #endregion

                        #region Remove Duplicates
                        if (csEventInfos != null)
                        {
                            string name = eventInfo.Name;

                            if (csEventInfos.ContainsKey(name))
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

                private RuntimePropertyInfo[] PopulateProperties(Filter filter)
                {
                    Debug.Assert(ReflectedType != null);

                    // m_csMemberInfos can be null at this point. It will be initialized when Insert
                    // is called in Populate after this returns.

                    RuntimeType declaringType = ReflectedType;
                    Debug.Assert(declaringType != null);

                    ListBuilder<RuntimePropertyInfo> list = default;

                    if (!RuntimeTypeHandle.IsInterface(declaringType))
                    {
                        while (RuntimeTypeHandle.IsGenericVariable(declaringType))
                            declaringType = declaringType.GetBaseType()!;

                        // Do not create the dictionary if we are filtering the properties by name already
                        Dictionary<string, List<RuntimePropertyInfo>>? csPropertyInfos = filter.CaseSensitive() ? null :
                            new Dictionary<string, List<RuntimePropertyInfo>>();

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

                private void PopulateProperties(
                    Filter filter,
                    RuntimeType declaringType,
                    Dictionary<string, List<RuntimePropertyInfo>>? csPropertyInfos,
                    bool[]? usedSlots,
                    ref ListBuilder<RuntimePropertyInfo> list)
                {
                    int tkDeclaringType = RuntimeTypeHandle.GetToken(declaringType);

                    // Arrays, Pointers, ByRef types and others generated only the fly by the RT do not have tokens.
                    if (MdToken.IsNullToken(tkDeclaringType))
                        return;

                    MetadataImport scope = RuntimeTypeHandle.GetMetadataImport(declaringType);

                    scope.EnumProperties(tkDeclaringType, out MetadataEnumResult tkProperties);

                    RuntimeModule declaringModuleHandle = RuntimeTypeHandle.GetModule(declaringType);

                    int numVirtuals = RuntimeTypeHandle.GetNumVirtuals(declaringType);

                    Debug.Assert((declaringType.IsInterface && usedSlots == null && csPropertyInfos == null) ||
                                    (!declaringType.IsInterface && usedSlots != null && usedSlots.Length >= numVirtuals));

                    for (int i = 0; i < tkProperties.Length; i++)
                    {
                        int tkProperty = tkProperties[i];

                        Debug.Assert(!MdToken.IsNullToken(tkProperty));
                        Debug.Assert(MdToken.IsTokenOfType(tkProperty, MetadataTokenType.Property));

                        if (filter.RequiresStringComparison())
                        {
                            if (!ModuleHandle.ContainsPropertyMatchingHash(declaringModuleHandle, tkProperty, filter.GetHashToMatch()))
                            {
                                Debug.Assert(!filter.Match(declaringType.GetRuntimeModule().MetadataImport.GetName(tkProperty)));
                                continue;
                            }

                            MdUtf8String name = declaringType.GetRuntimeModule().MetadataImport.GetName(tkProperty);

                            if (!filter.Match(name))
                                continue;
                        }

                        RuntimePropertyInfo propertyInfo =
                            new RuntimePropertyInfo(
                            tkProperty, declaringType, m_runtimeTypeCache, out bool isPrivate);

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
                            //
                            // A property on a base type is "overriden" by a property on a sub type
                            // if the getter/setter of the latter occupies the same vtable slot as
                            // the getter/setter of the former.
                            //
                            // We only need to examine the setter if a getter doesn't exist.
                            // It is not logical for the getter to be virtual but not the setter.

                            MethodInfo? associateMethod = propertyInfo.GetGetMethod() ?? propertyInfo.GetSetMethod();
                            if (associateMethod != null)
                            {
                                int slot = RuntimeMethodHandle.GetSlot((RuntimeMethodInfo)associateMethod);

                                if (slot < numVirtuals)
                                {
                                    Debug.Assert(associateMethod.IsVirtual);
                                    if (usedSlots[slot])
                                        continue;

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
                                if (!csPropertyInfos.TryGetValue(name, out List<RuntimePropertyInfo>? cache))
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
                                    if (propertyInfo.EqualsSig(list[j]!))
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
                internal T[] GetMemberList(MemberListType listType, string? name, CacheType cacheType)
                {
                    // name can be null only when listType falls into default case
                    switch (listType)
                    {
                        case MemberListType.CaseSensitive:
                            return m_csMemberInfos[name!] ?? Populate(name, listType, cacheType);

                        case MemberListType.CaseInsensitive:
                            return m_cisMemberInfos[name!] ?? Populate(name, listType, cacheType);

                        default:
                            Debug.Assert(listType == MemberListType.All);
                            if (Volatile.Read(ref m_cacheComplete))
                                return m_allMembers!;

                            return Populate(null, listType, cacheType);
                    }
                }

                internal RuntimeType ReflectedType => m_runtimeTypeCache.GetRuntimeType();

                #endregion
            }
            #endregion

            #region Private Data Members
            private readonly RuntimeType m_runtimeType;
            private RuntimeType? m_enclosingType;
            private TypeCode m_typeCode;
            private string? m_name;
            private string? m_fullname;
            private string? m_toString;
            private string? m_namespace;
            private readonly bool m_isGlobal;
            private bool m_bIsDomainInitialized;
            private MemberInfoCache<RuntimeMethodInfo>? m_methodInfoCache;
            private MemberInfoCache<RuntimeConstructorInfo>? m_constructorInfoCache;
            private MemberInfoCache<RuntimeFieldInfo>? m_fieldInfoCache;
            private MemberInfoCache<RuntimeType>? m_interfaceCache;
            private MemberInfoCache<RuntimeType>? m_nestedClassesCache;
            private MemberInfoCache<RuntimePropertyInfo>? m_propertyInfoCache;
            private MemberInfoCache<RuntimeEventInfo>? m_eventInfoCache;
            private static CerHashtable<RuntimeMethodInfo, RuntimeMethodInfo> s_methodInstantiations;
            private static object? s_methodInstantiationsLock;
            private string? m_defaultMemberName;
            private object? m_genericCache; // Generic cache for rare scenario specific data. It is used to cache Enum names and values.
            private object[]? _emptyArray; // Object array cache for Attribute.GetCustomAttributes() pathological no-result case.
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
            private string ConstructName([NotNull] ref string? name, TypeNameFormatFlags formatFlags) =>
                name ??= new RuntimeTypeHandle(m_runtimeType).ConstructName(formatFlags);

            private T[] GetMemberList<T>(ref MemberInfoCache<T>? m_cache, MemberListType listType, string? name, CacheType cacheType)
                where T : MemberInfo
            {
                MemberInfoCache<T> existingCache = GetMemberCache<T>(ref m_cache);
                return existingCache.GetMemberList(listType, name, cacheType);
            }

            private MemberInfoCache<T> GetMemberCache<T>(ref MemberInfoCache<T>? m_cache)
                where T : MemberInfo
            {
                MemberInfoCache<T>? existingCache = m_cache;

                if (existingCache == null)
                {
                    MemberInfoCache<T> newCache = new MemberInfoCache<T>(this);
                    existingCache = Interlocked.CompareExchange(ref m_cache, newCache, null);
                    existingCache ??= newCache;
                }

                return existingCache;
            }
            #endregion

            #region Internal Members

            internal object? GenericCache
            {
                get => m_genericCache;
                set => m_genericCache = value;
            }

            internal bool DomainInitialized
            {
                get => m_bIsDomainInitialized;
                set => m_bIsDomainInitialized = value;
            }

            internal string? GetName(TypeNameKind kind)
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

                    default:
                        throw new InvalidOperationException();
                }
            }

            internal string GetNameSpace()
            {
                // @Optimization - Use ConstructName to populate m_namespace
                if (m_namespace == null)
                {
                    Type type = m_runtimeType;
                    type = type.GetRootElementType();

                    while (type.IsNested)
                        type = type.DeclaringType!;

                    m_namespace = RuntimeTypeHandle.GetMetadataImport((RuntimeType)type).GetNamespace(type.MetadataToken).ToString();
                }

                return m_namespace;
            }

            internal TypeCode TypeCode
            {
                get => m_typeCode;
                set => m_typeCode = value;
            }

            internal RuntimeType? GetEnclosingType()
            {
                if (m_enclosingType == null)
                {
                    // Use void as a marker of null enclosing type
                    RuntimeType enclosingType = RuntimeTypeHandle.GetDeclaringType(GetRuntimeType());
                    Debug.Assert(enclosingType != typeof(void));
                    m_enclosingType = enclosingType ?? (RuntimeType)typeof(void);
                }

                return (m_enclosingType == typeof(void)) ? null : m_enclosingType;
            }

            internal RuntimeType GetRuntimeType() => m_runtimeType;

            internal bool IsGlobal => m_isGlobal;

            internal void InvalidateCachedNestedType() => m_nestedClassesCache = null;

            internal string? GetDefaultMemberName()
            {
                if (m_defaultMemberName == null)
                {
                    CustomAttributeData? attr = null;
                    Type DefaultMemberAttrType = typeof(DefaultMemberAttribute);
                    for (RuntimeType? t = m_runtimeType; t != null; t = t.GetBaseType())
                    {
                        IList<CustomAttributeData> attrs = CustomAttributeData.GetCustomAttributes(t);
                        for (int i = 0; i < attrs.Count; i++)
                        {
                            if (ReferenceEquals(attrs[i].Constructor.DeclaringType, DefaultMemberAttrType))
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

            internal object[] GetEmptyArray() => _emptyArray ??= (object[])Array.CreateInstance(m_runtimeType, 0);
            #endregion

            #region Caches Accessors
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
                    Interlocked.CompareExchange(ref s_methodInstantiationsLock!, new object(), null);

                bool lockTaken = false;
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

            internal RuntimeMethodInfo[] GetMethodList(MemberListType listType, string? name)
            {
                return GetMemberList<RuntimeMethodInfo>(ref m_methodInfoCache, listType, name, CacheType.Method);
            }

            internal RuntimeConstructorInfo[] GetConstructorList(MemberListType listType, string? name)
            {
                return GetMemberList<RuntimeConstructorInfo>(ref m_constructorInfoCache, listType, name, CacheType.Constructor);
            }

            internal RuntimePropertyInfo[] GetPropertyList(MemberListType listType, string? name)
            {
                return GetMemberList<RuntimePropertyInfo>(ref m_propertyInfoCache, listType, name, CacheType.Property);
            }

            internal RuntimeEventInfo[] GetEventList(MemberListType listType, string? name)
            {
                return GetMemberList<RuntimeEventInfo>(ref m_eventInfoCache, listType, name, CacheType.Event);
            }

            internal RuntimeFieldInfo[] GetFieldList(MemberListType listType, string? name)
            {
                return GetMemberList<RuntimeFieldInfo>(ref m_fieldInfoCache, listType, name, CacheType.Field);
            }

            internal RuntimeType[] GetInterfaceList(MemberListType listType, string? name)
            {
                return GetMemberList<RuntimeType>(ref m_interfaceCache, listType, name, CacheType.Interface);
            }

            internal RuntimeType[] GetNestedTypeList(MemberListType listType, string? name)
            {
                return GetMemberList<RuntimeType>(ref m_nestedClassesCache, listType, name, CacheType.NestedType);
            }

            internal MethodBase GetMethod(RuntimeType declaringType, RuntimeMethodHandleInternal method)
            {
                GetMemberCache<RuntimeMethodInfo>(ref m_methodInfoCache);
                return m_methodInfoCache!.AddMethod(declaringType, method, CacheType.Method);
            }

            internal MethodBase GetConstructor(RuntimeType declaringType, RuntimeMethodHandleInternal constructor)
            {
                GetMemberCache<RuntimeConstructorInfo>(ref m_constructorInfoCache);
                return m_constructorInfoCache!.AddMethod(declaringType, constructor, CacheType.Constructor);
            }

            internal FieldInfo GetField(RuntimeFieldHandleInternal field)
            {
                GetMemberCache<RuntimeFieldInfo>(ref m_fieldInfoCache);
                return m_fieldInfoCache!.AddField(field);
            }

            #endregion
        }
        #endregion

        #region Static Members

        #region Internal
        internal static RuntimeType? GetType(string typeName, bool throwOnError, bool ignoreCase,
            ref StackCrawlMark stackMark)
        {
            if (typeName == null)
                throw new ArgumentNullException(nameof(typeName));

            return RuntimeTypeHandle.GetTypeByName(
                typeName, throwOnError, ignoreCase, ref stackMark);
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        internal static MethodBase? GetMethodBase(RuntimeModule scope, int typeMetadataToken)
        {
            return GetMethodBase(new ModuleHandle(scope).ResolveMethodHandle(typeMetadataToken).GetMethodInfo());
        }

        internal static MethodBase? GetMethodBase(IRuntimeMethodInfo methodHandle)
        {
            return GetMethodBase(null, methodHandle);
        }

        internal static MethodBase? GetMethodBase(RuntimeType? reflectedType, IRuntimeMethodInfo methodHandle)
        {
            MethodBase? retval = GetMethodBase(reflectedType, methodHandle.Value);
            GC.KeepAlive(methodHandle);
            return retval;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "The code in this method looks up the method by name, but it always starts with a method handle." +
                            "To get here something somwhere had to get the method handle and thus the method must exist.")]
        internal static MethodBase? GetMethodBase(RuntimeType? reflectedType, RuntimeMethodHandleInternal methodHandle)
        {
            Debug.Assert(!methodHandle.IsNullHandle());

            if (RuntimeMethodHandle.IsDynamicMethod(methodHandle))
            {
                Resolver resolver = RuntimeMethodHandle.GetResolver(methodHandle);

                if (resolver != null)
                    return resolver.GetDynamicMethod();

                return null;
            }

            // verify the type/method relationship
            RuntimeType declaredType = RuntimeMethodHandle.GetDeclaringType(methodHandle);

            RuntimeType[]? methodInstantiation = null;

            reflectedType ??= declaredType;

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
                    MethodBase[] methodBases = (reflectedType.GetMember(
                        RuntimeMethodHandle.GetName(methodHandle), MemberTypes.Constructor | MemberTypes.Method,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) as MethodBase[])!;

                    bool loaderAssuredCompatible = false;
                    for (int i = 0; i < methodBases.Length; i++)
                    {
                        IRuntimeMethodInfo rmi = (IRuntimeMethodInfo)methodBases[i];
                        if (rmi.Value.Value == methodHandle.Value)
                            loaderAssuredCompatible = true;
                    }

                    if (!loaderAssuredCompatible)
                        throw new ArgumentException(SR.Format(
                            SR.Argument_ResolveMethodHandle,
                            reflectedType, declaredType));
                }
                // Action<in string> is assignable from, but not a subclass of Action<in object>.
                else if (declaredType.IsGenericType)
                {
                    // ignoring instantiation is the ReflectedType a subtype of the DeclaringType
                    RuntimeType declaringDefinition = (RuntimeType)declaredType.GetGenericTypeDefinition();

                    RuntimeType? baseType = reflectedType;

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
                        throw new ArgumentException(SR.Format(
                            SR.Argument_ResolveMethodHandle,
                            reflectedType, declaredType));
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
                    throw new ArgumentException(string.Format(
                        CultureInfo.CurrentCulture, SR.Argument_ResolveMethodHandle,
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

        internal object? GenericCache
        {
            get => CacheIfExists?.GenericCache;
            set => Cache.GenericCache = value;
        }

        internal bool DomainInitialized
        {
            get => Cache.DomainInitialized;
            set => Cache.DomainInitialized = value;
        }

        internal static FieldInfo GetFieldInfo(IRuntimeFieldInfo fieldHandle)
        {
            return GetFieldInfo(RuntimeFieldHandle.GetApproxDeclaringType(fieldHandle), fieldHandle);
        }

        internal static FieldInfo GetFieldInfo(RuntimeType? reflectedType, IRuntimeFieldInfo field)
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
                        throw new ArgumentException(SR.Format(
                            SR.Argument_ResolveFieldHandle,
                            reflectedType,
                            declaredType));
                    }
                }
            }

            FieldInfo retVal = reflectedType.Cache.GetField(fieldHandle);
            GC.KeepAlive(field);
            return retVal;
        }

        // Called internally
        private static PropertyInfo GetPropertyInfo(RuntimeType reflectedType, int tkProperty)
        {
            RuntimePropertyInfo property;
            RuntimePropertyInfo[] candidates =
                reflectedType.Cache.GetPropertyList(MemberListType.All, null);

            for (int i = 0; i < candidates.Length; i++)
            {
                property = candidates[i];
                if (property.MetadataToken == tkProperty)
                    return property;
            }

            Debug.Fail("Unreachable code");
            throw new SystemException();
        }

        internal static void ValidateGenericArguments(MemberInfo definition, RuntimeType[] genericArguments, Exception? e)
        {
            RuntimeType[]? typeContext = null;
            RuntimeType[]? methodContext = null;
            RuntimeType[] genericParameters;

            if (definition is Type)
            {
                RuntimeType genericTypeDefinition = (RuntimeType)definition;
                genericParameters = genericTypeDefinition.GetGenericArgumentsInternal();
                typeContext = genericArguments;
            }
            else
            {
                RuntimeMethodInfo genericMethodDefinition = (RuntimeMethodInfo)definition;
                genericParameters = genericMethodDefinition.GetGenericArgumentsInternal();
                methodContext = genericArguments;

                RuntimeType? declaringType = (RuntimeType?)genericMethodDefinition.DeclaringType;
                if (declaringType != null)
                {
                    typeContext = declaringType.GetTypeHandleInternal().GetInstantiationInternal();
                }
            }

            for (int i = 0; i < genericArguments.Length; i++)
            {
                Type genericArgument = genericArguments[i];
                Type genericParameter = genericParameters[i];

                if (!RuntimeTypeHandle.SatisfiesConstraints(genericParameter.GetTypeHandleInternal().GetTypeChecked(),
                    typeContext, methodContext, genericArgument.GetTypeHandleInternal().GetTypeChecked()))
                {
                    throw new ArgumentException(
                        SR.Format(SR.Argument_GenConstraintViolation, i.ToString(), genericArgument, definition, genericParameter), e);
                }
            }
        }

        private static void SplitName(string? fullname, out string? name, out string? ns)
        {
            name = null;
            ns = null;

            if (fullname == null)
                return;

            // Get namespace
            int nsDelimiter = fullname.LastIndexOf(".", StringComparison.Ordinal);
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

                if (allowPrefixLookup && name.EndsWith("*", StringComparison.Ordinal))
                {
                    // We set prefixLookup to true if name ends with a "*".
                    // We will also set listType to All so that all members are included in
                    // the candidates which are later filtered by FilterApplyPrefixLookup.
                    name = name[0..^1];
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
        private static void FilterHelper(BindingFlags bindingFlags, ref string name, out bool ignoreCase, out MemberListType listType) =>
            FilterHelper(bindingFlags, ref name!, false, out _, out ignoreCase, out listType);

        // Only called by GetXXXCandidates, GetInterfaces, and GetNestedTypes when FilterHelper has set "prefixLookup" to true.
        // Most of the plural GetXXX methods allow prefix lookups while the singular GetXXX methods mostly do not.
        private static bool FilterApplyPrefixLookup(MemberInfo memberInfo, string name, bool ignoreCase)
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
            string name, bool prefixLookup)
        {
            Debug.Assert(memberInfo != null);
            Debug.Assert(name is null || (bindingFlags & BindingFlags.IgnoreCase) == 0 || (name.ToLowerInvariant().Equals(name)));

            // Filter by Public & Private
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

            bool isInherited = !ReferenceEquals(memberInfo.DeclaringType, memberInfo.ReflectedType);

            // Filter by DeclaredOnly
            if ((bindingFlags & BindingFlags.DeclaredOnly) != 0 && isInherited)
                return false;

            // Filter by Static & Instance
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

            // Filter by name wrt prefixLookup and implicitly by case sensitivity
            if (prefixLookup)
            {
                if (!FilterApplyPrefixLookup(memberInfo, name!, (bindingFlags & BindingFlags.IgnoreCase) != 0))
                    return false;
            }

            // @Asymmetry - Internal, inherited, instance, non-protected, non-virtual, non-abstract members returned
            //              iff BindingFlags !DeclaredOnly, Instance and Public are present except for fields
            if (((bindingFlags & BindingFlags.DeclaredOnly) == 0) &&        // DeclaredOnly not present
                 isInherited &&                                            // Is inherited Member

                isNonProtectedInternal &&                                 // Is non-protected internal member
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

            return true;
        }

        // Used by GetInterface and GetNestedType(s) which don't need parameter type filtering.
        private static bool FilterApplyType(
            Type type, BindingFlags bindingFlags, string name, bool prefixLookup, string? ns)
        {
            Debug.Assert(type is not null);
            Debug.Assert(type is RuntimeType);

            bool isPublic = type.IsNestedPublic || type.IsPublic;

            if (!FilterApplyBase(type, bindingFlags, isPublic, type.IsNestedAssembly, isStatic: false, name, prefixLookup))
                return false;

            if (ns != null && ns != type.Namespace)
                return false;

            return true;
        }

        private static bool FilterApplyMethodInfo(
            RuntimeMethodInfo method, BindingFlags bindingFlags, CallingConventions callConv, Type[]? argumentTypes)
        {
            // Optimization: Pre-Calculate the method binding flags to avoid casting.
            return FilterApplyMethodBase(method, method.BindingFlags, bindingFlags, callConv, argumentTypes);
        }

        private static bool FilterApplyConstructorInfo(
            RuntimeConstructorInfo constructor, BindingFlags bindingFlags, CallingConventions callConv, Type[]? argumentTypes)
        {
            // Optimization: Pre-Calculate the method binding flags to avoid casting.
            return FilterApplyMethodBase(constructor, constructor.BindingFlags, bindingFlags, callConv, argumentTypes);
        }

        // Used by GetMethodCandidates/GetConstructorCandidates, InvokeMember, and CreateInstanceImpl to perform the necessary filtering.
        // Should only be called by FilterApplyMethodInfo and FilterApplyConstructorInfo.
        private static bool FilterApplyMethodBase(
            MethodBase methodBase, BindingFlags methodFlags, BindingFlags bindingFlags, CallingConventions callConv, Type[]? argumentTypes)
        {
            Debug.Assert(methodBase != null);

            bindingFlags ^= BindingFlags.DeclaredOnly;

            // Apply Base Filter
            if ((bindingFlags & methodFlags) != methodFlags)
                return false;

            // Check CallingConvention
            if ((callConv & CallingConventions.Any) == 0)
            {
                if ((callConv & CallingConventions.VarArgs) != 0 &&
                    (methodBase.CallingConvention & CallingConventions.VarArgs) == 0)
                    return false;

                if ((callConv & CallingConventions.Standard) != 0 &&
                    (methodBase.CallingConvention & CallingConventions.Standard) == 0)
                    return false;
            }

            // Check if argumentTypes supplied
            if (argumentTypes != null)
            {
                ParameterInfo[] parameterInfos = methodBase.GetParametersNoCopy();

                if (argumentTypes.Length != parameterInfos.Length)
                {
                    // If the number of supplied arguments differs than the number in the signature AND
                    // we are not filtering for a dynamic call -- InvokeMethod or CreateInstance -- filter out the method.
                    if ((bindingFlags &
                        (BindingFlags.InvokeMethod | BindingFlags.CreateInstance | BindingFlags.GetProperty | BindingFlags.SetProperty)) == 0)
                        return false;

                    bool testForParamArray = false;
                    bool excessSuppliedArguments = argumentTypes.Length > parameterInfos.Length;

                    if (excessSuppliedArguments)
                    {
                        // There are more supplied arguments than parameters: the method could be varargs
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
                    }
                    else
                    {
                        // There are fewer supplied arguments than parameters: the missing arguments could be optional
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
                    }

                    // ParamArray
                    if (testForParamArray)
                    {
                        if (parameterInfos.Length == 0)
                            return false;

                        // The last argument of the signature could be a param array.
                        bool shortByMoreThanOneSuppliedArgument = argumentTypes.Length < parameterInfos.Length - 1;

                        if (shortByMoreThanOneSuppliedArgument)
                            return false;

                        ParameterInfo lastParameter = parameterInfos[^1];

                        if (!lastParameter.ParameterType.IsArray)
                            return false;

                        if (!lastParameter.IsDefined(typeof(ParamArrayAttribute), false))
                            return false;
                    }
                }
                else
                {
                    // Exact Binding
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
                                if (argumentTypes[i] is Type t && !t.MatchesParameterTypeExactly(parameterInfos[i]))
                                    return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        #endregion

        #endregion

        #region Private Data Members

#pragma warning disable CA1823
        private readonly object m_keepalive; // This will be filled with a LoaderAllocator reference when this RuntimeType represents a collectible type
#pragma warning restore CA1823
        private IntPtr m_cache;
        internal IntPtr m_handle;

        internal static readonly RuntimeType ValueType = (RuntimeType)typeof(System.ValueType);

        private static readonly RuntimeType ObjectType = (RuntimeType)typeof(object);
        private static readonly RuntimeType StringType = (RuntimeType)typeof(string);
        #endregion

        #region Constructor

        internal RuntimeType() { throw new NotSupportedException(); }

        #endregion

        #region Private\Internal Members

        internal IntPtr GetUnderlyingNativeHandle()
        {
            return m_handle;
        }

        internal override bool CacheEquals(object? o)
        {
            return (o is RuntimeType t) && (t.m_handle == m_handle);
        }

        private RuntimeTypeCache? CacheIfExists
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (m_cache != IntPtr.Zero)
                {
                    object? cache = GCHandle.InternalGet(m_cache);
                    Debug.Assert(cache == null || cache is RuntimeTypeCache);
                    return Unsafe.As<RuntimeTypeCache>(cache);
                }
                return null;
            }
        }

        private RuntimeTypeCache Cache
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (m_cache != IntPtr.Zero)
                {
                    object? cache = GCHandle.InternalGet(m_cache);
                    if (cache != null)
                    {
                        Debug.Assert(cache is RuntimeTypeCache);
                        return Unsafe.As<RuntimeTypeCache>(cache);
                    }
                }
                return InitializeCache();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private RuntimeTypeCache InitializeCache()
        {
            if (m_cache == IntPtr.Zero)
            {
                RuntimeTypeHandle th = new RuntimeTypeHandle(this);
                IntPtr newgcHandle = th.GetGCHandle(GCHandleType.WeakTrackResurrection);
                IntPtr gcHandle = Interlocked.CompareExchange(ref m_cache, newgcHandle, IntPtr.Zero);
                if (gcHandle != IntPtr.Zero)
                    th.FreeGCHandle(newgcHandle);
            }

            RuntimeTypeCache? cache = (RuntimeTypeCache?)GCHandle.InternalGet(m_cache);
            if (cache == null)
            {
                cache = new RuntimeTypeCache(this);
                RuntimeTypeCache? existingCache = (RuntimeTypeCache?)GCHandle.InternalCompareExchange(m_cache, cache, null);
                if (existingCache != null)
                    cache = existingCache;
            }

            Debug.Assert(cache != null);
            return cache;
        }

        internal void ClearCache()
        {
            // If there isn't a GCHandle yet, there's nothing more to do.
            if (Volatile.Read(ref m_cache) == IntPtr.Zero)
            {
                return;
            }

            // Set the GCHandle to null. The cache will be re-created next time it is needed.
            GCHandle.InternalSet(m_cache, null);
        }

        private string? GetDefaultMemberName()
        {
            return Cache.GetDefaultMemberName();
        }

        #endregion

        #region Type Overrides

        #region Get XXXInfo Candidates

        private const int GenericParameterCountAny = -1;

        private ListBuilder<MethodInfo> GetMethodCandidates(
            string? name, int genericParameterCount, BindingFlags bindingAttr, CallingConventions callConv,
            Type[]? types, bool allowPrefixLookup)
        {
            FilterHelper(bindingAttr, ref name, allowPrefixLookup, out bool prefixLookup, out bool ignoreCase, out MemberListType listType);

            RuntimeMethodInfo[] cache = Cache.GetMethodList(listType, name);

            ListBuilder<MethodInfo> candidates = new ListBuilder<MethodInfo>(cache.Length);
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeMethodInfo methodInfo = cache[i];
                if (genericParameterCount != GenericParameterCountAny && genericParameterCount != methodInfo.GenericParameterCount)
                    continue;

                if (FilterApplyMethodInfo(methodInfo, bindingAttr, callConv, types) &&
                    (!prefixLookup || FilterApplyPrefixLookup(methodInfo, name!, ignoreCase)))
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
            FilterHelper(bindingAttr, ref name, allowPrefixLookup, out bool prefixLookup, out bool ignoreCase, out MemberListType listType);

            RuntimeConstructorInfo[] cache = Cache.GetConstructorList(listType, name);

            ListBuilder<ConstructorInfo> candidates = new ListBuilder<ConstructorInfo>(cache.Length);
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeConstructorInfo constructorInfo = cache[i];
                if (FilterApplyConstructorInfo(constructorInfo, bindingAttr, callConv, types) &&
                    (!prefixLookup || FilterApplyPrefixLookup(constructorInfo, name!, ignoreCase)))
                {
                    candidates.Add(constructorInfo);
                }
            }

            return candidates;
        }

        private ListBuilder<PropertyInfo> GetPropertyCandidates(
            string? name, BindingFlags bindingAttr, Type[]? types, bool allowPrefixLookup)
        {
            FilterHelper(bindingAttr, ref name, allowPrefixLookup, out bool prefixLookup, out bool ignoreCase, out MemberListType listType);

            RuntimePropertyInfo[] cache = Cache.GetPropertyList(listType, name);

            bindingAttr ^= BindingFlags.DeclaredOnly;

            ListBuilder<PropertyInfo> candidates = new ListBuilder<PropertyInfo>(cache.Length);
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimePropertyInfo propertyInfo = cache[i];
                if ((bindingAttr & propertyInfo.BindingFlags) == propertyInfo.BindingFlags &&
                    (!prefixLookup || FilterApplyPrefixLookup(propertyInfo, name!, ignoreCase)) &&
                    (types == null || (propertyInfo.GetIndexParameters().Length == types.Length)))
                {
                    candidates.Add(propertyInfo);
                }
            }

            return candidates;
        }

        private ListBuilder<EventInfo> GetEventCandidates(string? name, BindingFlags bindingAttr, bool allowPrefixLookup)
        {
            FilterHelper(bindingAttr, ref name, allowPrefixLookup, out bool prefixLookup, out bool ignoreCase, out MemberListType listType);

            RuntimeEventInfo[] cache = Cache.GetEventList(listType, name);

            bindingAttr ^= BindingFlags.DeclaredOnly;

            ListBuilder<EventInfo> candidates = new ListBuilder<EventInfo>(cache.Length);
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeEventInfo eventInfo = cache[i];
                if ((bindingAttr & eventInfo.BindingFlags) == eventInfo.BindingFlags &&
                    (!prefixLookup || FilterApplyPrefixLookup(eventInfo, name!, ignoreCase)))
                {
                    candidates.Add(eventInfo);
                }
            }

            return candidates;
        }

        private ListBuilder<FieldInfo> GetFieldCandidates(string? name, BindingFlags bindingAttr, bool allowPrefixLookup)
        {
            FilterHelper(bindingAttr, ref name, allowPrefixLookup, out bool prefixLookup, out bool ignoreCase, out MemberListType listType);

            RuntimeFieldInfo[] cache = Cache.GetFieldList(listType, name);

            bindingAttr ^= BindingFlags.DeclaredOnly;

            ListBuilder<FieldInfo> candidates = new ListBuilder<FieldInfo>(cache.Length);
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeFieldInfo fieldInfo = cache[i];
                if ((bindingAttr & fieldInfo.BindingFlags) == fieldInfo.BindingFlags &&
                    (!prefixLookup || FilterApplyPrefixLookup(fieldInfo, name!, ignoreCase)))
                {
                    candidates.Add(fieldInfo);
                }
            }

            return candidates;
        }

        private ListBuilder<Type> GetNestedTypeCandidates(string? fullname, BindingFlags bindingAttr, bool allowPrefixLookup)
        {
            bindingAttr &= ~BindingFlags.Static;
            SplitName(fullname, out string? name, out string? ns);
            FilterHelper(bindingAttr, ref name, allowPrefixLookup, out bool prefixLookup, out _, out MemberListType listType);

            RuntimeType[] cache = Cache.GetNestedTypeList(listType, name);

            ListBuilder<Type> candidates = new ListBuilder<Type>(cache.Length);
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeType nestedClass = cache[i];
                if (FilterApplyType(nestedClass, bindingAttr, name!, prefixLookup, ns))
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
            return GetMethodCandidates(null, GenericParameterCountAny, bindingAttr, CallingConventions.Any, null, false).ToArray();
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

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type[] GetInterfaces()
        {
            RuntimeType[] candidates = Cache.GetInterfaceList(MemberListType.All, null);
            return new ReadOnlySpan<Type>(candidates).ToArray();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            return GetNestedTypeCandidates(null, bindingAttr, false).ToArray();
        }

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            ListBuilder<MethodInfo> methods = GetMethodCandidates(null, GenericParameterCountAny, bindingAttr, CallingConventions.Any, null, false);
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

        public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type ifaceType)
        {
            if (IsGenericParameter)
                throw new InvalidOperationException(SR.Arg_GenericParameter);

            if (ifaceType is null)
                throw new ArgumentNullException(nameof(ifaceType));

            RuntimeType? ifaceRtType = ifaceType as RuntimeType;

            if (ifaceRtType == null)
                throw new ArgumentException(SR.Argument_MustBeRuntimeType, nameof(ifaceType));

            RuntimeTypeHandle ifaceRtTypeHandle = ifaceRtType.GetTypeHandleInternal();

            GetTypeHandleInternal().VerifyInterfaceIsImplemented(ifaceRtTypeHandle);
            Debug.Assert(ifaceType.IsInterface);  // VerifyInterfaceIsImplemented enforces this invariant
            Debug.Assert(!IsInterface); // VerifyInterfaceIsImplemented enforces this invariant

            // SZArrays implement the methods on IList`1, IEnumerable`1, and ICollection`1 with
            // SZArrayHelper and some runtime magic. We don't have accurate interface maps for them.
            if (IsSZArray && ifaceType.IsGenericType)
                throw new ArgumentException(SR.Argument_ArrayGetInterfaceMap);

            int ifaceVirtualMethodCount = RuntimeTypeHandle.GetNumVirtualsAndStaticVirtuals(ifaceRtType);

            InterfaceMapping im;
            im.InterfaceType = ifaceType;
            im.TargetType = this;
            im.InterfaceMethods = new MethodInfo[ifaceVirtualMethodCount];
            im.TargetMethods = new MethodInfo[ifaceVirtualMethodCount];

            for (int i = 0; i < ifaceVirtualMethodCount; i++)
            {
                RuntimeMethodHandleInternal ifaceRtMethodHandle = RuntimeTypeHandle.GetMethodAt(ifaceRtType, i);

                // GetMethodBase will convert this to the instantiating/unboxing stub if necessary
                MethodBase ifaceMethodBase = GetMethodBase(ifaceRtType, ifaceRtMethodHandle)!;
                Debug.Assert(ifaceMethodBase is RuntimeMethodInfo);
                im.InterfaceMethods[i] = (MethodInfo)ifaceMethodBase;

                // If the impl is null, then virtual stub dispatch is active.
                RuntimeMethodHandleInternal classRtMethodHandle = GetTypeHandleInternal().GetInterfaceMethodImplementation(ifaceRtTypeHandle, ifaceRtMethodHandle);

                if (classRtMethodHandle.IsNullHandle())
                    continue;

                // If we resolved to an interface method, use the interface type as reflected type. Otherwise use `this`.
                RuntimeType reflectedType = RuntimeMethodHandle.GetDeclaringType(classRtMethodHandle);
                if (!reflectedType.IsInterface)
                    reflectedType = this;

                // GetMethodBase will convert this to the instantiating/unboxing stub if necessary
                MethodBase? rtTypeMethodBase = GetMethodBase(reflectedType, classRtMethodHandle);
                // a class may not implement all the methods of an interface (abstract class) so null is a valid value
                Debug.Assert(rtTypeMethodBase is null || rtTypeMethodBase is RuntimeMethodInfo);
                im.TargetMethods[i] = (MethodInfo)rtTypeMethodBase!;
            }

            return im;
        }
        #endregion

        #region Find XXXInfo

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected override MethodInfo? GetMethodImpl(
            string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConv,
            Type[]? types, ParameterModifier[]? modifiers)
        {
            return GetMethodImplCommon(name, GenericParameterCountAny, bindingAttr, binder, callConv, types, modifiers);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected override MethodInfo? GetMethodImpl(
            string name, int genericParameterCount, BindingFlags bindingAttr, Binder? binder, CallingConventions callConv,
            Type[]? types, ParameterModifier[]? modifiers)
        {
            return GetMethodImplCommon(name, genericParameterCount, bindingAttr, binder, callConv, types, modifiers);
        }

        private MethodInfo? GetMethodImplCommon(
            string? name, int genericParameterCount, BindingFlags bindingAttr, Binder? binder, CallingConventions callConv,
            Type[]? types, ParameterModifier[]? modifiers)
        {
            ListBuilder<MethodInfo> candidates = GetMethodCandidates(name, genericParameterCount, bindingAttr, callConv, types, false);

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
                        {
                            throw new AmbiguousMatchException(SR.Arg_AmbiguousMatchException);
                        }
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
                return System.DefaultBinder.ExactBinding(candidates.ToArray(), types, modifiers) as ConstructorInfo;

            binder ??= DefaultBinder;
            return binder.SelectMethod(bindingAttr, candidates.ToArray(), types, modifiers) as ConstructorInfo;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        protected override PropertyInfo? GetPropertyImpl(
            string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            ListBuilder<PropertyInfo> candidates = GetPropertyCandidates(name, bindingAttr, types, false);

            if (candidates.Count == 0)
                return null;

            if (types == null || types.Length == 0)
            {
                // no arguments
                if (candidates.Count == 1)
                {
                    PropertyInfo firstCandidate = candidates[0];

                    if (returnType is not null && !returnType.IsEquivalentTo(firstCandidate.PropertyType))
                        return null;

                    return firstCandidate;
                }
                else
                {
                    if (returnType is null)
                        // if we are here we have no args or property type to select over and we have more than one property with that name
                        throw new AmbiguousMatchException(SR.Arg_AmbiguousMatchException);
                }
            }

            if ((bindingAttr & BindingFlags.ExactBinding) != 0)
                return System.DefaultBinder.ExactPropertyBinding(candidates.ToArray(), returnType, types, modifiers);

            binder ??= DefaultBinder;
            return binder.SelectProperty(bindingAttr, candidates.ToArray(), returnType, types, modifiers);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo? GetEvent(string name, BindingFlags bindingAttr)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));

            FilterHelper(bindingAttr, ref name, out _, out MemberListType listType);

            RuntimeEventInfo[] cache = Cache.GetEventList(listType, name);
            EventInfo? match = null;

            bindingAttr ^= BindingFlags.DeclaredOnly;

            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeEventInfo eventInfo = cache[i];
                if ((bindingAttr & eventInfo.BindingFlags) == eventInfo.BindingFlags)
                {
                    if (match != null)
                        throw new AmbiguousMatchException(SR.Arg_AmbiguousMatchException);

                    match = eventInfo;
                }
            }

            return match;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo? GetField(string name, BindingFlags bindingAttr)
        {
            if (name is null) throw new ArgumentNullException();

            FilterHelper(bindingAttr, ref name, out _, out MemberListType listType);

            RuntimeFieldInfo[] cache = Cache.GetFieldList(listType, name);
            FieldInfo? match = null;

            bindingAttr ^= BindingFlags.DeclaredOnly;
            bool multipleStaticFieldMatches = false;

            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeFieldInfo fieldInfo = cache[i];
                if ((bindingAttr & fieldInfo.BindingFlags) == fieldInfo.BindingFlags)
                {
                    if (match != null)
                    {
                        if (ReferenceEquals(fieldInfo.DeclaringType, match.DeclaringType))
                            throw new AmbiguousMatchException(SR.Arg_AmbiguousMatchException);

                        if ((match.DeclaringType!.IsInterface) && (fieldInfo.DeclaringType!.IsInterface))
                            multipleStaticFieldMatches = true;
                    }

                    if (match == null || fieldInfo.DeclaringType!.IsSubclassOf(match.DeclaringType!) || match.DeclaringType!.IsInterface)
                        match = fieldInfo;
                }
            }

            if (multipleStaticFieldMatches && match!.DeclaringType!.IsInterface)
                throw new AmbiguousMatchException(SR.Arg_AmbiguousMatchException);

            return match;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type? GetInterface(string fullname, bool ignoreCase)
        {
            if (fullname is null) throw new ArgumentNullException(nameof(fullname));

            BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.NonPublic;

            bindingAttr &= ~BindingFlags.Static;

            if (ignoreCase)
                bindingAttr |= BindingFlags.IgnoreCase;

            string name, ns;
            SplitName(fullname, out name!, out ns!);
            FilterHelper(bindingAttr, ref name, out _, out MemberListType listType);

            RuntimeType[] cache = Cache.GetInterfaceList(listType, name);

            RuntimeType? match = null;

            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeType iface = cache[i];
                if (FilterApplyType(iface, bindingAttr, name, false, ns))
                {
                    if (match != null)
                        throw new AmbiguousMatchException(SR.Arg_AmbiguousMatchException);

                    match = iface;
                }
            }

            return match;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type? GetNestedType(string fullname, BindingFlags bindingAttr)
        {
            if (fullname is null) throw new ArgumentNullException(nameof(fullname));

            bindingAttr &= ~BindingFlags.Static;
            string name, ns;
            SplitName(fullname, out name!, out ns!);
            FilterHelper(bindingAttr, ref name, out _, out MemberListType listType);

            RuntimeType[] cache = Cache.GetNestedTypeList(listType, name);

            RuntimeType? match = null;

            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeType nestedType = cache[i];
                if (FilterApplyType(nestedType, bindingAttr, name, false, ns))
                {
                    if (match != null)
                        throw new AmbiguousMatchException(SR.Arg_AmbiguousMatchException);

                    match = nestedType;
                }
            }

            return match;
        }

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));

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
                methods = GetMethodCandidates(name, GenericParameterCountAny, bindingAttr, CallingConventions.Any, null, true);
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
            if (member is null) throw new ArgumentNullException(nameof(member));

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

        private static MemberInfo? GetMethodWithSameMetadataDefinitionAs(RuntimeType runtimeType, MemberInfo method)
        {
            RuntimeMethodInfo[] cache = runtimeType.Cache.GetMethodList(MemberListType.CaseSensitive, method.Name);

            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeMethodInfo candidate = cache[i];
                if (candidate.HasSameMetadataDefinitionAs(method))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static MemberInfo? GetConstructorWithSameMetadataDefinitionAs(RuntimeType runtimeType, MemberInfo constructor)
        {
            RuntimeConstructorInfo[] cache = runtimeType.Cache.GetConstructorList(MemberListType.CaseSensitive, constructor.Name);

            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeConstructorInfo candidate = cache[i];
                if (candidate.HasSameMetadataDefinitionAs(constructor))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static MemberInfo? GetPropertyWithSameMetadataDefinitionAs(RuntimeType runtimeType, MemberInfo property)
        {
            RuntimePropertyInfo[] cache = runtimeType.Cache.GetPropertyList(MemberListType.CaseSensitive, property.Name);

            for (int i = 0; i < cache.Length; i++)
            {
                RuntimePropertyInfo candidate = cache[i];
                if (candidate.HasSameMetadataDefinitionAs(property))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static MemberInfo? GetFieldWithSameMetadataDefinitionAs(RuntimeType runtimeType, MemberInfo field)
        {
            RuntimeFieldInfo[] cache = runtimeType.Cache.GetFieldList(MemberListType.CaseSensitive, field.Name);

            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeFieldInfo candidate = cache[i];
                if (candidate.HasSameMetadataDefinitionAs(field))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static MemberInfo? GetEventWithSameMetadataDefinitionAs(RuntimeType runtimeType, MemberInfo eventInfo)
        {
            RuntimeEventInfo[] cache = runtimeType.Cache.GetEventList(MemberListType.CaseSensitive, eventInfo.Name);

            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeEventInfo candidate = cache[i];
                if (candidate.HasSameMetadataDefinitionAs(eventInfo))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static MemberInfo? GetNestedTypeWithSameMetadataDefinitionAs(RuntimeType runtimeType, MemberInfo nestedType)
        {
            RuntimeType[] cache = runtimeType.Cache.GetNestedTypeList(MemberListType.CaseSensitive, nestedType.Name);

            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeType candidate = cache[i];
                if (candidate.HasSameMetadataDefinitionAs(nestedType))
                {
                    return candidate;
                }
            }

            return null;
        }
        #endregion

        #region Identity

        public sealed override bool IsCollectible
        {
            get
            {
                RuntimeType thisType = this;
                return RuntimeTypeHandle.IsCollectible(new QCallTypeHandle(ref thisType)) != Interop.BOOL.FALSE;
            }
        }

        public override MethodBase? DeclaringMethod
        {
            get
            {
                if (!IsGenericParameter)
                    throw new InvalidOperationException(SR.Arg_NotGenericParameter);

                IRuntimeMethodInfo declaringMethod = RuntimeTypeHandle.GetDeclaringMethod(this);

                if (declaringMethod == null)
                    return null;

                return GetMethodBase(RuntimeMethodHandle.GetDeclaringType(declaringMethod), declaringMethod);
            }
        }
        #endregion

        #region Hierarchy

        public override bool IsSubclassOf(Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            RuntimeType? rtType = type as RuntimeType;
            if (rtType == null)
                return false;

            RuntimeType? baseType = GetBaseType();

            while (baseType != null)
            {
                if (baseType == rtType)
                    return true;

                baseType = baseType.GetBaseType();
            }

            // pretty much everything is a subclass of object, even interfaces
            // notice that interfaces are really odd because they do not have a BaseType
            // yet IsSubclassOf(typeof(object)) returns true
            if (rtType == ObjectType && rtType != this)
                return true;

            return false;
        }

#if FEATURE_TYPEEQUIVALENCE
        // Reflexive, symmetric, transitive.
        public override bool IsEquivalentTo([NotNullWhen(true)] Type? other)
        {
            if (!(other is RuntimeType otherRtType))
            {
                return false;
            }

            if (otherRtType == this)
            {
                return true;
            }

            return RuntimeTypeHandle.IsEquivalentTo(this, otherRtType);
        }
#endif // FEATURE_TYPEEQUIVALENCE

        #endregion

        #region Name

        public override string? FullName => GetCachedName(TypeNameKind.FullName);

        public override string? AssemblyQualifiedName
        {
            get
            {
                string? fullname = FullName;

                // FullName is null if this type contains generic parameters but is not a generic type definition.
                if (fullname == null)
                    return null;

                return Assembly.CreateQualifiedName(Assembly.FullName, fullname);
            }
        }

        public override string? Namespace
        {
            get
            {
                string ns = Cache.GetNameSpace();
                if (string.IsNullOrEmpty(ns))
                {
                    return null;
                }

                return ns;
            }
        }

        #endregion

        #region Attributes

        public override Guid GUID
        {
            get
            {
                Guid result = default;
                GetGUID(ref result);
                return result;
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void GetGUID(ref Guid result);

        internal bool IsDelegate() => GetBaseType() == typeof(MulticastDelegate);

        public override GenericParameterAttributes GenericParameterAttributes
        {
            get
            {
                if (!IsGenericParameter)
                    throw new InvalidOperationException(SR.Arg_NotGenericParameter);


                RuntimeTypeHandle.GetMetadataImport(this).GetGenericParamProps(MetadataToken, out GenericParameterAttributes attributes);

                return attributes;
            }
        }

        #endregion

        #region Arrays

        public sealed override bool IsSZArray => RuntimeTypeHandle.IsSZArray(this);

        internal object[] GetEmptyArray() => Cache.GetEmptyArray();

        #endregion

        #region Generics
        internal RuntimeType[] GetGenericArgumentsInternal()
        {
            return GetRootElementType().GetTypeHandleInternal().GetInstantiationInternal();
        }

        public override Type[] GetGenericArguments()
        {
            Type[] types = GetRootElementType().GetTypeHandleInternal().GetInstantiationPublic();
            return types ?? Type.EmptyTypes;
        }

        public override Type MakeGenericType(Type[] instantiation)
        {
            if (instantiation == null)
                throw new ArgumentNullException(nameof(instantiation));

            if (!IsGenericTypeDefinition)
                throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericTypeDefinition, this));

            RuntimeType[] genericParameters = GetGenericArgumentsInternal();
            if (genericParameters.Length != instantiation.Length)
                throw new ArgumentException(SR.Argument_GenericArgsCount, nameof(instantiation));

            if (instantiation.Length == 1 && instantiation[0] is RuntimeType rt)
            {
                ThrowIfTypeNeverValidGenericArgument(rt);
                try
                {
                    return new RuntimeTypeHandle(this).Instantiate(rt);
                }
                catch (TypeLoadException e)
                {
                    ValidateGenericArguments(this, new[] { rt }, e);
                    throw;
                }
            }

            RuntimeType[] instantiationRuntimeType = new RuntimeType[instantiation.Length];

            bool foundSigType = false;
            bool foundNonRuntimeType = false;
            for (int i = 0; i < instantiation.Length; i++)
            {
                Type instantiationElem = instantiation[i];
                if (instantiationElem == null)
                    throw new ArgumentNullException();

                RuntimeType? rtInstantiationElem = instantiationElem as RuntimeType;

                if (rtInstantiationElem == null)
                {
                    foundNonRuntimeType = true;
                    if (instantiationElem.IsSignatureType)
                    {
                        foundSigType = true;
                    }
                }

                instantiationRuntimeType[i] = rtInstantiationElem!;
            }

            if (foundNonRuntimeType)
            {
                if (foundSigType)
                    return new SignatureConstructedGenericType(this, instantiation);

                return System.Reflection.Emit.TypeBuilderInstantiation.MakeGenericType(this, (Type[])(instantiation.Clone()));
            }

            SanityCheckGenericArguments(instantiationRuntimeType, genericParameters);

            Type ret;
            try
            {
                ret = new RuntimeTypeHandle(this).Instantiate(instantiationRuntimeType);
            }
            catch (TypeLoadException e)
            {
                ValidateGenericArguments(this, instantiationRuntimeType, e);
                throw;
            }

            return ret;
        }

        public override int GenericParameterPosition
        {
            get
            {
                if (!IsGenericParameter)
                    throw new InvalidOperationException(SR.Arg_NotGenericParameter);

                return new RuntimeTypeHandle(this).GetGenericVariableIndex();
            }
        }

        public override bool ContainsGenericParameters =>
            GetRootElementType().GetTypeHandleInternal().ContainsGenericVariables();

        public override Type[] GetGenericParameterConstraints()
        {
            if (!IsGenericParameter)
                throw new InvalidOperationException(SR.Arg_NotGenericParameter);

            Type[] constraints = new RuntimeTypeHandle(this).GetConstraints();
            return constraints ?? Type.EmptyTypes;
        }
        #endregion

        #region Misc
        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other) => HasSameMetadataDefinitionAsCore<RuntimeType>(other);

        public override Type MakePointerType() => new RuntimeTypeHandle(this).MakePointer();

        public override Type MakeByRefType() => new RuntimeTypeHandle(this).MakeByRef();

        public override Type MakeArrayType() => new RuntimeTypeHandle(this).MakeSZArray();

        public override Type MakeArrayType(int rank)
        {
            if (rank <= 0)
                throw new IndexOutOfRangeException();

            return new RuntimeTypeHandle(this).MakeArray(rank);
        }

        public override StructLayoutAttribute? StructLayoutAttribute => PseudoCustomAttribute.GetStructLayoutCustomAttribute(this);

        #endregion

        #region Invoke Member

        private static readonly RuntimeType s_typedRef = (RuntimeType)typeof(TypedReference);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool CanValueSpecialCast(RuntimeType valueType, RuntimeType targetType);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object AllocateValueType(RuntimeType type, object? value, bool fForceTypeChange);

        internal object? CheckValue(object? value, Binder? binder, CultureInfo? culture, BindingFlags invokeAttr)
        {
            // this method is used by invocation in reflection to check whether a value can be assigned to type.
            if (IsInstanceOfType(value))
            {
                // Since this cannot be a generic parameter, we use RuntimeTypeHandle.IsValueType here
                // because it is faster than IsValueType
                Debug.Assert(!IsGenericParameter);

                Type type = value!.GetType();

                if (!ReferenceEquals(type, this) && RuntimeTypeHandle.IsValueType(this))
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
                Pointer? pointer = value as Pointer;
                if (pointer != null)
                    valueType = (RuntimeType)pointer.GetPointerType();
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
                throw new ArgumentException(SR.Format(SR.Arg_ObjObjEx, value.GetType(), this));

            return TryChangeType(value, binder, culture, needsSpecialCast);
        }

        // Factored out of CheckValue to reduce code complexity.
        private object? TryChangeType(object value, Binder? binder, CultureInfo? culture, bool needsSpecialCast)
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
                    Pointer? pointer = value as Pointer;
                    if (pointer != null)
                        valueType = (RuntimeType)pointer.GetPointerType();
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

            throw new ArgumentException(SR.Format(SR.Arg_ObjObjEx, value.GetType(), this));
        }

        #endregion

        #endregion

        public override string ToString() => GetCachedName(TypeNameKind.ToString)!;

        #region MemberInfo Overrides

        public override string Name => GetCachedName(TypeNameKind.Name)!;

        // This method looks like an attractive inline but expands to two calls,
        // neither of which can be inlined or optimized further. So block it
        // from inlining.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private string? GetCachedName(TypeNameKind kind) => Cache.GetName(kind);

        public override Type? DeclaringType => Cache.GetEnclosingType();

        #endregion

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

            object? instance;

            args ??= Array.Empty<object>();

            // Without a binder we need to do use the default binder...
            binder ??= DefaultBinder;

            // deal with the __COMObject case first. It is very special because from a reflection point of view it has no ctors
            // so a call to GetMemberCons would fail
            bool publicOnly = (bindingAttr & BindingFlags.NonPublic) == 0;
            bool wrapExceptions = (bindingAttr & BindingFlags.DoNotWrapExceptions) == 0;
            if (args.Length == 0 && (bindingAttr & BindingFlags.Public) != 0 && (bindingAttr & BindingFlags.Instance) != 0
                && (IsGenericCOMObjectImpl() || IsValueType))
            {
                instance = CreateInstanceDefaultCtor(publicOnly, wrapExceptions);
            }
            else
            {
                ConstructorInfo[] candidates = GetConstructors(bindingAttr);
                List<MethodBase> matches = new List<MethodBase>(candidates.Length);

                // We cannot use Type.GetTypeArray here because some of the args might be null
                Type[] argsType = new Type[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] is object arg)
                    {
                        argsType[i] = arg.GetType();
                    }
                }

                for (int i = 0; i < candidates.Length; i++)
                {
                    if (FilterApplyConstructorInfo((RuntimeConstructorInfo)candidates[i], bindingAttr, CallingConventions.Any, argsType))
                        matches.Add(candidates[i]);
                }

                if (matches.Count == 0)
                {
                    throw new MissingMethodException(SR.Format(SR.MissingConstructor_Name, FullName));
                }

                MethodBase[] cons = matches.ToArray();

                MethodBase? invokeMethod;
                object? state = null;

                try
                {
                    invokeMethod = binder.BindToMethod(bindingAttr, cons, ref args, null, culture, null, out state);
                }
                catch (MissingMethodException) { invokeMethod = null; }

                if (invokeMethod is null)
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
                    instance = Activator.CreateInstance(this, nonPublic: true, wrapExceptions: wrapExceptions);
                }
                else
                {
                    instance = ((ConstructorInfo)invokeMethod).Invoke(bindingAttr, binder, args, culture);
                    if (state != null)
                        binder.ReorderArgumentArray(ref args, state);
                }
            }

            return instance;
        }

        /// <summary>
        /// Helper to invoke the default (parameterless) constructor.
        /// </summary>
        [DebuggerStepThrough]
        [DebuggerHidden]
        internal object? CreateInstanceDefaultCtor(bool publicOnly, bool wrapExceptions)
        {
            // Get or create the cached factory. Creating the cache will fail if one
            // of our invariant checks fails; e.g., no appropriate ctor found.

            if (GenericCache is not ActivatorCache cache)
            {
                cache = new ActivatorCache(this);
                GenericCache = cache;
            }

            if (!cache.CtorIsPublic && publicOnly)
            {
                throw new MissingMethodException(SR.Format(SR.Arg_NoDefCTor, this));
            }

            // Compat: allocation always takes place outside the try block so that OOMs
            // bubble up to the caller; the ctor invocation is within the try block so
            // that it can be wrapped in TIE if needed.

            object? obj = cache.CreateUninitializedObject(this);
            try
            {
                cache.CallConstructor(obj);
            }
            catch (Exception e) when (wrapExceptions)
            {
                throw new TargetInvocationException(e);
            }

            return obj;
        }

        // Specialized version of the above for Activator.CreateInstance<T>()
        [DebuggerStepThrough]
        [DebuggerHidden]
        internal object? CreateInstanceOfT()
        {
            if (GenericCache is not ActivatorCache cache)
            {
                cache = new ActivatorCache(this);
                GenericCache = cache;
            }

            if (!cache.CtorIsPublic)
            {
                throw new MissingMethodException(SR.Format(SR.Arg_NoDefCTor, this));
            }

            object? obj = cache.CreateUninitializedObject(this);
            try
            {
                cache.CallConstructor(obj);
            }
            catch (Exception e)
            {
                throw new TargetInvocationException(e);
            }

            return obj;
        }

        internal void InvalidateCachedNestedType() => Cache.InvalidateCachedNestedType();

        internal bool IsGenericCOMObjectImpl() => RuntimeTypeHandle.IsComObject(this, true);

        #endregion

        #region Legacy internal static

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object _CreateEnum(RuntimeType enumType, long value);

        internal static object CreateEnum(RuntimeType enumType, long value)
        {
            return _CreateEnum(enumType, value);
        }

#if FEATURE_COMINTEROP
        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern object InvokeDispMethod(
            string name, BindingFlags invokeAttr, object target, object?[]? args,
            bool[]? byrefModifiers, int culture, string[]? namedParameters);
#endif // FEATURE_COMINTEROP

        #endregion

#if FEATURE_COMINTEROP
        [RequiresUnreferencedCode("The member might be removed")]
        private object? ForwardCallToInvokeMember(
            string memberName,
            BindingFlags flags,
            object? target,
            object[] aArgs, // in/out - only byref values are in a valid state upon return
            bool[] aArgsIsByRef,
            int[]? aArgsWrapperTypes, // _maybe_null_
            Type[] aArgsTypes,
            Type retType)
        {
            if (!Marshal.IsBuiltInComSupported)
            {
                throw new NotSupportedException(SR.NotSupported_COM);
            }

            Debug.Assert(
                aArgs.Length == aArgsIsByRef.Length
                && aArgs.Length == aArgsTypes.Length
                && (aArgsWrapperTypes == null || aArgs.Length == aArgsWrapperTypes.Length), "Input arrays should all be of the same length");

            int cArgs = aArgs.Length;

            // Handle arguments that are passed as ByRef and those
            // arguments that need to be wrapped.
            ParameterModifier[]? aParamMod = null;
            if (cArgs > 0)
            {
                ParameterModifier paramMod = new ParameterModifier(cArgs);
                for (int i = 0; i < cArgs; i++)
                {
                    paramMod[i] = aArgsIsByRef[i];
                }

                aParamMod = new ParameterModifier[] { paramMod };
                if (aArgsWrapperTypes != null)
                {
                    WrapArgsForInvokeCall(aArgs, aArgsWrapperTypes);
                }
            }

            // For target invocation exceptions, the exception is wrapped.
            flags |= BindingFlags.DoNotWrapExceptions;
            object? ret = InvokeMember(memberName, flags, null, target, aArgs, aParamMod, null, null);

            // Convert each ByRef argument that is _not_ of the proper type to
            // the parameter type.
            for (int i = 0; i < cArgs; i++)
            {
                // Determine if the parameter is ByRef.
                if (aParamMod![0][i] && aArgs[i] != null)
                {
                    Type argType = aArgsTypes[i];
                    if (!ReferenceEquals(argType, aArgs[i].GetType()))
                    {
                        aArgs[i] = ForwardCallBinder.ChangeType(aArgs[i], argType, null);
                    }
                }
            }

            // If the return type is _not_ of the proper type, then convert it.
            if (ret != null)
            {
                if (!ReferenceEquals(retType, ret.GetType()))
                {
                    ret = ForwardCallBinder.ChangeType(ret, retType, null);
                }
            }

            return ret;
        }

        private static void WrapArgsForInvokeCall(object[] aArgs, int[] aArgsWrapperTypes)
        {
            int cArgs = aArgs.Length;
            for (int i = 0; i < cArgs; i++)
            {
                if (aArgsWrapperTypes[i] == 0)
                {
                    continue;
                }

                if (((DispatchWrapperType)aArgsWrapperTypes[i]).HasFlag(DispatchWrapperType.SafeArray))
                {
                    Type wrapperType = null!;
                    bool isString = false;

                    // Determine the type of wrapper to use.
                    switch ((DispatchWrapperType)aArgsWrapperTypes[i] & ~DispatchWrapperType.SafeArray)
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
                            Debug.Fail("[RuntimeType.WrapArgsForInvokeCall]Invalid safe array wrapper type specified.");
                            break;
                    }

                    // Allocate the new array of wrappers.
                    Array oldArray = (Array)aArgs[i];
                    int numElems = oldArray.Length;
                    object[] newArray = (object[])Array.CreateInstance(wrapperType, numElems);

                    // Retrieve the ConstructorInfo for the wrapper type.
                    ConstructorInfo wrapperCons;
                    if (isString)
                    {
                        wrapperCons = wrapperType.GetConstructor(new Type[] { typeof(string) })!;
                    }
                    else
                    {
                        wrapperCons = wrapperType.GetConstructor(new Type[] { typeof(object) })!;
                    }

                    // Wrap each of the elements of the array.
                    for (int currElem = 0; currElem < numElems; currElem++)
                    {
                        if (isString)
                        {
                            newArray[currElem] = wrapperCons.Invoke(new object?[] { (string?)oldArray.GetValue(currElem) });
                        }
                        else
                        {
                            newArray[currElem] = wrapperCons.Invoke(new object?[] { oldArray.GetValue(currElem) });
                        }
                    }

                    // Update the argument.
                    aArgs[i] = newArray;
                }
                else
                {
                    // Determine the wrapper to use and then wrap the argument.
                    switch ((DispatchWrapperType)aArgsWrapperTypes[i])
                    {
                        case DispatchWrapperType.Unknown:
                            aArgs[i] = new UnknownWrapper(aArgs[i]);
                            break;
                        case DispatchWrapperType.Dispatch:
                            Debug.Assert(OperatingSystem.IsWindows());
                            aArgs[i] = new DispatchWrapper(aArgs[i]);
                            break;
                        case DispatchWrapperType.Error:
                            aArgs[i] = new ErrorWrapper(aArgs[i]);
                            break;
                        case DispatchWrapperType.Currency:
                            aArgs[i] = new CurrencyWrapper(aArgs[i]);
                            break;
                        case DispatchWrapperType.BStr:
                            aArgs[i] = new BStrWrapper((string)aArgs[i]);
                            break;
                        default:
                            Debug.Fail("[RuntimeType.WrapArgsForInvokeCall]Invalid wrapper type specified.");
                            break;
                    }
                }
            }
        }

        private static OleAutBinder? s_ForwardCallBinder;
        private static OleAutBinder ForwardCallBinder => s_ForwardCallBinder ??= new OleAutBinder();

        [Flags]
        private enum DispatchWrapperType : int
        {
            // This enum must stay in sync with the DispatchWrapperType enum defined in MLInfo.h
            Unknown = 0x00000001,
            Dispatch = 0x00000002,
            // Record          = 0x00000004,
            Error = 0x00000008,
            Currency = 0x00000010,
            BStr = 0x00000020,
            SafeArray = 0x00010000
        }

#endif // FEATURE_COMINTEROP
    }

    #region Library
    internal readonly unsafe struct MdUtf8String
    {
        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern bool EqualsCaseInsensitive(void* szLhs, void* szRhs, int cSz);

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern uint HashCaseInsensitive(void* sz, int cSz);

        private readonly byte* m_pStringHeap;        // This is the raw UTF8 string.
        private readonly int m_StringHeapByteLength;

        internal MdUtf8String(void* pStringHeap)
        {
            byte* pStringBytes = (byte*)pStringHeap;
            if (pStringHeap != null)
            {
                m_StringHeapByteLength = string.strlen(pStringBytes);
            }
            else
            {
                m_StringHeapByteLength = 0;
            }

            m_pStringHeap = pStringBytes;
        }

        internal MdUtf8String(byte* pUtf8String, int cUtf8String)
        {
            m_pStringHeap = pUtf8String;
            m_StringHeapByteLength = cUtf8String;
        }

        // Very common called version of the Equals pair
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool Equals(MdUtf8String s)
        {
            if (s.m_StringHeapByteLength != m_StringHeapByteLength)
            {
                return false;
            }
            else
            {
                return SpanHelpers.SequenceEqual(ref *s.m_pStringHeap, ref *m_pStringHeap, (uint)m_StringHeapByteLength);
            }
        }

        internal bool EqualsCaseInsensitive(MdUtf8String s)
        {
            if (s.m_StringHeapByteLength != m_StringHeapByteLength)
            {
                return false;
            }
            else
            {
                return (m_StringHeapByteLength == 0) || EqualsCaseInsensitive(s.m_pStringHeap, m_pStringHeap, m_StringHeapByteLength);
            }
        }

        internal uint HashCaseInsensitive()
        {
            return HashCaseInsensitive(m_pStringHeap, m_StringHeapByteLength);
        }

        public override string ToString()
            => Encoding.UTF8.GetString(new ReadOnlySpan<byte>(m_pStringHeap, m_StringHeapByteLength));
    }
    #endregion
}

namespace System.Reflection
{
    // Reliable hashtable thread safe for multiple readers and single writer. Note that the reliability goes together with thread
    // safety. Thread safety for multiple readers requires atomic update of the state that also makes the table
    // reliable in the presence of asynchronous exceptions.
    internal struct CerHashtable<K, V> where K : class
    {
        private sealed class Table
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
                        Debug.Assert(!hit.Equals(key), "Key was already in CerHashtable!  Potential race condition (or bug) in the Reflection cache?");

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
            // For strings we don't want the key to differ across domains as CerHashtable might be shared.
            if (!(key is string sKey))
            {
                return key.GetHashCode();
            }
            else
            {
                return sKey.GetNonRandomizedHashCode();
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
            get
            {
                Table table = Volatile.Read(ref m_Table);
                if (table == null)
                    return default!;

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
                        return default!;
                    }
                }
            }
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
        }
    }
}
