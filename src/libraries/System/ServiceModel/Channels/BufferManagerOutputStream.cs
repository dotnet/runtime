////------------------------------------------------------------
//// Copyright (c) Microsoft Corporation.  All rights reserved.
////------------------------------------------------------------

//namespace System.ServiceModel.Channels
//{
//    using System;
//    using System.Runtime;
//    using Microsoft.ServiceModel; // for QuotaExceededException
//    //using System.ServiceModel.Channels;
//    //using System.ServiceModel.Diagnostics.Application;

//    class BufferManagerOutputStream : BufferedOutputStream
//    {
//        string quotaExceededString;
//        private object xmlBufferQuotaExceeded;
//        private int initialBufferSize;
//        private int maxBufferSize;
//        private InternalBufferManager bufferManager;

//        public BufferManagerOutputStream(string quotaExceededString)
//            : base()
//        {
//            this.quotaExceededString = quotaExceededString;
//        }

//        public BufferManagerOutputStream(string quotaExceededString, int maxSize)
//            : base(maxSize)
//        {
//            this.quotaExceededString = quotaExceededString;
//        }

//        //public BufferManagerOutputStream(string quotaExceededString, int initialSize, int maxSize, BufferManager bufferManager)
//        //    : base(initialSize, maxSize, BufferManager.GetInternalBufferManager(bufferManager))
//        //{
//        //    this.quotaExceededString = quotaExceededString;
//        //}

//        public BufferManagerOutputStream(object xmlBufferQuotaExceeded, int initialBufferSize, int maxBufferSize, InternalBufferManager bufferManager)
//        {
//            this.xmlBufferQuotaExceeded = xmlBufferQuotaExceeded;
//            this.initialBufferSize = initialBufferSize;
//            this.maxBufferSize = maxBufferSize;
//            this.bufferManager = bufferManager;
//        }

//        public void Init(int initialSize, int maxSizeQuota, InternalBufferManager bufferManager)
//        {
//            Init(initialSize, maxSizeQuota, maxSizeQuota, bufferManager);
//        }

//        public void Init(int initialSize, int maxSizeQuota, int effectiveMaxSize, InternalBufferManager bufferManager)
//        {
//            base.Reinitialize(initialSize, maxSizeQuota, effectiveMaxSize, null/*BufferManager.GetInternalBufferManager(bufferManager)*/);
//        }

//        protected override Exception CreateQuotaExceededException(int maxSizeQuota)
//        {
//            string excMsg = SR.GetString(this.quotaExceededString, maxSizeQuota);
//            //if (TD.MaxSentMessageSizeExceededIsEnabled())
//            //{
//            //    TD.MaxSentMessageSizeExceeded(excMsg);
//            //}
//            return new NotSupportedException();// QuotaExceededException(excMsg);
//        }
//    }
//}
