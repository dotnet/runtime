// zipmark.cs
// (This file is the NZipLib sources with a crude benchmark class attached -
//  2002-15-05 Dan Lewis <dihlewis@yahoo.co.uk>)
// -------------------------------------------------------------------------
//
// NZipLib source:
// Copyright (C) 2001 Mike Krueger
//
// This file was translated from java, it was part of the GNU Classpath
// Copyright (C) 2001 Free Software Foundation, Inc.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// As a special exception, if you link this library with other files to
// produce an executable, this library does not by itself cause the
// resulting executable to be covered by the GNU General Public License.
// This exception does not however invalidate any other reasons why the
// executable file might be covered by the GNU General Public License.

using System;
using System.IO;
using NZlib.Streams;
using NZlib.Checksums;
using NZlib.Compression;

class ZipMark {
	static int Iterations = 1000;
	static int BlockSize = 1024;

	public static void Main (string [] args)
	{
		if (args.Length == 0 || args.Length > 3) {
			Console.WriteLine ("Usage: zipmark FILE [ITERATIONS] [BLOCKSIZE]");
			return;
		}
	
		string filename = args [0];
		FileInfo file = new FileInfo (filename);
		if (!file.Exists) {
			Console.WriteLine ("Couldn't find file {0}", filename);
			return;
		}

		FileStream fs = file.OpenRead ();

		byte [] raw = new byte [file.Length];
		int count = fs.Read (raw, 0, (int)file.Length);
		fs.Close ();

		if (count != file.Length) {
			Console.WriteLine ("Couldn't read file {0}", filename);
			return;
		}

		Deflater def = new Deflater (Deflater.BEST_COMPRESSION, false);
		Inflater inf = new Inflater (false);

		// 1. Count deflated size

		int cooked_size = Deflate (def, raw, null);
		byte [] cooked = new byte [cooked_size];

		// 2. Deflate & Inflate

		if (args.Length > 1)
			Iterations = Int32.Parse (args [1]);
		if (args.Length > 2)
			BlockSize = Int32.Parse (args [2]);

		for (int i = 0; i < Iterations; ++ i) {
			Deflate (def, raw, cooked);
			Inflate (inf, cooked, raw);
		}

		return;
	}

	static int Deflate (Deflater def, byte [] src, byte [] dest)
	{
		bool count;
		int offset, length, remain;
		
		if (dest == null) {
			dest = new byte [BlockSize];
			count = true;
		} else
			count = false;
		
		def.Reset ();
		def.SetInput (src);

		offset = 0;
		while (!def.IsFinished) {
			if (def.IsNeedingInput)
				def.Finish ();

			remain = Math.Min (dest.Length - offset, BlockSize);
			if (remain == 0)
				break;

			length = def.Deflate (dest, offset, remain);
			if (!count)
				offset += length;
		}

		return def.TotalOut;
	}

	static int Inflate (Inflater inf, byte [] src, byte [] dest)
	{
		int offset, length, remain;
	
		inf.Reset ();
		inf.SetInput (src);

		offset = 0;
		while (!inf.IsNeedingInput) {
			remain = Math.Min (dest.Length - offset, BlockSize);
			if (remain == 0)
				break;

			length = inf.Inflate (dest, offset, remain);
			offset += length;
		}

		return inf.TotalOut;
	}
}

// ----------------------  NZipLib sources from here on --------------------------

namespace NZlib.Compression {
	
	/// <summary>
	/// This is the Deflater class.  The deflater class compresses input
	/// with the deflate algorithm described in RFC 1951.  It has several
	/// compression levels and three different strategies described below.
	///
	/// This class is <i>not</i> thread safe.  This is inherent in the API, due
	/// to the split of deflate and setInput.
	/// 
	/// author of the original java version : Jochen Hoenicke
	/// </summary>
	public class Deflater
	{
		/// <summary>
		/// The best and slowest compression level.  This tries to find very
		/// long and distant string repetitions.
		/// </summary>
		public static  int BEST_COMPRESSION = 9;
		
		/// <summary>
		/// The worst but fastest compression level.
		/// </summary>
		public static  int BEST_SPEED = 1;
		
		/// <summary>
		/// The default compression level.
		/// </summary>
		public static  int DEFAULT_COMPRESSION = -1;
		
		/// <summary>
		/// This level won't compress at all but output uncompressed blocks.
		/// </summary>
		public static  int NO_COMPRESSION = 0;
		
		/// <summary>
		/// The default strategy.
		/// </summary>
		public static  int DEFAULT_STRATEGY = 0;
		
		
		/// <summary>
		/// This strategy will only allow longer string repetitions.  It is
		/// useful for random data with a small character set.
		/// </summary>
		public static  int FILTERED = 1;
		
		/// <summary>
		/// This strategy will not look for string repetitions at all.  It
		/// only encodes with Huffman trees (which means, that more common
		/// characters get a smaller encoding.
		/// </summary>
		public static  int HUFFMAN_ONLY = 2;
		
		/// <summary>
		/// The compression method.  This is the only method supported so far.
		/// There is no need to use this constant at all.
		/// </summary>
		public static  int DEFLATED = 8;
		
		/*
		* The Deflater can do the following state transitions:
			*
			* (1) -> INIT_STATE   ----> INIT_FINISHING_STATE ---.
			*        /  | (2)      (5)                         |
			*       /   v          (5)                         |
			*   (3)| SETDICT_STATE ---> SETDICT_FINISHING_STATE |(3)
			*       \   | (3)                 |        ,-------'
			*        |  |                     | (3)   /
			*        v  v          (5)        v      v
			* (1) -> BUSY_STATE   ----> FINISHING_STATE
			*                                | (6)
			*                                v
			*                           FINISHED_STATE
			*    \_____________________________________/
			*          | (7)
			*          v
			*        CLOSED_STATE
			*
			* (1) If we should produce a header we start in INIT_STATE, otherwise
			*     we start in BUSY_STATE.
			* (2) A dictionary may be set only when we are in INIT_STATE, then
			*     we change the state as indicated.
			* (3) Whether a dictionary is set or not, on the first call of deflate
			*     we change to BUSY_STATE.
			* (4) -- intentionally left blank -- :)
			* (5) FINISHING_STATE is entered, when flush() is called to indicate that
			*     there is no more INPUT.  There are also states indicating, that
			*     the header wasn't written yet.
			* (6) FINISHED_STATE is entered, when everything has been flushed to the
			*     internal pending output buffer.
			* (7) At any time (7)
			*
			*/
			
		private static  int IS_SETDICT              = 0x01;
		private static  int IS_FLUSHING             = 0x04;
		private static  int IS_FINISHING            = 0x08;
		
		private static  int INIT_STATE              = 0x00;
		private static  int SETDICT_STATE           = 0x01;
//		private static  int INIT_FINISHING_STATE    = 0x08;
//		private static  int SETDICT_FINISHING_STATE = 0x09;
		private static  int BUSY_STATE              = 0x10;
		private static  int FLUSHING_STATE          = 0x14;
		private static  int FINISHING_STATE         = 0x1c;
		private static  int FINISHED_STATE          = 0x1e;
		private static  int CLOSED_STATE            = 0x7f;
		
		/// <summary>
		/// Compression level.
		/// </summary>
		private int level;
		
		/// <summary>
		/// should we include a header.
		/// </summary>
		private bool noHeader;
		
//		/// <summary>
//		/// Compression strategy.
//		/// </summary>
//		private int strategy;
		
		/// <summary>
		/// The current state.
		/// </summary>
		private int state;
		
		/// <summary>
		/// The total bytes of output written.
		/// </summary>
		private int totalOut;
		
		/// <summary>
		/// The pending output.
		/// </summary>
		private DeflaterPending pending;
		
		/// <summary>
		/// The deflater engine.
		/// </summary>
		private DeflaterEngine engine;
		
		/// <summary>
		/// Creates a new deflater with default compression level.
		/// </summary>
		public Deflater() : this(DEFAULT_COMPRESSION, false)
		{
			
		}
		
		/// <summary>
		/// Creates a new deflater with given compression level.
		/// </summary>
		/// <param name="lvl">
		/// the compression level, a value between NO_COMPRESSION
		/// and BEST_COMPRESSION, or DEFAULT_COMPRESSION.
		/// </param>
		/// <exception cref="System.ArgumentOutOfRangeException">if lvl is out of range.</exception>
		public Deflater(int lvl) : this(lvl, false)
		{
			
		}
		
		/// <summary>
		/// Creates a new deflater with given compression level.
		/// </summary>
		/// <param name="lvl">
		/// the compression level, a value between NO_COMPRESSION
		/// and BEST_COMPRESSION.
		/// </param>
		/// <param name="nowrap">
		/// true, if we should suppress the deflate header at the
		/// beginning and the adler checksum at the end of the output.  This is
		/// useful for the GZIP format.
		/// </param>
		/// <exception cref="System.ArgumentOutOfRangeException">if lvl is out of range.</exception>
		public Deflater(int lvl, bool nowrap)
		{
			if (lvl == DEFAULT_COMPRESSION) {
				lvl = 6;
			} else if (lvl < NO_COMPRESSION || lvl > BEST_COMPRESSION) {
				throw new ArgumentOutOfRangeException("lvl");
			}
			
			pending = new DeflaterPending();
			engine = new DeflaterEngine(pending);
			this.noHeader = nowrap;
			SetStrategy(DEFAULT_STRATEGY);
			SetLevel(lvl);
			Reset();
		}
		
		
		/// <summary>
		/// Resets the deflater.  The deflater acts afterwards as if it was
		/// just created with the same compression level and strategy as it
		/// had before.
		/// </summary>
		public void Reset()
		{
			state = (noHeader ? BUSY_STATE : INIT_STATE);
			totalOut = 0;
			pending.Reset();
			engine.Reset();
		}
		
		/// <summary>
		/// Gets the current adler checksum of the data that was processed so far.
		/// </summary>
		public int Adler {
			get {
				return engine.Adler;
			}
		}
		
		/// <summary>
		/// Gets the number of input bytes processed so far.
		/// </summary>
		public int TotalIn {
			get {
				return engine.TotalIn;
			}
		}
		
		/// <summary>
		/// Gets the number of output bytes so far.
		/// </summary>
		public int TotalOut {
			get {
				return totalOut;
			}
		}
		
		/// <summary>
		/// Flushes the current input block.  Further calls to deflate() will
		/// produce enough output to inflate everything in the current input
		/// block.  This is not part of Sun's JDK so I have made it package
		/// private.  It is used by DeflaterOutputStream to implement
		/// flush().
		/// </summary>
		public void Flush() 
		{
			state |= IS_FLUSHING;
		}
		
		/// <summary>
		/// Finishes the deflater with the current input block.  It is an error
		/// to give more input after this method was called.  This method must
		/// be called to force all bytes to be flushed.
		/// </summary>
		public void Finish() 
		{
			state |= IS_FLUSHING | IS_FINISHING;
		}
		
		/// <summary>
		/// Returns true if the stream was finished and no more output bytes
		/// are available.
		/// </summary>
		public bool IsFinished {
			get {
				return state == FINISHED_STATE && pending.IsFlushed;
			}
		}
		
		/// <summary>
		/// Returns true, if the input buffer is empty.
		/// You should then call setInput(). 
		/// NOTE: This method can also return true when the stream
		/// was finished.
		/// </summary>
		public bool IsNeedingInput {
			get {
				return engine.NeedsInput();
			}
		}
		
		/// <summary>
		/// Sets the data which should be compressed next.  This should be only
		/// called when needsInput indicates that more input is needed.
		/// If you call setInput when needsInput() returns false, the
		/// previous input that is still pending will be thrown away.
		/// The given byte array should not be changed, before needsInput() returns
		/// true again.
		/// This call is equivalent to <code>setInput(input, 0, input.length)</code>.
		/// </summary>
		/// <param name="input">
		/// the buffer containing the input data.
		/// </param>
		/// <exception cref="System.InvalidOperationException">
		/// if the buffer was finished() or ended().
		/// </exception>
		public void SetInput(byte[] input)
		{
			SetInput(input, 0, input.Length);
		}
		
		/// <summary>
		/// Sets the data which should be compressed next.  This should be
		/// only called when needsInput indicates that more input is needed.
		/// The given byte array should not be changed, before needsInput() returns
		/// true again.
		/// </summary>
		/// <param name="input">
		/// the buffer containing the input data.
		/// </param>
		/// <param name="off">
		/// the start of the data.
		/// </param>
		/// <param name="len">
		/// the length of the data.
		/// </param>
		/// <exception cref="System.InvalidOperationException">
		/// if the buffer was finished() or ended() or if previous input is still pending.
		/// </exception>
		public void SetInput(byte[] input, int off, int len)
		{
			if ((state & IS_FINISHING) != 0) {
				throw new InvalidOperationException("finish()/end() already called");
			}
			engine.SetInput(input, off, len);
		}
		
		/// <summary>
		/// Sets the compression level.  There is no guarantee of the exact
		/// position of the change, but if you call this when needsInput is
		/// true the change of compression level will occur somewhere near
		/// before the end of the so far given input.
		/// </summary>
		/// <param name="lvl">
		/// the new compression level.
		/// </param>
		public void SetLevel(int lvl)
		{
			if (lvl == DEFAULT_COMPRESSION) {
				lvl = 6;
			} else if (lvl < NO_COMPRESSION || lvl > BEST_COMPRESSION) {
				throw new ArgumentOutOfRangeException("lvl");
			}
			
			
			if (level != lvl) {
				level = lvl;
				engine.SetLevel(lvl);
			}
		}
		
		/// <summary>
		/// Sets the compression strategy. Strategy is one of
		/// DEFAULT_STRATEGY, HUFFMAN_ONLY and FILTERED.  For the exact
		/// position where the strategy is changed, the same as for
		/// setLevel() applies.
		/// </summary>
		/// <param name="stgy">
		/// the new compression strategy.
		/// </param>
		public void SetStrategy(int stgy)
		{
			if (stgy != DEFAULT_STRATEGY && stgy != FILTERED && stgy != HUFFMAN_ONLY) {
			    throw new Exception();
			}
			engine.Strategy = stgy;
		}
		
		/// <summary>
		/// Deflates the current input block to the given array.  It returns
		/// the number of bytes compressed, or 0 if either
		/// needsInput() or finished() returns true or length is zero.
		/// </summary>
		/// <param name="output">
		/// the buffer where to write the compressed data.
		/// </param>
		public int Deflate(byte[] output)
		{
			return Deflate(output, 0, output.Length);
		}
		
