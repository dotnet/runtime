#include "internal.h"

uint32_t align_to(uint32_t val, uint32_t align)
{
    assert(align != 0);
    return (val + (align - 1)) & ~(align - 1);
}

// Brian Kernighan�s algorithm for counting bits
size_t count_set_bits(uint64_t val)
{
    size_t count = 0;
    while (val)
    {
        val = val & (val - 1);
        count++;
    }
    return count;
}

bool advance_stream(uint8_t const** data, size_t* data_len, size_t b)
{
    assert(data != NULL && data_len != NULL);
    if (*data_len < b)
        return false;

    *data += b;
    *data_len -= b;
    return true;
}

// This is a little-endian format in the physical form.
static bool read_le(uint8_t const** data, size_t* data_len, void* o, size_t o_size)
{
    assert(data != NULL && data_len != NULL && o != NULL);
    if (*data_len < o_size)
        return false;

    uint64_t integer;
    uint8_t const* d = *data;
    switch (o_size)
    {
    case 8:
        integer = (uint64_t)*d++;
        integer |= (uint64_t)*d++ << 8;
        integer |= (uint64_t)*d++ << 16;
        integer |= (uint64_t)*d++ << 24;
        integer |= (uint64_t)*d++ << 32;
        integer |= (uint64_t)*d++ << 40;
        integer |= (uint64_t)*d++ << 48;
        integer |= (uint64_t)*d++ << 56;
        break;
    case 4:
        integer = (uint64_t)*d++;
        integer |= (uint64_t)*d++ << 8;
        integer |= (uint64_t)*d++ << 16;
        integer |= (uint64_t)*d++ << 24;
        break;
    case 2:
        integer = (uint64_t)*d++;
        integer |= (uint64_t)*d++ << 8;
        break;
    case 1:
        integer = (uint64_t)*d++;
        break;
    default:
        return false;
    }

    memcpy(o, &integer, o_size);
    *data = d;
    *data_len -= o_size;
    return true;
}

bool read_u8(uint8_t const** data, size_t* data_len, uint8_t* o)
{
    return read_le(data, data_len, o, sizeof(*o));
}

bool read_i8(uint8_t const** data, size_t* data_len, int8_t* o)
{
    return read_le(data, data_len, o, sizeof(*o));
}

bool read_u16(uint8_t const** data, size_t* data_len, uint16_t* o)
{
    return read_le(data, data_len, o, sizeof(*o));
}

bool read_i16(uint8_t const** data, size_t* data_len, int16_t* o)
{
    return read_le(data, data_len, o, sizeof(*o));
}

bool read_u32(uint8_t const** data, size_t* data_len, uint32_t* o)
{
    return read_le(data, data_len, o, sizeof(*o));
}

bool read_i32(uint8_t const** data, size_t* data_len, int32_t* o)
{
    return read_le(data, data_len, o, sizeof(*o));
}

bool read_u64(uint8_t const** data, size_t* data_len, uint64_t* o)
{
    return read_le(data, data_len, o, sizeof(*o));
}

bool read_i64(uint8_t const** data, size_t* data_len, int64_t* o)
{
    return read_le(data, data_len, o, sizeof(*o));
}

// II.23.2
// This is a big-endian format in the physical form.
bool decompress_u32(uint8_t const** data, size_t* data_len, uint32_t* o)
{
    assert(data != NULL && data_len != NULL && o != NULL);
    uint8_t const* s = *data;
    assert(s != NULL);

    uint32_t val;
    switch (*s & 0xc0)
    {
    case 0xc0:
        if (*data_len < 4)
            return false;

        *data_len -= 4;
        val = ((*s++ & 0x1f) << 24);
        val |= (*s++ << 16);
        val |= (*s++ << 8);
        val |= *s++;
        break;

    case 0x80:
        if (*data_len < 2)
            return false;

        *data_len -= 2;
        val = ((*s++ & 0x3f) << 8);
        val |= *s++;
        break;

    default:
        if (*data_len < 1)
            return false;

        *data_len -= 1;
        val = *s++;
        break;
    }

    *o = val;
    *data = s;
    return true;
}
