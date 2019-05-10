set(CMAKE_INCLUDE_CURRENT_DIR ON)

# Needed due to the cmunged files being in the binary folders, the set(CMAKE_INCLUDE_CURRENT_DIR ON) is not enough
include_directories(BEFORE ${CMAKE_CURRENT_SOURCE_DIR})
include_directories(${ARCH_SOURCES_DIR})

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
    set(VM_HEADERS_GDBJIT
        gdbjit.h
    )
endif(FEATURE_GDBJIT)

if(FEATURE_JIT_PITCHING)
   add_definitions(-DFEATURE_JIT_PITCHING)
endif(FEATURE_JIT_PITCHING)

set(VM_SOURCES_DAC_AND_WKS_COMMON
    appdomain.cpp
    array.cpp
    assembly.cpp
    baseassemblyspec.cpp
    binder.cpp
    callcounter.cpp
    ceeload.cpp
    class.cpp
    classhash.cpp
    clsload.cpp
    codeman.cpp
    codeversion.cpp
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
    diagnosticprotocolhelper.cpp
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
    gctoclreventsink.cpp
    gcheaputilities.cpp
    gchandleutilities.cpp
    genericdict.cpp
    generics.cpp
    hash.cpp
    hillclimbing.cpp
    ilinstrumentation.cpp
    ilstubcache.cpp
    ilstubresolver.cpp
    inlinetracking.cpp
    instmethhash.cpp
    jithost.cpp
    jitinterface.cpp
    loaderallocator.cpp
    memberload.cpp
    method.cpp
    methoddescbackpatchinfo.cpp
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
    sigformat.cpp
    siginfo.cpp
    spinlock.cpp
    stackwalk.cpp
    stublink.cpp
    stubmgr.cpp
    syncblk.cpp
    threadpoolrequest.cpp
    threads.cpp
    threadstatics.cpp
    tieredcompilation.cpp
    typectxt.cpp
    typedesc.cpp
    typeequivalencehash.cpp
    typehandle.cpp
    typehash.cpp
    typestring.cpp
    util.cpp
    vars.cpp
    versionresilienthashcode.cpp
    virtualcallstub.cpp
    win32threadpool.cpp
    yieldprocessornormalized.cpp
    zapsig.cpp
)

set(VM_HEADERS_DAC_AND_WKS_COMMON
    ../inc/corjit.h
    ../inc/corjitflags.h
    ../inc/corjithost.h
    appdomain.hpp
    appdomain.inl
    array.h
    assembly.hpp
    baseassemblyspec.h
    baseassemblyspec.inl
    binder.h
    ceeload.h
    ceeload.inl
    class.h
    class.inl
    classhash.h
    clsload.hpp
    clsload.inl
    codeman.h
    codeman.inl
    codeversion.h
    comdelegate.h
    contractimpl.h
    crst.h
    debugdebugger.h
    debuginfostore.h
    decodemd.h
    disassembler.h
    dllimport.h
    domainfile.h
    domainfile.inl
    dynamicmethod.h
    ecall.h
    eedbginterfaceimpl.h
    eedbginterfaceimpl.inl
    eehash.h
    eehash.inl
    encee.h
    excep.h
    exstate.h
    field.h
    fptrstubs.h
    frames.h
    gctoclreventsink.h
    gcheaputilities.h
    generics.h
    generics.inl
    hash.h
    hillclimbing.h
    ilinstrumentation.h
    ilstubcache.h
    ilstubresolver.h
    inlinetracking.h
    instmethhash.h
    jithost.h
    jitinterface.h
    loaderallocator.hpp
    loaderallocator.inl
    memberload.h
    method.hpp
    method.inl
    methoddescbackpatchinfo.h
    methodimpl.h
    methoditer.h
    methodtable.h
    methodtable.inl
    object.h
    object.inl
    pefile.h
    pefile.inl
    peimage.h
    peimage.inl
    peimagelayout.h
    peimagelayout.inl
    perfmap.h
    perfinfo.h
    precode.h
    rejit.h
    rejit.inl
    sigformat.h
    siginfo.hpp
    spinlock.h
    stackwalk.h
    stublink.h
    stublink.inl
    stubmgr.h
    syncblk.h
    syncblk.inl
    threadpoolrequest.h
    threads.h
    threads.inl
    threadstatics.h
    typectxt.h
    typedesc.h
    typedesc.inl
    typeequivalencehash.hpp
    typehandle.h
    typehandle.inl
    typehash.h
    typehashingalgorithms.h
    typestring.h
    util.hpp
    vars.hpp
    versionresilienthashcode.h
    virtualcallstub.h
    win32threadpool.h
    zapsig.h
)

