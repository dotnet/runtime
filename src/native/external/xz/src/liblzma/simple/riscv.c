// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       riscv.c
/// \brief      Filter for 32-bit/64-bit little/big endian RISC-V binaries
///
/// This converts program counter relative addresses in function calls
/// (JAL, AUIPC+JALR), address calculation of functions and global
/// variables (AUIPC+ADDI), loads (AUIPC+load), and stores (AUIPC+store).
///
/// For AUIPC+inst2 pairs, the paired instruction checking is fairly relaxed.
/// The paired instruction opcode must only have its lowest two bits set,
/// meaning it will convert any paired instruction that is not a 16-bit
/// compressed instruction. This was shown to be enough to keep the number
/// of false matches low while improving code size and speed.
//
//  Authors:    Lasse Collin
//              Jia Tan
//
//  Special thanks:
//
//    - Chien Wong <m@xv97.com> provided a few early versions of RISC-V
//      filter variants along with test files and benchmark results.
//
//    - Igor Pavlov helped a lot in the filter design, getting it both
//      faster and smaller. The implementation here is still independently
//      written, not based on LZMA SDK.
//
///////////////////////////////////////////////////////////////////////////////

/*

RISC-V filtering
================

    RV32I and RV64I, possibly combined with extensions C, Zfh, F, D,
    and Q, are identical enough that the same filter works for both.

    The instruction encoding is always little endian, even on systems
    with big endian data access. Thus the same filter works for both
    endiannesses.

    The following instructions have program counter relative
    (pc-relative) behavior:

JAL
---

    JAL is used for function calls (including tail calls) and
    unconditional jumps within functions. Jumps within functions
    aren't useful to filter because the absolute addresses often
    appear only once or at most a few times. Tail calls and jumps
    within functions look the same to a simple filter so neither
    are filtered, that is, JAL x0 is ignored (the ABI name of the
    register x0 is "zero").

    Almost all calls store the return address to register x1 (ra)
    or x5 (t0). To reduce false matches when the filter is applied
    to non-code data, only the JAL instructions that use x1 or x5
    are converted. JAL has pc-relative range of +/-1 MiB so longer
    calls and jumps need another method (AUIPC+JALR).

C.J and C.JAL
-------------

    C.J and C.JAL have pc-relative range of +/-2 KiB.

    C.J is for tail calls and jumps within functions and isn't
    filtered for the reasons mentioned for JAL x0.

    C.JAL is an RV32C-only instruction. Its encoding overlaps with
    RV64C-only C.ADDIW which is a common instruction. So if filtering
    C.JAL was useful (it wasn't tested) then a separate filter would
    be needed for RV32 and RV64. Also, false positives would be a
    significant problem when the filter is applied to non-code data
    because C.JAL needs only five bits to match. Thus, this filter
    doesn't modify C.JAL instructions.

BEQ, BNE, BLT, BGE, BLTU, BGEU, C.BEQZ, and C.BNEZ
--------------------------------------------------

    These are conditional branches with pc-relative range
    of +/-4 KiB (+/-256 B for C.*). The absolute addresses often
    appear only once and very short distances are the most common,
    so filtering these instructions would make compression worse.

AUIPC with rd != x0
-------------------

    AUIPC is paired with a second instruction (inst2) to do
    pc-relative jumps, calls, loads, stores, and for taking
    an address of a symbol. AUIPC has a 20-bit immediate and
    the possible inst2 choices have a 12-bit immediate.

    AUIPC stores pc + 20-bit signed immediate to a register.
    The immediate encodes a multiple of 4 KiB so AUIPC itself
    has a pc-relative range of +/-2 GiB. AUIPC does *NOT* set
    the lowest 12 bits of the result to zero! This means that
    the 12-bit immediate in inst2 cannot just include the lowest
    12 bits of the absolute address as is; the immediate has to
    compensate for the lowest 12 bits that AUIPC copies from the
    program counter. This means that a good filter has to convert
    not only AUIPC but also the paired inst2.

    A strict filter would focus on filtering the following
    AUIPC+inst2 pairs:

      - AUIPC+JALR: Function calls, including tail calls.

      - AUIPC+ADDI: Calculating the address of a function
        or a global variable.

      - AUIPC+load/store from the base instruction sets
        (RV32I, RV64I) or from the floating point extensions
        Zfh, F, D, and Q:
          * RV32I: LB, LH, LW, LBU, LHU, SB, SH, SW
          * RV64I has also: LD, LWU, SD
          * Zfh: FLH, FSH
          * F: FLW, FSW
          * D: FLD, FSD
          * Q: FLQ, FSQ

    NOTE: AUIPC+inst2 can only be a pair if AUIPC's rd specifies
    the same register as inst2's rs1.

    Instead of strictly accepting only the above instructions as inst2,
    this filter uses a much simpler condition: the lowest two bits of
    inst2 must be set, that is, inst2 must not be a 16-bit compressed
    instruction. So this will accept all 32-bit and possible future
    extended instructions as a pair to AUIPC if the bits in AUIPC's
    rd [11:7] match the bits [19:15] in inst2 (the bits that I-type and
    S-type instructions use for rs1). Testing showed that this relaxed
    condition for inst2 did not consistently or significantly affect
    compression ratio but it reduced code size and improved speed.

    Additionally, the paired instruction is always treated as an I-type
    instruction. The S-type instructions used by stores (SB, SH, SW,
    etc.) place the lowest 5 bits of the immediate in a different
    location than I-type instructions. AUIPC+store pairs are less
    common than other pairs, and testing showed that the extra
    code required to handle S-type instructions was not worth the
    compression ratio gained.

    AUIPC+inst2 don't necessarily appear sequentially next to each
    other although very often they do. Especially AUIPC+JALR are
    sequential as that may allow instruction fusion in processors
    (and perhaps help branch prediction as a fused AUIPC+JALR is
    a direct branch while JALR alone is an indirect branch).

    Clang 16 can generate code where AUIPC+inst2 is split:

      - AUIPC is outside a loop and inst2 (load/store) is inside
        the loop. This way the AUIPC instruction needs to be
        executed only once.

      - Load-modify-store may have AUIPC for the load and the same
        AUIPC-result is used for the store too. This may get combined
        with AUIPC being outside the loop.

      - AUIPC is before a conditional branch and inst2 is hundreds
        of bytes away at the branch target.

      - Inner and outer pair:

            auipc   a1,0x2f
            auipc   a2,0x3d
            ld      a2,-500(a2)
            addi    a1,a1,-233

      - Many split pairs with an untaken conditional branch between:

            auipc   s9,0x1613   # Pair 1
            auipc   s4,0x1613   # Pair 2
            auipc   s6,0x1613   # Pair 3
            auipc   s10,0x1613  # Pair 4
            beqz    a5,a3baae
            ld      a0,0(a6)
            ld      a6,246(s9)  # Pair 1
            ld      a1,250(s4)  # Pair 2
            ld      a3,254(s6)  # Pair 3
            ld      a4,258(s10) # Pair 4

    It's not possible to find all split pairs in a filter like this.
    At least in 2024, simple sequential pairs are 99 % of AUIPC uses
    so filtering only such pairs gives good results and makes the
    filter simpler. However, it's possible that future compilers will
    produce different code where sequential pairs aren't as common.

    This filter doesn't convert AUIPC instructions alone because:

    (1) The conversion would be off-by-one (or off-by-4096) half the
        time because the lowest 12 bits from inst2 (inst2_imm12)
        aren't known. We only know that the absolute address is
        pc + AUIPC_imm20 + [-2048, +2047] but there is no way to
        know the exact 4096-byte multiple (or 4096 * n + 2048):
        there are always two possibilities because AUIPC copies
        the 12 lowest bits from pc instead of zeroing them.

        NOTE: The sign-extension of inst2_imm12 adds a tiny bit
        of extra complexity to AUIPC math in general but it's not
        the reason for this problem. The sign-extension only changes
        the relative position of the pc-relative 4096-byte window.

    (2) Matching AUIPC instruction alone requires only seven bits.
        When the filter is applied to non-code data, that leads
        to many false positives which make compression worse.
        As long as most AUIPC+inst2 pairs appear as two consecutive
        instructions, converting only such pairs gives better results.

    In assembly, AUIPC+inst2 tend to look like this:

        # Call:
        auipc   ra, 0x12345
        jalr    ra, -42(ra)

        # Tail call:
        auipc   t1, 0x12345
        jalr    zero, -42(t1)

        # Getting the absolute address:
        auipc   a0, 0x12345
        addi    a0, a0, -42

        # rd of inst2 isn't necessarily the same as rs1 even
        # in cases where there is no reason to preserve rs1.
        auipc   a0, 0x12345
        addi    a1, a0, -42

    As of 2024, 16-bit instructions from the C extension don't
    appear as inst2. The RISC-V psABI doesn't list AUIPC+C.* as
    a linker relaxation type explicitly but it's not disallowed
    either. Usefulness is limited as most of the time the lowest
    12 bits won't fit in a C instruction. This filter doesn't
    support AUIPC+C.* combinations because this makes the filter
    simpler, there are no test files, and it hopefully will never
    be needed anyway.

    (Compare AUIPC to ARM64 where ADRP does set the lowest 12 bits
    to zero. The paired instruction has the lowest 12 bits of the
    absolute address as is in a zero-extended immediate. Thus the
    ARM64 filter doesn't need to care about the instructions that
    are paired with ADRP. An off-by-4096 issue can still occur if
    the code section isn't aligned with the filter's start offset.
    It's not a problem with standalone ELF files but Windows PE
    files need start_offset=3072 for best results. Also, a .tar
    stores files with 512-byte alignment so most of the time it
    won't be the best for ARM64.)

AUIPC with rd == x0
-------------------

    AUIPC instructions with rd=x0 are reserved for HINTs in the base
    instruction set. Such AUIPC instructions are never filtered.

    As of January 2024, it seems likely that AUIPC with rd=x0 will
    be used for landing pads (pseudoinstruction LPAD). LPAD is used
    to mark valid targets for indirect jumps (for JALR), for example,
    beginnings of functions. The 20-bit immediate in LPAD instruction
    is a label, not a pc-relative address. Thus it would be
    counterproductive to convert AUIPC instructions with rd=x0.

    Often the next instruction after LPAD won't have rs1=x0 and thus
    the filtering would be skipped for that reason alone. However,
    it's not good to rely on this. For example, consider a function
    that begins like this:

        int foo(int i)
        {
            if (i <= 234) {
                ...
            }

    A compiler may generate something like this:

        lpad    0x54321
        li      a5, 234
        bgt     a0, a5, .L2

    Converting the pseudoinstructions to raw instructions:

        auipc   x0, 0x54321
        addi    x15, x0, 234
        blt     x15, x10, .L2

    In this case the filter would undesirably convert the AUIPC+ADDI
    pair if the filter didn't explicitly skip AUIPC instructions
    that have rd=x0.

*/


