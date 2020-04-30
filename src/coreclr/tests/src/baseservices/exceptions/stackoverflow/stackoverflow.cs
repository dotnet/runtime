using System;
using System.Threading;

namespace TestStackOverflow
{
    struct LargeStruct256
    {
        Guid g0;
        Guid g1;
        Guid g2;
        Guid g3;
        Guid g4;
        Guid g5;
        Guid g6;
        Guid g7;
        Guid g8;
        Guid g9;
        Guid ga;
        Guid gb;
        Guid gc;
        Guid gd;
        Guid ge;
        Guid gf;
    }

    struct LargeStruct4096
    {
        LargeStruct256 s0;
        LargeStruct256 s1;
        LargeStruct256 s2;
        LargeStruct256 s3;
        LargeStruct256 s4;
        LargeStruct256 s5;
        LargeStruct256 s6;
        LargeStruct256 s7;
        LargeStruct256 s8;
        LargeStruct256 s9;
        LargeStruct256 sa;
        LargeStruct256 sb;
        LargeStruct256 sc;
        LargeStruct256 sd;
        LargeStruct256 se;
        LargeStruct256 sf;
    }

    struct LargeStruct65536
    {
        LargeStruct4096 s0;
        LargeStruct4096 s1;
        LargeStruct4096 s2;
        LargeStruct4096 s3;
        LargeStruct4096 s4;
        LargeStruct4096 s5;
        LargeStruct4096 s6;
        LargeStruct4096 s7;
        LargeStruct4096 s8;
        LargeStruct4096 s9;
        LargeStruct4096 sa;
        LargeStruct4096 sb;
        LargeStruct4096 sc;
        LargeStruct4096 sd;
        LargeStruct4096 se;
        LargeStruct4096 sf;
    }
    class Program
    {
        static void InfiniteRecursionA()
        {
            InfiniteRecursionB();
        }

        static void InfiniteRecursionB()
        {
            InfiniteRecursionC();
        }
        static void InfiniteRecursionC()
        {
            InfiniteRecursionA();
        }

        static void InfiniteRecursionA2()
        {
            LargeStruct65536 s;
            InfiniteRecursionB2();
        }

        static void InfiniteRecursionB2()
        {
            LargeStruct65536 s;
            InfiniteRecursionC2();
        }

        static void InfiniteRecursionC2()
        {
            LargeStruct65536 s;
            InfiniteRecursionA2();
        }

        static void MainThreadTest(bool smallframe)
        {
            if (smallframe)
            {
                InfiniteRecursionA();
            }
            else
            {
                InfiniteRecursionA2();
            }
        }

        static void SecondaryThreadsTest(bool smallframe)
        {
            Thread[] threads = new Thread[32];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(() => {
                    if (smallframe)
                    {
                        InfiniteRecursionA();
                    }
                    else
                    {
                        InfiniteRecursionA2();
                    }
                });
                threads[i].Start();
            }

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
            }
        }

        static void Main(string[] args)
        {
            bool smallframe = (args[0] == "smallframe");
            if (args[1] == "secondary")
            {
                SecondaryThreadsTest(smallframe);
            }
            else if (args[1] == "main")
            {
                MainThreadTest(smallframe);
            }
        }
    }
}

