// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection.PortableExecutable;

namespace ILLink.Tasks
{
    public static class Utils
    {
        public static bool IsManagedAssembly(string fileName)
        {
            try
            {
                using (Stream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    PEHeaders headers = new PEHeaders(fileStream);
                    return headers.CorHeader != null;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
