set(SHARED_CLRMATH_BASE_DIR ${CMAKE_CURRENT_LIST_DIR})

set(SHARED_CLRMATH_INCLUDE_DIR ${SHARED_CLRMATH_BASE_DIR}/inc)
set(SHARED_CLRMATH_SOURCE_DIR ${SHARED_CLRMATH_BASE_DIR}/src)

set(SHARED_CLRMATH_SOURCES "")

include_directories("${SHARED_CLRMATH_INCLUDE_DIR}")

list(APPEND SHARED_CLRMATH_SOURCES
  ${SHARED_CLRMATH_SOURCE_DIR}/libm_special.cpp

  # ${SHARED_CLRMATH_SOURCE_DIR}/opt/cos.cpp
  # ${SHARED_CLRMATH_SOURCE_DIR}/opt/cosf.cpp

  ${SHARED_CLRMATH_SOURCE_DIR}/opt/coshf.cpp

  ${SHARED_CLRMATH_SOURCE_DIR}/opt/exp.cpp
  ${SHARED_CLRMATH_SOURCE_DIR}/opt/expf.cpp
  ${SHARED_CLRMATH_SOURCE_DIR}/opt/exp_tables.cpp

  ${SHARED_CLRMATH_SOURCE_DIR}/opt/logf.cpp
  ${SHARED_CLRMATH_SOURCE_DIR}/opt/log_tables.cpp

  ${SHARED_CLRMATH_SOURCE_DIR}/opt/pow.cpp
  ${SHARED_CLRMATH_SOURCE_DIR}/opt/powf.cpp
  ${SHARED_CLRMATH_SOURCE_DIR}/opt/pow_data.cpp

  # ${SHARED_CLRMATH_SOURCE_DIR}/opt/sin.cpp
  # ${SHARED_CLRMATH_SOURCE_DIR}/opt/sinf.cpp

  # ${SHARED_CLRMATH_SOURCE_DIR}/opt/tan.cpp
  # ${SHARED_CLRMATH_SOURCE_DIR}/opt/tanf.cpp

  ${SHARED_CLRMATH_SOURCE_DIR}/opt/tanhf.cpp

  ${SHARED_CLRMATH_SOURCE_DIR}/opt/data/_exp_j_by_64.cpp
  ${SHARED_CLRMATH_SOURCE_DIR}/opt/data/_log_data.cpp

  ${SHARED_CLRMATH_SOURCE_DIR}/ref/acos.cpp
  ${SHARED_CLRMATH_SOURCE_DIR}/ref/acosf.cpp

  ${SHARED_CLRMATH_SOURCE_DIR}/ref/acosh.cpp
  ${SHARED_CLRMATH_SOURCE_DIR}/ref/acoshf.cpp

  ${SHARED_CLRMATH_SOURCE_DIR}/ref/asin.cpp
  ${SHARED_CLRMATH_SOURCE_DIR}/ref/asinf.cpp

  ${SHARED_CLRMATH_SOURCE_DIR}/ref/asinh.cpp
  ${SHARED_CLRMATH_SOURCE_DIR}/ref/asinhf.cpp

  ${SHARED_CLRMATH_SOURCE_DIR}/ref/atan.cpp
  ${SHARED_CLRMATH_SOURCE_DIR}/ref/atanf.cpp

  ${SHARED_CLRMATH_SOURCE_DIR}/ref/atanh.cpp
  ${SHARED_CLRMATH_SOURCE_DIR}/ref/atanhf.cpp

  ${SHARED_CLRMATH_SOURCE_DIR}/ref/atan2.cpp
  ${SHARED_CLRMATH_SOURCE_DIR}/ref/atan2f.cpp

  ${SHARED_CLRMATH_SOURCE_DIR}/ref/ceil.cpp
  ${SHARED_CLRMATH_SOURCE_DIR}/ref/ceilf.cpp

  ${SHARED_CLRMATH_SOURCE_DIR}/ref/cosh.cpp

  ${SHARED_CLRMATH_SOURCE_DIR}/ref/floor.cpp
  ${SHARED_CLRMATH_SOURCE_DIR}/ref/floorf.cpp

  ${SHARED_CLRMATH_SOURCE_DIR}/ref/ilogb.cpp
  ${SHARED_CLRMATH_SOURCE_DIR}/ref/ilogbf.cpp

  ${SHARED_CLRMATH_SOURCE_DIR}/ref/modf.cpp
  ${SHARED_CLRMATH_SOURCE_DIR}/ref/modff.cpp

  ${SHARED_CLRMATH_SOURCE_DIR}/ref/sinh.cpp
  ${SHARED_CLRMATH_SOURCE_DIR}/ref/sinhf.cpp

  ${SHARED_CLRMATH_SOURCE_DIR}/ref/tanh.cpp
)
