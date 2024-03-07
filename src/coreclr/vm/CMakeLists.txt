set(CMAKE_INCLUDE_CURRENT_DIR ON)

# Needed due to the cmunged files being in the binary folders, the set(CMAKE_INCLUDE_CURRENT_DIR ON) is not enough
include_directories(BEFORE ${CMAKE_CURRENT_SOURCE_DIR})
include_directories(${ARCH_SOURCES_DIR})
include_directories(${CMAKE_CURRENT_SOURCE_DIR}/../interop/inc)
include_directories(${CLR_SRC_NATIVE_DIR})

# needed when zLib compression is used
include_directories(${CLR_SRC_NATIVE_DIR}/libs/System.IO.Compression.Native)
include_directories(${CLR_SRC_NATIVE_DIR}/libs/Common)

add_definitions(-DUNICODE)
add_definitions(-D_UNICODE)

if(FEATURE_AUTO_TRACE)
    add_definitions(-DFEATURE_AUTO_TRACE)
endif(FEATURE_AUTO_TRACE)

foreach (Config DEBUG CHECKED)
    add_compile_definitions($<$<CONFIG:${Config}>:WRITE_BARRIER_CHECK>)
endforeach (Config)

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

if(FEATURE_PERFTRACING)
    set(CORECLR_EVENTPIPE_SHIM_DIR ${CMAKE_CURRENT_SOURCE_DIR}/eventing/eventpipe)
    include_directories(${CORECLR_EVENTPIPE_SHIM_DIR})
endif(FEATURE_PERFTRACING)

