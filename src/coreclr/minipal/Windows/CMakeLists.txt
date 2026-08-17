set(SOURCES
    doublemapping.cpp
    dn-u16.cpp
    dn-stdio.cpp
    memory.cpp
    wait.cpp
)
add_library(coreclrminipal
    STATIC
    ${SOURCES}
)

target_link_libraries(coreclrminipal PRIVATE minipal)
