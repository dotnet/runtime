set(CMAKE_MSVC_RUNTIME_LIBRARY MultiThreaded)
add_executable_clr(StressLogAnalyzer StressLogAnalyzer.cpp StressLogDump.cpp StressLogPlugin.cpp)

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
