// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#import <Foundation/Foundation.h>

//---------------------------------------------------------------------------------------
//
// get_managed_args: converts arguments passed to the Objective-C program to their
// C-representation so they can be passed to the managed side.
// The caller is responsible for freeing up the allocated memory. 
// This can be achieved by calling the accompanied 'free_managed_args' function.
//
// Arguments:
// 	* managed_args_array - pointer to array of strings to hold converted arguments.
//
// Return Value:
//	int - number of arguments (size of the array of string)
//
size_t get_managed_args (char*** managed_args_array)
{
    id args_array = [[NSProcessInfo processInfo] arguments];
    size_t args_count = [args_array count];
    assert (args_count <= 128);
    *managed_args_array = (char**) malloc (sizeof(char*) * args_count);
    size_t argi;
    for (argi = 0; argi < args_count; argi++) {
        NSString* arg = [args_array objectAtIndex: argi];
        const char* cstring = [arg UTF8String];
        size_t cstring_len = strlen(cstring) + 1;
        (*managed_args_array)[argi] = (char*) malloc (sizeof(char) * cstring_len);
        strcpy((*managed_args_array)[argi], cstring);
    }
    return argi;
}

//---------------------------------------------------------------------------------------
//
// free_managed_args: frees up the allocated memory for the program arguments.
//
// Arguments:
// 	* managed_args_array - pointer to array of strings which converted program arguments
// 	* array_size - number of arguments (size of the array of string)
//
void free_managed_args (char*** managed_args_array, size_t array_size)
{
    if (*managed_args_array != NULL)
    {
        for (size_t i = 0; i < array_size; i++)
            free((*managed_args_array)[i]);
        free(*managed_args_array);
    }
}
