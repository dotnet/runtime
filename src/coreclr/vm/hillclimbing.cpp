// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//=========================================================================

//
// HillClimbing.cpp
//
// Defines classes for the ThreadPool's HillClimbing concurrency-optimization
// algorithm.
//

//=========================================================================

//
// TODO: write an essay about how/why this works.  Maybe put it in BotR?
//

#include "common.h"
#include "hillclimbing.h"
#include "win32threadpool.h"

//
// Default compilation mode is /fp:precise, which disables fp intrinsics. This causes us to pull in FP stuff (sin,cos,etc.) from
// The CRT, and increases our download size by ~5k.  We don't need the extra precision this gets us, so let's switch to
// the intrinsic versions.
//
#ifdef _MSC_VER
#pragma float_control(precise, off)
#endif



const double pi = 3.141592653589793;

void HillClimbing::Initialize()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(!ThreadpoolMgr::UsePortableThreadPool());

    m_wavePeriod = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_HillClimbing_WavePeriod);
    m_maxThreadWaveMagnitude = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_HillClimbing_MaxWaveMagnitude);
    m_threadMagnitudeMultiplier = (double)CLRConfig::GetConfigValue(CLRConfig::INTERNAL_HillClimbing_WaveMagnitudeMultiplier) / 100.0;
    m_samplesToMeasure = m_wavePeriod * (int)CLRConfig::GetConfigValue(CLRConfig::INTERNAL_HillClimbing_WaveHistorySize);
    m_targetThroughputRatio = (double)CLRConfig::GetConfigValue(CLRConfig::INTERNAL_HillClimbing_Bias) / 100.0;
    m_targetSignalToNoiseRatio = (double)CLRConfig::GetConfigValue(CLRConfig::INTERNAL_HillClimbing_TargetSignalToNoiseRatio) / 100.0;
    m_maxChangePerSecond = (double)CLRConfig::GetConfigValue(CLRConfig::INTERNAL_HillClimbing_MaxChangePerSecond);
    m_maxChangePerSample = (double)CLRConfig::GetConfigValue(CLRConfig::INTERNAL_HillClimbing_MaxChangePerSample);
    m_sampleIntervalLow = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_HillClimbing_SampleIntervalLow);
    m_sampleIntervalHigh = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_HillClimbing_SampleIntervalHigh);
    m_throughputErrorSmoothingFactor = (double)CLRConfig::GetConfigValue(CLRConfig::INTERNAL_HillClimbing_ErrorSmoothingFactor) / 100.0;
    m_gainExponent = (double)CLRConfig::GetConfigValue(CLRConfig::INTERNAL_HillClimbing_GainExponent) / 100.0;
    m_maxSampleError = (double)CLRConfig::GetConfigValue(CLRConfig::INTERNAL_HillClimbing_MaxSampleErrorPercent) / 100.0;
    m_currentControlSetting = 0;
    m_totalSamples = 0;
    m_lastThreadCount = 0;
    m_averageThroughputNoise = 0;
    m_elapsedSinceLastChange = 0;
    m_completionsSinceLastChange = 0;
    m_accumulatedCompletionCount = 0;
    m_accumulatedSampleDuration = 0;

    m_samples = new double[m_samplesToMeasure];
    m_threadCounts = new double[m_samplesToMeasure];

    // seed our random number generator with the CLR instance ID and the process ID, to avoid correlations with other CLR ThreadPool instances.
#ifndef DACCESS_COMPILE
    m_randomIntervalGenerator.Init(((int)GetClrInstanceId() << 16) ^ (int)GetCurrentProcessId());
#endif
    m_currentSampleInterval = m_randomIntervalGenerator.Next(m_sampleIntervalLow, m_sampleIntervalHigh+1);
}

int HillClimbing::Update(int currentThreadCount, double sampleDuration, int numCompletions, int* pNewSampleInterval)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!ThreadpoolMgr::UsePortableThreadPool());

#ifdef DACCESS_COMPILE
    return 1;
