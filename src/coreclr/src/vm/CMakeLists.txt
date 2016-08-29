set(CMAKE_INCLUDE_CURRENT_DIR ON)

# Needed due to the cmunged files being in the binary folders, the set(CMAKE_INCLUDE_CURRENT_DIR ON) is not enough
include_directories(BEFORE ${CMAKE_CURRENT_SOURCE_DIR}) 

include_directories(${CLR_DIR}/src/gc)

include_directories(${ARCH_SOURCES_DIR})

add_definitions(-DFEATURE_LEAVE_RUNTIME_HOLDER=1)

add_definitions(-DUNICODE)
add_definitions(-D_UNICODE)


if(CMAKE_CONFIGURATION_TYPES) # multi-configuration generator?
  foreach (Config DEBUG CHECKED)  
     set_property(DIRECTORY APPEND PROPERTY COMPILE_DEFINITIONS $<$<CONFIG:${Config}>:WRITE_BARRIER_CHECK=1>)
  endforeach (Config)
else()
  if(UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG OR UPPERCASE_CMAKE_BUILD_TYPE STREQUAL CHECKED)
    add_definitions(-DWRITE_BARRIER_CHECK=1)
  endif(UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG OR UPPERCASE_CMAKE_BUILD_TYPE STREQUAL CHECKED)
endif(CMAKE_CONFIGURATION_TYPES)

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

if(FEATURE_GDBJIT)
    set(VM_SOURCES_GDBJIT
        gdbjit.cpp
    )
    add_definitions(-DFEATURE_GDBJIT)
endif(FEATURE_GDBJIT)

set(VM_SOURCES_DAC_AND_WKS_COMMON
    appdomain.cpp
    array.cpp
    assembly.cpp
    baseassemblyspec.cpp
    binder.cpp
    ceeload.cpp
    class.cpp
    classhash.cpp
    clsload.cpp
    codeman.cpp
    comdelegate.cpp
    contractimpl.cpp
    coreassemblyspec.cpp
    corebindresult.cpp
    corhost.cpp
    crst.cpp
    debugdebugger.cpp
    debughelp.cpp
    debuginfostore.cpp
    decodemd.cpp
    disassembler.cpp
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
    genericdict.cpp
    generics.cpp
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
    pefile.cpp
    peimage.cpp
    peimagelayout.cpp
    perfmap.cpp
    perfinfo.cpp
    precode.cpp
    prestub.cpp
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
    versionresilienthashcode.cpp
    virtualcallstub.cpp
    win32threadpool.cpp
    zapsig.cpp
    ${VM_SOURCES_GDBJIT}
)

if(FEATURE_READYTORUN)
    list(APPEND VM_SOURCES_DAC_AND_WKS_COMMON
        readytoruninfo.cpp
    )
endif(FEATURE_READYTORUN)

set(VM_SOURCES_DAC
    ${VM_SOURCES_DAC_AND_WKS_COMMON}
    contexts.cpp
    threaddebugblockinginfo.cpp
)

set(VM_SOURCES_WKS
    ${VM_SOURCES_DAC_AND_WKS_COMMON}
    appdomainnative.cpp
    appdomainstack.cpp
    assemblyname.cpp
    assemblynative.cpp
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
    commodule.cpp
    compatibilityswitch.cpp
    comsynchronizable.cpp
    comthreadpool.cpp
    comutilnative.cpp
    comwaithandle.cpp
    constrainedexecutionregion.cpp
    coverage.cpp
    customattribute.cpp
    custommarshalerinfo.cpp
    dllimportcallback.cpp
    eeconfig.cpp
    eecontract.cpp
    eemessagebox.cpp
    eepolicy.cpp
    eetoprofinterfaceimpl.cpp
    eventstore.cpp
    fcall.cpp
    fieldmarshaler.cpp
    finalizerthread.cpp
    frameworkexceptionloader.cpp
    gccover.cpp
    gcenv.ee.cpp
    gcenv.os.cpp
    gchelpers.cpp
    genmeth.cpp
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
    marvin32.cpp
    mdaassistants.cpp
    methodtablebuilder.cpp
    mlinfo.cpp
    mscorlib.cpp # <DisablePrecompiledHeaders>true</DisablePrecompiledHeaders>
    multicorejit.cpp # Condition="'$(FeatureMulticoreJIT)' == 'true'
    multicorejitplayer.cpp # Condition="'$(FeatureMulticoreJIT)' == 'true'
    nativeeventsource.cpp
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
    profilermetadataemitvalidator.cpp
    profilingenumerators.cpp
    profilinghelper.cpp
    proftoeeinterfaceimpl.cpp
    qcall.cpp
    reflectclasswriter.cpp
    reflectioninvocation.cpp
    runtimehandles.cpp
    safehandle.cpp
    security.cpp
    securityattributes.cpp
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
    typeparse.cpp
    verifier.cpp
    weakreferencenative.cpp
)

if(FEATURE_EVENT_TRACE)
    list(APPEND VM_SOURCES_WKS
        eventtrace.cpp
        )
