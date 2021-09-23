// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// DacDbiInterface.h
//

//
// Define the interface between the DAC and DBI.
//*****************************************************************************

#ifndef _DACDBI_INTERFACE_H_
#define _DACDBI_INTERFACE_H_

#include <metahost.h>

// The DAC/DBI interface can use structures and LSPTR declarations from the
// existing V2 interfaces
#include "dbgipcevents.h"

//-----------------------------------------------------------------------------
// Deallocation function for memory allocated with the global IAllocator object.
//
// Arguments:
//    p - pointer to delete. Allocated with IAllocator::Alloc
//
// Notes:
//    This should invoke the dtor and then call IAllocator::Free.
//    In the DAC implementation, this will call via IAllocator.
//    In the DBI implementation, this can directly call delete (assuming the IAllocator::Free
//    directly called new).
template<class T> void DeleteDbiMemory(T *p);
template<class T> void DeleteDbiArrayMemory(T *p, int count);
// Need a class to serve as a tag that we can use to overload New/Delete.
class forDbiWorker {};
extern forDbiWorker forDbi;
extern void * operator new(size_t lenBytes, const forDbiWorker &);
extern void * operator new[](size_t lenBytes, const forDbiWorker &);
extern void operator delete(void *p, const forDbiWorker &);
extern void operator delete[](void *p, const forDbiWorker &);

// The dac exposes a way to walk all GC references in the process.  This
// includes both strong references and weak references.  This is done
// through a referece walk.
typedef void* * RefWalkHandle;

#include "dacdbistructures.h"

// This is the current format of code:DbiVersion.   It needs to be rev'ed when we decide to store something
// else other than the product version of the DBI in DbiVersion (e.g. a timestamp).  See
// code:CordbProcess::CordbProcess#DBIVersionChecking for more information.
const DWORD kCurrentDbiVersionFormat = 1;

//-----------------------------------------------------------------------------
// This is a low-level interface between DAC and DBI.
// The DAC is the raw DAC-ized code from the EE.
// DBI is the implementation of ICorDebug on top of that.
//
// This interface should be:
//  - Stateless: The DAC component should not have any persistent state. It should not have any resources
//          that it needs to clean up. DBI can store all the state (eg, list of of modules).
//          Using IAllocator/IStringHolder interfaces to allocate data to pass back out is ok because DBI owns
//          the resources, not the DAC layer.
//  - blittable: The types on the interface should be blittable. For example, use TIDs instead of OS Thread handles.
//          Passing pointers to be used as out-parameters is ok.
//  - lightweight: it will inevitably have many methods on it and should be very fluid to use.
//  - very descriptive: heavily call out liabilities on the runtime. For example, don't just have a method like
//      "GetName" where Name is ambiguous. Heavily comment exactly what Name is, when it may fail, if it's 0-length,
//     if it's unique, etc. This serves two purposes:
//       a) it helps ensure the right invariants flow up to the public API level.
//       b) it helps ensure that the debugger is making the right assumptions about the runtime's behavior.
//
// #Marshaling:
//   This interface should be marshalable such that the caller (the Right Side) can exist in one
//   process, while the implementation of Dac could be on another machine.
//   - All types need to be marshable.
//   - Use OUT and OPTIONAL as defined in windef.h to guide the marshaler. Here are how types are marshaled:
//         T  : value-type, copied on input.
//         T*  : will be marshaled as non-null by-ref (copy on input, copy on return),
//         const T*: non-null, copy on input only.
//         OUT T*: non-null copy-on-return only.
//         OPTIONAL T*: by-ref, could be null.
//   - The marshaler has special knowledge of IStringHolder and DacDbiArrayList<T>.
//   - You can write custom marshalers for non-blittable structures defined in DacDbiStructures.h.
//   - There is custom handling for marshalling callbacks.
//
//
// Threading: The interface (and the underlying DataTarget) are free-threaded to leverage
//    concurrency.
//
// Allocation:
//    This interface can use IAllocator to allocate objects and hand them back. The allocated objects should be:
//      - closed, serializable object graphs.
//      - should have private fields and public accessors
//      - have dtors that free any allocated the memory via calling DeleteDbiMemory.
//    Objects can be declared in a header and shared between both dbi and dac.
//    Consider using DacDbiArrayList<T> instead of custom allocations.

// Error handling:
//   Any call on the interface may fail. For example, the data-target may not have access to the necessary memory.
//   Methods should throw on error.
//
// #Enumeration
// General rules about Enumerations:
//    - Do not assume that enumerations exposed here are in any particular order.
//    - many enumerations also correspond to Load/Unload events. Since load/unload aren't atomic with publishing
//      in an enumeration, this is a Total Ordering of things:
//        a) object shows up in enumeration
//        b) load event.
//        c) ... steady state ...
//        d) object removed from DacDbi enumeration;
//             Any existing handles we get beyond this are explicitly associated with a Cordb* object; which can be
//             neutered on the unload event by Dbi.
//        e) unload event.
//         - Send after it's reachability from other objects is broken. (Eg, For AppDomain unload
//         means no threads left in that appdomain)
//         - Send before it's deleted (so VMPTR is still valid; not yet recycled).
//         - Send early enough that property access can at least gracefully fail. (eg,
//         Module::GetName should either return the name, or fail)
//
//         Cordb must neuter any Cordb objects that have any pre-existing handles to the object.
//             After this point, gauranteed that nobody can discover the VMPTR any more:
//             - doesn't show up in enumerations (so can't be discoverered implicitly)
//             - object should not be discoverable by other objects in VM.
//             - any Cordb object that already had it would be neutered by Dbi.
//             - Therefore nothing should even be asking Dac for it.
//        f) object deleted.
//     Think of it like this: The event occurs to let you know that the enumeration has been updated.
//
//     A robust debugger should not rely on events for correctness. For example,
//     a corrupt debuggee may send:
//     1) multiple load events. (if target repeats due to an issue)
//     2) no load event and only an unload event. (if target fails inbetween
//     publish (a) and load (b), and then backout code sends the unload).
//     3) no unload event. (eg, if target is rudely killed)
//     4) multiple unload events (if target repeats due to bug)
//
//     This satisfies the following rules:
//     - once you get the load event, you can find the object via enumeration
//     - once an item is discoverable, it must immediately show up in the enumeration.
//     - once you get the unload event, the object is dead and can't be rediscovered via enumeration.
//
//     This is an issue even for well-behaved targets. Imagine if a debugger attaches right after
//     an unload event is sent. We don't want the debugger to enumerate and re-discover the
//     unloaded object because now that the unload event is already sent, the debugger won't get
//     any further notification of when the object is deleted in the target.
//     Thus it's valuable for the debugger to have debug-only checks after unload events to assert
//     that the object is no longer discoverable.
//
//.............................................................................
// The purpose of this object is to provide EE funcationality back to
// the debugger. This represents the entire set of EE functions used
// by the debugger.
//
// We will make this interface larger over time to grow the functionality
// between the EE and the Debugger.
//
//
//-----------------------------------------------------------------------------
class IDacDbiInterface
{
public:
    class IStringHolder;

    // The following tag tells the DD-marshalling tool to start scanning.
    // BEGIN_MARSHAL

    //-----------------------------------------------------------------------------
    // Functions to control the behavior of the DacDbi implementation itself.
    //-----------------------------------------------------------------------------

    //
    // Check whether the version of the DBI matches the version of the runtime.
    // This is only called when we are remote debugging.  On Windows, we should have checked all the
    // versions before we call any API on the IDacDbiInterface.  See
    // code:CordbProcess::CordbProcess#DBIVersionChecking for more information on version checks.
    //
    // Return Value:
    //    S_OK on success.
    //
    // Notes:
    //    THIS MUST BE THE FIRST API ON THE INTERFACE!
    //
    virtual
    HRESULT CheckDbiVersion(const DbiVersion * pVersion) = 0;

    //
    // Flush the DAC cache. This should be called when target memory changes.
    //
    //
    // Return Value:
    //    S_OK on success.
    //
    // Notes:
    //    If this fails, the interface is in an undefined state.
    //    This must be called anytime target memory changes, else all other functions
    //    (besides Destroy) may yield out-of-date or semantically incorrect results.
    //
    virtual
    HRESULT FlushCache() = 0;

    //
    // Control DAC's checking of the target's consistency. Specifically, if this is disabled then
    // ASSERTs in VM code are ignored. The default is disabled, since DAC should do it's best to
    // return results even with a corrupt or unsyncrhonized target. See
    // code:ClrDataAccess::TargetConsistencyAssertsEnabled for more details.
    //
    // When testing with a non-corrupt and properly syncrhonized target, this should be enabled to
    // help catch bugs.
    //
    // Arguments:
    //   fEnableAsserts - whether ASSERTs should be raised when consistency checks fail (_DEBUG
    //   builds only)
    //
    // Notes:
    //   In the future we may want to extend DAC target consistency checks to be retail checks
    //   (exceptions) as well. We'll also need a mechanism for disabling them (eg. when an advanced
    //   user wants to try to get a result anyway even though the target is inconsistent). In that
    //   case we'll want an additional argument here for enabling/disabling the throwing of
    //   consistency failures exceptions (this is independent from asserts - there are legitimate
    //   scenarios for all 4 combinations).
    //
    virtual
    void DacSetTargetConsistencyChecks(bool fEnableAsserts) = 0;

    //
    // Destroy the interface object. The client should call this when it's done
    // with the IDacDbiInterface to free up any resources.
    //
    // Return Value:
    //    None.
    //
    // Notes:
    //    The client should not call anything else on this interface after Destroy.
    //
    virtual
    void Destroy() = 0;

    //-----------------------------------------------------------------------------
    // General purpose target inspection functions
    //-----------------------------------------------------------------------------

    //
    // Query if Left-side is started up?
    //
    //
    // Return Value:
    //    BOOL whether Left-side is intialized.
    //
    // Notes:
    //   If the Left-side is not yet started up, then data in the LS is not yet initialized enough
    //   for us to make meaningful queries, but the runtime will fire "Startup Exception" when it is.
    //
    //   If the left-side is started up, then data is ready. (Although data may be temporarily inconsistent,
    //   see DataSafe). We may still get a Startup Exception in these cases, but it can be ignored.
    //
    virtual
    BOOL IsLeftSideInitialized() = 0;


    //
    // Get an LS Appdomain via an AppDomain unique ID.
    // Fails if the AD is not found or if the ID is invalid.
    //
    // Arguments:
    //  appdomainId      - "unique appdomain ID". Must be a valid Id.
    //
    // Return Value:
    //    VMPTR_AppDomain for the corresponding AppDomain ID.  Else throws.
    //
    // Notes:
    //   This query is based off the lifespan of the AppDomain from the VM's perspective.
    //   The AppDomainId is most likely obtained from an AppDomain-Created debug events.
    //   An AppDomainId is unique for the lifetime of the VM.
    //   This is the inverse function of GetAppDomainId().
    //
    virtual
    VMPTR_AppDomain GetAppDomainFromId(ULONG appdomainId) = 0;


    //
    // Get the AppDomain ID for an AppDomain.
    //
    // Arguments:
    //  vmAppDomain  - VM pointer to the AppDomain object of interest
    //
    // Return Value:
    //    AppDomain ID for appdomain. Else throws.
    //
    // Notes:
    //   An AppDomainId is unique for the lifetime of the VM. It is non-zero.
    //
    virtual
    ULONG GetAppDomainId(VMPTR_AppDomain vmAppDomain) = 0;

    //
    // Get the managed AppDomain object for an AppDomain.
    //
    // Arguments:
    //  vmAppDomain  - VM pointer to the AppDomain object of interest
    //
    // Return Value:
    //    objecthandle for the managed app domain object or the Null VMPTR if there is no
    //    object created yet
    //
    // Notes:
    //   The AppDomain managed object is lazily constructed on the AppDomain the first time
    //   it is requested. It may be NULL.
    //
    virtual
    VMPTR_OBJECTHANDLE GetAppDomainObject(VMPTR_AppDomain vmAppDomain) = 0;

    virtual
    void GetAssemblyFromDomainAssembly(VMPTR_DomainAssembly vmDomainAssembly, OUT VMPTR_Assembly * vmAssembly) = 0;

    //
    // Determines whether the runtime security system has assigned full-trust to this assembly.
    //
    // Arguments:
    //      vmDomainAssembly - VM pointer to the assembly in question.
    //
    // Return Value:
    //      Returns trust status for the assembly.
    //      Throws on error.
    //
    // Notes:
    //      Of course trusted malicious code in the process could always cause this API to lie.  However,
    //      an assembly loaded without full-trust should have no way of causing this API to return true.
    //
    virtual
    BOOL IsAssemblyFullyTrusted(VMPTR_DomainAssembly vmDomainAssembly) = 0;


    //
    // Get the full AD friendly name for the given EE AppDomain.
    //
    // Arguments:
    //     vmAppDomain - VM pointer to the AppDomain.
    //     pStrName    - required out parameter where the name will be stored.
    //
    // Return Value:
    //     None. On success, sets the string via the holder. Throws on error.
    //     This either sets pStrName or Throws. It won't do both.
    //
    // Notes:
    //    AD names have an unbounded length.  AppDomain friendly names can also change, and
    //    so callers should be prepared to listen for name-change events and requery.
    //    AD names are specified by the user.
    //
    virtual
    void GetAppDomainFullName(
        VMPTR_AppDomain vmAppDomain,
        IStringHolder * pStrName) = 0;


