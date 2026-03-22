// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

public static class Program
{
    private static int _errors;

    [Fact]
    public static int Main()
    {
        TestNonGeneric();

        TestGeneric<int>();
        TestGeneric<Guid>();
        TestGeneric<object>();
        TestGeneric<string>();
        TestGeneric<StringBuilder>();
        TestGeneric<Stream>();

        TestGeneric<int[]>();
        TestGeneric<Guid[]>();
        TestGeneric<object[]>();
        TestGeneric<string[]>();
        TestGeneric<StringBuilder[]>();
        TestGeneric<Stream[]>();

        return 100 + _errors;
    }

    public static void TestNonGeneric()
    {
        AssertValid(NonGenericIl.GetInstanceBasicClosed,
            del => del(),
            typeof(NonGeneric).GetMethod(nameof(NonGeneric.InstanceBasic), Constants.InstanceFlags));
        AssertValid(NonGenericIl.GetInstanceBasicOpen,
            del => del(new NonGeneric()),
            typeof(NonGeneric).GetMethod(nameof(NonGeneric.InstanceBasic), Constants.InstanceFlags));
        AssertValid(NonGenericIl.GetInstanceReturnClosed,
            del => AssertEquals(Constants.TestInt, del()),
            typeof(NonGeneric).GetMethod(nameof(NonGeneric.InstanceReturn), Constants.InstanceFlags));
        AssertValid(NonGenericIl.GetInstanceReturnOpen,
            del => AssertEquals(Constants.TestInt, del(new NonGeneric())),
            typeof(NonGeneric).GetMethod(nameof(NonGeneric.InstanceReturn), Constants.InstanceFlags));
        AssertValid(NonGenericIl.GetInstanceParamClosed,
            del => AssertEquals(Constants.TestString, del(Constants.TestString)),
            typeof(NonGeneric).GetMethod(nameof(NonGeneric.InstanceParam), Constants.InstanceFlags));
        AssertValid(NonGenericIl.GetInstanceParamOpen,
            del => AssertEquals(Constants.TestString, del(new NonGeneric(), Constants.TestString)),
            typeof(NonGeneric).GetMethod(nameof(NonGeneric.InstanceParam), Constants.InstanceFlags));

        AssertValid(NonGenericIl.GetInstanceVirtualClosed,
            del => AssertEquals(Constants.TestString, del()),
            typeof(NonGeneric).GetMethod(nameof(NonGeneric.Virtual), Constants.InstanceFlags));
        AssertValid(NonGenericIl.GetInstanceVirtualOpen,
            del => AssertEquals(Constants.TestString, del(new NonGeneric())),
            typeof(NonGeneric).GetMethod(nameof(NonGeneric.Virtual), Constants.InstanceFlags));

        AssertValid(NonGeneric.GetStaticBasicClosed,
            del => del(),
            typeof(NonGeneric).GetMethod(nameof(NonGeneric.StaticBasic), Constants.StaticFlags));
        AssertValid(NonGeneric.GetStaticReturnClosed,
            del => AssertEquals(Constants.TestInt, del()),
            typeof(NonGeneric).GetMethod(nameof(NonGeneric.StaticReturn), Constants.StaticFlags));
        AssertValid(NonGeneric.GetStaticParamClosed,
            del => AssertEquals(null, del()),
            typeof(NonGeneric).GetMethod(nameof(NonGeneric.StaticParam), Constants.StaticFlags));
        AssertValid(NonGeneric.GetStaticParamOpen,
            del => AssertEquals(Constants.TestString, del(Constants.TestString)),
            typeof(NonGeneric).GetMethod(nameof(NonGeneric.StaticParam), Constants.StaticFlags));

        AssertValid(StaticResolver<NonGeneric>.GetInterfaceStaticAbstract,
            del => AssertEquals(Constants.TestString, del()),
            typeof(NonGeneric).GetMethod(nameof(IStaticInterface.InterfaceStaticAbstract), Constants.StaticFlags));
        AssertValid(StaticResolver<NonGeneric>.GetInterfaceStaticVirtual,
            del => AssertEquals(Constants.TestString, del()),
            typeof(IStaticInterface).GetMethod(nameof(IStaticInterface.InterfaceStaticVirtual), Constants.StaticFlags));
        AssertValid(StaticResolver<NonGeneric>.GetInterfaceStaticVirtualOverriden,
            del => AssertEquals(Constants.TestString, del()),
            typeof(NonGeneric).GetMethod(nameof(IStaticInterface.InterfaceStaticVirtualOverriden), Constants.StaticFlags));

        AssertValid(VirtualResolver.GetInterfaceInstance,
            del => AssertEquals(Constants.TestString, del(new NonGeneric())),
            typeof(IInterface).GetMethod(nameof(IInterface.InterfaceInstance), Constants.InstanceFlags));
        AssertValid(VirtualResolver.GetInterfaceInstanceDim,
            del => AssertEquals(Constants.TestString, del(new NonGeneric())),
            typeof(IInterface).GetMethod(nameof(IInterface.InterfaceInstanceDim), Constants.InstanceFlags));
        AssertValid(VirtualResolver.GetInterfaceInstanceDimOverriden,
            del => AssertEquals(Constants.TestString, del(new NonGeneric())),
            typeof(IInterface).GetMethod(nameof(IInterface.InterfaceInstanceDimOverriden), Constants.InstanceFlags));

        AssertValid(VirtualResolver.GetBaseAbstract,
            del => AssertEquals(Constants.TestString, del(new NonGeneric())),
            typeof(BaseClass).GetMethod(nameof(BaseClass.BaseAbstract), Constants.InstanceFlags));
        AssertValid(VirtualResolver.GetBaseVirtual,
            del => AssertEquals(Constants.TestString, del(new NonGeneric())),
            typeof(BaseClass).GetMethod(nameof(BaseClass.BaseVirtual), Constants.InstanceFlags));
        AssertValid(VirtualResolver.GetBaseVirtualOverriden,
            del => AssertEquals(Constants.TestString, del(new NonGeneric())),
            typeof(BaseClass).GetMethod(nameof(BaseClass.BaseVirtualOverriden), Constants.InstanceFlags));

        AssertValid(NonGenericStructIl.GetInstanceBasicOpen,
            del => del(new NonGenericStruct()),
            typeof(NonGenericStruct).GetMethod(nameof(NonGenericStruct.InstanceBasic), Constants.InstanceFlags));
        AssertValid(NonGenericStructIl.GetInstanceReturnOpen,
            del => AssertEquals(Constants.TestInt, del(new NonGenericStruct())),
            typeof(NonGenericStruct).GetMethod(nameof(NonGenericStruct.InstanceReturn), Constants.InstanceFlags));
        AssertValid(NonGenericStructIl.GetInstanceParamOpen,
            del => AssertEquals(Constants.TestString, del(new NonGenericStruct(), Constants.TestString)),
            typeof(NonGenericStruct).GetMethod(nameof(NonGenericStruct.InstanceParam), Constants.InstanceFlags));

        AssertValid(NonGenericStruct.GetStaticBasicClosed,
            del => del(),
            typeof(NonGenericStruct).GetMethod(nameof(NonGenericStruct.StaticBasic), Constants.StaticFlags));
        AssertValid(NonGenericStruct.GetStaticReturnClosed,
            del => AssertEquals(Constants.TestInt, del()),
            typeof(NonGenericStruct).GetMethod(nameof(NonGenericStruct.StaticReturn), Constants.StaticFlags));
        AssertValid(NonGenericStruct.GetStaticParamClosed,
            del => AssertEquals(null, del()),
            typeof(NonGenericStruct).GetMethod(nameof(NonGenericStruct.StaticParam), Constants.StaticFlags));
        AssertValid(NonGenericStruct.GetStaticParamOpen,
            del => AssertEquals(Constants.TestString, del(Constants.TestString)),
            typeof(NonGenericStruct).GetMethod(nameof(NonGenericStruct.StaticParam), Constants.StaticFlags));

        AssertValid(StaticResolver<NonGenericStruct>.GetInterfaceStaticAbstract,
            del => AssertEquals(Constants.TestString, del()),
            typeof(NonGenericStruct).GetMethod(nameof(IStaticInterface.InterfaceStaticAbstract), Constants.StaticFlags));
        AssertValid(StaticResolver<NonGenericStruct>.GetInterfaceStaticVirtual,
            del => AssertEquals(Constants.TestString, del()),
            typeof(IStaticInterface).GetMethod(nameof(IStaticInterface.InterfaceStaticVirtual), Constants.StaticFlags));
        AssertValid(StaticResolver<NonGenericStruct>.GetInterfaceStaticVirtualOverriden,
            del => AssertEquals(Constants.TestString, del()),
            typeof(NonGenericStruct).GetMethod(nameof(IStaticInterface.InterfaceStaticVirtualOverriden), Constants.StaticFlags));

        AssertValid(VirtualResolver.GetInterfaceInstance,
            del => AssertEquals(Constants.TestString, del(new NonGenericStruct())),
            typeof(IInterface).GetMethod(nameof(IInterface.InterfaceInstance), Constants.InstanceFlags));
        AssertValid(VirtualResolver.GetInterfaceInstanceDim,
            del => AssertEquals(Constants.TestString, del(new NonGenericStruct())),
            typeof(IInterface).GetMethod(nameof(IInterface.InterfaceInstanceDim), Constants.InstanceFlags));
        AssertValid(VirtualResolver.GetInterfaceInstanceDimOverriden,
            del => AssertEquals(Constants.TestString, del(new NonGenericStruct())),
            typeof(IInterface).GetMethod(nameof(IInterface.InterfaceInstanceDimOverriden), Constants.InstanceFlags));

        AssertThrows<NotSupportedException>(() => NonGenericStructIl.GetInstanceBasicClosed());
        AssertThrows<NotSupportedException>(() => NonGenericStructIl.GetInstanceBasicClosed());
        AssertThrows<NotSupportedException>(() => NonGenericStructIl.GetInstanceReturnClosed());
        AssertThrows<NotSupportedException>(() => NonGenericStructIl.GetInstanceParamClosed());
    }

