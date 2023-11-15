project(SwiftInteropLibraries)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")

set(SWIFT_SOURCES
  Dummy
  ErrorHandling
  SelfContext
)

foreach(SOURCE ${SWIFT_SOURCES})
    add_custom_target(${SOURCE}SwiftLibrary ALL
        COMMAND xcrun swiftc -emit-library ${CMAKE_CURRENT_SOURCE_DIR}/${SOURCE}.swift -o ${CMAKE_CURRENT_BINARY_DIR}/lib${SOURCE}.dylib
        DEPENDS ${CMAKE_CURRENT_SOURCE_DIR}/${SOURCE}.swift
        COMMENT "Generating ${SOURCE} library"
    )
    install(FILES ${CMAKE_CURRENT_BINARY_DIR}/lib${SOURCE}.dylib
        DESTINATION bin
    )
endforeach()
