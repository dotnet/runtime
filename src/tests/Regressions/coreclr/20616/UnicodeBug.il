.assembly extern mscorlib { }

.assembly UnicodeBug { }

.method static int32 main()
{
  .entrypoint

  //
  // These encode to 2 UTF-8 bytes
  //

  // A single Cyrillic character (matching case)
  ldstr bytearray (3B 04 ) // л
  ldc.i4.1
  ldc.i4.1
  call class [mscorlib]System.Type class [mscorlib]System.Type::GetType(string, bool, bool)
  pop

  // A single Cyrillic character (mismatched case)
  ldstr bytearray (1B 04 ) // Л
  ldc.i4.1
  ldc.i4.1
  call class [mscorlib]System.Type class [mscorlib]System.Type::GetType(string, bool, bool)
  pop

  // "Common language runtime" in Russian
  ldstr "общеязыковаяиспОлняющаясреда" // "O" in the middle is capital"
  ldc.i4.1
  ldc.i4.1
  call class [mscorlib]System.Type class [mscorlib]System.Type::GetType(string, bool, bool)
  pop

  // MEETEI MAYEK LETTER KOK
  ldstr bytearray (C0 AB ) // ꯀ
  ldc.i4.1
  ldc.i4.1
  call class [mscorlib]System.Type class [mscorlib]System.Type::GetType(string, bool, bool)
  pop

  ldstr bytearray (41 FF ) // ａ
  ldc.i4.1
  ldc.i4.1
  call class [mscorlib]System.Type class [mscorlib]System.Type::GetType(string, bool, bool)
  pop

  ldstr bytearray (42 FF 35 FF 27 FF ) // ｂＵＧ
  ldc.i4.1
  ldc.i4.1
  call class [mscorlib]System.Type class [mscorlib]System.Type::GetType(string, bool, bool)
  pop

  //
  // These encode to 4 UTF-8 bytes
  //

  //
  // These actually don't test case-insensitiveness because we don't support that,
  // but at least make sure we can find them using the case-insensitive code path
  // case-sensitively.
  //

  // A single character from the Supplementary unicode plane
  ldstr bytearray (01 D8 28 DC ) // 𐐨
  ldc.i4.1
  ldc.i4.1
  call class [mscorlib]System.Type class [mscorlib]System.Type::GetType(string, bool, bool)
  pop

  // Two characters from the Supplementary unicode plane
  ldstr bytearray (01 D8 29 DC 01 D8 2A DC ) // 𐐩𐐪
  ldc.i4.1
  ldc.i4.1
  call class [mscorlib]System.Type class [mscorlib]System.Type::GetType(string, bool, bool)
  pop

  // Two characters from the Supplementary unicode plane with a standard in between
  ldstr bytearray (01 D8 2A DC 61 00 01 D8 2A DC ) // 𐐪a𐐪
  ldc.i4.1
  ldc.i4.1
  call class [mscorlib]System.Type class [mscorlib]System.Type::GetType(string, bool, bool)
  pop

  // Two characters from the Supplementary unicode plane with z standard in between
  ldstr bytearray (01 D8 2A DC 7A 00 01 D8 2A DC ) // 𐐪z𐐪
  ldc.i4.1
  ldc.i4.1
  call class [mscorlib]System.Type class [mscorlib]System.Type::GetType(string, bool, bool)
  pop

  ldc.i4 100
  ret
}

.class 'л' { }
.class 'Общеязыковаяисполняющаясреда' { } // "O" at the beginning is capital
.class 'ꯀ' { }
.class 'Ａ' { }
.class 'Ｂｕｇ' { }
.class '𐐨' { }
.class '𐐩𐐪' { }
.class '𐐪A𐐪' { }
.class '𐐪Z𐐪' { }