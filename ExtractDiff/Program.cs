using System;
using System.IO;
using ExtractDiff.Core;

namespace ExtractDiff
{
    class Program
    {
        private static ZipService _zipService;
        private static PackageManager _packageManager;
        private static DiffService _diffService;

        static void Main(string[] args)
        {
            if (args[0].Contains("?") || args[0].Contains("help", StringComparison.InvariantCultureIgnoreCase))
            {
                PrintHelp();
                return;
            }

            string workingDirectory = "";
            string packageName = "";
            string newPackageVersion = "";
            string packageNamePart = "";
            string downloadUrlPattern = "";

            if (args.Length > 0)
                workingDirectory = args[0];
            if (args.Length > 1)
                packageName = args[1];
            if (args.Length > 2)
                newPackageVersion = args[2];
            if (args.Length > 3)
                packageNamePart = args[3];
            if (args.Length > 4)
                downloadUrlPattern = args[4];

            Logger.LogInformation($"Running with values workingDirectory {workingDirectory}, packageName: {packageName}, newPackageVersion: {newPackageVersion}, packageNamePart: {packageNamePart}, downloadUrlPattern: {downloadUrlPattern}");

            // Validate Params
            if (string.IsNullOrWhiteSpace(workingDirectory))
                throw new ArgumentNullException(nameof(workingDirectory));
            if (string.IsNullOrWhiteSpace(packageName))
                throw new ArgumentNullException(nameof(packageName));
            if (string.IsNullOrWhiteSpace(newPackageVersion))
                throw new ArgumentNullException(nameof(newPackageVersion));
            if (string.IsNullOrWhiteSpace(packageNamePart))
            {
                packageNamePart = packageName + ".";
            }

            // Cough cough: "Dependency Injection"?
            _zipService = new ZipService();
            _packageManager = new PackageManager(downloadUrlPattern);
            _diffService = new DiffService(_packageManager, _zipService, workingDirectory);

            if (Version.TryParse(newPackageVersion, out var newVersion) == false)
            {
                newVersion = new Version(_packageManager.GetVersionFromPackage(newPackageVersion, packageNamePart));
            }
            if (newVersion.Build == 0)
            {
                Logger.LogInformation("New version is the first release of this minor, diffs are only done between patches");
                return;
            }

            var newPackagePath = Path.Combine(workingDirectory, packageNamePart + newVersion.ToString(3) + ".zip");

            _packageManager.EnsurePackage(packageName, newVersion, newPackagePath);
            _zipService.Extract(newPackagePath);

            Version oldVersion = new Version(newVersion.Major, newVersion.Minor, newVersion.Build - 1);
            while (oldVersion.Build >= 0)
            {
                Logger.LogInformation($"Creating diff package between {newVersion} and {oldVersion}");
                var oldPackagePath = Path.Combine(workingDirectory, packageNamePart + oldVersion.ToString(3) + ".zip");
                _packageManager.EnsurePackage(packageName, oldVersion, oldPackagePath);
                _zipService.Extract(oldPackagePath);
                _diffService.CompareFoldersAndZipDiff(oldPackagePath, newPackagePath, packageName, packageNamePart);
                oldPackagePath.Replace(".zip", "").DeleteDirectory();

                if (oldVersion.Build == 0)
                    break;
                oldVersion = new Version(oldVersion.Major, oldVersion.Minor, oldVersion.Build - 1);
            }
            newPackagePath.Replace(".zip", "").DeleteDirectory();
        }

        private static void PrintHelp()
        {
            Console.WriteLine(
                $"Extract diff will compare different versions of packages, and create diff packages, of what has actually changed in between versions.");
            Console.WriteLine(
                $"It is used to just apply changed files when upgrading, rather than having to overwrite all files.");
            Console.WriteLine();
            Console.WriteLine($"Invoke with:");
            Console.WriteLine($@"- WorkingDirectory              - where can I work, and put my output - i.e. c:\temp");
            Console.WriteLine($"- PackageName                   - the package name trying to make compare files of - i.e. 'UmbracoCms' or 'UmbracoDeploy'");
            Console.WriteLine($@"- NewPackageVersion/Location    - path to new version package, or just the version, i.e. 'c:\temp\UmbracoCms.7.13.2' if the version isn't released yet or just '7.13.2' if its a released version");
            Console.WriteLine($"- PackageNamePart               - the part of the .zip file that isnt part of the version number - i.e. 'UmbracoCms.' or 'UmbracoDeploy.v'");
            Console.WriteLine("- DownloadUrlPattern            - where to download packages to compare - i.e. 'https://umbracoreleases.blob.core.windows.net/download/UmbracoCms.{{version}}.zip'");

            Console.WriteLine();
            Console.WriteLine(
                $"Example: ExtractDiff.dll \"d:\\temp\" UmbracoCms \"C:\\Users\\user\\Downloads\\UmbracoCms.7.13.2.zip\" \"UmbracoCms.\" \"https://umbracoreleases.blob.core.windows.net/download/UmbracoCms.{{version}}.zip\"");
        }
    }
}
