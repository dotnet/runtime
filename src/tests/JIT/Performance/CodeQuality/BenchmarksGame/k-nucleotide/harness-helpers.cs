// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Helper functionality to locate inputs and find outputs for
// k-nucleotide benchmark in CoreCLR test harness

using System;
using System.IO;
using System.Reflection;

namespace BenchmarksGame
{
    class TestHarnessHelpers
    {
        public int[] expectedCountLetter;
        public int[] expectedCountPairs;
        public int[] expectedCountFragments;
        public int[][] expectedFrequencies;
        private readonly string resourceName;

        public TestHarnessHelpers(bool bigInput, [System.Runtime.CompilerServices.CallerFilePath] string csFileName = "")
        {
            if (bigInput)
            {
                expectedCountLetter = new int[] { 302923, 301375, 198136, 197566 };
                expectedCountPairs = new int[] { 91779, 91253, 91225, 90837, 60096, 60030, 59889, 59795, 59756, 59713, 59572, 59557, 39203, 39190, 39081, 39023 };
                expectedCountFragments = new int[] { 11765, 3572, 380, 7, 7 };
                resourceName = $"{Path.GetFileNameWithoutExtension(csFileName)}.knucleotide-input-big.txt";
            }
            else
            {
                expectedCountLetter = new int[] { 1576, 1480, 974, 970 };
                expectedCountPairs = new int[] { 496, 480, 470, 420, 316, 315, 310, 302, 298, 292, 273, 272, 202, 201, 185, 167 };
                expectedCountFragments = new int[] { 54, 24, 4, 0, 0 };
                resourceName = $"{Path.GetFileNameWithoutExtension(csFileName)}.knucleotide-input.txt";
            }
            expectedFrequencies = new int[][] { expectedCountLetter, expectedCountPairs };
        }

        public Stream GetInputStream()
        {
            var assembly = typeof(TestHarnessHelpers).GetTypeInfo().Assembly;
            return assembly.GetManifestResourceStream(resourceName);
        }
    }
}
