# Metetron.FileParser
Provides a framework for parsing files.

It handles checking the file system periodically for changes.
The logic for parsing must be implmented by the user.
Files are processed via Hangfire Background Jobs and files are copied to a working directory before they are processed.
