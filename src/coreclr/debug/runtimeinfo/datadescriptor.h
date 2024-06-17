// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// No include guards. This file is included multiple times.

// The format is:
// CDAC_BASELINE("string") baseline data contract that the runtime should follow. "empty" is reasonable
// CDAC_TYPES_BEGIN()
// ... <types> ...
// CDAC_TYPES_END()
// CDAC_GLOBALS_BEGIN()
// ... <globals> ...
// CDAC_GLOBALS_END()
//
// In <types> the format is:
// CDAC_TYPE_BEGIN(cdacTypeIdentifier) // defined a new data descriptor named cdacIdentifier
//
// CDAC_TYPE_SIZE(k) -or- CDAC_TYPE_INDETERMINATE(cdacTypeIdentifier) specifies that the type has
//   size k (bytes - usually sizeof(SomeNativeType)) or specify that the type's size is not provided
//   It is important that CDAC_TYPE_SIZE or CDAC_TYPE_INDETERMINATE immediately follows
//   CDAC_TYPE_BEGIN
//
// CDAC_TYPE_FIELD(cdacTypeIdentifier, cdacFieldTypeIdentifier, cdacFieldName, k) specifies the
//   field of "cdacTypeIdentifier" that has name cdacFieldName and has the type
//   "cdacFieldtypeIdentifier" located at offset k in the type layout.  k is usually
//   offsetof(SomeClass, m_FieldName) if the field is public
//
//     if the field is private, the convention is that SomeClass declares a friend struct
//     cdac_offsets<T> and provides a specialization of cdac_offsets<T> with a public constexpr
//     size_t member that holds the offset:
//
//     class MyClass {
//       private:
//         void* m_myField;
//         friend template<typename T> cdac_offsets<T>;
//     };
//     template<> struct cdac_offsets<MyClass> {
//       static constexpr size_t MyField = offsetof(MyClass, m_myField);
//     };
//
//     then the field layout can be specified as
//     CDAC_TYPE_FIELD(MyClassLayout, pointer, MyField, cdac_offsets<MyClass>::MyField)
//  There can be zero or more CDAC_TYPE_FIELD entries per type layout
//
// CDAC_TYPE_END(cdacTypeIdentifier)  specifies the end of the type layout for cdacTypeIdentifier
//
// In <globals> the format is:
//
// CDAC_GLOBAL(cdacGlobalName, cdacTypeIdentifier, value)
// or
// CDAC_GLOBAL_POINTER(cdacGlobalName, cdacTypeIdentifier, address)
//
// Zero or more globals can be defined
//
// if a global is given with CDAC_GLOBAL(), `value` should be a constexpr uint64_t (or convertible
// to uint64_t) for example, it can be a literal constant or a preprocessor definition
//
// if a global is a CDAC_GLOBAL_POINTER(), address should be a constexpr pointer or a constexpr
// uintptr_t
//
//
//
// This file is compiled using the target architecture.  Preprocessor defines for the target
// platform will be available.  It is ok to use `#ifdef`.

#ifndef CDAC_BASELINE
#define CDAC_BASELINE(identifier)
#endif
#ifndef CDAC_TYPES_BEGIN
#define CDAC_TYPES_BEGIN()
#endif
#ifndef CDAC_TYPE_BEGIN
#define CDAC_TYPE_BEGIN(tyname)
#endif
#ifndef CDAC_TYPE_SIZE
#define CDAC_TYPE_SIZE(k)
#endif
#ifndef CDAC_TYPE_INDETERMINATE
#define CDAC_TYPE_INDETERMINATE(tyname)
#endif
#ifndef CDAC_TYPE_FIELD
#define CDAC_TYPE_FIELD(tyname,fieldtyname,fieldname,off)
#endif
#ifndef CDAC_TYPE_END
#define CDAC_TYPE_END(tyname)
#endif
#ifndef CDAC_TYPES_END
#define CDAC_TYPES_END()
#endif
#ifndef CDAC_GLOBALS_BEGIN
#define CDAC_GLOBALS_BEGIN()
#endif
#ifndef CDAC_GLOBAL
#define CDAC_GLOBAL(globalname,tyname,val)
#endif
#ifndef CDAC_GLOBAL_POINTER
#define CDAC_GLOBAL_POINTER(globalname,addr)
#endif
#ifndef CDAC_GLOBALS_END
#define CDAC_GLOBALS_END()
#endif

