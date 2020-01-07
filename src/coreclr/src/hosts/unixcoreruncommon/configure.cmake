check_symbol_exists(getauxval sys/auxv.h HAVE_GETAUXVAL)

configure_file(
	${CMAKE_CURRENT_SOURCE_DIR}/config.h.in
	${CMAKE_CURRENT_BINARY_DIR}/config.h)
