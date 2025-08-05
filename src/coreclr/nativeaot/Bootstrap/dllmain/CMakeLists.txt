project(dllmain)

set(SOURCES
    dllmain.cpp
)

add_library(dllmain STATIC ${SOURCES})

install_bootstrapper_object(dllmain aotsdk)
