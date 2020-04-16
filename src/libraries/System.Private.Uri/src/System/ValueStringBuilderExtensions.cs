using System.Diagnostics;

namespace System.Text
{
    internal ref partial struct ValueStringBuilder
    {
        public void AppendString(string value) => AppendSlow(value);

        public void Append(string source, int offset, int length) => Append(source.AsSpan(offset, length));

        public void AppendNumber(ushort value)
        {
            const int MaxLength = 5;
            bool success = value.TryFormat(AppendSpan(MaxLength), out int charsWritten);
            Debug.Assert(success);
            Length -= MaxLength - charsWritten;
        }
    }
}