		/// <summary>
		/// Deflates the current input block to the given array.  It returns
		/// the number of bytes compressed, or 0 if either
		/// needsInput() or finished() returns true or length is zero.
		/// </summary>
		/// <param name="output">
		/// the buffer where to write the compressed data.
		/// </param>
		/// <param name="offset">
		/// the offset into the output array.
		/// </param>
		/// <param name="length">
		/// the maximum number of bytes that may be written.
		/// </param>
		/// <exception cref="System.InvalidOperationException">
		/// if end() was called.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// if offset and/or length don't match the array length.
		/// </exception>
		public int Deflate(byte[] output, int offset, int length)
		{
			int origLength = length;
			
			if (state == CLOSED_STATE) {
				throw new InvalidOperationException("Deflater closed");
			}
			
			if (state < BUSY_STATE) {
				/* output header */
				int header = (DEFLATED +
				              ((DeflaterConstants.MAX_WBITS - 8) << 4)) << 8;
				int level_flags = (level - 1) >> 1;
				if (level_flags < 0 || level_flags > 3) {
					level_flags = 3;
				}
				header |= level_flags << 6;
				if ((state & IS_SETDICT) != 0) {
					/* Dictionary was set */
					header |= DeflaterConstants.PRESET_DICT;
				}
				header += 31 - (header % 31);
				
				
				pending.WriteShortMSB(header);
				if ((state & IS_SETDICT) != 0) {
					int chksum = engine.Adler;
					engine.ResetAdler();
					pending.WriteShortMSB(chksum >> 16);
					pending.WriteShortMSB(chksum & 0xffff);
				}
				
				state = BUSY_STATE | (state & (IS_FLUSHING | IS_FINISHING));
			}
			
			for (;;) {
				int count = pending.Flush(output, offset, length);
				offset   += count;
				totalOut += count;
				length   -= count;
				
				if (length == 0 || state == FINISHED_STATE) {
					break;
				}
				
				if (!engine.Deflate((state & IS_FLUSHING) != 0, (state & IS_FINISHING) != 0)) {
					if (state == BUSY_STATE) {
						/* We need more input now */
						return origLength - length;
					} else if (state == FLUSHING_STATE) {
						if (level != NO_COMPRESSION) {
							/* We have to supply some lookahead.  8 bit lookahead
							 * are needed by the zlib inflater, and we must fill
							 * the next byte, so that all bits are flushed.
							 */
							int neededbits = 8 + ((-pending.BitCount) & 7);
							while (neededbits > 0) {
								/* write a static tree block consisting solely of
								 * an EOF:
								 */
								pending.WriteBits(2, 10);
								neededbits -= 10;
							}
						}
						state = BUSY_STATE;
					} else if (state == FINISHING_STATE) {
						pending.AlignToByte();
						/* We have completed the stream */
						if (!noHeader) {
							int adler = engine.Adler;
							pending.WriteShortMSB(adler >> 16);
							pending.WriteShortMSB(adler & 0xffff);
						}
						state = FINISHED_STATE;
					}
				}
			}
			return origLength - length;
		}
		
		/// <summary>
		/// Sets the dictionary which should be used in the deflate process.
		/// This call is equivalent to <code>setDictionary(dict, 0, dict.Length)</code>.
		/// </summary>
		/// <param name="dict">
		/// the dictionary.
		/// </param>
		/// <exception cref="System.InvalidOperationException">
		/// if setInput () or deflate () were already called or another dictionary was already set.
		/// </exception>
		public void SetDictionary(byte[] dict)
		{
			SetDictionary(dict, 0, dict.Length);
		}
		
		/// <summary>
		/// Sets the dictionary which should be used in the deflate process.
		/// The dictionary should be a byte array containing strings that are
		/// likely to occur in the data which should be compressed.  The
		/// dictionary is not stored in the compressed output, only a
		/// checksum.  To decompress the output you need to supply the same
		/// dictionary again.
		/// </summary>
		/// <param name="dict">
		/// the dictionary.
		/// </param>
		/// <param name="offset">
		/// an offset into the dictionary.
		/// </param>
		/// <param name="length">
		/// the length of the dictionary.
		/// </param>
		/// <exception cref="System.InvalidOperationException">
		/// if setInput () or deflate () were already called or another dictionary was already set.
		/// </exception>
		public void SetDictionary(byte[] dict, int offset, int length)
		{
			if (state != INIT_STATE) {
				throw new InvalidOperationException();
			}
			
			state = SETDICT_STATE;
			engine.SetDictionary(dict, offset, length);
		}
	}
}

namespace NZlib.Compression {
	
	/// <summary>
	/// This class contains constants used for the deflater.
	/// </summary>
	public class DeflaterConstants 
	{
		public const bool DEBUGGING = false;
		
		public const int STORED_BLOCK = 0;
		public const int STATIC_TREES = 1;
		public const int DYN_TREES    = 2;
		public const int PRESET_DICT  = 0x20;
		
		public const int DEFAULT_MEM_LEVEL = 8;
		
		public const int MAX_MATCH = 258;
		public const int MIN_MATCH = 3;
		
		public const int MAX_WBITS = 15;
		public const int WSIZE = 1 << MAX_WBITS;
		public const int WMASK = WSIZE - 1;
		
		public const int HASH_BITS = DEFAULT_MEM_LEVEL + 7;
		public const int HASH_SIZE = 1 << HASH_BITS;
		public const int HASH_MASK = HASH_SIZE - 1;
		public const int HASH_SHIFT = (HASH_BITS + MIN_MATCH - 1) / MIN_MATCH;
		
		public const int MIN_LOOKAHEAD = MAX_MATCH + MIN_MATCH + 1;
		public const int MAX_DIST = WSIZE - MIN_LOOKAHEAD;
		
		public const int PENDING_BUF_SIZE = 1 << (DEFAULT_MEM_LEVEL + 8);
		public static int MAX_BLOCK_SIZE = Math.Min(65535, PENDING_BUF_SIZE-5);
		
		public const int DEFLATE_STORED = 0;
		public const int DEFLATE_FAST   = 1;
		public const int DEFLATE_SLOW   = 2;
		
		public static int[] GOOD_LENGTH = { 0, 4, 4, 4, 4, 8,  8,  8,  32,  32 };
		public static int[] MAX_LAZY    = { 0, 4, 5, 6, 4,16, 16, 32, 128, 258 };
		public static int[] NICE_LENGTH = { 0, 8,16,32,16,32,128,128, 258, 258 };
		public static int[] MAX_CHAIN   = { 0, 4, 8,32,16,32,128,256,1024,4096 };
		public static int[] COMPR_FUNC  = { 0, 1, 1, 1, 1, 2,  2,  2,   2,   2 };
	}
}

namespace NZlib.Compression {
	
	public class DeflaterEngine : DeflaterConstants 
	{
		private  static int TOO_FAR = 4096;
		
		private int ins_h;
//		private byte[] buffer;
		private short[] head;
		private short[] prev;
		
		private int matchStart, matchLen;
		private bool prevAvailable;
		private int blockStart;
		private int strstart, lookahead;
		private byte[] window;
		
		private int strategy, max_chain, max_lazy, niceLength, goodLength;
		
		/// <summary>
		/// The current compression function.
		/// </summary>
		private int comprFunc;
		
		/// <summary>
		/// The input data for compression.
		/// </summary>
		private byte[] inputBuf;
		
		/// <summary>
		/// The total bytes of input read.
		/// </summary>
		private int totalIn;
		
		/// <summary>
		/// The offset into inputBuf, where input data starts.
		/// </summary>
		private int inputOff;
		
		/// <summary>
		/// The end offset of the input data.
		/// </summary>
		private int inputEnd;
		
		private DeflaterPending pending;
		private DeflaterHuffman huffman;
		
		/// <summary>
		/// The adler checksum
		/// </summary>
		private Adler32 adler;
		
		public DeflaterEngine(DeflaterPending pending) 
		{
			this.pending = pending;
			huffman = new DeflaterHuffman(pending);
			adler = new Adler32();
			
			window = new byte[2*WSIZE];
			head   = new short[HASH_SIZE];
			prev   = new short[WSIZE];
			
			/* We start at index 1, to avoid a implementation deficiency, that
			* we cannot build a repeat pattern at index 0.
			*/
			blockStart = strstart = 1;
		}
		
		public void Reset()
		{
			huffman.Reset();
			adler.Reset();
			blockStart = strstart = 1;
			lookahead = 0;
			totalIn   = 0;
			prevAvailable = false;
			matchLen = MIN_MATCH - 1;
			
			for (int i = 0; i < HASH_SIZE; i++) {
				head[i] = 0;
			}
			
			for (int i = 0; i < WSIZE; i++) {
				prev[i] = 0;
			}
		}
		
		public void ResetAdler()
		{
			adler.Reset();
		}
		
		public int Adler {
			get {
				return (int)adler.Value;
			}
		}
		
		public int TotalIn {
			get {
				return totalIn;
			}
		}
		
		public int Strategy {
			get {
				return strategy;
			}
			set {
				strategy = value;
			}
		}
		
		public void SetLevel(int lvl)
		{
			goodLength = DeflaterConstants.GOOD_LENGTH[lvl];
			max_lazy   = DeflaterConstants.MAX_LAZY[lvl];
			niceLength = DeflaterConstants.NICE_LENGTH[lvl];
			max_chain  = DeflaterConstants.MAX_CHAIN[lvl];
			
			if (DeflaterConstants.COMPR_FUNC[lvl] != comprFunc) {
//				if (DeflaterConstants.DEBUGGING) {
//					Console.WriteLine("Change from "+comprFunc +" to "
//					                  + DeflaterConstants.COMPR_FUNC[lvl]);
//				}
				switch (comprFunc) {
					case DEFLATE_STORED:
						if (strstart > blockStart) {
							huffman.FlushStoredBlock(window, blockStart,
							                         strstart - blockStart, false);
							blockStart = strstart;
						}
						UpdateHash();
						break;
					case DEFLATE_FAST:
						if (strstart > blockStart) {
							huffman.FlushBlock(window, blockStart, strstart - blockStart,
							                   false);
							blockStart = strstart;
						}
						break;
					case DEFLATE_SLOW:
						if (prevAvailable) {
							huffman.TallyLit(window[strstart-1] & 0xff);
						}
						if (strstart > blockStart) {
							huffman.FlushBlock(window, blockStart, strstart - blockStart,
							                   false);
							blockStart = strstart;
						}
						prevAvailable = false;
						matchLen = MIN_MATCH - 1;
						break;
				}
				comprFunc = COMPR_FUNC[lvl];
			}
		}
		
		private void UpdateHash() 
		{
//			if (DEBUGGING) {
//				Console.WriteLine("updateHash: "+strstart);
//			}
			ins_h = (window[strstart] << HASH_SHIFT) ^ window[strstart + 1];
		}
		
		private int InsertString() 
		{
			short match;
			int hash = ((ins_h << HASH_SHIFT) ^ window[strstart + (MIN_MATCH -1)]) & HASH_MASK;
			
//			if (DEBUGGING) {
//				if (hash != (((window[strstart] << (2*HASH_SHIFT)) ^ 
//				              (window[strstart + 1] << HASH_SHIFT) ^ 
//				              (window[strstart + 2])) & HASH_MASK)) {
//						throw new Exception("hash inconsistent: "+hash+"/"
//						                    +window[strstart]+","
//						                    +window[strstart+1]+","
//						                    +window[strstart+2]+","+HASH_SHIFT);
//					}
//			}
			
			prev[strstart & WMASK] = match = head[hash];
			head[hash] = (short)strstart;
			ins_h = hash;
			return match & 0xffff;
		}
		
		public void FillWindow()
		{
			while (lookahead < DeflaterConstants.MIN_LOOKAHEAD && inputOff < inputEnd) {
				int more = 2*WSIZE - lookahead - strstart;
				
				/* If the window is almost full and there is insufficient lookahead,
				* move the upper half to the lower one to make room in the upper half.
				*/
				if (strstart >= WSIZE + MAX_DIST) {
					System.Array.Copy(window, WSIZE, window, 0, WSIZE);
					matchStart -= WSIZE;
					strstart -= WSIZE;
					blockStart -= WSIZE;
					
					/* Slide the hash table (could be avoided with 32 bit values
					 * at the expense of memory usage).
					 */
					 for (int i = 0; i < HASH_SIZE; i++) {
					 	int m = head[i];
					 	head[i] = m >= WSIZE ? (short) (m - WSIZE) : (short)0;
					 }
					 more += WSIZE;
				}
				
				if (more > inputEnd - inputOff) {
					more = inputEnd - inputOff;
				}
				
				System.Array.Copy(inputBuf, inputOff, window, strstart + lookahead, more);
				adler.Update(inputBuf, inputOff, more);
				inputOff  += more;
				totalIn   += more;
				lookahead += more;
				
				if (lookahead >= MIN_MATCH) {
					UpdateHash();
				}
			}
		}
		
		private bool FindLongestMatch(int curMatch) 
		{
			int chainLength = this.max_chain;
			int niceLength  = this.niceLength;
			short[] prev    = this.prev;
			int scan        = this.strstart;
			int match;
			int best_end = this.strstart + matchLen;
			int best_len = Math.Max(matchLen, MIN_MATCH - 1);
			
			int limit = Math.Max(strstart - MAX_DIST, 0);
			
			int strend = strstart + MAX_MATCH - 1;
			byte scan_end1 = window[best_end - 1];
			byte scan_end  = window[best_end];
			
			/* Do not waste too much time if we already have a good match: */
			if (best_len >= this.goodLength) {
				chainLength >>= 2;
			}
			
			/* Do not look for matches beyond the end of the input. This is necessary
			* to make deflate deterministic.
			*/
			if (niceLength > lookahead) {
				niceLength = lookahead;
			}
			
			if (DeflaterConstants.DEBUGGING && strstart > 2*WSIZE - MIN_LOOKAHEAD) {
			    throw new InvalidOperationException("need lookahead");
			}
			
			do {
				if (DeflaterConstants.DEBUGGING && curMatch >= strstart) {
					throw new InvalidOperationException("future match");
				}
				if (window[curMatch + best_len] != scan_end      || 
				    window[curMatch + best_len - 1] != scan_end1 || 
				    window[curMatch] != window[scan]             || 
				    window[curMatch+1] != window[scan + 1]) {
				    continue;
				}
				
				match = curMatch + 2;
				scan += 2;
				
				/* We check for insufficient lookahead only every 8th comparison;
				* the 256th check will be made at strstart+258.
				*/
				while (window[++scan] == window[++match] && 
				       window[++scan] == window[++match] && 
				       window[++scan] == window[++match] && 
				       window[++scan] == window[++match] && 
				       window[++scan] == window[++match] && 
				       window[++scan] == window[++match] && 
				       window[++scan] == window[++match] && 
				       window[++scan] == window[++match] && scan < strend) ;
				
				if (scan > best_end) {
					//  	if (DeflaterConstants.DEBUGGING && ins_h == 0)
					//  	  System.err.println("Found match: "+curMatch+"-"+(scan-strstart));
					matchStart = curMatch;
					best_end = scan;
					best_len = scan - strstart;
					if (best_len >= niceLength) {
						break;
					}
					
					scan_end1  = window[best_end-1];
					scan_end   = window[best_end];
				}
				scan = strstart;
			} while ((curMatch = (prev[curMatch & WMASK] & 0xffff)) > limit && --chainLength != 0);
			
			matchLen = Math.Min(best_len, lookahead);
			return matchLen >= MIN_MATCH;
		}
		
		public void SetDictionary(byte[] buffer, int offset, int length) 
		{
			if (DeflaterConstants.DEBUGGING && strstart != 1) {
				throw new InvalidOperationException("strstart not 1");
			}
			adler.Update(buffer, offset, length);
			if (length < MIN_MATCH) {
				return;
			}
			if (length > MAX_DIST) {
				offset += length - MAX_DIST;
				length = MAX_DIST;
			}
			
			System.Array.Copy(buffer, offset, window, strstart, length);
			
			UpdateHash();
			--length;
			while (--length > 0) {
				InsertString();
				strstart++;
			}
			strstart += 2;
			blockStart = strstart;
		}
		
