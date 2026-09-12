// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef LOAD_CONTEXT_STORAGE_H
#define LOAD_CONTEXT_STORAGE_H

#include <assert.h>
#include <utility>
#include <coreclr_delegates.h>
#include <pal.h>

class load_context_storage
{
public:
    load_context_storage() = default;

    static load_context_storage create_default()
    {
        return load_context_storage();
    }

    static load_context_storage create_isolated()
    {
        return load_context_storage(kind::isolated_context);
    }

    static load_context_storage create_named(const pal::char_t* identifier)
    {
        return load_context_storage(identifier);
    }

    load_context_storage(const load_context_storage&) = delete;
    load_context_storage& operator=(const load_context_storage&) = delete;

    load_context_storage(load_context_storage&& other)
        : _kind{other._kind}
        , _identifier{std::move(other._identifier)}
    {
        if (_kind == kind::named_context)
            _context.identifier = _identifier.c_str();

        other._kind = kind::default_context;
    }

    load_context_storage& operator=(load_context_storage&& other)
    {
        if (this != &other)
        {
            _kind = other._kind;
            _identifier = std::move(other._identifier);
            if (_kind == kind::named_context)
                _context.identifier = _identifier.c_str();

            other._kind = kind::default_context;
        }

        return *this;
    }

    void* get()
    {
        switch (_kind)
        {
        case kind::default_context:
            return nullptr;
        case kind::isolated_context:
            return CORECLR_LOAD_CONTEXT_ISOLATED;
        case kind::named_context:
            return &_context;
        default:
            assert(false && "Unknown load context kind");
            return nullptr;
        }
    }

private:
    enum class kind
    {
        default_context,
        isolated_context,
        named_context,
    };

    load_context_storage(kind kind)
        : _kind{kind}
    { }

    load_context_storage(const pal::char_t* identifier)
        : _kind{kind::named_context}
        , _identifier{identifier}
    {
        _context.identifier = _identifier.c_str();
    }

    kind _kind{kind::default_context};
    struct coreclr_load_context _context{sizeof(struct coreclr_load_context), nullptr};
    pal::string_t _identifier;
};

#endif // LOAD_CONTEXT_STORAGE_H
