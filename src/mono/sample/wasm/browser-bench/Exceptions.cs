// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Sample
{
    class ExceptionsTask : BenchTask
    {
        public override string Name => "Exceptions";
        Measurement[] measurements;

        public ExceptionsTask()
        {
            measurements = new Measurement[] {
                new NoExceptionHandling(),
                new TryCatch(),
                new TryCatchThrow(),
                new TryCatchFilter(),
                new TryCatchFilterInline(),
                //new TryCatchFilterThrow(),
                //new TryCatchFilterThrowApplies(),
            };
        }

        public override Measurement[] Measurements
        {
            get
            {
                return measurements;
            }
        }

        public override void Initialize()
        {
        }

        public abstract class ExcMeasurement : BenchTask.Measurement
        {
            public override int InitialSamples => 10000;
        }

        class NoExceptionHandling : ExcMeasurement
        {
            public override string Name => "NoExceptionHandling";
            public override int InitialSamples => 1000000;
            bool increaseCounter = false;
            int unusedCounter;

            public override void RunStep()
            {
                DoNothing();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            void DoNothing ()
            {
                if (increaseCounter)
                    unusedCounter++;
            }
        }

        class TryCatch : ExcMeasurement
        {
            public override string Name => "TryCatch";
            public override int InitialSamples => 1000000;
            bool doThrow = false;

            public override void RunStep()
            {
                try
                {
                    DoNothing();
                } catch
                {
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            void DoNothing ()
            {
                if (doThrow)
                    throw new Exception ("Reached DoThrow and throwed");
            }
        }

        class TryCatchThrow : ExcMeasurement
        {
            public override string Name => "TryCatchThrow";
            bool doThrow = true;

            public override void RunStep()
            {
                try
                {
                    DoThrow();
                }
                catch
                {
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            void DoThrow()
            {
                if (doThrow)
                    throw new System.Exception("Reached DoThrow and throwed");
            }
        }

        class TryCatchFilter : ExcMeasurement
        {
            public override string Name => "TryCatchFilter";
            bool doThrow = false;

            public override void RunStep()
            {
                try
                {
                    DoNothing();
                }
                catch (Exception e) when (e.Message == "message")
                {
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            void DoNothing()
            {
                if (doThrow)
                    throw new Exception("Reached DoThrow and throwed");
            }
        }

        class TryCatchFilterInline : ExcMeasurement
        {
            public override string Name => "TryCatchFilterInline";
            bool doThrow = false;

            public override void RunStep()
            {
                try
                {
                    DoNothing();
                }
                catch (Exception e) when (e.Message == "message")
                {
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void DoNothing()
            {
                if (doThrow)
                    throw new Exception("Reached DoThrow and throwed");
            }
        }

        class TryCatchFilterThrow : ExcMeasurement
        {
            public override string Name => "TryCatchFilterThrow";
            bool doThrow = true;

            public override void RunStep()
            {
                try
                {
                    DoThrow();
                }
                catch (Exception e) when (e.Message == "message")
                {
                }
                catch
                {
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            void DoThrow()
            {
                if (doThrow)
                    throw new System.Exception("Reached DoThrow and throwed");
            }
        }

        class TryCatchFilterThrowApplies : ExcMeasurement
        {
            public override string Name => "TryCatchFilterThrowApplies";
            bool doThrow = true;

            public override void RunStep()
            {
                try
                {
                    DoThrow();
                }
                catch (Exception e) when (e.Message == "Reached DoThrow and throwed")
                {
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            void DoThrow()
            {
                if (doThrow)
                    throw new System.Exception("Reached DoThrow and throwed");
            }
        }
    }
}
