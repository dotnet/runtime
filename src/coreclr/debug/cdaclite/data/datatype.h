// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// datatype.h
//
// Infrastructure for descriptor-driven Data types (see runtimetypes.h for the
// actual type definitions). A Data type derives from Struct and declares its
// fields as typed members via the CDAC_* macros, e.g.:
//
//     struct Thread : Struct
//     {
//         Thread() : Struct("Thread") {}
//         CDAC_PTR(LinkNext)
//         CDAC_PTR(CachedStackBase)
//     };
//
// Each field member self-registers with its owning Struct, so Struct::Load()
// can read every field generically. Field OFFSETS come from the target's
// in-memory data descriptor at runtime -- they are never written here. A field's
// descriptor name is its member name (stringized once by the macro), so the two
// can never drift apart. Access is ergonomic: `thread.LinkNext` implicitly
// converts to the field value.
//*****************************************************************************

#ifndef CDACLITE_DATATYPE_H
#define CDACLITE_DATATYPE_H

#include <stdint.h>

#include "target.h"

namespace cdac
{
namespace data
{
    enum class FieldKind { Ptr, U32, U64, Addr };

    class Struct;

    // Base for a single field: knows its descriptor name and kind, stores the read
    // value, and links into its owner's field list. Optional fields do not fail
    // Struct::Load when absent from the descriptor (value stays 0).
    class FieldBase
    {
    public:
        FieldBase(Struct* owner, const char* name, FieldKind kind, bool optional = false);

        const char* Name() const { return m_name; }
        FieldKind Kind() const { return m_kind; }
        bool Optional() const { return m_optional; }
        uint64_t Raw() const { return m_value; }
        void SetRaw(uint64_t value) { m_value = value; }
        FieldBase* Next() const { return m_next; }

    private:
        friend class Struct;
        const char* m_name;
        FieldKind m_kind;
        bool m_optional;
        uint64_t m_value;
        FieldBase* m_next;
    };

    // Base for a Data type. Holds the descriptor type name and the intrusive list
    // of self-registered fields. Non-copyable (fields hold owner-relative links).
    class Struct
    {
    public:
        Struct(const Struct&) = delete;
        Struct& operator=(const Struct&) = delete;

        const char* TypeName() const { return m_typeName; }

        // Reads every registered field at 'address' using descriptor offsets.
        // Returns false if any field is missing from the descriptor or unreadable.
        bool Load(const Target& target, uint64_t address)
        {
            for (FieldBase* field = m_head; field != nullptr; field = field->Next())
            {
                uint64_t value = 0;
                bool ok = false;
                switch (field->Kind())
                {
                case FieldKind::Ptr:
                    ok = target.TryReadFieldPointer(address, m_typeName, field->Name(), value);
                    break;
                case FieldKind::U64:
                    ok = target.TryReadFieldUInt64(address, m_typeName, field->Name(), value);
                    break;
                case FieldKind::U32:
                {
                    uint32_t value32 = 0;
                    ok = target.TryReadFieldUInt32(address, m_typeName, field->Name(), value32);
                    value = value32;
                    break;
                }
                case FieldKind::Addr:
                    ok = target.TryGetFieldAddress(address, m_typeName, field->Name(), value);
                    break;
                }

                if (!ok && !field->Optional())
                {
                    return false;
                }
                field->SetRaw(ok ? value : 0);
            }
            return true;
        }

    protected:
        explicit Struct(const char* typeName) : m_typeName(typeName), m_head(nullptr) {}

    private:
        friend class FieldBase;
        void Register(FieldBase* field) { field->m_next = m_head; m_head = field; }

        const char* m_typeName;
        FieldBase* m_head;
    };

    inline FieldBase::FieldBase(Struct* owner, const char* name, FieldKind kind, bool optional)
        : m_name(name), m_kind(kind), m_optional(optional), m_value(0), m_next(nullptr)
    {
        owner->Register(this);
    }

    // Typed field members. Implicit conversion gives ergonomic value access.
    class Ptr : public FieldBase
    {
    public:
        Ptr(Struct* owner, const char* name, bool optional = false) : FieldBase(owner, name, FieldKind::Ptr, optional) {}
        operator uint64_t() const { return Raw(); }
    };

    class U64 : public FieldBase
    {
    public:
        U64(Struct* owner, const char* name, bool optional = false) : FieldBase(owner, name, FieldKind::U64, optional) {}
        operator uint64_t() const { return Raw(); }
    };

    class U32 : public FieldBase
    {
    public:
        U32(Struct* owner, const char* name, bool optional = false) : FieldBase(owner, name, FieldKind::U32, optional) {}
        operator uint32_t() const { return (uint32_t)Raw(); }
    };

    // Address of an inline field/array (== managed [FieldAddress]).
    class Addr : public FieldBase
    {
    public:
        Addr(Struct* owner, const char* name, bool optional = false) : FieldBase(owner, name, FieldKind::Addr, optional) {}
        operator uint64_t() const { return Raw(); }
    };

    // Declare a field member whose descriptor field name equals the member name.
    // CDAC_OPT_* variants do not fail Load() when the field is absent (value 0).
    #define CDAC_PTR(name)  ::cdac::data::Ptr  name { this, #name };
    #define CDAC_U64(name)  ::cdac::data::U64  name { this, #name };
    #define CDAC_U32(name)  ::cdac::data::U32  name { this, #name };
    #define CDAC_ADDR(name) ::cdac::data::Addr name { this, #name };
    #define CDAC_OPT_PTR(name) ::cdac::data::Ptr name { this, #name, true };
}
}

#endif // CDACLITE_DATATYPE_H
