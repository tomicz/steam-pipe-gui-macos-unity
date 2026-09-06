using System.Collections.Generic;
using NUnit.Framework;

namespace Tomicz.Deployer.Tests
{
    public class VdfGeneratorTests
    {
        private static readonly List<KeyValuePair<string, string>> SingleDepot = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("1001", "/sdk/tools/ContentBuilder/scripts/depot_build_1001.vdf")
        };

        [Test]
        public void AppBuild_WritesAppIdDescriptionOutputAndBranch()
        {
            string vdf = VdfGenerator.AppBuild("1000", "Test 1.0", "/sdk/tools/ContentBuilder/output/StandaloneOSX", "beta", SingleDepot);

            Assert.That(vdf, Does.StartWith("appbuild\n{\n"));
            Assert.That(vdf, Does.Contain("\t\"appid\" \"1000\"\n"));
            Assert.That(vdf, Does.Contain("\t\"desc\" \"Test 1.0\"\n"));
            Assert.That(vdf, Does.Contain("\t\"buildoutput\" \"/sdk/tools/ContentBuilder/output/StandaloneOSX\"\n"));
            Assert.That(vdf, Does.Contain("\t\"setlive\" \"beta\"\n"));
            Assert.That(vdf, Does.EndWith("}\n"));
        }

        [Test]
        public void AppBuild_EmptyBranch_WritesEmptySetLive()
        {
            string vdf = VdfGenerator.AppBuild("1000", "Test", "/out", "", SingleDepot);

            Assert.That(vdf, Does.Contain("\t\"setlive\" \"\"\n"));
        }

        [Test]
        public void AppBuild_ListsEveryDepotWithItsScriptPath()
        {
            List<KeyValuePair<string, string>> depots = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("1001", "/scripts/depot_build_1001.vdf"),
                new KeyValuePair<string, string>("1002", "/scripts/depot_build_1002.vdf")
            };

            string vdf = VdfGenerator.AppBuild("1000", "Test", "/out", "beta", depots);

            Assert.That(vdf, Does.Contain("\t\"depots\"\n\t{\n\t\t\"1001\" \"/scripts/depot_build_1001.vdf\"\n\t\t\"1002\" \"/scripts/depot_build_1002.vdf\"\n\t}\n"));
        }

        [Test]
        public void DepotBuild_WritesDepotIdContentRootAndLocalPath()
        {
            string vdf = VdfGenerator.DepotBuild("1001", "/sdk/tools/ContentBuilder/content/StandaloneOSX", "DLC/*");

            Assert.That(vdf, Does.StartWith("DepotBuildConfig\n{\n"));
            Assert.That(vdf, Does.Contain("\t\"DepotID\" \"1001\"\n"));
            Assert.That(vdf, Does.Contain("\t\"contentroot\" \"/sdk/tools/ContentBuilder/content/StandaloneOSX\"\n"));
            Assert.That(vdf, Does.Contain("\t\t\"LocalPath\" \"DLC/*\"\n"));
            Assert.That(vdf, Does.Contain("\t\t\"DepotPath\" \".\"\n"));
            Assert.That(vdf, Does.Contain("\t\t\"recursive\" \"1\"\n"));
            Assert.That(vdf, Does.Contain("\t\"FileExclusion\" \"*.pdb\"\n"));
        }

        [Test]
        public void DepotBuild_EmptyLocalPath_UploadsEverything()
        {
            string vdf = VdfGenerator.DepotBuild("1001", "/content", "");

            Assert.That(vdf, Does.Contain("\t\t\"LocalPath\" \"*\"\n"));
        }
    }
}
