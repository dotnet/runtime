
set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(UTILCODE_COMMON_SOURCES
  clrhost_nodependencies.cpp
  ccomprc.cpp
  ex.cpp
  sbuffer.cpp
  sstring_com.cpp
  fstring.cpp
  namespaceutil.cpp
  makepath.cpp
  splitpath.cpp
  clrconfig.cpp
  configuration.cpp
  collections.cpp
  posterror.cpp
  fstream.cpp
  clrhelpers.cpp
  stgpool.cpp
  stgpooli.cpp
  stgpoolreadonly.cpp
  utsem.cpp
  peinformation.cpp
  check.cpp
  log.cpp
  arraylist.cpp
  bitvector.cpp
  comex.cpp
  delayloadhelpers.cpp
  guidfromname.cpp
  jitperf.cpp
  memorypool.cpp
  iallocator.cpp
  loaderheap.cpp
  outstring.cpp
  ilformatter.cpp
  opinfo.cpp
  corimage.cpp
  format1.cpp
  prettyprintsig.cpp
  regutil.cpp
  sigbuilder.cpp
  sigparser.cpp
  sstring.cpp
  util_nodependencies.cpp
  utilmessagebox.cpp
  safewrap.cpp
  clrhost.cpp
  cycletimer.cpp
  md5.cpp
  util.cpp
  stresslog.cpp
  debug.cpp
  pedecoder.cpp
  winfix.cpp
  longfilepathwrappers.cpp
  yieldprocessornormalized.cpp
)

# These source file do not yet compile on Linux.
# They should be moved out from here into the declaration
# of UTILCODE_SOURCES above after fixing compiler errors.
if(WIN32)
  list(APPEND UTILCODE_COMMON_SOURCES 
    appxutil.cpp
    dacutil.cpp
    dlwrap.cpp
    loadrc.cpp
    securitywrapper.cpp
    securityutil.cpp
    stacktrace.cpp
  )

  if(CLR_CMAKE_PLATFORM_ARCH_I386 OR CLR_CMAKE_PLATFORM_ARCH_ARM)
    list(APPEND UTILCODE_COMMON_SOURCES
      lazycow.cpp
    )
  endif()
  
endif(WIN32)
  
set(UTILCODE_SOURCES 
  ${UTILCODE_COMMON_SOURCES}
  perflog.cpp
)

set(UTILCODE_DAC_SOURCES 
  ${UTILCODE_COMMON_SOURCES}
  hostimpl.cpp
)

set(UTILCODE_CROSSGEN_SOURCES 
  ${UTILCODE_COMMON_SOURCES}
  hostimpl.cpp
  perflog.cpp
)

set(UTILCODE_STATICNOHOST_SOURCES 
  ${UTILCODE_COMMON_SOURCES}
  hostimpl.cpp
)

set (UTILCODE_DEPENDENCIES eventing_headers)

convert_to_absolute_path(UTILCODE_SOURCES ${UTILCODE_SOURCES})
convert_to_absolute_path(UTILCODE_DAC_SOURCES ${UTILCODE_DAC_SOURCES})
convert_to_absolute_path(UTILCODE_CROSSGEN_SOURCES ${UTILCODE_CROSSGEN_SOURCES})
convert_to_absolute_path(UTILCODE_STATICNOHOST_SOURCES ${UTILCODE_STATICNOHOST_SOURCES})

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_subdirectory(dac)
add_subdirectory(dyncrt)
add_subdirectory(staticnohost)
add_subdirectory(crossgen)
