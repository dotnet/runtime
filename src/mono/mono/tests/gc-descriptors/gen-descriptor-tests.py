#!/usr/bin/env python3

from __future__ import print_function
from optparse import OptionParser
import sys

parser = OptionParser ()
parser.add_option ("--switch", action = "store_true", dest = "switch")
parser.add_option ("--one-method-if", action = "store_true", dest = "one_method_if")

(options, args) = parser.parse_args ()

def print_file (file_name):
    f = open (file_name, "r")
    for line in f:
        sys.stdout.write (line + " ")
        sys.stdout.flush ()
    f.close ()

print_file ("descriptor-tests-prefix.cs")

print ("public struct NoRef1 { int x; }")
for i in range (1, 17):
    print ("public struct NoRef{0} {{ NoRef{1} a, b; }}".format (1 << i, 1 << (i-1)))

print ("")

names = []

max_offset = 512
max_bitmap = 257

for offset in range (0, max_offset, 19):
    for bitmap in range (0, max_bitmap):
        name = "Bitmap{0}Skip{1}".format (bitmap, offset)
        names.append (name)
        print ("public struct {0} : Filler {{".format (name))
        for i in range (0, 16):
            bit = 1 << i
            if offset & bit:
                print ("  NoRef{0} skip{1};".format (bit, bit))
        for i in range (0, 9):
            bit = 1 << i
            if bitmap & bit:
                print ("  object ref{0};".format (i))
            else:
                print ("  int bit{0};".format (i))
        print ("  public void Fill (object[] refs) {")
        for i in range (0, 9):
            bit = 1 << i
            if bitmap & bit:
                print ("    ref{0} = refs [{1}];".format (i, i))
        print ("  }")
        print ("}")
        print ("public class {0}Wrapper : Filler {{".format (name))
        print ("  {0}[] a;".format (name))
        print ("  public {0}Wrapper () {{".format (name))
        print ("    a = new {0} [1];".format (name))
        print ("  }")
        print ("  public void Fill (object[] refs) {")
        print ("    a [0].Fill (refs);")
        print ("  }")
        print ("}\n")

def search_method_name (left, right):
    return "MakeAndFillL{0}R{1}".format (left, right)

def gen_new (name):
    print ("Filler b;")
    print ("if (wrap)")
    print ("  b = new {0}Wrapper ();".format (name))
    print ("else")
    print ("  b = new {0} ();".format (name))
    print ("b.Fill (refs); return b;")

def gen_binary_search_body (left, right, one_method):
    if left + 1 >= right:
        gen_new (names [left])
    else:
        mid = (left + right) // 2
        print ("if (which < {0}) {{".format (mid))
        if one_method:
            gen_binary_search_body (left, mid, one_method)
        else:
            print ("return {0} (which, refs, wrap);".format (search_method_name (left, mid)))
        print ("} else {")
        if one_method:
            gen_binary_search_body (mid, right, one_method)
        else:
            print ("return {0} (which, refs, wrap);".format (search_method_name (mid, right)))
        print ("}")

def gen_binary_search (left, right, one_method):
    name = search_method_name (left, right)
    print ("public static Filler {0} (int which, object[] refs, bool wrap) {{".format (name))
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
        print ("      case {0}: {{".format (i))
        gen_new (names [i])
        print ("}")
    print ("      default: return null;")
    print ("    }")
    print ("  }")
else:
    method_name = gen_binary_search (0, len (names), options.one_method_if)
    print ("  public static Filler MakeAndFill (int which, object[] refs, bool wrap) {")
    print ("    if (which >= {0}) return null;".format (len (names)))
    print ("    return {0} (which, refs, wrap);".format (method_name))
    print ("  }")
print ("  public const int NumWhich = {0};".format (len (names)))
print ("}")

print ("")

print_file ("descriptor-tests-driver.cs")
