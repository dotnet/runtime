cmake_minimum_required(VERSION 2.8.12.2)


set(CMAKE_INCLUDE_CURRENT_DIR ON)


set(SOURCES
    clralltestevents.cpp
    )
include_directories(${COREPAL_SOURCE_DIR}/prebuilt/inc)
include_directories(${COREPAL_SOURCE_DIR}/inc/rt)


add_executable(eventprovidertest
    ${SOURCES}
    )
set(EVENT_PROVIDER_DEPENDENCIES "")
set(EVENT_PROVIDER_LINKER_OTPTIONS "")  
if(CMAKE_SYSTEM_NAME STREQUAL Linux)

    list(APPEND EVENT_PROVIDER_DEPENDENCIES 
        eventprovider
        )
    list(APPEND EVENT_PROVIDER_LINKER_OTPTIONS  
        ${EVENT_PROVIDER_DEPENDENCIES}
        )

endif(CMAKE_SYSTEM_NAME STREQUAL Linux)

add_dependencies(eventprovidertest  ${EVENT_PROVIDER_DEPENDENCIES} coreclrpal)
target_link_libraries(eventprovidertest
    coreclrpal
    ${EVENT_PROVIDER_LINKER_OTPTIONS}
    )

