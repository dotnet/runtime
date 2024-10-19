set(SOURCES
    doublemapping.cpp
    dn-u16.cpp
    ${CLR_SRC_NATIVE_DIR}/minipal/xoshiro128pp.c
)

add_library(coreclrminipal
    STATIC
    ${SOURCES}
)

target_link_libraries(coreclrminipal PRIVATE minipal)
