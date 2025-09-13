﻿using System.CommandLine;
using ivinject.Features.Command;

namespace ivinject;

internal static class Program
{
    private static readonly RootCommand RootCommand = new IviRootCommand();
    
    private static async Task<int> Main(string[] args)
    {
        return await RootCommand.Parse(args).InvokeAsync();
    }
}