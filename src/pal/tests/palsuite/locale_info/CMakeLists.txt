cmake_minimum_required(VERSION 2.8.12.2)

# TODO: make the following tests compile if they are needed
# add_subdirectory(CompareStringA)
# add_subdirectory(CompareStringW)
# add_subdirectory(GetLocaleInfoW)
# add_subdirectory(GetSystemDefaultLangID)
# add_subdirectory(GetThreadLocale)
# add_subdirectory(GetTimeZoneInformation)
# add_subdirectory(GetUserDefaultLangID)
# add_subdirectory(GetUserDefaultLCID)
# add_subdirectory(IsValidLocale)
# add_subdirectory(SetThreadLocale)

add_subdirectory(GetACP)
add_subdirectory(GetCPInfo)
add_subdirectory(IsDBCSLeadByte)
add_subdirectory(IsDBCSLeadByteEx)
add_subdirectory(IsValidCodePage)
add_subdirectory(MultiByteToWideChar)
add_subdirectory(WideCharToMultiByte)




