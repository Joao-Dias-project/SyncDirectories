using System.Security.Cryptography;
using System.Timers;

namespace VeeamSoftwareTask
{
    class Program
    {
        private static int syncInterval;
        private static System.Timers.Timer syncTimer;
        private static string logFilePath;
        private static string sourceFolderPath;
        private static string replicaFolderPath;
        
        static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Usage: FolderSync.exe <syncIntervalInMinutes> <logFilePath> <sourceFolderPath> <replicaFolderPath>");
                return;
            }

            
            syncInterval = int.Parse(args[0]);
            logFilePath = args[1];
            sourceFolderPath = args[2];
            replicaFolderPath = args[3];

            syncTimer = new System.Timers.Timer(syncInterval * 60000); 
            syncTimer.Elapsed += OnTimedEvent;
            syncTimer.AutoReset = true;
            syncTimer.Enabled = true;

            Console.WriteLine($"Synchronization started. Interval: {syncInterval} minutes.");
            WriteLog("Synchronization started.");

            SyncDirectories(sourceFolderPath, replicaFolderPath);

            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            SyncDirectories(sourceFolderPath, replicaFolderPath);
        }

        static void SyncDirectories(string sourceDir, string replicaDir)
        {
            try
            {
                DirectoryInfo sourceDirectory = new DirectoryInfo(sourceDir);
                DirectoryInfo replicaDirectory = new DirectoryInfo(replicaDir);

                if (!sourceDirectory.Exists)
                {
                    throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDir);
                }

                if (!replicaDirectory.Exists)
                {
                    Directory.CreateDirectory(replicaDir);
                    WriteLog($"Created directory: {replicaDir}");
                }

                FileInfo[] sourceFiles = sourceDirectory.GetFiles();
                foreach (FileInfo sourceFile in sourceFiles)
                {
                    string replicaFilePath = Path.Combine(replicaDir, sourceFile.Name);
                    if (!File.Exists(replicaFilePath) || !FilesAreEqual(sourceFile.FullName, replicaFilePath))
                    {
                        sourceFile.CopyTo(replicaFilePath, true);
                        WriteLog($"Copied/Updated file: {sourceFile.FullName} to {replicaFilePath}");
                    }
                }

                FileInfo[] replicaFiles = replicaDirectory.GetFiles();
                foreach (FileInfo replicaFile in replicaFiles)
                {
                    string sourceFilePath = Path.Combine(sourceDir, replicaFile.Name);
                    if (!File.Exists(sourceFilePath))
                    {
                        replicaFile.Delete();
                        WriteLog($"Deleted file: {replicaFile.FullName}");
                    }
                }

                DirectoryInfo[] sourceSubDirs = sourceDirectory.GetDirectories();
                foreach (DirectoryInfo sourceSubDir in sourceSubDirs)
                {
                    string replicaSubDirPath = Path.Combine(replicaDir, sourceSubDir.Name);
                    SyncDirectories(sourceSubDir.FullName, replicaSubDirPath);
                }

                DirectoryInfo[] replicaSubDirs = replicaDirectory.GetDirectories();
                foreach (DirectoryInfo replicaSubDir in replicaSubDirs)
                {
                    string sourceSubDirPath = Path.Combine(sourceDir, replicaSubDir.Name);
                    if (!Directory.Exists(sourceSubDirPath))
                    {
                        replicaSubDir.Delete(true);
                        WriteLog($"Deleted directory: {replicaSubDir.FullName}");
                    }
                }

                WriteLog("Synchronization completed successfully.");
            }
            catch (Exception ex)
            {
                WriteLog("An error occurred during synchronization: " + ex.Message);
            }
        }

        static bool FilesAreEqual(string filePath1, string filePath2)
        {
            using (var hashAlgorithm = MD5.Create())
            {
                using (var stream1 = new FileStream(filePath1, FileMode.Open, FileAccess.Read))
                using (var stream2 = new FileStream(filePath2, FileMode.Open, FileAccess.Read))
                {
                    byte[] hash1 = hashAlgorithm.ComputeHash(stream1);
                    byte[] hash2 = hashAlgorithm.ComputeHash(stream2);

                    for (int i = 0; i < hash1.Length; i++)
                    {
                        if (hash1[i] != hash2[i])
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
        }

        static void WriteLog(string message)
        {
            string logMessage = $"{DateTime.Now}: {message}";
            Console.WriteLine(logMessage);
            try
            {
                using (StreamWriter sw = new StreamWriter(logFilePath, true))
                {
                    sw.WriteLine(logMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while writing to the log file: " + ex.Message);
            }
        }
    }
}
