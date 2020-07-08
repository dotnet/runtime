// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

using System.Collections.Generic;

//Repro from http://www.simple-talk.com/dotnet/.net-framework/the-dangers-of-the-large-object-heap/




namespace LOH_test
{

    class Program
    {
        //percent difference between the bytes allocated with small blocks only and with larger blocks
        //This accounts for difference in fragmentation
        const int maxDiffPercent = 30;

        // Static variable used to store our 'big' block. This ensures that the block is always up for garbage collection.

        static byte[] bigBlock;


        // Allocates 90,000 byte blocks, optionally intersperced with larger blocks
        // Return how many MB can be allocated until OOM
        static int Fill(bool allocateBigBlocks)
        {

            // Number of bytes in a small block

            // 90000 bytes, just above the limit for the LOH

            const int blockSize = 90000;



            // Number of bytes in a larger block: 16Mb initially

            int largeBlockSize = 1 << 24;



            // Number of small blocks allocated

            int count = 0;



            try
            {

                // We keep the 'small' blocks around 

                // (imagine an algorithm that allocates memory in chunks)

                List<byte[]> smallBlocks = new List<byte[]>();



                for (; ; )
                {

                    // Allocate a temporary larger block if we're set up to do so

                    if (allocateBigBlocks)
                    {

                        bigBlock = new byte[largeBlockSize];

                        // The next 'large' block will be just slightly larger

                        largeBlockSize++;

                    }



                    // Allocate a small block

                    smallBlocks.Add(new byte[blockSize]);

                    count++;

                }

            }

            catch (OutOfMemoryException)
            {

                // Force a GC, which should empty the LOH again

                bigBlock = null;

                GC.Collect();

            }

            int TotalMBAllocated = (int)((double)(count * blockSize) / (double)(1024 * 1024));

            // Display the results for the amount of memory we managed to allocate

            Console.WriteLine("{0}: {1}Mb allocated"

                              , (allocateBigBlocks ? "With large blocks" : "Only small blocks")
                              , TotalMBAllocated);

            return TotalMBAllocated;

        }



        static int Main(string[] args)
        {

            // Display results for cases both with and without the larger blocks

            int w_LargerBlocks = Fill(true);

            int onlySmallBlocks = Fill(false);

            int FragmentationDiffPercent = (int) (((double)(onlySmallBlocks - w_LargerBlocks) / (double)onlySmallBlocks) * 100);
            Console.WriteLine("Fragmentation difference percent = {0}%", FragmentationDiffPercent);
            

            if (FragmentationDiffPercent > maxDiffPercent)
            {
                Console.WriteLine("Test Failed!");
                return 1;
            }
            Console.WriteLine("Test Passed");
            return 100;

        }

    }

}
