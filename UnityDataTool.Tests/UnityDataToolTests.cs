using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using UnityDataTools.FileSystem;

namespace UnityDataTools.UnityDataTool.Tests
{
    [TestFixtureSource(typeof(TestData), nameof(TestData.GetTestFolders))]
    public class UnityDataToolTests
    {
        private string m_TestOutputFolder;
        private string m_TestFolder;

        public UnityDataToolTests(string testFolder)
        {
            m_TestFolder = testFolder;
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            TestData.LoadTestData();
            
            m_TestOutputFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "test_folder");
            Directory.CreateDirectory(m_TestOutputFolder);
            Directory.SetCurrentDirectory(m_TestOutputFolder);
        }

        [TearDown]
        public void Teardown()
        {
            foreach (var file in new DirectoryInfo(m_TestOutputFolder).EnumerateFiles())
            {
                file.Delete();
            }
        }
        
        [Test]
        public void ArchiveExtract_FilesExtractedSuccessfully()
        {
            var path = Path.Combine(m_TestFolder, "AssetBundles", "assetbundle");

            Assert.AreEqual(0, Program.Main(new string[] { "archive", "extract", path }));
            Assert.IsTrue(File.Exists(Path.Combine(m_TestOutputFolder, "CAB-5d40f7cad7c871cf2ad2af19ac542994")));
            Assert.IsTrue(File.Exists(Path.Combine(m_TestOutputFolder, "CAB-5d40f7cad7c871cf2ad2af19ac542994.resS")));
            Assert.IsTrue(File.Exists(Path.Combine(m_TestOutputFolder, "CAB-5d40f7cad7c871cf2ad2af19ac542994.resource")));
        }

        [Test]
        public void ArchiveList_ListFilesCorrectly()
        {
            var path = Path.Combine(m_TestFolder, "AssetBundles", "assetbundle");

            using var sw = new StringWriter();

            var currentOut = Console.Out;
            Console.SetOut(sw);

            Assert.AreEqual(0, Program.Main(new string[] { "archive", "list", path }));

            var lines = sw.ToString().Split(sw.NewLine);

            Assert.AreEqual("CAB-5d40f7cad7c871cf2ad2af19ac542994", lines[0]);
            Assert.AreEqual($"  Size: {TestData.ExpectedValues[m_TestFolder]["CAB-5d40f7cad7c871cf2ad2af19ac542994-Size"]}", lines[1]);
            Assert.AreEqual($"  Flags: {(ArchiveNodeFlags)TestData.ExpectedValues[m_TestFolder]["CAB-5d40f7cad7c871cf2ad2af19ac542994-Flags"]}", lines[2]);

            Assert.AreEqual("CAB-5d40f7cad7c871cf2ad2af19ac542994.resS", lines[4]);
            Assert.AreEqual($"  Size: {TestData.ExpectedValues[m_TestFolder]["CAB-5d40f7cad7c871cf2ad2af19ac542994.resS-Size"]}", lines[5]);
            Assert.AreEqual($"  Flags: {(ArchiveNodeFlags)TestData.ExpectedValues[m_TestFolder]["CAB-5d40f7cad7c871cf2ad2af19ac542994.resS-Flags"]}", lines[6]);

            Assert.AreEqual("CAB-5d40f7cad7c871cf2ad2af19ac542994.resource", lines[8]);
            Assert.AreEqual($"  Size: {TestData.ExpectedValues[m_TestFolder]["CAB-5d40f7cad7c871cf2ad2af19ac542994.resource-Size"]}", lines[9]);
            Assert.AreEqual($"  Flags: {(ArchiveNodeFlags)TestData.ExpectedValues[m_TestFolder]["CAB-5d40f7cad7c871cf2ad2af19ac542994.resource-Flags"]}", lines[10]);

            Console.SetOut(currentOut);
        }