    //
    // #ModuleNames
    //
    // Modules / Assemblies have many different naming schemes:
    //
    // 1) Metadata Scope name: All modules have metadata, and each metadata scope has a name assigned
    //    by the creator of that scope (eg, the compiler). This usually is similar to the filename, but could
    //     be arbitrary.
    //     eg: "Foo"
    //
    // 2) FileRecord: the File record entry in the manifest module's metadata (table 0x26) for this module.
    //     eg: "Foo"
    //
    // 3) Managed module path: This is path that the image was loaded from. Eg, "c:\foo.dll". For non-file
    //    based modules (like in-memory, dynamic), there is no file path. The specific path is determined by
    //    fusion / loader policy.
    //    eg: "c:\foo.dll"
    //
    // 4) GAC path: If the module is loaded from the GAC, this is the path on disk into the gac cache that
    //    the image was pulled from.
    //    eg: "
    //
    // 5) Ngen path: If the module was ngenned, this is the path on disk into the ngen cache that the image
    //    was pulled from.
    //    eg:
    //
    // 6) Fully Qualified Assembly Name: this is an abstract name, which the CLR (fusion / loader) will
    //    resolve (to a filename for file-based modules). Managed apps may need to deal in terms of FQN,
    //    but the debugging services generally avoid them.
    //    eg: "Foo, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089, processorArchitecture=MSIL".
    //


    //
    // Get the "simple name" of a module. This is a heuristic within the CLR to return a simple,
    // not-well-specified, but meaningful, name for a module.
    //
    // Arguments:
    //    vmModule - module to query
    //    pStrFileName - string holder to get simple name.
    //
    // Return Value:
    //     None, but pStrFilename will be initialized upon return.
    //     Throws if there was a problem reading the data with DAC or if there is an OOM exception,
    //     in which case no string was stored into pStrFilename.
    //
    // Notes:
    //   See code:#ModuleNames for an overview on module names.
    //
    //   This is really just using code:Module::GetSimpleName.
    //   This gives back a meaningful name, which is generally some combination of the metadata
    //   name of the FileRecord name. This is important because it's valid even when a module
    //   doesn't have a filename.
    //
    //   The simple name does not have any meaning. It is not a filename, does not necessarily have any
    //   relationship to the filename, and it's not necesarily the metadata name.
    //   Do not use the simple name for anything other than as a pretty string to give the an end user.
    //
    virtual
    void GetModuleSimpleName(VMPTR_Module vmModule, IStringHolder * pStrFilename) = 0;


    //
    // Get the full path and file name to the assembly's manifest module.
    //
    // Arguments:
    //     vmAssembly       - VM pointer to the Assembly.
    //     pStrFilename     - required out parameter where the filename will be stored.
    //
    // Return Value:
    //     TRUE on success, in which case the filename was stored into pStrFilename
    //     FALSE if the assembly has no filename (eg. for in-memory assemblies), in which
    //     case an empty string was stored into pStrFilename.
    //     Throws if there was a problem reading the data with DAC, in which case
    //     no string was stored into pStrFilename.
    //
    // Notes:
    //     See code:#ModuleNames for an overview on module names.
    //
    //     Normally this is just the filename from which the dll containing the assembly was
    //     loaded.  In the case of multi-module assemblies, this is the filename for the
    //     manifest module (the one containing the assembly manifest).  For in-memory
    //     assemblies (eg. those loaded from a Byte[], and those created by Reflection.Emit
    //     which will not be saved to disk) there is no filename.  In that case this API
    //     returns an empty string.
    //
    virtual
    BOOL GetAssemblyPath(VMPTR_Assembly   vmAssembly,
                         IStringHolder *  pStrFilename) = 0;


    // get a type def resolved across modules
    // Arguments:
    //     input:  pTypeRefInfo   - domain file and type ref from the referencing module
    //     output: pTargetRefInfo - domain file and type def from the referenced type (this may
    //                              come from a module other than the referencing module)
    // Note: throws
    virtual
    void ResolveTypeReference(const TypeRefData * pTypeRefInfo,
                              TypeRefData *       pTargetRefInfo) = 0;
    //
    // Get the full path and file name to the module (if any).
    //
    // Arguments:
    //     vmModule - VM pointer to the module.
    //     pStrFilename - required out parameter where the filename will be stored.
    //
    // Return Value:
    //     TRUE on success, in which case the filename was stored into pStrFilename
    //     FALSE the module has no filename (eg. for in-memory assemblies), in which
    //     case an empty string was stored into pStrFilename.
    //     Throws an exception if there was a problem reading the data with DAC, in which case
    //     no string was stored into pStrFilename.
    //
    // Notes:
    //     See code:#ModuleNames for an overview on module names.
    //
    //     Normally this is just the filename from which the module was loaded.
    //     For in-memory module (eg. those loaded from a Byte[], and those created by Reflection.Emit
    //     which will not be saved to disk) there is no filename.  In that case this API
    //     returns an empty string.  Consider GetModuleSimpleName in those cases.
    //
    //     We intentionally don't use the function name "GetModuleFileName" here because
    //     winbase #defines that token (along with many others) to have an A or W suffix.
    //
    virtual
    BOOL GetModulePath(VMPTR_Module vmModule,
                       IStringHolder *  pStrFilename) = 0;


    //
    // Get the full path and file name to the ngen image for the module (if any).
    //
    // Arguments:
    //     vmModule - VM pointer to the module.
    //     pStrFilename - required out parameter where the filename will be stored.
    //
    // Return Value:
    //     TRUE on success, in which case the filename was stored into pStrFilename
    //     FALSE the module has no filename (eg. for in-memory assemblies), in which
    //     case an empty string was stored into pStrFilename.
    //     Throws an exception if there was a problem reading the data with DAC, in which case
    //     no string was stored into pStrFilename.
    //
    // Notes:
    //     See code:#ModuleNames for an overview on module names.
    //
    virtual
    BOOL GetModuleNGenPath(VMPTR_Module vmModule,
                           IStringHolder *  pStrFilename) = 0;



    // Get the metadata for the target module
    //
    // Arguments:
    //    vmModule - target module to get metadata for.
    //    pTargetBuffer - Out parameter to get target-buffer for metadata. Gauranteed to be non-empty on
    //       return. This will throw CORDBG_E_MISSING_METADATA hr if the buffer is empty.
    //       This does not gaurantee that the buffer is readable. For example, in a minidump, buffer's
    //       memory may not be present.
    //
    // Notes:
    //    Each module's metadata exists as a raw buffer in the target. This finds that target buffer and
    //    returns it. The host can then use OpenScopeOnMemory to create an instance of the metadata in
    //    the host process space.
    //
    //    For dynamic modules, the CLR will eagerly serialize the metadata at "debuggable" points. This
    //    could be after each type is loaded; or after a bulk update.
    //    For non-dynamic modules (both in-memory and file-based), the metadata exists in the PEFile's image.
    //
    //    Failure cases:
    //    This should succeed in normal, live-debugging scenarios. However, common failure paths here would be:
    //
    //    1. Data structures are intact, but Unable to even find the TargetBuffer in the target. In this
    //    case  Metadata is truly missing. Likely means:
    //    - target is in the middle of generating metadata for a large bulk operation. (For example, attach
    //    to a TypeLibConverter using Ref.Emit to emit a module for a very large .tlb file).
    //    - corrupted target,
    //    - or the target had some error(out-of-memory?) generating the metadata.
    //    This throws CORDBG_E_MISSING_METADATA.
    //
    //    2. Target buffer is found, but memory it describes is not present. Likely means a minidump
    //    scenario with missing memory. Client should use alternative metadata location techniques (such as
    //    an ImagePath to locate the original image and then pulling metadata from that file).
    //
    virtual
    void GetMetadata(VMPTR_Module vmModule, OUT TargetBuffer * pTargetBuffer) = 0;


    // Definitions for possible symbol formats
    // This is equivalent to code:ESymbolFormat in the runtime
    typedef enum
    {
        kSymbolFormatNone,  // No symbols available
        kSymbolFormatPDB,   // PDB symbol format - use diasymreader.dll
    } SymbolFormat;

    //
    // Get the in-memory symbol (PDB/ILDB) buffer in the target if present.
    //
    // Arguments:
    //    vmModule- module to query for.
    //    pTargetBuffer - out parameter to get buffer in target of symbols. If no symbols, pTargetBuffer is empty on return.
    //    pSymbolFormat - out parameter to get the format of the symbols.
    //
    // Returns:
    //   1) If there are in-memory symbols for the given module, pTargetBuffer is set to the buffer describing
    //   the symbols and pSymbolFormat is set to indicate PDB or ILDB format. This buffer can then be read,
    //   converted into an IStream, and passed to ISymUnmanagedBinder::CreateReaderForStream.
    //   2) If the target is valid, but there is no symbols for the module, then pTargetBuffer->IsEmpty() == true
    //   and *pSymbolFormat == kSymbolFormatNone.
    //   3) Else, throws exception.
    //
    //
    // Notes:
    //   For file-based modules, PDBs are normally on disk and the debugger retreieves them via a symbol
    //   path without any help from ICorDebug.
    //   However, in some cases, the PDB is stored in-memory and so the debugger needs ICorDebug. Common
    //   cases include:
    //   - dynamic modules generated with reflection-emit.
    //   - in-memory modules loaded by Load(Byte[],Byte[]), which provide the PDB as a byte[].
    //   - hosted modules where the host (such as SQL) store the PDB.
    //
    //   In all cases, this can commonly fail. Executable code does not need to have a PDB.
    virtual
    void GetSymbolsBuffer(VMPTR_Module vmModule, OUT TargetBuffer * pTargetBuffer, OUT SymbolFormat * pSymbolFormat) = 0;

    //
    // Get properties for a module
    //
    // Arguments:
    //    vmModule - vm handle to a module
    //    pData - required out parameter which will be filled out with module properties
    //
    // Notes:
    //    See definition of DomainFileInfo for more details about what properties
    //    this gives back.
    virtual
    void GetModuleData(VMPTR_Module vmModule, OUT ModuleInfo * pData) = 0;


    //
    // Get properties for a DomainFile
    //
    // Arguments:
    //    vmDomainFile - vm handle to a DomainFile
    //    pData - required out parameter which will be filled out with module properties
    //
    // Notes:
    //    See definition of DomainFileInfo for more details about what properties
    //    this gives back.
    virtual
    void GetDomainFileData(VMPTR_DomainFile vmDomainFile, OUT DomainFileInfo * pData) = 0;

    virtual
    void GetModuleForDomainFile(VMPTR_DomainFile vmDomainFile, OUT VMPTR_Module * pModule) = 0;

    //.........................................................................
    // These methods were the methods that DBI was calling from IXClrData in V2.
    // We imported them over to this V3 interface so that we can sever all ties between DBI and the
    // old IXClrData.
    //
    // The exact semantics of these are whatever their V2 IXClrData counterpart did.
    // We may eventually migrate these to their real V3 replacements.
    //.........................................................................

    // "types" of addresses. This is taken exactly from the definition, but renamed to match
    // CLR coding conventions.
    typedef enum
    {
        kAddressUnrecognized,
        kAddressManagedMethod,
        kAddressRuntimeManagedCode,
        kAddressRuntimeUnmanagedCode,
        kAddressGcData,
        kAddressRuntimeManagedStub,
        kAddressRuntimeUnmanagedStub,
    } AddressType;

    //
    // Get the "type" of address.
    //
    // Arguments:
    //    address      - address to query type.
    //
    // Return Value:
    //    Type of address. Throws on error.
    //
    // Notes:
    //    This is taken exactly from the IXClrData definition.
    //    This is provided for V3 compatibility to support Interop-debugging.
    //    This should eventually be deprecated.
    //
    virtual
    AddressType GetAddressType(CORDB_ADDRESS address) = 0;


    //
    // Query if address is a CLR stub.
    //
    // Arguments:
    //   address  - Target address to query for.
    //
    //
    // Return Value:
    //    true if the address is a CLR stub.
    //
    // Notes:
    //    This is used to implement ICorDebugProcess::IsTransitionStub
    //    This yields true if the address is claimed by a CLR stub manager, or if the IP is in mscorwks.
    //    Conceptually, This should eventually be merged with GetAddressType().
    //
    virtual
    BOOL IsTransitionStub(CORDB_ADDRESS address) = 0;

    //.........................................................................
    // Get the values of the JIT Optimization and EnC flags.
    //
    // Arguments:
    //    vmDomainFile -   (input) VM DomainFile (module) for which we are retrieving flags
    //    pfAllowJITOpts - (mandatory output) true iff this is not compiled for debug,
    //                      i.e., without optimization
    //    pfEnableEnc -    (mandatory output) true iff this module has EnC enabled
    //
    // Return Value:
    //    Returns on success. Throws on failure.
    //
    // Notes:
    //    This is used to implement both ICorDebugModule2::GetJitCompilerFlags and
    //    ICorDebugCode2::GetCompilerFlags.
    //.........................................................................

    virtual
    void GetCompilerFlags(
        VMPTR_DomainFile vmDomainFile,
        OUT BOOL * pfAllowJITOpts,
        OUT BOOL * pfEnableEnC) = 0;

    //.........................................................................
    // Set the values of the JIT optimization and EnC flags.
    //
    // Arguments:
    //    vmDomainFile -   (input) VM DomainFile (module) for which we are retrieving flags
    //    pfAllowJITOpts - (input) true iff this should not be compiled for debug,
    //                      i.e., without optimization
    //    pfEnableEnc -    (input) true iff this module should have EnC enabled. If this is
    //                      false, no change is made to the EnC flags. In other words, once EnC is enabled,
    //                      there is no way to disable it.
    //
    // Return Value:
    //    S_OK on success and all bits were set.
    //    CORDBG_S_NOT_ALL_BITS_SET - if not all bits are set. Must use GetCompileFlags to
    //      determine which bits were set.
    //    CORDBG_E_CANT_CHANGE_JIT_SETTING_FOR_ZAP_MODULE - if module is ngenned.
    //    Throw on other errors.
    //
    // Notes:
    //    Caller can only use this at module-load before any methods are jitted.
    //    This may be called multiple times.
    //    This is used to implement both ICorDebugModule2::SetJitCompilerFlags and
    //    ICorDebugModule::EnableJITDebugging.
    //.........................................................................

