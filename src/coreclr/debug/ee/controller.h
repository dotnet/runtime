// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: controller.h
//

//
// Debugger control flow object
//
//*****************************************************************************

#ifndef CONTROLLER_H_
#define CONTROLLER_H_

/* ========================================================================= */

#if !defined(DACCESS_COMPILE)

#include "frameinfo.h"

/* ------------------------------------------------------------------------- *
 * Forward declarations
 * ------------------------------------------------------------------------- */

class DebuggerPatchSkip;
class DebuggerThreadStarter;
class DebuggerController;
class DebuggerControllerQueue;
struct DebuggerControllerPatch;
class DebuggerUserBreakpoint;
class ControllerStackInfo;

typedef struct _DR6 *PDR6;
typedef struct _DR6 {
    DWORD       B0 : 1;
    DWORD       B1 : 1;
    DWORD       B2 : 1;
    DWORD       B3 : 1;
    DWORD       Pad1 : 9;
    DWORD       BD : 1;
    DWORD       BS : 1;
    DWORD       BT : 1;
} DR6;

typedef struct _DR7 *PDR7;
typedef struct _DR7 {
    DWORD       L0 : 1;
    DWORD       G0 : 1;
    DWORD       L1 : 1;
    DWORD       G1 : 1;
    DWORD       L2 : 1;
    DWORD       G2 : 1;
    DWORD       L3 : 1;
    DWORD       G3 : 1;
    DWORD       LE : 1;
    DWORD       GE : 1;
    DWORD       Pad1 : 3;
    DWORD       GD : 1;
    DWORD       Pad2 : 1;
    DWORD       Pad3 : 1;
    DWORD       Rwe0 : 2;
    DWORD       Len0 : 2;
    DWORD       Rwe1 : 2;
    DWORD       Len1 : 2;
    DWORD       Rwe2 : 2;
    DWORD       Len2 : 2;
    DWORD       Rwe3 : 2;
    DWORD       Len3 : 2;
} DR7;


// Ticket for ensuring that it's safe to get a stack trace.
class StackTraceTicket
{
public:
    // Each ctor is a rule for why it's safety to run a stacktrace.

    // Safe if we're at certain types of patches.
    StackTraceTicket(DebuggerControllerPatch * patch);

    // Safe if there was already another stack trace at this spot. (Grandfather clause)
    StackTraceTicket(ControllerStackInfo * info);

    // Safe it we're at a Synchronized point point.
    StackTraceTicket(Thread * pThread);

    // Safe b/c the context shows we're in native managed code
    StackTraceTicket(const BYTE * ip);

    // DebuggerUserBreakpoint has a special case of safety.
    StackTraceTicket(DebuggerUserBreakpoint * p);

    // This is like a contract violation.
    // Unsafe tickets. Use as:
    //      StackTraceTicket ticket(StackTraceTicket::UNSAFE_TICKET);
    enum EUNSAFE {
        // Ticket is unsafe. Potential issue.
        UNSAFE_TICKET = 0,

        // For some wacky reason, it's safe to take a stacktrace here, but
        // there's not an easily verifiable rule. Use this ticket very sparingly
        // because it's much more difficult to verify.
        SPECIAL_CASE_TICKET = 1
    };
    StackTraceTicket(EUNSAFE e) { };

private:
    // Tickets can't be copied around. Hide these definitions so to enforce that.
    // We still need the Copy ctor so that it can be passed in as a parameter.
    void operator=(StackTraceTicket & other);
};

/* ------------------------------------------------------------------------- *
 * ControllerStackInfo utility
 * ------------------------------------------------------------------------- *
 * class ControllerStackInfo is a class designed
 *  to simply obtain a two-frame stack trace: it will obtain the bottommost
 *  framepointer (m_bottomFP), a given target frame (m_activeFrame), and the
 *  frame above the target frame (m_returnFrame).  Note that the target frame
 *  may be the bottommost, 'active' frame, or it may be a frame higher up in
 *  the stack.  ControllerStackInfo accomplishes this by starting at the
 *  bottommost frame and walking upwards until it reaches the target frame,
 *  whereupon it records the m_activeFrame info, gets called once more to
 *  fill in the m_returnFrame info, and thereafter stops the stack walk.
 *
 *  public:
 *  void * m_bottomFP:   Frame pointer for the
 *      bottommost (most active)
 *      frame.  We can add more later, if we need it.  Currently just used in
 *      TrapStep.  NULL indicates an uninitialized value.
 *
 *  void * m_targetFP:  The frame pointer to the frame
 *      that we actually want the info of.
 *
 *  bool m_targetFrameFound:  Set to true if
 *      WalkStack finds the frame indicated by targetFP handed to GetStackInfo
 *      false otherwise.
 *
 *  FrameInfo m_activeFrame:   A FrameInfo
 *      describing the target frame.  This should always be valid after a
 *      call to GetStackInfo.
 *
 *  private:
 *  bool m_activeFound:  Set to true if we found the target frame.
 *  bool m_returnFound:  Set to true if we found the target's return frame.
 *
 *  FrameInfo m_returnFrame:   A FrameInfo
 *      describing the frame above the target frame, if  target's
 *      return frame were found (call HasReturnFrame() to see if this is
 *      valid). Otherwise, this will be the same as m_activeFrame, above
 */
class ControllerStackInfo
{
public:
    friend class StackTraceTicket;

    ControllerStackInfo()
    {
        INDEBUG(m_dbgExecuted = false);
    }

    FramePointer            m_bottomFP;
    FramePointer            m_targetFP;
    bool                    m_targetFrameFound;

    FrameInfo               m_activeFrame;

    CorDebugChainReason     m_specialChainReason;

    // static StackWalkAction ControllerStackInfo::WalkStack()   The
    //      callback that will be invoked by the DebuggerWalkStackProc.
    //      Note that the data argument is the "this" pointer to the
    //      ControllerStackInfo.
    static StackWalkAction WalkStack(FrameInfo *pInfo, void *data);


    //void ControllerStackInfo::GetStackInfo():   GetStackInfo
    //      is invoked by the user to trigger the stack walk.  This will
    //      cause the stack walk detailed in the class description to happen.
    // Thread* thread:  The thread to do the stack walk on.
    // void* targetFP:  Can be either NULL (meaning that the bottommost
    //      frame is the target), or an frame pointer, meaning that the
    //      caller wants information about a specific frame.
    // CONTEXT* pContext:  A pointer to a CONTEXT structure.  Can be null,
    //      we use our temp context.
    // bool suppressUMChainFromComPlusMethodFrameGeneric - A ridiculous flag that is trying to narrowly
    //      target a fix for issue 650903.
    // StackTraceTicket - ticket ensuring that we have permission to call this.
    void GetStackInfo(
        StackTraceTicket ticket,
        Thread *thread,
        FramePointer targetFP,
        CONTEXT *pContext,
        bool suppressUMChainFromComPlusMethodFrameGeneric = false
        );

    //bool ControllerStackInfo::HasReturnFrame()  Returns
    //      true if m_returnFrame is valid.  Returns false
    //      if m_returnFrame is set to m_activeFrame
    bool HasReturnFrame(bool allowUnmanaged = false) {LIMITED_METHOD_CONTRACT;  return m_returnFound && (allowUnmanaged || m_returnFrame.managed); }

    FrameInfo& GetReturnFrame(bool allowUnmanaged = false) {LIMITED_METHOD_CONTRACT; return HasReturnFrame(allowUnmanaged) ? m_returnFrame : m_activeFrame; }
    // This function "undoes" an unwind, i.e. it takes the active frame (the current frame)
    // and sets it to be the return frame (the caller frame).  Currently it is only used by
    // the stepper to step out of an LCG method.  See DebuggerStepper::DetectHandleLCGMethods()
    // for more information.
    void SetReturnFrameWithActiveFrame();

private:
    // If we don't have a valid context, then use this temp cache.
    CONTEXT                 m_tempContext;

    bool                    m_activeFound;
    bool                    m_returnFound;
    FrameInfo               m_returnFrame;

    // A ridiculous flag that is targeting a very narrow fix at issue 650903
    // (4.5.1/Blue).  This is set for the duration of a stackwalk designed to
    // help us "Step Out" to a managed frame (i.e., managed-only debugging).
    bool                    m_suppressUMChainFromComPlusMethodFrameGeneric;

    // Track if this stackwalk actually happened.
    // This is used by the StackTraceTicket(ControllerStackInfo * info) ticket.
    INDEBUG(bool m_dbgExecuted);
};

#endif // !DACCESS_COMPILE


/* ------------------------------------------------------------------------- *
 * DebuggerController routines
 * ------------------------------------------------------------------------- */

// simple ref-counted buffer that's shared among DebuggerPatchSkippers for a
// given DebuggerControllerPatch.  upon creation the refcount will be 1.  when
// the last skipper and controller are cleaned up the buffer will be released.
// note that there isn't a clear owner of this buffer since a controller can be
// cleaned up while the final skipper is still in flight.
class SharedPatchBypassBuffer
{
public:
    SharedPatchBypassBuffer() : m_refCount(1)
    {
#ifdef _DEBUG
        DWORD cbToProtect = MAX_INSTRUCTION_LENGTH;
        _ASSERTE(DbgIsExecutable((BYTE*)PatchBypass, cbToProtect));
#endif // _DEBUG

        // sentinel value indicating uninitialized data
        *(reinterpret_cast<DWORD*>(PatchBypass)) = SentinelValue;
#ifdef TARGET_AMD64
        *(reinterpret_cast<DWORD*>(BypassBuffer)) = SentinelValue;
        RipTargetFixup = 0;
        RipTargetFixupSize = 0;
#elif defined(TARGET_ARM64)
        RipTargetFixup = 0;
#endif
    }

    ~SharedPatchBypassBuffer()
    {
        // trap deletes that don't go through Release()
        _ASSERTE(m_refCount == 0);
    }

    LONG AddRef()
    {
#if !defined(DACCESS_COMPILE) && defined(HOST_OSX) && defined(HOST_ARM64)
        ExecutableWriterHolder<LONG> refCountWriterHolder(&m_refCount, sizeof(LONG));
        LONG *pRefCountRW = refCountWriterHolder.GetRW();
#else // !DACCESS_COMPILE && HOST_OSX && HOST_ARM64
        LONG *pRefCountRW = &m_refCount;
#endif // !DACCESS_COMPILE && HOST_OSX && HOST_ARM64

        LONG newRefCount = InterlockedIncrement(pRefCountRW);
        _ASSERTE(newRefCount > 0);
        return newRefCount;
    }

