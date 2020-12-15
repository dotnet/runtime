if((CMAKE_SYSTEM_NAME STREQUAL "Darwin") OR (CMAKE_SYSTEM_NAME STREQUAL "iOS"))
  # Quiet 'file ... has no symbols' messages from ranlib
  find_program(CMAKE_XCRUN NAMES xcrun)
  execute_process(COMMAND ${CMAKE_XCRUN} -find libtool
    OUTPUT_VARIABLE CMAKE_LIBTOOL
    OUTPUT_STRIP_TRAILING_WHITESPACE)
  get_property(languages GLOBAL PROPERTY ENABLED_LANGUAGES)
  # -D makes ranlib deterministic
  set(RANLIB_FLAGS "-D -no_warning_for_no_symbols")
  foreach(lang ${languages})
    set(CMAKE_${lang}_CREATE_STATIC_LIBRARY
      "\"${CMAKE_LIBTOOL}\" -static ${RANLIB_FLAGS} -o <TARGET> <LINK_FLAGS> <OBJECTS>")
  endforeach()
  # Another instance
  set(MONO_RANLIB "${PROJECT_BINARY_DIR}/mono-ranlib")
  file(WRITE ${MONO_RANLIB} "#!/bin/sh\n")
  file(APPEND ${MONO_RANLIB} "${CMAKE_RANLIB} ${RANLIB_FLAGS} $*")
  execute_process(COMMAND chmod a+x ${MONO_RANLIB})
  set(CMAKE_RANLIB "${MONO_RANLIB}")
endif()
