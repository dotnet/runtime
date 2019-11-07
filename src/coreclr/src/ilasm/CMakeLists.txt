project(ilasm)

add_definitions(-DUNICODE)
add_definitions(-D_UNICODE)
add_definitions(-D_FEATURE_NO_HOST)
add_definitions(-D__ILASM__)

add_definitions(-DFEATURE_CORECLR)

include_directories(.)

set(ILASM_SOURCES
  assem.cpp
  writer.cpp
  writer_enc.cpp
  method.cpp
  asmman.cpp
  main.cpp
  assembler.cpp
  prebuilt/asmparse.cpp
)

set(ILASM_HEADERS
  asmenum.h
  asmman.hpp
  asmparse.h
  asmtemplates.h
  assembler.h
  binstr.h
  class.hpp
  ilasmpch.h
  method.hpp
  nvpair.h
  typar.hpp
)

if(WIN32)
  list(APPEND ILASM_SOURCES ${ILASM_HEADERS})

  set(ILASM_RESOURCES Native.rc)
  add_definitions(-DFX_VER_INTERNALNAME_STR=ilasm.exe)
endif(WIN32)


if(CLR_CMAKE_PLATFORM_UNIX)
  # Need generate a right form of asmparse.cpp to avoid the following options.
  # Clang also produces a bad-codegen on this prebuilt file with optimization.
  # https://github.com/dotnet/coreclr/issues/2305
  add_compile_options("$<$<COMPILE_LANGUAGE:CXX>:-Wno-register>")
  add_compile_options(-Wno-array-bounds)
  add_compile_options(-Wno-unused-label)
  set_source_files_properties( prebuilt/asmparse.cpp PROPERTIES COMPILE_FLAGS "-O0" )
endif(CLR_CMAKE_PLATFORM_UNIX)

if(CLR_CMAKE_PLATFORM_LINUX OR CLR_CMAKE_PLATFORM_FREEBSD OR CLR_CMAKE_PLATFORM_NETBSD)
    # This option is necessary to ensure that the overloaded delete operator defined inside
    # of the utilcode will be used instead of the standard library delete operator.
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Xlinker -Bsymbolic -Xlinker -Bsymbolic-functions")

    # The following linked options can be inserted into the linker libraries list to
    # ensure proper resolving of circular references between a subset of the libraries.
    set(START_LIBRARY_GROUP -Wl,--start-group)
    set(END_LIBRARY_GROUP -Wl,--end-group)
endif(CLR_CMAKE_PLATFORM_LINUX OR CLR_CMAKE_PLATFORM_FREEBSD OR CLR_CMAKE_PLATFORM_NETBSD)


_add_executable(ilasm
  ${ILASM_SOURCES}
  ${ILASM_RESOURCES}
)

set(ILASM_LINK_LIBRARIES
  utilcodestaticnohost
  mscorpe
  ${START_LIBRARY_GROUP} # Start group of libraries that have circular references
  mdhotdata_full
  mdcompiler_wks
  mdruntime_wks
  mdruntimerw_wks
  strongname_tool
  mdstaticapi
  ${END_LIBRARY_GROUP} # End group of libraries that have circular references
  ceefgen
  corguids
)

if(WIN32)
    list(APPEND ILASM_LINK_LIBRARIES
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_VCRT_LIB}
        mdwinmd_wks
        kernel32.lib
        advapi32.lib
        ole32.lib
        oleaut32.lib
        uuid.lib
        user32.lib
        version.lib
        shlwapi.lib
        bcrypt.lib
        RuntimeObject.lib
    )
else()
    list(APPEND ILASM_LINK_LIBRARIES
        coreclrpal
        mscorrc_debug
        palrt
    )
endif(WIN32)


if(CLR_CMAKE_PLATFORM_UNIX)
  target_link_libraries(ilasm
    ${ILASM_LINK_LIBRARIES}
    utilcodestaticnohost
    mscorrc_debug
    coreclrpal
    palrt
  )

  # FreeBSD and NetBSD implement dlopen(3) in libc
  if(NOT CMAKE_SYSTEM_NAME STREQUAL FreeBSD AND NOT CMAKE_SYSTEM_NAME STREQUAL NetBSD)
    target_link_libraries(ilasm
      dl
    )
  endif(NOT CMAKE_SYSTEM_NAME STREQUAL FreeBSD AND NOT CMAKE_SYSTEM_NAME STREQUAL NetBSD)
else()
  target_link_libraries(ilasm
    ${ILASM_LINK_LIBRARIES}
    ole32
    oleaut32
    shell32
  )
endif(CLR_CMAKE_PLATFORM_UNIX)

install_clr(ilasm)
