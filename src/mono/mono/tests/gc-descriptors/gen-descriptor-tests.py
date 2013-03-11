#!/usr/bin/env python

from optparse import OptionParser

parser = OptionParser ()
parser.add_option ("--switch", action = "store_true", dest = "switch")
parser.add_option ("--one-method-if", action = "store_true", dest = "one_method_if")

(options, args) = parser.parse_args ()

def print_file (file_name):
    f = open (file_name, "r")
    for line in f:
        print (line),
    f.close ()

print_file ("descriptor-tests-prefix.cs")

print ("public struct NoRef1 { int x; }")
for i in range (1, 17):
    print ("public struct NoRef%d { NoRef%d a, b; }" % (1 << i, 1 << (i-1)))

print ("")

names = []

max_offset = 512
max_bitmap = 257

for offset in range (0, max_offset, 19):
    for bitmap in range (0, max_bitmap):
        name = "Bitmap%dSkip%d" % (bitmap, offset)
        names.append (name)
        print ("public struct %s : Filler {" % name)
        for i in range (0, 16):
            bit = 1 << i
            if offset & bit:
                print ("  NoRef%d skip%d;" % (bit, bit))
        for i in range (0, 9):
            bit = 1 << i
            if bitmap & bit:
                print ("  object ref%d;" % i)
            else:
                print ("  int bit%d;" % i)
        print ("  public void Fill (object[] refs) {")
        for i in range (0, 9):
            bit = 1 << i
            if bitmap & bit:
                print ("    ref%d = refs [%d];" % (i, i))
        print ("  }")
        print ("}")
        print ("public class %sWrapper : Filler {" % name)
        print ("  %s[] a;" % name)
        print ("  public %sWrapper () {" % name)
        print ("    a = new %s [1];" % name)
        print ("  }")
        print ("  public void Fill (object[] refs) {")
        print ("    a [0].Fill (refs);")
        print ("  }")
        print ("}\n")

def search_method_name (left, right):
    return "MakeAndFillL%dR%d" % (left, right)

def gen_new (name):
    print ("Filler b;")
    print ("if (wrap)")
    print ("  b = new %sWrapper ();" % name)
    print ("else")
    print ("  b = new %s ();" % name)
    print ("b.Fill (refs); return b;")

def gen_binary_search_body (left, right, one_method):
    if left + 1 >= right:
        gen_new (names [left])
    else:
        mid = (left + right) // 2
        print ("if (which < %d) {" % mid)
        if one_method:
            gen_binary_search_body (left, mid, one_method)
        else:
            print ("return %s (which, refs, wrap);" % search_method_name (left, mid))
        print ("} else {")
        if one_method:
            gen_binary_search_body (mid, right, one_method)
        else:
            print ("return %s (which, refs, wrap);" % search_method_name (mid, right))
        print ("}")

def gen_binary_search (left, right, one_method):
    name = search_method_name (left, right)
    print ("public static Filler %s (int which, object[] refs, bool wrap) {" % name)
    gen_binary_search_body (left, right, one_method)
    print ("}")
    if not one_method and left + 1 < right:
        mid = (left + right) // 2
        gen_binary_search (left, mid, one_method)
        gen_binary_search (mid, right, one_method)
    return name

print ("public class Bitmaps {")
if options.switch:
    print ("  public static Filler MakeAndFill (int which, object[] refs, bool wrap) {")
    print ("    switch (which) {")
    for i in range (0, len (names)):
        print ("      case %d: {" % i)
        gen_new (names [i])
        print ("}")
    print ("      default: return null;")
    print ("    }")
    print ("  }")
else:
    method_name = gen_binary_search (0, len (names), options.one_method_if)
    print ("  public static Filler MakeAndFill (int which, object[] refs, bool wrap) {")
    print ("    if (which >= %d) return null;" % len (names))
    print ("    return %s (which, refs, wrap);" % method_name)
    print ("  }")
print ("  public const int NumWhich = %d;" % len (names))
print ("}")

print ("")

print_file ("descriptor-tests-driver.cs")
