// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*
** This program was translated to C# and adapted for xunit-performance.
** New variants of several tests were added to compare class versus 
** struct and to compare jagged arrays vs multi-dimensional arrays.
*/

/*
** BYTEmark (tm)
** BYTE Magazine's Native Mode benchmarks
** Rick Grehan, BYTE Magazine
**
** Create:
** Revision: 3/95
**
** DISCLAIMER
** The source, executable, and documentation files that comprise
** the BYTEmark benchmarks are made available on an "as is" basis.
** This means that we at BYTE Magazine have made every reasonable
** effort to verify that the there are no errors in the source and
** executable code.  We cannot, however, guarantee that the programs
** are error-free.  Consequently, McGraw-HIll and BYTE Magazine make
** no claims in regard to the fitness of the source code, executable
** code, and documentation of the BYTEmark.
** 
** Furthermore, BYTE Magazine, McGraw-Hill, and all employees
** of McGraw-Hill cannot be held responsible for any damages resulting
** from the use of this code or the results obtained from using
** this code.
*/

using System;

/*
** TYPEDEFS
*/
internal struct huff_node
{
    public byte c;                /* Byte value */
    public float freq;             /* Frequency */
    public int parent;             /* Parent node */
    public int left;               /* Left pointer = 0 */
    public int right;              /* Right pointer = 1 */
};

/************************
** HUFFMAN COMPRESSION **
************************/
public class Huffman : HuffStruct
{
    public override string Name()
    {
        return "HUFFMAN";
    }

    /**************
	** DoHuffman **
	***************
	** Execute a huffman compression on a block of plaintext.
	** Note that (as with IDEA encryption) an iteration of the
	** Huffman test includes a compression AND a decompression.
	** Also, the compression cycle includes building the
	** Huffman tree.
	*/
    public override double Run()
    {
        huff_node[] hufftree;
        long accumtime;
        double iterations;
        byte[] comparray;
        byte[] decomparray;
        byte[] plaintext;

        InitWords();

        /*
		** Allocate memory for the plaintext and the compressed text.
		** We'll be really pessimistic here, and allocate equal amounts
		** for both (though we know...well, we PRESUME) the compressed
		** stuff will take less than the plain stuff.
		** Also note that we'll build a 3rd buffer to decompress
		** into, and we preallocate space for the huffman tree.
		** (We presume that the Huffman tree will grow no larger
		** than 512 bytes.  This is actually a super-conservative
		** estimate...but, who cares?)
		*/
        plaintext = new byte[this.arraysize];
        comparray = new byte[this.arraysize];
        decomparray = new byte[this.arraysize];

        hufftree = new huff_node[512];

        /*
		** Build the plaintext buffer.  Since we want this to
		** actually be able to compress, we'll use the
		** wordcatalog to build the plaintext stuff.
		*/
        create_text_block(plaintext, this.arraysize - 1, 500);
        //		for (int i = 0; i < this.arraysize-1; i++) {
        //			Console.Write((char)plaintext[i]);
        //		}
        plaintext[this.arraysize - 1] = (byte)'\0';
        // plaintextlen=this.arraysize;

        /*
		** See if we need to perform self adjustment loop.
		*/
        if (this.adjust == 0)
        {
            /*
			** Do self-adjustment.  This involves initializing the
			** # of loops and increasing the loop count until we
			** get a number of loops that we can use.
			*/

            for (this.loops = 100;
              this.loops < global.MAXHUFFLOOPS;
              this.loops += 10)
            {
                if (DoHuffIteration(plaintext,
                    comparray,
                    decomparray,
                  this.arraysize,
                  this.loops,
                  hufftree) > global.min_ticks)
                    break;
            }
        }

        /*
		** All's well if we get here.  Do the test.
		*/
        accumtime = 0L;
        iterations = (double)0.0;

        do
        {
            accumtime += DoHuffIteration(plaintext,
                comparray,
                decomparray,
                this.arraysize,
                this.loops,
                hufftree);
            iterations += (double)this.loops;
        } while (ByteMark.TicksToSecs(accumtime) < this.request_secs);

        /*
		** Clean up, calculate results, and go home.  Be sure to
		** show that we don't have to rerun adjustment code.
		*/
        //this.iterspersec=iterations / TicksToFracSecs(accumtime);

        if (this.adjust == 0)
            this.adjust = 1;

        return (iterations / ByteMark.TicksToFracSecs(accumtime));
    }


