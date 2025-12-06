
using System;
using System.IO;
using UnityEngine;
using System.Runtime.ExceptionServices;

namespace PhaseAgnostic_Tooling_Tests
{
    public static class UlpTestFileOutput
    {
        private const string FILE_EXT = ".ulpData";

        internal const string DATE_FORMAT = "MM-dd-yy";

        private const string PARENT_DIRECTORY = "Ulp Test Output";

        private static readonly string Root_Path = Path.Join(Directory.GetParent(Application.dataPath).FullName, PARENT_DIRECTORY);

        public static ExceptionDispatchInfo WriteTextToFile(string fileName, string data)
        {
            /// Should be wary of this logic. It is naive file-output that will most likely fail under concurrency and/or parallelism.
            
            ConditionalDirectoryCreation(Root_Path);

            DateTime t = DateTime.Now;

            var dString = t.ToString(DATE_FORMAT);

            string subDirectory = Path.Join(Root_Path, dString);

            ConditionalDirectoryCreation(subDirectory);

            string fileWithExt = fileName + FILE_EXT;

            string filePath = Path.Join(subDirectory, fileWithExt);

            ExceptionDispatchInfo edi = null;

            try
            {
                File.WriteAllText(filePath, data);

                UnityEngine.Debug.Log($"File Output Written:\n{filePath}");
            }

            catch (Exception ex)
            {
                edi = ExceptionDispatchInfo.Capture(ex);
            }

            return edi;
        }

        private static void ConditionalDirectoryCreation(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);

                    UnityEngine.Debug.Log($"Creating Directory @ {path}");
                }
            }

            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}