    public static void TestGeneric<T>()
    {
        TestNonGenericClass<T>();

        TestGenericClass<T>();

        TestNonGenericStruct<T>();

        TestGenericStruct<T>();
    }

    public static void TestNonGenericClass<T>()
    {
        AssertValid(NonGenericIl.GetInstanceGenericClosed<T>,
            del => AssertEquals(typeof(T), del()),
            typeof(NonGeneric).GetMethod(nameof(NonGeneric.InstanceGeneric), Constants.InstanceFlags)?.MakeGenericMethod(typeof(T)));
        AssertValid(NonGenericIl.GetInstanceGenericOpen<T>,
            del => AssertEquals(typeof(T), del(new NonGeneric())),
            typeof(NonGeneric).GetMethod(nameof(NonGeneric.InstanceGeneric), Constants.InstanceFlags)?.MakeGenericMethod(typeof(T)));
        AssertValid(NonGenericIl.GetInstanceGenericParamClosed<T>,
            del => AssertEquals(typeof(T), del(default)),
            typeof(NonGeneric).GetMethod(nameof(NonGeneric.InstanceGenericParam), Constants.InstanceFlags)?.MakeGenericMethod(typeof(T)));
        AssertValid(NonGenericIl.GetInstanceGenericParamOpen<T>,
            del => AssertEquals(typeof(T), del(new NonGeneric(), default)),
            typeof(NonGeneric).GetMethod(nameof(NonGeneric.InstanceGenericParam), Constants.InstanceFlags)?.MakeGenericMethod(typeof(T)));

        AssertValid(NonGeneric.GetStaticGenericClosed<T>,
            del => AssertEquals(typeof(T), del()),
            typeof(NonGeneric).GetMethod(nameof(NonGeneric.StaticGeneric), Constants.StaticFlags)?.MakeGenericMethod(typeof(T)));
        AssertValid(NonGeneric.GetStaticGenericParamOpen<T>,
            del => AssertEquals(typeof(T), del(default)),
            typeof(NonGeneric).GetMethod(nameof(NonGeneric.StaticGenericParam), Constants.StaticFlags)?.MakeGenericMethod(typeof(T)));

        if (typeof(T).IsValueType)
        {
            AssertThrows<NotSupportedException>(() => NonGeneric.GetStaticGenericParamClosed<T>());
        }
        else
        {
            AssertValid(NonGeneric.GetStaticGenericParamClosed<T>,
                del => AssertEquals(typeof(T), del()),
                typeof(NonGeneric).GetMethod(nameof(NonGeneric.StaticGenericParam), Constants.StaticFlags)?.MakeGenericMethod(typeof(T)));
        }
    }