    /*********************
	** create_text_line **
	**********************
	** Create a random line of text, stored at *dt.  The line may be
	** no more than nchars long.
	*/
    private static void create_text_line(byte[] dt, int nchars, int lower)
    {
        int charssofar;        /* # of characters so far */
        int tomove;            /* # of characters to move */
        string myword;        /* Local buffer for words */

        int index = 0;

        charssofar = 0;

        do
        {
            /*
			** Grab a random word from the wordcatalog
			*/
            myword = wordcatarray[ByteMark.abs_randwc(Huffman.WORDCATSIZE)];

            /*
			** Append a blank.
			*/
            myword += " ";
            tomove = myword.Length;

            /*
			** See how long it is.  If its length+charssofar > nchars, we have
			** to trim it.
			*/
            if ((tomove + charssofar) > nchars)
                tomove = nchars - charssofar;
            /*
			** Attach the word to the current line.  Increment counter.
			*/
            for (int i = 0; i < tomove; i++)
            {
                dt[lower + index++] = (byte)myword[i];
            }
            charssofar += tomove;

            /*
			** If we're done, bail out.  Otherwise, go get another word.
			*/
        } while (charssofar < nchars);

        return;
    }

    /**********************
	** create_text_block **
	***********************
	** Build a block of text randomly loaded with words.  The words
	** come from the wordcatalog (which must be loaded before you
	** call this).
	** *tb points to the memory where the text is to be built.
	** tblen is the # of bytes to put into the text block
	** maxlinlen is the maximum length of any line (line end indicated
	**  by a carriage return).
	*/
    private static void create_text_block(byte[] tb,
                int tblen,
                short maxlinlen)
    {
        int bytessofar;       /* # of bytes so far */
        int linelen;          /* Line length */

        bytessofar = 0;
        do
        {
            /*
			** Pick a random length for a line and fill the line.
			** Make sure the line can fit (haven't exceeded tablen) and also
			** make sure you leave room to append a carriage return.
			*/
            linelen = ByteMark.abs_randwc(maxlinlen - 6) + 6;
            if ((linelen + bytessofar) > tblen)
                linelen = tblen - bytessofar;

            if (linelen > 1)
            {
                create_text_line(tb, linelen, bytessofar);
            }
            tb[linelen] = (byte)'\n';          /* Add the carriage return */

            bytessofar += linelen;
        } while (bytessofar < tblen);
    }

    /********************
	** DoHuffIteration **
	*********************
	** Perform the huffman benchmark.  This routine
	**  (a) Builds the huffman tree
	**  (b) Compresses the text
	**  (c) Decompresses the text and verifies correct decompression
	*/
    private static long DoHuffIteration(byte[] plaintext,
        byte[] comparray,
        byte[] decomparray,
        int arraysize,
        int nloops,
        huff_node[] hufftree)
    {
        int i;                          /* Index */
        int j;                         /* Bigger index */
        int root;                       /* Pointer to huffman tree root */
        float lowfreq1, lowfreq2;       /* Low frequency counters */
        int lowidx1, lowidx2;           /* Indexes of low freq. elements */
        int bitoffset;                 /* Bit offset into text */
        int textoffset;                /* Char offset into text */
        int maxbitoffset;              /* Holds limit of bit offset */
        int bitstringlen;              /* Length of bitstring */
        int c;                          /* Character from plaintext */
        byte[] bitstring = new byte[30];             /* Holds bitstring */
        long elapsed;                  /* For stopwatch */

        /*
		** Start the stopwatch
		*/
        elapsed = ByteMark.StartStopwatch();

        /*
		** Do everything for nloops
		*/
        while (nloops-- != 0)
        {
            /*
			** Calculate the frequency of each byte value. Store the
			** results in what will become the "leaves" of the
			** Huffman tree.  Interior nodes will be built in those
			** nodes greater than node #255.
			*/
            for (i = 0; i < 256; i++)
            {
                hufftree[i].freq = (float)0.0;
                hufftree[i].c = (byte)i;
            }

            for (j = 0; j < arraysize; j++)
                hufftree[plaintext[j]].freq += (float)1.0;

            for (i = 0; i < 256; i++)
                if (hufftree[i].freq != (float)0.0)
                    hufftree[i].freq /= (float)arraysize;

            /*
			** Build the huffman tree.  First clear all the parent
			** pointers and left/right pointers.  Also, discard all
			** nodes that have a frequency of true 0.
			*/
            for (i = 0; i < 512; i++)
            {
                if (hufftree[i].freq == (float)0.0)
                    hufftree[i].parent = EXCLUDED;
                else
                    hufftree[i].parent = hufftree[i].left = hufftree[i].right = -1;
            }

            /*
			** Go through the tree. Finding nodes of really low
			** frequency.
			*/
            root = 255;                       /* Starting root node-1 */
            while (true)
            {
                lowfreq1 = (float)2.0; lowfreq2 = (float)2.0;
                lowidx1 = -1; lowidx2 = -1;
                /*
				** Find first lowest frequency.
				*/
                for (i = 0; i <= root; i++)
                    if (hufftree[i].parent < 0)
                        if (hufftree[i].freq < lowfreq1)
                        {
                            lowfreq1 = hufftree[i].freq;
                            lowidx1 = i;
                        }

                /*
				** Did we find a lowest value?  If not, the
				** tree is done.
				*/
                if (lowidx1 == -1) break;

                /*
				** Find next lowest frequency
				*/
                for (i = 0; i <= root; i++)
                    if ((hufftree[i].parent < 0) && (i != lowidx1))
                        if (hufftree[i].freq < lowfreq2)
                        {
                            lowfreq2 = hufftree[i].freq;
                            lowidx2 = i;
                        }

                /*
				** If we could only find one item, then that
				** item is surely the root, and (as above) the
				** tree is done.
				*/
                if (lowidx2 == -1) break;

                /*
				** Attach the two new nodes to the current root, and
				** advance the current root.
				*/
                root++;                 /* New root */
                hufftree[lowidx1].parent = root;
                hufftree[lowidx2].parent = root;
                hufftree[root].freq = lowfreq1 + lowfreq2;
                hufftree[root].left = lowidx1;
                hufftree[root].right = lowidx2;
                hufftree[root].parent = -2;       /* Show root */
            }

            /*
			** Huffman tree built...compress the plaintext
			*/
            bitoffset = 0;                           /* Initialize bit offset */
            for (i = 0; i < arraysize; i++)
            {
                c = (int)plaintext[i];                 /* Fetch character */
                                                       /*
                                                       ** Build a bit string for byte c
                                                       */
                bitstringlen = 0;
                while (hufftree[c].parent != -2)
                {
                    if (hufftree[hufftree[c].parent].left == c)
                        bitstring[bitstringlen] = (byte)'0';
                    else
                        bitstring[bitstringlen] = (byte)'1';
                    c = hufftree[c].parent;
                    bitstringlen++;
                }

                /*
				** Step backwards through the bit string, setting
				** bits in the compressed array as you go.
				*/
                while (bitstringlen-- != 0)
                {
                    SetCompBit(comparray, bitoffset, bitstring[bitstringlen]);
                    bitoffset++;
                }
            }

            /*
			** Compression done.  Perform de-compression.
			*/
            maxbitoffset = bitoffset;
            bitoffset = 0;
            textoffset = 0;
            do
            {
                i = root;
                while (hufftree[i].left != -1)
                {
                    if (GetCompBit(comparray, bitoffset) == 0)
                        i = hufftree[i].left;
                    else
                        i = hufftree[i].right;
                    bitoffset++;
                }
                decomparray[textoffset] = hufftree[i].c;

#if DEBUG
                if (hufftree[i].c != plaintext[textoffset])
                {
                    /* Show error */
                    string error = String.Format("Huffman: error at textoffset {0}", textoffset);
                    throw new Exception(error);
                }
#endif
                textoffset++;
            } while (bitoffset < maxbitoffset);
        }       /* End the big while(nloops--) from above */

        /*
		** All done
		*/
        return (ByteMark.StopStopwatch(elapsed));
    }