    LONG Release()
    {
#if !DACCESS_COMPILE && HOST_OSX && HOST_ARM64
        ExecutableWriterHolder<LONG> refCountWriterHolder(&m_refCount, sizeof(LONG));
        LONG *pRefCountRW = refCountWriterHolder.GetRW();
#else // !DACCESS_COMPILE && HOST_OSX && HOST_ARM64
        LONG *pRefCountRW = &m_refCount;
#endif // !DACCESS_COMPILE && HOST_OSX && HOST_ARM64

        LONG newRefCount = InterlockedDecrement(pRefCountRW);
        _ASSERTE(newRefCount >= 0);

        if (newRefCount == 0)
        {
            TRACE_FREE(this);
            DeleteInteropSafeExecutable(this);
        }

        return newRefCount;
    }

    // "PatchBypass" must be the first field of this class for alignment to be correct.
    BYTE    PatchBypass[MAX_INSTRUCTION_LENGTH];
#if defined(TARGET_AMD64)
    // If you update this value, make sure that it fits in the data payload of a
    // DebuggerHeapExecutableMemoryChunk.
    const static int cbBufferBypass = 0x40;
    BYTE    BypassBuffer[cbBufferBypass];

    UINT_PTR                RipTargetFixup;
    BYTE                    RipTargetFixupSize;
#elif defined(TARGET_ARM64)
    UINT_PTR                RipTargetFixup;
#endif

private:
    const static DWORD SentinelValue = 0xffffffff;
    LONG    m_refCount;
};

// struct DebuggerFunctionKey:  Provides a means of hashing unactivated
// breakpoints, it's used mainly for the case where the function to put
// the breakpoint in hasn't been JITted yet.
// Module* module:  Module that the method belongs to.
// mdMethodDef md:  meta data token for the method.
struct DebuggerFunctionKey1
{
    PTR_Module              module;
    mdMethodDef             md;
};

typedef DebuggerFunctionKey1 UNALIGNED DebuggerFunctionKey;

// IL Primary: Breakpoints on IL code may need to be applied to multiple
// copies of code. Historically generics was the only way IL code was JITTed
// multiple times but more recently the CodeVersionManager and tiered compilation
// provide more open-ended mechanisms to have multiple native code bodies derived
// from a single IL method body.
// The "primary" is a patch we keep to record the IL offset or native offset, and
// is used to create new "replica" patches. For native offsets only offset 0 is allowed
// because that is the only one that we think would have a consistent semantic
// meaning across different code bodies.
// There can also be multiple IL bodies for the same method given EnC or ReJIT.
// A given primary breakpoint is tightly bound to one particular IL body determined
// by encVersion. ReJIT + breakpoints isn't currently supported.
//
//
// IL Replica: The replicas created from Primary patches. If the primary used an IL offset
// then the replica also initially has an IL offset that will later become a native offset.
// If the primary uses a native offset (0) then the replica will also have a native offset (0).
// These patches always resolve to addresses in jitted code.
//
//
// NativeManaged: A patch we apply to managed code, usually for walkers etc. If this code
// is jitted then these patches are always bound to one exact jitted code body.
// If you need to be 100% sure I suggest you do more code review but I believe we also
// use this for managed code from other code generators such as a stub or statically compiled
// code that executes in cooperative mode.
//
//
// NativeUnmanaged: A patch applied to any kind of native code.

enum DebuggerPatchKind { PATCH_KIND_IL_PRIMARY, PATCH_KIND_IL_REPLICA, PATCH_KIND_NATIVE_MANAGED, PATCH_KIND_NATIVE_UNMANAGED };

// struct DebuggerControllerPatch:  An entry in the patch (hash) table,
// this should contain all the info that's needed over the course of a
// patch's lifetime.
//
// FREEHASHENTRY entry:  Three ULONGs, this is required
//      by the underlying hashtable implementation
//  DWORD opcode:  A nonzero opcode && address field means that
//      the patch has been applied to something. This may not be a full
//      opcode, it is possible it is a partial opcode.
//      A patch with a zero'd opcode field means that the patch isn't
//      actually tracking a valid break opcode.  See DebuggerPatchTable
//      for more details.
// DebuggerController *controller:  The controller that put this
//      patch here.
// BOOL fSaveOpcode:  If true, then unapply patch will save
//      a copy of the opcode in opcodeSaved, and apply patch will
//      copy opcodeSaved to opcode rather than grabbing the opcode
//      from the instruction.  This is useful mainly when the JIT
//      has moved code, and we don't want to erroneously pick up the
//      user break instruction.
//      Full story:
//      FJIT moves the code.  Once that's done, it calls Debugger->MoveCode(MethodDesc
//      *) to let us know the code moved.  At that point, unbind all the breakpoints
//      in the method.  Then we whip over all the patches, and re-bind all the
//      patches in the method.  However, we can't guarantee that the code will exist
//      in both the old & new locations exclusively of each other (the method could
//      be 0xFF bytes big, and get moved 0x10 bytes in one direction), so instead of
//      simply re-using the unbind/rebind logic as it is, we need a special case
//      wherein the old method isn't valid.  Instead, we'll copy opcode into
//      opcodeSaved, and then zero out opcode (we need to zero out opcode since that
//      tells us that the patch is invalid, if the right side sees it).  Thus the run-
//      around.
// DebuggerPatchKind: see above
// DWORD opcodeSaved:  Contains an opcode if fSaveOpcode == true
// SIZE_T nVersion:  If the patch is stored by IL offset, then we
//      must also store the version ID so that we know which version
//      this is supposed to be applied to.  Note that this will only
//      be set for DebuggerBreakpoints & DebuggerEnCBreakpoints.  For
//      others, it should be set to DMI_VERSION_INVALID.  For constants,
//      see DebuggerJitInfo
// DebuggerJitInfo dji:  A pointer to the debuggerJitInfo that describes
//      the method (and version) that this patch is applied to.  This field may
//      also have the value DebuggerJitInfo::DMI_VERSION_INVALID

// SIZE_T patchId:  Within a given patch table, all patches have a
//      semi-unique ID.  There should be one and only 1 patch for a given
//      {pid,nVersion} tuple, thus ensuring that we don't duplicate
//      patches from multiple, previous versions.
// AppDomain * pAppDomain:  Either NULL (patch applies to all appdomains
//      that the debugger is attached to)
//      or contains a pointer to an AppDomain object (patch applies only to
//      that A.D.)

// NOTE: due to unkind abuse of type system you cannot add ctor/dtor to this
//       type and expect them to be automatically invoked!
struct DebuggerControllerPatch
{
    friend class DebuggerPatchTable;
    friend class DebuggerController;

    FREEHASHENTRY           entry;
    DebuggerController      *controller;
    DebuggerFunctionKey     key;
    SIZE_T                  offset;
    PTR_CORDB_ADDRESS_TYPE  address;
    FramePointer            fp;
    PRD_TYPE                opcode; // See description above.
    BOOL                    fSaveOpcode;
    PRD_TYPE                opcodeSaved;
    BOOL                    offsetIsIL;
    TraceDestination        trace;
    MethodDesc*             pMethodDescFilter; // used for IL Primary patches that should only bind to jitted
                                               // code versions for a single generic instantiation
private:
    DebuggerPatchKind       kind;
    int                     refCount;
    union
    {
        SIZE_T     encVersion; // used for Primary patches, to record which EnC version this Primary applies to
        DebuggerJitInfo        *dji; // used for Replica and native patches, though only when tracking JIT Info
    };

#ifndef FEATURE_EMULATE_SINGLESTEP
    // this is shared among all the skippers for this controller. see the comments
    // right before the definition of SharedPatchBypassBuffer for lifetime info.
    SharedPatchBypassBuffer* m_pSharedPatchBypassBuffer;
#endif // !FEATURE_EMULATE_SINGLESTEP

public:
    SIZE_T                  patchId;
    AppDomain              *pAppDomain;

    BOOL IsNativePatch();
    BOOL IsManagedPatch();
    BOOL IsILPrimaryPatch();
    BOOL IsILReplicaPatch();
    DebuggerPatchKind  GetKind();

    // A patch has DJI if it was created with it or if it has been mapped to a
    // function that has been jitted while JIT tracking was on.  It does not
    // necessarily mean the patch is bound.  ILPrimary patches never have DJIs.
    // Patches will never have DJIs if we are not tracking JIT information.
    //
    // Patches can also be unbound, e.g. in UnbindFunctionPatches.  Any DJI gets cleared
    // when the patch is unbound.  This appears to be used as an indicator
    // to Debugger::MapAndBindFunctionPatches to make sure that
    // we don't skip the patch when we get new code.
    BOOL HasDJI()
    {
        return (!IsILPrimaryPatch() && dji != NULL);
    }

    DebuggerJitInfo *GetDJI()
    {
        _ASSERTE(!IsILPrimaryPatch());
        return dji;
    }

    // Determine if the patch is related to EnC remap logic.
    BOOL IsEnCRemapPatch();

    // These tell us which EnC version a patch relates to.  They are used
    // to determine if we are mapping a patch to a new version.
    //
    BOOL HasEnCVersion()
    {
        return (IsILPrimaryPatch() || HasDJI());
    }

    SIZE_T GetEnCVersion()
    {
        _ASSERTE(HasEnCVersion());
        return (IsILPrimaryPatch() ? encVersion : (HasDJI() ? GetDJI()->m_encVersion : CorDB_DEFAULT_ENC_FUNCTION_VERSION));
    }

    // We set the DJI explicitly after mapping a patch
    // to freshly jitted code or to a new version.  The Unbind/Bind/MovedCode mess
    // for the FJIT will also set the DJI to NULL as an indicator that Debugger::MapAndBindFunctionPatches
    // should not skip the patch.
    void SetDJI(DebuggerJitInfo *newDJI)
    {
        _ASSERTE(!IsILPrimaryPatch());
        dji = newDJI;
    }

