#include "internal.h"

uint32_t align_to(uint32_t val, uint32_t align)
{
    assert(align != 0);
    return (val + (align - 1)) & ~(align - 1);
}

// Brian Kernighan's algorithm for counting bits
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

bool advance_output_stream(uint8_t** data, size_t* data_len, size_t b)
{
    return advance_stream((uint8_t const**)data, data_len, b);
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

// This is a little-endian format in the physical form.
static bool write_le(uint8_t** data, size_t* data_len, uint64_t o, size_t o_size_to_write)
{
    assert(data != NULL && data_len != NULL);
    if (*data_len < o_size_to_write)
        return false;

    uint8_t* d = *data;
    switch(o_size_to_write)
    {
    case 8:
        *d++ = (uint8_t)o;
        *d++ = (uint8_t)(o >> 8);
        *d++ = (uint8_t)(o >> 16);
        *d++ = (uint8_t)(o >> 24);
        *d++ = (uint8_t)(o >> 32);
        *d++ = (uint8_t)(o >> 40);
        *d++ = (uint8_t)(o >> 48);
        *d++ = (uint8_t)(o >> 56);
        break;
    case 4:
        *d++ = (uint8_t)o;
        *d++ = (uint8_t)(o >> 8);
        *d++ = (uint8_t)(o >> 16);
        *d++ = (uint8_t)(o >> 24);
        break;
    case 2:
        *d++ = (uint8_t)o;
        *d++ = (uint8_t)(o >> 8);
        break;
    case 1:
        *d++ = (uint8_t)o;
        break;
    default:
        return false;
    }
    *data = d;
    *data_len -= o_size_to_write;
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

bool write_u8(uint8_t** data, size_t* data_len, uint8_t o)
{
    return write_le(data, data_len, o, sizeof(o));
}

bool write_i8(uint8_t** data, size_t* data_len, int8_t o)
{
    return write_le(data, data_len, o, sizeof(o));
}

bool write_u16(uint8_t** data, size_t* data_len, uint16_t o)
{
    return write_le(data, data_len, o, sizeof(o));
}

bool write_i16(uint8_t** data, size_t* data_len, int16_t o)
{
    return write_le(data, data_len, o, sizeof(o));
}

bool write_u32(uint8_t** data, size_t* data_len, uint32_t o)
{
    return write_le(data, data_len, o, sizeof(o));
}

bool write_i32(uint8_t** data, size_t* data_len, int32_t o)
{
    return write_le(data, data_len, o, sizeof(o));
}

bool write_u64(uint8_t** data, size_t* data_len, uint64_t o)
{
    return write_le(data, data_len, o, sizeof(o));
}

bool write_i64(uint8_t** data, size_t* data_len, int64_t o)
{
    return write_le(data, data_len, o, sizeof(o));
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

enum {
    SIGN_MASK_ONEBYTE  = 0xffffffc0,        // Mask the same size as the missing bits.
    SIGN_MASK_TWOBYTE  = 0xffffe000,        // Mask the same size as the missing bits.
    SIGN_MASK_FOURBYTE = 0xf0000000,        // Mask the same size as the missing bits.
};

// II.23.2
// This is a big-endian format in the physical form.
bool decompress_i32(uint8_t const** data, size_t* data_len, int32_t* o)
{
    uint32_t unsigned_value;
    size_t original_data_len = *data_len;
    if (!decompress_u32(data, data_len, &unsigned_value))
        return false;

    bool is_signed = (unsigned_value & 1) != 0;
    unsigned_value >>= 1;
    if (is_signed)
    {
        switch (original_data_len - *data_len)
        {
            // Sign extend the value based on the number of bytes used.
            case 1:
                unsigned_value |= SIGN_MASK_ONEBYTE;
                break;
            case 2:
                unsigned_value |= SIGN_MASK_TWOBYTE;
                break;
            case 4:
                unsigned_value |= SIGN_MASK_FOURBYTE;
                break;
            default:
                assert(false);
                return false;
        }
    }
    *o = (int32_t)unsigned_value;
    return true;
}

// II.23.2
// This is a big-endian format in the physical form.
bool compress_u32(uint32_t data, uint8_t* compressed, size_t* compressed_len)
{
    assert(compressed != NULL && compressed_len != NULL);
    if (data <= 0x7f)
    {
        if (*compressed_len < 1)
            return false;
        compressed[0] = (uint8_t)data;
        *compressed_len = 1;
    }
    else if (data <= 0x3fff)
    {
        if (*compressed_len < 2)
            return false;
        compressed[0] = (uint8_t)((data >> 8) | 0x80);
        compressed[1] = (uint8_t)data;
        *compressed_len = 2;
    }
    else if (data <= 0x1fffffff)
    {
        if (*compressed_len < 4)
            return false;
        compressed[0] = (uint8_t)((data >> 24) | 0xc0);
        compressed[1] = (uint8_t)(data >> 16);
        compressed[2] = (uint8_t)(data >> 8);
        compressed[3] = (uint8_t)data;
        *compressed_len = 4;
    }
    else
    {
        return false;
    }
    return true;
}
