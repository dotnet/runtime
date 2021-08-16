// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace test45929
{
    public class Program
    {
        static int Main(string[] _)
        {
            Console.WriteLine("The test started.");
            Console.WriteLine("Progress:");

            ThreadPool.GetMinThreads(out int _, out int cptMin);
            ThreadPool.SetMinThreads(3500, cptMin);

            Test.Run();

            Console.WriteLine("Finished successfully");

            return 100;
        }

        class Test
        {
            readonly TestCore methods;
            MethodInfo methodInfo;

            public Test()
            {
                methods = new TestCore();
                methodInfo = GetMethod("ExceptionDispatchInfoCaptureThrow");
                if (methodInfo is null)
                {
                    throw new InvalidOperationException("The methodInfo object is missing or empty.");
                }
            }

            public static void Run()
            {
                long progress = 0;
                var test = new Test();
                const int MaxCount = 1000000;
                Parallel.For(
                    0,
                    MaxCount,
                    new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    i =>
                    {
                        if (Interlocked.Increment(ref progress) % 10000 == 0)
                        {
                            Console.WriteLine($"{DateTime.Now} : {progress * 100D / MaxCount:000.0}%");
                        }
                        test.Invoke();
                    });
            }

            public void Invoke()
            {
                try
                {
                    methodInfo.Invoke(methods, null);
                }
                catch
                {
                    // Ignore
                }
            }

            static MethodInfo GetMethod(string methodName)
            {
                foreach (MethodInfo method in typeof(TestCore).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (methodName == method.Name)
                    {
                        return method;
                    }
                }
                return null;
            }

            class TestCore
            {
                const int ExpirySeconds = 1000;

                // An exception instance that gets refreshed every ExpirySeconds.
                (ExceptionDispatchInfo Exception, DateTime CacheExpiryDateTime) exceptionCache;

                /// <summary>
                /// Captures and throws the a cached instance of an exception.
                /// </summary>
                public void ExceptionDispatchInfoCaptureThrow()
                {
                    var error = GetCachedError();
                    error.Throw();
                }

                ExceptionDispatchInfo GetCachedError()
                {
                    try
                    {
                        var cache = exceptionCache;
                        if (cache.Exception != null)
                        {
                            if (exceptionCache.CacheExpiryDateTime > DateTime.UtcNow)
                            {
                                return cache.Exception;
                            }
                        }
                        throw new Exception("Test");
                    }
                    catch (Exception ex)
                    {
                        ExceptionDispatchInfo edi = ExceptionDispatchInfo.Capture(ex);
                        exceptionCache = (edi, DateTime.UtcNow.AddSeconds(ExpirySeconds));
                        return edi;
                    }
                }
            }
        }
    }
}