    // A patch is bound if we've mapped it to a real honest-to-goodness
    // native address.
    // Note that we currently activate all patches immediately after binding them, and
    // delete all patches after unactivating them.  This means that the window where
    // a patch is bound but not active is very small (and should always be protected by
    // a lock).  We rely on this correlation in a few places, and ASSERT it explicitly there.
    BOOL IsBound()
    {
        if( address == NULL ) {
            // patch is unbound, cannot be active
            _ASSERTE( PRDIsEmpty(opcode) );
            return FALSE;
        }

        // IL Primary patches are never bound.
        _ASSERTE( !IsILPrimaryPatch() );

        return TRUE;
    }

    // It would be nice if we never needed IsBreakpointPatch or IsStepperPatch,
    // but a few bits of the existing code look at which controller type is involved.
    BOOL IsBreakpointPatch();
    BOOL IsStepperPatch();

    bool IsActivated()
    {
        // Patch is activate if we've stored a non-zero opcode
        // Note: this might be a problem as opcode 0 may be a valid opcode (see issue 366221).
        if( PRDIsEmpty(opcode) ) {
            return FALSE;
        }

        // Patch is active, so it must also be bound
        _ASSERTE( address != NULL );
        return TRUE;
    }

    bool IsFree()       {return (refCount == 0);}
    bool IsTriggering() {return (refCount > 1);}

    // Is this patch at a position at which it's safe to take a stack?
    bool IsSafeForStackTrace();

#ifndef FEATURE_EMULATE_SINGLESTEP
    // gets a pointer to the shared buffer
    SharedPatchBypassBuffer* GetOrCreateSharedPatchBypassBuffer();

    // entry point for general initialization when the controller is being created
    void Initialize()
    {
        m_pSharedPatchBypassBuffer = NULL;
    }

    // entry point for general cleanup when the controller is being removed from the patch table
    void DoCleanup()
    {
        if (m_pSharedPatchBypassBuffer != NULL)
            m_pSharedPatchBypassBuffer->Release();
    }
#endif // !FEATURE_EMULATE_SINGLESTEP

    void LogInstance()
    {
        LOG((LF_CORDB, LL_INFO10000, "  DCP: %p\n"
            "              patchId: 0x%zx\n"
            "               offset: 0x%zx\n"
            "              address: %p\n"
            "           offsetIsIL: %s\n"
            "             refCount: %d\n"
            "                 kind: %d\n"
            "              IsBound: %s\n"
            "        IsNativePatch: %s\n"
            "       IsManagedPatch: %s\n"
            "     IsILPrimaryPatch: %s\n"
            "     IsILReplicaPatch: %s\n",
            this, patchId, offset, address, (offsetIsIL ? "true" : "false"), refCount, GetKind(),
            (IsBound() ? "true" : "false"),
            (IsNativePatch() ? "true" : "false"),
            (IsManagedPatch() ? "true" : "false"),
            (IsILPrimaryPatch() ? "true" : "false"),
            (IsILReplicaPatch() ? "true" : "false")));
    }
};

typedef DPTR(DebuggerControllerPatch) PTR_DebuggerControllerPatch;

/* class DebuggerPatchTable:  This is the table that contains
 *  information about the patches (breakpoints) maintained by the
 *  debugger for a variety of purposes.
 *  The only tricky part is that
 *  patches can be hashed either by the address that they're applied to,
 *  or by DebuggerFunctionKey.  If address is equal to zero, then the
 *  patch is hashed by DebuggerFunctionKey.
 *
 *  Patch table inspection scheme:
 *
 *  We have to be able to inspect memory (read/write) from the right
 *  side w/o the help of the left side.  When we do unmanaged debugging,
 *  we need to be able to R/W memory out of a debuggee s.t. the debugger
 *  won't see our patches.  So we have to be able to read our patch table
 *  from the left side, which is problematic since we know that the left
 *  side will be arbitrarily frozen, but we don't know where.
 *
 *  So our scheme is this:
 *  we'll send a pointer to the g_patches table over in startup,
 *  and when we want to inspect it at runtime, we'll freeze the left side,
 *  then read-memory the "data" (m_pcEntries) array over to the right.  We'll
 *  iterate through the array & assume that anything with a non-zero opcode
 *  and address field is valid.  To ensure that the assumption is ok, we
 *  use the zeroing allocator which zeros out newly created space, and
 *  we'll be very careful about zeroing out the opcode field during the
 *  Unapply operation
 *
 * NOTE: Don't mess with the memory protections on this while the
 * left side is frozen (ie, no threads are executing).
 * WriteMemory depends on being able to write the patchtable back
 * if it was read successfully.
 */
#define DPT_INVALID_SLOT (UINT32_MAX)
#define DPT_DEFAULT_TRACE_TYPE TRACE_OTHER

/* Although CHashTableAndData can grow, we always use a fixed number of buckets.
 * This is problematic for tables like the patch table which are usually small, but
 * can become huge.  When the number of entries far exceeds the number of buckets,
 * lookup and addition basically degrade into linear searches.  There is a trade-off
 * here between wasting memory for unused buckets, and performance of large tables.
 * Also note that the number of buckets should be a prime number.
*/
#define DPT_HASH_BUCKETS 1103

class DebuggerPatchTable : private CHashTableAndData<CNewZeroData>
{
    VPTR_BASE_CONCRETE_VTABLE_CLASS(DebuggerPatchTable);

public:
    virtual ~DebuggerPatchTable() = default;

    friend class DebuggerRCThread;
private:
    //incremented so that we can get DPT-wide unique PIDs.
    // pid = Patch ID.
    SIZE_T m_patchId;
    // Given a patch, retrieves the correct key.  The return value of this function is passed to Cmp(), Find(), etc.
    SIZE_T Key(DebuggerControllerPatch *patch)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        // Most clients of CHashTable pass a host pointer as the key.   However, the key really could be
        // anything.  In our case, the key can either be a host pointer of type DebuggerFunctionKey or
        // the address of the patch.
        if (patch->address == NULL)
        {
            return (SIZE_T)(&patch->key);
        }
        else
        {
            return (SIZE_T)(dac_cast<TADDR>(patch->address));
        }
    }

    // Given two DebuggerControllerPatches, tells
    // whether they are equal or not.  Does this by comparing the correct
    // key.
    // BYTE* pc1:  If pc2 is hashed by address,
    //  pc1 is an address.  If
    //  pc2 is hashed by DebuggerFunctionKey,
    //  pc1 is a DebuggerFunctionKey
    //Returns true if the two patches are equal, false otherwise
    BOOL Cmp(SIZE_T k1, const HASHENTRY * pc2)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        DebuggerControllerPatch * pPatch2 = dac_cast<PTR_DebuggerControllerPatch>(const_cast<HASHENTRY *>(pc2));

        if (pPatch2->address == NULL)
        {
            // k1 is a host pointer of type DebuggerFunctionKey.
            DebuggerFunctionKey * pKey1 = reinterpret_cast<DebuggerFunctionKey *>(k1);

            return ((pKey1->module != pPatch2->key.module) || (pKey1->md != pPatch2->key.md));
        }
        else
        {
            return ((SIZE_T)(dac_cast<TADDR>(pPatch2->address)) != k1);
        }
    }

    //Computes a hash value based on an address
    ULONG HashAddress(PTR_CORDB_ADDRESS_TYPE address)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return (ULONG)(SIZE_T)(dac_cast<TADDR>(address));
    }

    //Computes a hash value based on a DebuggerFunctionKey
    ULONG HashKey(DebuggerFunctionKey * pKey)
    {
        SUPPORTS_DAC;
        return HashPtr(pKey->md, pKey->module);
    }

    //Computes a hash value from a patch, using the address field
    // if the patch is hashed by address, using the DebuggerFunctionKey
    // otherwise
    ULONG Hash(DebuggerControllerPatch * pPatch)
    {
        SUPPORTS_DAC;

        if (pPatch->address == NULL)
            return HashKey(&(pPatch->key));
        else
            return HashAddress(pPatch->address);
    }
    //Public Members
public:
    enum {
        DCP_PATCHID_INVALID,
        DCP_PATCHID_FIRST_VALID,
    };

#ifndef DACCESS_COMPILE

    DebuggerPatchTable() : CHashTableAndData<CNewZeroData>(DPT_HASH_BUCKETS) { }

    HRESULT Init()
    {
        WRAPPER_NO_CONTRACT;

        m_patchId = DCP_PATCHID_FIRST_VALID;

        SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;
        return NewInit(17, sizeof(DebuggerControllerPatch), 101);
    }

    // Assuming that the chain of patches (as defined by all the
    // GetNextPatch from this patch) are either sorted or NULL, take the given
    // patch (which should be the first patch in the chain).  This
    // is called by AddPatch to make sure that the order of the
    // patches is what we want for things like EnC, DePatchSkips,etc.
    void SortPatchIntoPatchList(DebuggerControllerPatch **ppPatch);

    void SpliceOutOfList(DebuggerControllerPatch *patch);

    void SpliceInBackOf(DebuggerControllerPatch *patchAppend,
                        DebuggerControllerPatch *patchEnd);

    //
    // Note that patches may be reallocated - do not keep a pointer to a patch.
    //
    DebuggerControllerPatch *AddPatchForMethodDef(DebuggerController *controller,
                                      Module *module,
                                      mdMethodDef md,
                                      MethodDesc *pMethodDescFilter,
                                      size_t offset,
                                      BOOL offsetIsIL,
                                      DebuggerPatchKind kind,
                                      FramePointer fp,
                                      AppDomain *pAppDomain,
                                      SIZE_T primaryEnCVersion,
                                      DebuggerJitInfo *dji);

    DebuggerControllerPatch *AddPatchForAddress(DebuggerController *controller,
                                      MethodDesc *fd,
                                      size_t offset,
                                      DebuggerPatchKind kind,
                                      CORDB_ADDRESS_TYPE *address,
                                      FramePointer fp,
                                      AppDomain *pAppDomain,
                                      DebuggerJitInfo *dji = NULL,
                                      SIZE_T patchId = DCP_PATCHID_INVALID,
                                      TraceType traceType = DPT_DEFAULT_TRACE_TYPE);

    // Set the native address for this patch.
    void BindPatch(DebuggerControllerPatch *patch, CORDB_ADDRESS_TYPE *address);
    void UnbindPatch(DebuggerControllerPatch *patch);
    void RemovePatch(DebuggerControllerPatch *patch);

    // This is a sad legacy workaround. The patch table (implemented as this
    // class) is shared across process. We publish the runtime offsets of
    // some key fields. Since those fields are private, we have to provide
    // accessors here. So if you're not using these functions, don't start.
    // We can hopefully remove them.
    static SIZE_T GetOffsetOfEntries()
    {
        // assert that we the offsets of these fields in the base class is
        // the same as the offset of this field in this class.
        _ASSERTE((void*)(DebuggerPatchTable*)NULL == (void*)(CHashTableAndData<CNewZeroData>*)NULL);
        return helper_GetOffsetOfEntries();
    }

    static SIZE_T GetOffsetOfCount()
    {
        _ASSERTE((void*)(DebuggerPatchTable*)NULL == (void*)(CHashTableAndData<CNewZeroData>*)NULL);
        return helper_GetOffsetOfCount();
    }

    // GetPatch  find the first  patch in the hash table
    //      that is hashed by matching the {Module,mdMethodDef} to the
    //      patch's DebuggerFunctionKey.  This will NOT find anything
    //      hashed by address, even if that address is within the
    //      method specified.
    // You can use GetNextPatch to iterate through all the patches keyed by
    // this Module,mdMethodDef pair
    DebuggerControllerPatch *GetPatch(Module *module, mdToken md)
    {
        DebuggerFunctionKey key;

        key.module = module;
        key.md = md;

        return reinterpret_cast<DebuggerControllerPatch *>(Find(HashKey(&key), (SIZE_T)&key));
    }
