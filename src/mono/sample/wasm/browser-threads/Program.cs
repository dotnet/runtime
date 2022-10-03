// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Sample
{
    public partial class Test
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            return 0;
        }

        [JSImport("Sample.Test.updateProgress", "main.js")]
        static partial void updateProgress(string status);

        internal static void UpdateProgress(string status) => updateProgress(status);

        static Demo _demo = null;

        [JSExport]
        public static void Start(int n)
        {
            var comp = new ExpensiveComputation(n);
            comp.Start();
            _demo = new Demo(UpdateProgress, comp);
        }

        [JSExport]
        public static int Progress()
        {
            if (_demo.Progress())
                return 0; /* done */
            else
                return 1; /* continue */
        }

        [JSExport]
        public static int GetAnswer() { return _demo.Result; }
    }

}

public class ExpensiveComputation
{
    private readonly TaskCompletionSource<int> _tcs = new();
    private readonly int UpTo;
    public ExpensiveComputation(int n) { UpTo = n; }
    public long CallCounter { get; private set; }
    public Task<int> Completion => _tcs.Task;

    public void Start()
    {
        new Thread((o) => ((ExpensiveComputation)o).Run()).Start(this);
    }

    public void Run()
    {
        long result = Fib(UpTo);
        if (result < (long)int.MaxValue)
            _tcs.SetResult((int)result);
        else
            _tcs.SetException(new Exception("Fibonacci computation exceeded Int32.MaxValue"));
    }
    public long Fib(int n)
    {
        CallCounter++;
        // make some garbage every 1000 calls
        if (CallCounter % 1000 == 0)
        {
            AllocateGarbage();
        }
        // and collect it every once in a while
        if (CallCounter % 500000 == 0)
            GC.Collect();

        if (n < 2)
            return n;
        return Fib(n - 1) + Fib(n - 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AllocateGarbage()
    {
        object[] garbage = new object[200];
        garbage[12] = new object();
        garbage[197] = garbage;
    }

}

public class Demo
{
    public class Animation
    {
        private readonly Action<string> _updateProgress;
        private int _counter = 0;

        private readonly IReadOnlyList<string> _animations = new string[] { "⚀", "⚁", "⚂", "⚃", "⚄", "⚅" };

        public void Step(string suffix = "")
        {
            _updateProgress(_animations[_counter++] + suffix);
            if (_counter >= _animations.Count)
            {
                _counter = 0;
            }
        }

        public Animation(Action<string> updateProgress)
        {
            _updateProgress = updateProgress;
        }


    }

    private readonly Action<string> _updateProgress;
    private readonly Animation _animation;
    private readonly ExpensiveComputation _expensiveComputation;

    public Demo(Action<string> updateProgress, ExpensiveComputation comp)
    {
        _updateProgress = updateProgress;
        _animation = new Animation(updateProgress);
        _expensiveComputation = comp;
    }

    public bool Progress()
    {
        _animation.Step($"{_expensiveComputation.CallCounter} calls");
        if (_expensiveComputation.Completion.IsCompleted)
        {
            _updateProgress("✌︎");
            return true;
        }
        else
        {
            return false;
        }
    }

    public int Result => _expensiveComputation.Completion.Result;
}