    virtual
    HRESULT SetCompilerFlags(VMPTR_DomainFile vmDomainFile,
                          BOOL             fAllowJitOpts,
                          BOOL             fEnableEnC) = 0;

    //
    // Enumerate all AppDomains in the process.
    //
    // Arguments:
    //    fpCallback   - callback to invoke on each appdomain
    //    pUserData    - user data to supply for each callback.
    //
    // Return Value:
    //    Returns on success. Throws on error.
    //
    // Notes:
    //    Enumerates all appdomains in the process, including the Default-domain.
    //    Appdomains must show up in this list before the AD Load event is sent, and before
    //    that appdomain is discoverable from the debugger.
    //    See enumeration rules for details.
    //
    typedef void (*FP_APPDOMAIN_ENUMERATION_CALLBACK)(VMPTR_AppDomain vmAppDomain, CALLBACK_DATA pUserData);
    virtual
    void EnumerateAppDomains(FP_APPDOMAIN_ENUMERATION_CALLBACK fpCallback,
                                CALLBACK_DATA                            pUserData) = 0;


    //
    // Eunmerate all Assemblies in an appdomain. Enumerations is in load-order
    //
    // Arguments:
    //    vmAppDomain  - domain in which to enumerate
    //    fpCallback   - address to query type.
    //    pUserData    - required out parameter for type of address.
    //
    // Return Value:
    //    Returns on success. Throws on error.
    //
    // Notes:
    //    Enumerates all executable assemblies (both shared and unshared) within an appdomain.
    //    This does not include inspection-only assemblies because those are just data and
    //    not executable (eg, they'll never show up on the stack and you can't set a breakpoint in them).
    //    This enumeration needs to be consistent with load/unload events.
    //    See enumeration rules for details.
    //
    //    The order of the enumeration is the order the assemblies where loaded.
    //    Ultimately, the debugger needs to be able to tell the user the load
    //    order of assemblies (it can do this with native dlls). Since
    //    managed assembliees don't 1:1 correspond to native dlls, debuggers
    //    need this information from the runtime.
    //

    typedef void (*FP_ASSEMBLY_ENUMERATION_CALLBACK)(VMPTR_DomainAssembly vmDomainAssembly, CALLBACK_DATA pUserData);
    virtual
    void EnumerateAssembliesInAppDomain(VMPTR_AppDomain                  vmAppDomain,
                                           FP_ASSEMBLY_ENUMERATION_CALLBACK fpCallback,
                                           CALLBACK_DATA                           pUserData) = 0;



    //
    // Callback function for EnumerateModulesInAssembly
    //
    // This can throw on error.
    //
    // Arguments:
    //    vmModule - new module from the enumeration
    //    pUserData - user data passed to EnumerateModulesInAssembly
    typedef void (*FP_MODULE_ENUMERATION_CALLBACK)(VMPTR_DomainFile vmModule, CALLBACK_DATA pUserData);

    //
    // Enumerates all the code Modules in an assembly.
    //
    // Arguments:
    //    vmAssembly - assembly to enumerate within
    //    fpCallback - callback function to invoke on each module
    //    pUserData - arbitrary data passed to the callback
    //
    // Notes:
    //    This only enumerates "code" modules (ie, modules that have executable code in them). That
    //    includes normal file-based, ngenned, in-memory, and even dynamic modules.
    //    That excludes:
    //    - Resource modules (which have no code or metadata)
    //    - Inspection-only modules. These are viewed as pure data from the debugger's perspective.
    //
    virtual
    void EnumerateModulesInAssembly(
            VMPTR_DomainAssembly vmAssembly,
            FP_MODULE_ENUMERATION_CALLBACK fpCallback,
            CALLBACK_DATA pUserData) = 0;



    //
    // When stopped at an event, request a synchronization.
    //
    //
    // Return Value:
    //    Returns on success. Throws on error.
    //
    // Notes:
    //    Call this when an event is dispatched (eg, LoadModule) to request the runtime
    //    synchronize. This does a cooperative sync with the LS. This is not an async break
    //    and can not be called at arbitrary points.
    //    This primitive lets the LS always take the V3 codepath and defer decision making to the RS.
    //    The V2 behavior is to call this after every event (Since that's what V2 did).
    //    The V3 behavior is to never call this.
    //
    //    If this is called, the LS will sync and we will get a SyncComplete.
    //
    //    This is also like a precursor to "AsyncBreakAllOtherThreads"
    //
    virtual
    void RequestSyncAtEvent() = 0;

    // Sets a flag inside LS.Debugger that indicates that
    // 1. all "first chance exception" events should not be sent to the debugger
    // 2. "exception handler found" events for exceptions never crossing JMC frames should not be sent to the debugger
    //
    // Arguments:
    //    sendExceptionsOutsideOfJMC - new value for the flag Debugger::m_sendExceptionsOutsideOfJMC.
    //
    // Return Value:
    //    Returns error code, never throws.
    //
    // Note: This call is used by ICorDebugProcess8.EnableExceptionCallbacksOutsideOfMyCode.
    virtual
    HRESULT SetSendExceptionsOutsideOfJMC(BOOL sendExceptionsOutsideOfJMC) = 0;

    //
    // Notify the debuggee that a debugger atach is pending.
    //
    // Arguments:
    //     None
    //
    // Return Value:
    //    Returns on success. Throws on error.
    //
    // Notes:
    //     Attaching means that CORDebuggerPendingAttach() will now return true.
    //     This doesn't do anything else (eg, no fake events).
    //
    //     @dbgtodo- still an open Feature-Crew decision how this is exposed publicly.
    virtual
    void MarkDebuggerAttachPending() = 0;

    //
    // Notify the debuggee that a debugger is attached / detached.
    //
    // Arguments:
    //     fAttached - true if we're attaching, false if we're detaching.
    //
    // Return Value:
    //    Returns on success. Throws on error.
    //
    // Notes:
    //     Attaching means that CorDebuggerAttached() will now return true.
    //     This doesn't do anything else (eg, no fake events).
    //     This lets the V3 codepaths invade the LS to subscribe to events.
    //
    //     @dbgtodo- still an open Feature-Crew decision how this is exposed publicly.
    virtual
    void MarkDebuggerAttached(BOOL fAttached) = 0;



    //
    // Hijack a thread. This will effectively do a native func-eval of the thread to set the IP
    //  to a hijack stub and push the parameters.
    //
    // Arguments:
    //    dwThreadId - OS thread to hijack. This must be consistent with pRecord and pOriginalContext
    //    pRecord - optional pointer to Exception record. Required if this is hijacked at an exception.
    //              NULL if this is hijacked at a managed IP.
    //    pOriginalContext - optional pointer to buffer to receive the context that the thread is hijacked from.
    //              The caller can use this to either restore the hijack or walk the hijack.
    //    cbSizeContext - size in bytes of buffer pointed to by pContext
    //    reason  - reason code for the hijack. The hijack stub can then delegate to the proper hijack.
    //    pUserData - arbitrary data passed through to hijack. This is reason-depedendent.
    //    pRemoteContextAddr - If non-NULL this receives the remote address where the CONTEXT was written in the
    //              in the debuggee.
    //
    // Assumptions:
    //    Caller must guarantee this is safe.
    //    This is intended to be used at a thread that either just had an exception or is at a managed IP.
    //    If this is hijacked at an exception, client must cancel the exception (gh / DBG_CONTINUE)
    //    so that the OS exception processing doesn't interfere with the hijack.
    //
    // Notes:
    //   Hijack is hard, so we want 1 hijack stub that handles all our hijacking needs.
    //   This lets us share:
    //     - assembly stubs (which are very platform specific)
    //     - hijacking / restoration mechanics,
    //     - making the hijack walkable via the stackwalker.
    //
    //   Hijacking can be used to implement: func-eval, FE abort, Synchronizing,
    //     dispatching Unhandled Exception notifications.
    //
    //   Nesting: Since Hijacking passes the key state off to the hijacked thread, (such as original
    //     context to be used with restoring the hijack), the raw hijacking nests just like function
    //     calls. However, the client may need to keep additional state to handle nesting. For example,
    //     nested hijacks will require the client to track multiple CONTEXT*.
    //
    //   If the thread is in jitted code, then the hijack needs to cooperate with the in-process
    //    stackwalker that the GC uses. It must be in cooperative mode, and push a Frame on the
    //    frame chain to protect the managed frames it hijacked from before it goes to preemptive mode.

    virtual
    void Hijack(
        VMPTR_Thread                 vmThread,
        ULONG32                      dwThreadId,
        const EXCEPTION_RECORD *     pRecord,
        T_CONTEXT *                    pOriginalContext,
        ULONG32                      cbSizeContext,
        EHijackReason::EHijackReason reason,
        void *                       pUserData,
        CORDB_ADDRESS *              pRemoteContextAddr) = 0;


    //
    // Callback function for connection enumeration.
    //
    // Arguments:
    //   id - the connection ID.
    //   pName - the name of the connection.
    //   pUserData - user data supplied to EnumerateConnections
    typedef void (*FP_CONNECTION_CALLBACK)(DWORD id, LPCWSTR pName, CALLBACK_DATA pUserData);

    //
    // Enumerate all the Connections in the process.
    //
    // Arguments:
    //    fpCallback - callback to invoke for each connection
    //    pUserData - random user data to pass to callback.
    //
    // Notes:
    //    This enumerates all the connections. The host notifies the debugger of Connections
    //    via the ICLRDebugManager interface.
    //    ICorDebug has no interest in connections. It's merely the transport between the host and the debugger.
    //    Ideally, that transport would be more general.
    //
    //    V2 Attach would provide faked up CreateConnection, ChangeConnection events on attach.
    //    This enumeration ability allows V3 to emulate that behavior.
    //

    //
    // Enumerate all threads in the target.
    //
    // Arguments:
    //    fpCallback - callback function to invoke on each thread.
    //    pUserData - arbitrary user data supplied to each callback.
    //
    // Notes:
    //    This enumerates the ThreadStore in the target, which is all the Thread* objects.
    //    This includes threads that have entered the runtime. This may include threads
    //    even before that thread has executed IL and after that thread no longer has managed
    //    code on its stack.

    // Callback invoked for each thread.
    typedef void (*FP_THREAD_ENUMERATION_CALLBACK)(VMPTR_Thread vmThread, CALLBACK_DATA pUserData);

    virtual
    void EnumerateThreads(FP_THREAD_ENUMERATION_CALLBACK fpCallback, CALLBACK_DATA pUserData) = 0;


    // Check if the thread is dead
    //
    // Arguments:
    //    vmThread - valid thread to check if it's dead.
    //
    // Returns: true if the thread is "dead", which means it can never call managed code again.
    //
    // Notes:
    //    #IsThreadMarkedDead
    //    Threads shutdown states are:
    //    1) Thread is running managed code normally. Thread eventually exits all managed code and
    //    gets to a point where it will never call managed code again.
    //    2) Thread is marked as dead.
    //         - For threads created outside of the runtime (such as a native thread that wanders into
    //         managed code), this mark can happen in DllMain(ThreadDetach)
    //         - For threads created by the runtime (eg, System.Threading.Thread.Start), this may be done
    //         at the top of the threads stack after it calls the user's  Thread-Proc.
    //    3) MAYBE Native thread exits at this point (or it may not). This would be the common case
    //         for threads created outside the runtime.
    //    4) Thread exit event is sent.
    //         - For threads created by the runtime, this may be sent at the top of the thread's
    //         stack (or even when we know that the thread will never execute managed code again)
    //         - For threads created outside the runtime, this is more difficult. A thread can
    //         call into managed code and then return, and then call back into managed code at a
    //         later time (The finalizer does this!). So it's not clear when the native thread
    //         actually exits and will never call managed code again. The only hook we have for
    //         this is DllMain(Thread-Detach). We can mark bits in DllMain, but we can't send
    //         debugger notifications (too dangerous from such a restricted context).
    //         So we may mark the thread as dead, but then sweep later (perhaps on the finalizer
    //         thread), and thus send the Exit events later.
    //    5) Native thread may exit at this point. This is the common case for threads created by
    //         the runtime.
    //
    //    The underlying native thread may have exited at eitehr #3 or #5. Because of this
    //    flexibility, we don't want to rely on native thread exit events.
    //    This function checks if a Thread is passed state #2 (marked as dead). The key invariant
    //    is that once a thread is marked as dead:
    //     - it can never call managed code again.
    //     - it should not be discoverable by DacDbi enumerations.
    //
    //    DBI should prefer relying on IsThreadMarkedDead rather than event notifications (either
    //    managed or native) because tracking events requires that DBI maintain state, which means
    //    that attach + dump cases may break. For example, we want a full dump at the ExitThread
    //    event to have the same view as a live process at the ExitThread event.
    //
    //    We avoid relying on the native thread exit notifications because:
    //    - that's a specific feature of the Win32 debugging API that may not be available on other platforms.
    //    - the only native events the pipeline gets are Exceptions.
    //
    //    Whether a thread is dead can be inferred from the ICorDebug API. However, we have this
    //    on DacDbi to ensure that this definition is consistent with the other DacDbi methods,
    //    especially the enumeration and discovery rules.
    virtual
    bool IsThreadMarkedDead(VMPTR_Thread vmThread) = 0;


    //
    // Return the handle of the specified thread.
    //
    // Arguments:
    //    vmThread - the specified thread
    //
    // Return Value:
    //    the handle of the specified thread
    //
    // @dbgtodo- this should go away in V3. This is useless on a dump.

    virtual
    HANDLE GetThreadHandle(VMPTR_Thread vmThread) = 0;

    //
    // Return the object handle for the managed Thread object corresponding to the specified thread.
    //
    // Arguments:
    //    vmThread - the specified thread
    //
    // Return Value:
    //    This function returns the object handle for the managed Thread object corresponding to the
    //    specified thread.  The return value may be NULL if a managed Thread object has not been created
    //    for the specified thread yet.
    //

