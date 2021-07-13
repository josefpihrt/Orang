﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Orang.CommandLine.Help;
using Orang.Expressions;
using Orang.FileSystem;
using Orang.Text.RegularExpressions;
using static Orang.Logger;

namespace Orang.CommandLine
{
    internal static class ParseHelpers
    {
        public static bool TryParseHighlightOptions(
            IEnumerable<string> values,
            out HighlightOptions highlightOptions,
            HighlightOptions? defaultValue = null,
            OptionValueProvider? provider = null)
        {
            return TryParseHighlightOptions(
                values,
                out highlightOptions,
                defaultValue,
                default(ContentDisplayStyle?),
                provider);
        }

        public static bool TryParseHighlightOptions(
            IEnumerable<string> values,
            out HighlightOptions highlightOptions,
            HighlightOptions? defaultValue = null,
            ContentDisplayStyle? contentDisplayStyle = null,
            OptionValueProvider? provider = null)
        {
            if (contentDisplayStyle == ContentDisplayStyle.Value
                || contentDisplayStyle == ContentDisplayStyle.ValueDetail)
            {
                defaultValue = HighlightOptions.None;
            }

            if (values.Any())
            {
                string[] arr = values.ToArray();

                for (int i = 0; i < arr.Length; i++)
                {
                    switch (arr[i])
                    {
                        case "new-line":
                            {
                                LogHelpers.WriteObsoleteWarning($"Value '{arr[i]}' is obsolete. "
                                    + $"Use value '{OptionValues.HighlightOptions_Newline.HelpValue}' instead.");

                                arr[i] = OptionValues.HighlightOptions_Newline.Value;
                                break;
                            }
                        case "nl":
                            {
                                LogHelpers.WriteObsoleteWarning($"Value '{arr[i]}' is obsolete. "
                                    + $"Use value '{OptionValues.HighlightOptions_Newline.HelpValue}' instead.");

                                arr[i] = OptionValues.HighlightOptions_Newline.Value;
                                break;
                            }
                    }
                }

                values = arr;
            }

            return TryParseAsEnumFlags(
                values,
                OptionNames.Highlight,
                out highlightOptions,
                defaultValue: defaultValue,
                provider: provider ?? OptionValueProviders.HighlightOptionsProvider);
        }

        public static bool TryParseFileProperties(
            IEnumerable<string> values,
            string optionName,
            out FilterPredicate<DateTime>? creationTimePredicate,
            out FilterPredicate<DateTime>? modifiedTimePredicate,
            out FilterPredicate<long>? sizePredicate)
        {
            creationTimePredicate = null;
            modifiedTimePredicate = null;
            sizePredicate = null;

            foreach (string value in values)
            {
                Expression? expression = null;
                OptionValue? optionValue = null;

                try
                {
                    expression = Expression.Parse(value);

                    if (OptionValues.FileProperty_CreationTime.IsKeyOrShortKey(expression.Identifier))
                    {
                        optionValue = OptionValues.FileProperty_CreationTime;

                        creationTimePredicate = new FilterPredicate<DateTime>(
                            expression,
                            PredicateHelpers.GetDateTimePredicate(expression));
                    }
                    else if (OptionValues.FileProperty_ModifiedTime.IsKeyOrShortKey(expression.Identifier))
                    {
                        optionValue = OptionValues.FileProperty_ModifiedTime;

                        modifiedTimePredicate = new FilterPredicate<DateTime>(
                            expression,
                            PredicateHelpers.GetDateTimePredicate(expression));
                    }
                    else if (OptionValues.FileProperty_Size.IsKeyOrShortKey(expression.Identifier))
                    {
                        optionValue = OptionValues.FileProperty_Size;

                        if (expression.Kind == ExpressionKind.DecrementExpression)
                        {
                            WriteOptionError(value, optionName, HelpProvider.GetExpressionsText("  ", includeDate: false));
                            return false;
                        }

                        sizePredicate = new FilterPredicate<long>(expression, PredicateHelpers.GetLongPredicate(expression));
                    }
                    else
                    {
                        WriteOptionError(value, optionName, OptionValueProviders.FilePropertiesProvider);
                        return false;
                    }
                }
                catch (ArgumentException)
                {
                    if (expression != null
                        && optionValue != null)
                    {
                        WriteOptionValueError(
                            expression.Value,
                            optionValue,
                            HelpProvider.GetExpressionsText(
                                "  ",
                                includeDate: optionValue != OptionValues.FileProperty_Size));
                    }
                    else
                    {
                        WriteOptionError(value, optionName, HelpProvider.GetExpressionsText("  "));
                    }

                    return false;
                }
            }

            return true;
        }

