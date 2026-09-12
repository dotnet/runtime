// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// json.h
//
// A tiny, dependency-free JSON parser used to read the in-memory data
// descriptor. It supports the standard JSON grammar plus `//` and `/* */`
// comments (the "jsonc" superset used by the data descriptor docs).
//*****************************************************************************

#ifndef CDACLITE_JSON_H
#define CDACLITE_JSON_H

#include <stdint.h>
#include <string>
#include <vector>
#include <map>

namespace cdac
{
namespace json
{
    enum class Type
    {
        Null,
        Boolean,
        Number,
        String,
        Array,
        Object
    };

    class Value
    {
    public:
        Type type = Type::Null;

        bool boolean = false;

        // Numbers keep both the raw text and the parsed forms so callers can
        // pick the interpretation they need.
        std::string rawNumber;
        double number = 0.0;
        int64_t integer = 0;
        bool isInteger = false;

        std::string string;
        std::vector<Value> array;
        std::map<std::string, Value> object;

        bool IsNull() const { return type == Type::Null; }
        bool IsObject() const { return type == Type::Object; }
        bool IsArray() const { return type == Type::Array; }
        bool IsString() const { return type == Type::String; }
        bool IsNumber() const { return type == Type::Number; }

        // Returns the child value for an object key, or nullptr if absent / not an object.
        const Value* Find(const std::string& key) const;

        // Interprets this value as an unsigned 64-bit integer. Accepts JSON numbers,
        // decimal strings, and hex strings ("0x..."). Returns false otherwise.
        bool TryGetUInt64(uint64_t& out) const;
    };

    // Parses the entire text as a single JSON value. On failure returns false and
    // fills 'error' with a short description.
    bool Parse(const char* text, size_t length, Value& root, std::string& error);
}
}

#endif // CDACLITE_JSON_H
