set(CMAKE_INCLUDE_CURRENT_DIR ON)

# WINTODO: Conditionalize the next check
# AMD64 specific sources subdirectory
set(AMD64_SOURCES_DIR amd64)

# Needed due to the cmunged files being in the binary folders, the set(CMAKE_INCLUDE_CURRENT_DIR ON) is not enough
include_directories(BEFORE ${CMAKE_CURRENT_SOURCE_DIR}) 

include_directories(${CLR_DIR}/src/gc)
include_directories(${AMD64_SOURCES_DIR})

add_definitions(-D_TARGET_AMD64_=1)
add_definitions(-DDBG_TARGET_64BIT=1)
add_definitions(-DDBG_TARGET_AMD64=1)
add_definitions(-DDBG_TARGET_WIN64=1)

add_definitions(-DFEATURE_LEAVE_RUNTIME_HOLDER=1)

add_definitions(-DUNICODE)
add_definitions(-D_UNICODE)

# Add the Merge flag here is needed
add_definitions(-DFEATURE_MERGE_JIT_AND_ENGINE)

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

set(VM_SOURCES_DAC_AND_WKS_COMMON
    appdomain.cpp
    array.cpp
    assembly.cpp
    baseassemblyspec.cpp
    binder.cpp
    ceeload.cpp
    certificatecache.cpp
    class.cpp
    classhash.cpp
    clsload.cpp
    codeman.cpp
    comdelegate.cpp
    contexts.cpp
    contractimpl.cpp
    coreassemblyspec.cpp
    corebindresult.cpp
    corhost.cpp
    crst.cpp
    debugdebugger.cpp
    debughelp.cpp
    debuginfostore.cpp
    decodemd.cpp
    dllimport.cpp
    domainfile.cpp
    dynamicmethod.cpp
    ecall.cpp
    eedbginterfaceimpl.cpp
    eehash.cpp
    eetwain.cpp
    encee.cpp
    excep.cpp
    exstate.cpp
    field.cpp
    formattype.cpp
    fptrstubs.cpp
    frames.cpp
    ../gc/gccommon.cpp
    gcdecode.cpp
    ../gc/gceesvr.cpp
    ../gc/gceewks.cpp
    ../gc/gcscan.cpp
    ../gc/gcsvr.cpp
    ../gc/gcwks.cpp
    genericdict.cpp
    generics.cpp
    ../gc/handletable.cpp
    ../gc/handletablecore.cpp
    ../gc/handletablescan.cpp
    hash.cpp
    hillclimbing.cpp
    ilstubcache.cpp
    ilstubresolver.cpp
    inlinetracking.cpp
    instmethhash.cpp
    jitinterface.cpp
    loaderallocator.cpp
    memberload.cpp
    method.cpp
    methodimpl.cpp
    methoditer.cpp
    methodtable.cpp
    object.cpp
    ../gc/objecthandle.cpp
    pefile.cpp
    peimage.cpp
    peimagelayout.cpp
    perfmap.cpp
    precode.cpp
    prestub.cpp
    readytoruninfo.cpp
    rejit.cpp
    securitydescriptor.cpp
    securitydescriptorassembly.cpp
    sigformat.cpp
    siginfo.cpp
    stackwalk.cpp
    stublink.cpp
    stubmgr.cpp
    syncblk.cpp
    threadpoolrequest.cpp
    threads.cpp
    threadstatics.cpp
    typectxt.cpp
    typedesc.cpp
    typehandle.cpp
    typehash.cpp
    typestring.cpp
    util.cpp
    vars.cpp
    virtualcallstub.cpp
    win32threadpool.cpp
    zapsig.cpp
)

set(VM_SOURCES_DAC
    ${VM_SOURCES_DAC_AND_WKS_COMMON}
    threaddebugblockinginfo.cpp
)

