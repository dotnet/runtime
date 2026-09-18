project (ThreadStateDestroyedNative)

include_directories(${INC_PLATFORM_DIR})

set(SOURCES ThreadStateDestroyedNative.cpp)

if(NOT CLR_CMAKE_HOST_WIN32)
    add_compile_options(-pthread)
endif()

add_library (ThreadStateDestroyedNative ${TEST_LIB_TYPE} ${SOURCES})

install (TARGETS ThreadStateDestroyedNative DESTINATION bin)