        public static bool TryParseSortOptions(
            IEnumerable<string> values,
            string optionName,
            out SortOptions? sortOptions)
        {
            sortOptions = null;
            int maxCount = 0;

            List<string>? options = null;

            if (!values.Any())
                return true;

            foreach (string value in values)
            {
                int index = value.IndexOf('=');

                if (index >= 0)
                {
                    string key = value.Substring(0, index);
                    string value2 = value.Substring(index + 1);

                    if (OptionValues.MaxCount.IsKeyOrShortKey(key))
                    {
                        if (!TryParseCount(value2, OptionNames.Sort, out maxCount, value))
                            return false;
                    }
                    else
                    {
                        WriteOptionError(value, optionName, OptionValueProviders.SortFlagsProvider);
                        return false;
                    }
                }
                else
                {
                    (options ??= new List<string>()).Add(value);
                }
            }

            if (!TryParseAsEnumValues(
                options,
                optionName,
                out ImmutableArray<SortFlags> flags,
                provider: OptionValueProviders.SortFlagsProvider))
            {
                return false;
            }

            SortDirection direction = (flags.Contains(SortFlags.Descending))
                ? SortDirection.Descending
                : SortDirection.Ascending;

            List<SortDescriptor>? descriptors = null;

            foreach (SortFlags flag in flags)
            {
                switch (flag)
                {
                    case SortFlags.Name:
                        {
                            AddDescriptor(SortProperty.Name, direction);
                            break;
                        }
                    case SortFlags.CreationTime:
                        {
                            AddDescriptor(SortProperty.CreationTime, direction);
                            break;
                        }
                    case SortFlags.ModifiedTime:
                        {
                            AddDescriptor(SortProperty.ModifiedTime, direction);
                            break;
                        }
                    case SortFlags.Size:
                        {
                            AddDescriptor(SortProperty.Size, direction);
                            break;
                        }
                    case SortFlags.None:
                    case SortFlags.Ascending:
                    case SortFlags.Descending:
                    case SortFlags.CultureInvariant:
                        {
                            break;
                        }
                    default:
                        {
                            throw new InvalidOperationException($"Unknown enum value '{flag}'.");
                        }
                }
            }

            bool cultureInvariant = flags.Contains(SortFlags.CultureInvariant);

            if (descriptors != null)
            {
                sortOptions = new SortOptions(descriptors.ToImmutableArray(), cultureInvariant: cultureInvariant, maxCount: maxCount);
            }
            else
            {
                sortOptions = new SortOptions(
                    ImmutableArray.Create(new SortDescriptor(SortProperty.Name, direction)),
                    cultureInvariant: cultureInvariant,
                    maxCount: maxCount);
            }

            return true;

            void AddDescriptor(SortProperty p, SortDirection d)
            {
                (descriptors ??= new List<SortDescriptor>()).Add(new SortDescriptor(p, d));
            }
        }

