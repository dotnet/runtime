if(WIN32)
    list(APPEND LINK_LIBRARIES_ADDITIONAL
        ole32.lib
        advapi32.lib
    )
endif(WIN32)

# Consumed by native test assets
SET(CLR_INTEROP_TEST_ROOT ${CMAKE_CURRENT_SOURCE_DIR})

include_directories(common)
add_subdirectory(NativeCallable)
add_subdirectory(PrimitiveMarshalling/Bool)
add_subdirectory(PrimitiveMarshalling/UIntPtr)
add_subdirectory(ArrayMarshalling/BoolArray)
add_subdirectory(ArrayMarshalling/ByValArray)
add_subdirectory(PrimitiveMarshalling/EnumMarshalling)
add_subdirectory(FuncPtrAsDelegateParam)
add_subdirectory(SimpleStruct)
add_subdirectory(StructMarshalling/PInvoke)
add_subdirectory(BestFitMapping)
add_subdirectory(RefInt)
add_subdirectory(RefCharArray)
add_subdirectory(StringMarshalling/LPSTR)
add_subdirectory(StringMarshalling/LPTSTR)
add_subdirectory(StringMarshalling/UTF8)
add_subdirectory(MarshalAPI/FunctionPointer)
add_subdirectory(MarshalAPI/IUnknown)
add_subdirectory(SizeConst)
add_subdirectory(DllImportAttribute/ExeFile)
add_subdirectory(DllImportAttribute/FileNameContainDot)
add_subdirectory(DllImportAttribute/Simple)

if(WIN32)
    add_subdirectory(COM/NativeServer)
    add_subdirectory(COM/NativeClients/Primitives)
    add_subdirectory(IJW/FakeMscoree)

    # IJW isn't supported on ARM64
    if(NOT CLR_CMAKE_PLATFORM_ARCH_ARM64)
        add_subdirectory(IJW/ManagedCallingNative/IjwNativeDll)
        add_subdirectory(IJW/NativeCallingManaged/IjwNativeCallingManagedDll)
    endif()
endif(WIN32)