CDAC_BASELINE("empty")
CDAC_TYPES_BEGIN()

CDAC_TYPE_BEGIN(Thread)
CDAC_TYPE_INDETERMINATE(Thread)
CDAC_TYPE_FIELD(Thread, /*uint32*/, Id, cdac_offsets<Thread>::Id)
CDAC_TYPE_FIELD(Thread, /*nuint*/, OSId, cdac_offsets<Thread>::OSId)
CDAC_TYPE_FIELD(Thread, GCHandle, GCHandle, cdac_offsets<Thread>::ExposedObject)
CDAC_TYPE_FIELD(Thread, GCHandle, LastThrownObject, cdac_offsets<Thread>::LastThrownObject)
CDAC_TYPE_FIELD(Thread, pointer, LinkNext, cdac_offsets<Thread>::Link)
CDAC_TYPE_END(Thread)

CDAC_TYPE_BEGIN(ThreadStore)
CDAC_TYPE_INDETERMINATE(ThreadStore)
CDAC_TYPE_FIELD(ThreadStore, /*SLink*/, FirstThreadLink, cdac_offsets<ThreadStore>::FirstThreadLink)
CDAC_TYPE_FIELD(ThreadStore, /*int32*/, ThreadCount, cdac_offsets<ThreadStore>::ThreadCount)
CDAC_TYPE_FIELD(ThreadStore, /*int32*/, UnstartedCount, cdac_offsets<ThreadStore>::UnstartedCount)
CDAC_TYPE_FIELD(ThreadStore, /*int32*/, BackgroundCount, cdac_offsets<ThreadStore>::BackgroundCount)
CDAC_TYPE_FIELD(ThreadStore, /*int32*/, PendingCount, cdac_offsets<ThreadStore>::PendingCount)
CDAC_TYPE_FIELD(ThreadStore, /*int32*/, DeadCount, cdac_offsets<ThreadStore>::DeadCount)
CDAC_TYPE_END(ThreadStore)

CDAC_TYPE_BEGIN(GCHandle)
CDAC_TYPE_SIZE(sizeof(OBJECTHANDLE))
CDAC_TYPE_END(GCHandle)

CDAC_TYPES_END()

CDAC_GLOBALS_BEGIN()
CDAC_GLOBAL_POINTER(ThreadStore, &ThreadStore::s_pThreadStore)
CDAC_GLOBAL_POINTER(FinalizerThread, &::g_pFinalizerThread)
CDAC_GLOBAL_POINTER(GCThread, &::g_pSuspensionThread)
#if FEATURE_EH_FUNCLETS
CDAC_GLOBAL(FeatureEHFunclets, uint8, 1)
#else
CDAC_GLOBAL(FeatureEHFunclets, uint8, 0)
#endif
CDAC_GLOBAL(SOSBreakingChangeVersion, uint8, SOS_BREAKING_CHANGE_VERSION)
CDAC_GLOBALS_END()

#undef CDAC_BASELINE
#undef CDAC_TYPES_BEGIN
#undef CDAC_TYPE_BEGIN
#undef CDAC_TYPE_INDETERMINATE
#undef CDAC_TYPE_SIZE
#undef CDAC_TYPE_FIELD
#undef CDAC_TYPE_END
#undef CDAC_TYPES_END
#undef CDAC_GLOBALS_BEGIN
#undef CDAC_GLOBAL
#undef CDAC_GLOBAL_POINTER
#undef CDAC_GLOBALS_END