        public static bool TryParseModifyOptions(
            IEnumerable<string> values,
            string optionName,
            EnumerableModifier<string>? modifier,
            [NotNullWhen(true)] out ModifyOptions? modifyOptions,
            out bool aggregateOnly)
        {
            modifyOptions = null;
            aggregateOnly = false;

            var sortProperty = ValueSortProperty.None;
            List<string>? options = null;

            foreach (string value in values)
            {
                int index = value.IndexOf('=');

                if (index >= 0)
                {
                    string key = value.Substring(0, index);
                    string value2 = value.Substring(index + 1);

                    if (OptionValues.SortBy.IsKeyOrShortKey(key))
                    {
                        if (!TryParseAsEnum(
                            value2,
                            optionName,
                            out sortProperty,
                            provider: OptionValueProviders.ValueSortPropertyProvider))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        WriteOptionError(value, optionName, OptionValueProviders.ModifyFlagsProvider);
                        return false;
                    }
                }
                else
                {
                    string value2 = value;

                    if (value2 == "ao")
                    {
                        LogHelpers.WriteObsoleteWarning(
                            $"Value '{value2}' is obsolete. Use value '{OptionValues.ModifyFlags_AggregateOnly.HelpValue}' instead.");

                        value2 = OptionValues.ModifyFlags_AggregateOnly.ShortValue;
                    }

                    (options ??= new List<string>()).Add(value2);
                }
            }

            var modifyFlags = ModifyFlags.None;

            if (options != null
                && !TryParseAsEnumFlags(
                    options,
                    optionName,
                    out modifyFlags,
                    provider: OptionValueProviders.ModifyFlagsProvider))
            {
                return false;
            }

            ModifyFlags except_Intersect_Group = modifyFlags & ModifyFlags.Except_Intersect_Group;

            if (except_Intersect_Group != ModifyFlags.None
                && except_Intersect_Group != ModifyFlags.Except
                && except_Intersect_Group != ModifyFlags.Intersect
                && except_Intersect_Group != ModifyFlags.Group)
            {
                WriteError($"Values '{OptionValues.ModifyFlags_Except.HelpValue}', "
                    + $"'{OptionValues.ModifyFlags_Intersect.HelpValue}' and "
                    + $"'{OptionValues.ModifyFlags_Group.HelpValue}' cannot be used at the same time.");

                return false;
            }

            var functions = ModifyFunctions.None;

            if ((modifyFlags & ModifyFlags.Distinct) != 0)
                functions |= ModifyFunctions.Distinct;

            if ((modifyFlags & ModifyFlags.Ascending) != 0)
                functions |= ModifyFunctions.Sort;

            if ((modifyFlags & ModifyFlags.Descending) != 0)
                functions |= ModifyFunctions.SortDescending;

            if ((modifyFlags & ModifyFlags.Except) != 0)
                functions |= ModifyFunctions.Except;

            if ((modifyFlags & ModifyFlags.Intersect) != 0)
                functions |= ModifyFunctions.Intersect;

            if ((modifyFlags & ModifyFlags.Group) != 0)
                functions |= ModifyFunctions.Group;

            if ((modifyFlags & ModifyFlags.RemoveEmpty) != 0)
                functions |= ModifyFunctions.RemoveEmpty;

            if ((modifyFlags & ModifyFlags.RemoveWhiteSpace) != 0)
                functions |= ModifyFunctions.RemoveWhiteSpace;

            if ((modifyFlags & ModifyFlags.TrimStart) != 0)
                functions |= ModifyFunctions.TrimStart;

            if ((modifyFlags & ModifyFlags.TrimEnd) != 0)
                functions |= ModifyFunctions.TrimEnd;

            if ((modifyFlags & ModifyFlags.ToLower) != 0)
                functions |= ModifyFunctions.ToLower;

            if ((modifyFlags & ModifyFlags.ToUpper) != 0)
                functions |= ModifyFunctions.ToUpper;

            if (sortProperty != ValueSortProperty.None
                && (functions & ModifyFunctions.Sort) == 0
                && (functions & ModifyFunctions.SortDescending) == 0)
            {
                functions |= ModifyFunctions.Sort;
            }

            aggregateOnly = (modifyFlags & ModifyFlags.AggregateOnly) != 0;

            if (modifyFlags != ModifyFlags.None
                || functions != ModifyFunctions.None
                || modifier != null)
            {
                modifyOptions = new ModifyOptions(
                    functions: functions,
                    aggregate: (modifyFlags & ModifyFlags.Aggregate) != 0 || aggregateOnly,
                    ignoreCase: (modifyFlags & ModifyFlags.IgnoreCase) != 0,
                    cultureInvariant: (modifyFlags & ModifyFlags.CultureInvariant) != 0,
                    sortProperty: sortProperty,
                    modifier: modifier);
            }
            else
            {
                modifyOptions = ModifyOptions.Default;
            }

            return true;
        }

