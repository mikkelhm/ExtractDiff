using System;
using System.IO;
using System.Net;

namespace ExtractDiff.Core
{
    public class PackageManager
    {
        private readonly string _downloadUrl;

        public PackageManager(string downloadUrl)
        {
            _downloadUrl = downloadUrl;
        }

        public void EnsurePackage(string packageName, Version version, string downloadLocation)
        {
            if (File.Exists(downloadLocation))
            {
                Logger.LogInformation($"Package {downloadLocation} is already present, skipping download");
                return;
            }

            using (var client = new WebClient())
            {
                var url = _downloadUrl.Replace("{version}", version.ToString(3));
                Logger.LogInformation($"Downloading package from location {url}");
                client.DownloadFile(new Uri(url), downloadLocation);
                Logger.LogInformation($"Package {downloadLocation} successfully downloaded");
            }
        }

        public string GetVersionFromPackage(string packagePath, string packageNamePart)
        {
            Logger.LogInformation($"Trying to get version form file {packagePath}");
            var fileName = Path.GetFileNameWithoutExtension(packagePath);
            var versionPart = fileName.Substring(packageNamePart.Length);
            Logger.LogInformation($"Returning version {versionPart} from filePath {packagePath}");
            return versionPart;
        }
    }
}
