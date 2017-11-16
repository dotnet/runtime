// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    /// <summary>
    /// Barrier unit tests
    /// </summary>
    public class BarrierTests
    {
        /// <summary>
        /// Runs all the unit tests
        /// </summary>
        /// <returns>True if all tests succeeded, false if one or more tests failed</returns>
        [Fact]
        public static void RunBarrierConstructorTests()
        {
            RunBarrierTest1_ctor(10, null);
            RunBarrierTest1_ctor(0, null);
        }

        [Fact]
        public static void RunBarrierConstructorTests_NegativeTests()
        {
            RunBarrierTest1_ctor(-1, typeof(ArgumentOutOfRangeException));
            RunBarrierTest1_ctor(int.MaxValue, typeof(ArgumentOutOfRangeException));
        }

        /// <summary>
        /// Testing Barrier constructor
        /// </summary>
        /// <param name="initialCount">The initial barrier count</param>
        /// <param name="exceptionType">Type of the exception in case of invalid input, null for valid cases</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static void RunBarrierTest1_ctor(int initialCount, Type exceptionType)
        {
            try
            {
                Barrier b = new Barrier(initialCount);
                Assert.Equal(initialCount, b.ParticipantCount);
            }
            catch (Exception ex)
            {
                Assert.NotNull(exceptionType);
                Assert.IsType(exceptionType, ex);
            }
        }

        [Fact]
        public static void RunBarrierSignalAndWaitTests()
        {
            RunBarrierTest2_SignalAndWait(1, new TimeSpan(0, 0, 0, 0, -1), true, null);
            RunBarrierTest2_SignalAndWait(5, new TimeSpan(0, 0, 0, 0, 100), false, null);
            RunBarrierTest2_SignalAndWait(5, new TimeSpan(0), false, null);
            RunBarrierTest3_SignalAndWait(3);
        }

        [Fact]
        public static void RunBarrierSignalAndWaitTests_NegativeTests()
        {
            RunBarrierTest2_SignalAndWait(1, new TimeSpan(0, 0, 0, 0, -2), false, typeof(ArgumentOutOfRangeException));
        }

        /// <summary>
        /// Test SignalAndWait sequential
        /// </summary>
        /// <param name="initialCount">The initial barrier participants</param>
        /// <param name="timeout">SignalAndWait timeout</param>
        /// <param name="result">Expected return value</param>
        /// <param name="exceptionType">Type of the exception in case of invalid input, null for valid cases</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static void RunBarrierTest2_SignalAndWait(int initialCount, TimeSpan timeout, bool result, Type exceptionType)
        {
            Barrier b = new Barrier(initialCount);
            try
            {
                Assert.Equal(result, b.SignalAndWait(timeout));
                if (result && b.CurrentPhaseNumber != 1)
                {
                    Assert.Equal(1, b.CurrentPhaseNumber);
                    Assert.Equal(b.ParticipantCount, b.ParticipantsRemaining);
                }
            }
            catch (Exception ex)
            {
                Assert.NotNull(exceptionType);
                Assert.IsType(exceptionType, ex);
            }
        }

        /// <summary>
        /// Test SignalANdWait parallel
        /// </summary>
        /// <param name="initialCount">Initial barrier count</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static void RunBarrierTest3_SignalAndWait(int initialCount)
        {
            Barrier b = new Barrier(initialCount);
            for (int i = 0; i < initialCount - 1; i++)
            {
                Task.Run(() => b.SignalAndWait());
            }
            b.SignalAndWait();

            Assert.Equal(1, b.CurrentPhaseNumber);
            Assert.Equal(b.ParticipantCount, b.ParticipantsRemaining);
        }

        [Fact]
        public static void RunBarrierAddParticipantsTest()
        {
            RunBarrierTest4_AddParticipants(0, 1, null);
            RunBarrierTest4_AddParticipants(5, 3, null);
        }

        [Fact]
        public static void RunBarrierAddParticipantsTest_NegativeTests()
        {
            RunBarrierTest4_AddParticipants(0, 0, typeof(ArgumentOutOfRangeException));
            RunBarrierTest4_AddParticipants(2, -1, typeof(ArgumentOutOfRangeException));
            RunBarrierTest4_AddParticipants(0x00007FFF, 1, typeof(ArgumentOutOfRangeException));
            RunBarrierTest4_AddParticipants(100, int.MaxValue, typeof(ArgumentOutOfRangeException));
        }

        [Fact]
        public static void TooManyParticipants()
        {
            Barrier b = new Barrier(Int16.MaxValue);
            Assert.Throws<InvalidOperationException>(() => b.AddParticipant());
        }

        [Fact]
        public static void RemovingParticipants()
        {
            Barrier b;

            b = new Barrier(1);
            b.RemoveParticipant();
            Assert.Throws<ArgumentOutOfRangeException>(() => b.RemoveParticipant());

            b = new Barrier(1);
            b.RemoveParticipants(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => b.RemoveParticipants(1));

            b = new Barrier(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => b.RemoveParticipants(2));
        }

        [Fact]
        public static async Task RemovingWaitingParticipants()
        {
            Barrier b = new Barrier(4);
            Task t = Task.Run(() =>
            {
                b.SignalAndWait();
            });

            while (b.ParticipantsRemaining > 3)
            {
                await Task.Delay(100);
            }

            b.RemoveParticipants(2); // Legal. Succeeds.

            Assert.Equal(1, b.ParticipantsRemaining);

            Assert.Throws<ArgumentOutOfRangeException>(() => b.RemoveParticipants(20)); // Too few total to remove

            Assert.Equal(1, b.ParticipantsRemaining);

            Assert.Throws<InvalidOperationException>(() => b.RemoveParticipants(2)); // Too few remaining to remove

            Assert.Equal(1, b.ParticipantsRemaining);
            b.RemoveParticipant(); // Barrier survives the incorrect removals, and we can still remove correctly.

            await t; // t can now complete.
        }

        /// <summary>
        /// Test AddParticipants
        /// </summary>
        /// <param name="initialCount">The initial barrier participants count</param>
        /// <param name="participantsToAdd">The participants that will be added</param>
        /// <param name="exceptionType">Type of the exception in case of invalid input, null for valid cases</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static void RunBarrierTest4_AddParticipants(int initialCount, int participantsToAdd, Type exceptionType)
        {
            Barrier b = new Barrier(initialCount);
            try
            {
                Assert.Equal(0, b.AddParticipants(participantsToAdd));
                Assert.Equal(initialCount + participantsToAdd, b.ParticipantCount);
            }
            catch (Exception ex)
            {
                Assert.NotNull(exceptionType);
                Assert.IsType(exceptionType, ex);
            }
        }

        [Fact]
        public static void RunBarrierRemoveParticipantsTests()
        {
            RunBarrierTest5_RemoveParticipants(1, 1, null);
            RunBarrierTest5_RemoveParticipants(10, 7, null);
        }

        [Fact]
        public static void RunBarrierRemoveParticipantsTests_NegativeTests()
        {
            RunBarrierTest5_RemoveParticipants(10, 0, typeof(ArgumentOutOfRangeException));
            RunBarrierTest5_RemoveParticipants(1, -1, typeof(ArgumentOutOfRangeException));
            RunBarrierTest5_RemoveParticipants(5, 6, typeof(ArgumentOutOfRangeException));
        }

        /// <summary>
        /// Test RemoveParticipants
        /// </summary>
        /// <param name="initialCount">The initial barrier participants count</param>
        /// <param name="participantsToRemove">The participants that will be added</param>
        /// <param name="exceptionType">Type of the exception in case of invalid input, null for valid cases</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static void RunBarrierTest5_RemoveParticipants(int initialCount, int participantsToRemove, Type exceptionType)
        {
            Barrier b = new Barrier(initialCount);
            try
            {
                b.RemoveParticipants(participantsToRemove);
                Assert.Equal(initialCount - participantsToRemove, b.ParticipantCount);
            }
            catch (Exception ex)
            {
                Assert.NotNull(exceptionType);
                Assert.IsType(exceptionType, ex);
            }
        }

        /// <summary>
        /// Test Dispose
        /// </summary>
        /// <returns>True if the test succeeded, false otherwise</returns>
        [Fact]
        public static void RunBarrierTest6_Dispose()
        {
            Barrier b = new Barrier(1);
            b.Dispose();
            Assert.Throws<ObjectDisposedException>(() => b.SignalAndWait());
        }

        [Fact]
        public static void SignalBarrierWithoutParticipants()
        {
            using (Barrier b = new Barrier(0))
            {
                Assert.Throws<InvalidOperationException>(() => b.SignalAndWait());
            }
        }

        [Fact]
        public static void RunBarrierTest7a()
        {
            for (int j = 0; j < 100; j++)
            {
                Barrier b = new Barrier(0);
                Action[] actions = new Action[4];
                for (int k = 0; k < 4; k++)
                {
                    actions[k] = (Action)(() =>
                    {
                        for (int i = 0; i < 400; i++)
                        {
                            b.AddParticipant();
                            b.RemoveParticipant();
                        }
                    });
                }

                Task[] tasks = new Task[actions.Length];
                for (int k = 0; k < tasks.Length; k++)
                    tasks[k] = Task.Factory.StartNew((index) => actions[(int)index](), k, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
                Task.WaitAll(tasks);
                Assert.Equal(0, b.ParticipantCount);
            }
        }

        /// <summary>
        /// Test the case when the post phase action throws an exception
        /// </summary>
        /// <returns>True if the test succeeded, false otherwise</returns>
        [Fact]
        public static void RunBarrierTest8_PostPhaseException()
        {
            bool shouldThrow = true;
            int participants = 4;
            Barrier barrier = new Barrier(participants, (b) =>
                {
                    if (shouldThrow)
                        throw new InvalidOperationException();
                });
            int succeededCount = 0;

            // Run threads that will expect BarrierPostPhaseException when they call SignalAndWait, and increment the count in the catch block
            // The BarrierPostPhaseException inner exception should be the real exception thrown by the post phase action
            Task[] threads = new Task[participants];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = Task.Run(() =>
                    {
                        try
                        {
                            barrier.SignalAndWait();
                        }
                        catch (BarrierPostPhaseException ex)
                        {
                            if (ex.InnerException.GetType().Equals(typeof(InvalidOperationException)))
                                Interlocked.Increment(ref succeededCount);
                        }
                    });
            }

            foreach (Task t in threads)
                t.Wait();
            Assert.Equal(participants, succeededCount);
            Assert.Equal(1, barrier.CurrentPhaseNumber);

            // turn off the exception
            shouldThrow = false;

            // Now run the threads again and they shouldn't got the exception, decrement the count if it got the exception
            threads = new Task[participants];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = Task.Run(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();
                    }
                    catch (BarrierPostPhaseException)
                    {
                        Interlocked.Decrement(ref succeededCount);
                    }
                });
            }
            foreach (Task t in threads)
                t.Wait();
            Assert.Equal(participants, succeededCount);
        }

        /// <summary>
        /// Test ithe case when the post phase action throws an exception
        /// </summary>
        /// <returns>True if the test succeeded, false otherwise</returns>
        [Fact]
        public static void RunBarrierTest9_PostPhaseException()
        {
            Barrier barrier = new Barrier(1, (b) => b.SignalAndWait());
            EnsurePostPhaseThrew(barrier);

            barrier = new Barrier(1, (b) => b.Dispose());
            EnsurePostPhaseThrew(barrier);

            barrier = new Barrier(1, (b) => b.AddParticipant());
            EnsurePostPhaseThrew(barrier);

            barrier = new Barrier(1, (b) => b.RemoveParticipant());
            EnsurePostPhaseThrew(barrier);
        }

        [Fact]
        [OuterLoop]
        public static void RunBarrierTest10a()
        {
            // Regression test for Barrier race condition
            for (int j = 0; j < 1000; j++)
            {
                Barrier b = new Barrier(2);
                Task[] tasks = new Task[2];
                var src = new CancellationTokenSource();
                for (int i = 0; i < tasks.Length; i++)
                {
                    tasks[i] = Task.Run(() =>
                    {
                        try
                        {
                            if (b.SignalAndWait(-1, src.Token))
                                src.Cancel();
                        }
                        catch (OperationCanceledException)
                        {
                        }
                    });
                }
                Task.WaitAll(tasks);
            }
        }

        [Fact]
        public static void RunBarrierTest10b()
        {
            // Regression test for Barrier race condition
            for (int j = 0; j < 10; j++)
            {
                Barrier b = new Barrier(2);
                var t1 = Task.Run(() =>
                    {
                        b.SignalAndWait();
                        b.RemoveParticipant();
                        b.SignalAndWait();
                    });

                var t2 = Task.Run(() => b.SignalAndWait());
                Task.WaitAll(t1, t2);
                if (j > 0 && j % 1000 == 0)
                    Debug.WriteLine(" > Finished {0} iterations", j);
            }
        }

        [Fact]
        public static void RunBarrierTest10c()
        {
            for (int j = 0; j < 10; j++)
            {
                Task[] tasks = new Task[2];
                Barrier b = new Barrier(3);
                tasks[0] = Task.Run(() => b.SignalAndWait());
                tasks[1] = Task.Run(() => b.SignalAndWait());

                b.SignalAndWait();
                b.Dispose();

                GC.Collect();

                Task.WaitAll(tasks);
            }
        }

        [Fact]
        public static void PostPhaseException()
        {
            Exception exc = new Exception("inner");

            Assert.NotNull(new BarrierPostPhaseException().Message);
            Assert.NotNull(new BarrierPostPhaseException((string)null).Message);
            Assert.Equal("test", new BarrierPostPhaseException("test").Message);
            Assert.NotNull(new BarrierPostPhaseException(exc).Message);
            Assert.Same(exc, new BarrierPostPhaseException(exc).InnerException);
            Assert.Equal("test", new BarrierPostPhaseException("test", exc).Message);
            Assert.Same(exc, new BarrierPostPhaseException("test", exc).InnerException);
        }

        #region Helper Methods

        /// <summary>
        /// Ensures the post phase action throws if Dispose,SignalAndWait and Add/Remove participants called from it.
        /// </summary>
        private static void EnsurePostPhaseThrew(Barrier barrier)
        {
            BarrierPostPhaseException be = Assert.Throws<BarrierPostPhaseException>(() => barrier.SignalAndWait());
            Assert.IsType<InvalidOperationException>(be.InnerException);
        }

        #endregion
    }
}
