project(bootstrapperdll)

add_definitions(-DNATIVEAOT_DLL)

set(SOURCES
    ../main.cpp
)

add_library(bootstrapperdll STATIC ${SOURCES})

install_bootstrapper_object(bootstrapperdll aotsdk)