set(VM_SOURCES_DAC_AND_WKS_COMMON
    appdomain.cpp
    array.cpp
    assembly.cpp
    assemblybinder.cpp
    binder.cpp
    bundle.cpp
    castcache.cpp
    callcounting.cpp
    ceeload.cpp
    class.cpp
    classhash.cpp
    classlayoutinfo.cpp
    clsload.cpp
    codeman.cpp
    codeversion.cpp
    contractimpl.cpp
    corhost.cpp
    crst.cpp
    debugdebugger.cpp
    debughelp.cpp
    debuginfostore.cpp
    decodemd.cpp
    disassembler.cpp
    domainassembly.cpp
    dynamicmethod.cpp
    ecall.cpp
    eedbginterfaceimpl.cpp
    eehash.cpp
    eetwain.cpp
    encee.cpp
    excep.cpp
    exinfo.cpp
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
    nativeimage.cpp
    object.cpp
    onstackreplacement.cpp
    peassembly.cpp
    peimage.cpp
    perfmap.cpp
    perfinfo.cpp
    pgo.cpp
    precode.cpp
    prestub.cpp
    readytorunstandalonemethodmetadata.cpp
    rejit.cpp
    sigformat.cpp
    siginfo.cpp
    spinlock.cpp
    stackwalk.cpp
    stublink.cpp
    stubmgr.cpp
    syncblk.cpp
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
    assemblybinder.h
    binder.h
    castcache.h
    callcounting.h
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
    contractimpl.h
    crst.h
    debugdebugger.h
    debuginfostore.h
    decodemd.h
    disassembler.h
    domainassembly.h
    dynamicmethod.h
    ecall.h
    eedbginterfaceimpl.h
    eedbginterfaceimpl.inl
    eehash.h
    eehash.inl
    encee.h
    excep.h
    exinfo.h
    exstate.h
    field.h
    fptrstubs.h
    frames.h
    gccover.h
    gctoclreventsink.h
    gcheaputilities.h
    generics.h
    hash.h
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
    onstackreplacement.h
    peassembly.h
    peassembly.inl
    peimage.h
    peimage.inl
    peimagelayout.h
    peimagelayout.inl
    perfmap.h
    perfinfo.h
    pgo.h
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
    assemblynative.cpp
    assemblyspec.cpp
    baseassemblyspec.cpp
    cachelinealloc.cpp
    callconvbuilder.cpp
    callhelpers.cpp
    callsiteinspect.cpp
    clrconfignative.cpp
    clrex.cpp
    clrvarargs.cpp
    comdatetime.cpp
    comdelegate.cpp
    comdependenthandle.cpp
    comdynamic.cpp
    commodule.cpp
    compatibilityswitch.cpp
    comsynchronizable.cpp
    comthreadpool.cpp
    comutilnative.cpp
    comwaithandle.cpp
    coreassemblyspec.cpp
    corelib.cpp # <DisablePrecompiledHeaders>true</DisablePrecompiledHeaders>
    customattribute.cpp
    custommarshalerinfo.cpp
    autotrace.cpp
    dllimport.cpp
    dllimportcallback.cpp
    dynamicinterfacecastable.cpp
    eeconfig.cpp
    eecontract.cpp
    eepolicy.cpp
    eetoprofinterfaceimpl.cpp
    eventpipeinternal.cpp
    eventstore.cpp
    fcall.cpp
    fieldmarshaler.cpp
    finalizerthread.cpp
    frozenobjectheap.cpp
    gccover.cpp
    gcenv.ee.cpp
    gcenv.ee.common.cpp
    gchelpers.cpp
    genanalysis.cpp
    genmeth.cpp
    hosting.cpp
    hostinformation.cpp
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
    multicorejit.cpp # Condition="'$(FeatureMulticoreJIT)' == 'true'
    multicorejitplayer.cpp # Condition="'$(FeatureMulticoreJIT)' == 'true'
    nativeeventsource.cpp
    nativelibrary.cpp
    nativelibrarynative.cpp
    objectlist.cpp
    olevariant.cpp
    pendingload.cpp
    pinvokeoverride.cpp
    profdetach.cpp
    profilingenumerators.cpp
    profilinghelper.cpp
    proftoeeinterfaceimpl.cpp
    qcall.cpp
    qcallentrypoints.cpp
    reflectclasswriter.cpp
    reflectioninvocation.cpp
    runtimehandles.cpp
    safehandle.cpp
    simplerwlock.cpp
    stackingallocator.cpp
    stringliteralmap.cpp
    stubcache.cpp
    stubgen.cpp
    stubhelpers.cpp
    syncclean.cpp
    synch.cpp
    tailcallhelp.cpp
    threaddebugblockinginfo.cpp
    threadsuspend.cpp
    typeparse.cpp
    weakreferencenative.cpp
    yieldprocessornormalized.cpp
    ${VM_SOURCES_GDBJIT}
)

# coreclr needs to compile codeman.cpp differently depending on flavor (i.e. dll vs. static lib))
list(REMOVE_ITEM VM_SOURCES_WKS codeman.cpp)

