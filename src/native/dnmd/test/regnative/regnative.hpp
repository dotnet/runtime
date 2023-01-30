#include <cassert>
#include <cstdint>

#include <dncp.h>
#include <cor.h>
#include <dnmd_interfaces.hpp>

#ifdef _MSC_VER
#define EXPORT extern "C" __declspec(dllexport)
#else
#define EXPORT extern "C" __attribute__((__visibility__("default")))
#endif // !_MSC_VER

#define ARRAY_SIZE(a) (sizeof(a) / sizeof(*a))
