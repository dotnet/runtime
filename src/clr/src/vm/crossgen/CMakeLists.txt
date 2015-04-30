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
    ../crossdomaincalls.cpp
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
    ../typeequivalencehash.cpp
    ../gcdecode.cpp
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
    ../readytoruninfo.cpp
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
    ../zapsig.cpp
    ../dbggcinfodecoder.cpp
    ../gcinfodecoder.cpp
    ../sha1.cpp
    ../amd64/stublinkeramd64.cpp
    ../crossgencompile.cpp
)

include_directories(BEFORE ..)
include_directories(${CLR_DIR}/src/gc)
include_directories(../amd64)

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

add_library(cee_crossgen ${VM_CROSSGEN_SOURCES})
