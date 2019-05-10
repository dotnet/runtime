include(${CLR_DIR}/crossgen.cmake)

set(VM_CROSSGEN_SOURCES
  ../appdomain.cpp
  ../array.cpp
  ../assembly.cpp
  ../assemblyspec.cpp
  ../baseassemblyspec.cpp
  ../binder.cpp
  ../ceeload.cpp
  ../ceemain.cpp
  ../class.cpp
  ../classhash.cpp
  ../clrex.cpp
  ../clrprivbinderutil.cpp
  ../clsload.cpp
  ../codeman.cpp
  ../codeversion.cpp
  ../comdelegate.cpp
  ../compile.cpp
  ../contractimpl.cpp
  ../coreassemblyspec.cpp
  ../corebindresult.cpp
  ../crossgencompile.cpp
  ../custommarshalerinfo.cpp
  ../dataimage.cpp
  ../debuginfostore.cpp
  ../decodemd.cpp
  ../dllimport.cpp
  ../dllimportcallback.cpp
  ../domainfile.cpp
  ../ecall.cpp
  ../eeconfig.cpp
  ../eehash.cpp
  ../eetwain.cpp
  ../excep.cpp
  ../field.cpp
  ../fieldmarshaler.cpp
  ../formattype.cpp
  ../gcinfodecoder.cpp
  ../genericdict.cpp
  ../generics.cpp
  ../genmeth.cpp
  ../hash.cpp
  ../ilinstrumentation.cpp
  ../ilmarshalers.cpp
  ../ilstubcache.cpp
  ../ilstubresolver.cpp
  ../inlinetracking.cpp
  ../instmethhash.cpp
  ../interoputil.cpp
  ../invokeutil.cpp
  ../jithost.cpp
  ../jitinterface.cpp
  ../loaderallocator.cpp
  ../memberload.cpp
  ../method.cpp
  ../methodimpl.cpp
  ../methodtable.cpp
  ../methodtablebuilder.cpp
  ../mlinfo.cpp
  ../mscorlib.cpp
  ../olevariant.cpp
  ../pefile.cpp
  ../peimage.cpp
  ../peimagelayout.cpp
  ../pendingload.cpp
  ../precode.cpp
  ../sha1.cpp
  ../sigformat.cpp
  ../siginfo.cpp
  ../simplerwlock.cpp
  ../spinlock.cpp
  ../stackingallocator.cpp
  ../stubcache.cpp
  ../stubgen.cpp
  ../stublink.cpp
  ../typectxt.cpp
  ../typedesc.cpp
  ../typeequivalencehash.cpp
  ../typehandle.cpp
  ../typehash.cpp
  ../typeparse.cpp
  ../typestring.cpp
  ../util.cpp
  ../vars.cpp
  ../versionresilienthashcode.cpp
  ../zapsig.cpp
)

set(VM_CROSSGEN_HEADERS
  ../appdomain.hpp
  ../appdomain.inl
  ../array.h
  ../assembly.hpp
  ../assemblyspec.hpp
  ../assemblyspecbase.h
  ../baseassemblyspec.h
  ../baseassemblyspec.inl
  ../binder.h
  ../ceeload.h
  ../ceeload.inl
  ../ceemain.h
  ../class.h
  ../class.inl
  ../classhash.h
  ../clrex.h
  ../clsload.hpp
  ../clsload.inl
  ../codeman.h
  ../codeversion.h
  ../comdelegate.h
  ../compile.h
  ../contractimpl.h
  ../custommarshalerinfo.h
  ../dataimage.h
  ../debuginfostore.h
  ../decodemd.h
  ../dllimport.h
  ../dllimportcallback.h
  ../domainfile.h
  ../domainfile.inl
  ../ecall.h
  ../eeconfig.h
  ../eehash.h
  ../eehash.inl
  ../excep.h
  ../field.h
  ../fieldmarshaler.h
  ../genericdict.h
  ../generics.h
  ../generics.inl
  ../hash.h
  ../ilinstrumentation.h
  ../ilmarshalers.h
  ../ilstubcache.h
  ../ilstubresolver.h
  ../inlinetracking.h
  ../instmethhash.h
  ../interoputil.h
  ../invokeutil.h
  ../jithost.h
  ../jitinterface.h
  ../loaderallocator.hpp
  ../loaderallocator.inl
  ../memberload.h
  ../method.hpp
  ../method.inl
  ../methodimpl.h
  ../methodtable.h
  ../methodtable.inl
  ../methodtablebuilder.h
  ../methodtablebuilder.inl
  ../mlinfo.h
  ../mscorlib.h
  ../olevariant.h
  ../pefile.h
  ../pefile.inl
  ../peimage.h
  ../peimage.inl
  ../peimagelayout.h
  ../peimagelayout.inl
  ../pendingload.h
  ../precode.h
  ../sha1.h
  ../sigformat.h
  ../siginfo.hpp
  ../simplerwlock.hpp
  ../spinlock.h
  ../stackingallocator.h
  ../stubcache.h
  ../stubgen.h
  ../stublink.h
  ../stublink.inl
  ../typectxt.h
  ../typedesc.h
  ../typedesc.inl
  ../typeequivalencehash.hpp
  ../typehandle.h
  ../typehandle.inl
  ../typehash.h
  ../typeparse.h
  ../typestring.h
  ../util.hpp
  ../vars.hpp
  ../versionresilienthashcode.h
  ../zapsig.h
)

