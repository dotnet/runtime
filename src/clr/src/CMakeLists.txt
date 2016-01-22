include_directories("inc")
include_directories("strongname/inc")
include_directories("inc/winrt")
include_directories("debug/inc")
include_directories("debug/inc/${ARCH_SOURCES_DIR}")
include_directories("debug/inc/dump")
include_directories("md/inc")
include_directories("classlibnative/bcltype")
include_directories("classlibnative/cryptography")
include_directories("classlibnative/inc")
include_directories("${GENERATED_INCLUDE_DIR}")

# The Following Logic is used to wire up Build dependencies for Generated files in Event Logging 
# ClrEtwAll.man                  - Event Schema
# ClrEtwAllMeta.lst              - MetaData list [provided to ensure Windows Desktop is not broken]
# genXplatEventing.py            - has the core logic for parsing Event Schema
# genWinEtw.py                   - Uses genXplatEventing to generate Windows Specific ETW Files
# clretwallmain.h and etmdummy.h - Provides the Event Logging Functionality to the VM
# clrxplatevents.h               - Used by clretwallmain.h on Non Windows platform
# ClrEtwAll.h                    - Used by clretwallmain.h on  Windows 
# ClrEtwAll.rc                   - Used by src/dlls/clretwrc/clretrw.rc on Windows

set (ScriptGeneratedEventFiles
      ${GENERATED_INCLUDE_DIR}/clretwallmain.h
      ${GENERATED_INCLUDE_DIR}/etmdummy.h
)
set (GeneratedEventFiles)

if(WIN32)
    set (GenEventFilesScript "${CLR_DIR}/src/scripts/genWinEtw.py")
    set (GenEventArgs --eventheader "${GENERATED_INCLUDE_DIR}/ClrEtwAll.h" --macroheader "${GENERATED_INCLUDE_DIR}/clretwallmain.h")

    list (APPEND ScriptGeneratedEventFiles
          ${GENERATED_INCLUDE_DIR}/ClrEtwAll.h
    )

    list (APPEND GeneratedEventFiles
         ${GENERATED_INCLUDE_DIR}/ClrEtwAll.rc
    )

    add_custom_command(
      COMMENT "Generating ETW resource Files"
      COMMAND ${MC} -h ${GENERATED_INCLUDE_DIR} -r ${GENERATED_INCLUDE_DIR} -b -co -um -p FireEtw "${VM_DIR}/ClrEtwAll.man"
      OUTPUT ${GENERATED_INCLUDE_DIR}/ClrEtwAll.h
      DEPENDS "${VM_DIR}/ClrEtwAll.man"
    )
else()
    set (GenEventFilesScript "${CLR_DIR}/src/scripts/genXplatEventing.py")
    set (GenEventArgs   --inc  "${GENERATED_INCLUDE_DIR}")

    list (APPEND ScriptGeneratedEventFiles
          ${GENERATED_INCLUDE_DIR}/clrxplatevents.h
    )
endif(WIN32)

add_custom_command(
  COMMENT "Generating Eventing Files"
  COMMAND ${PYTHON} -B -Wall -Werror ${GenEventFilesScript} ${GenEventArgs} --man "${VM_DIR}/ClrEtwAll.man" --exc "${VM_DIR}/ClrEtwAllMeta.lst" --dummy "${GENERATED_INCLUDE_DIR}/etmdummy.h"
  OUTPUT ${ScriptGeneratedEventFiles}
  DEPENDS ${GenEventFilesScript} "${VM_DIR}/ClrEtwAll.man" "${VM_DIR}/ClrEtwAllMeta.lst" "${CLR_DIR}/src/scripts/genXplatEventing.py"
)

list (APPEND GeneratedEventFiles
      ${ScriptGeneratedEventFiles}
)

add_custom_target(
  GeneratedEventingFiles
  DEPENDS ${GeneratedEventFiles}
)