    virtual
    VMPTR_OBJECTHANDLE GetThreadObject(VMPTR_Thread vmThread) = 0;
    
    //
    // Get the allocation info corresponding to the specified thread.
    //
    // Arguments:
    //    vmThread - the specified thread
    //    threadAllocInfo - the allocated bytes from SOH and UOH so far on this thread
    //

    virtual
    void GetThreadAllocInfo(VMPTR_Thread vmThread, DacThreadAllocInfo* threadAllocInfo) = 0;

    //
    // Set and reset the TSNC_DebuggerUserSuspend bit on the state of the specified thread
    // according to the CorDebugThreadState.
    //
    // Arguments:
    //    vmThread   - the specified thread
    //    debugState - the desired CorDebugThreadState
    //

    virtual
    void SetDebugState(VMPTR_Thread        vmThread,
                       CorDebugThreadState debugState) = 0;

    //
    // Returns TRUE if this thread has an unhandled exception
    //
    // Arguments:
    //    vmThread   - the thread to query
    //
    // Return Value
    //    TRUE iff this thread has an unhandled exception
    //
    virtual
     BOOL HasUnhandledException(VMPTR_Thread vmThread) = 0;

    //
    // Return the user state of the specified thread.  Most of the state are derived from
    // the ThreadState of the specified thread, e.g. TS_Background, TS_Unstarted, etc.
    // The exception is USER_UNSAFE_POINT, which we need to do a one-frame stackwalk to figure out.
    //
    // Arguments:
    //    vmThread - the specified thread
    //
    // Return Value:
    //    the user state of the specified thread
    //

    virtual
    CorDebugUserState GetUserState(VMPTR_Thread vmThread) = 0;


    //
    // Returns most of the user state of the specified thread,
    // i.e. flags which can be derived from the ThreadState:
    //      USER_STOP_REQUESTED, USER_SUSPEND_REQUESTED, USER_BACKGROUND, USER_UNSTARTED
    //      USER_STOPPED, USER_WAIT_SLEEP_JOIN, USER_SUSPENDED, USER_THREADPOOL
    //
    // Only USER_UNSAFE_POINT is always set to 0, since it takes additional stackwalk.
    // If you need USER_UNSAFE_POINT, use GetUserState(VMPTR_Thread);
    //
    // Arguments:
    //    vmThread - the specified thread
    //
    // Return Value:
    //    the user state of the specified thread
    //
    virtual
    CorDebugUserState GetPartialUserState(VMPTR_Thread vmThread) = 0;


    //
    // Return the connection ID of the specified thread.
    //
    // Arguments:
    //    vmThread - the specified thread
    //
    // Return Value:
    //    the connection ID of the specified thread
    //

    virtual
    CONNID GetConnectionID(VMPTR_Thread vmThread) = 0;

    //
    // Return the task ID of the specified thread.
    //
    // Arguments:
    //    vmThread - the specified thread
    //
    // Return Value:
    //    the task ID of the specified thread
    //

    virtual
    TASKID GetTaskID(VMPTR_Thread vmThread) = 0;

    //
    // Return the OS thread ID of the specified thread
    //
    // Arguments:
    //    vmThread - the specified thread; cannot be NULL
    //
    // Return Value:
    //    the OS thread ID of the specified thread. Returns 0 if not scheduled.
    //

    virtual
    DWORD TryGetVolatileOSThreadID(VMPTR_Thread vmThread) = 0;

    //
    // Return the unique thread ID of the specified thread. The value used for the thread ID changes
    // depending on whether the runtime is being hosted. In non-hosted scenarios, a managed thread will
    // always be associated with the same native thread, and so we can use the OS thread ID as the thread ID
    // for the managed thread. In hosted scenarios, however, a managed thread may run on multiple native
    // threads. It may not even have a backing native thread if it's switched out. Therefore, we can't use
    // the OS thread ID as the thread ID. Instead, we use the internal managed thread ID.
    //
    // Arguments:
    //    vmThread - the specified thread; cannot be NULL
    //
    // Return Value:
    //    Returns a stable and unique thread ID for the lifetime of the specified managed thread.
    //

    virtual
    DWORD GetUniqueThreadID(VMPTR_Thread vmThread) = 0;

    //
    // Return the object handle to the managed Exception object of the current exception
    // on the specified thread.  The return value could be NULL if there is no current exception.
    //
    // Arguments:
    //    vmThread - the specified thread
    //
    // Return Value:
    //    This function returns the object handle to the managed Exception object of the current exception.
    //    The return value may be NULL if there is no exception being processed, or if the specified thread
    //    is an unmanaged thread which has entered and exited the runtime.
    //

    virtual
    VMPTR_OBJECTHANDLE GetCurrentException(VMPTR_Thread vmThread) = 0;

    //
    // Return the object handle to the managed object for a given CCW pointer.
    //
    // Arguments:
    //    ccwPtr - the specified ccw pointer
    //
    // Return Value:
    //    This function returns the object handle to the managed object for a given CCW pointer.
    //

    virtual
    VMPTR_OBJECTHANDLE GetObjectForCCW(CORDB_ADDRESS ccwPtr) = 0;

    //
    // Return the object handle to the managed CustomNotification object of the current notification
    // on the specified thread.  The return value could be NULL if there is no current notification.
    //
    // Arguments:
    //    vmThread - the specified thread on which the notification occurred
    //
    // Return Value:
    //    This function returns the object handle to the managed CustomNotification object of the current notification.
    //    The return value may be NULL if there is no current notification.
    //

    virtual
    VMPTR_OBJECTHANDLE GetCurrentCustomDebuggerNotification(VMPTR_Thread vmThread) = 0;


    //
    // Return the current appdomain the specified thread is in.
    //
    // Arguments:
    //    vmThread - the specified thread
    //
    // Return Value:
    //    the current appdomain of the specified thread
    //
    // Notes:
    //    This function throws if the current appdomain is NULL for whatever reason.
    //

    virtual
    VMPTR_AppDomain GetCurrentAppDomain(VMPTR_Thread vmThread) = 0;


    //
    // Resolve an assembly
    //
    // Arguments:
    //    vmScope - module containing metadata that the token is scoped to.
    //    tkAssemblyRef - assembly ref token to lookup.
    //
    // Returns:
    //    Assembly that the loader/fusion has bound to the given assembly ref.
    //    Returns NULL if the assembly has not yet been loaded (a common case).
    //    Throws on error.
    //
    // Notes:
    //    A single module has metadata that specifies references via tokens. The
    //    loader/fusion goes through tremendous and random policy hoops to determine
    //    which specific file actually gets bound to the reference. This policy includes
    //    things like config files, registry settings, and many other knobs.
    //
    //    The debugger can't duplicate this policy with 100% accuracy, and
    //    so we need DAC to lookup the assembly that was actually loaded.
    virtual
    VMPTR_DomainAssembly ResolveAssembly(VMPTR_DomainFile vmScope, mdToken tkAssemblyRef) = 0;

    //-----------------------------------------------------------------------------
    // Interface for initializing the native/IL sequence points and native var info
    // for a function.
    // Arguments:
    //    input:
    //       vmMethodDesc    MethodDesc of the function
    //       startAddr       starting address of the function--this serves to
    //                       differentiate various EnC versions of the function
    //       fCodePitched    indicates whether code for the function has been pitched
    //       fJitComplete    indicates whether the function has been jitted
    //    output:
    //       pNativeVarData  space for the native code offset information for locals
    //       pSequencePoints space for the IL/native sequence points
    // Return value:
    //    none, but may throw an exception
    // Assumptions:
    //    vmMethodDesc, pNativeVarInfo and pSequencePoints are non-NULL

    // Notes:
    //-----------------------------------------------------------------------------

    virtual
    void GetNativeCodeSequencePointsAndVarInfo(VMPTR_MethodDesc  vmMethodDesc,
                                               CORDB_ADDRESS     startAddress,
                                               BOOL              fCodeAvailabe,
                                               OUT NativeVarData *   pNativeVarData,
                                               OUT SequencePoints *  pSequencePoints) = 0;

    //
    // Return the filter CONTEXT on the LS.  Once we move entirely over to the new managed pipeline
    // built on top of the Win32 debugging API, this won't be necessary.
    //
    // Arguments:
    //    vmThread - the specified thread
    //
    // Return Value:
    //    the filter CONTEXT of the specified thread
    //
    // Notes:
    //    This function should go away when everything is moved OOP and
    //    we don't have a filter CONTEXT on the LS anymore.
    //

    virtual
    VMPTR_CONTEXT GetManagedStoppedContext(VMPTR_Thread vmThread) = 0;

    typedef enum
    {
        kInvalid,
        kManagedStackFrame,
        kExplicitFrame,
        kNativeStackFrame,
        kNativeRuntimeUnwindableStackFrame,
        kAtEndOfStack,
    } FrameType;

    // The stackwalker functions allocate persistent state within DDImpl. Clients can hold onto
    // this via an opaque StackWalkHandle.
    typedef void* * StackWalkHandle;

    //
    // Create a stackwalker on the specified thread and return a handle to it.
    // Initially, the stackwalker is at the filter CONTEXT if there is one.
    // Otherwise it is at the leaf CONTEXT.  It DOES NOT fast forward to the first frame of interest.
    //
    // Arguments:
    //    vmThread               - the specified thread
    //    pInternalContextBuffer - a CONTEXT buffer for the stackwalker to work with
    //    ppSFIHandle            - out parameter; return a handle to the stackwalker
    //
    // Notes:
    //    Call DeleteStackWalk() to delete the stackwalk buffer.
    //    This is a special case that violates the 'no state' tenant.
    //

    virtual
    void CreateStackWalk(VMPTR_Thread           vmThread,
                         DT_CONTEXT *           pInternalContextBuffer,
                         OUT StackWalkHandle *  ppSFIHandle) = 0;

    // Delete the stackwalk object created from CreateStackWalk.
    virtual
    void DeleteStackWalk(StackWalkHandle ppSFIHandle) = 0;

    //
    // Get the CONTEXT of the current frame where the stackwalker is stopped at.
    //
    // Arguments:
    //    pSFIHandle - the handle to the stackwalker
    //    pContext   - OUT: the CONTEXT to be filled out. The context control flags are ignored.
    //

    virtual
    void GetStackWalkCurrentContext(StackWalkHandle pSFIHandle,
                                    DT_CONTEXT *    pContext) = 0;

    //
    // Set the stackwalker to the given CONTEXT.  The CorDebugSetContextFlag indicates whether
    // the CONTEXT is "active", meaning that the IP is point at the current instruction,
    // not the return address of some function call.
    //
    // Arguments:
    //    vmThread   - the current thread
    //    pSFIHandle - the handle to the stackwalker
    //    flag       - flag to indicate whether the specified CONTEXT is "active"
    //    pContext   - the specified CONTEXT. This may make correctional adjustments to the context's IP.
    //

    virtual
    void SetStackWalkCurrentContext(VMPTR_Thread           vmThread,
                                    StackWalkHandle        pSFIHandle,
                                    CorDebugSetContextFlag flag,
                                    DT_CONTEXT *           pContext) = 0;

    //
    // Unwind the stackwalker to the next frame.  The next frame could be any actual stack frame,
    // explicit frame, native marker frame, etc.  Call GetStackWalkCurrentFrameInfo() to find out
    // more about the frame.
    //
    // Arguments:
    //    pSFIHandle - the handle to the stackwalker
    //
    // Return Value:
    //    Return TRUE if we successfully unwind to the next frame.
    //    Return FALSE if there is no more frames to walk.
    //    Throw on error.
    //

    virtual
    BOOL UnwindStackWalkFrame(StackWalkHandle pSFIHandle) = 0;

    //
    // Check whether the specified CONTEXT is valid.  The only check we perform right now is whether the
    // SP in the specified CONTEXT is in the stack range of the thread.
    //
    // Arguments:
    //    vmThread   - the specified thread
    //    pContext   - the CONTEXT to be checked
    //
    // Return Value:
    //    Return S_OK if the CONTEXT passes our checks.
    //    Returns CORDBG_E_NON_MATCHING_CONTEXT if the SP in the specified CONTEXT doesn't fall in the stack
    //         range of the thread.
    //    Throws on error.
    //

    virtual
    HRESULT CheckContext(VMPTR_Thread       vmThread,
                         const DT_CONTEXT * pContext) = 0;

    //
    // Fill in the DebuggerIPCE_STRData structure with information about the current frame
    // where the stackwalker is stopped at.
    //
    // Arguments:
    //    pSFIHandle - the handle to the stackwalker
    //    pFrameData - the DebuggerIPCE_STRData to be filled out;
    //                 it can be NULL if you just want to know the frame type
    //
    // Return Value:
    //    Return the type of the current frame
    //

    virtual
    FrameType GetStackWalkCurrentFrameInfo(StackWalkHandle                 pSFIHandle,
                                           OPTIONAL DebuggerIPCE_STRData * pFrameData) = 0;

    //
    // Return the number of internal frames on the specified thread.
    //
    // Arguments:
    //    vmThread - the thread whose internal frames are being retrieved
    //
    // Return Value:
    //    Return the number of internal frames.
    //
    // Notes:
    //    Explicit frames are "marker objects" the runtime pushes on the stack to mark special places, e.g.
    //    appdomain transition, managed-to- unmanaged transition, etc.  Internal frames are only a subset of
    //    explicit frames.  Explicit frames which are not interesting to the debugger are not exposed (e.g.
    //    GCFrame).  Internal frames are interesting to the debugger if they have a CorDebugInternalFrameType
    //    other than STUBFRAME_NONE.
    //
    //    The user should call this function before code:IDacDbiInterface::EnumerateInternalFrames to figure
    //    out how many interesting internal frames there are.
    //

    virtual
    ULONG32 GetCountOfInternalFrames(VMPTR_Thread vmThread) = 0;

