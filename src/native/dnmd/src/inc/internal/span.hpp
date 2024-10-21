#ifndef _SRC_INC_INTERNAL_SPAN_HPP_
#define _SRC_INC_INTERNAL_SPAN_HPP_

#include <cstdlib>
#include <stdexcept>

template<typename T>
class span
{
protected:
    T* _ptr;
    size_t _size;
public:
    span()
        : _ptr{}
        , _size{}
    { }

    span(T* ptr, size_t len)
            : _ptr{ ptr }, _size{ len }
    { }

    span(span const & other) = default;

    span& operator=(span&& other) noexcept = default;

    size_t size() const noexcept
    {
        return _size;
    }

    operator T* () noexcept
    {
        return _ptr;
    }

    operator T const* () const noexcept
    {
        return _ptr;
    }

    T& operator[](size_t idx)
    {
        if (_ptr == nullptr)
            throw std::runtime_error{ "Deref null" };
        if (idx >= _size)
            throw std::out_of_range{ "Out of bounds access" };
        return _ptr[idx];
    }

    operator span<T const>() const
    {
        return { _ptr, _size };
    }

    T* begin() noexcept
    {
        return _ptr;
    }

    T const* cbegin() const noexcept
    {
        return _ptr;
    }

    T* end() noexcept
    {
        return _ptr + _size;
    }

    T const* cend() const noexcept
    {
        return _ptr + _size;
    }
};

template<typename T, typename Deleter>
class owning_span final : public span<T>
{
public:
    owning_span() : span<T>{}
    { }

    owning_span(T* ptr, size_t len)
        : span<T>{ ptr, len }
    { }

    owning_span(owning_span&& other) noexcept
        : span<T>{}
    {
        *this = std::move(other);
    }

    ~owning_span()
    {
        Deleter{}(this->_ptr);
    }

    owning_span& operator=(owning_span&& other) noexcept
    {
        if (this->_ptr != nullptr)
            Deleter{}(this->_ptr);

        this->_ptr = other._ptr;
        this->_size = other._size;
        other._ptr = {};
        other._size = {};
        return *this;
    }

    T* release() noexcept
    {
        T* tmp = this->_ptr;
        this->_ptr = {};
        return tmp;
    }

    operator owning_span<T const, Deleter>() const
    {
        return { this->_ptr, this->_size };
    }
};

struct free_deleter final
{
    void operator()(void* ptr)
    {
        std::free(ptr);
    }
};

template<typename T>
using malloc_span = owning_span<T, free_deleter>;

template<typename T>
span<T> slice(span<T> b, size_t offset)
{
    if (offset > b.size())
        throw std::out_of_range{ "Out of bounds access" };
    return { b + offset, b.size() - offset };
}

#endif // _SRC_INC_INTERNAL_SPAN_HPP_