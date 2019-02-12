using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace ExtractDiff
{
    public class PackageDownloader
    {
        private readonly string _downloadUrl;

        public PackageDownloader(string downloadUrl)
        {
            _downloadUrl = downloadUrl;
        }
        //private string formsUrl =
        //    "https://umbraconightlies.blob.core.windows.net/umbraco-forms-release/UmbracoForms.Files.{version}.zip";

        //private string deployUrl =
        //    "https://umbraconightlies.blob.core.windows.net/umbraco-deploy-release/UmbracoDeploy.v{version}.zip";

        //private static string umbracoUrl = "";

        public  void EnsurePackage(string packageName, Version version, string downloadLocation)
        {
            if (File.Exists(downloadLocation))
                return;
            using (var client = new WebClient())
            {
                var url = _downloadUrl.Replace("{version}", version.ToString(3));
                client.DownloadFile(new Uri(url), downloadLocation);
            }
        }
    }
}
