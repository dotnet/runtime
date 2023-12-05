#ifndef _SRC_INTERFACES_SIGNATURES_HPP_
#define _SRC_INTERFACES_SIGNATURES_HPP_

#include <internal/dnmd_platform.hpp>
#include <internal/span.hpp>

#include <external/cor.h>

#include <cstdint>

malloc_span<uint8_t> GetMethodDefSigFromMethodRefSig(span<uint8_t> methodRefSig);

#endif // _SRC_INTERFACES_SIGNATURES_HPP_
