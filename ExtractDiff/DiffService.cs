using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ExtractDiff
{
    public class DiffService
    {
        private readonly PackageManager _packageManager;
        private readonly ZipService _zipService;
        private readonly string _workingDirectory;

        public DiffService(PackageManager packageManager, ZipService zipService, string workingDirectory)
        {
            _packageManager = packageManager;
            _zipService = zipService;
            _workingDirectory = workingDirectory;
        }

        public void CompareFoldersAndZipDiff(string oldPackagePath, string newPackagePath, string packageName, string packageNamePart)
        {
            Logger.LogInformation($"Comparing {newPackagePath} to {oldPackagePath} to get the diff");
            var oldVersion = _packageManager.GetVersionFromPackage(oldPackagePath, packageNamePart);
            var newVersion = _packageManager.GetVersionFromPackage(newPackagePath, packageNamePart);
            var diffWorkDir = Path.Combine(_workingDirectory, $"{packageName}.Diff.{oldVersion}-{newVersion}");
            if (Directory.Exists(diffWorkDir))
                diffWorkDir.DeleteDirectory();

            CopyDirectory(newPackagePath.Replace(".zip", ""), diffWorkDir, oldPackagePath.Replace(".zip", ""));
            _zipService.DeleteEmptyDirs(diffWorkDir);
            _zipService.CreateZip(diffWorkDir + ".zip", diffWorkDir);
            diffWorkDir.DeleteDirectory();
        }

        /// <summary>
        /// A performance-optimized method to copy a directory to another including all subdirectories
        /// Adapted from: http://stackoverflow.com/a/2801234/5018
        /// </summary>
        /// <param name="source">The source directory</param>
        /// <param name="target">The target to copy to</param>
        private bool CopyDirectory(string source, string target, string olddir)
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

        bool FileEquals(string path1, string path2)
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
    }
}