set(VM_HEADERS_WKS
    ${VM_HEADERS_DAC_AND_WKS_COMMON}
    ../inc/jithelpers.h
    appdomainnative.hpp
    assemblynative.hpp
    assemblyspec.hpp
    assemblyspecbase.h
    baseassemblyspec.h
    baseassemblyspec.inl
    cachelinealloc.h
    callhelpers.h
    callsiteinspect.h
    callconvbuilder.hpp
    ceemain.h
    clrconfignative.h
    clrex.h
    clrvarargs.h
    comdatetime.h
    comdelegate.h
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
    autotrace.h
    diagnosticserveradapter.h
    dllimport.h
    dllimportcallback.h
    eeconfig.h
    eecontract.h
    eepolicy.h
    eeprofinterfaces.h
    eeprofinterfaces.inl
    eetoprofinterfaceimpl.h
    eetoprofinterfaceimpl.inl
    eventpipeadapter.h
    eventpipeadaptertypes.h
    eventpipeinternal.h
    eventstore.hpp
    fcall.h
    fieldmarshaler.h
    finalizerthread.h
    frozenobjectheap.h
    gcenv.h
    gcenv.os.h
    gchelpers.h
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
    corelib.h
    multicorejit.h
    multicorejitimpl.h
    nativeeventsource.h
    objectlist.h
    olevariant.h
    pendingload.h
    profdetach.h
    profilingenumerators.h
    profilinghelper.h
    proftoeeinterfaceimpl.h
    proftoeeinterfaceimpl.inl
    qcall.h
    reflectclasswriter.h
    reflectioninvocation.h
    runtimehandles.h
    simplerwlock.hpp
    stackingallocator.h
    stringliteralmap.h
    stubcache.h
    stubgen.h
    stubhelpers.h
    syncclean.hpp
    synch.h
    tailcallhelp.h
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

if (CLR_CMAKE_TARGET_ARCH_AMD64 AND CLR_CMAKE_TARGET_WIN32)
  set ( GC_SOURCES_WKS
    ${GC_SOURCES_WKS}
    ../gc/vxsort/isa_detection.cpp
    ../gc/vxsort/do_vxsort_avx2.cpp
    ../gc/vxsort/do_vxsort_avx512.cpp
    ../gc/vxsort/machine_traits.avx2.cpp
    ../gc/vxsort/smallsort/bitonic_sort.AVX2.int64_t.generated.cpp
    ../gc/vxsort/smallsort/bitonic_sort.AVX2.int32_t.generated.cpp
    ../gc/vxsort/smallsort/bitonic_sort.AVX512.int64_t.generated.cpp
    ../gc/vxsort/smallsort/bitonic_sort.AVX512.int32_t.generated.cpp
    ../gc/vxsort/smallsort/avx2_load_mask_tables.cpp
)
endif (CLR_CMAKE_TARGET_ARCH_AMD64 AND CLR_CMAKE_TARGET_WIN32)

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
        eventtrace_bulktype.cpp
        eventtrace_gcheap.cpp
        )
    list(APPEND VM_HEADERS_WKS
        eventtracepriv.h
        )
endif(FEATURE_EVENT_TRACE)

#
# Targeted interop scenarios
#
# Shared assets
list(APPEND VM_SOURCES_WKS
    interoplibinterface_shared.cpp
)
list(APPEND VM_HEADERS_WKS
    interoplibinterface.h
)

if(FEATURE_COMWRAPPERS)
    list(APPEND VM_SOURCES_WKS
        interoplibinterface_comwrappers.cpp
        rcwrefcache.cpp
    )
    list(APPEND VM_HEADERS_WKS
        rcwrefcache.h
    )
endif(FEATURE_COMWRAPPERS)

if(FEATURE_OBJCMARSHAL)
    list(APPEND VM_SOURCES_WKS
        interoplibinterface_objc.cpp
    )
endif(FEATURE_OBJCMARSHAL)

if(CLR_CMAKE_TARGET_WIN32)

    set(VM_SOURCES_DAC_AND_WKS_WIN32
        amsi.cpp
    )

    set(VM_HEADERS_DAC_AND_WKS_WIN32
        amsi.h
    )

    # COM interop scenarios
    list(APPEND VM_SOURCES_DAC_AND_WKS_WIN32
        clrtocomcall.cpp
        )
    list(APPEND VM_HEADERS_DAC_AND_WKS_WIN32
        clrtocomcall.h
        )

    list(APPEND VM_SOURCES_WKS
        ${VM_SOURCES_DAC_AND_WKS_WIN32}
        # These should not be included for Linux
        dwreport.cpp
        eventreporter.cpp
        rtlfunctions.cpp
    )

    list(APPEND VM_HEADERS_WKS
        ${VM_HEADERS_DAC_AND_WKS_WIN32}
        # These should not be included for Linux
        dwreport.h
        eventreporter.h
        rtlfunctions.h
    )

    # COM interop scenarios
    list(APPEND VM_SOURCES_WKS
        classcompat.cpp
        comcache.cpp
        comcallablewrapper.cpp
        comconnectionpoints.cpp
        cominterfacemarshaler.cpp
        commtmemberinfomap.cpp
        comtoclrcall.cpp
        dispatchinfo.cpp
        dispparammarshaler.cpp
        mngstdinterfaces.cpp
        olecontexthelpers.cpp
        runtimecallablewrapper.cpp
        stdinterfaces.cpp
        stdinterfaces_wrapper.cpp
        )
    list(APPEND VM_HEADERS_WKS
        classcompat.h
        comcache.h
        comcallablewrapper.h
        comconnectionpoints.h
        cominterfacemarshaler.h
        commtmemberinfomap.h
        comtoclrcall.h
        dispatchinfo.h
        dispparammarshaler.h
        mngstdinterfaces.h
        olecontexthelpers.h
        runtimecallablewrapper.h
        stdinterfaces.h
        stdinterfaces_internal.h
        )

    list(APPEND VM_SOURCES_DAC
        ${VM_SOURCES_DAC_AND_WKS_WIN32}
    )

    list(APPEND VM_HEADERS_DAC
        ${VM_HEADERS_DAC_AND_WKS_WIN32}
    )