set(VM_SOURCES_WKS
    ${VM_SOURCES_DAC_AND_WKS_COMMON}
    appdomainhelper.cpp
    appdomainnative.cpp
    appdomainstack.cpp
    assemblyname.cpp
    assemblynative.cpp
    assemblysink.cpp
    assemblyspec.cpp
    cachelinealloc.cpp
    callhelpers.cpp
    ceemain.cpp
    clrex.cpp
    clrprivbinderutil.cpp
    clrvarargs.cpp
    comdatetime.cpp
    comdependenthandle.cpp
    comdynamic.cpp
    comisolatedstorage.cpp
    commemoryfailpoint.cpp
    commethodrental.cpp
    commodule.cpp
    compatibilityswitch.cpp
    comsynchronizable.cpp
    comthreadpool.cpp
    comutilnative.cpp
    comwaithandle.cpp
    constrainedexecutionregion.cpp
    coverage.cpp
    crossdomaincalls.cpp
    customattribute.cpp
    custommarshalerinfo.cpp
    dbggcinfodecoder.cpp
    dllimportcallback.cpp
    eeconfig.cpp
    eecontract.cpp
    eemessagebox.cpp
    eepolicy.cpp
    eetoprofinterfaceimpl.cpp
    eventstore.cpp
    exceptionhandling.cpp
    fcall.cpp
    fieldmarshaler.cpp
    finalizerthread.cpp
    frameworkexceptionloader.cpp
    gccover.cpp
    gcenv.cpp
    gchelpers.cpp
    gchost.cpp
    gcinfodecoder.cpp
    genmeth.cpp
    ../gc/handletablecache.cpp
    hostexecutioncontext.cpp
    hosting.cpp
    ibclogger.cpp
    ilmarshalers.cpp
    interopconverter.cpp
    interoputil.cpp
    interpreter.cpp
    invokeutil.cpp
    jithelpers.cpp
    listlock.cpp
    managedmdimport.cpp
    marshalnative.cpp
    mdaassistants.cpp
    message.cpp
    methodtablebuilder.cpp
    mlinfo.cpp
    mscorlib.cpp # <DisablePrecompiledHeaders>true</DisablePrecompiledHeaders>
    multicorejit.cpp # Condition="'$(FeatureMulticoreJIT)' == 'true'
    multicorejitplayer.cpp # Condition="'$(FeatureMulticoreJIT)' == 'true'
    nativeoverlapped.cpp
    objectlist.cpp
    olevariant.cpp
    pefingerprint.cpp
    pendingload.cpp
    perfdefaults.cpp
    profattach.cpp
    profattachclient.cpp
    profattachserver.cpp
    profdetach.cpp
    profilingenumerators.cpp
    profilinghelper.cpp
    proftoeeinterfaceimpl.cpp
    qcall.cpp
    reflectclasswriter.cpp
    reflectioninvocation.cpp
    runtimehandles.cpp
    rwlock.cpp
    safehandle.cpp
    security.cpp
    securityattributes.cpp
    securityconfig.cpp
    securitydeclarative.cpp
    securitydeclarativecache.cpp
    securitydescriptorappdomain.cpp
    securityhostprotection.cpp
    securitymeta.cpp
    securitypolicy.cpp
    securitytransparentassembly.cpp
    sha1.cpp
    simplerwlock.cpp
    sourceline.cpp
    spinlock.cpp
    stackingallocator.cpp
    stringliteralmap.cpp
    stubcache.cpp
    stubgen.cpp
    stubhelpers.cpp
    syncclean.cpp
    synch.cpp
    synchronizationcontextnative.cpp
    testhookmgr.cpp
    threaddebugblockinginfo.cpp
    threadsuspend.cpp
    typeequivalencehash.cpp
    typeparse.cpp
    verifier.cpp
    weakreferencenative.cpp
)

if(WIN32)

set(VM_SOURCES_DAC_AND_WKS_WIN32
    clrtocomcall.cpp
    comtoclrcall.cpp
    rcwwalker.cpp
    umthunkhash.cpp #" Condition="'$(FeatureMixedMode)' == 'true'
    winrttypenameconverter.cpp
)

list(APPEND VM_SOURCES_WKS 
    ${VM_SOURCES_DAC_AND_WKS_WIN32}
    marvin32.cpp # move out of win32 when FEATURE_RANDOMIZED_STRING_HASHING is enabled for linux
    # These should not be included for Linux
    appxutil.cpp
    assemblynativeresource.cpp
    classcompat.cpp
    classfactory.cpp
    clrprivbinderwinrt.cpp
    clrprivtypecachewinrt.cpp
    comcache.cpp
    comcallablewrapper.cpp
    comconnectionpoints.cpp
    cominterfacemarshaler.cpp
    commtmemberinfomap.cpp
    dispatchinfo.cpp
    dispparammarshaler.cpp
    dwreport.cpp
    eventreporter.cpp
    eventtrace.cpp
    extensibleclassfactory.cpp
    microsoft.comservices_i.c
    mngstdinterfaces.cpp
    notifyexternals.cpp
    olecontexthelpers.cpp
    profilermetadataemitvalidator.cpp
    rcwrefcache.cpp
    rtlfunctions.cpp
    runtimecallablewrapper.cpp
    securityprincipal.cpp
    stacksampler.cpp
    stdinterfaces.cpp
    stdinterfaces_wrapper.cpp
    winrthelpers.cpp    
)

list(APPEND VM_SOURCES_DAC 
    ${VM_SOURCES_DAC_AND_WKS_WIN32}
    # These should not be included for Linux
    clrprivbinderwinrt.cpp
    clrprivtypecachewinrt.cpp
)

