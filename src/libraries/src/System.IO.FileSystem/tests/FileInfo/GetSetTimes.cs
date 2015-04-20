// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Threading;
using Xunit;

public class FileInfo_GetSetTimes
{
    enum TimeProperty
    {
        CreationTime,
        LastAccessTime,
        LastWriteTime
    }
    
    [Fact]
    public static void ConsistencyTest()
    {
        String fileName = Path.Combine(TestInfo.CurrentDirectory, "FileInfo_GetSetTimes");

        FileInfo file = new FileInfo(fileName);
        file.Create().Dispose();
        
        foreach(TimeProperty timeProperty in Enum.GetValues(typeof(TimeProperty)))
        {
            if (!Interop.IsWindows && timeProperty == TimeProperty.CreationTime) // birthtime not supported on Linux
            {
                continue;
            }

            foreach (DateTimeKind kind in  Enum.GetValues(typeof(DateTimeKind)))
            {
                DateTime dt = new DateTime(2014, 12, 1, 12, 0, 0, kind);
                foreach (bool setUtc in new [] { false, true } )
                {
                    if (setUtc)
                    {
                        switch (timeProperty)
                        {
                            case TimeProperty.CreationTime:
                                file.CreationTimeUtc = dt;
                                break;
                            case TimeProperty.LastAccessTime:
                                file.LastAccessTimeUtc = dt;
                                break;
                            case TimeProperty.LastWriteTime:
                                file.LastWriteTimeUtc = dt;
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        switch (timeProperty)
                        {
                            case TimeProperty.CreationTime:
                                file.CreationTime = dt;
                                break;
                            case TimeProperty.LastAccessTime:
                                file.LastAccessTime = dt;
                                break;
                            case TimeProperty.LastWriteTime:
                                file.LastWriteTime = dt;
                                break;
                            default:
                                break;
                        }
                    }

                    DateTime actual, actualUtc;
                    switch (timeProperty)
                    {
                        case TimeProperty.CreationTime:
                            actual = file.CreationTime;
                            actualUtc = file.CreationTimeUtc;
                            break;
                        case TimeProperty.LastAccessTime:
                            actual = file.LastAccessTime;
                            actualUtc = file.LastAccessTimeUtc;
                            break;
                        case TimeProperty.LastWriteTime:
                            actual = file.LastWriteTime;
                            actualUtc = file.LastWriteTimeUtc;
                            break;
                        default:
                            throw new ArgumentException("Invalid time property type");
                    }

                    DateTime expected = dt.ToLocalTime();
                    DateTime expectedUtc = dt.ToUniversalTime();

                    if (dt.Kind == DateTimeKind.Unspecified)
                    {
                        if (setUtc)
                        {
                            expectedUtc = dt;
                        }
                        else
                        {
                            expected = dt;
                        }
                    }

                    Assert.Equal(expected, actual); //, "Local {0} should be correct for DateTimeKind.{1} when set with Set{0}{2}", timeProperty, kind, setUtc ? "Utc" : "");
                    Assert.Equal(expectedUtc, actualUtc); //, "Universal {0} should be correct for DateTimeKind.{1} when set with Set{0}{2}", timeProperty, kind, setUtc ? "Utc" : "");
                }
            }
        }

        file.Delete();
    }
}