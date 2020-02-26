// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.CommandLineUtils
{
    internal class CommandArgument
    {
        public CommandArgument()
        {
            Values = new List<string>();
        }

        public string Name { get; set; }
        public bool ShowInHelpText { get; set; } = true;
        public string Description { get; set; }
        public List<string> Values { get; private set; }
        public bool MultipleValues { get; set; }
        public string Value
        {
            get
            {
                return Values.FirstOrDefault();
            }
        }
    }
}
