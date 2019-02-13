using System;
using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace ExtractDiff
{
    public class ZipService
    {

        public void Extract(string packagePath)
        {
            if (File.Exists(packagePath))
            {
                var extractDir = packagePath.Replace(".zip", "");
                if (Directory.Exists(extractDir) == false)
                    ExtractZipFile(packagePath, packagePath.Replace(".zip", ""));
            }
            else
            {
                throw new ArgumentException($"Unable to find package at path {packagePath}", nameof(packagePath));
            }
        }

        /// <summary>
        /// Extracts an zip file to a specified output folder using the ICSharpCode.SharpZipLib 
        /// NB. Empty folders in the zip aren't extracted
        /// </summary>
        /// <param name="archiveFilenameIn">The zip file to extract</param>
        /// <param name="outFolder">The folder to put the file in. The folder is created if it doesnt exist</param>
        public bool ExtractZipFile(string archiveFilenameIn, string outFolder)
        {
            try
            {
                ZipFile zipFile = null;
                try
                {
                    var fileStream = File.OpenRead(archiveFilenameIn);
                    zipFile = new ZipFile(fileStream);
                    foreach (ZipEntry zipEntry in zipFile)
                    {
                        // Ignore directories
                        if (zipEntry.IsFile == false)
                            continue;

                        var entryFileName = zipEntry.Name;

                        var buffer = new byte[2048];
                        var zipStream = zipFile.GetInputStream(zipEntry);

                        var fullZipToPath = Path.Combine(outFolder, entryFileName);
                        var directoryName = Path.GetDirectoryName(fullZipToPath);

                        if (string.IsNullOrEmpty(directoryName) == false)
                            Directory.CreateDirectory(directoryName);

                        // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                        // of the file, but does not waste memory.
                        // The "using" will close the stream even if an exception occurs.
                        using (var streamWriter = File.Create(fullZipToPath))
                        {
                            StreamUtils.Copy(zipStream, streamWriter, buffer);
                        }
                    }
                }
                finally
                {
                    if (zipFile != null)
                    {
                        zipFile.IsStreamOwner = true; // Makes close also shut the underlying stream
                        zipFile.Close(); // Ensure we release resources
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Error while extracting file {archiveFilenameIn} to path {outFolder}", e);
                return false;
            }
            Logger.LogInformation($"Extracted package {archiveFilenameIn} to path {outFolder}");
            return true;
        }


        // Compresses the files in the nominated folder, and creates a zip file on disk named as outPathname.
        //
        public void CreateZip(string outPathname, string folderName)
        {
            FileStream fsOut = File.Create(outPathname);
            ZipOutputStream zipStream = new ZipOutputStream(fsOut);

            zipStream.SetLevel(3); //0-9, 9 being the highest level of compression

            // This setting will strip the leading part of the folder path in the entries, to
            // make the entries relative to the starting folder.
            // To include the full path for each entry up to the drive root, assign folderOffset = 0.
            int folderOffset = folderName.Length + (folderName.EndsWith("\\") ? 0 : 1);

            CompressFolder(folderName, zipStream, folderOffset);

            zipStream.IsStreamOwner = true; // Makes the Close also Close the underlying stream
            zipStream.Close();
            Logger.LogInformation($"Folder {folderName} was archived to {outPathname}");
        }

        // Recurses down the folder structure
        //
        private void CompressFolder(string path, ZipOutputStream zipStream, int folderOffset)
        {
            string[] files = Directory.GetFiles(path);

            foreach (string filename in files)
            {
                FileInfo fi = new FileInfo(filename);

                string entryName = filename.Substring(folderOffset); // Makes the name in zip based on the folder
                entryName = ZipEntry.CleanName(entryName); // Removes drive from name and fixes slash direction
                ZipEntry newEntry = new ZipEntry(entryName);
                newEntry.DateTime = fi.LastWriteTime; // Note the zip format stores 2 second granularity

                newEntry.Size = fi.Length;

                zipStream.PutNextEntry(newEntry);

                // Zip the file in buffered chunks
                // the "using" will close the stream even if an exception occurs
                byte[] buffer = new byte[4096];
                using (FileStream streamReader = File.OpenRead(filename))
                {
                    StreamUtils.Copy(streamReader, zipStream, buffer);
                }
                zipStream.CloseEntry();
            }
            string[] folders = Directory.GetDirectories(path);
            foreach (string folder in folders)
            {
                CompressFolder(folder, zipStream, folderOffset);
            }
        }
    }
}