#endif // #ifndef DACCESS_COMPILE

    // GetPatch will translate find the first  patch in the hash
    //      table that is hashed by address.  It will NOT find anything hashed
    //      by {Module,mdMethodDef}, or by MethodDesc.
    DebuggerControllerPatch * GetPatch(PTR_CORDB_ADDRESS_TYPE address)
    {
        SUPPORTS_DAC;
        ARM_ONLY(_ASSERTE(dac_cast<TADDR>(address) & THUMB_CODE));

        DebuggerControllerPatch * pPatch =
            dac_cast<PTR_DebuggerControllerPatch>(Find(HashAddress(address), (SIZE_T)(dac_cast<TADDR>(address))));

        return pPatch;
    }

    DebuggerControllerPatch *GetNextPatch(DebuggerControllerPatch *prev);

    // Find the first patch in the patch table, and store
    //      index info in info.  Along with GetNextPatch, this can
    //      iterate through the whole patch table.  Note that since the
    //      hashtable operates via iterating through all the contents
    //      of all the buckets, if you add an entry while iterating
    //      through the table, you  may or may not iterate across
    //      the new entries.  You will iterate through all the entries
    //      that were present at the beginning of the run.  You
    //      safely delete anything you've already iterated by, anything
    //      else is kinda risky.
    DebuggerControllerPatch * GetFirstPatch(HASHFIND * pInfo)
    {
        SUPPORTS_DAC;

        return dac_cast<PTR_DebuggerControllerPatch>(FindFirstEntry(pInfo));
    }

    // Along with GetFirstPatch, this can iterate through
    //      the whole patch table.  See GetFirstPatch for more info
    //      on the rules of iterating through the table.
    DebuggerControllerPatch * GetNextPatch(HASHFIND * pInfo)
    {
        SUPPORTS_DAC;

        return dac_cast<PTR_DebuggerControllerPatch>(FindNextEntry(pInfo));
    }

    // Used by DebuggerController to translate an index
    //      of a patch into a direct pointer.
    inline HASHENTRY * GetEntryPtr(ULONG iEntry)
    {
        SUPPORTS_DAC;

        return EntryPtr(iEntry);
    }

    // Used by DebuggerController to grab indices of patches
    //      rather than holding direct pointers to them.
    inline ULONG GetItemIndex(HASHENTRY * p)
    {
        SUPPORTS_DAC;

        return ItemIndex(p);
    }

#ifdef _DEBUG
public:
    // DEBUG An internal debugging routine, it iterates
    //      through the hashtable, stopping at every
    //      single entry, no matter what it's state.
    void CheckPatchTable();
#endif // _DEBUG

    // Count how many patches are in the table.
    // Use for asserts
    int GetNumberOfPatches();
};

typedef VPTR(class DebuggerPatchTable) PTR_DebuggerPatchTable;


#if !defined(DACCESS_COMPILE)

// DebuggerControllerPage|Will eventually be used for
//      'break when modified' behaviour'
typedef struct DebuggerControllerPage
{
    DebuggerControllerPage  *next;
    const BYTE              *start, *end;
    DebuggerController      *controller;
    bool                     readable;
} DebuggerControllerPage;

// DEBUGGER_CONTROLLER_TYPE:  Identifies the type of the controller.
// It exists b/c we have RTTI turned off.
// Note that the order of these is important - SortPatchIntoPatchList
// relies on this ordering.
//
// DEBUGGER_CONTROLLER_STATIC|Base class response.  Should never be
//      seen, since we shouldn't be asking the base class about this.
// DEBUGGER_CONTROLLER_BREAKPOINT|DebuggerBreakpoint
// DEBUGGER_CONTROLLER_STEPPER|DebuggerStepper
// DEBUGGER_CONTROLLER_THREAD_STARTER|DebuggerThreadStarter
// DEBUGGER_CONTROLLER_ENC|DebuggerEnCBreakpoint
// DEBUGGER_CONTROLLER_PATCH_SKIP|DebuggerPatchSkip
// DEBUGGER_CONTROLLER_JMC_STEPPER|DebuggerJMCStepper - steps through Just-My-Code
// DEBUGGER_CONTROLLER_CONTINUABLE_EXCEPTION|DebuggerContinuableExceptionBreakpoint
enum DEBUGGER_CONTROLLER_TYPE
{
    DEBUGGER_CONTROLLER_THREAD_STARTER,
    DEBUGGER_CONTROLLER_ENC,
    DEBUGGER_CONTROLLER_PATCH_SKIP,
    DEBUGGER_CONTROLLER_BREAKPOINT,
    DEBUGGER_CONTROLLER_STEPPER,
    DEBUGGER_CONTROLLER_FUNC_EVAL_COMPLETE,
    DEBUGGER_CONTROLLER_USER_BREAKPOINT,  // UserBreakpoints are used  by Runtime threads to
                                          // send that they've hit a user breakpoint to the Right Side.
    DEBUGGER_CONTROLLER_JMC_STEPPER,      // Stepper that only stops in JMC-functions.
    DEBUGGER_CONTROLLER_CONTINUABLE_EXCEPTION,
    DEBUGGER_CONTROLLER_DATA_BREAKPOINT,
    DEBUGGER_CONTROLLER_STATIC,
};

enum TP_RESULT
{
    TPR_TRIGGER,            // This controller wants to SendEvent
    TPR_IGNORE,             // This controller doesn't want to SendEvent
    TPR_TRIGGER_ONLY_THIS,  // This, and only this controller, should be triggered.
                            // Right now, only the DebuggerEnCRemap controller
                            // returns this, the remap patch should be the first
                            // patch in the list.
    TPR_TRIGGER_ONLY_THIS_AND_LOOP,
                            // This, and only this controller, should be triggered.
                            // Right now, only the DebuggerEnCRemap controller
                            // returns this, the remap patch should be the first
                            // patch in the list.
                            // After triggering this, DPOSS should skip the
                            // ActivatePatchSkip call, so we hit the other
                            // breakpoints at this location.
    TPR_IGNORE_AND_STOP,    // Don't SendEvent, and stop asking other
                            // controllers if they want to.
                            // Service any previous triggered controllers.
};

enum SCAN_TRIGGER
{
    ST_PATCH        = 0x1,  // Only look for patches
    ST_SINGLE_STEP  = 0x2,  // Look for patches, and single-steps.
} ;

enum TRIGGER_WHY
{
    TY_NORMAL       = 0x0,
    TY_SHORT_CIRCUIT= 0x1,  // EnC short circuit - see DispatchPatchOrSingleStep
} ;

// the return value for DebuggerController::DispatchPatchOrSingleStep
enum DPOSS_ACTION
{
    // the following enum has been carefully ordered to optimize the helper
    // functions below. Do not re-order them w/o changing the helper funcs.
    DPOSS_INVALID   = 0x0,          // invalid action value
    DPOSS_DONT_CARE = 0x1,          // don't care about this exception
    DPOSS_USED_WITH_NO_EVENT = 0x2, // Care about this exception but won't send event to RS
    DPOSS_USED_WITH_EVENT = 0x3,    // Care about this exception and will send event to RS
};

// helper function
inline bool IsInUsedAction(DPOSS_ACTION action)
{
    _ASSERTE(action != DPOSS_INVALID);
    return (action >= DPOSS_USED_WITH_NO_EVENT);
}

inline void VerifyExecutableAddress(const BYTE* address)
{
// TODO: : when can we apply this to x86?
#if defined(HOST_64BIT)
#if defined(_DEBUG)
#ifndef TARGET_UNIX
    MEMORY_BASIC_INFORMATION mbi;

    if (sizeof(mbi) == ClrVirtualQuery(address, &mbi, sizeof(mbi)))
    {
        if (!(mbi.State & MEM_COMMIT))
        {
            STRESS_LOG1(LF_GCROOTS, LL_ERROR, "VerifyExecutableAddress: address is uncommitted memory, address=0x%p", address);
            CONSISTENCY_CHECK_MSGF((mbi.State & MEM_COMMIT), ("VEA: address (0x%p) is uncommitted memory.", address));
        }

        if (!(mbi.Protect & (PAGE_EXECUTE | PAGE_EXECUTE_READ | PAGE_EXECUTE_READWRITE | PAGE_EXECUTE_WRITECOPY)))
        {
            STRESS_LOG1(LF_GCROOTS, LL_ERROR, "VerifyExecutableAddress: address is not executable, address=0x%p", address);
            CONSISTENCY_CHECK_MSGF((mbi.Protect & (PAGE_EXECUTE | PAGE_EXECUTE_READ | PAGE_EXECUTE_READWRITE | PAGE_EXECUTE_WRITECOPY)),
                ("VEA: address (0x%p) is not on an executable page.", address));
        }
    }
#endif // !TARGET_UNIX
#endif // _DEBUG
#endif // HOST_64BIT
}

