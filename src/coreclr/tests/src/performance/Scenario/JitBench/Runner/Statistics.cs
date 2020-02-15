using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JitBench
{
    public static class Statistics
    {
        public static double SampleStandardDeviation(this IEnumerable<double> data)
        {
            int n = data.Count();
            double sampleMean = data.Average();
            return Math.Sqrt(data.Select(x => (x - sampleMean) * (x - sampleMean)).Sum() / (n - 1));
        }

        public static double StandardError(this IEnumerable<double> data)
        {
            int n = data.Count();
            return SampleStandardDeviation(data) / Math.Sqrt(n);
        }

        public static double MarginOfError95(this IEnumerable<double> data)
        {
            return StandardError(data) * 1.96;
        }

        public static double Median(this IEnumerable<double> data)
        {
            double[] dataArr = data.ToArray();
            Array.Sort(dataArr);
            if(dataArr.Length % 2 == 1)
            {
                return dataArr[dataArr.Length / 2];
            }
            else
            {
                int midpoint = dataArr.Length / 2;
                return (dataArr[midpoint-1] + dataArr[midpoint]) / 2;
            }
        }

        public static double Quartile1(this IEnumerable<double> data)
        {
            double[] dataArr = data.ToArray();
            Array.Sort(dataArr);
            if (dataArr.Length % 2 == 1)
            {
                return Median(dataArr.Take(dataArr.Length / 2 + 1));
            }
            else
            {
                return Median(dataArr.Take(dataArr.Length / 2));
            }
        }

        public static double Quartile3(this IEnumerable<double> data)
        {
            double[] dataArr = data.ToArray();
            Array.Sort(dataArr);
            if (dataArr.Length % 2 == 1)
            {
                return Median(dataArr.Skip(dataArr.Length / 2 ));
            }
            else
            {
                return Median(dataArr.Skip(dataArr.Length / 2));
            }
        }
    }
}
