﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using DotMarkdown;
using Orang.CommandLine;

namespace Orang.Documentation;

internal class DocumentationWriter : MarkdownDocumentationWriter
{
    public DocumentationWriter(MarkdownWriter writer) : base(writer)
    {
    }

    public override void WriteOptionDescription(CommandOption option)
    {
        string description = option.FullDescription;

        _writer.WriteString(description);

        if (TryGetProvider(option, out OptionValueProvider? provider))
        {
            if (!string.IsNullOrEmpty(description))
            {
                _writer.WriteLine();
                _writer.WriteLine();
            }

            OptionValueProvider? provider2 = provider;

            if (provider.Parent is not null)
                provider2 = provider.Parent;

            string metaValueUrl = provider2.Name;

            _writer.WriteLink(provider2.Name, "OptionValues.md" + MarkdownHelpers.CreateGitHubHeadingLink(metaValueUrl));

            _writer.WriteString(": ");

            using (IEnumerator<string> en = provider
                .Values
                .Where(f => !f.Hidden)
                .Select(f => f.HelpValue)
                .GetEnumerator())
            {
                if (en.MoveNext())
                {
                    while (true)
                    {
                        string value = en.Current;

                        Match metaValueMatch = Regex.Match(value, @"(?<==)\<[\p{Lu}_]+>\z");

                        if (metaValueMatch.Success
                            && OptionValueProviders.ProvidersByName.ContainsKey(metaValueMatch.Value))
                        {
                            _writer.WriteInlineCode(value.Remove(metaValueMatch.Index));
                            _writer.WriteLink(
                                metaValueMatch.Value,
                                "OptionValues.md" + MarkdownHelpers.CreateGitHubHeadingLink(metaValueMatch.Value));
                        }
                        else
                        {
                            _writer.WriteInlineCode(en.Current);
                        }

                        if (en.MoveNext())
                        {
                            _writer.WriteString(", ");
                        }
                        else
                        {
                            break;
                        }
                    }

                    _writer.WriteString(".");
                }
            }
        }

        if (!string.IsNullOrEmpty(description)
            || provider is not null)
        {
            _writer.WriteLine();
            _writer.WriteLine();
        }
    }

    private static bool TryGetProvider(CommandOption option, [NotNullWhen(true)] out OptionValueProvider? provider)
    {
        if (option.ValueProviderName is not null)
            return OptionValueProviders.ProvidersByName.TryGetValue(option.ValueProviderName, out provider);

        if (option.MetaValue is not null
            && OptionValueProviders.ProvidersByName.TryGetValue(option.MetaValue, out provider))
        {
            return true;
        }

        Match match = OptionValueProvider.MetaValueRegex.Match(option.MetaValue);

        if (match.Success)
            return OptionValueProviders.ProvidersByName.TryGetValue(match.Value, out provider);

        provider = null;
        return false;
    }
}
