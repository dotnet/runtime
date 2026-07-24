// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// json.cpp
//
// Implementation of the tiny JSON parser declared in json.h.
//*****************************************************************************

#include "json.h"

#include <stdlib.h>
#include <string.h>

namespace cdac
{
namespace json
{
    const Value* Value::Find(const std::string& key) const
    {
        if (type != Type::Object)
        {
            return nullptr;
        }
        std::map<std::string, Value>::const_iterator it = object.find(key);
        return (it == object.end()) ? nullptr : &it->second;
    }

    bool Value::TryGetUInt64(uint64_t& out) const
    {
        if (type == Type::Number)
        {
            if (isInteger)
            {
                out = (uint64_t)integer;
                return true;
            }
            if (number >= 0.0)
            {
                out = (uint64_t)number;
                return true;
            }
            return false;
        }

        if (type == Type::String)
        {
            const char* text = string.c_str();
            char* end = nullptr;
            int base = 10;
            if (string.size() > 2 && text[0] == '0' && (text[1] == 'x' || text[1] == 'X'))
            {
                base = 16;
            }
            errno = 0;
            unsigned long long parsed = strtoull(text, &end, base);
            if (errno != 0 || end == text || *end != '\0')
            {
                return false;
            }
            out = (uint64_t)parsed;
            return true;
        }

        return false;
    }

    namespace
    {
        class Parser
        {
        private:
            const char* m_cur;
            const char* m_end;
            std::string m_error;

        public:
            Parser(const char* text, size_t length)
                : m_cur(text), m_end(text + length)
            {
            }

            const std::string& Error() const { return m_error; }

            bool ParseDocument(Value& root)
            {
                SkipWhitespace();
                if (!ParseValue(root))
                {
                    return false;
                }
                SkipWhitespace();
                if (m_cur != m_end)
                {
                    return Fail("trailing content after JSON value");
                }
                return true;
            }

        private:
            bool Fail(const char* message)
            {
                if (m_error.empty())
                {
                    m_error = message;
                }
                return false;
            }

            bool AtEnd() const { return m_cur >= m_end; }
            char Peek() const { return AtEnd() ? '\0' : *m_cur; }

