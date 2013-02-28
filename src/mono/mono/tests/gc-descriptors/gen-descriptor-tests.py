#!/usr/bin/env python

def print_file (file_name):
    f = open (file_name, "r")
    for line in f:
        print line,
    f.close ()

print_file ("descriptor-tests-prefix.cs")

print "public struct NoRef1 { int x; }"
for i in range (1, 17):
    print "public struct NoRef%d { NoRef%d a, b; }" % (1 << i, 1 << (i-1))

print ""

names = []

max_offset = 512
max_bitmap = 512

for offset in range (0, max_offset, 19):
    for bitmap in range (0, max_bitmap):
        name = "Bitmap%dSkip%d" % (bitmap, offset)
        names.append (name)
        print "public class %s : Filler {" % name
        for i in range (0, 16):
            bit = 1 << i
            if offset & bit:
                print "  NoRef%d skip%d;" % (bit, bit)
        for i in range (0, 9):
            bit = 1 << i
            if bitmap & bit:
                print "  object ref%d;" % i
            else:
                print "  int bit%d;" % i
        print "  public void Fill (object[] refs) {"
        for i in range (0, 9):
            bit = 1 << i
            if bitmap & bit:
                print "    ref%d = refs [%d];" % (i, i)
        print "  }"
        print "}\n"

def search_method_name (left, right):
    return "MakeAndFillL%dR%d" % (left, right)

def gen_binary_search (left, right):
    name = search_method_name (left, right)
    print "public static Filler %s (int which, object[] refs) {" % name
    if left + 1 >= right:
        print "var b = new %s (); b.Fill (refs); return b;" % (names [left])
    else:
        mid = (left + right) / 2
        print "if (which < %d) {" % mid
        print "return %s (which, refs);" % search_method_name (left, mid)
        print "} else {"
        print "return %s (which, refs);" % search_method_name (mid, right)
        print "}"
    print "}"
    if left + 1 < right:
        gen_binary_search (left, mid)
        gen_binary_search (mid, right)
    return name

print "public class Bitmaps {"
method_name = gen_binary_search (0, len (names))
print "  public static Filler MakeAndFill (int which, object[] refs) {"
print "    if (which >= %d) return null;" % len (names)
print "    return %s (which, refs);" % method_name
print "  }"
print "  public const int NumWhich = %d;" % len (names)
print "}"

print ""

print_file ("descriptor-tests-driver.cs")
