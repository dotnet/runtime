# CMake script to copy object files and run objcopy --localize-hidden on the copies.
# Invoked via: cmake -DOBJCOPY=<path> -DOBJECT_FILES=<semicolon-separated-list> -DOUTPUT_DIR=<dir> -P localize-hidden-symbols.cmake

file(MAKE_DIRECTORY ${OUTPUT_DIR})

foreach(obj_file ${OBJECT_FILES})
  get_filename_component(obj_name "${obj_file}" NAME)
  set(output_file "${OUTPUT_DIR}/${obj_name}")
  file(COPY_FILE "${obj_file}" "${output_file}")
  execute_process(COMMAND ${OBJCOPY} --localize-hidden ${output_file} RESULT_VARIABLE result)
  if(NOT result EQUAL 0)
    message(FATAL_ERROR "objcopy --localize-hidden failed on ${output_file}")
  endif()
endforeach()
