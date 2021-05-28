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

        class TryCatch : ExcMeasurement
        {
            public override string Name => "TryCatch";
            public override int InitialSamples => 1000000;
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
            }
        }

        class TryCatchThrow : ExcMeasurement
        {
            public override string Name => "TryCatchThrow";
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
                throw new System.Exception("Reached DoThrow and throwed");
            }
        }

        class TryCatchFilter : ExcMeasurement
        {
            public override string Name => "TryCatchFilter";
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
            }
        }

        class TryCatchFilterInline : ExcMeasurement
        {
            public override string Name => "TryCatchFilterInline";
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
            }
        }

        class TryCatchFilterThrow : ExcMeasurement
        {
            public override string Name => "TryCatchFilterThrow";
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
                throw new System.Exception("Reached DoThrow and throwed");
            }
        }

        class TryCatchFilterThrowApplies : ExcMeasurement
        {
            public override string Name => "TryCatchFilterThrowApplies";
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
                throw new System.Exception("Reached DoThrow and throwed");
            }
        }
    }
}
