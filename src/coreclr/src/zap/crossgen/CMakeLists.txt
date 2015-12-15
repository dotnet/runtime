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

if (WIN32 AND FEATURE_READYTORUN)
    list(APPEND ZAP_SOURCES
        ../zapreadytorun.cpp
        ../nativeformatwriter.cpp
    )
endif (WIN32 AND FEATURE_READYTORUN)

add_precompiled_header(common.h ../common.cpp ZAP_SOURCES)
add_library(corzap_crossgen STATIC ${ZAP_SOURCES})