		private bool DeflateStored(bool flush, bool finish)
		{
			if (!flush && lookahead == 0) {
				return false;
			}
			
			strstart += lookahead;
			lookahead = 0;
			
			int storedLen = strstart - blockStart;
			
			if ((storedLen >= DeflaterConstants.MAX_BLOCK_SIZE) || /* Block is full */
				(blockStart < WSIZE && storedLen >= MAX_DIST) ||   /* Block may move out of window */
				flush) {
				bool lastBlock = finish;
				if (storedLen > DeflaterConstants.MAX_BLOCK_SIZE) {
					storedLen = DeflaterConstants.MAX_BLOCK_SIZE;
					lastBlock = false;
				}
				
//				if (DeflaterConstants.DEBUGGING) {
//					Console.WriteLine("storedBlock["+storedLen+","+lastBlock+"]");
//				}
					
				huffman.FlushStoredBlock(window, blockStart, storedLen, lastBlock);
				blockStart += storedLen;
				return !lastBlock;
			}
			return true;
		}
		
		private bool DeflateFast(bool flush, bool finish)
		{
			if (lookahead < MIN_LOOKAHEAD && !flush) {
				return false;
			}
			
			while (lookahead >= MIN_LOOKAHEAD || flush) {
				if (lookahead == 0) {
					/* We are flushing everything */
					huffman.FlushBlock(window, blockStart, strstart - blockStart, finish);
					blockStart = strstart;
					return false;
				}
				
				int hashHead;
				if (lookahead >= MIN_MATCH && 
				    (hashHead = InsertString()) != 0 && 
				    strategy != Deflater.HUFFMAN_ONLY && 
				    strstart - hashHead <= MAX_DIST && 
				    FindLongestMatch(hashHead)) {
					/* longestMatch sets matchStart and matchLen */
//					if (DeflaterConstants.DEBUGGING) {
//						for (int i = 0 ; i < matchLen; i++) {
//							if (window[strstart+i] != window[matchStart + i]) {
//								throw new Exception();
//							}
//						}
//					}
					
					huffman.TallyDist(strstart - matchStart, matchLen);
					
					lookahead -= matchLen;
					if (matchLen <= max_lazy && lookahead >= MIN_MATCH) {
						while (--matchLen > 0) {
							++strstart;
							InsertString();
						}
						++strstart;
					} else {
						strstart += matchLen;
						if (lookahead >= MIN_MATCH - 1) {
							UpdateHash();
						}
					}
					matchLen = MIN_MATCH - 1;
					continue;
				} else {
					/* No match found */
					huffman.TallyLit(window[strstart] & 0xff);
					++strstart;
					--lookahead;
				}
				
				if (huffman.IsFull()) {
					bool lastBlock = finish && lookahead == 0;
					huffman.FlushBlock(window, blockStart, strstart - blockStart,
					                   lastBlock);
					blockStart = strstart;
					return !lastBlock;
				}
			}
			return true;
		}
		
		private bool DeflateSlow(bool flush, bool finish)
		{
			if (lookahead < MIN_LOOKAHEAD && !flush) {
				return false;
			}
			
			while (lookahead >= MIN_LOOKAHEAD || flush) {
				if (lookahead == 0) {
					if (prevAvailable) {
						huffman.TallyLit(window[strstart-1] & 0xff);
					}
					prevAvailable = false;
					
					/* We are flushing everything */
					if (DeflaterConstants.DEBUGGING && !flush) {
						throw new Exception("Not flushing, but no lookahead");
					}
					huffman.FlushBlock(window, blockStart, strstart - blockStart,
					                   finish);
					blockStart = strstart;
					return false;
				}
				
				int prevMatch = matchStart;
				int prevLen = matchLen;
				if (lookahead >= MIN_MATCH) {
					int hashHead = InsertString();
					if (strategy != Deflater.HUFFMAN_ONLY && hashHead != 0 && strstart - hashHead <= MAX_DIST && FindLongestMatch(hashHead))
						{
							/* longestMatch sets matchStart and matchLen */
							
							/* Discard match if too small and too far away */
							if (matchLen <= 5 && (strategy == Deflater.FILTERED || (matchLen == MIN_MATCH && strstart - matchStart > TOO_FAR))) {
								matchLen = MIN_MATCH - 1;
							}
						}
				}
				
				/* previous match was better */
				if (prevLen >= MIN_MATCH && matchLen <= prevLen) {
//					if (DeflaterConstants.DEBUGGING) {
//						for (int i = 0 ; i < matchLen; i++) {
//							if (window[strstart-1+i] != window[prevMatch + i])
//								throw new Exception();
//						}
//					}
					huffman.TallyDist(strstart - 1 - prevMatch, prevLen);
					prevLen -= 2;
					do {
						strstart++;
						lookahead--;
						if (lookahead >= MIN_MATCH) {
							InsertString();
						}
					} while (--prevLen > 0);
					strstart ++;
					lookahead--;
					prevAvailable = false;
					matchLen = MIN_MATCH - 1;
				} else {
					if (prevAvailable) {
						huffman.TallyLit(window[strstart-1] & 0xff);
					}
					prevAvailable = true;
					strstart++;
					lookahead--;
				}
				
				if (huffman.IsFull()) {
					int len = strstart - blockStart;
					if (prevAvailable) {
						len--;
					}
					bool lastBlock = (finish && lookahead == 0 && !prevAvailable);
					huffman.FlushBlock(window, blockStart, len, lastBlock);
					blockStart += len;
					return !lastBlock;
				}
			}
			return true;
		}
		
		public bool Deflate(bool flush, bool finish)
		{
			bool progress;
			do {
				FillWindow();
				bool canFlush = flush && inputOff == inputEnd;
//				if (DeflaterConstants.DEBUGGING) {
//					Console.WriteLine("window: ["+blockStart+","+strstart+","
//					                  +lookahead+"], "+comprFunc+","+canFlush);
//				}
				switch (comprFunc) {
					case DEFLATE_STORED:
						progress = DeflateStored(canFlush, finish);
					break;
					case DEFLATE_FAST:
						progress = DeflateFast(canFlush, finish);
					break;
					case DEFLATE_SLOW:
						progress = DeflateSlow(canFlush, finish);
					break;
					default:
						throw new InvalidOperationException("unknown comprFunc");
				}
			} while (pending.IsFlushed && progress); /* repeat while we have no pending output and progress was made */
			return progress;
		}
		
		public void SetInput(byte[] buf, int off, int len)
		{
			if (inputOff < inputEnd) {
				throw new InvalidOperationException("Old input was not completely processed");
			}
			
			int end = off + len;
			
			/* We want to throw an ArrayIndexOutOfBoundsException early.  The
			* check is very tricky: it also handles integer wrap around.
			*/
			if (0 > off || off > end || end > buf.Length) {
				throw new ArgumentOutOfRangeException();
			}
			
			inputBuf = buf;
			inputOff = off;
			inputEnd = end;
		}
		
		public bool NeedsInput()
		{
			return inputEnd == inputOff;
		}
	}
}

namespace NZlib.Compression {
	
	/// <summary>
	/// This is the DeflaterHuffman class.
	/// 
	/// This class is <i>not</i> thread safe.  This is inherent in the API, due
	/// to the split of deflate and setInput.
	/// 
	/// author of the original java version : Jochen Hoenicke
	/// </summary>
	public class DeflaterHuffman
	{
		private static  int BUFSIZE = 1 << (DeflaterConstants.DEFAULT_MEM_LEVEL + 6);
		private static  int LITERAL_NUM = 286;
		private static  int DIST_NUM = 30;
		private static  int BITLEN_NUM = 19;
		private static  int REP_3_6    = 16;
		private static  int REP_3_10   = 17;
		private static  int REP_11_138 = 18;
		private static  int EOF_SYMBOL = 256;
		private static  int[] BL_ORDER = { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };
		
		private static byte[] bit4Reverse = {
			0,
			8,
			4,
			12,
			2,
			10,
			6,
			14,
			1,
			9,
			5,
			13,
			3,
			11,
			7,
			15
		};
		
		
		public class Tree 
		{
			public short[] freqs;
			public short[] codes;
			public byte[]  length;
			public int[]   bl_counts;
			public int     minNumCodes, numCodes;
			public int     maxLength;
			DeflaterHuffman dh;
			
			public Tree(DeflaterHuffman dh, int elems, int minCodes, int maxLength) 
			{
				this.dh =  dh;
				this.minNumCodes = minCodes;
				this.maxLength  = maxLength;
				freqs  = new short[elems];
				bl_counts = new int[maxLength];
			}
			
			public void Reset() 
			{
				for (int i = 0; i < freqs.Length; i++) {
					freqs[i] = 0;
				}
				codes = null;
				length = null;
			}
			
			public void WriteSymbol(int code)
			{
//				if (DeflaterConstants.DEBUGGING) {
//					freqs[code]--;
//					//  	  Console.Write("writeSymbol("+freqs.length+","+code+"): ");
//				}
				dh.pending.WriteBits(codes[code] & 0xffff, length[code]);
			}
			
			public void CheckEmpty()
			{
				bool empty = true;
				for (int i = 0; i < freqs.Length; i++) {
					if (freqs[i] != 0) {
						Console.WriteLine("freqs["+i+"] == "+freqs[i]);
						empty = false;
					}
				}
				if (!empty) {
					throw new Exception();
				}
				Console.WriteLine("checkEmpty suceeded!");
			}
			
			public void SetStaticCodes(short[] stCodes, byte[] stLength)
			{
				codes = stCodes;
				length = stLength;
			}
			
			public void BuildCodes() 
			{
				int numSymbols = freqs.Length;
				int[] nextCode = new int[maxLength];
				int code = 0;
				codes = new short[freqs.Length];
				
//				if (DeflaterConstants.DEBUGGING) {
//					Console.WriteLine("buildCodes: "+freqs.Length);
//				}
				
				for (int bits = 0; bits < maxLength; bits++) {
					nextCode[bits] = code;
					code += bl_counts[bits] << (15 - bits);
//					if (DeflaterConstants.DEBUGGING) {
//						Console.WriteLine("bits: "+(bits+1)+" count: "+bl_counts[bits]
//						                  +" nextCode: "+code); // HACK : Integer.toHexString(
//					}
				}
				if (DeflaterConstants.DEBUGGING && code != 65536) {
					throw new Exception("Inconsistent bl_counts!");
				}
				
				for (int i=0; i < numCodes; i++) {
					int bits = length[i];
					if (bits > 0) {
//						if (DeflaterConstants.DEBUGGING) {
//								Console.WriteLine("codes["+i+"] = rev(" + nextCode[bits-1]+")," // HACK : Integer.toHexString(
//								                  +bits);
//						}
						codes[i] = BitReverse(nextCode[bits-1]);
						nextCode[bits-1] += 1 << (16 - bits);
					}
				}
			}
			
			private void BuildLength(int[] childs)
			{
				this.length = new byte [freqs.Length];
				int numNodes = childs.Length / 2;
				int numLeafs = (numNodes + 1) / 2;
				int overflow = 0;
				
				for (int i = 0; i < maxLength; i++) {
					bl_counts[i] = 0;
				}
				
				/* First calculate optimal bit lengths */
				int[] lengths = new int[numNodes];
				lengths[numNodes-1] = 0;
				
				for (int i = numNodes - 1; i >= 0; i--) {
					if (childs[2*i+1] != -1) {
						int bitLength = lengths[i] + 1;
						if (bitLength > maxLength) {
							bitLength = maxLength;
							overflow++;
						}
						lengths[childs[2*i]] = lengths[childs[2*i+1]] = bitLength;
					} else {
						/* A leaf node */
						int bitLength = lengths[i];
						bl_counts[bitLength - 1]++;
						this.length[childs[2*i]] = (byte) lengths[i];
					}
				}
				
//				if (DeflaterConstants.DEBUGGING) {
//					Console.WriteLine("Tree "+freqs.Length+" lengths:");
//					for (int i=0; i < numLeafs; i++) {
//						Console.WriteLine("Node "+childs[2*i]+" freq: "+freqs[childs[2*i]]
//						                  + " len: "+length[childs[2*i]]);
//					}
//				}
				
				if (overflow == 0) {
					return;
				}
				
				int incrBitLen = maxLength - 1;
				do {
					/* Find the first bit length which could increase: */
					while (bl_counts[--incrBitLen] == 0)
						;
					
					/* Move this node one down and remove a corresponding
					* amount of overflow nodes.
					*/
					do {
						bl_counts[incrBitLen]--;
						bl_counts[++incrBitLen]++;
						overflow -= 1 << (maxLength - 1 - incrBitLen);
					} while (overflow > 0 && incrBitLen < maxLength - 1);
				} while (overflow > 0);
				
				/* We may have overshot above.  Move some nodes from maxLength to
				* maxLength-1 in that case.
				*/
				bl_counts[maxLength-1] += overflow;
				bl_counts[maxLength-2] -= overflow;
				
				/* Now recompute all bit lengths, scanning in increasing
				* frequency.  It is simpler to reconstruct all lengths instead of
				* fixing only the wrong ones. This idea is taken from 'ar'
				* written by Haruhiko Okumura.
				*
				* The nodes were inserted with decreasing frequency into the childs
				* array.
				*/
				int nodePtr = 2 * numLeafs;
				for (int bits = maxLength; bits != 0; bits--) {
					int n = bl_counts[bits-1];
					while (n > 0) {
						int childPtr = 2*childs[nodePtr++];
						if (childs[childPtr + 1] == -1) {
							/* We found another leaf */
							length[childs[childPtr]] = (byte) bits;
							n--;
						}
					}
				}
//				if (DeflaterConstants.DEBUGGING) {
//					Console.WriteLine("*** After overflow elimination. ***");
//					for (int i=0; i < numLeafs; i++) {
//						Console.WriteLine("Node "+childs[2*i]+" freq: "+freqs[childs[2*i]]
//						                  + " len: "+length[childs[2*i]]);
//					}
//				}
			}
			