endif(CLR_CMAKE_TARGET_WIN32)

if(CLR_CMAKE_TARGET_WIN32)

if(CLR_CMAKE_TARGET_ARCH_AMD64)
    set(VM_SOURCES_WKS_ARCH_ASM
        ${ARCH_SOURCES_DIR}/AsmHelpers.asm
        ${ARCH_SOURCES_DIR}/CallDescrWorkerAMD64.asm
        ${ARCH_SOURCES_DIR}/ComCallPreStub.asm
        ${ARCH_SOURCES_DIR}/GenericComCallStubs.asm
        ${ARCH_SOURCES_DIR}/GenericComPlusCallStubs.asm
        ${ARCH_SOURCES_DIR}/getstate.asm
        ${ARCH_SOURCES_DIR}/JitHelpers_Fast.asm
        ${ARCH_SOURCES_DIR}/JitHelpers_FastWriteBarriers.asm
        ${ARCH_SOURCES_DIR}/JitHelpers_InlineGetThread.asm
        ${ARCH_SOURCES_DIR}/JitHelpers_SingleAppDomain.asm
        ${ARCH_SOURCES_DIR}/JitHelpers_Slow.asm
        ${ARCH_SOURCES_DIR}/patchedcode.asm
        ${ARCH_SOURCES_DIR}/PInvokeStubs.asm
        ${ARCH_SOURCES_DIR}/RedirectedHandledJITCase.asm
        ${ARCH_SOURCES_DIR}/ThePreStubAMD64.asm
        ${ARCH_SOURCES_DIR}/thunktemplates.asm
        ${ARCH_SOURCES_DIR}/Context.asm
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
        ${ARCH_SOURCES_DIR}/thunktemplates.asm
    )

    set(VM_HEADERS_WKS_ARCH_ASM
        ${ARCH_SOURCES_DIR}/asmconstants.h
    )
elseif(CLR_CMAKE_TARGET_ARCH_ARM64)
    set(VM_SOURCES_WKS_ARCH_ASM
        ${ARCH_SOURCES_DIR}/AsmHelpers.asm
        ${ARCH_SOURCES_DIR}/CallDescrWorkerARM64.asm
        ${ARCH_SOURCES_DIR}/patchedcode.asm
        ${ARCH_SOURCES_DIR}/PInvokeStubs.asm
        ${ARCH_SOURCES_DIR}/thunktemplates.asm
    )

    set(VM_HEADERS_WKS_ARCH_ASM
        ${ARCH_SOURCES_DIR}/asmconstants.h
    )
endif()