        public static bool TryParseModifier(
            IEnumerable<string> values,
            string optionName,
            [NotNullWhen(true)] out EnumerableModifier<string>? modifier)
        {
            modifier = null;

            if (!values.Any())
                return false;

            string value = values.First();

            if (!TryParseAsEnumFlags(
                values.Skip(1),
                optionName,
                out ModifierOptions options,
                ModifierOptions.None,
                OptionValueProviders.ModifierOptionsProvider))
            {
                return false;
            }

            if ((options & ModifierOptions.FromFile) != 0
                && !FileSystemHelpers.TryReadAllText(value, out value!, ex => WriteError(ex)))
            {
                return false;
            }

            if ((options & ModifierOptions.FromDll) != 0)
            {
                return DelegateFactory.TryCreateFromAssembly(
                    value,
                    typeof(IEnumerable<string>),
                    typeof(IEnumerable<string>),
                    out modifier);
            }
            else if ((options & ModifierOptions.FromFile) != 0)
            {
                return DelegateFactory.TryCreateFromSourceText(
                    value,
                    typeof(IEnumerable<string>),
                    typeof(IEnumerable<string>),
                    out modifier);
            }
            else
            {
                return DelegateFactory.TryCreateFromExpression(
                    value,
                    "ModifierClass",
                    "ModifierMethod",
                    "IEnumerable<string>",
                    typeof(IEnumerable<string>),
                    "IEnumerable<string>",
                    typeof(IEnumerable<string>),
                    "items",
                    out modifier);
            }
        }

        public static bool TryParseReplaceOptions(
            IEnumerable<string> values,
            string optionName,
            string? replacement,
            MatchEvaluator? matchEvaluator,
            [NotNullWhen(true)] out ReplaceOptions? replaceOptions)
        {
            replaceOptions = null;
            var replaceFlags = ReplaceFlags.None;

            if (values != null
                && !TryParseAsEnumFlags(
                    values,
                    optionName,
                    out replaceFlags,
                    provider: OptionValueProviders.ReplaceFlagsProvider))
            {
                return false;
            }

            var functions = ReplaceFunctions.None;

            if ((replaceFlags & ReplaceFlags.TrimStart) != 0)
                functions |= ReplaceFunctions.TrimStart;

            if ((replaceFlags & ReplaceFlags.TrimEnd) != 0)
                functions |= ReplaceFunctions.TrimEnd;

            if ((replaceFlags & ReplaceFlags.ToLower) != 0)
                functions |= ReplaceFunctions.ToLower;

            if ((replaceFlags & ReplaceFlags.ToUpper) != 0)
                functions |= ReplaceFunctions.ToUpper;

            if (matchEvaluator != null)
            {
                replaceOptions = new ReplaceOptions(
                    matchEvaluator: matchEvaluator,
                    functions: functions,
                    cultureInvariant: (replaceFlags & ReplaceFlags.CultureInvariant) != 0);
            }
            else
            {
                replaceOptions = new ReplaceOptions(
                    replacement: replacement,
                    functions: functions,
                    cultureInvariant: (replaceFlags & ReplaceFlags.CultureInvariant) != 0);
            }

            return true;
        }

