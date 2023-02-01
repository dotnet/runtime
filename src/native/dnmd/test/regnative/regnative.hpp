#include <cstddef>
#include <cstdint>
#include <cstring>
#include <cassert>

#include <internal/dnmd_platform.hpp>
#include <dnmd_interfaces.hpp>

#ifdef _MSC_VER
#define EXPORT extern "C" __declspec(dllexport)
#else
#define EXPORT extern "C" __attribute__((__visibility__("default")))
#endif // !_MSC_VER