else(CLR_CMAKE_TARGET_WIN32)

    if(CLR_CMAKE_TARGET_ARCH_AMD64)
        set(VM_SOURCES_WKS_ARCH_ASM
            ${ARCH_SOURCES_DIR}/asmhelpers.S
            ${ARCH_SOURCES_DIR}/calldescrworkeramd64.S
            ${ARCH_SOURCES_DIR}/externalmethodfixupthunk.S
            ${ARCH_SOURCES_DIR}/getstate.S
            ${ARCH_SOURCES_DIR}/jithelpers_fast.S
            ${ARCH_SOURCES_DIR}/jithelpers_fastwritebarriers.S
            ${ARCH_SOURCES_DIR}/jithelpers_singleappdomain.S
            ${ARCH_SOURCES_DIR}/jithelpers_slow.S
            ${ARCH_SOURCES_DIR}/patchedcode.S
            ${ARCH_SOURCES_DIR}/pinvokestubs.S
            ${ARCH_SOURCES_DIR}/redirectedhandledjitcase.S
            ${ARCH_SOURCES_DIR}/theprestubamd64.S
            ${ARCH_SOURCES_DIR}/thunktemplates.S
            ${ARCH_SOURCES_DIR}/Context.S
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
            ${ARCH_SOURCES_DIR}/thunktemplates.S
            )
    elseif(CLR_CMAKE_TARGET_ARCH_ARM)
        set(VM_SOURCES_WKS_ARCH_ASM
            ${ARCH_SOURCES_DIR}/asmhelpers.S
            ${ARCH_SOURCES_DIR}/ehhelpers.S
            ${ARCH_SOURCES_DIR}/patchedcode.S
            ${ARCH_SOURCES_DIR}/pinvokestubs.S
            ${ARCH_SOURCES_DIR}/thunktemplates.S
        )
    elseif(CLR_CMAKE_TARGET_ARCH_ARM64)
        set(VM_SOURCES_WKS_ARCH_ASM
            ${ARCH_SOURCES_DIR}/asmhelpers.S
            ${ARCH_SOURCES_DIR}/calldescrworkerarm64.S
            ${ARCH_SOURCES_DIR}/patchedcode.S
            ${ARCH_SOURCES_DIR}/pinvokestubs.S
            ${ARCH_SOURCES_DIR}/thunktemplates.S
        )
    elseif(CLR_CMAKE_TARGET_ARCH_LOONGARCH64)
        set(VM_SOURCES_WKS_ARCH_ASM
            ${ARCH_SOURCES_DIR}/asmhelpers.S
            ${ARCH_SOURCES_DIR}/calldescrworkerloongarch64.S
            ${ARCH_SOURCES_DIR}/pinvokestubs.S
            ${ARCH_SOURCES_DIR}/thunktemplates.S
        )
    elseif(CLR_CMAKE_TARGET_ARCH_RISCV64)
        set(VM_SOURCES_WKS_ARCH_ASM
            ${ARCH_SOURCES_DIR}/asmhelpers.S
            ${ARCH_SOURCES_DIR}/calldescrworkerriscv64.S
            ${ARCH_SOURCES_DIR}/pinvokestubs.S
            ${ARCH_SOURCES_DIR}/thunktemplates.S
        )
    endif()

endif(CLR_CMAKE_TARGET_WIN32)


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
        ${ARCH_SOURCES_DIR}/cgenx86.cpp
        ${ARCH_SOURCES_DIR}/excepx86.cpp
        ${ARCH_SOURCES_DIR}/gmsx86.cpp
        ${ARCH_SOURCES_DIR}/stublinkerx86.cpp
    )

    set(VM_HEADERS_DAC_AND_WKS_ARCH
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
    )

    set(VM_HEADERS_DAC_AND_WKS_ARCH
        ${ARCH_SOURCES_DIR}/asmconstants.h
        ${ARCH_SOURCES_DIR}/excepcpu.h
        ${ARCH_SOURCES_DIR}/virtualcallstubcpu.hpp
    )

    set(VM_SOURCES_WKS_ARCH
        ${ARCH_SOURCES_DIR}/profiler.cpp
        ${ARCH_SOURCES_DIR}/singlestepper.cpp
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
    )

    set(VM_HEADERS_DAC_AND_WKS_ARCH
        ${ARCH_SOURCES_DIR}/virtualcallstubcpu.hpp
        exceptionhandling.h
    )

    set(VM_SOURCES_WKS_ARCH
        ${ARCH_SOURCES_DIR}/profiler.cpp
        gcinfodecoder.cpp
    )

    if(CLR_CMAKE_HOST_UNIX)
        list(APPEND VM_SOURCES_WKS_ARCH
            ${ARCH_SOURCES_DIR}/singlestepper.cpp
        )
    endif(CLR_CMAKE_HOST_UNIX)