        public static bool TryParseDisplay(
            IEnumerable<string> values,
            string optionName,
            out ContentDisplayStyle? contentDisplayStyle,
            out PathDisplayStyle? pathDisplayStyle,
            out LineDisplayOptions lineDisplayOptions,
            out LineContext lineContext,
            out DisplayParts displayParts,
            out ImmutableArray<FileProperty> fileProperties,
            out string? indent,
            out string? separator,
            out bool noAlign,
            OptionValueProvider? contentDisplayStyleProvider = null,
            OptionValueProvider? pathDisplayStyleProvider = null)
        {
            contentDisplayStyle = null;
            pathDisplayStyle = null;
            lineDisplayOptions = LineDisplayOptions.None;
            lineContext = default;
            displayParts = DisplayParts.None;
            fileProperties = ImmutableArray<FileProperty>.Empty;
            indent = null;
            separator = null;
            noAlign = false;

            ImmutableArray<FileProperty>.Builder? builder = null;

            foreach (string value in values)
            {
                int index = value.IndexOf('=');

                if (index >= 0)
                {
                    string key = value.Substring(0, index);
                    string value2 = value.Substring(index + 1);

                    if (key == "t")
                    {
                        LogHelpers.WriteObsoleteWarning($"Value '{key}' is obsolete. "
                            + $"Use value '{OptionValues.Display_Context.HelpValue}' instead.");

                        key = OptionValues.Display_Context.ShortKey;
                    }
                    else if (key == "ta")
                    {
                        LogHelpers.WriteObsoleteWarning($"Value '{key}' is obsolete. "
                            + $"Use value '{OptionValues.Display_ContextAfter.HelpValue}' instead.");

                        key = OptionValues.Display_ContextAfter.ShortKey;
                    }
                    else if (key == "tb")
                    {
                        LogHelpers.WriteObsoleteWarning($"Value '{key}' is obsolete. "
                            + $"Use value '{OptionValues.Display_ContextBefore.HelpValue}' instead.");

                        key = OptionValues.Display_ContextBefore.ShortKey;
                    }

                    if (OptionValues.Display_Content.IsKeyOrShortKey(key))
                    {
                        if (!TryParseAsEnum(
                            value2,
                            optionName,
                            out ContentDisplayStyle contentDisplayStyle2,
                            provider: contentDisplayStyleProvider))
                        {
                            return false;
                        }

                        contentDisplayStyle = contentDisplayStyle2;
                    }
                    else if (OptionValues.Display_Path.IsKeyOrShortKey(key))
                    {
                        if (!TryParseAsEnum(
                            value2,
                            optionName,
                            out PathDisplayStyle pathDisplayStyle2,
                            provider: pathDisplayStyleProvider))
                        {
                            return false;
                        }

                        pathDisplayStyle = pathDisplayStyle2;
                    }
                    else if (OptionValues.Display_Indent.IsKeyOrShortKey(key))
                    {
                        indent = value2;
                    }
                    else if (OptionValues.Display_Separator.IsKeyOrShortKey(key))
                    {
                        separator = RegexEscape.ConvertCharacterEscapes(value2);
                    }
                    else if (OptionValues.Display_Context.IsKeyOrShortKey(key))
                    {
                        if (!TryParseCount(value2, OptionNames.Display, out int count, value))
                            return false;

                        lineContext = new LineContext(count);
                    }
                    else if (OptionValues.Display_ContextBefore.IsKeyOrShortKey(key))
                    {
                        if (!TryParseCount(value2, OptionNames.Display, out int before, value))
                            return false;

                        lineContext = lineContext.WithBefore(before);
                    }
                    else if (OptionValues.Display_ContextAfter.IsKeyOrShortKey(key))
                    {
                        if (!TryParseCount(value2, OptionNames.Display, out int after, value))
                            return false;

                        lineContext = lineContext.WithAfter(after);
                    }
                    else
                    {
                        WriteOptionError(value, optionName, OptionValueProviders.DisplayProvider);
                        return false;
                    }
                }
                else if (OptionValues.Display_Summary.IsValueOrShortValue(value))
                {
                    displayParts |= DisplayParts.Summary;
                }
                else if (OptionValues.Display_Count.IsValueOrShortValue(value))
                {
                    displayParts |= DisplayParts.Count;
                }
                else if (OptionValues.Display_CreationTime.IsValueOrShortValue(value))
                {
                    (builder ??= ImmutableArray.CreateBuilder<FileProperty>()).Add(FileProperty.CreationTime);
                }
                else if (OptionValues.Display_ModifiedTime.IsValueOrShortValue(value))
                {
                    (builder ??= ImmutableArray.CreateBuilder<FileProperty>()).Add(FileProperty.ModifiedTime);
                }
                else if (OptionValues.Display_Size.IsValueOrShortValue(value))
                {
                    (builder ??= ImmutableArray.CreateBuilder<FileProperty>()).Add(FileProperty.Size);
                }
                else if (OptionValues.Display_LineNumber.IsValueOrShortValue(value))
                {
                    lineDisplayOptions |= LineDisplayOptions.IncludeLineNumber;
                }
                else if (OptionValues.Display_TrimLine.IsValueOrShortValue(value))
                {
                    lineDisplayOptions |= LineDisplayOptions.TrimLine;
                }
                else if (OptionValues.Display_TrimLine.IsValueOrShortValue(value))
                {
                    lineDisplayOptions |= LineDisplayOptions.TrimLine;
                }
                else if (OptionValues.Display_TrimLine.IsValueOrShortValue(value))
                {
                    lineDisplayOptions |= LineDisplayOptions.TrimLine;
                }
                else if (OptionValues.Display_NoAlign.IsValueOrShortValue(value))
                {
                    noAlign = true;
                }
                else
                {
                    WriteOptionError(value, optionName, OptionValueProviders.DisplayProvider);
                    return false;
                }
            }

            if (builder != null)
                fileProperties = builder.ToImmutableArray();

            return true;
        }

