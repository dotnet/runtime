if (CLR_CMAKE_HOST_WIN32)
  # 4365 - signed/unsigned mismatch
  add_compile_options(/wd4365)

  # IJW
  add_compile_options(/clr)

  # IJW requires the CRT as a dll, not linked in
  set(CMAKE_MSVC_RUNTIME_LIBRARY MultiThreaded$<$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>:Debug>DLL)

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

  # IJW isn't compatible with EHCONT, which requires CFG
  if(CMAKE_CXX_FLAGS MATCHES "/guard:ehcont")
    string(REPLACE "/guard:ehcont" "" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")
  endif()

  # IJW isn't compatible with GR-
  if(CMAKE_CXX_FLAGS MATCHES "/GR-")
    string(REPLACE "/GR-" "" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")
  endif()

endif()
