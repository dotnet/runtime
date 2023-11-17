project(SwiftSelfContext)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")

set(SOURCE SwiftSelfContext)

add_custom_target(${SOURCE} ALL
    COMMAND xcrun swiftc -emit-library ${CMAKE_CURRENT_SOURCE_DIR}/${SOURCE}.swift -o ${CMAKE_CURRENT_BINARY_DIR}/lib${SOURCE}.dylib
    DEPENDS ${CMAKE_CURRENT_SOURCE_DIR}/${SOURCE}.swift
    COMMENT "Generating ${SOURCE} library"
)

install(FILES ${CMAKE_CURRENT_BINARY_DIR}/lib${SOURCE}.dylib
    DESTINATION bin
)
