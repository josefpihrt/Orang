﻿---
sidebar_position: 0
sidebar_label: Orang Command-line Tool
---

# Orang Command\-line Tool <img align="left" src="../../images/icon48.png" />

Search, replace, rename and delete files and its content using the power of \.NET regular expressions\.

## Commands

| Command                                  | Description                                                                                           |
| ---------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| [copy](Commands/copy.md)                 | Searches the file system for files and directories and copy them to a destination directory\.         |
| [delete](Commands/delete.md)             | Deletes files and directories\.                                                                       |
| [find](Commands/find.md)                 | Searches the file system for files and directories and optionally searches files' content\.           |
| [help](Commands/help.md)                 | Displays help\.                                                                                       |
| [move](Commands/move.md)                 | Searches the file system for files and directories and move them to a destination directory\.         |
| [regex escape](Commands/regex-escape.md) | Escapes special characters by replacing them with their escape codes\.                                |
| [regex list](Commands/regex-list.md)     | Lists regular expression patterns\.                                                                   |
| [regex match](Commands/regex-match.md)   | Searches the input string for occurrences of the regular expression\.                                 |
| [regex split](Commands/regex-split.md)   | Splits the input string into an list of substrings at the positions defined by a regular expression\. |
| [rename](Commands/rename.md)             | Renames files and directories\.                                                                       |
| [replace](Commands/replace.md)           | Searches the file system for files and replaces its content\.                                         |
| [spellcheck](Commands/spellcheck.md)     | Searches the files' content for potential misspellings and typos\.                                    |
| [sync](Commands/sync.md)                 | Synchronizes content of two directories\.                                                             |


## Exit Code

Code | Comment
--- | ---
0 | Match found
1 | No match found
2 | Error occurred or execution canceled

## Redirected/Piped Input

Redirected/piped input will be used either as a raw text or as a list of paths separated with newlines.

Command | Piped Input
--- | ---
[copy](Commands/copy.md) | list of paths
[delete](Commands/delete.md) | list of paths
[find](Commands/find.md) | text (default) or list of paths when `--pipe p[aths]` is specified
[move](Commands/move.md) | list of paths
[regex escape](Commands/regex-escape.md) | text
[regex match](Commands/regex-match.md) | text
[regex split](Commands/regex-split.md) | text
[rename](Commands/rename.md) | list of paths
[replace](Commands/replace.md) | text (default) or list of paths when `--pipe p[aths]` is specified
[sync](Commands/sync.md) | list of paths

## Multi-value Parameters

A lot of Orang parameters can have multiple values. Only shortcoming of this approach is that
a user cannot specify argument (usually path(s)) as a last value of a command
if the argument is preceded with multi-value parameter.

Following command is invalid because path `C:/Documents` is treated as a value of multi-value parameter `-c | --content`.
```
orang find -c "^abc" i m "C:/Documents"
```

To fix this problem you can either add parameter `--paths`
```
orang find -c "abc" i m --paths "C:/Documents"
```

or you can specify path right after the command name:

```
orang find "C:/Documents" -c "abc" i m
```

## Links

* [List of Option Values](OptionValues.md)
* [How To](HowTo.md)

## External Links

* [.NET Core Global Tools Overview](https://docs.microsoft.com/dotnet/core/tools/global-tools)
* [Create a .NET Core Global Tool Using the .NET Core CLI](https://docs.microsoft.com/dotnet/core/tools/global-tools-how-to-create)
* [.NET Core 2.1 Global Tools](https://natemcmaster.com/blog/2018/05/12/dotnet-global-tools/)
* [Windows CMD Shell](https://ss64.com/nt/syntax.html)
* [Parsing C++ Command-Line Arguments](https://docs.microsoft.com/cpp/cpp/parsing-cpp-command-line-arguments?view=vs-2019)

*\(Generated with [DotMarkdown](http://github.com/JosefPihrt/DotMarkdown)\)*