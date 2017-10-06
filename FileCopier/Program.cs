using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;

namespace FileCopier
{
    internal static class Program
    {
        internal static readonly string DestinationPath = ConfigurationManager.AppSettings["DestinationPath"];
        internal static string[] FileList = ConfigurationManager.AppSettings["FileList"].Split(',');
        internal static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        internal static readonly string SourcePath = ConfigurationManager.AppSettings["SourcePath"];
        internal static readonly bool VerifyModifyDate = Convert.ToBoolean(ConfigurationManager.AppSettings["VerifyModifyDate"]);

        private static void Main(string[] args)
        {
            if (HelpArgumentsProvided(args)) return;

            Log.Info("Starting.");

            try
            {
                if (DestinationPathExists() == false)
                {
                    Log.Info($"Destination path does not exist. The folder will be created on: {DestinationPath}");

                    Directory.CreateDirectory(DestinationPath);
                }

                CopyFiles();
            }
            catch (Exception ex)
            {
                Log.Info("Process failed. Exception: ", ex);
            }
            finally
            {
                Log.Info("Finished.");
            }
        }

        private static void CopyFiles()
        {
            ValidateFileList();

            foreach (var fileName in FileList)
            {
                var sourceFile = Path.Combine(SourcePath, fileName);
                var destinationFile = Path.Combine(DestinationPath, fileName);

                if (SourceFileExists(sourceFile))
                {
                    if (NeedsToUpdateFile(destinationFile))
                    {
                        if (VerifyWriteDate(sourceFile, destinationFile))
                        {
                            File.Copy(sourceFile, destinationFile, true);
                            Log.Info($"{fileName} has been updated on {DestinationPath}.");
                        }
                        else
                        {
                            Log.Info($"{fileName} is up-to-date. No changes will be made.");
                        }
                    }
                    else
                    {
                        File.Copy(sourceFile, destinationFile, true);
                        Log.Info($"{fileName} has been copied/replaced on {DestinationPath}");
                    }
                }
                else
                {
                    Log.Info($"{fileName} could not be found on {SourcePath}.");
                }
            }
        }

        private static void ValidateFileList()
        {
            if (FileList.Length == 1 && string.IsNullOrWhiteSpace(FileList.First()))
            {
                Log.Info("A file list wasn't provided. All files on the source folder will be copied to the destination folder.");

                FileList = Directory.GetFiles(SourcePath).Select(Path.GetFileName).ToArray();
            }
        }

        private static bool NeedsToUpdateFile(string destinationFile)
        {
            var needsToUpdate = VerifyModifyDate && File.Exists(destinationFile);

            return needsToUpdate;
        }

        private static bool DestinationPathExists()
        {
            var exists = Directory.Exists(DestinationPath);

            return exists;
        }

        private static bool SourceFileExists(string sourceFile)
        {
            var exists = File.Exists(sourceFile);

            return exists;
        }

        private static bool VerifyWriteDate(string sourceFile, string destinationFile)
        {
            var fromSource = File.GetLastWriteTime(sourceFile);
            var fromDestionation = File.GetLastWriteTime(destinationFile);

            var value = fromSource > fromDestionation;

            return value;
        }

        private static bool HelpArgumentsProvided(IEnumerable<string> args)
        {
            var showHelp = args.Select(s => s.ToLowerInvariant()).Intersect(new[] { "help", "/?", "?", "--help", "-help", "-h" }).Any();

            if (showHelp == false) return false;

            ShowHelp();

            return true;
        }

        private static void ShowHelp()
        {
            var helpMessage = new StringBuilder();

            helpMessage.AppendLine("How to use: ");
            helpMessage.AppendLine("");
            helpMessage.AppendLine("On your App.Config file, search for the app.Settings section. There, you will find the necessary keys you will need to provide:");
            helpMessage.AppendLine("'SourcePath' - This is the folder where your files are stored.");
            helpMessage.AppendLine("'DestinationPath' - This is the folder where you want your files to be copied into.");
            helpMessage.AppendLine("'VerifyModifyDate' - This flag indicates if the last modification date should be considered to replace files.");
            helpMessage.AppendLine("'FileList' - The file list to be copied on the source folder (separated by comma). If this list isn't provided, everything will be copied.");
            helpMessage.AppendLine("");
            helpMessage.AppendLine("Press any key to exit.");

            Console.Write(helpMessage.ToString());
            Console.ReadKey();
        }
    }
}