elseif(CLR_CMAKE_TARGET_ARCH_LOONGARCH64)
    set(VM_SOURCES_DAC_AND_WKS_ARCH
        ${ARCH_SOURCES_DIR}/stubs.cpp
        exceptionhandling.cpp
    )

    set(VM_HEADERS_DAC_AND_WKS_ARCH
        ${ARCH_SOURCES_DIR}/virtualcallstubcpu.hpp
        exceptionhandling.h
    )

    set(VM_SOURCES_WKS_ARCH
        ${ARCH_SOURCES_DIR}/profiler.cpp
        gcinfodecoder.cpp
    )
elseif(CLR_CMAKE_TARGET_ARCH_RISCV64)
    set(VM_SOURCES_DAC_AND_WKS_ARCH
        ${ARCH_SOURCES_DIR}/stubs.cpp
        exceptionhandling.cpp
    )

    set(VM_HEADERS_DAC_AND_WKS_ARCH
        ${ARCH_SOURCES_DIR}/virtualcallstubcpu.hpp
        exceptionhandling.h
    )

    set(VM_SOURCES_WKS_ARCH
        ${ARCH_SOURCES_DIR}/profiler.cpp
        ${ARCH_SOURCES_DIR}/singlestepper.cpp
        gcinfodecoder.cpp
    )
endif()

if(CLR_CMAKE_HOST_UNIX)
    list(APPEND VM_SOURCES_WKS_ARCH
        ${ARCH_SOURCES_DIR}/unixstubs.cpp
    )
endif(CLR_CMAKE_HOST_UNIX)

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

if (CLR_CMAKE_TARGET_WIN32)
    list(APPEND VM_SOURCES_WKS ${VM_HEADERS_WKS})
    list(APPEND VM_SOURCES_WKS ${VM_HEADERS_WKS_ARCH_ASM})
    list(APPEND VM_SOURCES_DAC ${VM_HEADERS_DAC})
endif(CLR_CMAKE_TARGET_WIN32)

# these two files we need to compile differently for standalone and mergeable flavors of WKS
set (VM_SOURCES_WKS_SPECIAL
    codeman.cpp
    ceemain.cpp
    peimagelayout.cpp
)

list(APPEND VM_SOURCES_DAC
    peimagelayout.cpp
)

# gcdecode.cpp is included by both JIT and VM. to avoid duplicate definitions we need to
# treat it in a special way when statically linking with both VM and JIT
if(CLR_CMAKE_TARGET_ARCH_I386)
    if(CLR_CMAKE_TARGET_WIN32)
        list(APPEND VM_SOURCES_DAC
            gcdecode.cpp
        )
    endif()
    list(APPEND VM_SOURCES_WKS_SPECIAL
        gcdecode.cpp
    )
endif()

convert_to_absolute_path(VM_SOURCES_WKS ${VM_SOURCES_WKS})
convert_to_absolute_path(VM_SOURCES_WKS_ARCH_ASM ${VM_SOURCES_WKS_ARCH_ASM})
convert_to_absolute_path(VM_SOURCES_DAC ${VM_SOURCES_DAC})
convert_to_absolute_path(VM_SOURCES_WKS_SPECIAL ${VM_SOURCES_WKS_SPECIAL})

add_library_clr(cee_dac ${VM_SOURCES_DAC})
add_dependencies(cee_dac eventing_headers)
set_target_properties(cee_dac PROPERTIES DAC_COMPONENT TRUE)
target_precompile_headers(cee_dac PRIVATE [["common.h"]])

add_subdirectory(wks)

if(FEATURE_PERFTRACING)
    add_subdirectory(eventing)
endif(FEATURE_PERFTRACING)
