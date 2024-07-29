using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace ReadMemBytes
{
    public class Program
    {
        static int Pass = 100;
        static int Fail = -1;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe int TestMemBytesNotReadPastTheLimit(byte *p, int byteLength)
        {
            int count = 0;
            for (int i= 0; i< byteLength; ++i)
            {
                // RyuJIT lowering has an optimization to recognize the condition in
                // "If" stmnt and generate "test byte ptr [p+i], 0" instruction. Due
                // to a bug that has been fixed it will end up generating "test dword ptr [p+i], 0"
                // that will lead to reading 4 bytes instead of a single byte.
                if ((p[i] & 0xffffff00) != 0)
                {
                    ++count;
                }
            }

            return count;
        }
        [Fact]
        public static unsafe int TestEntryPoint()
        {
            byte* buffer = stackalloc byte[4];
            buffer[0] = 0;
            buffer[1] = buffer[2] = buffer[3] = 0xff;

            int result = TestMemBytesNotReadPastTheLimit(buffer, 1);
            if (result != 0)
            {
                Console.WriteLine("Failed: Read past the end of buffer");
                return Fail;
            }
            else
            {
                Console.WriteLine("Pass");
            }

            return Pass;

        }
    }
}
