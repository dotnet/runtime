// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Testing
{
    // This is a workaround for https://github.com/xunit/xunit/issues/1782 - as such, this code is a copy-paste
    // from xUnit with the exception of fixing the bug.
    //
    // This will only work with [ConditionalTheory].
    internal class WORKAROUND_SkippedDataRowTestCase : XunitTestCase
    {
        string skipReason;

        /// <summary/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public WORKAROUND_SkippedDataRowTestCase() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="XunitSkippedDataRowTestCase"/> class.
        /// </summary>
        /// <param name="diagnosticMessageSink">The message sink used to send diagnostic messages</param>
        /// <param name="defaultMethodDisplay">Default method display to use (when not customized).</param>
        /// <param name="testMethod">The test method this test case belongs to.</param>
        /// <param name="skipReason">The reason that this test case will be skipped</param>
        /// <param name="testMethodArguments">The arguments for the test method.</param>
        [Obsolete("Please call the constructor which takes TestMethodDisplayOptions")]
        public WORKAROUND_SkippedDataRowTestCase(IMessageSink diagnosticMessageSink,
                                           TestMethodDisplay defaultMethodDisplay,
                                           ITestMethod testMethod,
                                           string skipReason,
                                           object[] testMethodArguments = null)
            : this(diagnosticMessageSink, defaultMethodDisplay, TestMethodDisplayOptions.None, testMethod, skipReason, testMethodArguments) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="XunitSkippedDataRowTestCase"/> class.
        /// </summary>
        /// <param name="diagnosticMessageSink">The message sink used to send diagnostic messages</param>
        /// <param name="defaultMethodDisplay">Default method display to use (when not customized).</param>
        /// <param name="defaultMethodDisplayOptions">Default method display options to use (when not customized).</param>
        /// <param name="testMethod">The test method this test case belongs to.</param>
        /// <param name="skipReason">The reason that this test case will be skipped</param>
        /// <param name="testMethodArguments">The arguments for the test method.</param>
        public WORKAROUND_SkippedDataRowTestCase(IMessageSink diagnosticMessageSink,
                                           TestMethodDisplay defaultMethodDisplay,
                                           TestMethodDisplayOptions defaultMethodDisplayOptions,
                                           ITestMethod testMethod,
                                           string skipReason,
                                           object[] testMethodArguments = null)
            : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod, testMethodArguments)
        {
            this.skipReason = skipReason;
        }

        /// <inheritdoc/>
        public override void Deserialize(IXunitSerializationInfo data)
        {
            // SkipReason has to be read before we call base.Deserialize, this is the workaround.
            this.skipReason = data.GetValue<string>("SkipReason");

            base.Deserialize(data);
        }

        /// <inheritdoc/>
        protected override string GetSkipReason(IAttributeInfo factAttribute)
        {
            return skipReason;
        }

        /// <inheritdoc/>
        public override void Serialize(IXunitSerializationInfo data)
        {
            base.Serialize(data);

            data.AddValue("SkipReason", skipReason);
        }
    }
}
