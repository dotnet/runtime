// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using Microsoft.Extensions.DependencyInjection.ServiceLookup;

namespace Microsoft.Extensions.DependencyInjection
{
    internal class CallSiteJsonFormatter: CallSiteVisitor<CallSiteJsonFormatter.CallSiteFormatterContext, object>
    {
        internal static CallSiteJsonFormatter Instance = new CallSiteJsonFormatter();

        private CallSiteJsonFormatter()
        {
        }

        public string Format(ServiceCallSite callSite)
        {
            var stringBuilder = new StringBuilder();
            var context = new CallSiteFormatterContext(stringBuilder, 0);

            VisitCallSite(callSite, context);

            return stringBuilder.ToString();
        }

        protected override object VisitConstructor(ConstructorCallSite constructorCallSite, CallSiteFormatterContext argument)
        {
            argument.WriteProperty("implementationType", constructorCallSite.ImplementationType);

            if (constructorCallSite.ParameterCallSites.Length > 0)
            {
                argument.StartProperty("arguments");

                var childContext = argument.StartArray();
                foreach (var parameter in constructorCallSite.ParameterCallSites)
                {
                    childContext.StartArrayItem();
                    VisitCallSite(parameter, childContext);
                }
                argument.EndArray();
            }

            return null;
        }

        protected override object VisitCallSiteMain(ServiceCallSite callSite, CallSiteFormatterContext argument)
        {
            var childContext = argument.StartObject();

            childContext.WriteProperty("serviceType", callSite.ServiceType);
            childContext.WriteProperty("kind", callSite.Kind);
            childContext.WriteProperty("cache", callSite.Cache.Location);

            base.VisitCallSiteMain(callSite, childContext);

            argument.EndObject();

            return null;
        }

        protected override object VisitConstant(ConstantCallSite constantCallSite, CallSiteFormatterContext argument)
        {
            argument.WriteProperty("value", constantCallSite.DefaultValue ?? "");

            return null;
        }

        protected override object VisitServiceProvider(ServiceProviderCallSite serviceProviderCallSite, CallSiteFormatterContext argument)
        {
            return null;
        }

        protected override object VisitServiceScopeFactory(ServiceScopeFactoryCallSite serviceScopeFactoryCallSite, CallSiteFormatterContext argument)
        {
            return null;
        }

        protected override object VisitIEnumerable(IEnumerableCallSite enumerableCallSite, CallSiteFormatterContext argument)
        {
            argument.WriteProperty("itemType", enumerableCallSite.ItemType);
            argument.WriteProperty("size", enumerableCallSite.ServiceCallSites.Length);

            if (enumerableCallSite.ServiceCallSites.Length > 0)
            {
                argument.StartProperty("items");

                var childContext = argument.StartArray();
                foreach (var item in enumerableCallSite.ServiceCallSites)
                {
                    childContext.StartArrayItem();
                    VisitCallSite(item, childContext);
                }
                argument.EndArray();
            }
            return null;
        }

        protected override object VisitFactory(FactoryCallSite factoryCallSite, CallSiteFormatterContext argument)
        {
            argument.WriteProperty("method", factoryCallSite.Factory.Method);

            return null;
        }

        internal struct CallSiteFormatterContext
        {
            public CallSiteFormatterContext(StringBuilder builder, int offset)
            {
                Builder = builder;
                Offset = offset;
                _firstItem = true;
            }

            private bool _firstItem;

            public int Offset { get; }

            public StringBuilder Builder { get; }

            public CallSiteFormatterContext IncrementOffset()
            {
                return new CallSiteFormatterContext(Builder, Offset + 4)
                {
                    _firstItem = true
                };
            }

            private void WriteOffset()
            {
                for (int i = 0; i < Offset; i++)
                {
                    Builder.Append(' ');
                }
            }

            public CallSiteFormatterContext StartObject()
            {
                WriteOffset();
                Builder.AppendLine("{");
                return IncrementOffset();
            }

            public void EndObject()
            {
                Builder.AppendLine();
                WriteOffset();
                Builder.Append("}");
            }

            public void StartProperty(string name)
            {
                if (!_firstItem)
                {
                    Builder.AppendLine(",");
                }
                else
                {
                    _firstItem = false;
                }

                WriteOffset();
                Builder.AppendFormat("\"{0}\":", name);
            }

            public void StartArrayItem()
            {
                if (!_firstItem)
                {
                    Builder.AppendLine(",");
                }
                else
                {
                    _firstItem = false;
                }
            }

            public void WriteProperty(string name, object value)
            {
                StartProperty(name);
                if (value != null)
                {
                    Builder.AppendFormat(" \"{0}\"", value);
                }
                else
                {
                    Builder.AppendFormat( "null");
                }
            }

            public CallSiteFormatterContext StartArray()
            {
                Builder.AppendLine();
                WriteOffset();
                Builder.AppendLine("[");
                return IncrementOffset();
            }

            public void EndArray()
            {
                if (!_firstItem)
                {
                    Builder.AppendLine();
                }
                WriteOffset();
                Builder.Append("]");
            }
        }
    }
}
