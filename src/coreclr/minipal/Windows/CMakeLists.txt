set(SOURCES
    doublemapping.cpp
    dn-u16.cpp
    ${CLR_SRC_NATIVE_DIR}/minipal/utf8.c
)

if(NOT CLR_CROSS_COMPONENTS_BUILD)
    list(APPEND SOURCES
        ${CLR_SRC_NATIVE_DIR}/minipal/cpufeatures.c
    )
endif()

add_library(coreclrminipal
    STATIC
    ${SOURCES}
)
