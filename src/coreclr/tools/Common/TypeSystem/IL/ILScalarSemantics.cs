// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Internal.IL
{
    // Shared by concrete IL interpretation (type preinitialization) and symbolic interpretation (LLVM IR).
    internal static class ILScalarSemantics
    {
        public static ILOpcode NormalizeBranch(ILOpcode opcode)
            => opcode >= ILOpcode.br && opcode <= ILOpcode.blt_un ? opcode - ILOpcode.br + ILOpcode.br_s : opcode;

        public static ulong Mask(int bits) => bits == 64 ? ulong.MaxValue : (1UL << bits) - 1;

        public static long Signed(ulong value, int bits)
            => unchecked((long)(value << (64 - bits))) >> (64 - bits);

        public static bool IsDivisionSafe(ILOpcode opcode, int bits, ulong? left, ulong? right)
        {
            if (!right.HasValue || (right.Value & Mask(bits)) == 0)
                return false;
            return opcode is ILOpcode.div_un or ILOpcode.rem_un ||
                (right.Value & Mask(bits)) != Mask(bits) ||
                (left.HasValue && (left.Value & Mask(bits)) != (1UL << (bits - 1)));
        }

        public static bool TryBinary(ILOpcode opcode, int bits, ulong left, ulong right, out ulong result)
        {
            ulong mask = Mask(bits);
            left &= mask;
            right &= mask;
            result = 0;
            if (opcode is ILOpcode.div or ILOpcode.div_un or ILOpcode.rem or ILOpcode.rem_un &&
                !IsDivisionSafe(opcode, bits, left, right))
                return false;

            unchecked
            {
                result = opcode switch
                {
                    ILOpcode.add => left + right,
                    ILOpcode.sub => left - right,
                    ILOpcode.mul => left * right,
                    ILOpcode.and => left & right,
                    ILOpcode.or => left | right,
                    ILOpcode.xor => left ^ right,
                    ILOpcode.shl => left << ((int)right & (bits - 1)),
                    ILOpcode.shr => (ulong)(Signed(left, bits) >> ((int)right & (bits - 1))),
                    ILOpcode.shr_un => left >> ((int)right & (bits - 1)),
                    ILOpcode.div => (ulong)(Signed(left, bits) / Signed(right, bits)),
                    ILOpcode.rem => (ulong)(Signed(left, bits) % Signed(right, bits)),
                    ILOpcode.div_un => left / right,
                    ILOpcode.rem_un => left % right,
                    _ => throw new ArgumentOutOfRangeException(nameof(opcode)),
                };
            }
            result &= mask;
            return true;
        }

        public static string IntegerPredicate(ILOpcode opcode) => NormalizeBranch(opcode) switch
        {
            ILOpcode.ceq or ILOpcode.beq_s => "eq",
            ILOpcode.bne_un_s => "ne",
            ILOpcode.clt or ILOpcode.blt_s => "slt",
            ILOpcode.cgt or ILOpcode.bgt_s => "sgt",
            ILOpcode.ble_s => "sle",
            ILOpcode.bge_s => "sge",
            ILOpcode.clt_un or ILOpcode.blt_un_s => "ult",
            ILOpcode.cgt_un or ILOpcode.bgt_un_s => "ugt",
            ILOpcode.ble_un_s => "ule",
            ILOpcode.bge_un_s => "uge",
            _ => throw new ArgumentOutOfRangeException(nameof(opcode)),
        };

        // The .un comparisons are unordered for floating point, including NaNs.
        public static string FloatPredicate(ILOpcode opcode) => NormalizeBranch(opcode) switch
        {
            ILOpcode.ceq or ILOpcode.beq_s => "oeq",
            ILOpcode.bne_un_s => "une",
            ILOpcode.clt or ILOpcode.blt_s => "olt",
            ILOpcode.cgt or ILOpcode.bgt_s => "ogt",
            ILOpcode.ble_s => "ole",
            ILOpcode.bge_s => "oge",
            ILOpcode.clt_un or ILOpcode.blt_un_s => "ult",
            ILOpcode.cgt_un or ILOpcode.bgt_un_s => "ugt",
            ILOpcode.ble_un_s => "ule",
            ILOpcode.bge_un_s => "uge",
            _ => throw new ArgumentOutOfRangeException(nameof(opcode)),
        };

        public static bool Compare(ILOpcode opcode, int bits, ulong left, ulong right)
        {
            left &= Mask(bits);
            right &= Mask(bits);
            return IntegerPredicate(opcode) switch
            {
                "eq" => left == right, "ne" => left != right,
                "slt" => Signed(left, bits) < Signed(right, bits),
                "sgt" => Signed(left, bits) > Signed(right, bits),
                "sle" => Signed(left, bits) <= Signed(right, bits),
                "sge" => Signed(left, bits) >= Signed(right, bits),
                "ult" => left < right, "ugt" => left > right,
                "ule" => left <= right, "uge" => left >= right,
                _ => throw new InvalidOperationException(),
            };
        }

        public static bool Compare(ILOpcode opcode, double left, double right) => FloatPredicate(opcode) switch
        {
            "oeq" => left == right, "une" => left != right,
            "olt" => left < right, "ogt" => left > right,
            "ole" => left <= right, "oge" => left >= right,
            "ult" => !(left >= right), "ugt" => !(left <= right),
            "ule" => !(left > right), "uge" => !(left < right),
            _ => throw new InvalidOperationException(),
        };
    }
}