#include "simple_private.h"


// This checks two conditions at once:
//    - AUIPC rd == inst2 rs1.
//    - inst2 opcode has the lowest two bits set.
//
// The 8 bit left shift aligns the rd of AUIPC with the rs1 of inst2.
// By XORing the registers, any non-zero value in those bits indicates the
// registers are not equal and thus not an AUIPC pair. Subtracting 3 from
// inst2 will zero out the first two opcode bits only when they are set.
// The mask tests if any of the register or opcode bits are set (and thus
// not an AUIPC pair).
//
// Alternative expression: (((((auipc) << 8) ^ (inst2)) & 0xF8003) != 3)
#define NOT_AUIPC_PAIR(auipc, inst2) \
	((((auipc) << 8) ^ ((inst2) - 3)) & 0xF8003)

// This macro checks multiple conditions:
//   (1) AUIPC rd [11:7] == x2 (special rd value).
//   (2) AUIPC bits 12 and 13 set (the lowest two opcode bits of packed inst2).
//   (3) inst2_rs1 doesn't equal x0 or x2 because the opposite
//       conversion is only done when
//       auipc_rd != x0 &&
//       auipc_rd != x2 &&
//       auipc_rd == inst2_rs1.
//
// The left-hand side takes care of (1) and (2).
//   (a) The lowest 7 bits are already known to be AUIPC so subtracting 0x17
//       makes those bits zeros.
//   (b) If AUIPC rd equals x2, subtracting 0x100 makes bits [11:7] zeros.
//       If rd doesn't equal x2, then there will be at least one non-zero bit
//       and the next step (c) is irrelevant.
//   (c) If the lowest two opcode bits of the packed inst2 are set in [13:12],
//       then subtracting 0x3000 will make those bits zeros. Otherwise there
//       will be at least one non-zero bit.
//
// The shift by 18 removes the high bits from the final '>=' comparison and
// ensures that any non-zero result will be larger than any possible result
// from the right-hand side of the comparison. The cast ensures that the
// left-hand side didn't get promoted to a larger type than uint32_t.
//
// On the right-hand side, inst2_rs1 & 0x1D will be non-zero as long as
// inst2_rs1 is not x0 or x2.
//
// The final '>=' comparison will make the expression true if:
//   - The subtraction caused any bits to be set (special AUIPC rd value not
//     used or inst2 opcode bits not set). (non-zero >= non-zero or 0)
//   - The subtraction did not cause any bits to be set but inst2_rs1 was
//     x0 or x2. (0 >= 0)
#define NOT_SPECIAL_AUIPC(auipc, inst2_rs1) \
	((uint32_t)(((auipc) - 0x3117) << 18) >= ((inst2_rs1) & 0x1D))


