Zlib-Intel is an optimized version of Zlib developed by Intel to take advantage of modern CPU features. 

Brief Listing of Changes:
New deflate_medium() strategy for compression levels 4-6
Improved hashing using the SSE4.2 CRC32 instruction
SSE Saturated Subtractions for hash table shifting
Faster CRC32 calculations using SSE4.2 PCLMULQDQ instruction
Reduced loop unrolling

Detailed explanations of these optimizations available in the official whitepaper: http://www.intel.com/content/dam/www/public/us/en/documents/white-papers/zlib-compression-whitepaper-copy.pdf