    public static void TestGenericClass<T>()
    {
        AssertValid(GenericIl<T>.GetInstanceBasicOpen,
            del => del(new Generic<T>()),
            typeof(Generic<T>).GetMethod(nameof(Generic<>.InstanceBasic), Constants.InstanceFlags));
        AssertValid(GenericIl<T>.GetInstanceReturnOpen,
            del => AssertEquals(Constants.TestInt, del(new Generic<T>())),
            typeof(Generic<T>).GetMethod(nameof(Generic<>.InstanceReturn), Constants.InstanceFlags));
        AssertValid(GenericIl<T>.GetInstanceParamOpen,
            del => AssertEquals(Constants.TestString, del(new Generic<T>(), Constants.TestString)),
            typeof(Generic<T>).GetMethod(nameof(Generic<>.InstanceParam), Constants.InstanceFlags));
        AssertValid(GenericIl<T>.GetInstanceGenericOpen,
            del => AssertEquals(typeof(T), del(new Generic<T>())),
            typeof(Generic<T>).GetMethod(nameof(Generic<>.InstanceGeneric), Constants.InstanceFlags));
        AssertValid(GenericIl<T>.GetInstanceGenericParamOpen,
            del => AssertEquals(typeof(T), del(new Generic<T>(), default)),
            typeof(Generic<T>).GetMethod(nameof(Generic<>.InstanceGenericParam), Constants.InstanceFlags));
        AssertValid(GenericIl<T>.GetInstanceDoubleGenericOpen<T>,
            del => AssertEquals((typeof(T), typeof(T)), del(new Generic<T>())),
            typeof(Generic<T>).GetMethod(nameof(Generic<>.InstanceDoubleGeneric), Constants.InstanceFlags)?.MakeGenericMethod(typeof(T)));

        AssertValid(GenericIl<T>.GetInstanceVirtualOpen,
            del => AssertEquals(Constants.TestString, del(new Generic<T>())),
            typeof(Generic<T>).GetMethod(nameof(Generic<>.Virtual), Constants.InstanceFlags));

        AssertValid(Generic<T>.GetStaticBasicClosed,
            del => del(),
            typeof(Generic<T>).GetMethod(nameof(Generic<>.StaticBasic), Constants.StaticFlags));
        AssertValid(Generic<T>.GetStaticReturnClosed,
            del => AssertEquals(Constants.TestInt, del()),
            typeof(Generic<T>).GetMethod(nameof(Generic<>.StaticReturn), Constants.StaticFlags));
        AssertValid(Generic<T>.GetStaticParamClosed,
            del => AssertEquals(null, del()),
            typeof(Generic<T>).GetMethod(nameof(Generic<>.StaticParam), Constants.StaticFlags));
        AssertValid(Generic<T>.GetStaticParamOpen,
            del => AssertEquals(Constants.TestString, del(Constants.TestString)),
            typeof(Generic<T>).GetMethod(nameof(Generic<>.StaticParam), Constants.StaticFlags));
        AssertValid(Generic<T>.GetStaticGenericClosed,
            del => AssertEquals(typeof(T), del()),
            typeof(Generic<T>).GetMethod(nameof(Generic<>.StaticGeneric), Constants.StaticFlags));
        AssertValid(Generic<T>.GetStaticGenericParamOpen,
            del => AssertEquals(typeof(T), del(default)),
            typeof(Generic<T>).GetMethod(nameof(Generic<>.StaticGenericParam), Constants.StaticFlags));
        AssertValid(Generic<T>.GetStaticDoubleGenericOpen<T>,
            del => AssertEquals((typeof(T), typeof(T)), del()),
            typeof(Generic<T>).GetMethod(nameof(Generic<>.StaticDoubleGeneric), Constants.StaticFlags)?.MakeGenericMethod(typeof(T)));

        AssertValid(StaticResolver<Generic<T>>.GetInterfaceStaticAbstract,
            del => AssertEquals(Constants.TestString, del()),
            typeof(Generic<T>).GetMethod(nameof(IStaticInterface.InterfaceStaticAbstract), Constants.StaticFlags));
        AssertValid(StaticResolver<Generic<T>>.GetInterfaceStaticVirtual,
            del => AssertEquals(Constants.TestString, del()),
            typeof(IStaticInterface).GetMethod(nameof(IStaticInterface.InterfaceStaticVirtual), Constants.StaticFlags));
        AssertValid(StaticResolver<Generic<T>>.GetInterfaceStaticVirtualOverriden,
            del => AssertEquals(Constants.TestString, del()),
            typeof(Generic<T>).GetMethod(nameof(IStaticInterface.InterfaceStaticVirtualOverriden), Constants.StaticFlags));

        AssertValid(VirtualResolver.GetInterfaceInstance,
            del => AssertEquals(Constants.TestString, del(new Generic<T>())),
            typeof(IInterface).GetMethod(nameof(IInterface.InterfaceInstance), Constants.InstanceFlags));
        AssertValid(VirtualResolver.GetInterfaceInstanceDim,
            del => AssertEquals(Constants.TestString, del(new Generic<T>())),
            typeof(IInterface).GetMethod(nameof(IInterface.InterfaceInstanceDim), Constants.InstanceFlags));
        AssertValid(VirtualResolver.GetInterfaceInstanceDimOverriden,
            del => AssertEquals(Constants.TestString, del(new Generic<T>())),
            typeof(IInterface).GetMethod(nameof(IInterface.InterfaceInstanceDimOverriden), Constants.InstanceFlags));

        AssertValid(VirtualResolver.GetBaseAbstract,
            del => AssertEquals(Constants.TestString, del(new Generic<T>())),
            typeof(BaseClass).GetMethod(nameof(BaseClass.BaseAbstract), Constants.InstanceFlags));
        AssertValid(VirtualResolver.GetBaseVirtual,
            del => AssertEquals(Constants.TestString, del(new Generic<T>())),
            typeof(BaseClass).GetMethod(nameof(BaseClass.BaseVirtual), Constants.InstanceFlags));
        AssertValid(VirtualResolver.GetBaseVirtualOverriden,
            del => AssertEquals(Constants.TestString, del(new Generic<T>())),
            typeof(BaseClass).GetMethod(nameof(BaseClass.BaseVirtualOverriden), Constants.InstanceFlags));

        if (typeof(T).IsValueType)
        {
            AssertThrows<NotSupportedException>(() => Generic<T>.GetStaticGenericParamClosed());
        }
        else
        {
            AssertValid(Generic<T>.GetStaticGenericParamClosed,
                del => AssertEquals(typeof(T), del()),
                typeof(Generic<T>).GetMethod(nameof(Generic<>.StaticGenericParam), Constants.StaticFlags));
        }

        AssertThrows<NotSupportedException>(() => GenericIl<T>.GetInstanceBasicClosed());
        AssertThrows<NotSupportedException>(() => GenericIl<T>.GetInstanceReturnClosed());
        AssertThrows<NotSupportedException>(() => GenericIl<T>.GetInstanceParamClosed());
        AssertThrows<NotSupportedException>(() => GenericIl<T>.GetInstanceGenericClosed());
        AssertThrows<NotSupportedException>(() => GenericIl<T>.GetInstanceGenericParamClosed());
        AssertThrows<NotSupportedException>(() => GenericIl<T>.GetInstanceDoubleGenericClosed<T>());
        AssertThrows<NotSupportedException>(() => GenericIl<T>.GetInstanceVirtualClosed());
    }

