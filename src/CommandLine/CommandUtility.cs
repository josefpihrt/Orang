﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using static Orang.Logger;

namespace Orang.CommandLine
{
    internal static class CommandUtility
    {
        public static bool IsCompoundCommand(string command)
        {
            switch (command)
            {
                case "regex-escape":
                case "regex-list":
                case "regex-match":
                case "regex-split":
                    return true;
            }

            return false;
        }

        internal static bool CheckCommandName(ref string[] args, bool showErrorMessage = true)
        {
            switch (args[0])
            {
                case "regex-escape":
                case "regex-list":
                case "regex-match":
                case "regex-split":
                    {
                        if (showErrorMessage)
                            WriteError($"Command '{args[0]}' is invalid. Use command '{args[0].Replace('-', ' ')}' instead.");

                        return false;
                    }
                case "escape":
                    {
                        ReplaceArgs("regex-escape", "regex escape", ref args);
                        break;
                    }
                case "list-patterns":
                    {
                        ReplaceArgs("regex-list", "regex list", ref args);
                        break;
                    }
                case "match":
                    {
                        ReplaceArgs("regex-match", "regex match", ref args);
                        break;
                    }
                case "split":
                    {
                        ReplaceArgs("regex-split", "regex split", ref args);
                        break;
                    }
                case "regex":
                    {
                        if (args.Length > 1)
                        {
                            switch (args[1])
                            {
                                case "escape":
                                    {
                                        ReplaceArgs("regex-escape", ref args);
                                        break;
                                    }
                                case "list":
                                    {
                                        ReplaceArgs("regex-list", ref args);
                                        break;
                                    }
                                case "match":
                                    {
                                        ReplaceArgs("regex-match", ref args);
                                        break;
                                    }
                                case "split":
                                    {
                                        ReplaceArgs("regex-split", ref args);
                                        break;
                                    }
                                default:
                                    {
                                        var commandName = "regex";

                                        if (!args[1].StartsWith('-'))
                                            commandName += " " + args[1];

                                        if (showErrorMessage)
                                            WriteErrorMessage(commandName);

                                        return false;
                                    }
                            }
                        }
                        else
                        {
                            if (showErrorMessage)
                                WriteErrorMessage("regex");

                            return false;
                        }

                        break;
                    }
            }

            return true;

            static void WriteErrorMessage(string commandName)
            {
                WriteError($"Command '{commandName}' is invalid. "
                    + $"Use following commands instead:{Environment.NewLine}"
                    + $"  regex escape{Environment.NewLine}"
                    + $"  regex list{Environment.NewLine}"
                    + $"  regex match{Environment.NewLine}"
                    + $"  regex split{Environment.NewLine}");
            }
        }

        private static void ReplaceArgs(string commandName, string commandAlias, ref string[] args)
        {
            if (commandAlias != null)
            {
                WriteWarning($"Command '{args[0]}' has been deprecated "
                    + $"and will be removed in future version. Use command '{commandAlias}' instead.");
            }

            args[0] = commandName;
        }

        private static void ReplaceArgs(string commandName, ref string[] args)
        {
            const int diff = 1;

            var args2 = new string[args.Length - diff];

            Array.Copy(args, diff + 1, args2, 1, args.Length - (diff + 1));

            args2[0] = commandName;

            args = args2;
        }
    }
}