using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Unity.CoreCLRHelpers;

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
}
