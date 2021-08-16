// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Marshalling
{

    #region Simple classes

    public class SimpleEmptyClass
    {
    }

    public class SimpleByteClass
    {
        public byte x;
    }

    public class SimpleInt16Class
    {
        public short x;
    }

    public class SimpleInt32Class
    {
        public int x;
    }

    public class SimpleInt64Class
    {
        public long x;
    }

    #endregion

    #region LayoutKind.Explicit classes

    [StructLayout(LayoutKind.Explicit)]
    public class ExplicitEmptyBase
    {
    }

    [StructLayout(LayoutKind.Explicit)]
    public class ClassWithExplicitEmptyBase : ExplicitEmptyBase
    {
        [FieldOffset(0)]
        public int i;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0)]
    public class ExplicitEmptySizeZeroBase
    {
    }

    [StructLayout(LayoutKind.Explicit)]
    public class ClassWithExplicitEmptySizeZeroBase : ExplicitEmptySizeZeroBase
    {
        [FieldOffset(0)]
        public int i;
    }

    [StructLayout(LayoutKind.Explicit)]
    public class ExplicitByteBase
    {
        [FieldOffset(0)]
        public byte x;
    }

    [StructLayout(LayoutKind.Explicit)]
    public class ClassWithExplicitByteBase : ExplicitByteBase
    {
        [FieldOffset(1)]
        public int i;
    }

    [StructLayout(LayoutKind.Explicit)]
    public class ExplicitInt16Base
    {
        [FieldOffset(0)]
        public short x;
    }

    [StructLayout(LayoutKind.Explicit)]
    public class ClassWithExplicitInt16Base : ExplicitInt16Base
    {
        [FieldOffset(1)]
        public int i;
    }

    [StructLayout(LayoutKind.Explicit)]
    public class ExplicitInt32Base
    {
        [FieldOffset(0)]
        public int x;
    }

    [StructLayout(LayoutKind.Explicit)]
    public class ClassWithExplicitInt32Base : ExplicitInt32Base
    {
        [FieldOffset(1)]
        public int i;
    }

    [StructLayout(LayoutKind.Explicit)]
    public class ExplicitInt64Base
    {
        [FieldOffset(0)]
        public long x;
    }

    [StructLayout(LayoutKind.Explicit)]
    public class ClassWithExplicitInt64Base : ExplicitInt64Base
    {
        [FieldOffset(1)]
        public int i;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class SequentialEmptyBase
    {
    }

    #endregion

    #region LayoutKind.Sequential classes

    [StructLayout(LayoutKind.Sequential)]
    public class ClassWithSequentialEmptyBase : SequentialEmptyBase
    {
        public int i;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class SequentialByteBase
    {
        public byte x;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class ClassWithSequentialByteBase : SequentialByteBase
    {
        public int i;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class SequentialInt16Base
    {
        public short x;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class ClassWithSequentialInt16Base : SequentialInt16Base
    {
        public int i;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class SequentialInt32Base
    {
        public int x;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class ClassWithSequentialInt32Base : SequentialInt32Base
    {
        public int i;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class SequentialInt64Base
    {
        public long x;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class ClassWithSequentialInt64Base : SequentialInt64Base
    {
        public int i;
    }

    #endregion
}
