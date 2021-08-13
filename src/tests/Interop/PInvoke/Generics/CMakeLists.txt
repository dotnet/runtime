project (GenericsNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
if(CLR_CMAKE_TARGET_ARCH_I386)
  add_definitions(-DTARGET_X86)
  add_definitions(-DTARGET_XARCH)
elseif(CLR_CMAKE_TARGET_ARCH_AMD64)
  add_definitions(-DTARGET_AMD64)
  add_definitions(-DTARGET_XARCH)
elseif(CLR_CMAKE_TARGET_ARCH_ARM)
  add_definitions(-DTARGET_ARM)
  add_definitions(-DTARGET_ARMARCH)
elseif(CLR_CMAKE_TARGET_ARCH_ARM64)
  add_definitions(-DTARGET_ARM64)
  add_definitions(-DTARGET_ARMARCH)
endif()
if (CMAKE_CXX_COMPILER_ID STREQUAL "GNU")
  # The ABI for passing parameters with 32-byte alignment has changed in GCC 4.6
  add_compile_options(-Wno-psabi)
endif()
if (MSVC)
  # The MSVC require explicitly ask for AVX2 so define would be present.
  add_compile_options(/arch:AVX2)
else()
  if (NOT CLR_CMAKE_TARGET_ARCH_ARM64 AND NOT CLR_CMAKE_TARGET_ARCH_ARM)
    # We need -march=native so we can detect if AVX2 is present.
    # ARM does not like that option too and it make no sense to have this detection there.
    add_compile_options(-march=native)
  endif()
endif()
set(SOURCES
  GenericsNative.IUnknown.cpp
  GenericsNative.NullableB.cpp
  GenericsNative.NullableC.cpp
  GenericsNative.NullableD.cpp
  GenericsNative.NullableF.cpp
  GenericsNative.NullableL.cpp
  GenericsNative.NullableU.cpp
  GenericsNative.Point1B.cpp
  GenericsNative.Point1C.cpp
  GenericsNative.Point1D.cpp
  GenericsNative.Point1F.cpp
  GenericsNative.Point1L.cpp
  GenericsNative.Point1U.cpp
  GenericsNative.Point2B.cpp
  GenericsNative.Point2C.cpp
  GenericsNative.Point2D.cpp
  GenericsNative.Point2F.cpp
  GenericsNative.Point2L.cpp
  GenericsNative.Point2U.cpp
  GenericsNative.Point3B.cpp
  GenericsNative.Point3C.cpp
  GenericsNative.Point3D.cpp
  GenericsNative.Point3F.cpp
  GenericsNative.Point3L.cpp
  GenericsNative.Point3U.cpp
  GenericsNative.Point4B.cpp
  GenericsNative.Point4C.cpp
  GenericsNative.Point4D.cpp
  GenericsNative.Point4F.cpp
  GenericsNative.Point4L.cpp
  GenericsNative.Point4U.cpp
  GenericsNative.SequentialClassB.cpp
  GenericsNative.SequentialClassC.cpp
  GenericsNative.SequentialClassD.cpp
  GenericsNative.SequentialClassF.cpp
  GenericsNative.SequentialClassL.cpp
  GenericsNative.SequentialClassU.cpp
  GenericsNative.SpanB.cpp
  GenericsNative.SpanC.cpp
  GenericsNative.SpanD.cpp
  GenericsNative.SpanF.cpp
  GenericsNative.SpanL.cpp
  GenericsNative.SpanU.cpp
  GenericsNative.Vector64B.cpp
  GenericsNative.Vector64C.cpp
  GenericsNative.Vector64D.cpp
  GenericsNative.Vector64F.cpp
  GenericsNative.Vector64L.cpp
  GenericsNative.Vector64U.cpp
  GenericsNative.Vector128B.cpp
  GenericsNative.Vector128C.cpp
  GenericsNative.Vector128D.cpp
  GenericsNative.Vector128F.cpp
  GenericsNative.Vector128L.cpp
  GenericsNative.Vector128U.cpp
  GenericsNative.Vector256B.cpp
  GenericsNative.Vector256C.cpp
  GenericsNative.Vector256D.cpp
  GenericsNative.Vector256F.cpp
  GenericsNative.Vector256L.cpp
  GenericsNative.Vector256U.cpp
  GenericsNative.VectorB.cpp
  GenericsNative.VectorC.cpp
  GenericsNative.VectorD.cpp
  GenericsNative.VectorF.cpp
  GenericsNative.VectorL.cpp
  GenericsNative.VectorU.cpp
)
add_library (GenericsNative SHARED ${SOURCES})
install (TARGETS GenericsNative DESTINATION bin)
