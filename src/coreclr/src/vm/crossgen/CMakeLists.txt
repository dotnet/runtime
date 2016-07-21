include(${CLR_DIR}/crossgen.cmake)

set(VM_CROSSGEN_SOURCES
    ../class.cpp
    ../appdomain.cpp
    ../array.cpp
    ../assembly.cpp
    ../assemblyspec.cpp
    ../binder.cpp
    ../ceeload.cpp
    ../ceemain.cpp
    ../classhash.cpp
    ../clrex.cpp
    ../clrprivbinderutil.cpp
    ../clsload.cpp
    ../comdelegate.cpp
    ../codeman.cpp
    ../compile.cpp
    ../constrainedexecutionregion.cpp
    ../custommarshalerinfo.cpp
    ../domainfile.cpp
    ../baseassemblyspec.cpp
    ../corebindresult.cpp
    ../coreassemblyspec.cpp
    ../dataimage.cpp
    ../decodemd.cpp
    ../debuginfostore.cpp
    ../ecall.cpp
    ../eeconfig.cpp
    ../eehash.cpp
    ../eetwain.cpp
    ../excep.cpp
    ../field.cpp
    ../fieldmarshaler.cpp
    ../formattype.cpp
    ../genericdict.cpp
    ../generics.cpp
    ../genmeth.cpp
    ../hash.cpp
    ../ilmarshalers.cpp
    ../ilstubcache.cpp
    ../ilstubresolver.cpp
    ../instmethhash.cpp
    ../interoputil.cpp
    ../invokeutil.cpp
    ../inlinetracking.cpp
    ../contractimpl.cpp
    ../jitinterface.cpp
    ../loaderallocator.cpp
    ../listlock.cpp
    ../memberload.cpp
    ../method.cpp
    ../methodimpl.cpp
    ../methodtable.cpp
    ../methodtablebuilder.cpp
    ../mscorlib.cpp
    ../stubcache.cpp
    ../mlinfo.cpp
    ../dllimport.cpp
    ../dllimportcallback.cpp
    ../pefile.cpp
    ../pefingerprint.cpp
    ../peimage.cpp
    ../peimagelayout.cpp
    ../pendingload.cpp
    ../precode.cpp
    ../olevariant.cpp
    ../security.cpp
    ../securitypolicy.cpp
    ../securityattributes.cpp
    ../securitydeclarative.cpp
    ../securitydeclarativecache.cpp
    ../securitydescriptor.cpp
    ../securitydescriptorappdomain.cpp
    ../securitydescriptorassembly.cpp
    ../securitymeta.cpp
    ../securitytransparentassembly.cpp
    ../siginfo.cpp
    ../sigformat.cpp
    ../simplerwlock.cpp
    ../spinlock.cpp
    ../stackingallocator.cpp
    ../stubgen.cpp
    ../stublink.cpp
    ../typectxt.cpp
    ../typedesc.cpp
    ../typehandle.cpp
    ../typehash.cpp
    ../typeparse.cpp
    ../typestring.cpp
    ../util.cpp
    ../vars.cpp
    ../versionresilienthashcode.cpp
    ../zapsig.cpp
    ../gcinfodecoder.cpp
    ../sha1.cpp
    ../crossgencompile.cpp
)

if(FEATURE_READYTORUN)
    list(APPEND VM_CROSSGEN_SOURCES
      ../readytoruninfo.cpp
    )
endif(FEATURE_READYTORUN)

include_directories(BEFORE ..)
include_directories(${CLR_DIR}/src/gc)
include_directories(../${ARCH_SOURCES_DIR})

if(CLR_CMAKE_TARGET_ARCH_AMD64)
  list(APPEND VM_CROSSGEN_SOURCES 
    ../${ARCH_SOURCES_DIR}/stublinkeramd64.cpp
  )
elseif(CLR_CMAKE_TARGET_ARCH_I386)
  list(APPEND VM_CROSSGEN_SOURCES 
    ../${ARCH_SOURCES_DIR}/stublinkerx86.cpp
    ../gcdecode.cpp
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

  add_precompiled_header(common.h ../common.cpp VM_CROSSGEN_SOURCES)
  # mscorlib.cpp does not compile with precompiled header file
  set_source_files_properties(../mscorlib.cpp PROPERTIES COMPILE_FLAGS "/Y-")
endif (WIN32)

if (CLR_CMAKE_PLATFORM_LINUX)
  list(APPEND VM_CROSSGEN_SOURCES
    ../perfmap.cpp
    ../perfinfo.cpp
  )
endif (CLR_CMAKE_PLATFORM_LINUX)

add_library_clr(cee_crossgen ${VM_CROSSGEN_SOURCES})
