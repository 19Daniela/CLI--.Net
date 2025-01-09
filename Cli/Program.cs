using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

var bundleOption = new Option<FileInfo>("--output", "File path and name")
{
    IsRequired = true,
};

var languageOption = new Option<string>("--language", "Programming languages (comma-separated or 'all')")
{
    IsRequired = true
};

var noteOption = new Option<bool>("--note", "Include source note");

var sortOption = new Option<string>("--sort", () => "name", "Sort order: 'name' or 'type'");

var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines", "Remove empty lines from code");

var authorOption = new Option<string>("--author", "Name of the file author");

var bundleCommand = new Command("bundle", "Bundle code files to a single file")
{
    bundleOption,
    languageOption,
    noteOption,
    sortOption,
    authorOption,
    removeEmptyLinesOption
};

bundleCommand.Handler = CommandHandler.Create<FileInfo, string, bool, string, bool, string>((output, language, note, sort, removeEmptyLines, author) =>
{
    try
    {
        var files = Directory.GetFiles(".", "*.*", SearchOption.AllDirectories)
                             .Where(f => language == "all" || language.Split(',').Any(lang => f.EndsWith($".{lang}")))
                             .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"));
        if (sort == "type")
        {
            files = files.OrderBy(f => Path.GetExtension(f));
        }
        else
        {
            files = files.OrderBy(f => Path.GetFileName(f));
        }
        using (var writer = new StreamWriter(output.FullName))
        {
            if (!string.IsNullOrEmpty(author))
            {
                writer.WriteLine($"// Author: {author};");
            }
            foreach (var file in files)
            {
                if (note)
                {
                    writer.WriteLine($"// Source: {file}");
                }
                foreach (var line in File.ReadLines(file))
                {
                    if (!removeEmptyLines || !string.IsNullOrWhiteSpace(line))
                    {
                        writer.WriteLine(line);
                    }
                }
            }
        }
        Console.WriteLine("Bundle created successfully");
    }
    catch (DirectoryNotFoundException)
    {
        Console.WriteLine("Error: File path is invalid");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unexpected error: {ex.Message}");
    }
});

var rootCommand = new RootCommand("Root command for File Bundler CLI");
rootCommand.AddCommand(bundleCommand);

await rootCommand.InvokeAsync(args);