    //
    // Enumerate the internal frames on the specified thread and invoke the provided callback on each of
    // them.  Information about the internal frame is stored in the DebuggerIPCE_STRData.
    //
    // Arguments:
    //    vmThread - the thread to be walked fpCallback - callback function invoked on each internal frame
    //    pUserData - user-specified custom data
    //
    // Notes:
    //    The user can call code:IDacDbiInterface::GetCountOfInternalFrames to figure out how many internal
    //    frames are on the thread before calling this function. Also, refer to the comment of that function
    //    to find out more about internal frames.
    //

    typedef void (*FP_INTERNAL_FRAME_ENUMERATION_CALLBACK)(const DebuggerIPCE_STRData * pFrameData, CALLBACK_DATA pUserData);

    virtual
    void EnumerateInternalFrames(VMPTR_Thread                            vmThread,
                                 FP_INTERNAL_FRAME_ENUMERATION_CALLBACK  fpCallback,
                                 CALLBACK_DATA                           pUserData) = 0;

    //
    // Given the FramePointer of the parent frame and the FramePointer of the current frame,
    // check if the current frame is the parent frame.  fpParent should have been returned
    // previously by the DacDbiInterface via GetStackWalkCurrentFrameInfo().
    //
    // Arguments:
    //    fpToCheck - the FramePointer of the current frame
    //    fpParent  - the FramePointer of the parent frame; should have been returned earlier by the DDI
    //
    // Return Value:
    //    Return TRUE if the current frame is indeed the parent frame
    //
    // Note:
    //    Because of the complexity involved in checking for the parent frame, we should always
    //    ask the ExceptionTracker to do it.
    //

    virtual
    BOOL IsMatchingParentFrame(FramePointer fpToCheck, FramePointer fpParent) = 0;

    //
    // Return the stack parameter size of a given method.  This is necessary on x86 for unwinding.
    //
    // Arguments:
    //    controlPC - any address in the specified method; you can use the current PC of the stack frame
    //
    // Return Value:
    //    Return the size of the stack parameters of the given method.
    //    Return 0 for vararg methods.
    //
    // Assumptions:
    //    The callee stack parameter size is constant throughout a method.
    //

    virtual
    ULONG32 GetStackParameterSize(CORDB_ADDRESS controlPC) = 0;

    //
    // Return the FramePointer of the current frame where the stackwalker is stopped at.
    //
    // Arguments:
    //    pSFIHandle - the handle to the stackwalker
    //
    // Return Value:
    //    the FramePointer of the current frame
    //
    // Notes:
    //    The FramePointer of a stack frame is:
    //    the stack address of the return address on x86,
    //    the current SP on AMD64,
    //
    //    On x86, to get the stack address of the return address, we need to unwind one more frame
    //    and use the SP of the caller frame as the FramePointer of the callee frame.  This
    //    function does NOT do that.  It just returns the SP.  The caller needs to handle the
    //    unwinding.
    //
    //    The FramePointer of an explicit frame is just the stack address of the explicit frame.
    //

    virtual
    FramePointer GetFramePointer(StackWalkHandle pSFIHandle) = 0;

    //
    // Check whether the specified CONTEXT is the CONTEXT of the leaf frame.  This function doesn't care
    // whether the leaf frame is native or managed.
    //
    // Arguments:
    //    vmThread  - the specified thread
    //    pContext  - the CONTEXT to check
    //
    // Return Value:
    //    Return TRUE if the specified CONTEXT is the leaf CONTEXT.
    //
    // Notes:
    //    Currently we check the specified CONTEXT against the filter CONTEXT first.
    //    This will be deprecated in V3.
    //

    virtual
    BOOL IsLeafFrame(VMPTR_Thread       vmThread,
                     const DT_CONTEXT * pContext) = 0;

    // Get the context for a particular thread of the target process.
    // Arguments:
    //     input:  vmThread       - the thread for which the context is required
    //     output: pContextBuffer - the address of the CONTEXT to be initialized.
    //                              The memory for this belongs to the caller. It must not be NULL.
    // Note: throws
    virtual
    void GetContext(VMPTR_Thread vmThread, DT_CONTEXT * pContextBuffer) = 0;

    //
    // This is a simple helper function to convert a CONTEXT to a DebuggerREGDISPLAY.  We need to do this
    // inside DDI because the RS has no notion of REGDISPLAY.
    //
    // Arguments:
    //    pInContext - the CONTEXT to be converted
    //    pOutDRD    - the converted DebuggerREGDISPLAY
    //    fActive    - Indicate whether the CONTEXT is active or not.  An active CONTEXT means that the
    //                 IP is the next instruction to be executed, not the return address of a function call.
    //                 The opposite of an active CONTEXT is an unwind CONTEXT, which is obtained from
    //                 unwinding.
    //

    virtual
    void ConvertContextToDebuggerRegDisplay(const DT_CONTEXT * pInContext,
                                            DebuggerREGDISPLAY * pOutDRD,
                                            BOOL fActive) = 0;

    typedef enum
    {
        kNone,
        kILStub,
        kLCGMethod,
    } DynamicMethodType;

    //
    // Check whether the specified method is an IL stub or an LCG method.  This answer determines if we
    // need to expose the method in a V2-style stackwalk.
    //
    // Arguments:
    //    vmMethodDesc - the method to be checked
    //
    // Return Value:
    //    Return kNone if the method is neither an IL stub or an LCG method.
    //    Return kILStub if the method is an IL stub.
    //    Return kLCGMethod if the method is an LCG method.
    //

    virtual
    DynamicMethodType IsILStubOrLCGMethod(VMPTR_MethodDesc vmMethodDesc) = 0;

    //
    // Return a TargetBuffer for the raw vararg signature.
    // Also return the address of the first argument in the vararg signature.
    //
    // Arguments:
    //    VASigCookieAddr - the target address of the VASigCookie pointer (double indirection)
    //    pArgBase        - out parameter; return the target address of the first word of the arguments
    //
    // Return Value:
    //    Return a TargetBuffer for the raw vararg signature.
    //
    // Notes:
    //    We can't take a VMPTR here because VASigCookieAddr does not come from the DDI.  Instead,
    //    we use the native variable information to figure out which stack slot contains the
    //    VASigCookie pointer.  So a remote address is all we have got.
    //
    //    Ideally we should be able to return just a SigParser, but doing so has a not-so-trivial problem.
    //    The memory used for the signature pointed to by the SigParser cannot be allocated in the DAC cache,
    //    since it'll be used by mscordbi.  We don't have a clean way to allocate memory in mscordbi without
    //    breaking the Signature abstraction.
    //
    //    The other option would be to create a new sub-type like "SignatureCopy" which allocates and frees
    //    its own backing memory.  Currently we don't want to share heaps between mscordacwks.dll and
    //    mscordbi.dll, and so we would have to jump through some hoops to allocate with an allocator
    //    in mscordbi.dll.
    //

    virtual
    TargetBuffer GetVarArgSig(CORDB_ADDRESS   VASigCookieAddr,
                              OUT CORDB_ADDRESS * pArgBase) = 0;

    //
    // Indicates if the specified type requires 8-byte alignment.
    //
    // Arguments:
    //    thExact - the exact TypeHandle of the type to query
    //
    // Return Value:
    //    TRUE if the type requires 8-byte alignment.
    //

    virtual
    BOOL RequiresAlign8(VMPTR_TypeHandle thExact) = 0;

    //
    // Resolve the raw generics token to the real generics type token.  The resolution is based on the
    // given index.  See Notes below.
    //
    // Arguments:
    //    dwExactGenericArgsTokenIndex - the variable index of the generics type token
    //    rawToken                     - the raw token to be resolved
    //
    // Return Value:
    //    Return the actual generics type token.
    //
    // Notes:
    //    DDI tells the RS which variable stores the generics type token, but DDI doesn't retrieve the value
    //    of the variable itself.  Instead, the RS retrieves the value of the variable.  However,
    //    in some cases, the variable value is not the generics type token.  In this case, we need to
    //    "resolve" the variable value to the generics type token.  The RS should call this API to do that.
    //
    //    If the index is 0, then the generics type token is the MethodTable of the "this" object.
    //    rawToken will be the address of the "this" object.
    //
    //    If the index is TYPECTXT_ILNUM, the generics type token is a secret argument.
    //    It could be a MethodDesc or a MethodTable, and in this case no resolution is actually necessary.
    //    rawToken will be the actual secret argument, and this API really is just a nop.
    //
    //    However, we don't want the RS to know all this logic.
    //

    virtual
    GENERICS_TYPE_TOKEN ResolveExactGenericArgsToken(DWORD               dwExactGenericArgsTokenIndex,
                                                     GENERICS_TYPE_TOKEN rawToken) = 0;

    //-----------------------------------------------------------------------------
    // Functions to get information about code objects
    //-----------------------------------------------------------------------------

    // GetILCodeAndSig returns the function's ILCode and SigToken given
    // a module and a token. The info will come from a MethodDesc, if
    // one exists or from metadata.
    //
    // Arguments:
    //    Input:
    //    vmDomainFile   - module containing metadata for the method
    //    functionToken  - metadata token for the function
    //    Output (required):
    //    codeInfo       - start address and size of the IL
    //    pLocalSigToken - signature token for the method
    virtual
    void GetILCodeAndSig(VMPTR_DomainFile vmDomainFile,
                         mdToken          functionToken,
                         OUT TargetBuffer *   pCodeInfo,
                         OUT mdToken *        pLocalSigToken) = 0;

    // Gets information about a native code blob:
    //    it's method desc, whether it's an instantiated generic, its EnC version number
    //    and hot and cold region information.
    // Arguments:
    //    Input:
    //        vmDomainFile  - module containing metadata for the method
    //        functionToken - token for the function for which we need code info
    //    Output (required):
    //        pCodeInfo     - data structure describing the native code regions.
    // Notes: If the function is unjitted, the method desc will be NULL and the
    //        output parameter will be invalid. In general, if the native start address
    //        is unavailable for any reason, the output parameter will also be
    //        invalid (i.e., pCodeInfo->IsValid is false).

    virtual
    void GetNativeCodeInfo(VMPTR_DomainFile         vmDomainFile,
                           mdToken                  functionToken,
                           OUT NativeCodeFunctionData * pCodeInfo) = 0;

    // Gets information about a native code blob:
    //    it's method desc, whether it's an instantiated generic, its EnC version number
    //    and hot and cold region information.
    //    This is similar to function above, just works from a different starting point
    //    Also this version can get info for any particular EnC version instance
    //    because they all have different start addresses whereas the above version gets
    //    the most recent one
    // Arguments:
    //    Input:
    //        hotCodeStartAddr  - the beginning of the code hot code region
    //    Output (required):
    //        pCodeInfo     - data structure describing the native code regions.

    virtual
    void GetNativeCodeInfoForAddr(VMPTR_MethodDesc    vmMethodDesc,
                                  CORDB_ADDRESS hotCodeStartAddr,
                                  NativeCodeFunctionData * pCodeInfo) = 0;

    //-----------------------------------------------------------------------------
    // Functions to get information about types
    //-----------------------------------------------------------------------------

    // Determine if a type is a ValueType
    //
    // Arguments:
    //     input:  vmTypeHandle  - the type being checked (works even on unrestored types)
    //
    // Return:
    //        TRUE iff the type is a ValueType

    virtual
    BOOL IsValueType (VMPTR_TypeHandle th) = 0;

    // Determine if a type has generic parameters
    //
    // Arguments:
    //     input:  vmTypeHandle  - the type being checked (works even on unrestored types)
    //
    // Return:
    //        TRUE iff the type has generic parameters

    virtual
    BOOL HasTypeParams (VMPTR_TypeHandle th) = 0;

    // Get type information for a class
    //
    // Arguments:
    //     input:  vmAppDomain   - appdomain where we will fetch field data for the type
    //             thExact       - exact type handle for type
    //     output:
    //             pData         - structure containing information about the class and its
    //                             fields

    virtual
    void GetClassInfo (VMPTR_AppDomain  vmAppDomain,
                       VMPTR_TypeHandle thExact,
                       ClassInfo *      pData) = 0;

    // get field information and object size for an instantiated generic
    //
    // Arguments:
    //     input:  vmDomainFile  - module containing metadata for the type
    //             thExact       - exact type handle for type (may be NULL)
    //             thApprox      - approximate type handle for the type
    //     output:
    //             pFieldList    - array of structures containing information about the fields. Clears any previous
    //                             contents. Allocated and initialized by this function.
    //             pObjectSize   - size of the instantiated object
    //
    virtual
    void GetInstantiationFieldInfo (VMPTR_DomainFile             vmDomainFile,
                                    VMPTR_TypeHandle             vmThExact,
                                    VMPTR_TypeHandle             vmThApprox,
                                    OUT DacDbiArrayList<FieldData> * pFieldList,
                                    OUT SIZE_T *                     pObjectSize) = 0;

    // use a type handle to get the information needed to create the corresponding RS CordbType instance
    //
    // Arguments:
    //     input:  boxed        - indicates what, if anything, is boxed. See code:AreValueTypesBoxed for more
    //                            specific information
    //             vmAppDomain  - module containing metadata for the type
    //             vmTypeHandle - type handle for the type
    //     output: pTypeInfo    - holds information needed to build the corresponding CordbType
    //
    virtual
    void TypeHandleToExpandedTypeInfo(AreValueTypesBoxed                       boxed,
                                      VMPTR_AppDomain                          vmAppDomain,
                                      VMPTR_TypeHandle                         vmTypeHandle,
                                      DebuggerIPCE_ExpandedTypeData *          pTypeInfo) = 0;

    virtual
    void GetObjectExpandedTypeInfo(AreValueTypesBoxed                   boxed,
                                   VMPTR_AppDomain                      vmAppDomain,
                                   CORDB_ADDRESS                        addr,
                                   OUT DebuggerIPCE_ExpandedTypeData *  pTypeInfo) = 0;


