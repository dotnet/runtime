set(CMAKE_MSVC_RUNTIME_LIBRARY MultiThreaded)
# Using add_executable instead of add_executable_clr because changing the MSVC runtime causes issues with ASAN
# TODO: Figure out why it causes problems
add_executable(StressLogAnalyzer StressLogAnalyzer.cpp StressLogDump.cpp StressLogPlugin.cpp)

if(CLR_CMAKE_TARGET_WIN32)
    target_link_libraries(StressLogAnalyzer
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_VCRT_LIB}
    )
else()
    target_link_libraries(StressLogAnalyzer
        coreclrpal
    )
endif(CLR_CMAKE_TARGET_WIN32)

install_clr(TARGETS StressLogAnalyzer DESTINATIONS . COMPONENT runtime)