			public void BuildTree()
			{
				int numSymbols = freqs.Length;
				
				/* heap is a priority queue, sorted by frequency, least frequent
				* nodes first.  The heap is a binary tree, with the property, that
				* the parent node is smaller than both child nodes.  This assures
				* that the smallest node is the first parent.
				*
				* The binary tree is encoded in an array:  0 is root node and
				* the nodes 2*n+1, 2*n+2 are the child nodes of node n.
				*/
				int[] heap = new int[numSymbols];
				int heapLen = 0;
				int maxCode = 0;
				for (int n = 0; n < numSymbols; n++) {
					int freq = freqs[n];
					if (freq != 0) {
						/* Insert n into heap */
						int pos = heapLen++;
						int ppos;
						while (pos > 0 && freqs[heap[ppos = (pos - 1) / 2]] > freq) {
							heap[pos] = heap[ppos];
							pos = ppos;
						}
						heap[pos] = n;
						
						maxCode = n;
					}
				}
				
				/* We could encode a single literal with 0 bits but then we
				* don't see the literals.  Therefore we force at least two
				* literals to avoid this case.  We don't care about order in
				* this case, both literals get a 1 bit code.
				*/
				while (heapLen < 2) {
					int node = maxCode < 2 ? ++maxCode : 0;
					heap[heapLen++] = node;
				}
				
				numCodes = Math.Max(maxCode + 1, minNumCodes);
				
				int numLeafs = heapLen;
				int[] childs = new int[4*heapLen - 2];
				int[] values = new int[2*heapLen - 1];
				int numNodes = numLeafs;
				for (int i = 0; i < heapLen; i++) {
					int node = heap[i];
					childs[2*i]   = node;
					childs[2*i+1] = -1;
					values[i] = freqs[node] << 8;
					heap[i] = i;
				}
				
				/* Construct the Huffman tree by repeatedly combining the least two
				* frequent nodes.
				*/
				do {
					int first = heap[0];
					int last  = heap[--heapLen];
					
					/* Propagate the hole to the leafs of the heap */
					int ppos = 0;
					int path = 1;
					while (path < heapLen) {
						if (path + 1 < heapLen && values[heap[path]] > values[heap[path+1]]) {
							path++;
						}
						
						heap[ppos] = heap[path];
						ppos = path;
						path = path * 2 + 1;
					}
					
					/* Now propagate the last element down along path.  Normally
					* it shouldn't go too deep.
					*/
					int lastVal = values[last];
					while ((path = ppos) > 0 && values[heap[ppos = (path - 1)/2]] > lastVal) {
						heap[path] = heap[ppos];
					}
					heap[path] = last;
					
					
					int second = heap[0];
					
					/* Create a new node father of first and second */
					last = numNodes++;
					childs[2*last] = first;
					childs[2*last+1] = second;
					int mindepth = Math.Min(values[first] & 0xff, values[second] & 0xff);
					values[last] = lastVal = values[first] + values[second] - mindepth + 1;
					
					/* Again, propagate the hole to the leafs */
					ppos = 0;
					path = 1;
					while (path < heapLen) {
						if (path + 1 < heapLen && values[heap[path]] > values[heap[path+1]]) {
							path++;
						}
						
						heap[ppos] = heap[path];
						ppos = path;
						path = ppos * 2 + 1;
					}
					
					/* Now propagate the new element down along path */
					while ((path = ppos) > 0 && values[heap[ppos = (path - 1)/2]] > lastVal) {
						heap[path] = heap[ppos];
					}
					heap[path] = last;
				} while (heapLen > 1);
				
				if (heap[0] != childs.Length / 2 - 1) {
					throw new Exception("Weird!");
				}
				BuildLength(childs);
			}
			
			public int GetEncodedLength()
			{
				int len = 0;
				for (int i = 0; i < freqs.Length; i++) {
					len += freqs[i] * length[i];
				}
				return len;
			}
			
			public void CalcBLFreq(Tree blTree) 
			{
				int max_count;               /* max repeat count */
				int min_count;               /* min repeat count */
				int count;                   /* repeat count of the current code */
				int curlen = -1;             /* length of current code */
				
				int i = 0;
				while (i < numCodes) {
					count = 1;
					int nextlen = length[i];
					if (nextlen == 0) {
						max_count = 138;
						min_count = 3;
					} else {
						max_count = 6;
						min_count = 3;
						if (curlen != nextlen) {
							blTree.freqs[nextlen]++;
							count = 0;
						}
					}
					curlen = nextlen;
					i++;
					
					while (i < numCodes && curlen == length[i]) {
						i++;
						if (++count >= max_count) {
							break;
						}
					}
					
					if (count < min_count) {
						blTree.freqs[curlen] += (short)count;
					} else if (curlen != 0) {
						blTree.freqs[REP_3_6]++;
					} else if (count <= 10) {
						blTree.freqs[REP_3_10]++;
					} else {
						blTree.freqs[REP_11_138]++;
					}
				}
			}
			
			public void WriteTree(Tree blTree)
			{
				int max_count;               /* max repeat count */
				int min_count;               /* min repeat count */
				int count;                   /* repeat count of the current code */
				int curlen = -1;             /* length of current code */
				
				int i = 0;
				while (i < numCodes) {
					count = 1;
					int nextlen = length[i];
					if (nextlen == 0) {
						max_count = 138;
						min_count = 3;
					} else {
						max_count = 6;
						min_count = 3;
						if (curlen != nextlen) {
							blTree.WriteSymbol(nextlen);
							count = 0;
						}
					}
					curlen = nextlen;
					i++;
					
					while (i < numCodes && curlen == length[i]) {
						i++;
						if (++count >= max_count) {
							break;
						}
					}
					
					if (count < min_count) {
						while (count-- > 0) {
							blTree.WriteSymbol(curlen);
						}
					}
					else if (curlen != 0) {
						blTree.WriteSymbol(REP_3_6);
						dh.pending.WriteBits(count - 3, 2);
					} else if (count <= 10) {
						blTree.WriteSymbol(REP_3_10);
						dh.pending.WriteBits(count - 3, 3);
					} else {
						blTree.WriteSymbol(REP_11_138);
						dh.pending.WriteBits(count - 11, 7);
					}
				}
			}
		}
		
		public DeflaterPending pending;
		private Tree literalTree, distTree, blTree;
		
		private short[] d_buf;
		private byte[] l_buf;
		private int last_lit;
		private int extra_bits;
		
		private static short[] staticLCodes;
		private static byte[]  staticLLength;
		private static short[] staticDCodes;
		private static byte[]  staticDLength;
		
		/// <summary>
		/// Reverse the bits of a 16 bit value.
		/// </summary>
		public static short BitReverse(int value) 
		{
			return (short) (bit4Reverse[value & 0xF] << 12
			                | bit4Reverse[(value >> 4) & 0xF] << 8
			                | bit4Reverse[(value >> 8) & 0xF] << 4
			                | bit4Reverse[value >> 12]);
		}
		
		
		static DeflaterHuffman() 
		{
			/* See RFC 1951 3.2.6 */
			/* Literal codes */
			staticLCodes = new short[LITERAL_NUM];
			staticLLength = new byte[LITERAL_NUM];
			int i = 0;
			while (i < 144) {
				staticLCodes[i] = BitReverse((0x030 + i) << 8);
				staticLLength[i++] = 8;
			}
			while (i < 256) {
				staticLCodes[i] = BitReverse((0x190 - 144 + i) << 7);
				staticLLength[i++] = 9;
			}
			while (i < 280) {
				staticLCodes[i] = BitReverse((0x000 - 256 + i) << 9);
				staticLLength[i++] = 7;
			}
			while (i < LITERAL_NUM) {
				staticLCodes[i] = BitReverse((0x0c0 - 280 + i)  << 8);
				staticLLength[i++] = 8;
			}
			
			/* Distant codes */
			staticDCodes = new short[DIST_NUM];
			staticDLength = new byte[DIST_NUM];
			for (i = 0; i < DIST_NUM; i++) {
				staticDCodes[i] = BitReverse(i << 11);
				staticDLength[i] = 5;
			}
		}
		
		public DeflaterHuffman(DeflaterPending pending)
		{
			this.pending = pending;
			
			literalTree = new Tree(this, LITERAL_NUM, 257, 15);
			distTree    = new Tree(this, DIST_NUM, 1, 15);
			blTree      = new Tree(this, BITLEN_NUM, 4, 7);
			
			d_buf = new short[BUFSIZE];
			l_buf = new byte [BUFSIZE];
		}
		
		public void Reset() 
		{
			last_lit = 0;
			extra_bits = 0;
			literalTree.Reset();
			distTree.Reset();
			blTree.Reset();
		}
		
		private int L_code(int len) 
		{
			if (len == 255) {
				return 285;
			}
			
			int code = 257;
			while (len >= 8) {
				code += 4;
				len >>= 1;
			}
			return code + len;
		}
		
		private int D_code(int distance) 
		{
			int code = 0;
			while (distance >= 4) {
				code += 2;
				distance >>= 1;
			}
			return code + distance;
		}
		
		public void SendAllTrees(int blTreeCodes)
		{
			blTree.BuildCodes();
			literalTree.BuildCodes();
			distTree.BuildCodes();
			pending.WriteBits(literalTree.numCodes - 257, 5);
			pending.WriteBits(distTree.numCodes - 1, 5);
			pending.WriteBits(blTreeCodes - 4, 4);
			for (int rank = 0; rank < blTreeCodes; rank++) {
				pending.WriteBits(blTree.length[BL_ORDER[rank]], 3);
			}
			literalTree.WriteTree(blTree);
			distTree.WriteTree(blTree);
//			if (DeflaterConstants.DEBUGGING) {
//				blTree.CheckEmpty();
//			}
		}
		
		public void CompressBlock()
		{
			for (int i = 0; i < last_lit; i++) {
				int litlen = l_buf[i] & 0xff;
				int dist = d_buf[i];
				if (dist-- != 0) {
//					if (DeflaterConstants.DEBUGGING) {
//						Console.Write("["+(dist+1)+","+(litlen+3)+"]: ");
//					}
					
					int lc = L_code(litlen);
					literalTree.WriteSymbol(lc);
					
					int bits = (lc - 261) / 4;
					if (bits > 0 && bits <= 5) {
						pending.WriteBits(litlen & ((1 << bits) - 1), bits);
					}
					
					int dc = D_code(dist);
					distTree.WriteSymbol(dc);
					
					bits = dc / 2 - 1;
					if (bits > 0) {
						pending.WriteBits(dist & ((1 << bits) - 1), bits);
					}
				} else {
//					if (DeflaterConstants.DEBUGGING) {
//						if (litlen > 32 && litlen < 127) {
//							Console.Write("("+(char)litlen+"): ");
//						} else {
//							Console.Write("{"+litlen+"}: ");
//						}
//					}
					literalTree.WriteSymbol(litlen);
				}
			}
//			if (DeflaterConstants.DEBUGGING) {
//				Console.Write("EOF: ");
//			}
			literalTree.WriteSymbol(EOF_SYMBOL);
//			if (DeflaterConstants.DEBUGGING) {
//				literalTree.CheckEmpty();
//				distTree.CheckEmpty();
//			}
		}
		
		public void FlushStoredBlock(byte[] stored, int stored_offset, int stored_len, bool lastBlock)
		{
//			if (DeflaterConstants.DEBUGGING) {
//				Console.WriteLine("Flushing stored block "+ stored_len);
//			}
			pending.WriteBits((DeflaterConstants.STORED_BLOCK << 1)
			                  + (lastBlock ? 1 : 0), 3);
			pending.AlignToByte();
			pending.WriteShort(stored_len);
			pending.WriteShort(~stored_len);
			pending.WriteBlock(stored, stored_offset, stored_len);
			Reset();
		}
		
		public void FlushBlock(byte[] stored, int stored_offset, int stored_len, bool lastBlock)
		{
			literalTree.freqs[EOF_SYMBOL]++;
			
			/* Build trees */
			literalTree.BuildTree();
			distTree.BuildTree();
			
			/* Calculate bitlen frequency */
			literalTree.CalcBLFreq(blTree);
			distTree.CalcBLFreq(blTree);
			
			/* Build bitlen tree */
			blTree.BuildTree();
			
			int blTreeCodes = 4;
			for (int i = 18; i > blTreeCodes; i--) {
				if (blTree.length[BL_ORDER[i]] > 0) {
					blTreeCodes = i+1;
				}
			}
			int opt_len = 14 + blTreeCodes * 3 + blTree.GetEncodedLength() + 
			              literalTree.GetEncodedLength() + distTree.GetEncodedLength() + 
			              extra_bits;
			
			int static_len = extra_bits;
			for (int i = 0; i < LITERAL_NUM; i++) {
				static_len += literalTree.freqs[i] * staticLLength[i];
			}
			for (int i = 0; i < DIST_NUM; i++) {
				static_len += distTree.freqs[i] * staticDLength[i];
			}
			if (opt_len >= static_len) {
				/* Force static trees */
				opt_len = static_len;
			}
			
			if (stored_offset >= 0 && stored_len+4 < opt_len >> 3) {
				/* Store Block */
//				if (DeflaterConstants.DEBUGGING) {
//					Console.WriteLine("Storing, since " + stored_len + " < " + opt_len
//					                  + " <= " + static_len);
//				}
				FlushStoredBlock(stored, stored_offset, stored_len, lastBlock);
			} else if (opt_len == static_len) {
				/* Encode with static tree */
				pending.WriteBits((DeflaterConstants.STATIC_TREES << 1) + (lastBlock ? 1 : 0), 3);
				literalTree.SetStaticCodes(staticLCodes, staticLLength);
				distTree.SetStaticCodes(staticDCodes, staticDLength);
				CompressBlock();
				Reset();
			} else {
				/* Encode with dynamic tree */
				pending.WriteBits((DeflaterConstants.DYN_TREES << 1) + (lastBlock ? 1 : 0), 3);
				SendAllTrees(blTreeCodes);
				CompressBlock();
				Reset();
			}
		}
		
		public bool IsFull()
		{
			return last_lit + 16 >= BUFSIZE; // HACK: This was == 'last_lit == BUFSIZE', but errors occured with DeflateFast
		}
		
		public bool TallyLit(int lit)
		{
//			if (DeflaterConstants.DEBUGGING) {
//				if (lit > 32 && lit < 127) {
//					Console.WriteLine("("+(char)lit+")");
//				} else {
//					Console.WriteLine("{"+lit+"}");
//				}
//			}
			d_buf[last_lit] = 0;
			l_buf[last_lit++] = (byte)lit;
			literalTree.freqs[lit]++;
			return IsFull();
		}
		
		public bool TallyDist(int dist, int len)
		{
//			if (DeflaterConstants.DEBUGGING) {
//				Console.WriteLine("["+dist+","+len+"]");
//			}
			
			d_buf[last_lit]   = (short)dist;
			l_buf[last_lit++] = (byte)(len - 3);
			
			int lc = L_code(len - 3);
			literalTree.freqs[lc]++;
			if (lc >= 265 && lc < 285) {
				extra_bits += (lc - 261) / 4;
			}
			
			int dc = D_code(dist - 1);
			distTree.freqs[dc]++;
			if (dc >= 4) {
				extra_bits += dc / 2 - 1;
			}
			return IsFull();
		}
	}
}

namespace NZlib.Compression {
	
	/// <summary>
	/// This class stores the pending output of the Deflater.
	/// 
	/// author of the original java version : Jochen Hoenicke
	/// </summary>
	public class DeflaterPending : PendingBuffer
	{
		public DeflaterPending() : base(DeflaterConstants.PENDING_BUF_SIZE)
		{
		}
	}
}

namespace NZlib.Compression {
	
	/// <summary>
	/// Inflater is used to decompress data that has been compressed according
	/// to the "deflate" standard described in rfc1950.
	///
	/// The usage is as following.  First you have to set some input with
	/// <code>setInput()</code>, then inflate() it.  If inflate doesn't
	/// inflate any bytes there may be three reasons:
	/// <ul>
	/// <li>needsInput() returns true because the input buffer is empty.
	/// You have to provide more input with <code>setInput()</code>.
	/// NOTE: needsInput() also returns true when, the stream is finished.
	/// </li>
	/// <li>needsDictionary() returns true, you have to provide a preset
	///    dictionary with <code>setDictionary()</code>.</li>
	/// <li>finished() returns true, the inflater has finished.</li>
	/// </ul>
	/// Once the first output byte is produced, a dictionary will not be
	/// needed at a later stage.
	///
	/// author of the original java version : John Leuner, Jochen Hoenicke
	/// </summary>
	public class Inflater
	{
		/// <summary>
		/// Copy lengths for literal codes 257..285
		/// </summary>
		private static int[] CPLENS = {
			3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31,
			35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258
		};
		
