using ILRepacking;
using Mono.Cecil;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ILRepack.IntegrationTests.NuGet
{
    public static class TestHelpers
    {
        public static string GenerateTempFolder()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public static void CleanupTempFolder(ref string tempDirectory)
        {
            if (tempDirectory == null || !Directory.Exists(tempDirectory)) return;
            Directory.Delete(tempDirectory, true);
            tempDirectory = null;
        }

        public static void DoRepackForCmd(params string[] args)
        {
            DoRepackForCmd((IEnumerable<string>)args);
        }

        public static void DoRepackForCmd(IEnumerable<string> args)
        {
            var repackOptions = new RepackOptions(args.Concat(new[] { "/log" }));
            var repack = new ILRepacking.ILRepack(repackOptions);
            repack.Repack();
            ReloadAndCheckReferences(repackOptions);
        }

        private static void ReloadAndCheckReferences(RepackOptions repackOptions)
        {
            var loaded = new List<AssemblyDefinition>();

            var outputFile = AssemblyDefinition.ReadAssembly(repackOptions.OutputFile, new ReaderParameters(ReadingMode.Immediate));
            loaded.Add(outputFile);

            var mergedFiles = repackOptions.ResolveFiles().Select(f =>
            {
                var def = AssemblyDefinition.ReadAssembly(f, new ReaderParameters(ReadingMode.Deferred));
                loaded.Add(def);
                return def;
            });

            try
            {
                foreach (var a in outputFile.MainModule.AssemblyReferences.Where(x => mergedFiles.Any(y => repackOptions.KeepOtherVersionReferences ? x.FullName == y.FullName : x.Name == y.Name.Name)))
                {
                    Assert.Fail($"Merged assembly retains a reference to one (or more) of the merged files: {a.FullName}");
                }
            }
            finally
            {
                foreach (var a in loaded)
                    a.Dispose();
            }
        }

        public static void SaveAs(Stream input, string directory, string fileName)
        {
            var path = Path.Combine(directory, Path.GetFileName(fileName));
            using (var stream = input)
            using (var file = new FileStream(path, FileMode.Create))
            {
                stream.CopyTo(file);
            }
        }
    }
}
