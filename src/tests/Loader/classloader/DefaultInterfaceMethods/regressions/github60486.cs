using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;
using TestLibrary;

public interface IPublisher<out TData>
{
    event Action<TData> OnPublish;
}

public interface TestItf1<TT>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    void TestMethod1(IPublisher<TT> publisher, StackFrame[] expectedFrames)
    {
        StackFrame.Validate(Environment.StackTrace, expectedFrames);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void TestMethod2(IPublisher<TT> publisher, StackFrame[] expectedFrames)
    {
        TestMethod3(this, publisher, expectedFrames);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    protected static void TestMethod3(TestItf1<TT> subscriber, IPublisher<TT> publisher, StackFrame[] expectedFrames)
    {
        StackFrame.Validate(Environment.StackTrace, expectedFrames);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void TestMethod4(IPublisher<TT> publisher, StackFrame[] expectedFrames)
    {
        TestMethod3(this, publisher, expectedFrames);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void TestMethod5(IPublisher<TT> publisher, StackFrame[] expectedFrames)
    {
        TestMethod3(this, publisher, expectedFrames);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void TestMethod10(IPublisher<TT> publisher, StackFrame[] expectedFrames)
    {
        TestMethod3(this, publisher, expectedFrames);
    }

    void TestMethod11(IPublisher<TT> publisher, StackFrame[] expectedFrames);
}

public interface TestItf2<TT> : TestItf1<TT>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    void TestItf1<TT>.TestMethod5(IPublisher<TT> publisher, StackFrame[] expectedFrames)
    {
        TestMethod3(this, publisher, expectedFrames);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void TestItf1<TT>.TestMethod10(IPublisher<TT> publisher, StackFrame[] expectedFrames)
    {
        TestMethod3(this, publisher, expectedFrames);
    }
}

public interface TestItf3<TT> : TestItf1<TT>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    void TestMethod6(IPublisher<TT> publisher, StackFrame[] expectedFrames)
    {
        TestMethod3(this, publisher, expectedFrames);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void TestMethod7(IPublisher<TT> publisher, StackFrame[] expectedFrames)
    {
        TestMethod8(this, publisher, expectedFrames);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    protected static void TestMethod8(TestItf1<TT> subscriber, IPublisher<TT> publisher, StackFrame[] expectedFrames)
    {
        StackFrame.Validate(Environment.StackTrace, expectedFrames);
    }

    void TestMethod9(IPublisher<TT> publisher, StackFrame[] expectedFrames);
}

public interface TestItf4<TT> : TestItf3<TT>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    void TestItf3<TT>.TestMethod9(IPublisher<TT> publisher, StackFrame[] expectedFrames)
    {
        TestMethod8(this, publisher, expectedFrames);
    }
}

public class ProgramBase<TT> : TestItf4<TT>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestMethod10(IPublisher<TT> publisher, StackFrame[] expectedFrames)
    {
        TestItf1<TT>.TestMethod3(this, publisher, expectedFrames);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestMethod11(IPublisher<TT> publisher, StackFrame[] expectedFrames)
    {
        TestItf1<TT>.TestMethod3(this, publisher, expectedFrames);
    }
}

public class Program : ProgramBase<InputData>, TestItf2<InputData>
{
    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void TestEntryPoint()
    {
        ValidateCurrentMethod();
        new Program().Start();
        ValidateExceptionStackTrace();
    }

    private static void ValidateCurrentMethod()
    {
        for (int i = 0; i < 2; i++)
        {
            Assert.Equal(nameof(ValidateCurrentMethod), MethodBase.GetCurrentMethod().Name);
            Assert.Equal(nameof(GetCurrentMethodInlineable), GetCurrentMethodInlineable().Name);
            MethodBase genericMethod = GetCurrentMethodGeneric<string>();
            Assert.Equal(nameof(GetCurrentMethodGeneric), genericMethod.Name);
            Assert.True(genericMethod.IsGenericMethodDefinition);
            Assert.Equal(typeof(Program).Assembly, Assembly.GetExecutingAssembly());
            Assert.Equal(typeof(Program).Assembly, Assembly.GetCallingAssembly());
            ValidateRecursiveCurrentMethod(3);
            InputData byref = new InputData { i = 2 };
            ValidateTransitionRoots(new InputData { i = 1 }, ref byref,
                (new InputData { i = 3 }, new InputData { i = 4 }));
        }
    }

    private static MethodBase GetCurrentMethodInlineable() => MethodBase.GetCurrentMethod();

    private static MethodBase GetCurrentMethodGeneric<T>() => MethodBase.GetCurrentMethod();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ValidateTransitionRoots(InputData value, ref InputData byref, (InputData, InputData) pair)
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        Assert.Equal(1, value.i);
        Assert.Equal(2, byref.i);
        Assert.Equal(3, pair.Item1.i);
        Assert.Equal(4, pair.Item2.i);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ValidateRecursiveCurrentMethod(int depth)
    {
        Assert.Equal(nameof(ValidateRecursiveCurrentMethod), MethodBase.GetCurrentMethod().Name);
        if (depth > 0)
        {
            ValidateRecursiveCurrentMethod(depth - 1);
        }
        else
        {
            int frameCount = 0;
            foreach (System.Diagnostics.StackFrame frame in new System.Diagnostics.StackTrace().GetFrames())
            {
                if (frame.GetMethod().Name == nameof(ValidateRecursiveCurrentMethod))
                {
                    frameCount++;
                }
            }

            Assert.Equal(4, frameCount);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowForStackTrace() => throw new Exception();

    private static void ValidateExceptionStackTrace()
    {
        try
        {
            ThrowForStackTrace();
        }
        catch (Exception ex)
        {
            Assert.NotNull(ex.StackTrace);

            int frameCount = 0;
            foreach (string line in ex.StackTrace.Split(
                new string[] { Environment.NewLine },
                StringSplitOptions.None))
            {
                if (line.Contains($"{nameof(Program)}.{nameof(ThrowForStackTrace)}(", StringComparison.Ordinal))
                {
                    frameCount++;
                }
            }

            Assert.Equal(1, frameCount);
        }
    }

    public void Start()
    {
        var t1 = this as TestItf1<InputData>;
        t1.TestMethod1(null, new[] { new StackFrame("TestItf1`1", "TestMethod1") });
        t1.TestMethod2(null, new[] { new StackFrame("TestItf1`1", "TestMethod3"), new StackFrame("TestItf1`1", "TestMethod2") });
        t1.TestMethod4(null, new[] { new StackFrame("TestItf1`1", "TestMethod3"), new StackFrame("Program", "TestMethod4") });
        t1.TestMethod5(null, new[] { new StackFrame("TestItf1`1", "TestMethod3"), new StackFrame(new[] { "TestItf2`1", "TestItf1" }, "TestMethod5") });

        var t3 = this as TestItf3<InputData>;
        t3.TestMethod6(null, new[] { new StackFrame("TestItf1`1", "TestMethod3"), new StackFrame("TestItf3`1", "TestMethod6") });
        t3.TestMethod7(null, new[] { new StackFrame("TestItf3`1", "TestMethod8"), new StackFrame("TestItf3`1", "TestMethod7") });
        t3.TestMethod9(null, new[] { new StackFrame("TestItf3`1", "TestMethod8"), new StackFrame(new[] { "TestItf4`1", "TestItf3" }, "TestMethod9") });

        t1.TestMethod10(null, new[] { new StackFrame("TestItf1`1", "TestMethod3"), new StackFrame("ProgramBase`1", "TestMethod10") });
        t1.TestMethod11(null, new[] { new StackFrame("TestItf1`1", "TestMethod3"), new StackFrame("ProgramBase`1", "TestMethod11") });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestMethod4(IPublisher<InputData> publisher, StackFrame[] expectedFrames)
    {
        TestItf1<InputData>.TestMethod3(this, publisher, expectedFrames);
    }
}

public class InputData
{
    public int i;
}

public class StackFrame
{
    public string [] ClassName { get; set; }
    public string MethodName { get; set; } = string.Empty;

    public StackFrame(string [] className, string methodName)
    {
        ClassName = className;
        MethodName = methodName;
    }

    public StackFrame(string className, string methodName)
    {
        ClassName = new string[] { className };
        MethodName = methodName;
    }

    public static void Validate(string testStack, StackFrame[] expectedFrames)
    {
        int index = 1;

        string[] lines = testStack.Split(
            new string[] { Environment.NewLine },
            StringSplitOptions.None
        );

        //Console.WriteLine(testStack);

        foreach (var frame in expectedFrames)
        {
            var line = lines[index++].Trim();


            if (!line.StartsWith($"at {frame.ClassName[0]}") || !line.Contains($".{frame.MethodName}") || (frame.ClassName.Length > 1 && !line.Contains($".{frame.ClassName[1]}")))
            {
                Console.WriteLine($"Expected {frame.ClassName}.{frame.MethodName} but got {line}");
                Console.WriteLine(testStack);
                Environment.Exit(1);
            }
        }
    }
}