#endif // !DACCESS_COMPILE


// DebuggerController:   DebuggerController serves
// both as a static class that dispatches exceptions coming from the
// EE, and as an abstract base class for other debugger concepts.
class DebuggerController
{
    VPTR_BASE_CONCRETE_VTABLE_CLASS(DebuggerController);

#if !defined(DACCESS_COMPILE)

    // Needs friendship for lock because of EnC locking workarounds.
    friend class DebuggerEnCBreakpoint;

    friend class DebuggerPatchSkip;
    friend class DebuggerRCThread; //so we can get offsets of fields the
    //right side needs to read
    friend class Debugger; // So Debugger can lock, use, unlock the patch
    // table in MapAndBindFunctionBreakpoints
    friend void Debugger::UnloadModule(Module* pRuntimeModule, AppDomain *pAppDomain);

    //
    // Static functionality
    //

  public:
    class ControllerLockHolder : public CrstHolder
    {
    public:
        ControllerLockHolder() : CrstHolder(&g_criticalSection) { WRAPPER_NO_CONTRACT; }
    };

    static HRESULT Initialize();

    // Remove and cleanup all DebuggerControllers for detach
    static void DeleteAllControllers();

    //
    // global event dispatching functionality
    //


    // Controllers are notified when they enter/exit func-evals (on their same thread,
    // on any any thread if the controller doesn't have a thread).
    // The original use for this was to allow steppers to skip through func-evals.
    // thread - the thread doing the funceval.
    static void DispatchFuncEvalEnter(Thread * thread);
    static void DispatchFuncEvalExit(Thread * thread);

    static bool DispatchNativeException(EXCEPTION_RECORD *exception,
                                        CONTEXT *context,
                                        DWORD code,
                                        Thread *thread);

    static bool DispatchUnwind(Thread *thread,
                               MethodDesc *fd, DebuggerJitInfo * pDJI, SIZE_T offset,
                               FramePointer handlerFP,
                               CorDebugStepReason unwindReason);

    static bool DispatchTraceCall(Thread *thread,
                                  const BYTE *address);

    static PRD_TYPE GetPatchedOpcode(CORDB_ADDRESS_TYPE *address);

    static BOOL CheckGetPatchedOpcode(CORDB_ADDRESS_TYPE *address, /*OUT*/ PRD_TYPE *pOpcode);

    // pIP is the ip right after the prolog of the method we've entered.
    // fp is the frame pointer for that method.
    static void DispatchMethodEnter(void * pIP, FramePointer fp);


    // Delete any patches that exist for a specific module and optionally a specific AppDomain.
    // If pAppDomain is specified, then only patches tied to the specified AppDomain are
    // removed.  If pAppDomain is null, then all patches for the module are removed.
     static void RemovePatchesFromModule( Module* pModule, AppDomain* pAppdomain );

    // Check whether there are any pathces in the patch table for the specified module.
    static bool ModuleHasPatches( Module* pModule );

#if FEATURE_METADATA_UPDATER
    static DebuggerControllerPatch *GetEnCPatch(const BYTE *address);
#endif //FEATURE_METADATA_UPDATER

    static DPOSS_ACTION ScanForTriggers(CORDB_ADDRESS_TYPE *address,
                                Thread *thread,
                                CONTEXT *context,
                                DebuggerControllerQueue *pDcq,
                                SCAN_TRIGGER stWhat,
                                TP_RESULT *pTpr);


    static DebuggerPatchSkip *ActivatePatchSkip(Thread *thread,
                                                const BYTE *eip,
                                                BOOL fForEnC);


    static DPOSS_ACTION DispatchPatchOrSingleStep(Thread *thread,
                                          CONTEXT *context,
                                          CORDB_ADDRESS_TYPE *ip,
                                          SCAN_TRIGGER which);


    static int GetNumberOfPatches()
    {
        if (g_patches == NULL)
            return 0;

        return g_patches->GetNumberOfPatches();
    }

    static int GetTotalMethodEnter() {LIMITED_METHOD_CONTRACT;  return g_cTotalMethodEnter; }

#if defined(_DEBUG)
    // Debug check that we only have 1 thread-starter per thread.
    // Check this new one against all existing ones.
    static void EnsureUniqueThreadStarter(DebuggerThreadStarter * pNew);
#endif
    // If we have a thread-starter on the given EE thread, make sure it's cancel.
    // Thread-Starters normally delete themselves when they fire. But if the EE
    // destroys the thread before it fires, then we'd still have an active DTS.
    static void CancelOutstandingThreadStarter(Thread * pThread);

    static void AddRefPatch(DebuggerControllerPatch *patch);
    static void ReleasePatch(DebuggerControllerPatch *patch);

  private:

    static bool MatchPatch(Thread *thread, CONTEXT *context,
                           DebuggerControllerPatch *patch);

    // Returns TRUE if we should continue to dispatch after this exception
    // hook.
    static BOOL DispatchExceptionHook(Thread *thread, CONTEXT *context,
                                      EXCEPTION_RECORD *exception);

protected:
#ifdef _DEBUG
    static bool HasLock()
    {
       return g_criticalSection.OwnedByCurrentThread() != 0;
    }
#endif

#endif // !DACCESS_COMPILE

private:
    SPTR_DECL(DebuggerPatchTable, g_patches);
    SVAL_DECL(BOOL, g_patchTableValid);

#if !defined(DACCESS_COMPILE)

private:
    static DebuggerControllerPage *g_protections;
    static DebuggerController *g_controllers;

    // This is the "Controller" lock. It synchronizes the controller infrastructure.
    // It is smaller than the debugger lock, but larger than the debugger-data lock.
    // It needs to be taken in execution-control related callbacks; and will also call
    // back into the EE when held (most notably for the stub-managers; but also for various
    // query operations).
    static CrstStatic g_criticalSection;

    // Write is protected by both Debugger + Controller Lock
    static int g_cTotalMethodEnter;

    static bool BindPatch(DebuggerControllerPatch *patch,
                          MethodDesc *fd,
                          CORDB_ADDRESS_TYPE *startAddr);
    static bool ApplyPatch(DebuggerControllerPatch *patch);
    static bool UnapplyPatch(DebuggerControllerPatch *patch);
    static bool IsPatched(CORDB_ADDRESS_TYPE *address, BOOL native);

    static void ActivatePatch(DebuggerControllerPatch *patch);
    static void DeactivatePatch(DebuggerControllerPatch *patch);

    static void ApplyTraceFlag(Thread *thread);
    static void UnapplyTraceFlag(Thread *thread);

    virtual void DebuggerDetachClean();

  public:
    static const BYTE *GetILPrestubDestination(const BYTE *prestub);
    static const BYTE *GetILFunctionCode(MethodDesc *fd);

    //
    // Non-static functionality
    //

  public:

    DebuggerController(Thread * pThread, AppDomain * pAppDomain);
    virtual ~DebuggerController();
    void Delete();
    bool IsDeleted() { return m_deleted; }

#endif // !DACCESS_COMPILE


    // Return the pointer g_patches.
    // Access to patch table for the RC threads (EE,DI)
    // Why: The right side needs to know the address of the patch
    // table (which never changes after it gets created) so that ReadMemory,
    // WriteMemory can work from out-of-process.  This should only be used in
    // when the Runtime Controller is starting up, and not thereafter.
    // How:return g_patches;
public:
    static DebuggerPatchTable * GetPatchTable() {LIMITED_METHOD_DAC_CONTRACT;  return g_patches; }
    static BOOL  GetPatchTableValid() {LIMITED_METHOD_DAC_CONTRACT;  return g_patchTableValid; }

#if !defined(DACCESS_COMPILE)
    static BOOL *GetPatchTableValidAddr() {LIMITED_METHOD_CONTRACT;  return &g_patchTableValid; }

    // Is there a patch at addr?
    // We sometimes want to use this version of the method
    // (as opposed to IsPatched) because there is
    // a race condition wherein a patch can be added to the table, we can
    // ask about it, and then we can actually apply the patch.
    // How: If the patch table contains a patch at that address, there
    // is.
    static bool IsAddressPatched(CORDB_ADDRESS_TYPE *address)
    {
        return (g_patches->GetPatch(address) != NULL);
    }

    //
    // Event setup
    //

    Thread *GetThread() { return m_thread; }

    // The next two are very similar.  Both work on offsets,
    // but one takes a "patch id".  I don't think these are really needed: the
    // controller itself can act as the id of the patch.
    BOOL AddBindAndActivateNativeManagedPatch(
                  MethodDesc * fd,
                  DebuggerJitInfo *dji,
                  SIZE_T offset,
                  FramePointer fp,
                  AppDomain *pAppDomain);

    // Add a patch at the start of a not-yet-jitted method.
    void AddPatchToStartOfLatestMethod(MethodDesc * fd);


    // This version is particularly useful b/c it doesn't assume that the
    // patch is inside a managed method.
    DebuggerControllerPatch *AddAndActivateNativePatchForAddress(CORDB_ADDRESS_TYPE *address,
                                      FramePointer fp,
                                      bool managed,
                                      TraceType traceType);



    bool PatchTrace(TraceDestination *trace, FramePointer fp, bool fStopInUnmanaged);

    void AddProtection(const BYTE *start, const BYTE *end, bool readable);
    void RemoveProtection(const BYTE *start, const BYTE *end, bool readable);

    static BOOL IsSingleStepEnabled(Thread *pThread);
    bool IsSingleStepEnabled();
    void EnableSingleStep();
    static void EnableSingleStep(Thread *pThread);

    void DisableSingleStep();

    void EnableExceptionHook();
    void DisableExceptionHook();

    void         EnableUnwind(FramePointer frame);
    void         DisableUnwind();
    FramePointer GetUnwind();

    void EnableTraceCall(FramePointer fp);
    void DisableTraceCall();

    bool IsMethodEnterEnabled();
    void EnableMethodEnter();
    void DisableMethodEnter();

    void DisableAll();

    virtual DEBUGGER_CONTROLLER_TYPE GetDCType( void )
        { return DEBUGGER_CONTROLLER_STATIC; }