// The encode and decode functions are split for this filter because of the
// AUIPC+inst2 filtering. This filter design allows a decoder-only
// implementation to be smaller than alternative designs.

#ifdef HAVE_ENCODER_RISCV
static size_t
riscv_encode(void *simple lzma_attribute((__unused__)),
		uint32_t now_pos,
		bool is_encoder lzma_attribute((__unused__)),
		uint8_t *buffer, size_t size)
{
	// Avoid using i + 8 <= size in the loop condition.
	//
	// NOTE: If there is a JAL in the last six bytes of the stream, it
	// won't be converted. This is intentional to keep the code simpler.
	if (size < 8)
		return 0;

	size -= 8;

	size_t i;

	// The loop is advanced by 2 bytes every iteration since the
	// instruction stream may include 16-bit instructions (C extension).
	for (i = 0; i <= size; i += 2) {
		uint32_t inst = buffer[i];

		if (inst == 0xEF) {
			// JAL
			const uint32_t b1 = buffer[i + 1];

			// Only filter rd=x1(ra) and rd=x5(t0).
			if ((b1 & 0x0D) != 0)
				continue;

			// The 20-bit immediate is in four pieces.
			// The encoder stores it in big endian form
			// since it improves compression slightly.
			const uint32_t b2 = buffer[i + 2];
			const uint32_t b3 = buffer[i + 3];
			const uint32_t pc = now_pos + (uint32_t)i;

// The following chart shows the highest three bytes of JAL, focusing on
// the 20-bit immediate field [31:12]. The first row of numbers is the
// bit position in a 32-bit little endian instruction. The second row of
// numbers shows the order of the immediate field in a J-type instruction.
// The last row is the bit number in each byte.
//
// To determine the amount to shift each bit, subtract the value in
// the last row from the value in the second last row. If the number
// is positive, shift left. If negative, shift right.
//
// For example, at the rightmost side of the chart, the bit 4 in b1 is
// the bit 12 of the address. Thus that bit needs to be shifted left
// by 12 - 4 = 8 bits to put it in the right place in the addr variable.
//
// NOTE: The immediate of a J-type instruction holds bits [20:1] of
// the address. The bit [0] is always 0 and not part of the immediate.
//
// |          b3             |          b2             |          b1         |
// | 31 30 29 28 27 26 25 24 | 23 22 21 20 19 18 17 16 | 15 14 13 12 x x x x |
// | 20 10  9  8  7  6  5  4 |  3  2  1 11 19 18 17 16 | 15 14 13 12 x x x x |
// |  7  6  5  4  3  2  1  0 |  7  6  5  4  3  2  1  0 |  7  6  5  4 x x x x |

			uint32_t addr = ((b1 & 0xF0) << 8)
					| ((b2 & 0x0F) << 16)
					| ((b2 & 0x10) << 7)
					| ((b2 & 0xE0) >> 4)
					| ((b3 & 0x7F) << 4)
					| ((b3 & 0x80) << 13);

			addr += pc;

			buffer[i + 1] = (uint8_t)((b1 & 0x0F)
					| ((addr >> 13) & 0xF0));

			buffer[i + 2] = (uint8_t)(addr >> 9);
			buffer[i + 3] = (uint8_t)(addr >> 1);

			// The "-2" is included because the for-loop will
			// always increment by 2. In this case, we want to
			// skip an extra 2 bytes since we used 4 bytes
			// of input.
			i += 4 - 2;

		} else if ((inst & 0x7F) == 0x17) {
			// AUIPC
			inst |= (uint32_t)buffer[i + 1] << 8;
			inst |= (uint32_t)buffer[i + 2] << 16;
			inst |= (uint32_t)buffer[i + 3] << 24;

			// Branch based on AUIPC's rd. The bitmask test does
			// the same thing as this:
			//
			//     const uint32_t auipc_rd = (inst >> 7) & 0x1F;
			//     if (auipc_rd != 0 && auipc_rd != 2) {
 			if (inst & 0xE80) {
				// AUIPC's rd doesn't equal x0 or x2.

				// Check if AUIPC+inst2 are a pair.
				uint32_t inst2 = read32le(buffer + i + 4);

				if (NOT_AUIPC_PAIR(inst, inst2)) {
					// The NOT_AUIPC_PAIR macro allows
					// a false AUIPC+AUIPC pair if the
					// bits [19:15] (where rs1 would be)
					// in the second AUIPC match the rd
					// of the first AUIPC.
					//
					// We must skip enough forward so
					// that the first two bytes of the
					// second AUIPC cannot get converted.
					// Such a conversion could make the
					// current pair become a valid pair
					// which would desync the decoder.
					//
					// Skipping six bytes is enough even
					// though the above condition looks
					// at the lowest four bits of the
					// buffer[i + 6] too. This is safe
					// because this filter never changes
					// those bits if a conversion at
					// that position is done.
					i += 6 - 2;
					continue;
				}

				// Convert AUIPC+inst2 to a special format:
				//
				//   - The lowest 7 bits [6:0] retain the
				//     AUIPC opcode.
				//
				//   - The rd [11:7] is set to x2(sp). x2 is
				//     used as the stack pointer so AUIPC with
				//     rd=x2 should be very rare in real-world
				//     executables.
				//
				//   - The remaining 20 bits [31:12] (that
				//     normally hold the pc-relative immediate)
				//     are used to store the lowest 20 bits of
				//     inst2. That is, the 12-bit immediate of
				//     inst2 is not included.
				//
				//   - The location of the original inst2 is
				//     used to store the 32-bit absolute
				//     address in big endian format. Compared
				//     to the 20+12-bit split encoding, this
				//     results in a longer uninterrupted
				//     sequence of identical common bytes
				//     when the same address is referred
				//     with different instruction pairs
				//     (like AUIPC+LD vs. AUIPC+ADDI) or
				//     when the occurrences of the same
				//     pair use different registers. When
				//     referring to adjacent memory locations
				//     (like function calls that go via the
				//     ELF PLT), in big endian order only the
				//     last 1-2 bytes differ; in little endian
				//     the differing 1-2 bytes would be in the
				//     middle of the 8-byte sequence.
				//
				// When reversing the transformation, the
				// original rd of AUIPC can be restored
				// from inst2's rs1 as they are required to
				// be the same.

				// Arithmetic right shift makes sign extension
				// trivial but (1) it's implementation-defined
				// behavior (C99/C11/C23 6.5.7-p5) and so is
				// (2) casting unsigned to signed (6.3.1.3-p3).
				//
				// One can check for (1) with
				//
				//     if ((-1 >> 1) == -1) ...
				//
				// but (2) has to be checked from the
				// compiler docs. GCC promises that (1)
				// and (2) behave in the common expected
				// way and thus
				//
				//     addr += (uint32_t)(
				//             (int32_t)inst2 >> 20);
				//
				// does the same as the code below. But since
				// the 100 % portable way is only a few bytes
				// bigger code and there is no real speed
				// difference, let's just use that, especially
				// since the decoder doesn't need this at all.
				uint32_t addr = inst & 0xFFFFF000;
				addr += (inst2 >> 20)
						- ((inst2 >> 19) & 0x1000);

				addr += now_pos + (uint32_t)i;

				// Construct the first 32 bits:
				//   [6:0]    AUIPC opcode
				//   [11:7]   Special AUIPC rd = x2
				//   [31:12]  The lowest 20 bits of inst2
				inst = 0x17 | (2 << 7) | (inst2 << 12);

				write32le(buffer + i, inst);

				// The second 32 bits store the absolute
				// address in big endian order.
				write32be(buffer + i + 4, addr);
			} else {
				// AUIPC's rd equals x0 or x2.
				//
				// x0 indicates a landing pad (LPAD).
				// It's always skipped.
				//
				// AUIPC with rd == x2 is used for the special
				// format as explained above. When the input
				// contains a byte sequence that matches the
				// special format, "fake" decoding must be
				// done to keep the filter bijective (that
				// is, safe to apply on arbitrary data).
				//
				// See the "x0 or x2" section in riscv_decode()
				// for how the "real" decoding is done. The
				// "fake" decoding is a simplified version
				// of "real" decoding with the following
				// differences (these reduce code size of
				// the decoder):
				// (1) The lowest 12 bits aren't sign-extended.
				// (2) No address conversion is done.
				// (3) Big endian format isn't used (the fake
				//     address is in little endian order).

				// Check if inst matches the special format.
				const uint32_t fake_rs1 = inst >> 27;

				if (NOT_SPECIAL_AUIPC(inst, fake_rs1)) {
					i += 4 - 2;
					continue;
				}

				const uint32_t fake_addr =
						read32le(buffer + i + 4);

				// Construct the second 32 bits:
				//   [19:0]   Upper 20 bits from AUIPC
				//   [31:20]  The lowest 12 bits of fake_addr
				const uint32_t fake_inst2 = (inst >> 12)
						| (fake_addr << 20);

				// Construct new first 32 bits from:
				//   [6:0]   AUIPC opcode
				//   [11:7]  Fake AUIPC rd = fake_rs1
				//   [31:12] The highest 20 bits of fake_addr
				inst = 0x17 | (fake_rs1 << 7)
					| (fake_addr & 0xFFFFF000);

				write32le(buffer + i, inst);
				write32le(buffer + i + 4, fake_inst2);
			}

			i += 8 - 2;
		}
	}

	return i;
}


