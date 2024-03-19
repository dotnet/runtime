// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_OBJCMARSHAL

class ObjCMarshalNative
{
public:
    using TryGetCallback = void*(F_CALL_CONV *)(void);
    using TryGetTaggedMemoryCallback = CLR_BOOL(F_CALL_CONV *)(_In_ Object *, _Out_ void **);
    using BeginEndCallback = void(F_CALL_CONV *)(void);
    using IsReferencedCallback = int(F_CALL_CONV *)(_In_ void*);
    using EnteredFinalizationCallback = void(F_CALL_CONV *)(_In_ void*);

public: // Instance inspection
    static bool IsTrackedReference(_In_ Object * pObject, _Out_ bool* isReferenced);
public: // GC interaction
    static bool RegisterBeginEndCallback(void * callback);
    static void BeforeRefCountedHandleCallbacks();
    static void AfterRefCountedHandleCallbacks();
    static void OnEnteredFinalizerQueue(_In_ Object * object);
};

#endif // FEATURE_OBJCMARSHAL