#else

    //
    // If someone changed the thread count without telling us, update our records accordingly.
    //
    if (currentThreadCount != m_lastThreadCount)
        ForceChange(currentThreadCount, Initializing);

    //
    // Update the cumulative stats for this thread count
    //
    m_elapsedSinceLastChange += sampleDuration;
    m_completionsSinceLastChange += numCompletions;

    //
    // Add in any data we've already collected about this sample
    //
    sampleDuration += m_accumulatedSampleDuration;
    numCompletions += m_accumulatedCompletionCount;

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
    if (m_totalSamples > 0 && ((currentThreadCount-1.0) / numCompletions) >= m_maxSampleError)
    {
        // not accurate enough yet.  Let's accumulate the data so far, and tell the ThreadPool
        // to collect a little more.
        m_accumulatedSampleDuration = sampleDuration;
        m_accumulatedCompletionCount = numCompletions;
        *pNewSampleInterval = 10;
        return currentThreadCount;
    }

    //
    // We've got enouugh data for our sample; reset our accumulators for next time.
    //
    m_accumulatedSampleDuration = 0;
    m_accumulatedCompletionCount = 0;

    //
    // Add the current thread count and throughput sample to our history
    //
    double throughput = (double)numCompletions / sampleDuration;
    FireEtwThreadPoolWorkerThreadAdjustmentSample(throughput, GetClrInstanceId());

    int sampleIndex = m_totalSamples % m_samplesToMeasure;
    m_samples[sampleIndex] = throughput;
    m_threadCounts[sampleIndex] = currentThreadCount;
    m_totalSamples++;

    //
    // Set up defaults for our metrics
    //
    Complex threadWaveComponent = 0;
    Complex throughputWaveComponent = 0;
    double throughputErrorEstimate = 0;
    Complex ratio = 0;
    double confidence = 0;

    HillClimbingStateTransition transition = Warmup;

    //
    // How many samples will we use?  It must be at least the three wave periods we're looking for, and it must also be a whole
    // multiple of the primary wave's period; otherwise the frequency we're looking for will fall between two  frequency bands
    // in the Fourier analysis, and we won't be able to measure it accurately.
    //
    int sampleCount = ((int)min(m_totalSamples-1, m_samplesToMeasure) / m_wavePeriod) * m_wavePeriod;

    if (sampleCount > m_wavePeriod)
    {
        //
        // Average the throughput and thread count samples, so we can scale the wave magnitudes later.
        //
        double sampleSum = 0;
        double threadSum = 0;
        for (int i = 0; i < sampleCount; i++)
        {
            sampleSum += m_samples[(m_totalSamples - sampleCount + i) % m_samplesToMeasure];
            threadSum += m_threadCounts[(m_totalSamples - sampleCount + i) % m_samplesToMeasure];
        }
        double averageThroughput = sampleSum / sampleCount;
        double averageThreadCount = threadSum / sampleCount;

        if (averageThroughput > 0 && averageThreadCount > 0)
        {
            //
            // Calculate the periods of the adjacent frequency bands we'll be using to measure noise levels.
            // We want the two adjacent Fourier frequency bands.
            //
            double adjacentPeriod1 = sampleCount / (((double)sampleCount / (double)m_wavePeriod) + 1);
            double adjacentPeriod2 = sampleCount / (((double)sampleCount / (double)m_wavePeriod) - 1);

            //
            // Get the three different frequency components of the throughput (scaled by average
            // throughput).  Our "error" estimate (the amount of noise that might be present in the
            // frequency band we're really interested in) is the average of the adjacent bands.
            //
            throughputWaveComponent = GetWaveComponent(m_samples, sampleCount, m_wavePeriod) / averageThroughput;
            throughputErrorEstimate = abs(GetWaveComponent(m_samples, sampleCount, adjacentPeriod1) / averageThroughput);
            if (adjacentPeriod2 <= sampleCount)
                throughputErrorEstimate = max(throughputErrorEstimate, abs(GetWaveComponent(m_samples, sampleCount, adjacentPeriod2) / averageThroughput));

            //
            // Do the same for the thread counts, so we have something to compare to.  We don't measure thread count
            // noise, because there is none; these are exact measurements.
            //
            threadWaveComponent = GetWaveComponent(m_threadCounts, sampleCount, m_wavePeriod) / averageThreadCount;

            //
            // Update our moving average of the throughput noise.  We'll use this later as feedback to
            // determine the new size of the thread wave.
            //
            if (m_averageThroughputNoise == 0)
                m_averageThroughputNoise = throughputErrorEstimate;
            else
                m_averageThroughputNoise = (m_throughputErrorSmoothingFactor * throughputErrorEstimate) + ((1.0-m_throughputErrorSmoothingFactor) * m_averageThroughputNoise);

            if (abs(threadWaveComponent) > 0)
            {
                //
                // Adjust the throughput wave so it's centered around the target wave, and then calculate the adjusted throughput/thread ratio.
                //
                ratio = (throughputWaveComponent - (m_targetThroughputRatio * threadWaveComponent)) / threadWaveComponent;
                transition = ClimbingMove;
            }
            else
            {
                ratio = 0;
                transition = Stabilizing;
            }

            //
            // Calculate how confident we are in the ratio.  More noise == less confident.  This has
            // the effect of slowing down movements that might be affected by random noise.
            //
            double noiseForConfidence = max(m_averageThroughputNoise, throughputErrorEstimate);
            if (noiseForConfidence > 0)
                confidence = (abs(threadWaveComponent) / noiseForConfidence) / m_targetSignalToNoiseRatio;
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
    double move = min(1.0, max(-1.0, ratio.r));

    //
    // Apply our confidence multiplier.
    //
    move *= min(1.0, max(0.0, confidence));

    //
    // Now apply non-linear gain, such that values around zero are attenuated, while higher values
    // are enhanced.  This allows us to move quickly if we're far away from the target, but more slowly
    // if we're getting close, giving us rapid ramp-up without wild oscillations around the target.
    //
    double gain = m_maxChangePerSecond * sampleDuration;
    move = pow(fabs(move), m_gainExponent) * (move >= 0.0 ? 1 : -1) * gain;
    move = min(move, m_maxChangePerSample);

    //
    // If the result was positive, and CPU is > 95%, refuse the move.
    //
    if (move > 0.0 && ThreadpoolMgr::cpuUtilization > CpuUtilizationHigh)
        move = 0.0;

    //
    // Apply the move to our control setting
    //
    m_currentControlSetting += move;

    //
    // Calculate the new thread wave magnitude, which is based on the moving average we've been keeping of
    // the throughput error.  This average starts at zero, so we'll start with a nice safe little wave at first.
    //
    int newThreadWaveMagnitude = (int)(0.5 + (m_currentControlSetting * m_averageThroughputNoise * m_targetSignalToNoiseRatio * m_threadMagnitudeMultiplier * 2.0));
    newThreadWaveMagnitude = min(newThreadWaveMagnitude, m_maxThreadWaveMagnitude);
    newThreadWaveMagnitude = max(newThreadWaveMagnitude, 1);

    //
    // Make sure our control setting is within the ThreadPool's limits
    //
    m_currentControlSetting = min(ThreadpoolMgr::MaxLimitTotalWorkerThreads-newThreadWaveMagnitude, m_currentControlSetting);
    m_currentControlSetting = max(ThreadpoolMgr::MinLimitTotalWorkerThreads, m_currentControlSetting);

    //
    // Calculate the new thread count (control setting + square wave)
    //
    int newThreadCount = (int)(m_currentControlSetting + newThreadWaveMagnitude * ((m_totalSamples / (m_wavePeriod/2)) % 2));

    //
    // Make sure the new thread count doesn't exceed the ThreadPool's limits
    //
    newThreadCount = min(ThreadpoolMgr::MaxLimitTotalWorkerThreads, newThreadCount);
    newThreadCount = max(ThreadpoolMgr::MinLimitTotalWorkerThreads, newThreadCount);

    //
    // Record these numbers for posterity
    //
    FireEtwThreadPoolWorkerThreadAdjustmentStats(
        sampleDuration,
        throughput,
        threadWaveComponent.r,
        throughputWaveComponent.r,
        throughputErrorEstimate,
        m_averageThroughputNoise,
        ratio.r,
        confidence,
        m_currentControlSetting,
        (unsigned short)newThreadWaveMagnitude,
        GetClrInstanceId());

    //
    // If all of this caused an actual change in thread count, log that as well.
    //
    if (newThreadCount != currentThreadCount)
        ChangeThreadCount(newThreadCount, transition);

    //
    // Return the new thread count and sample interval.  This is randomized to prevent correlations with other periodic
    // changes in throughput.  Among other things, this prevents us from getting confused by Hill Climbing instances
    // running in other processes.
    //
    // If we're at minThreads, and we seem to be hurting performance by going higher, we can't go any lower to fix this.  So
    // we'll simply stay at minThreads much longer, and only occasionally try a higher value.
    //
    if (ratio.r < 0.0 && newThreadCount == ThreadpoolMgr::MinLimitTotalWorkerThreads)
        *pNewSampleInterval = (int)(0.5 + m_currentSampleInterval * (10.0 * min(-ratio.r, 1.0)));
    else
        *pNewSampleInterval = m_currentSampleInterval;

    return newThreadCount;

#endif //DACCESS_COMPILE
}


void HillClimbing::ForceChange(int newThreadCount, HillClimbingStateTransition transition)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!ThreadpoolMgr::UsePortableThreadPool());

    if (newThreadCount != m_lastThreadCount)
    {
        m_currentControlSetting += (newThreadCount - m_lastThreadCount);
        ChangeThreadCount(newThreadCount, transition);
    }
}


