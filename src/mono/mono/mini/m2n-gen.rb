cookies = [
  "V",
  "VI",
  "VII",
  "VIII",
  "VIIII",
  "VIIIII",
  "VIIIIII",
  "I",
  "II",
  "III",
  "IIII",
  "IIIII",
  "IIIIII",
  "IIIIIII",
  "IIIIIIIII",
  "L",
  "LL",
  "LI",
  "LIL",
  "LILII",
  "DD",
  "DDD",
  "VIF",
  "VIFF",
  "VIFFFF",
  "VIFFFFFI",
  "FF",
  "DI",
  "FI",
  "IIL",
  "IILI",
  "IILLLI",
  "LII",
  "VID",
  "VIIIIIII",
  "VILLI",

  "DID",
  "DIDD",
  "FIF",
  "FIFF",
  "LILL",
  "VIL",
]

def type_to_sig_type(c)
  return "void" if c == 'V'
  return "int" if c == "I"
  return "gint64" if c == "L"
  return "float" if c == "F"
  return "double" if c == "D"
  raise "can't handle " + c
end

class EmitCtx
  def initialize()
    @iarg = 0
    @farg = 0
  end

  def emit(c)
    if c == "I"
      @iarg += 1
      return "(int)margs->iargs [#{@iarg - 1}]"
    end
    if c == "F"
      @farg += 1
      return "*(float*)&margs->fargs [FIDX (#{@farg - 1})]"
    end
    if c == "L"
      @iarg += 2
      return "get_long_arg (margs, #{@iarg - 2})"
    end
    if c == "D"
      @farg += 1
      return "margs->fargs [FIDX (#{@farg - 1})]"
    end

    raise "IDK how to handle " + c
  end
end

cookies.each { |c|
  puts "static void"
  puts "wasm_invoke_#{c.downcase} (void *target_func, InterpMethodArguments *margs)"
  puts "{"

  print "\t", type_to_sig_type(c[0]), " (*func)("
  c.each_char().drop(1).each_with_index {|p, i| 
    print ", " if i > 0
    print type_to_sig_type(p), " arg_#{i}"
  }

  #generate a correct signature if the function takes no arguments
  print "void" if c.length == 1

  puts ") = target_func;"

  puts "\n"
  
  ctx = EmitCtx.new

  print "\t"
  if c[0] != "V"
    print type_to_sig_type(c[0]), " res = "
  end

  print "func (";
  c.each_char().drop(1).each_with_index {|p, i| 
    print ", " if i > 0
    print ctx.emit(p)
  }
  puts ");"

  if c[0] != "V"
    print "\t*(", type_to_sig_type(c[0]), "*)margs->retval = res;\n"
  end  

  puts "\n}"
  puts ""
}


puts "static void\nicall_trampoline_dispatch (const char *cookie, void *target_func, InterpMethodArguments *margs)"
puts "{"
cookies.each_with_index { |c, i| 
  print "\t"
  print "else " unless i == 0
  printf "if (!strcmp (\"#{c}\", cookie))\n"
  puts "\t\twasm_invoke_#{c.downcase } (target_func, margs);"
}
puts "\telse {"
puts "\t\tprintf (\"CANNOT HANDLE COOKIE %s\\n\", cookie);"
puts "\t\tg_assert (0);"
puts "\t}"
puts "}"