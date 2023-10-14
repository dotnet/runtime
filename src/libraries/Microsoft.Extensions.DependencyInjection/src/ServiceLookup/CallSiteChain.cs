// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal sealed class CallSiteChain
    {
        private readonly Dictionary<ServiceIdentifier, ChainItemInfo> _callSiteChain;

        public CallSiteChain()
        {
            _callSiteChain = new Dictionary<ServiceIdentifier, ChainItemInfo>();
        }

        public void CheckCircularDependency(ServiceIdentifier serviceIdentifier)
        {
            if (_callSiteChain.ContainsKey(serviceIdentifier))
            {
                throw new InvalidOperationException(CreateCircularDependencyExceptionMessage(serviceIdentifier));
            }
        }

        public void Remove(ServiceIdentifier serviceIdentifier)
        {
            _callSiteChain.Remove(serviceIdentifier);
        }

        public void Add(ServiceIdentifier serviceIdentifier, Type? implementationType = null)
        {
            _callSiteChain[serviceIdentifier] = new ChainItemInfo(_callSiteChain.Count, implementationType);
        }

        private string CreateCircularDependencyExceptionMessage(ServiceIdentifier serviceIdentifier)
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.Append(SR.Format(SR.CircularDependencyException, TypeNameHelper.GetTypeDisplayName(serviceIdentifier.ServiceType)));
            messageBuilder.AppendLine();

            AppendResolutionPath(messageBuilder, serviceIdentifier);

            return messageBuilder.ToString();
        }

        private void AppendResolutionPath(StringBuilder builder, ServiceIdentifier currentlyResolving)
        {
            var ordered = new List<KeyValuePair<ServiceIdentifier, ChainItemInfo>>(_callSiteChain);
            ordered.Sort((a, b) => a.Value.Order.CompareTo(b.Value.Order));

            foreach (KeyValuePair<ServiceIdentifier, ChainItemInfo> pair in ordered)
            {
                ServiceIdentifier serviceIdentifier = pair.Key;
                Type? implementationType = pair.Value.ImplementationType;
                if (implementationType == null || serviceIdentifier.ServiceType == implementationType)
                {
                    builder.Append(TypeNameHelper.GetTypeDisplayName(serviceIdentifier.ServiceType));
                }
                else
                {
                    builder.Append(TypeNameHelper.GetTypeDisplayName(serviceIdentifier.ServiceType))
                           .Append('(')
                           .Append(TypeNameHelper.GetTypeDisplayName(implementationType))
                           .Append(')');
                }

                builder.Append(" -> ");
            }

            builder.Append(TypeNameHelper.GetTypeDisplayName(currentlyResolving.ServiceType));
        }

        private readonly struct ChainItemInfo
        {
            public int Order { get; }
            public Type? ImplementationType { get; }

            public ChainItemInfo(int order, Type? implementationType)
            {
                Order = order;
                ImplementationType = implementationType;
            }
        }
    }
}
