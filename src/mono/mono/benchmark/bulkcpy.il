.assembly BulkCpy {}

.class public auto ansi sealed beforefieldinit BulkCpy {

	.field public static unsigned int8 Data at RandomData

	.method static public void Main() il managed {
		.entrypoint

		.locals (
			int32 count,
			int32 start,
			int32 elapsed,
			unsigned int8[] buff1,
			unsigned int8[] buff2,
			unsigned int8& pinned dest1,
			unsigned int8& pinned dest2,
			int32 size
		)

		ldc.i4 2053
		stloc size

		ldloc size
		newarr [mscorlib]System.Byte
		dup
		stloc buff1
		ldc.i4.0
		ldelema [mscorlib]System.Byte
		stloc dest1

		ldloc size
		newarr [mscorlib]System.Byte
		dup
		stloc buff2
		ldc.i4.0
		ldelema [mscorlib]System.Byte
		stloc dest2


		ldc.i4 1000000
		stloc count

		call int32 [mscorlib]System.Environment::get_TickCount()
		stloc start

	loop:
		ldloc dest1
		ldsflda unsigned int8 BulkCpy::Data
		ldc.i4 2053
		cpblk

		ldloc dest1
		ldsflda unsigned int8 BulkCpy::Data
		ldc.i4 0
		cpblk

		ldloc dest2
		ldsflda unsigned int8 BulkCpy::Data
		ldc.i4 1
		cpblk

		ldloc dest1
		ldsflda unsigned int8 BulkCpy::Data
		ldc.i4 2
		cpblk

		ldloc dest2
		ldsflda unsigned int8 BulkCpy::Data
		ldc.i4 3
		cpblk

		ldloc dest1
		ldsflda unsigned int8 BulkCpy::Data
		ldc.i4 4
		cpblk

		ldloc dest2
		ldsflda unsigned int8 BulkCpy::Data
		ldc.i4 5
		cpblk

		ldloc dest1
		ldsflda unsigned int8 BulkCpy::Data
		ldc.i4 6
		cpblk

		ldloc dest2
		ldsflda unsigned int8 BulkCpy::Data
		ldc.i4 7
		cpblk

		ldloc dest1
		ldsflda unsigned int8 BulkCpy::Data
		ldc.i4 8
		cpblk

		ldloc dest2
		ldsflda unsigned int8 BulkCpy::Data
		ldc.i4 9
		cpblk

		ldloc dest1
		ldsflda unsigned int8 BulkCpy::Data
		ldc.i4 10
		cpblk

		ldloc dest2
		ldsflda unsigned int8 BulkCpy::Data
		ldc.i4 11
		cpblk

		ldloc dest1
		ldsflda unsigned int8 BulkCpy::Data
		ldc.i4 12
		cpblk

		ldloc dest2
		ldsflda unsigned int8 BulkCpy::Data
		ldc.i4 13
		cpblk

		ldloc dest1
		ldsflda unsigned int8 BulkCpy::Data
		ldc.i4 14
		cpblk

		ldloc dest2
		ldsflda unsigned int8 BulkCpy::Data
		ldc.i4 15
		cpblk

		ldloc dest1
		ldsflda unsigned int8 BulkCpy::Data
		ldc.i4 16
		cpblk

		ldloc dest2
		ldsflda unsigned int8 BulkCpy::Data
		ldc.i4 2053
		cpblk

		ldloc count
		ldc.i4.1
		sub
		dup
		stloc count
		brtrue loop


		call int32 [mscorlib]System.Environment::get_TickCount()
		ldloc start
		sub
		stloc elapsed

		ldstr "Elapsed : {0} ms."
		ldloc elapsed
		box [mscorlib]System.Int32
		call void [mscorlib]System.Console::WriteLine(string, object)

		ret
	}

}


