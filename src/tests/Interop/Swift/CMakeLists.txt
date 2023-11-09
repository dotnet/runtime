project(SwiftInteropLibraries)
# include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")

if(NOT CMAKE_Swift_COMPILER)
  set(CMAKE_Swift_COMPILER swiftc)
endif()

enable_language(Swift)

set(SOURCE_FILES_DUMMY Dummy.swift)
set(SOURCE_FILES_ERROR_HANDLING ErrorHandling.swift)
set(SOURCE_FILES_SELF_CONTEXT SelfContext.swift)

add_library(Dummy SHARED ${SOURCE_FILES_DUMMY})
add_library(ErrorHandling SHARED ${SOURCE_FILES_ERROR_HANDLING})
add_library(SelfContext SHARED ${SOURCE_FILES_SELF_CONTEXT})

# Set the output name for the libraries
set_target_properties(Dummy PROPERTIES OUTPUT_NAME "Dummy")
set_target_properties(ErrorHandling PROPERTIES OUTPUT_NAME "ErrorHandling")
set_target_properties(SelfContext PROPERTIES OUTPUT_NAME "SelfContext")

install(TARGETS Dummy ErrorHandling SelfContext
  LIBRARY DESTINATION bin
)