# AMD64 specific asm sources
set(VM_SOURCES_WKS_AMD64_ASM
    ${AMD64_SOURCES_DIR}/AsmHelpers.asm
    ${AMD64_SOURCES_DIR}/CallDescrWorkerAMD64.asm
    ${AMD64_SOURCES_DIR}/ComCallPreStub.asm
    ${AMD64_SOURCES_DIR}/CrtHelpers.asm
    ${AMD64_SOURCES_DIR}/GenericComCallStubs.asm
    ${AMD64_SOURCES_DIR}/GenericComPlusCallStubs.asm
    ${AMD64_SOURCES_DIR}/getstate.asm
    ${AMD64_SOURCES_DIR}/InstantiatingStub.asm
    ${AMD64_SOURCES_DIR}/JitHelpers_Fast.asm
    ${AMD64_SOURCES_DIR}/JitHelpers_FastWriteBarriers.asm
    ${AMD64_SOURCES_DIR}/JitHelpers_InlineGetAppDomain.asm
    ${AMD64_SOURCES_DIR}/JitHelpers_InlineGetThread.asm
    ${AMD64_SOURCES_DIR}/JitHelpers_Slow.asm
    ${AMD64_SOURCES_DIR}/PInvokeStubs.asm
    ${AMD64_SOURCES_DIR}/RedirectedHandledJITCase.asm
    ${AMD64_SOURCES_DIR}/ThePreStubAMD64.asm
    ${AMD64_SOURCES_DIR}/ExternalMethodFixupThunk.asm
    ${AMD64_SOURCES_DIR}/TlsGetters.asm # Condition="'$(FeatureImplicitTls)' != 'true'
    ${AMD64_SOURCES_DIR}/UMThunkStub.asm
    ${AMD64_SOURCES_DIR}/VirtualCallStubAMD64.asm
)

else()
set(VM_SOURCES_WKS_AMD64_ASM
    ${AMD64_SOURCES_DIR}/calldescrworkeramd64.S
    ${AMD64_SOURCES_DIR}/crthelpers.S
    ${AMD64_SOURCES_DIR}/externalmethodfixupthunk.S
    ${AMD64_SOURCES_DIR}/getstate.S
    ${AMD64_SOURCES_DIR}/jithelpers_fast.S
    ${AMD64_SOURCES_DIR}/jithelpers_fastwritebarriers.S
    ${AMD64_SOURCES_DIR}/jithelpers_slow.S
    ${AMD64_SOURCES_DIR}/theprestubamd64.S
    ${AMD64_SOURCES_DIR}/unixasmhelpers.S
    ${AMD64_SOURCES_DIR}/umthunkstub.S
    ${AMD64_SOURCES_DIR}/virtualcallstubamd64.S
)
endif(WIN32)

# AMD64 specific cpp sources

set(VM_SOURCES_DAC_AND_WKS_AMD64
    ${AMD64_SOURCES_DIR}/cgenamd64.cpp
    ${AMD64_SOURCES_DIR}/excepamd64.cpp
    ${AMD64_SOURCES_DIR}/gmsamd64.cpp
    ${AMD64_SOURCES_DIR}/stublinkeramd64.cpp
)

set(VM_SOURCES_WKS_AMD64
#The following files need to be ported to Linux
    ${AMD64_SOURCES_DIR}/jithelpersamd64.cpp
    ${AMD64_SOURCES_DIR}/jitinterfaceamd64.cpp
    ${AMD64_SOURCES_DIR}/profiler.cpp
    jitinterfacegen.cpp
)

if(CLR_CMAKE_PLATFORM_UNIX)

list(APPEND VM_SOURCES_WKS_AMD64
    ${AMD64_SOURCES_DIR}/unixstubs.cpp
)

endif(CLR_CMAKE_PLATFORM_UNIX)

set(VM_SOURCES_DAC_AMD64
    gcinfodecoder.cpp
    dbggcinfodecoder.cpp
    exceptionhandling.cpp
)

list(APPEND VM_SOURCES_WKS 
    ${VM_SOURCES_WKS_AMD64}
    ${VM_SOURCES_DAC_AND_WKS_AMD64}
)

list(APPEND VM_SOURCES_DAC
    ${VM_SOURCES_DAC_AMD64}
    ${VM_SOURCES_DAC_AND_WKS_AMD64}
)

convert_to_absolute_path(VM_SOURCES_WKS ${VM_SOURCES_WKS})
convert_to_absolute_path(VM_SOURCES_WKS_AMD64_ASM ${VM_SOURCES_WKS_AMD64_ASM})
convert_to_absolute_path(VM_SOURCES_DAC ${VM_SOURCES_DAC})
    
add_subdirectory(dac)
add_subdirectory(wks)