		/// <summary>
		/// Extra bits for literal codes 257..285
		/// </summary>
		private static int[] CPLEXT = {
			0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2,
			3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0
		};
		
		/// <summary>
		/// Copy offsets for distance codes 0..29
		/// </summary>
		private static int[] CPDIST = {
			1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193,
			257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145,
			8193, 12289, 16385, 24577
		};
		
		/// <summary>
		/// Extra bits for distance codes
		/// </summary>
		private static int[] CPDEXT = {
			0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6,
			7, 7, 8, 8, 9, 9, 10, 10, 11, 11,
			12, 12, 13, 13
		};
		
		/// <summary>
		/// This are the state in which the inflater can be.
		/// </summary>
		private const int DECODE_HEADER           = 0;
		private const int DECODE_DICT             = 1;
		private const int DECODE_BLOCKS           = 2;
		private const int DECODE_STORED_LEN1      = 3;
		private const int DECODE_STORED_LEN2      = 4;
		private const int DECODE_STORED           = 5;
		private const int DECODE_DYN_HEADER       = 6;
		private const int DECODE_HUFFMAN          = 7;
		private const int DECODE_HUFFMAN_LENBITS  = 8;
		private const int DECODE_HUFFMAN_DIST     = 9;
		private const int DECODE_HUFFMAN_DISTBITS = 10;
		private const int DECODE_CHKSUM           = 11;
		private const int FINISHED                = 12;
		
		/// <summary>
		/// This variable contains the current state.
		/// </summary>
		private int mode;
		
		/// <summary>
		/// The adler checksum of the dictionary or of the decompressed
		/// stream, as it is written in the header resp. footer of the
		/// compressed stream. 
		/// Only valid if mode is DECODE_DICT or DECODE_CHKSUM.
		/// </summary>
		private int readAdler;
		
		/// <summary>
		/// The number of bits needed to complete the current state.  This
		/// is valid, if mode is DECODE_DICT, DECODE_CHKSUM,
		/// DECODE_HUFFMAN_LENBITS or DECODE_HUFFMAN_DISTBITS.
		/// </summary>
		private int neededBits;
		private int repLength, repDist;
		private int uncomprLen;
		
		/// <summary>
		/// True, if the last block flag was set in the last block of the
		/// inflated stream.  This means that the stream ends after the
		/// current block.
		/// </summary>
		private bool isLastBlock;
		
		/// <summary>
		/// The total number of inflated bytes.
		/// </summary>
		private int totalOut;
		
		/// <summary>
		/// The total number of bytes set with setInput().  This is not the
		/// value returned by getTotalIn(), since this also includes the
		/// unprocessed input.
		/// </summary>
		private int totalIn;
		
		/// <summary>
		/// This variable stores the nowrap flag that was given to the constructor.
		/// True means, that the inflated stream doesn't contain a header nor the
		/// checksum in the footer.
		/// </summary>
		private bool nowrap;
		
		private StreamManipulator input;
		private OutputWindow outputWindow;
		private InflaterDynHeader dynHeader;
		private InflaterHuffmanTree litlenTree, distTree;
		private Adler32 adler;
		
		/// <summary>
		/// Creates a new inflater.
		/// </summary>
		public Inflater() : this(false)
		{
		}
		
		/// <summary>
		/// Creates a new inflater.
		/// </summary>
		/// <param name="nowrap">
		/// true if no header and checksum field appears in the
		/// stream.  This is used for GZIPed input.  For compatibility with
		/// Sun JDK you should provide one byte of input more than needed in
		/// this case.
		/// </param>
		public Inflater(bool nowrap)
		{
			this.nowrap = nowrap;
			this.adler = new Adler32();
			input = new StreamManipulator();
			outputWindow = new OutputWindow();
			mode = nowrap ? DECODE_BLOCKS : DECODE_HEADER;
		}
		
		/// <summary>
		/// Resets the inflater so that a new stream can be decompressed.  All
		/// pending input and output will be discarded.
		/// </summary>
		public void Reset()
		{
			mode = nowrap ? DECODE_BLOCKS : DECODE_HEADER;
			totalIn = totalOut = 0;
			input.Reset();
			outputWindow.Reset();
			dynHeader = null;
			litlenTree = null;
			distTree = null;
			isLastBlock = false;
			adler.Reset();
		}
		
		/// <summary>
		/// Decodes the deflate header.
		/// </summary>
		/// <returns>
		/// false if more input is needed.
		/// </returns>
		/// <exception cref="System.FormatException">
		/// if header is invalid.
		/// </exception>
		private bool DecodeHeader()
		{
			int header = input.PeekBits(16);
			if (header < 0) {
				return false;
			}
			input.DropBits(16);
			/* The header is written in "wrong" byte order */
			header = ((header << 8) | (header >> 8)) & 0xffff;
			if (header % 31 != 0) {
				throw new FormatException("Header checksum illegal");
			}
			
			if ((header & 0x0f00) != (Deflater.DEFLATED << 8)) {
				throw new FormatException("Compression Method unknown");
			}
			
			/* Maximum size of the backwards window in bits.
			* We currently ignore this, but we could use it to make the
			* inflater window more space efficient. On the other hand the
			* full window (15 bits) is needed most times, anyway.
			int max_wbits = ((header & 0x7000) >> 12) + 8;
			*/
			
			if ((header & 0x0020) == 0) { // Dictionary flag?
				mode = DECODE_BLOCKS;
			} else {
				mode = DECODE_DICT;
				neededBits = 32;
			}
			return true;
		}
		
		/// <summary>
		/// Decodes the dictionary checksum after the deflate header.
		/// </summary>
		/// <returns>
		/// false if more input is needed.
		/// </returns>
		private bool DecodeDict()
		{
			while (neededBits > 0) {
				int dictByte = input.PeekBits(8);
				if (dictByte < 0) {
					return false;
				}
				input.DropBits(8);
				readAdler = (readAdler << 8) | dictByte;
				neededBits -= 8;
			}
			return false;
		}
		
		/// <summary>
		/// Decodes the huffman encoded symbols in the input stream.
		/// </summary>
		/// <returns>
		/// false if more input is needed, true if output window is
		/// full or the current block ends.
		/// </returns>
		/// <exception cref="System.FormatException">
		/// if deflated stream is invalid.
		/// </exception>
		private bool DecodeHuffman()
		{
			int free = outputWindow.GetFreeSpace();
			while (free >= 258) {
				int symbol;
				switch (mode) {
					case DECODE_HUFFMAN:
						/* This is the inner loop so it is optimized a bit */
						while (((symbol = litlenTree.GetSymbol(input)) & ~0xff) == 0) {
							outputWindow.Write(symbol);
							if (--free < 258) {
								return true;
							}
						}
						if (symbol < 257) {
							if (symbol < 0) {
								return false;
							} else {
								/* symbol == 256: end of block */
								distTree = null;
								litlenTree = null;
								mode = DECODE_BLOCKS;
								return true;
							}
						}
						
						try {
							repLength = CPLENS[symbol - 257];
							neededBits = CPLEXT[symbol - 257];
						} catch (Exception) {
							throw new FormatException("Illegal rep length code");
						}
						goto case DECODE_HUFFMAN_LENBITS;/* fall through */
					case DECODE_HUFFMAN_LENBITS:
						if (neededBits > 0) {
							mode = DECODE_HUFFMAN_LENBITS;
							int i = input.PeekBits(neededBits);
							if (i < 0) {
								return false;
							}
							input.DropBits(neededBits);
							repLength += i;
						}
						mode = DECODE_HUFFMAN_DIST;
						goto case DECODE_HUFFMAN_DIST;/* fall through */
					case DECODE_HUFFMAN_DIST:
						symbol = distTree.GetSymbol(input);
						if (symbol < 0) {
							return false;
						}
						try {
							repDist = CPDIST[symbol];
							neededBits = CPDEXT[symbol];
						} catch (Exception) {
							throw new FormatException("Illegal rep dist code");
						}
						
						goto case DECODE_HUFFMAN_DISTBITS;/* fall through */
					case DECODE_HUFFMAN_DISTBITS:
						if (neededBits > 0) {
							mode = DECODE_HUFFMAN_DISTBITS;
							int i = input.PeekBits(neededBits);
							if (i < 0) {
								return false;
							}
							input.DropBits(neededBits);
							repDist += i;
						}
						outputWindow.Repeat(repLength, repDist);
						free -= repLength;
						mode = DECODE_HUFFMAN;
						break;
					default:
						throw new FormatException();
				}
			}
			return true;
		}
		
		/// <summary>
		/// Decodes the adler checksum after the deflate stream.
		/// </summary>
		/// <returns>
		/// false if more input is needed.
		/// </returns>
		/// <exception cref="System.FormatException">
		/// DataFormatException, if checksum doesn't match.
		/// </exception>
		private bool DecodeChksum()
		{
			while (neededBits > 0) {
				int chkByte = input.PeekBits(8);
				if (chkByte < 0) {
					return false;
				}
				input.DropBits(8);
				readAdler = (readAdler << 8) | chkByte;
				neededBits -= 8;
			}
			if ((int) adler.Value != readAdler) {
				throw new FormatException("Adler chksum doesn't match: "
				                          + (int)adler.Value
				                          + " vs. " + readAdler);
			}
			mode = FINISHED;
			return false;
		}
		
		/// <summary>
		/// Decodes the deflated stream.
		/// </summary>
		/// <returns>
		/// false if more input is needed, or if finished.
		/// </returns>
		/// <exception cref="System.FormatException">
		/// DataFormatException, if deflated stream is invalid.
		/// </exception>
		private bool Decode()
		{
			switch (mode) {
				case DECODE_HEADER:
					return DecodeHeader();
				case DECODE_DICT:
					return DecodeDict();
				case DECODE_CHKSUM:
					return DecodeChksum();
				
				case DECODE_BLOCKS:
					if (isLastBlock) {
						if (nowrap) {
							mode = FINISHED;
							return false;
						} else {
							input.SkipToByteBoundary();
							neededBits = 32;
							mode = DECODE_CHKSUM;
							return true;
						}
					}
					
					int type = input.PeekBits(3);
					if (type < 0) {
						return false;
					}
					input.DropBits(3);
					
					if ((type & 1) != 0) {
						isLastBlock = true;
					}
					switch (type >> 1) {
						case DeflaterConstants.STORED_BLOCK:
							input.SkipToByteBoundary();
							mode = DECODE_STORED_LEN1;
							break;
						case DeflaterConstants.STATIC_TREES:
							litlenTree = InflaterHuffmanTree.defLitLenTree;
							distTree = InflaterHuffmanTree.defDistTree;
							mode = DECODE_HUFFMAN;
							break;
						case DeflaterConstants.DYN_TREES:
							dynHeader = new InflaterDynHeader();
							mode = DECODE_DYN_HEADER;
							break;
						default:
							throw new FormatException("Unknown block type "+type);
					}
					return true;
				
				case DECODE_STORED_LEN1: {
					if ((uncomprLen = input.PeekBits(16)) < 0) {
						return false;
					}
					input.DropBits(16);
					mode = DECODE_STORED_LEN2;
				}
				goto case DECODE_STORED_LEN2; /* fall through */
				case DECODE_STORED_LEN2: {
					int nlen = input.PeekBits(16);
					if (nlen < 0) {
						return false;
					}
					input.DropBits(16);
					if (nlen != (uncomprLen ^ 0xffff)) {
						throw new FormatException("broken uncompressed block");
					}
					mode = DECODE_STORED;
				}
				goto case DECODE_STORED;/* fall through */
				case DECODE_STORED: {
					int more = outputWindow.CopyStored(input, uncomprLen);
					uncomprLen -= more;
					if (uncomprLen == 0) {
						mode = DECODE_BLOCKS;
						return true;
					}
					return !input.IsNeedingInput;
				}
				
				case DECODE_DYN_HEADER:
					if (!dynHeader.Decode(input)) {
						return false;
					}
					
					litlenTree = dynHeader.BuildLitLenTree();
					distTree = dynHeader.BuildDistTree();
					mode = DECODE_HUFFMAN;
					goto case DECODE_HUFFMAN; /* fall through */
				case DECODE_HUFFMAN:
				case DECODE_HUFFMAN_LENBITS:
				case DECODE_HUFFMAN_DIST:
				case DECODE_HUFFMAN_DISTBITS:
					return DecodeHuffman();
				case FINISHED:
					return false;
				default:
					throw new FormatException();
			}
		}
			
		/// <summary>
		/// Sets the preset dictionary.  This should only be called, if
		/// needsDictionary() returns true and it should set the same
		/// dictionary, that was used for deflating.  The getAdler()
		/// function returns the checksum of the dictionary needed.
		/// </summary>
		/// <param name="buffer">
		/// the dictionary.
		/// </param>
		/// <exception cref="System.InvalidOperationException">
		/// if no dictionary is needed.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// if the dictionary checksum is wrong.
		/// </exception>
		public void SetDictionary(byte[] buffer)
		{
			SetDictionary(buffer, 0, buffer.Length);
		}
		
		/// <summary>
		/// Sets the preset dictionary.  This should only be called, if
		/// needsDictionary() returns true and it should set the same
		/// dictionary, that was used for deflating.  The getAdler()
		/// function returns the checksum of the dictionary needed.
		/// </summary>
		/// <param name="buffer">
		/// the dictionary.
		/// </param>
		/// <param name="off">
		/// the offset into buffer where the dictionary starts.
		/// </param>
		/// <param name="len">
		/// the length of the dictionary.
		/// </param>
		/// <exception cref="System.InvalidOperationException">
		/// if no dictionary is needed.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// if the dictionary checksum is wrong.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// if the off and/or len are wrong.
		/// </exception>
		public void SetDictionary(byte[] buffer, int off, int len)
		{
			if (!IsNeedingDictionary) {
				throw new InvalidOperationException();
			}
			
			adler.Update(buffer, off, len);
			if ((int) adler.Value != readAdler) {
				throw new ArgumentException("Wrong adler checksum");
			}
			adler.Reset();
			outputWindow.CopyDict(buffer, off, len);
			mode = DECODE_BLOCKS;
		}
		
		/// <summary>
		/// Sets the input.  This should only be called, if needsInput()
		/// returns true.
		/// </summary>
		/// <param name="buf">
		/// the input.
		/// </param>
		/// <exception cref="System.InvalidOperationException">
		/// if no input is needed.
		/// </exception>
		public void SetInput(byte[] buf)
		{
			SetInput(buf, 0, buf.Length);
		}
		
		/// <summary>
		/// Sets the input.  This should only be called, if needsInput()
		/// returns true.
		/// </summary>
		/// <param name="buf">
		/// the input.
		/// </param>
		/// <param name="off">
		/// the offset into buffer where the input starts.
		/// </param>
		/// <param name="len">
		/// the length of the input.
		/// </param>
		/// <exception cref="System.InvalidOperationException">
		/// if no input is needed.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// if the off and/or len are wrong.
		/// </exception>
		public void SetInput(byte[] buf, int off, int len)
		{
			input.SetInput(buf, off, len);
			totalIn += len;
		}
		
