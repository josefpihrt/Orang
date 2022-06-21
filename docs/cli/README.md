﻿# Orang Command\-Line Interface <img align="left" src="../../images/icon48.png">

Search, replace, rename and delete files and its content using the power of \.NET regular expressions\.

## Commands

| Command                                 | Description                                                                                           |
| --------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| [copy](copy-command.md)                 | Searches the file system for files and directories and copy them to a destination directory\.         |
| [delete](delete-command.md)             | Deletes files and directories\.                                                                       |
| [find](find-command.md)                 | Searches the file system for files and directories and optionally searches files' content\.           |
| [help](help-command.md)                 | Displays help\.                                                                                       |
| [move](move-command.md)                 | Searches the file system for files and directories and move them to a destination directory\.         |
| [regex escape](regex-escape-command.md) | Escapes special characters by replacing them with their escape codes\.                                |
| [regex list](regex-list-command.md)     | Lists regular expression patterns\.                                                                   |
| [regex match](regex-match-command.md)   | Searches the input string for occurrences of the regular expression\.                                 |
| [regex split](regex-split-command.md)   | Splits the input string into an list of substrings at the positions defined by a regular expression\. |
| [rename](rename-command.md)             | Renames files and directories\.                                                                       |
| [replace](replace-command.md)           | Searches the file system for files and replaces its content\.                                         |
| [spellcheck](spellcheck-command.md)     | Searches the files' content for potential misspellings and typos\.                                    |
| [sync](sync-command.md)                 | Synchronizes content of two directories\.                                                             |


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
[copy](copy-command.md) | list of paths
[delete](delete-command.md) | list of paths
[escape](escape-command.md) | text
[find](find-command.md) | text (default) or list of paths when `--pipe p[aths]` is specified
[match](match-command.md) | text
[move](move-command.md) | list of paths
[rename](rename-command.md) | list of paths
[replace](replace-command.md) | text (default) or list of paths when `--pipe p[aths]` is specified
[split](split-command.md) | text
[sync](sync-command.md) | list of paths

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