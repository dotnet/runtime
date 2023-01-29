#include <cassert>
#include <platform.h>
#include <external/cor.h>

#include <dncp.h>
#include <dnmd_interfaces.hpp>

#ifdef _MSC_VER
#define W(str)  L##str
#define EXPORT extern "C" __declspec(dllexport)
#else
#define W(str)  u##str
#define EXPORT extern "C" __attribute__((__visibility__("default")))
#endif // !_MSC_VER
