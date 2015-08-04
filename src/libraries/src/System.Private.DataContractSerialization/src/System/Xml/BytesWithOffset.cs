namespace System.Xml
{
    internal struct BytesWithOffset
    {
        private readonly byte[] _bytes;
        private readonly int _offset;

        public BytesWithOffset(byte[] bytes, int offset)
        {
            _bytes = bytes;
            _offset = offset;
        }

        public byte[] Bytes
        {
            get
            {
                return _bytes;
            }
        }

        public int Offset
        {
            get
            {
                return _offset;
            }
        }
    }
}
