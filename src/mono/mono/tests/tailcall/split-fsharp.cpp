//
// This program is used to split fsharp-deeptail.il into separate tests.
//
// Algorithm is just to split on the newline delimited ldstr opcodes
// within the main@ function, and generate file names from the ldstr,
// replacing special characters with underscores.
//
// git rm fsharp-deeptail-*
// rm fsharp-deeptail-*
// g++ split-fsharp.cpp  && ./a.out < fsharp-deeptail.il
// git add fsharp-deeptail/*
//
// Note This is valid C++98 for use with older compilers, where C++11 would be desirable.
#include <stdlib.h>
#include <iostream>
#include <fstream>
#include <vector>
#include <string>
#include <assert.h>
#include <string.h>
#include <set>
#include <stdio.h>
using namespace std;
#include <sys/stat.h>
#ifdef _WIN32
#include <direct.h>
#endif

typedef vector<string> strings_t;

struct test_t
{
	string name;
	strings_t content;
};

typedef vector<test_t> tests_t;

static void
CreateDir (const char *a)
{
#ifdef _WIN32
	_mkdir (a);
#else
	mkdir (a, 0777);
#endif
}

int main ()
{
	typedef set<string> names_t; // consider map<string, vector<string>> tests;
	names_t names;
	string line;
	strings_t prefix;
	tests_t tests;
	test_t test_dummy;
	test_t *test = &test_dummy;
	strings_t suffix;

	while (getline (cin, line))
	{
		prefix.push_back (line);
		if (line == ".entrypoint")
			break;
	}

	CreateDir ("tailcall");
	CreateDir ("tailcall/fsharp-deeptail");

	const string marker_ldstr = "ldstr \"";	// start of each test, and contains name
	const string marker_ldc_i4_0 = "ldc.i4.0";	// start of suffix

	while (getline (cin, line))
	{
		// tests are delimited by empty lines
		if (line.length() == 0)
		{
			if (tests.size() && tests.back().name.length())
			{
				names_t::iterator a = names.find(tests.back().name);
				if (a != names.end())
				{
					fprintf(stderr, "duplicate %s\n", a->c_str());
					exit(1);
				}
				names.insert(names.end(), tests.back().name);
			}
			tests.resize (tests.size () + 1);
			test = &tests.back ();
			assert (getline (cin, line));
			// tests start with ldstr
			//printf("%s\n", line.c_str());
			const bool ldstr = line.length () >= marker_ldstr.length () && memcmp(line.c_str (), marker_ldstr.c_str (), marker_ldstr.length ()) == 0;
			const bool ldc_i4_0 = line.length () >= marker_ldc_i4_0.length () && memcmp(line.c_str (), marker_ldc_i4_0.c_str (), marker_ldc_i4_0.length ()) == 0;
			assert (ldstr || ldc_i4_0);
			if (ldc_i4_0)
			{
				suffix.push_back (line);
				break;
			}
			string name1 = &line [marker_ldstr.length ()];
			*strchr ((char*)name1.c_str (), '"') = 0; // truncate at quote
			test->name = name1.c_str ();
#if 1
			// Change some chars to underscores.
			for (string::iterator c = test->name.begin(); c != test->name.end(); ++c)
				if (strchr("<>", *c))
					*c = '_';
#else
			// remove some chars
			while (true)
			{
				size_t c;
				if ((c = test->name.find('<')) != string::npos)
				{
					test->name = test->name.replace(c, 1, "_");
					continue;
				}
				if ((c = test->name.find('>')) != string::npos)
				{
					test->name = test->name.replace(c, 1, "_");
					continue;
				}
				if ((c = test->name.find("..")) != string::npos)
				{
					test->name = test->name.replace(c, 2, "_");
					continue;
				}
				if ((c = test->name.find("__")) != string::npos)
				{
					test->name = test->name.replace(c, 2, "_");
					continue;
				}
				if ((c = test->name.find('.')) != string::npos)
				{
					test->name = test->name.replace(c, 1, "_");
					continue;
				}
				break;
			}
#endif
			//printf("%s\n", test->name.c_str());
		}
		test->content.push_back (line);
	}
	while (getline (cin, line))
		suffix.push_back (line);

	for (tests_t::const_iterator t = tests.begin(); t != tests.end(); ++t)
	{
		if (t->name.length() == 0)
			continue;
		//printf("%s\n", t->name.c_str());
		FILE* output = fopen(("tailcall/fsharp-deeptail/" + t->name + ".il").c_str(), "w");
		for (strings_t::const_iterator a = prefix.begin(); a != prefix.end(); ++a)
			fprintf(output, "%s\n", a->c_str());
		fputs("\n", output);
		for (strings_t::const_iterator a = t->content.begin(); a != t->content.end(); ++a)
			fprintf(output, "%s\n", a->c_str());
		fputs("\n", output);
		for (strings_t::const_iterator a = suffix.begin(); a != suffix.end(); ++a)
			fprintf(output, "%s\n", a->c_str());
		fclose(output);
	}
#if 0
	for (names_t::const_iterator t = names.begin(); t != names.end(); ++t)
		fputs(("\ttailcall/fsharp-deeptail/" + *t + ".il \\\n").c_str(), stdout);
	for (names_t::const_iterator t = names.begin(); t != names.end(); ++t)
		fputs(("INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/" + *t + ".exe\n").c_str(), stdout);
	for (names_t::const_iterator t = names.begin(); t != names.end(); ++t)
		fputs(("#PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/" + *t + ".exe\n").c_str(), stdout);
	for (names_t::const_iterator t = names.begin(); t != names.end(); ++t)
		fputs(("PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/" + *t + ".exe\n").c_str(), stdout);
#endif
}
