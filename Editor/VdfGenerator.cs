using System.Collections.Generic;
using System.Text;

namespace Tomicz.Deployer
{
    /// <summary>
    /// Builds the SteamPipe VDF scripts as strings. No Unity or file system dependencies, so it can be unit tested.
    /// </summary>
    public static class VdfGenerator
    {
        /// <param name="depotScripts">Depot ID paired with the absolute path of that depot's VDF script.</param>
        public static string AppBuild(string appId, string description, string buildOutputPath, string setLiveBranch, IEnumerable<KeyValuePair<string, string>> depotScripts)
        {
            StringBuilder vdf = new StringBuilder();

            vdf.Append("appbuild\n{\n");
            vdf.Append($"\t\"appid\" \"{appId}\"\n");
            vdf.Append($"\t\"desc\" \"{description}\"\n");
            vdf.Append($"\t\"buildoutput\" \"{buildOutputPath}\"\n");
            vdf.Append("\t\"contentroot\" \"\"\n");
            vdf.Append($"\t\"setlive\" \"{setLiveBranch}\"\n");
            vdf.Append("\t\"preview\" \"0\"\n");
            vdf.Append("\t\"local\" \"\"\n");
            vdf.Append("\t\"depots\"\n\t{\n");

            foreach (KeyValuePair<string, string> depot in depotScripts)
            {
                vdf.Append($"\t\t\"{depot.Key}\" \"{depot.Value}\"\n");
            }

            vdf.Append("\t}\n}\n");

            return vdf.ToString();
        }

        /// <param name="localPath">Files to upload, relative to contentRoot. Wildcards allowed. Empty means everything.</param>
        public static string DepotBuild(string depotId, string contentRoot, string localPath)
        {
            if (string.IsNullOrEmpty(localPath))
            {
                localPath = "*";
            }

            return "DepotBuildConfig\n{\n" +
                   $"\t\"DepotID\" \"{depotId}\"\n" +
                   $"\t\"contentroot\" \"{contentRoot}\"\n" +
                   "\t\"FileMapping\"\n\t{\n" +
                   $"\t\t\"LocalPath\" \"{localPath}\"\n" +
                   "\t\t\"DepotPath\" \".\"\n" +
                   "\t\t\"recursive\" \"1\"\n" +
                   "\t}\n" +
                   "\t\"FileExclusion\" \"*.pdb\"\n" +
                   "}\n";
        }
    }
}
