// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Zstd
    {
        internal enum ZSTD_error
        {
            no_error = 0,
            GENERIC = -1,
            prefix_unknown = -10,
            version_unsupported = -12,
            frameParameter_unsupported = -14,
            frameParameter_windowTooLarge = -16,
            corruption_detected = -20,
            checksum_wrong = -22,
            literals_headerWrong = -24,
            dictionary_corrupted = -30,
            dictionary_wrong = -32,
            dictionaryCreation_failed = -34,
            parameter_unsupported = -40,
            parameter_combination_unsupported = -41,
            parameter_outOfBound = -42,
            tableLog_tooLarge = -44,
            maxSymbolValue_tooLarge = -46,
            maxSymbolValue_tooSmall = -48,
            cannotProduce_uncompressedBlock = -49,
            stabilityCondition_notRespected = -50,
            stage_wrong = -60,
            init_missing = -62,
            memory_allocation = -64,
            workSpace_tooSmall = -66,
            dstSize_tooSmall = -70,
            srcSize_wrong = -72,
            dstBuffer_null = -74,
            noForwardProgress_destFull = -80,
            noForwardProgress_inputEmpty = -82,
        }

        // Compression context management
        [LibraryImport(Libraries.CompressionNative)]
        internal static partial SafeZstdCompressHandle ZSTD_createCCtx();

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_freeCCtx(IntPtr cctx);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial SafeZstdDecompressHandle ZSTD_createDCtx();

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_freeDCtx(IntPtr dctx);

        // Dictionary management
        [LibraryImport(Libraries.CompressionNative)]
        internal static unsafe partial SafeZstdCDictHandle ZSTD_createCDict_byReference(byte* dictBuffer, nuint dictSize, int compressionLevel);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_freeCDict(IntPtr cdict);

        [LibraryImport(Libraries.CompressionNative)]
        internal static unsafe partial SafeZstdDDictHandle ZSTD_createDDict_byReference(byte* dictBuffer, nuint dictSize);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_freeDDict(IntPtr ddict);

        // Dictionary training functions
        [LibraryImport(Libraries.CompressionNative)]
        internal static unsafe partial nuint ZDICT_trainFromBuffer(byte* dictBuffer, nuint dictBufferCapacity, byte* samplesBuffer, nuint* samplesSizes, uint nbSamples);

        // Compression functions
        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_compressBound(nuint srcSize);

        [LibraryImport(Libraries.CompressionNative)]
        internal static unsafe partial nuint ZSTD_compress2(SafeZstdCompressHandle cctx, byte* dst, nuint dstCapacity, byte* src, nuint srcSize);

        [LibraryImport(Libraries.CompressionNative)]
        internal static unsafe partial nuint ZSTD_compress_usingCDict(SafeZstdCompressHandle cctx, byte* dst, nuint dstCapacity, byte* src, nuint srcSize, SafeZstdCDictHandle cdict);

        // Decompression functions
        [LibraryImport(Libraries.CompressionNative)]
        internal static unsafe partial nuint ZSTD_decompress(byte* dst, nuint dstCapacity, byte* src, nuint srcSize);

        [LibraryImport(Libraries.CompressionNative)]
        internal static unsafe partial nuint ZSTD_decompressDCtx(SafeZstdDecompressHandle dctx, byte* dst, nuint dstCapacity, byte* src, nuint srcSize);

        [LibraryImport(Libraries.CompressionNative)]
        internal static unsafe partial nuint ZSTD_decompress_usingDDict(SafeZstdDecompressHandle dctx, byte* dst, nuint dstCapacity, byte* src, nuint srcSize, SafeZstdDDictHandle ddict);

        [LibraryImport(Libraries.CompressionNative)]
        internal static unsafe partial ulong ZSTD_decompressBound(byte* src, nuint srcSize);

        // Streaming decompression
        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_decompressStream(SafeZstdDecompressHandle dctx, ref ZstdOutBuffer output, ref ZstdInBuffer input);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_DCtx_setParameter(SafeZstdDecompressHandle dctx, ZstdDParameter param, int value);

        [LibraryImport(Libraries.CompressionNative)]
        internal static unsafe partial nuint ZSTD_DCtx_refPrefix(SafeZstdDecompressHandle dctx, byte* prefix, nuint prefixSize);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_DCtx_reset(SafeZstdDecompressHandle dctx, ZstdResetDirective reset);

        // Streaming compression
        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_compressStream2(SafeZstdCompressHandle cctx, ref ZstdOutBuffer output, ref ZstdInBuffer input, ZstdEndDirective endOp);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_CCtx_setParameter(SafeZstdCompressHandle cctx, ZstdCParameter param, int value);

        [LibraryImport(Libraries.CompressionNative)]
        internal static unsafe partial nuint ZSTD_CCtx_refPrefix(SafeZstdCompressHandle cctx, byte* prefix, nuint prefixSize);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_CCtx_reset(SafeZstdCompressHandle cctx, ZstdResetDirective reset);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_CCtx_setPledgedSrcSize(SafeZstdCompressHandle cctx, ulong pledgedSrcSize);

        // Compression level functions
        [LibraryImport(Libraries.CompressionNative)]
        internal static partial int ZSTD_minCLevel();

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial int ZSTD_maxCLevel();

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial int ZSTD_defaultCLevel();

        // Error checking
        [LibraryImport(Libraries.CompressionNative)]
        internal static partial uint ZSTD_isError(nuint result);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial IntPtr ZSTD_getErrorName(nuint result);

        // Dictionary context functions
        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_DCtx_refDDict(SafeZstdDecompressHandle dctx, SafeZstdDDictHandle ddict);

        [LibraryImport(Libraries.CompressionNative)]
        internal static partial nuint ZSTD_CCtx_refCDict(SafeZstdCompressHandle cctx, SafeZstdCDictHandle cdict);

        // Enums and structures for streaming
        internal enum ZstdEndDirective
        {
            ZSTD_e_continue = 0,
            ZSTD_e_flush = 1,
            ZSTD_e_end = 2
        }

        internal enum ZstdCParameter
        {
            /// <summary>
            /// Set compression parameters according to pre-defined cLevel table.
            /// Note that exact compression parameters are dynamically determined,
            /// depending on both compression level and srcSize (when known).
            /// Default level is ZSTD_CLEVEL_DEFAULT==3.
            /// Special: value 0 means default, which is controlled by ZSTD_CLEVEL_DEFAULT.
            /// Note 1 : it's possible to pass a negative compression level.
            /// Note 2 : setting a level does not automatically set all other compression parameters
            ///   to default. Setting this will however eventually dynamically impact the compression
            ///   parameters which have not been manually set. The manually set
            ///   ones will 'stick'.
            /// </summary>
            ZSTD_c_compressionLevel = 100,

            // Advanced compression parameters :
            // It's possible to pin down compression parameters to some specific values.
            // In which case, these values are no longer dynamically selected by the compressor

            /// <summary>
            /// Maximum allowed back-reference distance, expressed as power of 2.
            /// This will set a memory budget for streaming decompression,
            /// with larger values requiring more memory
            /// and typically compressing more.
            /// Must be clamped between ZSTD_WINDOWLOG_MIN and ZSTD_WINDOWLOG_MAX.
            /// Special: value 0 means "use default windowLog".
            /// Note: Using a windowLog greater than ZSTD_WINDOWLOG_LIMIT_DEFAULT
            ///       requires explicitly allowing such size at streaming decompression stage.
            /// </summary>
            ZSTD_c_windowLog = 101,

            /// <summary>
            /// Size of the initial probe table, as a power of 2.
            /// Resulting memory usage is (1 &lt;&lt; (hashLog+2)).
            /// Must be clamped between ZSTD_HASHLOG_MIN and ZSTD_HASHLOG_MAX.
            /// Larger tables improve compression ratio of strategies &lt;= dFast,
            /// and improve speed of strategies &gt; dFast.
            /// Special: value 0 means "use default hashLog".
            /// </summary>
            ZSTD_c_hashLog = 102,

            /// <summary>
            /// Size of the multi-probe search table, as a power of 2.
            /// Resulting memory usage is (1 &lt;&lt; (chainLog+2)).
            /// Must be clamped between ZSTD_CHAINLOG_MIN and ZSTD_CHAINLOG_MAX.
            /// Larger tables result in better and slower compression.
            /// This parameter is useless for "fast" strategy.
            /// It's still useful when using "dfast" strategy,
            /// in which case it defines a secondary probe table.
            /// Special: value 0 means "use default chainLog".
            /// </summary>
            ZSTD_c_chainLog = 103,

            /// <summary>
            /// Number of search attempts, as a power of 2.
            /// More attempts result in better and slower compression.
            /// This parameter is useless for "fast" and "dFast" strategies.
            /// Special: value 0 means "use default searchLog".
            /// </summary>
            ZSTD_c_searchLog = 104,

            /// <summary>
            /// Minimum size of searched matches.
            /// Note that Zstandard can still find matches of smaller size,
            /// it just tweaks its search algorithm to look for this size and larger.
            /// Larger values increase compression and decompression speed, but decrease ratio.
            /// Must be clamped between ZSTD_MINMATCH_MIN and ZSTD_MINMATCH_MAX.
            /// Note that currently, for all strategies &lt; btopt, effective minimum is 4.
            ///                    , for all strategies &gt; fast, effective maximum is 6.
            /// Special: value 0 means "use default minMatchLength".
            /// </summary>
            ZSTD_c_minMatch = 105,

            /// <summary>
            /// Impact of this field depends on strategy.
            /// For strategies btopt, btultra &amp; btultra2:
            ///     Length of Match considered "good enough" to stop search.
            ///     Larger values make compression stronger, and slower.
            /// For strategy fast:
            ///     Distance between match sampling.
            ///     Larger values make compression faster, and weaker.
            /// Special: value 0 means "use default targetLength".
            /// </summary>
            ZSTD_c_targetLength = 106,

            /// <summary>
            /// See ZSTD_strategy enum definition.
            /// The higher the value of selected strategy, the more complex it is,
            /// resulting in stronger and slower compression.
            /// Special: value 0 means "use default strategy".
            /// </summary>
            ZSTD_c_strategy = 107,

            /// <summary>
            /// v1.5.6+
            /// Attempts to fit compressed block size into approximately targetCBlockSize.
            /// Bound by ZSTD_TARGETCBLOCKSIZE_MIN and ZSTD_TARGETCBLOCKSIZE_MAX.
            /// Note that it's not a guarantee, just a convergence target (default:0).
            /// No target when targetCBlockSize == 0.
            /// This is helpful in low bandwidth streaming environments to improve end-to-end latency,
            /// when a client can make use of partial documents (a prominent example being Chrome).
            /// Note: this parameter is stable since v1.5.6.
            /// It was present as an experimental parameter in earlier versions,
            /// but it's not recommended using it with earlier library versions
            /// due to massive performance regressions.
            /// </summary>
            ZSTD_c_targetCBlockSize = 130,

            // LDM mode parameters

            /// <summary>
            /// Enable long distance matching.
            /// This parameter is designed to improve compression ratio
            /// for large inputs, by finding large matches at long distance.
            /// It increases memory usage and window size.
            /// Note: enabling this parameter increases default ZSTD_c_windowLog to 128 MB
            /// except when expressly set to a different value.
            /// Note: will be enabled by default if ZSTD_c_windowLog >= 128 MB and
            /// compression strategy >= ZSTD_btopt (== compression level 16+)
            /// </summary>
            ZSTD_c_enableLongDistanceMatching = 160,

            /// <summary>
            /// Size of the table for long distance matching, as a power of 2.
            /// Larger values increase memory usage and compression ratio,
            /// but decrease compression speed.
            /// Must be clamped between ZSTD_HASHLOG_MIN and ZSTD_HASHLOG_MAX
            /// default: windowlog - 7.
            /// Special: value 0 means "automatically determine hashlog".
            /// </summary>
            ZSTD_c_ldmHashLog = 161,

            /// <summary>
            /// Minimum match size for long distance matcher.
            /// Larger/too small values usually decrease compression ratio.
            /// Must be clamped between ZSTD_LDM_MINMATCH_MIN and ZSTD_LDM_MINMATCH_MAX.
            /// Special: value 0 means "use default value" (default: 64).
            /// </summary>
            ZSTD_c_ldmMinMatch = 162,

            /// <summary>
            /// Log size of each bucket in the LDM hash table for collision resolution.
            /// Larger values improve collision resolution but decrease compression speed.
            /// The maximum value is ZSTD_LDM_BUCKETSIZELOG_MAX.
            /// Special: value 0 means "use default value" (default: 3).
            /// </summary>
            ZSTD_c_ldmBucketSizeLog = 163,

            /// <summary>
            /// Frequency of inserting/looking up entries into the LDM hash table.
            /// Must be clamped between 0 and (ZSTD_WINDOWLOG_MAX - ZSTD_HASHLOG_MIN).
            /// Default is MAX(0, (windowLog - ldmHashLog)), optimizing hash table usage.
            /// Larger values improve compression speed.
            /// Deviating far from default value will likely result in a compression ratio decrease.
            /// Special: value 0 means "automatically determine hashRateLog".
            /// </summary>
            ZSTD_c_ldmHashRateLog = 164,


            // frame parameters*/

            /// <summary>
            /// Content size will be written into frame header _whenever known_ (default:1)
            /// Content size must be known at the beginning of compression.
            /// This is automatically the case when using ZSTD_compress2(),
            /// For streaming scenarios, content size must be provided with ZSTD_CCtx_setPledgedSrcSize()
            /// </summary>
            ZSTD_c_contentSizeFlag = 200,

            /// <summary>
            /// A 32-bits checksum of content is written at end of frame (default:0)
            /// </summary>
            ZSTD_c_checksumFlag = 201,

            /// <summary>
            /// When applicable, dictionary's ID is written into frame header (default:1)
            /// </summary>
            ZSTD_c_dictIDFlag = 202,

            /* multi-threading parameters */
            /* These parameters are only active if multi-threading is enabled (compiled with build macro ZSTD_MULTITHREAD).
             * Otherwise, trying to set any other value than default (0) will be a no-op and return an error.
             * In a situation where it's unknown if the linked library supports multi-threading or not,
             * setting ZSTD_c_nbWorkers to any value >= 1 and consulting the return value provides a quick way to check this property.
             */

            /// <summary>
            /// Select how many threads will be spawned to compress in parallel.
            /// When nbWorkers >= 1, triggers asynchronous mode when invoking ZSTD_compressStream*() :
            /// ZSTD_compressStream*() consumes input and flush output if possible, but immediately gives back control to caller,
            /// while compression is performed in parallel, within worker thread(s).
            /// (note : a strong exception to this rule is when first invocation of ZSTD_compressStream2() sets ZSTD_e_end :
            ///  in which case, ZSTD_compressStream2() delegates to ZSTD_compress2(), which is always a blocking call).
            /// More workers improve speed, but also increase memory usage.
            /// Default value is `0`, aka "single-threaded mode" : no worker is spawned,
            /// compression is performed inside Caller's thread, and all invocations are blocking
            /// </summary>
            ZSTD_c_nbWorkers = 400,

            /// <summary>
            /// Size of a compression job. This value is enforced only when nbWorkers >= 1.
            /// Each compression job is completed in parallel, so this value can indirectly impact the nb of active threads.
            /// 0 means default, which is dynamically determined based on compression parameters.
            /// Job size must be a minimum of overlap size, or ZSTDMT_JOBSIZE_MIN (= 512 KB), whichever is largest.
            /// The minimum size is automatically and transparently enforced.
            /// </summary>
            ZSTD_c_jobSize = 401,

            /// <summary>
            /// Control the overlap size, as a fraction of window size.
            /// The overlap size is an amount of data reloaded from previous job at the beginning of a new job.
            /// It helps preserve compression ratio, while each job is compressed in parallel.
            /// This value is enforced only when nbWorkers >= 1.
            /// Larger values increase compression ratio, but decrease speed.
            /// Possible values range from 0 to 9 :
            /// - 0 means "default" : value will be determined by the library, depending on strategy
            /// - 1 means "no overlap"
            /// - 9 means "full overlap", using a full window size.
            /// Each intermediate rank increases/decreases load size by a factor 2 :
            /// 9: full window;  8: w/2;  7: w/4;  6: w/8;  5:w/16;  4: w/32;  3:w/64;  2:w/128;  1:no overlap;  0:default
            /// default value varies between 6 and 9, depending on strategy
            /// </summary>
            ZSTD_c_overlapLog = 402,

            /* note : additional experimental parameters are also available
             * within the experimental section of the API.
             * At the time of this writing, they include :
             * ZSTD_c_rsyncable
             * ZSTD_c_format
             * ZSTD_c_forceMaxWindow
             * ZSTD_c_forceAttachDict
             * ZSTD_c_literalCompressionMode
             * ZSTD_c_srcSizeHint
             * ZSTD_c_enableDedicatedDictSearch
             * ZSTD_c_stableInBuffer
             * ZSTD_c_stableOutBuffer
             * ZSTD_c_blockDelimiters
             * ZSTD_c_validateSequences
             * ZSTD_c_blockSplitterLevel
             * ZSTD_c_splitAfterSequences
             * ZSTD_c_useRowMatchFinder
             * ZSTD_c_prefetchCDictTables
             * ZSTD_c_enableSeqProducerFallback
             * ZSTD_c_maxBlockSize
             * Because they are not stable, it's necessary to define ZSTD_STATIC_LINKING_ONLY to access them.
             * note : never ever use experimentalParam? names directly;
             *        also, the enums values themselves are unstable and can still change.
             */
            ZSTD_c_experimentalParam1 = 500,
            ZSTD_c_experimentalParam2 = 10,
            ZSTD_c_experimentalParam3 = 1000,
            ZSTD_c_experimentalParam4 = 1001,
            ZSTD_c_experimentalParam5 = 1002,
            /* was ZSTD_c_experimentalParam6=1003; is now ZSTD_c_targetCBlockSize */
            ZSTD_c_experimentalParam7 = 1004,
            ZSTD_c_experimentalParam8 = 1005,
            ZSTD_c_experimentalParam9 = 1006,
            ZSTD_c_experimentalParam10 = 1007,
            ZSTD_c_experimentalParam11 = 1008,
            ZSTD_c_experimentalParam12 = 1009,
            ZSTD_c_experimentalParam13 = 1010,
            ZSTD_c_experimentalParam14 = 1011,
            ZSTD_c_experimentalParam15 = 1012,
            ZSTD_c_experimentalParam16 = 1013,
            ZSTD_c_experimentalParam17 = 1014,
            ZSTD_c_experimentalParam18 = 1015,
            ZSTD_c_experimentalParam19 = 1016,
            ZSTD_c_experimentalParam20 = 1017
        }

        internal enum ZstdDParameter
        {
            /// <summary>
            /// Select a size limit (in power of 2) beyond which
            /// the streaming API will refuse to allocate memory buffer
            /// in order to protect the host from unreasonable memory requirements.
            /// This parameter is only useful in streaming mode, since no internal buffer is allocated in single-pass mode.
            /// By default, a decompression context accepts window sizes &lt;= (1 &lt;&lt; ZSTD_WINDOWLOG_LIMIT_DEFAULT).
            /// Special: value 0 means "use default maximum windowLog".
            /// </summary>
            ZSTD_d_windowLogMax = 100,

            /* note : additional experimental parameters are also available
             * within the experimental section of the API.
             * At the time of this writing, they include :
             * ZSTD_d_format
             * ZSTD_d_stableOutBuffer
             * ZSTD_d_forceIgnoreChecksum
             * ZSTD_d_refMultipleDDicts
             * ZSTD_d_disableHuffmanAssembly
             * ZSTD_d_maxBlockSize
             * Because they are not stable, it's necessary to define ZSTD_STATIC_LINKING_ONLY to access them.
             * note : never ever use experimentalParam? names directly
             */
            ZSTD_d_experimentalParam1 = 1000,
            ZSTD_d_experimentalParam2 = 1001,
            ZSTD_d_experimentalParam3 = 1002,
            ZSTD_d_experimentalParam4 = 1003,
            ZSTD_d_experimentalParam5 = 1004,
            ZSTD_d_experimentalParam6 = 1005

        }

        internal enum ZstdResetDirective
        {
            ZSTD_reset_session_only = 1,
            ZSTD_reset_parameters = 2,
            ZSTD_reset_session_and_parameters = 3
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct ZstdInBuffer
        {
            internal byte* src;
            internal nuint size;
            internal nuint pos;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct ZstdOutBuffer
        {
            internal byte* dst;
            internal nuint size;
            internal nuint pos;
        }
    }
}
