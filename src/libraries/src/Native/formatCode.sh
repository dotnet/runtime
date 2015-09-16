#!/usr/bin/env bash

# OS X names clang-format as clang-format; Ubuntu uses
# clang-format-version so check for the right one
if which "clang-format-3.6" > /dev/null 2>&1 ; then
   export CF="$(which clang-format-3.6)"
elif which "clang-format" > /dev/null 2>&1 ; then
   export CF="$(which clang-format)"
else
   echo "Unable to find clang-format"
   exit 1
fi

for D in */; do
    for file in "${D}"*.cpp; do
        if [ -e $file ] ; then
            $CF -style=file -i "$file"
        fi
    done
   
    for file in "${D}"*.h ; do
        if [ -e $file ] ; then
            $CF -style=file -i "$file"
        fi
    done

   for file in "${D}"*.in ; do
       if [ -e $file ] ; then
            $CF -style=file -i "$file"
       fi
   done
done
