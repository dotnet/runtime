#!/bin/bash

# Get symbols using nm
nm $1 > symbols.txt

# Strip _$ and demangle
cat symbols.txt | awk '{print $3}' | sed 's/_$//g' | xargs -I {} xcrun swift-demangle {} > demangled_symbols.txt

echo "Demangled symbols are exported to demangled_symbols.txt."