#!/usr/bin/env python
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
## See the LICENSE file in the project root for more information.
#
##
# Title               :smarty_error_parser.py
#
# Notes:
#
# Python script to parse the smarty.fail.x.xml file. It will exit with the
# correct exit code if it found errors. It will also print the tests that
# failed. Note, this is only used by the CI.
#
################################################################################

import sys

################################################################################
# Main
################################################################################

if __name__ == "__main__":

   xml_output = None

   try:

      with open(sys.argv[1]) as file_handle:
         xml_output = file_handle.readlines()

         try:
            # only one line.
            xml_output = xml_output[0]

         except:
            print "Error, no results communicated. Infrastructure problem."
            sys.exit(1)
   except:
      print "Error, no results communicated. Infrastructure problem."
      sys.exit(1)
   
   try:
      # If at any time there are things missing then the test passes!

      if (xml_output == "empty"):
         sys.exit(0)

      xml_output = xml_output.split("[TESTS]")

      tests = xml_output[1].split("Tests.lst=")[1:]
      tests = [test.split("#")[1].split("CATS")[0] for test in tests]

      categories = xml_output[0]

      print "Test Failures."
      print

      for test in tests:
         print test

      sys.exit(1)

   except:

      raise


