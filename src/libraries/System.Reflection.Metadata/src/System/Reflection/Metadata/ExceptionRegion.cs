// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata
{
    public struct ExceptionRegion
    {
        private readonly ExceptionRegionKind kind;
        private readonly int tryOffset;
        private readonly int tryLength;
        private readonly int handlerOffset;
        private readonly int handlerLength;
        private readonly int classTokenOrFilterOffset;

        internal ExceptionRegion(
            ExceptionRegionKind kind,
            int tryOffset,
            int tryLength,
            int handlerOffset,
            int handlerLength,
            int classTokenOrFilterOffset)
        {
            this.kind = kind;
            this.tryOffset = tryOffset;
            this.tryLength = tryLength;
            this.handlerOffset = handlerOffset;
            this.handlerLength = handlerLength;
            this.classTokenOrFilterOffset = classTokenOrFilterOffset;
        }

        public ExceptionRegionKind Kind
        {
            get { return kind; }
        }

        /// <summary>
        /// Start IL offset of the try block.
        /// </summary>
        public int TryOffset
        {
            get { return tryOffset; }
        }

        /// <summary>
        /// Length in bytes of try block.
        /// </summary>
        public int TryLength
        {
            get { return tryLength; }
        }

        /// <summary>
        /// Start IL offset of the exception handler.
        /// </summary>
        public int HandlerOffset
        {
            get { return handlerOffset; }
        }

        /// <summary>
        /// Length in bytes of the exception handler.
        /// </summary>
        public int HandlerLength
        {
            get { return handlerLength; }
        }

        /// <summary>
        /// IL offset of the start of the filter block, or -1 if the region is not a filter.
        /// </summary>
        public int FilterOffset
        {
            get { return (Kind == ExceptionRegionKind.Filter) ? classTokenOrFilterOffset : -1; }
        }

        /// <summary>
        /// Returns a TypeRef, TypeDef, or TypeSpec handle if the region represents a catch, nil token otherwise. 
        /// </summary>
        public Handle CatchType
        {
            get { return (Kind == ExceptionRegionKind.Catch) ? new Handle((uint)classTokenOrFilterOffset) : default(Handle); }
        }
    }
}
