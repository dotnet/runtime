# Contains the crossgen build specific definitions. Included by the leaf crossgen cmake files.

add_definitions(
    -DCROSSGEN_COMPILE
    -DCROSS_COMPILE
    -DFEATURE_NATIVE_IMAGE_GENERATION
    -DSELF_NO_HOST)

remove_definitions(
    -DEnC_SUPPORTED
    -DFEATURE_EVENT_TRACE=1
    -DFEATURE_LOADER_OPTIMIZATION
    -DFEATURE_MULTICOREJIT
    -DFEATURE_RANDOMIZED_STRING_HASHING
    -DFEATURE_VERSIONING_LOG
)

if(WIN32)
    add_definitions(-MT)
endif(WIN32)
