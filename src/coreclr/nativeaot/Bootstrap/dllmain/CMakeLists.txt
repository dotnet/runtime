project(dllmain)

set(SOURCES
    dllmain.cpp
)

add_library(dllmain STATIC ${SOURCES})

install_bootstrapper_object(dllmain aotsdk)

add_library(dllmain.GuardCF STATIC ${SOURCES})
install_bootstrapper_object(dllmain.GuardCF aotsdk)
set_target_properties(dllmain.GuardCF PROPERTIES CLR_CONTROL_FLOW_GUARD ON)
