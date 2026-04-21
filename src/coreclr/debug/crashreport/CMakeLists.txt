set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(CRASHREPORT_SOURCES
    crashjsonwriter.cpp
    inproccrashreporter.cpp
)

add_library(inproccrashreport OBJECT ${CRASHREPORT_SOURCES})