    // Return true iff this is one of the stepper types.
    // if true, we can safely cast this controller to a DebuggerStepper*.
    inline bool IsStepperDCType()
    {
        DEBUGGER_CONTROLLER_TYPE e = this->GetDCType();
        return (e == DEBUGGER_CONTROLLER_STEPPER) || (e == DEBUGGER_CONTROLLER_JMC_STEPPER);
    }

    void Enqueue();
    void Dequeue();

  protected:
    // Helper function that is called on each virtual trace call target to set a trace patch
    static void PatchTargetVisitor(TADDR pVirtualTraceCallTarget, VOID* pUserData);

    DebuggerControllerPatch *AddILPrimaryPatch(Module *module,
                  mdMethodDef md,
                  MethodDesc *pMethodDescFilter,
                  SIZE_T offset,
                  BOOL offsetIsIL,
                  SIZE_T encVersion);

    BOOL AddBindAndActivateILReplicaPatch(DebuggerControllerPatch *primary,
                                        DebuggerJitInfo *dji);

    BOOL AddILPatch(AppDomain * pAppDomain, Module *module,
                    mdMethodDef md,
                    MethodDesc* pMethodFilter,
                    SIZE_T encVersion,  // what encVersion does this apply to?
                    SIZE_T offset,
                    BOOL offsetIsIL);

    BOOL AddBindAndActivatePatchForMethodDesc(MethodDesc *fd,
                  DebuggerJitInfo *dji,
                  SIZE_T nativeOffset,
                  DebuggerPatchKind kind,
                  FramePointer fp,
                  AppDomain *pAppDomain);

  protected:

    //
    // Target event handlers
    //


    // Notify a controller that a func-eval is starting/ending on the given thread.
    // If a controller's m_thread!=NULL, then it is only notified of func-evals on
    // its thread.
    // Controllers don't need to Enable anything to get this, and most controllers
    // can ignore it.
    virtual void TriggerFuncEvalEnter(Thread * thread);
    virtual void TriggerFuncEvalExit(Thread * thread);

    virtual TP_RESULT TriggerPatch(DebuggerControllerPatch *patch,
                              Thread *thread,
                              TRIGGER_WHY tyWhy);

    // Dispatched when we get a SingleStep exception on this thread.
    // Return true if we want SendEvent to get called.

    virtual bool TriggerSingleStep(Thread *thread, const BYTE *ip);


    // Dispatched to notify the controller when we are going to a filter/handler
    // that's in the stepper's current frame or above (a caller frame).
    // 'desc' & 'offset' are the location of the filter/handler (ie, this is where
    // execution will continue)
    // 'frame' points into the stack at the return address for the function w/ the handler.
    // If (frame > m_unwindFP) then the filter/handler is in a caller, else
    // it's in the same function as the current stepper (It's not in a child because
    // we don't dispatch in that case).
    virtual void TriggerUnwind(Thread *thread, MethodDesc *fd, DebuggerJitInfo * pDJI,
                               SIZE_T offset, FramePointer fp,
                               CorDebugStepReason unwindReason);

    virtual void TriggerTraceCall(Thread *thread, const BYTE *ip);
    virtual TP_RESULT TriggerExceptionHook(Thread *thread, CONTEXT * pContext,
                                      EXCEPTION_RECORD *exception);

    // Trigger when we've entered a method
    // thread - current thread
    // desc - the method that we've entered
    // ip - the address after the prolog. A controller can patch this address.
    //      To stop in this method.
    // Returns true if the trigger will disable itself from further method entry
    //      triggers else returns false (passing through a cctor can cause this).
    // A controller can't block in this trigger! It can only update state / set patches
    // and then return.
    virtual void TriggerMethodEnter(Thread * thread,
                                    DebuggerJitInfo *dji,
                                    const BYTE * ip,
                                    FramePointer fp);


    // Send the managed debug event.
    // This is called after TriggerPatch/TriggerSingleStep actually trigger.
    // Note this can have a strange interaction with SetIp. Specifically this thread:
    // 1) may call TriggerXYZ which queues the controller for send event.
    // 2) blocks on a the debugger lock (in which case SetIp may get invoked on it)
    // 3) then sends the event
    // If SetIp gets invoked at step 2, the thread's IP may have changed such that it should no
    // longer trigger. Eg, perhaps we were about to send a breakpoint, and then SetIp moved us off
    // the bp. So we pass in an extra flag, fInterruptedBySetIp,  to let the controller decide how to handle this.
    // Since SetIP only works within a single function, this can only be an issue if a thread's current stopping
    // location and the patch it set are in the same function. (So this could happen for step-over, but never
    // step-out).
    // This flag will almost always be false.
    //
    // Once we actually send the event, we're under the debugger lock, and so the world is stable underneath us.
    // But the world may change underneath a thread between when SendEvent gets queued and by the time it's actually called.
    // So SendIPCEvent may need to do some last-minute sanity checking (like the SetIP case) to ensure it should
    // still send.
    //
    // Returns true if send an event, false elsewise.
    virtual bool SendEvent(Thread *thread, bool fInterruptedBySetIp);

    AppDomain           *m_pAppDomain;

  private:

    Thread             *m_thread;
    DebuggerController *m_next;
    bool                m_singleStep;
    bool                m_exceptionHook;
    bool                m_traceCall;
protected:
    FramePointer        m_traceCallFP;
private:
    FramePointer        m_unwindFP;
    int                 m_eventQueuedCount;
    bool                m_deleted;
    bool                m_fEnableMethodEnter;

#endif // !DACCESS_COMPILE
};


#if !defined(DACCESS_COMPILE)

/* ------------------------------------------------------------------------- *
 * DebuggerPatchSkip routines
 * ------------------------------------------------------------------------- */

class DebuggerPatchSkip : public DebuggerController
{
    friend class DebuggerController;

    DebuggerPatchSkip(Thread *thread,
                      DebuggerControllerPatch *patch,
                      AppDomain *pAppDomain);

    ~DebuggerPatchSkip();

    bool TriggerSingleStep(Thread *thread,
                           const BYTE *ip);

    TP_RESULT TriggerExceptionHook(Thread *thread, CONTEXT * pContext,
                              EXCEPTION_RECORD *exception);

    TP_RESULT TriggerPatch(DebuggerControllerPatch *patch,
                              Thread *thread,
                              TRIGGER_WHY tyWhy);

    virtual DEBUGGER_CONTROLLER_TYPE GetDCType(void)
        { return DEBUGGER_CONTROLLER_PATCH_SKIP; }

    void CopyInstructionBlock(BYTE *to, const BYTE* from);

    void DecodeInstruction(CORDB_ADDRESS_TYPE *code);

    void DebuggerDetachClean();

    CORDB_ADDRESS_TYPE      *m_address;
    int                      m_iOrigDisp;        // the original displacement of a relative call or jump
    InstructionAttribute     m_instrAttrib;      // info about the instruction being skipped over
#ifndef FEATURE_EMULATE_SINGLESTEP
    // this is shared among all the skippers and the controller. see the comments
    // right before the definition of SharedPatchBypassBuffer for lifetime info.
    SharedPatchBypassBuffer *m_pSharedPatchBypassBuffer;

public:
    CORDB_ADDRESS_TYPE *GetBypassAddress()
    {
        _ASSERTE(m_pSharedPatchBypassBuffer);
        BYTE* patchBypass = m_pSharedPatchBypassBuffer->PatchBypass;
        return (CORDB_ADDRESS_TYPE *)patchBypass;
    }
#endif // !FEATURE_EMULATE_SINGLESTEP
};

/* ------------------------------------------------------------------------- *
 * DebuggerBreakpoint routines
 * ------------------------------------------------------------------------- */

// DebuggerBreakpoint:
// DBp represents a user-placed breakpoint, and when Triggered, will
// always want to be activated, whereupon it will inform the right side of
// being hit.
class DebuggerBreakpoint : public DebuggerController
{
public:
    DebuggerBreakpoint(Module *module,
                       mdMethodDef md,
                       AppDomain *pAppDomain,
                       SIZE_T m_offset,
                       bool m_native,
                       SIZE_T ilEnCVersion,  // must give the EnC version for non-native bps
                       MethodDesc *nativeMethodDesc,  // must be non-null when m_native, null otherwise
                       DebuggerJitInfo *nativeJITInfo,  // optional when m_native, null otherwise
                       bool nativeCodeBindAllVersions,
                       BOOL *pSucceed
                       );

    virtual DEBUGGER_CONTROLLER_TYPE GetDCType( void )
        { return DEBUGGER_CONTROLLER_BREAKPOINT; }

private:

    TP_RESULT TriggerPatch(DebuggerControllerPatch *patch,
                      Thread *thread,
                      TRIGGER_WHY tyWhy);
    bool SendEvent(Thread *thread, bool fInterruptedBySetIp);
};

// * ------------------------------------------------------------------------ *
// * DebuggerStepper routines
// * ------------------------------------------------------------------------ *
//

//  DebuggerStepper:  This subclass of DebuggerController will
//  be instantiated to create a "Step" operation, meaning that execution
//  should continue until a range of IL code is exited.
class DebuggerStepper : public DebuggerController
{
public:
    DebuggerStepper(Thread *thread,
                    CorDebugUnmappedStop rgfMappingStop,
                    CorDebugIntercept interceptStop,
                    AppDomain *appDomain);
    ~DebuggerStepper();

    bool Step(FramePointer fp, bool in,
              COR_DEBUG_STEP_RANGE *range, SIZE_T cRange, bool rangeIL);
    void StepOut(FramePointer fp, StackTraceTicket ticket);

    virtual DEBUGGER_CONTROLLER_TYPE GetDCType( void )
        { return DEBUGGER_CONTROLLER_STEPPER; }

    //MoveToCurrentVersion makes sure that the stepper is prepared to
    //  operate within the version of the code specified by djiNew.
    //  Currently, this means to map the ranges into the ranges of the djiNew.
    //  Idempotent.
    void MoveToCurrentVersion( DebuggerJitInfo *djiNew);

    // Public & Polymorphic on flavor (traditional vs. JMC).