extern lzma_ret
lzma_simple_riscv_encoder_init(lzma_next_coder *next,
		const lzma_allocator *allocator,
		const lzma_filter_info *filters)
{
	return lzma_simple_coder_init(next, allocator, filters,
			&riscv_encode, 0, 8, 2, true);
}


extern LZMA_API(size_t)
lzma_bcj_riscv_encode(uint32_t start_offset, uint8_t *buf, size_t size)
{
	// start_offset must be a multiple of two.
	start_offset &= ~UINT32_C(1);
	return riscv_encode(NULL, start_offset, true, buf, size);
}
#endif


#ifdef HAVE_DECODER_RISCV
static size_t
riscv_decode(void *simple lzma_attribute((__unused__)),
		uint32_t now_pos,
		bool is_encoder lzma_attribute((__unused__)),
		uint8_t *buffer, size_t size)
{
	if (size < 8)
		return 0;

	size -= 8;

	size_t i;
	for (i = 0; i <= size; i += 2) {
		uint32_t inst = buffer[i];

		if (inst == 0xEF) {
			// JAL
			const uint32_t b1 = buffer[i + 1];

			// Only filter rd=x1(ra) and rd=x5(t0).
			if ((b1 & 0x0D) != 0)
				continue;

			const uint32_t b2 = buffer[i + 2];
			const uint32_t b3 = buffer[i + 3];
			const uint32_t pc = now_pos + (uint32_t)i;

// |          b3             |          b2             |          b1         |
// | 31 30 29 28 27 26 25 24 | 23 22 21 20 19 18 17 16 | 15 14 13 12 x x x x |
// | 20 10  9  8  7  6  5  4 |  3  2  1 11 19 18 17 16 | 15 14 13 12 x x x x |
// |  7  6  5  4  3  2  1  0 |  7  6  5  4  3  2  1  0 |  7  6  5  4 x x x x |

			uint32_t addr = ((b1 & 0xF0) << 13)
					| (b2 << 9) | (b3 << 1);

			addr -= pc;

			buffer[i + 1] = (uint8_t)((b1 & 0x0F)
					| ((addr >> 8) & 0xF0));

			buffer[i + 2] = (uint8_t)(((addr >> 16) & 0x0F)
					| ((addr >> 7) & 0x10)
					| ((addr << 4) & 0xE0));

			buffer[i + 3] = (uint8_t)(((addr >> 4) & 0x7F)
					| ((addr >> 13) & 0x80));

			i += 4 - 2;

		} else if ((inst & 0x7F) == 0x17) {
			// AUIPC
			uint32_t inst2;

			inst |= (uint32_t)buffer[i + 1] << 8;
			inst |= (uint32_t)buffer[i + 2] << 16;
			inst |= (uint32_t)buffer[i + 3] << 24;

			if (inst & 0xE80) {
				// AUIPC's rd doesn't equal x0 or x2.

				// Check if it is a "fake" AUIPC+inst2 pair.
				inst2 = read32le(buffer + i + 4);

				if (NOT_AUIPC_PAIR(inst, inst2)) {
					i += 6 - 2;
					continue;
				}

				// Decode (or more like re-encode) the "fake"
				// pair. The "fake" format doesn't do
				// sign-extension, address conversion, or
				// use big endian. (The use of little endian
				// allows sharing the write32le() calls in
				// the decoder to reduce code size when
				// unaligned access isn't supported.)
				uint32_t addr = inst & 0xFFFFF000;
				addr += inst2 >> 20;

				inst = 0x17 | (2 << 7) | (inst2 << 12);
				inst2 = addr;
			} else {
				// AUIPC's rd equals x0 or x2.

				// Check if inst matches the special format
				// used by the encoder.
				const uint32_t inst2_rs1 = inst >> 27;

				if (NOT_SPECIAL_AUIPC(inst, inst2_rs1)) {
					i += 4 - 2;
					continue;
				}

				// Decode the "real" pair.
				uint32_t addr = read32be(buffer + i + 4);

				addr -= now_pos + (uint32_t)i;

				// The second instruction:
				//   - Get the lowest 20 bits from inst.
				//   - Add the lowest 12 bits of the address
				//     as the immediate field.
				inst2 = (inst >> 12) | (addr << 20);

				// AUIPC:
				//   - rd is the same as inst2_rs1.
				//   - The sign extension of the lowest 12 bits
				//     must be taken into account.
				inst = 0x17 | (inst2_rs1 << 7)
					| ((addr + 0x800) & 0xFFFFF000);
			}

			// Both decoder branches write in little endian order.
			write32le(buffer + i, inst);
			write32le(buffer + i + 4, inst2);

			i += 8 - 2;
		}
	}

	return i;
}


extern lzma_ret
lzma_simple_riscv_decoder_init(lzma_next_coder *next,
		const lzma_allocator *allocator,
		const lzma_filter_info *filters)
{
	return lzma_simple_coder_init(next, allocator, filters,
			&riscv_decode, 0, 8, 2, false);
}


extern LZMA_API(size_t)
lzma_bcj_riscv_decode(uint32_t start_offset, uint8_t *buf, size_t size)
{
	// start_offset must be a multiple of two.
	start_offset &= ~UINT32_C(1);
	return riscv_decode(NULL, start_offset, false, buf, size);
}
#endif
