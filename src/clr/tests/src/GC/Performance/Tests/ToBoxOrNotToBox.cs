// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace ToBoxOrNotToBox
{
    public interface IObjectId
    {
    }
  
    internal class ClassObjectId : IObjectId
    {
        private int m_TypeId;
        private long m_InstanceId;

        public int TypeId
        {
            get { return m_TypeId; }
            set { m_TypeId = value; }
        }

        public long InstanceId
        {
            get { return m_InstanceId; }
            set { m_InstanceId = value; }
        }

        public ClassObjectId(int typeId, long instanceId)
        {
            m_TypeId = typeId;
            m_InstanceId = instanceId;
        }
    }

    internal struct InternalObjectId
    {
        private int m_TypeId;
        private long m_InstanceId;

        public int TypeId
        {
            get { return m_TypeId; }
            set { m_TypeId = value; }
        }

        public long InstanceId
        {
            get { return m_InstanceId; }
            set { m_InstanceId = value; }
        }

        public InternalObjectId(int typeId, long instanceId)
        {
            m_TypeId = typeId;
            m_InstanceId = instanceId;
        }
    }

    public struct ObjectId
    {
        private int m_TypeId;
        private long m_InstanceId;

        internal ObjectId(InternalObjectId id)
        {
            m_TypeId = id.TypeId;
            m_InstanceId = id.InstanceId;
        }
    }

    class Program
    {
        static void PerfTest2(int count, int length)
        {            
            for (int i = 0; i < count; i++)
            {
                ClassObjectId[] internalIds = new ClassObjectId[length];
                for (int k = 0; k < length; k++)
                {
                    internalIds[k] = new ClassObjectId(10, k + 1);                   
                }

                IObjectId[] publicIds = new IObjectId[length];
                Array.Copy(internalIds, publicIds, length);
            }

        }

        static void PerfTest1(int count, int length)
        {
            
            for (int i = 0; i < count; i++)
            {
                InternalObjectId[] internalIds = new InternalObjectId[length];
                for (int k = 0; k < length; k++)
                {
                    internalIds[k] = new InternalObjectId(10, k + 1);                    
                }

                ObjectId[] publicIds = new ObjectId[length];
                for (int k = 0; k < length; k++)
                {
                    publicIds[k] = new ObjectId(internalIds[k]);
                }
            }

        }
        
        static void Main(string[] args)
        {

            const int baseCount = 2000000;
            for (int i = 1; i <= 1000000; i+=10000)
            {
                int count = baseCount / i;
                PerfTest1(count, i);                
                PerfTest2(count, i);
            }

        }
    }
}
