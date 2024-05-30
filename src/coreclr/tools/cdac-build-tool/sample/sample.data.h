CDAC_BASELINE("empty")
CDAC_TYPES_BEGIN()

CDAC_TYPE_BEGIN(ManagedThread)
CDAC_TYPE_INDETERMINATE(ManagedThread)
CDAC_TYPE_FIELD(ManagedThread, GCHandle, GCHandle, offsetof(ManagedThread,m_gcHandle))
CDAC_TYPE_FIELD(ManagedThread, pointer, Next, offsetof(ManagedThread,m_next))
CDAC_TYPE_END(ManagedThread)

CDAC_TYPE_BEGIN(GCHandle)
CDAC_TYPE_SIZE(sizeof(intptr_t))
CDAC_TYPE_END(GCHandle)

CDAC_TYPES_END()

CDAC_GLOBALS_BEGIN()
CDAC_GLOBAL_POINTER(ManagedThreadStore, &g_managedThreadStore)
#if FEATURE_EH_FUNCLETS
CDAC_GLOBAL(FeatureEHFunclets, uint8, 1)
#else
CDAC_GLOBAL(FeatureEHFunclets, uint8, 0)
#endif
CDAC_GLOBAL(SomeMagicNumber, uint32, 42)
CDAC_GLOBALS_END()