    virtual
    void GetObjectExpandedTypeInfoFromID(AreValueTypesBoxed                   boxed,
                                         VMPTR_AppDomain                      vmAppDomain,
                                         COR_TYPEID                           id,
                                         OUT DebuggerIPCE_ExpandedTypeData *  pTypeInfo) = 0;


    // Get type handle for a TypeDef token, if one exists. For generics this returns the open type.
    // Note there is no guarantee the returned handle will be fully restored (in pre-jit scenarios),
    // only that it exists. Later functions that use this type handle should fail if they require
    // information not yet available at the current restoration level
    //
    // Arguments:
    //     input: vmModule      - the module scope in which to look up the type def
    //            metadataToken - the type definition to retrieve
    //
    // Return value: the type handle if it exists or throws CORDBG_E_CLASS_NOT_LOADED if it isn't loaded
    //
    virtual
    VMPTR_TypeHandle GetTypeHandle(VMPTR_Module vmModule,
                                   mdTypeDef metadataToken) = 0;

    // Get the approximate type handle for an instantiated type. This may be identical to the exact type handle,
    // but if we have code sharing for generics, it may differ in that it may have canonical type parameters.
    // This will occur if we have not yet loaded an exact type but we have loaded the canonical form of the
    // type.
    //
    // Arguments:
    //     input: pTypeData  - information needed to get the type handle, this includes a list of type parameters
    //                         and the number of entries in the list. Allocated and initialized by the caller.
    // Return value: the approximate type handle
    //
    virtual
    VMPTR_TypeHandle GetApproxTypeHandle(TypeInfoList * pTypeData) = 0;

    // Get the exact type handle from type data.
    // Arguments:
    //     input: pTypeData        - type information for the type. includes information about
    //                               the top-level type as well as information
    //                               about the element type for array types, the referent for
    //                               pointer types, or actual parameters for generic class or
    //                               valuetypes, as appropriate for the top-level type.
    //            pArgInfo         - This is preallocated and initialized by the caller and contains two fields:
    //                 genericArgsCount - number of type parameters (these may be actual type parameters
    //                                    for generics or they may represent the element type or referent
    //                                    type.
    //                 pGenericArgData  - list of type parameters
    //                 vmTypeHandle     - the exact type handle derived from the type information
    // Return Value: an HRESULT indicating the result of the operation
    virtual
    HRESULT GetExactTypeHandle(DebuggerIPCE_ExpandedTypeData * pTypeData,
                               ArgInfoList *   pArgInfo,
                               VMPTR_TypeHandle& vmTypeHandle) = 0;

    //
    // Retrieve the generic type params for a given MethodDesc.  This function is specifically
    // for stackwalking because it requires the generic type token on the stack.
    //
    // Arguments:
    //    vmAppDomain   - the appdomain of the MethodDesc
    //    vmMethodDesc  - the method in question
    //    genericsToken - the generic type token in the stack frame owned by the method
    //
    //    pcGenericClassTypeParams - out parameter; returns the number of type parameters for the class
    //                               containing the method in question; must not be NULL
    //    pGenericTypeParams       - out parameter; returns an array of type parameters and
    //                               the count of the total number of type parameters; must not be NULL
    //
    // Notes:
    //    The memory for the array is allocated by this function on the Dbi heap.
    //    The caller is responsible for releasing it.
    //

    virtual
    void GetMethodDescParams(VMPTR_AppDomain     vmAppDomain,
                             VMPTR_MethodDesc    vmMethodDesc,
                             GENERICS_TYPE_TOKEN genericsToken,
                             OUT UINT32 *            pcGenericClassTypeParams,
                             OUT TypeParamsList *    pGenericTypeParams) = 0;

    // Get the target field address of a thread local static.
    // Arguments:
    //     input: vmField         - pointer to the field descriptor for the static field
    //            vmRuntimeThread - thread to which the static field belongs. This must
    //                              NOT be NULL
    // Return Value: The target address of the field if the field is allocated.
    //               NULL if the field storage is not yet allocated.
    //
    // Note:
    //  Static field storage is lazily allocated, so this may commonly return NULL.
    //  This is an inspection only method and can not allocate the static storage.
    //  Field storage is constant once allocated, so this value can be cached.

    virtual
    CORDB_ADDRESS GetThreadStaticAddress(VMPTR_FieldDesc vmField,
                                         VMPTR_Thread    vmRuntimeThread) = 0;

    // Get the target field address of a collectible types static.
    // Arguments:
    //     input: vmField         - pointer to the field descriptor for the static field
    //            vmAppDomain     - AppDomain to which the static field belongs. This must
    //                              NOT be NULL
    // Return Value: The target address of the field if the field is allocated.
    //               NULL if the field storage is not yet allocated.
    //
    // Note:
    //  Static field storage may not exist yet, so this may commonly return NULL.
    //  This is an inspection only method and can not allocate the static storage.
    //  Field storage is not constant once allocated so this value can not be cached
    //  across a Continue

    virtual
    CORDB_ADDRESS GetCollectibleTypeStaticAddress(VMPTR_FieldDesc vmField,
                                                  VMPTR_AppDomain vmAppDomain) = 0;

    // Get information about a field added with Edit And Continue.
    // Arguments:
    //     intput:  pEnCFieldInfo - information about the EnC added field including:
    //                              object to which it belongs (if this is null the field is static)
    //                              the field token
    //                              the class token for the class to which the field was added
    //                              the offset to the fields
    //                              the domain file
    //                              an indication of the type: whether it's a class or value type
    //     output:  pFieldData    - information about the EnC added field
    //              pfStatic      - flag to indicate whether the field is static
    virtual
    void GetEnCHangingFieldInfo(const EnCHangingFieldInfo * pEnCFieldInfo,
                                OUT FieldData *           pFieldData,
                                OUT BOOL *                pfStatic) = 0;


    // GetTypeHandleParams gets the necessary data for a type handle, i.e. its
    // type parameters, e.g. "String" and "List<int>" from the type handle
    // for "Dict<String,List<int>>", and sends it back to the right side.
    // Arguments:
    //    input:  vmAppDomain  - app domain to which the type belongs
    //            vmTypeHandle - type handle for the type
    //    output: pParams      - list of instances of DebuggerIPCE_ExpandedTypeData,
    //                           one for each type parameter. These will be used on the
    //                           RS to build up an instantiation which will allow
    //                           building an instance of CordbType for the top-level
    //                           type. The memory for this list is allocated on the dbi
    //                           heap in this function.
    // This will not fail except for OOM

    virtual
    void GetTypeHandleParams(VMPTR_AppDomain  vmAppDomain,
                             VMPTR_TypeHandle vmTypeHandle,
                             OUT TypeParamsList * pParams) = 0;

    // GetSimpleType
    // gets the metadata token and domain file corresponding to a simple type
    // Arguments:
    //     input:  vmAppDomain - Appdomain in which simpleType resides
    //             simpleType  - CorElementType value corresponding to a simple type
    //     output: pMetadataToken - the metadata token corresponding to simpleType,
    //                              in the scope of vmDomainFile.
    //             vmDomainFile   - the domainFile for simpleType
    // Notes:
    //    This is inspection-only. If the type is not yet loaded, it will throw CORDBG_E_CLASS_NOT_LOADED.
    //    It will not try to load a type.
    //    If the type has been loaded, vmDomainFile will be non-null unless the target is somehow corrupted.
    //    In that case, we will throw CORDBG_E_TARGET_INCONSISTENT.

    virtual
    void GetSimpleType(VMPTR_AppDomain    vmAppDomain,
                       CorElementType     simpleType,
                       OUT mdTypeDef *        pMetadataToken,
                       OUT VMPTR_Module     * pVmModule,
                       OUT VMPTR_DomainFile * pVmDomainFile) = 0;

    // for the specified object returns TRUE if the object derives from System.Exception
    virtual
    BOOL IsExceptionObject(VMPTR_Object vmObject) = 0;

    // gets the list of raw stack frames for the specified exception object
    virtual
    void GetStackFramesFromException(VMPTR_Object vmObject, DacDbiArrayList<DacExceptionCallStackData>& dacStackFrames) = 0;

    // Returns true if the argument is a runtime callable wrapper
    virtual
    BOOL IsRcw(VMPTR_Object vmObject) = 0;

    // retrieves the list of COM interfaces implemented by vmObject, as it is known at
    // the time of the call (the list may change as new interface types become available
    // in the runtime)
    virtual
    void GetRcwCachedInterfaceTypes(
                        VMPTR_Object vmObject,
                        VMPTR_AppDomain vmAppDomain,
                        BOOL bIInspectableOnly,
                        OUT DacDbiArrayList<DebuggerIPCE_ExpandedTypeData> * pDacInterfaces) = 0;

    // retrieves the list of interfaces pointers implemented by vmObject, as it is known at
    // the time of the call (the list may change as new interface types become available
    // in the runtime)
    virtual
    void GetRcwCachedInterfacePointers(
                        VMPTR_Object vmObject,
                        BOOL bIInspectableOnly,
                        OUT DacDbiArrayList<CORDB_ADDRESS> * pDacItfPtrs) = 0;

    // retrieves a list of interface types corresponding to the passed in
    // list of IIDs. the interface types are retrieved from an app domain
    // IID / Type cache, that is updated as new types are loaded. will
    // have NULL entries corresponding to unknown IIDs in "iids"
    virtual
    void GetCachedWinRTTypesForIIDs(
                        VMPTR_AppDomain vmAppDomain,
    					DacDbiArrayList<GUID> & iids,
	    				OUT DacDbiArrayList<DebuggerIPCE_ExpandedTypeData> * pTypes) = 0;

    // retrieves the whole app domain cache of IID / Type mappings.
    virtual
    void GetCachedWinRTTypes(
                        VMPTR_AppDomain vmAppDomain,
                        OUT DacDbiArrayList<GUID> * piids,
                        OUT DacDbiArrayList<DebuggerIPCE_ExpandedTypeData> * pTypes) = 0;


    // ----------------------------------------------------------------------------
    // functions to get information about reference/handle referents for ICDValue
    // ----------------------------------------------------------------------------

    // Get object information for a TypedByRef object. Initializes the objRef and typedByRefType fields of
    // pObjectData (type info for the referent).
    // Arguments:
    //     input:  pTypedByRef - pointer to a TypedByRef struct
    //             vmAppDomain - AppDomain for the type of the object referenced
    //     output: pObjectData - information about the object referenced by pTypedByRef
    // Note: Throws
    virtual
    void GetTypedByRefInfo(CORDB_ADDRESS             pTypedByRef,
                           VMPTR_AppDomain           vmAppDomain,
                           DebuggerIPCE_ObjectData * pObjectData) = 0;

    // Get the string length and offset to string base for a string object
    // Arguments:
    //     input:  objPtr - address of a string object
    //     output: pObjectData - fills in the string fields stringInfo.offsetToStringBase and
    //             stringInfo.length
    // Note: throws
    virtual
    void GetStringData(CORDB_ADDRESS objectAddress, DebuggerIPCE_ObjectData * pObjectData) = 0;

    // Get information for an array type referent of an objRef, including rank, upper and lower bounds,
    // element size and type, and the number of elements.
    // Arguments:
    //     input:  objectAddress - the address of an array object
    //     output: pObjectData   - fills in the array-related fields:
    //                             arrayInfo.offsetToArrayBase,
    //                             arrayInfo.offsetToLowerBounds,
    //                             arrayInfo.offsetToUpperBounds,
    //                             arrayInfo.componentCount,
    //                             arrayInfo.rank,
    //                             arrayInfo.elementSize,
    // Note: throws
    virtual
    void GetArrayData(CORDB_ADDRESS objectAddress, DebuggerIPCE_ObjectData * pObjectData) = 0;

    // Get information about an object for which we have a reference, including the object size and
    // type information.
    // Arguments:
    //     input:  objectAddress - address of the object for which we want information
    //             type          - the basic type of the object (we may find more specific type
    //                             information for the object)
    //             vmAppDomain   - the appdomain to which the object belong
    //     output: pObjectData   - fills in the size and type information fields
    // Note: throws
    virtual
    void GetBasicObjectInfo(CORDB_ADDRESS             objectAddress,
                            CorElementType            type,
                            VMPTR_AppDomain           vmAppDomain,
                            DebuggerIPCE_ObjectData * pObjectData) = 0;

    // --------------------------------------------------------------------------------------------
#ifdef TEST_DATA_CONSISTENCY
    // Determine whether a crst is held by the left side. When the DAC is executing VM code that takes a
    // lock, we want to know whether the LS already holds that lock. If it does, we will assume the locked
    // data is in an inconsistent state and will throw an exception, rather than relying on this data. This
    // function is part of a self-test that will ensure we are correctly detecting when the LS holds a lock
    // on data the RS is trying to inspect.
    // Argument:
    //     input:  vmCrst    - the lock to test
    //     output: none
    // Notes:
    //     Throws
    //     For this code to run, the environment variable TestDataConsistency must be set to 1.
    virtual
    void TestCrst(VMPTR_Crst vmCrst) = 0;

    // Determine whether a crst is held by the left side. When the DAC is executing VM code that takes a
    // lock, we want to know whether the LS already holds that lock. If it does, we will assume the locked
    // data is in an inconsistent state and will throw an exception, rather than relying on this data. This
    // function is part of a self-test that will ensure we are correctly detecting when the LS holds a lock
    // on data the RS is trying to inspect.
    // Argument:
    //     input:  vmRWLock  - the lock to test
    //     output: none
    // Notes:
    //     Throws
    //     For this code to run, the environment variable TestDataConsistency must be set to 1.

