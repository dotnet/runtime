// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Mono.Cecil;

namespace Mono.Linker.Tests.TestCasesRunner
{
    public class TrimmingTestLogger : ILogger
    {
        /// <summary>
        /// The names of a warning's origin member, captured while the member is still attached to
        /// its declaring type.
        /// </summary>
        public readonly record struct OriginNames(string FullName, string DeclaringTypeFullName, string DeclaringTypeName, string Name);

        readonly List<MessageContainer> MessageContainers;

        readonly Dictionary<object, OriginNames> OriginNamesByProvider;

        public TrimmingTestLogger()
        {
            MessageContainers = new List<MessageContainer>();
            OriginNamesByProvider = new Dictionary<object, OriginNames>(ReferenceEqualityComparer.Instance);
        }

        public ImmutableArray<MessageContainer> GetLoggedMessages()
        {
            return MessageContainers.ToImmutableArray();
        }

        /// <summary>
        /// Returns the names the origin member of <paramref name="message"/> had when the message was
        /// logged, or null if the message has no member origin.
        /// </summary>
        public OriginNames? GetOriginNames(MessageContainer message)
        {
            if (message.Origin?.Provider is not object provider)
                return null;

            return OriginNamesByProvider.TryGetValue(provider, out var names) ? names : null;
        }

        public void LogMessage(MessageContainer message)
        {
            // This is to force Cecil to load all the information from the assembly
            // When the message is logged, the assembly is still opened by ILLink and available
            // later on during validation, it may already be closed and Cecil's lazy loading might fail.
            message.ToString();

            // Warnings can be reported on members which the trimmer goes on to remove. Removing a
            // member detaches it from its declaring type, so afterwards its FullName no longer
            // includes the namespace or the declaring type and can't be matched against the names
            // the test's expectations refer to. Record the names while they are still accurate.
            if (message.Origin?.Provider is IMemberDefinition member && !OriginNamesByProvider.ContainsKey(member))
            {
                OriginNamesByProvider.Add(member, new OriginNames(
                    member.FullName,
                    member.DeclaringType?.FullName,
                    member.DeclaringType?.Name,
                    member.Name));
            }

            MessageContainers.Add(message);
        }
    }
}
