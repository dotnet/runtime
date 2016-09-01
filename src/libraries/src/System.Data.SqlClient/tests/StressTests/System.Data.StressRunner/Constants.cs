// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace DPStressHarness
{
    public static class Constants
    {
        public const string XML_ELEM_RESULTS = "PerfResults";
        public const string XML_ELEM_RUN = "Run";
        public const string XML_ELEM_RUN_METRIC = "RunMetric";
        public const string XML_ELEM_TEST = "Test";
        public const string XML_ELEM_TEST_METRIC = "TestMetric";
        public const string XML_ELEM_EXCEPTION = "Exception";

        public const string XML_ATTR_RUN_LABEL = "label";
        public const string XML_ATTR_RUN_START_TIME = "startTime";
        public const string XML_ATTR_RUN_OFFICIAL = "official";
        public const string XML_ATTR_RUN_MILESTONE = "milestone";
        public const string XML_ATTR_RUN_BRANCH = "branch";
        public const string XML_ATTR_RUN_UPLOADED = "uploaded";
        public const string XML_ATTR_RUN_METRIC_NAME = "name";
        public const string XML_ATTR_TEST_NAME = "name";
        public const string XML_ATTR_TEST_METRIC_NAME = "name";
        public const string XML_ATTR_TEST_METRIC_UNITS = "units";
        public const string XML_ATTR_TEST_METRIC_ISHIGHERBETTER = "isHigherBetter";

        public const string XML_ATTR_VALUE_TRUE = "true";
        public const string XML_ATTR_VALUE_FALSE = "false";

        public const string RUN_METRIC_PROCESSOR_COUNT = "Processor Count";
        public const string RUN_DNS_HOST_NAME = "DNS Host Name";
        public const string RUN_IDENTITY_NAME = "Identity Name";
        public const string RUN_PROCESS_MACHINE_NAME = "Process Machine Name";

        public const string TEST_METRIC_TEST_ASSEMBLY = "Test Assembly";
        public const string TEST_METRIC_TEST_IMPROVEMENT = "Improvement";
        public const string TEST_METRIC_TEST_OWNER = "Owner";
        public const string TEST_METRIC_TEST_CATEGORY = "Category";
        public const string TEST_METRIC_TEST_PRIORITY = "Priority";
        public const string TEST_METRIC_APPLICATION_NAME = "Application Name";
        public const string TEST_METRIC_TARGET_ASSEMBLY_NAME = "Target Assembly Name";
        public const string TEST_METRIC_ELAPSED_SECONDS = "Elapsed Seconds";
        public const string TEST_METRIC_RPS = "Requests Per Second";
        public const string TEST_METRIC_PEAK_WORKING_SET = "Peak Working Set";
        public const string TEST_METRIC_WORKING_SET = "Working Set";
        public const string TEST_METRIC_PRIVATE_BYTES = "Private Bytes";
    }
}
