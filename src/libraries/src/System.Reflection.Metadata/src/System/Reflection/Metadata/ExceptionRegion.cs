// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata
{
    public struct ExceptionRegion
    {
        private readonly ExceptionRegionKind _kind;
        private readonly int _tryOffset;
        private readonly int _tryLength;
        private readonly int _handlerOffset;
        private readonly int _handlerLength;
        private readonly int _classTokenOrFilterOffset;

        internal ExceptionRegion(
            ExceptionRegionKind kind,
            int tryOffset,
            int tryLength,
            int handlerOffset,
            int handlerLength,
            int classTokenOrFilterOffset)
        {
            _kind = kind;
            _tryOffset = tryOffset;
            _tryLength = tryLength;
            _handlerOffset = handlerOffset;
            _handlerLength = handlerLength;
            _classTokenOrFilterOffset = classTokenOrFilterOffset;
        }

        public ExceptionRegionKind Kind
        {
            get { return _kind; }
        }

        /// <summary>
        /// Start IL offset of the try block.
        /// </summary>
        public int TryOffset
        {
            get { return _tryOffset; }
        }

        /// <summary>
        /// Length in bytes of try block.
        /// </summary>
        public int TryLength
        {
            get { return _tryLength; }
        }

        /// <summary>
        /// Start IL offset of the exception handler.
        /// </summary>
        public int HandlerOffset
        {
            get { return _handlerOffset; }
        }

        /// <summary>
        /// Length in bytes of the exception handler.
        /// </summary>
        public int HandlerLength
        {
            get { return _handlerLength; }
        }

        /// <summary>
        /// IL offset of the start of the filter block, or -1 if the region is not a filter.
        /// </summary>
        public int FilterOffset
        {
            get { return (Kind == ExceptionRegionKind.Filter) ? _classTokenOrFilterOffset : -1; }
        }

        /// <summary>
        /// Returns a TypeRef, TypeDef, or TypeSpec handle if the region represents a catch, nil token otherwise. 
        /// </summary>
        public EntityHandle CatchType
        {
            get { return (Kind == ExceptionRegionKind.Catch) ? new EntityHandle((uint)_classTokenOrFilterOffset) : default(EntityHandle); }
        }
    }
}
