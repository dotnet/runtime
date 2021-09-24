using System.Runtime.InteropServices;

namespace NativeExports
{
    public static unsafe class Characters
    {
        [UnmanagedCallersOnly(EntryPoint = "unicode_return_as_uint")]
        public static uint ReturnUnicodeAsUInt(ushort input)
        {
            return input;
        }

        [UnmanagedCallersOnly(EntryPoint = "char_return_as_uint")]
        public static uint ReturnUIntAsUInt(uint input)
        {
            return input;
        }

        [UnmanagedCallersOnly(EntryPoint = "char_return_as_refuint")]
        public static void ReturnUIntAsRefUInt(uint input, uint* res)
        {
            *res = input;
        }
    }
}
