// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace Tracing.Tests.Common
{
    public enum EventPipeSerializationFormat
    {
        NetPerf,
        NetTrace
    }

    public sealed class TraceConfiguration
    {
        private ConstructorInfo m_configurationCtor;
        private MethodInfo m_enableProviderMethod;
        private MethodInfo m_setProfilerSamplingRateMethod;

        private object m_configurationObject;

        public TraceConfiguration(
            string outputFile,
            uint circularBufferMB)
        {
            // Initialize reflection references.
            if(!Initialize())
            {
                throw new InvalidOperationException("Reflection failed.");
            }

            m_configurationObject = m_configurationCtor.Invoke(
                new object[]
                {
                    outputFile,
                    EventPipeSerializationFormat.NetTrace,
                    circularBufferMB
                });
        }

        public void EnableProvider(
            string providerName,
            UInt64 keywords,
            uint level)
        {
            m_enableProviderMethod.Invoke(
                m_configurationObject,
                new object[]
                {
                    providerName,
                    keywords,
                    level
                });
        }

        internal object ConfigurationObject
        {
            get { return m_configurationObject; }
        }

        public void SetSamplingRate(TimeSpan minDelayBetweenSamples)
        {
            m_setProfilerSamplingRateMethod.Invoke(
                m_configurationObject,
                new object[]
                {
                    minDelayBetweenSamples
                });
        }

        private bool Initialize()
        {
           Assembly SPC = typeof(System.Diagnostics.Tracing.EventSource).Assembly;
           if(SPC == null)
           {
               Console.WriteLine("System.Private.CoreLib assembly == null");
               return false;
           }

           Type configurationType = SPC.GetType("System.Diagnostics.Tracing.EventPipeConfiguration");
           if(configurationType == null)
           {
               Console.WriteLine("configurationType == null");
               return false;
           }

            Type formatType = SPC.GetType("System.Diagnostics.Tracing.EventPipeSerializationFormat");
            if (formatType == null)
            {
                Console.WriteLine("formatType == null");
                return false;
            }

            m_configurationCtor = configurationType.GetConstructor(
               BindingFlags.NonPublic | BindingFlags.Instance,
               null,
               new Type[] { typeof(string), formatType, typeof(uint) },
               null);
           if(m_configurationCtor == null)
           {
               Console.WriteLine("configurationCtor == null");
               return false;
           }

           m_enableProviderMethod = configurationType.GetMethod(
               "EnableProvider",
               BindingFlags.NonPublic | BindingFlags.Instance);
           if(m_enableProviderMethod == null)
           {
               Console.WriteLine("enableProviderMethod == null");
               return false;
           }

           m_setProfilerSamplingRateMethod = configurationType.GetMethod(
               "SetProfilerSamplingRate",
               BindingFlags.NonPublic | BindingFlags.Instance);
           if(m_setProfilerSamplingRateMethod == null)
           {
               Console.WriteLine("setProfilerSamplingRate == null");
               return false;
           }

           return true;
        }
    }
}
