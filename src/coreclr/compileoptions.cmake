if (CLR_CMAKE_PLATFORM_UNIX)
  # Disable frame pointer optimizations so profilers can get better call stacks
  add_compile_options(-fno-omit-frame-pointer)

  # The -fms-extensions enable the stuff like __if_exists, __declspec(uuid()), etc.
  add_compile_options(-fms-extensions )
  #-fms-compatibility      Enable full Microsoft Visual C++ compatibility
  #-fms-extensions         Accept some non-standard constructs supported by the Microsoft compiler

  # Make signed arithmetic overflow of addition, subtraction, and multiplication wrap around
  # using twos-complement representation (this is normally undefined according to the C++ spec).
  add_compile_options(-fwrapv)

  if(CLR_CMAKE_PLATFORM_DARWIN)
    # We cannot enable "stack-protector-strong" on OS X due to a bug in clang compiler (current version 7.0.2)
    add_compile_options(-fstack-protector)
  else()
    add_compile_options(-fstack-protector-strong)
  endif(CLR_CMAKE_PLATFORM_DARWIN)

  add_definitions(-DDISABLE_CONTRACTS)
  # The -ferror-limit is helpful during the porting, it makes sure the compiler doesn't stop
  # after hitting just about 20 errors.
  add_compile_options(-ferror-limit=4096)

  if (CLR_CMAKE_WARNINGS_ARE_ERRORS)
    # All warnings that are not explicitly disabled are reported as errors
    add_compile_options(-Werror)
  endif(CLR_CMAKE_WARNINGS_ARE_ERRORS)

  # Disabled warnings
  add_compile_options(-Wno-unused-private-field)
  add_compile_options(-Wno-unused-variable)
  # Explicit constructor calls are not supported by clang (this->ClassName::ClassName())
  add_compile_options(-Wno-microsoft)
  # This warning is caused by comparing 'this' to NULL
  add_compile_options(-Wno-tautological-compare)
  # There are constants of type BOOL used in a condition. But BOOL is defined as int
  # and so the compiler thinks that there is a mistake.
  add_compile_options(-Wno-constant-logical-operand)

  add_compile_options(-Wno-unknown-warning-option)

  #These seem to indicate real issues
  add_compile_options(-Wno-invalid-offsetof)
  # The following warning indicates that an attribute __attribute__((__ms_struct__)) was applied
  # to a struct or a class that has virtual members or a base class. In that case, clang
  # may not generate the same object layout as MSVC.
  add_compile_options(-Wno-incompatible-ms-struct)

  # Some architectures (e.g., ARM) assume char type is unsigned while CoreCLR assumes char is signed
  # as x64 does. It has been causing issues in ARM (https://github.com/dotnet/coreclr/issues/4746)
  add_compile_options(-fsigned-char)
endif(CLR_CMAKE_PLATFORM_UNIX)

if(CLR_CMAKE_PLATFORM_UNIX_ARM)
   # Because we don't use CMAKE_C_COMPILER/CMAKE_CXX_COMPILER to use clang
   # we have to set the triple by adding a compiler argument
   add_compile_options(-mthumb)
   add_compile_options(-mfpu=vfpv3)
   if(ARM_SOFTFP)
     add_definitions(-DARM_SOFTFP)
     add_compile_options(-mfloat-abi=softfp)
     add_compile_options(-target armv7-linux-gnueabi)
   else()
     add_compile_options(-target armv7-linux-gnueabihf)
   endif(ARM_SOFTFP)
endif(CLR_CMAKE_PLATFORM_UNIX_ARM)

