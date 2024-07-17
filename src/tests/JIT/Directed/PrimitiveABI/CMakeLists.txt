project (PrimitiveABI)
include_directories(${INC_PLATFORM_DIR})

if(CLR_CMAKE_HOST_WIN32)
    set_source_files_properties(PrimitiveABI.c PROPERTIES COMPILE_OPTIONS /TC) # compile as C
else()
    set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -fvisibility=hidden -Oz")
endif()

add_library (PrimitiveABI SHARED PrimitiveABI.c)

install (TARGETS PrimitiveABI DESTINATION bin)
