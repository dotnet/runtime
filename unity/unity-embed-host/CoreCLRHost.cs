using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

[assembly:DisableRuntimeMarshalling]

namespace Unity.CoreCLRHelpers;

using StringPtr = IntPtr;
static unsafe class CoreCLRHost
{
    static ALCWrapper alcWrapper;
    static FieldInfo assemblyHandleField;

    public static int InitMethod(HostStruct* functionStruct, int structSize)
    {
        if (Marshal.SizeOf<HostStruct>() != structSize)
            throw new Exception("Invalid struct size");

        alcWrapper = new ALCWrapper();
        assemblyHandleField = typeof(Assembly).Assembly.GetType("System.Reflection.RuntimeAssembly").GetField("m_assembly", BindingFlags.Instance | BindingFlags.NonPublic);
        if (assemblyHandleField == null)
            throw new Exception("Failed to find RuntimeAssembly.m_assembly field.");

        functionStruct->LoadFromMemory = &CallLoadFromAssemblyData;
        functionStruct->LoadFromPath = &CallLoadFromAssemblyPath;
        functionStruct->string_from_utf16 = &string_from_utf16;
        functionStruct->string_new_len = &string_new_len;
        functionStruct->string_new_utf16 = &string_new_utf16;
        functionStruct->string_new_wrapper = &string_new_wrapper;

        return 0;
    }

    [UnmanagedCallersOnly]
    static IntPtr /*Assembly*/ CallLoadFromAssemblyData(byte* data, long size)
    {
        var assembly = alcWrapper.CallLoadFromAssemblyData(data, size);
        return (IntPtr)assemblyHandleField.GetValue(assembly);
    }

    [UnmanagedCallersOnly]
    static IntPtr /*Assembly*/ CallLoadFromAssemblyPath(byte* path, int length)
    {
        var assembly = alcWrapper.CallLoadFromAssemblyPath(Encoding.UTF8.GetString(path, length));
        return (IntPtr)assemblyHandleField.GetValue(assembly);

    }

    [UnmanagedCallersOnly]
    static StringPtr string_from_utf16(ushort* text)
    {
        var s = new string((char*)text);
        return StringToPtr(s);

    }

    [UnmanagedCallersOnly]
    static StringPtr string_new_len(void* domain /* unused */, sbyte* text, uint length)
    {
        var s = new string(text, 0, (int)length, Encoding.UTF8);
        return StringToPtr(s);

    }

    [UnmanagedCallersOnly]
    static StringPtr string_new_utf16(void* domain /* unused */, ushort* text, uint length)
    {
        var s = new string((char*)text, 0, (int)length);
        return StringToPtr(s);

    }

    [UnmanagedCallersOnly]
    static StringPtr string_new_wrapper(sbyte* text)
    {
        var s = new string(text);
        return StringToPtr(s);

    }

    static StringPtr StringToPtr(string s)
    {
        // Return raw object pointer for now with the NullGC.
        // This will become a GCHandle in the future.
        return Unsafe.As<string, StringPtr>(ref s);
    }
}