if(FEATURE_READYTORUN)
  list(APPEND VM_CROSSGEN_SOURCES
    ../readytoruninfo.cpp
  )
  list(APPEND VM_CROSSGEN_HEADERS
    ../readytoruninfo.h
  )
endif(FEATURE_READYTORUN)

include_directories(BEFORE ..)
include_directories(${CLR_DIR}/src/gc)
include_directories(../${ARCH_SOURCES_DIR})

if(CLR_CMAKE_TARGET_ARCH_AMD64)
  list(APPEND VM_CROSSGEN_SOURCES
    ../${ARCH_SOURCES_DIR}/stublinkeramd64.cpp
  )
  list(APPEND VM_CROSSGEN_HEADERS
    ../${ARCH_SOURCES_DIR}/stublinkeramd64.h
  )
elseif(CLR_CMAKE_TARGET_ARCH_I386)
  list(APPEND VM_CROSSGEN_SOURCES
    ../${ARCH_SOURCES_DIR}/stublinkerx86.cpp
    ../gcdecode.cpp
  )
  list(APPEND VM_CROSSGEN_HEADERS
    ../${ARCH_SOURCES_DIR}/stublinkerx86.h
  )
elseif(CLR_CMAKE_TARGET_ARCH_ARM)
  list(APPEND VM_CROSSGEN_SOURCES
    ../${ARCH_SOURCES_DIR}/stubs.cpp
  )
elseif(CLR_CMAKE_TARGET_ARCH_ARM64)
  list(APPEND VM_CROSSGEN_SOURCES
    ../${ARCH_SOURCES_DIR}/stubs.cpp
  )
else()
  clr_unknown_arch()
endif()

if (WIN32)
  list(APPEND VM_CROSSGEN_SOURCES
    ../classcompat.cpp
    ../clrprivbinderwinrt.cpp
    ../clrprivtypecachewinrt.cpp
    ../comtoclrcall.cpp
    ../clrtocomcall.cpp
    ../crossgenroparsetypename.cpp
    ../crossgenroresolvenamespace.cpp
    ../runtimecallablewrapper.cpp
    ../winrthelpers.cpp
    ../winrttypenameconverter.cpp
  )

  list(APPEND VM_CROSSGEN_HEADERS
    ../classcompat.h
    ../clrprivbinderwinrt.h
    ../clrprivtypecachewinrt.h
    ../clrtocomcall.h
    ../comtoclrcall.h
    ../crossgenroresolvenamespace.h
    ../runtimecallablewrapper.h
    ../winrthelpers.h
    ../winrttypenameconverter.h
  )

  add_precompiled_header(common.h ../common.cpp VM_CROSSGEN_SOURCES)
  # mscorlib.cpp does not compile with precompiled header file
  set_source_files_properties(../mscorlib.cpp PROPERTIES COMPILE_FLAGS "/Y-")
  list(APPEND VM_CROSSGEN_SOURCES ${VM_CROSSGEN_HEADERS})
endif (WIN32)

if (CLR_CMAKE_PLATFORM_LINUX)
  list(APPEND VM_CROSSGEN_SOURCES
    ../perfmap.cpp
    ../perfinfo.cpp
  )
endif (CLR_CMAKE_PLATFORM_LINUX)

add_library_clr(cee_crossgen ${VM_CROSSGEN_SOURCES})
add_dependencies(cee_crossgen eventing_headers)