		/// <summary>
		/// Inflates the compressed stream to the output buffer.  If this
		/// returns 0, you should check, whether needsDictionary(),
		/// needsInput() or finished() returns true, to determine why no
		/// further output is produced.
		/// </summary>
		/// <param name = "buf">
		/// the output buffer.
		/// </param>
		/// <returns>
		/// the number of bytes written to the buffer, 0 if no further
		/// output can be produced.
		/// </returns>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// if buf has length 0.
		/// </exception>
		/// <exception cref="System.FormatException">
		/// if deflated stream is invalid.
		/// </exception>
		public int Inflate(byte[] buf)
		{
			return Inflate(buf, 0, buf.Length);
		}
		
		/// <summary>
		/// Inflates the compressed stream to the output buffer.  If this
		/// returns 0, you should check, whether needsDictionary(),
		/// needsInput() or finished() returns true, to determine why no
		/// further output is produced.
		/// </summary>
		/// <param name = "buf">
		/// the output buffer.
		/// </param>
		/// <param name = "off">
		/// the offset into buffer where the output should start.
		/// </param>
		/// <param name = "len">
		/// the maximum length of the output.
		/// </param>
		/// <returns>
		/// the number of bytes written to the buffer, 0 if no further output can be produced.
		/// </returns>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// if len is &lt;= 0.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// if the off and/or len are wrong.
		/// </exception>
		/// <exception cref="System.FormatException">
		/// if deflated stream is invalid.
		/// </exception>
		public int Inflate(byte[] buf, int off, int len)
		{
			if (len <= 0) {
				throw new ArgumentOutOfRangeException("len <= 0");
			}
			int count = 0;
			int more;
			do {
				if (mode != DECODE_CHKSUM) {
					/* Don't give away any output, if we are waiting for the
					* checksum in the input stream.
					*
					* With this trick we have always:
					*   needsInput() and not finished()
					*   implies more output can be produced.
					*/
					more = outputWindow.CopyOutput(buf, off, len);
					adler.Update(buf, off, more);
					off += more;
					count += more;
					totalOut += more;
					len -= more;
					if (len == 0) {
						return count;
					}
				}
			} while (Decode() || (outputWindow.GetAvailable() > 0 &&
			                      mode != DECODE_CHKSUM));
			return count;
		}
		
		/// <summary>
		/// Returns true, if the input buffer is empty.
		/// You should then call setInput(). 
		/// NOTE: This method also returns true when the stream is finished.
		/// </summary>
		public bool IsNeedingInput {
			get {
				return input.IsNeedingInput;
			}
		}
		
		/// <summary>
		/// Returns true, if a preset dictionary is needed to inflate the input.
		/// </summary>
		public bool IsNeedingDictionary {
			get {
				return mode == DECODE_DICT && neededBits == 0;
			}
		}
		
		/// <summary>
		/// Returns true, if the inflater has finished.  This means, that no
		/// input is needed and no output can be produced.
		/// </summary>
		public bool IsFinished {
			get {
				return mode == FINISHED && outputWindow.GetAvailable() == 0;
			}
		}
		
		/// <summary>
		/// Gets the adler checksum.  This is either the checksum of all
		/// uncompressed bytes returned by inflate(), or if needsDictionary()
		/// returns true (and thus no output was yet produced) this is the
		/// adler checksum of the expected dictionary.
		/// </summary>
		/// <returns>
		/// the adler checksum.
		/// </returns>
		public int Adler {
			get {
				return IsNeedingDictionary ? readAdler : (int) adler.Value;
			}
		}
		
		/// <summary>
		/// Gets the total number of output bytes returned by inflate().
		/// </summary>
		/// <returns>
		/// the total number of output bytes.
		/// </returns>
		public int TotalOut {
			get {
				return totalOut;
			}
		}
		
		/// <summary>
		/// Gets the total number of processed compressed input bytes.
		/// </summary>
		/// <returns>
		/// the total number of bytes of processed input bytes.
		/// </returns>
		public int TotalIn {
			get {
				return totalIn - RemainingInput;
			}
		}
		
		/// <summary>
		/// Gets the number of unprocessed input.  Useful, if the end of the
		/// stream is reached and you want to further process the bytes after
		/// the deflate stream.
		/// </summary>
		/// <returns>
		/// the number of bytes of the input which were not processed.
		/// </returns>
		public int RemainingInput {
			get {
				return input.AvailableBytes;
			}
		}
	}
}

namespace NZlib.Compression {
	
	public class InflaterDynHeader
	{
		private const int LNUM   = 0;
		private const int DNUM   = 1;
		private const int BLNUM  = 2;
		private const int BLLENS = 3;
		private const int LLENS  = 4;
		private const int DLENS  = 5;
		private const int LREPS  = 6;
		private const int DREPS  = 7;
		private const int FINISH = 8;
		
		private byte[] blLens;
		private byte[] litlenLens;
		private byte[] distLens;
		
		private InflaterHuffmanTree blTree;
		
		private int mode;
		private int lnum, dnum, blnum;
		private int repBits;
		private byte repeatedLen;
		private int ptr;
		
		private static int[] BL_ORDER = { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };
		
		public InflaterDynHeader()
		{
		}
		
		public bool Decode(StreamManipulator input)
		{
			decode_loop:
			for (;;) {
				switch (mode) {
					case LNUM:
						lnum = input.PeekBits(5);
						if (lnum < 0) {
							return false;
						}
						lnum += 257;
						input.DropBits(5);
						litlenLens = new byte[lnum];
						//  	    System.err.println("LNUM: "+lnum);
						mode = DNUM;
						goto case DNUM;/* fall through */
					case DNUM:
						dnum = input.PeekBits(5);
						if (dnum < 0) {
							return false;
						}
						dnum++;
						input.DropBits(5);
						distLens = new byte[dnum];
						//  	    System.err.println("DNUM: "+dnum);
						mode = BLNUM;
						goto case BLNUM;/* fall through */
					case BLNUM:
						blnum = input.PeekBits(4);
						if (blnum < 0) {
							return false;
						}
						blnum += 4;
						input.DropBits(4);
						blLens = new byte[19];
						ptr = 0;
						//  	    System.err.println("BLNUM: "+blnum);
						mode = BLLENS;
						goto case BLLENS;/* fall through */
					case BLLENS:
						while (ptr < blnum) {
							int len = input.PeekBits(3);
							if (len < 0) {
								return false;
							}
							input.DropBits(3);
							//  		System.err.println("blLens["+BL_ORDER[ptr]+"]: "+len);
							blLens[BL_ORDER[ptr]] = (byte) len;
							ptr++;
						}
						blTree = new InflaterHuffmanTree(blLens);
						blLens = null;
						ptr = 0;
						mode = LLENS;
						goto case LLENS;/* fall through */
					case LLENS:
						while (ptr < lnum) {
							int symbol = blTree.GetSymbol(input);
							if (symbol < 0) {
								return false;
							}
							switch (symbol) {
								default:
									//  		    System.err.println("litlenLens["+ptr+"]: "+symbol);
									litlenLens[ptr++] = (byte) symbol;
									break;
								case 16: /* repeat last len 3-6 times */
									if (ptr == 0) {
										throw new Exception("Repeating, but no prev len");
									}
									
									//  		    System.err.println("litlenLens["+ptr+"]: repeat");
									repeatedLen = litlenLens[ptr-1];
									repBits = 2;
									for (int i = 3; i-- > 0; ) {
										if (ptr >= lnum) {
											throw new Exception();
										}
										litlenLens[ptr++] = repeatedLen;
									}
									mode = LREPS;
									goto decode_loop;
								case 17: /* repeat zero 3-10 times */
									//  		    System.err.println("litlenLens["+ptr+"]: zero repeat");
									repeatedLen = 0;
									repBits = 3;
									for (int i = 3; i-- > 0; ) {
										if (ptr >= lnum) {
											throw new Exception();
										}
										litlenLens[ptr++] = repeatedLen;
									}
									mode = LREPS;
									goto decode_loop;
								case 18: /* repeat zero 11-138 times */
									//  		    System.err.println("litlenLens["+ptr+"]: zero repeat");
									repeatedLen = 0;
									repBits = 7;
									for (int i = 11; i-- > 0; ) {
										if (ptr >= lnum) {
											throw new Exception();
										}
										litlenLens[ptr++] = repeatedLen;
									}
									mode = LREPS;
									goto decode_loop;
							}
						}
						ptr = 0;
						mode = DLENS;
						goto case DLENS;/* fall through */
						case DLENS:
							while (ptr < dnum) {
								int symbol = blTree.GetSymbol(input);
								if (symbol < 0) {
									return false;
								}
								switch (symbol) {
									default:
										distLens[ptr++] = (byte) symbol;
										//  		    System.err.println("distLens["+ptr+"]: "+symbol);
										break;
									case 16: /* repeat last len 3-6 times */
										if (ptr == 0) {
											throw new Exception("Repeating, but no prev len");
										}
										//  		    System.err.println("distLens["+ptr+"]: repeat");
										repeatedLen = distLens[ptr-1];
										repBits = 2;
										for (int i = 3; i-- > 0; ) {
											if (ptr >= dnum) {
												throw new Exception();
											}
											distLens[ptr++] = repeatedLen;
										}
										mode = DREPS;
										goto decode_loop;
									case 17: /* repeat zero 3-10 times */
										//  		    System.err.println("distLens["+ptr+"]: repeat zero");
										repeatedLen = 0;
										repBits = 3;
										for (int i = 3; i-- > 0; ) {
											if (ptr >= dnum) {
												throw new Exception();
											}
											distLens[ptr++] = repeatedLen;
										}
										mode = DREPS;
										goto decode_loop;
									case 18: /* repeat zero 11-138 times */
										//  		    System.err.println("distLens["+ptr+"]: repeat zero");
										repeatedLen = 0;
										repBits = 7;
										for (int i = 11; i-- > 0; ) {
											if (ptr >= dnum) {
												throw new Exception();
											}
											distLens[ptr++] = repeatedLen;
										}
										mode = DREPS;
										goto decode_loop;
								}
							}
							mode = FINISH;
						return true;
					case LREPS:
						{
							int count = input.PeekBits(repBits);
							if (count < 0) {
								return false;
							}
							input.DropBits(repBits);
							//  	      System.err.println("litlenLens repeat: "+repBits);
							while (count-- > 0) {
								if (ptr >= lnum) {
									throw new Exception();
								}
								litlenLens[ptr++] = repeatedLen;
							}
						}
						mode = LLENS;
						goto decode_loop;
					case DREPS:
						{
							int count = input.PeekBits(repBits);
							if (count < 0) {
								return false;
							}
							input.DropBits(repBits);
							while (count-- > 0) {
								if (ptr >= dnum) {
									throw new Exception();
								}
								distLens[ptr++] = repeatedLen;
							}
						}
						mode = DLENS;
						goto decode_loop;
				}
			}
		}
		
		public InflaterHuffmanTree BuildLitLenTree()
		{
			return new InflaterHuffmanTree(litlenLens);
		}
		
		public InflaterHuffmanTree BuildDistTree()
		{
			return new InflaterHuffmanTree(distLens);
		}
	}
}

namespace NZlib.Compression {
	
	public class InflaterHuffmanTree 
	{
		private static int MAX_BITLEN = 15;
		private short[] tree;
		
		public static InflaterHuffmanTree defLitLenTree, defDistTree;
		
		static InflaterHuffmanTree()
		{
			try {
				byte[] codeLengths = new byte[288];
				int i = 0;
				while (i < 144) {
					codeLengths[i++] = 8;
				}
				while (i < 256) {
					codeLengths[i++] = 9;
				}
				while (i < 280) {
					codeLengths[i++] = 7;
				}
				while (i < 288) {
					codeLengths[i++] = 8;
				}
				defLitLenTree = new InflaterHuffmanTree(codeLengths);
				
				codeLengths = new byte[32];
				i = 0;
				while (i < 32) {
					codeLengths[i++] = 5;
				}
				defDistTree = new InflaterHuffmanTree(codeLengths);
			} catch (Exception) {
				throw new ApplicationException("InflaterHuffmanTree: static tree length illegal");
			}
		}
		
		/// <summary>
		/// Constructs a Huffman tree from the array of code lengths.
		/// </summary>
		/// <param name = "codeLengths">
		/// the array of code lengths
		/// </param>
		public InflaterHuffmanTree(byte[] codeLengths)
		{
			BuildTree(codeLengths);
		}
		
		private void BuildTree(byte[] codeLengths)
		{
			int[] blCount  = new int[MAX_BITLEN + 1];
			int[] nextCode = new int[MAX_BITLEN + 1];
			
			for (int i = 0; i < codeLengths.Length; i++) {
				int bits = codeLengths[i];
				if (bits > 0)
					blCount[bits]++;
			}
			
			int code = 0;
			int treeSize = 512;
			for (int bits = 1; bits <= MAX_BITLEN; bits++) {
				nextCode[bits] = code;
				code += blCount[bits] << (16 - bits);
				if (bits >= 10) {
					/* We need an extra table for bit lengths >= 10. */
					int start = nextCode[bits] & 0x1ff80;
					int end   = code & 0x1ff80;
					treeSize += (end - start) >> (16 - bits);
				}
			}
			if (code != 65536) {
				throw new Exception("Code lengths don't add up properly.");
			}
			/* Now create and fill the extra tables from longest to shortest
			* bit len.  This way the sub trees will be aligned.
			*/
			tree = new short[treeSize];
			int treePtr = 512;
			for (int bits = MAX_BITLEN; bits >= 10; bits--) {
				int end   = code & 0x1ff80;
				code -= blCount[bits] << (16 - bits);
				int start = code & 0x1ff80;
				for (int i = start; i < end; i += 1 << 7) {
					tree[DeflaterHuffman.BitReverse(i)] = (short) ((-treePtr << 4) | bits);
					treePtr += 1 << (bits-9);
				}
			}
			
			for (int i = 0; i < codeLengths.Length; i++) {
				int bits = codeLengths[i];
				if (bits == 0) {
					continue;
				}
				code = nextCode[bits];
				int revcode = DeflaterHuffman.BitReverse(code);
				if (bits <= 9) {
					do {
						tree[revcode] = (short) ((i << 4) | bits);
						revcode += 1 << bits;
					} while (revcode < 512);
				} else {
					int subTree = tree[revcode & 511];
					int treeLen = 1 << (subTree & 15);
					subTree = -(subTree >> 4);
					do {
						tree[subTree | (revcode >> 9)] = (short) ((i << 4) | bits);
						revcode += 1 << bits;
					} while (revcode < treeLen);
				}
				nextCode[bits] = code + (1 << (16 - bits));
			}
			
		}
		
		/// <summary>
		/// Reads the next symbol from input.  The symbol is encoded using the
		/// huffman tree.
		/// </summary>
		/// <param name="input">
		/// input the input source.
		/// </param>
		/// <returns>
		/// the next symbol, or -1 if not enough input is available.
		/// </returns>
		public int GetSymbol(StreamManipulator input)
		{
			int lookahead, symbol;
			if ((lookahead = input.PeekBits(9)) >= 0) {
				if ((symbol = tree[lookahead]) >= 0) {
					input.DropBits(symbol & 15);
					return symbol >> 4;
				}
				int subtree = -(symbol >> 4);
				int bitlen = symbol & 15;
				if ((lookahead = input.PeekBits(bitlen)) >= 0) {
					symbol = tree[subtree | (lookahead >> 9)];
					input.DropBits(symbol & 15);
					return symbol >> 4;
				} else {
					int bits = input.AvailableBits;
					lookahead = input.PeekBits(bits);
					symbol = tree[subtree | (lookahead >> 9)];
					if ((symbol & 15) <= bits) {
						input.DropBits(symbol & 15);
						return symbol >> 4;
					} else {
						return -1;
					}
				}
			} else {
				int bits = input.AvailableBits;
				lookahead = input.PeekBits(bits);
				symbol = tree[lookahead];
				if (symbol >= 0 && (symbol & 15) <= bits) {
					input.DropBits(symbol & 15);
					return symbol >> 4;
				} else {
					return -1;
				}
			}
		}
	}
}