function(add_library_clr)
    add_library(${ARGV})
    add_dependencies(${ARGV0} GeneratedEventingFiles)
endfunction()

function(add_executable_clr)
    add_executable(${ARGV})
    add_dependencies(${ARGV0} GeneratedEventingFiles)
endfunction()

if(CLR_CMAKE_PLATFORM_UNIX)
  add_subdirectory(ToolBox/SOS/Strike)

  # Include the dummy c++ include files
  include_directories("pal/inc/rt/cpp")

  # This prevents inclusion of standard C compiler headers
  add_compile_options(-nostdinc)

  if(NOT CLR_CMAKE_PLATFORM_DARWIN)
    # This prevents inclusion of standard C++ compiler headers
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -nostdinc++")
  endif(NOT CLR_CMAKE_PLATFORM_DARWIN)

  set (NATIVE_RESOURCE_DIR ${CMAKE_CURRENT_SOURCE_DIR}/nativeresources)
  include_directories(${NATIVE_RESOURCE_DIR})
  set (RC_TO_CPP ${NATIVE_RESOURCE_DIR}/rctocpp.awk)
  set (PROCESS_RC ${NATIVE_RESOURCE_DIR}/processrc.awk)
  set (RESOURCE_STRING_HEADER_DIR ${NATIVE_RESOURCE_DIR})

  # Create a command to create a C++ source file containing an array of
  # NativeStringResource structs which represent the information from a
  # given Windows .rc file. The target C++ file path is returned in the
  # variable specified by the TARGET_FILE parameter.
  function(build_resources SOURCE TARGET_NAME TARGET_FILE)

    get_compile_definitions(PREPROCESS_DEFINITIONS)
    get_include_directories(INCLUDE_DIRECTORIES)

    set(PREPROCESSED_SOURCE ${CMAKE_CURRENT_BINARY_DIR}/${TARGET_NAME}.rc.i)
    set(RESOURCE_ENTRY_ARRAY_CPP ${CMAKE_CURRENT_BINARY_DIR}/${TARGET_NAME}.cpp)

    add_custom_command(
      OUTPUT ${RESOURCE_ENTRY_ARRAY_CPP}
      # Preprocess the windows .rc file
      COMMAND ${CMAKE_CXX_COMPILER} -E -P ${PREPROCESS_DEFINITIONS} ${INCLUDE_DIRECTORIES} -o ${PREPROCESSED_SOURCE} -x c ${SOURCE}
      # Convert the preprocessed .rc file to a C++ file which will be used to make a static lib.
      COMMAND ${AWK} -v name=${TARGET_NAME} -f ${RC_TO_CPP} -f ${PROCESS_RC} ${PREPROCESSED_SOURCE} >${RESOURCE_ENTRY_ARRAY_CPP}
            DEPENDS ${SOURCE} ${RC_TO_CPP} ${PROCESS_RC}
    )

    include_directories(${RESOURCE_STRING_HEADER_DIR})
    set(${TARGET_FILE} ${RESOURCE_ENTRY_ARRAY_CPP} PARENT_SCOPE)

  endfunction()

  add_subdirectory(nativeresources)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_subdirectory(utilcode)
add_subdirectory(gcinfo)
add_subdirectory(coreclr)
add_subdirectory(jit)
add_subdirectory(vm)
add_subdirectory(md)
add_subdirectory(debug)
add_subdirectory(inc)
add_subdirectory(strongname)
add_subdirectory(binder)
add_subdirectory(classlibnative)
add_subdirectory(dlls)
add_subdirectory(ToolBox)
add_subdirectory(tools)
add_subdirectory(unwinder)
add_subdirectory(ildasm)
add_subdirectory(ilasm)

if(WIN32)
  add_subdirectory(ipcman)
endif(WIN32)

if(CLR_CMAKE_PLATFORM_UNIX)
    add_subdirectory(palrt)
endif(CLR_CMAKE_PLATFORM_UNIX)
