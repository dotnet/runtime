
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
  sha1.cpp
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

add_library_clr(utilcode_dac STATIC ${UTILCODE_DAC_SOURCES})
add_library_clr(utilcode STATIC ${UTILCODE_SOURCES})
add_library_clr(utilcodestaticnohost STATIC ${UTILCODE_STATICNOHOST_SOURCES})
add_library_clr(utilcode_crossgen STATIC ${UTILCODE_CROSSGEN_SOURCES})

if(CLR_CMAKE_PLATFORM_UNIX)
  target_link_libraries(utilcodestaticnohost  nativeresourcestring)
  target_link_libraries(utilcode_crossgen  nativeresourcestring)
  target_link_libraries(utilcode_dac nativeresourcestring)
  target_link_libraries(utilcode nativeresourcestring)
  add_dependencies(utilcode_dac coreclrpal)
  add_dependencies(utilcode coreclrpal)
endif(CLR_CMAKE_PLATFORM_UNIX)


if(WIN32)
    target_compile_definitions(utilcodestaticnohost PRIVATE _CRTIMP=) # use static version of crt
endif(WIN32)

set_target_properties(utilcode_dac PROPERTIES DAC_COMPONENT TRUE)
set_target_properties(utilcode_crossgen PROPERTIES CROSSGEN_COMPONENT TRUE)
target_compile_definitions(utilcode_dac PRIVATE SELF_NO_HOST)
target_compile_definitions(utilcodestaticnohost PRIVATE SELF_NO_HOST)
add_dependencies(utilcode_dac ${UTILCODE_DEPENDENCIES})
add_dependencies(utilcode ${UTILCODE_DEPENDENCIES})
add_dependencies(utilcode_crossgen ${UTILCODE_DEPENDENCIES})
add_dependencies(utilcodestaticnohost ${UTILCODE_DEPENDENCIES})
target_precompile_header(TARGET utilcode_dac HEADER stdafx.h)
target_precompile_header(TARGET utilcode HEADER stdafx.h)
target_precompile_header(TARGET utilcode_crossgen HEADER stdafx.h)
target_precompile_header(TARGET utilcodestaticnohost HEADER stdafx.h)