set( GC_SOURCES_DAC_AND_WKS_COMMON
  ../gc/handletable.cpp
  ../gc/handletablecore.cpp
  ../gc/handletablescan.cpp
  ../gc/objecthandle.cpp)

set( GC_HEADERS_DAC_AND_WKS_COMMON
    ../gc/handletable.h
    ../gc/handletable.inl
    ../gc/handletablepriv.h
    ../gc/objecthandle.h)

if(FEATURE_READYTORUN)
    list(APPEND VM_SOURCES_DAC_AND_WKS_COMMON
        readytoruninfo.cpp
    )
    list(APPEND VM_HEADERS_DAC_AND_WKS_COMMON
        readytoruninfo.h
    )
endif(FEATURE_READYTORUN)

if(FEATURE_JIT_PITCHING)
    list(APPEND VM_SOURCES_DAC_AND_WKS_COMMON
        codepitchingmanager.cpp
    )
endif(FEATURE_JIT_PITCHING)

set(VM_SOURCES_DAC
    ${VM_SOURCES_DAC_AND_WKS_COMMON}
    threaddebugblockinginfo.cpp
)

set(VM_HEADERS_DAC
    ${VM_HEADERS_DAC_AND_WKS_COMMON}
    threaddebugblockinginfo.h
)

set(GC_SOURCES_DAC
    ${GC_SOURCES_DAC_AND_WKS_COMMON})

set(GC_HEADERS_DAC
    ${GC_HEADERS_DAC_AND_WKS_COMMON})

set(VM_SOURCES_WKS
    ${VM_SOURCES_DAC_AND_WKS_COMMON}
    appdomainnative.cpp
    assemblyname.cpp
    assemblynative.cpp
    assemblyspec.cpp
    cachelinealloc.cpp
    callhelpers.cpp
    callsiteinspect.cpp
    ceemain.cpp
    clrconfignative.cpp
    clrex.cpp
    clrprivbinderutil.cpp
    clrvarargs.cpp
    comdatetime.cpp
    comdependenthandle.cpp
    comdynamic.cpp
    commodule.cpp
    compatibilityswitch.cpp
    comsynchronizable.cpp
    comthreadpool.cpp
    comutilnative.cpp
    comwaithandle.cpp
    customattribute.cpp
    custommarshalerinfo.cpp
    diagnosticserver.cpp
    dllimportcallback.cpp
    eeconfig.cpp
    eecontract.cpp
    eemessagebox.cpp
    eepolicy.cpp
    eetoprofinterfaceimpl.cpp
    eventpipe.cpp
    eventpipeconfiguration.cpp
    eventpipeevent.cpp
    eventpipeeventinstance.cpp
    eventpipeeventsource.cpp
    eventpipeblock.cpp
    eventpipefile.cpp
    eventpipeinternal.cpp
    eventpipejsonfile.cpp
    eventpipemetadatagenerator.cpp
    eventpipeprotocolhelper.cpp
    eventpipeprovider.cpp
    eventpipebuffer.cpp
    eventpipebuffermanager.cpp
    eventpipesession.cpp
    eventpipesessionprovider.cpp
    eventstore.cpp
    fastserializer.cpp
    fcall.cpp
    fieldmarshaler.cpp
    finalizerthread.cpp
    frameworkexceptionloader.cpp
    gccover.cpp
    gcenv.ee.static.cpp
    gcenv.ee.common.cpp
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
    managedmdimport.cpp
    marshalnative.cpp
    methodtablebuilder.cpp
    mlinfo.cpp
    mscorlib.cpp # <DisablePrecompiledHeaders>true</DisablePrecompiledHeaders>
    multicorejit.cpp # Condition="'$(FeatureMulticoreJIT)' == 'true'
    multicorejitplayer.cpp # Condition="'$(FeatureMulticoreJIT)' == 'true'
    nativeeventsource.cpp
    nativeoverlapped.cpp
    nativelibrarynative.cpp
    objectlist.cpp
    olevariant.cpp
    pendingload.cpp
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
    sampleprofiler.cpp
    sha1.cpp
    simplerwlock.cpp
    sourceline.cpp
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
    weakreferencenative.cpp
    ${VM_SOURCES_GDBJIT}
)

