include_directories( ${INC_PLATFORM_DIR} )
set(SOURCES native.cpp)

add_library(nativelibrary ${TEST_LIB_TYPE} ${SOURCES})

install(TARGETS nativelibrary DESTINATION bin)
