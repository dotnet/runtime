
set(CMAKE_INCLUDE_CURRENT_DIR ON)

add_definitions(-DNO_CRT)

if(WIN32)
    add_compile_options(/wd4996)
endif(WIN32)

set(UTILCODE_SOURCES
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
  registrywrapper.cpp
  collections.cpp
  genericstackprobe.cpp
  posterror.cpp
  fstream.cpp
  clrhelpers.cpp
  stgpool.cpp
  stgpooli.cpp
  stgpoolreadonly.cpp
  lazycow.cpp
  utsem.cpp
  peinformation.cpp
  check.cpp
  log.cpp
  apithreadstress.cpp
  arraylist.cpp
  bitvector.cpp
  comex.cpp
  delayloadhelpers.cpp
  guidfromname.cpp
  jitperf.cpp
  memorypool.cpp
  rangetree.cpp
  iallocator.cpp
  loaderheap.cpp
  perflog.cpp
  outstring.cpp
  ilformatter.cpp
  opinfo.cpp
  dacutil.cpp
  sortversioning.cpp
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
)

# These source file do not yet compile on Linux.
# They should be moved out from here into the declaration
# of UTILCODE_SOURCES above after fixing compiler errors.
if(WIN32)
  list(APPEND UTILCODE_SOURCES 
    appxutil.cpp
    dlwrap.cpp
    downlevel.cpp
    loadrc.cpp
    newapis.cpp
    tls.cpp
    securitywrapper.cpp
    securityutil.cpp
    stacktrace.cpp
  )
endif(WIN32)

convert_to_absolute_path(UTILCODE_SOURCES ${UTILCODE_SOURCES})

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_subdirectory(dac)
add_subdirectory(dyncrt)
add_subdirectory(staticnohost)

