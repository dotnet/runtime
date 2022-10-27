// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Mono.Linker.Tests.TestCasesRunner
{
    public class LinkerDriver
    {
        protected class TestDriver : Driver
        {
            readonly LinkerCustomizations _customization;

            public TestDriver(Queue<string> args, LinkerCustomizations customizations) : base(args)
            {
                _customization = customizations;
            }

            protected override LinkContext GetDefaultContext(Pipeline pipeline, ILogger logger)
            {
                LinkContext context = base.GetDefaultContext(pipeline, logger);
                _customization.CustomizeLinkContext(context);
                return context;
            }
        }

        public virtual void Link(string[] args, LinkerCustomizations customizations, ILogger logger)
        {
            Driver.ProcessResponseFile(args, out var queue);
            using (var driver = new TestDriver(queue, customizations))
            {
                driver.Run(logger);
            }
        }
    }
}
