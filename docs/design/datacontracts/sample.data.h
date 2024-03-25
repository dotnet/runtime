CDAC_BASELINE("net9.0-rc1/Release/osx-arm64")
CDAC_TYPES_BEGIN()

CDAC_TYPE_BEGIN(ManagedThread)
CDAC_TYPE_INDETERMINATE(ManagedThread)
CDAC_TYPE_FIELD(ManagedThread, GCHandle, offsetof(ManagedThread,GCHandle))
CDAC_TYPE_END(ManagedThread)

CDAC_TYPE_BEGIN(GCHandle)
CDAC_TYPE_SIZE(sizeof(intptr_t))
CDAC_TYPE_END(GCHandle)

CDAC_TYPES_END()

CDAC_GLOBALS_BEGIN()
CDAC_GLOBAL(ManagedThreadStore, (uint64_t)(uintptr_t)&g_managedThreadStore)
CDAC_GLOBAL(FeatureCOMFlag, FEATURE_COM)
CDAC_GLOBALS_END()