    virtual
    void TestRWLock(VMPTR_SimpleRWLock vmRWLock) = 0;
#endif
    // --------------------------------------------------------------------------------------------
    // Get the address of the Debugger control block on the helper thread. The debugger control block
    // contains information about the status of the debugger, handles to various events and space to hold
    // information sent back and forth between the debugger and the debuggee's helper thread.
    // Arguments: none
    // Return Value: The remote address of the Debugger control block allocated on the helper thread
    //               if it has been successfully allocated or NULL otherwise.
    virtual
    CORDB_ADDRESS GetDebuggerControlBlockAddress() = 0;

    // Creates a VMPTR of an Object. The Object is found by dereferencing ptr
    // as though it is a target address to an OBJECTREF. This is similar to
    // GetObject with another level of indirection.
    //
    // Arguments:
    //    ptr     - A target address pointing to an OBJECTREF
    //
    // Return Value:
    //    A VMPTR to the Object which ptr points to
    //
    // Notes:
    //    The VMPTR this produces can be deconstructed by GetObjectContents.
    //    This function will throw if given a NULL or otherwise invalid pointer,
    //    but if given a valid address to an invalid pointer, it will produce
    //    a VMPTR_Object which points to invalid memory.
    virtual
    VMPTR_Object GetObjectFromRefPtr(CORDB_ADDRESS ptr) = 0;

    // Creates a VMPTR of an Object. The Object is assumed to be at the target
    // address supplied by ptr
    //
    // Arguments:
    //    ptr     - A target address to an Object
    //
    // Return Value:
    //    A VMPTR to the Object which was at ptr
    //
    // Notes:
    //    The VMPTR this produces can be deconstructed by GetObjectContents.
    //    This will produce a VMPTR_Object regardless of whether the pointer is
    //    valid or not.
    virtual
    VMPTR_Object GetObject(CORDB_ADDRESS ptr) = 0;

    // Sets state in the native binder.
    //
    // Arguments:
    //    ePolicy - the NGEN policy to change
    //
    // Return Value:
    //    HRESULT indicating if the state was successfully updated
    //
    virtual
    HRESULT EnableNGENPolicy(CorDebugNGENPolicy ePolicy) = 0;

    // Sets the NGEN compiler flags. This restricts NGEN to only use images with certain
    // types of pregenerated code. With respect to debugging this is used to specify that
    // the NGEN image must be debuggable aka non-optimized code. Note that these flags
    // are merged with other sources of configuration so it is possible that the final
    // result retrieved from GetDesiredNGENCompilerFlags does not match what was specfied
    // in this call.
    //
    // If an NGEN image of the appropriate type isn't available then one of two things happens:
    // a) the NGEN image isn't loaded and CLR loads the MSIL image instead
    // b) the NGEN image is loaded, but we don't use the pregenerated code it contains
    //    and instead use only the MSIL and metadata
    //
    // This function is only legal to call at app startup before any decisions have been
    // made about NGEN image loading. Once we begin loading this configuration is immutable.
    //
    //
    // Arguments:
    //    dwFlags - the new NGEN compiler flags that should go into effect
    //
    // Return Value:
    //    HRESULT indicating if the state was successfully updated. On error the
    //    current flags in effect will not have changed.
    //
    virtual
    HRESULT SetNGENCompilerFlags(DWORD dwFlags) = 0;

    // Gets the NGEN compiler flags currently in effect. This accounts for settings that
    // were caused by SetDesiredNGENCompilerFlags as well as other configuration sources.
    // See SetDesiredNGENCompilerFlags for more info
    //
    // Arguments:
    //    pdwFlags - the NGEN compiler flags currently in effect
    //
    // Return Value:
    //    HRESULT indicating if the state was successfully retrieved.
    //
    virtual
    HRESULT GetNGENCompilerFlags(DWORD *pdwFlags) = 0;

    // Create a VMPTR_OBJECTHANDLE from a CORDB_ADDRESS pointing to an object handle
    //
    // Arguments:
    //     handle: target address of a GC handle
    //
    // ReturnValue:
    //     returns a VMPTR_OBJECTHANDLE with the handle as the m_addr field
    //
    // Notes:
    //     This will produce a VMPTR_OBJECTHANDLE regardless of whether handle is
    //     valid.
    //     Ideally we'd be using only strongly-typed variables on the RS, and then this would be unnecessary
    virtual
    VMPTR_OBJECTHANDLE GetVmObjectHandle(CORDB_ADDRESS handleAddress) = 0;

    // Validate that the VMPTR_OBJECTHANDLE refers to a legitimate managed object
    //
    // Arguments:
    //     handle: the GC handle to be validated
    //
    // Return value:
    //     TRUE if the object appears to be valid (its a heuristic), FALSE if it definately is not valid
    //
    virtual
    BOOL IsVmObjectHandleValid(VMPTR_OBJECTHANDLE vmHandle) = 0;

    // indicates if the specified module is a WinRT module
    //
    // Arguments:
    //     vmModule: the module to check
    //     isWinRT: out parameter indicating state of module
    //
    // Return value:
    //     S_OK indicating that the operation succeeded
    //
    virtual
    HRESULT IsWinRTModule(VMPTR_Module vmModule, BOOL& isWinRT) = 0;

    // Determines the app domain id for the object refered to by a given VMPTR_OBJECTHANDLE
    //
    // Arguments:
    //     handle: the GC handle which refers to the object of interest
    //
    // Return value:
    //     The app domain id of the object of interest
    //
    // This may throw if the object handle is corrupt (it doesn't refer to a managed object)
    virtual
    ULONG GetAppDomainIdFromVmObjectHandle(VMPTR_OBJECTHANDLE vmHandle) = 0;


    // Get the target address from a VMPTR_OBJECTHANDLE, i.e., the handle address
    // Arguments:
    //     vmHandle - (input) the VMPTR_OBJECTHANDLE from which we need the target address
    // Return value: the target address from the VMPTR_OBJECTHANDLE
    //
    virtual
    CORDB_ADDRESS GetHandleAddressFromVmHandle(VMPTR_OBJECTHANDLE vmHandle) = 0;

    // Given a VMPTR to an Object return the target address
    //
    // Arguments:
    //    obj      - the Object VMPTR to get the address from
    //
    // Return Value:
    //    Return the target address which obj is using
    //
    // Notes:
    //    The VMPTR this consumes can be reconstructed using GetObject and
    //    providing the address stored in the returned TargetBuffer. This has
    //    undefined behavior for invalid VMPTR_Objects.

    virtual
    TargetBuffer GetObjectContents(VMPTR_Object obj) = 0;

    // The callback used to enumerate blocking objects
    typedef void (*FP_BLOCKINGOBJECT_ENUMERATION_CALLBACK)(DacBlockingObject blockingObject,
                                                           CALLBACK_DATA pUserData);

    //
    // Enumerate all monitors blocking a thread
    //
    // Arguments:
    //    vmThread     - the thread to get monitor data for
    //    fpCallback   - callback to invoke on the blocking data for each monitor
    //    pUserData    - user data to supply for each callback.
    //
    // Return Value:
    //    Returns on success. Throws on error.
    //
    //
    virtual
    void EnumerateBlockingObjects(VMPTR_Thread                           vmThread,
                                  FP_BLOCKINGOBJECT_ENUMERATION_CALLBACK fpCallback,
                                  CALLBACK_DATA                          pUserData) = 0;



    //
    // Returns the thread which owns the monitor lock on an object and the acquisition
    // count
    //
    // Arguments:
    //    vmObject          - The object to check for ownership

    //
    // Return Value:
    //    Throws on error. Inside the structure we have:
    //    pVmThread         - the owning or thread or VMPTR_Thread::NullPtr() if unowned
    //    pAcquisitionCount - the number of times the lock would need to be released in
    //                        order for it to be unowned
    //
    virtual
    MonitorLockInfo GetThreadOwningMonitorLock(VMPTR_Object vmObject) = 0;

    //
    // Enumerate all threads waiting on the monitor event for an object
    //
    // Arguments:
    //    vmObject     - the object whose monitor event we are interested in
    //    fpCallback   - callback to invoke on each thread in the queue
    //    pUserData    - user data to supply for each callback.
    //
    // Return Value:
    //    Returns on success. Throws on error.
    //
    //
    virtual
    void EnumerateMonitorEventWaitList(VMPTR_Object                   vmObject,
                                       FP_THREAD_ENUMERATION_CALLBACK fpCallback,
                                       CALLBACK_DATA                  pUserData) = 0;

    //
    // Returns the managed debugging flags for the process (a combination
    // of the CLR_DEBUGGING_PROCESS_FLAGS flags). This function specifies,
    // beyond whether or not a managed debug event is pending, also if the
    // event (if one exists) is caused by a Debugger.Launch(). This is
    // important b/c Debugger.Launch calls should *NOT* cause the debugger
    // to terminate the process when the attach is canceled.
    virtual
    CLR_DEBUGGING_PROCESS_FLAGS GetAttachStateFlags() = 0;

    virtual
    bool GetMetaDataFileInfoFromPEFile(VMPTR_PEFile vmPEFile,
                                       DWORD & dwTimeStamp,
                                       DWORD & dwImageSize,
                                       bool  & isNGEN,
                                       IStringHolder* pStrFilename) = 0;

    virtual
    bool GetILImageInfoFromNgenPEFile(VMPTR_PEFile vmPEFile,
                                      DWORD & dwTimeStamp,
                                      DWORD & dwSize,
                                      IStringHolder* pStrFilename) = 0;


    virtual
    bool IsThreadSuspendedOrHijacked(VMPTR_Thread vmThread) = 0;


    typedef void* * HeapWalkHandle;

    // Returns true if it is safe to walk the heap.  If this function returns false,
    // you could still create a heap walk and attempt to walk it, but there's no
    // telling how much of the heap will be available.
    virtual
    bool AreGCStructuresValid() = 0;

    // Creates a HeapWalkHandle which can be used to walk the managed heap with the
    // WalkHeap function.  Note if this function completes successfully you will need
    // to delete the handle by passing it into DeleteHeapWalk.
    //
    // Arguments:
    //   pHandle - the location to store the heap walk handle in
    //
    // Returns:
    //   S_OK on success, an error code on failure.
    virtual
    HRESULT CreateHeapWalk(OUT HeapWalkHandle * pHandle) = 0;


    // Deletes the give HeapWalkHandle.  Note you must call this function if
    // CreateHeapWalk returns success.
    virtual
    void DeleteHeapWalk(HeapWalkHandle handle) = 0;

    // Walks the heap using the given heap walk handle, enumerating objects
    // on the managed heap.  Note that walking the heap requires that the GC
    // data structures be in a valid state, which you can find by calling
    // AreGCStructuresValid.
    //
    // Arguments:
    //   handle   - a HeapWalkHandle obtained from CreateHeapWalk
    //   count    - the number of object addresses to obtain; pValues must
    //              be at least as large as count
    //   objects  - the location to stuff the object addresses found during
    //              the heap walk; this array should be at least "count" in
    //              length; this field must not be null
    //   pFetched - a location to store the actual number of values filled
    //              into pValues; this field must not be null
    //
    // Returns:
    //   S_OK on success, a failure HRESULT otherwise.
    //
    // Note:
    //   You should iteratively call WalkHeap requesting more values until
    //   *pFetched != count..  This signifies that we have reached the end
    //   of the heap walk.
    virtual
    HRESULT WalkHeap(HeapWalkHandle handle,
                     ULONG count,
                     OUT COR_HEAPOBJECT * objects,
                     OUT ULONG * pFetched) = 0;

    virtual
    HRESULT GetHeapSegments(OUT DacDbiArrayList<COR_SEGMENT> * pSegments) = 0;

    virtual
    bool IsValidObject(CORDB_ADDRESS obj) = 0;

    virtual
    bool GetAppDomainForObject(CORDB_ADDRESS obj, OUT VMPTR_AppDomain * pApp,
                                OUT VMPTR_Module * pModule,
                                OUT VMPTR_DomainFile * pDomainFile) = 0;


    //   Reference Walking.

    //  Creates a reference walk.
    //  Parameters:
    //      pHandle - out - the reference walk handle to create
    //      walkStacks - in - whether or not to report stack references
    //      walkFQ - in - whether or not to report references from the finalizer queue
    //      handleWalkMask - in - the types of handles report (see CorGCReferenceType, cordebug.idl)
    //  Returns:
    //      An HRESULT indicating whether it succeded or failed.
    //  Exceptions:
    //      Does not throw, but does not catch exceptions either.
    virtual
    HRESULT CreateRefWalk(OUT RefWalkHandle * pHandle, BOOL walkStacks, BOOL walkFQ, UINT32 handleWalkMask) = 0;

    // Deletes a reference walk.
    // Parameters:
    //      handle - in - the handle of the reference walk to delete
    // Excecptions:
    //      Does not throw, but does not catch exceptions either.
    virtual
    void DeleteRefWalk(RefWalkHandle handle) = 0;

    // Enumerates GC references in the process based on the parameters passed to CreateRefWalk.
    // Parameters:
    //      handle - in - the RefWalkHandle to enumerate
    //      count - in - the capacity of "refs"
    //      refs - in/out - an array to write the references to
    //      pFetched - out - the number of references written
    virtual
    HRESULT WalkRefs(RefWalkHandle handle, ULONG count, OUT DacGcReference * refs, OUT ULONG * pFetched) = 0;

    virtual
    HRESULT GetTypeID(CORDB_ADDRESS obj, COR_TYPEID * pType) = 0;

    virtual
    HRESULT GetTypeIDForType(VMPTR_TypeHandle vmTypeHandle, COR_TYPEID *pId) = 0;

    virtual
    HRESULT GetObjectFields(COR_TYPEID id, ULONG32 celt, OUT COR_FIELD * layout, OUT ULONG32 * pceltFetched) = 0;

    virtual
    HRESULT GetTypeLayout(COR_TYPEID id, COR_TYPE_LAYOUT * pLayout) = 0;

    virtual
    HRESULT GetArrayLayout(COR_TYPEID id, COR_ARRAY_LAYOUT * pLayout) = 0;