set(VM_HEADERS_WKS
    ${VM_HEADERS_DAC_AND_WKS_COMMON}
    ../inc/jithelpers.h
    coreclr/corebindresult.h
    coreclr/corebindresult.inl
    appdomainnative.hpp
    assemblyname.hpp
    assemblynative.hpp
    assemblyspec.hpp
    assemblyspecbase.h
    cachelinealloc.h
    callcounter.h
    callhelpers.h
    callsiteinspect.h
    ceemain.h
    clrconfignative.h
    clrex.h
    clrvarargs.h
    comdatetime.h
    comdependenthandle.h
    comdynamic.h
    commodule.h
    compatibilityswitch.h
    comsynchronizable.h
    comthreadpool.h
    comutilnative.h
    comwaithandle.h
    customattribute.h
    custommarshalerinfo.h
    diagnosticserver.h
    diagnosticsprotocol.h
    dllimportcallback.h
    eeconfig.h
    eecontract.h
    eemessagebox.h
    eepolicy.h
    eeprofinterfaces.h
    eeprofinterfaces.inl
    eetoprofinterfaceimpl.h
    eetoprofinterfaceimpl.inl
    eventpipe.h
    eventpipeblock.h
    eventpipebuffer.h
    eventpipebuffermanager.h
    eventpipeconfiguration.h
    eventpipeevent.h
    eventpipeeventinstance.h
    eventpipeeventsource.h
    eventpipefile.h
    eventpipeinternal.h
    eventpipejsonfile.h
    eventpipemetadatagenerator.h
    eventpipeprotocolhelper.h
    eventpipeprovider.h
    eventpipesession.h
    eventpipesessionprovider.h
    eventstore.hpp
    fastserializer.h
    fcall.h
    fieldmarshaler.h
    finalizerthread.h
    frameworkexceptionloader.h
    gccover.h
    gcenv.h
    gcenv.ee.h
    gcenv.os.h
    gchelpers.h
    hosting.h
    ibclogger.h
    ilmarshalers.h
    interopconverter.h
    interoputil.h
    interoputil.inl
    interpreter.h
    interpreter.hpp
    invokeutil.h
    managedmdimport.hpp
    marshalnative.h
    methodtablebuilder.h
    mlinfo.h
    mscorlib.h
    multicorejit.h
    multicorejitimpl.h
    nativeeventsource.h
    nativeoverlapped.h
    nativelibrarynative.h
    objectlist.h
    olevariant.h
    pendingload.h
    profattach.h
    profattachclient.h
    profattachserver.h
    profdetach.h
    profilermetadataemitvalidator.h
    profilingenumerators.h
    profilinghelper.h
    profilinghelper.h
    proftoeeinterfaceimpl.h
    proftoeeinterfaceimpl.inl
    qcall.h
    reflectclasswriter.h
    reflectioninvocation.h
    runtimehandles.h
    sampleprofiler.h
    sha1.h
    simplerwlock.hpp
    sourceline.h
    stackingallocator.h
    stringliteralmap.h
    stubcache.h
    stubgen.h
    stubhelpers.h
    syncclean.hpp
    synch.h
    synchronizationcontextnative.h
    testhookmgr.h
    tieredcompilation.h
    threaddebugblockinginfo.h
    threadsuspend.h
    typeparse.h
    weakreferencenative.h
    ${VM_HEADERS_GDBJIT}
)

set(GC_SOURCES_WKS
    ${GC_SOURCES_DAC_AND_WKS_COMMON}
    ../gc/gceventstatus.cpp
    ../gc/gcconfig.cpp
    ../gc/gccommon.cpp
    ../gc/gcscan.cpp
    ../gc/gcsvr.cpp
    ../gc/gcwks.cpp
    ../gc/gchandletable.cpp
    ../gc/gceesvr.cpp
    ../gc/gceewks.cpp
    ../gc/gcload.cpp
    ../gc/softwarewritewatch.cpp
    ../gc/handletablecache.cpp)

