// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing.Internal;

namespace Microsoft.Extensions.FileSystemGlobbing.Tests.PatternContexts
{
    internal class MockRaggedPatternBuilder
    {
        private MockRaggedPattern _result;

        public static MockRaggedPatternBuilder New()
        {
            return new MockRaggedPatternBuilder();
        }

        private MockRaggedPatternBuilder()
        {
            _result = new MockRaggedPattern();
        }

        public MockRaggedPatternBuilder AddStart(params string[] values)
        {
            foreach (var value in values)
            {
                var segment = new MockNonRecursivePathSegment(value);
                _result.StartsWith.Add(segment);
                _result.Segments.Add(segment);
            }

            return this;
        }

        public MockRaggedPatternBuilder AddContainsGroup(params string[] values)
        {
            var last = _result.Segments.Last();

            if (!(last is MockRecursivePathSegment))
            {
                AddRecursive();
            }

            var containSegment = new List<IPathSegment>();
            foreach (var value in values)
            {
                var segment = new MockNonRecursivePathSegment(value);
                containSegment.Add(segment);
                _result.Segments.Add(segment);
            }

            _result.Contains.Add(containSegment);

            AddRecursive();

            return this;
        }

        public MockRaggedPatternBuilder AddEnd(params string[] values)
        {
            foreach (var value in values)
            {
                _result.EndsWith.Add(new MockNonRecursivePathSegment(value));
            }

            return this;
        }

        public MockRaggedPatternBuilder AddRecursive()
        {
            _result.Segments.Add(new MockRecursivePathSegment());

            return this;
        }

        public IRaggedPattern Build()
        {
            return _result;
        }

        class MockRaggedPattern : IRaggedPattern
        {
            public IList<IPathSegment> Segments { get; } = new List<IPathSegment>();

            public IList<IPathSegment> StartsWith { get; } = new List<IPathSegment>();

            public IList<IList<IPathSegment>> Contains { get; } = new List<IList<IPathSegment>>();

            public IList<IPathSegment> EndsWith { get; } = new List<IPathSegment>();

            public IPatternContext CreatePatternContextForExclude()
            {
                throw new NotImplementedException();
            }

            public IPatternContext CreatePatternContextForInclude()
            {
                throw new NotImplementedException();
            }
        }
    }
}
