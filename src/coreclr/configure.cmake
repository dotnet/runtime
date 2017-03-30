include(CheckCXXSourceCompiles)

# VC++ guarantees support for LTCG (LTO's equivalent)
if(NOT WIN32)
  # Function required to give CMAKE_REQUIRED_* local scope
  function(check_have_lto)
    set(CMAKE_REQUIRED_FLAGS -flto)
    set(CMAKE_REQUIRED_LIBRARIES -flto -fuse-ld=gold)
    check_cxx_source_compiles("int main() { return 0; }" HAVE_LTO)
  endfunction(check_have_lto)
  check_have_lto()
endif(NOT WIN32)