namespace NZlib.Compression {
	
	/// <summary>
	/// This class is general purpose class for writing data to a buffer.
	/// 
	/// It allows you to write bits as well as bytes
	/// Based on DeflaterPending.java
	/// 
	/// author of the original java version : Jochen Hoenicke
	/// </summary>
	public class PendingBuffer
	{
		protected byte[] buf;
		int    start;
		int    end;
		
		uint    bits;
		int    bitCount;
		
		public PendingBuffer() : this( 4096 )
		{
			
		}
		
		public PendingBuffer(int bufsize)
		{
			buf = new byte[bufsize];
		}
		
		public void Reset() 
		{
			start = end = bitCount = 0;
		}
		
		public void WriteByte(int b)
		{
			if (DeflaterConstants.DEBUGGING && start != 0)
				throw new Exception();
			buf[end++] = (byte) b;
		}
		
		public void WriteShort(int s)
		{
			if (DeflaterConstants.DEBUGGING && start != 0) {
				throw new Exception();
			}
			buf[end++] = (byte) s;
			buf[end++] = (byte) (s >> 8);
		}
		
		public void WriteInt(int s)
		{
			if (DeflaterConstants.DEBUGGING && start != 0) {
				throw new Exception();
			}
			buf[end++] = (byte) s;
			buf[end++] = (byte) (s >> 8);
			buf[end++] = (byte) (s >> 16);
			buf[end++] = (byte) (s >> 24);
		}
		
		public void WriteBlock(byte[] block, int offset, int len)
		{
			if (DeflaterConstants.DEBUGGING && start != 0) {
				throw new Exception();
			}
			System.Array.Copy(block, offset, buf, end, len);
			end += len;
		}
		
		public int BitCount {
			get {
				return bitCount;
			}
		}
		
		public void AlignToByte() 
		{
			if (DeflaterConstants.DEBUGGING && start != 0) {
				throw new Exception();
			}
			if (bitCount > 0) {
				buf[end++] = (byte) bits;
				if (bitCount > 8) {
					buf[end++] = (byte) (bits >> 8);
				}
			}
			bits = 0;
			bitCount = 0;
		}
		
		public void WriteBits(int b, int count)
		{
			if (DeflaterConstants.DEBUGGING && start != 0) {
				throw new Exception();
			}
//			if (DeflaterConstants.DEBUGGING) {
//				Console.WriteLine("writeBits("+b+","+count+")");
//			}
			bits |= (uint)(b << bitCount);
			bitCount += count;
			if (bitCount >= 16) {
				buf[end++] = (byte) bits;
				buf[end++] = (byte) (bits >> 8);
				bits >>= 16;
				bitCount -= 16;
			}
		}
		
		public void WriteShortMSB(int s) 
		{
			if (DeflaterConstants.DEBUGGING && start != 0) {
				throw new Exception();
			}
			buf[end++] = (byte) (s >> 8);
			buf[end++] = (byte) s;
		}
		
		public bool IsFlushed {
			get {
				return end == 0;
			}
		}
		
		/// <summary>
		/// Flushes the pending buffer into the given output array.  If the
		/// output array is to small, only a partial flush is done.
		/// </summary>
		/// <param name="output">
		/// the output array;
		/// </param>
		/// <param name="offset">
		/// the offset into output array;
		/// </param>
		/// <param name="length">		
		/// length the maximum number of bytes to store;
		/// </param>
		/// <exception name="ArgumentOutOfRangeException">
		/// IndexOutOfBoundsException if offset or length are invalid.
		/// </exception>
		public int Flush(byte[] output, int offset, int length) 
		{
			if (bitCount >= 8) {
				buf[end++] = (byte) bits;
				bits >>= 8;
				bitCount -= 8;
			}
			if (length > end - start) {
				length = end - start;
				System.Array.Copy(buf, start, output, offset, length);
				start = 0;
				end = 0;
			} else {
				System.Array.Copy(buf, start, output, offset, length);
				start += length;
			}
			return length;
		}
		
		public byte[] ToByteArray()
		{
			byte[] ret = new byte[ end - start ];
			System.Array.Copy(buf, start, ret, 0, ret.Length);
			start = 0;
			end = 0;
			return ret;
		}
	}
}	

namespace NZlib.Checksums {
	
	/// <summary>
	/// Computes Adler32 checksum for a stream of data. An Adler32
	/// checksum is not as reliable as a CRC32 checksum, but a lot faster to
	/// compute.
	/// 
	/// The specification for Adler32 may be found in RFC 1950.
	/// ZLIB Compressed Data Format Specification version 3.3)
	/// 
	/// 
	/// From that document:
	/// 
	///      "ADLER32 (Adler-32 checksum)
	///       This contains a checksum value of the uncompressed data
	///       (excluding any dictionary data) computed according to Adler-32
	///       algorithm. This algorithm is a 32-bit extension and improvement
	///       of the Fletcher algorithm, used in the ITU-T X.224 / ISO 8073
	///       standard.
	/// 
	///       Adler-32 is composed of two sums accumulated per byte: s1 is
	///       the sum of all bytes, s2 is the sum of all s1 values. Both sums
	///       are done modulo 65521. s1 is initialized to 1, s2 to zero.  The
	///       Adler-32 checksum is stored as s2*65536 + s1 in most-
	///       significant-byte first (network) order."
	/// 
	///  "8.2. The Adler-32 algorithm
	/// 
	///    The Adler-32 algorithm is much faster than the CRC32 algorithm yet
	///    still provides an extremely low probability of undetected errors.
	/// 
	///    The modulo on unsigned long accumulators can be delayed for 5552
	///    bytes, so the modulo operation time is negligible.  If the bytes
	///    are a, b, c, the second sum is 3a + 2b + c + 3, and so is position
	///    and order sensitive, unlike the first sum, which is just a
	///    checksum.  That 65521 is prime is important to avoid a possible
	///    large class of two-byte errors that leave the check unchanged.
	///    (The Fletcher checksum uses 255, which is not prime and which also
	///    makes the Fletcher check insensitive to single byte changes 0 -
	///    255.)
	/// 
	///    The sum s1 is initialized to 1 instead of zero to make the length
	///    of the sequence part of s2, so that the length does not have to be
	///    checked separately. (Any sequence of zeroes has a Fletcher
	///    checksum of zero.)"
	/// </summary>
	/// <see cref="NZlib.Streams.InflaterInputStream"/>
	/// <see cref="NZlib.Streams.DeflaterOutputStream"/>
	public sealed class Adler32 : IChecksum
	{
		/// <summary>
		/// largest prime smaller than 65536
		/// </summary>
		readonly static uint BASE = 65521;
		
		uint checksum;
		
		/// <summary>
		/// Returns the Adler32 data checksum computed so far.
		/// </summary>
		public long Value {
			get {
				return (long) checksum & 0xFFFFFFFFL;
			}
		}
		
		/// <summary>
		/// Creates a new instance of the <code>Adler32</code> class.
		/// The checksum starts off with a value of 1.
		/// </summary>
		public Adler32()
		{
			Reset();
		}
		
		/// <summary>
		/// Resets the Adler32 checksum to the initial value.
		/// </summary>
		public void Reset()
		{
			checksum = 1; //Initialize to 1
		}
		
		/// <summary>
		/// Updates the checksum with the byte b.
		/// </summary>
		/// <param name="bval">
		/// the data value to add. The high byte of the int is ignored.
		/// </param>
		public void Update(int bval)
		{
			//We could make a length 1 byte array and call update again, but I
			//would rather not have that overhead
			uint s1 = checksum & 0xFFFF;
			uint s2 = checksum >> 16;
			
			s1 = (s1 + ((uint)bval & 0xFF)) % BASE;
			s2 = (s1 + s2) % BASE;
			
			checksum = (s2 << 16) + s1;
		}
		
		/// <summary>
		/// Updates the checksum with the bytes taken from the array.
		/// </summary>
		/// <param name="buffer">
		/// buffer an array of bytes
		/// </param>
		public void Update(byte[] buffer)
		{
			Update(buffer, 0, buffer.Length);
		}
		
		/// <summary>
		/// Updates the checksum with the bytes taken from the array.
		/// </summary>
		/// <param name="buf">
		/// an array of bytes
		/// </param>
		/// <param name="off">
		/// the start of the data used for this update
		/// </param>
		/// <param name="len">
		/// the number of bytes to use for this update
		/// </param>
		public void Update(byte[] buf, int off, int len)
		{
			if (buf == null) {
				throw new ArgumentNullException("buf");
			}
			
			if (off < 0 || len < 0 || off + len > buf.Length) {
				throw new ArgumentOutOfRangeException();
			}
			
			//(By Per Bothner)
			uint s1 = checksum & 0xFFFF;
			uint s2 = checksum >> 16;
			
			while (len > 0) {
				// We can defer the modulo operation:
				// s1 maximally grows from 65521 to 65521 + 255 * 3800
				// s2 maximally grows by 3800 * median(s1) = 2090079800 < 2^31
				int n = 3800;
				if (n > len) {
					n = len;
				}
				len -= n;
				while (--n >= 0) {
					s1 = s1 + (uint)(buf[off++] & 0xFF);
					s2 = s2 + s1;
				}
				s1 %= BASE;
				s2 %= BASE;
			}
			
			checksum = (s2 << 16) | s1;
		}
	}
}

namespace NZlib.Checksums {
	
	/// <summary>
	/// Generate a table for a byte-wise 32-bit CRC calculation on the polynomial:
	/// x^32+x^26+x^23+x^22+x^16+x^12+x^11+x^10+x^8+x^7+x^5+x^4+x^2+x+1.
	///
	/// Polynomials over GF(2) are represented in binary, one bit per coefficient,
	/// with the lowest powers in the most significant bit.  Then adding polynomials
	/// is just exclusive-or, and multiplying a polynomial by x is a right shift by
	/// one.  If we call the above polynomial p, and represent a byte as the
	/// polynomial q, also with the lowest power in the most significant bit (so the
	/// byte 0xb1 is the polynomial x^7+x^3+x+1), then the CRC is (q*x^32) mod p,
	/// where a mod b means the remainder after dividing a by b.
	///
	/// This calculation is done using the shift-register method of multiplying and
	/// taking the remainder.  The register is initialized to zero, and for each
	/// incoming bit, x^32 is added mod p to the register if the bit is a one (where
	/// x^32 mod p is p+x^32 = x^26+...+1), and the register is multiplied mod p by
	/// x (which is shifting right by one and adding x^32 mod p if the bit shifted
	/// out is a one).  We start with the highest power (least significant bit) of
	/// q and repeat for all eight bits of q.
	///
	/// The table is simply the CRC of all possible eight bit values.  This is all
	/// the information needed to generate CRC's on data a byte at a time for all
	/// combinations of CRC register values and incoming bytes.
	/// </summary>
	public sealed class Crc32 : IChecksum
	{
		readonly static uint CrcSeed = 0xFFFFFFFF;
		
		readonly static uint[] CrcTable = new uint[] {
			0x00000000, 0x77073096, 0xEE0E612C, 0x990951BA, 0x076DC419,
			0x706AF48F, 0xE963A535, 0x9E6495A3, 0x0EDB8832, 0x79DCB8A4,
			0xE0D5E91E, 0x97D2D988, 0x09B64C2B, 0x7EB17CBD, 0xE7B82D07,
			0x90BF1D91, 0x1DB71064, 0x6AB020F2, 0xF3B97148, 0x84BE41DE,
			0x1ADAD47D, 0x6DDDE4EB, 0xF4D4B551, 0x83D385C7, 0x136C9856,
			0x646BA8C0, 0xFD62F97A, 0x8A65C9EC, 0x14015C4F, 0x63066CD9,
			0xFA0F3D63, 0x8D080DF5, 0x3B6E20C8, 0x4C69105E, 0xD56041E4,
			0xA2677172, 0x3C03E4D1, 0x4B04D447, 0xD20D85FD, 0xA50AB56B,
			0x35B5A8FA, 0x42B2986C, 0xDBBBC9D6, 0xACBCF940, 0x32D86CE3,
			0x45DF5C75, 0xDCD60DCF, 0xABD13D59, 0x26D930AC, 0x51DE003A,
			0xC8D75180, 0xBFD06116, 0x21B4F4B5, 0x56B3C423, 0xCFBA9599,
			0xB8BDA50F, 0x2802B89E, 0x5F058808, 0xC60CD9B2, 0xB10BE924,
			0x2F6F7C87, 0x58684C11, 0xC1611DAB, 0xB6662D3D, 0x76DC4190,
			0x01DB7106, 0x98D220BC, 0xEFD5102A, 0x71B18589, 0x06B6B51F,
			0x9FBFE4A5, 0xE8B8D433, 0x7807C9A2, 0x0F00F934, 0x9609A88E,
			0xE10E9818, 0x7F6A0DBB, 0x086D3D2D, 0x91646C97, 0xE6635C01,
			0x6B6B51F4, 0x1C6C6162, 0x856530D8, 0xF262004E, 0x6C0695ED,
			0x1B01A57B, 0x8208F4C1, 0xF50FC457, 0x65B0D9C6, 0x12B7E950,
			0x8BBEB8EA, 0xFCB9887C, 0x62DD1DDF, 0x15DA2D49, 0x8CD37CF3,
			0xFBD44C65, 0x4DB26158, 0x3AB551CE, 0xA3BC0074, 0xD4BB30E2,
			0x4ADFA541, 0x3DD895D7, 0xA4D1C46D, 0xD3D6F4FB, 0x4369E96A,
			0x346ED9FC, 0xAD678846, 0xDA60B8D0, 0x44042D73, 0x33031DE5,
			0xAA0A4C5F, 0xDD0D7CC9, 0x5005713C, 0x270241AA, 0xBE0B1010,
			0xC90C2086, 0x5768B525, 0x206F85B3, 0xB966D409, 0xCE61E49F,
			0x5EDEF90E, 0x29D9C998, 0xB0D09822, 0xC7D7A8B4, 0x59B33D17,
			0x2EB40D81, 0xB7BD5C3B, 0xC0BA6CAD, 0xEDB88320, 0x9ABFB3B6,
			0x03B6E20C, 0x74B1D29A, 0xEAD54739, 0x9DD277AF, 0x04DB2615,
			0x73DC1683, 0xE3630B12, 0x94643B84, 0x0D6D6A3E, 0x7A6A5AA8,
			0xE40ECF0B, 0x9309FF9D, 0x0A00AE27, 0x7D079EB1, 0xF00F9344, 
			0x8708A3D2, 0x1E01F268, 0x6906C2FE, 0xF762575D, 0x806567CB,
			0x196C3671, 0x6E6B06E7, 0xFED41B76, 0x89D32BE0, 0x10DA7A5A,
			0x67DD4ACC, 0xF9B9DF6F, 0x8EBEEFF9, 0x17B7BE43, 0x60B08ED5,
			0xD6D6A3E8, 0xA1D1937E, 0x38D8C2C4, 0x4FDFF252, 0xD1BB67F1,
			0xA6BC5767, 0x3FB506DD, 0x48B2364B, 0xD80D2BDA, 0xAF0A1B4C,
			0x36034AF6, 0x41047A60, 0xDF60EFC3, 0xA867DF55, 0x316E8EEF,
			0x4669BE79, 0xCB61B38C, 0xBC66831A, 0x256FD2A0, 0x5268E236,
			0xCC0C7795, 0xBB0B4703, 0x220216B9, 0x5505262F, 0xC5BA3BBE,
			0xB2BD0B28, 0x2BB45A92, 0x5CB36A04, 0xC2D7FFA7, 0xB5D0CF31,
			0x2CD99E8B, 0x5BDEAE1D, 0x9B64C2B0, 0xEC63F226, 0x756AA39C,
			0x026D930A, 0x9C0906A9, 0xEB0E363F, 0x72076785, 0x05005713,
			0x95BF4A82, 0xE2B87A14, 0x7BB12BAE, 0x0CB61B38, 0x92D28E9B,
			0xE5D5BE0D, 0x7CDCEFB7, 0x0BDBDF21, 0x86D3D2D4, 0xF1D4E242,
			0x68DDB3F8, 0x1FDA836E, 0x81BE16CD, 0xF6B9265B, 0x6FB077E1,
			0x18B74777, 0x88085AE6, 0xFF0F6A70, 0x66063BCA, 0x11010B5C,
			0x8F659EFF, 0xF862AE69, 0x616BFFD3, 0x166CCF45, 0xA00AE278,
			0xD70DD2EE, 0x4E048354, 0x3903B3C2, 0xA7672661, 0xD06016F7,
			0x4969474D, 0x3E6E77DB, 0xAED16A4A, 0xD9D65ADC, 0x40DF0B66,
			0x37D83BF0, 0xA9BCAE53, 0xDEBB9EC5, 0x47B2CF7F, 0x30B5FFE9,
			0xBDBDF21C, 0xCABAC28A, 0x53B39330, 0x24B4A3A6, 0xBAD03605,
			0xCDD70693, 0x54DE5729, 0x23D967BF, 0xB3667A2E, 0xC4614AB8,
			0x5D681B02, 0x2A6F2B94, 0xB40BBE37, 0xC30C8EA1, 0x5A05DF1B, 
			0x2D02EF8D
		};
		