        [Test]
        public void DumpText_DefaultArgs_TextFileCreatedCorrectly(
            [Values("", "-f text", "--output-format text")] string options)
        {
            var path = Path.Combine(m_TestFolder, "AssetBundles", "assetbundle");
            var outputFile = Path.Combine(m_TestOutputFolder, "CAB-5d40f7cad7c871cf2ad2af19ac542994.txt");

            Assert.AreEqual(0, Program.Main(new string[] { "dump", path }.Concat(options.Split(" ", StringSplitOptions.RemoveEmptyEntries)).ToArray()));
            Assert.IsTrue(File.Exists(outputFile));

            var content = File.ReadAllText(outputFile);
            var expected = File.ReadAllText(Path.Combine(m_TestFolder, "ExpectedData", "dump", "CAB-5d40f7cad7c871cf2ad2af19ac542994.txt"));

            // Normalize  line endings.
            content = Regex.Replace(content, @"\r\n|\n\r|\r", "\n");
            expected = Regex.Replace(expected, @"\r\n|\n\r|\r", "\n");

            Assert.AreEqual(expected, content);
        }

        [Test]
        public void DumpText_SkipLargeArrays_TextFileCreatedCorrectly(
            [Values("-s", "--skip-large-arrays")] string options)
        {
            var path = Path.Combine(m_TestFolder, "AssetBundles", "assetbundle");
            var outputFile = Path.Combine(m_TestOutputFolder, "CAB-5d40f7cad7c871cf2ad2af19ac542994.txt");

            Assert.AreEqual(0, Program.Main(new string[] { "dump", path }.Concat(options.Split(" ", StringSplitOptions.RemoveEmptyEntries)).ToArray()));
            Assert.IsTrue(File.Exists(outputFile));

            var content = File.ReadAllText(outputFile);
            var expected = File.ReadAllText(Path.Combine(m_TestFolder, "ExpectedData", "dump-s", "CAB-5d40f7cad7c871cf2ad2af19ac542994.txt"));

            // Normalize  line endings.
            content = Regex.Replace(content, @"\r\n|\n\r|\r", "\n");
            expected = Regex.Replace(expected, @"\r\n|\n\r|\r", "\n");

            Assert.AreEqual(expected, content);
        }

        [Test]
        public void Analyze_DefaultArgs_DatabaseCorrect()
        {
            var databasePath = Path.Combine(m_TestOutputFolder, "database.db");
            var analyzePath = Path.Combine(m_TestFolder, "AssetBundles");

            Assert.AreEqual(0, Program.Main(new string[] { "analyze", analyzePath }));

            ValidateDatabase(databasePath, false);
        }

        [Test]
        public void Analyze_WithRefs_DatabaseCorrect(
            [Values("-r", "--extract-references")] string options)
        {
            var databasePath = Path.Combine(m_TestOutputFolder, "database.db");
            var analyzePath = Path.Combine(m_TestFolder, "AssetBundles");

            Assert.AreEqual(0, Program.Main(new string[] { "analyze", analyzePath }.Concat(options.Split(" ")).ToArray()));

            ValidateDatabase(databasePath, true);
        }

        [Test]
        public void Analyze_WithPattern_DatabaseCorrect(
            [Values("-p *.", "--search-pattern *.")] string options)
        {
            var databasePath = Path.Combine(m_TestOutputFolder, "database.db");
            var analyzePath = Path.Combine(m_TestFolder, "AssetBundles");

            Assert.AreEqual(0, Program.Main(new string[] { "analyze", analyzePath }.Concat(options.Split(" ")).ToArray()));

            ValidateDatabase(databasePath, false);
        }

        [Test]
        public void Analyze_WithPatternNoMatch_DatabaseEmpty(
            [Values("-p *.x", "--search-pattern *.x")] string options)
        {
            var databasePath = Path.Combine(m_TestOutputFolder, "database.db");
            var analyzePath = Path.Combine(m_TestFolder, "AssetBundles");

            Assert.AreEqual(0, Program.Main(new string[] { "analyze", analyzePath }.Concat(options.Split(" ")).ToArray()));

            using var db = new SQLiteConnection($"Data Source={databasePath};Version=3;New=True;Foreign Keys=False;");
            db.Open();

            using (var cmd = db.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM objects";

                Assert.AreEqual(0, cmd.ExecuteScalar());
            }
        }

