using System.IO;

namespace StockTaggerLocalAI.Services;

public static class PromptLoader
{
    public static string Load(string fileName)
    {
        var promptPath = Path.Combine(
            AppContext.BaseDirectory,
            "Prompts",
            fileName);

        if (!File.Exists(promptPath))
        {
            throw new FileNotFoundException(
                $"Prompt file not found: {fileName}",
                promptPath);
        }

        return File.ReadAllText(promptPath);
    }
}
