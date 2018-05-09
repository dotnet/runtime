// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Globalization
{
    internal class CalendricalCalculationsHelper
    {
        private const double FullCircleOfArc = 360.0; // 360.0;
        private const int HalfCircleOfArc = 180;
        private const double TwelveHours = 0.5; // half a day
        private const double Noon2000Jan01 = 730120.5;
        internal const double MeanTropicalYearInDays = 365.242189;
        private const double MeanSpeedOfSun = MeanTropicalYearInDays / FullCircleOfArc;
        private const double LongitudeSpring = 0.0;
        private const double TwoDegreesAfterSpring = 2.0;
        private const int SecondsPerDay = 24 * 60 * 60; // 24 hours * 60 minutes * 60 seconds

        private const int DaysInUniformLengthCentury = 36525;
        private const int SecondsPerMinute = 60;
        private const int MinutesPerDegree = 60;

        private static readonly long s_startOf1810 = GetNumberOfDays(new DateTime(1810, 1, 1));
        private static readonly long s_startOf1900Century = GetNumberOfDays(new DateTime(1900, 1, 1));

        private static readonly double[] s_coefficients1900to1987 = new double[] { -0.00002, 0.000297, 0.025184, -0.181133, 0.553040, -0.861938, 0.677066, -0.212591 };
        private static readonly double[] s_coefficients1800to1899 = new double[] { -0.000009, 0.003844, 0.083563, 0.865736, 4.867575, 15.845535, 31.332267, 38.291999, 28.316289, 11.636204, 2.043794 };
        private static readonly double[] s_coefficients1700to1799 = new double[] { 8.118780842, -0.005092142, 0.003336121, -0.0000266484 };
        private static readonly double[] s_coefficients1620to1699 = new double[] { 196.58333, -4.0675, 0.0219167 };
        private static readonly double[] s_lambdaCoefficients = new double[] { 280.46645, 36000.76983, 0.0003032 };
        private static readonly double[] s_anomalyCoefficients = new double[] { 357.52910, 35999.05030, -0.0001559, -0.00000048 };
        private static readonly double[] s_eccentricityCoefficients = new double[] { 0.016708617, -0.000042037, -0.0000001236 };
        private static readonly double[] s_coefficients = new double[] { Angle(23, 26, 21.448), Angle(0, 0, -46.8150), Angle(0, 0, -0.00059), Angle(0, 0, 0.001813) };
        private static readonly double[] s_coefficientsA = new double[] { 124.90, -1934.134, 0.002063 };
        private static readonly double[] s_coefficientsB = new double[] { 201.11, 72001.5377, 0.00057 };

        private static double RadiansFromDegrees(double degree)
        {
            return degree * Math.PI / 180;
        }

        private static double SinOfDegree(double degree)
        {
            return Math.Sin(RadiansFromDegrees(degree));
        }

        private static double CosOfDegree(double degree)
        {
            return Math.Cos(RadiansFromDegrees(degree));
        }
        private static double TanOfDegree(double degree)
        {
            return Math.Tan(RadiansFromDegrees(degree));
        }

        public static double Angle(int degrees, int minutes, double seconds)
        {
            return ((seconds / SecondsPerMinute + minutes) / MinutesPerDegree) + degrees;
        }

        private static double Obliquity(double julianCenturies)
        {
            return PolynomialSum(s_coefficients, julianCenturies);
        }

        internal static long GetNumberOfDays(DateTime date)
        {
            return date.Ticks / GregorianCalendar.TicksPerDay;
        }

        private static int GetGregorianYear(double numberOfDays)
        {
            return new DateTime(Math.Min((long)(Math.Floor(numberOfDays) * GregorianCalendar.TicksPerDay), DateTime.MaxValue.Ticks)).Year;
        }

        private enum CorrectionAlgorithm
        {
            Default,
            Year1988to2019,
            Year1900to1987,
            Year1800to1899,
            Year1700to1799,
            Year1620to1699
        }

        private struct EphemerisCorrectionAlgorithmMap
        {
            public EphemerisCorrectionAlgorithmMap(int year, CorrectionAlgorithm algorithm)
            {
                _lowestYear = year;
                _algorithm = algorithm;
            }

            internal int _lowestYear;
            internal CorrectionAlgorithm _algorithm;
        };

        private static readonly EphemerisCorrectionAlgorithmMap[] s_ephemerisCorrectionTable = new EphemerisCorrectionAlgorithmMap[]
        {
            // lowest year that starts algorithm, algorithm to use
            new EphemerisCorrectionAlgorithmMap(2020, CorrectionAlgorithm.Default),
            new EphemerisCorrectionAlgorithmMap(1988, CorrectionAlgorithm.Year1988to2019),
            new EphemerisCorrectionAlgorithmMap(1900, CorrectionAlgorithm.Year1900to1987),
            new EphemerisCorrectionAlgorithmMap(1800, CorrectionAlgorithm.Year1800to1899),
            new EphemerisCorrectionAlgorithmMap(1700, CorrectionAlgorithm.Year1700to1799),
            new EphemerisCorrectionAlgorithmMap(1620, CorrectionAlgorithm.Year1620to1699),
            new EphemerisCorrectionAlgorithmMap(int.MinValue, CorrectionAlgorithm.Default) // default must be last
        };

        private static double Reminder(double divisor, double dividend)
        {
            double whole = Math.Floor(divisor / dividend);
            return divisor - (dividend * whole);
        }

        private static double NormalizeLongitude(double longitude)
        {
            longitude = Reminder(longitude, FullCircleOfArc);
            if (longitude < 0)
            {
                longitude += FullCircleOfArc;
            }
            return longitude;
        }

        public static double AsDayFraction(double longitude)
        {
            return longitude / FullCircleOfArc;
        }

        private static double PolynomialSum(double[] coefficients, double indeterminate)
        {
            double sum = coefficients[0];
            double indeterminateRaised = 1;
            for (int i = 1; i < coefficients.Length; i++)
            {
                indeterminateRaised *= indeterminate;
                sum += (coefficients[i] * indeterminateRaised);
            }

            return sum;
        }

        private static double CenturiesFrom1900(int gregorianYear)
        {
            long july1stOfYear = GetNumberOfDays(new DateTime(gregorianYear, 7, 1));
            return (double)(july1stOfYear - s_startOf1900Century) / DaysInUniformLengthCentury;
        }

        // the following formulas defines a polynomial function which gives us the amount that the earth is slowing down for specific year ranges
        private static double DefaultEphemerisCorrection(int gregorianYear)
        {
            Debug.Assert(gregorianYear < 1620 || 2020 <= gregorianYear);
            long january1stOfYear = GetNumberOfDays(new DateTime(gregorianYear, 1, 1));
            double daysSinceStartOf1810 = january1stOfYear - s_startOf1810;
            double x = TwelveHours + daysSinceStartOf1810;
            return ((Math.Pow(x, 2) / 41048480) - 15) / SecondsPerDay;
        }

        private static double EphemerisCorrection1988to2019(int gregorianYear)
        {
            Debug.Assert(1988 <= gregorianYear && gregorianYear <= 2019);
            return (double)(gregorianYear - 1933) / SecondsPerDay;
        }

        private static double EphemerisCorrection1900to1987(int gregorianYear)
        {
            Debug.Assert(1900 <= gregorianYear && gregorianYear <= 1987);
            double centuriesFrom1900 = CenturiesFrom1900(gregorianYear);
            return PolynomialSum(s_coefficients1900to1987, centuriesFrom1900);
        }

        private static double EphemerisCorrection1800to1899(int gregorianYear)
        {
            Debug.Assert(1800 <= gregorianYear && gregorianYear <= 1899);
            double centuriesFrom1900 = CenturiesFrom1900(gregorianYear);
            return PolynomialSum(s_coefficients1800to1899, centuriesFrom1900);
        }

        private static double EphemerisCorrection1700to1799(int gregorianYear)
        {
            Debug.Assert(1700 <= gregorianYear && gregorianYear <= 1799);
            double yearsSince1700 = gregorianYear - 1700;
            return PolynomialSum(s_coefficients1700to1799, yearsSince1700) / SecondsPerDay;
        }

        private static double EphemerisCorrection1620to1699(int gregorianYear)
        {
            Debug.Assert(1620 <= gregorianYear && gregorianYear <= 1699);
            double yearsSince1600 = gregorianYear - 1600;
            return PolynomialSum(s_coefficients1620to1699, yearsSince1600) / SecondsPerDay;
        }

        // ephemeris-correction: correction to account for the slowing down of the rotation of the earth
        private static double EphemerisCorrection(double time)
        {
            int year = GetGregorianYear(time);
            foreach (EphemerisCorrectionAlgorithmMap map in s_ephemerisCorrectionTable)
            {
                if (map._lowestYear <= year)
                {
                    switch (map._algorithm)
                    {
                        case CorrectionAlgorithm.Default: return DefaultEphemerisCorrection(year);
                        case CorrectionAlgorithm.Year1988to2019: return EphemerisCorrection1988to2019(year);
                        case CorrectionAlgorithm.Year1900to1987: return EphemerisCorrection1900to1987(year);
                        case CorrectionAlgorithm.Year1800to1899: return EphemerisCorrection1800to1899(year);
                        case CorrectionAlgorithm.Year1700to1799: return EphemerisCorrection1700to1799(year);
                        case CorrectionAlgorithm.Year1620to1699: return EphemerisCorrection1620to1699(year);
                    }

                    break; // break the loop and assert eventually 
                }
            }

            Debug.Fail("Not expected to come here");
            return DefaultEphemerisCorrection(year);
        }

        public static double JulianCenturies(double moment)
        {
            double dynamicalMoment = moment + EphemerisCorrection(moment);
            return (dynamicalMoment - Noon2000Jan01) / DaysInUniformLengthCentury;
        }

        private static bool IsNegative(double value)
        {
            return Math.Sign(value) == -1;
        }

        private static double CopySign(double value, double sign)
        {
            return (IsNegative(value) == IsNegative(sign)) ? value : -value;
        }

        // equation-of-time; approximate the difference between apparent solar time and mean solar time
        // formal definition is EOT = GHA - GMHA
        // GHA is the Greenwich Hour Angle of the apparent (actual) Sun
        // GMHA is the Greenwich Mean Hour Angle of the mean (fictitious) Sun
        // http://www.esrl.noaa.gov/gmd/grad/solcalc/
        // http://en.wikipedia.org/wiki/Equation_of_time
        private static double EquationOfTime(double time)
        {
            double julianCenturies = JulianCenturies(time);
            double lambda = PolynomialSum(s_lambdaCoefficients, julianCenturies);
            double anomaly = PolynomialSum(s_anomalyCoefficients, julianCenturies);
            double eccentricity = PolynomialSum(s_eccentricityCoefficients, julianCenturies);

            double epsilon = Obliquity(julianCenturies);
            double tanHalfEpsilon = TanOfDegree(epsilon / 2);
            double y = tanHalfEpsilon * tanHalfEpsilon;

            double dividend = ((y * SinOfDegree(2 * lambda))
                - (2 * eccentricity * SinOfDegree(anomaly))
                + (4 * eccentricity * y * SinOfDegree(anomaly) * CosOfDegree(2 * lambda))
                - (0.5 * Math.Pow(y, 2) * SinOfDegree(4 * lambda))
                - (1.25 * Math.Pow(eccentricity, 2) * SinOfDegree(2 * anomaly)));
            double divisor = 2 * Math.PI;
            double equation = dividend / divisor;

            // approximation of equation of time is not valid for dates that are many millennia in the past or future
            // thus limited to a half day
            return CopySign(Math.Min(Math.Abs(equation), TwelveHours), equation);
        }

        private static double AsLocalTime(double apparentMidday, double longitude)
        {
            // slightly inaccurate since equation of time takes mean time not apparent time as its argument, but the difference is negligible
            double universalTime = apparentMidday - AsDayFraction(longitude);
            return apparentMidday - EquationOfTime(universalTime);
        }

        // midday
        public static double Midday(double date, double longitude)
        {
            return AsLocalTime(date + TwelveHours, longitude) - AsDayFraction(longitude);
        }

        private static double InitLongitude(double longitude)
        {
            return NormalizeLongitude(longitude + HalfCircleOfArc) - HalfCircleOfArc;
        }

        // midday-in-tehran
        public static double MiddayAtPersianObservationSite(double date)
        {
            return Midday(date, InitLongitude(52.5)); // 52.5 degrees east - longitude of UTC+3:30 which defines Iranian Standard Time
        }

        private static double PeriodicTerm(double julianCenturies, int x, double y, double z)
        {
            return x * SinOfDegree(y + z * julianCenturies);
        }

        private static double SumLongSequenceOfPeriodicTerms(double julianCenturies)
        {
            double sum = 0.0;
            sum += PeriodicTerm(julianCenturies, 403406, 270.54861, 0.9287892);
            sum += PeriodicTerm(julianCenturies, 195207, 340.19128, 35999.1376958);
            sum += PeriodicTerm(julianCenturies, 119433, 63.91854, 35999.4089666);
            sum += PeriodicTerm(julianCenturies, 112392, 331.2622, 35998.7287385);
            sum += PeriodicTerm(julianCenturies, 3891, 317.843, 71998.20261);
            sum += PeriodicTerm(julianCenturies, 2819, 86.631, 71998.4403);
            sum += PeriodicTerm(julianCenturies, 1721, 240.052, 36000.35726);
            sum += PeriodicTerm(julianCenturies, 660, 310.26, 71997.4812);
            sum += PeriodicTerm(julianCenturies, 350, 247.23, 32964.4678);
            sum += PeriodicTerm(julianCenturies, 334, 260.87, -19.441);
            sum += PeriodicTerm(julianCenturies, 314, 297.82, 445267.1117);
            sum += PeriodicTerm(julianCenturies, 268, 343.14, 45036.884);
            sum += PeriodicTerm(julianCenturies, 242, 166.79, 3.1008);
            sum += PeriodicTerm(julianCenturies, 234, 81.53, 22518.4434);
            sum += PeriodicTerm(julianCenturies, 158, 3.5, -19.9739);
            sum += PeriodicTerm(julianCenturies, 132, 132.75, 65928.9345);
            sum += PeriodicTerm(julianCenturies, 129, 182.95, 9038.0293);
            sum += PeriodicTerm(julianCenturies, 114, 162.03, 3034.7684);
            sum += PeriodicTerm(julianCenturies, 99, 29.8, 33718.148);
            sum += PeriodicTerm(julianCenturies, 93, 266.4, 3034.448);
            sum += PeriodicTerm(julianCenturies, 86, 249.2, -2280.773);
            sum += PeriodicTerm(julianCenturies, 78, 157.6, 29929.992);
            sum += PeriodicTerm(julianCenturies, 72, 257.8, 31556.493);
            sum += PeriodicTerm(julianCenturies, 68, 185.1, 149.588);
            sum += PeriodicTerm(julianCenturies, 64, 69.9, 9037.75);
            sum += PeriodicTerm(julianCenturies, 46, 8.0, 107997.405);
            sum += PeriodicTerm(julianCenturies, 38, 197.1, -4444.176);
            sum += PeriodicTerm(julianCenturies, 37, 250.4, 151.771);
            sum += PeriodicTerm(julianCenturies, 32, 65.3, 67555.316);
            sum += PeriodicTerm(julianCenturies, 29, 162.7, 31556.08);
            sum += PeriodicTerm(julianCenturies, 28, 341.5, -4561.54);
            sum += PeriodicTerm(julianCenturies, 27, 291.6, 107996.706);
            sum += PeriodicTerm(julianCenturies, 27, 98.5, 1221.655);
            sum += PeriodicTerm(julianCenturies, 25, 146.7, 62894.167);
            sum += PeriodicTerm(julianCenturies, 24, 110.0, 31437.369);
            sum += PeriodicTerm(julianCenturies, 21, 5.2, 14578.298);
            sum += PeriodicTerm(julianCenturies, 21, 342.6, -31931.757);
            sum += PeriodicTerm(julianCenturies, 20, 230.9, 34777.243);
            sum += PeriodicTerm(julianCenturies, 18, 256.1, 1221.999);
            sum += PeriodicTerm(julianCenturies, 17, 45.3, 62894.511);
            sum += PeriodicTerm(julianCenturies, 14, 242.9, -4442.039);
            sum += PeriodicTerm(julianCenturies, 13, 115.2, 107997.909);
            sum += PeriodicTerm(julianCenturies, 13, 151.8, 119.066);
            sum += PeriodicTerm(julianCenturies, 13, 285.3, 16859.071);
            sum += PeriodicTerm(julianCenturies, 12, 53.3, -4.578);
            sum += PeriodicTerm(julianCenturies, 10, 126.6, 26895.292);
            sum += PeriodicTerm(julianCenturies, 10, 205.7, -39.127);
            sum += PeriodicTerm(julianCenturies, 10, 85.9, 12297.536);
            sum += PeriodicTerm(julianCenturies, 10, 146.1, 90073.778);
            return sum;
        }

        private static double Aberration(double julianCenturies)
        {
            return (0.0000974 * CosOfDegree(177.63 + (35999.01848 * julianCenturies))) - 0.005575;
        }

        private static double Nutation(double julianCenturies)
        {
            double a = PolynomialSum(s_coefficientsA, julianCenturies);
            double b = PolynomialSum(s_coefficientsB, julianCenturies);
            return (-0.004778 * SinOfDegree(a)) - (0.0003667 * SinOfDegree(b));
        }

        public static double Compute(double time)
        {
            double julianCenturies = JulianCenturies(time);
            double lambda = 282.7771834
                + (36000.76953744 * julianCenturies)
                + (0.000005729577951308232 * SumLongSequenceOfPeriodicTerms(julianCenturies));

            double longitude = lambda + Aberration(julianCenturies) + Nutation(julianCenturies);
            return InitLongitude(longitude);
        }

        public static double AsSeason(double longitude)
        {
            return (longitude < 0) ? (longitude + FullCircleOfArc) : longitude;
        }

        private static double EstimatePrior(double longitude, double time)
        {
            double timeSunLastAtLongitude = time - (MeanSpeedOfSun * AsSeason(InitLongitude(Compute(time) - longitude)));
            double longitudeErrorDelta = InitLongitude(Compute(timeSunLastAtLongitude) - longitude);
            return Math.Min(time, timeSunLastAtLongitude - (MeanSpeedOfSun * longitudeErrorDelta));
        }

        // persian-new-year-on-or-before
        //  number of days is the absolute date. The absolute date is the number of days from January 1st, 1 A.D.
        //  1/1/0001 is absolute date 1.
        internal static long PersianNewYearOnOrBefore(long numberOfDays)
        {
            double date = (double)numberOfDays;

            double approx = EstimatePrior(LongitudeSpring, MiddayAtPersianObservationSite(date));
            long lowerBoundNewYearDay = (long)Math.Floor(approx) - 1;
            long upperBoundNewYearDay = lowerBoundNewYearDay + 3; // estimate is generally within a day of the actual occurrance (at the limits, the error expands, since the calculations rely on the mean tropical year which changes...)
            long day = lowerBoundNewYearDay;
            for (; day != upperBoundNewYearDay; ++day)
            {
                double midday = MiddayAtPersianObservationSite((double)day);
                double l = Compute(midday);
                if ((LongitudeSpring <= l) && (l <= TwoDegreesAfterSpring))
                {
                    break;
                }
            }
            Debug.Assert(day != upperBoundNewYearDay);

            return day - 1;
        }
    }
}
