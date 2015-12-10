project(ilasm)

add_definitions(-DUNICODE)
add_definitions(-D_UNICODE)
add_definitions(-D_FEATURE_NO_HOST)
add_definitions(-D__ILASM__)

add_definitions(-DFEATURE_CORECLR)

include_directories(.)

set(ILASM_SOURCES
  assem.cpp
  writer.cpp
  writer_enc.cpp
  method.cpp
  asmman.cpp
  main.cpp
  assembler.cpp
  prebuilt/asmparse.c
)
if(WIN32)
  set_source_files_properties(prebuilt/asmparse.c PROPERTIES COMPILE_FLAGS "/TP")
endif(WIN32)

add_executable(ilasm
  ${ILASM_SOURCES}
)

set(ILASM_LINK_LIBRARIES
  mscorpe
  utilcodestaticnohost
  mdhotdata_full
  corguids
)

if(CLR_CMAKE_PLATFORM_UNIX)
  #TODO
else()
  target_link_libraries(ilasm
      ${ILASM_LINK_LIBRARIES}
      coreclr
      msvcrt
      ole32
      oleaut32
      shell32
  )

  install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/ilasm.pdb DESTINATION PDB)
endif(CLR_CMAKE_PLATFORM_UNIX)

install (TARGETS ilasm DESTINATION .)