    // Regular steppers want to EnableTraceCall; and JMC-steppers want to EnableMethodEnter.
    // (They're very related - they both stop at the next "interesting" managed code run).
    // So we just gloss over the difference w/ some polymorphism.
    virtual void EnablePolyTraceCall();

protected:
    // Steppers override these so that they can skip func-evals.
    void TriggerFuncEvalEnter(Thread * thread);
    void TriggerFuncEvalExit(Thread * thread);

    bool TrapStepInto(ControllerStackInfo *info,
                      const BYTE *ip,
                      TraceDestination *pTD);

    bool TrapStep(ControllerStackInfo *info, bool in);

    // @todo - must remove that fForceTraditional flag. Need a way for a JMC stepper
    // to do a Trad step out.
    void TrapStepOut(ControllerStackInfo *info, bool fForceTraditional = false);

    // Polymorphic on flavor (Traditional vs. Just-My-Code)
    virtual void TrapStepNext(ControllerStackInfo *info);
    virtual bool TrapStepInHelper(ControllerStackInfo * pInfo,
                                  const BYTE * ipCallTarget,
                                  const BYTE * ipNext,
                                  bool fCallingIntoFunclet,
                                  bool fIsJump);
    virtual bool IsInterestingFrame(FrameInfo * pFrame);
    virtual bool DetectHandleNonUserCode(ControllerStackInfo *info, DebuggerMethodInfo * pInfo);


    //DetectHandleInterceptors will figure out if the current
    // frame is inside an interceptor, and if we're not interested in that
    // interceptor, it will set a breakpoint outside it so that we can
    // run to after the interceptor.
    virtual bool DetectHandleInterceptors(ControllerStackInfo *info);

    // This function checks whether the given IP is in an LCG method.  If so, it enables
    // JMC and does a step out.  This effectively makes sure that we never stop in an LCG method.
    BOOL DetectHandleLCGMethods(const PCODE ip, MethodDesc * pMD, ControllerStackInfo * pInfo);

    bool IsAddrWithinFrame(DebuggerJitInfo *dji,
                           MethodDesc* pMD,
                           const BYTE* currentAddr,
                           const BYTE* targetAddr);

    // x86 shouldn't need to call this method directly.
    // We should call IsAddrWithinFrame() on x86 instead.
    // That's why I use a name with the word "funclet" in it to scare people off.
    bool IsAddrWithinMethodIncludingFunclet(DebuggerJitInfo *dji,
                                            MethodDesc* pMD,
                                            const BYTE* targetAddr);

    //ShouldContinue returns false if the DebuggerStepper should stop
    // execution and inform the right side.  Returns true if the next
    // breakpointexecution should be set, and execution allowed to continue
    bool ShouldContinueStep( ControllerStackInfo *info, SIZE_T nativeOffset );

    //IsInRange returns true if the given IL offset is inside of
    // any of the COR_DEBUG_STEP_RANGE structures given by range.
    bool IsInRange(SIZE_T offset, COR_DEBUG_STEP_RANGE *range, SIZE_T rangeCount,
                   ControllerStackInfo *pInfo = NULL);
    bool IsRangeAppropriate(ControllerStackInfo *info);



    TP_RESULT TriggerPatch(DebuggerControllerPatch *patch,
                      Thread *thread,
                      TRIGGER_WHY tyWhy);
    bool TriggerSingleStep(Thread *thread, const BYTE *ip);
    void TriggerUnwind(Thread *thread, MethodDesc *fd, DebuggerJitInfo * pDJI,
                      SIZE_T offset, FramePointer fp,
                      CorDebugStepReason unwindReason);
    void TriggerTraceCall(Thread *thread, const BYTE *ip);
    bool SendEvent(Thread *thread, bool fInterruptedBySetIp);


    virtual void TriggerMethodEnter(Thread * thread, DebuggerJitInfo * dji, const BYTE * ip, FramePointer fp);


    void ResetRange();

    //  Given a set of IL ranges, convert them to native and cache them.
    bool SetRangesFromIL(DebuggerJitInfo * dji, COR_DEBUG_STEP_RANGE *ranges, SIZE_T rangeCount);

    // Return true if this stepper is alive, but frozen. (we freeze when the stepper
    // enters a nested func-eval).
    bool IsFrozen();

    // Returns true if this stepper is 'dead' - which happens if a non-frozen stepper
    // gets a func-eval exit.
    bool IsDead();

    // Prepare for sending an event.
    void PrepareForSendEvent(StackTraceTicket ticket);

protected:
    bool                    m_stepIn;
    CorDebugStepReason      m_reason; // Why did we stop?
    FramePointer            m_fpStepInto; // if we get a trace call
                                //callback, we may end up completing
                                // a step into.  If fp is less than th is
                                // when we stop,
                                // then we're actually in a STEP_CALL

    CorDebugIntercept       m_rgfInterceptStop; // If we hit a
    // frame that's an interceptor (internal or otherwise), should we stop?

    CorDebugUnmappedStop    m_rgfMappingStop; // If we hit a frame
    // that's at an interesting mapping point (prolog, epilog,etc), should
    // we stop?

    COR_DEBUG_STEP_RANGE *  m_range; // Ranges for active steppers are always in native offsets.

    SIZE_T                  m_rangeCount;
    SIZE_T                  m_realRangeCount; // @todo - delete b/c only used for CodePitching & Old-Enc

    // The original step intention.
    // As the stepper moves through code, it may change its other members.
    // ranges may get deleted, m_stepIn may get toggled, etc.
    // So we can't recover the original step direction from our other fields.
    // We need to know the original direction (as well as m_fp) so we know
    // if the frame we want to stop in is valid.
    //
    // Note that we can't really tell this by looking at our other state variables.
    // For example, a single-instruction step looks like a step-over.
    enum EStepMode
    {
        cStepOver,  // Stop in level above or at m_fp.
        cStepIn,    // Stop in level  above, below, or at m_fp.
        cStepOut    // Only stop in level above m_fp
    } m_eMode;

    // The frame that the stepper was originally created in.
    // This is the only frame that the ranges are valid in.
    FramePointer            m_fp;

#if defined(FEATURE_EH_FUNCLETS)
    // This frame pointer is used for funclet stepping.
    // See IsRangeAppropriate() for more information.
    FramePointer            m_fpParentMethod;
#endif // FEATURE_EH_FUNCLETS

    //m_fpException is 0 if we haven't stepped into an exception,
    //  and is ignored.  If we get a TriggerUnwind while mid-step, we note
    //  the value of frame here, and use that to figure out if we should stop.
    FramePointer            m_fpException;
    MethodDesc *            m_fdException;

    // Counter of FuncEvalEnter/Exits - used to determine if we're entering / exiting
    // a func-eval.
    int                     m_cFuncEvalNesting;

    // To freeze a stepper, we disable all triggers. We have to remember that so that
    // we can reenable them on Thaw.
    DWORD                   m_bvFrozenTriggers;

    // Values to use in m_bvFrozenTriggers.
    enum ETriggers
    {
        kSingleStep  = 0x1,
        kMethodEnter = 0x2,
    };


    void EnableJMCBackStop(MethodDesc * pStartMethod);

#ifdef _DEBUG
    // MethodDesc that the Stepin started in.
    // This is used for the JMC-backstop.
    MethodDesc * m_StepInStartMethod;

    // This flag is to ensure that PrepareForSendEvent is called before SendEvent.
    bool                    m_fReadyToSend;
#endif
};



/* ------------------------------------------------------------------------- *
 * DebuggerJMCStepper routines
 * ------------------------------------------------------------------------- */
class DebuggerJMCStepper : public DebuggerStepper
{
public:
    DebuggerJMCStepper(Thread *thread,
                    CorDebugUnmappedStop rgfMappingStop,
                    CorDebugIntercept interceptStop,
                    AppDomain *appDomain);
    ~DebuggerJMCStepper();

    virtual DEBUGGER_CONTROLLER_TYPE GetDCType( void )
        { return DEBUGGER_CONTROLLER_JMC_STEPPER; }

    virtual void EnablePolyTraceCall();
protected:
    virtual void TrapStepNext(ControllerStackInfo *info);
    virtual bool TrapStepInHelper(ControllerStackInfo * pInfo,
                                  const BYTE * ipCallTarget,
                                  const BYTE * ipNext,
                                  bool fCallingIntoFunclet,
                                  bool fIsJump);
    virtual bool IsInterestingFrame(FrameInfo * pFrame);
    virtual void TriggerMethodEnter(Thread * thread, DebuggerJitInfo * dji, const BYTE * ip, FramePointer fp);
    virtual bool DetectHandleNonUserCode(ControllerStackInfo *info, DebuggerMethodInfo * pInfo);
    virtual bool DetectHandleInterceptors(ControllerStackInfo *info);


private:

};


/* ------------------------------------------------------------------------- *
 * DebuggerThreadStarter routines
 * ------------------------------------------------------------------------- */
// DebuggerThreadStarter:  Once triggered, it sends the thread attach
// message to the right side (where the CreateThread managed callback
// gets called).  It then promptly disappears, as it's only purpose is to
// alert the right side that a new thread has begun execution.
class DebuggerThreadStarter : public DebuggerController
{
public:
    DebuggerThreadStarter(Thread *thread);

    virtual DEBUGGER_CONTROLLER_TYPE GetDCType( void )
        { return DEBUGGER_CONTROLLER_THREAD_STARTER; }

private:
    TP_RESULT TriggerPatch(DebuggerControllerPatch *patch,
                      Thread *thread,
                      TRIGGER_WHY tyWhy);
    void TriggerTraceCall(Thread *thread, const BYTE *ip);
    bool SendEvent(Thread *thread, bool fInterruptedBySetIp);
};

#ifdef FEATURE_DATABREAKPOINT

class DebuggerDataBreakpoint : public DebuggerController
{
private:
    CONTEXT m_context;
public:
    DebuggerDataBreakpoint(Thread* pThread) : DebuggerController(pThread, NULL)
    {
        LOG((LF_CORDB, LL_INFO10000, "D:DDBP: Data Breakpoint event created\n"));
        memcpy(&m_context, g_pEEInterface->GetThreadFilterContext(pThread), sizeof(CONTEXT));
    }

    virtual DEBUGGER_CONTROLLER_TYPE GetDCType(void)
    {
        return DEBUGGER_CONTROLLER_DATA_BREAKPOINT;
    }

