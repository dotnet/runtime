set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(CRASHREPORT_SOURCES
    signalsafejsonwriter.cpp
    inproccrashreporter.cpp
)

add_library(inproccrashreport OBJECT ${CRASHREPORT_SOURCES})

target_sources(coreclrpal PRIVATE $<TARGET_OBJECTS:inproccrashreport>)
