// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace System.Threading
{
    internal sealed partial class PortableThreadPool
    {
        /// <summary>
        /// Hill climbing algorithm used for determining the number of threads needed for the thread pool.
        /// </summary>
        private sealed partial class HillClimbing
        {
            private const int LogCapacity = 200;
            private const int DefaultSampleIntervalMsLow = 10;
            private const int DefaultSampleIntervalMsHigh = 200;

            public static readonly bool IsDisabled = AppContextConfigHelper.GetBooleanConfig("System.Threading.ThreadPool.HillClimbing.Disable", false);

            // SOS's ThreadPool command depends on this name
            public static readonly HillClimbing ThreadPoolHillClimber = new HillClimbing();

            // SOS's ThreadPool command depends on the enum values
            public enum StateOrTransition
            {
                Warmup,
                Initializing,
                RandomMove,
                ClimbingMove,
                ChangePoint,
                Stabilizing,
                Starvation,
                ThreadTimedOut,
            }

            // SOS's ThreadPool command depends on the names of all fields
            private struct LogEntry
            {
                public int tickCount;
                public StateOrTransition stateOrTransition;
                public int newControlSetting;
                public int lastHistoryCount;
                public float lastHistoryMean;
            }

            private readonly int _wavePeriod;
            private readonly int _samplesToMeasure;
            private readonly double _targetThroughputRatio;
            private readonly double _targetSignalToNoiseRatio;
            private readonly double _maxChangePerSecond;
            private readonly double _maxChangePerSample;
            private readonly int _maxThreadWaveMagnitude;
            private readonly int _sampleIntervalMsLow;
            private readonly double _threadMagnitudeMultiplier;
            private readonly int _sampleIntervalMsHigh;
            private readonly double _throughputErrorSmoothingFactor;
            private readonly double _gainExponent;
            private readonly double _maxSampleError;

            private double _currentControlSetting;
            private long _totalSamples;
            private int _lastThreadCount;
            private double _averageThroughputNoise;
            private double _secondsElapsedSinceLastChange;
            private double _completionsSinceLastChange;
            private int _accumulatedCompletionCount;
            private double _accumulatedSampleDurationSeconds;
            private readonly double[] _samples;
            private readonly double[] _threadCounts;
            private int _currentSampleMs;

            private readonly Random _randomIntervalGenerator = new Random();

            private readonly LogEntry[] _log = new LogEntry[LogCapacity]; // SOS's ThreadPool command depends on this name
            private int _logStart; // SOS's ThreadPool command depends on this name
            private int _logSize; // SOS's ThreadPool command depends on this name

            public HillClimbing()
            {
                _wavePeriod = AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.WavePeriod", 4, false);
                _maxThreadWaveMagnitude = AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.MaxWaveMagnitude", 20, false);
                _threadMagnitudeMultiplier = AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.WaveMagnitudeMultiplier", 100, false) / 100.0;
                _samplesToMeasure = _wavePeriod * AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.WaveHistorySize", 8, false);
                _targetThroughputRatio = AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.Bias", 15, false) / 100.0;
                _targetSignalToNoiseRatio = AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.TargetSignalToNoiseRatio", 300, false) / 100.0;
                _maxChangePerSecond = AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.MaxChangePerSecond", 4, false);
                _maxChangePerSample = AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.MaxChangePerSample", 20, false);
                int sampleIntervalMsLow = AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.SampleIntervalLow", DefaultSampleIntervalMsLow, false);
                int sampleIntervalMsHigh = AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.SampleIntervalHigh", DefaultSampleIntervalMsHigh, false);
                if (sampleIntervalMsLow <= sampleIntervalMsHigh)
                {
                    _sampleIntervalMsLow = sampleIntervalMsLow;
                    _sampleIntervalMsHigh = sampleIntervalMsHigh;
                }
                else
                {
                    _sampleIntervalMsLow = DefaultSampleIntervalMsLow;
                    _sampleIntervalMsHigh = DefaultSampleIntervalMsHigh;
                }
                _throughputErrorSmoothingFactor = AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.ErrorSmoothingFactor", 1, false) / 100.0;
                _gainExponent = AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.GainExponent", 200, false) / 100.0;
                _maxSampleError = AppContextConfigHelper.GetInt32Config("System.Threading.ThreadPool.HillClimbing.MaxSampleErrorPercent", 15, false) / 100.0;

                _samples = new double[_samplesToMeasure];
                _threadCounts = new double[_samplesToMeasure];

                _currentSampleMs = _randomIntervalGenerator.Next(_sampleIntervalMsLow, _sampleIntervalMsHigh + 1);
            }

            public (int newThreadCount, int newSampleMs) Update(int currentThreadCount, double sampleDurationSeconds, int numCompletions)
            {

                //
                // If someone changed the thread count without telling us, update our records accordingly.
                //
                if (currentThreadCount != _lastThreadCount)
                    ForceChange(currentThreadCount, StateOrTransition.Initializing);

                //
                // Update the cumulative stats for this thread count
                //
                _secondsElapsedSinceLastChange += sampleDurationSeconds;
                _completionsSinceLastChange += numCompletions;

                //
                // Add in any data we've already collected about this sample
                //
                sampleDurationSeconds += _accumulatedSampleDurationSeconds;
                numCompletions += _accumulatedCompletionCount;

                //
                // We need to make sure we're collecting reasonably accurate data.  Since we're just counting the end
                // of each work item, we are goinng to be missing some data about what really happened during the
                // sample interval.  The count produced by each thread includes an initial work item that may have
                // started well before the start of the interval, and each thread may have been running some new
                // work item for some time before the end of the interval, which did not yet get counted.  So
                // our count is going to be off by +/- threadCount workitems.
                //
                // The exception is that the thread that reported to us last time definitely wasn't running any work
                // at that time, and the thread that's reporting now definitely isn't running a work item now.  So
                // we really only need to consider threadCount-1 threads.
                //
                // Thus the percent error in our count is +/- (threadCount-1)/numCompletions.
                //
                // We cannot rely on the frequency-domain analysis we'll be doing later to filter out this error, because
                // of the way it accumulates over time.  If this sample is off by, say, 33% in the negative direction,
                // then the next one likely will be too.  The one after that will include the sum of the completions
                // we missed in the previous samples, and so will be 33% positive.  So every three samples we'll have
                // two "low" samples and one "high" sample.  This will appear as periodic variation right in the frequency
                // range we're targeting, which will not be filtered by the frequency-domain translation.
                //
                if (_totalSamples > 0 && ((currentThreadCount - 1.0) / numCompletions) >= _maxSampleError)
                {
                    // not accurate enough yet.  Let's accumulate the data so far, and tell the ThreadPool
                    // to collect a little more.
                    _accumulatedSampleDurationSeconds = sampleDurationSeconds;
                    _accumulatedCompletionCount = numCompletions;
                    return (currentThreadCount, 10);
                }

                //
                // We've got enouugh data for our sample; reset our accumulators for next time.
                //
                _accumulatedSampleDurationSeconds = 0;
                _accumulatedCompletionCount = 0;

                //
                // Add the current thread count and throughput sample to our history
                //
                double throughput = numCompletions / sampleDurationSeconds;
                if (NativeRuntimeEventSource.Log.IsEnabled())
                {
                    NativeRuntimeEventSource.Log.ThreadPoolWorkerThreadAdjustmentSample(throughput);
                }

                int sampleIndex = (int)(_totalSamples % _samplesToMeasure);
                _samples[sampleIndex] = throughput;
                _threadCounts[sampleIndex] = currentThreadCount;
                _totalSamples++;

                //
                // Set up defaults for our metrics
                //
                Complex threadWaveComponent = default;
                Complex throughputWaveComponent = default;
                double throughputErrorEstimate = 0;
                Complex ratio = default;
                double confidence = 0;

                StateOrTransition state = StateOrTransition.Warmup;

                //
                // How many samples will we use?  It must be at least the three wave periods we're looking for, and it must also be a whole
                // multiple of the primary wave's period; otherwise the frequency we're looking for will fall between two  frequency bands
                // in the Fourier analysis, and we won't be able to measure it accurately.
                //
                int sampleCount = ((int)Math.Min(_totalSamples - 1, _samplesToMeasure)) / _wavePeriod * _wavePeriod;

                if (sampleCount > _wavePeriod)
                {
                    //
                    // Average the throughput and thread count samples, so we can scale the wave magnitudes later.
                    //
                    double sampleSum = 0;
                    double threadSum = 0;
                    for (int i = 0; i < sampleCount; i++)
                    {
                        sampleSum += _samples[(_totalSamples - sampleCount + i) % _samplesToMeasure];
                        threadSum += _threadCounts[(_totalSamples - sampleCount + i) % _samplesToMeasure];
                    }
                    double averageThroughput = sampleSum / sampleCount;
                    double averageThreadCount = threadSum / sampleCount;

                    if (averageThroughput > 0 && averageThreadCount > 0)
                    {
                        //
                        // Calculate the periods of the adjacent frequency bands we'll be using to measure noise levels.
                        // We want the two adjacent Fourier frequency bands.
                        //
                        double adjacentPeriod1 = sampleCount / (((double)sampleCount / _wavePeriod) + 1);
                        double adjacentPeriod2 = sampleCount / (((double)sampleCount / _wavePeriod) - 1);

                        //
                        // Get the the three different frequency components of the throughput (scaled by average
                        // throughput).  Our "error" estimate (the amount of noise that might be present in the
                        // frequency band we're really interested in) is the average of the adjacent bands.
                        //
                        throughputWaveComponent = GetWaveComponent(_samples, sampleCount, _wavePeriod) / averageThroughput;
                        throughputErrorEstimate = (GetWaveComponent(_samples, sampleCount, adjacentPeriod1) / averageThroughput).Abs();
                        if (adjacentPeriod2 <= sampleCount)
                        {
                            throughputErrorEstimate = Math.Max(throughputErrorEstimate, (GetWaveComponent(_samples, sampleCount, adjacentPeriod2) / averageThroughput).Abs());
                        }

                        //
                        // Do the same for the thread counts, so we have something to compare to.  We don't measure thread count
                        // noise, because there is none; these are exact measurements.
                        //
                        threadWaveComponent = GetWaveComponent(_threadCounts, sampleCount, _wavePeriod) / averageThreadCount;

                        //
                        // Update our moving average of the throughput noise.  We'll use this later as feedback to
                        // determine the new size of the thread wave.
                        //
                        if (_averageThroughputNoise == 0)
                            _averageThroughputNoise = throughputErrorEstimate;
                        else
                            _averageThroughputNoise = (_throughputErrorSmoothingFactor * throughputErrorEstimate) + ((1.0 - _throughputErrorSmoothingFactor) * _averageThroughputNoise);

                        if (threadWaveComponent.Abs() > 0)
                        {
                            //
                            // Adjust the throughput wave so it's centered around the target wave, and then calculate the adjusted throughput/thread ratio.
                            //
                            ratio = (throughputWaveComponent - (_targetThroughputRatio * threadWaveComponent)) / threadWaveComponent;
                            state = StateOrTransition.ClimbingMove;
                        }
                        else
                        {
                            ratio = new Complex(0, 0);
                            state = StateOrTransition.Stabilizing;
                        }

                        //
                        // Calculate how confident we are in the ratio.  More noise == less confident.  This has
                        // the effect of slowing down movements that might be affected by random noise.
                        //
                        double noiseForConfidence = Math.Max(_averageThroughputNoise, throughputErrorEstimate);
                        if (noiseForConfidence > 0)
                            confidence = (threadWaveComponent.Abs() / noiseForConfidence) / _targetSignalToNoiseRatio;
                        else
                            confidence = 1.0; //there is no noise!

                    }
                }

                //
                // We use just the real part of the complex ratio we just calculated.  If the throughput signal
                // is exactly in phase with the thread signal, this will be the same as taking the magnitude of
                // the complex move and moving that far up.  If they're 180 degrees out of phase, we'll move
                // backward (because this indicates that our changes are having the opposite of the intended effect).
                // If they're 90 degrees out of phase, we won't move at all, because we can't tell whether we're
                // having a negative or positive effect on throughput.
                //
                double move = Math.Min(1.0, Math.Max(-1.0, ratio.Real));

                //
                // Apply our confidence multiplier.
                //
                move *= Math.Min(1.0, Math.Max(0.0, confidence));

                //
                // Now apply non-linear gain, such that values around zero are attenuated, while higher values
                // are enhanced.  This allows us to move quickly if we're far away from the target, but more slowly
                // if we're getting close, giving us rapid ramp-up without wild oscillations around the target.
                //
                double gain = _maxChangePerSecond * sampleDurationSeconds;
                move = Math.Pow(Math.Abs(move), _gainExponent) * (move >= 0.0 ? 1 : -1) * gain;
                move = Math.Min(move, _maxChangePerSample);

                //
                // If the result was positive, and CPU is > 95%, refuse the move.
                //
                PortableThreadPool threadPoolInstance = ThreadPoolInstance;
                if (move > 0.0 && threadPoolInstance._cpuUtilization > CpuUtilizationHigh)
                    move = 0.0;

                //
                // Apply the move to our control setting
                //
                _currentControlSetting += move;

                //
                // Calculate the new thread wave magnitude, which is based on the moving average we've been keeping of
                // the throughput error.  This average starts at zero, so we'll start with a nice safe little wave at first.
                //
                int newThreadWaveMagnitude = (int)(0.5 + (_currentControlSetting * _averageThroughputNoise * _targetSignalToNoiseRatio * _threadMagnitudeMultiplier * 2.0));
                newThreadWaveMagnitude = Math.Min(newThreadWaveMagnitude, _maxThreadWaveMagnitude);
                newThreadWaveMagnitude = Math.Max(newThreadWaveMagnitude, 1);

                //
                // Make sure our control setting is within the ThreadPool's limits
                //
                int maxThreads = threadPoolInstance._maxThreads;
                int minThreads = threadPoolInstance._minThreads;

                _currentControlSetting = Math.Min(maxThreads - newThreadWaveMagnitude, _currentControlSetting);
                _currentControlSetting = Math.Max(minThreads, _currentControlSetting);

                //
                // Calculate the new thread count (control setting + square wave)
                //
                int newThreadCount = (int)(_currentControlSetting + newThreadWaveMagnitude * ((_totalSamples / (_wavePeriod / 2)) % 2));

                //
                // Make sure the new thread count doesn't exceed the ThreadPool's limits
                //
                newThreadCount = Math.Min(maxThreads, newThreadCount);
                newThreadCount = Math.Max(minThreads, newThreadCount);

                //
                // Record these numbers for posterity
                //

                if (NativeRuntimeEventSource.Log.IsEnabled())
                {
                    NativeRuntimeEventSource.Log.ThreadPoolWorkerThreadAdjustmentStats(sampleDurationSeconds, throughput, threadWaveComponent.Real, throughputWaveComponent.Real,
                        throughputErrorEstimate, _averageThroughputNoise, ratio.Real, confidence, _currentControlSetting, (ushort)newThreadWaveMagnitude);
                }


                //
                // If all of this caused an actual change in thread count, log that as well.
                //
                if (newThreadCount != currentThreadCount)
                    ChangeThreadCount(newThreadCount, state);

                //
                // Return the new thread count and sample interval.  This is randomized to prevent correlations with other periodic
                // changes in throughput.  Among other things, this prevents us from getting confused by Hill Climbing instances
                // running in other processes.
                //
                // If we're at minThreads, and we seem to be hurting performance by going higher, we can't go any lower to fix this.  So
                // we'll simply stay at minThreads much longer, and only occasionally try a higher value.
                //
                int newSampleInterval;
                if (ratio.Real < 0.0 && newThreadCount == minThreads)
                    newSampleInterval = (int)(0.5 + _currentSampleMs * (10.0 * Math.Min(-ratio.Real, 1.0)));
                else
                    newSampleInterval = _currentSampleMs;

                return (newThreadCount, newSampleInterval);
            }

            private void ChangeThreadCount(int newThreadCount, StateOrTransition state)
            {
                _lastThreadCount = newThreadCount;
                _currentSampleMs = _randomIntervalGenerator.Next(_sampleIntervalMsLow, _sampleIntervalMsHigh + 1);
                double throughput = _secondsElapsedSinceLastChange > 0 ? _completionsSinceLastChange / _secondsElapsedSinceLastChange : 0;
                LogTransition(newThreadCount, throughput, state);
                _secondsElapsedSinceLastChange = 0;
                _completionsSinceLastChange = 0;
            }

            private void LogTransition(int newThreadCount, double throughput, StateOrTransition stateOrTransition)
            {
                // Use the _log array as a circular array for log entries
                int index = (_logStart + _logSize) % LogCapacity;

                if (_logSize == LogCapacity)
                {
                    _logStart = (_logStart + 1) % LogCapacity;
                    _logSize--; // hide this slot while we update it
                }

                ref LogEntry entry = ref _log[index];

                entry.tickCount = Environment.TickCount;
                entry.stateOrTransition = stateOrTransition;
                entry.newControlSetting = newThreadCount;
                entry.lastHistoryCount = (int)(Math.Min(_totalSamples, _samplesToMeasure) / _wavePeriod) * _wavePeriod;
                entry.lastHistoryMean = (float)throughput;

                _logSize++;

                if (NativeRuntimeEventSource.Log.IsEnabled())
                {
                    NativeRuntimeEventSource.Log.ThreadPoolWorkerThreadAdjustmentAdjustment(
                        throughput,
                        (uint)newThreadCount,
                        (NativeRuntimeEventSource.ThreadAdjustmentReasonMap)stateOrTransition);
                }
            }

            public void ForceChange(int newThreadCount, StateOrTransition state)
            {
                if (_lastThreadCount != newThreadCount)
                {
                    _currentControlSetting += newThreadCount - _lastThreadCount;
                    ChangeThreadCount(newThreadCount, state);
                }
            }

            private Complex GetWaveComponent(double[] samples, int numSamples, double period)
            {
                Debug.Assert(numSamples >= period); // can't measure a wave that doesn't fit
                Debug.Assert(period >= 2); // can't measure above the Nyquist frequency
                Debug.Assert(numSamples <= samples.Length); // can't measure more samples than we have

                //
                // Calculate the sinusoid with the given period.
                // We're using the Goertzel algorithm for this.  See http://en.wikipedia.org/wiki/Goertzel_algorithm.
                //

                double w = 2 * Math.PI / period;
                double cos = Math.Cos(w);
                double coeff = 2 * cos;
                double q0 = 0, q1 = 0, q2 = 0;
                for (int i = 0; i < numSamples; ++i)
                {
                    q0 = coeff * q1 - q2 + samples[(_totalSamples - numSamples + i) % _samplesToMeasure];
                    q2 = q1;
                    q1 = q0;
                }
                return new Complex(q1 - q2 * cos, q2 * Math.Sin(w)) / numSamples;
            }
        }
    }
}