    virtual TP_RESULT TriggerPatch(DebuggerControllerPatch *patch, Thread *thread,  TRIGGER_WHY tyWhy);

    virtual bool TriggerSingleStep(Thread *thread, const BYTE *ip);

    bool SendEvent(Thread *thread, bool fInterruptedBySetIp)
    {
        CONTRACTL
        {
            NOTHROW;
            SENDEVENT_CONTRACT_ITEMS;
        }
        CONTRACTL_END;

        LOG((LF_CORDB, LL_INFO10000, "DDBP::SE: in DebuggerDataBreakpoint's SendEvent\n"));

        g_pDebugger->SendDataBreakpoint(thread, &m_context, this);

        Delete();

        return true;
    }

    static bool IsDataBreakpoint(Thread *thread, CONTEXT * pContext);
    static bool TriggerDataBreakpoint(Thread *thread, CONTEXT * pContext);
};

#endif // FEATURE_DATABREAKPOINT


/* ------------------------------------------------------------------------- *
 * DebuggerUserBreakpoint routines.  UserBreakpoints are used
 * by Runtime threads to send that they've hit a user breakpoint to the
 * Right Side.
 * ------------------------------------------------------------------------- */
class DebuggerUserBreakpoint : public DebuggerStepper
{
public:
    static void HandleDebugBreak(Thread * pThread);

    static bool IsFrameInDebuggerNamespace(FrameInfo * pFrame);

    virtual DEBUGGER_CONTROLLER_TYPE GetDCType( void )
        { return DEBUGGER_CONTROLLER_USER_BREAKPOINT; }
private:
    // Don't construct these directly. Use HandleDebugBreak().
    DebuggerUserBreakpoint(Thread *thread);


    virtual bool IsInterestingFrame(FrameInfo * pFrame);

    bool SendEvent(Thread *thread, bool fInterruptedBySetIp);
};

/* ------------------------------------------------------------------------- *
 * DebuggerFuncEvalComplete routines
 * ------------------------------------------------------------------------- */
class DebuggerFuncEvalComplete : public DebuggerController
{
public:
    DebuggerFuncEvalComplete(Thread *thread,
                             void *dest);

    virtual DEBUGGER_CONTROLLER_TYPE GetDCType( void )
        { return DEBUGGER_CONTROLLER_FUNC_EVAL_COMPLETE; }

private:
    TP_RESULT TriggerPatch(DebuggerControllerPatch *patch,
                      Thread *thread,
                      TRIGGER_WHY tyWhy);
    bool SendEvent(Thread *thread, bool fInterruptedBySetIp);
    DebuggerEval* m_pDE;
};

// continuable-exceptions
/* ------------------------------------------------------------------------- *
 * DebuggerContinuableExceptionBreakpoint routines
 * ------------------------------------------------------------------------- *
 *
 * DebuggerContinuableExceptionBreakpoint:  Implementation of Continuable Exception support uses this.
 */
class DebuggerContinuableExceptionBreakpoint : public DebuggerController
{
public:
    DebuggerContinuableExceptionBreakpoint(Thread *pThread,
                                           SIZE_T m_offset,
                                           DebuggerJitInfo *jitInfo,
                                           AppDomain *pAppDomain);

    virtual DEBUGGER_CONTROLLER_TYPE GetDCType( void )
        { return DEBUGGER_CONTROLLER_CONTINUABLE_EXCEPTION; }

private:
    TP_RESULT TriggerPatch(DebuggerControllerPatch *patch,
                      Thread *thread,
                      TRIGGER_WHY tyWhy);

    bool SendEvent(Thread *thread, bool fInterruptedBySetIp);
};


#ifdef FEATURE_METADATA_UPDATER
//---------------------------------------------------------------------------------------
//
// DebuggerEnCBreakpoint - used by edit and continue to support remapping
//
// When a method is updated, we make no immediate attempt to remap any existing execution
// of the previous version.  Instead we set EnC breakpoints in the previous version, and
// prompt the debugger whenever one is hit, giving it the opportunity to request a remap to the
// latest version of the method.
//
// Over long debugging sessions which make many edits to large methods, we can create
// a large number of these breakpoints.  We currently make no attempt to reclaim the
// code or patch overhead for previous versions. Ideally we'd be able to detect when there are
// no outstanding references to older method versions and clean up. At the very least, we
// could remove all but the first patch when there are no outstanding
// frames for a specific version of an edited method.
//
class DebuggerEnCBreakpoint final : public DebuggerController
{
public:
    // We have two types of EnC breakpoints. The first is the one we
    // sprinkle through previous code versions to let us know when execution
    // is occurring in a function that now has a new version.
    // The second is when we've actually resumed excecution into a remapped
    // function and we need to then notify the debugger.
    enum TriggerType {REMAP_PENDING, REMAP_COMPLETE};

    // Create and activate an EnC breakpoint at the specified native offset
    DebuggerEnCBreakpoint(SIZE_T m_offset,
                          DebuggerJitInfo *jitInfo,
                          TriggerType fTriggerType,
                          AppDomain *pAppDomain);

    virtual DEBUGGER_CONTROLLER_TYPE GetDCType( void ) override
        { return DEBUGGER_CONTROLLER_ENC; }

private:
    TP_RESULT TriggerPatch(DebuggerControllerPatch *patch,
                      Thread *thread,
                      TRIGGER_WHY tyWhy) override;

    TP_RESULT HandleRemapComplete(DebuggerControllerPatch *patch,
                                   Thread *thread,
                                   TRIGGER_WHY tyWhy);

    DebuggerJitInfo *m_jitInfo;
    TriggerType m_fTriggerType;
};
#endif //FEATURE_METADATA_UPDATER

/* ========================================================================= */

enum
{
    EVENTS_INIT_ALLOC = 5
};

class DebuggerControllerQueue
{
    DebuggerController **m_events;
    DWORD m_dwEventsCount;
    DWORD m_dwEventsAlloc;
    DWORD m_dwNewEventsAlloc;

public:
    DebuggerControllerQueue()
        : m_events(NULL),
          m_dwEventsCount(0),
          m_dwEventsAlloc(0),
          m_dwNewEventsAlloc(0)
    {
    }


    ~DebuggerControllerQueue()
    {
        if (m_events != NULL)
            delete [] m_events;
    }

    BOOL dcqEnqueue(DebuggerController *dc, BOOL fSort)
    {
        LOG((LF_CORDB, LL_INFO100000,"DCQ::dcqE\n"));

        _ASSERTE( dc != NULL );

        if (m_dwEventsCount == m_dwEventsAlloc)
        {
            if (m_events == NULL)
                m_dwNewEventsAlloc = EVENTS_INIT_ALLOC;
            else
                m_dwNewEventsAlloc = m_dwEventsAlloc<<1;

            DebuggerController **newEvents = new (nothrow) DebuggerController * [m_dwNewEventsAlloc];

            if (newEvents == NULL)
                return FALSE;

            if (m_events != NULL)
                // The final argument to CopyMemory cannot over/underflow.
                // The amount of memory copied has a strict upper bound of the size of the array,
                // which cannot exceed the pointer size for the platform.
               CopyMemory(newEvents, m_events, (SIZE_T)sizeof(*m_events) * (SIZE_T)m_dwEventsAlloc);

            m_events = newEvents;
            m_dwEventsAlloc = m_dwNewEventsAlloc;
        }

        dc->Enqueue();

        // Make sure to place high priority patches into
        // the event list first. This ensures, for
        // example, that thread starts fire before
        // breakpoints.
        if (fSort && (m_dwEventsCount > 0))
        {
            DWORD i;
            for (i = 0; i < m_dwEventsCount; i++)
            {
                _ASSERTE(m_events[i] != NULL);

                if (m_events[i]->GetDCType() > dc->GetDCType())
                {
                    // The final argument to CopyMemory cannot over/underflow.
                    // The amount of memory copied has a strict upper bound of the size of the array,
                    // which cannot exceed the pointer size for the platform.
                    MoveMemory(&m_events[i+1], &m_events[i], (SIZE_T)sizeof(DebuggerController*) * (SIZE_T)(m_dwEventsCount - i));
                    m_events[i] = dc;
                    break;
                }
            }

            if (i == m_dwEventsCount)
                m_events[m_dwEventsCount] = dc;

            m_dwEventsCount++;
        }
        else
            m_events[m_dwEventsCount++] = dc;

        return TRUE;
    }

    DWORD dcqGetCount(void)
    {
        return m_dwEventsCount;
    }

    DebuggerController *dcqGetElement(DWORD dwElement)
    {
        LOG((LF_CORDB, LL_INFO100000,"DCQ::dcqGE\n"));

        DebuggerController *dcp = NULL;

        _ASSERTE(dwElement < m_dwEventsCount);
        if (dwElement < m_dwEventsCount)
        {
            dcp = m_events[dwElement];
        }

        _ASSERTE(dcp != NULL);
        return dcp;
    }

    // Kinda wacked, but this actually releases stuff in FILO order, not
    // FIFO order.  If we do this in an extra loop, then the perf
    // is better than sliding everything down one each time.
    void dcqDequeue(DWORD dw = 0xFFffFFff)
    {
        if (dw == 0xFFffFFff)
        {
            dw = (m_dwEventsCount - 1);
        }

        LOG((LF_CORDB, LL_INFO100000,"DCQ::dcqD element index "
            "0x%x of 0x%x\n", dw, m_dwEventsCount));

        _ASSERTE(dw < m_dwEventsCount);

        m_events[dw]->Dequeue();

        // Note that if we're taking the element off the end (m_dwEventsCount-1),
        // the following will no-op.
        // The final argument to MoveMemory cannot over/underflow.
        // The amount of memory copied has a strict upper bound of the size of the array,
        // which cannot exceed the pointer size for the platform.
        MoveMemory(&(m_events[dw]),
                   &(m_events[dw + 1]),
                   (SIZE_T)sizeof(DebuggerController *) * (SIZE_T)(m_dwEventsCount - dw - 1));
        m_dwEventsCount--;
    }
};

// Include all of the inline stuff now.
#include "controller.inl"

#endif // !DACCESS_COMPILE

#endif /*  CONTROLLER_H_ */
