set(SOURCES
    ${RUNTIME_DIR}/windows/CLREventBase.cpp
    doublemapping.cpp
    dn-u16.cpp
    dn-stdio.cpp
    mapping.cpp
    ${CMAKE_CURRENT_SOURCE_DIR}/../mappingholders.cpp
    memory.cpp
)
add_library(coreclrminipal
    STATIC
    ${SOURCES}
)

target_link_libraries(coreclrminipal PRIVATE minipal)