		/// <summary>
		/// The crc data checksum so far.
		/// </summary>
		uint crc = 0;
		
		/// <summary>
		/// Returns the CRC32 data checksum computed so far.
		/// </summary>
		public long Value {
			get {
				return (long)crc;
			}
		}
		
		/// <summary>
		/// Resets the CRC32 data checksum as if no update was ever called.
		/// </summary>
		public void Reset() 
		{ 
			crc = 0; 
		}
		
		/// <summary>
		/// Updates the checksum with the int bval.
		/// </summary>
		/// <param name = "bval">
		/// the byte is taken as the lower 8 bits of bval
		/// </param>
		public void Update(int bval)
		{
			crc ^= CrcSeed;
			crc  = CrcTable[(crc ^ bval) & 0xFF] ^ (crc >> 8);
			crc ^= CrcSeed;
		}
		
		/// <summary>
		/// Updates the checksum with the bytes taken from the array.
		/// </summary>
		/// <param name="buffer">
		/// buffer an array of bytes
		/// </param>
		public void Update(byte[] buffer)
		{
			Update(buffer, 0, buffer.Length);
		}
		
		/// <summary>
		/// Adds the byte array to the data checksum.
		/// </summary>
		/// <param name = "buf">
		/// the buffer which contains the data
		/// </param>
		/// <param name = "off">
		/// the offset in the buffer where the data starts
		/// </param>
		/// <param name = "len">
		/// the length of the data
		/// </param>
		public void Update(byte[] buf, int off, int len)
		{
			if (buf == null) {
				throw new ArgumentNullException("buf");
			}
			
			if (off < 0 || len < 0 || off + len > buf.Length) {
				throw new ArgumentOutOfRangeException();
			}
			
			crc ^= CrcSeed;
			
			while (--len >= 0) {
				crc = CrcTable[(crc ^ buf[off++]) & 0xFF] ^ (crc >> 8);
			}
			
			crc ^= CrcSeed;
		}
	}
}

namespace NZlib.Checksums {
	
	/// <summary>
	/// Interface to compute a data checksum used by checked input/output streams.
	/// A data checksum can be updated by one byte or with a byte array. After each
	/// update the value of the current checksum can be returned by calling
	/// <code>getValue</code>. The complete checksum object can also be reset
	/// so it can be used again with new data.
	/// </summary>
	public interface IChecksum
	{
		/// <summary>
		/// Returns the data checksum computed so far.
		/// </summary>
		long Value {
			get;
		}
		
		/// <summary>
		/// Resets the data checksum as if no update was ever called.
		/// </summary>
		void Reset();
		
		/// <summary>
		/// Adds one byte to the data checksum.
		/// </summary>
		/// <param name = "bval">
		/// the data value to add. The high byte of the int is ignored.
		/// </param>
		void Update(int bval);
		
		/// <summary>
		/// Updates the data checksum with the bytes taken from the array.
		/// </summary>
		/// <param name="buffer">
		/// buffer an array of bytes
		/// </param>
		void Update(byte[] buffer);
		
		/// <summary>
		/// Adds the byte array to the data checksum.
		/// </summary>
		/// <param name = "buf">
		/// the buffer which contains the data
		/// </param>
		/// <param name = "off">
		/// the offset in the buffer where the data starts
		/// </param>
		/// <param name = "len">
		/// the length of the data
		/// </param>
		void Update(byte[] buf, int off, int len);
	}
}

namespace NZlib.Streams {
	
	/// <summary>
	/// Contains the output from the Inflation process.
	/// We need to have a window so that we can refer backwards into the output stream
	/// to repeat stuff.
	///
	/// author of the original java version : John Leuner
	/// </summary>
	public class OutputWindow
	{
		private static int WINDOW_SIZE = 1 << 15;
		private static int WINDOW_MASK = WINDOW_SIZE - 1;
		
		private byte[] window = new byte[WINDOW_SIZE]; //The window is 2^15 bytes
		private int window_end  = 0;
		private int window_filled = 0;
		
		public void Write(int abyte)
		{
			if (window_filled++ == WINDOW_SIZE) {
				throw new InvalidOperationException("Window full");
			}
			window[window_end++] = (byte) abyte;
			window_end &= WINDOW_MASK;
		}
		
		
		private void SlowRepeat(int rep_start, int len, int dist)
		{
			while (len-- > 0) {
				window[window_end++] = window[rep_start++];
				window_end &= WINDOW_MASK;
				rep_start &= WINDOW_MASK;
			}
		}
		
		public void Repeat(int len, int dist)
		{
			if ((window_filled += len) > WINDOW_SIZE) {
				throw new InvalidOperationException("Window full");
			}
			
			int rep_start = (window_end - dist) & WINDOW_MASK;
			int border = WINDOW_SIZE - len;
			if (rep_start <= border && window_end < border) {
				if (len <= dist) {
					System.Array.Copy(window, rep_start, window, window_end, len);
					window_end += len;
				}				else {
					/* We have to copy manually, since the repeat pattern overlaps.
					*/
					while (len-- > 0) {
						window[window_end++] = window[rep_start++];
					}
				}
			} else {
				SlowRepeat(rep_start, len, dist);
			}
		}
		
		public int CopyStored(StreamManipulator input, int len)
		{
			len = Math.Min(Math.Min(len, WINDOW_SIZE - window_filled), input.AvailableBytes);
			int copied;
			
			int tailLen = WINDOW_SIZE - window_end;
			if (len > tailLen) {
				copied = input.CopyBytes(window, window_end, tailLen);
				if (copied == tailLen) {
					copied += input.CopyBytes(window, 0, len - tailLen);
				}
			} else {
				copied = input.CopyBytes(window, window_end, len);
			}
			
			window_end = (window_end + copied) & WINDOW_MASK;
			window_filled += copied;
			return copied;
		}
		
		public void CopyDict(byte[] dict, int offset, int len)
		{
			if (window_filled > 0) {
				throw new InvalidOperationException();
			}
			
			if (len > WINDOW_SIZE) {
				offset += len - WINDOW_SIZE;
				len = WINDOW_SIZE;
			}
			System.Array.Copy(dict, offset, window, 0, len);
			window_end = len & WINDOW_MASK;
		}
		
		public int GetFreeSpace()
		{
			return WINDOW_SIZE - window_filled;
		}
		
		public int GetAvailable()
		{
			return window_filled;
		}
		
		public int CopyOutput(byte[] output, int offset, int len)
		{
			int copy_end = window_end;
			if (len > window_filled) {
				len = window_filled;
			} else {
				copy_end = (window_end - window_filled + len) & WINDOW_MASK;
			}
			
			int copied = len;
			int tailLen = len - copy_end;
			
			if (tailLen > 0) {
				System.Array.Copy(window, WINDOW_SIZE - tailLen,
				                  output, offset, tailLen);
				offset += tailLen;
				len = copy_end;
			}
			System.Array.Copy(window, copy_end - len, output, offset, len);
			window_filled -= copied;
			if (window_filled < 0) {
				throw new InvalidOperationException();
			}
			return copied;
		}
		
		public void Reset()
		{
			window_filled = window_end = 0;
		}
	}
}

namespace NZlib.Streams {
	
	/// <summary>
	/// This class allows us to retrieve a specified amount of bits from
	/// the input buffer, as well as copy big byte blocks.
	///
	/// It uses an int buffer to store up to 31 bits for direct
	/// manipulation.  This guarantees that we can get at least 16 bits,
	/// but we only need at most 15, so this is all safe.
	///
	/// There are some optimizations in this class, for example, you must
	/// never peek more then 8 bits more than needed, and you must first
	/// peek bits before you may drop them.  This is not a general purpose
	/// class but optimized for the behaviour of the Inflater.
	///
	/// authors of the original java version : John Leuner, Jochen Hoenicke
	/// </summary>
	public class StreamManipulator
	{
		private byte[] window;
		private int window_start = 0;
		private int window_end = 0;
		
		private uint buffer = 0;
		private int bits_in_buffer = 0;
		
		/// <summary>
		/// Get the next n bits but don't increase input pointer.  n must be
		/// less or equal 16 and if you if this call succeeds, you must drop
		/// at least n-8 bits in the next call.
		/// </summary>
		/// <returns>
		/// the value of the bits, or -1 if not enough bits available.  */
		/// </returns>
		public int PeekBits(int n)
		{
			if (bits_in_buffer < n) {
				if (window_start == window_end) {
					return -1;
				}
				buffer |= (uint)((window[window_start++] & 0xff |
				                 (window[window_start++] & 0xff) << 8) << bits_in_buffer);
				bits_in_buffer += 16;
			}
			return (int)(buffer & ((1 << n) - 1));
		}
		
		/// <summary>
		/// Drops the next n bits from the input.  You should have called peekBits
		/// with a bigger or equal n before, to make sure that enough bits are in
		/// the bit buffer.
		/// </summary>
		public void DropBits(int n)
		{
			buffer >>= n;
			bits_in_buffer -= n;
		}
		
		/// <summary>
		/// Gets the next n bits and increases input pointer.  This is equivalent
		/// to peekBits followed by dropBits, except for correct error handling.
		/// </summary>
		/// <returns>
		/// the value of the bits, or -1 if not enough bits available.
		/// </returns>
		public int GetBits(int n)
		{
			int bits = PeekBits(n);
			if (bits >= 0) {
				DropBits(n);
			}
			return bits;
		}
		
		/// <summary>
		/// Gets the number of bits available in the bit buffer.  This must be
		/// only called when a previous peekBits() returned -1.
		/// </summary>
		/// <returns>
		/// the number of bits available.
		/// </returns>
		public int AvailableBits {
			get {
				return bits_in_buffer;
			}
		}
		
		/// <summary>
		/// Gets the number of bytes available.
		/// </summary>
		/// <returns>
		/// the number of bytes available.
		/// </returns>
		public int AvailableBytes {
			get {
				return window_end - window_start + (bits_in_buffer >> 3);
			}
		}
		
		/// <summary>
		/// Skips to the next byte boundary.
		/// </summary>
		public void SkipToByteBoundary()
		{
			buffer >>= (bits_in_buffer & 7);
			bits_in_buffer &= ~7;
		}
		
		public bool IsNeedingInput {
			get {
				return window_start == window_end;
			}
		}
		
		/// <summary>
		/// Copies length bytes from input buffer to output buffer starting
		/// at output[offset].  You have to make sure, that the buffer is
		/// byte aligned.  If not enough bytes are available, copies fewer
		/// bytes.
		/// </summary>
		/// <param name="output">
		/// the buffer.
		/// </param>
		/// <param name="offset">
		/// the offset in the buffer.
		/// </param>
		/// <param name="length">
		/// the length to copy, 0 is allowed.
		/// </param>
		/// <returns>
		/// the number of bytes copied, 0 if no byte is available.
		/// </returns>
		public int CopyBytes(byte[] output, int offset, int length)
		{
			if (length < 0) {
				throw new ArgumentOutOfRangeException("length negative");
			}
			if ((bits_in_buffer & 7) != 0)   {
				/* bits_in_buffer may only be 0 or 8 */
				throw new InvalidOperationException("Bit buffer is not aligned!");
			}
			
			int count = 0;
			while (bits_in_buffer > 0 && length > 0) {
				output[offset++] = (byte) buffer;
				buffer >>= 8;
				bits_in_buffer -= 8;
				length--;
				count++;
			}
			if (length == 0) {
				return count;
			}
			
			int avail = window_end - window_start;
			if (length > avail) {
				length = avail;
			}
			System.Array.Copy(window, window_start, output, offset, length);
			window_start += length;
			
			if (((window_start - window_end) & 1) != 0) {
				/* We always want an even number of bytes in input, see peekBits */
				buffer = (uint)(window[window_start++] & 0xff);
				bits_in_buffer = 8;
			}
			return count + length;
		}
		
		public StreamManipulator()
		{
		}
		
		public void Reset()
		{
			buffer = (uint)(window_start = window_end = bits_in_buffer = 0);
		}
		
		public void SetInput(byte[] buf, int off, int len)
		{
			if (window_start < window_end) {
				throw new InvalidOperationException("Old input was not completely processed");
			}
			
			int end = off + len;
			
			/* We want to throw an ArrayIndexOutOfBoundsException early.  The
			* check is very tricky: it also handles integer wrap around.
			*/
			if (0 > off || off > end || end > buf.Length) {
				throw new ArgumentOutOfRangeException();
			}
			
			if ((len & 1) != 0) {
				/* We always want an even number of bytes in input, see peekBits */
				buffer |= (uint)((buf[off++] & 0xff) << bits_in_buffer);
				bits_in_buffer += 8;
			}
			
			window = buf;
			window_start = off;
			window_end = end;
		}
	}
}
