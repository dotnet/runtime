// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Runtime.CompilerServices;

namespace ConsoleApplication1
{
    class Program
    {
        /// <summary>
        /// AV when switch optimized away in x64. Should be somewhat rare but we optimize because all the switch cases result in the same assignment
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static int Main(string[] args)
        {
            try
            {
                ProductPatchLevel.GetPatchLevel(Product.Client);
                Console.WriteLine("Pass");
                return 100;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine("Fail");
                return -1;
            }
        }
    }

    public enum Product
    {
        Client,
        SDK,
        SAG,
    }

    public static class ProductPatchLevel
    {
        private const int ClientLevel = 0;
        private const int SDKLevel = 0;
        private const int SAGLevel = 0;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int GetPatchLevel(Product p)
        {
            int patchLevel = 0;
            switch (p)
            {
                case Product.Client:
                    {
                        patchLevel = ClientLevel;
                        break;
                    }

                case Product.SDK:
                    {
                        patchLevel = SDKLevel;
                        break;
                    }

                case Product.SAG:
                    {
                        patchLevel = SAGLevel;
                        break;
                    }

            }

            return patchLevel;
        }
    }

}