    public static void TestNonGenericStruct<T>()
    {
        AssertValid(NonGenericStructIl.GetInstanceGenericOpen<T>,
            del => AssertEquals(typeof(T), del(new NonGenericStruct())),
            typeof(NonGenericStruct).GetMethod(nameof(NonGenericStruct.InstanceGeneric), Constants.InstanceFlags)?.MakeGenericMethod(typeof(T)));
        AssertValid(NonGenericStructIl.GetInstanceGenericParamOpen<T>,
            del => AssertEquals(typeof(T), del(new NonGenericStruct(), default)),
            typeof(NonGenericStruct).GetMethod(nameof(NonGenericStruct.InstanceGenericParam), Constants.InstanceFlags)?.MakeGenericMethod(typeof(T)));

        AssertValid(NonGenericStruct.GetStaticGenericClosed<T>,
            del => AssertEquals(typeof(T), del()),
            typeof(NonGenericStruct).GetMethod(nameof(NonGenericStruct.StaticGeneric), Constants.StaticFlags)?.MakeGenericMethod(typeof(T)));
        AssertValid(NonGenericStruct.GetStaticGenericParamOpen<T>,
            del => AssertEquals(typeof(T), del(default)),
            typeof(NonGenericStruct).GetMethod(nameof(NonGenericStruct.StaticGenericParam), Constants.StaticFlags)?.MakeGenericMethod(typeof(T)));

        if (typeof(T).IsValueType)
        {
            AssertThrows<NotSupportedException>(() => NonGenericStruct.GetStaticGenericParamClosed<T>());
        }
        else
        {
            AssertValid(NonGenericStruct.GetStaticGenericParamClosed<T>,
                del => AssertEquals(typeof(T), del()),
                typeof(NonGenericStruct).GetMethod(nameof(NonGenericStruct.StaticGenericParam), Constants.StaticFlags)?.MakeGenericMethod(typeof(T)));
        }

        AssertThrows<NotSupportedException>(() => NonGenericStructIl.GetInstanceGenericClosed<T>());
        AssertThrows<NotSupportedException>(() => NonGenericStructIl.GetInstanceGenericParamClosed<T>());
    }

