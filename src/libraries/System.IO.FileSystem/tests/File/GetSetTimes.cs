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

public class File_GetSetTimes
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
        String fileName = Path.Combine(TestInfo.CurrentDirectory, "File_GetSetTimes");

        File.Create(fileName).Dispose();
        
        foreach(TimeProperty timeProperty in Enum.GetValues(typeof(TimeProperty)))
        {
            if (!Interop.IsWindows && timeProperty == TimeProperty.CreationTime) // roundtripping birthtime not supported on Unix
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
                                File.SetCreationTimeUtc(fileName, dt);
                                break;
                            case TimeProperty.LastAccessTime:
                                File.SetLastAccessTimeUtc(fileName, dt);
                                break;
                            case TimeProperty.LastWriteTime:
                                File.SetLastWriteTimeUtc(fileName, dt);
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
                                File.SetCreationTime(fileName, dt);
                                break;
                            case TimeProperty.LastAccessTime:
                                File.SetLastAccessTime(fileName, dt);
                                break;
                            case TimeProperty.LastWriteTime:
                                File.SetLastWriteTime(fileName, dt);
                                break;
                            default:
                                break;
                        }
                    }

                    DateTime actual, actualUtc;
                    switch (timeProperty)
                    {
                        case TimeProperty.CreationTime:
                            actual = File.GetCreationTime(fileName);
                            actualUtc = File.GetCreationTimeUtc(fileName);
                            break;
                        case TimeProperty.LastAccessTime:
                            actual = File.GetLastAccessTime(fileName);
                            actualUtc = File.GetLastAccessTimeUtc(fileName);
                            break;
                        case TimeProperty.LastWriteTime:
                            actual = File.GetLastWriteTime(fileName);
                            actualUtc = File.GetLastWriteTimeUtc(fileName);
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

        File.Delete(fileName);
    }
}