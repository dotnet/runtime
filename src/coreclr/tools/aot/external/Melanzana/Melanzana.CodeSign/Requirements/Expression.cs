namespace Melanzana.CodeSign.Requirements
{
    public abstract partial class Expression
    {
        public abstract int Size { get; }

        public abstract void Write(Span<byte> buffer, out int bytesWritten);
    }
}