    public static void TestGenericStruct<T>()
    {
        AssertValid(GenericStructIl<T>.GetInstanceBasicOpen,
            del => del(new GenericStruct<T>()),
            typeof(GenericStruct<T>).GetMethod(nameof(GenericStruct<>.InstanceBasic), Constants.InstanceFlags));
        AssertValid(GenericStructIl<T>.GetInstanceReturnOpen,
            del => AssertEquals(Constants.TestInt, del(new GenericStruct<T>())),
            typeof(GenericStruct<T>).GetMethod(nameof(GenericStruct<>.InstanceReturn), Constants.InstanceFlags));
        AssertValid(GenericStructIl<T>.GetInstanceParamOpen,
            del => AssertEquals(Constants.TestString, del(new GenericStruct<T>(), Constants.TestString)),
            typeof(GenericStruct<T>).GetMethod(nameof(GenericStruct<>.InstanceParam), Constants.InstanceFlags));
        AssertValid(GenericStructIl<T>.GetInstanceGenericOpen,
            del => AssertEquals(typeof(T), del(new GenericStruct<T>())),
            typeof(GenericStruct<T>).GetMethod(nameof(GenericStruct<>.InstanceGeneric), Constants.InstanceFlags));
        AssertValid(GenericStructIl<T>.GetInstanceGenericParamOpen,
            del => AssertEquals(typeof(T), del(new GenericStruct<T>(), default)),
            typeof(GenericStruct<T>).GetMethod(nameof(GenericStruct<>.InstanceGenericParam), Constants.InstanceFlags));
        AssertValid(GenericStructIl<T>.GetInstanceDoubleGenericOpen<T>,
            del => AssertEquals((typeof(T), typeof(T)), del(new GenericStruct<T>())),
            typeof(GenericStruct<T>).GetMethod(nameof(GenericStruct<>.InstanceDoubleGeneric), Constants.InstanceFlags)?.MakeGenericMethod(typeof(T)));

        AssertValid(GenericStruct<T>.GetStaticBasicClosed,
            del => del(),
            typeof(GenericStruct<T>).GetMethod(nameof(GenericStruct<>.StaticBasic), Constants.StaticFlags));
        AssertValid(GenericStruct<T>.GetStaticReturnClosed,
            del => AssertEquals(Constants.TestInt, del()),
            typeof(GenericStruct<T>).GetMethod(nameof(GenericStruct<>.StaticReturn), Constants.StaticFlags));
        AssertValid(GenericStruct<T>.GetStaticParamClosed,
            del => AssertEquals(null, del()),
            typeof(GenericStruct<T>).GetMethod(nameof(GenericStruct<>.StaticParam), Constants.StaticFlags));
        AssertValid(GenericStruct<T>.GetStaticParamOpen,
            del => AssertEquals(Constants.TestString, del(Constants.TestString)),
            typeof(GenericStruct<T>).GetMethod(nameof(GenericStruct<>.StaticParam), Constants.StaticFlags));
        AssertValid(GenericStruct<T>.GetStaticGenericClosed,
            del => AssertEquals(typeof(T), del()),
            typeof(GenericStruct<T>).GetMethod(nameof(GenericStruct<>.StaticGeneric), Constants.StaticFlags));
        AssertValid(GenericStruct<T>.GetStaticGenericParamOpen,
            del => AssertEquals(typeof(T), del(default)),
            typeof(GenericStruct<T>).GetMethod(nameof(GenericStruct<>.StaticGenericParam), Constants.StaticFlags));
        AssertValid(GenericStruct<T>.GetStaticDoubleGenericOpen<T>,
            del => AssertEquals((typeof(T), typeof(T)), del()),
            typeof(GenericStruct<T>).GetMethod(nameof(GenericStruct<>.StaticDoubleGeneric), Constants.StaticFlags)?.MakeGenericMethod(typeof(T)));

        AssertValid(StaticResolver<GenericStruct<T>>.GetInterfaceStaticAbstract,
            del => AssertEquals(Constants.TestString, del()),
            typeof(GenericStruct<T>).GetMethod(nameof(IStaticInterface.InterfaceStaticAbstract), Constants.StaticFlags));
        AssertValid(StaticResolver<GenericStruct<T>>.GetInterfaceStaticVirtual,
            del => AssertEquals(Constants.TestString, del()),
            typeof(IStaticInterface).GetMethod(nameof(IStaticInterface.InterfaceStaticVirtual), Constants.StaticFlags));
        AssertValid(StaticResolver<GenericStruct<T>>.GetInterfaceStaticVirtualOverriden,
            del => AssertEquals(Constants.TestString, del()),
            typeof(GenericStruct<T>).GetMethod(nameof(IStaticInterface.InterfaceStaticVirtualOverriden), Constants.StaticFlags));

        AssertValid(VirtualResolver.GetInterfaceInstance,
            del => AssertEquals(Constants.TestString, del(new GenericStruct<T>())),
            typeof(IInterface).GetMethod(nameof(IInterface.InterfaceInstance), Constants.InstanceFlags));
        AssertValid(VirtualResolver.GetInterfaceInstanceDim,
            del => AssertEquals(Constants.TestString, del(new GenericStruct<T>())),
            typeof(IInterface).GetMethod(nameof(IInterface.InterfaceInstanceDim), Constants.InstanceFlags));
        AssertValid(VirtualResolver.GetInterfaceInstanceDimOverriden,
            del => AssertEquals(Constants.TestString, del(new GenericStruct<T>())),
            typeof(IInterface).GetMethod(nameof(IInterface.InterfaceInstanceDimOverriden), Constants.InstanceFlags));

        if (typeof(T).IsValueType)
        {
            AssertThrows<NotSupportedException>(() => GenericStruct<T>.GetStaticGenericParamClosed());
        }
        else
        {
            AssertValid(GenericStruct<T>.GetStaticGenericParamClosed,
                del => AssertEquals(typeof(T), del()),
                typeof(GenericStruct<T>).GetMethod(nameof(GenericStruct<>.StaticGenericParam), Constants.StaticFlags));
        }

        AssertThrows<NotSupportedException>(() => GenericStructIl<T>.GetInstanceBasicClosed());
        AssertThrows<NotSupportedException>(() => GenericStructIl<T>.GetInstanceReturnClosed());
        AssertThrows<NotSupportedException>(() => GenericStructIl<T>.GetInstanceParamClosed());
        AssertThrows<NotSupportedException>(() => GenericStructIl<T>.GetInstanceGenericClosed());
        AssertThrows<NotSupportedException>(() => GenericStructIl<T>.GetInstanceGenericParamClosed());
        AssertThrows<NotSupportedException>(() => GenericStructIl<T>.GetInstanceDoubleGenericClosed<T>());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void AssertValid<TDelegate>(Func<TDelegate> f, Action<TDelegate> a, MethodInfo? info, bool recursed = false, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "") where TDelegate : Delegate
    {
        try
        {
            TDelegate del = f();

            AssertEquals(false, del is null, line: line, file: file);
            AssertEquals(false, del!.Method is null, line: line, file: file);
            AssertEquals(false, del.Method!.DeclaringType is null, line: line, file: file);

            AssertEquals(typeof(TDelegate), GetType(del), line: line, file: file);

            AssertEquals(false, del.Method.IsGenericMethodDefinition, line: line, file: file);
            AssertEquals(false, del.Method.DeclaringType!.IsGenericTypeDefinition, line: line, file: file);

            AssertEquals(true, del.Target is null, line: line, file: file);

            a(del);

            AssertEquals(false, info is null, line: line, file: file);

            if (info is null)
                return;

            AssertEquals(info, del.Method, line: line, file: file);
            AssertEquals(info.DeclaringType, del.Method.DeclaringType, line: line, file: file);

            if (!recursed)
            {
                // verify fallback path works
                // this is fine on AOT due to normal creation guaranteeing the method and stubs are present
                AssertValid(() =>
                {
                    TDelegate? unused = null;
                    return RuntimeHelpers.GetDelegate(info!.MethodHandle.GetFunctionPointer(), ref unused);
                }, a, info, true);
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine($"{file}:L{line} exception: {exception}");
            _errors++;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void AssertEquals<T>(T left, T right, [CallerArgumentExpression("left")] string a = "", [CallerArgumentExpression("right")] string b = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
    {
        if (EqualityComparer<T>.Default.Equals(left, right))
            return;
        Console.WriteLine($"{file}:L{line} test failed ({a}: {left}, {b}: {right}).");
        _errors++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void AssertThrows<T>(Action action, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "") where T : Exception
    {
        try
        {
            action();
            Console.WriteLine($"{file}:L{line} test failed (expected {typeof(T).Name}).");
            _errors++;
        }
        catch (T)
        {
            // ignore
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type? GetType(object? o)
    {
        return o?.GetType();
    }
}
