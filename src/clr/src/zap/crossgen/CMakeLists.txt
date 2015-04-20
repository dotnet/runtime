include(${CLR_DIR}/crossgen.cmake)

set(ZAP_SOURCES
    ../svcworker.cpp
    ../zapper.cpp
    ../zaprelocs.cpp
    ../zapcode.cpp
    ../zapheaders.cpp
    ../zapimage.cpp
    ../zapinfo.cpp
    ../zapimport.cpp
    ../zapinnerptr.cpp
    ../zapmetadata.cpp
    ../zapwriter.cpp
    ../zapwrapper.cpp
    ../zapperstats.cpp
    ../nativeformatwriter.cpp
)

add_precompiled_header(common.h ../common.cpp ZAP_SOURCES)
add_library(corzap_crossgen STATIC ${ZAP_SOURCES})
