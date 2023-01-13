#ifndef _SRC_INC_SPAN_H_
#define _SRC_INC_SPAN_H_

#include <cstdlib>
#include <stdexcept>
#include <memory>

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

    span& operator=(span&& other) = default;

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
};

template<typename T, typename Deleter>
class owning_span : public span<T>
{
public:
    owning_span() : span<T>{}
    { }

    owning_span(T* ptr, size_t len)
        : span<T>{ ptr, len }
    { }

    owning_span(owning_span&& other)
        : span<T>{}
    {
        *this = other;
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
};

struct free_deleter
{
    void operator()(void* ptr)
    {
        free(ptr);
    }
};

template<typename T>
using malloc_span = owning_span<T, free_deleter>;

#endif // _SRC_INC_SPAN_H_