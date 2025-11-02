set(SOURCES
    doublemapping.cpp
    dn-u16.cpp
)

add_library(coreclrminipal_objects
    OBJECT
    ${SOURCES}
)

add_library(coreclrminipal
    STATIC
    $<TARGET_OBJECTS:coreclrminipal_objects>
)

target_link_libraries(coreclrminipal PRIVATE minipal)

if (CLR_CMAKE_TARGET_ARCH_WASM)
  install_clr(TARGETS coreclrminipal DESTINATIONS sharedFramework COMPONENT runtime)
endif (CLR_CMAKE_TARGET_ARCH_WASM)
