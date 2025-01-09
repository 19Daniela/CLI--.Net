using System.CommandLine;


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

bundleCommand.SetHandler(
    (string[] languages, FileInfo output, bool note, string sort, bool removeEmptyLines, string author) =>
    {
        try
        {
            var files = GetFilesToBundle(languages);
            files = SortFiles(files, sort);
            using var writer = new StreamWriter(output.FullName);

            writer.WriteLine($"// Author: {author}");
            writer.WriteLine("// Bundled Code Starts Here");
            writer.WriteLine();

            foreach (var file in files)
            {
                if (note)
                {
                    writer.WriteLine($"// File: {Path.GetRelativePath(Directory.GetCurrentDirectory(), file)}");
                }

                var fileLines = File.ReadAllLines(file); 

                if (removeEmptyLines)
                {
                    fileLines = fileLines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
                }

                foreach (var line in fileLines)  
                {
                    writer.WriteLine(line);
                }

                writer.WriteLine(); 
            }

            writer.WriteLine("// Bundled Code Ends Here");

            Console.WriteLine("bundle command executed successfully!");
            Console.WriteLine($"The output file is created in: {output.FullName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    },
    bundleOption,
    languageOption,
    noteOption,
    sortOption,
    authorOption,
    removeEmptyLinesOption);

var createRspCommand = new Command("create-rsp", "create file to exe bundle")
{
    new Argument<FileInfo?>("file", "file name")
};

createRspCommand.SetHandler(async (FileInfo? file) =>
{
    file ??= new FileInfo("default.rsp");
    try
    {
        Console.WriteLine($"Creating response file: {file.FullName}");
        Console.WriteLine("Type the languages (e.g., csharp, javascript, html or 'all'):");
        var languages = Console.ReadLine() ?? "all";

        Console.WriteLine("Enter the name of the output file:");
        var output = Console.ReadLine() ?? "bundled_code.txt";

        Console.WriteLine("Do you want to add comments with the file name? (true/false):");
        var noteInput = Console.ReadLine();
        var note = bool.TryParse(noteInput, out var noteResult) ? noteResult : false;

        Console.WriteLine("Sort type (name or type):");
        var sort = Console.ReadLine() ?? "name";

        Console.WriteLine("Do you want to delete empty lines? (true/false):");
        var removeEmptyLinesInput = Console.ReadLine();
        var removeEmptyLines = bool.TryParse(removeEmptyLinesInput, out var removeEmptyLinesResult) ? removeEmptyLinesResult : false;

        Console.WriteLine("The name of the creator:");
        var author = Console.ReadLine() ?? "Unknown Author";

        var bundleCommand = $"bundle --language {languages} --output {output} --note {note} --sort {sort} --remove-empty-lines {removeEmptyLines} --author \"{author}\"";
        await File.WriteAllTextAsync(file.FullName, bundleCommand);
        Console.WriteLine($"The response file was created successfully: {file.FullName}!");
        Console.WriteLine($"To run the command: dotnet @{file.FullName}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }
}, (System.CommandLine.Binding.IValueDescriptor<FileInfo>)createRspCommand.Arguments[0]);



var rootCommand = new RootCommand(" CLI tool for packaging code files")
{
    bundleCommand, createRspCommand
};

return await rootCommand.InvokeAsync(args);

static string[] GetFilesToBundle(string[] languages)
{
    var excludedDirectories = new[] { "bin", "obj", ".git", "node_modules" };
    var currentDir = Directory.GetCurrentDirectory();

    var allFiles = Directory.GetFiles(currentDir, "*.*", SearchOption.AllDirectories)
                            .Where(file => !excludedDirectories.Any(dir => file.Contains(Path.DirectorySeparatorChar + dir + Path.DirectorySeparatorChar)))
                            .ToList();

    if (languages.Contains("all", StringComparer.OrdinalIgnoreCase))
    {
        return allFiles.ToArray();
    }

    var extensions = languages.Select(lang => lang.ToLower()) 
                              .Select(lang => lang switch
                              {
                                  "csharp" => ".cs",
                                  "python" => ".py",
                                  "javascript" => ".js",
                                  "java" => ".java",
                                  "cpp" => ".cpp",
                                  "html" => ".html",
                                  "txt" => ".txt",
                                  "word" => ".docs",
                                  _ => "." + lang
                              }).ToList();

    Console.WriteLine("Filtered files for bundling:");
    foreach (var file in allFiles)
    {
        Console.WriteLine(file);
    }

    return allFiles.Where(file => extensions.Contains(Path.GetExtension(file).ToLower()))
                   .ToArray();
}


static string[] SortFiles(string[] files, string sortOption)
{
    return sortOption.ToLower() switch
    {
        "type" => files.OrderBy(f => Path.GetExtension(f)).ToArray(),
        _ => files.OrderBy(f => f).ToArray(),
    };
}