        public static bool TryParseOutputOptions(
            IEnumerable<string> values,
            string optionName,
            out string? path,
            out Verbosity verbosity,
            [NotNullWhen(true)] out Encoding? encoding,
            out bool append)
        {
            path = null;
            verbosity = Verbosity.Normal;
            encoding = Encoding.UTF8;
            append = false;

            if (!values.Any())
                return true;

            if (!TryEnsureFullPath(values.First(), out path))
                return false;

            foreach (string value in values.Skip(1))
            {
                string option = value;

                int index = option.IndexOf('=');

                if (index >= 0)
                {
                    string key = option.Substring(0, index);
                    string value2 = option.Substring(index + 1);

                    if (OptionValues.Verbosity.IsKeyOrShortKey(key))
                    {
                        if (!TryParseVerbosity(value2, out verbosity))
                            return false;
                    }
                    else if (OptionValues.Encoding.IsKeyOrShortKey(key))
                    {
                        if (!TryParseEncoding(value2, out encoding))
                            return false;
                    }
                    else
                    {
                        WriteOptionError(value, optionName, OptionValueProviders.OutputFlagsProvider);
                        return false;
                    }
                }
                else if (OptionValues.Output_Append.IsValueOrShortValue(value))
                {
                    append = true;
                }
                else
                {
                    WriteOptionError(value, optionName, OptionValueProviders.OutputFlagsProvider);
                    return false;
                }
            }

            return true;
        }

        public static bool TryParseReplacement(
            IEnumerable<string> values,
            out string? replacement,
            out MatchEvaluator? matchEvaluator)
        {
            replacement = null;
            matchEvaluator = null;

            if (!values.Any())
                return true;

            string value = values.First();

            if (!TryParseAsEnumFlags(
                values.Skip(1),
                OptionNames.Replacement,
                out ReplacementOptions options,
                ReplacementOptions.None,
                OptionValueProviders.ReplacementOptionsProvider))
            {
                return false;
            }

            if ((options & ReplacementOptions.FromFile) != 0
                && !FileSystemHelpers.TryReadAllText(value, out value!, ex => WriteError(ex)))
            {
                return false;
            }

            if ((options & ReplacementOptions.CSharp) != 0)
            {
                if ((options & ReplacementOptions.FromFile) != 0)
                {
                    return DelegateFactory.TryCreateFromSourceText(value, typeof(string), typeof(Match), out matchEvaluator);
                }
                else
                {
                    return DelegateFactory.TryCreateFromExpression(
                        value,
                        "EvaluatorClass",
                        "EvaluatorMethod",
                        "string",
                        typeof(string),
                        "Match",
                        typeof(Match),
                        "match",
                        out matchEvaluator);
                }
            }
            else if ((options & ReplacementOptions.FromDll) != 0)
            {
                return DelegateFactory.TryCreateFromAssembly(value, typeof(string), typeof(Match), out matchEvaluator);
            }
            else
            {
                replacement = value;

                if ((options & ReplacementOptions.Literal) != 0)
                    replacement = RegexEscape.EscapeSubstitution(replacement);

                if ((options & ReplacementOptions.Escape) != 0)
                    replacement = RegexEscape.ConvertCharacterEscapes(replacement);
            }

            return true;
        }

        public static bool TryParseInput(
            IEnumerable<string> values,
            out string input)
        {
            if (!values.Any())
                throw new InvalidOperationException("Input is missing.");

            input = values.First();

            if (!TryParseAsEnumFlags(
                values.Skip(1),
                OptionNames.Input,
                out InputOptions options,
                InputOptions.None,
                OptionValueProviders.InputOptionsProvider))
            {
                return false;
            }

            if ((options & InputOptions.Escape) != 0)
                input = RegexEscape.ConvertCharacterEscapes(input);

            return true;
        }

        public static bool TryParseAsEnumFlags<TEnum>(
            IEnumerable<string>? values,
            string optionName,
            out TEnum result,
            TEnum? defaultValue = null,
            OptionValueProvider? provider = null) where TEnum : struct
        {
            result = (TEnum)(object)0;

            if (values?.Any() != true)
            {
                if (defaultValue != null)
                {
                    result = (TEnum)(object)defaultValue;
                }

                return true;
            }

            int flags = 0;

            foreach (string value in values)
            {
                if (!TryParseAsEnum(value, optionName, out TEnum result2, provider: provider))
                    return false;

                flags |= (int)(object)result2;
            }

            result = (TEnum)(object)flags;

            return true;
        }

