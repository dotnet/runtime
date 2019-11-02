set(CMAKE_INCLUDE_CURRENT_DIR ON)

#this will again add vm to include directories. However, this will add vm to be the first include folder.
#This is required because util.hpp is also present in ildasm folder and so due to ordering wrong util.hpp
#is pickedup.
include_directories(BEFORE ${VM_DIR})

include_directories(${VM_DIR}/${ARCH_SOURCES_DIR})
include_directories(${CLR_DIR}/src/md/compiler)

add_definitions(-DSNAPI_INTERNAL)

set(STRONGNAME_SOURCES
  strongname.cpp
  strongnameinternal.cpp
)

convert_to_absolute_path(STRONGNAME_SOURCES ${STRONGNAME_SOURCES})

add_library_clr(strongname_tool ${STRONGNAME_SOURCES})

add_library_clr(strongname_dac ${STRONGNAME_SOURCES})
add_dependencies(strongname_dac eventing_headers)
set_target_properties(strongname_dac PROPERTIES DAC_COMPONENT TRUE)
target_compile_definitions(strongname_dac PRIVATE STRONGNAME_IN_VM)

add_library_clr(strongname_wks ${STRONGNAME_SOURCES})
add_dependencies(strongname_wks eventing_headers)
target_compile_definitions(strongname_wks PRIVATE STRONGNAME_IN_VM)

add_library_clr(strongname_crossgen ${STRONGNAME_SOURCES})
add_dependencies(strongname_crossgen eventing_headers)
set_target_properties(strongname_crossgen PROPERTIES CROSSGEN_COMPONENT TRUE)
target_compile_definitions(strongname_crossgen PRIVATE STRONGNAME_IN_VM)
