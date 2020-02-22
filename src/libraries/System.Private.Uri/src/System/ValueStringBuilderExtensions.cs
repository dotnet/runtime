namespace System.Text
{
    internal ref partial struct ValueStringBuilder
    {
        public void Replace(char oldChar, char newChar)
        {
            Span<char> span = _chars.Slice(0, _pos);
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == oldChar)
                {
                    span[i] = newChar;
                }
            }
        }
    }
}
