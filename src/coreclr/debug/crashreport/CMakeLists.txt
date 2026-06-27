set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(CRASHREPORT_SOURCES
    crashreportstringutils.cpp
    signalsafeformatter.cpp
    signalsafejsonwriter.cpp
    signalsafeconsolewriter.cpp
    inproccrashreportlifecycle.cpp
    inproccrashreporter.cpp
    inproccrashreportwatchdog.cpp
)

add_library(inproccrashreport OBJECT ${CRASHREPORT_SOURCES})
target_include_directories(inproccrashreport PRIVATE ${CLR_DIR}/pal/src/include)

target_sources(coreclrpal PRIVATE $<TARGET_OBJECTS:inproccrashreport>)
