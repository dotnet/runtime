project (Test11242Lib)
include_directories(${INC_PLATFORM_DIR})

if(CLR_CMAKE_HOST_WIN32)
    set_source_files_properties(Test11242.c PROPERTIES COMPILE_OPTIONS /TC) # compile as C
else()
    set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -fvisibility=hidden")
    set(CMAKE_CPP_FLAGS "${CMAKE_CPP_FLAGS} -fvisibility=hidden -Wno-return-type-c-linkage")
endif()

# add the executable
add_library (Test11242Lib SHARED Test11242.c)

# add the install targets
install (TARGETS Test11242Lib DESTINATION bin)
