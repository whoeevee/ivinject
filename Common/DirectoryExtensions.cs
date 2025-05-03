namespace ivinject.Common;

internal static class DirectoryExtensions
{
    internal static string TempDirectoryPath()
    {
        return Path.Combine(
            Path.GetTempPath(),
            Path.ChangeExtension(Path.GetRandomFileName(), null)
        );
    }
    
    internal static string ApplicationDataPath() => 
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    
    internal static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        var dirs = dir.GetDirectories();

        Directory.CreateDirectory(destinationDir);

        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
        }

        if (!recursive) return;
        
        foreach (var subDir in dirs)
        {
            var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir, true);
        }
    }
}