        [Test]
        public void Analyze_WithOutputFile_DatabaseCorrect(
            [Values("-o my_database", "--output-file my_database")] string options)
        {
            var databasePath = Path.Combine(m_TestOutputFolder, "my_database");
            var analyzePath = Path.Combine(m_TestFolder, "AssetBundles");

            Assert.AreEqual(0, Program.Main(new string[] { "analyze", analyzePath }.Concat(options.Split(" ")).ToArray()));

            ValidateDatabase(databasePath, false);
        }

        private void ValidateDatabase(string databasePath, bool withRefs)
        {
            using var db = new SQLiteConnection($"Data Source={databasePath};Version=3;New=True;Foreign Keys=False;");
            db.Open();

            using (var cmd = db.CreateCommand())
            {
                cmd.CommandText =
                    @"SELECT 
                    (SELECT COUNT(*) FROM animation_clips),
                    (SELECT COUNT(*) FROM asset_bundles),
                    (SELECT COUNT(*) FROM assets),
                    (SELECT COUNT(*) FROM audio_clips),
                    (SELECT COUNT(*) FROM meshes),
                    (SELECT COUNT(*) FROM objects),
                    (SELECT COUNT(*) FROM refs),
                    (SELECT COUNT(*) FROM serialized_files),
                    (SELECT COUNT(*) FROM shader_subprograms),
                    (SELECT COUNT(*) FROM shaders),
                    (SELECT COUNT(*) FROM shader_keywords),
                    (SELECT COUNT(*) FROM shader_subprogram_keywords),
                    (SELECT COUNT(*) FROM textures),
                    (SELECT COUNT(*) FROM types)";

                using var reader = cmd.ExecuteReader();

                reader.Read();

                Assert.AreEqual(TestData.ExpectedValues[m_TestFolder]["animation_clips_count"], reader.GetInt32(0));
                Assert.AreEqual(TestData.ExpectedValues[m_TestFolder]["asset_bundles_count"], reader.GetInt32(1));
                Assert.AreEqual(TestData.ExpectedValues[m_TestFolder]["assets_count"], reader.GetInt32(2));
                Assert.AreEqual(TestData.ExpectedValues[m_TestFolder]["audio_clips_count"], reader.GetInt32(3));
                Assert.AreEqual(TestData.ExpectedValues[m_TestFolder]["meshes_count"], reader.GetInt32(4));
                Assert.AreEqual(TestData.ExpectedValues[m_TestFolder]["objects_count"], reader.GetInt32(5));
                Assert.AreEqual(withRefs ? TestData.ExpectedValues[m_TestFolder]["refs_count"] : 0, reader.GetInt32(6));
                Assert.AreEqual(TestData.ExpectedValues[m_TestFolder]["serialized_files_count"], reader.GetInt32(7));
                Assert.AreEqual(TestData.ExpectedValues[m_TestFolder]["shader_subprograms_count"], reader.GetInt32(8));
                Assert.AreEqual(TestData.ExpectedValues[m_TestFolder]["shaders_count"], reader.GetInt32(9));
                Assert.AreEqual(TestData.ExpectedValues[m_TestFolder]["shader_keywords_count"], reader.GetInt32(10));
                Assert.AreEqual(TestData.ExpectedValues[m_TestFolder]["shader_subprogram_keywords_count"], reader.GetInt32(11));
                Assert.AreEqual(TestData.ExpectedValues[m_TestFolder]["textures_count"], reader.GetInt32(12));
                Assert.AreEqual(TestData.ExpectedValues[m_TestFolder]["types_count"], reader.GetInt32(13));
            }
        }
    }
}