if (WIN32)
  # Compile options for targeting windows

  # The following options are set by the razzle build
  add_compile_options(/TP) # compile all files as C++
  add_compile_options(/d2Zi+) # make optimized builds debugging easier
  add_compile_options(/nologo) # Suppress Startup Banner
  add_compile_options(/W3) # set warning level to 3
  add_compile_options(/WX) # treat warnings as errors
  add_compile_options(/Oi) # enable intrinsics
  add_compile_options(/Oy-) # disable suppressing of the creation of frame pointers on the call stack for quicker function calls
  add_compile_options(/U_MT) # undefine the predefined _MT macro
  add_compile_options(/GF) # enable read-only string pooling
  add_compile_options(/Gm-) # disable minimal rebuild
  add_compile_options(/EHa) # enable C++ EH (w/ SEH exceptions)
  add_compile_options(/Zp8) # pack structs on 8-byte boundary
  add_compile_options(/Gy) # separate functions for linker
  add_compile_options(/Zc:wchar_t-) # C++ language conformance: wchar_t is NOT the native type, but a typedef
  add_compile_options(/Zc:forScope) # C++ language conformance: enforce Standard C++ for scoping rules
  add_compile_options(/GR-) # disable C++ RTTI
  add_compile_options(/FC) # use full pathnames in diagnostics
  add_compile_options(/MP) # Build with Multiple Processes (number of processes equal to the number of processors)
  add_compile_options(/GS) # Buffer Security Check
  add_compile_options(/Zm200) # Specify Precompiled Header Memory Allocation Limit of 150MB
  add_compile_options(/wd4960 /wd4961 /wd4603 /wd4627 /wd4838 /wd4456 /wd4457 /wd4458 /wd4459 /wd4091 /we4640)
  add_compile_options(/Zi) # enable debugging information
  add_compile_options(/ZH:SHA_256) # use SHA256 for generating hashes of compiler processed source files.

  if (CLR_CMAKE_PLATFORM_ARCH_I386)
    add_compile_options(/Gz)
  endif (CLR_CMAKE_PLATFORM_ARCH_I386)

  add_compile_options($<$<OR:$<CONFIG:Release>,$<CONFIG:Relwithdebinfo>>:/GL>)
  add_compile_options($<$<OR:$<OR:$<CONFIG:Release>,$<CONFIG:Relwithdebinfo>>,$<CONFIG:Checked>>:/O1>)

  if (CLR_CMAKE_PLATFORM_ARCH_AMD64)
  # The generator expression in the following command means that the /homeparams option is added only for debug builds
  add_compile_options($<$<CONFIG:Debug>:/homeparams>) # Force parameters passed in registers to be written to the stack
  endif (CLR_CMAKE_PLATFORM_ARCH_AMD64)

  # enable control-flow-guard support for native components for non-Arm64 builds
  add_compile_options(/guard:cf) 

  # Statically linked CRT (libcmt[d].lib, libvcruntime[d].lib and libucrt[d].lib) by default. This is done to avoid  
  # linking in VCRUNTIME140.DLL for a simplified xcopy experience by reducing the dependency on VC REDIST.  
  #  
  # For Release builds, we shall dynamically link into uCRT [ucrtbase.dll] (which is pushed down as a Windows Update on downlevel OS) but  
  # wont do the same for debug/checked builds since ucrtbased.dll is not redistributable and Debug/Checked builds are not  
  # production-time scenarios.  
  add_compile_options($<$<OR:$<CONFIG:Release>,$<CONFIG:Relwithdebinfo>>:/MT>)  
  add_compile_options($<$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>:/MTd>)  

  set(CMAKE_ASM_MASM_FLAGS "${CMAKE_ASM_MASM_FLAGS} /ZH:SHA_256")
  
endif (WIN32)

if(CLR_CMAKE_ENABLE_CODE_COVERAGE)

  if(CLR_CMAKE_PLATFORM_UNIX)
    string(TOUPPER ${CMAKE_BUILD_TYPE} UPPERCASE_CMAKE_BUILD_TYPE)
    if(NOT UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG)
      message( WARNING "Code coverage results with an optimised (non-Debug) build may be misleading" )
    endif(NOT UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG)

    add_compile_options(-fprofile-arcs)
    add_compile_options(-ftest-coverage)
    set(CLANG_COVERAGE_LINK_FLAGS  "--coverage")
    set(CMAKE_SHARED_LINKER_FLAGS  "${CMAKE_SHARED_LINKER_FLAGS} ${CLANG_COVERAGE_LINK_FLAGS}")
    set(CMAKE_EXE_LINKER_FLAGS     "${CMAKE_EXE_LINKER_FLAGS} ${CLANG_COVERAGE_LINK_FLAGS}")
  else()
    message(FATAL_ERROR "Code coverage builds not supported on current platform")
  endif(CLR_CMAKE_PLATFORM_UNIX)

endif(CLR_CMAKE_ENABLE_CODE_COVERAGE)