void HillClimbing::ChangeThreadCount(int newThreadCount, HillClimbingStateTransition transition)
{
    LIMITED_METHOD_CONTRACT;

    m_lastThreadCount = newThreadCount;
    m_currentSampleInterval = m_randomIntervalGenerator.Next(m_sampleIntervalLow, m_sampleIntervalHigh+1);
    double throughput = (m_elapsedSinceLastChange > 0) ? (m_completionsSinceLastChange / m_elapsedSinceLastChange) : 0;
    LogTransition(newThreadCount, throughput, transition);
    m_elapsedSinceLastChange = 0;
    m_completionsSinceLastChange = 0;
}


GARY_IMPL(HillClimbingLogEntry, HillClimbingLog, HillClimbingLogCapacity);
GVAL_IMPL(int, HillClimbingLogFirstIndex);
GVAL_IMPL(int, HillClimbingLogSize);


void HillClimbing::LogTransition(int threadCount, double throughput, HillClimbingStateTransition transition)
{
    LIMITED_METHOD_CONTRACT;

#ifndef DACCESS_COMPILE
    int index = (HillClimbingLogFirstIndex + HillClimbingLogSize) % HillClimbingLogCapacity;

    if (HillClimbingLogSize == HillClimbingLogCapacity)
    {
        HillClimbingLogFirstIndex = (HillClimbingLogFirstIndex + 1) % HillClimbingLogCapacity;
        HillClimbingLogSize--; //hide this slot while we update it
    }

    HillClimbingLogEntry* entry = &HillClimbingLog[index];

    entry->TickCount = GetTickCount();
    entry->Transition = transition;
    entry->NewControlSetting = threadCount;

    entry->LastHistoryCount = (int)(min(m_totalSamples, m_samplesToMeasure) / m_wavePeriod) * m_wavePeriod;
    entry->LastHistoryMean = (float) throughput;

    HillClimbingLogSize++;

    FireEtwThreadPoolWorkerThreadAdjustmentAdjustment(
        throughput,
        threadCount,
        transition,
        GetClrInstanceId());

#endif //DACCESS_COMPILE
}

Complex HillClimbing::GetWaveComponent(double* samples, int sampleCount, double period)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!ThreadpoolMgr::UsePortableThreadPool());

    _ASSERTE(sampleCount >= period); //can't measure a wave that doesn't fit
    _ASSERTE(period >= 2); //can't measure above the Nyquist frequency

    //
    // Calculate the sinusoid with the given period.
    // We're using the Goertzel algorithm for this.  See http://en.wikipedia.org/wiki/Goertzel_algorithm.
    //
    double w = 2.0 * pi / period;
    double cosine = cos(w);
    double sine = sin(w);
    double coeff = 2.0 * cosine;
    double q0 = 0, q1 = 0, q2 = 0;

    for (int i = 0; i < sampleCount; i++)
    {
        double sample = samples[(m_totalSamples - sampleCount + i) % m_samplesToMeasure];

        q0 = coeff * q1 - q2 + sample;
        q2 = q1;
        q1 = q0;
    }

    return Complex(q1 - q2 * cosine, q2 * sine) / (double)sampleCount;
}

