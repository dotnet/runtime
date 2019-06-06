cmake_minimum_required(VERSION 2.8.12.2)

set(SOURCES
  ${CMAKE_CURRENT_BINARY_DIR}/clralltestevents.cpp
)

set(EVENT_MANIFEST ${VM_DIR}/ClrEtwAll.man)
set(TEST_GENERATOR ${CLR_DIR}/src/scripts/genEventingTests.py)

include(FindPythonInterp)

add_custom_command(OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/clralltestevents.cpp
  COMMAND ${PYTHON_EXECUTABLE} ${TEST_GENERATOR} --testdir "${CMAKE_CURRENT_BINARY_DIR}" --man "${EVENT_MANIFEST}"
  DEPENDS ${EVENT_MANIFEST} ${TEST_GENERATOR}
  COMMENT "Updating clralltestevents.cpp"
)

include_directories(${GENERATED_INCLUDE_DIR})

if(FEATURE_PAL)
    include_directories(${COREPAL_SOURCE_DIR}/inc/rt)
endif(FEATURE_PAL)

add_executable(eventprovidertest
            ${SOURCES}
            )
set(EVENT_PROVIDER_DEPENDENCIES "")
if(FEATURE_EVENT_TRACE)
    add_definitions(-DFEATURE_EVENT_TRACE)
    list(APPEND EVENT_PROVIDER_DEPENDENCIES
        eventprovider
        )
    if(CLR_CMAKE_PLATFORM_LINUX)
        list(APPEND EVENT_PROVIDER_DEPENDENCIES
            coreclrtraceptprovider
            )
    endif(CLR_CMAKE_PLATFORM_LINUX)
endif(FEATURE_EVENT_TRACE)

target_link_libraries(eventprovidertest  ${EVENT_PROVIDER_DEPENDENCIES} coreclrpal)
add_dependencies(eventprovidertest eventing_headers)