            void SkipWhitespace()
            {
                while (!AtEnd())
                {
                    char c = *m_cur;
                    if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                    {
                        m_cur++;
                    }
                    else if (c == '/' && (m_cur + 1) < m_end && m_cur[1] == '/')
                    {
                        m_cur += 2;
                        while (!AtEnd() && *m_cur != '\n')
                        {
                            m_cur++;
                        }
                    }
                    else if (c == '/' && (m_cur + 1) < m_end && m_cur[1] == '*')
                    {
                        m_cur += 2;
                        while ((m_cur + 1) < m_end && !(m_cur[0] == '*' && m_cur[1] == '/'))
                        {
                            m_cur++;
                        }
                        if ((m_cur + 1) < m_end)
                        {
                            m_cur += 2;
                        }
                        else
                        {
                            m_cur = m_end;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            bool ParseValue(Value& value)
            {
                SkipWhitespace();
                if (AtEnd())
                {
                    return Fail("unexpected end of input");
                }

                char c = Peek();
                switch (c)
                {
                case '{':
                    return ParseObject(value);
                case '[':
                    return ParseArray(value);
                case '"':
                    value.type = Type::String;
                    return ParseString(value.string);
                case 't':
                case 'f':
                    return ParseBool(value);
                case 'n':
                    return ParseNull(value);
                default:
                    if (c == '-' || (c >= '0' && c <= '9'))
                    {
                        return ParseNumber(value);
                    }
                    return Fail("unexpected character");
                }
            }

            bool ParseObject(Value& value)
            {
                value.type = Type::Object;
                m_cur++; // consume '{'
                SkipWhitespace();
                if (Peek() == '}')
                {
                    m_cur++;
                    return true;
                }

                for (;;)
                {
                    SkipWhitespace();
                    if (Peek() != '"')
                    {
                        return Fail("expected object key");
                    }
                    std::string key;
                    if (!ParseString(key))
                    {
                        return false;
                    }
                    SkipWhitespace();
                    if (Peek() != ':')
                    {
                        return Fail("expected ':' after object key");
                    }
                    m_cur++; // consume ':'

                    Value child;
                    if (!ParseValue(child))
                    {
                        return false;
                    }
                    value.object[key] = child;

                    SkipWhitespace();
                    char c = Peek();
                    if (c == ',')
                    {
                        m_cur++;
                        continue;
                    }
                    if (c == '}')
                    {
                        m_cur++;
                        return true;
                    }
                    return Fail("expected ',' or '}' in object");
                }
            }

            bool ParseArray(Value& value)
            {
                value.type = Type::Array;
                m_cur++; // consume '['
                SkipWhitespace();
                if (Peek() == ']')
                {
                    m_cur++;
                    return true;
                }

                for (;;)
                {
                    Value child;
                    if (!ParseValue(child))
                    {
                        return false;
                    }
                    value.array.push_back(child);

                    SkipWhitespace();
                    char c = Peek();
                    if (c == ',')
                    {
                        m_cur++;
                        continue;
                    }
                    if (c == ']')
                    {
                        m_cur++;
                        return true;
                    }
                    return Fail("expected ',' or ']' in array");
                }
            }

            bool ParseString(std::string& out)
            {
                m_cur++; // consume opening quote
                out.clear();
                while (!AtEnd())
                {
                    char c = *m_cur++;
                    if (c == '"')
                    {
                        return true;
                    }
                    if (c == '\\')
                    {
                        if (AtEnd())
                        {
                            return Fail("unterminated escape");
                        }
                        char esc = *m_cur++;
                        switch (esc)
                        {
                        case '"': out.push_back('"'); break;
                        case '\\': out.push_back('\\'); break;
                        case '/': out.push_back('/'); break;
                        case 'b': out.push_back('\b'); break;
                        case 'f': out.push_back('\f'); break;
                        case 'n': out.push_back('\n'); break;
                        case 'r': out.push_back('\r'); break;
                        case 't': out.push_back('\t'); break;
                        case 'u':
                        {
                            if ((m_cur + 4) > m_end)
                            {
                                return Fail("truncated \\u escape");
                            }
                            unsigned int code = 0;
                            for (int i = 0; i < 4; i++)
                            {
                                char h = *m_cur++;
                                code <<= 4;
                                if (h >= '0' && h <= '9') code |= (unsigned)(h - '0');
                                else if (h >= 'a' && h <= 'f') code |= (unsigned)(h - 'a' + 10);
                                else if (h >= 'A' && h <= 'F') code |= (unsigned)(h - 'A' + 10);
                                else return Fail("invalid \\u escape");
                            }
                            // Emit as UTF-8 (BMP only; the descriptor uses ASCII keys/values).
                            if (code < 0x80)
                            {
                                out.push_back((char)code);
                            }
                            else if (code < 0x800)
                            {
                                out.push_back((char)(0xC0 | (code >> 6)));
                                out.push_back((char)(0x80 | (code & 0x3F)));
                            }
                            else
                            {
                                out.push_back((char)(0xE0 | (code >> 12)));
                                out.push_back((char)(0x80 | ((code >> 6) & 0x3F)));
                                out.push_back((char)(0x80 | (code & 0x3F)));
                            }
                            break;
                        }
                        default:
                            return Fail("invalid escape character");
                        }
                    }
                    else
                    {
                        out.push_back(c);
                    }
                }
                return Fail("unterminated string");
            }

            bool ParseNumber(Value& value)
            {
                const char* start = m_cur;
                bool isFloat = false;

                if (Peek() == '-')
                {
                    m_cur++;
                }
                while (!AtEnd())
                {
                    char c = *m_cur;
                    if (c >= '0' && c <= '9')
                    {
                        m_cur++;
                    }
                    else if (c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-')
                    {
                        isFloat = (c == '.' || c == 'e' || c == 'E') ? true : isFloat;
                        m_cur++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (m_cur == start)
                {
                    return Fail("invalid number");
                }

                value.type = Type::Number;
                value.rawNumber.assign(start, (size_t)(m_cur - start));

                value.number = strtod(value.rawNumber.c_str(), nullptr);
                if (!isFloat)
                {
                    char* end = nullptr;
                    errno = 0;
                    long long parsed = strtoll(value.rawNumber.c_str(), &end, 10);
                    if (errno == 0 && end != value.rawNumber.c_str() && *end == '\0')
                    {
                        value.integer = (int64_t)parsed;
                        value.isInteger = true;
                    }
                }
                return true;
            }

            bool ParseBool(Value& value)
            {
                if ((size_t)(m_end - m_cur) >= 4 && strncmp(m_cur, "true", 4) == 0)
                {
                    m_cur += 4;
                    value.type = Type::Boolean;
                    value.boolean = true;
                    return true;
                }
                if ((size_t)(m_end - m_cur) >= 5 && strncmp(m_cur, "false", 5) == 0)
                {
                    m_cur += 5;
                    value.type = Type::Boolean;
                    value.boolean = false;
                    return true;
                }
                return Fail("invalid literal");
            }

            bool ParseNull(Value& value)
            {
                if ((size_t)(m_end - m_cur) >= 4 && strncmp(m_cur, "null", 4) == 0)
                {
                    m_cur += 4;
                    value.type = Type::Null;
                    return true;
                }
                return Fail("invalid literal");
            }
        };
    }

    bool Parse(const char* text, size_t length, Value& root, std::string& error)
    {
        Parser parser(text, length);
        if (!parser.ParseDocument(root))
        {
            error = parser.Error();
            return false;
        }
        error.clear();
        return true;
    }
}
}