endif(FEATURE_EVENT_TRACE)

if(WIN32)

set(VM_SOURCES_DAC_AND_WKS_WIN32
    clrtocomcall.cpp
    rcwwalker.cpp
    winrttypenameconverter.cpp
)

list(APPEND VM_SOURCES_WKS 
    ${VM_SOURCES_DAC_AND_WKS_WIN32}
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
    comtoclrcall.cpp
    dispatchinfo.cpp
    dispparammarshaler.cpp
    dwreport.cpp
    eventreporter.cpp
    extensibleclassfactory.cpp
    microsoft.comservices_i.c
    mngstdinterfaces.cpp
    notifyexternals.cpp
    olecontexthelpers.cpp    
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

if(CLR_CMAKE_TARGET_ARCH_AMD64)
    set(VM_SOURCES_WKS_ARCH_ASM
        ${ARCH_SOURCES_DIR}/AsmHelpers.asm
        ${ARCH_SOURCES_DIR}/CallDescrWorkerAMD64.asm
        ${ARCH_SOURCES_DIR}/ComCallPreStub.asm
        ${ARCH_SOURCES_DIR}/CrtHelpers.asm
        ${ARCH_SOURCES_DIR}/GenericComCallStubs.asm
        ${ARCH_SOURCES_DIR}/GenericComPlusCallStubs.asm
        ${ARCH_SOURCES_DIR}/getstate.asm
        ${ARCH_SOURCES_DIR}/InstantiatingStub.asm
        ${ARCH_SOURCES_DIR}/JitHelpers_Fast.asm
        ${ARCH_SOURCES_DIR}/JitHelpers_FastWriteBarriers.asm
        ${ARCH_SOURCES_DIR}/JitHelpers_InlineGetAppDomain.asm
        ${ARCH_SOURCES_DIR}/JitHelpers_InlineGetThread.asm
        ${ARCH_SOURCES_DIR}/JitHelpers_Slow.asm
        ${ARCH_SOURCES_DIR}/PInvokeStubs.asm
        ${ARCH_SOURCES_DIR}/RedirectedHandledJITCase.asm
        ${ARCH_SOURCES_DIR}/ThePreStubAMD64.asm
        ${ARCH_SOURCES_DIR}/ExternalMethodFixupThunk.asm
        ${ARCH_SOURCES_DIR}/TlsGetters.asm # Condition="'$(FeatureImplicitTls)' != 'true'
        ${ARCH_SOURCES_DIR}/UMThunkStub.asm
        ${ARCH_SOURCES_DIR}/VirtualCallStubAMD64.asm
    )
elseif(CLR_CMAKE_TARGET_ARCH_I386)
    set(VM_SOURCES_WKS_ARCH_ASM
        ${ARCH_SOURCES_DIR}/RedirectedHandledJITCase.asm
        ${ARCH_SOURCES_DIR}/asmhelpers.asm
        ${ARCH_SOURCES_DIR}/fptext.asm
        ${ARCH_SOURCES_DIR}/gmsasm.asm
        ${ARCH_SOURCES_DIR}/jithelp.asm
    )
elseif(CLR_CMAKE_TARGET_ARCH_ARM)
    set(VM_SOURCES_WKS_ARCH_ASM
        ${ARCH_SOURCES_DIR}/asmhelpers.asm
        ${ARCH_SOURCES_DIR}/CrtHelpers.asm
        ${ARCH_SOURCES_DIR}/ehhelpers.asm
        ${ARCH_SOURCES_DIR}/memcpy.asm
        ${ARCH_SOURCES_DIR}/patchedcode.asm
        ${ARCH_SOURCES_DIR}/PInvokeStubs.asm
    )
elseif(CLR_CMAKE_TARGET_ARCH_ARM64)
    set(VM_SOURCES_WKS_ARCH_ASM
        ${ARCH_SOURCES_DIR}/AsmHelpers.asm
        ${ARCH_SOURCES_DIR}/CallDescrWorkerARM64.asm
        ${ARCH_SOURCES_DIR}/CrtHelpers.asm
        ${ARCH_SOURCES_DIR}/PInvokeStubs.asm
    )

endif()

else(WIN32)

    if(CLR_CMAKE_TARGET_ARCH_AMD64)
        set(VM_SOURCES_WKS_ARCH_ASM
            ${ARCH_SOURCES_DIR}/calldescrworkeramd64.S
            ${ARCH_SOURCES_DIR}/crthelpers.S
            ${ARCH_SOURCES_DIR}/externalmethodfixupthunk.S
            ${ARCH_SOURCES_DIR}/getstate.S
            ${ARCH_SOURCES_DIR}/jithelpers_fast.S
            ${ARCH_SOURCES_DIR}/jithelpers_fastwritebarriers.S
            ${ARCH_SOURCES_DIR}/jithelpers_slow.S
            ${ARCH_SOURCES_DIR}/pinvokestubs.S
            ${ARCH_SOURCES_DIR}/theprestubamd64.S
            ${ARCH_SOURCES_DIR}/unixasmhelpers.S
            ${ARCH_SOURCES_DIR}/umthunkstub.S
            ${ARCH_SOURCES_DIR}/virtualcallstubamd64.S
        )
    elseif(CLR_CMAKE_TARGET_ARCH_ARM)
        set(VM_SOURCES_WKS_ARCH_ASM
            ${ARCH_SOURCES_DIR}/asmhelpers.S
            ${ARCH_SOURCES_DIR}/crthelpers.S
            ${ARCH_SOURCES_DIR}/ehhelpers.S
            ${ARCH_SOURCES_DIR}/memcpy.S
            ${ARCH_SOURCES_DIR}/patchedcode.S
            ${ARCH_SOURCES_DIR}/pinvokestubs.S
        )
    elseif(CLR_CMAKE_TARGET_ARCH_ARM64)
        set(VM_SOURCES_WKS_ARCH_ASM
            ${ARCH_SOURCES_DIR}/asmhelpers.S
            ${ARCH_SOURCES_DIR}/calldescrworkerarm64.S
            ${ARCH_SOURCES_DIR}/crthelpers.S
            ${ARCH_SOURCES_DIR}/pinvokestubs.S
        )
    endif()
    
endif(WIN32)


if(CLR_CMAKE_TARGET_ARCH_AMD64)
    set(VM_SOURCES_DAC_AND_WKS_ARCH
        ${ARCH_SOURCES_DIR}/cgenamd64.cpp
        ${ARCH_SOURCES_DIR}/excepamd64.cpp
        ${ARCH_SOURCES_DIR}/gmsamd64.cpp
        ${ARCH_SOURCES_DIR}/stublinkeramd64.cpp
    )
    
    set(VM_SOURCES_WKS_ARCH
        ${ARCH_SOURCES_DIR}/jithelpersamd64.cpp
        ${ARCH_SOURCES_DIR}/jitinterfaceamd64.cpp
        ${ARCH_SOURCES_DIR}/profiler.cpp
        exceptionhandling.cpp
        gcinfodecoder.cpp
        jitinterfacegen.cpp
    )
elseif(CLR_CMAKE_TARGET_ARCH_I386)
    set(VM_SOURCES_DAC_AND_WKS_ARCH
        gcdecode.cpp
        exinfo.cpp
        ${ARCH_SOURCES_DIR}/cgenx86.cpp
        ${ARCH_SOURCES_DIR}/excepx86.cpp
        ${ARCH_SOURCES_DIR}/gmsx86.cpp
        ${ARCH_SOURCES_DIR}/stublinkerx86.cpp
    )
    
    set(VM_SOURCES_WKS_ARCH
        ${ARCH_SOURCES_DIR}/jithelp.asm
        ${ARCH_SOURCES_DIR}/jitinterfacex86.cpp
        ${ARCH_SOURCES_DIR}/profiler.cpp
    )
elseif(CLR_CMAKE_TARGET_ARCH_ARM)
    set(VM_SOURCES_DAC_AND_WKS_ARCH
        ${ARCH_SOURCES_DIR}/exceparm.cpp
        ${ARCH_SOURCES_DIR}/stubs.cpp
        ${ARCH_SOURCES_DIR}/armsinglestepper.cpp
    )
    
    set(VM_SOURCES_WKS_ARCH
        ${ARCH_SOURCES_DIR}/jithelpersarm.cpp
        ${ARCH_SOURCES_DIR}/profiler.cpp
        exceptionhandling.cpp
        gcinfodecoder.cpp
    )
elseif(CLR_CMAKE_TARGET_ARCH_ARM64)
    set(VM_SOURCES_DAC_AND_WKS_ARCH
        ${ARCH_SOURCES_DIR}/cgenarm64.cpp
        ${ARCH_SOURCES_DIR}/stubs.cpp
        exceptionhandling.cpp
        gcinfodecoder.cpp
    )
endif()

if(CLR_CMAKE_PLATFORM_UNIX)
    list(APPEND VM_SOURCES_WKS_ARCH
        ${ARCH_SOURCES_DIR}/unixstubs.cpp
    )
endif(CLR_CMAKE_PLATFORM_UNIX)

set(VM_SOURCES_DAC_ARCH
    gcinfodecoder.cpp
    exceptionhandling.cpp
)

list(APPEND VM_SOURCES_WKS 
    ${VM_SOURCES_WKS_ARCH}
    ${VM_SOURCES_DAC_AND_WKS_ARCH}
)

list(APPEND VM_SOURCES_DAC
    ${VM_SOURCES_DAC_ARCH}
    ${VM_SOURCES_DAC_AND_WKS_ARCH}
)

convert_to_absolute_path(VM_SOURCES_WKS ${VM_SOURCES_WKS})
convert_to_absolute_path(VM_SOURCES_WKS_ARCH_ASM ${VM_SOURCES_WKS_ARCH_ASM})
convert_to_absolute_path(VM_SOURCES_DAC ${VM_SOURCES_DAC})

add_subdirectory(dac)
add_subdirectory(wks)
