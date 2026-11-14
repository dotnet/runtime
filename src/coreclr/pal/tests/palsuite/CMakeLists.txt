project(PALTESTSUITE)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

include_directories(${PALTESTSUITE_SOURCE_DIR}/common)
include_directories(${RUNTIME_DIR})

# All test will link against these libraries:
# pthread and m are part of the Android C library (bionic),
# so we don't need to link them separately
if(NOT CLR_CMAKE_TARGET_ANDROID)
  list(APPEND COMMON_TEST_LIBRARIES pthread)
  list(APPEND COMMON_TEST_LIBRARIES m)
endif()

list(APPEND COMMON_TEST_LIBRARIES coreclrpal)
list(APPEND COMMON_TEST_LIBRARIES coreclrminipal)

if (CMAKE_CXX_COMPILER_ID MATCHES "Clang")
  add_compile_options(-Wno-incompatible-pointer-types-discards-qualifiers)
  add_compile_options(-Wno-int-to-void-pointer-cast)
elseif (CMAKE_CXX_COMPILER_ID STREQUAL "GNU")
  add_compile_options(-Wno-sign-compare)
  add_compile_options(-Wno-narrowing)
  add_compile_options(-fno-builtin)
  add_compile_options($<$<COMPILE_LANGUAGE:CXX>:-fpermissive>)
  add_compile_options(-Wno-int-to-pointer-cast)
endif()


#these PAL tests have special build needs, and are built separately
add_subdirectory(exception_handling/pal_sxs/test1)
if(FEATURE_EVENT_TRACE)
  add_subdirectory(eventprovider)
endif(FEATURE_EVENT_TRACE)

