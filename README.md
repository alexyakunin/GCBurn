# GCBurn
Garbage collection / allocation performance tests for C# / .NET and Go.

## Description and results

Please see [Go vs C#, part 2: Garbage Collection](https://medium.com/servicetitan-engineering/go-vs-c-part-2-garbage-collection-9384677f86f1).

Other useful links:
- Raw [test-all](https://github.com/alexyakunin/GCBurn/blob/master/src/test-all) output for a set of machines can be found in the ["results" folder](https://github.com/alexyakunin/GCBurn/blob/master/results)
- The [spreadsheet summarizing the most important metrics](https://docs.google.com/spreadsheets/d/1DeWpvmIBl95aRe30m9toQRvMZ5M9Nu-oy6aa5B3o6bw/edit?usp=sharing).

## Running GCBurn

Prerequisites:
1. Install .NET Core SDK: https://www.microsoft.com/net/download
2. Install Go: https://golang.org/doc/install?download

To run a single test, use `run` or `Run.bat` scripts; `--help` option shows all other options you can use.

To run a sequence of tests, use `test-all` or `Test-All.bat` scripts, and likely, that's the way you want to run it. There are two options:
- `-o OUTPUT_SUFFIX_STRING` changes the names of its output files from `*-Default.txt` to `*-OUTPUT_SUFFIX_STRING.txt`
- `-d DURATION_IN_SECONDS` sets the duration of a single GCBurn test pass. The default duration is 2 minutes - we've found it's almost always enough to catch long Gen2 GC pauses on this test; besides that, setting it to larger values tends to crash Go more reliably on "**Static set = 50+% RAM**" test cases.

## Contributing

If you are willing to translate the test to another language (e.g. Java) and share your findings, it would be simply amazing. Please [contact me on Facebook](https://www.facebook.com/alexander.yakunin) if you need any help with this.
