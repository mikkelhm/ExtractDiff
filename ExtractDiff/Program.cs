using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ExtractDiff
{
    class Program
    {
        private static string workingDirectory;
        // UmbracoCms, UmbracoDeploy, UmbracoForms
        private static string packageName;
        // UmbracoCms.7.12.2, UmbracoDeploy.v2.0.15, UmbracoForms.Files.7.0.6 - woohoo for consistency
        private static string packageNameVersionIndexOf;
        private static ZipService _zipService;
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine($"I need to be invoked with 4 args, workingDirectory, packageName, newVersion");
                return;
            }

            workingDirectory = args[0];
            packageName = args[1];
            //var oldPackagePath = args[2];
            var newPackageVersion = args[2];
            if (Version.TryParse(newPackageVersion, out var newVersion) == false)
            {
                newVersion = new Version(GetVersionFromPackage(newPackageVersion));
            }
            if (newVersion.Build == 0)
            {
                Console.WriteLine("New version is the first release of this minor, diffs are only done between patches");
                return;
            }
            Version oldVersion;
            if (args.Length > 3)
            {
                oldVersion = new Version(args[3]);
            }
            else
            {
                oldVersion = new Version(newVersion.Major, newVersion.Minor, newVersion.Build - 1);
            }
            if (args.Length > 4)
            {
                packageNameVersionIndexOf = args[4];
            }
            else
            {
                packageNameVersionIndexOf = packageName + ".";
            }
            Console.WriteLine($"Running with values workingDirectory {workingDirectory}, packageName: {packageName}, newPackageVersion: {newPackageVersion}, packageNameVersionIndexOf: {packageNameVersionIndexOf}");

            // Cough cough: "Dependency Injection"?
            _zipService = new ZipService();
            var umbracoPackageDownloader = new PackageDownloader("https://umbracoreleases.blob.core.windows.net/download/UmbracoCms.{version}.zip");

            var oldPackagePath =
                Path.Combine(workingDirectory, packageNameVersionIndexOf + oldVersion.ToString(3) + ".zip");
            var newPackagePath = Path.Combine(workingDirectory, packageNameVersionIndexOf + newVersion.ToString(3) + ".zip");
            umbracoPackageDownloader.EnsurePackage(packageName, oldVersion, oldPackagePath);
            umbracoPackageDownloader.EnsurePackage(packageName, newVersion, newPackagePath);
            Extract(oldPackagePath);
            Extract(newPackagePath);

            CompareVersions(oldPackagePath, newPackagePath);
        }

        private static void CompareVersions(string oldPackagePath, string newPackagePath)
        {
            var oldVersion = GetVersionFromPackage(oldPackagePath);
            var newVersion = GetVersionFromPackage(newPackagePath);
            var workDir = Path.Combine(workingDirectory, $"{packageName}.Diff.{oldVersion}-{newVersion}");
            if (Directory.Exists(workDir))
                DeleteEmptyDirs(workDir);

            CopyDirectory(newPackagePath.Replace(".zip", ""), workDir, oldPackagePath.Replace(".zip", ""));

            _zipService.CreateZip(workDir + ".zip", workDir);
            DeleteEmptyDirs(workDir);

        }

        private static string GetVersionFromPackage(string packagePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(packagePath);
            var versionPart = fileName.Substring(packageNameVersionIndexOf.Length);
            return versionPart;
        }

        private static void Extract(string packagePath)
        {
            if (File.Exists(packagePath))
            {
                var extractDir = packagePath.Replace(".zip", "");
                if (Directory.Exists(extractDir) == false)
                    _zipService.ExtractZipFile(packagePath, packagePath.Replace(".zip", ""));
            }
        }


        /// <summary>
        /// A performance-optimized method to copy a directory to another including all subdirectories
        /// Adapted from: http://stackoverflow.com/a/2801234/5018
        /// </summary>
        /// <param name="source">The source directory</param>
        /// <param name="target">The target to copy to</param>
        private static bool CopyDirectory(string source, string target, string olddir)
        {
            try
            {
                var stack = new Stack<Folders>();
                stack.Push(new Folders(source, target));

                while (stack.Count > 0)
                {
                    var folders = stack.Pop();
                    if (Directory.Exists(folders.Target) == false)
                        Directory.CreateDirectory(folders.Target);

                    if (Directory.Exists(folders.Source) == false)
                        Directory.CreateDirectory(folders.Source);

                    foreach (var file in Directory.GetFiles(folders.Source, "*.*"))
                    {
                        if (file == null)
                            continue;

                        // if the file is new compared to the old version, copy it
                        var oldFilePath = file.Replace(source, olddir);
                        if (File.Exists(oldFilePath))
                        {
                            var oldfileInfo = new FileInfo(oldFilePath);
                            var newFileInfo = new FileInfo(file);
                            if (oldfileInfo.Length == newFileInfo.Length)
                            {
                                if (FileEquals(oldFilePath, file))
                                    continue;
                            }
                        }

                        var targetFile = Path.Combine(folders.Target, Path.GetFileName(file));



                        File.Copy(file, targetFile, true);
                    }

                    foreach (var folder in Directory.GetDirectories(folders.Source))
                    {
                        if (folder != null)
                            stack.Push(new Folders(folder, Path.Combine(folders.Target, Path.GetFileName(folder))));
                    }
                }
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }
        internal class Folders
        {
            internal string Source { get; private set; }
            internal string Target { get; private set; }

            internal Folders(string source, string target)
            {
                Source = source;
                Target = target;
            }
        }
        static bool FileEquals(string path1, string path2)
        {
            byte[] file1 = File.ReadAllBytes(path1);
            byte[] file2 = File.ReadAllBytes(path2);
            if (file1.Length == file2.Length)
            {
                for (int i = 0; i < file1.Length; i++)
                {
                    if (file1[i] != file2[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        static void DeleteEmptyDirs(string dir)
        {
            if (String.IsNullOrEmpty(dir))
                throw new ArgumentException(
                    "Starting directory is a null reference or an empty string",
                    "dir");

            try
            {
                foreach (var d in Directory.EnumerateDirectories(dir))
                {
                    DeleteEmptyDirs(d);
                }

                var entries = Directory.EnumerateFileSystemEntries(dir);

                if (!entries.Any())
                {
                    try
                    {
                        Directory.Delete(dir);
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (DirectoryNotFoundException) { }
                }
            }
            catch (UnauthorizedAccessException) { }
        }
    }
}
