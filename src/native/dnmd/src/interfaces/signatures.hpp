#ifndef _SRC_INTERFACES_SIGNATURES_HPP_
#define _SRC_INTERFACES_SIGNATURES_HPP_

#include <internal/dnmd_platform.hpp>
#include <internal/span.hpp>

#include <external/cor.h>

#include <cstdint>
#include <functional>

malloc_span<uint8_t> GetMethodDefSigFromMethodRefSig(span<uint8_t> methodRefSig);

// Import a signature from one set of module and assembly metadata into another set of module and assembly metadata.
// The module and assembly metadata for source or destination can be the same metadata.
// The supported signature kinds are:
// - MethodDefSig (II.23.2.1)
// - MethodRefSig (II.23.2.2)
// - StandaloneMethodSig (II.23.2.3)
// - FieldSig (II.23.2.4)
// - PropertySig (II.23.2.5)
// - LocalVarSig (II.23.2.6)
// - MethodSpec (II.23.2.15)
HRESULT ImportSignatureIntoModule(
    mdhandle_t sourceAssembly,
    mdhandle_t sourceModule,
    span<const uint8_t> sourceAssemblyHash,
    mdhandle_t destinationAssembly,
    mdhandle_t destinationModule,
    span<const uint8_t> signature,
    std::function<void(mdcursor_t)> onRowAdded,
    malloc_span<uint8_t>& importedSignature);

// Import a TypeSpecBlob (II.23.2.14) from one set of module and assembly metadata into another set of module and assembly metadata.
HRESULT ImportTypeSpecBlob(
    mdhandle_t sourceAssembly,
    mdhandle_t sourceModule,
    span<const uint8_t> sourceAssemblyHash,
    mdhandle_t destinationAssembly,
    mdhandle_t destinationModule,
    span<const uint8_t> typeSpecBlob,
    std::function<void(mdcursor_t)> onRowAdded,
    malloc_span<uint8_t>& importedTypeSpecBlob);

#endif // _SRC_INTERFACES_SIGNATURES_HPP_
