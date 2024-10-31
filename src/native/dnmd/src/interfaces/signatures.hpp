#ifndef _SRC_INTERFACES_SIGNATURES_HPP_
#define _SRC_INTERFACES_SIGNATURES_HPP_

#include <internal/dnmd_platform.hpp>
#include <internal/span.hpp>

#include <external/cor.h>

#include <array>
#include <cstdint>
#include <functional>
#include <cassert>

/// @brief A span that that supports owning a specified number of elements in itself.
/// @tparam T The type of the elements in the span.
/// @tparam NumInlineElements The number of elements to store in the span itself.
template <typename T, size_t NumInlineElements>
struct base_inline_span : public span<T>
{
    base_inline_span() : span<T>()
    {
        this->_ptr = _storage.data();
    }

    base_inline_span(size_t size) : span<T>()
    {
        this->_ptr = size > NumInlineElements ? base_inline_span::allocate_noninline_memory(size) : _storage.data();
        this->_size = size;
    }

    base_inline_span(base_inline_span&& other)
    {
        *this = std::move(other);
    }

    base_inline_span& operator=(base_inline_span&& other) noexcept
    {
        if (this->size() > NumInlineElements)
        {
            base_inline_span::free_noninline_memory(this->_ptr, this->size());
            this->_ptr = nullptr;
        }

        if (other.size() > NumInlineElements)
        {
            this->_ptr = other._ptr;
            other._ptr = nullptr;
        }
        else
        {
            std::copy(other.begin(), other.end(), _storage.begin());
            this->_ptr = _storage.data();
            this->_size = other._size;
        }

        return *this;
    }

    void resize(size_t newSize)
    {
        if (this->size() > NumInlineElements && newSize < NumInlineElements)
        {
            // Transitioning from a non-inline buffer to the inline buffer.
            std::copy(this->begin(), this->begin() + newSize, _storage.begin());
            base_inline_span::free_noninline_memory(this->_ptr, this->size());
            this->_ptr = _storage.data();
        }
        else if (this->size() <= NumInlineElements && newSize <= NumInlineElements)
        {
            // We're staying within the inline buffer, so just update the size.
            this->_size = newSize;
        }
        else if (this->size() > NumInlineElements && newSize < this->size())
        {
            // Shrinking the buffer, but still keeping it as a non-inline buffer.
            this->_size = newSize;
        }
        else
        {
            // Growing the buffer from the inline buffer to a non-inline buffer.
            assert(this->size() <= NumInlineElements && newSize > NumInlineElements);
            T* newPtr = base_inline_span::allocate_noninline_memory(this->size());
            std::copy(this->begin(), this->end(), newPtr);
            this->_ptr = newPtr;
            this->_size = newSize;
        }
    }

    ~base_inline_span()
    {
        if (this->size() > NumInlineElements)
        {
            assert(this->_ptr != _storage.data());
            base_inline_span::free_noninline_memory(this->_ptr, this->size());
            this->_ptr = nullptr;
        }
        else
        {
            assert(this->_ptr == _storage.data());
        }
    }

private:
    std::array<T, NumInlineElements> _storage;

    static T* allocate_noninline_memory(size_t numElements)
    {
        assert(numElements > NumInlineElements);
        return new T[numElements];
    }

    static void free_noninline_memory(T* ptr, size_t numElements)
    {
        UNREFERENCED_PARAMETER(numElements);
        assert(numElements > NumInlineElements);
        delete[] ptr;
    }
};

/// @brief An span with inline storage for up to 64 bytes.
/// @tparam T The element type of the span.
template <typename T>
using inline_span = base_inline_span<T, 64 / sizeof(T)>;

void GetMethodDefSigFromMethodRefSig(span<uint8_t> methodRefSig, inline_span<uint8_t>& methodDefSig);

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
    inline_span<uint8_t>& importedSignature);

// Import a TypeSpecBlob (II.23.2.14) from one set of module and assembly metadata into another set of module and assembly metadata.
HRESULT ImportTypeSpecBlob(
    mdhandle_t sourceAssembly,
    mdhandle_t sourceModule,
    span<const uint8_t> sourceAssemblyHash,
    mdhandle_t destinationAssembly,
    mdhandle_t destinationModule,
    span<const uint8_t> typeSpecBlob,
    std::function<void(mdcursor_t)> onRowAdded,
    inline_span<uint8_t>& importedTypeSpecBlob);

#endif // _SRC_INTERFACES_SIGNATURES_HPP_
