cmake_minimum_required (VERSION 2.6)
project (IjwNativeDll)
include_directories( ${INC_PLATFORM_DIR} )
set(SOURCES IjwNativeDll.cpp)

if (WIN32)
  # 4365 - signed/unsigned mismatch
  add_compile_options(/wd4365)

  # IJW
  add_compile_options(/clr)
  
  # IJW requires the CRT as a dll, not linked in
  add_compile_options(/MD$<$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>:d>)

  # CMake enables /RTC1 and /EHsc by default, but they're not compatible with /clr, so remove them
  if(CMAKE_CXX_FLAGS_DEBUG MATCHES "/RTC1")
    string(REPLACE "/RTC1" " " CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG}")
  endif()
  
  if(CMAKE_CXX_FLAGS MATCHES "/EHsc")
    string(REPLACE "/EHsc" "" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")
  endif()

  # IJW isn't compatible with CFG
   if(CMAKE_CXX_FLAGS MATCHES "/guard:cf")
    string(REPLACE "/guard:cf" "" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")
   endif()

     # IJW isn't compatible with GR-
   if(CMAKE_CXX_FLAGS MATCHES "/GR-")
    string(REPLACE "/GR-" "" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")
   endif()

endif()

# add the shared library
add_library (IjwNativeDll SHARED ${SOURCES})
target_link_libraries(IjwNativeDll ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS IjwNativeDll DESTINATION bin)
