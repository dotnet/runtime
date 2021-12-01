project (out_of_range_fp_to_int_conversionsnative)
include_directories(${INC_PLATFORM_DIR})

# add the executable
add_library (out_of_range_fp_to_int_conversionsnative SHARED out_of_range_fp_to_int_conversions.cpp)

# add the install targets
install (TARGETS out_of_range_fp_to_int_conversionsnative DESTINATION bin)