    /***************
	** SetCompBit **
	****************
	** Set a bit in the compression array.  The value of the
	** bit is set according to char bitchar.
	*/
    private static void SetCompBit(byte[] comparray,
            int bitoffset,
            byte bitchar)
    {
        int byteoffset;
        int bitnumb;

        /*
		** First calculate which element in the comparray to
		** alter. and the bitnumber.
		*/
        byteoffset = bitoffset >> 3;
        bitnumb = bitoffset % 8;

        /*
		** Set or clear
		*/
        if (bitchar == '1')
            comparray[byteoffset] |= ((byte)(1 << bitnumb));
        else
        {
            // JTR: Work around compiler bug: (byte)~(1<<bitnumb);
            //int b = ~(1<<bitnumb);
            comparray[byteoffset] &= unchecked((byte)(~(1 << bitnumb)));
        }

        return;
    }

    /***************
	** GetCompBit **
	****************
	** Return the bit value of a bit in the comparession array.
	** Returns 0 if the bit is clear, nonzero otherwise.
	*/
    private static int GetCompBit(byte[] comparray,
            int bitoffset)
    {
        int byteoffset;
        int bitnumb;

        /*
		** Calculate byte offset and bit number.
		*/
        byteoffset = bitoffset >> 3;
        bitnumb = bitoffset % 8;

        /*
		** Fetch
		*/
        return ((1 << bitnumb) & comparray[byteoffset]);
    }

    protected const int WORDCATSIZE = 50;
    protected const int EXCLUDED = 32000;          /* Big positive value */
    protected static string[] wordcatarray;
    protected static void InitWords()
    {
        wordcatarray = new string[]
        {   "Hello",
            "He",
            "Him",
            "the",
            "this",
            "that",
            "though",
            "rough",
            "cough",
            "obviously",
            "But",
            "but",
            "bye",
            "begin",
            "beginning",
            "beginnings",
            "of",
            "our",
            "ourselves",
            "yourselves",
            "to",
            "together",
            "togetherness",
            "from",
            "either",
            "I",
            "A",
            "return",
            "However",
            "that",
            "example",
            "yet",
            "quickly",
            "all",
            "if",
            "were",
            "includes",
            "always",
            "never",
            "not",
            "small",
            "returns",
            "set",
            "basic",
            "Entered",
            "with",
            "used",
            "shown",
            "you",
            "know" };
    }
}