add_executable_clr(paltests
  EXCLUDE_FROM_ALL
  paltests.cpp
  common/palsuite.cpp
  c_runtime/sscanf_s/test1/test1.cpp
  c_runtime/sscanf_s/test10/test10.cpp
  c_runtime/sscanf_s/test11/test11.cpp
  c_runtime/sscanf_s/test12/test12.cpp
  c_runtime/sscanf_s/test13/test13.cpp
  c_runtime/sscanf_s/test14/test14.cpp
  c_runtime/sscanf_s/test15/test15.cpp
  c_runtime/sscanf_s/test16/test16.cpp
  c_runtime/sscanf_s/test17/test17.cpp
  c_runtime/sscanf_s/test2/test2.cpp
  c_runtime/sscanf_s/test3/test3.cpp
  c_runtime/sscanf_s/test4/test4.cpp
  c_runtime/sscanf_s/test5/test5.cpp
  c_runtime/sscanf_s/test6/test6.cpp
  c_runtime/sscanf_s/test7/test7.cpp
  c_runtime/sscanf_s/test8/test8.cpp
  c_runtime/sscanf_s/test9/test9.cpp
  c_runtime/_vsnprintf_s/test1/test1.cpp
  c_runtime/_vsnprintf_s/test10/test10.cpp
  c_runtime/_vsnprintf_s/test11/test11.cpp
  c_runtime/_vsnprintf_s/test12/test12.cpp
  c_runtime/_vsnprintf_s/test13/test13.cpp
  c_runtime/_vsnprintf_s/test14/test14.cpp
  c_runtime/_vsnprintf_s/test15/test15.cpp
  c_runtime/_vsnprintf_s/test16/test16.cpp
  c_runtime/_vsnprintf_s/test17/test17.cpp
  c_runtime/_vsnprintf_s/test18/test18.cpp
  c_runtime/_vsnprintf_s/test19/test19.cpp
  c_runtime/_vsnprintf_s/test2/test2.cpp
  c_runtime/_vsnprintf_s/test4/test4.cpp
  c_runtime/_vsnprintf_s/test6/test6.cpp
  c_runtime/_vsnprintf_s/test8/test8.cpp
  c_runtime/_vsnprintf_s/test9/test9.cpp
  c_runtime/_wcslwr_s/test1/test1.cpp
  #debug_api/DebugBreak/test1/test1.cpp
  debug_api/OutputDebugStringW/test1/test1.cpp
  #exception_handling/pal_except/test1/test1.cpp
  #exception_handling/pal_except/test2/test2.cpp
  #exception_handling/pal_except/test3/test3.cpp
  #exception_handling/pal_except/test4/test4.cpp
  #exception_handling/pal_except/test5/test5.cpp
  #exception_handling/pal_except/test6/test6.cpp
  #exception_handling/pal_except/test7/test7.cpp
  #exception_handling/PAL_EXCEPT_FILTER/test1/PAL_EXCEPT_FILTER.cpp
  #exception_handling/PAL_EXCEPT_FILTER/test2/pal_except_filter.cpp
  #exception_handling/PAL_EXCEPT_FILTER/test3/pal_except_filter.cpp
  #exception_handling/PAL_EXCEPT_FILTER_EX/test1/PAL_EXCEPT_FILTER_EX.cpp
  #exception_handling/PAL_EXCEPT_FILTER_EX/test2/pal_except_filter_ex.cpp
  #exception_handling/PAL_EXCEPT_FILTER_EX/test3/pal_except_filter.cpp
  #exception_handling/pal_finally/test1/pal_finally.cpp
  #exception_handling/PAL_TRY_EXCEPT/test1/PAL_TRY_EXCEPT.cpp
  #exception_handling/PAL_TRY_EXCEPT/test2/PAL_TRY_EXCEPT.cpp
  #exception_handling/PAL_TRY_EXCEPT_EX/test1/PAL_TRY_EXCEPT_EX.cpp
  #exception_handling/PAL_TRY_EXCEPT_EX/test2/PAL_TRY_EXCEPT_EX.cpp
  #exception_handling/PAL_TRY_EXCEPT_EX/test3/PAL_TRY_EXCEPT_EX.cpp
  #exception_handling/PAL_TRY_LEAVE_FINALLY/test1/PAL_TRY_LEAVE_FINALLY.cpp
  exception_handling/RaiseException/test1/test1.cpp
  exception_handling/RaiseException/test2/test2.cpp
  exception_handling/RaiseException/test3/test.cpp
#  filemapping_memmgt/FreeLibrary/test1/dlltest.cpp
  filemapping_memmgt/FreeLibrary/test1/FreeLibrary.cpp
  filemapping_memmgt/FreeLibrary/test2/test2.cpp
  filemapping_memmgt/GetModuleFileNameA/test1/GetModuleFileNameA.cpp
  filemapping_memmgt/GetModuleFileNameA/test2/GetModuleFileNameA.cpp
  filemapping_memmgt/GetModuleFileNameW/test1/GetModuleFileNameW.cpp
  filemapping_memmgt/GetModuleFileNameW/test2/GetModuleFileNameW.cpp
  filemapping_memmgt/GetProcAddress/test1/test1.cpp
#  filemapping_memmgt/GetProcAddress/test1/testlib.cpp
  filemapping_memmgt/GetProcAddress/test2/test2.cpp
#  filemapping_memmgt/GetProcAddress/test2/testlib.cpp
  filemapping_memmgt/ProbeMemory/ProbeMemory_neg1/ProbeMemory_neg.cpp
  filemapping_memmgt/ProbeMemory/test1/ProbeMemory.cpp
  loader/LoadLibraryW/test1/LoadLibraryW.cpp
  loader/LoadLibraryW/test2/loadlibraryw.cpp
  loader/LoadLibraryW/test3/loadlibraryw.cpp
  loader/LoadLibraryW/test5/loadlibraryw.cpp
  miscellaneous/CloseHandle/test2/test.cpp
  miscellaneous/FlushInstructionCache/test1/test1.cpp
  miscellaneous/FormatMessageW/test1/test.cpp
  miscellaneous/FormatMessageW/test2/test.cpp
  miscellaneous/FormatMessageW/test3/test.cpp
  miscellaneous/FormatMessageW/test4/test.cpp
  miscellaneous/FormatMessageW/test5/test.cpp
  miscellaneous/FormatMessageW/test6/test.cpp
  miscellaneous/FreeEnvironmentStringsW/test1/test.cpp
  miscellaneous/FreeEnvironmentStringsW/test2/test.cpp
  miscellaneous/GetEnvironmentStringsW/test1/test.cpp
  miscellaneous/GetEnvironmentVariableA/test1/test.cpp
  miscellaneous/GetEnvironmentVariableA/test2/test.cpp
  miscellaneous/GetEnvironmentVariableA/test3/test.cpp
  miscellaneous/GetEnvironmentVariableA/test4/test.cpp
  miscellaneous/GetEnvironmentVariableA/test5/test5.cpp
  miscellaneous/GetEnvironmentVariableA/test6/test6.cpp
  miscellaneous/GetEnvironmentVariableW/test1/test.cpp
  miscellaneous/GetEnvironmentVariableW/test2/test.cpp
  miscellaneous/GetEnvironmentVariableW/test3/test.cpp
  miscellaneous/GetEnvironmentVariableW/test4/test.cpp
  miscellaneous/GetEnvironmentVariableW/test5/test5.cpp
  miscellaneous/GetEnvironmentVariableW/test6/test6.cpp
  miscellaneous/GetLastError/test1/test.cpp
  miscellaneous/GetSystemInfo/test1/test.cpp
  miscellaneous/InterlockedCompareExchange/test1/test.cpp
  miscellaneous/InterlockedCompareExchange/test2/test.cpp
  miscellaneous/InterlockedCompareExchange64/test1/test.cpp
  miscellaneous/InterlockedCompareExchange64/test2/test.cpp
  miscellaneous/InterlockedCompareExchangePointer/test1/test.cpp
  miscellaneous/InterlockedDecrement/test1/test.cpp
  miscellaneous/InterlockedDecrement/test2/test.cpp
  miscellaneous/InterlockedDecrement64/test1/test.cpp
  miscellaneous/InterlockedDecrement64/test2/test.cpp
  miscellaneous/InterlockedExchange/test1/test.cpp
  miscellaneous/InterlockedExchange64/test1/test.cpp
  miscellaneous/InterLockedExchangeAdd/test1/test.cpp
  miscellaneous/InterlockedExchangePointer/test1/InterlockedExchangePointer.cpp
  miscellaneous/InterlockedIncrement/test1/test.cpp
  miscellaneous/InterlockedIncrement/test2/test.cpp
  miscellaneous/InterlockedIncrement64/test1/test.cpp
  miscellaneous/InterlockedIncrement64/test2/test.cpp
  miscellaneous/SetEnvironmentVariableA/test1/test1.cpp
  miscellaneous/SetEnvironmentVariableA/test2/test2.cpp
  miscellaneous/SetEnvironmentVariableA/test3/test3.cpp
  miscellaneous/SetEnvironmentVariableA/test4/test4.cpp
  miscellaneous/SetEnvironmentVariableW/test1/test.cpp
  miscellaneous/SetEnvironmentVariableW/test2/test.cpp
  miscellaneous/SetEnvironmentVariableW/test3/test3.cpp
  miscellaneous/SetEnvironmentVariableW/test4/test4.cpp
  miscellaneous/SetLastError/test1/test.cpp
  pal_specific/PAL_Initialize_Terminate/test1/PAL_Initialize_Terminate.cpp
  pal_specific/PAL_Initialize_Terminate/test2/pal_initialize_twice.cpp
  samples/test1/test.cpp
  samples/test2/test.cpp
  threading/CreateThread/test1/test1.cpp
  threading/CreateThread/test2/test2.cpp
  threading/CreateThread/test3/test3.cpp
  threading/DuplicateHandle/test7/test7.cpp
  threading/DuplicateHandle/test8/test8.cpp
#  threading/DuplicateHandle/test9/test9.cpp
  threading/ExitProcess/test1/ExitProcess.cpp
  threading/ExitProcess/test2/test2.cpp
  threading/ExitProcess/test3/test3.cpp
  threading/ExitThread/test1/test1.cpp
  threading/GetCurrentProcess/test1/process.cpp
  threading/GetCurrentProcessId/test1/processId.cpp
  threading/GetCurrentThread/test1/thread.cpp
  threading/GetCurrentThread/test2/test2.cpp
  threading/GetCurrentThreadId/test1/threadId.cpp
  threading/QueryThreadCycleTime/test1/test1.cpp
  threading/ResumeThread/test1/test1.cpp
  threading/RuntimeEvent/test1/test1.cpp
  threading/SwitchToThread/test1/test1.cpp
  threading/TerminateProcess/test1/TerminateProcess.cpp
  threading/ThreadPriority/test1/ThreadPriority.cpp
  threading/YieldProcessor/test1/test1.cpp
)

