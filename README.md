# Orang <img align="left" src="images/icon48.png">

Orang is a cross-platform command-line tool for:

* [searching](https://josefpihrt.github.io/docs/orang/cli/commands/copy) files, directories and files' content,
* [replacing](https://josefpihrt.github.io/docs/orang/cli/commands/replace) files' content,
* [copying](https://josefpihrt.github.io/docs/orang/cli/commands/copy) files and directories,
* [moving](https://josefpihrt.github.io/docs/orang/cli/commands/move) files and directories,
* [renaming](https://josefpihrt.github.io/docs/orang/cli/commands/rename) files and directories,
* [deleting](https://josefpihrt.github.io/docs/orang/cli/commands/delete) files, directories or its content,
* [synchronizing](https://josefpihrt.github.io/docs/orang/cli/commands/sync) content of two directories,
* [spellchecking](https://josefpihrt.github.io/docs/orang/cli/commands/spellcheck) files' content,
* executing [Regex](https://docs.microsoft.com/cs-cz/dotnet/api/system.text.regularexpressions.regex?view=netcore-3.0) functions such as [match](https://josefpihrt.github.io/docs/orang/cli/commands/match) or [split](https://josefpihrt.github.io/docs/orang/cli/commands/split)

All these commands are powered with [.NET regular expression engine](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expressions).

## Requirements

Orang requires [.NET Core Runtime](https://dotnet.microsoft.com/download/dotnet-core/current/runtime) 3.1 or higher.

## How to Install

Orang is distributed as a [.NET Core global tool](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools). To install Orang run:

```
dotnet tool install -g orang.dotnet.cli
```

To install non-alpha version run:

```
dotnet tool install -g orang.dotnet.cli --version <VERSION>
```

To update Orang run:

```
dotnet tool update -g orang.dotnet.cli
```

## How to Use

```
orang [command] [parameters]
```

## Basic Commands

* [find](https://josefpihrt.github.io/docs/orang/cli/commands/find)
* [replace](https://josefpihrt.github.io/docs/orang/cli/commands/replace)
* [rename](https://josefpihrt.github.io/docs/orang/cli/commands/rename)
* [delete](https://josefpihrt.github.io/docs/orang/cli/commands/delete)
* [spellcheck](https://josefpihrt.github.io/docs/orang/cli/commands/spellcheck)

For a list of all commands please see [Orang Command-Line Reference](https://josefpihrt.github.io/docs/orang/cli/commands)

## How to Learn

For a full list of commands, parameters and parameter values run:

```
orang help [command] [-v d]
```

For a full manual run:

```
orang help -m [-v d]
```

For a full list of .NET regular expressions syntax run:

```
orang list-patterns
```

## Features

### Single match can span over multiple lines

Orang supports matches across multiple lines.

### Dry run

The option `-d, --dry-run` gives you opportunity to see the results before you actually replace, rename or delete anything.

### Display match and replacement side-by-side

The option `-t, --highlight` with values `m[atch] r[eplacement]` gives you opportunity to see the match and the replacement side-by-side in the output.

### Use C# code to compute replacements

Use `-r, --replacement <EXPRESSION> cs[harp]` syntax to specify C# inline expression.
The expression is considered to be expression-body of a method with signature `string M(Match match)`

Use `-r, --replacement <CODE_FILE_PATH> cs[harp] f[rom-file]` syntax to specify C# code file.
This code file must contain public method with signature `string M(Match match)`.

### Load pattern from a file

The more complicated a pattern is, the less readable it becomes when written in one line.

```
orang find --content "(?x)(?<=(\A|\.)\s*)\p{Ll}\w+\b"
```

The option `f[rom-file]` gives you opportunity to store pattern in a file where it can be formatted.

```
orang find --content "pattern.txt" from-file
```
or
```
orang find -c "pattern.txt" f
```

Note: Replacement string can be store in a file as well.

### Sample Command

Goal: Capitalize first character of a word at the beginning of the text or at the beginning of a sentence.

File `pattern.txt` has following content:

```
(?x)      # set multiline option
(?<=      # is preceded with
  (\A|\.) # beginning of text or a dot
  \s*     # zero or more white-space characters
)
\p{Ll}    # lowercase letter
\w+       # one or more word characters
\b        # word boundary (between word and non-word character)
```

```
orang replace ^
 --extension txt ^
 --content "pattern.txt" from-file ^
 --replacement "char.ToUpper(match.Value[0]) + match.Value.Substring(1)" csharp ^
 --highlight match replacement ^
 --display path=omit summary ^
 --dry-run
```
or
```
orang replace -e txt -c "pattern.txt" f -r "char.ToUpper(match.Value[0]) + match.Value.Substring(1)" cs -t m r -y p=o su -d
```

![Capitalize first character in a sentence](/images/CapitalizeFirstCharInSentence.png)

## Links

* [Regular Expression Language - Quick Reference](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference)
* [.NET Regular Expressions](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expressions)
* [Regular Expression Options](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-options)
