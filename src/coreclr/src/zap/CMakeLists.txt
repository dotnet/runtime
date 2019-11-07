set(ZAP_SOURCES
    svcworker.cpp
    zapper.cpp
    zaprelocs.cpp
    zapcode.cpp
    zapheaders.cpp
    zapimage.cpp
    zapinfo.cpp
    zapimport.cpp
    zapinnerptr.cpp
    zapmetadata.cpp
    zapwriter.cpp
    zapwrapper.cpp
    zapperstats.cpp
    nativeformatwriter.cpp
)

set(ZAP_HEADERS
    zaprelocs.h
    zapcode.h
    zapheaders.h
    zapimage.h
    zapinfo.h
    zapimport.h
    zapinnerptr.h
    zaplog.h
    zapmetadata.h
    zapnodetype.h
    zapwriter.h
    zapwrapper.h
    zapperstats.h
    nativeformatwriter.h
)

if (FEATURE_READYTORUN)
    list(APPEND ZAP_SOURCES
        zapreadytorun.cpp
        nativeformatwriter.cpp
    )
    list(APPEND ZAP_HEADERS
        zapreadytorun.h
        nativeformatwriter.h
    )
endif (FEATURE_READYTORUN)

if (WIN32)
    list(APPEND ZAP_SOURCES ${ZAP_HEADERS})
endif(WIN32)

add_library_clr(corzap_crossgen STATIC ${ZAP_SOURCES})
set_target_properties(corzap_crossgen PROPERTIES CROSSGEN_COMPONENT TRUE)
target_precompile_header(TARGET corzap_crossgen HEADER common.h)

if(FEATURE_MERGE_JIT_AND_ENGINE)
  target_link_libraries(corzap_crossgen clrjit_crossgen)
endif(FEATURE_MERGE_JIT_AND_ENGINE)
