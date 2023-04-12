#include<stdio.h>
#include<assert.h>

typedef signed char gint8;
typedef unsigned char guint8;
typedef signed int gint32;
typedef unsigned int guint32;
typedef unsigned int gsize;
typedef signed int gssize;

#define _riscv_emit(p, insn) \
	do { \
		*(guint32 *) (p) = (insn); \
		(p) += sizeof (guint32); \
	} while (0)

#define RISCV_BITS(value, start, count) (((value) >> (start)) & ((1 << (count)) - 1))
#define RISCV_SIGN(value) (-(((value) >> (sizeof (guint32) * 8 - 1)) & 1))

#define RISCV_VALID_IMM32(value)	 \
	(((gint32)value) == (value))

#define RISCV_ENCODE_I_IMM(imm) \
	(RISCV_BITS ((imm), 0, 12) << 20)

#define RISCV_DECODE_I_IMM(ins) \
	((RISCV_BITS ((ins), 20, 12) << 0) | (RISCV_SIGN ((ins)) << 12))

#define RISCV_VALID_I_IMM(value) \
	(RISCV_DECODE_I_IMM (RISCV_ENCODE_I_IMM ((value))) == (value))

#define RISCV_DECODE_U_IMM(ins) \
	(RISCV_BITS ((ins), 12, 20) << 0)

#define RISCV_ENCODE_U_IMM(imm) \
	(RISCV_BITS ((imm), 0, 20) << 12)

#define RISCV_VALID_U_IMM(value) \
	(RISCV_DECODE_U_IMM (RISCV_ENCODE_U_IMM ((value))) == (value))

#define _riscv_u_op(p, opcode, rd, imm) \
	do { \
		assert (RISCV_VALID_U_IMM ((guint32) (gsize) (imm))); \
		_riscv_emit ((p), ((opcode) << 0) | \
		                  ((rd) << 7) | \
		                  (RISCV_ENCODE_U_IMM ((guint32) (gsize) (imm)))); \
	} while (0)

#define _riscv_i_op(p, opcode, funct3, rd, rs1, imm) \
	do { \
		assert (RISCV_VALID_I_IMM ((gint32) (gssize) (imm))); \
		_riscv_emit ((p), ((opcode) << 0) | \
		                  ((rd) << 7) | \
		                  ((funct3) << 12) | \
		                  ((rs1) << 15) | \
		                  (RISCV_ENCODE_I_IMM ((gint32) (gssize) (imm)))); \
	} while (0)

#define riscv_lui(p, rd, imm)                      _riscv_u_op  ((p), 0b0110111, (rd), (imm))
#define riscv_addi(p, rd, rs1, imm)                _riscv_i_op  ((p), 0b0010011, 0b000, (rd), (rs1), (imm))
#define riscv_addiw(p, rd, rs1, imm)               _riscv_i_op  ((p), 0b0011011, 0b000, (rd), (rs1), (imm))
#define riscv_jalr(p, rd, rs1, imm)                _riscv_i_op  ((p), 0b1100111, 0b000, (rd), (rs1), (imm))

guint8 *
mono_riscv_emit_imm (guint8 *code, int rd, gsize imm)
{
	if (RISCV_VALID_I_IMM (imm)) {
		riscv_addi (code, rd, 0, imm);
		return code;
	}

	/**
	 * use LUI & ADDIW load 32 bit Imm
	 * LUI: High 20 bit of imm
	 * ADDIW: Low 12 bit of imm
	 */
	if (RISCV_VALID_IMM32 (imm)) {
		gint32 Hi = RISCV_BITS (imm, 12, 20);
		gint32 Lo = RISCV_BITS (imm, 0, 12);

		// Lo is in signed num
		// if Lo >= 0x800
		// convert into ((Hi + 1) << 20) -  (0x1000 - Lo)
		if (Lo >= 0x800) {
			if (imm > 0)
				Hi += 1;
			Lo = Lo - 0x1000;
		}

		assert(Hi <= 0xfffff);
		riscv_lui (code, rd, Hi);
		riscv_addiw (code, rd, rd, Lo);
		return code;
	}

	assert(0);
}

int main(int argc, char const *argv[])
{

    
    gint8 *code = malloc(10*sizeof(gint8));
    for (int i = -1118200000; i < 2147483647; i++)
    {
        gint8 *p = mono_riscv_emit_imm(code, 10, i);
        riscv_jalr(p, 0, 1, 0);

        int (* func)() = code;
        int res = func();
        if(i != res){
            printf("except: %d, got = %d\n", i, res);
            return 1;
        }
        if(i % 1000000 == 0)
            printf("progress: %d\n",i);
    }
    printf("all correct\n");
    return 0;
}
