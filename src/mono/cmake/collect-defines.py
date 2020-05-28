#!/usr/bin/env python3
import sys
import re

defines={}
for filename in sys.argv[1:]:
    with open (filename, 'r') as f:
        for line in f:
            line = line.strip ()
            if '#ifdef' in line or '#ifndef' in line or '#elif' in line or '#if' in line:
                line = line.replace ('defined ', 'defined').replace ('\t', ' ')
                parts = line.split (' ')
                if '#ifdef' in line or '#ifndef' in line:
                    defines [parts [1]] = 1
                else:
                    for part in parts[1:]:
                        match = re.search ('defined\(([a-zA-Z0-9_]+)\)', part)
                        if match != None:
                            define = match.group (1)
                            defines [define] = 1
                        else:
                            match = re.search ('[a-zA-Z_][a-zA-Z0-9_]+', part)
                            if match != None:
                                define = match.group (0)
                                defines [define] = 1

for define in defines.keys ():
    if define.startswith ("HAVE_"):
        print (define)