        public static bool TryParseAsEnumValues<TEnum>(
            IEnumerable<string>? values,
            string optionName,
            out ImmutableArray<TEnum> result,
            ImmutableArray<TEnum> defaultValue = default,
            OptionValueProvider? provider = null) where TEnum : struct
        {
            if (values?.Any() != true)
            {
                result = (defaultValue.IsDefault) ? ImmutableArray<TEnum>.Empty : defaultValue;

                return true;
            }

            ImmutableArray<TEnum>.Builder builder = ImmutableArray.CreateBuilder<TEnum>();

            foreach (string value in values)
            {
                if (!TryParseAsEnum(value, optionName, out TEnum result2, provider: provider))
                    return false;

                builder.Add(result2);
            }

            result = builder.ToImmutableArray();

            return true;
        }

        public static bool TryParseAsEnum<TEnum>(
            string value,
            string optionName,
            out TEnum result,
            TEnum? defaultValue = null,
            OptionValueProvider? provider = null) where TEnum : struct
        {
            if (!TryParseAsEnum(value, out result, defaultValue, provider))
            {
                string optionValues = OptionValueProviders.GetHelpText(provider, multiline: true)
                    ?? OptionValue.GetDefaultHelpText<TEnum>(multiline: true);

                WriteOptionError(value, optionName, optionValues);
                return false;
            }

            return true;
        }

        public static bool TryParseAsEnum<TEnum>(
            string value,
            out TEnum result,
            TEnum? defaultValue = null,
            OptionValueProvider? provider = default) where TEnum : struct
        {
            if (value == null
                && defaultValue != null)
            {
                result = defaultValue.Value;
                return true;
            }

            if (provider != null)
            {
                return provider.TryParseEnum(value, out result);
            }
            else
            {
                return Enum.TryParse(value?.Replace("-", ""), ignoreCase: true, out result);
            }
        }

        public static bool TryParseVerbosity(string value, out Verbosity verbosity)
        {
            return TryParseAsEnum(
                value,
                OptionNames.Verbosity,
                out verbosity,
                provider: OptionValueProviders.VerbosityProvider);
        }

        // https://docs.microsoft.com/en-us/dotnet/api/system.text.encoding#remarks
        public static bool TryParseEncoding(string name, [NotNullWhen(true)] out Encoding? encoding)
        {
            if (name == "utf-8-no-bom")
            {
                encoding = Text.EncodingHelpers.UTF8NoBom;
                return true;
            }

            try
            {
                encoding = Encoding.GetEncoding(name);
                return true;
            }
            catch (ArgumentException ex)
            {
                WriteError(ex);

                encoding = null;
                return false;
            }
        }

        public static bool TryParseEncoding(string name, out Encoding encoding, Encoding defaultEncoding)
        {
            if (name == null)
            {
                encoding = defaultEncoding;
                return true;
            }

            return TryParseEncoding(name, out encoding, defaultEncoding);
        }

        public static bool TryParseMaxCount(IEnumerable<string> values, out int maxMatchingFiles, out int maxMatchesInFile)
        {
            maxMatchingFiles = 0;
            maxMatchesInFile = 0;

            if (!values.Any())
                return true;

            foreach (string value in values)
            {
                int index = value.IndexOf('=');

                if (index >= 0)
                {
                    string key = value.Substring(0, index);
                    string value2 = value.Substring(index + 1);

                    if (OptionValues.MaxMatches.IsKeyOrShortKey(key))
                    {
                        if (!TryParseCount(value2, OptionNames.MaxCount, out maxMatchesInFile, value))
                            return false;
                    }
                    else
                    {
                        WriteOptionError(value, OptionNames.MaxCount, OptionValueProviders.MaxOptionsProvider);
                        return false;
                    }
                }
                else if (!TryParseCount(value, OptionNames.MaxCount, out maxMatchingFiles))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryParseCount(string value, string optionName, out int count, string? value2 = null)
        {
            if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out count))
                return true;

            WriteError($"Option '{OptionNames.GetHelpText(optionName)}' has invalid value '{value2 ?? value}'.");
            return false;
        }

