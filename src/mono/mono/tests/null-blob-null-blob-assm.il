// This assembly compiles to a CIL image with a zero sized Blob stream
.assembly 'null-blob-null-blob-assm'
{
	.ver 0:0:0:0
}
.assembly extern 'null-blob-tgt' { }
.class extern forwarder C1
{
	.assembly extern 'null-blob-tgt'
}