add_dependencies(paltests coreclrpal)

target_link_libraries(paltests
  PUBLIC
  ${COMMON_TEST_LIBRARIES}
)

if(CLR_CMAKE_TARGET_OPENBSD)
  # A few PAL tests (e.g. VirtualAlloc/VirtualProtect) deliberately request simultaneous
  # W+X (PAGE_EXECUTE_READWRITE) mappings to exercise the PAL API. OpenBSD rejects those
  # unless the binary carries the PT_OPENBSD_WXNEEDED note and runs from a wxallowed mount.
  # The runtime itself never needs this (it uses W^X double-mapping), so scope it here.
  target_link_options(paltests PRIVATE "-Wl,-z,wxneeded")
endif(CLR_CMAKE_TARGET_OPENBSD)

install (TARGETS paltests DESTINATION paltests COMPONENT paltests EXCLUDE_FROM_ALL)
add_dependencies(paltests_install paltests)
install (PROGRAMS runpaltests.sh runpaltestshelix.sh DESTINATION paltests COMPONENT paltests EXCLUDE_FROM_ALL)

if(CLR_CMAKE_HOST_BROWSER)
  install(FILES wasm/index.html paltestlist.txt DESTINATION ${CMAKE_CURRENT_BINARY_DIR} COMPONENT paltests EXCLUDE_FROM_ALL)
endif(CLR_CMAKE_HOST_BROWSER)
