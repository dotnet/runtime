project(mscorpe)

set(MSCORPE_SOURCES
  iceefilegen.cpp
  ceefilegenwriter.cpp
  pewriter.cpp
  ceefilegenwritertokens.cpp
)

add_library_clr(mscorpe STATIC
  ${MSCORPE_SOURCES}
)

# Disable the hosted contract machinery to avoid requiring the contract implementation.
target_compile_definitions(mscorpe PRIVATE SELF_NO_HOST)
