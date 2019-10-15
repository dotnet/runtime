include_directories( ${INC_PLATFORM_DIR} )
set(SOURCES native.cpp)

add_library(nativelibrary SHARED ${SOURCES})

install(TARGETS nativelibrary DESTINATION bin)
