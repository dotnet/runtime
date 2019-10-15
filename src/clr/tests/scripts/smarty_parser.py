#!/usr/bin/env python
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
## See the LICENSE file in the project root for more information.
#
##
# Title               :smarty_parser.py
#
# Notes:
#
# Simple class to parse through the smarty .smrt files and individual output
# files.
#
# Expects:
#
# Smarty results directory:
#
#   --Smarty.run.xx
#     |
#     |---
#        |
#        |--- Smarty.run.xx.fail.smrt (optional, if there are failures)
#        |--- Smarty.run.xx.pass.smrt (optional, if there are passes)
#        |--- Smarty.rpt.xx.html
#        |--- Smarty.rpt.xx.passed.html
#        |--- Smarty.xml
#        |--- Smrt00000xx
#             |
#             |---
#                |
#                |---Tests.lst_<test_name>.cmd_xx.x.y.html
#                |---Tests.lst_<test_name>.cmd_xx.x.y.txt
#    
################################################################################

from collections import defaultdict
import os
import re
import unittest
import sys

################################################################################

class SmartyParser:

   def __init__(self, path):
      if not os.path.isdir(path):
         raise Exception("Expected a valid path to parse through")

      self.m_path = path
      self.m_missing = []
      self.m_tests = []
      self.m_passed = []
      self.m_failed = []
 
   def parse(self):
      files = os.listdir(self.m_path)
      
      failed_smrt_files = []
      passed_smrt_files = []
      
      for file in files:
         if "fail.smrt" in file:
            failed_smrt_files.append(file)
         elif "pass.smrt" in file:
            passed_smrt_files.append(file)
            
      def parse_smrt_files(smrt_list):
         test_list = []
      
         for smrt_file in smrt_list:
            lines = None
            with open(os.path.join(self.m_path, smrt_file)) as file_handle:
               lines = file_handle.readlines()
            
            lines = "\n".join(lines)
            lines = lines.split("[TESTS]")

            tests = lines[1].split("Tests.lst=")[1:]
            
            test_names = []
            for test in tests:
               split = test.split(",")
               
               dir = split[3].strip()
               test_name = split[0].strip()
               test_name = "cmd_".join(test_name.split("cmd"))
            
               test_names.append((dir, test_name))
            
            for test in test_names:
               test_list.append(test)
               
         return test_list
      
      failed_tests = parse_smrt_files(failed_smrt_files)
      passed_tests = parse_smrt_files(passed_smrt_files)
      
      cached_ls = defaultdict(lambda: None)
 
      def iterate_tests(test_list):
         local_tests = []
 
         for test in test_list:
            smrt_dir = test[0]
            
            if cached_ls[smrt_dir] is None:
               cached_ls[smrt_dir] = os.listdir(os.path.join(self.m_path, smrt_dir))
               ds = defaultdict(lambda: [])
               file_names = cached_ls[smrt_dir]
               
               for file_name in file_names:
                  split = file_name.split(".result")
                  ds[split[0]].append(".result".join(split))
                  
               cached_ls[smrt_dir] = ds
            
            result_files = cached_ls[test[0]]["Tests.lst_" + test[1]]
            
            if len(result_files) == 0:
               self.m_missing.append(test[0])

            else:
               for file in result_files:  
                  if os.path.splitext(file)[1] == ".html":
                     result = self.parse_smarty_file(os.path.join(self.m_path, test[0], file))
                     
                     local_tests.append(result)
                     self.m_tests.append(result)
                     
      self.m_failed = iterate_tests(failed_tests)
      self.m_passed = iterate_tests(passed_tests)

   def parse_smarty_file(self, path):
      lines = self.remove_tags(path)

      tags = defaultdict(lambda: False)

      tags["TEST_IDENTIFIER"] = True
      tags["CATEGORIES"] = True
      tags["RELATIVEPATH"] = True
      tags["WORKINGDIR"] = True
      tags["TEST_CMD_LINE"] = True
      tags["TEST_EXPECTED_RETURN_CODE"] = True
      tags["TEST_ACTUAL_RETURN_CODE"] = True
      tags["TEST_START_TIME"] = True
      tags["TEST_END_TIME"] = True
      tags["TEST_RESULT"] = True
      tags["TEST_OUTPUT"] = True

      capturing_output = False

      for line in lines:
         if "TEST OUTPUT" in line:
            capturing_output = True
            tags["TEST_OUTPUT"] = []

         elif capturing_output is True:
            if "TEXT_EXPECTED_RETURN_CODE" in line:
               capturing_output = False
               tags["TEST_OUTPUT"] = "\n".join(tags["TEST_OUTPUT"])

            else:
               tags["TEST_OUTPUT"].append(line)

         elif "=" in line:
            split = line.split(" = ")

            if tags[split[0]] is True:
               value = "=".join(split[1:])

               tags[split[0]] = value.strip()

         elif ":" in line:
            # TEST_CMD_LINE does not use =
            split = line.split(": ")

            if tags[split[0]] is True:
               tags[split[0]] = ":".join(split[1:])

      return tags        

   def remove_tags(self, path):
      tag_re = re.compile(r'<[^>]+>')
      lines = []

      with open(path) as file_handle:
         for line in file_handle:
            line = tag_re.sub('', line)
            
            # Smarty has a bug such that </BODY will
            # possible be missing the ending >
            # Check for this and remove the tag if found.

            if "</BODY" in line and "</BODY>" not in line:
               line = line.replace("</BODY", "")

            line = line.replace("\r\n", "")

            if len(line) != 0:
               lines.append(line)
      
      return lines
