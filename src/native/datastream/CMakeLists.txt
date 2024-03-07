set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(DATASTREAM_SOURCES
    data_stream.c
    data_stream.h
)

add_library(datastream OBJECT ${DATASTREAM_SOURCES})

add_library_clr(datastreamlib SHARED ${DATASTREAM_SOURCES})
target_compile_options(datastreamlib PRIVATE -DBUILD_SHARED_LIBRARY)

if(CLR_CMAKE_TARGET_WIN32)
  install(FILES $<TARGET_OBJECTS:datastream> DESTINATION datastream COMPONENT cdac RENAME datastream.obj)
else()
  install(FILES $<TARGET_OBJECTS:datastream> DESTINATION datastream COMPONENT cdac RENAME datastream.o)
endif()

install_clr(TARGETS datastreamlib DESTINATIONS . COMPONENT cdac)
