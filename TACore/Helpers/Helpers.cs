using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using KNFoundation;
using ICSharpCode.SharpZipLib.Zip;

namespace TACore {
    public class Helpers {

        public static string PathByAppendingUniqueFileNameFromFileNameInPath(string suggestedFileName, string directoryPath) {

            // This method passes back a unique file name for the passed file and path. 
            // So, for example, if the caller wants to put a file called "Hello.txt" in ~/Desktop
            // and that file already exists, it'll give back ~/Desktop/Hello 2.txt".
            // The method respects extensions and will keep incrementing the number until it finds a unique name. 

            if (suggestedFileName == null || suggestedFileName.Length == 0 || directoryPath == null) {
                return null;
            }

            Boolean fileMade = false;
            int uNum = 2;

            if (!File.Exists(Path.Combine(directoryPath, suggestedFileName))) {
                return Path.Combine(directoryPath, suggestedFileName);
            } else {

                while (!fileMade) {

                    string newName = String.Format("{0} {1}{2}",
                        Path.GetFileNameWithoutExtension(suggestedFileName),
                        uNum,
                        Path.GetExtension(suggestedFileName));

                    string newPathPerhaps = Path.Combine(directoryPath, newName);
                    if (File.Exists(newPathPerhaps)) {
                        uNum++;
                    } else {
                        fileMade = true;
                        return newPathPerhaps;
                    }

                }

            }

            // If here, something went very wrong

            return Path.Combine(directoryPath, suggestedFileName);
        }

        public static void ExtractZipFileAtPathToDirectory(string zipFilePath, string directoryPath) {
            ExtractZipFileInStreamToDirectory(File.OpenRead(zipFilePath), directoryPath);
        }

        public static void ExtractZipFileInStreamToDirectory(Stream stream, string directoryPath) {

            using (ZipInputStream s = new ZipInputStream(stream)) {

                ZipEntry theEntry;
                while ((theEntry = s.GetNextEntry()) != null) {

                    string directoryName = Path.Combine(directoryPath, Path.GetDirectoryName(theEntry.Name));
                    string fileName = Path.GetFileName(theEntry.Name);

                    // create directory
                    if (directoryName.Length > 0) {
                        Directory.CreateDirectory(directoryName);

                        
                    }

                    if (fileName != String.Empty) {
                        using (FileStream streamWriter = File.Create(Path.Combine(directoryName, fileName))) {

                            int size = 2048;
                            byte[] data = new byte[2048];
                            while (true) {
                                size = s.Read(data, 0, data.Length);
                                if (size > 0) {
                                    streamWriter.Write(data, 0, size);
                                } else {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void ArchiveInstallToZipFileAtDirectoryForBackup(WoWInstall install, string zipFilePath) {

            List<string> foldersToZip = new List<string>();

            string wtfPath = Path.Combine(install.DirectoryPath, Constants.kWoWWTFFolderName);
            if (Directory.Exists(wtfPath)) {
                foldersToZip.Add(wtfPath);
            }

            string interfacePath = Path.Combine(install.DirectoryPath, Constants.kWoWInterfaceFolderName);
            if (Directory.Exists(interfacePath)) {
                foldersToZip.Add(interfacePath);
            }

            if (foldersToZip.Count == 0) {
				throw new BackupException(BackupFailureReason.NothingToBackup);
            }

            ZipOutputStream s = new ZipOutputStream(File.Create(zipFilePath));
            s.SetLevel(9); // 0 - store only to 9 - means best compression

            foreach (string directoryPath in foldersToZip) {
                AddFolderAtPathToZipStream(directoryPath, install.DirectoryPath, s);
            }

            // Finish is important to ensure trailing information for a Zip file is appended.  Without this
            // the created file would be invalid.
            s.Finish();

            // Close is important to wrap things up and unlock the file.
            s.Close();
        }


        private static void AddFolderAtPathToZipStream(string folder, string zipRoot, ZipOutputStream stream) {

            DirectoryInfo dirInfo = new DirectoryInfo(folder);

            if (dirInfo.Exists) {

                foreach (FileInfo file in dirInfo.GetFiles()) {

                    string filePath = file.FullName;
                    string relativePath = filePath.Replace(zipRoot, "");
                    if (relativePath.StartsWith("\\")) {
                        // Directory paths in zips start with no slash
                        relativePath = relativePath.Remove(0, 1);
                        relativePath = relativePath.Replace("\\", "/");
                    }

                    byte[] fileData = File.ReadAllBytes(filePath);
                    
                    ZipEntry fileEntry = new ZipEntry(relativePath);
                    fileEntry.Size = fileData.Length;

                    stream.PutNextEntry(fileEntry);

                    stream.Write(fileData, 0, fileData.Length);

                }

                foreach (DirectoryInfo directory in dirInfo.GetDirectories()) {
                    AddFolderAtPathToZipStream(directory.FullName, zipRoot, stream);
                }

            }

        }
    }
}