set(GC_HEADERS_WKS
    ${GC_HEADERS_DAC_AND_WKS_COMMON}
    ../gc/gceventstatus.h
    ../gc/gcconfig.h
    ../gc/gcscan.h
    ../gc/gchandletableimpl.h
    ../gc/softwarewritewatch.h)

if(FEATURE_EVENT_TRACE)
    list(APPEND VM_SOURCES_WKS
        eventtrace.cpp
        )
    list(APPEND VM_HEADERS_WKS
        eventtracepriv.h
        )
endif(FEATURE_EVENT_TRACE)

if(FEATURE_STANDALONE_GC)
    list(APPEND VM_SOURCES_WKS
        gcenv.ee.standalone.cpp
        )
endif(FEATURE_STANDALONE_GC)

if(WIN32)

set(VM_SOURCES_DAC_AND_WKS_WIN32
    amsi.cpp
    clrtocomcall.cpp
    rcwwalker.cpp
    winrttypenameconverter.cpp
)

set(VM_HEADERS_DAC_AND_WKS_WIN32
    amsi.h
    clrtocomcall.h
    rcwwalker.h
    winrttypenameconverter.h
)

list(APPEND VM_SOURCES_WKS
    ${VM_SOURCES_DAC_AND_WKS_WIN32}
    # These should not be included for Linux
    classcompat.cpp
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
    mngstdinterfaces.cpp
    notifyexternals.cpp
    olecontexthelpers.cpp
    rcwrefcache.cpp
    rtlfunctions.cpp
    runtimecallablewrapper.cpp
    stacksampler.cpp
    stdinterfaces.cpp
    stdinterfaces_wrapper.cpp
    winrthelpers.cpp
)

list(APPEND VM_HEADERS_WKS
    ${VM_HEADERS_DAC_AND_WKS_WIN32}
    # These should not be included for Linux
    classcompat.h
    clrprivbinderwinrt.h
    clrprivtypecachewinrt.h
    comcache.h
    comcallablewrapper.h
    comconnectionpoints.h
    cominterfacemarshaler.h
    commtmemberinfomap.h
    comtoclrcall.h
    dispatchinfo.h
    dispparammarshaler.h
    dwreport.h
    eventreporter.h
    extensibleclassfactory.h
    mngstdinterfaces.h
    notifyexternals.h
    olecontexthelpers.h
    rcwrefcache.h
    rtlfunctions.h
    runtimecallablewrapper.h
    stacksampler.h
    stdinterfaces.h
    stdinterfaces_internal.h
    winrthelpers.h
)

list(APPEND VM_SOURCES_DAC
    ${VM_SOURCES_DAC_AND_WKS_WIN32}
    # These should not be included for Linux
    clrprivbinderwinrt.cpp
    clrprivtypecachewinrt.cpp
)

list(APPEND VM_HEADERS_DAC
    ${VM_HEADERS_DAC_AND_WKS_WIN32}
    # These should not be included for Linux
    clrprivbinderwinrt.h
    clrprivtypecachewinrt.h
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
        ${ARCH_SOURCES_DIR}/JitHelpers_InlineGetThread.asm
        ${ARCH_SOURCES_DIR}/JitHelpers_SingleAppDomain.asm
        ${ARCH_SOURCES_DIR}/JitHelpers_Slow.asm
        ${ARCH_SOURCES_DIR}/PInvokeStubs.asm
        ${ARCH_SOURCES_DIR}/RedirectedHandledJITCase.asm
        ${ARCH_SOURCES_DIR}/ThePreStubAMD64.asm
        ${ARCH_SOURCES_DIR}/ExternalMethodFixupThunk.asm
        ${ARCH_SOURCES_DIR}/UMThunkStub.asm
        ${ARCH_SOURCES_DIR}/VirtualCallStubAMD64.asm
    )

    set(VM_HEADERS_WKS_ARCH_ASM
        ${ARCH_SOURCES_DIR}/asmconstants.h
    )
