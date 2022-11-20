﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Orang;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class CommandOption
{
    public CommandOption(
        PropertyInfo propertyInfo,
        string name,
        string? shortName = null,
        string? metaValue = null,
        string? description = null,
        string? additionalDescription = null,
        bool isRequired = false,
        string? valueProviderName = null)
    {
        PropertyInfo = propertyInfo;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ShortName = shortName;
        Description = description;
        AdditionalDescription = additionalDescription;
        IsRequired = isRequired;
        MetaValue = metaValue;
        ValueProviderName = valueProviderName;
    }

    public PropertyInfo PropertyInfo { get; }

    public string Name { get; }

    public string? ShortName { get; }

    public string? MetaValue { get; }

    public string? Description { get; }

    public string? AdditionalDescription { get; }

    public string FullDescription => Description + AdditionalDescription;

    public bool IsRequired { get; }

    public string? ValueProviderName { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay
    {
        get
        {
            var sb = new StringBuilder();

            if (!IsRequired)
                sb.Append("[");

            if (!string.IsNullOrEmpty(ShortName))
            {
                sb.Append("-");
                sb.Append(ShortName);
                sb.Append("|");
            }

            if (!string.IsNullOrEmpty(Name))
            {
                sb.Append("--");
                sb.Append(Name);
            }

            if (!IsRequired)
                sb.Append("]");

            if (!string.IsNullOrEmpty(MetaValue))
            {
                sb.Append("  ");
                sb.Append(MetaValue);
            }

            if (!string.IsNullOrEmpty(Description))
            {
                sb.Append("  ");
                sb.Append(Description);
            }

            return sb.ToString();
        }
    }
}
