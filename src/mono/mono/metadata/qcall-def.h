/**
 * \file
 * This file contains the default set of the mono qcalls.
 * Each type that has qcall methods must be declared here
 * with the FCClassElement macro as follows:
 *
 * 	FCClassElement(class_name, namespace, symbol_name)
 *  where symbol_name is an array of MonoQCallFunc.
 *
 * FCClassElements have to be sorted by name then namespace, 
 * but that the functions in each one can be in any order, but 
 * have to end with a func_flag_end_of_array (0x01) entry.
 **/

FCClassElement("", "", NULL)
