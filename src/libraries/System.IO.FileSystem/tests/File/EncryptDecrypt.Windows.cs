// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Security;
using System.ServiceProcess;
using Xunit;
using Xunit.Abstractions;

namespace System.IO.Tests
{
    public partial class EncryptDecrypt
    {
        partial void EnsureEFSServiceStarted()
        {
            try
            {
                using var sc = new ServiceController("EFS");
                _output.WriteLine($"EFS service is: {sc.Status}");
                if (sc.Status != ServiceControllerStatus.Running)
                {
                    _output.WriteLine("Trying to start EFS service");
                    sc.Start();
                    _output.WriteLine($"EFS service is now: {sc.Status}");
                }
            }
            catch (Exception e)
            {
                _output.WriteLine(e.ToString());
            }
        }

        partial void LogEFSDiagnostics()
        {
            int hours = 1; // how many hours to look backwards
            string query = @$"
                        <QueryList>
                          <Query Id='0' Path='System'>
                            <Select Path='System'>
                                *[System[Provider/@Name='Server']]
                            </Select>
                            <Select Path='System'>
                                *[System[Provider/@Name='Service Control Manager']]
                            </Select>
                            <Select Path='System'>
                                *[System[Provider/@Name='Microsoft-Windows-EFS']]
                            </Select>
                            <Suppress Path='System'>
                                *[System[TimeCreated[timediff(@SystemTime) &gt;= {hours * 60 * 60 * 1000L}]]]
                            </Suppress>
                          </Query>
                        </QueryList> ";

            var eventQuery = new EventLogQuery("System", PathType.LogName, query);

            using var eventReader = new EventLogReader(eventQuery);

            EventRecord record = eventReader.ReadEvent();
            var garbage = new string[] { "Background Intelligent", "Intel", "Defender", "Intune", "BITS", "NetBT"};

            _output.WriteLine("=====  Dumping recent relevant events: =====");
            while (record != null)
            {
                string description = "";
                try
                {
                    description = record.FormatDescription();
                }
                catch (EventLogException) { }

                if (!garbage.Any(term => description.Contains(term, StringComparison.OrdinalIgnoreCase)))
                {
                    _output.WriteLine($"{record.TimeCreated} {record.ProviderName} [{record.LevelDisplayName} {record.Id}] {description.Replace("\r\n", "  ")}");
                }

                record = eventReader.ReadEvent();
            }

            _output.WriteLine("==== Finished dumping =====");
        }
    }
}
