# CMake script to run objcopy --localize-hidden on a list of object files.
# Invoked via: cmake -DOBJCOPY=<path> -DOBJECT_FILES=<semicolon-separated-list> -P localize-hidden-symbols.cmake

foreach(obj_file ${OBJECT_FILES})
  execute_process(COMMAND ${OBJCOPY} --localize-hidden ${obj_file} RESULT_VARIABLE result)
  if(NOT result EQUAL 0)
    message(FATAL_ERROR "objcopy --localize-hidden failed on ${obj_file}")
  endif()
endforeach()