.data RandomData = bytearray (
	25 4e d7 23 b9 f8 fb ef 
	4e c2 b6 23 a9 16 d1 2a 
	eb f6 89 78 3f 35 26 c0 
	21 43 82 4c 27 f0 b2 cb 
	70 d7 ed b6 fb e6 80 cf 
	c9 f7 d5 72 9e e8 31 02 
	d1 25 61 8b ff f4 eb 3e 
	f9 6b 69 bd 15 7a d6 cd 
	08 9e bd 58 d4 32 59 25 
	43 51 76 b8 db 4c 32 61 
	68 69 ee ea e7 eb 33 a1 
	ac 2d dc 9c 43 58 b9 df 
	79 c1 61 71 4e d8 3a b0 
	5b 9a 8b 12 7a d9 b5 b1 
	76 ec 6b 75 5b b9 dc 92 
	18 88 43 3c 38 11 51 a3 
	4b c3 ff f0 79 c9 d2 86 
	b1 89 9f 47 b0 4f fb e6 
	ae be 8f 35 0d f8 0d 2f 
	56 12 92 5c e3 9e c7 55 
	b5 e3 c0 2f 6d f1 fd 97 
	e0 c5 bb c0 25 59 1a e9 
	92 3b d8 63 16 b3 4c 3f 
	45 f1 f3 84 08 b1 b6 1b 
	38 01 6e 78 85 60 34 c2 
	ce 5f f1 db 48 bf eb 40 
	85 37 b7 73 b9 0d 23 a0 
	76 cd 03 fc 5a 6c b3 44 
	9f 41 ca 06 e7 ea ed bb 
	85 52 0b 53 1d ee 02 c7 
	57 6f 9e d4 22 49 0c b8 
	b1 73 05 c2 9c 2d a6 3c 
	69 b8 d0 f3 74 26 87 f0 
	b7 6c d3 c3 0f a4 b5 c9 
	f8 1b 58 c5 c3 04 4c f6 
	8e 19 a2 22 47 af db 27 
	fa 97 b5 be a0 5d 1d e5 
	4b cc cc e2 66 f3 a6 2a 
	e3 cb 5a 3c 68 a1 d9 91 
	d5 8a c4 3a 94 d9 cb 63 
	f3 63 2c 65 c9 b6 06 e2 
	61 81 31 03 00 14 27 c7 
	36 7b 5b 84 1f 7b 2a f3 
	96 8d 9f 2d d1 43 84 1d 
	d8 67 43 43 4d 49 2a db 
	43 4d 77 19 a0 7f 84 fd 
	87 e6 a4 94 bf be 69 b7 
	5d 1f 87 0e 7e 21 23 bc 
	f7 9c 2b 1b 70 1a 03 2c 
	60 28 53 19 c1 73 c7 4d 
	8a b4 85 58 d9 a4 ce 4c 
	27 08 a9 2e 0e 72 b0 6d 
	1f 78 88 31 e5 39 5f d6 
	ce 4d 84 e5 33 a7 7c 7f 
	8e f9 68 98 0b 9e e9 5f 
	f0 96 62 98 11 6e 3b 11 
	4e bb a2 ea d0 dd d9 a0 
	73 ce 62 c1 8d bd 50 63 
	1d f6 73 dc b9 21 04 6b 
	77 fd 7c 8e e5 58 de 46 
	32 39 48 f6 63 12 86 bd 
	3f 3b 0d 0a 35 3f 43 50 
	31 d2 b4 71 60 18 42 00 
	37 d3 c5 99 70 04 7f 9d 
	ae a4 98 32 33 90 9b 14 
	52 fc 41 a4 9f 2f 74 07 
	36 70 19 bd 46 39 4d 80 
	cf 9b 0a 11 36 67 62 c2 
	bb ac 15 a0 54 7d e1 24 
	de 30 c5 a2 f8 11 45 ed 
	af fd a5 0a 36 6d 31 15 
	6d 0a 3b e0 a6 3c 51 c1 
	6e 55 d8 62 cf cd 25 64 
	72 a8 cd 1b 51 9a 94 c8 
	75 64 41 e8 57 68 89 9c 
	49 10 14 2f 39 4b de 0c 
	ea b5 d0 df 64 6c c1 84 
	07 55 d9 90 be 92 62 9b 
	f8 17 14 40 61 5c ca 4a 
	88 2d a1 b2 fc 2f 4e a0 
	7d f9 32 38 31 00 01 73 
	eb fa 88 18 7a 02 d4 24 
	02 c6 cc a3 4f 1f 7c b5 
	bf e5 43 44 c0 1e 17 f2 
	4c 1d 0c c8 7c a6 68 87 
	cb 97 9a 83 b7 5c d9 a1 
	e6 a3 4e d9 96 8e e4 13 
	f8 1e 57 66 fe d0 f9 5a 
	6f 9b ec 78 70 dc a5 11 
	4b a5 0a 4d 65 94 b2 09 
	cd f1 04 39 44 b2 dc af 
	de 3d 92 35 21 09 c6 f4 
	c2 c2 aa 56 16 ed d9 a8 
	e4 e3 c4 73 f2 54 a5 19 
	8d cc a8 c1 93 a4 99 77 
	19 b7 76 e6 9c 23 aa 5b 
	b7 52 c6 c9 d2 9d a8 eb 
	3e e2 73 07 1f 79 b6 20 
	aa 75 96 5d 3d 93 a8 f4 
	f4 dd fd 92 57 5a 32 3e 
	d1 63 ed 94 d9 b4 6b 90 
	c5 90 a2 12 93 73 da 01 
	66 f0 79 9b e3 c1 74 53 
	19 d8 6a 2a eb 25 78 3d 
	7b 6c ac 2f 68 68 91 0e 
	5e 73 c5 94 cf a0 93 7d 
	bf ea b5 b9 e7 02 cd 26 
	c1 50 32 47 89 5e 77 21 
	12 94 e6 af ec 7d f9 49 
	30 93 9d 9c 37 a2 1e 1a 
	20 da 1e 16 ad 66 f5 31 
	f5 33 81 fe cf b4 74 48 
	c0 1b 2f 4a 73 ee f8 28 
	53 47 b0 38 05 84 11 23 
	cb 7d e9 66 05 ce 3b d0 
	09 f7 b3 39 75 71 54 62 
	a6 3b dd ec 81 3e cf 92 
	3c 1e 49 3d 8e 1f 89 6b 
	5b aa 9c 2b 04 79 b7 65 
	90 9d 71 30 86 b9 12 ac 
	0d 01 50 55 f3 86 c1 6c 
	ca 0d 14 1b 29 92 19 9f 
	3a 9e d6 ac 15 5a 6e 56 
	07 51 95 0b 5c ea e0 ee 
	f2 2b 13 d0 16 db 3e c3 
	0b 8e 1e 2c 8d d1 32 2e 
	1e 27 ec 6e 45 0d 2f 6d 
	09 f5 fa 7f 61 7f 78 48 
	eb 22 4b ad 7b 62 c7 45 
	19 9f 23 a7 74 0e 79 97 
	b6 33 74 49 34 ee 1b 61 
	7d be 8d d7 2f 6f 94 c1 
	49 35 06 13 c3 1b 5a 51 
	8f 52 02 9c 22 64 75 8f 
	77 f3 80 ac 34 f1 d4 1c 
	90 b0 69 f1 e9 88 27 01 
	4f 10 0e 5f e6 9a 80 e6 
	aa 63 12 6f c6 dc 9d 71 
	47 7e 1c b0 48 cf b7 73 
	e7 fb e7 dc ad 54 71 43 
	7f 7c 75 7a 22 54 93 65 
	b1 90 b4 7f 89 87 f3 71 
	27 e6 b5 ea be 70 e3 8c 
	5a 5e a1 cb 50 6e 76 b5 
	ae bd 32 57 82 7f 9f 22 
	78 85 49 3c 60 30 c3 2e 
	24 25 2e e4 df 69 f5 2e 
	0f 89 a8 cf bd 14 4b e7 
	09 cb 67 38 e9 8e 0e 4a 
	63 0c ce 63 f0 7c 82 8d 
	9a a3 65 b9 02 c9 e1 3b 
	ec 8a c6 db 57 8b 1f a3 
	4a 67 22 66 01 5b c5 a9 
	25 40 80 c4 43 4b d5 55 
	ef 2b 44 a7 84 f0 a3 5d 
	1a fd a2 f3 9e cf c6 ca 
	47 72 95 04 47 b2 08 b6 
	fa a0 9a a8 ee 26 8c 4c 
	35 32 1a df 7f 62 c4 5e 
	6f c6 f3 94 d3 e4 1a df 
	68 6b 62 bf d4 04 3d 71 
	41 dc b8 c3 27 e1 fd 8f 
	6f b0 59 0f bf c2 c4 35 
	22 65 a3 7e 3c 30 b6 33 
	a3 38 f8 2c 52 e7 eb 05 
	b2 e7 f0 a5 16 d4 12 94 
	cf b7 8a df 5e 86 78 70 
	0b 94 d5 87 dc 90 bb ab 
	b8 c2 ca 04 74 17 02 a3 
	7c 82 76 a3 41 c7 1e 3f 
	13 be a8 e5 29 aa 9b cd 
	27 ef 3e f6 83 bf e4 e5 
	67 37 a4 a8 e5 6f 90 c2 
	a8 f9 39 64 60 3b d7 07 
	38 2a d3 d1 38 15 67 3b 
	e8 2f 49 17 12 6f af e5 
	3b d2 ab ba 65 8d b5 c7 
	57 58 a7 72 83 ad 48 01 
	8e e1 fc 61 80 eb 1d d8 
	39 54 7e 10 12 a3 6d fc 
	29 67 57 69 fd 71 35 28 
	1d fe cd fa ba 34 ce 3f 
	15 8f 5a 5b b9 22 78 40 
	63 15 85 5e 7a 09 ba 6d 
	16 c1 8d a6 59 01 7f 5b 
	b0 26 96 8e fb 0b 5b bc 
	f2 2a b5 f3 8c 62 cf b6 
	2e 02 d4 0d 0b 63 2e 08 
	8c 67 59 23 b7 84 27 04 
	37 b9 e6 8e f9 3a a9 46 
	62 b2 d8 82 50 3d 88 50 
	37 b9 fd 85 5c d8 45 0a 
	19 d5 6b 8a b0 58 c9 d9 
	9b e9 62 d8 3f 3a c7 24 
	0e 64 70 06 0b 69 fc 5a 
	3e c5 7f 63 0a 44 97 a1 
	c6 dd f9 4e d0 f6 8a bd 
	25 5c 01 93 6b 69 eb 04 
	3f 13 05 8e 0d 04 7d e4 
	f3 90 a6 56 19 d5 82 81 
	13 f6 65 9c 52 0d f4 df 
	a0 56 08 ad 72 94 d5 59 
	ed cb 7f 74 32 54 84 df 
	64 55 ca 22 69 b9 16 d9 
	9b 01 5a 3b dd d5 8a db 
	31 f6 78 67 26 aa 86 be 
	6d 2a d1 46 6c c5 b1 96 
	55 e2 76 a8 e1 2b 0b 60 
	01 79 8c 5e 74 39 69 bb 
	b8 9c 4d 54 9a 91 e0 cb 
	8b 13 d6 b6 3f 2a e4 f9 
	48 02 a5 d8 60 00 1c ae 
	d3 fb cb 5c de 2c 53 2d 
	d1 2c 8d ba 3a cb 7f 8b 
	2a b6 22 7b 27 0d ed b4 
	70 1b 32 1a 25 65 89 9f 
	30 b9 ec 5d 4b eb 08 6c 
	a2 05 98 f1 ed cc ad 63 
	f6 90 ba 66 d2 9b ef 32 
	2a f0 98 ec 64 60 24 e0 
	a4 9a 8e 10 1a 15 82 5f 
	d9 cd ed bf b2 e1 0b 5c 
	02 d8 51 90 c6 d5 88 39 
	6e 46 24 27 cf a7 90 bd 
	36 84 12 80 a8 07 62 d4 
	12 ab c7 1b 29 7a 21 3d 
	9b 03 13 f4 f1 c8 66 f4 
	eb 10 0c a6 2c 86 8e 0b 
	90 a8 f7 dc 87 d6 c1 69 
	f9 5b 9d 8a b1 b4 a6 63 
	15 57 6f b5 cb 84 e6 a7 
	7b 68 96 97 c9 fc 48 e7 
	c6 cd e8 35 46 57 87 0a 
	0c ac fb d8 40 69 b5 e8 
	4a 9b f6 74 51 8e 0d 1c 
	74 47 92 2c 6a 0e 3c 68 
	8a e1 2f 38 ee 01 82 71 
	48 c3 02 d7 23 a2 01 99 
	51 b2 00 00 17 0e 70 0f 
	aa 6d 25 48 da e2 43 5e 
	9f 29 8d bd 22 75 58 ba 
	3c 57 4b da df 5b b5 c6 
	f9 7a 1d f8 15 a0 6b 74 
	44 c1 7a f3 7d c5 8b 19 
	a5 09 f0 87 0a 3a 9a c0 
	15 77 ee 25 2f 6f ab b8 
	ab d2 a1 d5 7b 3e ec 59 
	ef 6e 42 26 86 94 da e2 
	5a 56 02 c7 29 80 d9 98 
	ea c2 9d 72 8c 64 44 8b 
	1a 1a dd dc 9f 14 0a 2f 
	70 f0 9e d2 ad 74 1f a1 
	a7 a3 70 13 c0 05 c2 b3 
	24 09 da 04 7c 88 17 07 
	2e e7 0d f8 f5 38 f3 73 
	f7 98 2c 7b 35 4d 55 c8 
	7f a5 09 c2 99 45 fc 64 
	3a a6 30 ca 15 6e 38 29 
	a5 e2 73 d7 30 b4 7a e8 
	24 07 50 38 2d d2 72 73 
	e1 15 9e c9 b1 93 6e 3a 
	9d 79 cf 8a e1 00 49 2a 
	c4 4b ea d8 7d dd c7 61 
	6b 7f 86 88 53 36 5a 39 
	52 0a 3c 26 8c 63 2c 94 
	b7 7b 42 1a e7 ef 77 27 
	10 d5 5d 49 49 44 ba 4e 
	c4 42 ee f7 cb 76 bd 37 
	c9 f7 a7 83 d9 ac f9 7b 
	62 de 3d 7b 3e be 6a 2b 
	31 0b 9f 18 4e b5 d2 98 
	1f 80 b4 60 21 10 f2 24 
	fd 99 3d a3 46 3a 93 66 
	f6 76 db 5a 5c d0 1a da 
	dd fe d8 3d 81 0d 10 1c 
	88 57 4b f1 d6 bd 54 62 
	bd 32 40 8b 1d 01 86 b5 
	cd 26 aa 58 cf a8 ed 9a 
	c0 fd 7f 8c 86 93 ac f3 
	95 19 ca 41 ba 3a b4 88 
	29 8b 1b 5f c3 04 7a e5 
	53 55 d9 a7 b6 f5 00 37 
	bc ce 37 d2 69 bc e6 8b 
	12 91 d0 18 4f 12 af 23 
	98 ed c0 3d ad 59 12 a6 
	6c 9c 64 35 05 cc dd f3 
	7f 7e 46 2d 97 e4 0a 42 
	23 cc d9 c6 f0 7e 3c 47 
	ae 95 41 d3 bc e2 75 7a 
	f3 66 cd 29 7a 0a 46 39 
	64 77 32 3a a5 d3 ec 8a 
	f4 ca 1d c7 56 2a b8 ad 
	47 76 c1 92 47 bc ed 9f 
	4a 6a 0a 10 d9 8a d8 49 
	4e 9f 38 14 0b 5d 5f b5 
	da 0a 34 e2 65 c5 b7 65 
	a7 94 d3 cd 22 7d c9 cf 
	9f 08 b0 71 35 11 26 9c 
	2f 5f f6 7c ab ac 0d 2f 
	3f 1b 30 bd 48 da 11 32 
	23 db 60 14 2c 68 99 68 
	7c 73 d8 5e a3 3f 25 5b 
	8d be 7e 52 e1 33 aa 63 
	29 67 29 7b 38 34 fd a1 
	ae b8 a8 eb f8 fc 4c 2c 
	4c 30 90 b5 f2 5e 74 0b 
	7a f6 7b 65 c6 e5 54 6c 
	d0 0b 54 90 c3 ee df 60 
	3f 33 84 fd 70 ef 42 f5 
	24 fa b1 94 ca 17 27 f2 
	3d 97 be 8c 92 49 47 b9 
	ee 00 0f 9c 5b ac f8 08 
	8e f2 25 72 6d 2a ba 4f 
	0a 28 16 d2 9e bf e3 a0 
	81 0f c9 d5 9c 49 27 0b 
	91 d6 e6 a1 c3 0e 48 d8 
	40 80 ad 4b e8 37 ba 2c 
	74 21 15 7c 90 88 1c 90 
	99 72 8f 4b ec 64 49 2c 
	7d 06 fe cb 59 f6 82 7a 
	fe be 8c 64 5b 82 7c b7 
	79 a0 56 37 9c 37 d0 0c 
	df 5c 12 a3 84 f3 8b 06 
	f2 2d b4 94 c7 2f 7e 1a 
	0e 84 1f 64 33 91 f8 f1 
	dc 76 ae b1 7c bb bd 26 
	b9 de 1d e3 d7 f7 e0 cb 
	5d 24 12 7a 08 a3 76 b3 
	24 f1 e3 a1 dd ec 53 4f 
	0b 56 05 97 da f6 6a 0f 
	c7 34 d9 97 d3 ca 3b d3 
	f0 a0 8c 68 88 12 7d c1 
	5e 89 a5 00 0b 9e ba 71 
	2c 59 2b 12 a2 28 17 01 
	8c 84 20 d7 08 7e 10 fa 
	ac 0b 5e 8d a4 9f 46 6b 
	69 d4 0c dc 75 69 cb 55 
	e0 ea dc 93 75 66 cc b4 
	a5 1e 2e 30 94 e2 82 5e 
	7d ef 64 46 44 d4 2d cf 
	b8 b8 cd 23 c2 af f6 7a 
	da fa 97 9e c7 3e be 81 
	23 93 a9 3b da 06 d8 65 
	a0 7f 9b f4 e9 85 40 57 
	76 e1 fa bf bd 3a c8 97 
	e2 51 87 10 da fd ac 36 
	65 a9 08 a8 ba 3a 86 99 
	64 4f e4 d8 55 bd 55 d5 
	92 1e 58 94 ed 95 36 91 
	f5 0a 33 9d 7f 9e dc 92 
	5e 6e 6a 17 0b 5a 77 0d 
	02 db 21 43 3c 66 00 16 
	df 82 4a 6d 37 9e d9 94 
	bd 33 9d 68 a7 7a f4 c2 
	ab b2 f3 78 53 16 f0 c1 
	51 bd 13 d7 14 1c b4 5e 
	67 55 de 32 22 7f 82 8a 
	b2 fa 8c 8a ee 2a b8 db 
	35 3b fe c9 cd 2b 34 c4 
	59 2b 40 91 ec 53 38 13 
	f3 66 fc 18 3f 47 7d bf 
	d3 bd 57 4a 66 0b 6f 9e 
	bd 2a e5 6d 1d ed bc e1 
	30 94 cb 8b fb de 54 a0 
	8e 21 42 25 6f 9d 78 44 
	be 92 4a a4 96 78 78 7c 
	a9 39 39 42 27 11 60 29 
	98 fe e8 4e 9a 97 af 1e 
	3d 70 fb f6 91 e8 54 97 
	d7 26 b8 99 37 bc fc 26 
	97 a5 f3 6d f9 58 b5 e0 
	d9 86 94 c3 eb f7 b7 66 
	04 71 5d 47 90 89 a6 5c 
	e8 28 34 de 39 37 b7 5b 
	c2 aa 84 05 b3 16 df 6b 
	52 0b 98 03 16 dc 9a a0 
	95 08 8b 39 6b 5e 7b 4e 
	a0 94 f0 94 d6 cc c2 b8 
	96 7d 03 e8 34 bb 8b 39 
	54 66 19 af 06 d1 39 71 
	44 c4 62 9c 55 f3 9e b8 
	ce 68 a8 d6 d1 4c 24 21 
	01 55 df 60 11 44 bf c0 
	39 69 d2 af fe ea ef 5d 
	48 04 5e 93 73 f1 f7 c8 
	8e 15 5e 9b e4 07 5b 7a 
	f3 ab b3 50 55 dc 8f 5f 
	cd 2f 4a 5c f7 96 8a b7 
	84 f8 15 72 7c af 08 8a 
	f3 85 37 a0 7b 57 6f 87 
	dd be aa 70 f4 f9 50 73 
	ba 3b 11 65 b2 05 89 6c 
	a0 90 c3 06 d6 c2 dd ce 
	47 b4 93 82 49 56 3f 3c 
	3b f8 f9 7c 79 d6 9f 19 
	d8 81 22 4e c3 13 00 33 
	8f 3f 37 c8 83 d2 97 c2 
	a2 2a e6 b2 03 15 97 b4 
	79 ba 35 42 13 02 a6 63 
	2c 4e d1 54 67 47 9e ca 
	0e 70 f7 b2 0b 86 ac 17 
	2b 20 cb 97 c8 72 7c 98 
	11 28 55 e9 73 e8 58 c4 
	67 26 c1 7e 21 0c c5 95 
	fd 8f 58 b9 f6 71 24 00 
	0d 12 8f 5d 43 63 fa a6 
	52 04 3e e4 16 c9 cf cd 
	f6 76 ad 6b 96 94 e2 54 
	6f b8 b3 2d bf d5 8e 09 
	bf f6 82 4c 10 f9 50 2d 
	d4 14 36 93 69 de 20 b4 
	89 6e 71 42 3a 96 4e 9f 
	4f 0b 6d fb f9 79 92 2d 
	c6 ea c6 e1 f8 ee 0c c4 
	38 21 06 de 4b e9 93 97 
	b5 ec 8e 22 59 1d d1 3b 
	13 08 76 b5 dc ac f6 94 
	b9 06 0e 68 9d 95 0b 73 
	e2 44 8c 8e ed c9 f3 a8 
	e3 9e de 4c 18 82 5c f2 
	24 15 90 9f 41 e1 fc 4a 
	89 89 7e 3d 75 9c 5b 7d 
	0a 43 51 bb 9f 03 27 05 
	aa 8d d2 a5 03 0a 3e f5 
	3e d9 bc 0b 94 ec 7e 3f 
	2f 57 6e 70 e5 5c 0c d4 
	32 42 2d e0 b2 da f5 24 
	29 a0 59 07 4e ad 61 83 
	bc ce 4e c9 cb 5b 82 4e 
	c9 b4 63 20 b0 2e f0 ac 
	fc 1e 26 90 90 bb 94 c1 
	60 2c 68 b0 5f de 12 74 
	22 5a 62 b7 63 ed d9 bf 
	db 46 63 0f b6 0c c8 aa 
	95 2d c3 03 1d 09 c9 db 
	6b 9b d8 69 b6 17 39 60 
	33 ab d7 fc 53 40 84 09 
	c8 ad 7d d5 9c 32 30 a8 
	f2 3c 24 e9 a0 7b 40 77 
	25 3f 24 e6 b8 d1 67 49 
	12 9e fa 87 01 83 9f 36 
	88 3f 0f a9 11 48 be 61 
	74 36 81 13 df 6e 3a b4 
	71 32 29 7d c3 00 4d 57 
	53 4c f4 be 67 2c 91 d0 
	d2 69 ea d4 50 4e c7 57 
	86 95 37 bf e3 ce 70 d4 
	06 0d 23 b7 6d 30 a0 98 
	1e 23 d6 d6 a7 a1 e5 47 
	71 4f 1e 20 d7 36 6c d4 
	37 e3 91 cc d3 c8 af 08 
	a2 ea e7 d4 3c 3f 0f 92 
	1a e4 20 c5 b1 7f 5e 80 
	d7 0d 1f 73 e7 cd f8 18 
	70 1d ea d4 39 57 65 b9 
	ea d2 7f 9c 52 c6 0f b8 
	c5 83 be 87 09 dd bd 37 
	9b 09 e2 47 a2 26 8b 35 
	3e ce 7b 44 bb aa a1 cb 
	67 66 d8 53 4a 80 e9 7a 
	15 87 00 3e b9 0f ad 6c 
	c5 83 a7 90 a6 3f 22 73 
	e4 e5 50 3f 42 0a 4b ab 
	d9 9f 16 f1 d8 49 20 b1 
	73 d6 d3 d7 ef 6f b6 f9 
	72 ab e3 b2 ee 06 dd ee 
	0d 0a 1e 48 e0 b1 95 10 
	fc 4e cd 10 e0 e4 f6 4a 
	61 6a 0c b1 9c 3c 2c 89 
	a4 a2 2e 14 42 03 1f 7c 
	9e c8 83 a4 a8 fd 66 f3 
	60 44 bc f0 87 f2 42 38 
	22 79 b3 38 02 9c 67 4a 
	9d 6a 49 3d a5 16 eb 09 
	f6 6e 53 4e 96 81 18 80 
	d1 27 d5 5f eb 0d f5 0f 
	8e 1b 45 e4 f8 03 1c 7d 
	fd 8d b6 f5 45 1d e3 a9 
	0a 1c 8e 26 9d a2 34 e3 
	94 b4 7c a1 de 1b 42 14 
	47 8c 8b 07 21 1c 79 d0 
	fe b5 ec 60 33 94 df 9a 
	b4 a4 c1 51 a5 1c 75 75 
	80 f0 a6 44 c2 d6 d0 07 
	3e f8 26 83 95 3a a7 8d 
	a7 9e e6 a6 36 82 6f bc 
	f7 b3 22 7e 0f b5 c6 e2 
	83 ba 5a 7a dc b3 51 b1 
	b9 19 d8 3c 3c 27 e8 41 
	b6 0d 26 88 1f a6 31 10 
	fa 95 13 fb ee 09 dc 8f 
	48 d6 cc ab c0 34 df 67 
	26 7c 98 f9 31 f2 4b d7 
	26 a1 a1 d4 1f 61 62 df 
	82 61 65 99 3d 75 57 75 
	56 2f 70 08 95 35 4c 10 
	e2 58 ca 36 78 73 db 46 
	4c 18 54 6d 2e e7 de 0a 
	31 fa bc ec 88 52 08 6b 
	74 16 d2 b5 b7 46 3f 1d 
	d2 81 f8 2f 51 88 27 d1 
	61 4b d1 28 60 2b 3a c2 
	9a a1 da ed b3 14 3d a7 
	a1 b7 58 ac 13 e6 99 69 
	ef 48 c4 76 ac 0f b9 0b 
	69 e5 62 db 27 e3 a0 53 
	21 05 99 65 c2 b8 c4 6c 
	c7 25 6a 42 fa 7f 78 76 
	da e6 87 e4 02 d0 0e 6a 
	28 61 59 29 87 d6 d8 61 
	5c f9 39 4d 69 d8 39 6b 
	75 72 b0 3f 30 b3 4c f9 
)
