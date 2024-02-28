// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Restricted callouts refer to calls to classlib defined code written in C# made from the runtime during a
// garbage collection. As such these C# methods are constrained in what they can do and must be written very
// carefully. The most obvious restriction is that they cannot trigger a GC (by attempting to allocate memory
// for example) since that would lead to an immediate deadlock.
//
// Known constraints:
//  * No triggering of GCs (new, boxing value types, foreach over a type that allocates for its IEnumerator,
//    calling GC.Collect etc.).
//  * No exceptions can leak out of the callout.
//  * No blocking (or expensive) operations that could starve the GC or potentially lead to deadlocks.
//  * No use of runtime facilities that check whether a GC is in progress, these will deadlock. The big
//    example we know about so far is making a p/invoke call.
//  * For the AfterMarkPhase callout special attention must be paid to avoid any action that reads the MethodTable*
//    from an object header (e.g. casting). At this point the GC may have mark bits set in the pointer.
//

class MethodTable;

// Enum for the various GC callouts available. The values and their meanings are a contract with the classlib
// so be careful altering these.
enum GcRestrictedCalloutKind
{
    GCRC_StartCollection    = 0,    // Collection is about to begin
    GCRC_EndCollection      = 1,    // Collection has completed
    GCRC_AfterMarkPhase     = 2,    // All live objects are marked (not including ready for finalization
                                    // objects), no handles have been cleared
    GCRC_Count                      // Maximum number of callout types
};

class RestrictedCallouts
{
public:
    // One time startup initialization.
    static bool Initialize();

    // Register callback of the given type to the method with the given address. The most recently registered
    // callbacks are called first. Returns true on success, false if insufficient memory was available for the
    // registration.
    static bool RegisterGcCallout(GcRestrictedCalloutKind eKind, void * pCalloutMethod);

    // Unregister a previously registered callout. Removes the first registration that matches on both callout
    // kind and address. Causes a fail fast if the registration doesn't exist.
    static void UnregisterGcCallout(GcRestrictedCalloutKind eKind, void * pCalloutMethod);

    // Register callback for the "is alive" property of ref counted handles with objects of the given type
    // (the type match must be exact). The most recently registered callbacks are called first. Returns true
    // on success, false if insufficient memory was available for the registration.
    static bool RegisterRefCountedHandleCallback(void * pCalloutMethod, MethodTable * pTypeFilter);

    // Unregister a previously registered callout. Removes the first registration that matches on both callout
    // address and filter type. Causes a fail fast if the registration doesn't exist.
    static void UnregisterRefCountedHandleCallback(void * pCalloutMethod, MethodTable * pTypeFilter);

    // Invoke all the registered GC callouts of the given kind. The condemned generation of the current
    // collection is passed along to the callouts.
    static void InvokeGcCallouts(GcRestrictedCalloutKind eKind, uint32_t uiCondemnedGeneration);

    // Invoke all the registered ref counted handle callouts for the given object extracted from the handle.
    // The result is the union of the results for all the handlers that matched the object type (i.e. if one
    // of them returned true the overall result is true otherwise false is returned (which includes the case
    // where no handlers matched)). Since there should be no other side-effects of the callout, the
    // invocations cease as soon as a handler returns true.
    static bool InvokeRefCountedHandleCallbacks(Object * pObject);

private:
    // Context struct used to record which GC callbacks are registered to be made (we allow multiple
    // registrations).
    struct GcRestrictedCallout
    {
        GcRestrictedCallout *   m_pNext;            // Next callout to make or NULL
        void *                  m_pCalloutMethod;   // Address of code to call
    };

    // The head of the chains of GC callouts, one per callout type.
    static GcRestrictedCallout * s_rgGcRestrictedCallouts[GCRC_Count];

    // The handle table only has one callout type, for ref-counted handles. But it allows the client to
    // specify a type filter: i.e. only handles with an object of the exact type specified will have the
    // callout invoked.
    struct HandleTableRestrictedCallout
    {
        HandleTableRestrictedCallout *  m_pNext;            // Next callout to make or NULL
        void *                          m_pCalloutMethod;   // Address of code to call
        MethodTable *                        m_pTypeFilter;      // Type of object for which callout will be made
    };

    // The head of the chain of HandleTable callouts.
    static HandleTableRestrictedCallout * s_pHandleTableRestrictedCallouts;

    // Lock protecting access to s_rgGcRestrictedCallouts and s_pHandleTableRestrictedCallouts during
    // registration and unregistration (not used during actual callbacks since everything is single threaded
    // then).
    static CrstStatic s_sLock;

    // Prototypes for the callouts.
    typedef void (REDHAWK_CALLCONV * GcRestrictedCallbackFunction)(uint32_t uiCondemnedGeneration);
    typedef CLR_BOOL (STDCALL * HandleTableRestrictedCallbackFunction)(Object * pObject);
};
