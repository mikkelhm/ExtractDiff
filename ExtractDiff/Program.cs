using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ExtractDiff
{
    class Program
    {
        //private static string workingDirectory;
        // UmbracoCms, UmbracoDeploy, UmbracoForms
        //private static string packageName;
        // UmbracoCms.7.12.2, UmbracoDeploy.v2.0.15, UmbracoForms.Files.7.0.6 - woohoo for consistency
        //private static string packageNamePart;
        private static ZipService _zipService;
        private static PackageManager _packageManager;
        private static DiffService _diffService;

        static void Main(string[] args)
        {
            string workingDirectory = "";
            string packageName = "";
            string newPackageVersion = "";
            string packageNamePart = "";

            if (args.Length > 0)
                workingDirectory = args[0];
            if (args.Length > 1)
                packageName = args[1];
            if (args.Length > 2)
                newPackageVersion = args[2];
            if (args.Length > 3)
                packageNamePart = args[3];

            Logger.LogInformation($"Running with values workingDirectory {workingDirectory}, packageName: {packageName}, newPackageVersion: {newPackageVersion}, packageNamePart: {packageNamePart}");

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
            _packageManager = new PackageManager("https://umbracoreleases.blob.core.windows.net/download/UmbracoCms.{version}.zip");
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

    }
}
