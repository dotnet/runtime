set(CMAKE_INCLUDE_CURRENT_DIR ON)

include_directories(${CLR_ARTIFACTS_OBJ_DIR})
include_directories(${CLR_DIR})

set(CRASHREPORT_SOURCES
    crashjsonwriter.cpp
    inproccrashreporter.cpp
)

add_library(inproccrashreport OBJECT ${CRASHREPORT_SOURCES})
