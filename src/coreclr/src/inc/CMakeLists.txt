set( CORGUIDS_IDL_SOURCES
  cordebug.idl
  xcordebug.idl
  clrdata.idl
  clrinternal.idl
  xclrdata.idl
  corprof.idl
  corpub.idl
  mscorsvc.idl
  clrprivappxhosting.idl
  clrprivbinding.idl
  clrprivhosting.idl
  clrprivruntimebinders.idl
  corsym.idl
  sospriv.idl
)

if(WIN32)
#Build for corguids is done in two steps:
#1. compile .idl to *_i.c : This is done using custom midl command
#2. compile *_i.c to .lib

# Get the current list of definitions to pass to midl
get_compile_definitions(MIDL_DEFINITIONS)
get_include_directories(MIDL_INCLUDE_DIRECTORIES)


# Run custom midl command over each idl file
FIND_PROGRAM( MIDL midl.exe )
foreach(GENERATE_IDL IN LISTS CORGUIDS_IDL_SOURCES)
   get_filename_component(IDLNAME ${GENERATE_IDL} NAME_WE)
   set(OUT_NAME ${CMAKE_CURRENT_BINARY_DIR}/idls_out/${IDLNAME}_i.c)
   list(APPEND CORGUIDS_SOURCES ${OUT_NAME})
   add_custom_command(OUTPUT ${OUT_NAME}
                      COMMAND ${MIDL} ${MIDL_INCLUDE_DIRECTORIES} /h ${CMAKE_CURRENT_BINARY_DIR}/idls_out/${IDLNAME}.h ${MIDL_DEFINITIONS} /out ${CMAKE_CURRENT_BINARY_DIR}/idls_out ${CMAKE_CURRENT_SOURCE_DIR}/${GENERATE_IDL}
                      DEPENDS ${CMAKE_CURRENT_SOURCE_DIR}/${GENERATE_IDL} 
                      COMMENT "Compiling ${GENERATE_IDL}")
endforeach(GENERATE_IDL)

set_source_files_properties(${CORGUIDS_SOURCES}
                            PROPERTIES GENERATED TRUE)

# Compile *_i.c as C files
add_compile_options(/TC)

else()

#The MIDL tool exists for Windows only, so for other systems, we have the prebuilt xxx_i.cpp files checked in

# The prebuilt files contain extra '!_MIDL_USE_GUIDDEF_' after the #endif, but not in the comment.
# In order to not to have to modify these prebuilt files, we disable the extra tokens warning.
if (CMAKE_CXX_COMPILER_ID MATCHES "Clang")
add_compile_options(-Wno-extra-tokens)
elseif (CMAKE_CXX_COMPILER_ID STREQUAL "GNU")
add_compile_options(-Wno-endif-labels)
endif()

add_compile_options(-D_MIDL_USE_GUIDDEF_)
foreach(IDL_SOURCE IN LISTS CORGUIDS_IDL_SOURCES)
   get_filename_component(IDLNAME ${IDL_SOURCE} NAME_WE)
   set(C_SOURCE ../pal/prebuilt/idl/${IDLNAME}_i.cpp)
   list(APPEND CORGUIDS_SOURCES ${C_SOURCE})
endforeach(IDL_SOURCE)

add_compile_options(-fPIC)
endif(WIN32)

if(FEATURE_JIT_PITCHING)
   add_definitions(-DFEATURE_JIT_PITCHING)
endif(FEATURE_JIT_PITCHING)

# Compile *_i.cpp to lib
_add_library(corguids ${CORGUIDS_SOURCES})

# Binplace the inc files for packaging later.

_install (FILES cfi.h
               cor.h
               cordebuginfo.h
               coredistools.h
               corhdr.h
               corinfo.h
               corjit.h
               corjithost.h
               opcode.def
               openum.h
               gcinfoencoder.h
               gcinfotypes.h
               DESTINATION inc)
_install (TARGETS corguids DESTINATION lib)
