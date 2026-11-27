project (Test111242Lib)
include_directories(${INC_PLATFORM_DIR})

if(CLR_CMAKE_HOST_WIN32)
    set_source_files_properties(Test111242.c PROPERTIES COMPILE_OPTIONS /TC) # compile as C
else()
    set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -fvisibility=hidden")
    set(CMAKE_CPP_FLAGS "${CMAKE_CPP_FLAGS} -fvisibility=hidden -Wno-return-type-c-linkage")
endif()

# wasi-libc only provides setjmp/longjmp with the sjlj lowering, which the runtime is not built with.
if(CLR_CMAKE_TARGET_WASI)
    return()
endif()

# add the executable
add_library (Test111242Lib ${TEST_LIB_TYPE} Test111242.c)

# add the install targets
install (TARGETS Test111242Lib DESTINATION bin)
