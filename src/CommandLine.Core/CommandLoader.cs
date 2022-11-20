﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Reflection;
using CommandLine;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Orang.CommandLine.Annotations;

namespace Orang;

internal static class CommandLoader
{
    public static IEnumerable<Command> LoadCommands(Assembly assembly)
    {
        foreach (System.Reflection.TypeInfo type in assembly.GetTypes())
        {
            if (!type.IsAbstract)
            {
                VerbAttribute? verbAttribute = type.GetCustomAttribute<VerbAttribute>();

                if (verbAttribute is not null)
                    yield return CreateCommand(type, verbAttribute);
            }
        }
    }

    public static Command? LoadCommand(Assembly assembly, string commandName)
    {
        foreach (System.Reflection.TypeInfo type in assembly.GetTypes())
        {
            if (type.IsAbstract)
                continue;

            VerbAttribute? verbAttribute = type.GetCustomAttribute<VerbAttribute>();

            if (verbAttribute?.Name == commandName)
                return CreateCommand(type, verbAttribute);
        }

        return null;
    }

    private static Command CreateCommand(Type type, VerbAttribute verbAttribute)
    {
        ImmutableArray<CommandArgument>.Builder arguments = ImmutableArray.CreateBuilder<CommandArgument>();
        ImmutableArray<CommandOption>.Builder options = ImmutableArray.CreateBuilder<CommandOption>();

        Dictionary<string, string> providerMap = type
            .GetCustomAttributes<OptionValueProviderAttribute>()
            .ToDictionary(f => f.PropertyName, f => f.ProviderName);

        foreach (PropertyInfo propertyInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            OptionAttribute? optionAttribute = null;

            foreach (Attribute attribute in propertyInfo.GetCustomAttributes())
            {
                if (attribute is ValueAttribute valueAttribute)
                {
                    var argument = new CommandArgument(
                        index: valueAttribute.Index,
                        name: valueAttribute.MetaName,
                        description: valueAttribute.HelpText,
                        isRequired: valueAttribute.Required);

                    arguments.Add(argument);
                    break;
                }
                else if (optionAttribute is null)
                {
                    optionAttribute = attribute as OptionAttribute;

                    if (optionAttribute is not null)
                        continue;
                }
            }

            if (optionAttribute is not null)
            {
                Debug.Assert(
                    propertyInfo.PropertyType != typeof(bool) || string.IsNullOrEmpty(optionAttribute.MetaValue),
                    $"{type.Name}.{propertyInfo.Name}");

                var option = new CommandOption(
                    propertyInfo: propertyInfo,
                    name: optionAttribute.LongName,
                    shortName: optionAttribute.ShortName,
                    metaValue: optionAttribute.MetaValue,
                    description: optionAttribute.HelpText,
                    additionalDescription: propertyInfo.GetCustomAttribute<AdditionalDescriptionAttribute>()?.Text,
                    isRequired: optionAttribute.Required,
                    valueProviderName: (providerMap.TryGetValue(propertyInfo.Name, out string? valueProviderName))
                        ? valueProviderName
                        : null);

                options.Add(option);
            }
        }

        CommandAliasAttribute commandAliasAttribute = type.GetCustomAttribute<CommandAliasAttribute>();
        CommandGroupAttribute commandGroupAttribute = type.GetCustomAttribute<CommandGroupAttribute>()!;

        return new Command(
            verbAttribute.Name,
            verbAttribute.HelpText,
            (commandGroupAttribute is not null)
                ? new CommandGroup(commandGroupAttribute.Name, commandGroupAttribute.Ordinal)
                : CommandGroup.Default,
            commandAliasAttribute?.Alias,
            arguments.OrderBy(f => f.Index),
            options.OrderBy(f => f, CommandOptionComparer.Name));
    }
}
