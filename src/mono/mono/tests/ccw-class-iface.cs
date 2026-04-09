// Test the ClassInterfaceType attribute.

using System;
using System.Runtime.InteropServices;

[ComVisible(false)]
public interface IInvisible
{
    int invisible_iface();
}

public interface IApple
{
    int iface1_parent_method();
}

[Guid ("12345678-0000-0000-0000-000000000002")]
public interface IBanana : IApple
{
    int iface1_method();
}

public interface ICherry
{
    int iface2_method();
}

public interface IDrupe
{
    int parent_iface_method();
}

public class TestGeneric<T,V>
{
}

public class TestParent : IDrupe
{
    public interface INested
    {
        TestGeneric<ICherry, IBanana> method(in ICherry[] cherries, ref object something, out string output, float fl1, float fl2);
    }

    public virtual int parent_method_virtual()
    {
        return 101;
    }

    public int parent_iface_method()
    {
        return 112;
    }

    public int parent_method()
    {
        return 104;
    }

    public virtual int parent_property {get {return 102;}}

    public virtual int parent_method_override()
    {
        return 103;
    }
}

[ClassInterface(ClassInterfaceType.AutoDual)]
public class TestAutoDual : TestParent, IBanana, ICherry
{
    internal int invisible_internal()
    {
        return 0;
    }

    protected int invisible_protected()
    {
        return 0;
    }

    [ComVisible(false)]
    public int invisible_hidden()
    {
        return 0;
    }

    public static int invisible_static()
    {
        return 0;
    }

    public TestAutoDual(int arg)
    {
    }

    public int iface2_method()
    {
        return 109;
    }

    public int child_method()
    {
        return 110;
    }

    public virtual int child_method_virtual()
    {
        return 106;
    }

    public int iface1_method()
    {
        return 107;
    }

    public override int parent_method_override()
    {
        return 203;
    }

    public int iface1_parent_method()
    {
        return 108;
    }
}

[ClassInterface(ClassInterfaceType.None)]
public class TestNone : TestParent, IInvisible, IBanana, ICherry
{
    public int iface2_method()
    {
        return 1;
    }

    public int child_method()
    {
        return 2;
    }

    public int iface1_method()
    {
        return 3;
    }

    public int iface1_parent_method()
    {
        return 4;
    }

    public int invisible_iface()
    {
        return 0;
    }
}

[ClassInterface(ClassInterfaceType.AutoDispatch)]
public class TestAutoDispatch : TestParent, IBanana, ICherry
{
    public int iface2_method()
    {
        return 1;
    }

    public int child_method()
    {
        return 2;
    }

    public int iface1_method()
    {
        return 3;
    }

    public int iface1_parent_method()
    {
        return 4;
    }
}

public class Tests
{
    [DllImport("libtest")]
    public static extern int mono_test_ccw_class_type_auto_dispatch([MarshalAs (UnmanagedType.Interface)] TestAutoDispatch obj);
    [DllImport("libtest")]
    public static extern int mono_test_ccw_class_type_auto_dual([MarshalAs (UnmanagedType.Interface)] TestAutoDual obj);
    [DllImport("libtest")]
    public static extern int mono_test_ccw_class_type_none([MarshalAs (UnmanagedType.Interface)] TestNone obj);
    [DllImport("libtest")]
    public static extern int mono_test_ccw_query_interface([MarshalAs (UnmanagedType.Interface)] TestParent obj);

    public static int Main()
    {
        TestAutoDual autodual = new TestAutoDual(105);
        int hr;

        hr = mono_test_ccw_class_type_auto_dual(autodual);
        if (hr != 0)
        {
            Console.Error.WriteLine("TestAutoDual failed: {0}", hr);
            return 1;
        }

        hr = mono_test_ccw_class_type_auto_dispatch(new TestAutoDispatch());
        if (hr != 0)
        {
            Console.Error.WriteLine("TestAutoDispatch failed: {0}", hr);
            return 4;
        }

        hr = mono_test_ccw_class_type_none(new TestNone());
        if (hr != 0)
        {
            Console.Error.WriteLine("TestNone failed: {0}", hr);
            return 5;
        }

        /*TODO: Reject Guid.Empty when we support generated class GUIDs*/
        if (typeof(TestParent).GUID != Guid.Empty &&
			typeof(TestParent).GUID != new Guid("7dcc27e3-e226-35ca-a942-2286b21f2525"))
        {
            Console.Error.WriteLine("Unexpected typeof(TestParent).GUID: {0}", typeof(TestParent).GUID);
            return 6;
        }

        /*TODO: Reject Guid.Empty when we support generated class GUIDs*/
        if (typeof(int).GUID != Guid.Empty &&
			typeof(int).GUID != new Guid("a310fadd-7c33-377c-9d6b-599b0317d7f2"))
        {
            Console.Error.WriteLine("Unexpected typeof(int).GUID: {0}", typeof(int).GUID);
            return 7;
        }

        if (typeof(TestParent.INested).GUID != new Guid("9aea5855-969a-3c25-8a78-15186615895c"))
        {
            Console.Error.WriteLine("Unexpected typeof(TestParent.INested).GUID: {0}", typeof(TestParent.INested).GUID);
            return 8;
        }

        if (typeof(IDrupe).GUID != new Guid("9f001e6b-a244-3911-88db-bb2b6d5843aa"))
        {
            Console.Error.WriteLine("Unexpected typeof(IDrupe).GUID: {0}", typeof(IDrupe).GUID);
            return 9;
        }

        hr = mono_test_ccw_query_interface(new TestParent());
        if (hr != 0)
        {
            Console.Error.WriteLine("TestQueryInterface failed: {0}", hr);
            return 10;
        }

        return 0;
    }
}
