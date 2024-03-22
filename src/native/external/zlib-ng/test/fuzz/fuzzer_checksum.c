#include <stdio.h>
#include <assert.h>

#include "zbuild.h"
#ifdef ZLIB_COMPAT
#  include "zlib.h"
#else
#  include "zlib-ng.h"
#endif

int LLVMFuzzerTestOneInput(const uint8_t *data, size_t dataLen) {
    uint32_t crc0 = PREFIX(crc32)(0L, NULL, 0);
    uint32_t crc1 = crc0;
    uint32_t crc2 = crc0;
    uint32_t adler0 = PREFIX(adler32)(0L, NULL, 0);
    uint32_t adler1 = adler0;
    uint32_t adler2 = adler0;
    uint32_t combine1, combine2;
    /* Checksum with a buffer of size equal to the first byte in the input. */
    uint32_t buffSize = data[0];
    uint32_t offset = 0;
    uint32_t op;

    /* Discard inputs larger than 1Mb. */
    static size_t kMaxSize = 1024 * 1024;
    if (dataLen < 1 || dataLen > kMaxSize)
        return 0;

    /* Make sure the buffer has at least a byte. */
    if (buffSize == 0)
        ++buffSize;

    /* CRC32 */
    op = PREFIX(crc32_combine_gen)(buffSize);
    for (offset = 0; offset + buffSize <= dataLen; offset += buffSize) {
        uint32_t crc3 = PREFIX(crc32_z)(crc0, data + offset, buffSize);
        uint32_t crc4 = PREFIX(crc32_combine_op)(crc1, crc3, op);
        crc1 = PREFIX(crc32_z)(crc1, data + offset, buffSize);
        assert(crc1 == crc4);
        Z_UNUSED(crc1);
        Z_UNUSED(crc4);
    }
    crc1 = PREFIX(crc32_z)(crc1, data + offset, dataLen % buffSize);

    crc2 = PREFIX(crc32_z)(crc2, data, dataLen);

    assert(crc1 == crc2);
    Z_UNUSED(crc1);
    Z_UNUSED(crc2);
    combine1 = PREFIX(crc32_combine)(crc1, crc2, (z_off_t)dataLen);
    combine2 = PREFIX(crc32_combine)(crc1, crc1, (z_off_t)dataLen);
    assert(combine1 == combine2);

    /* Fast CRC32 combine. */
    op = PREFIX(crc32_combine_gen)((z_off_t)dataLen);
    combine1 = PREFIX(crc32_combine_op)(crc1, crc2, op);
    combine2 = PREFIX(crc32_combine_op)(crc2, crc1, op);
    assert(combine1 == combine2);
    combine1 = PREFIX(crc32_combine)(crc1, crc2, (z_off_t)dataLen);
    combine2 = PREFIX(crc32_combine_op)(crc2, crc1, op);
    assert(combine1 == combine2);

    /* Adler32 */
    for (offset = 0; offset + buffSize <= dataLen; offset += buffSize)
        adler1 = PREFIX(adler32_z)(adler1, data + offset, buffSize);
    adler1 = PREFIX(adler32_z)(adler1, data + offset, dataLen % buffSize);

    adler2 = PREFIX(adler32_z)(adler2, data, dataLen);

    assert(adler1 == adler2);
    Z_UNUSED(adler1);
    Z_UNUSED(adler2);
    combine1 = PREFIX(adler32_combine)(adler1, adler2, (z_off_t)dataLen);
    combine2 = PREFIX(adler32_combine)(adler1, adler1, (z_off_t)dataLen);
    assert(combine1 == combine2);
    Z_UNUSED(combine1);
    Z_UNUSED(combine2);

    /* This function must return 0. */
    return 0;
}