        public static bool TryParseChar(string value, out char result)
        {
            if (value.Length == 2
                && value[0] == '\\'
                && value[1] >= 48
                && value[1] <= 57)
            {
                result = value[1];
                return true;
            }

            if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int charCode))
            {
                if (charCode < 0 || charCode > 0xFFFF)
                {
                    WriteError("Value must be in range from 0 to 65535.");
                    result = default;
                    return false;
                }

                result = (char)charCode;
            }
            else if (!char.TryParse(value, out result))
            {
                WriteError($"Could not parse '{value}' as character value.");
                return false;
            }

            return true;
        }

        public static bool TryEnsureFullPath(IEnumerable<string> paths, out ImmutableArray<string> fullPaths)
        {
            ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>();

            foreach (string path in paths)
            {
                if (!TryEnsureFullPath(path, out string? fullPath))
                    return false;

                builder.Add(fullPath);
            }

            fullPaths = builder.ToImmutableArray();
            return true;
        }

        public static bool TryEnsureFullPath(string path, [NotNullWhen(true)] out string? fullPath)
        {
            try
            {
                fullPath = FileSystemHelpers.EnsureFullPath(path);
                return true;
            }
            catch (ArgumentException ex)
            {
                WriteError($"Path '{path}' is invalid: {ex.Message}.");
                fullPath = null;
                return false;
            }
        }

        internal static void WriteOptionError(string value, string optionName, OptionValueProvider? provider = null)
        {
            WriteOptionError(value, optionName, OptionValueProviders.GetHelpText(provider, multiline: true));
        }

        private static void WriteOptionError(string value, string optionName, string? allowedValues)
        {
            WriteParseError(value, OptionNames.GetHelpText(optionName), allowedValues);
        }

        internal static void WriteOptionValueError(
            string value,
            OptionValue optionValue,
            OptionValueProvider? provider = null)
        {
            WriteOptionValueError(value, optionValue, OptionValueProviders.GetHelpText(provider, multiline: true));
        }

        internal static void WriteOptionValueError(string value, OptionValue optionValue, string? allowedValues)
        {
            WriteParseError(value, optionValue.HelpValue, allowedValues);
        }

        private static void WriteParseError(string value, string optionText, string? allowedValues)
        {
            string message = $"Option '{optionText}' has invalid value '{value}'.";

            if (!string.IsNullOrEmpty(allowedValues))
                message += $"{Environment.NewLine}{Environment.NewLine}Allowed values:{Environment.NewLine}{allowedValues}";

            WriteError(message);
        }

        internal static bool TryParseProperties(string ask, IEnumerable<string> name, CommonFindCommandOptions options)
        {
            if (!TryParseAsEnum(
                ask,
                OptionNames.Ask,
                out AskMode askMode,
                defaultValue: AskMode.None,
                OptionValueProviders.AskModeProvider))
            {
                return false;
            }

            if (askMode == AskMode.Value
                && ConsoleOut.Verbosity < Verbosity.Normal)
            {
                WriteError($"Option '{OptionNames.GetHelpText(OptionNames.Ask)}' cannot have value "
                    + $"'{OptionValueProviders.AskModeProvider.GetValue(nameof(AskMode.Value)).HelpValue}' when "
                    + $"'{OptionNames.GetHelpText(OptionNames.Verbosity)}' is set to "
                    + $"'{OptionValueProviders.VerbosityProvider.GetValue(ConsoleOut.Verbosity.ToString()).HelpValue}'.");

                return false;
            }

            if (!FilterParser.TryParse(
                name,
                OptionNames.Name,
                OptionValueProviders.PatternOptionsProvider,
                out Filter? nameFilter,
                out FileNamePart namePart,
                allowNull: true))
            {
                return false;
            }

            options.AskMode = askMode;
            options.NameFilter = nameFilter;
            options.NamePart = namePart;

            return true;
        }

        public static bool TryParseTargetDirectory(
            string value,
            [NotNullWhen(true)] out string? result,
            CommonCopyCommandOptions options,
            string directoryName,
            string optionName)
        {
            result = null;

            if (value != null)
            {
                if (!TryEnsureFullPath(value, out result))
                    return false;
            }
            else
            {
                int length = options.Paths.Length;

                if (length < 2)
                {
                    WriteError($"{directoryName} directory is required. It can be specified either as a last unnamed parameter "
                        + $"or using option '{OptionNames.GetHelpText(optionName)}'.");

                    return false;
                }
                else
                {
                    result = options.Paths[length - 1].Path;
                    options.Paths = options.Paths.RemoveAt(length - 1);
                }
            }

            return true;
        }
    }
}