    virtual
    void GetGCHeapInformation(OUT COR_HEAPINFO * pHeapInfo) = 0;

    // If a PEFile has an RW capable IMDInternalImport, this returns the address of the MDInternalRW
    // object which implements it.
    //
    //
    // Arguments:
    //    vmPEFile - target PEFile to get metadata MDInternalRW for.
    //    pAddrMDInternalRW - If a PEFile has an RW capable IMDInternalImport, this will be set to the address
    //                        of the MDInternalRW object which implements it. Otherwise it will be NULL.
    //
    virtual
    HRESULT GetPEFileMDInternalRW(VMPTR_PEFile vmPEFile, OUT TADDR* pAddrMDInternalRW) = 0;

    // DEPRECATED - use GetActiveRejitILCodeVersionNode
    // Retrieves the active ReJitInfo for a given module/methodDef, if it exists.
    //     Active is defined as after GetReJitParameters returns from the profiler dll and
    //     no call to Revert has completed yet.
    //
    //
    // Arguments:
    //    vmModule - The module to search in
    //    methodTk - The methodDef token indicates the method within the module to check
    //    pReJitInfo - [out] The RejitInfo request, if any, that is active on this method. If no request
    //                 is active this will be pReJitInfo->IsNull() == TRUE.
    //
    // Returns:
    //    S_OK regardless of whether a rejit request is active or not, as long as the answer is certain
    //    error HRESULTs such as CORDBG_READ_VIRTUAL_FAILURE are possible
    //
    virtual
    HRESULT GetReJitInfo(VMPTR_Module vmModule, mdMethodDef methodTk, OUT VMPTR_ReJitInfo* pReJitInfo) = 0;

    // DEPRECATED - use GetNativeCodeVersionNode
    // Retrieves the ReJitInfo for a given MethodDesc/code address, if it exists.
    //
    //
    // Arguments:
    //    vmMethod         - The method to look for
    //    codeStartAddress - The code start address disambiguates between multiple rejitted instances
    //                       of the method.
    //    pReJitInfo - [out] The RejitInfo request that corresponds to this MethodDesc/code address, if it exists.
    //                       NULL otherwise.
    //
    // Returns:
    //    S_OK regardless of whether a rejit request is active or not, as long as the answer is certain
    //    error HRESULTs such as CORDBG_READ_VIRTUAL_FAILURE are possible
    //
    virtual
    HRESULT GetReJitInfo(VMPTR_MethodDesc vmMethod, CORDB_ADDRESS codeStartAddress, OUT VMPTR_ReJitInfo* pReJitInfo) = 0;

    // DEPRECATED - use GetILCodeVersion
    // Retrieves the SharedReJitInfo for a given ReJitInfo.
    //
    //
    // Arguments:
    //    vmReJitInfo      - The ReJitInfo to inspect
    //    pSharedReJitInfo - [out] The SharedReJitInfo that is pointed to by vmReJitInfo.
    //
    // Returns:
    //    S_OK if no error
    //    error HRESULTs such as CORDBG_READ_VIRTUAL_FAILURE are possible
    //
    virtual
    HRESULT GetSharedReJitInfo(VMPTR_ReJitInfo vmReJitInfo, VMPTR_SharedReJitInfo* pSharedReJitInfo) = 0;

    // DEPRECATED - use GetILCodeVersionData
    // Retrieves useful data from a SharedReJitInfo such as IL code and IL mapping.
    //
    //
    // Arguments:
    //    sharedReJitInfo  - The SharedReJitInfo to inspect
    //    pData            - [out] Various properties of the SharedReJitInfo such as IL code and IL mapping.
    //
    // Returns:
    //    S_OK if no error
    //    error HRESULTs such as CORDBG_READ_VIRTUAL_FAILURE are possible
    //
    virtual
    HRESULT GetSharedReJitInfoData(VMPTR_SharedReJitInfo sharedReJitInfo, DacSharedReJitInfo* pData) = 0;

    // Retrieves a bit field indicating which defines were in use when clr was built. This only includes
    // defines that are specified in the Debugger::_Target_Defines enumeration, which is a small subset of
    // all defines.
    //
    //
    // Arguments:
    //    pDefines  - [out] The set of defines clr.dll was built with. Bit offsets are encoded using the
    //                enumeration Debugger::_Target_Defines
    //
    // Returns:
    //    S_OK if no error
    //    error HRESULTs such as CORDBG_READ_VIRTUAL_FAILURE are possible
    //
    virtual
    HRESULT GetDefinesBitField(ULONG32 *pDefines) = 0;

    // Retrieves a version number indicating the shape of the data structures used in the Metadata implementation
    // inside clr.dll. This number changes anytime a datatype layout changes so that they can be correctly
    // deserialized from out of process
    //
    //
    // Arguments:
    //    pMDStructuresVersion  - [out] The layout version number for metadata data structures. See
    //                            Debugger::Debugger() in Debug\ee\Debugger.cpp for a description of the options.
    //
    // Returns:
    //    S_OK if no error
    //    error HRESULTs such as CORDBG_READ_VIRTUAL_FAILURE are possible
    //
    virtual
    HRESULT GetMDStructuresVersion(ULONG32* pMDStructuresVersion) = 0;

    // Retrieves the active rejit ILCodeVersionNode for a given module/methodDef, if it exists.
    //     Active is defined as after GetReJitParameters returns from the profiler dll and
    //     no call to Revert has completed yet.
    //
    //
    // Arguments:
    //    vmModule - The module to search in
    //    methodTk - The methodDef token indicates the method within the module to check
    //    pILCodeVersionNode - [out] The Rejit request, if any, that is active on this method. If no request
    //                          is active this will be pILCodeVersionNode->IsNull() == TRUE.
    //
    // Returns:
    //    S_OK regardless of whether a rejit request is active or not, as long as the answer is certain
    //    error HRESULTs such as CORDBG_READ_VIRTUAL_FAILURE are possible
    //
    virtual
        HRESULT GetActiveRejitILCodeVersionNode(VMPTR_Module vmModule, mdMethodDef methodTk, OUT VMPTR_ILCodeVersionNode* pVmILCodeVersionNode) = 0;

    // Retrieves the NativeCodeVersionNode for a given MethodDesc/code address, if it exists.
    // NOTE: The initial (default) code generated for a MethodDesc is a valid MethodDesc/code address pair but it won't have a corresponding
    // NativeCodeVersionNode.
    //
    //
    // Arguments:
    //    vmMethod                 - The method to look for
    //    codeStartAddress         - The code start address disambiguates between multiple jitted instances of the method.
    //    pVmNativeCodeVersionNode - [out] The NativeCodeVersionNode request that corresponds to this MethodDesc/code address, if it exists.
    //                               NULL otherwise.
    //
    // Returns:
    //    S_OK regardless of whether a rejit request is active or not, as long as the answer is certain
    //    error HRESULTs such as CORDBG_READ_VIRTUAL_FAILURE are possible
    //
    virtual
        HRESULT GetNativeCodeVersionNode(VMPTR_MethodDesc vmMethod, CORDB_ADDRESS codeStartAddress, OUT VMPTR_NativeCodeVersionNode* pVmNativeCodeVersionNode) = 0;

    // Retrieves the ILCodeVersionNode for a given NativeCodeVersionNode.
    // This may return a NULL node if the native code belongs to the default IL version for this this method.
    //
    //
    // Arguments:
    //    vmNativeCodeVersionNode  - The NativeCodeVersionNode to inspect
    //    pVmILCodeVersionNode     - [out] The ILCodeVersionNode that is pointed to by vmNativeCodeVersionNode, if any.
    //
    // Returns:
    //    S_OK if no error
    //    error HRESULTs such as CORDBG_READ_VIRTUAL_FAILURE are possible
    //
    virtual
        HRESULT GetILCodeVersionNode(VMPTR_NativeCodeVersionNode vmNativeCodeVersionNode, VMPTR_ILCodeVersionNode* pVmILCodeVersionNode) = 0;

    // Retrieves useful data from an ILCodeVersion such as IL code and IL mapping.
    //
    //
    // Arguments:
    //    ilCodeVersionNode - The ILCodeVersionNode to inspect
    //    pData             - [out] Various properties of the ILCodeVersionNode such as IL code and IL mapping.
    //
    // Returns:
    //    S_OK if no error
    //    error HRESULTs such as CORDBG_READ_VIRTUAL_FAILURE are possible
    //
    virtual
        HRESULT GetILCodeVersionNodeData(VMPTR_ILCodeVersionNode ilCodeVersionNode, DacSharedReJitInfo* pData) = 0;

    // Enable or disable the GC notification events. The GC notification events are turned off by default
    // They will be delivered through ICorDebugManagedCallback4
    //
    //
    // Arguments:
    //    fEnable - true to enable the events, false to disable
    //
    // Returns:
    //    S_OK if no error
    //    error HRESULTs such as CORDBG_READ_VIRTUAL_FAILURE are possible
    //
    virtual
        HRESULT EnableGCNotificationEvents(BOOL fEnable) = 0;


    typedef enum
    {
        kClosedDelegate,
        kOpenDelegate,
        kOpenInstanceVSD,
        kClosedStaticWithScpecialSig,
        kTrueMulticastDelegate,
        kWrapperDelegate,
        kUnmanagedFunctionDelegate,
        kUnknownDelegateType
    } DelegateType;

    // Returns true if the object is a type deriving from System.MulticastDelegate
    //
    // Arguments:
    //    vmObject - pointer to runtime object to query for.
    //
    virtual
    BOOL IsDelegate(VMPTR_Object vmObject) = 0;

    // Returns the delegate type
    virtual
    HRESULT GetDelegateType(VMPTR_Object delegateObject, DelegateType *delegateType) = 0;

    virtual
    HRESULT GetDelegateFunctionData(
        DelegateType delegateType,
        VMPTR_Object delegateObject,
        OUT VMPTR_DomainFile *ppFunctionDomainFile,
        OUT mdMethodDef *pMethodDef) = 0;

    virtual
    HRESULT GetDelegateTargetObject(
        DelegateType delegateType,
        VMPTR_Object delegateObject,
        OUT VMPTR_Object *ppTargetObj,
        OUT VMPTR_AppDomain *ppTargetAppDomain) = 0;

    virtual
    HRESULT GetLoaderHeapMemoryRanges(OUT DacDbiArrayList<COR_MEMORY_RANGE> *pRanges) = 0;

    virtual
    HRESULT IsModuleMapped(VMPTR_Module pModule, OUT BOOL *isModuleMapped) = 0;

    virtual
    bool MetadataUpdatesApplied() = 0;

    // The following tag tells the DD-marshalling tool to stop scanning.
    // END_MARSHAL

    //-----------------------------------------------------------------------------
    // Utility interface used for passing strings out of these APIs.  The caller
    // provides an implementation of this that uses whatever memory allocation
    // strategy it desires, and IDacDbiInterface APIs will call AssignCopy in order
    // to pass back the contents of strings.
    //
    // This permits the client and implementation of IDacDbiInterface to be in
    // different DLLs with their own heap allocation mechanism, while avoiding
    // the ugly and verbose 2-call C-style string passing API pattern.
    //-----------------------------------------------------------------------------
    class IStringHolder
    {
    public:
        //
        // Store a copy of of the provided string.
        //
        // Arguments:
        //     psz - The null-terminated unicode string to copy.
        //
        // Return Value:
        //     S_OK on success, typical HRESULT return values on failure.
        //
        // Notes:
        //    The underlying object is responsible for allocating and freeing the
        //    memory for this copy.  The object must not store the value of psz,
        //    it is no longer valid after this call returns.
        //
        virtual
        HRESULT AssignCopy(const WCHAR * psz) = 0;
    };


    //-----------------------------------------------------------------------------
    // Interface for allocations
    // This lets DD allocate buffers to pass back to DBI; and thus avoids
    // the common 2-step (query size/allocate/query data) pattern.
    //
    // Note that mscordacwks.dll and clients cannot share the same heap allocator,
    // DAC statically links the CRT to avoid run-time dependencies on non-OS libraries.
    //-----------------------------------------------------------------------------
    class IAllocator
    {
    public:
        // Allocate
        // Expected to throw on error.
        virtual
        void * Alloc(SIZE_T lenBytes) = 0;

        // Free. This shouldn't throw.
        virtual
        void Free(void * p) = 0;
    };


    //-----------------------------------------------------------------------------
    // Callback interface to provide Metadata lookup.
    //-----------------------------------------------------------------------------
    class IMetaDataLookup
    {
    public:
        //
        // Lookup a metadata importer via PEFile.
        //
        // Returns:
        //    A IMDInternalImport used by dac-ized VM code. The object is NOT addref-ed. See lifespan notes below.
        //    Returns NULL if no importer is available.
        //    Throws on exceptional circumstances (eg, detects the debuggee is corrupted).
        //
        // Notes:
        //    IMDInternalImport is a property of PEFile. The DAC-ized code uses it as a weak reference,
        //    and so we avoid doing an AddRef() here because that would mean we need to add Release() calls
        //    in DAC-only paths.
        //    The metadata importers are not DAC-ized, and thus we have a local copy in the host.
        //    If it was dac-ized, then DAC would get the importer just like any other field.
        //
        //    lifespan of returned object:
        //    - DBI owns the metadata importers.
        //    - DBI must not free the importer without calling Flush() on DAC first.
        //    - DAC will only invoke this when in a DD primitive, which was in turn invoked by DBI.
        //    - For performance reasons, we want to allow DAC to cache this between Flush() calls.
        //    - If DAC caches the importer, it will only use it when DBI invokes a DD primitive.
        //    - the reference count of the returned object is not adjusted.
        //
        virtual
        IMDInternalImport * LookupMetaData(VMPTR_PEFile addressPEFile, bool &isILMetaDataForNGENImage) = 0;
    };

}; // end IDacDbiInterface


#endif // _DACDBI_INTERFACE_H_
