using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

namespace System.Text
{
    internal ref partial struct ValueStringBuilder
    {
        public void AppendString(string value) => AppendSlow(value);

        public void Append(string source, int offset, int length) => Append(source.AsSpan(offset, length));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append(char c1, char c2)
        {
            uint twoCharsAsUInt;
            if (BitConverter.IsLittleEndian)
            {
                twoCharsAsUInt = c1 | (uint)c2 << 16;
            }
            else
            {
                twoCharsAsUInt = (uint)c1 << 16 | c2;
            }

            int pos = _pos;
            Span<char> chars = _chars;
            if ((uint)(pos + 1) < (uint)chars.Length)
            {
                Unsafe.As<char, uint>(ref Unsafe.Add(ref MemoryMarshal.GetReference(chars), pos)) = twoCharsAsUInt;
                _pos = pos + 2;
            }
            else
            {
                GrowAndAppendTwoChars(twoCharsAsUInt);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void GrowAndAppendTwoChars(uint twoCharsAsUInt)
        {
            Grow(2);
            Unsafe.As<char, uint>(ref Unsafe.Add(ref MemoryMarshal.GetReference(_chars), _pos)) = twoCharsAsUInt;
            _pos += 2;
        }

        public void AppendNumber(ushort value)
        {
            const int MaxLength = 5;
            bool success = value.TryFormat(AppendSpan(MaxLength), out int charsWritten);
            Debug.Assert(success);
            Length -= MaxLength - charsWritten;
        }
    }
}