elseif(CLR_CMAKE_TARGET_ARCH_I386)
    set(VM_SOURCES_WKS_ARCH_ASM
        ${ARCH_SOURCES_DIR}/RedirectedHandledJITCase.asm
        ${ARCH_SOURCES_DIR}/asmhelpers.asm
        ${ARCH_SOURCES_DIR}/gmsasm.asm
        ${ARCH_SOURCES_DIR}/jithelp.asm
        ${ARCH_SOURCES_DIR}/PInvokeStubs.asm
    )

    set(VM_HEADERS_WKS_ARCH_ASM
        ${ARCH_SOURCES_DIR}/asmconstants.h
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

    set(VM_HEADERS_WKS_ARCH_ASM
        ${ARCH_SOURCES_DIR}/asmconstants.h
    )
elseif(CLR_CMAKE_TARGET_ARCH_ARM64)
    set(VM_SOURCES_WKS_ARCH_ASM
        ${ARCH_SOURCES_DIR}/AsmHelpers.asm
        ${ARCH_SOURCES_DIR}/CallDescrWorkerARM64.asm
        ${ARCH_SOURCES_DIR}/CrtHelpers.asm
        ${ARCH_SOURCES_DIR}/PInvokeStubs.asm
    )

    set(VM_HEADERS_WKS_ARCH_ASM
        ${ARCH_SOURCES_DIR}/asmconstants.h
    )
endif()

else(WIN32)

    if(CLR_CMAKE_TARGET_ARCH_AMD64)
        set(VM_SOURCES_WKS_ARCH_ASM
            ${ARCH_SOURCES_DIR}/asmhelpers.S
            ${ARCH_SOURCES_DIR}/calldescrworkeramd64.S
            ${ARCH_SOURCES_DIR}/crthelpers.S
            ${ARCH_SOURCES_DIR}/externalmethodfixupthunk.S
            ${ARCH_SOURCES_DIR}/getstate.S
            ${ARCH_SOURCES_DIR}/jithelpers_fast.S
            ${ARCH_SOURCES_DIR}/jithelpers_fastwritebarriers.S
            ${ARCH_SOURCES_DIR}/jithelpers_singleappdomain.S
            ${ARCH_SOURCES_DIR}/jithelpers_slow.S
            ${ARCH_SOURCES_DIR}/pinvokestubs.S
            ${ARCH_SOURCES_DIR}/theprestubamd64.S
            ${ARCH_SOURCES_DIR}/unixasmhelpers.S
            ${ARCH_SOURCES_DIR}/umthunkstub.S
            ${ARCH_SOURCES_DIR}/virtualcallstubamd64.S
        )
    elseif(CLR_CMAKE_TARGET_ARCH_I386)
        set(VM_SOURCES_WKS_ARCH_ASM
            ${ARCH_SOURCES_DIR}/ehhelpers.S
            ${ARCH_SOURCES_DIR}/asmhelpers.S
            ${ARCH_SOURCES_DIR}/jithelp.S
            ${ARCH_SOURCES_DIR}/gmsasm.S
            ${ARCH_SOURCES_DIR}/pinvokestubs.S
            ${ARCH_SOURCES_DIR}/umthunkstub.S
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

    set(VM_HEADERS_DAC_AND_WKS_ARCH
        ${ARCH_SOURCES_DIR}/asmconstants.h
        ${ARCH_SOURCES_DIR}/cgencpu.h
        ${ARCH_SOURCES_DIR}/excepcpu.h
        ${ARCH_SOURCES_DIR}/gmscpu.h
        ${ARCH_SOURCES_DIR}/stublinkeramd64.h
    )

    set(VM_SOURCES_WKS_ARCH
        ${ARCH_SOURCES_DIR}/jithelpersamd64.cpp
        ${ARCH_SOURCES_DIR}/jitinterfaceamd64.cpp
        ${ARCH_SOURCES_DIR}/profiler.cpp
        exceptionhandling.cpp
        gcinfodecoder.cpp
        jitinterfacegen.cpp
    )

    set(VM_HEADERS_WKS_ARCH
        exceptionhandling.h
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

    set(VM_HEADERS_DAC_AND_WKS_ARCH
        exinfo.h
        ${ARCH_SOURCES_DIR}/cgencpu.h
        ${ARCH_SOURCES_DIR}/excepcpu.h
        ${ARCH_SOURCES_DIR}/gmscpu.h
        ${ARCH_SOURCES_DIR}/stublinkerx86.h
    )

    set(VM_SOURCES_WKS_ARCH
        ${ARCH_SOURCES_DIR}/jitinterfacex86.cpp
        ${ARCH_SOURCES_DIR}/profiler.cpp
        exceptionhandling.cpp
        gcinfodecoder.cpp
    )

    set(VM_HEADERS_WKS_ARCH
        exceptionhandling.h
    )
elseif(CLR_CMAKE_TARGET_ARCH_ARM)
    set(VM_SOURCES_DAC_AND_WKS_ARCH
        ${ARCH_SOURCES_DIR}/exceparm.cpp
        ${ARCH_SOURCES_DIR}/stubs.cpp
        ${ARCH_SOURCES_DIR}/armsinglestepper.cpp
    )

    set(VM_HEADERS_DAC_AND_WKS_ARCH
        ${ARCH_SOURCES_DIR}/asmconstants.h
        ${ARCH_SOURCES_DIR}/excepcpu.h
        ${ARCH_SOURCES_DIR}/virtualcallstubcpu.hpp
    )

    set(VM_SOURCES_WKS_ARCH
        ${ARCH_SOURCES_DIR}/jithelpersarm.cpp
        ${ARCH_SOURCES_DIR}/profiler.cpp
        exceptionhandling.cpp
        gcinfodecoder.cpp
    )

    set(VM_HEADERS_WKS_ARCH
        exceptionhandling.h
    )
elseif(CLR_CMAKE_TARGET_ARCH_ARM64)
    set(VM_SOURCES_DAC_AND_WKS_ARCH
        ${ARCH_SOURCES_DIR}/stubs.cpp
        exceptionhandling.cpp
        gcinfodecoder.cpp
    )

    set(VM_HEADERS_DAC_AND_WKS_ARCH
        ${ARCH_SOURCES_DIR}/virtualcallstubcpu.hpp
        exceptionhandling.h
    )
endif()

if(CLR_CMAKE_PLATFORM_UNIX)
    list(APPEND VM_SOURCES_WKS_ARCH
        ${ARCH_SOURCES_DIR}/unixstubs.cpp
    )
endif(CLR_CMAKE_PLATFORM_UNIX)

set(VM_SOURCES_DAC_ARCH
    exceptionhandling.cpp
)

set(VM_HEADERS_DAC_ARCH
    exceptionhandling.h
)

list(APPEND VM_SOURCES_WKS
    ${VM_SOURCES_WKS_ARCH}
    ${VM_SOURCES_DAC_AND_WKS_ARCH}
)

list(APPEND VM_HEADERS_WKS
    ${VM_HEADERS_WKS_ARCH}
    ${VM_HEADERS_DAC_AND_WKS_ARCH}
)

list(APPEND VM_SOURCES_DAC
    ${VM_SOURCES_DAC_ARCH}
    ${VM_SOURCES_DAC_AND_WKS_ARCH}
)

list(APPEND VM_HEADERS_DAC
    ${VM_HEADERS_DAC_ARCH}
    ${VM_HEADERS_DAC_AND_WKS_ARCH}
)

list(APPEND VM_SOURCES_WKS
     ${GC_SOURCES_WKS}
)

list(APPEND VM_HEADERS_WKS
     ${GC_HEADERS_WKS}
)

# The DAC does need GC sources in order to link correctly, even if
# it's not used.
list(APPEND VM_SOURCES_DAC
    ${GC_SOURCES_DAC}
)

list(APPEND VM_HEADERS_DAC
    ${GC_HEADERS_DAC}
)

if (WIN32)
    list(APPEND VM_SOURCES_WKS ${VM_HEADERS_WKS})
    list(APPEND VM_SOURCES_WKS_ARCH_ASM ${VM_HEADERS_WKS_ARCH_ASM})
    list(APPEND VM_SOURCES_DAC ${VM_HEADERS_DAC})
endif(WIN32)

convert_to_absolute_path(VM_SOURCES_WKS ${VM_SOURCES_WKS})
convert_to_absolute_path(VM_SOURCES_WKS_ARCH_ASM ${VM_SOURCES_WKS_ARCH_ASM})
convert_to_absolute_path(VM_SOURCES_DAC ${VM_SOURCES_DAC})

add_subdirectory(dac)
add_subdirectory(wks)

if(FEATURE_PERFTRACING)
    add_subdirectory(eventing)
endif(FEATURE_PERFTRACING)
