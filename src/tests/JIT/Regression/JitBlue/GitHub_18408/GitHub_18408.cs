// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Threading;
using System.Runtime.CompilerServices;
using Xunit;

class MetadataReader
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public Method GetMethod(MethodHandle handle)
    {
        return new Method(this, handle, MethodAttributes.Abstract);
    }

}

struct Handle
{
    int _value;

    public MethodHandle ToMethodHandle(MetadataReader reader)
    {
        return new MethodHandle(this);
    }

    public int GetValue()
    {
        return _value;
    }
}

static class MetadataReaderExtensions
{
    public static unsafe Handle AsHandle(this int token)
    {
        return *(Handle*)&token;
    }
}

struct MethodHandle
{
    internal int _value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal MethodHandle(Handle value)
    {
        _value = value.GetValue();
    }

    public Method GetMethod(MetadataReader reader)
    {
        return reader.GetMethod(this);
    }
}

struct Method
{
    internal MetadataReader _reader;
    internal MethodHandle _handle;
    internal MethodAttributes _flags;

    public Method(MetadataReader r, MethodHandle h, MethodAttributes f)
    {
        _reader = r;
        _handle = h;
        _flags = f;
    }

    public MethodAttributes Flags => _flags;
}

struct QMethodDefinition
{
    private QMethodDefinition(MetadataReader reader, int token)
    {
        _reader = reader;
        _handle = token;
    }

    public static QMethodDefinition FromObjectAndInt(MetadataReader reader, int token)
    {
        return new QMethodDefinition(reader, token);
    }

    public MetadataReader Reader { get { return _reader; } }
    public int Token { get { return _handle; } }

    public bool IsValid { get { return _reader == null; } }

    private readonly MetadataReader _reader;
    private readonly int _handle;

    public MetadataReader NativeFormatReader { get { return _reader; } }
    public MethodHandle NativeFormatHandle { get { return _handle.AsHandle().ToMethodHandle(NativeFormatReader); } }
}

public class GitHub_18408
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static object foo(QMethodDefinition methodHandle)
    {
        Method method = methodHandle.NativeFormatHandle.GetMethod(methodHandle.NativeFormatReader);
        return (method.Flags != (MethodAttributes)0) ? new object() : null;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        MetadataReader r = new MetadataReader();

        if (foo(QMethodDefinition.FromObjectAndInt(r, 1)) == null)
        {
            Console.WriteLine("FAIL");
            return -1;
        }

        Console.WriteLine("PASS");
        return 100;
    }
}
