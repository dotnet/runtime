// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        internal partial class PerformanceCounterOptions
        {
            internal const int SDDL_REVISION_1 = 1;

            public const int PERF_NO_INSTANCES = -1;  // no instances (see NumInstances above)

            public const int PERF_SIZE_DWORD = 0x00000000;
            public const int PERF_SIZE_LARGE = 0x00000100;
            public const int PERF_SIZE_ZERO = 0x00000200;  // for Zero Length fields
            public const int PERF_SIZE_VARIABLE_LEN = 0x00000300;  // length is In CounterLength field

            public const int PERF_NO_UNIQUE_ID = -1;

            //
            //  select one of the following values to indicate the counter field usage
            //
            public const int PERF_TYPE_NUMBER = 0x00000000;  // a number (not a counter)
            public const int PERF_TYPE_COUNTER = 0x00000400;  // an increasing numeric value
            public const int PERF_TYPE_TEXT = 0x00000800;  // a text field
            public const int PERF_TYPE_ZERO = 0x00000C00;  // displays a zero

            //
            //  If the PERF_TYPE_NUMBER field was selected, then select one of the
            //  following to describe the Number
            //
            public const int PERF_NUMBER_HEX = 0x00000000;  // display as HEX value
            public const int PERF_NUMBER_DECIMAL = 0x00010000;  // display as a decimal integer
            public const int PERF_NUMBER_DEC_1000 = 0x00020000;  // display as a decimal/1000

            //
            //  If the PERF_TYPE_COUNTER value was selected then select one of the
            //  following to indicate the type of counter
            //
            public const int PERF_COUNTER_VALUE = 0x00000000;  // display counter value
            public const int PERF_COUNTER_RATE = 0x00010000;  // divide ctr / delta time
            public const int PERF_COUNTER_FRACTION = 0x00020000;  // divide ctr / base
            public const int PERF_COUNTER_BASE = 0x00030000;  // base value used In fractions
            public const int PERF_COUNTER_ELAPSED = 0x00040000;  // subtract counter from current time
            public const int PERF_COUNTER_QUEUELEN = 0x00050000;  // Use Queuelen processing func.
            public const int PERF_COUNTER_HISTOGRAM = 0x00060000;  // Counter begins or ends a histogram
            public const int PERF_COUNTER_PRECISION = 0x00070000;  // divide ctr / private clock

            //
            //  If the PERF_TYPE_TEXT value was selected, then select one of the
            //  following to indicate the type of TEXT data.
            //
            public const int PERF_TEXT_UNICODE = 0x00000000;  // type of text In text field
            public const int PERF_TEXT_ASCII = 0x00010000;  // ASCII using the CodePage field

            //
            //  Timer SubTypes
            //
            public const int PERF_TIMER_TICK = 0x00000000;  // use system perf. freq for base
            public const int PERF_TIMER_100NS = 0x00100000;  // use 100 NS timer time base units
            public const int PERF_OBJECT_TIMER = 0x00200000;  // use the object timer freq

            //
            //  Any types that have calculations performed can use one or more of
            //  the following calculation modification flags listed here
            //
            public const int PERF_DELTA_COUNTER = 0x00400000;  // compute difference first
            public const int PERF_DELTA_BASE = 0x00800000;  // compute base diff as well
            public const int PERF_INVERSE_COUNTER = 0x01000000;  // show as 1.00-value (assumes:
            public const int PERF_MULTI_COUNTER = 0x02000000;  // sum of multiple instances

            //
            //  Select one of the following values to indicate the display suffix (if any)
            //
            public const int PERF_DISPLAY_NO_SUFFIX = 0x00000000;  // no suffix
            public const int PERF_DISPLAY_PER_SEC = 0x10000000;  // "/sec"
            public const int PERF_DISPLAY_PERCENT = 0x20000000;  // "%"
            public const int PERF_DISPLAY_SECONDS = 0x30000000;  // "secs"
            public const int PERF_DISPLAY_NOSHOW = 0x40000000;  // value is not displayed

            //
            //  Predefined counter types
            //

            // 32-bit Counter.  Divide delta by delta time.  Display suffix: "/sec"
            public const int PERF_COUNTER_COUNTER =
                    (PERF_SIZE_DWORD | PERF_TYPE_COUNTER | PERF_COUNTER_RATE |
                     PERF_TIMER_TICK | PERF_DELTA_COUNTER | PERF_DISPLAY_PER_SEC);


            // 64-bit Timer.  Divide delta by delta time.  Display suffix: "%"
            public const int PERF_COUNTER_TIMER =
                    (PERF_SIZE_LARGE | PERF_TYPE_COUNTER | PERF_COUNTER_RATE |
                    PERF_TIMER_TICK | PERF_DELTA_COUNTER | PERF_DISPLAY_PERCENT);

            // Queue Length Space-Time Product. Divide delta by delta time. No Display Suffix.
            public const int PERF_COUNTER_QUEUELEN_TYPE =
                    (PERF_SIZE_DWORD | PERF_TYPE_COUNTER | PERF_COUNTER_QUEUELEN |
                    PERF_TIMER_TICK | PERF_DELTA_COUNTER | PERF_DISPLAY_NO_SUFFIX);

            // Queue Length Space-Time Product. Divide delta by delta time. No Display Suffix.
            public const int PERF_COUNTER_LARGE_QUEUELEN_TYPE =
                    (PERF_SIZE_LARGE | PERF_TYPE_COUNTER | PERF_COUNTER_QUEUELEN |
                    PERF_TIMER_TICK | PERF_DELTA_COUNTER | PERF_DISPLAY_NO_SUFFIX);

            // Queue Length Space-Time Product using 100 Ns timebase.
            // Divide delta by delta time. No Display Suffix.
            public const int PERF_COUNTER_100NS_QUEUELEN_TYPE =
                        (PERF_SIZE_LARGE | PERF_TYPE_COUNTER | PERF_COUNTER_QUEUELEN |
                        PERF_TIMER_100NS | PERF_DELTA_COUNTER | PERF_DISPLAY_NO_SUFFIX);

            // Queue Length Space-Time Product using Object specific timebase.
            // Divide delta by delta time. No Display Suffix.
            public const int PERF_COUNTER_OBJ_TIME_QUEUELEN_TYPE =
                        (PERF_SIZE_LARGE | PERF_TYPE_COUNTER | PERF_COUNTER_QUEUELEN |
                        PERF_OBJECT_TIMER | PERF_DELTA_COUNTER | PERF_DISPLAY_NO_SUFFIX);

            // 64-bit Counter.  Divide delta by delta time. Display Suffix: "/sec"
            public const int PERF_COUNTER_BULK_COUNT =
                    (PERF_SIZE_LARGE | PERF_TYPE_COUNTER | PERF_COUNTER_RATE |
                    PERF_TIMER_TICK | PERF_DELTA_COUNTER | PERF_DISPLAY_PER_SEC);

            // Indicates the counter is not a  counter but rather Unicode text Display as text.
            public const int PERF_COUNTER_TEXT =
                    (PERF_SIZE_VARIABLE_LEN | PERF_TYPE_TEXT | PERF_TEXT_UNICODE |
                    PERF_DISPLAY_NO_SUFFIX);

            // Indicates the data is a counter  which should not be
            // time averaged on display (such as an error counter on a serial line)
            // Display as is.  No Display Suffix.
            public const int PERF_COUNTER_RAWCOUNT =
                    (PERF_SIZE_DWORD | PERF_TYPE_NUMBER | PERF_NUMBER_DECIMAL |
                    PERF_DISPLAY_NO_SUFFIX);

            // Same as PERF_COUNTER_RAWCOUNT except its size is a large integer
            public const int PERF_COUNTER_LARGE_RAWCOUNT =
                    (PERF_SIZE_LARGE | PERF_TYPE_NUMBER | PERF_NUMBER_DECIMAL |
                    PERF_DISPLAY_NO_SUFFIX);

            // Special case for RAWCOUNT that want to be displayed In hex
            // Indicates the data is a counter  which should not be
            // time averaged on display (such as an error counter on a serial line)
            // Display as is.  No Display Suffix.
            public const int PERF_COUNTER_RAWCOUNT_HEX =
                    (PERF_SIZE_DWORD | PERF_TYPE_NUMBER | PERF_NUMBER_HEX |
                    PERF_DISPLAY_NO_SUFFIX);

            // Same as PERF_COUNTER_RAWCOUNT_HEX except its size is a large integer
            public const int PERF_COUNTER_LARGE_RAWCOUNT_HEX =
                    (PERF_SIZE_LARGE | PERF_TYPE_NUMBER | PERF_NUMBER_HEX |
                    PERF_DISPLAY_NO_SUFFIX);

            // A count which is either 1 or 0 on each sampling interrupt (% busy)
            // Divide delta by delta base. Display Suffix: "%"
            public const int PERF_SAMPLE_FRACTION =
                    (PERF_SIZE_DWORD | PERF_TYPE_COUNTER | PERF_COUNTER_FRACTION |
                    PERF_DELTA_COUNTER | PERF_DELTA_BASE | PERF_DISPLAY_PERCENT);

            // A count which is sampled on each sampling interrupt (queue length)
            // Divide delta by delta time. No Display Suffix.
            public const int PERF_SAMPLE_COUNTER =
                    (PERF_SIZE_DWORD | PERF_TYPE_COUNTER | PERF_COUNTER_RATE |
                    PERF_TIMER_TICK | PERF_DELTA_COUNTER | PERF_DISPLAY_NO_SUFFIX);

            // A label: no data is associated with this counter (it has 0 length)
            // Do not display.
            public const int PERF_COUNTER_NODATA =
                    (PERF_SIZE_ZERO | PERF_DISPLAY_NOSHOW);

            // 64-bit Timer inverse (e.g., idle is measured, but display busy %)
            // Display 100 - delta divided by delta time.  Display suffix: "%"
            public const int PERF_COUNTER_TIMER_INV =
                    (PERF_SIZE_LARGE | PERF_TYPE_COUNTER | PERF_COUNTER_RATE |
                    PERF_TIMER_TICK | PERF_DELTA_COUNTER | PERF_INVERSE_COUNTER |
                    PERF_DISPLAY_PERCENT);

            // The divisor for a sample, used with the previous counter to form a
            // sampled %.  You must check for >0 before dividing by this!  This
            // counter will directly follow the  numerator counter.  It should not
            // be displayed to the user.
            public const int PERF_SAMPLE_BASE =
                    (PERF_SIZE_DWORD | PERF_TYPE_COUNTER | PERF_COUNTER_BASE |
                    PERF_DISPLAY_NOSHOW |
                    0x00000001);  // for compatibility with pre-beta versions

            // A timer which, when divided by an average base, produces a time
            // In seconds which is the average time of some operation.  This
            // timer times total operations, and  the base is the number of opera-
            // tions.  Display Suffix: "sec"
            public const int PERF_AVERAGE_TIMER =
                    (PERF_SIZE_DWORD | PERF_TYPE_COUNTER | PERF_COUNTER_FRACTION |
                    PERF_DISPLAY_SECONDS);

            // Used as the denominator In the computation of time or count
            // averages.  Must directly follow the numerator counter.  Not dis-
            // played to the user.
            public const int PERF_AVERAGE_BASE =
                    (PERF_SIZE_DWORD | PERF_TYPE_COUNTER | PERF_COUNTER_BASE |
                    PERF_DISPLAY_NOSHOW |
                    0x00000002);  // for compatibility with pre-beta versions


            // 64-bit Timer in object specific units. Display delta divided by
            // delta time as returned in the object type header structure.  Display suffix: "%"
            public const int PERF_OBJ_TIME_TIMER =
                        (PERF_SIZE_LARGE | PERF_TYPE_COUNTER | PERF_COUNTER_RATE |
                         PERF_OBJECT_TIMER | PERF_DELTA_COUNTER | PERF_DISPLAY_PERCENT);

            // A bulk count which, when divided (typically) by the number of
            // operations, gives (typically) the number of bytes per operation.
            // No Display Suffix.
            public const int PERF_AVERAGE_BULK =
                    (PERF_SIZE_LARGE | PERF_TYPE_COUNTER | PERF_COUNTER_FRACTION |
                    PERF_DISPLAY_NOSHOW);

            // 64-bit Timer in object specific units. Display delta divided by
            // delta time as returned in the object type header structure.  Display suffix: "%"
            public const int PERF_OBJ_TIME_TIME =
                        (PERF_SIZE_LARGE | PERF_TYPE_COUNTER | PERF_COUNTER_RATE |
                         PERF_OBJECT_TIMER | PERF_DELTA_COUNTER | PERF_DISPLAY_PERCENT);

            // 64-bit Timer In 100 nsec units. Display delta divided by
            // delta time.  Display suffix: "%"
            public const int PERF_100NSEC_TIMER =
                    (PERF_SIZE_LARGE | PERF_TYPE_COUNTER | PERF_COUNTER_RATE |
                    PERF_TIMER_100NS | PERF_DELTA_COUNTER | PERF_DISPLAY_PERCENT);

            // 64-bit Timer inverse (e.g., idle is measured, but display busy %)
            // Display 100 - delta divided by delta time.  Display suffix: "%"
            public const int PERF_100NSEC_TIMER_INV =
                    (PERF_SIZE_LARGE | PERF_TYPE_COUNTER | PERF_COUNTER_RATE |
                    PERF_TIMER_100NS | PERF_DELTA_COUNTER | PERF_INVERSE_COUNTER |
                    PERF_DISPLAY_PERCENT);

            // 64-bit Timer.  Divide delta by delta time.  Display suffix: "%"
            // Timer for multiple instances, so result can exceed 100%.
            public const int PERF_COUNTER_MULTI_TIMER =
                    (PERF_SIZE_LARGE | PERF_TYPE_COUNTER | PERF_COUNTER_RATE |
                    PERF_DELTA_COUNTER | PERF_TIMER_TICK | PERF_MULTI_COUNTER |
                    PERF_DISPLAY_PERCENT);

            // 64-bit Timer inverse (e.g., idle is measured, but display busy %)
            // Display 100 * _MULTI_BASE - delta divided by delta time.
            // Display suffix: "%" Timer for multiple instances, so result
            // can exceed 100%.  Followed by a counter of type _MULTI_BASE.
            public const int PERF_COUNTER_MULTI_TIMER_INV =
                    (PERF_SIZE_LARGE | PERF_TYPE_COUNTER | PERF_COUNTER_RATE |
                    PERF_DELTA_COUNTER | PERF_MULTI_COUNTER | PERF_TIMER_TICK |
                    PERF_INVERSE_COUNTER | PERF_DISPLAY_PERCENT);

            // Number of instances to which the preceding _MULTI_..._INV counter
            // applies.  Used as a factor to get the percentage.
            public const int PERF_COUNTER_MULTI_BASE =
                    (PERF_SIZE_LARGE | PERF_TYPE_COUNTER | PERF_COUNTER_BASE |
                    PERF_MULTI_COUNTER | PERF_DISPLAY_NOSHOW);

            // 64-bit Timer In 100 nsec units. Display delta divided by delta time.
            // Display suffix: "%" Timer for multiple instances, so result can exceed 100%.
            public const int PERF_100NSEC_MULTI_TIMER =
                    (PERF_SIZE_LARGE | PERF_TYPE_COUNTER | PERF_DELTA_COUNTER |
                    PERF_COUNTER_RATE | PERF_TIMER_100NS | PERF_MULTI_COUNTER |
                    PERF_DISPLAY_PERCENT);

            // 64-bit Timer inverse (e.g., idle is measured, but display busy %)
            // Display 100 * _MULTI_BASE - delta divided by delta time.
            // Display suffix: "%" Timer for multiple instances, so result
            // can exceed 100%.  Followed by a counter of type _MULTI_BASE.
            public const int PERF_100NSEC_MULTI_TIMER_INV =
                    (PERF_SIZE_LARGE | PERF_TYPE_COUNTER | PERF_DELTA_COUNTER |
                    PERF_COUNTER_RATE | PERF_TIMER_100NS | PERF_MULTI_COUNTER |
                    PERF_INVERSE_COUNTER | PERF_DISPLAY_PERCENT);

            // Indicates the data is a fraction of the following counter  which
            // should not be time averaged on display (such as free space over
            // total space.) Display as is.  Display the quotient as "%".
            public const int PERF_RAW_FRACTION =
                    (PERF_SIZE_DWORD | PERF_TYPE_COUNTER | PERF_COUNTER_FRACTION |
                    PERF_DISPLAY_PERCENT);

            public const int PERF_LARGE_RAW_FRACTION =
                        (PERF_SIZE_LARGE | PERF_TYPE_COUNTER | PERF_COUNTER_FRACTION |
                        PERF_DISPLAY_PERCENT);

            // Indicates the data is a base for the preceding counter which should
            // not be time averaged on display (such as free space over total space.)
            public const int PERF_RAW_BASE =
                    (PERF_SIZE_DWORD | PERF_TYPE_COUNTER | PERF_COUNTER_BASE |
                    PERF_DISPLAY_NOSHOW |
                    0x00000003);  // for compatibility with pre-beta versions

            public const int PERF_LARGE_RAW_BASE =
                        (PERF_SIZE_LARGE | PERF_TYPE_COUNTER | PERF_COUNTER_BASE |
                        PERF_DISPLAY_NOSHOW);

            // The data collected In this counter is actually the start time of the
            // item being measured. For display, this data is subtracted from the
            // sample time to yield the elapsed time as the difference between the two.
            // In the definition below, the PerfTime field of the Object contains
            // the sample time as indicated by the PERF_OBJECT_TIMER bit and the
            // difference is scaled by the PerfFreq of the Object to convert the time
            // units into seconds.
            public const int PERF_ELAPSED_TIME =
                    (PERF_SIZE_LARGE | PERF_TYPE_COUNTER | PERF_COUNTER_ELAPSED |
                    PERF_OBJECT_TIMER | PERF_DISPLAY_SECONDS);

            //
            //  The following counter type can be used with the preceding types to
            //  define a range of values to be displayed In a histogram.
            //

            //
            //  This counter is used to display the difference from one sample
            //  to the next. The counter value is a constantly increasing number
            //  and the value displayed is the difference between the current
            //  value and the previous value. Negative numbers are not allowed
            //  which shouldn't be a problem as long as the counter value is
            //  increasing or unchanged.
            //
            public const int PERF_COUNTER_DELTA =
                    (PERF_SIZE_DWORD | PERF_TYPE_COUNTER | PERF_COUNTER_VALUE |
                    PERF_DELTA_COUNTER | PERF_DISPLAY_NO_SUFFIX);

            public const int PERF_COUNTER_LARGE_DELTA =
                    (PERF_SIZE_LARGE | PERF_TYPE_COUNTER | PERF_COUNTER_VALUE |
                    PERF_DELTA_COUNTER | PERF_DISPLAY_NO_SUFFIX);

            // The timer used has the same frequency as the System Performance Timer
            public const int PERF_PRECISION_SYSTEM_TIMER =
                    (PERF_SIZE_LARGE | PERF_TYPE_COUNTER | PERF_COUNTER_PRECISION |
                     PERF_TIMER_TICK | PERF_DELTA_COUNTER | PERF_DISPLAY_PERCENT);

            //
            // The timer used has the same frequency as the 100 NanoSecond Timer
            public const int PERF_PRECISION_100NS_TIMER =
                    (PERF_SIZE_LARGE | PERF_TYPE_COUNTER | PERF_COUNTER_PRECISION |
                     PERF_TIMER_100NS | PERF_DELTA_COUNTER | PERF_DISPLAY_PERCENT);
            //
            // The timer used is of the frequency specified in the Object header's
            //  PerfFreq field (PerfTime is ignored)
            public const int PERF_PRECISION_OBJECT_TIMER =
                    (PERF_SIZE_LARGE | PERF_TYPE_COUNTER | PERF_COUNTER_PRECISION |
                     PERF_OBJECT_TIMER | PERF_DELTA_COUNTER | PERF_DISPLAY_PERCENT);

            public const uint PDH_FMT_DOUBLE = 0x00000200;
            public const uint PDH_FMT_NOSCALE = 0x00001000;
            public const uint PDH_FMT_NOCAP100 = 0x00008000;



            [StructLayout(LayoutKind.Sequential)]
            public struct PDH_RAW_COUNTER
            {
                public int CStatus;
                public long TimeStamp;
                public long FirstValue;
                public long SecondValue;
                public int MultiCount;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct PDH_FMT_COUNTERVALUE
            {
                public int CStatus;
                public double data;
            }

            public const int PDH_NO_DATA = unchecked((int)0x800007D5);
            public const int PDH_CALC_NEGATIVE_DENOMINATOR = unchecked((int)0x800007D6);
            public const int PDH_CALC_NEGATIVE_VALUE = unchecked((int)0x800007D8);

            //
            //  The following are used to determine the level of detail associated
            //  with the counter.  The user will be setting the level of detail
            //  that should be displayed at any given time.
            //
            public const int PERF_DETAIL_NOVICE = 100; // The uninformed can understand it
            public const int PERF_DETAIL_ADVANCED = 200; // For the advanced user
            public const int PERF_DETAIL_EXPERT = 300; // For the expert user
            public const int PERF_DETAIL_WIZARD = 400; // For the system designer
        }
    }
}

